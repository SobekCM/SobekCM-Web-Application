using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Caching.Memory;
using SobekCM.ImageServer;
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

    // Cache hit -- reading it here resets the sliding expiration clock, and the file is still on disk
    // since eviction hasn't run yet
    if (cache.TryGetValue(cacheKey, out string cachedScratchPath) && cachedScratchPath != null)
    {
        dziSourcePath = cachedScratchPath;
    }
    else
    {
        string extension = Path.GetExtension(objectKey);
        string scratchFileName = Guid.NewGuid().ToString("N") + extension;
        string scratchFilePath = Path.Combine(options.ScratchFolder, scratchFileName);

        try
        {
            using (var fileStream = new FileStream(scratchFilePath, FileMode.Create, FileAccess.Write))
            {
                await storageClient.DownloadObjectAsync(stageRequest.Bucket, objectKey, fileStream);
            }
        }
        catch (Google.GoogleApiException)
        {
            // Valid JS that fails loudly in the browser console, since a <script src> tag has no clean
            // way to surface an HTTP error status to the page itself
            return Results.Text("console.error('JPEG2000 image server: " + objectKey.Replace("'", "") + " not found in GCS');", "text/javascript");
        }

        dziSourcePath = options.ScratchFolder.Replace("\\", "/") + scratchFileName;

        var cacheOptions = new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(cacheMinutes) };
        cacheOptions.RegisterPostEvictionCallback((_, _, _, _) =>
        {
            try { File.Delete(scratchFilePath); } catch (IOException) { /* still in use -- next startup sweep will catch it */ }
        });
        cache.Set(cacheKey, dziSourcePath, cacheOptions);
    }

    string thisHostBaseUrl = request.Scheme + "://" + request.Host + "/";
    string openUrl = thisHostBaseUrl + "iipimage/iipsrv.fcgi?DeepZoom=" + dziSourcePath + ".dzi";

    // JsonSerializer.Serialize on a string produces a properly quoted/escaped JS string literal --
    // safer than hand-building the quotes around a path that includes a caller-influenced file name
    string javascript = "viewer.open(" + JsonSerializer.Serialize(openUrl) + ");";

    return Results.Text(javascript, "text/javascript");
});

app.Run();
