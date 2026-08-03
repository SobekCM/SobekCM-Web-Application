#region Using directives

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SobekCM.Builder_Library.Settings;
using SobekCM.Engine_Library.Database;
using SobekCM.Resource_Object.Divisions;
using SobekCM_Resource_Database;

using SobekCM.Tools;
#endregion

namespace SobekCM.Builder_Library.Modules.Items
{
    /// <summary> Item-level submission package module ensures a main thumbnail has been selected for this digital resource </summary>
    /// <remarks> This class implements the <see cref="abstractSubmissionPackageModule" /> abstract class and implements the <see cref="iSubmissionPackageModule" /> interface. </remarks>
    public class EnsureMainThumbnailModule : abstractSubmissionPackageModule
    {
        /// <summary> Ensures a main thumbnail has been selected for this digital resource </summary>
        /// <param name="Resource"> Incoming digital resource object </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> TRUE if processing can continue, FALSE if a critical error occurred which should stop all processing </returns>
        public override bool DoWork(Incoming_Digital_Resource Resource, Custom_Tracer Tracer)
        {
            Tracer?.Add_Trace("EnsureMainThumbnailModule.DoWork");

            bool hasNoMainThumbnailListed = (Resource.Metadata.Behaviors.Main_Thumbnail.Length == 0);
            string mainThumbnail = Resource.Metadata.Behaviors.Main_Thumbnail ?? string.Empty;

            // You can technically reference a web resource as the thumbnail
            if ((mainThumbnail.IndexOf("http:") >= 0) || (mainThumbnail.IndexOf("https:") >= 0))
                return true;

            // If this image does not have thm.jpg, but there IS a matching thm.jpg, that probably should be the thumbnail
            bool thumbnailOfThumbnailExists = (mainThumbnail.IndexOf(".jpg") > 0) && (mainThumbnail.IndexOf("thm.jpg") < 0) &&
                (File.Exists(Path.Combine(Resource.Resource_Folder, mainThumbnail.Replace(".jpg", "thm.jpg"))));

            // Ensure a thumbnail is attached
            if (hasNoMainThumbnailListed || thumbnailOfThumbnailExists ||
                !File.Exists(Path.Combine(Resource.Resource_Folder, Resource.Metadata.Behaviors.Main_Thumbnail)))
            {
                // Look for a valid thumbnail
                Resource.Metadata.Behaviors.Main_Thumbnail = GetThumbnail(Resource, Tracer, thumbnailOfThumbnailExists);

                // Should this be saved?
                if ((Resource.Metadata.Web.ItemID > 0) && (Resource.Metadata.Behaviors.Main_Thumbnail.Length > 0))
                {
                    SobekCM_Item_Database.Set_Item_Main_Thumbnail(Resource.BibID, Resource.VID, Resource.Metadata.Behaviors.Main_Thumbnail);
                }
            }

            return true;
        }

        private string GetThumbnail(Incoming_Digital_Resource Resource, Custom_Tracer Tracer, bool thumbnailOfThumbnailExists)
        {
            // This is the default main thumbnail image file name
            if (File.Exists(Path.Combine(Resource.Resource_Folder, "mainthm.jpg")))
                return Resource.Metadata.Behaviors.Main_Thumbnail = "mainthm.jpg";

            // If somehow the non thumbnail image was used here, convert to the thumbnail
            if ( thumbnailOfThumbnailExists)
                return Resource.Metadata.Behaviors.Main_Thumbnail.Replace(".jpg", "thm.jpg");

            // Look for a thumbnail image
            string[] jpeg_files = Directory.GetFiles(Resource.Resource_Folder, "*thm.jpg");
            if (jpeg_files.Length > 0)
                return (new FileInfo(jpeg_files[0])).Name;

            // Look for multimediat type thumbnail
            if (Resource.Metadata.Divisions.Page_Count == 0)
            {
                List<SobekCM_File_Info> downloads = Resource.Metadata.Divisions.Download_Other_Files;
                foreach (SobekCM_File_Info thisDownloadFile in downloads)
                {
                    string mimetype = thisDownloadFile.MIME_Type(thisDownloadFile.File_Extension).ToUpper();
                    if ((mimetype.IndexOf("AUDIO") >= 0) || (mimetype.IndexOf("VIDEO") >= 0))
                    {
                        if (File.Exists(Path.Combine(MultiInstance_Builder_Settings.Builder_Executable_Directory, "images\\multimedia.jpg")))
                        {
                            File.Copy(Path.Combine(MultiInstance_Builder_Settings.Builder_Executable_Directory, "images\\multimedia.jpg"), Path.Combine(Resource.Resource_Folder, "multimediathm.jpg"), true);
                            return "multimediathm.jpg";
                        }
                        break;
                    }
                }
            }

            return Resource.Metadata.Behaviors.Main_Thumbnail ?? string.Empty;
        }
    }
}
