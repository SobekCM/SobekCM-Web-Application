#region Using directives

using System;
using System.IO;
using System.Reflection;
using SobekCM.Builder_Library.Settings;
using SobekCM.Builder_Library.Tools;

using SobekCM.Tools;
#endregion

namespace SobekCM.Builder_Library.Modules.Items
{
    /// <summary> Item-level submission package module converts office files ( powerpoints and Word files )
    /// into a PDF, while still retaining the original file </summary>
    /// <remarks> This class implements the <see cref="abstractSubmissionPackageModule" /> abstract class and implements the <see cref="iSubmissionPackageModule" /> interface. </remarks>
    public class ConvertOfficeFilesToPdfModule : abstractSubmissionPackageModule
    {
        /// <summary> Converts office files ( powerpoints and Word files ) into a PDF, while still retaining the original file  </summary>
        /// <param name="Resource"> Incoming digital resource object </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> TRUE if processing can continue, FALSE if a critical error occurred which should stop all processing </returns>
        public override bool DoWork(Incoming_Digital_Resource Resource, Custom_Tracer Tracer)
        {
            Tracer?.Add_Trace("ConvertOfficeFilesToPdfModule.DoWork");

            // Nothing to do for a metadata-only update -- no resource files accompany it
            if (Resource.METS_Only_Package)
                return true;

            string resourceFolder = Resource.Resource_Folder;

            // Should we try to convert office files?
            if (Settings.Builder.Convert_Office_Files_To_PDF)
            {
                try
                {
                    // Preprocess each Powerpoint document to PDF
                    string[] ppt_files = File_System_Tools.GetFiles(resourceFolder, "*.ppt*");
                    foreach (string thisPowerpoint in ppt_files)
                    {
                        // Get the fileinfo and the name
                        var thisPowerpointInfo = new FileInfo(thisPowerpoint);
                        string filename = thisPowerpointInfo.Name.Replace(thisPowerpointInfo.Extension, "");

                        // Does a PDF version exist for this item?
                        string pdf_version = Path.Combine(resourceFolder, filename + ".pdf");
                        if (!File.Exists(pdf_version))
                        {
                            OnProcess($"Converting {thisPowerpoint} to PDF", "Image Processing", Resource.BibID + ":" + Resource.VID, String.Empty, Resource.BuilderLogId);

                            int conversion_error = Word_Powerpoint_to_PDF_Converter.Powerpoint_To_PDF(thisPowerpoint, pdf_version);
                            switch (conversion_error)
                            {
                                case 1:
                                    OnError("Error converting PPT to PDF: Can't open input file", Resource.BibID + ":" + Resource.VID, Resource.METS_Type_String, Resource.BuilderLogId);
                                    break;

                                case 2:
                                    OnError("Error converting PPT to PDF: Can't create output file", Resource.BibID + ":" + Resource.VID, Resource.METS_Type_String, Resource.BuilderLogId);
                                    break;

                                case 3:
                                    OnError("Error converting PPT to PDF: Converting failed", Resource.BibID + ":" + Resource.VID, Resource.METS_Type_String, Resource.BuilderLogId);
                                    break;

                                case 4:
                                    OnError("Error converting PPT to PDF: LibreOffice not installed or configured", Resource.BibID + ":" + Resource.VID, Resource.METS_Type_String, Resource.BuilderLogId);
                                    break;
                            }
                        }
                    }

                    // Preprocess each Word document to PDF
                    string[] doc_files = File_System_Tools.GetFiles(resourceFolder, "*.doc*");
                    foreach (string thisWordDoc in doc_files)
                    {
                        // Get the fileinfo and the name
                        var thisWordDocInfo = new FileInfo(thisWordDoc);
                        string filename = thisWordDocInfo.Name.Replace(thisWordDocInfo.Extension, "");

                        // Does a PDF version exist for this item?
                        string pdf_version = Path.Combine(resourceFolder, filename + ".pdf");
                        if (!File.Exists(pdf_version))
                        {
                            OnProcess($"Converting {thisWordDoc} to PDF", "Image Processing", Resource.BibID + ":" + Resource.VID, String.Empty, Resource.BuilderLogId);

                            int conversion_error = Word_Powerpoint_to_PDF_Converter.Word_To_PDF(thisWordDoc, pdf_version);
                            switch (conversion_error)
                            {
                                case 1:
                                    OnError("Error converting Word DOC to PDF: Can't open input file", Resource.BibID + ":" + Resource.VID, Resource.METS_Type_String, Resource.BuilderLogId);
                                    break;

                                case 2:
                                    OnError("Error converting Word DOC to PDF: Can't create output file", Resource.BibID + ":" + Resource.VID, Resource.METS_Type_String, Resource.BuilderLogId);
                                    break;

                                case 3:
                                    OnError("Error converting Word DOC to PDF: Converting failed", Resource.BibID + ":" + Resource.VID, Resource.METS_Type_String, Resource.BuilderLogId);
                                    break;

                                case 4:
                                    OnError("Error converting Word DOC to PDF: LibreOffice not installed or configured", Resource.BibID + ":" + Resource.VID, Resource.METS_Type_String, Resource.BuilderLogId);
                                    break;
                            }
                        }
                    }
                }
                catch (Exception ee)
                {
                    var errorWriter = new StreamWriter(Path.Combine(MultiInstance_Builder_Settings.Builder_Executable_Directory, "Logs", "error.log"), true);
                    errorWriter.WriteLine("Message: " + ee.Message);
                    errorWriter.WriteLine("Stack Trace: " + ee.StackTrace);
                    errorWriter.Flush();
                    errorWriter.Close();

                    OnError("Unknown error converting office files to PDF", Resource.BibID + ":" + Resource.VID, Resource.METS_Type_String, Resource.BuilderLogId);
                    OnError(ee.Message, Resource.BibID + ":" + Resource.VID, Resource.METS_Type_String, Resource.BuilderLogId);
                    Tracer?.Add_Trace("ConvertOfficeFilesToPdfModule.DoWork", "Unknown error converting office files to PDF: " + ee.Message, Custom_Trace_Type_Enum.Error);
                }
            }

            return true;
        }


    }
}
