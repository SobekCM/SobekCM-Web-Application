#region Using directives

using SobekCM.Core.MemoryMgmt;
using System;
using System.Threading;

#endregion

namespace SobekCM.Core.RateLimiting
{
    /// <summary> Shared counting and lockout logic behind the two per-subnet budgets: item views
    /// (<see cref="SustainedRateLimiting_Gateway"/>) and JP2 zoom opens (<see cref="JP2RateLimiting_Gateway"/>) </summary>
    /// <remarks> For each subnet there are separate anonymous and logged-on counters, each over a fixed hourly and a
    /// fixed daily window (a window starts when its counter is created and more hits don't extend it).
    /// <para>Reaching a limit starts a <b>lockout from that moment</b>: the hourly limit locks the subnet's requests
    /// with that logon status out for the hourly lockout length, the daily limit for the daily lockout length. The
    /// counter that tripped is cleared when the lockout starts (both counters, for a daily lockout), so counting
    /// starts fresh once it ends. That makes the block a predictable length, rather than "whatever is left of the
    /// window" -- which let a slow crawler off with a few minutes and was hard to reason about.</para>
    /// <para>The lockout is claimed through SharedCache.GetOrAdd, which creates the entry atomically, so only the
    /// request that actually starts a lockout logs it. Everything lives in <see cref="SharedCache"/>, so like the
    /// other limiters it's per server and resets on restart.</para> </remarks>
    internal sealed class SubnetBudget
    {
        private readonly string hourCounterPrefix;
        private readonly string dayCounterPrefix;
        private readonly string hourLockoutPrefix;
        private readonly string dayLockoutPrefix;
        private readonly string logEvent;
        private readonly string unit;
        private readonly string consequence;

        /// <summary> Boxed count for one window; a reference type so concurrent requests sharing the same
        /// cache entry can increment it via Interlocked, same shape as the other limiters' counters. </summary>
        private sealed class Counter
        {
            public int Count;
        }

        /// <summary> Constructor for a new instance of the SubnetBudget class </summary>
        /// <param name="KeyPrefix"> SharedCache key prefix for this budget, e.g. "SUSTRL_" </param>
        /// <param name="LogEvent"> Event name used in temp/ratelimiting.txt, one of the RateLimitLog_Gateway.Event_ constants </param>
        /// <param name="Unit"> What's being counted, for the log, e.g. "item views" </param>
        /// <param name="Consequence"> What a lockout means, for the log, e.g. "item pages blocked" </param>
        public SubnetBudget(string KeyPrefix, string LogEvent, string Unit, string Consequence)
        {
            hourCounterPrefix = KeyPrefix + "HOUR|";
            dayCounterPrefix = KeyPrefix + "DAY|";
            hourLockoutPrefix = KeyPrefix + "HOURLOCK|";
            dayLockoutPrefix = KeyPrefix + "DAYLOCK|";
            logEvent = LogEvent;
            unit = Unit;
            consequence = Consequence;
        }

        /// <summary> Pure check: are requests with this logon status from this subnet currently locked out, or at a
        /// ceiling? Never increments anything. </summary>
        /// <param name="SubnetKey"> Subnet key from <see cref="ClientSubnetKey.From"/>; NULL/empty always returns FALSE </param>
        /// <param name="LoggedOn"> Whether this request is logged on, which selects the counters and lockouts </param>
        /// <param name="HourlyCeiling"> Hourly limit for this logon status </param>
        /// <param name="DailyCeiling"> Daily limit for this logon status </param>
        public bool IsOverBudget(string SubnetKey, bool LoggedOn, int HourlyCeiling, int DailyCeiling)
        {
            if (string.IsNullOrEmpty(SubnetKey))
                return false;

            string key = counter_key(SubnetKey, LoggedOn);
            if ((SharedCache.Instance[dayLockoutPrefix + key] != null) || (SharedCache.Instance[hourLockoutPrefix + key] != null))
                return true;

            // Backstop for the instant between a counter reaching its ceiling and its lockout being claimed
            if ((SharedCache.Instance[hourCounterPrefix + key] is Counter hourCounter) && (hourCounter.Count >= HourlyCeiling))
                return true;

            if ((SharedCache.Instance[dayCounterPrefix + key] is Counter dayCounter) && (dayCounter.Count >= DailyCeiling))
                return true;

            return false;
        }

        /// <summary> Records one hit that was actually served, starting a lockout (and logging it) if this hit
        /// brings either window up to its ceiling </summary>
        /// <param name="SubnetKey"> Subnet key from <see cref="ClientSubnetKey.From"/> </param>
        /// <param name="LoggedOn"> Whether this hit is logged on, which selects the counters and lockouts </param>
        /// <param name="HourlyCeiling"> Hourly limit for this logon status </param>
        /// <param name="DailyCeiling"> Daily limit for this logon status </param>
        /// <param name="HourlyLockoutMinutes"> How long the hourly limit locks out for; anything below 1 is treated as 1 </param>
        /// <param name="DailyLockoutHours"> How long the daily limit locks out for; anything below 1 is treated as 1 </param>
        /// <remarks> Call only after <see cref="IsOverBudget"/> has let the hit through, so a blocked request is never
        /// counted. The check and this increment are deliberately not atomic: requests arriving at the same moment
        /// right at a ceiling can each get through, overshooting by a few. That's fine for a soft limit against
        /// sustained crawling, and the lockout itself is still claimed, and logged, exactly once. </remarks>
        public void RecordHit(string SubnetKey, bool LoggedOn, int HourlyCeiling, int DailyCeiling, int HourlyLockoutMinutes, int DailyLockoutHours)
        {
            if (string.IsNullOrEmpty(SubnetKey))
                return;

            string key = counter_key(SubnetKey, LoggedOn);
            int hourCount = increment(hourCounterPrefix + key, TimeSpan.FromHours(1));
            int dayCount = increment(dayCounterPrefix + key, TimeSpan.FromDays(1));

            // The daily limit wins when both are reached on the same hit, since its lockout is the longer one
            if (dayCount >= DailyCeiling)
            {
                TimeSpan lockout = TimeSpan.FromHours(Math.Max(1, DailyLockoutHours));
                if (start_lockout(dayLockoutPrefix + key, lockout, dayCounterPrefix + key, hourCounterPrefix + key))
                    RateLimitLog_Gateway.Budget_Lockout_Started(logEvent, SubnetKey, LoggedOn, "daily", DailyCeiling, unit, consequence, lockout);
            }
            else if (hourCount >= HourlyCeiling)
            {
                TimeSpan lockout = TimeSpan.FromMinutes(Math.Max(1, HourlyLockoutMinutes));
                if (start_lockout(hourLockoutPrefix + key, lockout, hourCounterPrefix + key))
                    RateLimitLog_Gateway.Budget_Lockout_Started(logEvent, SubnetKey, LoggedOn, "hourly", HourlyCeiling, unit, consequence, lockout);
            }
        }

        /// <summary> Claims a lockout atomically and, only for the caller that claims it, clears the counters that
        /// tripped it so counting starts fresh once it ends </summary>
        /// <returns> TRUE if this call started the lockout; FALSE if another request already had </returns>
        private static bool start_lockout(string LockoutKey, TimeSpan Duration, params string[] CountersToClear)
        {
            bool claimed = false;
            SharedCache.Instance.GetOrAdd(LockoutKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = Duration;
                claimed = true;
                return DateTime.UtcNow.Add(Duration);
            });

            if (!claimed)
                return false;

            foreach (string counterKey in CountersToClear)
                SharedCache.Instance.Remove(counterKey);

            return true;
        }

        /// <summary> Cache key suffix for one subnet and one logon status -- anonymous and logged-on hits never
        /// share a counter or a lockout </summary>
        private static string counter_key(string SubnetKey, bool LoggedOn)
        {
            return (LoggedOn ? "LOGGEDON|" : String.Empty) + SubnetKey;
        }

        private static int increment(string Key, TimeSpan Window)
        {
            Counter counter = (Counter)SharedCache.Instance.GetOrAdd(Key, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = Window;
                return new Counter();
            });

            return Interlocked.Increment(ref counter.Count);
        }
    }
}
