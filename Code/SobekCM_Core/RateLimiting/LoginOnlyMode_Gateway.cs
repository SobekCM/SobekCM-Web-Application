#region Using directives

using Microsoft.Extensions.Caching.Memory;
using SobekCM.Core.MemoryMgmt;
using System;
using System.Threading;

#endregion

namespace SobekCM.Core.RateLimiting
{
    /// <summary> Phase 6 of the GCS rate-limiting plan: site-wide load shedding as item traffic climbs. Robots are
    /// paused first, then anonymous visitors are asked to log on -- items only, or (by hand) the whole site. </summary>
    /// <remarks> Unlike the other limiters, this isn't aimed at one crawler: it responds to total load. Two automatic
    /// levels trip off the same site-wide hourly item counter:
    /// <list type="bullet">
    /// <item><description><b>Robot pause</b> at <see cref="RobotItemHitsPerHourThreshold"/> -- identified robots get
    /// HTTP 503 for item pages for <see cref="RobotPauseHours"/>. People see nothing. This is the cheap level: a
    /// paused robot's request stops being counted, so the total often never reaches the level below.</description></item>
    /// <item><description><b>Items need a logon</b> at <see cref="ItemHitsPerHourThreshold"/> -- anonymous visitors
    /// see "Log On to View Items" for <see cref="FuseHours"/>, then it clears itself (and trips again if traffic is
    /// still over).</description></item>
    /// </list>
    /// Closing the whole site is deliberately manual-only (<see cref="ManualMode"/>): a legitimate spike, like a class
    /// assignment, shouldn't be able to shut the library to the public on its own, and the items level already cuts
    /// off the real cost, GCS fetches. Logged-on users are never affected; logging on or registering is the way through.
    /// <para>Item hits are recorded from SobekCM.QueryInitializerHelpers.ItemViewRateLimitInitializer (every item view,
    /// logged on or not, since this measures load). The robot pause is enforced in RobotItemPauseInitializer, which runs
    /// before that counting; the items level in Item_HtmlSubwriter and Print_Item_HtmlSubwriter; the site level in
    /// LoginOnlyModeInitializer. All values are read once from appsettings.json at startup, so changing one needs an
    /// app restart.</para> </remarks>
    public static class LoginOnlyMode_Gateway
    {
        /// <summary> <see cref="ManualMode"/> value for no manual login-only mode </summary>
        public const string Mode_None = "None";

        /// <summary> <see cref="ManualMode"/> value requiring a logon to view items </summary>
        public const string Mode_Items = "Items";

        /// <summary> <see cref="ManualMode"/> value requiring a logon for every page in the site </summary>
        public const string Mode_Site = "Site";

        /// <summary> Whether the automatic levels (robot pause and items-level fuse) are active at all. Does not
        /// affect <see cref="ManualMode"/>, which is always honored </summary>
        public static bool Enabled { get; set; }

        /// <summary> Site-wide item hits per hour, across every visitor, that pauses identified robots -- the first
        /// and cheapest level, since robots are the traffic most likely to be driving a spike. 0 turns it off. </summary>
        public static int RobotItemHitsPerHourThreshold { get; set; } = 5000;

        /// <summary> How many hours identified robots are refused item pages once
        /// <see cref="RobotItemHitsPerHourThreshold"/> trips. Anything below 1 is treated as 1. </summary>
        public static int RobotPauseHours { get; set; } = 2;

        /// <summary> Site-wide item hits per hour, across every visitor, that trips the automatic fuse.
        /// PLACEHOLDER default -- set it from real traffic </summary>
        public static int ItemHitsPerHourThreshold { get; set; } = 10000;

        /// <summary> How many hours the automatic fuse keeps items login-only before clearing itself </summary>
        public static int FuseHours { get; set; } = 2;

        /// <summary> Login-only mode switched on by hand: <see cref="Mode_None"/>, <see cref="Mode_Items"/>, or
        /// <see cref="Mode_Site"/>. Never clears itself -- it stays until the setting is changed and the app
        /// restarts. The only way to require a logon for the whole site. </summary>
        public static string ManualMode { get; set; } = Mode_None;

        private const string ItemHourCounterKey = "LOGINONLY_ITEMHOUR";
        private const string FuseKey = "LOGINONLY_FUSE";
        private const string RobotPauseKey = "LOGINONLY_ROBOTPAUSE";

        /// <summary> Boxed count for the hourly window; a reference type so concurrent requests sharing the same
        /// cache entry can increment it via Interlocked, same shape as the other limiters' counters. </summary>
        private sealed class Counter
        {
            public int Count;
        }

        /// <summary> Whether the whole site currently requires a logon for anonymous page requests </summary>
        public static bool Site_Requires_Logon()
        {
            return String.Equals(ManualMode, Mode_Site, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary> Whether viewing an item currently requires a logon -- set by hand (items or site level),
        /// or because the automatic fuse tripped and hasn't expired yet </summary>
        public static bool Items_Require_Logon()
        {
            return Site_Requires_Logon()
                || String.Equals(ManualMode, Mode_Items, StringComparison.OrdinalIgnoreCase)
                || (SharedCache.Instance[FuseKey] != null);
        }

        /// <summary> How many seconds an identified robot should be refused item pages for, or NULL if robots are
        /// free to crawl items right now </summary>
        /// <remarks> Two things put robots on hold, and the longer one wins:
        /// <list type="bullet">
        /// <item><description>the robot pause, from <see cref="RobotItemHitsPerHourThreshold"/></description></item>
        /// <item><description>items requiring a logon at all -- a robot can't log on, so the alternative is letting it
        /// index the "Log On to View Items" page, which is worse for the site than a temporary 503</description></item>
        /// </list>
        /// The value is meant for a Retry-After header. A hand-set <see cref="ManualMode"/> never expires, so robots
        /// are told to come back in <see cref="RobotPauseHours"/> and will keep being turned away until it's cleared. </remarks>
        public static int? Robot_Items_Blocked_Seconds_Left()
        {
            int? secondsLeft = seconds_left(SharedCache.Instance[RobotPauseKey]);

            if (Items_Require_Logon())
            {
                // The fuse entry holds when it ends; a manual mode has no end, so fall back to the pause length
                int? logonSecondsLeft = seconds_left(SharedCache.Instance[FuseKey]) ?? (Math.Max(1, RobotPauseHours) * 3600);
                if ((!secondsLeft.HasValue) || (logonSecondsLeft.Value > secondsLeft.Value))
                    secondsLeft = logonSecondsLeft;
            }

            return secondsLeft;
        }

        /// <summary> Records one item view against the site-wide hourly counter, pausing robots and/or tripping the
        /// automatic fuse if this view brings the count up to either threshold </summary>
        /// <param name="SubnetKey"> Subnet key of this view, only used in the log entry if this view trips a level </param>
        /// <param name="LoggedOn"> Whether this view is logged on, only used in the log entry if this view trips a level </param>
        public static void RecordItemHit(string SubnetKey, bool LoggedOn)
        {
            if (!Enabled)
                return;

            Counter counter = (Counter)SharedCache.Instance.GetOrAdd(ItemHourCounterKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                return new Counter();
            });

            int count = Interlocked.Increment(ref counter.Count);

            if ((RobotItemHitsPerHourThreshold > 0) && (count >= RobotItemHitsPerHourThreshold) && (SharedCache.Instance[RobotPauseKey] == null))
                trip_robot_pause(count, SubnetKey, LoggedOn);

            if ((count >= ItemHitsPerHourThreshold) && (SharedCache.Instance[FuseKey] == null))
                trip_fuse(count, SubnetKey, LoggedOn);
        }

        /// <summary> The robot pause: identified robots are refused item pages for <see cref="RobotPauseHours"/>, then
        /// this clears on its own </summary>
        /// <remarks> Claimed through SharedCache.GetOrAdd, which creates the entry atomically, so several views crossing
        /// the threshold at once still start, and log, one pause. </remarks>
        private static void trip_robot_pause(int count, string SubnetKey, bool LoggedOn)
        {
            TimeSpan duration = TimeSpan.FromHours(Math.Max(1, RobotPauseHours));

            bool claimed = false;
            SharedCache.Instance.GetOrAdd(RobotPauseKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = duration;
                claimed = true;
                return DateTime.UtcNow.Add(duration);
            });

            if (!claimed)
                return;

            RateLimitLog_Gateway.Append(RateLimitLog_Gateway.Event_Robot_Pause, LoggedOn, SubnetKey,
                "SITE-WIDE: item views this hour (" + count + ") reached RobotItemHitsPerHourThreshold (" + RobotItemHitsPerHourThreshold +
                ") -- identified robots get HTTP 503 for item pages for " + Math.Max(1, RobotPauseHours) + " hour(s), then it clears " +
                "automatically. People are unaffected, and paused robot requests stop counting toward ItemHitsPerHourThreshold (" +
                ItemHitsPerHourThreshold + ").");
        }

        /// <summary> The automatic fuse: items require a logon for <see cref="FuseHours"/>, then this clears on
        /// its own -- no restart and no admin action. Also writes an entry to temp/ratelimiting.txt, the same
        /// log every other limiter writes to. </summary>
        /// <remarks> Several item views can cross the threshold at the same moment, and RecordItemHit's "no fuse
        /// yet" check can't stop that on its own. The fuse is claimed through SharedCache.GetOrAdd, which runs its
        /// factory under a lock and only when the key is missing, so exactly one caller creates it. Only that
        /// caller writes the log line, and the <see cref="FuseHours"/> expiration is set once rather than pushed
        /// back by each racer. The entry holds when the fuse ends, so Robot_Items_Blocked_Seconds_Left can turn it
        /// into a Retry-After. </remarks>
        private static void trip_fuse(int count, string SubnetKey, bool LoggedOn)
        {
            TimeSpan duration = TimeSpan.FromHours(Math.Max(1, FuseHours));

            bool claimed = false;
            SharedCache.Instance.GetOrAdd(FuseKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = duration;
                claimed = true;
                return DateTime.UtcNow.Add(duration);
            });

            if (!claimed)
                return;

            RateLimitLog_Gateway.Append(RateLimitLog_Gateway.Event_Login_Only_Fuse, LoggedOn, SubnetKey,
                "SITE-WIDE: item views this hour (" + count + ") reached ItemHitsPerHourThreshold (" + ItemHitsPerHourThreshold +
                ") -- anonymous visitors must log on to view items for " + Math.Max(1, FuseHours) + " hour(s), then it clears automatically " +
                "(set LoginOnlyMode:ManualMode to keep it on). This view just happened to be the one that crossed it.");
        }

        /// <summary> Seconds left on a cache entry holding when something ends, or NULL if there's nothing active </summary>
        private static int? seconds_left(object EndsEntry)
        {
            if (EndsEntry is DateTime endsUtc)
            {
                double secondsLeft = (endsUtc - DateTime.UtcNow).TotalSeconds;
                if (secondsLeft > 0)
                    return (int)Math.Ceiling(secondsLeft);
            }

            return null;
        }
    }
}
