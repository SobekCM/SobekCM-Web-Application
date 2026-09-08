using SobekCM.Builder_Library.Settings;
using SobekCM.Builder_Library.Tools;
using SobekCM.Core.Builder;
using SobekCM.Resource_Object.Utilities;
using System;
using System.IO;
using SobekCM.Tools;

namespace SobekCM.Builder_Library.Modules.Items
{
    /// <summary> Item-level submission package module that checks the size of the JPEGs and, if
    /// they are too large, converts them to TIFFs via ImageMagick - matching the non-master TIFF
    /// pattern <see cref="ConvertJpeg2000sItemModule"/> uses, since the oversized JPEG is destroyed
    /// in the process and the TIFF becomes the new master a properly-sized JPEG gets regenerated
    /// from later. Uses ImageMagick (rather than System.Drawing.Common) for both the dimension
    /// check and the TIFF creation, so this works outside Windows. </summary>
    /// <remarks> This class implements the <see cref="abstractSubmissionPackageModule" /> abstract class and implements the <see cref="iSubmissionPackageModule" /> interface. </remarks>
    public class ConvertLargeJpegsItemModule : abstractSubmissionPackageModule
    {
        private bool returnValue;

        /// <summary> Checks the size of each JPEG and, if too large, converts it to a TIFF via ImageMagick </summary>
        /// <param name="Resource"> Incoming digital resource object </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> TRUE if processing can continue, FALSE if a critical error occurred which should stop all processing </returns>
        public override bool DoWork(Incoming_Digital_Resource Resource, Custom_Tracer Tracer)
        {
            Tracer?.Add_Trace("ConvertLargeJpegsItemModule.DoWork");

            returnValue = true;

            // Nothing to do for a metadata-only update -- no resource files accompany it
            if (Resource.METS_Only_Package)
                return returnValue;

            string resourceFolder = Resource.Resource_Folder;
            string imagemagick_executable = MultiInstance_Builder_Settings.ImageMagick_Executable;

            if (String.IsNullOrEmpty(imagemagick_executable))
                return returnValue;

            string[] all_jpegs = File_System_Tools.GetFiles(resourceFolder, "*.jpg");
            if (all_jpegs.Length == 0)
                return returnValue;

            string packageName = Resource.BibID + ":" + Resource.VID;

            // Will keep a processing file for any TIFFs generated
            string procFile = Path.Combine(resourceFolder, "generated_tiffs.proc");

            foreach (string thisJpeg in all_jpegs)
            {
                // Exclude thumbnails
                if (thisJpeg.IndexOf("thm.jpg", StringComparison.InvariantCultureIgnoreCase) > 0) continue;

                var jpegFileInfo = new FileInfo(thisJpeg);
                string name_sans_extension = jpegFileInfo.Name.Replace(jpegFileInfo.Extension, "");

                if (!Image_Derivative_Creation_Processor.ImageMagick_Get_Dimensions(imagemagick_executable, thisJpeg, out int width, out int height))
                {
                    OnError("Unable to determine dimensions of JPEG '" + jpegFileInfo.Name + "' in ConvertLargeJpegsItemModule", packageName, Resource.METS_Type_String, Resource.BuilderLogId);
                    Tracer?.Add_Trace("ConvertLargeJpegsItemModule.DoWork", "ImageMagick was unable to determine the dimensions of '" + jpegFileInfo.Name + "'", Custom_Trace_Type_Enum.Error);
                    continue;
                }

                // Not oversized - nothing to do for this JPEG
                if ((width <= Settings.Resources.JPEG_Maximum_Width) && (height <= Settings.Resources.JPEG_Maximum_Height))
                    continue;

                // Back up the oversized original before it is replaced by a TIFF-derived one
                string backup_dir = Path.Combine(resourceFolder, Settings.Resources.Backup_Files_Folder_Name);
                if (!Directory.Exists(backup_dir))
                    Directory.CreateDirectory(backup_dir);
                string backup_file = Path.Combine(backup_dir, name_sans_extension + "_ORIG.jpg");
                File.Copy(thisJpeg, backup_file, true);

                string tiff_file = Path.Combine(resourceFolder, name_sans_extension + ".tif");

                OnProcess("\t\tConverting large JPEG '" + jpegFileInfo.Name + "'", "Image Processing", packageName, String.Empty, Resource.BuilderLogId);

                if (Image_Derivative_Creation_Processor.ImageMagick_Create_TIFF(imagemagick_executable, thisJpeg, tiff_file))
                {
                    // Delete the original oversized JPEG - the TIFF is now the master
                    File.Delete(thisJpeg);

                    // Keep the list of these generated TIFFs in a proc file
                    if (!Resource.ProcessingFlags.Contains(ProcessingFlag_Constants.NonMasterTiffs))
                        Resource.ProcessingFlags.Add(ProcessingFlag_Constants.NonMasterTiffs);

                    File.AppendAllText(procFile, name_sans_extension + ".tif" + Environment.NewLine);
                }
                else
                {
                    OnError("Unable to create TIFF from large JPEG '" + jpegFileInfo.Name + "' in ConvertLargeJpegsItemModule", packageName, Resource.METS_Type_String, Resource.BuilderLogId);
                    Tracer?.Add_Trace("ConvertLargeJpegsItemModule.DoWork", "ImageMagick was unable to create a TIFF from '" + jpegFileInfo.Name + "'", Custom_Trace_Type_Enum.Error);
                    returnValue = false;
                }
            }

            return returnValue;
        }
    }
}
