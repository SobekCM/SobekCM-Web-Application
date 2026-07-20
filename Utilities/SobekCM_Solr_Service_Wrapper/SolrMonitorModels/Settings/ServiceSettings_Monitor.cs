using System.Collections.Generic;

namespace SolrServiceMonitor.Settings
{
    /// <summary> Settings related to the monitoring service, both the internal monitoring 
    /// and external monitoring via the monitoring port (optional) </summary>
    public class ServiceSettings_Monitor
    {
        /// <summary> Constructor for a new instance of the ServiceSettings_Monitor class </summary>
        public ServiceSettings_Monitor()
        {
            Cores = new List<string>();
        }

        /// <summary> Collection of cores to monitor on the solr/lucene instance  (both by ping 
        /// and optionally by exposing the health check on the monitoring port ) </summary>
        /// <value> By default, this is empty - so all the cores will be discovered </value>
        /// <remarks> Multiple cores will be listed in the settings file with a pipe between them.  
        /// If a set is specified in the settings, then only those cores will be checked </remarks>
        public List<string> Cores { get; internal set; }

        /// <summary> Number of hard failures tolerated before the service terminates </summary>
        /// <value> By default, this value is four.  Setting this to -1 will result in the service never aborting.
        /// This will make it look like the service is running indefinitely, even if it is failing to ping
        /// and restart solr/lucene repeatedly. </value>
        public int FailsBeforeAbort { get; internal set; }

        /// <summary> Pause, in seconds, between successive pings of the solr/lucene system to 
        /// ensure the system is up and responding  </summary>
        /// <value> By default, this checks every 60 seconds </value>
        /// <remarks> If this is set to -1, then no automatic checks via the ping will occur </remarks>
        public int PauseDuration { get; set; }

        /// <summary> Monitoring port that will be opened to allow external queries for the 
        /// health of the cores, including a good amount of detailed information about each core</summary>
        /// <value> By default, the monitoring port is set to port 8984 </value>
        /// <remarks> If this is set to -1, the the monitoring port will not be opened and external
        /// healthcheck requests will not be supported </remarks>
        public int Port { get; set; }
    }
}
