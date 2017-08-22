using System.Text;
using System.Web.UI.WebControls;
using SobekCM.Core.Navigation;
using SobekCM.Core.Results;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Tools;

namespace SobekCM.Library.ResultsViewer
{
    /// <summary> Timeline results viewer will eventually show the results in a timeline view </summary>
    public class Timeline_ResultsViewer: abstract_ResultsViewer
    {
        /// <summary> Constructor for a new instance of the Timeline_ResultsViewer class </summary>
        public Timeline_ResultsViewer()
        {
            // Do nothing, but the base class constructor does stuff
        }

        /// <summary> Adds the controls for this result viewer to the place holder on the main form </summary>
        /// <param name="MainPlaceHolder"> Main place holder ( &quot;mainPlaceHolder&quot; ) in the itemNavForm form into which the the bulk of the result viewer's output is displayed</param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> Sorted tree with the results in hierarchical structure with volumes and issues under the titles and sorted by serial hierarchy </returns>
        public override void Add_HTML(PlaceHolder MainPlaceHolder, Custom_Tracer Tracer)
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("Custom_Thumbnail_ResultsViewer.Add_HTML", "Rendering results in thumbnail view");
            }

            // If results are null, or no results, return empty string
            if ((PagedResults == null) || (ResultsStats == null) || (ResultsStats.Total_Items <= 0))
                return;

            // Get the text search redirect stem and (writer-adjusted) base url 
            string textRedirectStem = Text_Redirect_Stem;
            string base_url = RequestSpecificValues.Current_Mode.Base_URL;
            if (RequestSpecificValues.Current_Mode.Writer_Type == Writer_Type_Enum.HTML_LoggedIn)
                base_url = RequestSpecificValues.Current_Mode.Base_URL + "l/";

            // Should the publication date be shown?
            bool showDate = RequestSpecificValues.Current_Mode.Sort >= 10;

            // Start this table
            StringBuilder resultsBldr = new StringBuilder(5000);

            //Add the necessary JavaScript, CSS files
            resultsBldr.AppendLine("  <script type=\"text/javascript\" src=\"" + Static_Resources_Gateway.Sobekcm_Thumb_Results_Js + "\"></script>");

            // Start this table
            resultsBldr.AppendLine("<div style=\"width:80%;margin-left:auto;margin-right:auto\">");
            resultsBldr.AppendLine("<h2>Timeline View</h2>");


            foreach (iSearch_Title_Result titleResult in PagedResults)
            {
                bool multiple_title = titleResult.Item_Count > 1;

                // Always get the first item for things like the main link and thumbnail
                iSearch_Item_Result firstItemResult = titleResult.Get_Item(0);

                // Determine the internal link to the first (possibly only) item
                string internal_link = base_url + titleResult.BibID + "/" + firstItemResult.VID + textRedirectStem;

                // For browses, just point to the title
                if ((RequestSpecificValues.Current_Mode.Mode == Display_Mode_Enum.Aggregation) && (RequestSpecificValues.Current_Mode.Aggregation_Type == Aggregation_Type_Enum.Browse_Info))
                    internal_link = base_url + titleResult.BibID + textRedirectStem;

                string title;
                if (multiple_title)
                {
                    // Determine term to use
                    string multi_term = "volume";
                    if (titleResult.MaterialType.ToUpper() == "NEWSPAPER")
                    {
                        multi_term = titleResult.Item_Count > 1 ? "issues" : "issue";
                    }
                    else
                    {
                        if (titleResult.Item_Count > 1)
                            multi_term = "volumes";
                    }

                    if ((showDate))
                    {
                        if (firstItemResult.PubDate.Length > 0)
                        {
                            title = "[" + firstItemResult.PubDate + "] " + titleResult.GroupTitle;
                        }
                        else
                        {
                            title = titleResult.GroupTitle;
                        }
                    }
                    else
                    {
                        title = titleResult.GroupTitle + "<br />( " + titleResult.Item_Count + " " + multi_term + " )";
                    }
                }
                else
                {
                    if (showDate)
                    {
                        if (firstItemResult.PubDate.Length > 0)
                        {
                            title = "[" + firstItemResult.PubDate + "] " + firstItemResult.Title;
                        }
                        else
                        {
                            title = firstItemResult.Title;
                        }
                    }
                    else
                    {
                        title = firstItemResult.Title;
                    }
                }

                // Add the title
                resultsBldr.AppendLine("<a href=\"" + internal_link + "\">" + title + "</a><br />");
            }

            // End this div
            resultsBldr.AppendLine("</div>");

            // Add this to the html table
            Literal mainLiteral = new Literal {Text = resultsBldr.ToString()};
            MainPlaceHolder.Controls.Add(mainLiteral);
        }
    }
}
