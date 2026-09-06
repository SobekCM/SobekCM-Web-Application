using System;
using System.Collections.Generic;

namespace SobekCM.Builder_Library.Settings
{
    /// <summary> Basic setting and configuration information used by the builder
    /// across all instances that it may be processing </summary>
    public static class MultiInstance_Builder_Settings
    {
        /// <summary> Fallback used when Stop_Hour is not configured or out of the valid 0-23 range </summary>
        private const int DEFAULT_STOP_HOUR = 23;

        /// <summary> Static constructor for the MultiInstance_Builder_Settings class </summary>
        static MultiInstance_Builder_Settings()
        {
            Instance_Package_Limit = -1;
            Instances = new List<Single_Instance_Configuration>();
        }

        /// <summary> Directory in which the builder is currently running </summary>
        public static string Builder_Executable_Directory { get; set; }

        /// <summary> Maximum number of packages to process for each instance, before moving onto the 
        /// instance  </summary>
        /// <remarks> -1 is the default value and indicates no limit </remarks>
        public static int Instance_Package_Limit { get; set; }

        /// <summary> ImageMagick executable file </summary>
        public static string ImageMagick_Executable { get; set; }

        /// <summary> Ghostscript executable file </summary>
        public static string Ghostscript_Executable { get; set; }

        /// <summary> Tesseract executable file </summary>
        public static string Tesseract_Executable { get; set; }

        /// <summary> LibreOffice executable file, used to convert Word/PowerPoint files to PDF </summary>
        public static string LibreOffice_Executable { get; set; }

        /// <summary> Hour of the day (0-23, local time) after which the builder stops polling for new
        /// work and exits, rather than continuing to poll indefinitely. 0 means never stop (poll
        /// indefinitely until stopped externally, e.g. the machine itself being powered off). NULL
        /// means not configured - falls back to the historical default of 11pm. </summary>
        /// <remarks> Lives in the config file (not the per-instance SobekCM_Settings DB table) because a
        /// single builder process can service multiple instances in one run - a stop hour tied to one
        /// instance's database wouldn't make sense for the process as a whole. Revisit once separate
        /// instances get merged into one shared database (targeted for 6.0). </remarks>
        public static int? Stop_Hour { get; set; }

        /// <summary> The next actual date/time (in TimeZone if configured, else machine-local - see
        /// Current_Time()) at which the builder should stop, computed from Stop_Hour by
        /// Recalculate_Next_Stop_Time(). NULL means never stop (Stop_Hour is 0). </summary>
        /// <remarks> Stop_Hour alone is just an hour-of-day, so comparing it directly against the current
        /// hour (the old behavior) isn't date-aware: for stop hours in the early morning (e.g. 1am), the
        /// simple "Hour >= Stop_Hour" check is already true late in the prior evening. Precomputing the
        /// next stop DateTime avoids that - see Recalculate_Next_Stop_Time(). </remarks>
        public static DateTime? Next_Stop_Time { get; private set; }

        /// <summary> Recomputes Next_Stop_Time from the current Stop_Hour (and Current_Time()). Must be called
        /// once after Stop_Hour (and, if configured, TimeZone) are set - both on the initial config read and
        /// again any time the config file is re-read mid-run, since Stop_Hour may have changed. </summary>
        /// <remarks> If the configured hour has already passed today, the next stop is tomorrow at that hour;
        /// otherwise it's still today. This is why a fresh config read - even mid-run - can legitimately move
        /// the stop time earlier (e.g. re-reading at 5:45pm with a new Stop_Hour of 20 keeps today at 8pm,
        /// since that's still in the future) or later (e.g. Stop_Hour of 16 read at 5:45pm computes tomorrow
        /// at 4pm, since today's 4pm has already passed). </remarks>
        public static void Recalculate_Next_Stop_Time()
        {
            int stop_hour = Stop_Hour ?? DEFAULT_STOP_HOUR;
            if ((stop_hour < 0) || (stop_hour > 23))
                stop_hour = DEFAULT_STOP_HOUR;

            // 0 means never stop
            if (stop_hour == 0)
            {
                Next_Stop_Time = null;
                return;
            }

            DateTime now = Current_Time();
            DateTime next_stop = new DateTime(now.Year, now.Month, now.Day, stop_hour, 0, 0);
            if (next_stop <= now)
                next_stop = next_stop.AddDays(1);

            Next_Stop_Time = next_stop;
        }

        private static string configuredTimeZone;
        private static TimeZoneInfo resolvedTimeZone;
        private static bool timeZoneResolved;

        /// <summary> IANA or Windows time zone identifier (e.g. "America/New_York") that Current_Time() (and
        /// so Stop_Hour, and anything else timestamped through Current_Time() - e.g. the "Builder Last Run
        /// Finished" status written to the database) should be reported in. NULL/not configured means the
        /// machine's own local time is used (the historical behavior) - set this when the machine's OS clock
        /// doesn't track the timezone you actually mean, e.g. a UTC-clocked VM whose shutdown schedule is
        /// pinned to Eastern wall-clock time (which itself shifts against UTC across DST) rather than a fixed
        /// UTC hour, or whose status timestamps you'd rather read in Eastern than raw UTC. </summary>
        public static string TimeZone
        {
            get { return configuredTimeZone; }
            set
            {
                configuredTimeZone = value;
                resolvedTimeZone = null;
                timeZoneResolved = false;
            }
        }

        /// <summary> Current time, in TimeZone if configured, otherwise the machine's own local time (the
        /// historical behavior). Use this instead of DateTime.Now anywhere the result is a status/display
        /// value (e.g. a timestamp written to the database for an admin page) - NOT for internal bookkeeping
        /// that needs to track the actual machine clock, like file/log naming or config-file change detection,
        /// which should keep using DateTime.Now directly. </summary>
        public static DateTime Current_Time()
        {
            if (String.IsNullOrEmpty(configuredTimeZone))
                return DateTime.Now;

            if (!timeZoneResolved)
            {
                timeZoneResolved = true;
                try
                {
                    resolvedTimeZone = TimeZoneInfo.FindSystemTimeZoneById(configuredTimeZone);
                }
                catch (Exception)
                {
                    // Leave resolvedTimeZone null - fall back to machine-local time below rather than
                    // throwing on every call because of a config typo.
                    resolvedTimeZone = null;
                }
            }

            return resolvedTimeZone == null ? DateTime.Now : TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, resolvedTimeZone);
        }

        /// <summary> Checks whether the current time (see Current_Time()) has passed Next_Stop_Time, meaning
        /// the builder should stop processing and exit cleanly. </summary>
        /// <returns> TRUE if processing should stop, otherwise FALSE </returns>
        /// <remarks> Replaces the old DB-flag-based abort mechanism (previously Abort_Database_Mechanism),
        /// which only made sense when a single instance was being processed - a flag stored in one
        /// instance's database has no clear meaning for a builder process servicing several instances at
        /// once. This check is a pure function of local config + the clock, so it's safe to call from
        /// anywhere regardless of which instance is currently being processed. If Recalculate_Next_Stop_Time()
        /// hasn't been called yet (Next_Stop_Time is still null), this returns FALSE rather than treating
        /// "not yet computed" as "never stop" being confused with "already past" - the reader calls it once
        /// up front, so in practice this only matters before the first config read completes. </remarks>
        public static bool Past_Stop_Hour()
        {
            if (Next_Stop_Time == null)
                return false;

            return Current_Time() >= Next_Stop_Time.Value;
        }

        /// <summary> List of all the SobekCM instances supported by this builder </summary>
        public static List<Single_Instance_Configuration> Instances { get; set; }

        /// <summary> List of any reading errors which may have occurred </summary>
        public static List<string> ReadingError { get; set; }

        /// <summary> Number of seconds between polls, from the configuration file (not the database) </summary>
        /// <remarks> This is used if the SobekCM Builder is working between multiple instances. If the SobekCM
        /// Builder is only servicing a single instance, then the data can be pulled from the database. </remarks>
        public static int? Override_Seconds_Between_Polls { get; set; }

        /// <summary> Add information about a new error encountered while reading the config file </summary>
        /// <param name="Error"> Error to log in this settings objects </param>
        public static void Add_Error(string Error)
        {
            if (ReadingError == null) ReadingError = new List<string>();

            ReadingError.Add(Error);
        }

        /// <summary> Clear the collections </summary>
        public static void Clear()
        {
            if (ReadingError != null) ReadingError.Clear();
            ReadingError = null;

            Instances.Clear();
        }

    }
}
