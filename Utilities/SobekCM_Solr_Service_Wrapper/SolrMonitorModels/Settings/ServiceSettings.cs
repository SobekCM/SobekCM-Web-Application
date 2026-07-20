using System;
using System.Collections.Specialized;
using System.Configuration;

namespace SolrServiceMonitor.Settings
{
    public static class ServiceSettings
    {
        /// <summary> Static constructor for the ServiceSettings static class </summary>
        /// <remarks> This reads the application config file to load all the user settings </remarks>
        static ServiceSettings()
        {
            // Pull all the settings from the application configuration file
            NameValueCollection appSettings = ConfigurationManager.AppSettings;
            Name = appSettings["Name"] ?? "Solr Service Wrapper";

            // Get the java values
            Java = new ServiceSettings_Java
            {
                AdditionalParameters = appSettings["Java.AdditionalParameters"],
                HeapMemory = appSettings["Java.HeapMemory"],
                Home = appSettings["Java.Home"]
            };

            // Get the logging values
            Logging = new ServiceSettings_Logging
            {
                StartEventId = IntFromString(appSettings["Logging.StartEventID"], 50800),
                StopEventId = IntFromString(appSettings["Logging.StopEventID"], 50801),
                PingEventId = IntFromString(appSettings["Logging.PingEventID"], -1),
                HealthCheckEventId = IntFromString(appSettings["Logging.HealthCheckEventID"], -1),
                MonitorPortOpenEventId = IntFromString(appSettings["Logging.MonitorPortOpenEventID"], -1),
                FailedStartEventId = IntFromString(appSettings["Logging.FailedStartEventID"], 50805),
                FailedStopEventId = IntFromString(appSettings["Logging.FailedStopEventID"], 50806),
                FailedPingEventId = IntFromString(appSettings["Logging.FailedPingEventID"], 50807),
                FailedHealthCheckEventId = IntFromString(appSettings["Logging.FailedHealthCheckEventID"], 50808),
                FailedMonitorPortOpenEventId = IntFromString(appSettings["Logging.FailedMonitorPortOpenEventID"], 50809),
                VerboseEventId = IntFromString(appSettings["Logging.VerboseEventID"], -1),
                ExceptionEventId = IntFromString(appSettings["Logging.ExceptionEventID"], 50811)
            };

            // Get the monitor values
            Monitor = new ServiceSettings_Monitor
            {
                FailsBeforeAbort = IntFromString(appSettings["Monitor.FailsBeforeAbort"], 4),
                PauseDuration = IntFromString(appSettings["Monitor.HealthCheckUrl"], 60),
                Port = IntFromString(appSettings["Monitor.Port"], 8080)               
            };

            // Look for the specified cores list
            string cores = appSettings["Monitor.Cores"];
            if (!String.IsNullOrWhiteSpace(cores))
            {
                string[] coreSplitter = cores.Split("|".ToCharArray());
                foreach (string thisCore in coreSplitter)
                {
                    if (!String.IsNullOrWhiteSpace(thisCore))
                    {
                        string coreTrimmed = thisCore.Trim().ToLower();
                        if ( !ServiceSettings.Monitor.Cores.Contains(coreTrimmed))
                            ServiceSettings.Monitor.Cores.Add(coreTrimmed);
                    }
                }
            }

            // Get the solr startup values
            Solr = new ServiceSettings_Solr
            {
                AdditionalParameters = appSettings["Solr.AdditionalParameters"],
                Directory = appSettings["Solr.Directory"],
                Port = IntFromString(appSettings["Solr.Port"], 8983),
                StopPort = IntFromString(appSettings["Solr.StopPort"], 7983),
                StopKey = appSettings["Solr.StopKey"],
                HostName = appSettings["Solr.HostName"],
                Home = appSettings["Solr.Home"]
            };

            // Get the starting arguments
            StartArgs = new ServiceSettings_StartArgs
            {
                ServerOpts = appSettings["StartArgs.SERVER_OPTS"],
                JavaMemory = appSettings["StartArgs.SOLR_JAVA_MEM"],
                TimeZone = appSettings["StartArgs.SOLR_TIMEZONE"],
                GcTune = appSettings["StartArgs.GC_TUNE"],
                GcLogOpts = appSettings["StartArgs.GC_LOG_OPTS"],
                JettyConfig = appSettings["StartArgs.SOLR_JETTY_CONFIG"]
            };

            // Get the override arguments
            Override = new ServiceSettings_Override()
            {
                Log4jConfig = appSettings["Override.LOG4J_CONFIG"],
                SolrHome = appSettings["Override.SOLR_HOME"],
                SolrServerDir = appSettings["Override.SOLR_SERVER_DIR"],
                SolrLogsDir = appSettings["Override.SOLR_LOGS_DIR"]
            };


        }


        /// <summary> Name of this service, to be used for reporting purposes </summary>
        /// <value> Loaded from the 'Name' key in the application settings - defaults to 'Solr Service Wrapper' </value>
        public static string Name { get; internal set; }

        /// <summary> All settings related to the Java Virtual Machine, in relation to the solr/lucene instance </summary>
        /// <remarks> These values are loaded from the application settings during startup </remarks>
        public static ServiceSettings_Java Java { get; internal set; }

        /// <summary> All settings related to the logging in the Windows event log </summary>
        /// <remarks> These values are loaded from the application settings during startup </remarks>
        public static ServiceSettings_Logging Logging { get; internal set; }

        /// <summary> All settings related to the Monitoring service (possibly) exposed on an administrative port </summary>
        /// <remarks> These values are loaded from the application settings during startup </remarks>
        public static ServiceSettings_Monitor Monitor { get; internal set; }

        /// <summary> All settings related to the Solr, in particular variables used during startup </summary>
        /// <remarks> These values are loaded from the application settings during startup </remarks>
        public static ServiceSettings_Solr Solr { get; internal set; }


        /// <summary> Settings related to the start-up of Solr and Jetty </summary>
        /// <remarks> These values are loaded from the application settings during startup </remarks>
        public static ServiceSettings_StartArgs StartArgs { get; internal set; }

        /// <summary> Settings which can override settings that are otherwise calculated or determined via registry lookups </summary>
        /// <remarks> These values are loaded from the application settings during startup </remarks>
        public static ServiceSettings_Override Override { get; internal set; }


        /// <summary> Safely converts a possible null string to an integer, given a default value </summary>
        /// <param name="Value"> String (possibly null) to convert </param>
        /// <param name="DefaultValue">The default value </param>
        /// <returns> Integer equivalent for the string, or the default value </returns>
        private static int IntFromString(string Value, int DefaultValue )
        {
            // If empty or NULL, return the default value
            if ( String.IsNullOrWhiteSpace(Value))
                return DefaultValue;

            // Try to parse the value to an integer and return that, otherwise, just return the default value
            int testValue;
            return Int32.TryParse(Value, out testValue) ? testValue : DefaultValue;
        }
    }
}
