#region Using directives

using Microsoft.Extensions.Caching.Memory;
using SobekCM.Core.MemoryMgmt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

#endregion

namespace SobekCM.Core.RateLimiting
{
    /// <summary> Phase 6 of the GCS rate-limiting plan: site-wide load shedding as item traffic climbs. Robots are
    /// paused first, then anonymous visitors are asked to log on -- items only, or (by hand) the whole site. </summary>
    /// <remarks> Unlike the other limiters, this isn't aimed at one crawler: it responds to total load. Two automatic
    /// levels, each counted separately -- see <see cref="RecordItemHit"/>:
    /// <list type="bullet">
    /// <item><description><b>Robot pause</b> at <see cref="RobotItemHitsPerHourThreshold"/> -- identified robots get
    /// HTTP 503 for item pages for <see cref="RobotPauseHours"/>. People see nothing. This is the cheap level: a
    /// paused robot's request stops being counted, so the total often never reaches the level below. Only robot views
    /// count toward it, in their <b>own</b> hourly counter, so a busy day of ordinary visitors can't pause robots on
    /// its own, and the counter is cleared when a pause starts so robots return to a full allowance.</description></item>
    /// <item><description><b>Items need a logon</b> at <see cref="ItemHitsPerHourThreshold"/> -- anonymous visitors
    /// see "Log On to View Items" for <see cref="FuseHours"/>, then it clears itself (and trips again if traffic is
    /// still over). Every view counts toward it, robots included, in a separate site-wide counter.</description></item>
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

        /// <summary> Item hits per hour <b>by identified robots</b> that pauses them -- the first and cheapest level,
        /// since robots are the traffic most likely to be driving a spike. Counted in its own hourly counter, separate
        /// from the site-wide total behind <see cref="ItemHitsPerHourThreshold"/>, so ordinary visitors being busy
        /// never pauses robots. 0 turns it off. </summary>
        public static int RobotItemHitsPerHourThreshold { get; set; } = 3000;

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
        private const string RobotItemHourCounterKey = "LOGINONLY_ROBOTITEMHOUR";
        private const string FuseKey = "LOGINONLY_FUSE";
        private const string RobotPauseKey = "LOGINONLY_ROBOTPAUSE";

        /// <summary> Kinds of visitor the site-wide hourly item views are split into, by user agent, so the fuse's log
        /// entry says what filled the hour (the view that crosses the line tells you nothing). Coarse on purpose: a few
        /// fixed buckets, never raw user agents. </summary>
        private static readonly string[] Visitor_Kinds = { "robots", "iPhone/iPad", "Android", "Chrome on Mac", "Windows", "other" };

        /// <summary> Boxed count for the hourly window; a reference type so concurrent requests sharing the same
        /// cache entry can increment it via Interlocked, same shape as the other limiters' counters. </summary>
        private sealed class Counter
        {
            public int Count;

            /// <summary> Views per <see cref="Visitor_Kinds"/> entry; only filled for the site-wide item counter </summary>
            public readonly int[] Kinds = new int[Visitor_Kinds.Length];
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

        /// <summary> Records one item view against the site-wide hourly counter (every view) and, if it's from a
        /// robot, separately against the robot-only hourly counter too -- tripping the automatic fuse and/or the
        /// robot pause if either of those counters, independently, reaches its own threshold on this view </summary>
        /// <param name="SubnetKey"> Subnet key of this view, only used in the log entry if this view trips a level </param>
        /// <param name="LoggedOn"> Whether this view is logged on, only used in the log entry if this view trips a level </param>
        /// <param name="Robot"> Whether this view is from an identified robot. Every view counts toward the site-wide
        /// total behind the fuse; only a robot's counts toward the robot pause. </param>
        public static void RecordItemHit(string SubnetKey, bool LoggedOn, bool Robot)
        {
            if (!Enabled)
                return;

            int count = increment_hourly(ItemHourCounterKey, visitor_kind(Robot, RateLimitLog_Gateway.current_user_agent()));

            if ((count >= ItemHitsPerHourThreshold) && (SharedCache.Instance[FuseKey] == null))
                trip_fuse(count, SubnetKey, LoggedOn);

            if ((!Robot) || (RobotItemHitsPerHourThreshold <= 0))
                return;

            int robotCount = increment_hourly(RobotItemHourCounterKey);

            if ((robotCount >= RobotItemHitsPerHourThreshold) && (SharedCache.Instance[RobotPauseKey] == null))
                trip_robot_pause(robotCount, SubnetKey, LoggedOn);
        }

        /// <summary> Adds one to an hourly counter and returns its new value. The window is fixed: it starts when the
        /// counter is created and more hits don't extend it. </summary>
        /// <param name="Key"> Counter's SharedCache key </param>
        /// <param name="Kind"> Index into <see cref="Visitor_Kinds"/> to count this hit under as well, or -1 for none </param>
        private static int increment_hourly(string Key, int Kind = -1)
        {
            Counter counter = (Counter)SharedCache.Instance.GetOrAdd(Key, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                return new Counter();
            });

            if (Kind >= 0)
                Interlocked.Increment(ref counter.Kinds[Kind]);

            return Interlocked.Increment(ref counter.Count);
        }

        /// <summary> Which of the <see cref="Visitor_Kinds"/> an item view is counted under </summary>
        /// <remarks> "Chrome on Mac" has its own bucket because DCDP's logs (2026-10) showed residential-proxy crawlers
        /// using it almost exclusively: hundreds of thousands of IPs, one or two views each, never loading a stylesheet.
        /// Fake phone browsers never get here; UserAgentBlocklist_Gateway refuses them first. </remarks>
        private static int visitor_kind(bool Robot, string UserAgent)
        {
            if (Robot)
                return 0;
            if (String.IsNullOrEmpty(UserAgent))
                return 5;
            if ((UserAgent.Contains("iPhone", StringComparison.Ordinal)) || (UserAgent.Contains("iPad", StringComparison.Ordinal)))
                return 1;
            if (UserAgent.Contains("Android", StringComparison.Ordinal))
                return 2;
            if ((UserAgent.Contains("Macintosh", StringComparison.Ordinal)) && (UserAgent.Contains("Chrome/", StringComparison.Ordinal)))
                return 3;
            if (UserAgent.Contains("Windows", StringComparison.Ordinal))
                return 4;
            return 5;
        }

        /// <summary> The three biggest <see cref="Visitor_Kinds"/> in this hour's site-wide item views, e.g.
        /// "Chrome on Mac 6120, Android 2100, robots 900", or an empty string if nothing is counted </summary>
        private static string top_visitor_kinds()
        {
            if (!(SharedCache.Instance[ItemHourCounterKey] is Counter counter))
                return String.Empty;

            List<string> parts = new List<string>();
            foreach (int kind in Enumerable.Range(0, Visitor_Kinds.Length).OrderByDescending(k => Volatile.Read(ref counter.Kinds[k])).Take(3))
            {
                int views = Volatile.Read(ref counter.Kinds[kind]);
                if (views > 0)
                    parts.Add(Visitor_Kinds[kind] + " " + views);
            }

            return String.Join(", ", parts);
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

            // Start the robot counter over. Paused robots aren't counted anyway, but clearing it here means that when
            // the pause lifts they begin at zero with a full allowance, instead of returning to a count still sitting
            // at the threshold and re-tripping on their first request.
            SharedCache.Instance.Remove(RobotItemHourCounterKey);

            RateLimitLog_Gateway.Append(RateLimitLog_Gateway.Event_Robot_Pause, LoggedOn, SubnetKey,
                "SITE-WIDE: robot item views this hour (" + count + ") reached RobotItemHitsPerHourThreshold (" + RobotItemHitsPerHourThreshold +
                ") -- identified robots get HTTP 503 for item pages for " + Math.Max(1, RobotPauseHours) + " hour(s), then it clears " +
                "automatically. People are unaffected. Robot counting starts over now, so when the pause lifts they get a full " +
                "allowance again. Robot views also count toward ItemHitsPerHourThreshold (" + ItemHitsPerHourThreshold + "), which is the " +
                "site-wide total behind the login-only fuse.");
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
                "(set LoginOnlyMode:ManualMode to keep it on). This view just happened to be the one that crossed it. " +
                "Biggest kinds of visitor this hour: " + top_visitor_kinds() + ".");
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
