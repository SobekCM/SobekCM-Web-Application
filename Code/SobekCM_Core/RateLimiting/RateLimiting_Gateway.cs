#region Using directives

using Microsoft.Extensions.Caching.Memory;
using SobekCM.Core.MemoryMgmt;
using System;
using System.Threading;

#endregion

namespace SobekCM.Core.RateLimiting
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
    /// cloud storage, and an item page view is the closest single-request signal for
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

        /// <summary> Maximum requests allowed from a single IP within <see cref="WindowSeconds"/> before it is
        /// banned, for requests that aren't logged on </summary>
        public static int RequestLimit { get; set; } = 30;

        /// <summary> Maximum logged-on requests allowed from a single IP within <see cref="WindowSeconds"/>
        /// before it is banned. Counted separately from anonymous requests, not exempt: a logged-on session is
        /// just a cookie, and a cookie can be exported into a scraper. </summary>
        public static int LoggedOnRequestLimit { get; set; } = 60;

        /// <summary> Length, in seconds, of the counting window </summary>
        public static int WindowSeconds { get; set; } = 30;

        /// <summary> How long, in minutes, an IP that exceeds the limit is banned for </summary>
        public static int BanMinutes { get; set; } = 10;

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
        /// <param name="ipAddress"> Requesting IP </param>
        /// <param name="loggedOn"> Whether this request is logged on, which selects both the counter and the
        /// limit it's held to </param>
        /// <remarks> Logged-on and anonymous hits go into separate counters for the same IP, rather than one
        /// shared counter with two ceilings like the subnet budgets use. The consequence of tripping this one
        /// is a ban on the whole IP, so on a shared address (a library NAT, a campus proxy) a shared counter
        /// would let an anonymous crawler spend a logged-on patron's allowance and get them banned with it.
        /// The ban itself is still per-IP either way, since IsBanned runs before anyone is known to be logged on. </remarks>
        public static void RecordHit(string ipAddress, bool loggedOn)
        {
            if ((!Enabled) || (string.IsNullOrEmpty(ipAddress)) || (IsExemptIp(ipAddress)))
                return;

            string counterKey = CounterKeyPrefix + (loggedOn ? "LOGGEDON|" : String.Empty) + ipAddress;
            Counter counter = (Counter)SharedCache.Instance.GetOrAdd(counterKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(WindowSeconds);
                return new Counter();
            });

            int count = Interlocked.Increment(ref counter.Count);
            int limit = loggedOn ? LoggedOnRequestLimit : RequestLimit;
            if (count <= limit)
                return;

            // Over the limit -- ban the IP and let this window's counter simply expire on its own. The ban is
            // claimed through GetOrAdd, which creates the entry atomically, so only the request that actually
            // starts a ban logs it and sets its expiration. Requests already in flight when the ban lands find it
            // in place and neither extend it nor log again. Deriving "new ban" from the counter instead would miss
            // a re-ban: when BanMinutes is shorter than WindowSeconds the ban can expire while the counter lives
            // on, and the next request starts a fresh ban at a count well past limit + 1.
            bool newBan = false;
            SharedCache.Instance.GetOrAdd(BanKeyPrefix + ipAddress, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(BanMinutes);
                newBan = true;
                return DateTime.UtcNow.AddMinutes(BanMinutes);
            });

            if (newBan)
            {
                RateLimitLog_Gateway.Append(RateLimitLog_Gateway.Event_Burst_Ban, loggedOn, ipAddress,
                    "more than " + limit + " item views in " + WindowSeconds + " s (" + RateLimitLog_Gateway.Who(loggedOn) +
                    " limit) -- whole IP banned for " + BanMinutes + " min, every request from it gets HTTP 429");
            }
        }
    }
}
