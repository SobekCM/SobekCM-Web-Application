using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SobekCM.ImageServer;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

// ***** TEMPORARY TEST SITE *****
// Spike for the JPEG2000/GCS latency question: stage a JP2 out of GCS into a local scratch folder on
// request, so a separately-configured iipsrv IIS handler can serve DeepZoom tiles from local disk without
// the main SobekCM web app ever touching image bytes itself. See the main app's JPEG2000_ItemViewer.cs for
// the calling side of this contract.

// One-off local tool: `dotnet run -- generate-key` prints a fresh shared key to paste into both sides'
// SharedKeyPath files. Not part of the running service.
if (args.Length > 0 && args[0] == "generate-key")
{
    Console.WriteLine(StageTokenCipher.GenerateBase64Key());
    return;
}

var builder = WebApplication.CreateBuilder(args);

ImageServerOptions options = builder.Configuration.GetSection("ImageServer").Get<ImageServerOptions>()
    ?? throw new InvalidOperationException("Missing \"ImageServer\" configuration section.");

GoogleCredential credential = CredentialFactory.FromFile<ServiceAccountCredential>(options.GcsServiceAccountJsonPath).ToGoogleCredential();
StorageClient storageClient = StorageClient.Create(credential);
byte[] sharedKey = Convert.FromBase64String(File.ReadAllText(options.SharedKeyPath).Trim());
var cache = new MemoryCache(new MemoryCacheOptions());

// The GCS credential/bucket connectivity itself is validated once above at startup (a bad key or missing
// permissions would already have thrown before this line) -- the one thing actually worth re-checking on
// an ongoing basis is the scratch folder, since that can go bad independently later (disk full, folder
// permissions changed, folder deleted out from under the running process) without the app ever restarting.
builder.Services.AddHealthChecks().AddCheck("scratchFolder", () =>
{
    try
    {
        if (!Directory.Exists(options.ScratchFolder))
            return HealthCheckResult.Unhealthy("Scratch folder does not exist: " + options.ScratchFolder);

        string probePath = Path.Combine(options.ScratchFolder, ".healthcheck-" + Guid.NewGuid().ToString("N"));
        File.WriteAllText(probePath, string.Empty);
        File.Delete(probePath);

        return HealthCheckResult.Healthy();
    }
    catch (Exception ee)
    {
        return HealthCheckResult.Unhealthy("Scratch folder is not writable: " + ee.Message);
    }
});

// Coalesces concurrent /render requests for the same file (e.g. a class of 40 all clicking the same page
// within moments of each other) into a single in-flight GCS download, instead of each racing an independent
// download. Lazy<Task<T>> is the standard trick around ConcurrentDictionary.GetOrAdd's own gotcha -- its
// valueFactory isn't guaranteed to run only once under contention, but constructing a Lazy is cheap/inert,
// so even if two get constructed, only the one that actually wins the slot ever has .Value touched, and
// that's what actually starts the download. See stage_and_get_dzi_source_path below.
var inFlightStagingRequests = new ConcurrentDictionary<string, Lazy<Task<string>>>();

// Tracks every scratch file currently backed by a live cache entry, so the periodic sweep below can tell
// a file that's genuinely still in use apart from an orphan -- a file's on-disk LastWriteTimeUtc never
// changes after it's written, but its cache entry can outlive MaxCacheMinutes indefinitely under sliding
// expiration if it keeps getting requested, so sweeping by age alone would eventually delete a file out
// from under an active viewer. Added right before cache.Set, removed in the post-eviction callback.
var liveScratchPaths = new ConcurrentDictionary<string, byte>();

if (!Directory.Exists(options.ScratchFolder))
    Directory.CreateDirectory(options.ScratchFolder);

if (options.EnableJp2PullLogging && !string.IsNullOrWhiteSpace(options.Jp2PullLogPath))
{
    string logDirectory = Path.GetDirectoryName(options.Jp2PullLogPath);
    if (!string.IsNullOrEmpty(logDirectory) && !Directory.Exists(logDirectory))
        Directory.CreateDirectory(logDirectory);
}

var jp2PullLogLock = new object();

var app = builder.Build();

// X-Forwarded-For / X-Forwarded-Proto are honored ONLY from the proxies listed in ImageServer:TrustedProxies.
// Any client can send those headers itself, so trusting them from every source would let a client choose the
// IP recorded in the JP2 pull log (and anything keyed on it later). Under IIS in-process hosting there is no
// proxy hop at all and Connection.RemoteIpAddress is already the real client, so the default -- an empty list
// -- ignores the headers entirely. List addresses or CIDR ranges only for a proxy or load balancer that
// actually sits in front of IIS and overwrites these headers.
if (options.TrustedProxies.Count > 0)
{
    var forwardedHeadersOptions = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
        ForwardLimit = 1
    };
    forwardedHeadersOptions.KnownIPNetworks.Clear();
    forwardedHeadersOptions.KnownProxies.Clear();
    foreach (string trustedProxy in options.TrustedProxies)
    {
        if (trustedProxy.Contains('/'))
            forwardedHeadersOptions.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(trustedProxy));
        else
            forwardedHeadersOptions.KnownProxies.Add(IPAddress.Parse(trustedProxy));
    }
    app.UseForwardedHeaders(forwardedHeadersOptions);
}

// Phase 0 of the rate-limiting plan: masks a client IP down to the subnet key that the future token-bucket
// limiters will actually key on, so the week-long baseline log lines up with what enforcement will later
// see. IPv4 -> zero the last octet ("a.b.c.0/24"); IPv6 -> zero bytes 6-15, keeping the first 48 bits
// ("xxxx:xxxx:xxxx::/48"). A single client's traffic still moves around within a /24 or /48, but the
// subnet itself is what a slow, distributed, non-bursting crawl can't hide from.
static string subnet_key_for(IPAddress ip)
{
    if (ip.IsIPv4MappedToIPv6)
        ip = ip.MapToIPv4();

    byte[] addressBytes = ip.GetAddressBytes();

    if (ip.AddressFamily == AddressFamily.InterNetwork)
    {
        addressBytes[3] = 0;
        return new IPAddress(addressBytes) + "/24";
    }

    for (int i = 6; i < addressBytes.Length; i++)
        addressBytes[i] = 0;
    return new IPAddress(addressBytes) + "/48";
}

// The one thing Phase 0 actually needs: a line per genuine GCS pull (never per request -- cache hits are
// free and aren't logged). Always goes through ILogger; also appended to Jp2PullLogPath as a flat,
// easily-grepped/scripted file if configured, so the baseline analysis doesn't have to be sifted out of
// general application logging. The lock is fine here since this only runs on the slow path (an actual
// multi-hundred-KB-plus GCS download), never on a cache hit.
void log_jp2_pull(string bucket, string tag, string objectKey, string clientIp, string subnetKey)
{
    if (!options.EnableJp2PullLogging)
        return;

    app.Logger.LogInformation(
        "SobekCM.ImageServer: JP2 pull bucket={Bucket} tag={Tag} objectKey={ObjectKey} clientIp={ClientIp} subnet={Subnet}",
        bucket, tag, objectKey, clientIp, subnetKey);

    if (string.IsNullOrWhiteSpace(options.Jp2PullLogPath))
        return;

    string line = string.Join(",",
        DateTime.UtcNow.ToString("O"), bucket, tag, objectKey, clientIp, subnetKey);

    try
    {
        lock (jp2PullLogLock)
        {
            File.AppendAllText(options.Jp2PullLogPath, line + Environment.NewLine);
        }
    }
    catch (IOException ee)
    {
        // Measurement logging must never take the actual staging path down with it
        app.Logger.LogWarning(ee, "SobekCM.ImageServer: failed to append to Jp2PullLogPath {Path}", options.Jp2PullLogPath);
    }
}

// Removes any scratch file that isn't backed by a live cache entry and is older than a short grace period
// (long enough for a normal in-flight download to finish, short enough to still catch orphans quickly).
// Covers: a failed/partial download (see download_and_cache's catch below -- that already deletes its own
// file on failure, this is the backstop for the cases it can't reach, like the process being killed mid-
// download), and a post-eviction delete that failed because the file was briefly locked. Safe to run while
// the cache is live, unlike a blind "delete anything older than MaxCacheMinutes" sweep would be, because it
// never touches a path still tracked in liveScratchPaths.
void sweep_scratch_folder()
{
    DateTime orphanCutoffUtc = DateTime.UtcNow - TimeSpan.FromMinutes(5);
    int swept = 0;

    foreach (string existingFile in Directory.GetFiles(options.ScratchFolder))
    {
        if (liveScratchPaths.ContainsKey(existingFile))
            continue;

        if (File.GetLastWriteTimeUtc(existingFile) >= orphanCutoffUtc)
            continue;

        try
        {
            long orphanSizeBytes = new FileInfo(existingFile).Length;
            File.Delete(existingFile);
            swept++;
            app.Logger.LogWarning("SobekCM.ImageServer: swept orphaned scratch file {File} ({Size} bytes)", existingFile, orphanSizeBytes);
        }
        catch (IOException) { /* still locked -- next sweep will catch it */ }
    }

    if (swept > 0)
        app.Logger.LogInformation("SobekCM.ImageServer: scratch folder sweep removed {Count} orphaned file(s)", swept);
}

// Startup safety net: anything already on disk when this instance starts is something a prior crash/recycle
// never got to evict-and-delete -- the running cache is empty at this point, so every file here is an orphan
// by definition, no age check needed.
foreach (string existingFile in Directory.GetFiles(options.ScratchFolder))
{
    try
    {
        File.Delete(existingFile);
        app.Logger.LogInformation("SobekCM.ImageServer: startup sweep removed leftover scratch file {File}", existingFile);
    }
    catch (IOException) { /* leave it, not worth failing startup over -- the periodic sweep will retry */ }
}

// Ongoing defense-in-depth sweep, independent of any OS-level scheduled task -- keeps this self-contained
// and portable (no dependency on a Windows Task Scheduler job that has to be recreated on every new host).
_ = Task.Run(async () =>
{
    using var timer = new PeriodicTimer(TimeSpan.FromMinutes(10));
    while (await timer.WaitForNextTickAsync())
    {
        try { sweep_scratch_folder(); }
        catch (Exception ee) { app.Logger.LogError(ee, "SobekCM.ImageServer: scratch folder sweep failed"); }
    }
});

app.MapHealthChecks("/health");

// Nothing this host serves is useful to a crawler -- /render scripts, staged scratch files, and the iipsrv
// tiles under /iipimage/ -- so disallow everything. Served from code rather than a file on disk: this app
// has no static file serving, and a file would be one more per-host thing to remember on each deployment.
app.MapGet("/robots.txt", () => Results.Text("User-agent: *\nDisallow: /\n", "text/plain"));

// Requested directly by the browser via <script src="https://.../render?token=...">, not by the main
// SobekCM app -- that's the whole point of this shape: SobekCM's own page render never blocks on this.
// Responds with a single "viewer.open(...)" JavaScript statement once the file is staged (or already
// cached), so the caller only has to define `viewer` first and drop in this <script> tag right after.
app.MapGet("/render", async (HttpRequest request) =>
{
    string token = request.Query["token"];
    if (string.IsNullOrEmpty(token))
        return Results.BadRequest();

    StageRequest stageRequest;
    try
    {
        byte[] tokenBytes = Convert.FromBase64String(token);
        string json = StageTokenCipher.Decrypt(tokenBytes, sharedKey);
        stageRequest = JsonSerializer.Deserialize<StageRequest>(json)
            ?? throw new InvalidOperationException("Empty stage request.");
    }
    catch (Exception)
    {
        // Wrong key, corrupted token, or tampered payload -- all look the same to the caller. A bare
        // 401 is fine here: a <script src> load failing just means OpenSeadragon never gets its
        // viewer.open() call, no confusing page-level error.
        return Results.Unauthorized();
    }

    if (Math.Abs((DateTime.UtcNow - stageRequest.IssuedUtc).TotalSeconds) > options.MaxTokenAgeSeconds)
        return Results.Unauthorized();

    string objectKey = stageRequest.Tag + (stageRequest.FileName ?? string.Empty);
    string cacheKey = "JP2|" + stageRequest.Bucket + "|" + objectKey;
    int cacheMinutes = Math.Clamp(stageRequest.CacheMinutes ?? options.DefaultCacheMinutes, 1, options.MaxCacheMinutes);

    IPAddress remoteIp = request.HttpContext.Connection.RemoteIpAddress;
    string clientIp = remoteIp?.ToString() ?? "unknown";
    string subnetKey = remoteIp != null ? subnet_key_for(remoteIp) : "unknown";

    string dziSourcePath;
    try
    {
        dziSourcePath = await stage_and_get_dzi_source_path(cacheKey, stageRequest.Bucket, stageRequest.Tag, objectKey, cacheMinutes, clientIp, subnetKey);
    }
    catch (Google.GoogleApiException)
    {
        // Valid JS that fails loudly in the browser console, since a <script src> tag has no clean way
        // to surface an HTTP error status to the page itself
        return Results.Text("console.error('JPEG2000 image server: " + objectKey.Replace("'", "") + " not found in GCS');", "text/javascript");
    }

    string thisHostBaseUrl = request.Scheme + "://" + request.Host + "/";
    string openUrl = thisHostBaseUrl + "iipimage/iipsrv.fcgi?DeepZoom=" + dziSourcePath + ".dzi";

    // JsonSerializer.Serialize on a string produces a properly quoted/escaped JS string literal --
    // safer than hand-building the quotes around a path that includes a caller-influenced file name
    string javascript = "viewer.open(" + JsonSerializer.Serialize(openUrl) + ");";

    return Results.Text(javascript, "text/javascript");
});

// Returns the cached dzi source path for (bucket, objectKey), downloading it first if needed. Concurrent
// callers for the same cacheKey share one in-flight download rather than each racing an independent one --
// see the inFlightStagingRequests comment above for how. Throws Google.GoogleApiException if the object
// genuinely isn't in GCS; every concurrent caller waiting on the same download sees that same exception.
async Task<string> stage_and_get_dzi_source_path(string cacheKey, string bucket, string tag, string objectKey, int cacheMinutes, string clientIp, string subnetKey)
{
    // Fast path: already cached from an earlier request. Reading it here resets the sliding expiration
    // clock, and the file is still on disk since eviction hasn't run yet. Not logged as a pull -- no GCS
    // fetch happens on this path, so it costs nothing and Phase 0 doesn't care about it.
    if (cache.TryGetValue(cacheKey, out string cachedScratchPath) && cachedScratchPath != null)
        return cachedScratchPath;

    Lazy<Task<string>> lazyDownload = inFlightStagingRequests.GetOrAdd(cacheKey, _ => new Lazy<Task<string>>(
        () => download_and_cache(cacheKey, bucket, tag, objectKey, cacheMinutes, clientIp, subnetKey),
        LazyThreadSafetyMode.ExecutionAndPublication));

    try
    {
        return await lazyDownload.Value;
    }
    finally
    {
        // Compare-and-remove: only clear the entry if it's still the one we just awaited, so we don't
        // accidentally remove a newer in-flight download some other request already started for a retry
        inFlightStagingRequests.TryRemove(new KeyValuePair<string, Lazy<Task<string>>>(cacheKey, lazyDownload));
    }
}

// The actual download -- runs at most once per cacheKey at a time, however many concurrent /render
// requests are waiting on it (see stage_and_get_dzi_source_path).
async Task<string> download_and_cache(string cacheKey, string bucket, string tag, string objectKey, int cacheMinutes, string clientIp, string subnetKey)
{
    // Belt and suspenders: another request may have already finished and populated the cache in the
    // narrow gap between this factory being scheduled and actually starting to run
    if (cache.TryGetValue(cacheKey, out string alreadyCachedPath) && alreadyCachedPath != null)
        return alreadyCachedPath;

    string extension = Path.GetExtension(objectKey);
    string scratchFileName = Guid.NewGuid().ToString("N") + extension;
    string scratchFilePath = Path.Combine(options.ScratchFolder, scratchFileName);

    try
    {
        using (var fileStream = new FileStream(scratchFilePath, FileMode.Create, FileAccess.Write))
        {
            await storageClient.DownloadObjectAsync(bucket, objectKey, fileStream);
        }

        // The one caller whose GetOrAdd actually won the single-flight race is the one attributed here --
        // by design, this logs the GCS fetch itself (the thing with a cost), not every /render request that
        // happened to be waiting on it.
        log_jp2_pull(bucket, tag, objectKey, clientIp, subnetKey);
    }
    catch (Exception ee)
    {
        // FileMode.Create already created (or truncated) the file before the download itself ran, so any
        // failure here -- object not found, a mid-transfer timeout/reset that leaves a partial file behind,
        // whatever -- has to clean up after itself. Nothing else will: this cacheKey never made it into
        // cache, so the post-eviction delete below never fires for it.
        app.Logger.LogWarning(ee, "SobekCM.ImageServer: failed to stage {Bucket}/{ObjectKey} -- deleting scratch file", bucket, objectKey);
        try { File.Delete(scratchFilePath); } catch (IOException) { /* periodic sweep will retry */ }
        throw;
    }

    string dziSourcePath = options.ScratchFolder.Replace("\\", "/") + scratchFileName;

    liveScratchPaths[scratchFilePath] = 0;

    var cacheOptions = new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(cacheMinutes) };
    cacheOptions.RegisterPostEvictionCallback((_, _, _, _) =>
    {
        liveScratchPaths.TryRemove(scratchFilePath, out _);
        try { File.Delete(scratchFilePath); } catch (IOException) { /* still in use -- periodic sweep will retry */ }
    });
    cache.Set(cacheKey, dziSourcePath, cacheOptions);

    return dziSourcePath;
}

app.Run();
