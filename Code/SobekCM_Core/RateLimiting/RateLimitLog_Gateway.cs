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

        /// <summary> Logs a subnet budget window that this hit has just brought up to one of its ceilings </summary>
        /// <param name="Event"> Which budget, <see cref="Event_JP2_Budget"/> or <see cref="Event_Item_View_Budget"/> </param>
        /// <param name="SubnetKey"> Subnet the shared counter belongs to </param>
        /// <param name="LoggedOn"> Whether the request that reached the ceiling was logged on </param>
        /// <param name="Window"> "hourly" or "daily" </param>
        /// <param name="Count"> The window's count, including this hit </param>
        /// <param name="AnonymousCeiling"> The window's ceiling for anonymous requests </param>
        /// <param name="LoggedOnCeiling"> The window's ceiling for logged-on requests </param>
        /// <param name="Unit"> What's being counted, e.g. "item views" </param>
        /// <param name="Consequence"> What happens from now on, e.g. "item pages blocked" </param>
        /// <remarks> Anonymous and logged-on requests share one counter per subnet with two ceilings, so each
        /// ceiling is logged on its own, when the count lands on it exactly. The counter only ever goes up by
        /// one, so that happens once per window. </remarks>
        public static void Budget_Ceiling_Reached(string Event, string SubnetKey, bool LoggedOn, string Window, int Count, int AnonymousCeiling, int LoggedOnCeiling, string Unit, string Consequence)
        {
            if (Count == AnonymousCeiling)
                Append(Event, LoggedOn, SubnetKey, Window + " anonymous limit of " + AnonymousCeiling + " " + Unit + " reached -- " + Consequence + " for anonymous visitors from this subnet until the " + Window + " window resets");

            if ((Count == LoggedOnCeiling) && (LoggedOnCeiling != AnonymousCeiling))
                Append(Event, LoggedOn, SubnetKey, Window + " logged-on limit of " + LoggedOnCeiling + " " + Unit + " reached -- " + Consequence + " for logged-on visitors from this subnet until the " + Window + " window resets");
        }
    }
}
