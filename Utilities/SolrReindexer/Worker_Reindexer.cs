#region Using directives

using SobekCM.Builder_Library;
using SobekCM.Builder_Library.Modules;
using SobekCM.Builder_Library.Modules.Folders;
using SobekCM.Builder_Library.Modules.Items;
using SobekCM.Builder_Library.Modules.PostProcess;
using SobekCM.Builder_Library.Modules.PreProcess;
using SobekCM.Builder_Library.Modules.Schedulable;
using SobekCM.Builder_Library.Settings;
using SobekCM.Core.Builder;
using SobekCM.Core.Client;
using SobekCM.Core.Configuration.Extensions;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.MicroservicesClient;
using SobekCM.Core.Settings;
using SobekCM.Engine_Library.ApplicationState;
using SobekCM.Engine_Library.Database;
using SobekCM.Engine_Library.Settings;
using SobekCM.Resource_Object.Configuration;
using SobekCM.Tools;
using SobekCM.Tools.Logs;
using SobekCM_Resource_Database;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using SobekCM.Resource_Object;

#endregion

namespace Solr_Reindexer
{

    public class Worker_Reindexer
    {

        private Builder_Settings builderSettings;
        private Builder_Modules builderModules;
        private readonly Single_Instance_Configuration instanceInfo;
        private InstanceWide_Settings settings;


        private readonly LogFileXhtml logger;


        private bool aborted;
        private bool verbose;
        private bool refreshed;
        private bool firstrun;


        private string finalmessage;

        private readonly List<string> aggregationsToRefresh;
        private readonly List<BibVidStruct> processedItems;
        private readonly List<BibVidStruct> deletedItems;


        private bool stillPendingItems;

        private bool noFoldersReported;


        public Worker_Reindexer(LogFileXhtml Logger, Single_Instance_Configuration InstanceInfo, bool Verbose)
        {
            // Save the log file and verbose flag
            logger = Logger;
            verbose = Verbose;
            instanceInfo = InstanceInfo;



            Add_NonError_To_Log("Worker_BulkLoader.Constructor: Start", verbose, String.Empty, String.Empty, -1);

            // Create new list of collections to build
            aggregationsToRefresh = new List<string>();
            processedItems = new List<BibVidStruct>();
            deletedItems = new List<BibVidStruct>();

            // Set some defaults
            aborted = false;
            refreshed = false;
            noFoldersReported = false;
            firstrun = true;

            Add_NonError_To_Log("Worker_BulkLoader.Constructor: Done", verbose, String.Empty, String.Empty, -1);
        }

        /// <summary> Performs the bulk loader process and handles any incoming digital resources </summary>
        /// <returns> TRUE if there are still pending items to be processed, otherwise FALSE </returns>
        public bool Perform_Reindexing(bool Verbose)
        {
            verbose = Verbose;

            Add_NonError_To_Log("Starting to perform bulk load", verbose, String.Empty, String.Empty, -1);

            // Refresh any settings and item lists 
            if (!Refresh_Settings_And_Item_List())
            {
                Add_Error_To_Log("Error refreshing settings and item list", String.Empty, String.Empty, -1);
                finalmessage = "Error refreshing settings and item list";
                return false;
            }

            Add_NonError_To_Log("Refreshed settings and item list", verbose, String.Empty, String.Empty, -1);

            DataSet itemSet = Engine_Database.Item_List(true, null);

            settings.Servers.Page_Solr_Index_URL = settings.Servers.Page_Solr_Index_URL.Replace("http://10.100.0.3:", "https://10.100.0.10:");
            settings.Servers.Document_Solr_Index_URL = settings.Servers.Document_Solr_Index_URL.Replace("http://10.100.0.3:", "https://10.100.0.10:");

            settings.Servers.Page_Solr_Index_URL = settings.Servers.Page_Solr_Index_URL.Replace("middlesexcc", "middlesex");
            settings.Servers.Document_Solr_Index_URL = settings.Servers.Document_Solr_Index_URL.Replace("middlesexcc", "middlesex");


            var reloadModule = new ReloadMetsAndBasicDbInfoModule();
            reloadModule.Settings = settings;

            var reindexModule = new SaveToSolrLuceneModule_v5();
            reindexModule.Settings = settings;

            DateTime start = DateTime.Now;


            Custom_Tracer tracer = new Custom_Tracer();

            int total_count = itemSet.Tables[0].Rows.Count;
            int completed = 1;


            foreach ( DataRow thisRow in itemSet.Tables[0].Rows)
            {
                string bibid = thisRow["BibID"].ToString();
                string vid = thisRow["VID"].ToString();

                //if (String.CompareOrdinal(bibid, "AA00000425") <= 0)
                //{
                //    Console.WriteLine("Skipping " + bibid + ":" + vid );
                //    completed++;
                //    continue;
                //}

                string resourceFolder = SobekCM_Item.Directory_From_Bib_VID(settings.Servers.Image_Server_Network, bibid, vid);
                if (!Directory.Exists(resourceFolder))
                {
                    Console.WriteLine(bibid + ":" + vid + " - missing resource folder");
                    completed++;
                    continue;
                }

                Console.WriteLine(bibid + ":" + vid + " - indexing ( " + completed + " out of " + total_count + ")");


                Incoming_Digital_Resource resource = new Incoming_Digital_Resource(resourceFolder, null);


                reloadModule.DoWork(resource, tracer);

                reindexModule.DoWork(resource, tracer);

                completed++;

            }

            TimeSpan span = DateTime.Now.Subtract(start);

            Console.WriteLine($"Completed in {span.TotalSeconds} seconds");
            Console.ReadKey();

            Add_NonError_To_Log("Process Done", verbose, String.Empty, String.Empty, -1);

            return stillPendingItems;
        }

        #region Refresh any settings and item lists and clear the item list

        /// <summary> Refresh the settings and item list from the database </summary>
        /// <returns> TRUE if successful, otherwise FALSE </returns>
        public bool Refresh_Settings_And_Item_List()
        {
            // Create the tracer for this
            var tracer = new Custom_Tracer();

            // Disable the cache
            CachedDataManager.Settings.Disabled = true;

            // Set all the database strings appropriately
            Engine_Database.Connection_String = instanceInfo.DatabaseConnection.Connection_String;
            SobekCM_Item_Database.Connection_String = instanceInfo.DatabaseConnection.Connection_String;

            // Get the settings values directly from the database
            settings = InstanceWide_Settings_Builder.Build_Settings(instanceInfo.DatabaseConnection);
            if (settings == null)
            {
                Add_Error_To_Log("Unable to pull the newest settings from the database", String.Empty, String.Empty, -1);
                return false;
            }

            ContentRoot_Gateway.ContentRootPath = settings.Servers.Application_Server_Network;

            // If this was not refreshed yet, ensure [BASEURL] is replaced
            if (!refreshed)
            {
                // Determine the base url
                string baseUrl = String.IsNullOrWhiteSpace(settings.Servers.Base_URL) ? settings.Servers.Application_Server_URL : settings.Servers.Base_URL;
                List<MicroservicesClient_Endpoint> endpoints = instanceInfo.Microservices.Endpoints;
                foreach (MicroservicesClient_Endpoint thisEndpoint in endpoints)
                {
                    if (thisEndpoint.URL.IndexOf("[BASEURL]") > 0)
                        thisEndpoint.URL = thisEndpoint.URL.Replace("[BASEURL]", baseUrl).Replace("//", "/").Replace("http:/", "http://").Replace("https:/", "https://");
                    else if ((thisEndpoint.URL.IndexOf("http:/") < 0) && (thisEndpoint.URL.IndexOf("https:/") < 0))
                        thisEndpoint.URL = (baseUrl + thisEndpoint.URL).Replace("//", "/").Replace("http:/", "http://").Replace("https:/", "https://");
                }
                refreshed = true;
            }

            // Set the microservice endpoints
            SobekEngineClient.Set_Endpoints(instanceInfo.Microservices);

            Engine_ApplicationCache_Gateway.Settings.Database_Connection.Connection_String = instanceInfo.DatabaseConnection.Connection_String;
            Engine_ApplicationCache_Gateway.Settings.Database_Connection.Database_Type = EngineAgnosticLayerDbAccess.EalDbTypeEnum.MSSQL;

            // Finalize the metadata config
            Engine_ApplicationCache_Gateway.Configuration.Metadata.Finalize_Metadata_Configuration();

            // Set the metadata preferences for writing
            ResourceObjectSettings.MetadataConfig = Engine_ApplicationCache_Gateway.Configuration.Metadata;

            // Also, load the builder configuration this way
            try
            {
                builderSettings = SobekEngineClient.Builder.Get_Builder_Settings(false, tracer);
            }
            catch (Exception ee)
            {
                Add_Error_To_Log("Unable to pull the builder settings from the engine", String.Empty, String.Empty, -1);
                Add_Error_To_Log(ee.Message, String.Empty, String.Empty, -1);
                return false;
            }

            return true;
        }

        #endregion


        #region Log-supporting methods

        private long Add_NonError_To_Log(string LogStatement, string DbLogType, string BibID_VID, string MetsType, long RelatedLogID)
        {
            Console.WriteLine(LogStatement);
            logger.AddNonError(LogStatement.Replace("\t", "....."));

            return -1;
        }

        private long Add_NonError_To_Log(string LogStatement, bool IsVerbose, string BibID_VID, string MetsType, long RelatedLogID)
        {
            Console.WriteLine(LogStatement);
            logger.AddNonError(LogStatement.Replace("\t", "....."));

            return -1;
        }

        private long Add_Error_To_Log(string LogStatement, string BibID_VID, string MetsType, long RelatedLogID)
        {
            Console.WriteLine(LogStatement);
            logger.AddError(LogStatement.Replace("\t", "....."));

            return -1;
        }

        private void Add_Error_To_Log(string LogStatement, string BibID_VID, string MetsType, long RelatedLogID, Exception Ee)
        {
            Console.WriteLine(LogStatement);
            logger.AddError(LogStatement.Replace("\t", "....."));

            // Determine, and create the local work space
            string localLogArea = Path.Combine(MultiInstance_Builder_Settings.Builder_Executable_Directory, "logs");

            if (!Directory.Exists(localLogArea))
                Directory.CreateDirectory(localLogArea);

            // Save the exception to an exception file
            var exception_writer = new StreamWriter(Path.Combine(localLogArea, "exceptions_log.txt"), true);
            exception_writer.WriteLine(String.Empty);
            exception_writer.WriteLine(String.Empty);
            exception_writer.WriteLine("----------------------------------------------------------");
            exception_writer.WriteLine("EXCEPTION CAUGHT " + DateTime.Now.ToString() + " BY PRELOADER");
            exception_writer.WriteLine(LogStatement.ToUpper().Replace("\t", "").Trim());
            exception_writer.WriteLine(Ee.ToString());
            exception_writer.Flush();
            exception_writer.Close();
        }

        private void Add_Complete_To_Log(string LogStatement, string DbLogType, string BibID_VID, string MetsType, long RelatedLogID)
        {
            Console.WriteLine(LogStatement);
            logger.AddComplete(LogStatement.Replace("\t", "....."));
        }

        #endregion
    }
}
