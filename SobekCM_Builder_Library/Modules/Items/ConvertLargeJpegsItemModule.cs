using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SobekCM.Builder_Library.Settings;
using SobekCM.Engine_Library.ApplicationState;
using SobekCM.Resource_Object.Utilities;

namespace SobekCM.Builder_Library.Modules.Items
{
    /// <summary> Item-level submission package module that checks the size of the JPEGs and if they are too large, converts them to TIFFs </summary>
    /// <remarks> This class implements the <see cref="abstractSubmissionPackageModule" /> abstract class and implements the <see cref="iSubmissionPackageModule" /> interface. </remarks>
    class ConvertLargeJpegsItemModule : abstractSubmissionPackageModule
    {
        private const int MAX_JPEG_WIDTH = 1000;

        /// <summary> Creates all the image derivative files from original jpeg and tiff files </summary>
        /// <param name="Resource"> Incoming digital resource object </param>
        /// <returns> TRUE if processing can continue, FALSE if a critical error occurred which should stop all processing </returns>
        public override bool DoWork(Incoming_Digital_Resource Resource)
        {
            string resourceFolder = Resource.Resource_Folder;
            string[] all_jpegs = Directory.GetFiles(resourceFolder, "*.jpg");

            // Check each JPEG
            FileStream reuseStream = null;
            foreach (string thisJpeg in all_jpegs)
            {
                // Exclude thumbnails
                if (thisJpeg.IndexOf("thm.jpg", StringComparison.InvariantCultureIgnoreCase) > 0) continue;

                string extension = Path.GetExtension(thisJpeg);
                string name = Path.GetFileName(thisJpeg);



                // Check the size
                // Load the JPEG
                try
                {
                    Image jpegSourceImg = SafeImageFromFile(thisJpeg, ref reuseStream);
                    if ((jpegSourceImg.Width > Engine_ApplicationCache_Gateway.Settings.Resources.JPEG_Maximum_Width) || (jpegSourceImg.Height > Engine_ApplicationCache_Gateway.Settings.Resources.JPEG_Maximum_Height))
                    {
                        // Copy the JPEG
                        string final_destination = Path.Combine(resourceFolder, Engine_ApplicationCache_Gateway.Settings.Resources.Backup_Files_Folder_Name);
                        if (!Directory.Exists(final_destination))
                            Directory.CreateDirectory(final_destination);
                        string copy_file = final_destination + "\\" + name.Replace(extension, "") + "_ORIG.jpg";
                        File.Copy(thisJpeg, copy_file, true);

                        // Determine the final name
                        string tiff_file = resourceFolder + "\\" + name.Replace(extension, "") + ".tif";

                        // Create the TIFF                        
                        jpegSourceImg.Save(tiff_file, ImageFormat.Tiff);

                        // Delete the original JPEG file
                        File.Delete(thisJpeg);
                    }
                }
                catch (Exception ee)
                {
                    // If the pixel format is strange, we can't use the .NET image way...
                    if (ee.Message == "A Graphics object cannot be created from an image that has an indexed pixel format.")
                    {

                        try
                        {
                            Image jpegSourceImg = EmptyImageFromFile(thisJpeg);
                            if ((jpegSourceImg.Width > Engine_ApplicationCache_Gateway.Settings.Resources.JPEG_Maximum_Width) || (jpegSourceImg.Height > Engine_ApplicationCache_Gateway.Settings.Resources.JPEG_Maximum_Height))
                            {
                                // Don't need this anymore
                                jpegSourceImg = null;

                                string imagemagick_executable = MultiInstance_Builder_Settings.ImageMagick_Executable;
                                if ((!String.IsNullOrEmpty(imagemagick_executable)) && (File.Exists(imagemagick_executable)))
                                {
                                    // Copy the JPEG
                                    string final_destination = Path.Combine(resourceFolder, Engine_ApplicationCache_Gateway.Settings.Resources.Backup_Files_Folder_Name);
                                    if (!Directory.Exists(final_destination))
                                        Directory.CreateDirectory(final_destination);
                                    string copy_file = final_destination + "\\" + name.Replace(extension, "") + "_ORIG.jpg";
                                    File.Copy(thisJpeg, copy_file, true);

                                    // Determine the final name
                                    string tiff_file = resourceFolder + "\\" + name.Replace(extension, "") + ".tif";



                                    // Create the TIFF via ImageMagick
                                    if (Image_Derivative_Creation_Processor.ImageMagick_Create_TIFF(imagemagick_executable, thisJpeg, tiff_file))
                                    {
                                        // Delete the original JPEG file
                                        File.Delete(thisJpeg);
                                    }
                                    else
                                    {
                                        OnError("Error saving new TIFF from the large JPEG in ConvertLargeJpegItemModule (using ImageMagick)", Resource.BibID + ":" + Resource.VID, Resource.METS_Type_String, Resource.BuilderLogId);
                                        return true;
                                    }
                                }
                            }
                        }
                        catch ( Exception ee_inner )
                        {
                            OnError("Error checking JPEG in ConvertLargeJpegItemModule (using ImageMagick): " + ee_inner.Message, Resource.BibID + ":" + Resource.VID, Resource.METS_Type_String, Resource.BuilderLogId);
                            return true;
                        }

                    }
                    else
                    {
                        OnError("Error checking JPEG in ConvertLargeJpegItemModule : " + ee.Message, Resource.BibID + ":" + Resource.VID, Resource.METS_Type_String, Resource.BuilderLogId);
                        return true;
                    }
                }
            }

            return true;
        }

        #region Method to return an image after closing connectio to the file

        private static Image SafeImageFromFile(string FilePath, ref FileStream ReuseStream)
        {
            // http://stackoverflow.com/questions/18250848/how-to-prevent-the-image-fromfile-method-to-lock-the-file
            Bitmap img;
            ReuseStream = new FileStream(FilePath, FileMode.Open, FileAccess.Read);
            using (Bitmap b = new Bitmap(ReuseStream))
            {
                img = new Bitmap(b.Width, b.Height, b.PixelFormat);

                try
                {
                    using (Graphics g = Graphics.FromImage(img))
                    {
                        g.DrawImage(b, 0, 0, img.Width, img.Height);
                        g.Flush();
                    }
                }
                catch (Exception ee )
                {
                    img = null;
                    ReuseStream.Close();
                    throw ee;
                }
            }

            ReuseStream.Close();
            return img;
        }

        private static Image EmptyImageFromFile(string FilePath)
        {
            Bitmap img;
            using (FileStream ReuseStream = new FileStream(FilePath, FileMode.Open, FileAccess.Read))
            {
                using (Bitmap b = new Bitmap(ReuseStream))
                {
                    img = new Bitmap(b.Width, b.Height, b.PixelFormat);
                }

                ReuseStream.Close();
            }
            return img;
        }

        #endregion

    }
}
