namespace SolrServiceMonitor.Settings
{
    /// <summary> Settings related to logging events and exceptions through the Windows event log </summary>
    public class ServiceSettings_Logging
    {
        /// <summary> Windows event log EventID used when a log is added as this 
        /// service starts if solr/lucene also successfully started </summary>
        /// <value> By default, this is EventID 50800 </value>
        /// <remarks> If this property is set to -1, the start event will 
        /// not be registered in the event log </remarks>
        public int StartEventId { get; set; }

        /// <summary> Windows event log EventID used when a log is added just before 
        /// this service stops if solr/lucene also was succesfully stopped </summary>
        /// <value> By default, this is EventID 50801 </value>
        /// <remarks> If this property is set to -1, the stop event will not 
        /// be registered in the event log </remarks>
        public int StopEventId { get; set; }

        /// <summary> Windows event log EventID used when a log is added when the 
        /// service pings the solr/lucene process to ensure it is running </summary>
        /// <value> By default, this is EventID -1 so this is not logged in the event log 
        /// (recommended EventID is 50802 if used) </value>
        /// <remarks> If this property is set to -1, the ping event will 
        /// not be registered in the event log </remarks>
        public int PingEventId { get; set; }

        /// <summary> Windows event log EventID used when a log is added that the monitoring 
        /// service was used by an external process to check the health of the solr/lucene index </summary>
        /// <value> By default, this is EventID -1 so this is not logged in the event log 
        /// (recommended EventID is 50803 if used) </value>
        /// <remarks> If this property is set to -1, the health check event will not 
        /// be registered in the event log </remarks>
        public int HealthCheckEventId { get; set; }

        /// <summary> Windows event log EventID used when a log is added that the monitoring 
        /// port was successfully opened </summary>
        /// <value> By default, this is EventID -1 so this is not logged in the event log 
        /// (recommended EventID is 50804 if used) </value>
        /// <remarks> If this property is set to -1, no log entry will be written 
        /// when the monitoring port is opened </remarks>
        public int MonitorPortOpenEventId { get; set; }

        /// <summary> Windows event log EventID used when a log is added if the solr/lucene 
        /// engine fails to start when requested </summary>
        /// <value> By default, this is EventID 50805 </value>
        /// <remarks> If this property is set to -1, the failed start event will not be 
        /// registered in the event log (NOT RECOMMENDED) </remarks>
        public int FailedStartEventId { get; set; }

        /// <summary> Windows event log EventID used when a log is added just before this 
        /// service stops if solr/lucene failed to correctly stop upon request </summary>
        /// <value> By default, this is EventID 50806 </value>
        /// <remarks> If this property is set to -1, the failed stop event will not be
        ///  registered in the event log (NOT RECOMMENDED) </remarks>
        public int FailedStopEventId { get; set; }

        /// <summary> Windows event log EventID used when a log is added when a ping of 
        /// the solr/lucene process fails </summary>
        /// <value> By default, this is EventID 50807 </value>
        /// <remarks> If this property is set to -1, the failed ping event will not be 
        /// registered in the event log (NOT RECOMMENDED) </remarks>
        public int FailedPingEventId { get; set; }

        /// <summary> Windows event log EventID used when a request by an external process to 
        /// check the health of the solr/lucene index fails completely </summary>
        /// <value> By default, this is EventID 50808 </value>
        /// <remarks> If this property is set to -1, the failed health check event will not 
        /// be registered in the event log (NOT RECOMMENDED) </remarks>
        public int FailedHealthCheckEventId { get; set; }

        /// <summary> Windows event log EventID used when a log is added if the monitoring 
        /// port could not be opened for any reason, usually because some other process is 
        /// already listening on that port </summary>
        /// <value> By default, this is EventID 50809 </value>
        /// <remarks> If this property is set to -1, the failed port opening event will not be 
        /// registered in the event log (NOT RECOMMENDED) </remarks>
        public int FailedMonitorPortOpenEventId { get; set; }

        /// <summary> Windows event log EventID used for very detailed messages that would usually
        /// only be used for development or debugging.  Equivalent to a (very) verbose flag. </summary>
        /// <value> By default, this is EventID -1 so extra verbose messages are not logged in the event log 
        /// (recommended EventID is 50810 if used) </value>
        /// <remarks> If this property is set to -1, the verbose messages will not 
        /// be registered in the event log </remarks>
        public int VerboseEventId { get; set; }

        /// <summary> Windows event log EventID used for any other exception that is caught within
        /// the system and does not fall into any of the special errors caught and logged separately </summary>
        /// <value> By default, this is EventID 50811 </value>
        /// <remarks> If this property is set to -1, the failed port opening event will not be 
        /// registered in the event log (NOT RECOMMENDED) </remarks>
        public int ExceptionEventId { get; set; }
    }
}
