namespace SobekCM.Core.MemoryMgmt
{
    /// <summary> File names for the shared diagnostic logs written to the application's temp folder </summary>
    /// <remarks> Kept as constants in one place, like SessionCache_Keys and RequestCache_Keys, so every writer
    /// agrees on the names. Each file has a single gateway that owns writing to it, which also serializes
    /// concurrent appends. </remarks>
    public static class LogFile_Names
    {
        /// <summary> Unhandled exceptions and other diagnostics, written through <see cref="ExceptionLog_Gateway"/> </summary>
        public const string Exceptions = "exceptions.txt";

        /// <summary> Every rate-limiting event -- bans, subnet budgets reached, site-wide fuses tripping --
        /// written through <see cref="SobekCM.Core.RateLimiting.RateLimitLog_Gateway"/> </summary>
        public const string RateLimiting = "ratelimiting.txt";

        /// <summary> Every self-registration attempt, written through <see cref="RegistrationLog_Gateway"/> </summary>
        public const string Registrations = "registrations.txt";
    }
}
