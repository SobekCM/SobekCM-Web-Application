using SobekCM.Core.MemoryMgmt;
using System;
using System.IO;

namespace SobekCM.Core.RateLimiting
{
    /// <summary> Centralizes appends to temp/ratelimiting.txt, the one log every rate limiter writes to </summary>
    /// <remarks> One tab-separated line per event, so the file is easy to grep or open in a spreadsheet:
    /// local time, event, whether the request that tripped it was anonymous or logged on, its IP (burst ban)
    /// or subnet (everything else), details, and -- for the per-IP and per-subnet events -- the user agent of the
    /// request that tripped it. A header line is written when the file is first created.
    /// <para>Only the moment something trips is logged -- an IP being banned, a subnet reaching a budget
    /// ceiling, a site-wide fuse tripping -- never each request turned away afterwards, so a crawler that keeps
    /// hammering a closed door can't flood the file.</para>
    /// <para><b>Site-wide events carry no user agent</b> (see is_site_wide). Those trip on a total across every
    /// visitor, so the request that happens to cross the threshold is usually just whoever arrived at that moment --
    /// logging its user agent said nothing about the traffic behind the event and read as if that visitor were the
    /// culprit. Their address is that same incidental request's subnet, for the same reason.</para>
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

        /// <summary> Event name for identified robots being paused off item pages site-wide, the level below the
        /// login-only fuse </summary>
        public const string Event_Robot_Pause = "ROBOT PAUSE";

        /// <summary> Event name for a whole /16 (IPv4) or /32 (IPv6) range banned because several of its IPs were
        /// burst-banned at the same time </summary>
        public const string Event_Range_Ban = "RANGE BAN";

        /// <summary> Event name for a request refused outright because its user agent matched the hardcoded
        /// blocklist -- see <see cref="SobekCM.Core.RateLimiting.UserAgentBlocklist_Gateway"/> </summary>
        public const string Event_UserAgent_Ban = "USER-AGENT BAN";

        private const string Header = "# time\tevent\ttripped by\tIP or subnet\tdetails\tuser agent";

        private static readonly object writeLock = new object();

        /// <summary> Whether rate-limiting events are written at all. Set once at startup from appsettings.json's
        /// "RateLimiting:LoggingEnabled" (see RateLimitingMiddleware), and covers every limiter, not only the
        /// burst ban. Defaults to true. </summary>
        public static bool Enabled { get; set; } = true;

        /// <summary> Returns the user agent of the request currently being handled, or null outside a request. Set once
        /// at startup (see RateLimitingMiddleware), since SobekCM_Core has no access to the HttpContext itself. </summary>
        /// <remarks> Only the request that trips an event is logged, so this is that one request's user agent -- not
        /// necessarily representative of the traffic that led up to it. </remarks>
        public static Func<string> CurrentUserAgent { get; set; }

        /// <summary> Text for the "tripped by" column </summary>
        /// <param name="LoggedOn"> Whether the request that tripped the event was logged on </param>
        public static string Who(bool LoggedOn)
        {
            return LoggedOn ? "logged on" : "anonymous";
        }

        /// <summary> Records one event -- to the central monitoring database if <see cref="Monitoring_Gateway.Sink"/>
        /// is configured, otherwise (or if that fails) as a line in temp/ratelimiting.txt. Never throws. </summary>
        /// <param name="Event"> Which limiter tripped, one of the Event_ constants </param>
        /// <param name="LoggedOn"> Whether the request that tripped it was logged on </param>
        /// <param name="Address"> Exact IP for the burst ban, subnet key for everything else </param>
        /// <param name="Details"> What was reached and what happens now </param>
        public static void Append(string Event, bool LoggedOn, string Address, string Details)
        {
            if (!Enabled)
                return;

            DateTime occurred = DateTime.Now;

            // No user agent on a site-wide event: it would be whoever happened to cross the threshold, not the
            // traffic that caused it (see the class remarks)
            string userAgent = is_site_wide(Event) ? null : current_user_agent();

            try
            {
                IMonitoringSink sink = Monitoring_Gateway.Sink;
                if (sink != null)
                {
                    Monitoring_RateLimit_Record record = new Monitoring_RateLimit_Record
                    {
                        OccurredUtc = occurred.ToUniversalTime(),
                        EventName = Event,
                        LoggedOn = LoggedOn,
                        Address = Address,
                        Details = Details,
                        UserAgent = userAgent,
                        File_Fallback = () => append_to_file(occurred, Event, LoggedOn, Address, Details, userAgent)
                    };
                    if (sink.TryEnqueue(record))
                        return;
                }
            }
            catch (Exception)
            {
                // Fall through to the file below
            }

            append_to_file(occurred, Event, LoggedOn, Address, Details, userAgent);
        }

        /// <summary> The current request's user agent through <see cref="CurrentUserAgent"/>, or null. Never throws. </summary>
        private static string current_user_agent()
        {
            try
            {
                return CurrentUserAgent?.Invoke();
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary> Whether this event trips on a site-wide total rather than on one IP or subnet's own traffic </summary>
        private static bool is_site_wide(string Event)
        {
            return (Event == Event_Robot_Pause) || (Event == Event_Login_Only_Fuse) || (Event == Event_JP2_Circuit_Breaker);
        }

        /// <summary> Appends one event line to temp/ratelimiting.txt. Never throws. </summary>
        private static void append_to_file(DateTime Occurred, string Event, bool LoggedOn, string Address, string Details, string UserAgent)
        {
            try
            {
                // A user agent is whatever the client sent, so keep it from breaking the one-line, tab-separated format
                string cleanUserAgent = (UserAgent ?? String.Empty).Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');

                string logPath = Path.Combine(AppRoot_Gateway.AppRootPath, "temp", LogFile_Names.RateLimiting);
                string line = Occurred.ToString("yyyy-MM-dd HH:mm:ss") + "\t" + Event + "\t" + Who(LoggedOn) + "\t" +
                    (String.IsNullOrEmpty(Address) ? "unknown" : Address) + "\t" + Details + "\t" + cleanUserAgent + Environment.NewLine;

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
