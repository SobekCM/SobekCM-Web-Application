using System;
using System.ServiceProcess;
using System.Threading;
using SolrMonitorModels.Java;
using SolrServiceMonitor.Settings;
using SolrServiceMonitor.Solr;
using SolrServiceMonitor.Utilities;

namespace SolrServiceMonitor
{
    partial class SolrService : ServiceBase
    {
        private Timer pingTimer;

        public SolrService()
        {
            InitializeComponent();

            this.ServiceName = "Solr Monitor Service";
            this.AutoLog = false;
        }

        protected override void OnStart(string[] args)
        {
            EventLogHelper.Write_Verbose("In SolrService.OnStart() method");

            // Check the JAVA version
            if (!JavaWrapper.Check_Java())
            {
                // Just abort, since the java wrapper logs any errors
                return;
            }

            // Try to start SOLR
            if (!SolrWrapper.Start_Direct(20000))
            {
                try
                {
                    EventLogHelper.Write_Verbose("Failed to start - exiting service with exit code of 1064");
                    ExitCode = 1064;
                    Stop();
                }
                catch (Exception ee)
                {
                    EventLogHelper.Write_Verbose("EXCEPTION caught while trying to exit service with exit code of 1064 due to failed solr/lucene start");
                }

                return;
            }

            // Try to ping the solr/lucene instance, just to ensure it is working
            try
            {
                if (SolrWrapper.Ping() == "OK")
                {
                    // Log the successful ping
                    EventLogHelper.Write_Event(ServiceSettings.Logging.PingEventId, "Successfully pinged solr/lucene for routine monitoring");
                }
                else
                {
                    // Write the error message
                    EventLogHelper.Write_Error(ServiceSettings.Logging.FailedPingEventId, "Error while pinging solr/lucene for routine monitoring");

                    // During startup, terminate on the first failed ping
                    ExitCode = 1064;
                    Stop();
                }
            }
            catch (Exception ex)
            {
                EventLogHelper.Write_Error(ServiceSettings.Logging.FailedPingEventId, "EXCEPTION CAUGHT while pinging solr/lucene for routine monitoring", ex);
                // During startup, terminate on the first failed ping
                ExitCode = 1064;
                Stop();
            }

            // Since Solr started, start up the timer
            if (ServiceSettings.Monitor.PauseDuration > 0)
            {
                EventLogHelper.Write_Verbose("Starting the ping timer in SolrService.OnStart() with an internval of " + ServiceSettings.Monitor.PauseDuration + " seconds.");

                // Tell the timer to fire again in XX milliseconds, and only fire that once
                pingTimer = new Timer(pingTimerExpired, null, ServiceSettings.Monitor.PauseDuration * 1000, Timeout.Infinite);
            }
        }

        protected override void OnStop()
        {
            EventLogHelper.Write_Verbose("In SolrService.OnStop() method");

            // Stop the ping timer
            if (pingTimer != null)
                pingTimer.Change(Timeout.Infinite, Timeout.Infinite);

            // Stop solr/lucene
            if (!SolrWrapper.Stop())
            {
                try
                {
                    EventLogHelper.Write_Verbose("Failed to stop solr/lucene on requested service stop - exiting service with exit code of 1064");
                    ExitCode = 1064;
                }
                catch (Exception ee)
                {
                    EventLogHelper.Write_Verbose("EXCEPTION caught while trying to exit service with exit code of 1064 due to failed solr/lucene stop on requested service stop");
                }
            }
        }

        private void pingTimerExpired(object State)
        {
            try
            {
                if (SolrWrapper.Ping() == "OK")
                {
                    // Log the successful ping
                    EventLogHelper.Write_Event(ServiceSettings.Logging.PingEventId, "Successfully pinged solr/lucene for routine monitoring");

                    // Tell the timer to fire again in XX milliseconds, and only fire that once
                    pingTimer.Change(ServiceSettings.Monitor.PauseDuration * 1000, Timeout.Infinite);
                }
                else
                {
                    // Write the error message
                    EventLogHelper.Write_Error(ServiceSettings.Logging.FailedPingEventId, "Error while pinging solr/lucene for routine monitoring");

                    // For now, terminate on the first failed ping
                    ExitCode = 1064;
                    Stop();
                }
            }
            catch (Exception ex)
            {
                EventLogHelper.Write_Error(ServiceSettings.Logging.FailedPingEventId, "EXCEPTION CAUGHT while pinging solr/lucene for routine monitoring", ex );
            }
        }
    }
}
