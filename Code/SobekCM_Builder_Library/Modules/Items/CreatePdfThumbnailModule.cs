#region Using directives

using System;
using System.IO;
using SobekCM.Builder_Library.Settings;
using SobekCM.Builder_Library.Tools;

using SobekCM.Tools;
#endregion

namespace SobekCM.Builder_Library.Modules.Items
{
    /// <summary> Item-level submission package module extracts a thumbnail image from a PDF file </summary>
    /// <remarks> This class implements the <see cref="abstractSubmissionPackageModule" /> abstract class and implements the <see cref="iSubmissionPackageModule" /> interface. </remarks>
    public class CreatePdfThumbnailModule : abstractSubmissionPackageModule
    {
        /// <summary> Extracts a thumbnail image from a PDF file </summary>
        /// <param name="Resource"> Incoming digital resource object </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> TRUE if processing can continue, FALSE if a critical error occurred which should stop all processing </returns>
        public override bool DoWork(Incoming_Digital_Resource Resource, Custom_Tracer Tracer)
        {
            Tracer?.Add_Trace("CreatePdfThumbnailModule.DoWork");

            // Nothing to do for a metadata-only update -- no resource files accompany it
            if (Resource.METS_Only_Package)
                return true;

            string resourceFolder = Resource.Resource_Folder;

            // Get the executable path/file for ghostscript and imagemagick
            string ghostscript_executable = MultiInstance_Builder_Settings.Ghostscript_Executable;
            string imagemagick_executable = MultiInstance_Builder_Settings.ImageMagick_Executable;

            // Preprocess each PDF
            string[] pdfs = File_System_Tools.GetFiles(resourceFolder, "*.pdf");
            foreach (string thisPdf in pdfs)
            {
                // Get the fileinfo and the name
                var thisPdfInfo = new FileInfo(thisPdf);
                string fileName = thisPdfInfo.Name.Replace(thisPdfInfo.Extension, "");

                // Does the thumbnail exist for this item?
                if (( !String.IsNullOrEmpty(ghostscript_executable)) && (!String.IsNullOrEmpty(imagemagick_executable)))
                {
                    if (!File.Exists(Path.Combine(resourceFolder, fileName + "thm.jpg")))
                    {
                        if (!PDF_Tools.Create_Thumbnail(resourceFolder, thisPdf, Path.Combine(resourceFolder, fileName + "thm.jpg"), ghostscript_executable, imagemagick_executable))
                            Tracer?.Add_Trace("CreatePdfThumbnailModule.DoWork", "Unable to create thumbnail for '" + thisPdfInfo.Name + "'", Custom_Trace_Type_Enum.Error);
                    }
                }
            }

            return true;
        }
    }
}
