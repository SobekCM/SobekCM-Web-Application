using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI.WebControls;
using SobekCM.Core.Aggregations;
using SobekCM.Core.Navigation;
using SobekCM.Core.Results;
using SobekCM.Core.Search;
using SobekCM.Core.Users;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Engine_Library.Database;
using SobekCM.Engine_Library.Solr.v5;
using SobekCM.Library.HTML;
using SobekCM.Library.ResultsViewer;
using SobekCM.Library.UI;
using SobekCM.Tools;

namespace SobekCM.Library.AggregationViewer.Viewers
{
    public class Thumbnails_Home_AggregationViewer : abstractAggregationViewer
    {
        private Item_Aggregation hierarchyObject;
        private bool canEditHomePage;
        private bool ifEditNoCkEditor;

        public Thumbnails_Home_AggregationViewer(RequestCache RequestSpecificValues, AggregationViewBag ViewBag) : base(RequestSpecificValues, ViewBag)
        {
            hierarchyObject = ViewBag.Hierarchy_Object;

            // Determine if the user can edit this
            canEditHomePage = (RequestSpecificValues.Current_User != null) && (RequestSpecificValues.Current_User.Is_Aggregation_Admin(hierarchyObject.Code));

            // Special code to determine if CKEditor should be used since it kind of destroys some more custom HTML
            ifEditNoCkEditor = false;
            if ((UI_ApplicationCache_Gateway.Settings.System.System_Abbreviation == "OPENNJ") &&
                (RequestSpecificValues.Current_Mode.Aggregation_Type == Aggregation_Type_Enum.Home_Edit) &&
                ((RequestSpecificValues.Current_Mode.Aggregation.Length == 0) || (RequestSpecificValues.Current_Mode.Aggregation.ToLower() == "all")))
                ifEditNoCkEditor = true;

            // Look for a user setting for 'Aggregation_HtmlSubwriter.Can_Edit_Home_Page' and if that included the aggregation code,
            // this non-admin user can edit the home page.
            if ((!canEditHomePage) && (RequestSpecificValues.Current_User != null) && (RequestSpecificValues.Current_Mode.Aggregation.Length > 0))
            {
                string possible_setting = "|" + (RequestSpecificValues.Current_User.Get_Setting("Aggregation_HtmlSubwriter.Can_Edit_Home_Page", String.Empty)).ToUpper() + "|";

                if (possible_setting.Contains("|" + RequestSpecificValues.Current_Mode.Aggregation.ToUpper() + "|"))
                {
                    RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Constructor", "User can edit home page.");
                    canEditHomePage = true;
                }
            }
        }

        public override Item_Aggregation_Views_Searches_Enum Type => Item_Aggregation_Views_Searches_Enum.Basic_Search;

        /// <summary> Gets the collection of special behaviors which this aggregation viewer  requests from the main HTML subwriter. </summary>
        public override List<HtmlSubwriter_Behaviors_Enum> AggregationViewer_Behaviors
        {
            get
            {
                return new List<HtmlSubwriter_Behaviors_Enum>
                        {
                            HtmlSubwriter_Behaviors_Enum.Aggregation_Suppress_Home_Text                            
                        };
            }
        }

        public override bool Secondary_Text_Requires_Controls => true;


        /// <summary> Add the HTML to be displayed in the search box </summary>
        /// <param name="Output"> Textwriter to write the HTML for this viewer</param>
        /// <param name="Tracer">Trace object keeps a list of each method executed and important milestones in rendering</param>
        public override void Add_Search_Box_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("Thumbnails_Home_AggregationViewer.Add_Search_Box_HTML", "Adding html for search box");
            }

            base.Add_Basic_Search_Box_HTML(Output, Tracer);
        }

        /// <summary> Add the HTML to be displayed below the search box </summary>
        /// <param name="Output"> Textwriter to write the HTML for this viewer</param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
        /// <remarks> This adds the search tips by calling the base method <see cref="abstractAggregationViewer.Add_Simple_Search_Tips"/> </remarks>
        public override void Add_Secondary_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("Thumbnails_Home_AggregationViewer.Add_Secondary_HTML", "Add the search thumbnails to the home page");
            }

            string url_options = UrlWriterHelper.URL_Options(RequestSpecificValues.Current_Mode);
            string urlOptions1 = String.Empty;
            string urlOptions2 = String.Empty;

            if (url_options.Length > 0)
            {
                urlOptions1 = "?" + url_options;
                urlOptions2 = "&" + url_options;
            }

            // Get the raw home hteml text
            string home_html = hierarchyObject.HomePageHtml.Content;

            if ((canEditHomePage) && (RequestSpecificValues.Current_Mode.Aggregation_Type == Aggregation_Type_Enum.Home_Edit))
            {
                string post_url = HttpUtility.HtmlEncode(HttpContext.Current.Items["Original_URL"].ToString());
                Output.WriteLine("<form name=\"home_edit_form\" method=\"post\" action=\"" + post_url + "\" id=\"addedForm\" >");
                Output.WriteLine("  <textarea id=\"sbkAghsw_HomeTextEdit\" name=\"sbkAghsw_HomeTextEdit\" >");
                Output.WriteLine(home_html.Replace("<%", "[%").Replace("%>", "%]"));
                Output.WriteLine("  </textarea>");
                Output.WriteLine();

                Output.WriteLine("<div id=\"sbkAghsw_HomeEditButtons\">");
                RequestSpecificValues.Current_Mode.Aggregation_Type = Aggregation_Type_Enum.Home;
                Output.WriteLine("  <button title=\"Do not apply changes\" class=\"roundbutton\" onclick=\"window.location.href='" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "';return false;\"><img src=\"" + Static_Resources_Gateway.Button_Previous_Arrow_Png + "\" class=\"roundbutton_img_left\" alt=\"\" /> CANCEL</button> &nbsp; &nbsp; ");

                // In some cases, we don't want the HTML editing to use CKEditor, since it can damage the HTML editing from source
                if (ifEditNoCkEditor)
                {
                    Output.WriteLine("  <button title=\"Save changes to this aggregation home page text\" class=\"roundbutton\" type=\"submit\">SAVE <img src=\"" + Static_Resources_Gateway.Button_Next_Arrow_Png + "\" class=\"roundbutton_img_right\" alt=\"\" /></button>");
                }
                else
                {
                    Output.WriteLine("  <button title=\"Save changes to this aggregation home page text\" class=\"roundbutton\" type=\"submit\" onclick=\"for(var i in CKEDITOR.instances) { CKEDITOR.instances[i].updateElement(); }\">SAVE <img src=\"" + Static_Resources_Gateway.Button_Next_Arrow_Png + "\" class=\"roundbutton_img_right\" alt=\"\" /></button>");
                }
                Output.WriteLine("</div>");
                Output.WriteLine("</form>");
                Output.WriteLine("<br /><br /><br />");
                Output.WriteLine();
            }
            else
            {
                Output.WriteLine("<div class=\"SobekText\" role=\"main\" id=\"main-content\">");

                // Add the highlights
                if ((hierarchyObject.Highlights != null) && (hierarchyObject.Highlights.Count > 0))
                {
                    Output.WriteLine(Highlight_To_Html(hierarchyObject.Highlights[0], RequestSpecificValues.Current_Mode.Base_Design_URL + hierarchyObject.ObjDirectory).Replace("<%URLOPTS%>", url_options).Replace("<%?URLOPTS%>", urlOptions1).Replace("<%&URLOPTS%>", urlOptions2));
                }

                // Determine the different counts as strings and replace if they exist
                if ((home_html.Contains("<%PAGES%>")) || (home_html.Contains("<%TITLES%>")) || (home_html.Contains("<%ITEMS%>")))
                {
                    if (hierarchyObject.Statistics == null)
                    {
                        home_html = home_html.Replace("<%PAGES%>", String.Empty).Replace("<%ITEMS%>", String.Empty).Replace("<%TITLES%>", String.Empty);
                    }
                    else
                    {
                        string page_count = Int_To_Comma_String(hierarchyObject.Statistics.Page_Count);
                        string item_count = Int_To_Comma_String(hierarchyObject.Statistics.Item_Count);
                        string title_count = Int_To_Comma_String(hierarchyObject.Statistics.Title_Count);

                        home_html = home_html.Replace("<%PAGES%>", page_count).Replace("<%ITEMS%>", item_count).Replace("<%TITLES%>", title_count);
                    }
                }

                // Replace any item aggregation specific custom directives
                string original_home = home_html;

                if ((hierarchyObject.Custom_Directives != null) && (hierarchyObject.Custom_Directives.Count > 0))
                    home_html = hierarchyObject.Custom_Directives.Keys.Where(original_home.Contains).Aggregate(home_html, (Current, ThisKey) => Current.Replace(ThisKey, hierarchyObject.Custom_Directives[ThisKey].Replacement_HTML));

                // Replace any standard directives last
                home_html = home_html.Replace("<%BASEURL%>", RequestSpecificValues.Current_Mode.Base_URL).Replace("<%URLOPTS%>", url_options).Replace("<%?URLOPTS%>", urlOptions1).Replace("<%&URLOPTS%>", urlOptions2);

                // Output the adjusted home html
                if (canEditHomePage)
                {
                    Output.WriteLine("<div id=\"sbkAghsw_EditableHome\">");
                    Output.WriteLine(home_html);
                    RequestSpecificValues.Current_Mode.Aggregation_Type = Aggregation_Type_Enum.Home_Edit;
                    Output.WriteLine("<div id=\"sbkAghsw_EditableHomeLink\"><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\" title=\"Edit this home text\"><img src=\"" + Static_Resources_Gateway.Edit_Gif + "\" alt=\"\" />edit content</a></div>");
                    RequestSpecificValues.Current_Mode.Aggregation_Type = Aggregation_Type_Enum.Home;
                    Output.WriteLine("</div>");
                    Output.WriteLine();

                    Output.WriteLine("<script>");
                    Output.WriteLine("  $(\"#sbkAghsw_EditableHome\").mouseover(function() { $(\"#sbkAghsw_EditableHomeLink\").css(\"display\",\"inline-block\"); });");
                    Output.WriteLine("  $(\"#sbkAghsw_EditableHome\").mouseout(function() { $(\"#sbkAghsw_EditableHomeLink\").css(\"display\",\"none\"); });");
                    Output.WriteLine("</script>");
                    Output.WriteLine();
                }
                else
                {
                    Output.WriteLine("<div id=\"sbkAghsw_Home\">");
                    Output.WriteLine(home_html);
                    Output.WriteLine("</div>");
                }
            }

            RequestSpecificValues.Current_Mode.Aggregation = hierarchyObject.Code;

            // If there are sub aggregations here, show them
            if (ViewBag.Hierarchy_Object.Children_Count > 0)
            {
                Output.WriteLine("<div class=\"SobekText\">");
                Aggregation_HtmlSubwriter.Add_SubCollection_Buttons(Output, RequestSpecificValues, ViewBag.Hierarchy_Object);
                Output.WriteLine("</div>");
            }
            RequestSpecificValues.Current_Mode.Aggregation = ViewBag.Hierarchy_Object.Code;
        }

        private void add_thumbnails(Custom_Tracer Tracer)
        {
            
        }

        public override void Add_Secondary_Controls(PlaceHolder MainPlaceHolder, Custom_Tracer Tracer)
        {
            // Build the search options
            Search_Options_Info searchOptions = new Search_Options_Info();
            searchOptions.Page = 1;
            searchOptions.ResultsPerPage = 20;
            searchOptions.AggregationCode = hierarchyObject.Code;
            searchOptions.Facets = hierarchyObject.Facets;
            searchOptions.Fields = hierarchyObject.Results_Fields;
            searchOptions.Sort = (ushort)1;

            // Should results be grouped?  Aggregation must be set and for the moment
            searchOptions.GroupItemsByTitle = hierarchyObject.GroupResults;

            Search_Results_Statistics stats;
            List<iSearch_Title_Result> results;

            // Build the user membership information
            Search_User_Membership_Info userInfo = new Search_User_Membership_Info();
            var Current_User = RequestSpecificValues.Current_User;
            if ((Current_User == null) || (!Current_User.LoggedOn))
            {
                Tracer.Add_Trace("Item_Aggregation_Utilities.Get_Browse_Results", "No current user or not logged in.");

                userInfo.LoggedIn = false;
            }
            else
            {
                Tracer.Add_Trace("Item_Aggregation_Utilities.Get_Browse_Results", "User is logged in");

                userInfo.LoggedIn = true;
                userInfo.UserID = userInfo.UserID;
                if (Current_User.User_Groups != null)
                {
                    Tracer.Add_Trace("Item_Aggregation_Utilities.Get_Browse_Results", "User has user groups.");

                    foreach (Simple_User_Group_Info groupInfo in Current_User.User_Groups)
                    {
                        userInfo.Add_User_Group(groupInfo.UserGroupID);
                    }
                }

                if ((Current_User.Is_Host_Admin) || (Current_User.Is_System_Admin) || (Current_User.Is_Portal_Admin))
                {
                    Tracer.Add_Trace("Item_Aggregation_Utilities.Get_Browse_Results", "User is a host, system, or portal admin.");
                    userInfo.Admin = true;
                }
                else if ((Current_User.Is_Aggregation_Admin(hierarchyObject.Code)) || (Current_User.Is_Aggregation_Curator(hierarchyObject.Code)))
                {
                    Tracer.Add_Trace("Item_Aggregation_Utilities.Get_Browse_Results", "User is an aggregation admin or curator");
                    userInfo.Admin = true;
                }
            }

            v5_Solr_Searcher.All_Browse(searchOptions, userInfo, Tracer, out stats, out results);

            Multiple_Paged_Results_Args returnValue = new Multiple_Paged_Results_Args(stats, results);

            if (stats.Total_Items > 0)
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine("<div id=\"Thp_ResultsDesc\" style=\"background-color:#eee; border: #ccc 1px solid;width: 100%\">");
                builder.AppendLine("<div id=\"Thp_ResultsTitle\" style=\"width:30%;display:inline-block; font-size: 1.15em; padding: 12px; padding-left:30px\" >Collection Items</div>");
                if ( stats.Total_Items > results.Count )
                {
                    builder.AppendLine("<div id=\"Thp_ResultsShowing\" style=\"width:30%;display:inline-block; padding: 12px; text-align:center;\" >Showing 20 items out of " + stats.Total_Items + "</div>");

                    RequestSpecificValues.Current_Mode.Aggregation_Type = Aggregation_Type_Enum.Browse_Info;
                    RequestSpecificValues.Current_Mode.Info_Browse_Mode = "all";
                    string url = UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode);
                    RequestSpecificValues.Current_Mode.Aggregation_Type = Aggregation_Type_Enum.Home;

                    builder.AppendLine("<div id=\"Thp_ResultsShowAll\" style=\"width:30%;display:inline-block; padding: 12px; text-align:right;\" ><a href=\"" + url + "\">View all</a></div>");
                }
                builder.AppendLine("</div>");
                MainPlaceHolder.Controls.Add(new Literal() { Text = builder.ToString() });


                var resultsViewer = new Thumbnail_ResultsViewer();
                resultsViewer.PagedResults = results;
                resultsViewer.ResultsStats = stats;
                resultsViewer.RequestSpecificValues = RequestSpecificValues;

                resultsViewer.Add_HTML(MainPlaceHolder, Tracer);
            }

            MainPlaceHolder.Controls.Add(new Literal() { Text = "</div>" });
        }

        private string Highlight_To_Html(Item_Aggregation_Highlights Highlight, string Directory)
        {
            RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Highlight_To_Html", "Entered...");

            StringBuilder highlightBldr = new StringBuilder(500);
            highlightBldr.Append("<span id=\"SobekHighlight\">" + Environment.NewLine);
            highlightBldr.Append("  <table>" + Environment.NewLine);
            highlightBldr.Append("    <tr><td>" + Environment.NewLine);

            if (!String.IsNullOrEmpty(Highlight.Link))
            {
                if (Highlight.Link.IndexOf("?") > 0)
                {
                    highlightBldr.Append("      <a href=\"" + Highlight.Link + "<%&URLOPTS%>\" title=\"" + Highlight.Tooltip + "\">" + Environment.NewLine);
                }
                else
                {
                    highlightBldr.Append("      <a href=\"" + Highlight.Link + "<%?URLOPTS%>\" title=\"" + Highlight.Tooltip + "\">" + Environment.NewLine);
                }

                highlightBldr.Append("        <img src=\"" + Directory + Highlight.Image + "\" alt=\"" + Highlight.Tooltip + "\"/>" + Environment.NewLine);
                highlightBldr.Append("      </a>" + Environment.NewLine);
            }
            else
            {
                highlightBldr.Append("      <img src=\"" + Directory + Highlight.Image + "\" alt=\"" + Highlight.Tooltip + "\"/>" + Environment.NewLine);
            }

            highlightBldr.Append("    </td></tr>" + Environment.NewLine);

            if (!String.IsNullOrEmpty(Highlight.Text))
            {
                highlightBldr.Append("    <tr><td>" + Environment.NewLine);

                if (!String.IsNullOrEmpty(Highlight.Link))
                {
                    if (Highlight.Link.IndexOf("?") > 0)
                    {
                        highlightBldr.Append("      <a href=\"" + Highlight.Link + "<%&URLOPTS%>\" title=\"" + Highlight.Tooltip + "\">" + Environment.NewLine);
                    }
                    else
                    {
                        highlightBldr.Append("      <a href=\"" + Highlight.Link + "<%?URLOPTS%>\" title=\"" + Highlight.Tooltip + "\">" + Environment.NewLine);
                    }

                    highlightBldr.Append("        <span class=\"SobekHighlightText\"> " + Highlight.Text + " </span>" + Environment.NewLine);
                    highlightBldr.Append("      </a>" + Environment.NewLine);
                }
                else
                {
                    highlightBldr.Append("      <span class=\"SobekHighlightText\"> " + Highlight.Text + " </span>" + Environment.NewLine);
                }

                highlightBldr.Append("    </td></tr>" + Environment.NewLine);
            }

            highlightBldr.Append("  </table>" + Environment.NewLine);
            highlightBldr.Append("</span>");

            return highlightBldr.ToString();
        }

        private string Int_To_Comma_String(int Value)
        {
            if (Value < 1000)
                return Value.ToString();

            string value_string = Value.ToString();
            if ((Value >= 1000) && (Value < 1000000))
            {
                return value_string.Substring(0, value_string.Length - 3) + "," + value_string.Substring(value_string.Length - 3);
            }

            return value_string.Substring(0, value_string.Length - 6) + "," + value_string.Substring(value_string.Length - 6, 3) + "," + value_string.Substring(value_string.Length - 3);
        }
    }
}
