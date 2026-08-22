#region Using directives

using System;
using System.Data;
using System.IO;
using System.Linq;
using SobekCM.Builder_Library.Tools;
using SobekCM.Engine_Library.Database;
using SobekCM.Engine_Library.Items;

using SobekCM.Tools;
#endregion

namespace SobekCM.Builder_Library.Modules.Items
{
    /// <summary> Item-level submission package module saves all of the digital resource information to the database </summary>
    /// <remarks> This class implements the <see cref="abstractSubmissionPackageModule" /> abstract class and implements the <see cref="iSubmissionPackageModule" /> interface. </remarks>
    public class SaveToDatabaseModule : abstractSubmissionPackageModule
    {
        /// <summary> Saves all of the digital resource information to the database </summary>
        /// <param name="Resource"> Incoming digital resource object </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> TRUE if processing can continue, FALSE if a critical error occurred which should stop all processing </returns>
        public override bool DoWork(Incoming_Digital_Resource Resource, Custom_Tracer Tracer)
        {
            Tracer?.Add_Trace("SaveToDatabaseModule.DoWork");

            // Determine total size on the disk
            string[] all_files_final = Directory.GetFiles(Resource.Resource_Folder);
            double size = all_files_final.Sum(ThisFile => (double)(((new FileInfo(ThisFile)).Length) / 1024));
            Resource.DiskSpaceMb = size;

            // Also, set the TextSearchable flag correctly
            string[] text_files = File_System_Tools.GetFiles(Resource.Resource_Folder, "*.txt");
            bool page_image_text_found = false;
            foreach (string thisFile in text_files)
            {
                // Is this text from a PAGE IMAGE (jpeg or jp2) file?
                string filename_sans_extension = Path.GetFileNameWithoutExtension(thisFile);
                string possible_jpeg = Path.Combine(Resource.Resource_Folder, filename_sans_extension + ".jpg");
                string possible_jp2 = Path.Combine(Resource.Resource_Folder, filename_sans_extension + ".jpg");
                if ((File.Exists(possible_jp2)) || (File.Exists(possible_jpeg)))
                {
                    page_image_text_found = true;
                    break;
                }
            }
            Resource.Metadata.Behaviors.Text_Searchable = page_image_text_found;

            // Do not save the viewers here, since the default will be used for NEW items and
            // no change for existing items
            Resource.Metadata.Behaviors.Clear_Views();

            // Save this package to the database
            if (!Resource.Save_to_Database(Resource.NewPackage, Settings))
            {
                OnError("Error saving data to SobekCM database.  The database may not reflect the most recent data in the METS.", Resource.BibID + ":" + Resource.VID, Resource.METS_Type_String, Resource.BuilderLogId);
                return true;
            }

            // Views were cleared above prior to saving (since new items get their defaults from the
            // database and existing items are otherwise left unchanged).  For NEW items in particular,
            // this save is the first time an ItemID/GroupID and a raft of other database-only defaults
            // (group title/type, restriction message, made-public date, PURL, etc.) come into existence --
            // none of that gets set on the in-memory Resource.Metadata by the earlier reload step, since
            // ReloadMetsAndBasicDbInfoModule only pulls database info for existing items.  Reuse the exact
            // same finalize logic the web/engine item-loading path uses to merge all of that back onto the
            // in-memory item now that it is saved, so the rest of the pipeline (e.g. the cache written out
            // at the end of processing) reflects reality for both new and existing items alike.
            DataSet itemDetails = Engine_Database.Get_Item_Details(Resource.BibID, Resource.VID, Tracer);
            if (itemDetails != null)
            {
                bool multipleVolumesExist = (itemDetails.Tables.Count > 2) && (itemDetails.Tables[2].Rows.Count > 0) && (Convert.ToInt32(itemDetails.Tables[2].Rows[0]["Total_Volumes"]) > 1);
                new SobekCM_METS_Based_ItemBuilder().Finish_Building_Item(Resource.Metadata, itemDetails, multipleVolumesExist, Tracer);
            }
            else
            {
                Tracer?.Add_Trace("SaveToDatabaseModule.DoWork", "Unable to pull item details from the database for " + Resource.BibID + ":" + Resource.VID, Custom_Trace_Type_Enum.Error);
            }

            return true;
        }
    }
}
