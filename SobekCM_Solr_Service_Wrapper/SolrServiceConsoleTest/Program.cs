using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using SolrMonitorModels.Java;
using SolrServiceMonitor.Settings;
using SolrServiceMonitor.Solr;

namespace SolrServiceConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            // Perform all the checks against JAVA
            if (!JavaWrapper.Check_Java())
            {
                // Just abort, since the java wrapper logs any errors
                return;
            }

            // See if Solr is already running, by trying to open a connection to the port
            if (is_port_in_use(ServiceSettings.Solr.Port))
            {
                Console.WriteLine("POrt is in use");
            }
            else
            {
                Console.WriteLine("Port is NOT in use");
            }

            


            Console.WriteLine("In SolrService.OnStart() method");

            // Try to start SOLR
            if (!SolrWrapper.Start_Direct(20000))
            {
                try
                {
                    Console.WriteLine("Failed to start - exiting service with exit code of 1064");
                    Console.WriteLine("Stopping... press any key to acknowledge");
                    Console.ReadKey();
                }
                catch (Exception ee)
                {
                    Console.WriteLine("EXCEPTION caught while trying to exit service with exit code of 1064 due to failed solr/lucene start");
                }

                return;
            }



            if (is_port_in_use(ServiceSettings.Solr.Port))
            {
                Console.WriteLine("Port is in use");
            }
            else
            {
                Console.WriteLine("Port is NOT in use");
            }

            // Try to ping the solr/lucene instance, just to ensure it is working
            try
            {
                if (SolrWrapper.Ping() == "OK")
                {
                    // Log the successful ping
                    Console.WriteLine("Successfully pinged solr/lucene for routine monitoring");
                }
                else
                {
                    // Write the error message
                    Console.WriteLine("Error while pinging solr/lucene for routine monitoring");

                    // During startup, terminate on the first failed ping
                    Console.WriteLine("Stopping... press any key to acknowledge");
                    Console.ReadKey();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("EXCEPTION CAUGHT while pinging solr/lucene for routine monitoring", ex);
                // During startup, terminate on the first failed ping
                Console.WriteLine("Stopping... press any key to acknowledge");
                Console.ReadKey();
            }


            if (is_port_in_use(ServiceSettings.Solr.Port))
            {
                Console.WriteLine("POrt is in use");
            }
            else
            {
                Console.WriteLine("Port is NOT in use");
            }

            Console.WriteLine("Stopping solr...");
            SolrWrapper.Stop();


            if (is_port_in_use(ServiceSettings.Solr.Port))
            {
                Console.WriteLine("POrt is in use");
            }
            else
            {
                Console.WriteLine("Port is NOT in use");
            }

            Console.ReadKey();
        }

        private static bool is_port_in_use(int Port)
        {
            using (TcpClient tcpClient = new TcpClient())
            {
                try
                {
                    tcpClient.Connect("127.0.0.1", Port);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }
    }
}
