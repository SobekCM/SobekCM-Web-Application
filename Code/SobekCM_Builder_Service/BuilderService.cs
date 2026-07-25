using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using SobekCM.Builder_Library;
using SobekCM.Builder_Library.Settings;
using SobekCM.Engine_Library.ApplicationState;
using SobekCM.Engine_Library.Database;

namespace SobekCM_Builder_Service
{
    public class BuilderWorker : BackgroundService
    {
        private readonly ILogger<BuilderWorker> _logger;

        public BuilderWorker(ILogger<BuilderWorker> logger)
        {
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Get the application data path
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SobekCM", "Builder");
            try
            {
                if (!Directory.Exists(appDataPath))
                    Directory.CreateDirectory(appDataPath);
            }
            catch (Exception ee)
            {
                _logger.LogError("Error creating the application data folder, which is missing.\n\n{AppDataPath}\n\n{Message}", appDataPath, ee.Message);
                return Task.CompletedTask;
            }

            // Verify the configuration file exists
            string config_file = Path.Combine(appDataPath, "config", "sobekcm.config");
            if (!File.Exists(config_file))
            {
                _logger.LogError("The configuration file is missing. Execution aborted.\n\n{ConfigFile}", config_file);
                return Task.CompletedTask;
            }

            // Assign the connection string and test the connection (if only a single connection listed)
            if (MultiInstance_Builder_Settings.Instances.Count == 1)
            {
                Engine_Database.Connection_String = MultiInstance_Builder_Settings.Instances[0].DatabaseConnection.Connection_String;
                if (!Engine_Database.Test_Connection())
                {
                    if (Engine_Database.Last_Exception != null)
                        _logger.LogError("Unable to connect to the database.\n\n{ConnectionString}\n\n{Message}", Engine_Database.Connection_String, Engine_Database.Last_Exception.Message);
                    else
                        _logger.LogError("Unable to connect to the database.\n\n{ConnectionString}", Engine_Database.Connection_String);
                    return Task.CompletedTask;
                }
            }

            // Verify connectivity and rights on the logs subfolder
            string executableDir = AppContext.BaseDirectory;
            MultiInstance_Builder_Settings.Builder_Executable_Directory = executableDir;
            string logFileDirectory = Path.Combine(executableDir, "logs");
            if (!Directory.Exists(logFileDirectory))
            {
                try
                {
                    Directory.CreateDirectory(logFileDirectory);
                }
                catch
                {
                    _logger.LogError("Error creating necessary logs subfolder. Please create manually.\n\n{LogFileDirectory}", logFileDirectory);
                    return Task.CompletedTask;
                }
            }

            try
            {
                string testFile = Path.Combine(logFileDirectory, "test.log");
                using StreamWriter testWriter = new StreamWriter(testFile, false);
                testWriter.WriteLine("TEST");
                testWriter.Flush();
                testWriter.Close();
                File.Delete(testFile);
            }
            catch
            {
                _logger.LogError("The service account needs modify rights on the logs subfolder. Please create manually.\n\n{LogFileDirectory}", logFileDirectory);
                return Task.CompletedTask;
            }

            // Look for Ghostscript from the registry, if not provided in the config file
            if (Engine_ApplicationCache_Gateway.Settings.Builder.Ghostscript_Executable.Length == 0)
            {
                string possible_ghost = Look_For_Variable_Registry_Key("SOFTWARE\\GPL Ghostscript", "GS_DLL");
                if (!String.IsNullOrEmpty(possible_ghost))
                    Engine_ApplicationCache_Gateway.Settings.Builder.Ghostscript_Executable = possible_ghost;
            }

            // Look for ImageMagick from the registry, if not provided in the config file
            string possible_imagemagick = Look_For_Variable_Registry_Key("SOFTWARE\\ImageMagick", "BinPath");
            if (!String.IsNullOrEmpty(possible_imagemagick))
                Engine_ApplicationCache_Gateway.Settings.Builder.ImageMagick_Executable = possible_imagemagick;

            // Run the builder controller (blocks until complete or service stop)
            var controller = new Worker_Controller(true);
            controller.Execute(false);

            // If this was set to aborting, record the final state
            Builder_Operation_Flag_Enum operationFlag = Abort_Database_Mechanism.Builder_Operation_Flag;
            if ((operationFlag == Builder_Operation_Flag_Enum.ABORTING) || (operationFlag == Builder_Operation_Flag_Enum.ABORT_REQUESTED))
                Abort_Database_Mechanism.Builder_Operation_Flag = Builder_Operation_Flag_Enum.LAST_EXECUTION_ABORTED;

            return Task.CompletedTask;
        }

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
                    string value64 = subKey.GetValue(KeyName) as string;
                    if (!String.IsNullOrEmpty(value64))
                        return value64;
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
                    string value32 = subKey.GetValue(KeyName) as string;
                    if (!String.IsNullOrEmpty(value32))
                        return value32;
                }
            }
            return null;
        }
    }
}
