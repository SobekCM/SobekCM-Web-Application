#region Using directives

using System;
using System.Text;
using SobekCM.Engine_Library.ApplicationState;
using SobekCM.Builder_Library;

#endregion

namespace SobekCM.Builder
{
    public class Program
    {
 
        static void Main(string[] Args)
        {
            bool show_help = false;
            string invalid_arg = String.Empty;
            bool verbose = false;
            bool run_once_only = false;

	        // Get values from the arguments
            foreach (string thisArgs in Args)
            {
                bool arg_handled = false;

                // Check for versioning option
                if (thisArgs == "--version")
                {
                    Console.WriteLine("You are running version " + Engine_ApplicationCache_Gateway.Settings.Static.Current_Builder_Version + " of the SobekCM Builder.");
                    return;
                }

                // Running in BACKGROUND mode is the default
                if (thisArgs == "--background")
                {
                    arg_handled = true;
                }

                // Argument says to only run once, then exit
                if (thisArgs == "--once")
                {

                    run_once_only = true;
                    arg_handled = true;
                }

                // Check for verbose flag
                if (thisArgs == "--verbose")
                {
                    verbose = true;
                    arg_handled = true;
                }

                // Check for no loader flag
                if (thisArgs == "--refresh_oai")
                {
                    arg_handled = true;
                }

                // Check for help
                if ((thisArgs == "--help") || (thisArgs == "?") || (thisArgs == "-help"))
                {
                    show_help = true;
                    arg_handled = true;
                }

                // If not handled, set as error
                if (!arg_handled)
                {
                    invalid_arg = thisArgs;
                    break;
                }
            }

            // Was there an invalid argument or was help requested
            if ((invalid_arg.Length > 0 ) || (show_help))
            {
                StringBuilder builder = new StringBuilder();
                builder.Append("\nThis application is used to bulk load SobekCM items, perform post-processing\n");
                builder.Append("for items loaded through the web, and perform some regular maintenance activities\n");
                builder.Append("in support of a SobekCM web application.\n\n");
                builder.Append("Usage: SobekCM_Builder [options]\n\n");
                builder.Append("Options:\n\n");
                builder.Append("  --version\tDisplays the current version of the SobekCM Builder\n\n");
                builder.Append("  --verbose\tFlag indicates to be verbose in the logs and console\n\n");
                builder.Append("  --help\t\tShows these instructions\n\n");
                builder.Append("  --once\t\tRuns one time only, then closes (rather than polling in background mode)\n\n");


                // If invalid arg, save to log file
                if (invalid_arg.Length > 0 )
                {
                    // Show INVALID ARGUMENT error in console
                    Console.WriteLine("\nINVALID ARGUMENT PROVIDED ( " + invalid_arg + " )");
                }

                Console.WriteLine(builder.ToString());
                return;
            }
            

            // Controller always runs in background mode
            Worker_Controller controller = new Worker_Controller(verbose );
            controller.Execute(run_once_only);

            // If this was set to aborting, set to last execution aborted
            Builder_Operation_Flag_Enum operationFlag = Abort_Database_Mechanism.Builder_Operation_Flag;
            if (( operationFlag == Builder_Operation_Flag_Enum.ABORTING ) || ( operationFlag == Builder_Operation_Flag_Enum.ABORT_REQUESTED ))
                Abort_Database_Mechanism.Builder_Operation_Flag = Builder_Operation_Flag_Enum.LAST_EXECUTION_ABORTED;

#if DEBUG
            Console.WriteLine();
            Console.WriteLine("Detected DEBUG mode.. press any key to continue");
            Console.ReadKey();
#endif
        }
    }
}
