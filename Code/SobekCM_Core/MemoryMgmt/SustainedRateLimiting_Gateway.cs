#region Using directives

using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading;

#endregion

namespace SobekCM.Core.MemoryMgmt
{
    /// <summary> Phase 2 of the GCS rate-limiting plan: a long-window budget on anonymous item views, keyed
    /// on the requester's /24 (or /48) subnet, aimed squarely at the crawler that never bursts. </summary>
    /// <remarks> This exists because of a real incident: a crawler that had not read robots.txt pulled page
    /// images roughly every 1.5 seconds, all day. At ~0.67 requests/second that sits permanently under
    /// <see cref="RateLimiting_Gateway"/>'s burst rule, so burst protection never fired once -- while still
    /// adding up to thousands of requests an hour. Short windows cannot see that; only a long one can.
    /// <para>What gets counted is the item page view, NOT the image fetch. Under GCS the full-size page
    /// image is handed to the browser as a direct signed URL (see JPEG_ItemViewer's use of
    /// SobekFileSystem.Resource_Web_Uri), so the bytes are pulled straight from Google and never reach this
    /// app at all -- they are uncountable and unmeasurable here, which is also why this budget tracks
    /// requests only and no byte count exists. But the app is the only thing that can mint that signed URL,
    /// so one anonymous item view equals one signed URL equals one GCS fetch: a clean 1:1 proxy.</para>
    /// <para><b>Logged-on requests are budgeted too, just more permissively.</b> A logged-on session is
    /// carried by a cookie, and a cookie can be exported out of a browser and handed to a scraper -- so
    /// "logged on" says someone has an account, not that they won't hammer the site, and exempting them
    /// would leave the easiest possible bypass wide open. Everything is counted against one counter per
    /// subnet; only the ceiling differs. Note there is deliberately no per-user tracking: an account-level
    /// budget would mean per-account state to keep, in exchange for a distinction that would never be
    /// used, since no individual user is ever going to be granted unlimited access to the items.</para>
    /// <para>Counters live in <see cref="SharedCache"/> under their own key prefixes, same as the other two
    /// limiters. RecordHit is called centrally from SobekCM.QueryInitializerHelpers.ItemViewRateLimitInitializer,
    /// which already fires on exactly this signal; IsOverBudget is a pure read, called from the item
    /// subwriters to decide whether to write any item content at all. Config is set once from Program.cs,
    /// so changing a limit needs an app restart -- same as every other gateway here.</para> </remarks>
    public static class SustainedRateLimiting_Gateway
    {
        /// <summary> Whether the sustained-crawl budget is active at all; false skips every check and never
        /// counts anything </summary>
        public static bool Enabled { get; set; }

        /// <summary> Maximum item views allowed from a single subnet within an hour, for a request that
        /// isn't logged on </summary>
        public static int HourlyLimit { get; set; } = 400;

        /// <summary> Maximum item views allowed from a single subnet within a day, for a request that isn't
        /// logged on </summary>
        public static int DailyLimit { get; set; } = 2500;

        /// <summary> Maximum item views allowed from a single subnet within an hour, for a logged-on
        /// request -- the more permissive ceiling, not an exemption </summary>
        public static int LoggedOnHourlyLimit { get; set; } = 1200;

        /// <summary> Maximum item views allowed from a single subnet within a day, for a logged-on request </summary>
        public static int LoggedOnDailyLimit { get; set; } = 7500;

        private const string HourCounterKeyPrefix = "SUSTRL_HOUR|";
        private const string DayCounterKeyPrefix = "SUSTRL_DAY|";

        /// <summary> Boxed count for one window; a reference type so concurrent requests sharing the same
        /// cache entry can increment it via Interlocked, same shape as the other limiters' counters. </summary>
        private sealed class Counter
        {
            public int Count;
        }

        /// <summary> Pure check: has this subnet used up its item-view budget for either window? Never
        /// increments anything. </summary>
        /// <param name="SubnetKey"> Subnet key from <see cref="ClientSubnetKey.From"/>; NULL/empty always
        /// returns FALSE, since there is nothing to budget against </param>
        /// <param name="LoggedOn"> Whether this particular request is logged on, which selects the ceiling
        /// the shared counter is compared against -- see the class remarks for why logging on raises the
        /// ceiling rather than removing it </param>
        public static bool IsOverBudget(string SubnetKey, bool LoggedOn)
        {
            if ((!Enabled) || (string.IsNullOrEmpty(SubnetKey)))
                return false;

            int hourlyCeiling = LoggedOn ? LoggedOnHourlyLimit : HourlyLimit;
            int dailyCeiling = LoggedOn ? LoggedOnDailyLimit : DailyLimit;

            if ((SharedCache.Instance[HourCounterKeyPrefix + SubnetKey] is Counter hourCounter) && (hourCounter.Count >= hourlyCeiling))
                return true;

            if ((SharedCache.Instance[DayCounterKeyPrefix + SubnetKey] is Counter dayCounter) && (dayCounter.Count >= dailyCeiling))
                return true;

            return false;
        }

        /// <summary> Records one item view against this subnet's hourly and daily windows. </summary>
        /// <remarks> One counter per subnet covering logged-on and anonymous traffic alike -- only the
        /// ceiling it gets compared against differs, so a subnet that has spent its anonymous allowance can
        /// still be served by logging on, and an exported cookie buys a scraper more room but not an escape.
        /// <para>Counts every qualifying view including ones already being blocked, so a crawler that keeps
        /// hammering after cutoff keeps its own counter pinned. It cannot extend the lockout indefinitely,
        /// though: each window expires a fixed hour/day after the counter was first created, not after the
        /// last hit.</para> </remarks>
        public static void RecordHit(string SubnetKey)
        {
            if ((!Enabled) || (string.IsNullOrEmpty(SubnetKey)))
                return;

            increment(HourCounterKeyPrefix + SubnetKey, TimeSpan.FromHours(1));
            increment(DayCounterKeyPrefix + SubnetKey, TimeSpan.FromDays(1));
        }

        private static void increment(string key, TimeSpan window)
        {
            Counter counter = (Counter)SharedCache.Instance.GetOrAdd(key, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = window;
                return new Counter();
            });

            Interlocked.Increment(ref counter.Count);
        }
    }
}
