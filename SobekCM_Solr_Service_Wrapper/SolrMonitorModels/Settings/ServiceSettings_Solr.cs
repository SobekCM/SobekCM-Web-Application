namespace SolrServiceMonitor.Settings
{
    /// <summary> Settings related to the solr instance to be launched and monitored </summary>
    public class ServiceSettings_Solr
    {
        /// <summary> Additional parameters included when calling solr/lucene to start </summary>
        /// <value> By default, this is empty (or NULL) </value>
        /// <remarks> This can be used to add a verbose flag, or any other support during solr startup </remarks>
        public string AdditionalParameters { get; set; }

        /// <summary> Main directory for the solr/lucene instance to launch and monitor </summary>
        /// <remarks> This is a REQUIRED value </remarks>
        public string Directory { get; set; }

        /// <summary> Port for the solr/lucene server to use, to be specified during startup </summary>
        /// <value> By default, this value is port 8983 </value>
        /// <remarks> The solr/lucene default is 8983 as well.  If this is empty or set to 8983, the
        /// port is NOT specified when the solr/lucene instance is launched.   If this is 
        /// specified, this is passed to solr during startup using the -p command line argument. </remarks>
        public int Port { get; set; }

        /// <summary> Port used to send a stop message to a running instance of solr/lucene server, specified during startup </summary>
        /// <value> By default, this value is port 7983 </value>
        public int StopPort { get; set; }

        /// <summary> Secret key used to stop the solr/lucene instance, specified during startup </summary>
        /// <value> By default, this value is 'solrrocks' </value>
        public string StopKey { get; set; }

        /// <summary> Host name to specify to the solr/lucene server </summary>
        /// <value> By default, this value is not specified </value>
        /// <remarks> The solr/lucene default is 'localhost'.  If this is empty or set to localhost, 
        /// the host name is NOT specified when the solr/lucene instance is launched.  If this is 
        /// specified, this is passed to solr during startup using the -h command line argument. </remarks>
        public string HostName { get; set; }

        /// <summary> Sets the solr.solr.home system property where all the cores should be listed </summary>
        /// <value> By default, this value is not specified </value>
        /// <remarks> The solr/lucene default is 'server/solr'. If this is empty or set to server/solr, 
        /// the home property is NOT specified when the solr/lucene instance is launched.  If this is 
        /// specified, this is passed to solr during startup using the -s command line argument.  </remarks>
        public string Home { get; set; }
        
    }
}
