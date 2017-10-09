using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SobekCM.Engine_Library.ApplicationState;

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

                        // Create the TIFF
                        string tiff_file = resourceFolder + "\\" + name.Replace(extension, "") + ".tif";
                        jpegSourceImg.Save(tiff_file, ImageFormat.Tiff);

                        // Delete the original JPEG file
                        File.Delete(thisJpeg);
                    }
                }
                catch (Exception ee)
                {
                    OnError("Error checking JPEG in ConvertLargeJpegItemModule : " + ee.Message, Resource.BibID + ":" + Resource.VID, Resource.METS_Type_String, Resource.BuilderLogId);
                    return true;
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
                using (Graphics g = Graphics.FromImage(img))
                {
                    g.DrawImage(b, 0, 0, img.Width, img.Height);
                    g.Flush();
                }
            }
            ReuseStream.Close();
            return img;
        }

        #endregion

    }
}
