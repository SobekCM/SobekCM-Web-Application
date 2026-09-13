#region Using directives

using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading;

#endregion

namespace SobekCM.Core.MemoryMgmt
{
    /// <summary> Phase 6 of the GCS rate-limiting plan: site-wide load shedding by requiring a logon. Two
    /// levels -- items only (anonymous visitors can still search and browse, but viewing an item needs a
    /// logon), or the whole site (every anonymous page request goes to the logon screen). </summary>
    /// <remarks> Unlike the other limiters, this isn't aimed at one crawler: it responds to total load. Only
    /// the items level ever trips automatically. Once site-wide item hits in an hour cross
    /// <see cref="ItemHitsPerHourThreshold"/>, items require a logon for <see cref="FuseHours"/>, then it clears
    /// itself (and trips again if traffic is still over). Closing the whole site is deliberately manual-only
    /// (<see cref="ManualMode"/>): a legitimate spike, like a class assignment, shouldn't be able to shut the
    /// library to the public on its own, and the items level already cuts off the real cost, GCS fetches.
    /// Logged-on users are never affected by either level; logging on or registering is the way through.
    /// <para>Item hits are recorded from SobekCM.QueryInitializerHelpers.ItemViewRateLimitInitializer (every
    /// item view, logged on or not, since this measures load). The items level is enforced in
    /// Item_HtmlSubwriter and Print_Item_HtmlSubwriter; the site level in LoginOnlyModeInitializer. All
    /// values are read once from appsettings.json at startup, so changing one needs an app restart.</para> </remarks>
    public static class LoginOnlyMode_Gateway
    {
        /// <summary> <see cref="ManualMode"/> value for no manual login-only mode </summary>
        public const string Mode_None = "None";

        /// <summary> <see cref="ManualMode"/> value requiring a logon to view items </summary>
        public const string Mode_Items = "Items";

        /// <summary> <see cref="ManualMode"/> value requiring a logon for every page in the site </summary>
        public const string Mode_Site = "Site";

        /// <summary> Whether the automatic items-level fuse is active at all. Does not affect
        /// <see cref="ManualMode"/>, which is always honored </summary>
        public static bool Enabled { get; set; }

        /// <summary> Site-wide item hits per hour, across every visitor, that trips the automatic fuse.
        /// PLACEHOLDER default -- set it from real traffic </summary>
        public static int ItemHitsPerHourThreshold { get; set; } = 10000;

        /// <summary> How many hours the automatic fuse keeps items login-only before clearing itself </summary>
        public static int FuseHours { get; set; } = 4;

        /// <summary> Login-only mode switched on by hand: <see cref="Mode_None"/>, <see cref="Mode_Items"/>, or
        /// <see cref="Mode_Site"/>. Never clears itself -- it stays until the setting is changed and the app
        /// restarts. The only way to require a logon for the whole site. </summary>
        public static string ManualMode { get; set; } = Mode_None;

        private const string ItemHourCounterKey = "LOGINONLY_ITEMHOUR";
        private const string FuseKey = "LOGINONLY_FUSE";

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

        /// <summary> Records one item view against the site-wide hourly counter, tripping the automatic fuse
        /// if this pushes it over <see cref="ItemHitsPerHourThreshold"/> </summary>
        public static void RecordItemHit()
        {
            if (!Enabled)
                return;

            Counter counter = (Counter)SharedCache.Instance.GetOrAdd(ItemHourCounterKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                return new Counter();
            });

            int count = Interlocked.Increment(ref counter.Count);
            if ((count >= ItemHitsPerHourThreshold) && (SharedCache.Instance[FuseKey] == null))
                trip_fuse(count);
        }

        /// <summary> The automatic fuse: items require a logon for <see cref="FuseHours"/>, then this clears on
        /// its own -- no restart and no admin action. Also appends a loud, greppable line to temp/exceptions.txt,
        /// the same alert mechanism the JP2 circuit breaker uses. </summary>
        private static void trip_fuse(int count)
        {
            SharedCache.Instance.Set(FuseKey, DateTime.UtcNow, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(FuseHours)
            });

            ExceptionLog_Gateway.Append(
                "*** LOGIN-ONLY MODE TRIPPED FOR ITEMS *** " + DateTime.UtcNow.ToString("O") +
                " -- site-wide item hits this hour (" + count + ") crossed ItemHitsPerHourThreshold (" +
                ItemHitsPerHourThreshold + "). Anonymous visitors must log on to view items for " + FuseHours +
                " hour(s), then it clears automatically -- no restart needed. To keep it on past that, set " +
                "LoginOnlyMode:ManualMode." + Environment.NewLine);
        }
    }
}
