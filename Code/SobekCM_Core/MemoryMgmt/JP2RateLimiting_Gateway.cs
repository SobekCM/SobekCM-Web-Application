#region Using directives

using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading;

#endregion

namespace SobekCM.Core.MemoryMgmt
{
    /// <summary> Phase 1 of the GCS rate-limiting plan: a budget on the JPEG2000 zoomable viewer, keyed on
    /// the requester's /24 (or /48) subnet (see <see cref="ClientSubnetKey"/>) rather than their exact IP --
    /// a slow, distributed, non-bursting crawl spreads itself across many IPs in the same subnet
    /// specifically to dodge an exact-IP counter like <see cref="RateLimiting_Gateway"/>'s. </summary>
    /// <remarks> Logged-on requests are budgeted too, against a higher ceiling rather than being exempt: a
    /// logged-on session is carried by a cookie, and a cookie can be exported into a scraper. There is one
    /// counter per subnet for everyone; only the ceiling it's compared against differs, so an exported
    /// cookie buys a scraper more room but not an escape. Same design as SustainedRateLimiting_Gateway.
    /// <para>Counter and circuit-breaker state live in <see cref="SharedCache"/> under their own key
    /// prefixes, same as <see cref="RateLimiting_Gateway"/>. IsOverBudget is a pure read, called from both
    /// the menu-building Prototyper (to hide the zoomable link) and the viewer itself (to fall back to the
    /// plain JPEG viewer); RecordHit is the only write, called exactly once per actual viewer open -- from
    /// JPEG2000_ItemViewer's constructor, on the one path where the viewer isn't already redirecting away.
    /// Both go through Budget_Exceeded rather than being called directly. Config is set once from Program.cs (same
    /// pattern as RateLimiting_Gateway/ExceptionLog_Gateway), including ManualDisable -- flipping that one
    /// currently still needs an app restart, same as every other value here; a true no-restart admin toggle
    /// would need routing through the existing DB/Additional-Settings mechanism instead, not attempted here.</para> </remarks>
    public static class JP2RateLimiting_Gateway
    {
        /// <summary> Whether the JP2 budget is active at all; false skips every check (both per-subnet and
        /// site-wide) and never counts anything </summary>
        public static bool Enabled { get; set; }

        /// <summary> Maximum JP2 viewer opens allowed from a single subnet within an hour, for a request that
        /// isn't logged on </summary>
        public static int HourlyLimit { get; set; } = 20;

        /// <summary> Maximum JP2 viewer opens allowed from a single subnet within a day, for a request that
        /// isn't logged on </summary>
        public static int DailyLimit { get; set; } = 100;

        /// <summary> Maximum JP2 viewer opens allowed from a single subnet within an hour, for a logged-on
        /// request -- the more permissive ceiling, not an exemption </summary>
        public static int LoggedOnHourlyLimit { get; set; } = 60;

        /// <summary> Maximum JP2 viewer opens allowed from a single subnet within a day, for a logged-on request </summary>
        public static int LoggedOnDailyLimit { get; set; } = 300;

        /// <summary> Site-wide JP2 viewer opens per hour, across every subnet, that trips the circuit
        /// breaker. PLACEHOLDER default -- meant to be replaced once the Phase 0 baseline log has a week
        /// of real traffic to set this from; there is no principled number yet. </summary>
        public static int SiteWideHourlyThreshold { get; set; } = 2000;

        /// <summary> Manual site-wide kill switch for the zoomable viewer -- set from appsettings.json's
        /// "JP2RateLimiting:ManualDisable" (see Program.cs). Applies to every request, including logged-on
        /// users: this is a "the zoom feature itself needs to come down" lever, not part of the per-subnet
        /// budget. </summary>
        /// <remarks> This is one of the two independent things <see cref="IsCircuitOpen"/> checks, and it is
        /// the one that does NOT time out. Program.cs copies this value into the static once at startup, so
        /// it takes an app restart to turn ON and another to turn back OFF -- editing appsettings.json alone
        /// changes nothing in a running app. (The other path, the automatic fuse in trip_circuit_breaker,
        /// behaves the opposite way: nobody sets it by hand and it clears itself after an hour.) </remarks>
        public static bool ManualDisable { get; set; }

        private const string HourCounterKeyPrefix = "JP2RL_HOUR|";
        private const string DayCounterKeyPrefix = "JP2RL_DAY|";
        private const string SiteWideHourCounterKey = "JP2RL_SITEHOUR";
        private const string CircuitOpenKey = "JP2RL_CIRCUITOPEN";

        /// <summary> Boxed count for one window; a reference type so concurrent requests sharing the same
        /// cache entry can increment it via Interlocked, same shape as RateLimiting_Gateway's own Counter. </summary>
        private sealed class Counter
        {
            public int Count;
        }

        /// <summary> Whether the zoomable viewer is currently shut off site-wide, for every request,
        /// logged-on or not. </summary>
        /// <remarks> Two independent paths get here, and they clear in completely different ways -- which is
        /// the first thing to establish when someone asks "is it coming back on by itself?":
        /// <see cref="ManualDisable"/> is set by hand in appsettings.json and needs an app restart to change
        /// in either direction, while the automatic fuse (see trip_circuit_breaker) is set by this class
        /// when site-wide traffic crosses <see cref="SiteWideHourlyThreshold"/> and expires on its own one
        /// hour later with no admin action at all. Neither one clears the other. </remarks>
        public static bool IsCircuitOpen()
        {
            return ManualDisable || (SharedCache.Instance[CircuitOpenKey] != null);
        }

        /// <summary> Pure check: is this subnet currently over its JP2 budget, or is the site-wide circuit
        /// breaker open? Never increments anything. </summary>
        /// <param name="SubnetKey"> Subnet key from <see cref="ClientSubnetKey.From"/>; NULL/empty always
        /// returns FALSE (nothing to key a per-subnet check on) unless the circuit itself is open </param>
        /// <param name="LoggedOn"> Whether this particular request is logged on, which selects the ceiling
        /// the shared counter is compared against </param>
        public static bool IsOverBudget(string SubnetKey, bool LoggedOn)
        {
            if (!Enabled)
                return false;

            if (IsCircuitOpen())
                return true;

            if (string.IsNullOrEmpty(SubnetKey))
                return false;

            int hourlyCeiling = LoggedOn ? LoggedOnHourlyLimit : HourlyLimit;
            int dailyCeiling = LoggedOn ? LoggedOnDailyLimit : DailyLimit;

            if ((SharedCache.Instance[HourCounterKeyPrefix + SubnetKey] is Counter hourCounter) && (hourCounter.Count >= hourlyCeiling))
                return true;

            if ((SharedCache.Instance[DayCounterKeyPrefix + SubnetKey] is Counter dayCounter) && (dayCounter.Count >= dailyCeiling))
                return true;

            return false;
        }

        /// <summary> Records one legitimate JP2 viewer open against both the subnet's hourly/daily counters
        /// and the site-wide hourly counter, auto-tripping the circuit breaker if the latter crosses
        /// <see cref="SiteWideHourlyThreshold"/>. Called only for opens that weren't already turned away by
        /// <see cref="IsOverBudget"/> -- there's no reason to spend budget on a request that never got the
        /// viewer anyway. </summary>
        public static void RecordHit(string SubnetKey)
        {
            if ((!Enabled) || (string.IsNullOrEmpty(SubnetKey)))
                return;

            increment(HourCounterKeyPrefix + SubnetKey, TimeSpan.FromHours(1));
            increment(DayCounterKeyPrefix + SubnetKey, TimeSpan.FromDays(1));

            int siteWideCount = increment(SiteWideHourCounterKey, TimeSpan.FromHours(1));
            if ((siteWideCount >= SiteWideHourlyThreshold) && (!IsCircuitOpen()))
                trip_circuit_breaker(siteWideCount);
        }

        private static int increment(string key, TimeSpan window)
        {
            Counter counter = (Counter)SharedCache.Instance.GetOrAdd(key, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = window;
                return new Counter();
            });

            return Interlocked.Increment(ref counter.Count);
        }

        /// <summary> The automatic fuse: shuts the zoomable viewer off site-wide for exactly one hour, then
        /// lets it come back on its own -- no restart, no admin action, and nothing to remember to undo.
        /// (<see cref="ManualDisable"/> is the other, hand-operated path into <see cref="IsCircuitOpen"/>,
        /// and that one does NOT time out.) Also appends a loud, greppable line to temp/exceptions.txt via
        /// ExceptionLog_Gateway -- there is no email/webhook alerting in this codebase to hook into yet, so
        /// this is the same "alert" mechanism every other diagnostic condition here already uses. </summary>
        private static void trip_circuit_breaker(int siteWideCount)
        {
            SharedCache.Instance.Set(CircuitOpenKey, DateTime.UtcNow, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });

            ExceptionLog_Gateway.Append(
                "*** JP2 RATE LIMIT CIRCUIT BREAKER TRIPPED *** " + DateTime.UtcNow.ToString("O") +
                " -- site-wide JP2 opens this hour (" + siteWideCount + ") crossed SiteWideHourlyThreshold (" +
                SiteWideHourlyThreshold + "). Zoomable viewer is off site-wide for 1 hour, then comes back " +
                "automatically -- no restart needed. To keep it off past that, set JP2RateLimiting:ManualDisable." +
                Environment.NewLine);
        }
    }
}
