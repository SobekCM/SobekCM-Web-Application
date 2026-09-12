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
    /// <para>Counters live in <see cref="SharedCache"/> under their own key prefixes, same as the other two
    /// limiters. RecordHit is called centrally from SobekCM.QueryInitializerHelpers.ItemViewRateLimitInitializer,
    /// which already fires on exactly this signal; IsOverBudget is a pure read, called from the page-image
    /// viewers to decide whether to withhold the signed URL. Config is set once from Program.cs, so changing
    /// a limit needs an app restart -- same as every other gateway here.</para> </remarks>
    public static class SustainedRateLimiting_Gateway
    {
        /// <summary> Whether the sustained-crawl budget is active at all; false skips every check and never
        /// counts anything </summary>
        public static bool Enabled { get; set; }

        /// <summary> Maximum anonymous item views allowed from a single subnet within an hour </summary>
        public static int HourlyLimit { get; set; } = 400;

        /// <summary> Maximum anonymous item views allowed from a single subnet within a day </summary>
        public static int DailyLimit { get; set; } = 2500;

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
        /// <param name="SubnetKey"> Subnet key from <see cref="AnonymousRequest.Subnet_Key"/>; NULL/empty
        /// always returns FALSE, since there is nothing to budget against </param>
        public static bool IsOverBudget(string SubnetKey)
        {
            if ((!Enabled) || (string.IsNullOrEmpty(SubnetKey)))
                return false;

            if ((SharedCache.Instance[HourCounterKeyPrefix + SubnetKey] is Counter hourCounter) && (hourCounter.Count >= HourlyLimit))
                return true;

            if ((SharedCache.Instance[DayCounterKeyPrefix + SubnetKey] is Counter dayCounter) && (dayCounter.Count >= DailyLimit))
                return true;

            return false;
        }

        /// <summary> Records one anonymous item view against this subnet's hourly and daily windows. </summary>
        /// <remarks> Unlike the JP2 budget, this counts every qualifying view including ones already being
        /// served degraded -- a crawler that keeps hammering after it has been cut off keeps its own counter
        /// pinned, which is the desired outcome. It cannot extend the lockout indefinitely, though: each
        /// window expires a fixed hour/day after the counter was first created, not after the last hit. </remarks>
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
