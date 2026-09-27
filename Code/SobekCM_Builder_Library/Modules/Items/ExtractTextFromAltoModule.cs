#region Using directives

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;
using SobekCM.Builder_Library.Tools;
using SobekCM.Tools;

#endregion

namespace SobekCM.Builder_Library.Modules.Items
{
    /// <summary> Item-level submission package module extracts indexable full text from page-level ALTO
    /// OCR files (e.g. 0001.alto.xml), which hold per-word coordinates and confidence rather than prose,
    /// and so cannot be handled by the generic <see cref="ExtractTextFromXmlModule" /> tag-stripper </summary>
    /// <remarks> This class implements the <see cref="abstractSubmissionPackageModule" /> abstract class and implements the <see cref="iSubmissionPackageModule" /> interface. </remarks>
    public class ExtractTextFromAltoModule : abstractSubmissionPackageModule
    {
        /// <summary> Extracts indexable full text from every page-level ALTO file in the resource folder </summary>
        /// <param name="Resource"> Incoming digital resource object </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> TRUE if processing can continue, FALSE if a critical error occurred which should stop all processing </returns>
        public override bool DoWork(Incoming_Digital_Resource Resource, Custom_Tracer Tracer)
        {
            Tracer?.Add_Trace("ExtractTextFromAltoModule.DoWork");

            // Nothing to do for a metadata-only update -- no resource files accompany it
            if (Resource.Metadata_Changes_Only)
                return true;

            string resourceFolder = Resource.Resource_Folder;

            string[] alto_files = File_System_Tools.GetFiles(resourceFolder, "*.alto.xml");
            foreach (string thisAlto in alto_files)
            {
                var thisAltoInfo = new FileInfo(thisAlto);

                // The page number prefix is everything before the first '.' (matches the page-grouping
                // logic in Division_Tree, so 0001.alto.xml pairs with 0001.jp2 / 0001.txt)
                string page_prefix = thisAltoInfo.Name.Substring(0, thisAltoInfo.Name.IndexOf('.'));
                string text_fileName = page_prefix + ".txt";
                string text_filePath = Path.Combine(resourceFolder, text_fileName);

                // Don't overwrite an existing text file -- it may already hold better (e.g. hand-corrected) text
                if (File.Exists(text_filePath))
                    continue;

                if (!Extract_Text(thisAlto, text_filePath))
                    Tracer?.Add_Trace("ExtractTextFromAltoModule.DoWork", "Unable to extract text from '" + thisAltoInfo.Name + "'", Custom_Trace_Type_Enum.Error);
            }

            return true;
        }

        /// <summary> Reads the ALTO word-level structure ( Layout/Page/.../TextLine/String ) and writes the
        /// recognized words, one line of output per ALTO text line, discarding all the coordinate/style/confidence
        /// attributes that make up the bulk of an ALTO file </summary>
        /// <param name="Alto_In_Name"> Full path to the source ALTO XML file </param>
        /// <param name="Text_Out_Name"> Output file name for the extracted text </param>
        /// <returns> TRUE if successful, otherwise FALSE </returns>
        internal static bool Extract_Text(string Alto_In_Name, string Text_Out_Name)
        {
            try
            {
                XDocument altoDoc = XDocument.Load(Alto_In_Name);
                if (altoDoc.Root == null)
                    return false;

                // Use whatever default namespace this ALTO file declares (producers vary: CCS/Newpah,
                // Library of Congress, or no namespace at all), rather than assuming one
                XNamespace ns = altoDoc.Root.GetDefaultNamespace();

                using (var outFile = new StreamWriter(Text_Out_Name, false, Encoding.UTF8))
                {
                    // TextLine elements appear in document (reading) order regardless of how deeply
                    // they are nested under TextBlock / ComposedBlock, so just walk all of them directly
                    foreach (XElement textLine in altoDoc.Descendants(ns + "TextLine"))
                    {
                        string line = Build_Line_Text(textLine, ns);
                        if (line.Length > 0)
                            outFile.WriteLine(line);
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary> Builds the readable text for one ALTO TextLine from its String children </summary>
        /// <remarks> Some ALTO producers (e.g. multi-engine OCR merges) leave more than one sibling
        /// &lt;String&gt; at the same horizontal position -- one per contributing OCR engine's guess for that
        /// word -- rather than expressing the alternate only via the nested &lt;ALTERNATIVE&gt; element. When a
        /// String's HPOS falls within the span of the previously kept String, it is treated as a competing
        /// reading for the same word slot and only the one with the higher WC (word confidence) is kept,
        /// instead of emitting both and doubling up on garbled near-duplicate words </remarks>
        private static string Build_Line_Text(XElement TextLine, XNamespace Ns)
        {
            double keptEnd = double.MinValue;
            double keptWordConf = 0;
            var words = new List<string>();

            foreach (XElement stringElement in TextLine.Elements(Ns + "String"))
            {
                string content = (string) stringElement.Attribute("CONTENT");
                if (String.IsNullOrEmpty(content))
                    continue;

                double hpos = (double?) stringElement.Attribute("HPOS") ?? 0;
                double width = (double?) stringElement.Attribute("WIDTH") ?? 0;
                double wordConf = (double?) stringElement.Attribute("WC") ?? 0;

                if ((words.Count > 0) && (hpos < keptEnd))
                {
                    // Overlaps the last kept word's span -- a competing reading, not a new word
                    if (wordConf > keptWordConf)
                    {
                        words[words.Count - 1] = content;
                        keptWordConf = wordConf;
                        keptEnd = Math.Max(keptEnd, hpos + width);
                    }
                    continue;
                }

                words.Add(content);
                keptWordConf = wordConf;
                keptEnd = hpos + width;
            }

            return String.Join(" ", words);
        }
    }
}
