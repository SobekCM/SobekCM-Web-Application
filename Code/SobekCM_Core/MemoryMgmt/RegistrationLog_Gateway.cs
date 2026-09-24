using System;
using System.IO;

namespace SobekCM.Core.MemoryMgmt
{
    /// <summary> Appends one line per self-registration attempt to temp/registrations.txt, so repeated
    /// registrations from the same IP (or the same email domain) are easy to spot </summary>
    /// <remarks> Tab-separated, with a header line when the file is created. Only the email DOMAIN is written,
    /// never the address. Serialized with its own lock, same as <see cref="ExceptionLog_Gateway"/>. Never throws. </remarks>
    public static class RegistrationLog_Gateway
    {
        private const string Header = "# Timestamp\tIPAddress\tEmailDomain\tSucceeded\tHoneypotTriggered\tEmailVerified\tUserAgent";

        private static readonly object writeLock = new object();

        /// <summary> Records one registration attempt </summary>
        /// <param name="IpAddress"> Client IP address </param>
        /// <param name="Email"> Email address entered (only its domain is logged), may be blank </param>
        /// <param name="Succeeded"> TRUE if the account was created </param>
        /// <param name="HoneypotTriggered"> TRUE if the hidden honeypot field was filled in </param>
        /// <param name="EmailVerified"> Whether the email address was verified, or NULL when that isn't
        /// known - there is no email verification step today, so callers pass NULL, logged as "n/a" </param>
        /// <param name="UserAgent"> Client user agent </param>
        public static void Append(string IpAddress, string Email, bool Succeeded, bool HoneypotTriggered, bool? EmailVerified, string UserAgent)
        {
            try
            {
                string line = String.Join("\t",
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    clean(IpAddress, "unknown"),
                    clean(Email_Domain(Email), String.Empty),
                    Succeeded ? "true" : "false",
                    HoneypotTriggered ? "true" : "false",
                    EmailVerified.HasValue ? (EmailVerified.Value ? "true" : "false") : "n/a",
                    clean(UserAgent, String.Empty)) + Environment.NewLine;

                string logPath = Path.Combine(AppRoot_Gateway.AppRootPath, "temp", LogFile_Names.Registrations);
                lock (writeLock)
                {
                    if (!File.Exists(logPath))
                        File.AppendAllText(logPath, Header + Environment.NewLine);

                    File.AppendAllText(logPath, line);
                }
            }
            catch (Exception)
            {
                // Best-effort logging -- nothing else to do if this itself fails
            }
        }

        /// <summary> The part of an email address after the last '@', lower-cased, or blank if there isn't one </summary>
        /// <param name="Email"> Email address as entered </param>
        public static string Email_Domain(string Email)
        {
            if (String.IsNullOrWhiteSpace(Email))
                return String.Empty;

            int at = Email.LastIndexOf('@');
            if ((at < 0) || (at == Email.Length - 1))
                return String.Empty;

            return Email.Substring(at + 1).Trim().ToLowerInvariant();
        }

        // Anything the client sent must not break the one-line, tab-separated format
        private static string clean(string Value, string IfBlank)
        {
            if (String.IsNullOrWhiteSpace(Value))
                return IfBlank;
            return Value.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ').Trim();
        }
    }
}
