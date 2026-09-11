#region Using directives

using Microsoft.Extensions.Caching.Memory;
using System;
using System.IO;
using System.Threading;

#endregion

namespace SobekCM.Core.MemoryMgmt
{
    /// <summary> Per-IP request rate limiting: an IP making more than <see cref="RequestLimit"/> counted
    /// hits within <see cref="WindowSeconds"/> is banned for <see cref="BanMinutes"/>. Counter and ban
    /// state live in <see cref="SharedCache"/> under their own key prefixes (a fourth caller pattern
    /// alongside the three documented on SharedCache itself), so they need no separate store and are
    /// wiped along with everything else by Clear_Cache()/RemoveAll(). </summary>
    /// <remarks> Checking and recording are deliberately separate calls, used from two different points
    /// in the pipeline: <see cref="IsBanned"/> is a pure read, called by RateLimitingMiddleware very
    /// early (before routing/QueryInitializer even know what the request is for) so an already-banned IP
    /// is rejected as cheaply as possible. <see cref="RecordHit"/> does the actual counting, called only
    /// by SobekCM.QueryInitializerHelpers.ItemViewRateLimitInitializer, once a fully-built Navigation_Object
    /// and User_Object are available -- the real concern this feature exists for is load against GCS
    /// cloud storage, and a non-logged-on user's item page view is the closest single-request signal for
    /// that available in this app's own pipeline (JP2 deep-zoom tiles are served by IIS's FastCgiModule
    /// directly, and thumbnails are built as direct /content/ URLs -- neither ever reaches this app's
    /// request pipeline at all). Because of this split, a hit that pushes an IP over the limit never
    /// blocks the request that triggered it -- the ban only takes effect starting with that IP's next
    /// request, once IsBanned sees it. Config is set once from Program.cs (same pattern as
    /// <c>ExceptionLog_Gateway</c>), since SobekCM_Core has no direct access to IConfiguration.
    /// Being SharedCache-backed, this is in-process and per-instance -- fine for a single-server
    /// deployment, but a load-balanced one would need a shared store for a ban to hold across instances. </remarks>
    public static class RateLimiting_Gateway
    {
        /// <summary> Whether rate limiting is active at all; false skips the check entirely </summary>
        public static bool Enabled { get; set; }

        /// <summary> Maximum requests allowed from a single IP within <see cref="WindowSeconds"/> before it is banned </summary>
        public static int RequestLimit { get; set; } = 300;

        /// <summary> Length, in seconds, of the counting window </summary>
        public static int WindowSeconds { get; set; } = 60;

        /// <summary> How long, in minutes, an IP that exceeds the limit is banned for </summary>
        public static int BanMinutes { get; set; } = 10;

        /// <summary> Whether each new ban is appended to temp/banned.log (date/time + IP only) -- set once
        /// at startup from appsettings.json's "RateLimiting:LoggingEnabled" (see RateLimitingMiddleware).
        /// Meant as a lightweight "is this actually triggering" check, not an audit trail, so only the
        /// moment an IP is banned is logged -- not every request rejected while the ban is still active. </summary>
        public static bool LoggingEnabled { get; set; } = true;

        /// <summary> Optional predicate for IPs that should never be rate limited at all -- neither
        /// counted nor banned. Wired up once at startup by RateLimitingMiddleware to check the live Engine
        /// IP restriction ranges (dev boxes, the web server itself, the Builder machine, etc.), so known
        /// infrastructure can never trip this feature regardless of how much traffic it generates -- kept
        /// as a delegate, re-evaluated on every call, rather than a snapshot copied in once, so it stays
        /// correct if those ranges are edited and the config reloaded, without RateLimiting_Gateway needing
        /// to know anything about how that config is structured or where it lives. Defaults to "nothing is
        /// exempt". </summary>
        public static Func<string, bool> IsExemptIp { get; set; } = _ => false;

        private const string CounterKeyPrefix = "RATELIMIT|";
        private const string BanKeyPrefix = "RATEBAN|";

        private static readonly object logWriteLock = new object();

        /// <summary> Boxed request count for one IP's current window; a reference type so concurrent
        /// requests sharing the same cache entry can increment it via Interlocked. </summary>
        private sealed class Counter
        {
            public int Count;
        }

        /// <summary> Pure check: is the given IP currently banned? Never increments anything -- see the
        /// class remarks for why checking and recording are separate calls. </summary>
        /// <returns> NULL if not banned; otherwise how many minutes remain on the ban </returns>
        public static int? IsBanned(string ipAddress)
        {
            if ((!Enabled) || (string.IsNullOrEmpty(ipAddress)) || (IsExemptIp(ipAddress)))
                return null;

            if (SharedCache.Instance[BanKeyPrefix + ipAddress] is DateTime bannedUntil)
            {
                double minutesLeft = (bannedUntil - DateTime.UtcNow).TotalMinutes;
                if (minutesLeft > 0)
                    return (int)Math.Ceiling(minutesLeft);
            }

            return null;
        }

        /// <summary> Records one hit against the given IP's current window, banning the IP if this push
        /// puts it over the limit. Never blocks or otherwise affects the request that triggered the ban --
        /// see the class remarks for why. </summary>
        public static void RecordHit(string ipAddress)
        {
            if ((!Enabled) || (string.IsNullOrEmpty(ipAddress)) || (IsExemptIp(ipAddress)))
                return;

            string counterKey = CounterKeyPrefix + ipAddress;
            Counter counter = (Counter)SharedCache.Instance.GetOrAdd(counterKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(WindowSeconds);
                return new Counter();
            });

            int count = Interlocked.Increment(ref counter.Count);
            if (count <= RequestLimit)
                return;

            // Over the limit -- ban the IP and let this window's counter simply expire on its own
            DateTime banUntil = DateTime.UtcNow.AddMinutes(BanMinutes);
            SharedCache.Instance.Set(BanKeyPrefix + ipAddress, banUntil, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(BanMinutes)
            });

            if (LoggingEnabled)
                LogBan(ipAddress);
        }

        /// <summary> Appends a single "date/time, IP" line to temp/banned.log under the current content
        /// root, serialized against other concurrent callers the same way ExceptionLog_Gateway.Append
        /// serializes temp/exceptions.txt. Never throws. </summary>
        private static void LogBan(string ipAddress)
        {
            try
            {
                string logPath = Path.Combine(AppRoot_Gateway.AppRootPath, "temp", "banned.log");
                string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\t" + ipAddress + Environment.NewLine;
                lock (logWriteLock)
                {
                    File.AppendAllText(logPath, line);
                }
            }
            catch (Exception)
            {
                // Best-effort logging -- nothing else to do if this itself fails.
            }
        }
    }
}
