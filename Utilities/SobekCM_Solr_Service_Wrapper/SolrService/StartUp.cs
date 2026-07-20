using System.ServiceProcess;
using System.Threading;
using SolrServiceMonitor.Settings;
using SolrServiceMonitor.Solr;

namespace SolrServiceMonitor
{
    static class StartUp
    {
        /// <summary> The main entry point for the application.  </summary>
        static void Main()
        {
            //if (SolrWrapper.Start())
            //{
            //    // pause here
            //    Thread.Sleep(1);

            //    SolrWrapper.Stop();
            //}


            ServiceBase[] servicesToRun = { new SolrService() };
            ServiceBase.Run(servicesToRun);
        }
    }
}
