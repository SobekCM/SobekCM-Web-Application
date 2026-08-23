using SobekCM.Builder_Library.Settings;
using SobekCM.Builder_Library.Tools;
using SobekCM.Core.Builder;
using SobekCM.Resource_Object.Utilities;
using SobekCM.Tools;
using System;
using System.IO;
using System.Reflection;

namespace SobekCM.Builder_Library.Modules.Items
{
    /// <summary> Item-level submission package module that creates the missing TIFF and JPEG
    /// derivatives for any JPEG2000 that doesn't already have an accompanying JPEG - the case
    /// where a JP2 master was obtained directly (e.g. harvested from an external IIIF/OAI source)
    /// with nothing else generated for it yet. Decodes via Kakadu's kdu_expand.exe when available,
    /// falling back to ImageMagick otherwise, then reuses the same ImageMagick derivative-creation
    /// path the TIFF-sourced modules use for the JPEG/thumbnail. The resulting TIFF is kept (not a
    /// disposable temp file) since later modules (e.g. OCR) may depend on it. </summary>
    /// <remarks> This class implements the <see cref="abstractSubmissionPackageModule" /> abstract class and implements the <see cref="iSubmissionPackageModule" /> interface. </remarks>
    public class ConvertJpeg2000sItemModule : abstractSubmissionPackageModule
    {
        private bool returnValue;
        private Custom_Tracer tracer;

        /// <summary> Creates the missing TIFF and JPEG derivatives for any JPEG2000 that doesn't already have a JPEG </summary>
        /// <param name="Resource"> Incoming digital resource object </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> TRUE if processing can continue, FALSE if a critical error occurred which should stop all processing </returns>
        public override bool DoWork(Incoming_Digital_Resource Resource, Custom_Tracer Tracer)
        {
            Tracer?.Add_Trace("ConvertJpeg2000sItemModule.DoWork");

            tracer = Tracer;
            returnValue = true;

            string resourceFolder = Resource.Resource_Folder;
            string imagemagick_executable = MultiInstance_Builder_Settings.ImageMagick_Executable;

            // Without ImageMagick there is no way to produce the JPEG/thumbnail regardless of
            // whether Kakadu is available, same hard requirement as the TIFF-sourced modules.
            if (String.IsNullOrEmpty(imagemagick_executable))
                return returnValue;

            string[] jp2_files = File_System_Tools.GetFiles(resourceFolder, "*.jp2");
            if (jp2_files.Length == 0)
                return returnValue;

            string startupPath = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);
            if (startupPath == null)
            {
                OnError("Unable to find the startup path in ConvertJpeg2000sItemModule!", String.Empty, String.Empty, -1);
                return false;
            }

            string kakadu_path = Path.Combine(startupPath, "Kakadu");
            string packageName = Resource.BibID + ":" + Resource.VID;

            // Create_JPEG2000s is deliberately FALSE - the JP2 already exists (that's the whole
            // premise of this module), so the TIFF-processing step below must never regenerate one.
            var imageProcessor = new Image_Derivative_Creation_Processor(imagemagick_executable, kakadu_path, true, false, Settings.Resources.JPEG_Width, Settings.Resources.JPEG_Height, false, Settings.Resources.Thumbnail_Width, Settings.Resources.Thumbnail_Height, null);
            imageProcessor.New_Task_String += imageProcessor_New_Task_String;
            imageProcessor.Error_Encountered += imageProcessor_Error_Encountered;

            // Will keep a processing file for any TIFFs generated
            string procFile = Path.Combine(resourceFolder, "generated_tiffs.proc");
            var procFilePtr = new FileInfo(procFile);

            foreach (string jp2File in jp2_files)
            {
                var jp2FileInfo = new FileInfo(jp2File);
                string name_sans_extension = jp2FileInfo.Name.Replace(jp2FileInfo.Extension, "");

                // Does this TIFF exist?  If so, don't regenerate
                string tiff_file = Path.Combine(resourceFolder, name_sans_extension + ".tif");
                if (File.Exists(tiff_file))
                {
                    continue;
                }

                string expected_jpg = Path.Combine(resourceFolder, name_sans_extension + ".jpg");

                // Already has its JPEG - not this module's job (matches the "for JPEG2000s
                // without any accompanying JPEGs" scope this module exists to cover).
                // If OCR needs to run, we need a good TIFF though.
                if ((Resource.ProcessingFlags.Contains(ProcessingFlag_Constants.RunOcr)) || (File.Exists(expected_jpg)))
                    continue;

                OnProcess("\t\tConverting JPEG2000 '" + jp2FileInfo.Name + "'", "Image Processing", packageName, String.Empty, Resource.BuilderLogId);

                imageProcessor.Create_TIFF_From_JPEG2000(jp2File, tiff_file, Resource.BuilderLogId, packageName);

                // Keep the list of these generated TIFFs in a proc file
                if ( File.Exists(tiff_file))
                {
                    if (!Resource.ProcessingFlags.Contains(ProcessingFlag_Constants.NonMasterTiffs))
                        Resource.ProcessingFlags.Add(ProcessingFlag_Constants.NonMasterTiffs);

                    File.AppendAllText(procFile, name_sans_extension + ".tif" + Environment.NewLine);                   
                }
                else
                {
                    OnError("Unable to create TIFF from JPEG2000 '" + jp2FileInfo.Name + "' in ConvertJpeg2000sItemModule", packageName, Resource.METS_Type_String, Resource.BuilderLogId);
                    Tracer?.Add_Trace("ConvertJpeg2000sItemModule.DoWork", "Unable to create TIFF from JPEG2000 '" + jp2FileInfo.Name + "'", Custom_Trace_Type_Enum.Error);
                    returnValue = false;
                    continue;
                }
            }

            return returnValue;
        }

        void imageProcessor_New_Task_String(string NewMessage, long ParentLogID, string BibID_VID)
        {
            OnProcess(NewMessage, "Image Processing", BibID_VID, String.Empty, ParentLogID);
        }

        void imageProcessor_Error_Encountered(string NewMessage, long ParentLogID, string BibID_VID)
        {
            if (NewMessage.IndexOf("WARNING: ") == 0)
            {
                OnProcess(NewMessage, "Image Processing", BibID_VID, String.Empty, ParentLogID);
            }
            else
            {
                OnError(NewMessage, BibID_VID, String.Empty, ParentLogID);
                tracer?.Add_Trace("ConvertJpeg2000sItemModule.imageProcessor_Error_Encountered", NewMessage, Custom_Trace_Type_Enum.Error);
                returnValue = false;
            }
        }
    }
}
