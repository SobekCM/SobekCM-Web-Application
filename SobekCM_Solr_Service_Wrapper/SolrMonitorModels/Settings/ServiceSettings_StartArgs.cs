using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolrServiceMonitor.Settings
{
    public class ServiceSettings_StartArgs
    {

        public string ServerOpts { get; internal set; }

        public string JavaMemory { get; internal set; }

        public string TimeZone { get; internal set; }

        public string GcTune { get; set; }

        public string GcLogOpts { get; internal set; }

        public string JettyConfig { get; internal set; }

    }
}
