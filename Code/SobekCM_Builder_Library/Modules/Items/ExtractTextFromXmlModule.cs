#region Using directives

using System.IO;
using SobekCM.Builder_Library.Tools;

using SobekCM.Tools;
#endregion

namespace SobekCM.Builder_Library.Modules.Items
{
    /// <summary> Item-level submission package module extracts indexable (i.e, without the tags) text from a HTML file </summary>
    /// <remarks> This class implements the <see cref="abstractSubmissionPackageModule" /> abstract class and implements the <see cref="iSubmissionPackageModule" /> interface. </remarks>
    public class ExtractTextFromXmlModule : abstractSubmissionPackageModule
    {
        /// <summary> Extracts indexable (i.e, without the tags) text from a HTML file </summary>
        /// <param name="Resource"> Incoming digital resource object </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> TRUE if processing can continue, FALSE if a critical error occurred which should stop all processing </returns>
        public override bool DoWork(Incoming_Digital_Resource Resource, Custom_Tracer Tracer)
        {
            Tracer?.Add_Trace("ExtractTextFromXmlModule.DoWork");

            // Nothing to do for a metadata-only update -- no resource files accompany it
            if (Resource.METS_Only_Package)
                return true;

            string resourceFolder = Resource.Resource_Folder;

            // Preprocess each XML file for the text
            string[] xml_files = File_System_Tools.GetFiles(resourceFolder, "*.xml");
            foreach (string thisXml in xml_files)
            {
                // Get the fileinfo and the name
                var thisXmlInfo = new FileInfo(thisXml);

                // Just don't pull text for the static page
                string xml_upper = thisXmlInfo.Name.ToUpper();
                if ((xml_upper.IndexOf(".METS") < 0) && (xml_upper != "DOC.XML") && (xml_upper != "CITATION_METS.XML") && (xml_upper != "MARC.XML"))
                {
                    string text_fileName = thisXmlInfo.Name.Replace(".", "_") + ".txt";

                    // Does the full text exist for this item?
                    if (!File.Exists(Path.Combine(resourceFolder, text_fileName)))
                    {
                        if (!HTML_XML_Text_Extractor.Extract_Text(thisXml, Path.Combine(resourceFolder, text_fileName)))
                            Tracer?.Add_Trace("ExtractTextFromXmlModule.DoWork", "Unable to extract text from '" + thisXmlInfo.Name + "'", Custom_Trace_Type_Enum.Error);
                    }
                }
            }

            return true;
        }
    }
}
