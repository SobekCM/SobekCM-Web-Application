#region Using directives

using System;
using SobekCM.Core.FileSystems;

using SobekCM.Tools;
#endregion

namespace SobekCM.Builder_Library.Modules.Items
{
    /// <summary> Item-level submission package module pulls an existing item's files back down from GCS
    /// into the local resource folder before reprocessing, in GCS Hybrid or GCS Full mode </summary>
    /// <remarks> This class implements the <see cref="abstractSubmissionPackageModule" /> abstract class and implements the <see cref="iSubmissionPackageModule" /> interface.<br /><br />
    /// No-op in Local mode, and no-op for a brand-new item (nothing in GCS yet to pull down). Must run
    /// FIRST in the per-item module chain, before any module that opens/reads a master image file --
    /// every conversion/OCR/thumbnail module downstream is an external tool that needs real bytes on
    /// local disk, none of them can operate against a GCS object directly. Under GCS Full mode this also
    /// pulls down METS/marc.xml/thumbnails, which (unlike Hybrid mode) have no permanent local copy either. <br /><br />
    /// Deliberately always re-downloads every file on every reprocess, even a metadata-only one -- no
    /// attempt is made here to detect and skip a metadata-only reprocess request. A future release is
    /// adding a processing-type table that will make that distinction possible; revisit this module then. <br /><br />
    /// <see cref="SobekFileSystem.DownloadAll"/> skips any file that already exists in <see cref="Incoming_Digital_Resource.Resource_Folder"/>
    /// rather than overwriting it -- that folder is the incoming submission's own working folder, so it may
    /// already hold a depositor's replacement for a file this item also has in GCS (a corrected page image,
    /// an updated METS, etc). This only fills in whatever the incoming submission didn't already provide. </remarks>
    public class StageResourceFilesLocallyModule : abstractSubmissionPackageModule
    {
        /// <summary> Pulls an existing item's files back down from GCS into the local resource folder
        /// before reprocessing, in GCS Hybrid or GCS Full mode </summary>
        /// <param name="Resource"> Incoming digital resource object </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> TRUE if processing can continue, FALSE if a critical error occurred which should stop all processing </returns>
        public override bool DoWork(Incoming_Digital_Resource Resource, Custom_Tracer Tracer)
        {
            Tracer?.Add_Trace("StageResourceFilesLocallyModule.DoWork");

            if ((Settings.Servers.File_System_Mode != "GCS Hybrid") && (Settings.Servers.File_System_Mode != "GCS Full"))
                return true;

            if (Resource.NewPackage)
                return true;

            try
            {
                SobekFileSystem.DownloadAll(Resource.BibID, Resource.VID, Resource.Resource_Folder);
            }
            catch (Exception ee)
            {
                OnError("Error staging files from GCS for " + Resource.BibID + ":" + Resource.VID + " : " + ee.Message, Resource.BibID + ":" + Resource.VID, Resource.METS_Type_String, Resource.BuilderLogId);
                Tracer?.Add_Trace("StageResourceFilesLocallyModule.DoWork", "Error staging files from GCS: " + ee.Message, Custom_Trace_Type_Enum.Error);
                return false;
            }

            return true;
        }
    }
}
