using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.Win32;
using SolrServiceMonitor.Settings;
using SolrServiceMonitor.Utilities;

namespace SolrMonitorModels.Java
{
    /// <summary> Wrapper handles the Java checks prior to starting solr and also for 
    /// checking the registry for the current version and Java home </summary>
    public static class JavaWrapper
    {
        /// <summary> Look for the Java executable information, and determine the version and the ability of 
        /// this version to support different capabilities </summary>
        /// <returns> TRUE if this version will work with solr/lucene </returns>
        public static bool Check_Java()
        {
            // Log some of this work
            StringBuilder loggingBuilder = new StringBuilder();

            // Was the Java executable hard coded in the settings?
            string java_executable;
            if (!String.IsNullOrEmpty(ServiceSettings.Java.Home))
            {
                string java_home = ServiceSettings.Java.Home;
                loggingBuilder.AppendLine("Found Java Home in the configuration file: " + java_home);

                // Do the directory and java executable exist?
                java_executable = Path.Combine(java_home, "bin", "java.exe");
                if ((!Directory.Exists(java_home)) || (!File.Exists(java_executable)))
                {
                    loggingBuilder.AppendLine("ERROR: Java.exe not found in " + java_home + "\\bin.  Please set Java.Home to a valid JRE/JDK directory in the configuration file.");

                    // For chuckles, see if the java information could be pulled from the registry
                    string regCheckError2;
                    string javaFromRegistry2 = Java_Executable_From_Registry(out regCheckError2);
                    if (!String.IsNullOrEmpty(javaFromRegistry2))
                    {
                        loggingBuilder.AppendLine("From the registry, Java appears to be at: " + javaFromRegistry2);
                        loggingBuilder.AppendLine("If you remove the Java home in the configuration file, this Java executable will be used.");
                    }

                    // Log this critical error and return FALSE
                    EventLogHelper.Write_Error(ServiceSettings.Logging.FailedStartEventId, loggingBuilder.ToString());
                    return false;
                }
            }
            else
            {
                // Look in the registry for the Java location
                string regCheckError;
                java_executable = Java_Executable_From_Registry(out regCheckError);
                if (String.IsNullOrEmpty(java_executable))
                {
                    loggingBuilder.AppendLine("ERROR: Unable to determine location of the Java executable from the registry.");
                    loggingBuilder.AppendLine(regCheckError);

                    loggingBuilder.AppendLine();
                    loggingBuilder.AppendLine("Please set Java.Home to a valid JRE/JDK directory in the configuration file.");

                    // Log this critical error and return FALSE
                    EventLogHelper.Write_Error(ServiceSettings.Logging.FailedStartEventId, loggingBuilder.ToString());
                    return false;
                }

                // (Potentially) log this
                loggingBuilder.AppendLine("Found Java executable in the registry: " + java_executable);
            }

            // Valid, so save this
            Java_Executable = java_executable;
            Is64bit = true;

            // This should now be a valid Java executable, so check for version and capability for the -server flag
            SimpleJavaResponse output = Run_Java_Executable("-version");
            if (output.ExitCode != 0)
            {
                loggingBuilder.AppendLine();
                loggingBuilder.AppendLine("UNKNOWN ERROR: Error running the java exectuable to determine version.");
                loggingBuilder.AppendLine( "'" + Java_Executable + " -version' fails with exit code of not zero.");

                // Log this critical error and return FALSE
                EventLogHelper.Write_Error(ServiceSettings.Logging.FailedStartEventId, loggingBuilder.ToString());
                return false;
            }

            // Parse for the build / version
            if (output.Response.IndexOf(" version ", StringComparison.OrdinalIgnoreCase) < 0)
            {
                loggingBuilder.AppendLine();
                loggingBuilder.AppendLine("UNKNOWN ERROR: Output from java version check is invalid.");
                loggingBuilder.AppendLine("'" + Java_Executable + " -version' output is not as expected.");

                // Log this critical error and return FALSE
                EventLogHelper.Write_Error(ServiceSettings.Logging.FailedStartEventId, loggingBuilder.ToString());
                return false;
            }

            string version_info;
            try
            {
                version_info = output.Response.Substring(output.Response.IndexOf(" version ", StringComparison.OrdinalIgnoreCase) + 8).Split("\r\n".ToCharArray())[0].Trim().Replace("\"","");
                string[] split = version_info.Split(".".ToCharArray());
                Java_Version = Int32.Parse(split[1]);
                Java_Build = -1;
                if (version_info.IndexOf("_") > 0)
                {
                    string java_build_string = version_info.Split("_".ToCharArray())[1];
                    Java_Build = Int32.Parse(java_build_string);
                }

                // Add to the log
                loggingBuilder.AppendLine();
                loggingBuilder.AppendLine("Java version is " + version_info + " ( Major version " + Java_Version + ", build " + Java_Build + " )");
            }
            catch
            {
                loggingBuilder.AppendLine();
                loggingBuilder.AppendLine("UNKNOWN ERROR: Output from java version check is invalid.");
                loggingBuilder.AppendLine("'" + Java_Executable + " -version' output is not as expected.");

                // Log this critical error and return FALSE
                EventLogHelper.Write_Error(ServiceSettings.Logging.FailedStartEventId, loggingBuilder.ToString());
                return false;
            }

            // Java must be version 1.7 or later to run Solr
            if (Java_Version < 7)
            {
                loggingBuilder.AppendLine();
                loggingBuilder.AppendLine("ERROR: Java 1.7 or later is required to run Solr.  Current Java version is: " + version_info );

                // Log this critical error and return FALSE
                EventLogHelper.Write_Error(ServiceSettings.Logging.FailedStartEventId, loggingBuilder.ToString());
                return false;
            }

            // Keep track of if a warning should be logged during this start-up
            bool warning_included = false;

            // Set some GC_TUNE value differently if this is version 7
            if (Java_Version == 7)
            {
                if ( ServiceSettings.StartArgs.GcTune.IndexOf("-XX:CMSFullGCsBeforeCompaction=1", StringComparison.Ordinal) < 0 ) 
                    ServiceSettings.StartArgs.GcTune = ServiceSettings.StartArgs.GcTune + " -XX:CMSFullGCsBeforeCompaction=1";
                if (ServiceSettings.StartArgs.GcTune.IndexOf("-XX:CMSTriggerPermRatio=80", StringComparison.Ordinal) < 0)
                    ServiceSettings.StartArgs.GcTune = ServiceSettings.StartArgs.GcTune + " -XX:CMSTriggerPermRatio=80";

                // Some builds don't work well with Lucene
                if ((Java_Build >= 40) && (Java_Build <= 51))
                {
                    if (ServiceSettings.StartArgs.GcTune.IndexOf("-XX:-UseSuperWord", StringComparison.Ordinal) < 0)
                        ServiceSettings.StartArgs.GcTune = ServiceSettings.StartArgs.GcTune + " -XX:-UseSuperWord";

                    // Log this to warn 
                    warning_included = true;
                    loggingBuilder.AppendLine();
                    loggingBuilder.AppendLine("WARNING: Java version " + version_info + " has known bugs with Lucene and requires the -XX:-UseSuperWord flag.");
                    loggingBuilder.AppendLine("Please consider upgrading your JVM.");
                }
            }

            // Check to see if this version of java supports the -server VM flag
            SimpleJavaResponse output_server = Run_Java_Executable("-server -version");
            if (output_server.ExitCode != 0)
            {
                warning_included = true;
                loggingBuilder.AppendLine();
                loggingBuilder.AppendLine("WARNING: You are using a JRE without support for -server option.  Please update to latest JDK for best performance.");
                ServerFlagSupported = false;
            }
            else
            {
                ServerFlagSupported = true;
            }

            SimpleJavaResponse output_x64 = Run_Java_Executable("-d64 -version");
            if (output_x64.ExitCode != 0)
            {
                warning_included = true;
                loggingBuilder.AppendLine();
                loggingBuilder.AppendLine("WARNING: 32-bit Java detected.  Not recommended for production.  Update your version or enter the path to the appropriate Java version in the configuration file.");
                Is64bit = false;
            }

            // If a warning was set
            if (warning_included)
            {
                EventLogHelper.Write_Error(ServiceSettings.Logging.StartEventId, "JAVA WARNING" + Environment.NewLine +  loggingBuilder);
            }

            return true;
        }

        private static SimpleJavaResponse Run_Java_Executable(string Arguments)
        {
            // Using string builders to collect the standard output and error
            StringBuilder output = new StringBuilder();
            StringBuilder error = new StringBuilder();
            int exitCode = -1;


            // Perform all this work within using tag to encourage disposing of the process and events
            using (Process startSolrProcess = new Process())
            using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
            using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
            {

                startSolrProcess.StartInfo.FileName = Java_Executable;
                startSolrProcess.StartInfo.Arguments = Arguments;

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
                    {
                        error.AppendLine("     " + e.Data);
                    }
                };

                // Start the process
                startSolrProcess.Start();

                // Start the asyncronous reading of the standard input and standard output
                startSolrProcess.BeginOutputReadLine();
                startSolrProcess.BeginErrorReadLine();

                // Wait for the process to copmlete
                if (startSolrProcess.WaitForExit(2000))
                {
                    // Give the output readers slightly more time to finish any reading
                    if (outputWaitHandle.WaitOne(100) && errorWaitHandle.WaitOne(100))
                    {
                        // Successfully ended and process completed.  Check process exit code here.
                        exitCode = startSolrProcess.ExitCode;
                    }
                }
                else
                {
                    // Timed out.
                    startSolrProcess.Close();
                }

                // Ensure the output and error read events are not fired again
                try
                {
                    startSolrProcess.CancelOutputRead();
                }
                catch {  }

                try
                {
                    startSolrProcess.CancelErrorRead();
                }
                catch { }
                

            }

            return new SimpleJavaResponse(exitCode, (output + Environment.NewLine + error).Trim());

        }



        private static string Java_Executable_From_Registry( out string ErrorMessage )
        {

            ErrorMessage = null;

            try
            {
                // Try to get the current version number first
                string registryVersionNumber = get_registry_value(@"SOFTWARE\JavaSoft\Java Runtime Environment", "CurrentVersion") as string;
                if (String.IsNullOrEmpty(registryVersionNumber))
                {
                    ErrorMessage =
                        "Unable to find current Java version information in the registry at HKLM\\Software\\JavaSoft\\Java Runtime Environment\\CurrentVersion." +
                        Environment.NewLine +
                        "If Java is installed, you can enter the path and filename into the configuration file under Java.Executable.";
                    return null;
                }

                // Now, with the version number, try to find the current java home
                string javaHome = get_registry_value(@"Software\JavaSoft\Java Runtime Environment\" + registryVersionNumber, "JavaHome") as string;
                if (String.IsNullOrEmpty(javaHome))
                {
                    ErrorMessage =
                        "Unable to find current Java home information in the registry at HKLM\\Software\\JavaSoft\\Java Runtime Environment\\" + registryVersionNumber + "\\JavaHome." +
                        Environment.NewLine +
                        "If Java is installed, you can enter the path and filename into the configuration file under Java.Executable.";
                    return null;
                }

                // Now, add Java.exe to the end of it and check if the file exists
                string returnValue = Path.Combine(javaHome, "bin", "Java.exe");
                if (!File.Exists(returnValue))
                {
                    ErrorMessage = "Java information from the registry is invalid.  Unable to find " + returnValue +
                                    Environment.NewLine +
                                    "If Java is installed, you can enter the path and filename into the configuration file under Java.Executable.";
                    return null;
                }

                // Everything appears valid!  Return the java executable
                return returnValue;
            }
            catch (Exception ee)
            {
                ErrorMessage = "Error attempting to read the Java values from the registry." + Environment.NewLine + ee.Message;
                return null;
            }
        }

        private static string get_registry_value(string HKLM_Key, string Value)
        {
            RegistryKey localKey = RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, RegistryView.Registry64);
            localKey = localKey.OpenSubKey(HKLM_Key);
            if (localKey != null)
            {
                string value64 = localKey.GetValue(Value) as string;
                if (!String.IsNullOrEmpty(value64))
                    return value64;
            }


            RegistryKey localKey32 = RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, RegistryView.Registry32);
            localKey32 = localKey32.OpenSubKey(HKLM_Key);
            if (localKey32 != null)
            {
                string value32 = localKey32.GetValue(Value) as string;
                if (!String.IsNullOrEmpty(value32))
                    return value32;
            }
            return null;
        }

        /// <summary> Java executable file, as determined by the registry and/or application configuration </summary>
        public static string Java_Executable { get; private set; }

        /// <summary> Major version of the Java executable ( i.e., 7, 8, etc.. ) </summary>
        public static int Java_Version { get; private set; }

        /// <summary> Build number of the Java executable ( i.e., 80, 81, etc.. ) </summary>
        public static int Java_Build { get; private set; }

        /// <summary> Flag indicates if the -server flag is supported by the Java executable </summary>
        public static bool ServerFlagSupported { get; private set; }

        /// <summary> Flag indicates if the Java executable supports 64-bit virtual machines </summary>
        public static bool Is64bit { get; private set; }


    }
}
