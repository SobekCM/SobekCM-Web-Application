namespace IiifImporter
{
    /// <summary> Downloads derivative and master image files referenced by a IIIF manifest. </summary>
    public class ImageDownloader
    {
        private readonly HttpClient _http;
        private readonly int? _maxSize;

        public ImageDownloader(HttpClient http, int? maxSize)
        {
            _http = http;
            _maxSize = maxSize;
        }

        /// <summary> Downloads a derivative of a IIIF Image API resource. When a max size was given
        /// at construction, requests it via the "!w,h" sizing syntax (the manifests themselves already
        /// use this against a level1 profile for thumbnails/previews); otherwise requests the image
        /// at full resolution ("full" size parameter). </summary>
        public Task DownloadAsync(string imageServiceBaseUrl, string destinationPath, CancellationToken cancellationToken = default)
        {
            string sizeSegment = _maxSize.HasValue ? $"!{_maxSize},{_maxSize}" : "full";
            string url = $"{imageServiceBaseUrl.TrimEnd('/')}/full/{sizeSegment}/0/default.jpg";
            return DownloadFileAsync(url, destinationPath, cancellationToken);
        }

        /// <summary> Downloads a file directly from a plain URL (e.g. David Rumsey's JP2K master
        /// download links) - no IIIF Image API sizing involved. </summary>
        public async Task DownloadFileAsync(string url, string destinationPath, CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using FileStream fileStream = File.Create(destinationPath);
            await response.Content.CopyToAsync(fileStream, cancellationToken);
        }
    }
}
