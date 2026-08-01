#region Using directives

using System.IO;
using SobekCM.Builder_Library.Tools;

using SobekCM.Tools;
#endregion

namespace SobekCM.Builder_Library.Modules.Items
{
    /// <summary> Item-level submission package module extracts indexable text from a PDF file </summary>
    /// <remarks> This class implements the <see cref="abstractSubmissionPackageModule" /> abstract class and implements the <see cref="iSubmissionPackageModule" /> interface. </remarks>
    public class ExtractTextFromPdfModule : abstractSubmissionPackageModule
    {
        /// <summary> Extracts indexable text from a PDF file </summary>
        /// <param name="Resource"> Incoming digital resource object </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> TRUE if processing can continue, FALSE if a critical error occurred which should stop all processing </returns>
        public override bool DoWork(Incoming_Digital_Resource Resource, Custom_Tracer Tracer)
        {
            Tracer?.Add_Trace("ExtractTextFromPdfModule.DoWork");

           // return true;
            string resourceFolder = Resource.Resource_Folder;

            // Preprocess each PDF
            string[] pdfs = Directory.GetFiles(resourceFolder, "*.pdf");
            foreach (string thisPdf in pdfs)
            {
                // Get the fileinfo and the name
                var thisPdfInfo = new FileInfo(thisPdf);
                string fileName = thisPdfInfo.Name.Replace(thisPdfInfo.Extension, "");

                // Does the full text exist for this item?
                if (!File.Exists(resourceFolder + "\\" + fileName + "_pdf.txt"))
                {
                    if (!PDF_Tools.Extract_Text(thisPdf, resourceFolder + "\\" + fileName + "_pdf.txt"))
                        Tracer?.Add_Trace("ExtractTextFromPdfModule.DoWork", "Unable to extract text from '" + thisPdfInfo.Name + "'", Custom_Trace_Type_Enum.Error);
                }
            }

            return true;
        }
    }
}
