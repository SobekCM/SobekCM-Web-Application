using SobekCM.Core.MemoryMgmt;
using System;
using System.IO;

namespace SobekCM.Core.RateLimiting
{
    /// <summary> Centralizes appends to temp/ratelimiting.txt, the one log every rate limiter writes to </summary>
    /// <remarks> One tab-separated line per event, so the file is easy to grep or open in a spreadsheet:
    /// local time, event, whether the request that tripped it was anonymous or logged on, its IP (burst ban)
    /// or subnet (everything else), and details. A header line is written when the file is first created.
    /// <para>Only the moment something trips is logged -- an IP being banned, a subnet reaching a budget
    /// ceiling, a site-wide fuse tripping -- never each request turned away afterwards, so a crawler that keeps
    /// hammering a closed door can't flood the file. For a site-wide fuse, the address is the subnet of the
    /// request that happened to cross the threshold, not a culprit.</para>
    /// <para>Serialized with its own lock for the same reason as <see cref="ExceptionLog_Gateway"/>. Never throws.</para> </remarks>
    public static class RateLimitLog_Gateway
    {
        /// <summary> Event name for an exact IP banned by the burst limiter </summary>
        public const string Event_Burst_Ban = "BURST BAN";

        /// <summary> Event name for a subnet reaching a JP2 zoom budget ceiling </summary>
        public const string Event_JP2_Budget = "JP2 ZOOM BUDGET";

        /// <summary> Event name for the automatic site-wide JP2 circuit breaker tripping </summary>
        public const string Event_JP2_Circuit_Breaker = "JP2 CIRCUIT BREAKER";

        /// <summary> Event name for a subnet reaching a sustained item-view budget ceiling </summary>
        public const string Event_Item_View_Budget = "ITEM VIEW BUDGET";

        /// <summary> Event name for the automatic site-wide login-only fuse tripping </summary>
        public const string Event_Login_Only_Fuse = "LOGIN-ONLY FUSE";

        /// <summary> Event name for a whole /16 (IPv4) or /32 (IPv6) range banned because several of its IPs were
        /// burst-banned at the same time </summary>
        public const string Event_Range_Ban = "RANGE BAN";

        private const string Header = "# time\tevent\ttripped by\tIP or subnet\tdetails";

        private static readonly object writeLock = new object();

        /// <summary> Whether rate-limiting events are written at all. Set once at startup from appsettings.json's
        /// "RateLimiting:LoggingEnabled" (see RateLimitingMiddleware), and covers every limiter, not only the
        /// burst ban. Defaults to true. </summary>
        public static bool Enabled { get; set; } = true;

        /// <summary> Text for the "tripped by" column </summary>
        /// <param name="LoggedOn"> Whether the request that tripped the event was logged on </param>
        public static string Who(bool LoggedOn)
        {
            return LoggedOn ? "logged on" : "anonymous";
        }

        /// <summary> Appends one event line to temp/ratelimiting.txt. Never throws. </summary>
        /// <param name="Event"> Which limiter tripped, one of the Event_ constants </param>
        /// <param name="LoggedOn"> Whether the request that tripped it was logged on </param>
        /// <param name="Address"> Exact IP for the burst ban, subnet key for everything else </param>
        /// <param name="Details"> What was reached and what happens now </param>
        public static void Append(string Event, bool LoggedOn, string Address, string Details)
        {
            if (!Enabled)
                return;

            try
            {
                string logPath = Path.Combine(AppRoot_Gateway.AppRootPath, "temp", LogFile_Names.RateLimiting);
                string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\t" + Event + "\t" + Who(LoggedOn) + "\t" +
                    (String.IsNullOrEmpty(Address) ? "unknown" : Address) + "\t" + Details + Environment.NewLine;

                lock (writeLock)
                {
                    if (!File.Exists(logPath))
                        File.AppendAllText(logPath, Header + Environment.NewLine);

                    File.AppendAllText(logPath, line);
                }
            }
            catch (Exception)
            {
                // Best-effort logging -- nothing else to do if this itself fails.
            }
        }

        /// <summary> Logs a subnet budget lockout that has just started because a limit was reached </summary>
        /// <param name="Event"> Which budget, <see cref="Event_JP2_Budget"/> or <see cref="Event_Item_View_Budget"/> </param>
        /// <param name="SubnetKey"> Subnet that's now locked out </param>
        /// <param name="LoggedOn"> Whether it's the subnet's logged-on requests that are locked out -- which is also
        /// whether the request that reached the limit was logged on </param>
        /// <param name="Window"> "hourly" or "daily" </param>
        /// <param name="Ceiling"> The limit that was reached </param>
        /// <param name="Unit"> What's being counted, e.g. "item views" </param>
        /// <param name="Consequence"> What the lockout means, e.g. "item pages blocked" </param>
        /// <param name="Lockout"> How long the lockout lasts, from now </param>
        /// <remarks> Called only by the request that actually claimed the lockout (see SubnetBudget), so each
        /// lockout is logged once. </remarks>
        public static void Budget_Lockout_Started(string Event, string SubnetKey, bool LoggedOn, string Window, int Ceiling, string Unit, string Consequence, TimeSpan Lockout)
        {
            string group = LoggedOn ? "logged-on" : "anonymous";
            string otherGroup = LoggedOn ? "anonymous" : "logged-on";
            Append(Event, LoggedOn, SubnetKey, Window + " " + group + " limit of " + Ceiling + " " + Unit + " reached -- " + Consequence +
                " for " + group + " visitors from this subnet for " + describe_duration(Lockout) + " (" + otherGroup + " visitors are counted separately)");
        }

        /// <summary> Short text for a lockout length, e.g. "60 minutes" or "24 hours" </summary>
        private static string describe_duration(TimeSpan Duration)
        {
            if ((Duration.TotalHours >= 2) && (Duration.TotalMinutes % 60 == 0))
                return (int)Duration.TotalHours + " hours";

            return (int)Duration.TotalMinutes + " minutes";
        }
    }
}
