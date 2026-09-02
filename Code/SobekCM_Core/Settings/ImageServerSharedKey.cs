namespace SobekCM.Core.Settings
{
    /// <summary> Local path to the base64-encoded AES-256 key shared with the separate JPEG2000 image
    /// server (SobekCM_ImageServer's own ImageServerOptions.SharedKeyPath, in its appsettings.json --
    /// the two must match byte-for-byte). Set once at startup by SobekCM's RequestContextMiddleware from
    /// its own appsettings.json ("ImageServer:SharedKeyPath"), the same way GcsServiceAccountJsonPathOverride
    /// is read for GCS:ServiceAccountJsonPath. Deliberately kept out of <see cref="Server_Settings"/>: a
    /// secret's location is local machine config, not something that belongs in the shared, DB-backed,
    /// admin-editable instance settings table. </summary>
    public static class ImageServerSharedKey
    {
        /// <summary> Local path to the shared key file, or NULL/empty if not configured -- callers should
        /// fall back to their own conventional default in that case </summary>
        public static string Path { get; set; }
    }
}
