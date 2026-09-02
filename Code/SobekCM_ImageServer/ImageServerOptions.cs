namespace SobekCM.ImageServer
{
    /// <summary> Bound from the "ImageServer" section of appsettings.json </summary>
    public class ImageServerOptions
    {
        /// <summary> Full path to the service account JSON key used to read from GCS -- this process needs
        /// only read access, never signing rights, since it never hands a GCS URL back to a browser </summary>
        public string GcsServiceAccountJsonPath { get; set; } = string.Empty;

        /// <summary> Full path to a file holding the base64-encoded AES-256 key shared with the main
        /// SobekCM web app, used to decrypt incoming /stage request tokens </summary>
        public string SharedKeyPath { get; set; } = string.Empty;

        /// <summary> Local/UNC folder every staged file is downloaded into, and that the separately-configured
        /// iipsrv IIS handler is expected to be pointed at </summary>
        public string ScratchFolder { get; set; } = string.Empty;

        /// <summary> How long a staged file stays cached (and stays on disk) when the request doesn't specify
        /// its own CacheMinutes </summary>
        public int DefaultCacheMinutes { get; set; } = 5;

        /// <summary> Upper bound on CacheMinutes a request is allowed to ask for, regardless of what it sends </summary>
        public int MaxCacheMinutes { get; set; } = 15;

        /// <summary> How old (in seconds) a request's IssuedUtc is allowed to be before it's rejected as a
        /// replay of a captured token </summary>
        public int MaxTokenAgeSeconds { get; set; } = 120;
    }
}
