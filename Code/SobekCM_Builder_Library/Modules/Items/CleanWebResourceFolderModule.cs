#region Using directives

using System;
using System.IO;
using SobekCM.Builder_Library.Tools;

using SobekCM.Tools;
#endregion

namespace SobekCM.Builder_Library.Modules.Items
{
    /// <summary> Item-level submission package module performs some cleanup on digital resource folders
    /// from previous versions that had some extraneous files and didn't store the backup files in a subfolder </summary>
    /// <remarks> This class implements the <see cref="abstractSubmissionPackageModule" /> abstract class and implements the <see cref="iSubmissionPackageModule" /> interface. </remarks>
    public class CleanWebResourceFolderModule : abstractSubmissionPackageModule
    {
        /// <summary> Performs some cleanup on digital resource folders from previous versions that had some 
        /// extraneous files and didn't store the backup files in a subfolder </summary>
        /// <param name="Resource"> Incoming digital resource object </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> TRUE if processing can continue, FALSE if a critical error occurred which should stop all processing </returns>
        public override bool DoWork(Incoming_Digital_Resource Resource, Custom_Tracer Tracer)
        {
            Tracer?.Add_Trace("CleanWebResourceFolderModule.DoWork");

            try
            {
                // Insure subfolder exists
                string backup_dir = Path.Combine(Resource.Resource_Folder, Settings.Resources.Backup_Files_Folder_Name);
                if (!Directory.Exists(backup_dir))
                {
                    Directory.CreateDirectory(backup_dir);
                }

                // Look for backup mets
                string[] backup_files = File_System_Tools.GetFiles(Resource.Resource_Folder, "*.mets.bak");
                foreach (string thisBackUpFile in backup_files)
                {
                    string name = Path.GetFileName(thisBackUpFile);
                    if (File.Exists(Path.Combine(backup_dir, name)))
                        File.Delete(Path.Combine(backup_dir, name));
                    File.Move(thisBackUpFile, Path.Combine(backup_dir, name));
                }

                // Look for the original mets
                if (File.Exists(Path.Combine(Resource.Resource_Folder, "original.mets.xml")))
                {
                    if (File.Exists(Path.Combine(backup_dir, "original.mets.xml")))
                        File.Delete(Path.Combine(backup_dir, "original.mets.xml"));
                    File.Move(Path.Combine(Resource.Resource_Folder, "original.mets.xml"), Path.Combine(backup_dir, "original.mets.xml"));
                }

                // If the citation_mets.xml file exists, delete that
                if (File.Exists(Path.Combine(Resource.Resource_Folder, "citation_mets.xml")))
                {
                    File.Delete(Path.Combine(Resource.Resource_Folder, "citation_mets.xml"));
                }
            }
            catch (Exception ee)
            {
                // Log as a warning
                OnProcess("WARNING: Unable to perform final cleanup on web folder", "Warning", Resource.BibID + ":" + Resource.VID, Resource.METS_Type_String, Resource.BuilderLogId);
                Tracer?.Add_Trace("CleanWebResourceFolderModule.DoWork", "WARNING: Unable to perform final cleanup on web folder: " + ee.Message, Custom_Trace_Type_Enum.Error);
            }

            return true;
        }
    }
}
