using Microsoft.Win32;
using SobekCM.Builder_Library;
using SobekCM.Builder_Library.Settings;
using SobekCM.Engine_Library.ApplicationState;
using SobekCM.Engine_Library.Database;
using SobekCM.Tools.Logs;
using SobekCM_Resource_Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Solr_Reindexer
{
    internal class Solr_Reindexer_Controller
    {
        private string configurationFile;
        private string logFileDirectory;
        private Single_Instance_Configuration instance;
        private Worker_Reindexer worker;

        private readonly bool verbose;


        public Solr_Reindexer_Controller(bool Verbose)
        {
            Console.WriteLine("    Solr_Reindexer.Constructor");

            verbose = Verbose;


            // Determine the directory this is runnin in
            string startupDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            MultiInstance_Builder_Settings.Builder_Executable_Directory = startupDirectory;

            logFileDirectory = Path.Combine(MultiInstance_Builder_Settings.Builder_Executable_Directory ?? String.Empty, "logs");
        }

        private void write_error(string Message, LogFileXhtml PreloaderLogger)
        {
            Console.WriteLine(Message + "\n");
            PreloaderLogger.AddError(Message);
        }

        private void write_nonerror(string Message, LogFileXhtml PreloaderLogger)
        {
            Console.WriteLine(Message + "\n");
            PreloaderLogger.AddNonError(Message);
        }

        private bool Configure_Builder_To_Run(LogFileXhtml PreloaderLogger)
        {
            Console.WriteLine("    Solr_Reindexer.Configure_Builders_To_Run");

            // Verify connectivity and rights on the logs subfolder
            if (!Directory.Exists(logFileDirectory))
            {
                try
                {
                    Directory.CreateDirectory(logFileDirectory);
                }
                catch
                {
                    Console.WriteLine("Error creating necessary logs subfolder under the application folder.\n");
                    Console.WriteLine("Please create manually.\n");
                    Console.WriteLine(logFileDirectory);
                    return false;
                }
            }
            try
            {
                var testWriter = new StreamWriter(Path.Combine(logFileDirectory, "test.log"), false);
                testWriter.WriteLine("TEST");
                testWriter.Flush();
                testWriter.Close();

                File.Delete(Path.Combine(logFileDirectory, "test.log"));
            }
            catch
            {
                Console.WriteLine("The service account needs modify rights on the logs subfolder.\n");
                Console.WriteLine("Please correct manually.\n");
                Console.WriteLine(logFileDirectory);
                return false;
            }

            // Now, veryify the configuration file exists
            configurationFile = Path.Combine(MultiInstance_Builder_Settings.Builder_Executable_Directory, "config", "sobekcm.config");
            if (!File.Exists(configurationFile))
            {
                write_error("The configuration file is missing!!", PreloaderLogger);
                write_error("Execution aborted due to missing configuration file.", PreloaderLogger);
                return false;
            }

            // Should be a config file now, so read it
            if (!MultiInstance_Builder_Settings_Reader.Read_Config(configurationFile))
            {
                write_error("Error encountered reading the configuration file!!", PreloaderLogger);
                write_error("Execution aborted due to incorrect configuration file.", PreloaderLogger);
                return false;
            }

            // If no instances exist, then the builder has nothing to do
            if ((MultiInstance_Builder_Settings.Instances.Count == 0) || (String.IsNullOrEmpty(MultiInstance_Builder_Settings.Instances[0].DatabaseConnection.Connection_String)))
            {
                write_error("No instances listed in the congfiguration file", PreloaderLogger);
                write_error("Execution aborted due to configuration file not including any instances to process", PreloaderLogger);
                return false;
            }

            // Save the list of instances
            foreach (Single_Instance_Configuration dbInfo in MultiInstance_Builder_Settings.Instances)
            {
                if (dbInfo.Is_Active)
                {
                    if (instance == null)
                    {
                        instance = dbInfo;
                    }
                    else
                    {

                        write_error("Can only list one actve instance in the congfiguration file", PreloaderLogger);
                        write_error("Execution aborted due to configuration file having more than one instances to process", PreloaderLogger);
                        return false;
                    }
                }
            }

            Console.WriteLine($"        Added 1 instances from configuration");

            // Build all the bulk loader objects


            SobekCM_Item_Database.Connection_String = instance.DatabaseConnection.Connection_String;
            Engine_Database.Connection_String = instance.DatabaseConnection.Connection_String;


            write_nonerror(instance.Name + " - Preparing to begin polling", PreloaderLogger);

            // Create the new bulk loader
            worker = new Worker_Reindexer(PreloaderLogger, instance, verbose);

            // Try to refresh to test database and engine connectivity
            if (!worker.Refresh_Settings_And_Item_List())
            {
                write_error(instance.Name + " - Error pulling setting of configuration information", PreloaderLogger);
            }

            return true;
        }

        /// <summary> Continuously execute the processes in a recurring background thread </summary>
        public void Execute()
        {
            Console.WriteLine("Entering Solr_Reindexer.Execute");

            var controllerStarted = DateTime.Now;

            // Determine the new log name
            string log_name = "incoming_" + controllerStarted.Year + "_" + controllerStarted.Month.ToString().PadLeft(2, '0') + "_" + controllerStarted.Day.ToString().PadLeft(2, '0') + ".html";
            string local_log_name = Path.Combine(logFileDirectory, log_name);

            // Create the new log file
            LogFileXhtml preloader_logger = null;
            try
            {
                preloader_logger = new LogFileXhtml(local_log_name, "SobekCM Incoming Packages Log", "UFDC_Builder.exe", true);
            }
            catch (Exception ee)
            {
                Console.WriteLine("Error creating logfile: " + ee.Message);
            }

            // Configure builders to run and run some basic tests
            if (!Configure_Builder_To_Run(preloader_logger))
                return;

            // Set the database connection strings
            Engine_Database.Connection_String = instance.DatabaseConnection.Connection_String;
            SobekCM_Item_Database.Connection_String = instance.DatabaseConnection.Connection_String;

            // Refresh all settings, etc..  (already happens in the Run_BulkLoader, almost immediately)
            //loaders[i].Refresh_Settings_And_Item_List();

            worker.Perform_Reindexing(verbose);
        }
    }
}
