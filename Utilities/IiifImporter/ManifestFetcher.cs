using System.Text.Json;

namespace IiifImporter
{
    /// <summary> Fetches an IIIF Presentation manifest from the David Rumsey LUNA IIIF endpoint </summary>
    public class ManifestFetcher
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _http;

        public ManifestFetcher(HttpClient http)
        {
            _http = http;
        }

        public async Task<(IiifManifest Manifest, string RawJson)> FetchAsync(string objectId, CancellationToken cancellationToken = default)
        {
            string url = $"https://www.davidrumsey.com/luna/servlet/iiif/m/{objectId}/manifest";
            string rawJson = await _http.GetStringAsync(url, cancellationToken);

            IiifManifest? manifest = JsonSerializer.Deserialize<IiifManifest>(rawJson, SerializerOptions);
            if (manifest == null)
                throw new InvalidOperationException($"Manifest for '{objectId}' deserialized to null.");

            return (manifest, rawJson);
        }
    }
}
