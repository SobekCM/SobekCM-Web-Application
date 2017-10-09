#region Using directives

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using Microsoft.Win32;
using SobekCM.Builder_Library.Settings;
using SobekCM.Engine_Library.ApplicationState;
using SobekCM.Engine_Library.Database;
using SobekCM.Tools.Logs;
using SobekCM_Resource_Database;

#endregion

namespace SobekCM.Builder_Library
{
    /// <summary> Class controls the execution of all tasks, whether being immediately executed
    /// or running continuously in a background thread </summary>
    public class Worker_Controller
    {
        private bool aborted;
        private DateTime controllerStarted;
        private DateTime configReadTime;
        private string configurationFile;
        private const int BULK_LOADER_END_HOUR = 23;
        private DateTime feedNextBuildTime;
        private readonly bool verbose;

        private readonly List<Single_Instance_Configuration> instances;
        private readonly List<Worker_BulkLoader> loaders;

        private readonly string logFileDirectory;
        private readonly string pluginRootDirectory;


        /// <summary> Constructor for a new instance of the Worker_Controller class </summary>
        /// <param name="Verbose"> Flag indicates if this should be verbose in the log file and console </param>
        public Worker_Controller( bool Verbose )
        {
            verbose = Verbose;
            controllerStarted = DateTime.Now;
            aborted = false;
            instances = new List<Single_Instance_Configuration>();
            loaders = new List<Worker_BulkLoader>();


            // Determine the directory this is runnin in
            string startupDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            MultiInstance_Builder_Settings.Builder_Executable_Directory = startupDirectory;

            logFileDirectory = Path.Combine(MultiInstance_Builder_Settings.Builder_Executable_Directory ?? String.Empty, "logs");
            pluginRootDirectory = Path.Combine(MultiInstance_Builder_Settings.Builder_Executable_Directory ?? String.Empty, "plugins");


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

        #region Method to load and test the original data

        private bool Configure_Builders_To_Run(LogFileXhtml PreloaderLogger)
        {
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
                StreamWriter testWriter = new StreamWriter(Path.Combine(logFileDirectory, "test.log"), false);
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

            // Verify connectivity and rights on the plugins subfolder
            if (!Directory.Exists(pluginRootDirectory))
            {
                try
                {
                    Directory.CreateDirectory(pluginRootDirectory);
                }
                catch
                {
                    Console.WriteLine("Error creating necessary plugins subfolder under the application folder.\n");
                    Console.WriteLine("Please create manually.\n");
                    Console.WriteLine(pluginRootDirectory);
                    return false;
                }
            }
            try
            {
                StreamWriter testWriter = new StreamWriter(Path.Combine(pluginRootDirectory, "test.log"), false);
                testWriter.WriteLine("TEST");
                testWriter.Flush();
                testWriter.Close();

                File.Delete(Path.Combine(pluginRootDirectory, "test.log"));
            }
            catch
            {
                Console.WriteLine("The service account needs modify rights on the plugins subfolder.\n");
                Console.WriteLine("Please correct manually.\n");
                Console.WriteLine(pluginRootDirectory);
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

            // Since the configuration was read, save the time
            configReadTime = DateTime.Now;

            // If no instances exist, then the builder has nothing to do
            if ((MultiInstance_Builder_Settings.Instances.Count == 0) || (String.IsNullOrEmpty(MultiInstance_Builder_Settings.Instances[0].DatabaseConnection.Connection_String)))
            {
                write_error("No instances listed in the congfiguration file", PreloaderLogger);
                write_error("Execution aborted due to configuration file not including any instances to process", PreloaderLogger);
                return false;
            }

            // This is a bit of shortcut in the case there is only one instance
            if (MultiInstance_Builder_Settings.Instances.Count == 1)
            {
                // If no database connection on the single instance, return
                if (String.IsNullOrEmpty(MultiInstance_Builder_Settings.Instances[0].DatabaseConnection.Connection_String))
                {
                    write_error("Single instance configuration is missing the database connection string", PreloaderLogger);
                    write_error("Execution aborted", PreloaderLogger);
                    return false;
                }

                Engine_Database.Connection_String = MultiInstance_Builder_Settings.Instances[0].DatabaseConnection.Connection_String;
                if (!Engine_Database.Test_Connection())
                {
                    write_error("Unable to connect to the database using provided connection string:", PreloaderLogger);
                    write_error(Engine_Database.Connection_String, PreloaderLogger);
                    write_error("Execution aborted", PreloaderLogger);
                    return false;
                }
            }

            // Look for the valid Image magick file information in the registry and configuration
            if ((String.IsNullOrEmpty(Engine_ApplicationCache_Gateway.Settings.Builder.ImageMagick_Executable)) || (!File.Exists(Engine_ApplicationCache_Gateway.Settings.Builder.ImageMagick_Executable)))
            {
                string possible_imagemagick = Look_For_Variable_Registry_Key("SOFTWARE\\ImageMagick", "BinPath");
                if ((!String.IsNullOrEmpty(possible_imagemagick)) && (Directory.Exists(possible_imagemagick)) && (File.Exists(Path.Combine(possible_imagemagick, "convert.exe"))))
                {
                    MultiInstance_Builder_Settings.ImageMagick_Executable = Path.Combine(possible_imagemagick, "convert.exe");
                }
            }
            else
            {
                MultiInstance_Builder_Settings.ImageMagick_Executable = Engine_ApplicationCache_Gateway.Settings.Builder.ImageMagick_Executable;
            }

            // If no ImageMagick file found, add a warning
            if ((String.IsNullOrEmpty(MultiInstance_Builder_Settings.ImageMagick_Executable)) || (!File.Exists(MultiInstance_Builder_Settings.ImageMagick_Executable)))
            {
                write_nonerror("WARNING: Could not find ImageMagick installed.  Some image processing will be unavailable.", PreloaderLogger);
            }

            // Look for a valid ghostscript file information in the registry and configuration
            if ((String.IsNullOrEmpty(Engine_ApplicationCache_Gateway.Settings.Builder.Ghostscript_Executable)) || (!File.Exists(Engine_ApplicationCache_Gateway.Settings.Builder.Ghostscript_Executable)))
            {
                string possible_ghost = Look_For_Variable_Registry_Key("SOFTWARE\\GPL Ghostscript", "GS_DLL");
                if (!String.IsNullOrEmpty(possible_ghost))
                {
                    string gsPath = Path.GetDirectoryName(possible_ghost);
                    if ((!String.IsNullOrEmpty(gsPath)) && (Directory.Exists(gsPath)) && ((File.Exists(Path.Combine(gsPath, "gswin32c.exe"))) || (File.Exists(Path.Combine(gsPath, "gswin64c.exe")))))
                    {
                        if (File.Exists(Path.Combine(gsPath, "gswin64c.exe")))
                            MultiInstance_Builder_Settings.Ghostscript_Executable = Path.Combine(gsPath, "gswin64c.exe");
                        else
                            MultiInstance_Builder_Settings.Ghostscript_Executable = Path.Combine(gsPath, "gswin32c.exe");
                    }
                }
            }
            else
            {
                MultiInstance_Builder_Settings.Ghostscript_Executable = Engine_ApplicationCache_Gateway.Settings.Builder.Ghostscript_Executable;
            }

            // If no Ghostscript file found, add a warning
            if ((String.IsNullOrEmpty(MultiInstance_Builder_Settings.Ghostscript_Executable)) || (!File.Exists(MultiInstance_Builder_Settings.Ghostscript_Executable)))
            {
                write_nonerror("WARNING: Could not find GhostScript installed.  Some PDF processing will be unavailable.", PreloaderLogger);
            }
            
            // Save the list of instances
            instances.Clear();
            foreach (Single_Instance_Configuration dbInfo in MultiInstance_Builder_Settings.Instances)
            {
                instances.Add(dbInfo);
            }

            // First, step through each active configuration and see if building is currently aborted 
            // while doing very minimal processes
            aborted = false;
            write_nonerror("Checking for initial abort condition", PreloaderLogger);
            string abort_message = String.Empty;
            int build_instances = 0;
            foreach (Single_Instance_Configuration dbConfig in instances)
            {
                if (!dbConfig.Is_Active)
                {
                    write_nonerror(dbConfig.Name + " is set to INACTIVE", PreloaderLogger);
                }
                else
                {

                    SobekCM_Item_Database.Connection_String = dbConfig.DatabaseConnection.Connection_String;
                    Engine_Database.Connection_String = dbConfig.DatabaseConnection.Connection_String;

                    // Check that this should not be skipped or aborted
                    Builder_Operation_Flag_Enum operationFlag = Abort_Database_Mechanism.Builder_Operation_Flag;
                    switch (operationFlag)
                    {
                        case Builder_Operation_Flag_Enum.ABORT_REQUESTED:
                        case Builder_Operation_Flag_Enum.ABORTING:
                            // Since this was an abort request at the very beginning, switch back to standard
                            Abort_Database_Mechanism.Builder_Operation_Flag = Builder_Operation_Flag_Enum.STANDARD_OPERATION;
                            build_instances++;
                            break;

                        case Builder_Operation_Flag_Enum.NO_BUILDING_REQUESTED:
                            abort_message = "PREVIOUS NO BUILDING flag found in " + dbConfig.Name;
                            write_nonerror(abort_message, PreloaderLogger);
                            break;

                        default:
                            build_instances++;
                            break;

                    }
                }
            }


            // If no instances to run just abort
            if (build_instances == 0)
            {
                // Add messages in each active instance
                foreach (Single_Instance_Configuration dbConfig in instances)
                {
                    if (dbConfig.Is_Active)
                    {
                        write_error("No active databases set for building in config file... Aborting", PreloaderLogger);
                        SobekCM_Item_Database.Connection_String = dbConfig.DatabaseConnection.Connection_String;
                        Engine_Database.Connection_String = dbConfig.DatabaseConnection.Connection_String;
                        Engine_Database.Builder_Add_Log_Entry(-1, String.Empty, "Standard", abort_message, String.Empty);

                        // Save information about this last run
                        Engine_Database.Set_Setting("Builder Version", Engine_ApplicationCache_Gateway.Settings.Static.Current_Builder_Version);
                        Engine_Database.Set_Setting("Builder Last Run Finished", DateTime.Now.ToString());
                        Engine_Database.Set_Setting("Builder Last Message", abort_message);
                    }
                }

                // Do nothing else
                return false;
            }

            // Build all the bulk loader objects
            loaders.Clear();
            foreach (Single_Instance_Configuration dbConfig in instances)
            {
                if (!dbConfig.Is_Active)
                {
                    loaders.Add(null);
                    continue;
                }

                SobekCM_Item_Database.Connection_String = dbConfig.DatabaseConnection.Connection_String;
                Engine_Database.Connection_String = dbConfig.DatabaseConnection.Connection_String;

                // At this point warn on mossing the Ghostscript and ImageMagick, to get it into each instances database logs
                if ((String.IsNullOrEmpty(MultiInstance_Builder_Settings.ImageMagick_Executable)) || (!File.Exists(MultiInstance_Builder_Settings.ImageMagick_Executable)))
                {
                    Engine_Database.Builder_Add_Log_Entry(-1, String.Empty, "Standard", "WARNING: Could not find ImageMagick installed.  Some image processing will be unavailable.", String.Empty);
                }

                if ((String.IsNullOrEmpty(MultiInstance_Builder_Settings.Ghostscript_Executable)) || (!File.Exists(MultiInstance_Builder_Settings.Ghostscript_Executable)))
                {
                    Engine_Database.Builder_Add_Log_Entry(-1, String.Empty, "Standard", "WARNING: Could not find GhostScript installed.  Some PDF processing will be unavailable.", String.Empty);
                }

                write_nonerror(dbConfig.Name + " - Preparing to begin polling", PreloaderLogger);
                Engine_Database.Builder_Add_Log_Entry(-1, String.Empty, "Standard", "Preparing to begin polling", String.Empty);

                // Create the new bulk loader
                Worker_BulkLoader newLoader = new Worker_BulkLoader(PreloaderLogger, dbConfig, verbose, logFileDirectory, pluginRootDirectory);

                // Try to refresh to test database and engine connectivity
                if (newLoader.Refresh_Settings_And_Item_List())
                    loaders.Add(newLoader);
                else
                {
                    write_error(dbConfig.Name + " - Error pulling setting of configuration information", PreloaderLogger);
                }
            }

            // If no loaders past the tests above, done
            if (loaders.Count == 0)
            {
                write_error("Aborting since no valid instances found to process", PreloaderLogger);
                return false;
            }

            // Set the maximum number of packages to process before moving to the next instance
            if (loaders.Count > 1)
                MultiInstance_Builder_Settings.Instance_Package_Limit = 100;

            return true;
        }


        #endregion

        #region Method to execute processes in background

        /// <summary> Continuously execute the processes in a recurring background thread </summary>
        public void Execute_In_Background()
        {
            // Determine the new log name
            string log_name = "incoming_" + controllerStarted.Year + "_" + controllerStarted.Month.ToString().PadLeft(2, '0') + "_" + controllerStarted.Day.ToString().PadLeft(2, '0') + ".html";
            string local_log_name = Path.Combine(logFileDirectory, log_name);

            // Create the new log file
            LogFileXhtml preloader_logger = null;
            try
            {
                preloader_logger = new LogFileXhtml(local_log_name, "SobekCM Incoming Packages Log", "UFDC_Builder.exe", true);
            }
            catch ( Exception ee )
            {
                Console.WriteLine("Error creating logfile: " + ee.Message);
            }

            // Configure builders to run and run some basic tests
            if ((!Configure_Builders_To_Run(preloader_logger)) || ( preloader_logger == null ))
                return;

            // Set the variable which will control background execution
	        int time_between_polls = Engine_ApplicationCache_Gateway.Settings.Builder.Override_Seconds_Between_Polls.HasValue ? Engine_ApplicationCache_Gateway.Settings.Builder.Override_Seconds_Between_Polls.Value : 60;
			if (( time_between_polls < 0 ) || ( MultiInstance_Builder_Settings.Instances.Count == 1 ))
				time_between_polls = Convert.ToInt32(Engine_ApplicationCache_Gateway.Settings.Builder.Seconds_Between_Polls);
            
            // Loop continually until the end hour is achieved
            Builder_Operation_Flag_Enum abort_flag = Builder_Operation_Flag_Enum.STANDARD_OPERATION;
            do
            {
                // Look to see if the config file has changed
                DateTime lastConfigWrite = File.GetLastWriteTime(configurationFile);
                if (lastConfigWrite > configReadTime)
                {
                    // RE-Configure builders to run and run some basic tests
                    if ((!Configure_Builders_To_Run(preloader_logger)) || (preloader_logger == null))
                        return;
                }


                bool skip_sleep = false;

				// Step through each instance
				for (int i = 0; i < loaders.Count; i++)
				{
					if (loaders[i] != null)
					{
                        // Get the instance
                        Single_Instance_Configuration dbInstance = instances[i];

						// Set the database connection strings
					    Engine_Database.Connection_String = dbInstance.DatabaseConnection.Connection_String;
                        SobekCM_Item_Database.Connection_String = dbInstance.DatabaseConnection.Connection_String;

						// Look for abort
						if (CheckForAbort())
                        {
							aborted = true;
							if (Abort_Database_Mechanism.Builder_Operation_Flag != Builder_Operation_Flag_Enum.NO_BUILDING_REQUESTED)
							{
								abort_flag = Builder_Operation_Flag_Enum.LAST_EXECUTION_ABORTED;
								Abort_Database_Mechanism.Builder_Operation_Flag = Builder_Operation_Flag_Enum.ABORTING;
							}
							break;
						}

						// Refresh all settings, etc..
						loaders[i].Refresh_Settings_And_Item_List();

						// Pull the abort/pause flag
						Builder_Operation_Flag_Enum currentPauseFlag = Abort_Database_Mechanism.Builder_Operation_Flag;

						// If not paused, run the prebuilder
						if (currentPauseFlag != Builder_Operation_Flag_Enum.PAUSE_REQUESTED)
						{
                            skip_sleep = skip_sleep || Run_BulkLoader(loaders[i], verbose);

							// Look for abort
							if ((!aborted) && (CheckForAbort()))
							{
								aborted = true;
								if (Abort_Database_Mechanism.Builder_Operation_Flag != Builder_Operation_Flag_Enum.NO_BUILDING_REQUESTED)
								{
									abort_flag = Builder_Operation_Flag_Enum.LAST_EXECUTION_ABORTED;
									Abort_Database_Mechanism.Builder_Operation_Flag = Builder_Operation_Flag_Enum.ABORTING;
								}
								break;
							}
						}
						else
						{
							preloader_logger.AddNonError( dbInstance.Name +  " - Building paused");
                            Engine_Database.Builder_Add_Log_Entry(-1, String.Empty, "Standard", "Building temporarily PAUSED", String.Empty);
						}
					}
				}

	            if (aborted)
		            break;


				// Publish the log
	            publish_log_file(local_log_name);

                // Sleep for correct number of milliseconds
                if ( !skip_sleep )
                    Thread.Sleep(1000 * time_between_polls);


            } while (DateTime.Now.Hour < BULK_LOADER_END_HOUR);

			// Do the final work for all of the different dbInstances
	        if (!aborted)
	        {
		        for (int i = 0; i < instances.Count; i++)
		        {
			        if (loaders[i] != null)
			        {
                        // Get the instance
                        Single_Instance_Configuration dbInstance = instances[i];

				        // Set the database flag
                        SobekCM_Item_Database.Connection_String = dbInstance.DatabaseConnection.Connection_String;
                        Engine_Database.Connection_String = dbInstance.DatabaseConnection.Connection_String;

				        // Pull the abort/pause flag
				        Builder_Operation_Flag_Enum currentPauseFlag2 = Abort_Database_Mechanism.Builder_Operation_Flag;

				        // If not paused, run the prebuilder
				        if (currentPauseFlag2 != Builder_Operation_Flag_Enum.PAUSE_REQUESTED)
				        {
					        // Initiate the recreation of the links between metadata and collections
                            Engine_Database.Admin_Update_Cached_Aggregation_Metadata_Links();
				        }

				        // Clear the memory
				        loaders[i].ReleaseResources();
			        }
		        }
	        }
	        else
	        {
		        // Mark the aborted in each instance
		        foreach (Single_Instance_Configuration dbConfig in instances )
		        {
					if (dbConfig.Is_Active)
					{
						Console.WriteLine("Setting abort flag message in " + dbConfig.Name);
						preloader_logger.AddNonError("Setting abort flag message in " + dbConfig.Name);
                        SobekCM_Item_Database.Connection_String = dbConfig.DatabaseConnection.Connection_String;
                        Engine_Database.Connection_String = dbConfig.DatabaseConnection.Connection_String;
                        Engine_Database.Builder_Add_Log_Entry(-1, String.Empty, "Standard", "Building ABORTED per request from database key", String.Empty);

						// Save information about this last run
                        Engine_Database.Set_Setting("Builder Version", Engine_ApplicationCache_Gateway.Settings.Static.Current_Builder_Version);
                        Engine_Database.Set_Setting("Builder Last Run Finished", DateTime.Now.ToString());
                        Engine_Database.Set_Setting("Builder Last Message", "Building ABORTED per request");

						// Finally, set the builder flag appropriately
						if ( abort_flag == Builder_Operation_Flag_Enum.LAST_EXECUTION_ABORTED )
							Abort_Database_Mechanism.Builder_Operation_Flag = Builder_Operation_Flag_Enum.LAST_EXECUTION_ABORTED;
					}
		        }
	        }


	        // Publish the log
			publish_log_file(local_log_name);


			//// Initiate a solr/lucene index optimization since we are done loading for a while
			//if (DateTime.Now.Day % 2 == 0)
			//{
			//	if (Engine_ApplicationCache_Gateway.Settings.Document_Solr_Index_URL.Length > 0)
			//	{
			//		Console.WriteLine("Initiating Solr/Lucene document index optimization");
			//		Solr_Controller.Optimize_Document_Index(Engine_ApplicationCache_Gateway.Settings.Document_Solr_Index_URL);
			//	}
			//}
			//else
			//{
			//	if (Engine_ApplicationCache_Gateway.Settings.Page_Solr_Index_URL.Length > 0)
			//	{
			//		Console.WriteLine("Initiating Solr/Lucene page index optimization");
			//		Solr_Controller.Optimize_Page_Index(Engine_ApplicationCache_Gateway.Settings.Page_Solr_Index_URL);
			//	}
			//}
			//// Sleep for twenty minutes to end this (the index rebuild might take some time)
            //Thread.Sleep(1000 * 20 * 60);
        }

		private void publish_log_file(string LocalLogName)
		{
			try
			{
                //if ((Engine_ApplicationCache_Gateway.Settings.Builder_Logs_Publish_Directory.Length > 0) && (Directory.Exists(Engine_ApplicationCache_Gateway.Settings.Builder_Logs_Publish_Directory)))
                //{
                //    if ( File.Exists(LocalLogName))
                //        File.Copy(LocalLogName, Engine_ApplicationCache_Gateway.Settings.Builder_Logs_Publish_Directory + "\\" + Path.GetFileName(LocalLogName), true );
                //}
			}
			catch
			{
				// Not critical error
			}
		}

        private bool Run_BulkLoader(Worker_BulkLoader Prebuilder, bool Verbose)
        {
            try
            {
                bool returnValue = Prebuilder.Perform_BulkLoader( Verbose );

                if (Prebuilder.Aborted)
                    aborted = true;

                return returnValue;
            }
            catch ( Exception ee )
            {
                return false;
            }
        }

         #endregion

        #region Method to immediately execute all requested processes - DEPRECATED

        // THIS REGION IS RETAINED JUST TO HAVE THIS CODE AVAILABLE, BUT:
        //    1) the builder ONLY runs in background mode
        //    2) the other portions from this method may be useful as the other
        //       builder options are refined as scheduled tasks


        ///// <summary> Immediately perform all requested tasks </summary>
        ///// <param name="BuildProductionMarcxmlFeed"> Flag indicates if the MarcXML feed for OPACs should be produced </param>
        ///// <param name="BuildTestMarcxmlFeed"> Flag indicates if the items set to be put in a TEST feed should have their MarcXML feed produced</param>
        ///// <param name="RunBulkloader"> Flag indicates if the preload </param>
        ///// <param name="CompleteStaticRebuild"> Flag indicates whether to rebuild all the item static pages </param>
        ///// <param name="MarcRebuild"> Flag indicates if all the MarcXML files for each resource should be rewritten from the METS/MODS metadata files </param>
        //public void Execute_Immediately(bool BuildProductionMarcxmlFeed, bool BuildTestMarcxmlFeed, bool RunBulkloader, bool CompleteStaticRebuild, bool MarcRebuild )
        //{
        //    // start with warnings on imagemagick and ghostscript not being installed
        //    if (Engine_ApplicationCache_Gateway.Settings.Builder.ImageMagick_Executable.Length == 0)
        //    {
        //        Console.WriteLine("WARNING: Could not find ImageMagick installed.  Some image processing will be unavailable.");
        //    }
        //    if (Engine_ApplicationCache_Gateway.Settings.Builder.Ghostscript_Executable.Length == 0)
        //    {
        //        Console.WriteLine("WARNING: Could not find GhostScript installed.  Some PDF processing will be unavailable.");
        //    }

        //    if (CompleteStaticRebuild)
        //    {
        //        Console.WriteLine("Beginning static rebuild");
        //        LogFileXhtml staticRebuildLog = new LogFileXhtml( logFileDirectory  + "\\static_rebuild.html");
        //        Static_Pages_Builder builder = new Static_Pages_Builder(Engine_ApplicationCache_Gateway.Settings.Servers.Application_Server_URL, Engine_ApplicationCache_Gateway.Settings.Servers.Static_Pages_Location, Engine_ApplicationCache_Gateway.Settings.Servers.Application_Server_Network);
        //        builder.Rebuild_All_Static_Pages(staticRebuildLog, true, String.Empty, -1);
        //    }
            
        //    if ( MarcRebuild )
        //    {
        //        Static_Pages_Builder builder = new Static_Pages_Builder(Engine_ApplicationCache_Gateway.Settings.Servers.Application_Server_URL, Engine_ApplicationCache_Gateway.Settings.Servers.Static_Pages_Location, Engine_ApplicationCache_Gateway.Settings.Servers.Application_Server_Network);
        //        builder.Rebuild_All_MARC_Files(Engine_ApplicationCache_Gateway.Settings.Servers.Image_Server_Network);
        //    }

        //    if (BuildProductionMarcxmlFeed)
        //    {
        //        Create_Complete_MarcXML_Feed(false);
        //    }

        //    if (BuildTestMarcxmlFeed)
        //    {
        //        Create_Complete_MarcXML_Feed(true);
        //    }

        //    // Create the log
        //    string directory = logFileDirectory;
        //    if (!Directory.Exists(directory))
        //        Directory.CreateDirectory(directory);

        //    // Run the PRELOADER
        //    if (RunBulkloader)
        //    {
        //        Run_BulkLoader( verbose );
        //    }
        //    else
        //    {
        //        Console.WriteLine("PreLoader skipped per command line arguments");
        //    }
        //}

        #endregion

        #region Methods to handle checking for abort requests

        private bool CheckForAbort()
        {
            if (aborted)
                return true;

            if (Abort_Database_Mechanism.Abort_Requested())
            {
                aborted = true;
                Abort_Database_Mechanism.Builder_Operation_Flag = Builder_Operation_Flag_Enum.ABORTING;
            }
            return aborted;
        }

        #endregion

        #region Method to create the mango/sus feed

        //private void Create_Complete_MarcXML_Feed( bool Test_Feed_Flag )
        //{
        //    // Determine some values based on whether this is for thr test feed or something else
        //    string feed_name = "Production MarcXML Feed";
        //    string file_name = "complete_marc.xml";
        //    string error_file_name = "complete_marc_last_error.html";
        //    if (Test_Feed_Flag)
        //    {
        //        feed_name = "Test MarcXML Feed";
        //        file_name = "test_marc.xml";
        //        error_file_name = "test_marc_last_error.html";
        //    }

        //    // Before doing this, create the Mango load
        //    try
        //    {
        //        // Create the Mango load stuff
        //        Console.WriteLine("Building " + feed_name);
        //        MarcXML_Load_Creator createEndeca = new MarcXML_Load_Creator();
        //        bool reportSuccess = createEndeca.Create_MarcXML_Data_File(Test_Feed_Flag, Path.Combine(logFileDirectory, file_name));

        //        // Publish this feed
        //        if (reportSuccess)
        //        {
        //            Engine_Database.Builder_Clear_Item_Error_Log(feed_name.ToUpper(), "", "UFDC Builder");
        //            File.Copy(Path.Combine(logFileDirectory, file_name), Engine_ApplicationCache_Gateway.Settings.MarcGeneration.MarcXML_Feed_Location + file_name, true);
        //        }
        //        else
        //        {
        //            string errors = createEndeca.Errors;
        //            if (errors.Length > 0)
        //            {
        //                StreamWriter writer = new StreamWriter(Engine_ApplicationCache_Gateway.Settings.MarcGeneration.MarcXML_Feed_Location + error_file_name, false);
        //                writer.WriteLine("<html><head><title>" + feed_name + " Errors</title></head><body><h1>" + feed_name + " Errors</h1>");
        //                writer.Write(errors.Replace("\r\n","<br />").Replace("\n","<br />").Replace("<br />", "<br />\r\n"));
        //                writer.Write("</body></html>");
        //                writer.Flush();
        //                writer.Close();

        //                Engine_Database.Builder_Add_Log_Entry(-1, feed_name.ToUpper(), "Error", "Resulting file failed validation", "");

        //                File.Copy(Path.Combine(logFileDirectory, file_name), Engine_ApplicationCache_Gateway.Settings.MarcGeneration.MarcXML_Feed_Location + file_name.Replace(".xml", "_error.xml"), true);
        //            }
        //        }
        //    }
        //    catch 
        //    {
        //        Engine_Database.Builder_Add_Log_Entry(-1, feed_name.ToUpper(), "Error", "Unknown exception caught", "");

        //        Console.WriteLine("ERROR BUILDING THE " + feed_name.ToUpper());
        //    }
        //}

        #endregion

        #region Code to read registry values

        private static string Look_For_Variable_Registry_Key(string Manufacturer, string KeyName)
        {
            RegistryKey localKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            localKey = localKey.OpenSubKey(Manufacturer);
            if (localKey != null)
            {
                string[] subkeys = localKey.GetSubKeyNames();
                foreach (string thisSubKey in subkeys)
                {
                    RegistryKey subKey = localKey.OpenSubKey(thisSubKey);
                    if (subKey != null) {
                        string value64 = subKey.GetValue(KeyName) as string;
                        if (!String.IsNullOrEmpty(value64))
                            return value64;
                    }
                }
            }
            RegistryKey localKey32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
            localKey32 = localKey32.OpenSubKey(Manufacturer);
            if (localKey32 != null)
            {
                string[] subkeys = localKey32.GetSubKeyNames();
                foreach (string thisSubKey in subkeys)
                {
                    RegistryKey subKey = localKey32.OpenSubKey(thisSubKey);
                    if (subKey != null) {
                        string value32 = subKey.GetValue(KeyName) as string;
                        if (!String.IsNullOrEmpty(value32))
                            return value32;
                    }
                }
            }
            return null;
        }

        #endregion

    }
}
