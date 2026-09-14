#region Using directives

using Microsoft.Extensions.Caching.Memory;
using SobekCM.Core.MemoryMgmt;
using System;
using System.Threading;

#endregion

namespace SobekCM.Core.RateLimiting
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
    /// would leave the easiest possible bypass wide open. Anonymous and logged-on views are counted separately
    /// per subnet, each against its own ceiling, so a burst of anonymous traffic can never use up logged-on
    /// visitors' allowance -- only logged-on traffic can get logged-on visitors blocked. The cost is that a
    /// scraper using an exported cookie gets both allowances, which is still bounded. Note there is
    /// deliberately no per-user tracking: an account-level budget would mean per-account state to keep, in
    /// exchange for a distinction that would never be used, since no individual user is ever going to be
    /// granted unlimited access to the items.</para>
    /// <para>Counters live in <see cref="SharedCache"/> under their own key prefixes, same as the other two
    /// limiters. Both calls are made from the item subwriters (Item_HtmlSubwriter and Print_Item_HtmlSubwriter):
    /// IsOverBudget first, to decide whether to write any item content at all, then RecordHit only for a view
    /// that's actually served -- a blocked view is never counted. Config is set once from Program.cs, so
    /// changing a limit needs an app restart -- same as every other gateway here.</para> </remarks>
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

        /// <summary> Pure check: have requests with this logon status from this subnet used up their item-view
        /// budget for either window? Never increments anything. </summary>
        /// <param name="SubnetKey"> Subnet key from <see cref="ClientSubnetKey.From"/>; NULL/empty always
        /// returns FALSE, since there is nothing to budget against </param>
        /// <param name="LoggedOn"> Whether this particular request is logged on, which selects both the counters
        /// and the ceilings it's held to -- see the class remarks for why logging on raises the ceiling rather
        /// than removing it </param>
        public static bool IsOverBudget(string SubnetKey, bool LoggedOn)
        {
            if ((!Enabled) || (string.IsNullOrEmpty(SubnetKey)))
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

        /// <summary> Records one item view that was actually served against this subnet's hourly and daily
        /// windows for its logon status, writing to temp/ratelimiting.txt the moment either window reaches its
        /// ceiling. </summary>
        /// <param name="SubnetKey"> Subnet key from <see cref="ClientSubnetKey.From"/> </param>
        /// <param name="LoggedOn"> Whether this view is logged on, which selects the counters it's recorded against </param>
        /// <remarks> Call only after <see cref="IsOverBudget"/> has let the view through. A view that's turned
        /// away is never counted: blocked requests aren't load, and counting them is what once let an already
        /// blocked anonymous crawler keep pushing a counter up. Nothing past the ceiling is counted, so the count
        /// holds there until the window expires, a fixed hour/day after the counter was first created.
        /// <para>The IsOverBudget check and this increment are deliberately not atomic. Requests arriving at the
        /// same moment right at the ceiling can each pass the check, so a subnet can overshoot by up to the number
        /// of concurrent requests, once per window; after that every check blocks. This is a soft limit against
        /// sustained crawling, not access control, and the counters are already approximate (per server, reset on
        /// restart). An atomic reserve across both the hourly and daily counters, with rollback when only one
        /// admits the view, isn't worth that complexity. The ceiling log line is still written exactly once,
        /// since the increment itself is atomic.</para> </remarks>
        public static void RecordHit(string SubnetKey, bool LoggedOn)
        {
            if ((!Enabled) || (string.IsNullOrEmpty(SubnetKey)))
                return;

            string counterKey = counter_key(SubnetKey, LoggedOn);
            int hourCount = increment(HourCounterKeyPrefix + counterKey, TimeSpan.FromHours(1));
            int dayCount = increment(DayCounterKeyPrefix + counterKey, TimeSpan.FromDays(1));

            RateLimitLog_Gateway.Budget_Ceiling_Reached(RateLimitLog_Gateway.Event_Item_View_Budget, SubnetKey, LoggedOn, "hourly", hourCount, LoggedOn ? LoggedOnHourlyLimit : HourlyLimit, "item views", "item pages blocked");
            RateLimitLog_Gateway.Budget_Ceiling_Reached(RateLimitLog_Gateway.Event_Item_View_Budget, SubnetKey, LoggedOn, "daily", dayCount, LoggedOn ? LoggedOnDailyLimit : DailyLimit, "item views", "item pages blocked");
        }

        /// <summary> Cache key suffix for one subnet's counters for one logon status -- anonymous and logged-on
        /// views never share a counter </summary>
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
    }
}
