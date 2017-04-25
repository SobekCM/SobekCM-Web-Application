using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SobekCM.Builder_Library.Modules.Items
{
    public class ExternallyLogBuilderWork : abstractSubmissionPackageModule
    {
        public static object logfile_lock;

        /// <summary> Method performs the work of the item-level submission package builder module </summary>
        /// <param name="Resource"> Incoming digital resource object </param>
        /// <returns> TRUE if processing can continue, FALSE if a critical error occurred which should stop all processing </returns>
        public override bool DoWork(Incoming_Digital_Resource Resource)
        {
            try
            {
                OnProcess("Externally logging work", "Standard", Resource.BibID + ":" + Resource.VID, Resource.METS_Type_String, Resource.BuilderLogId);

                // get the log file location
                string start_directory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).Replace("file:\\", "");
                string log_file_directory = Path.Combine(start_directory, "logs");

                if (!Directory.Exists(log_file_directory)) Directory.CreateDirectory(log_file_directory);

                string log_file_name = Path.Combine(log_file_directory, "external_log.txt");

                //lock (logfile_lock)
                //{
                    // Add this info
                    using (StreamWriter writer = new StreamWriter(log_file_name, true))
                    {
                        writer.WriteLine(Resource.BibID + ":" + Resource.VID + " handled at " + DateTime.Now.ToShortDateString());
                        writer.Flush();
                        writer.Close();
                    }
                //}
            }
            catch ( Exception ee )
            {
                OnError("Exception caught while externally logging work : " + ee.Message, Resource.BibID + ":" + Resource.VID, Resource.METS_Type_String, Resource.BuilderLogId);
            }


            return true;

        }
    }
}
