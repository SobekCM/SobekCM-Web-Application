#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.Navigation;
using SobekCM.Library;
using SobekCM.Library.MainWriters;
using SobekCM.Tools;
using System.IO;

#endregion

namespace SobekCM.Plugins.Dataset
{
    /// <summary> Main writer writes search results and item browses as a dataset represented in
    /// XML format to the response stream.  This is the native Microsoft.NET format, easily read into
    /// a remote dataset by using DataSet.ReadXML() </summary>
    /// <remarks> This class extends the abstract class <see cref="abstractMainWriter"/>. Loaded via
    /// reflection by <c>MainWriter_Factory</c>, registered through the "dataset" extension's
    /// &lt;mainWriter&gt; config element - not referenced anywhere in the core SobekCM_Library assembly. </remarks>
    public class Dataset_MainWriter : abstractMainWriter
    {
        /// <summary> Constructor for a new instance of the Dataset_MainWriter class </summary>
        /// <param name="Context"> Context for this individual HTTP request </param>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        public Dataset_MainWriter(HttpContext Context, RequestCache RequestSpecificValues) : base(Context, RequestSpecificValues)

        {
            // All work done in base class
        }

        /// <summary> Gets the code identifying the type of main writer </summary>
        /// <value> This property always returns <see cref="Writer_Codes.DataSet"/>. </value>
        public override string Writer_Type { get { return Writer_Codes.DataSet; } }

        /// <summary> Perform all the work of adding text directly to the response stream back to the web user </summary>
        /// <param name="Output"> Stream to which to write the text for this main writer </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        public override void Write_Body(TextWriter Output, Custom_Tracer Tracern)
        {
            switch (RequestSpecificValues.Current_Mode.Mode)
            {
                case Display_Mode_Enum.Results:
                case Display_Mode_Enum.Aggregation:
                    if (RequestSpecificValues.Paged_Results != null)
                        display_search_results();
                    break;

                default:
                    Output.Write("DataSet Writer - Unknown Mode");
                    break;
            }
        }

        private void display_search_results()
        {
            // Write this information
            //search_results.WriteXml(Output, XmlWriteMode.WriteSchema);
        }
    }
}
