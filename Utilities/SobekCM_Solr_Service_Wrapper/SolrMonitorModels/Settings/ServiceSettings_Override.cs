using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolrServiceMonitor.Settings
{
    public class ServiceSettings_Override
    {
        public string Log4jConfig { get; internal set; }

        public string SolrHome { get; internal set; }

        public string SolrServerDir { get; internal set; }

        public string SolrLogsDir { get; internal set; }

    }
}
