#region Using directives

using System;
using System.IO;
using System.Reflection;
using SobekCM.Builder_Library.Settings;
using SobekCM.Builder_Library.Tools;
using SobekCM.Engine_Library.Email;
using SobekCM.Resource_Object.Utilities;

using SobekCM.Tools;
#endregion

namespace SobekCM.Builder_Library.Modules.Items
{
    /// <summary> Item-level submission package module creates all the image derivative files from original jpeg and tiff files </summary>
    /// <remarks> This class implements the <see cref="abstractSubmissionPackageModule" /> abstract class and implements the <see cref="iSubmissionPackageModule" /> interface. </remarks>
    public class CreateImageDerivativesLegacyModule : abstractSubmissionPackageModule
    {
        private bool returnValue;
        private Custom_Tracer tracer;

        /// <summary> Creates all the image derivative files from original jpeg and tiff files </summary>
        /// <param name="Resource"> Incoming digital resource object </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> TRUE if processing can continue, FALSE if a critical error occurred which should stop all processing </returns>
        public override bool DoWork(Incoming_Digital_Resource Resource, Custom_Tracer Tracer)
        {
            Tracer?.Add_Trace("CreateImageDerivativesLegacyModule.DoWork");

            tracer = Tracer;
            returnValue = true;

            string resourceFolder = Resource.Resource_Folder;
            string bibID = Resource.BibID;
            string vid = Resource.VID;
            string imagemagick_executable = MultiInstance_Builder_Settings.ImageMagick_Executable;


            // Are there images that need to be processed here?
            if (!String.IsNullOrEmpty(imagemagick_executable))
            {
                // Get the list of jpeg and tiff files
                string[] jpeg_files = File_System_Tools.GetFiles(resourceFolder, "*.jpg");
                string[] tiff_files = File_System_Tools.GetFiles(resourceFolder, "*.tif");

                // Only continue if some exist
                if ((jpeg_files.Length > 0) || (tiff_files.Length > 0))
                {
                    string startupPath = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);
                    if (startupPath == null)
                    {
                        OnError("Unable to find the startup path in CreateImageDerivativesModule!", String.Empty, String.Empty, -1);
                        return false;
                    }

                    string kakadu_path = Path.Combine(startupPath, "Kakadu");

                    // Create the image process object for creating 
                    var imageProcessor = new Image_Derivative_Creation_Processor(imagemagick_executable, kakadu_path, true, true, Settings.Resources.JPEG_Width, Settings.Resources.JPEG_Height, false, Settings.Resources.Thumbnail_Width, Settings.Resources.Thumbnail_Height, null);
                    imageProcessor.New_Task_String += imageProcessor_New_Task_String;
                    imageProcessor.Error_Encountered += imageProcessor_Error_Encountered;

                    // Step through the JPEGS and ensure they have thumbnails (TIFF generation below makes them as well)
                    if (jpeg_files.Length > 0)
                    {
                        foreach (string jpegFile in jpeg_files)
                        {
                            var jpegFileInfo = new FileInfo(jpegFile);
                            string name = jpegFileInfo.Name.ToUpper();
                            if ((name.IndexOf("THM.JPG") < 0) && (name.IndexOf(".QC.JPG") < 0))
                            {
                                string name_sans_extension = jpegFileInfo.Name.Replace(jpegFileInfo.Extension, "");
                                if (!File.Exists(Path.Combine(resourceFolder, name_sans_extension + "thm.jpg")))
                                {
                                    imageProcessor.ImageMagick_Create_JPEG(jpegFile, Path.Combine(resourceFolder, name_sans_extension + "thm.jpg"), Settings.Resources.Thumbnail_Width, Settings.Resources.Thumbnail_Height, Resource.BuilderLogId, Resource.BibID + ":" + Resource.VID);
                                }
                            }
                        }
                    }


                    // Step through any TIFFs as well
                    if (tiff_files.Length > 0)
                    {
                        // Do a complete image derivative creation process on these TIFF files
                        imageProcessor.Process(resourceFolder, bibID, vid, tiff_files, Resource.BuilderLogId);

                        // Since we are actually creating page images here (most likely) try to add
                        // them to the package as well
                        foreach (string thisTiffFile in tiff_files)
                        {
                            // Get the name of the tiff file
                            var thisTiffFileInfo = new FileInfo(thisTiffFile);
                            string tiffFileName = thisTiffFileInfo.Name.Replace(thisTiffFileInfo.Extension, "");

                            // Get matching files
                            string[] matching_files = File_System_Tools.GetFiles(resourceFolder, tiffFileName + ".*");

                            // Now, step through all these files
                            foreach (string derivativeFile in matching_files)
                            {
                                // If this is a page image type file, add it
                                var derivativeFileInfo = new FileInfo(derivativeFile);
                                if (Settings.System.Page_Image_Extensions.Contains(derivativeFileInfo.Extension.ToUpper().Replace(".", "")))
                                    Resource.NewImageFiles.Add(derivativeFileInfo.Name);
                            }
                        }
                    }
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
                // Put this in the builder logs
                OnError(NewMessage, BibID_VID, String.Empty, ParentLogID);
                tracer?.Add_Trace("CreateImageDerivativesLegacyModule.imageProcessor_Error_Encountered", NewMessage, Custom_Trace_Type_Enum.Error);

                // Email a message
                string email_address = Settings.Email.System_Error_Email;
                if (String.IsNullOrWhiteSpace(email_address))
                    email_address = Settings.Email.System_Email;
                if (!String.IsNullOrEmpty(email_address))
                {
                    Email_Helper.SendEmail(email_address, "Image Derivation Error : " + BibID_VID, "An error was encountered while creating images for the web from the provided files in the SobekCM Builder service.  Processing of this item will be incomplete.\n\n" + NewMessage + "\n\nPlease review this item and correct the issue, most likely by checking the TIFFs and reloading them.", false, Settings.System.System_Name);
                }

                // This will indicate a failure
                returnValue = false;
            }
        }
    }
}

