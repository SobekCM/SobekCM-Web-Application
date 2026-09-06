using System;
using System.Diagnostics;
using System.IO;
using SobekCM.Builder_Library.Settings;
using SobekCM.Builder_Library.Tools;
using SobekCM.Tools;

namespace SobekCM.Builder_Library.Modules.Items
{
    /// <summary> Item-level submission package module looks for TIFF images without matching text files and
    /// uses Tesseract (if installed) to perform the OCR </summary>
    /// <remarks> This class implements the <see cref="abstractSubmissionPackageModule" /> abstract class 
    /// and implements the <see cref="iSubmissionPackageModule" /> interface. </remarks>
    public class TesseractOcrModule : abstractSubmissionPackageModule
    {
        /// <summary> Looks for TIFF images without matching text files and
        /// uses Tesseract (if installed) to perform the OCR </summary>
        /// <param name="Resource"> Incoming digital resource object </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> TRUE if processing can continue, FALSE if a critical error occurred which should stop all processing </returns>
        public override bool DoWork(Incoming_Digital_Resource Resource, Custom_Tracer Tracer)
        {
            Tracer?.Add_Trace("TesseractOcrModule.DoWork");

            // Nothing to do for a metadata-only update -- no resource files accompany it
            if (Resource.METS_Only_Package)
                return true;

            // If no METS file, skip this
            if (Resource.Metadata == null) return true;

            // Only certain TYPES should even be considered for OCR
            string type = Resource.Metadata.Bib_Info.SobekCM_Type_String;
            if (!type.Equals("book", StringComparison.OrdinalIgnoreCase) &&
                type.IndexOf("text", StringComparison.OrdinalIgnoreCase) != 0)
                return true;

            // Ensure the executable exists
            string tesseract_executable = MultiInstance_Builder_Settings.Tesseract_Executable;

            if (String.IsNullOrEmpty(tesseract_executable)) return true;

            try
            {
                if (!File.Exists(tesseract_executable))
                {
                    OnProcess("Tesseract OCR executable configured, but not present", "Tesseract OCR Module", Resource.BibID + ":" + Resource.VID, Resource.METS_Type_String, Resource.BuilderLogId);
                    return true;
                }
            }
            catch (Exception ee)
            {
                OnProcess("Exception thrown file checking for Tesseract OCR executable existance", "Tesseract OCR Module", Resource.BibID + ":" + Resource.VID, Resource.METS_Type_String, Resource.BuilderLogId);
                Tracer?.Add_Trace("TesseractOcrModule.DoWork", "Exception thrown while checking for Tesseract OCR executable existence: " + ee.Message, Custom_Trace_Type_Enum.Error);
                return true;
            }

            // Tesseract executable is configured and exists
            Tesseract_Processor.Tesseract_Executable = tesseract_executable;

            // Will only use the languag/type and directory information from the package
            string resourceFolder = Resource.Resource_Folder;
            string language = String.Empty;
            if ((Resource.Metadata.Bib_Info.Languages_Count > 0) && (!String.IsNullOrEmpty(Resource.Metadata.Bib_Info.Languages[0].Language_ISO_Code)))
            {
                language = Resource.Metadata.Bib_Info.Languages[0].Language_Text;
            }

            // Look through all the TIFFs
            string[] tiff_files = File_System_Tools.GetFiles(resourceFolder, "*.tif*");
            foreach (string thisTiffFile in tiff_files)
            {

                string textFileName = Path.GetFileNameWithoutExtension(thisTiffFile) + ".txt";
                string textFilePath = Path.Combine(resourceFolder, textFileName);
                string textFileOutputBase = Path.Combine(resourceFolder, Path.GetFileNameWithoutExtension(thisTiffFile));

                // Should this TIFF be processed by Tesseract OCR?
                bool processTiff = false;
                if (!File.Exists(textFilePath))
                {
                    processTiff = true;
                }
                else
                {
                    DateTime textLastModifiedDate = (new FileInfo(textFilePath)).LastWriteTime;
                    DateTime tiffLastModifiedDate = (new FileInfo(thisTiffFile)).LastWriteTime;

                    if (textLastModifiedDate.CompareTo(tiffLastModifiedDate) < 0)
                    {
                        processTiff = true;
                    }
                }

                // Newer TIFF than text, so process
                if (processTiff)
                {
                    // Was this successful?
                    if (!Tesseract_Processor.Process_TIFF(thisTiffFile, textFileOutputBase))
                    {
                        string exception_type = "Unknown Exception";
                        if (!String.IsNullOrEmpty(Tesseract_Processor.Last_Exception))
                            exception_type = Tesseract_Processor.Last_Exception;

                        OnProcess("Tesseract OCR exception on " + Path.GetFileName(thisTiffFile) + ": " + exception_type, "Tesseract OCR Module", Resource.BibID + ":" + Resource.VID, Resource.METS_Type_String, Resource.BuilderLogId);
                        Tracer?.Add_Trace("TesseractOcrModule.DoWork", "Tesseract OCR exception on " + Path.GetFileName(thisTiffFile) + ": " + exception_type, Custom_Trace_Type_Enum.Error);
                    }
                    else
                    {
                        OnProcess("Tesseract OCR successfuly on " + Path.GetFileName(thisTiffFile) + " to " + textFilePath, "Tesseract OCR Module", Resource.BibID + ":" + Resource.VID, Resource.METS_Type_String, Resource.BuilderLogId);
                    }
                }
            }

            return true;
        }
    }



    public static class Tesseract_Processor
    {
        public static string Tesseract_Executable { get; set; }

        public static string Last_Exception { get; private set; }

        /// <param name="SourceFileName"> TIFF file to OCR </param>
        /// <param name="OutputBase"> Output base path, WITHOUT an extension - Tesseract appends ".txt" to
        /// this itself, so passing a path that already ends in ".txt" results in a "*.txt.txt" file </param>
        public static bool Process_TIFF(string SourceFileName, string OutputBase)
        {
            Last_Exception = null;

            try
            {
                var tessProcess = new Process();
                tessProcess.StartInfo.FileName = Tesseract_Executable;
                tessProcess.StartInfo.Arguments = SourceFileName + " " + OutputBase;

                // Stop the process from opening a new window
                //process.StartInfo.RedirectStandardOutput = true;
                //process.StartInfo.UseShellExecute = false;
                //process.StartInfo.CreateNoWindow = true;


                tessProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                tessProcess.Start();

                tessProcess.WaitForExit(5000);

                return true;
            }
            catch (Exception ee)
            {
                Last_Exception = ee.Message;
                return false;
            }

        }
    }
}
