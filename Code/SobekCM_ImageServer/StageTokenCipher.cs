using System;
using System.Security.Cryptography;
using System.Text;

namespace SobekCM.ImageServer
{
    /// <summary> AES-256-GCM encrypt/decrypt for the /stage request token. Deliberately self-contained
    /// (no dependency on anything else in this project) because the exact same code is duplicated on the
    /// calling side (SobekCM_Library) -- the two processes share no assembly, only this token format and
    /// the key file's bytes. </summary>
    /// <remarks> Token layout: [12-byte nonce][ciphertext, same length as plaintext][16-byte tag]. </remarks>
    public static class StageTokenCipher
    {
        private const int NonceSizeBytes = 12;
        private const int TagSizeBytes = 16;

        /// <summary> Encrypts a plaintext string (typically JSON) into a token ready to send as a request body </summary>
        public static byte[] Encrypt(string plaintext, byte[] key)
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

        /// <summary> Decrypts a token produced by <see cref="Encrypt"/> back into its plaintext string.
        /// Throws <see cref="CryptographicException"/> if the key is wrong or the token was tampered with. </summary>
        public static string Decrypt(byte[] token, byte[] key)
        {
            if (token.Length < NonceSizeBytes + TagSizeBytes)
                throw new ArgumentException("Token too short to be valid.", nameof(token));

            int ciphertextLength = token.Length - NonceSizeBytes - TagSizeBytes;
            byte[] nonce = new byte[NonceSizeBytes];
            byte[] ciphertext = new byte[ciphertextLength];
            byte[] tag = new byte[TagSizeBytes];

            Buffer.BlockCopy(token, 0, nonce, 0, NonceSizeBytes);
            Buffer.BlockCopy(token, NonceSizeBytes, ciphertext, 0, ciphertextLength);
            Buffer.BlockCopy(token, NonceSizeBytes + ciphertextLength, tag, 0, TagSizeBytes);

            byte[] plaintextBytes = new byte[ciphertextLength];
            using (var aesGcm = new AesGcm(key, TagSizeBytes))
            {
                aesGcm.Decrypt(nonce, ciphertext, tag, plaintextBytes);
            }

            return Encoding.UTF8.GetString(plaintextBytes);
        }

        /// <summary> Generates a fresh base64-encoded AES-256 key, suitable for writing to the shared key
        /// file both processes read. Run this once (e.g. via `dotnet run -- generate-key`) and copy the
        /// output into both sides' SharedKeyPath. </summary>
        public static string GenerateBase64Key()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        }
    }
}
