namespace SolrServiceMonitor.Settings
{
    /// <summary> Settings related to the Java Virtual Machine used by Solr </summary>
    public class ServiceSettings_Java
    {
        /// <summary> Additional paramters that are passed to the JVM by being added to the solr startup script </summary>
        /// <value> By default, this is empty (or NULL) </value>
        /// <remarks> If this is specified, this is passed to solr during startup using the -a command line argument.</remarks>
        public string AdditionalParameters { get; set; }

        /// <summary> Heap memory to allow the JVM to utilize in the solr work </summary>
        /// <value> By default, solr assigned a very small amount of memory, so it is recommended to increase this </value>
        /// <remarks> The values should be entered like '4g', or '512m'.  If this is 
        /// specified, this is passed to solr during startup using the -m command line argument.</remarks>
        public string HeapMemory { get; set; }

        /// <summary> Java home for the valid JRE / JDK directory </summary>
        public string Home { get; set; }

    }
}
