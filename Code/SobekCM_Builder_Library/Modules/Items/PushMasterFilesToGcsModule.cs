#region Using directives

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SobekCM.Core.FileSystems;
using SobekCM.Resource_Object.Behaviors;
using SobekCM.Resource_Object.Configuration;

using SobekCM.Tools;
#endregion

namespace SobekCM.Builder_Library.Modules.Items
{
    /// <summary> Item-level submission package module uploads master/derivative image files to GCS and
    /// removes the local scratch copy, in GCS Hybrid mode </summary>
    /// <remarks> This class implements the <see cref="abstractSubmissionPackageModule" /> abstract class and implements the <see cref="iSubmissionPackageModule" /> interface.<br /><br />
    /// No-op in Local mode. Must run after every module that still needs to read real master-image bytes
    /// off local disk (JPEG2000/JPEG conversion, OCR, thumbnail/attribute/page-count extraction) and after
    /// METS/marc.xml/the database record/the protobuf cache are all finalized, since by that point nothing
    /// downstream needs the local copy any more. <see cref="Hybrid_FileSystem.IsGcsOnly"/> classifies each
    /// file the same way <see cref="SobekFileSystem.CopyFileIn"/> does internally -- used here only to
    /// decide whether the LOCAL scratch copy is safe to delete afterward (dual-write files, e.g. METS/
    /// thumbnails, keep their local copy; GCS-only master files do not). </remarks>
    public class PushMasterFilesToGcsModule : abstractSubmissionPackageModule
    {
        /// <summary> Number of files uploaded/deleted concurrently for one item. Each is a separate network
        /// round-trip, so this matters a lot for items with many small files (page images especially) --
        /// same reasoning and default as the standalone MigrateSobekFileSystem utility's --threads option.
        /// Builder processes items strictly one at a time (no other parallelism anywhere in this pipeline),
        /// so this doesn't stack with any other concurrency. </summary>
        private const int UploadDegreeOfParallelism = 8;

        /// <summary> Uploads master/derivative image files to GCS and removes the local scratch copy,
        /// in GCS Hybrid mode </summary>
        /// <param name="Resource"> Incoming digital resource object </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> TRUE if processing can continue, FALSE if a critical error occurred which should stop all processing </returns>
        public override bool DoWork(Incoming_Digital_Resource Resource, Custom_Tracer Tracer)
        {
            Tracer?.Add_Trace("PushMasterFilesToGcsModule.DoWork");

            if (Settings.Servers.File_System_Mode != "GCS Hybrid")
                return true;

            try
            {
                // Computed once per item -- TRUE if this item has a registered viewer (website/HTML/
                // OpenTextbook) that resolves other files in its folder via same-origin relative paths,
                // in which case its whole folder must stay local regardless of individual file extensions
                var viewerTypes = new List<string>();
                if (Resource.Metadata?.Behaviors?.Views_Count > 0)
                {
                    foreach (View_Object view in Resource.Metadata.Behaviors.Views)
                        viewerTypes.Add(view.View_Type);
                }
                bool requiresLocalFileBundle = Hybrid_FileSystem.Requires_Local_File_Bundle(viewerTypes);

                var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = UploadDegreeOfParallelism };
                Parallel.ForEach(Directory.GetFiles(Resource.Resource_Folder), parallelOptions, file =>
                {
                    string fileName = Path.GetFileName(file);
                    if (string.Equals(fileName, ResourceObjectSettings.Metadata_Cache_FileName, StringComparison.OrdinalIgnoreCase))
                        return;

                    bool isGcsOnly = Hybrid_FileSystem.IsGcsOnly(fileName, requiresLocalFileBundle);
                    SobekFileSystem.CopyFileIn(file, Resource.BibID, Resource.VID, fileName, RequiresLocalFileBundle: requiresLocalFileBundle);

                    if (isGcsOnly)
                        File.Delete(file);
                });
            }
            catch (AggregateException aee)
            {
                string combined = string.Join("; ", aee.InnerExceptions.Select(inner => inner.Message));
                OnError("Error pushing files to GCS for " + Resource.BibID + ":" + Resource.VID + " : " + combined, Resource.BibID + ":" + Resource.VID, Resource.METS_Type_String, Resource.BuilderLogId);
                Tracer?.Add_Trace("PushMasterFilesToGcsModule.DoWork", "Error pushing files to GCS: " + combined, Custom_Trace_Type_Enum.Error);
                return false;
            }
            catch (Exception ee)
            {
                OnError("Error pushing files to GCS for " + Resource.BibID + ":" + Resource.VID + " : " + ee.Message, Resource.BibID + ":" + Resource.VID, Resource.METS_Type_String, Resource.BuilderLogId);
                Tracer?.Add_Trace("PushMasterFilesToGcsModule.DoWork", "Error pushing files to GCS: " + ee.Message, Custom_Trace_Type_Enum.Error);
                return false;
            }

            return true;
        }
    }
}
