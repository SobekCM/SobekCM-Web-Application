#region Using directives

using System;
using System.Diagnostics;
using System.IO;
using SobekCM.Builder_Library.Tools;

using SobekCM.Tools;
#endregion

namespace SobekCM.Builder_Library.Modules.Items
{
    /// <summary> Item-level submission package module performs OCR on the incoming TIFF files to create indexable text </summary>
    /// <remarks> This class implements the <see cref="abstractSubmissionPackageModule" /> abstract class and implements the <see cref="iSubmissionPackageModule" /> interface. </remarks>
    public class OcrTiffsModule : abstractSubmissionPackageModule
    {
        /// <summary> Performs OCR on the incoming TIFF files to create indexable text </summary>
        /// <param name="Resource"> Incoming digital resource object </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> TRUE if processing can continue, FALSE if a critical error occurred which should stop all processing </returns>
        public override bool DoWork(Incoming_Digital_Resource Resource, Custom_Tracer Tracer)
        {
            Tracer?.Add_Trace("OcrTiffsModule.DoWork");

            string resourceFolder = Resource.Resource_Folder;

            // Run OCR for any TIFF files that do not have any corresponding TXT files
            if (Settings.Builder.OCR_Command_Prompt.Length > 0)
            {
                string[] ocr_tiff_files = File_System_Tools.GetFiles(resourceFolder, "*.tif");
                foreach (string thisTiffFile in ocr_tiff_files)
                {
                    var thisTiffFileInfo = new FileInfo(thisTiffFile);
                    string text_file = Path.Combine(resourceFolder, thisTiffFileInfo.Name.Replace(thisTiffFileInfo.Extension, "") + ".txt");
                    if (!File.Exists(text_file))
                    {
                        try
                        {
                            string command = String.Format(Settings.Builder.OCR_Command_Prompt, thisTiffFile, text_file);
                            var ocrProcess = new Process{ StartInfo = { FileName = command } };
                            ocrProcess.Start();
                            ocrProcess.WaitForExit();
                        }
                        catch (Exception ee)
                        {
                            OnError("Error launching OCR on (" + thisTiffFileInfo.Name + ")", Resource.BibID + ":" + Resource.VID, Resource.METS_Type_String, Resource.BuilderLogId);
                            Tracer?.Add_Trace("OcrTiffsModule.DoWork", "Error launching OCR on '" + thisTiffFileInfo.Name + "': " + ee.Message, Custom_Trace_Type_Enum.Error);
                        }
                    }
                }
            }

            return true;
        }
    }
}
