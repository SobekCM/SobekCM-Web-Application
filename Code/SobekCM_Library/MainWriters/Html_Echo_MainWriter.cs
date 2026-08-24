#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.Navigation;
using SobekCM.Tools;
using System.IO;

#endregion

namespace SobekCM.Library.MainWriters
{
    /// <summary> HTML echo writer is generally used just for directing search engine robots to pre-existing 
    /// html pages for indexing items, etc.. </summary>
    /// <remarks> This class extends the abstract class <see cref="abstractMainWriter"/>. </remarks>
    public class Html_Echo_MainWriter : abstractMainWriter
    {
        private readonly string fileToEcho;

        /// <summary> Constructor for a new instance of the Text_MainWriter class </summary>
        /// <param name="Context"> Context for this individual HTTP request </param>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        /// <param name="HTML_File_To_Echo"> The HTML file to echo </param>
        public Html_Echo_MainWriter(HttpContext Context, RequestCache RequestSpecificValues, string HTML_File_To_Echo) : base(Context, RequestSpecificValues)
        {
            fileToEcho = HTML_File_To_Echo;
        }

        /// <summary> Gets the enumeration of the type of main writer </summary>
        /// <value> This property always returns the enumerational value <see cref="Writer_Codes.HTML"/>. </value>
        public override string Writer_Type { get { return Writer_Codes.HTML_Echo; } }


        /// <summary> Perform all the work of adding text directly to the response stream back to the web user </summary>
        /// <param name="Output"> Stream to which to write the text for this main writer </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <remarks> The echoed file is a complete, pre-rendered HTML document (captured from this app's own
        /// live rendering by <c>Aggregation_Static_Page_Writer.Build_All_Browse</c>), not a body fragment - so
        /// unlike <see cref="Html_MainWriter"/>, no DOCTYPE/html/head/body wrapper is added here. </remarks>
        public override void Write_Body(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace("Html_Echo_MainWriter.Write_Body", "Reading the text from the file and echoing back to the output stream");

            Context.Response.ContentType = "text/html; charset=utf-8";

            try
            {
                var fileStream = new FileStream(fileToEcho, FileMode.Open, FileAccess.Read);
                var reader = new StreamReader(fileStream);
                string line = reader.ReadLine();
                while (line != null)
                {
                    Output.WriteLine(line);
                    line = reader.ReadLine();
                }
                reader.Close();
            }
            catch
            {
                Output.WriteLine("ERROR READING THE SOURCE FILE");
            }
        }
    }
}
