using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Xml;
using SolrMonitorModels.Java;
using SolrServiceMonitor.Settings;
using SolrServiceMonitor.Utilities;

namespace SolrServiceMonitor.Solr
{
    public class SolrWrapper
    {
        private const string SUCCESS_MESSAGE_TEMPLATE =
            "{0}\n\nEXECUTED: {1}\n\n-----------------------\n\nSTANDARD OUTPUT:\n\n{2}";

        private const string ERROR_MESSAGE_TEMPLATE =
            "{0}\n\nEXECUTED: {1}\n\n-----------------------\n\nSTANDARD OUTPUT:\n\n{2}\n-----------------------\n\nSTANDARD ERROR:\n\n{3}";

        private static string solrExecutable;

        static SolrWrapper()
        {
            solrExecutable = Path.Combine(ServiceSettings.Solr.Directory, "bin", "solr.cmd");
        }

        public static string Ping()
        {
            // Add verbose log
            EventLogHelper.Write_Verbose("Inside SolrWrapper.Ping() method");

            // Need to have a core to monitor/ping
            if ((ServiceSettings.Monitor.Cores == null) || (ServiceSettings.Monitor.Cores.Count == 0))
            {
                EventLogHelper.Write_Error(ServiceSettings.Logging.FailedPingEventId, "No core found to monitor, so no core to ping.\n\nUpdate the app.config Monitor.Cores value.");
                return "Service Configuration Error";
            }

            // Get the ping information
            string ping_url = "http://localhost:" + ServiceSettings.Solr.Port + "/solr/" + ServiceSettings.Monitor.Cores[0] + "/admin/ping?wt=xml";
            string response = Get_Solr_Web_Response(ping_url, true);

            // If there was an error, just return an empty string for the new status
            if (String.IsNullOrEmpty(response))
            {
                return String.Empty;
            }


            // Try to read the response into a XML document
            try
            {
                var xml = new XmlDocument();
                xml.LoadXml(response);

                EventLogHelper.Write_Verbose("Successfully converted the response into a XML document, apparently");




                string status = xml.DocumentElement.SelectSingleNode("/response/str[@name='status']").InnerText;

                EventLogHelper.Write_Verbose("Parsed the XML and received a status of '" + status + "'");
                return status;
            }
            catch (Exception ee)
            {
                EventLogHelper.Write_Error(ServiceSettings.Logging.ExceptionEventId,
                    "EXCEPTION caught in SolrWrapper.Ping() conversting response to a XML document and reading the status.\n\n--------------------\n\nRESPONSE:\n\n" +
                    response, ee);
                return null;
            }
        }

        public void HealthCheck()
        {

        }

        public static bool Start_Direct(int TimeOut)
        {

            // Add verbose log
            EventLogHelper.Write_Verbose("Inside SolrWrapper.Start_Direct() method");

            // Log this start
            StringBuilder logStartBuilder = new StringBuilder();


            // Check to see if the solr home directory exists
            string solr_top = ServiceSettings.Solr.Directory;
            if (!Directory.Exists(solr_top))
            {
                EventLogHelper.Write_Error( ServiceSettings.Logging.FailedStartEventId, "Solr directory " + solr_top + " not found!" + Environment.NewLine + Environment.NewLine + "Install solr/lucene and add the solr server directory under Solr.Directory to the configuration file for this service.");
                return false;
            }

            // Get and test the solr server directory
            string solr_server_dir = Path.Combine(ServiceSettings.Solr.Directory, "server");
            if (!String.IsNullOrEmpty(ServiceSettings.Override.SolrServerDir))
                solr_server_dir = ServiceSettings.Override.SolrServerDir;
            if (!Directory.Exists(solr_server_dir))
            {
                EventLogHelper.Write_Error(ServiceSettings.Logging.FailedStartEventId, "Solr server directory " + solr_server_dir + " not found!" + Environment.NewLine + Environment.NewLine + "Re-install solr/lucene and add the solr server directory under Solr.Directory to the configuration file for this service." + Environment.NewLine + Environment.NewLine + "If your setup is non-standard, you can use the Override.SOLR_SERVER_DIR value in the configuration file for this service.");
                return false;
            }

            // Get and test the solr home directory
            string solr_home = Path.Combine(ServiceSettings.Solr.Directory, "server", "solr");
            if (!String.IsNullOrEmpty(ServiceSettings.Override.SolrHome))
                solr_home = ServiceSettings.Override.SolrHome;
            if (!Directory.Exists(solr_home))
            {
                EventLogHelper.Write_Error(ServiceSettings.Logging.FailedStartEventId, "Solr home directory " + solr_home + " not found!" + Environment.NewLine + Environment.NewLine + "Re-install solr/lucene and add the solr server directory under Solr.Directory to the configuration file for this service." + Environment.NewLine + Environment.NewLine + "If your setup is non-standard, you can use the Override.SOLR_HOME value in the configuration file for this service.");
                return false;
            }

            // Backup log files ( use current timestamp for backup names )
            string logs_directory = Path.Combine(ServiceSettings.Solr.Directory, "server", "logs");
            if (!String.IsNullOrEmpty(ServiceSettings.Override.SolrLogsDir))
                logs_directory = ServiceSettings.Override.SolrLogsDir;


            // Only continue if log directory exists
            if (Directory.Exists(logs_directory))
            {
                string now_ts = DateTime.Now.Year + "-" + DateTime.Now.Month.ToString().PadLeft(2, '0') + "-" +
                                DateTime.Now.Day.ToString().PadLeft(2, '0') + "_" +
                                DateTime.Now.Hour.ToString().PadLeft(2, '0') +
                                DateTime.Now.Minute.ToString().PadLeft(2, '0');

                // Backing up the solr.log
                string solr_log = Path.Combine(logs_directory, "solr.log");
                if (File.Exists(solr_log))
                {
                    logStartBuilder.AppendLine("Backing up " + solr_log);
                    try
                    {
                        string new_solr_log = Path.Combine(logs_directory, "solr_log_" + now_ts + ".bak");
                        if (File.Exists(new_solr_log))
                        {
                            char append = 'a';
                            while ((append != 'z') &&
                                   (File.Exists(Path.Combine(logs_directory, "solr_log_" + now_ts + append + ".bak"))))
                            {
                                append = (char) (((int) append) + 1);
                            }
                            new_solr_log = Path.Combine(logs_directory, "solr_log_" + now_ts + append + ".bak");
                        }
                        File.Move(solr_log, new_solr_log);
                    }
                    catch (Exception ee)
                    {
                        logStartBuilder.AppendLine("Error backing up log file: " + ee.Message);
                    }
                }

                // Backing up the garbage collection log
                string solr_gc_log = Path.Combine(logs_directory, "solr_gc.log");
                if (File.Exists(solr_gc_log))
                {
                    logStartBuilder.AppendLine("Backing up " + solr_gc_log);
                    try
                    {
                        string new_solr_gc_log = Path.Combine(logs_directory, "solr_gc_log_" + now_ts + ".bak");
                        if (File.Exists(new_solr_gc_log))
                        {
                            char append = 'a';
                            while ((append != 'z') &&
                                   (File.Exists(Path.Combine(logs_directory, "solr_gc_log_" + now_ts + append + ".bak"))))
                            {
                                append = (char) (((int) append) + 1);
                            }
                            new_solr_gc_log = Path.Combine(logs_directory, "solr_gc_log_" + now_ts + append + ".bak");
                        }
                        File.Move(solr_gc_log, new_solr_gc_log);
                    }
                    catch (Exception ee)
                    {
                        logStartBuilder.AppendLine("Error backing up garbage collection log file: " + ee.Message);
                    }
                }
            }
            else
            {
                // log directory does not exist, try to create it
                try
                {
                    Directory.CreateDirectory(logs_directory);
                }
                catch (Exception ee)
                {
                    EventLogHelper.Write_Error(ServiceSettings.Logging.FailedStartEventId, "Logging directory " + ServiceSettings.Solr.Directory + " not found and the request to create the directory failed!", ee);
                    return false;
                }
            }

            // Check to see if the port is currently in use?
            if (is_port_in_use(ServiceSettings.Solr.Port))
            {
                EventLogHelper.Write_Event(ServiceSettings.Logging.FailedStartEventId, "Solr port (" + ServiceSettings.Solr.Port + ") is currently in use." + Environment.NewLine + Environment.NewLine + "Will attemp to restart solr by first issuing a stop command.");

                // Issue the stop
                Stop();

                // Now, double check that it is still not in use
                if (is_port_in_use(ServiceSettings.Solr.Port))
                {
                    EventLogHelper.Write_Event(ServiceSettings.Logging.FailedStartEventId, "A process is already listening on port " + ServiceSettings.Solr.Port + ".  If this is not Solr, then please choose a different port in the service configuration file.");
                    return false;
                }
            }

            // Set some initial values
            Exception CaughtException = null;

            // Determine some of the directory values that are derivaties of the solr home
            string log4j_props = Path.Combine(ServiceSettings.Solr.Directory, "server", "resources", "log4j.properties");
            if (!String.IsNullOrEmpty(ServiceSettings.Override.Log4jConfig))
                log4j_props = ServiceSettings.Override.Log4jConfig;

            string java_tempdir = Path.Combine(solr_server_dir, "tmp");
          //  string gc_log = Path.Combine(logs_directory, "solr_gc.log");
         //   string jar_file = Path.Combine(solr_server_dir, "start.jar");

            // Build the arguments
            StringBuilder argsBuilder = new StringBuilder();

            argsBuilder.Append(ServiceSettings.StartArgs.ServerOpts);
            argsBuilder.Append(" -Xss256k");
            argsBuilder.Append(" " + ServiceSettings.StartArgs.JavaMemory);
            argsBuilder.Append(" -Duser.timezone=" + ServiceSettings.StartArgs.TimeZone);
            argsBuilder.Append(" " + ServiceSettings.StartArgs.GcTune);
            argsBuilder.Append(" " + ServiceSettings.StartArgs.GcLogOpts);
            argsBuilder.Append(" -Xloggc:\"" + logs_directory + "\"/solr_gc.log");
            argsBuilder.Append(" -Dlog4j.configuration=\"file:" + log4j_props + "\"");
            argsBuilder.Append(" -DSTOP.PORT=" + ServiceSettings.Solr.StopPort);
            argsBuilder.Append(" -DSTOP.KEY=" + ServiceSettings.Solr.StopKey);
            argsBuilder.Append(" -Djetty.port=" + ServiceSettings.Solr.Port);
            argsBuilder.Append(" -Dsolr.solr.home=\"" + solr_home + "\"");
            argsBuilder.Append(" -Dsolr.install.dir=\"" + solr_top + "\"");
            argsBuilder.Append(" -DJetty.home=\"" + solr_server_dir + "\"");
            argsBuilder.Append(" -Djava.io.tmpdir=\"" + java_tempdir + "\"");
            argsBuilder.Append(" -jar start.jar");
            argsBuilder.Append(" \"--module=http\"");

            string args = argsBuilder.ToString();

            // Using string builders to collect the standard output and error
            StringBuilder output = new StringBuilder();
            StringBuilder error = new StringBuilder();

            // Perform all this work within using tag to encourage disposing of the process and events
            bool returnValue = true;
            using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
            using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
            {
                using (Process startSolrProcess = new Process())
                {
                    try
                    {

                        startSolrProcess.StartInfo.FileName = JavaWrapper.Java_Executable;
                        startSolrProcess.StartInfo.Arguments = args;
                        startSolrProcess.StartInfo.WorkingDirectory = solr_server_dir;

                        startSolrProcess.StartInfo.UseShellExecute = false;
                        startSolrProcess.StartInfo.RedirectStandardOutput = true;
                        startSolrProcess.StartInfo.RedirectStandardError = true;
                        startSolrProcess.StartInfo.CreateNoWindow = true;

                        // Add a function to handle asynchronous reading of the standard output
                        startSolrProcess.OutputDataReceived += (sender, e) =>
                        {
                            if (e.Data == null)
                            {
                                outputWaitHandle.Set();
                            }
                            else
                            //if (e.Data != null)
                            {
                                output.AppendLine("     " + e.Data);
                            }
                        };

                        // Add a function to handle asynchronous reading of the standard error
                        startSolrProcess.ErrorDataReceived += (sender, e) =>
                        {
                            if (e.Data == null)
                            {
                                errorWaitHandle.Set();
                            }
                            else
                            //if (e.Data != null)
                            {
                                error.AppendLine("     " + e.Data);
                            }
                        };

                        // Log (verbose) actual arguments
                        EventLogHelper.Write_Verbose("About to run command to start solr/jetty." + Environment.NewLine + Environment.NewLine + "Executable: " + JavaWrapper.Java_Executable + Environment.NewLine + Environment.NewLine + "Arguments: " + args + Environment.NewLine + Environment.NewLine + "Working Directory: " + solr_server_dir);

                        // Start the process
                        startSolrProcess.Start();

                        // Start the asyncronous reading of the standard input and standard output
                        startSolrProcess.BeginOutputReadLine();
                        startSolrProcess.BeginErrorReadLine();

                        // Wait for the process to copmlete
                        if (startSolrProcess.WaitForExit(TimeOut))
                        {
                            // Give the output readers slightly more time to finish any reading
                            if (outputWaitHandle.WaitOne(100) && errorWaitHandle.WaitOne(100))
                            {
                                // Successfully ended and process completed.  Check process exit code here.
                                int exitCode = startSolrProcess.ExitCode;
                                EventLogHelper.Write_Verbose("Solr/Lucene request closed with exit code of " + exitCode);
                            }

                            startSolrProcess.CancelOutputRead();
                            startSolrProcess.CancelErrorRead();
                        }
                        else
                        {
                            // Timed out.
                            EventLogHelper.Write_Verbose("Timeout occurred during solr/lucene startup");

                            outputWaitHandle.WaitOne(100);
                            errorWaitHandle.WaitOne(100);

                            startSolrProcess.CancelOutputRead();
                            startSolrProcess.CancelErrorRead();
                        }

                        startSolrProcess.Close();

                        // Add the error output if something was received
                        if (error.ToString().Trim().Length > 0)
                        {
                            logStartBuilder.AppendLine("==============================" + Environment.NewLine + "STANDARD OUTPUT: " + Environment.NewLine + output.ToString() + Environment.NewLine + Environment.NewLine + "STANDARD ERROR:" + Environment.NewLine + error.ToString());

                        }
                        EventLogHelper.Write_Event(ServiceSettings.Logging.StartEventId, "Solr/Lucene start attempt completed" + Environment.NewLine + logStartBuilder.ToString() + Environment.NewLine );
                    }
                    catch (Exception ee)
                    {
                        CaughtException = ee;
                        EventLogHelper.Write_Error(ServiceSettings.Logging.FailedStartEventId, String.Format(ERROR_MESSAGE_TEMPLATE, "EXCEPTION detected during solr/lucene startup attempt", solrExecutable + " " + argsBuilder, output.ToString(), error.ToString()), ee);
                        returnValue = true;
                    }
                }
            }

            // Log this to the solr log directory
            string solr_log_file = Path.Combine(logs_directory, "solr-" + ServiceSettings.Solr.Port + "-service.log");
            try
            {
                StreamWriter writer = new StreamWriter(solr_log_file, false);

                writer.WriteLine("LOG FILE CREATED " + DateTime.Now.ToString() + " BY SOLR SERVICE WRAPPER");
                writer.WriteLine();

                writer.WriteLine("Executable: " + JavaWrapper.Java_Executable);
                writer.WriteLine();
                writer.WriteLine("Arguments: " + args );
                writer.WriteLine();
                writer.WriteLine("Working directory: " + solr_server_dir);
                writer.WriteLine();

                writer.WriteLine("====================================================");
                writer.WriteLine();
                writer.WriteLine(logStartBuilder.ToString());
                writer.WriteLine();

                if (error.ToString().Trim().Length > 0)
                {

                    writer.WriteLine("====================================================");
                    writer.WriteLine();
                    writer.WriteLine("REDIRECTED ERROR:");
                    writer.WriteLine(error.ToString());
                    writer.WriteLine();
                }

                if (CaughtException != null)
                {
                    writer.WriteLine("====================================================");
                    writer.WriteLine();
                    writer.WriteLine("CAUGHT EXCEPTION:");
                    writer.WriteLine(CaughtException.Message);
                    writer.WriteLine();
                    writer.WriteLine(CaughtException.StackTrace);
                    writer.WriteLine();
                }

                writer.WriteLine("====================================================");
                writer.WriteLine();
                writer.WriteLine("REDIRECTED OUTPUT:");
                writer.WriteLine(output.ToString());
                writer.WriteLine();


                writer.Flush();
                writer.Close();
            }
            catch (Exception ee)
            {
                EventLogHelper.Write_Error(ServiceSettings.Logging.ExceptionEventId, "Unexpected exception trying to write the log file " + solr_log_file, ee);
            }

            return returnValue;
        }

        public static bool Stop()
        {
            // Add verbose log
            EventLogHelper.Write_Verbose("Inside SolrWrapper.Stop() method");

            // Check to see if the port is open
            if (!is_port_in_use(ServiceSettings.Solr.Port))
            {
                EventLogHelper.Write_Event(ServiceSettings.Logging.FailedStopEventId, "Solr port (" + ServiceSettings.Solr.Port + ") is not currently in use." + Environment.NewLine + Environment.NewLine + "Solr stop command will not be executed.");
                return true;
            }

            // Get and test the solr server directory
            string solr_server_dir = Path.Combine(ServiceSettings.Solr.Directory, "server");
            if (!String.IsNullOrEmpty(ServiceSettings.Override.SolrServerDir))
                solr_server_dir = ServiceSettings.Override.SolrServerDir;
            if (!Directory.Exists(solr_server_dir))
            {
                EventLogHelper.Write_Error(ServiceSettings.Logging.FailedStopEventId, "Solr server directory " + solr_server_dir + " not found!" + Environment.NewLine + Environment.NewLine + "Re-install solr/lucene and add the solr server directory under Solr.Directory to the configuration file for this service." + Environment.NewLine + Environment.NewLine + "If your setup is non-standard, you can use the Override.SOLR_SERVER_DIR value in the configuration file for this service.");
                return false;
            }

            // Build the arguments
            StringBuilder argsBuilder = new StringBuilder();
            argsBuilder.Append(" -DJetty.home=\"" + solr_server_dir + "\"");
            argsBuilder.Append(" -jar start.jar");
            argsBuilder.Append(" STOP.PORT=" + ServiceSettings.Solr.StopPort);
            argsBuilder.Append(" STOP.KEY=" + ServiceSettings.Solr.StopKey);
            argsBuilder.Append(" --stop");
            string args = argsBuilder.ToString();

            // Using string builders to collect the standard output and error
            StringBuilder output = new StringBuilder();
            StringBuilder error = new StringBuilder();

            // Perform all this work within using tag to encourage disposing of the process and events
            bool returnValue = true;
            using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
            using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
            {
                using (Process startSolrProcess = new Process())
                {
                    try
                    {

                        startSolrProcess.StartInfo.FileName = JavaWrapper.Java_Executable;
                        startSolrProcess.StartInfo.Arguments = args;
                        startSolrProcess.StartInfo.WorkingDirectory = solr_server_dir;

                        startSolrProcess.StartInfo.UseShellExecute = false;
                        startSolrProcess.StartInfo.RedirectStandardOutput = true;
                        startSolrProcess.StartInfo.RedirectStandardError = true;
                        startSolrProcess.StartInfo.CreateNoWindow = true;

                        // Add a function to handle asynchronous reading of the standard output
                        startSolrProcess.OutputDataReceived += (sender, e) =>
                        {
                            if (e.Data == null)
                            {
                                outputWaitHandle.Set();
                            }
                            else
                            //if (e.Data != null)
                            {
                                output.AppendLine("     " + e.Data);
                            }
                        };

                        // Add a function to handle asynchronous reading of the standard error
                        startSolrProcess.ErrorDataReceived += (sender, e) =>
                        {
                            if (e.Data == null)
                            {
                                errorWaitHandle.Set();
                            }
                            else
                            //if (e.Data != null)
                            {
                                error.AppendLine("     " + e.Data);
                            }
                        };

                        // Log (verbose) actual arguments
                        EventLogHelper.Write_Verbose("About to run command to start solr/jetty." + Environment.NewLine + Environment.NewLine + "Executable: " + JavaWrapper.Java_Executable + Environment.NewLine + Environment.NewLine + "Arguments: " + args + Environment.NewLine + Environment.NewLine + "Working Directory: " + solr_server_dir);

                        // Start the process
                        startSolrProcess.Start();

                        // Start the asyncronous reading of the standard input and standard output
                        startSolrProcess.BeginOutputReadLine();
                        startSolrProcess.BeginErrorReadLine();

                        // Wait for the process to copmlete
                        if (startSolrProcess.WaitForExit(5000))
                        {
                            // Give the output readers slightly more time to finish any reading
                            if (outputWaitHandle.WaitOne(100) && errorWaitHandle.WaitOne(100))
                            {
                                // Successfully ended and process completed.  Check process exit code here.
                                int exitCode = startSolrProcess.ExitCode;
                                EventLogHelper.Write_Verbose("Solr/Lucene request closed with exit code of " + exitCode);
                            }

                            startSolrProcess.CancelOutputRead();
                            startSolrProcess.CancelErrorRead();
                        }
                        else
                        {
                            // Timed out.
                            EventLogHelper.Write_Verbose("Timeout occurred during solr/lucene stop");

                            outputWaitHandle.WaitOne(100);
                            errorWaitHandle.WaitOne(100);

                            startSolrProcess.CancelOutputRead();
                            startSolrProcess.CancelErrorRead();
                        }

                        startSolrProcess.Close();

                        // If there was error caught from the misdirection add it here
                        StringBuilder stopSolrBuilder = new StringBuilder("Solr/Lucene stop attempt completed");
                        if ((error.ToString().Trim().Length > 0) || (output.ToString().Trim().Length > 0))
                        {
                            stopSolrBuilder.AppendLine();
                            stopSolrBuilder.AppendLine(" ============================== ");
                            stopSolrBuilder.AppendLine();

                            if (output.ToString().Trim().Length > 0)
                            {
                                stopSolrBuilder.AppendLine("Standard output (redirected):");
                                stopSolrBuilder.AppendLine(output.ToString().Trim());
                                stopSolrBuilder.AppendLine();
                            }

                            if (error.ToString().Trim().Length > 0)
                            {
                                stopSolrBuilder.AppendLine("Standard error (redirected):");
                                stopSolrBuilder.AppendLine(error.ToString().Trim());
                                stopSolrBuilder.AppendLine();
                            }
                        }

                        EventLogHelper.Write_Event(ServiceSettings.Logging.StopEventId, stopSolrBuilder.ToString());
                    }
                    catch (Exception ee)
                    {
                        EventLogHelper.Write_Error(ServiceSettings.Logging.FailedStopEventId, String.Format(ERROR_MESSAGE_TEMPLATE, "EXCEPTION detected during solr/lucene stop attempt", solrExecutable + " " + argsBuilder, output.ToString(), error.ToString()), ee);
                        return false;
                    }
                }
            }

            return true;
        }

        private static string Get_Solr_Web_Response(string URL, bool LogRequest )
        {
            // Return string
            string responseFromServer = String.Empty;

            // Add verbose log
            if ( LogRequest )
                EventLogHelper.Write_Verbose("In SolrWrapper.Get_Solr_Response('" + URL + "')");

            try
            {
                // Create a request for the URL
                WebRequest request = WebRequest.Create(URL);
                request.Credentials = CredentialCache.DefaultCredentials;
                ((HttpWebRequest) request).UserAgent = ServiceSettings.Name;

                // Get the response object
                using (WebResponse response = request.GetResponse())
                {

                    // Log the status
                    if (LogRequest)
                        EventLogHelper.Write_Verbose("Status received: " + ((HttpWebResponse) response).StatusDescription);

                    // Get the stream containing content returned by the server.
                    using (Stream dataStream = response.GetResponseStream())
                    {
                        if (dataStream != null)
                        {
                            dataStream.ReadTimeout = 30000;

                            // Open the stream using a StreamReader for easy access.
                            using (StreamReader reader = new StreamReader(dataStream))
                            {
                                // Read the content.
                                responseFromServer = reader.ReadToEnd();

                                // Clean up the streams and the response.
                                reader.Close();
                            }

                            dataStream.Close();
                        }
                    }
                    response.Close();
                }
            }
            catch (WebException we)
            {
                // Try to read the response from the exception
                try
                {
                    Stream dataStream = we.Response.GetResponseStream();
                    StreamReader reader = new StreamReader(dataStream);
                    string errorResponse = reader.ReadToEnd();

                    EventLogHelper.Write_Error(ServiceSettings.Logging.ExceptionEventId, "WEB EXCEPTION caught in SolrWrapper.Get_Solr_Response('" + URL + "').  Request returned a status of " + we.Status.ToString() + "\n\n--------------------\n\nRESPONSE:\n\n" + errorResponse, we);
                    return "404";
                }
                catch (Exception)
                {
                    EventLogHelper.Write_Error(ServiceSettings.Logging.ExceptionEventId, "WEB EXCEPTION caught in SolrWrapper.Get_Solr_Response('" + URL + "').  Request returned a status of " + we.Status.ToString(), we);
                    return "500";
                }
            }
            catch (Exception ee)
            {

                EventLogHelper.Write_Error(ServiceSettings.Logging.ExceptionEventId, "GENERAL EXCEPTION caught in SolrWrapper.Get_Solr_Response('" + URL + "')", ee);
                return null;
            }

            // Log the response
            if (LogRequest)
                EventLogHelper.Write_Verbose("RESPONSE RECEIVED:\n\n" + responseFromServer);

            return responseFromServer;
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
