using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SobekCM.ImageServer;
using System.Collections.Concurrent;
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

if (!Directory.Exists(options.ScratchFolder))
    Directory.CreateDirectory(options.ScratchFolder);

// Startup safety net: anything already older than the max cache window is something a prior crash/recycle
// never got to evict-and-delete -- the running cache has no record of it, so sweep by last-write time instead
DateTime startupCutoffUtc = DateTime.UtcNow - TimeSpan.FromMinutes(options.MaxCacheMinutes);
foreach (string existingFile in Directory.GetFiles(options.ScratchFolder))
{
    if (File.GetLastWriteTimeUtc(existingFile) < startupCutoffUtc)
    {
        try { File.Delete(existingFile); } catch (IOException) { /* leave it, not worth failing startup over */ }
    }
}

var app = builder.Build();

app.MapHealthChecks("/health");

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

    string dziSourcePath;
    try
    {
        dziSourcePath = await stage_and_get_dzi_source_path(cacheKey, stageRequest.Bucket, objectKey, cacheMinutes);
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
async Task<string> stage_and_get_dzi_source_path(string cacheKey, string bucket, string objectKey, int cacheMinutes)
{
    // Fast path: already cached from an earlier request. Reading it here resets the sliding expiration
    // clock, and the file is still on disk since eviction hasn't run yet.
    if (cache.TryGetValue(cacheKey, out string cachedScratchPath) && cachedScratchPath != null)
        return cachedScratchPath;

    Lazy<Task<string>> lazyDownload = inFlightStagingRequests.GetOrAdd(cacheKey, _ => new Lazy<Task<string>>(
        () => download_and_cache(cacheKey, bucket, objectKey, cacheMinutes),
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
async Task<string> download_and_cache(string cacheKey, string bucket, string objectKey, int cacheMinutes)
{
    // Belt and suspenders: another request may have already finished and populated the cache in the
    // narrow gap between this factory being scheduled and actually starting to run
    if (cache.TryGetValue(cacheKey, out string alreadyCachedPath) && alreadyCachedPath != null)
        return alreadyCachedPath;

    string extension = Path.GetExtension(objectKey);
    string scratchFileName = Guid.NewGuid().ToString("N") + extension;
    string scratchFilePath = Path.Combine(options.ScratchFolder, scratchFileName);

    using (var fileStream = new FileStream(scratchFilePath, FileMode.Create, FileAccess.Write))
    {
        await storageClient.DownloadObjectAsync(bucket, objectKey, fileStream);
    }

    string dziSourcePath = options.ScratchFolder.Replace("\\", "/") + scratchFileName;

    var cacheOptions = new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(cacheMinutes) };
    cacheOptions.RegisterPostEvictionCallback((_, _, _, _) =>
    {
        try { File.Delete(scratchFilePath); } catch (IOException) { /* still in use -- next startup sweep will catch it */ }
    });
    cache.Set(cacheKey, dziSourcePath, cacheOptions);

    return dziSourcePath;
}

app.Run();
