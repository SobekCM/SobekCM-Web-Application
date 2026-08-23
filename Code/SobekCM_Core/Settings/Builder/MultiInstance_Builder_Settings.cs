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

        /// <summary> Checks whether the current local time has passed the configured Stop_Hour, meaning
        /// the builder should stop processing and exit cleanly. </summary>
        /// <returns> TRUE if processing should stop, otherwise FALSE </returns>
        /// <remarks> Replaces the old DB-flag-based abort mechanism (previously Abort_Database_Mechanism),
        /// which only made sense when a single instance was being processed - a flag stored in one
        /// instance's database has no clear meaning for a builder process servicing several instances at
        /// once. This check is a pure function of local config + the clock, so it's safe to call from
        /// anywhere regardless of which instance is currently being processed. </remarks>
        public static bool Past_Stop_Hour()
        {
            int stop_hour = Stop_Hour ?? DEFAULT_STOP_HOUR;
            if ((stop_hour < 0) || (stop_hour > 23))
                stop_hour = DEFAULT_STOP_HOUR;

            // 0 means never stop
            if (stop_hour == 0)
                return false;

            return DateTime.Now.Hour >= stop_hour;
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
