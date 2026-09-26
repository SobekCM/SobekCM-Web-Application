#region Using directives

using SobekCM.Core.Navigation;
using SobekCM.Core.Results;
using SobekCM.Core.Search;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Library.Localization;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System;
using System.IO;
using System.Text;

#endregion

namespace SobekCM.Library.ResultsViewer
{
    /// <summary> Results viewer shows the thumbnail and a small citation block for each item in the result set.  </summary>
    /// <remarks> This class extends the abstract class <see cref="abstract_ResultsViewer"/> and implements the 
    /// <see cref="iResultsViewer" /> interface. </remarks>
    public class Brief_ResultsViewer : abstract_ResultsViewer
    {
        /// <summary> Constructor for a new instance of the Brief_ResultsViewer class </summary>
        public Brief_ResultsViewer() : base()
        {
            // Do nothing
        }

        /// <summary> Adds the controls for this result viewer to the place holder on the main form </summary>
        /// <param name="MainPlaceHolder"> Main place holder ( &quot;mainPlaceHolder&quot; ) in the itemNavForm form into which the the bulk of the result viewer's output is displayed</param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> Sorted tree with the results in hierarchical structure with volumes and issues under the titles and sorted by serial hierarchy </returns>
        public override void Add_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer?.Add_Trace("Brief_ResultsWriter.Add_HTML", "Rendering results in brief view");

            // If results are null, or no results, return empty string
            if ((PagedResults == null) || (ResultsStats == null) || (ResultsStats.Total_Items <= 0))
                return;

            // Get the text search redirect stem and (writer-adjusted) base url 
            string textRedirectStem = Text_Redirect_Stem;
            string base_url = RequestSpecificValues.Current_Mode.Base_URL;
            if (RequestSpecificValues.Current_Mode.Writer_Type == Writer_Codes.HTML_LoggedIn)
                base_url = RequestSpecificValues.Current_Mode.Base_URL + "l/";

            // Start the results
            var resultsBldr = new StringBuilder(2000);
            resultsBldr.AppendLine("<section class=\"sbkBrv_Results\">");

            // Set the counter for these results from the page 
            int current_page = RequestSpecificValues.Current_Mode.Page.HasValue ? RequestSpecificValues.Current_Mode.Page.Value : 1;
            int result_counter = ((current_page - 1) * Results_Per_Page) + 1;

            Tracer.Add_Trace("Brief_ResultsViewer.Add_HTML", "There are [" + PagedResults.Count + "] results @ [" + Results_Per_Page + "] per page.");

            // Step through all the results
            int current_row = 0;
            foreach (iSearch_Title_Result titleResult in PagedResults)
            {
                bool multiple_title = titleResult.Item_Count > 1;
                string access_type = String.Empty;

                // Always get the first item for things like the main link and thumbnail
                iSearch_Item_Result firstItemResult = titleResult.Get_Item(0);

                // Determine the internal link to the first (possibly only) item
                string internal_link = base_url + titleResult.BibID + "/" + firstItemResult.VID + textRedirectStem;

                // For browses, just point to the title
                if (RequestSpecificValues.Current_Mode.Mode == Display_Mode_Enum.Aggregation) // browse info only
                    internal_link = base_url + titleResult.BibID + "/" + firstItemResult.VID + textRedirectStem;

                // Start this row
                string title = firstItemResult.Title.Replace("<", "&lt;").Replace(">", "&gt;");
                if (multiple_title)
                {
                    title = titleResult.GroupTitle.Replace("<", "&lt;").Replace(">", "&gt;");
                    resultsBldr.AppendLine("\t<section class=\"sbkBrv_SingleResult\">");

                }
                else
                {
                    access_type = firstItemResult.AccessType;

                    if (String.IsNullOrEmpty(access_type))
                    {
                        resultsBldr.AppendLine("\t<section class=\"sbkBrv_SingleResult\" onclick=\"window.location.href='" + internal_link.Replace("'", "\\'") + "';\" >");
                    }
                    else
                    {
                        resultsBldr.AppendLine("\t<section class=\"sbkBrv_SingleResult sbkBrv_SingleResult_" + access_type + "\" onclick=\"window.location.href='" + internal_link.Replace("'", "\\'") + "';\" >");
                    }
                }

                // Add the counter as the first column
                resultsBldr.AppendLine("\t\t<div class=\"sbkBrv_SingleResultNum\">" + result_counter + "</div>");
                resultsBldr.Append("\t\t<div class=\"sbkBrv_SingleResultThumb\">");

                ////// Is this restricted?
                //bool restricted_by_ip = false;
                //if ((titleResult.Item_Count == 1) && (firstItemResult.IP_Restriction_Mask > 0))
                //{
                //    int comparison = firstItemResult.IP_Restriction_Mask & CurrentUserMask;
                //    if (comparison == 0)
                //    {
                //        restricted_by_ip = true;
                //    }
                //}

                // Calculate the thumbnail
                string thumb = titleResult.BibID.Substring(0, 2) + "/" + titleResult.BibID.Substring(2, 2) + "/" + titleResult.BibID.Substring(4, 2) + "/" + titleResult.BibID.Substring(6, 2) + "/" + titleResult.BibID.Substring(8) + "/" + firstItemResult.VID + "/" + (firstItemResult.MainThumbnail).Replace("\\", "/").Replace("//", "/");

                // Draw the thumbnail 
                if (!String.IsNullOrEmpty(firstItemResult.Group_Restrictions))
                {
                    resultsBldr.AppendLine("<a href=\"" + internal_link + "\"><img src=\"" + RequestSpecificValues.Current_Mode.Base_Design_URL + "restricted-thumb.png\" border=\"0px\" class=\"resultsThumbnail\" alt=\"RESTRICTED ITEM\" style=\"width:150px\" /></a></div>");
                }
                else if ((thumb.ToUpper().IndexOf(".JPG") < 0) && (thumb.ToUpper().IndexOf(".GIF") < 0))
                {
                    resultsBldr.AppendLine("<a href=\"" + internal_link + "\"><img src=\"" + Static_Resources_Gateway.Nothumb_Jpg + "\" border=\"0px\" class=\"resultsThumbnail\" alt=\"MISSING THUMBNAIL\" /></a></div>");
                }
                else
                {
                    resultsBldr.AppendLine("<a href=\"" + internal_link + "\"><img src=\"" + UI_ApplicationCache_Gateway.Settings.Servers.Image_URL + thumb + "\" class=\"resultsThumbnail\" alt=\"" + title.Replace("\"", "") + "\" /></a></div>");
                }

                resultsBldr.AppendLine("\t\t<div class=\"sbkBrv_SingleResultDesc\">");

                // Access restrictions, title, metadata and snippet (shared with the map view)
                Append_Result_Description(resultsBldr, titleResult, internal_link, "\t\t\t");

                // Add children, if there are some
                if (multiple_title)
                {
                    // Write to output
                    Output.Write(Restore_Role_Markup(resultsBldr.ToString()));
                    resultsBldr.Remove(0, resultsBldr.Length);

                    Add_Issue_Tree(Output, titleResult, current_row, textRedirectStem, base_url);
                }

                resultsBldr.AppendLine("\t\t</div>");

                resultsBldr.AppendLine("\t</section>");
                resultsBldr.AppendLine();

                // Increment the result counters
                result_counter++;
                current_row++;
            }

            // End this table
            resultsBldr.AppendLine("</section>");

            // Write to output
            Output.Write(Restore_Role_Markup(resultsBldr.ToString()));
        }
    }
}
