#region Using directives

using System;
using System.IO;
using SobekCM.Core.FileSystems;
using SobekCM.Resource_Object;

using SobekCM.Tools;
#endregion

namespace SobekCM.Builder_Library.Modules.Items
{
    /// <summary> Item-level submission package module restores the structure map (physical, download,
    /// and open-textbook division trees) and main thumbnail reference onto a METADATA_UPDATE package,
    /// pulling them from the item's currently-published METS </summary>
    /// <remarks> This class implements the <see cref="abstractSubmissionPackageModule" /> abstract class and implements the <see cref="iSubmissionPackageModule" /> interface.<br /><br />
    /// A METADATA_UPDATE submission carries only updated descriptive metadata -- its METS has no
    /// structMap of its own, and <see cref="Incoming_Digital_Resource.Move"/> already replaced the
    /// item's on-disk resource folder with that bare incoming folder before the per-item module chain
    /// runs. Without this module, everything downstream (database save, service METS, Solr/index) would
    /// persist the item with its file list and structure map wiped out. <br /><br />
    /// Must run before <see cref="MoveFilesToImageServerModule"/>, <see cref="SaveToDatabaseModule"/>, and
    /// <see cref="SaveServiceMetsModule"/> -- all of them act on whatever is currently on
    /// <see cref="Incoming_Digital_Resource.Metadata"/> as the new permanent record for this item. <br /><br />
    /// Reads the currently-published service METS for this BibID/VID via <see cref="SobekFileSystem.Ensure_Local_Copy"/>,
    /// which transparently downloads it from GCS first only when there is no permanent local copy already
    /// (GCS Full mode) -- a no-op download in Local or GCS Hybrid mode, where the service METS always has
    /// a permanent local copy. </remarks>
    public class SaveStructMapModule : abstractSubmissionPackageModule
    {
        /// <summary> Restores the structure map and main thumbnail reference onto a METADATA_UPDATE
        /// package, pulled from the item's currently-published METS </summary>
        /// <param name="Resource"> Incoming digital resource object </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> TRUE if processing can continue, FALSE if a critical error occurred which should stop all processing </returns>
        public override bool DoWork(Incoming_Digital_Resource Resource, Custom_Tracer Tracer)
        {
            Tracer?.Add_Trace("SaveStructMapModule.DoWork");

            // Only a METADATA_UPDATE package's METS lacks its own structure map
            if (!Resource.METS_Only_Package)
                return true;

            // A brand-new item has no prior METS to pull a structure map from
            if (Resource.NewPackage)
                return true;

            string metsFileName = Resource.BibID + "_" + Resource.VID + ".mets.xml";

            string activeMetsPath;
            try
            {
                activeMetsPath = SobekFileSystem.Ensure_Local_Copy(Resource.BibID, Resource.VID, metsFileName);
            }
            catch (Exception ee)
            {
                OnError("Error retrieving the active METS for " + Resource.BibID + ":" + Resource.VID + " : " + ee.Message, Resource.BibID + ":" + Resource.VID, Resource.METS_Type_String, Resource.BuilderLogId);
                Tracer?.Add_Trace("SaveStructMapModule.DoWork", "Error retrieving the active METS: " + ee.Message, Custom_Trace_Type_Enum.Error);
                return false;
            }

            if (String.IsNullOrEmpty(activeMetsPath) || !File.Exists(activeMetsPath))
            {
                OnError("Unable to find the active METS for " + Resource.BibID + ":" + Resource.VID + " to pull the structure map from", Resource.BibID + ":" + Resource.VID, Resource.METS_Type_String, Resource.BuilderLogId);
                Tracer?.Add_Trace("SaveStructMapModule.DoWork", "Unable to find the active METS to pull the structure map from", Custom_Trace_Type_Enum.Error);
                return false;
            }

            SobekCM_Item activeItem = SobekCM_Item.Read_METS(activeMetsPath);
            if (activeItem == null)
            {
                OnError("Unable to read the active METS for " + Resource.BibID + ":" + Resource.VID + " to pull the structure map from", Resource.BibID + ":" + Resource.VID, Resource.METS_Type_String, Resource.BuilderLogId);
                Tracer?.Add_Trace("SaveStructMapModule.DoWork", "Unable to read the active METS to pull the structure map from", Custom_Trace_Type_Enum.Error);
                return false;
            }

            // Replace the (empty) division trees on the incoming metadata-only METS with the
            // existing item's physical, download, and open-textbook trees
            Resource.Metadata.Divisions.Physical_Tree.Roots.Clear();
            Resource.Metadata.Divisions.Physical_Tree.Roots.AddRange(activeItem.Divisions.Physical_Tree.Roots);

            Resource.Metadata.Divisions.Download_Tree.Roots.Clear();
            Resource.Metadata.Divisions.Download_Tree.Roots.AddRange(activeItem.Divisions.Download_Tree.Roots);

            Resource.Metadata.Divisions.OpenTextbook_Tree.Roots.Clear();
            Resource.Metadata.Divisions.OpenTextbook_Tree.Roots.AddRange(activeItem.Divisions.OpenTextbook_Tree.Roots);

            // Retain the main thumbnail reference as well -- a metadata-only METS has none of its own
            Resource.Metadata.Behaviors.Main_Thumbnail = activeItem.Behaviors.Main_Thumbnail;

            return true;
        }
    }
}
