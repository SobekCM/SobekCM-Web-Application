#region Using directives

using SobekCM.Core.MemoryMgmt;

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
    /// <para><b>Reaching a limit starts a lockout from that moment</b> -- <see cref="HourlyLockoutMinutes"/> for
    /// the hourly limit, <see cref="DailyLockoutHours"/> for the daily one -- rather than blocking only until the
    /// window happens to end. The counting and lockouts themselves live in <see cref="SubnetBudget"/>, shared with
    /// JP2RateLimiting_Gateway.</para>
    /// <para>Both calls are made from the item subwriters (Item_HtmlSubwriter and Print_Item_HtmlSubwriter):
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

        /// <summary> How many minutes a subnet is locked out of item pages, starting the moment it reaches an hourly
        /// limit (for requests with that logon status). Anything below 1 is treated as 1. </summary>
        public static int HourlyLockoutMinutes { get; set; } = 60;

        /// <summary> How many hours a subnet is locked out of item pages, starting the moment it reaches a daily
        /// limit (for requests with that logon status). Anything below 1 is treated as 1. </summary>
        public static int DailyLockoutHours { get; set; } = 24;

        private static readonly SubnetBudget budget = new SubnetBudget("SUSTRL_", RateLimitLog_Gateway.Event_Item_View_Budget, "item views", "item pages blocked");

        /// <summary> Pure check: are requests with this logon status from this subnet locked out of item pages, or
        /// at a limit? Never increments anything. </summary>
        /// <param name="SubnetKey"> Subnet key from <see cref="ClientSubnetKey.From"/>; NULL/empty always
        /// returns FALSE, since there is nothing to budget against </param>
        /// <param name="LoggedOn"> Whether this particular request is logged on, which selects both the counters
        /// and the ceilings it's held to -- see the class remarks for why logging on raises the ceiling rather
        /// than removing it </param>
        public static bool IsOverBudget(string SubnetKey, bool LoggedOn)
        {
            if (!Enabled)
                return false;

            return budget.IsOverBudget(SubnetKey, LoggedOn, LoggedOn ? LoggedOnHourlyLimit : HourlyLimit, LoggedOn ? LoggedOnDailyLimit : DailyLimit);
        }

        /// <summary> Records one item view that was actually served, starting a lockout (and writing it to
        /// temp/ratelimiting.txt) if it brings this subnet up to an hourly or daily limit for its logon status </summary>
        /// <param name="SubnetKey"> Subnet key from <see cref="ClientSubnetKey.From"/> </param>
        /// <param name="LoggedOn"> Whether this view is logged on, which selects the counters it's recorded against </param>
        /// <remarks> Call only after <see cref="IsOverBudget"/> has let the view through. A view that's turned
        /// away is never counted: blocked requests aren't load, and counting them is what once let an already
        /// blocked anonymous crawler keep pushing a counter up.
        /// <para>The IsOverBudget check and this increment are deliberately not atomic. Requests arriving at the
        /// same moment right at a limit can each pass the check, so a subnet can overshoot by up to the number
        /// of concurrent requests; after that the lockout blocks every check. This is a soft limit against
        /// sustained crawling, not access control, and the counters are already approximate (per server, reset on
        /// restart). An atomic reserve across both the hourly and daily counters, with rollback when only one
        /// admits the view, isn't worth that complexity. The lockout is still claimed, and logged, exactly once
        /// (see <see cref="SubnetBudget"/>).</para> </remarks>
        public static void RecordHit(string SubnetKey, bool LoggedOn)
        {
            if (!Enabled)
                return;

            budget.RecordHit(SubnetKey, LoggedOn, LoggedOn ? LoggedOnHourlyLimit : HourlyLimit, LoggedOn ? LoggedOnDailyLimit : DailyLimit, HourlyLockoutMinutes, DailyLockoutHours);
        }
    }
}
