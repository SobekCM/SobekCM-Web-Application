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
    /// pulling them from the item's currently-published METS, and re-saves the merged result to the
    /// incoming METS file's own location before it moves on to the image server </summary>
    /// <remarks> This class implements the <see cref="abstractSubmissionPackageModule" /> abstract class and implements the <see cref="iSubmissionPackageModule" /> interface.<br /><br />
    /// A METADATA_UPDATE submission carries only updated descriptive metadata -- its METS has no
    /// structMap of its own, and <see cref="Incoming_Digital_Resource.Move"/> already replaced the
    /// item's on-disk resource folder with that bare incoming folder before the per-item module chain
    /// runs. Without this module, everything downstream (database save, service METS, Solr/index) would
    /// persist the item with its file list and structure map wiped out. <br /><br />
    /// Must run BEFORE <see cref="MoveFilesToImageServerModule"/> (and therefore before
    /// <see cref="ReloadMetsAndBasicDbInfoModule"/>, which runs after it). This module merges the
    /// structure map into <see cref="Incoming_Digital_Resource.Metadata"/> and then immediately
    /// re-saves it, via <see cref="Incoming_Digital_Resource.Save_SobekCM_Service_METS"/>, to the same
    /// "BibID_VID.mets.xml" path the incoming METS was just read from (still sitting in the processing
    /// folder at this point) -- so the file MoveFilesToImageServerModule renames to "recd_....mets.bak"
    /// and carries into the final folder is already the complete, merged METS, and
    /// ReloadMetsAndBasicDbInfoModule's later unconditional <see cref="Incoming_Digital_Resource.Load_METS()"/>
    /// re-read picks up the correct content rather than the bare metadata-only original. <br /><br />
    /// Reads the currently-published service METS for this BibID/VID via <see cref="SobekFileSystem.Ensure_Local_Copy"/>,
    /// which transparently downloads it from GCS first only when there is no permanent local copy already
    /// (GCS Full mode) -- a no-op download in Local or GCS Hybrid mode, where the service METS always has
    /// a permanent local copy. </remarks>
    public class SaveStructMapModule : abstractSubmissionPackageModule
    {
        /// <summary> Restores the structure map and main thumbnail reference onto a METADATA_UPDATE
        /// package, pulled from the item's currently-published METS, and re-saves the merged result
        /// back to the incoming METS file's own (processing-folder) location </summary>
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

            // Remember the incoming METS's own path -- Save_SobekCM_Service_METS always writes the
            // canonical "BibID_VID.mets.xml" name, which may differ from it (e.g. a depositor-provided
            // "BibID_VID.mets" or "BibID.mets.xml")
            string incomingMetsPath = Resource.METS_File;

            // Re-save the merged item back over the incoming METS, in the processing folder it was
            // just read from, so the file MoveFilesToImageServerModule moves up already has the
            // complete structure map on it
            if (!Resource.Save_SobekCM_Service_METS())
            {
                OnError("Error saving the merged METS for " + Resource.BibID + ":" + Resource.VID, Resource.BibID + ":" + Resource.VID, Resource.METS_Type_String, Resource.BuilderLogId);
                Tracer?.Add_Trace("SaveStructMapModule.DoWork", "Error saving the merged METS", Custom_Trace_Type_Enum.Error);
                return false;
            }

            // If the incoming file used some other name, remove it now that the canonical
            // "BibID_VID.mets.xml" holds the merged content -- otherwise both would get carried
            // up to the image server together
            string canonicalMetsPath = Path.Combine(Resource.Resource_Folder, Resource.BibID + "_" + Resource.VID + ".mets.xml");
            if ((!String.IsNullOrEmpty(incomingMetsPath)) && (String.Compare(incomingMetsPath, canonicalMetsPath, StringComparison.OrdinalIgnoreCase) != 0) && (File.Exists(incomingMetsPath)))
            {
                File.Delete(incomingMetsPath);
            }

            return true;
        }
    }
}
