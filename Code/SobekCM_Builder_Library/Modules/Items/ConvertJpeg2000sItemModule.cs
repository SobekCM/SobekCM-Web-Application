using System;
using System.IO;
using System.Reflection;
using SobekCM.Builder_Library.Settings;
using SobekCM.Resource_Object.Utilities;

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

        /// <summary> Creates the missing TIFF and JPEG derivatives for any JPEG2000 that doesn't already have a JPEG </summary>
        /// <param name="Resource"> Incoming digital resource object </param>
        /// <returns> TRUE if processing can continue, FALSE if a critical error occurred which should stop all processing </returns>
        public override bool DoWork(Incoming_Digital_Resource Resource)
        {
            returnValue = true;

            string resourceFolder = Resource.Resource_Folder;
            string imagemagick_executable = MultiInstance_Builder_Settings.ImageMagick_Executable;

            // Without ImageMagick there is no way to produce the JPEG/thumbnail regardless of
            // whether Kakadu is available, same hard requirement as the TIFF-sourced modules.
            if (String.IsNullOrEmpty(imagemagick_executable))
                return returnValue;

            string[] jp2_files = Directory.GetFiles(resourceFolder, "*.jp2");
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

            foreach (string jp2File in jp2_files)
            {
                var jp2FileInfo = new FileInfo(jp2File);
                string name_sans_extension = jp2FileInfo.Name.Replace(jp2FileInfo.Extension, "");
                string expected_jpg = Path.Combine(resourceFolder, name_sans_extension + ".jpg");

                // Already has its JPEG - not this module's job (matches the "for JPEG2000s
                // without any accompanying JPEGs" scope this module exists to cover).
                if (File.Exists(expected_jpg))
                    continue;

                OnProcess("\t\tConverting JPEG2000 '" + jp2FileInfo.Name + "'", "Image Processing", packageName, String.Empty, Resource.BuilderLogId);

                string tiff_file = Path.Combine(resourceFolder, name_sans_extension + ".tif");
                if ((!File.Exists(tiff_file)) || (File.GetLastWriteTime(tiff_file).CompareTo(File.GetLastWriteTime(jp2File)) < 0))
                    imageProcessor.Create_TIFF_From_JPEG2000(jp2File, tiff_file, Resource.BuilderLogId, packageName);

                if (!File.Exists(tiff_file))
                {
                    OnError("Unable to create TIFF from JPEG2000 '" + jp2FileInfo.Name + "' in ConvertJpeg2000sItemModule", packageName, Resource.METS_Type_String, Resource.BuilderLogId);
                    returnValue = false;
                    continue;
                }

                // Create the JPEG + thumbnail from the newly-decoded TIFF, same ImageMagick path
                // the TIFF-sourced modules use.
                imageProcessor.Image_Magick_Process_TIFF_File(tiff_file, name_sans_extension, resourceFolder, true, Settings.Resources.JPEG_Width, Settings.Resources.JPEG_Height, Resource.BuilderLogId, packageName);

                // Register the new TIFF + JPEG + thumbnail with the resource, same pattern as
                // CreateImageDerivativesLegacyModule's post-processing file registration loop.
                string[] matching_files = Directory.GetFiles(resourceFolder, name_sans_extension + ".*");
                foreach (string derivativeFile in matching_files)
                {
                    var derivativeFileInfo = new FileInfo(derivativeFile);
                    if (Settings.System.Page_Image_Extensions.Contains(derivativeFileInfo.Extension.ToUpper().Replace(".", "")))
                        Resource.NewImageFiles.Add(derivativeFileInfo.Name);
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
                returnValue = false;
            }
        }
    }
}
