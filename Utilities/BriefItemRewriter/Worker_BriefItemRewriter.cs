#region Using directives

using SobekCM.Builder_Library;
using SobekCM.Builder_Library.Modules.Items;
using SobekCM.Builder_Library.Settings;
using SobekCM.Core.FileSystems;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.Settings;
using SobekCM.Engine_Library.ApplicationState;
using SobekCM.Engine_Library.Database;
using SobekCM.Engine_Library.Items.BriefItems;
using SobekCM.Engine_Library.Settings;
using SobekCM.Resource_Object;
using SobekCM.Resource_Object.Configuration;
using SobekCM.Tools;
using SobekCM_Resource_Database;
using System;
using System.Data;
using System.IO;

#endregion

namespace BriefItemRewriter
{
    /// <summary> Pulls the full item list from the database and, for every item whose resource folder
    /// still exists, runs <see cref="ReloadMetsAndBasicDbInfoModule"/> (reads the METS + DB-sourced
    /// behavior info into a fresh <see cref="SobekCM_Item"/>) followed by <see cref="SaveProtobufCacheFile"/>
    /// (maps that item to a <see cref="SobekCM.Core.BriefItem.BriefItemInfo"/> and writes/overwrites its
    /// cache.protobuf) -- i.e. the exact same two modules the real Builder runs for an existing item, just
    /// driven directly against every item in the database instead of waiting for each one to be
    /// reprocessed via the AdditionalWorkNeeded flag. </summary>
    /// <remarks> Same shape as <c>Worker_Reindexer</c> in the sibling SolrReindexer utility -- pull the
    /// settings/item list from the database, then call builder modules' DoWork directly instead of going
    /// through the full Builder_Modules/Get_Submission_Module machinery, since only these two specific
    /// modules are needed here. </remarks>
    public class Worker_BriefItemRewriter
    {
        private readonly Single_Instance_Configuration instanceInfo;
        private InstanceWide_Settings settings;

        public Worker_BriefItemRewriter(Single_Instance_Configuration InstanceInfo)
        {
            instanceInfo = InstanceInfo;
        }

        /// <summary> Refresh the settings and configuration from the database, needed before any items
        /// can be processed </summary>
        /// <returns> TRUE if successful, otherwise FALSE </returns>
        public bool Refresh_Settings()
        {
            Engine_Database.Connection_String = instanceInfo.DatabaseConnection.Connection_String;
            SobekCM_Item_Database.Connection_String = instanceInfo.DatabaseConnection.Connection_String;

            settings = InstanceWide_Settings_Builder.Build_Settings(instanceInfo.DatabaseConnection);
            if (settings == null)
            {
                Console.WriteLine("Unable to pull the newest settings from the database");
                return false;
            }

            AppRoot_Gateway.AppRootPath = settings.Servers.Application_Server_Network;

            Engine_ApplicationCache_Gateway.Settings.Database_Connection.Connection_String = instanceInfo.DatabaseConnection.Connection_String;
            Engine_ApplicationCache_Gateway.Settings.Database_Connection.Database_Type = EngineAgnosticLayerDbAccess.EalDbTypeEnum.MSSQL;

            // Touching .Configuration.Metadata is what actually triggers the lazy load of every local
            // config file (including the brief item mapping config that BriefItem_Factory needs), not
            // just the metadata configuration specifically
            Engine_ApplicationCache_Gateway.Configuration.Metadata.Finalize_Metadata_Configuration();
            ResourceObjectSettings.MetadataConfig = Engine_ApplicationCache_Gateway.Configuration.Metadata;

            SobekFileSystem.Initialize(
    Engine_ApplicationCache_Gateway.Settings?.Servers?.Image_Server_Network ?? "",
    Engine_ApplicationCache_Gateway.Settings?.Servers?.Image_URL ?? "");


            return true;
        }

        /// <summary> Runs ReloadMetsAndBasicDbInfoModule then SaveProtobufCacheFile against every item in
        /// the database whose resource folder exists on disk </summary>
        public void Rebuild_Cache_For_All_Items()
        {
            Console.WriteLine("Compiling the whole protobuf-net model in place...");
            BriefItem_Cache.CompileProtobufModel();

            Console.WriteLine("Pulling the list of all items from the database...");
            DataSet itemSet = Engine_Database.Item_List(true, null);

            var reloadModule = new ReloadMetsAndBasicDbInfoModule { Settings = settings };
            var cacheModule = new SaveProtobufCacheFile { Settings = settings };

            var tracer = new Custom_Tracer();

            int total = itemSet.Tables[0].Rows.Count;
            int completed = 0;
            int succeeded = 0;
            int missingFolder = 0;
            int failed = 0;

            DateTime start = DateTime.Now;

            string logFile = "missing_stuff.txt";

            foreach (DataRow thisRow in itemSet.Tables[0].Rows)
            {
                string bibid = thisRow["BibID"].ToString();
                string vid = thisRow["VID"].ToString();
                completed++;

                string resourceFolder = SobekCM_Item.Directory_From_Bib_VID(settings.Servers.Image_Server_Network, bibid, vid);
                if (!Directory.Exists(resourceFolder))
                {
                    Console.WriteLine(bibid + ":" + vid + " - missing resource folder ( " + completed + " out of " + total + " )");
                    missingFolder++;
                    File.AppendAllText(logFile, bibid + ":" + vid + " - missing resource folder\n");
                    continue;
                }

                string mets = Path.Combine(resourceFolder, bibid + "_" + vid + ".mets.xml");
                if ( !File.Exists(mets))
                {
                    Console.WriteLine(bibid + ":" + vid + " - missing mets ( " + completed + " out of " + total + " )");
                    missingFolder++;
                    File.AppendAllText(logFile, bibid + ":" + vid + " - missing mets\n");
                    continue;
                }

                Console.WriteLine(bibid + ":" + vid + " - rebuilding cache ( " + completed + " out of " + total + " )");

                var resource = new Incoming_Digital_Resource(resourceFolder, null);

                try
                {
                    if ((!reloadModule.DoWork(resource, tracer)) || (!cacheModule.DoWork(resource, tracer)))
                    {
                        Console.WriteLine("     FAILED");
                        failed++;
                        Console.ReadKey();
                        continue;
                    }
                }
                catch (Exception ee)
                {
                    Console.WriteLine($"Error processing {bibid}:{vid}");
                    Console.ReadKey();
                }

                succeeded++;
            }

            TimeSpan span = DateTime.Now.Subtract(start);

            Console.WriteLine();
            Console.WriteLine($"Completed in {span.TotalSeconds} seconds");
            Console.WriteLine($"  {succeeded} succeeded, {missingFolder} missing resource folder, {failed} failed, {total} total");
        }
    }
}
