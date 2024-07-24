using System.Text;
using System.Web;
using SobekCM.Core.Aggregations;
using SobekCM.Core.BriefItem;
using SobekCM.Core.Navigation;
using SobekCM.Core.UI_Configuration.Citation;
using SobekCM.Library.UI;
using SobekCM.Tools;

namespace SobekCM.Library.Citation.SectionWriter
{
    /// <summary> Special citation section writer adds the aggregations related to this item </summary>
    /// <remarks> This class implements the <see cref="iCitationSectionWriter"/> interface.</remarks>
    public class Aggregation_SectionWriter : iCitationSectionWriter
    {
        /// <summary> Returns flag that indicates this citation section writer 
        /// will be writing values to the output stream </summary>
        /// <param name="ElementInfo"> Additional possible data about this citation element </param>
        /// <param name="Item"> Digital resource to analyze for data to write </param>
        /// <returns>TRUE if there is a value to write</returns>
        public bool Has_Data_To_Write(CitationElement ElementInfo, BriefItemInfo Item)
        {
            return ((Item.Behaviors != null) && (Item.Behaviors.Aggregation_Code_List != null) && (Item.Behaviors.Aggregation_Code_List.Count > 0));
        }

        /// <summary> Wites a section of citation from a provided digital resource </summary>
        /// <param name="ElementInfo"> Additional possible data about this citation element </param>
        /// <param name="Output"> Response stream for the item viewer to write directly to </param>
        /// <param name="Item"> Digital resource with all the data to write </param>
        /// <param name="LeftColumnWidth"> Number of pixels of the left column, or the definition terms </param>
        /// <param name="SearchLink"> Beginning of the search link that can be used to allow the web patron to select a term and run a search against this instance </param>
        /// <param name="SearchLinkEnd"> End of the search link that can be used to allow the web patron to select a term and run a search against this instance  </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <param name="CurrentRequest"> Information about the current request </param>
        public void Write_Citation_Section(CitationElement ElementInfo, StringBuilder Output, BriefItemInfo Item, int LeftColumnWidth, string SearchLink, string SearchLinkEnd, Custom_Tracer Tracer, Navigation_Object CurrentRequest)
        {
            string displayLabel = "Aggregations";

            Output.AppendLine("        <dt class=\"sbk_CivAGGGR_Element\" style=\"width:" + LeftColumnWidth + "px;\" >" + displayLabel + ": </dt>");
            Output.Append("        <dd class=\"sbk_CivAGGR_Element\" style=\"margin-left:" + LeftColumnWidth + "px;\" >");

            bool first = true;
            foreach (string code in Item.Behaviors.Aggregation_Code_List)
            {
                // Get the aggregation by code
                Item_Aggregation_Related_Aggregations aggr = UI_ApplicationCache_Gateway.Aggregations[code];

                // If it found the aggr stuff 
                if (aggr != null)
                {
                    // Go to a new line if not the first
                    if (first) first = false;
                    else Output.AppendLine("<br />");

                    // Write this
                    Output.AppendLine("<a href=\"" + CurrentRequest.Base_URL + code + "\">" + HttpUtility.HtmlEncode(aggr.Name) + "</a>");
                }
            }

            Output.AppendLine("</dd>");
            Output.AppendLine();

        }
    }
}
