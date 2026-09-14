#region Using directives

using Microsoft.Extensions.Caching.Memory;
using SobekCM.Core.MemoryMgmt;
using System;
using System.Threading;

#endregion

namespace SobekCM.Core.RateLimiting
{
    /// <summary> Phase 1 of the GCS rate-limiting plan: a budget on the JPEG2000 zoomable viewer, keyed on
    /// the requester's /24 (or /48) subnet (see <see cref="ClientSubnetKey"/>) rather than their exact IP --
    /// a slow, distributed, non-bursting crawl spreads itself across many IPs in the same subnet
    /// specifically to dodge an exact-IP counter like <see cref="RateLimiting_Gateway"/>'s. </summary>
    /// <remarks> Logged-on requests are budgeted too, against a higher ceiling rather than being exempt: a
    /// logged-on session is carried by a cookie, and a cookie can be exported into a scraper. Anonymous and
    /// logged-on opens are counted separately per subnet, each against its own ceiling, so anonymous zooming
    /// can never use up logged-on visitors' allowance. Same design as SustainedRateLimiting_Gateway.
    /// <para>Counter and circuit-breaker state live in <see cref="SharedCache"/> under their own key
    /// prefixes, same as <see cref="RateLimiting_Gateway"/>. IsOverBudget is a pure read, called from both
    /// the menu-building Prototyper (to hide the zoomable link) and the viewer itself (to fall back to the
    /// plain JPEG viewer); RecordHit is the only write, called exactly once per viewer open that's actually
    /// shown -- from JPEG2000_ItemViewer.Write_Main_Viewer_Section, not its constructor, so an item page that
    /// Item_HtmlSubwriter goes on to refuse (item-view budget, login-only mode) never counts.
    /// Both go through Budget_Exceeded rather than being called directly. Config is set once from Program.cs (same
    /// pattern as RateLimiting_Gateway/ExceptionLog_Gateway), including ManualDisable -- flipping that one
    /// currently still needs an app restart, same as every other value here; a true no-restart admin toggle
    /// would need routing through the existing DB/Additional-Settings mechanism instead, not attempted here.</para> </remarks>
    public static class JP2RateLimiting_Gateway
    {
        /// <summary> Whether the automatic JP2 budget is active; false skips the per-subnet ceilings and the
        /// automatic site-wide fuse, and never counts anything </summary>
        /// <remarks> Does NOT turn off <see cref="ManualDisable"/>, which applies whether or not this is set --
        /// same as LoginOnlyMode_Gateway's manual modes. A hand-set kill switch that silently does nothing
        /// because another setting is off would be the wrong surprise in an emergency. </remarks>
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

        /// <summary> How many hours the automatic circuit breaker keeps the zoomable viewer off site-wide once
        /// <see cref="SiteWideHourlyThreshold"/> trips it, before it clears itself -- the JP2 counterpart of
        /// LoginOnlyMode_Gateway.FuseHours. Anything below 1 is treated as 1. </summary>
        public static int CircuitBreakerHours { get; set; } = 1;

        /// <summary> Manual site-wide kill switch for the zoomable viewer -- set from appsettings.json's
        /// "JP2RateLimiting:ManualDisable" (see Program.cs). Applies to every request, including logged-on
        /// users: this is a "the zoom feature itself needs to come down" lever, not part of the per-subnet
        /// budget. </summary>
        /// <remarks> This is one of the two independent things <see cref="IsCircuitOpen"/> checks, and it is
        /// the one that does NOT time out. Program.cs copies this value into the static once at startup, so
        /// it takes an app restart to turn ON and another to turn back OFF -- editing appsettings.json alone
        /// changes nothing in a running app. (The other path, the automatic fuse in trip_circuit_breaker,
        /// behaves the opposite way: nobody sets it by hand and it clears itself after <see cref="CircuitBreakerHours"/>.) </remarks>
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
        /// when site-wide traffic crosses <see cref="SiteWideHourlyThreshold"/> and expires on its own
        /// <see cref="CircuitBreakerHours"/> later with no admin action at all. Neither one clears the other. </remarks>
        public static bool IsCircuitOpen()
        {
            return ManualDisable || (SharedCache.Instance[CircuitOpenKey] != null);
        }

        /// <summary> Pure check: is this subnet currently over its JP2 budget, or is the site-wide circuit
        /// breaker open? Never increments anything. </summary>
        /// <param name="SubnetKey"> Subnet key from <see cref="ClientSubnetKey.From"/>; NULL/empty always
        /// returns FALSE (nothing to key a per-subnet check on) unless the circuit itself is open </param>
        /// <param name="LoggedOn"> Whether this particular request is logged on, which selects both the counters
        /// and the ceilings it's held to </param>
        public static bool IsOverBudget(string SubnetKey, bool LoggedOn)
        {
            // Checked before Enabled, so ManualDisable works on its own (see Enabled)
            if (IsCircuitOpen())
                return true;

            if (!Enabled)
                return false;

            if (string.IsNullOrEmpty(SubnetKey))
                return false;

            string counterKey = counter_key(SubnetKey, LoggedOn);
            int hourlyCeiling = LoggedOn ? LoggedOnHourlyLimit : HourlyLimit;
            int dailyCeiling = LoggedOn ? LoggedOnDailyLimit : DailyLimit;

            if ((SharedCache.Instance[HourCounterKeyPrefix + counterKey] is Counter hourCounter) && (hourCounter.Count >= hourlyCeiling))
                return true;

            if ((SharedCache.Instance[DayCounterKeyPrefix + counterKey] is Counter dayCounter) && (dayCounter.Count >= dailyCeiling))
                return true;

            return false;
        }

        /// <summary> Records one legitimate JP2 viewer open against the subnet's hourly/daily counters for its
        /// logon status and the site-wide hourly counter, auto-tripping the circuit breaker if the latter crosses
        /// <see cref="SiteWideHourlyThreshold"/>. Called only for opens that weren't already turned away by
        /// <see cref="IsOverBudget"/> -- there's no reason to spend budget on a request that never got the
        /// viewer anyway. Writes to temp/ratelimiting.txt the moment a subnet window reaches its ceiling. </summary>
        /// <param name="SubnetKey"> Subnet key from <see cref="ClientSubnetKey.From"/> </param>
        /// <param name="LoggedOn"> Whether this open is logged on, which selects the subnet counters it's recorded
        /// against. Anonymous and logged-on opens never share a subnet counter, so anonymous zooming can't use up
        /// logged-on visitors' allowance. </param>
        public static void RecordHit(string SubnetKey, bool LoggedOn)
        {
            if ((!Enabled) || (string.IsNullOrEmpty(SubnetKey)))
                return;

            string counterKey = counter_key(SubnetKey, LoggedOn);
            int hourCount = increment(HourCounterKeyPrefix + counterKey, TimeSpan.FromHours(1));
            int dayCount = increment(DayCounterKeyPrefix + counterKey, TimeSpan.FromDays(1));

            RateLimitLog_Gateway.Budget_Ceiling_Reached(RateLimitLog_Gateway.Event_JP2_Budget, SubnetKey, LoggedOn, "hourly", hourCount, LoggedOn ? LoggedOnHourlyLimit : HourlyLimit, "zoom opens", "zoomable viewer withheld");
            RateLimitLog_Gateway.Budget_Ceiling_Reached(RateLimitLog_Gateway.Event_JP2_Budget, SubnetKey, LoggedOn, "daily", dayCount, LoggedOn ? LoggedOnDailyLimit : DailyLimit, "zoom opens", "zoomable viewer withheld");

            int siteWideCount = increment(SiteWideHourCounterKey, TimeSpan.FromHours(1));
            if ((siteWideCount >= SiteWideHourlyThreshold) && (!IsCircuitOpen()))
                trip_circuit_breaker(siteWideCount, SubnetKey, LoggedOn);
        }

        /// <summary> Cache key suffix for one subnet's counters for one logon status -- anonymous and logged-on
        /// opens never share a counter </summary>
        private static string counter_key(string SubnetKey, bool LoggedOn)
        {
            return (LoggedOn ? "LOGGEDON|" : String.Empty) + SubnetKey;
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

        /// <summary> The automatic fuse: shuts the zoomable viewer off site-wide for <see cref="CircuitBreakerHours"/>, then
        /// lets it come back on its own -- no restart, no admin action, and nothing to remember to undo.
        /// (<see cref="ManualDisable"/> is the other, hand-operated path into <see cref="IsCircuitOpen"/>,
        /// and that one does NOT time out.) Also writes an entry to temp/ratelimiting.txt via RateLimitLog_Gateway,
        /// the same log every other limiter writes to -- there is no email/webhook alerting in this codebase to
        /// hook into yet. </summary>
        /// <param name="siteWideCount"> Site-wide opens this hour, including the one that crossed the threshold </param>
        /// <param name="SubnetKey"> Subnet of the open that crossed the threshold, only used in the log entry </param>
        /// <param name="LoggedOn"> Whether the open that crossed the threshold was logged on, only used in the log entry </param>
        /// <remarks> Several opens can cross the threshold at the same moment, and RecordHit's "not already open"
        /// check can't stop that on its own. The open state is claimed through SharedCache.GetOrAdd, which runs its
        /// factory under a lock and only when the key is missing, so exactly one caller creates the entry. Only
        /// that caller writes the log line, and the one-hour expiration is set once rather than pushed back by
        /// each racer. </remarks>
        private static void trip_circuit_breaker(int siteWideCount, string SubnetKey, bool LoggedOn)
        {
            bool claimed = false;
            SharedCache.Instance.GetOrAdd(CircuitOpenKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(Math.Max(1, CircuitBreakerHours));
                claimed = true;
                return DateTime.UtcNow;
            });

            if (!claimed)
                return;

            RateLimitLog_Gateway.Append(RateLimitLog_Gateway.Event_JP2_Circuit_Breaker, LoggedOn, SubnetKey,
                "SITE-WIDE: zoom opens this hour (" + siteWideCount + ") reached SiteWideHourlyThreshold (" + SiteWideHourlyThreshold +
                ") -- zoomable viewer off for everyone for " + Math.Max(1, CircuitBreakerHours) + " hour(s), then back automatically (set JP2RateLimiting:ManualDisable " +
                "to keep it off). This open just happened to be the one that crossed it.");
        }
    }
}
