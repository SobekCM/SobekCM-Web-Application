using System;
using System.Security.Cryptography;
using System.Text;

namespace SobekCM.Tools
{
    /// <summary> Hashes and verifies user passwords, understanding both the current PBKDF2 format
    /// and the legacy SHA1-with-fixed-salt format it replaces, so existing accounts keep working
    /// without a forced password reset - see <see cref="VerifyPassword"/> remarks for the migration path </summary>
    public static class PasswordHasher
    {
        // OWASP-recommended floor for PBKDF2-HMAC-SHA256 as of this writing; raise over time as hardware improves
        private const int Pbkdf2Iterations = 210000;
        private const int SaltSizeBytes = 16;
        private const int HashSizeBytes = 32;
        private const string Pbkdf2Prefix = "PBKDF2";

        // Matches the fixed salt SecurityInfo.SHA1_EncryptString-based password hashes were created with;
        // kept only so VerifyPassword can still validate accounts that haven't logged in since the migration
        private const string LegacySalt = "This is my salt to add to the password";

        /// <summary> Hashes a plain-text password using PBKDF2-HMAC-SHA256 with a random per-call salt </summary>
        /// <param name="password"> Plain-text password </param>
        /// <returns> Self-describing string safe to store directly in the Password column:
        /// "PBKDF2$&lt;iterations&gt;$&lt;base64 salt&gt;$&lt;base64 hash&gt;" </returns>
        public static string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, Pbkdf2Iterations, HashAlgorithmName.SHA256, HashSizeBytes);

            return $"{Pbkdf2Prefix}${Pbkdf2Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        /// <summary> Verifies a plain-text password against a stored hash, transparently supporting
        /// both the current PBKDF2 format and the legacy SHA1-with-fixed-salt format </summary>
        /// <param name="password"> Plain-text password as submitted on the logon form </param>
        /// <param name="storedValue"> Value currently stored in the Password column for this user </param>
        /// <param name="needsUpgrade"> TRUE if the password matched but was stored in the legacy format -
        /// the caller should immediately re-hash with <see cref="HashPassword"/> and persist the result,
        /// so accounts migrate to the new format the next time they successfully log on rather than all
        /// at once, avoiding a forced reset for the whole user base </param>
        /// <returns> TRUE if the password is correct, otherwise FALSE </returns>
        public static bool VerifyPassword(string password, string storedValue, out bool needsUpgrade)
        {
            needsUpgrade = false;

            if (string.IsNullOrEmpty(storedValue))
                return false;

            if (storedValue.StartsWith(Pbkdf2Prefix + "$", StringComparison.Ordinal))
                return VerifyPbkdf2(password, storedValue);

            // Anything else is assumed to be a legacy bare-Base64 SHA1 digest
            needsUpgrade = VerifyLegacySha1(password, storedValue);
            return needsUpgrade;
        }

        private static bool VerifyPbkdf2(string password, string storedValue)
        {
            string[] parts = storedValue.Split('$');
            if (parts.Length != 4)
                return false;

            if (!int.TryParse(parts[1], out int iterations))
                return false;

            byte[] salt = Convert.FromBase64String(parts[2]);
            byte[] expectedHash = Convert.FromBase64String(parts[3]);
            byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, iterations, HashAlgorithmName.SHA256, expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }

        private static bool VerifyLegacySha1(string password, string storedValue)
        {
            string expected = SecurityInfo.SHA1_EncryptString(password + LegacySalt);

            byte[] expectedBytes = Encoding.UTF8.GetBytes(expected);
            byte[] actualBytes = Encoding.UTF8.GetBytes(storedValue);
            return CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
        }
    }
}
