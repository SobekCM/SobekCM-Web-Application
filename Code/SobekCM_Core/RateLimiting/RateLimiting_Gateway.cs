#region Using directives

using SobekCM.Core.MemoryMgmt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

#endregion

namespace SobekCM.Core.RateLimiting
{
    /// <summary> Per-IP request rate limiting: an IP making more than <see cref="RequestLimit"/> counted
    /// hits within <see cref="WindowSeconds"/> is banned for <see cref="BanMinutes"/>. When
    /// <see cref="RangeBanThreshold"/> different IPs from the same /16 (IPv4) or /32 (IPv6) range are banned at the
    /// same time, the whole range is banned for <see cref="RangeBanHours"/>. Counter and ban state live in
    /// <see cref="SharedCache"/> under their own key prefixes (a fourth caller pattern alongside the three
    /// documented on SharedCache itself), so they need no separate store and are wiped along with everything else
    /// by Clear_Cache()/RemoveAll(). </summary>
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
    /// <para><b>Range bans</b> answer a coordinated crawl spread across many addresses from one provider -- the
    /// incident that prompted them had 18 IPs from two hosting ranges each burst-banned within five seconds. The
    /// trigger is several <i>bans</i> overlapping, not traffic volume, so ordinary visitors sharing a large range
    /// almost never set it off together. Exempt IPs (see <see cref="IsExemptIp"/>) are never banned, even inside a
    /// banned range.</para>
    /// <para>Being SharedCache-backed, this is in-process and per-instance -- fine for a single-server
    /// deployment, but a load-balanced one would need a shared store for a ban to hold across instances.</para> </remarks>
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

        /// <summary> How many different IPs from the same /16 (IPv4) or /32 (IPv6) range have to be burst-banned at
        /// the same time -- their bans overlapping -- before the whole range is banned. 0 turns range bans off;
        /// 1 is treated as 2, since a single banned IP is just a burst ban. </summary>
        public static int RangeBanThreshold { get; set; } = 4;

        /// <summary> How many hours a range ban lasts, from the moment it starts. Anything below 1 is treated as 1. </summary>
        public static int RangeBanHours { get; set; } = 8;

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
        private const string RangeBanKeyPrefix = "RATERANGEBAN|";
        private const string RangeBannedIpsKeyPrefix = "RATERANGEIPS|";

        /// <summary> Boxed request count for one IP's current window; a reference type so concurrent
        /// requests sharing the same cache entry can increment it via Interlocked. </summary>
        private sealed class Counter
        {
            public int Count;
        }

        /// <summary> The IPs within one range that currently have a burst ban, each with when its ban ends. Guarded
        /// by locking the instance; bans are rare, so the lock is never contended in practice. </summary>
        private sealed class RangeBannedIps
        {
            public readonly Dictionary<string, DateTime> BanEnds = new Dictionary<string, DateTime>();
        }

        /// <summary> Pure check: is the given IP currently banned, on its own or as part of a banned range? Never
        /// increments anything -- see the class remarks for why checking and recording are separate calls. </summary>
        /// <param name="ipAddress"> Requesting IP </param>
        /// <param name="RangeBan"> TRUE when the ban being reported is a range ban, so the response can say the block
        /// is on the visitor's network rather than on anything they did </param>
        /// <returns> NULL if not banned; otherwise how many minutes remain on the ban (the longer one, if both apply) </returns>
        public static int? IsBanned(string ipAddress, out bool RangeBan)
        {
            RangeBan = false;
            if ((!Enabled) || (string.IsNullOrEmpty(ipAddress)) || (IsExemptIp(ipAddress)))
                return null;

            int? ipMinutesLeft = minutes_left(SharedCache.Instance[BanKeyPrefix + ipAddress]);

            if (RangeBanThreshold > 0)
            {
                string range = ClientSubnetKey.Range_For(ipAddress);
                int? rangeMinutesLeft = (range != null) ? minutes_left(SharedCache.Instance[RangeBanKeyPrefix + range]) : null;
                if ((rangeMinutesLeft.HasValue) && ((!ipMinutesLeft.HasValue) || (rangeMinutesLeft.Value > ipMinutesLeft.Value)))
                {
                    RangeBan = true;
                    return rangeMinutesLeft;
                }
            }

            return ipMinutesLeft;
        }

        /// <summary> Records one hit against the given IP's current window, banning the IP if this push
        /// puts it over the limit, and banning its whole range if that makes enough overlapping bans there. Never
        /// blocks or otherwise affects the request that triggered the ban -- see the class remarks for why. </summary>
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
            DateTime banEnds = DateTime.MinValue;
            SharedCache.Instance.GetOrAdd(BanKeyPrefix + ipAddress, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(BanMinutes);
                newBan = true;
                banEnds = DateTime.UtcNow.AddMinutes(BanMinutes);
                return banEnds;
            });

            if (!newBan)
                return;

            RateLimitLog_Gateway.Append(RateLimitLog_Gateway.Event_Burst_Ban, loggedOn, ipAddress,
                "more than " + limit + " item views in " + WindowSeconds + " s (" + RateLimitLog_Gateway.Who(loggedOn) +
                " limit) -- whole IP banned for " + BanMinutes + " min, every request from it gets HTTP 429");

            check_range_ban(ipAddress, loggedOn, banEnds);
        }

        /// <summary> Adds a newly banned IP to its range's list of currently banned IPs, and bans the whole range
        /// once enough of those bans overlap </summary>
        /// <param name="ipAddress"> IP that was just burst-banned </param>
        /// <param name="loggedOn"> Whether the request that caused that ban was logged on, only used in the log entry </param>
        /// <param name="banEnds"> When that IP's burst ban ends </param>
        /// <remarks> The range ban is claimed through SharedCache.GetOrAdd, so even if several bans in the same
        /// range reach the threshold at once, it starts, and is logged, exactly once. </remarks>
        private static void check_range_ban(string ipAddress, bool loggedOn, DateTime banEnds)
        {
            if (RangeBanThreshold <= 0)
                return;

            string range = ClientSubnetKey.Range_For(ipAddress);
            if (range == null)
                return;

            // Nothing to track once the range is already banned. RateLimitingMiddleware normally turns the range's
            // requests away before they can earn another burst ban, but this doesn't rely on that: without it, a
            // flood of bans during a range ban could keep the tracker alive and growing.
            if (SharedCache.Instance[RangeBanKeyPrefix + range] != null)
                return;

            // Tracks bans for as long as any of them could still be active: each new ban refreshes the sliding
            // expiration, and every ban lasts BanMinutes
            RangeBannedIps tracker = (RangeBannedIps)SharedCache.Instance.GetOrAdd(RangeBannedIpsKeyPrefix + range, entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(Math.Max(1, BanMinutes));
                return new RangeBannedIps();
            });

            List<string> bannedIps;
            lock (tracker)
            {
                DateTime now = DateTime.UtcNow;
                tracker.BanEnds[ipAddress] = banEnds;

                foreach (string expiredIp in tracker.BanEnds.Where(Pair => Pair.Value <= now).Select(Pair => Pair.Key).ToList())
                    tracker.BanEnds.Remove(expiredIp);

                if (tracker.BanEnds.Count < Math.Max(2, RangeBanThreshold))
                    return;

                bannedIps = tracker.BanEnds.Keys.OrderBy(Ip => Ip, StringComparer.Ordinal).ToList();
            }

            TimeSpan duration = TimeSpan.FromHours(Math.Max(1, RangeBanHours));
            bool newRangeBan = false;
            SharedCache.Instance.GetOrAdd(RangeBanKeyPrefix + range, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = duration;
                newRangeBan = true;
                return DateTime.UtcNow.Add(duration);
            });

            if (!newRangeBan)
                return;

            // Start the list fresh, so the same bans can't count again once this range ban expires
            SharedCache.Instance.Remove(RangeBannedIpsKeyPrefix + range);

            RateLimitLog_Gateway.Append(RateLimitLog_Gateway.Event_Range_Ban, loggedOn, range,
                bannedIps.Count + " IPs from this range burst-banned at the same time (" + String.Join(", ", bannedIps) +
                ") -- whole range banned for " + (int)duration.TotalHours + " hours, every request from it gets HTTP 429");
        }

        /// <summary> Minutes left on a ban entry read from the cache, or NULL if there's no active ban </summary>
        private static int? minutes_left(object BanEntry)
        {
            if (BanEntry is DateTime bannedUntil)
            {
                double minutesLeft = (bannedUntil - DateTime.UtcNow).TotalMinutes;
                if (minutesLeft > 0)
                    return (int)Math.Ceiling(minutesLeft);
            }

            return null;
        }
    }
}
