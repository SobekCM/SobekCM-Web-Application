#region Using directives

using SobekCM.Builder_Library.Settings;
using SobekCM.Engine_Library.Database;
using System;
using System.IO;
using System.Reflection;
using SobekCM_Resource_Database;

#endregion

namespace BriefItemRewriter
{
    /// <summary> Reads configuration, verifies a single active instance is configured, and hands off to
    /// <see cref="Worker_BriefItemRewriter"/> to do the actual work </summary>
    /// <remarks> Same shape as <c>Solr_Reindexer_Controller</c> in the sibling SolrReindexer utility --
    /// a teeny slimmed-down stand-in for the real Builder's controller/worker split, just enough to run
    /// a couple of specific builder modules against every item in the database. </remarks>
    internal class BriefItemRewriter_Controller
    {
        private string configurationFile;
        private Single_Instance_Configuration instance;
        private Worker_BriefItemRewriter worker;

        public BriefItemRewriter_Controller()
        {
            Console.WriteLine("BriefItemRewriter_Controller.Constructor");

            string startupDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            MultiInstance_Builder_Settings.Builder_Executable_Directory = startupDirectory;
        }

        private bool Configure_To_Run()
        {
            Console.WriteLine("BriefItemRewriter_Controller.Configure_To_Run");

            configurationFile = Path.Combine(MultiInstance_Builder_Settings.Builder_Executable_Directory ?? String.Empty, "config", "sobekcm.config");
            if (!File.Exists(configurationFile))
            {
                Console.WriteLine("Configuration file is missing: " + configurationFile);
                return false;
            }

            if (!MultiInstance_Builder_Settings_Reader.Read_Config(configurationFile))
            {
                Console.WriteLine("Error encountered reading the configuration file: " + configurationFile);
                return false;
            }

            // Save the single active instance
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
                        Console.WriteLine("Can only list one active instance in the configuration file");
                        return false;
                    }
                }
            }

            if ((instance == null) || (String.IsNullOrEmpty(instance.DatabaseConnection.Connection_String)))
            {
                Console.WriteLine("No active instance with a valid connection string was found in the configuration file");
                return false;
            }

            Console.WriteLine("Using instance: " + instance.Name);

            Engine_Database.Connection_String = instance.DatabaseConnection.Connection_String;
            SobekCM_Item_Database.Connection_String = instance.DatabaseConnection.Connection_String;

            worker = new Worker_BriefItemRewriter(instance);

            if (!worker.Refresh_Settings())
            {
                Console.WriteLine(instance.Name + " - Error pulling settings/configuration information");
                return false;
            }

            return true;
        }

        /// <summary> Reads configuration and, if valid, runs the cache rebuild against every item in the database </summary>
        public void Execute()
        {
            Console.WriteLine("Entering BriefItemRewriter_Controller.Execute");

            if (!Configure_To_Run())
                return;

            worker.Rebuild_Cache_For_All_Items();
        }
    }
}
