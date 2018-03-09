#region Using directives

using System;
using System.Collections.Generic;
using System.Text;
using System.Web.UI.WebControls;
using SobekCM.Core.Navigation;
using SobekCM.Core.Results;
using SobekCM.Library.UI;
using SobekCM.Tools;

#endregion

namespace SobekCM.Library.ResultsViewer
{
    /// <summary> Results viewer shows the results in a simple table view with each title on its own table row.  </summary>
    /// <remarks> This class extends the abstract class <see cref="abstract_ResultsViewer"/> and implements the 
    /// <see cref="iResultsViewer" /> interface. </remarks>
    public class Table_ResultsViewer : abstract_ResultsViewer
    {
        /// <summary> Constructor for a new instance of the Table_ResultsViewer class </summary>
        public Table_ResultsViewer() : base()
        {
            // Do nothing
        }

        /// <summary> Adds the controls for this result viewer to the place holder on the main form </summary>
        /// <param name="MainPlaceHolder"> Main place holder ( &quot;mainPlaceHolder&quot; ) in the itemNavForm form into which the the bulk of the result viewer's output is displayed</param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> Sorted tree with the results in hierarchical structure with volumes and issues under the titles and sorted by serial hierarchy </returns>
        public override void Add_HTML(PlaceHolder MainPlaceHolder, Custom_Tracer Tracer)
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("Table_ResultsWriter.Add_HTML", "Rendering results in table view");
            }

            // If results are null, or no results, return empty string
            if ((PagedResults == null) || (ResultsStats == null) || (ResultsStats.Total_Items <= 0))
                return;

            // Get the text search redirect stem and (writer-adjusted) base url 
            string textRedirectStem = Text_Redirect_Stem;
            string base_url = RequestSpecificValues.Current_Mode.Base_URL;
            if (RequestSpecificValues.Current_Mode.Writer_Type == Writer_Type_Enum.HTML_LoggedIn)
                base_url = RequestSpecificValues.Current_Mode.Base_URL + "l/";

            // Start the results
            StringBuilder resultsBldr = new StringBuilder(5000);
            resultsBldr.AppendLine("<br />");
            resultsBldr.AppendLine("<table width=\"100%\">");

            // Start the header row and add the 'No.' part
            short? currentOrder = RequestSpecificValues.Current_Mode.Sort;
            if (RequestSpecificValues.Current_Mode.Mode == Display_Mode_Enum.Results)
            {
                RequestSpecificValues.Current_Mode.Sort = 0;
                resultsBldr.AppendLine("\t<tr valign=\"bottom\" align=\"left\">");
                resultsBldr.AppendLine("\t\t<td width=\"30px\"><span class=\"SobekTableSortText\"><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode).Replace("&", "&amp;") + "\"><strong>No.</strong></a></span></td>");
            }
            else
            {
                resultsBldr.AppendLine("\t<tr valign=\"bottom\" align=\"left\">\n\t\t<td>No.</td>");
            }

            // Add the title column
            RequestSpecificValues.Current_Mode.Sort = 1;
            resultsBldr.AppendLine("\t\t<td><span class=\"SobekTableSortText\"><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode).Replace("&", "&amp;") + "\"><strong>" + UI_ApplicationCache_Gateway.Translation.Get_Translation("Title", RequestSpecificValues.Current_Mode.Language) + "</strong></a></span></td>");

            // Add the date column
            RequestSpecificValues.Current_Mode.Sort = 10;
            resultsBldr.AppendLine("\t\t<td><span class=\"SobekTableSortText\"><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode).Replace("&", "&amp;") + "\"><strong>" + UI_ApplicationCache_Gateway.Translation.Get_Translation("Date", RequestSpecificValues.Current_Mode.Language) + "</strong></a></span></td>");
            RequestSpecificValues.Current_Mode.Sort = currentOrder;
            resultsBldr.AppendLine("\t</tr>");

            resultsBldr.AppendLine("\t<tr><td bgcolor=\"#e7e7e7\" colspan=\"3\"></td></tr>");

            // Set the counter for these results from the page 
            int page = RequestSpecificValues.Current_Mode.Page.HasValue ? Math.Max(RequestSpecificValues.Current_Mode.Page.Value, ((ushort)1)) : 1;
            int result_counter = ((page - 1) * Results_Per_Page) + 1;
            int current_row = 0;

            // Step through all the results
            foreach (iSearch_Title_Result titleResult in PagedResults)
            {
                bool multiple_title = titleResult.Item_Count > 1;

                // Always get the first item for things like the main link and thumbnail
                iSearch_Item_Result firstItemResult = titleResult.Get_Item(0);

                //// Is this restricted?
                //bool restricted_by_ip = false;
                //if ((titleResult.Item_Count == 1) && (firstItemResult.IP_Restriction_Mask > 0))
                //{
                //    int comparison = dbItem.IP_Range_Membership & base.current_user_mask;
                //    if (comparison == 0)
                //    {
                //        restricted_by_ip = true;
                //    }
                //}

                // Determine the internal link to the first (possibly only) item
                string internal_link = base_url + titleResult.BibID + "/" + firstItemResult.VID + textRedirectStem;

                // For browses, just point to the title
                if (( RequestSpecificValues.Current_Mode.Mode == Display_Mode_Enum.Aggregation ) && ( RequestSpecificValues.Current_Mode.Aggregation_Type == Aggregation_Type_Enum.Browse_Info ))
                    internal_link = base_url + titleResult.BibID + textRedirectStem;

                // Start this row
                if ( multiple_title )
                    resultsBldr.AppendLine("\t<tr valign=\"top\" onmouseover=\"this.className='tableRowHighlight'\" onmouseout=\"this.className='tableRowNormal'\" >");
                else
                    resultsBldr.AppendLine("\t<tr valign=\"top\" onmouseover=\"this.className='tableRowHighlight'\" onmouseout=\"this.className='tableRowNormal'\" onclick=\"window.location.href='" + internal_link  + "';\" >");


                // Add the counter as the first column
                resultsBldr.AppendLine("\t\t<td>" + result_counter + "</td>");

                // Add differently depending on the child row count
                if ( !multiple_title )
                {
                    if (!String.IsNullOrEmpty(firstItemResult.Link))
                    {
                        resultsBldr.AppendLine("\t\t<td>" + firstItemResult.Title + " ( <a href=\"" + firstItemResult.Link + "\">external resource</a> | <a href=\"" + internal_link + "\">internal citation</a> )</td>");
                    }
                    else
                    {
                        resultsBldr.AppendLine("\t\t<td><a href=\"" + internal_link + "\">" + firstItemResult.Title + "</a></td>");
                    }
                    resultsBldr.AppendLine("\t\t<td>" + firstItemResult.PubDate + "</td></tr>");
                }
                else
                {
                    resultsBldr.AppendLine("\t\t<td colspan=\"2\">");

                    // Add this to the place holder
                    Literal thisLiteral = new Literal
                                              { Text = resultsBldr.ToString().Replace("&lt;role&gt;", "<i>").Replace( "&lt;/role&gt;", "</i>") };
                    MainPlaceHolder.Controls.Add(thisLiteral);
                    resultsBldr.Remove(0, resultsBldr.Length);

                    Add_Issue_Tree(MainPlaceHolder, titleResult, current_row, textRedirectStem, base_url);
                    resultsBldr.AppendLine("\t</td></tr>" );
                }

                // Add a horizontal line
                resultsBldr.AppendLine("\t<tr><td bgcolor=\"#e7e7e7\" colspan=\"3\"></td></tr>");

                // Increment the result counters
                result_counter++;
                current_row++;
            }

            // End this table
            resultsBldr.AppendLine("</table>");

            // Add this to the html table
            Literal mainLiteral = new Literal
                                      {  Text = resultsBldr.ToString().Replace("&lt;role&gt;", "<i>").Replace( "&lt;/role&gt;", "</i>")  };
            MainPlaceHolder.Controls.Add(mainLiteral);
        }
    }
}
