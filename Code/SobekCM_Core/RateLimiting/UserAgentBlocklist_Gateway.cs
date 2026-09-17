using Microsoft.Extensions.Caching.Memory;
using SobekCM.Core.MemoryMgmt;
using System;
using System.Buffers;

namespace SobekCM.Core.RateLimiting
{
    /// <summary> Hardcoded list of user agents refused outright, before any other layer runs -- for automated
    /// clients that don't deserve the lighter treatment Navigation_Object's robot token list gives cooperative
    /// crawlers (the fast static page, no zoom), only a closed door. </summary>
    /// <remarks> Hardcoded rather than appsettings.json-driven, on purpose: this behavior should be identical
    /// across every one of this instance's site configs without editing each one -- the same reasoning
    /// Navigation_Object.Robot_UserAgent_Tokens already uses. Add a new entry to <see cref="Banned_UserAgent_Tokens"/>
    /// and redeploy to ban another one; there is no per-site override.
    /// <para>Checked by RateLimitingMiddleware first, before anything else including the IP ban check -- it's the
    /// cheapest possible test (no cache lookup, doesn't need the requester's IP at all) and it's an unconditional
    /// no, regardless of how well-behaved any individual request looks.</para>
    /// <para>This is a different list from Navigation_Object.Robot_UserAgent_Tokens on purpose: that one governs
    /// how a request is SERVED (robots.txt still decides whether a crawler should be there at all), this one
    /// decides whether it's served at all. A token belongs here only once it's shown it isn't just an
    /// uncooperative crawler but something actively hostile.</para> </remarks>
    public static class UserAgentBlocklist_Gateway
    {
        /// <summary> User agent substrings that get an outright HTTP 403, from any IP </summary>
        /// <remarks> "UT-Dorkbot" is the University of Texas at Austin's web vulnerability scanner. Traffic
        /// analysis on 2026-09-16 found it firing SQL-injection and command-injection payloads (time-based
        /// sleep()/waitfor delay/ping) through its own User-Agent header at a site with no relationship to UT
        /// Austin, never once checking robots.txt, from 18 IPs in one /24 continuously for days. </remarks>
        private static readonly string[] Banned_UserAgent_Tokens =
        {
            "UT-DORKBOT",
        };

        private static readonly SearchValues<string> Banned_UserAgent_Search_Values =
            SearchValues.Create(Banned_UserAgent_Tokens, StringComparison.OrdinalIgnoreCase);

        private const string LoggedRecentlyKeyPrefix = "UABAN_LOGGED|";
        private static readonly TimeSpan LogCooldown = TimeSpan.FromHours(1);

        /// <summary> Whether this user agent is on the outright ban list. Pure check -- logs nothing; see
        /// <see cref="RecordBan"/> for that. </summary>
        public static bool IsBanned(string UserAgent)
        {
            return (!String.IsNullOrEmpty(UserAgent)) && (UserAgent.AsSpan().IndexOfAny(Banned_UserAgent_Search_Values) >= 0);
        }

        /// <summary> Logs a blocked request to temp/ratelimiting.txt, at most once per IP per hour </summary>
        /// <remarks> The block itself (see <see cref="IsBanned"/>) applies to every single matching request --
        /// only the logging is throttled, the same "don't let a crawler that keeps hammering a closed door
        /// flood the file" reasoning every other event in this system already follows. Claimed through
        /// SharedCache.GetOrAdd, which creates the entry atomically, so concurrent requests from the same IP
        /// can't each write a line. </remarks>
        public static void RecordBan(string IpAddress, string UserAgent)
        {
            if (String.IsNullOrEmpty(IpAddress))
                return;

            bool claimed = false;
            SharedCache.Instance.GetOrAdd(LoggedRecentlyKeyPrefix + IpAddress, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = LogCooldown;
                claimed = true;
                return DateTime.UtcNow;
            });

            if (!claimed)
                return;

            RateLimitLog_Gateway.Append(RateLimitLog_Gateway.Event_UserAgent_Ban, false, IpAddress,
                "User agent matched the hardcoded blocklist -- every request from this IP with this user agent gets " +
                "HTTP 403 (this line is written at most once per hour per IP; the block itself is not throttled)");
        }
    }
}
