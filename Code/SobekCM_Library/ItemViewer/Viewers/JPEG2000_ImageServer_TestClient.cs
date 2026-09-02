using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SobekCM.Library.ItemViewer.Viewers
{
    // ***** TEMPORARY TEST CODE *****
    /// <summary> Builds the "https://image-server/render?token=..." URL for a &lt;script src&gt; tag that
    /// defers JPEG2000 staging entirely to the separate SobekCM_ImageServer site (see that project's
    /// Program.cs, GET /render, for the other half of this contract). No network call happens on this side --
    /// the browser is the one that actually requests this URL once it parses the page, and the image server
    /// responds with a single "viewer.open(...)" JavaScript statement once it's staged the file. That's the
    /// whole point of this shape: SobekCM's own page render never blocks on GCS/staging latency at all. </summary>
    internal static class JPEG2000_ImageServer_TestClient
    {
        private const int NonceSizeBytes = 12;
        private const int TagSizeBytes = 16;

        /// <param name="ImageServerBaseUrl"> Server_Settings.JP2ServerUrl -- base URL of the separate image server site </param>
        /// <param name="SharedKeyPath"> Local path to the base64 AES-256 key shared with that image server </param>
        /// <param name="Bucket"> GCS bucket the file lives in </param>
        /// <param name="Tag"> GCS object key prefix for the digital resource (SobekFileSystem.AssociFilePath) </param>
        /// <param name="FileName"> File name within that prefix </param>
        /// <param name="CacheMinutes"> Optional override of the image server's default cache lifetime </param>
        internal static string BuildRenderScriptUrl(string ImageServerBaseUrl, string SharedKeyPath,
            string Bucket, string Tag, string FileName, int? CacheMinutes)
        {
            byte[] sharedKey = Convert.FromBase64String(File.ReadAllText(SharedKeyPath).Trim());

            string payloadJson = JsonSerializer.Serialize(new
            {
                Bucket,
                Tag,
                FileName,
                CacheMinutes,
                IssuedUtc = DateTime.UtcNow
            });

            byte[] token = encrypt(payloadJson, sharedKey);

            // Query-string safe: base64 alone can contain '+', '/', '=' which are meaningful in a URL
            string encodedToken = Uri.EscapeDataString(Convert.ToBase64String(token));

            return ImageServerBaseUrl.TrimEnd('/') + "/render?token=" + encodedToken;
        }

        /// <summary> AES-256-GCM encrypts a JSON stage request payload -- must match the token layout
        /// SobekCM_ImageServer.StageTokenCipher.Decrypt expects: [12-byte nonce][ciphertext][16-byte tag] </summary>
        private static byte[] encrypt(string plaintext, byte[] key)
        {
            byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            byte[] nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
            byte[] ciphertext = new byte[plaintextBytes.Length];
            byte[] tag = new byte[TagSizeBytes];

            using (var aesGcm = new AesGcm(key, TagSizeBytes))
            {
                aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);
            }

            byte[] token = new byte[NonceSizeBytes + ciphertext.Length + TagSizeBytes];
            Buffer.BlockCopy(nonce, 0, token, 0, NonceSizeBytes);
            Buffer.BlockCopy(ciphertext, 0, token, NonceSizeBytes, ciphertext.Length);
            Buffer.BlockCopy(tag, 0, token, NonceSizeBytes + ciphertext.Length, TagSizeBytes);
            return token;
        }
    }
    // ***** END TEMPORARY TEST CODE *****
}
