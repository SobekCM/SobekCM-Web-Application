#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.Aggregations;
using SobekCM.Core.Navigation;
using SobekCM.Core.Configuration.Localization;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Library.Database;
using SobekCM.Library.HTML;
using SobekCM.Library.Localization;
using SobekCM.Library.MainWriters;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;

#endregion

namespace SobekCM.Library.AggregationViewer.Viewers
{
    /// <summary> Renders the usage statistics for a single aggregation </summary>
    /// <remarks> This class implements the <see cref="iAggregationViewer"/> interface and extends the <see cref="abstractAggregationViewer"/> class.<br /><br />
    /// Aggregation viewers are used when displaying aggregation home pages, searches, browses, and information pages.<br /><br />
    /// During a valid html request to display the usage statistics page, the following steps occur:
    /// <ul>
    /// <li>Application state is built/verified by the Application_State_Builder </li>
    /// <li>Request is analyzed by the QueryString_Analyzer and output as a <see cref="Navigation_Object"/>  </li>
    /// <li>Main writer is created for rendering the output, in this case the <see cref="Html_MainWriter"/> </li>
    /// <li>The HTML writer will create the necessary subwriter.  For a collection-level request, an instance of the  <see cref="Aggregation_HtmlSubwriter"/> class is created. </li>
    /// <li>To display the requested collection view, the collection subwriter will creates an instance of this class </li>
    /// </ul></remarks>
    public class Usage_Statistics_AggregationViewer : abstractAggregationViewer
    {
        /// <summary> Constructor for a new instance of the Usage_Statistics_AggregationViewer class </summary>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        /// <param name="ViewBag"> Aggregation-specific request information, such as aggregation object and any browse object requested </param>
        public Usage_Statistics_AggregationViewer(RequestCache RequestSpecificValues, AggregationViewBag ViewBag, HttpContext Context)
            : base(RequestSpecificValues, ViewBag, Context)
        {
            // Everything done in base class constructor
        }

        /// <summary>Flag indicates whether the subaggregation selection panel is displayed for this collection viewer</summary>
        /// <value> This property always returns the <see cref="Selection_Panel_Display_Enum.Never"/> enumerational value </value>
        public override Selection_Panel_Display_Enum Selection_Panel_Display
        {
            get
            {
                return Selection_Panel_Display_Enum.Never;
            }
        }

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

        /// <summary> Gets the type of collection view or search supported by this collection viewer </summary>
        /// <value> This returns the <see cref="Item_Aggregation_Views_Searches_Enum.Usage_Statistics"/> enumerational value </value>
        public override Item_Aggregation_Views_Searches_Enum Type
        {
            get { return Item_Aggregation_Views_Searches_Enum.Usage_Statistics; }
        }

        /// <summary> Gets flag which indicates whether this is an internal view, which may have a 
        /// slightly different design feel </summary>
        /// <remarks> This returns FALSE by default, but can be overriden by individual viewer implementations</remarks>
        public override bool Is_Internal_View
        {
            get { return true; }
        }

        /// <summary> Title for the page that displays this viewer, this is shown in the search box at the top of the page, just below the banner </summary>
        public override string Viewer_Title
        {
            get
            {
                // Normalize the submode
                string submode = "views";
                if (!String.IsNullOrEmpty(RequestSpecificValues.Current_Mode.Info_Browse_Mode))
                    submode = RequestSpecificValues.Current_Mode.Info_Browse_Mode.ToLower();

                if ((submode != "views") && (submode != "itemviews") && (submode != "titles") && (submode != "items") && (submode != "definitions"))
                {
                    submode = "views";
                }

                // Show the next data, depending on type
                string language = RequestSpecificValues.Current_Mode.Language;
                switch (submode)
                {
                    case "views":
                        return Localization_Gateway.Usage_Statistics.Title_Collection_Views(language);

                    case "itemviews":
                        return Localization_Gateway.Usage_Statistics.Title_Item_Views(language);

                    case "titles":
                        return Localization_Gateway.Usage_Statistics.Title_Top_Titles(language);

                    case "items":
                        return Localization_Gateway.Usage_Statistics.Title_Top_Items(language);

                    case "definitions":
                        return Localization_Gateway.Usage_Statistics.Title_Definitions(language);

                    default:
                        return Localization_Gateway.Usage_Statistics.Title_Collection_Views(language);
                }
            }
        }

        /// <summary> Gets the URL for the icon related to this aggregational viewer task </summary>
        public override string Viewer_Icon
        {
            get { return Static_Resources_Gateway.Usage_Img; }
        }

        /// <summary> Add the HTML to be displayed in the search box </summary>
        /// <param name="Output"> Textwriter to write the HTML for this viewer</param>
        /// <param name="Tracer">Trace object keeps a list of each method executed and important milestones in rendering</param>
        /// <remarks> This adds the title of the into the box </remarks>
        public override void Write_Search_Box_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            // Do nothing
        }


        /// <summary> Add the HTML to be displayed below the search box </summary>
        /// <param name="Output"> Textwriter to write the HTML for this viewer</param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
        /// <remarks> This writes the HTML from the static browse or info page here  </remarks>
        public override void Write_Main_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer?.Add_Trace("Usage_Statistics_AggregationViewer.Write_Main_HTML", "Adding HTML");

            string language = RequestSpecificValues.Current_Mode.Language;
            string COLLECTION_VIEWS = Localization_Gateway.Usage_Statistics.Tab_Collection_Views(language);
            string ITEM_VIEWS = Localization_Gateway.Usage_Statistics.Tab_Item_Views(language);
            string TOP_TITLES = Localization_Gateway.Usage_Statistics.Tab_Top_Titles(language);
            string TOP_ITEMS = Localization_Gateway.Usage_Statistics.Tab_Top_Items(language);
            string DEFINITIONS = Localization_Gateway.Usage_Statistics.Tab_Definitions(language);

            Output.WriteLine("<div class=\"ShowSelectRow\">");
            Output.WriteLine("  <ul class=\"sbk_FauxDownwardTabsList\">");

            // Save and normalize the submode
            string submode = "views";
            if (!String.IsNullOrEmpty(RequestSpecificValues.Current_Mode.Info_Browse_Mode))
                submode = RequestSpecificValues.Current_Mode.Info_Browse_Mode.ToLower();
            if ((submode != "views") && (submode != "itemviews") && (submode != "titles") && (submode != "items") && (submode != "definitions"))
            {
                submode = "views";
            }


            if (submode == "views")
            {
                Output.WriteLine("    <li class=\"current\">" + COLLECTION_VIEWS + "</li>");
            }
            else
            {
                RequestSpecificValues.Current_Mode.Info_Browse_Mode = "views";
                Output.WriteLine("    <li><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">" + COLLECTION_VIEWS + "</a></li>");
            }

            if (submode == "itemviews")
            {
                Output.WriteLine("    <li class=\"current\">" + ITEM_VIEWS + "</li>");
            }
            else
            {
                RequestSpecificValues.Current_Mode.Info_Browse_Mode = "itemviews";
                Output.WriteLine("    <li><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">" + ITEM_VIEWS + "</a></li>");
            }

            if (submode == "titles")
            {
                Output.WriteLine("    <li class=\"current\">" + TOP_TITLES + "</li>");
            }
            else
            {
                RequestSpecificValues.Current_Mode.Info_Browse_Mode = "titles";
                Output.WriteLine("    <li><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">" + TOP_TITLES + "</a></li>");
            }

            if (submode == "items")
            {
                Output.WriteLine("    <li class=\"current\">" + TOP_ITEMS + "</li>");
            }
            else
            {
                RequestSpecificValues.Current_Mode.Info_Browse_Mode = "items";
                Output.WriteLine("    <li><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">" + TOP_ITEMS + "</a></li>");
            }

            if (submode == "definitions")
            {
                Output.WriteLine("    <li class=\"current\">" + DEFINITIONS + "</li>");
            }
            else
            {
                RequestSpecificValues.Current_Mode.Info_Browse_Mode = "definitions";
                Output.WriteLine("    <li><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">" + DEFINITIONS + "</a></li>");
            }
            RequestSpecificValues.Current_Mode.Info_Browse_Mode = submode;
            Output.WriteLine("  </ul>");
            Output.WriteLine("</div>");
            Output.WriteLine("<br />");

            // Show the next data, depending on type
            switch (submode)
            {
                case "views":
                    add_collection_usage_history(Output, SobekCM_Database.Get_Aggregation_Statistics_History(ViewBag.Hierarchy_Object.Code, Tracer), Tracer);
                    break;

                case "itemviews":
                    add_item_usage_history(Output, SobekCM_Database.Get_Aggregation_Statistics_History(ViewBag.Hierarchy_Object.Code, Tracer), Tracer);
                    break;

                case "titles":
                    add_titles_by_collection(Output, ViewBag.Hierarchy_Object.Code, Tracer);
                    break;

                case "items":
                    add_items_by_collection(Output, ViewBag.Hierarchy_Object.Code, Tracer);
                    break;

                case "definitions":
                    add_usage_definitions(Output, Tracer);
                    break;
            }
        }

        private string Month_From_Int(int Month_Int)
        {
            return Localization_Gateway.Usage_Statistics.Month(Month_Int, RequestSpecificValues.Current_Mode.Language);
        }

        #region Method to add collection history as html

        private void add_collection_usage_history(TextWriter Output, DataTable StatsCount, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace("Usage_Statistics_AggregationViewer.add_collection_history", "Rendering HTML");

            string language = RequestSpecificValues.Current_Mode.Language;

            Output.WriteLine("<div class=\"SobekText\">");
            Output.WriteLine("<p>" + Localization_Gateway.Usage_Statistics.Collection_History_Intro(language) + "</p>");

            RequestSpecificValues.Current_Mode.Info_Browse_Mode = "definitions";
            Output.WriteLine("<p>" + String.Format(Localization_Gateway.Usage_Statistics.Definitions_Link_Sentence(language), UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode)) + "</p>");
            Output.WriteLine("</div>");
            Output.WriteLine("<center>");

            Output.WriteLine("  <table border=\"0px\" cellspacing=\"0px\" class=\"statsTable\">");
            Output.WriteLine("    <tr align=\"right\" bgcolor=\"#0022a7\" >");
            Output.WriteLine("      <th width=\"120px\" align=\"left\"><span style=\"color: White\">" + Localization_Gateway.Usage_Statistics.Date(language) + "</span></th>");
            Output.WriteLine("      <th width=\"90px\" align=\"right\"><span style=\"color: White\">" + Localization_Gateway.Usage_Statistics.Total_Views_Html(language) + "</span></th>");
            Output.WriteLine("      <th width=\"90px\" align=\"right\"><span style=\"color: White\">" + Localization_Gateway.Usage_Statistics.Visits(language) + "</span></th>");
            Output.WriteLine("      <th width=\"90px\" align=\"right\"><span style=\"color: White\">" + Localization_Gateway.Usage_Statistics.Main_Pages_Html(language) + "</span></th>");
            Output.WriteLine("      <th width=\"90px\" align=\"right\"><span style=\"color: White\">" + Localization_Gateway.Usage_Statistics.Browses(language) + "</span></th>");
            Output.WriteLine("      <th width=\"90px\" align=\"right\"><span style=\"color: White\">" + Localization_Gateway.Usage_Statistics.Search_Results_Html(language) + "</span></th>");
            Output.WriteLine("      <th width=\"90px\" align=\"right\"><span style=\"color: White\">" + Localization_Gateway.Usage_Statistics.Title_Views_Html(language) + "</span></th>");
            Output.WriteLine("      <th width=\"90px\" align=\"right\"><span style=\"color: White\">" + Localization_Gateway.Usage_Statistics.Item_Views_Html(language) + "</span></th>");
            Output.WriteLine("    </tr>");

            const int COLUMNS = 8;
            string lastYear = String.Empty;
            int hits = 0;
            int sessions = 0;
            int mainPages = 0;
            int browses = 0;
            int searchResults = 0;
            int titleHits = 0;
            int itemHits = 0;

            // Add the collection level information
            if (StatsCount != null)
            {
                foreach (DataRow thisRow in StatsCount.Rows)
                {
                    if (thisRow[0].ToString() != lastYear)
                    {
                        Output.WriteLine("    <tr><td bgcolor=\"#7d90d5\" colspan=\"" + COLUMNS + "\"><span style=\"color: White\"><b> " + thisRow[0] + Localization_Gateway.Usage_Statistics.Year_Statistics_Suffix(language) + "</b></span></td></tr>");
                        lastYear = thisRow[0].ToString();
                    }
                    else
                    {
                        Output.WriteLine("    <tr><td bgcolor=\"#e7e7e7\" colspan=\"" + COLUMNS + "\"></td></tr>");
                    }
                    Output.WriteLine("    <tr align=\"right\" >");
                    Output.WriteLine("      <td align=\"left\">" + Month_From_Int(Convert.ToInt32(thisRow[1])) + " " + thisRow[0] + "</td>");

                    hits += Convert.ToInt32(thisRow[2]);
                    Output.WriteLine("      <td>" + thisRow[2] + "</td>");

                    sessions += Convert.ToInt32(thisRow[3]);
                    Output.WriteLine("      <td>" + thisRow[3] + "</td>");

                    int thisRowMainPage = Convert.ToInt32(thisRow[4]) + Convert.ToInt32(thisRow[6]);
                    mainPages += thisRowMainPage;
                    Output.WriteLine("      <td>" + thisRowMainPage + "</td>");

                    browses += Convert.ToInt32(thisRow[5]);
                    Output.WriteLine("      <td>" + thisRow[5] + "</td>");

                    searchResults += Convert.ToInt32(thisRow[7]);
                    Output.WriteLine("      <td>" + thisRow[7] + "</td>");

                    if (thisRow[8] != DBNull.Value)
                    {
                        titleHits += Convert.ToInt32(thisRow[8]);
                        Output.WriteLine("      <td>" + thisRow[8] + "</td>");
                    }
                    else
                    {
                        Output.WriteLine("      <td>0</td>");
                    }
                    if (thisRow[9] != DBNull.Value)
                    {
                        itemHits += Convert.ToInt32(thisRow[9]);
                        Output.WriteLine("      <td>" + thisRow[9] + "</td>");
                    }
                    else
                    {
                        Output.WriteLine("      <td>0</td>");
                    }

                    Output.WriteLine("    </tr>");
                }
                Output.WriteLine("    <tr><td bgcolor=\"Black\" colspan=\"" + COLUMNS + "\"></td></tr>");
                Output.WriteLine("    <tr align=\"right\" >");
                Output.WriteLine("      <td align=\"left\"><b>" + Localization_Gateway.Usage_Statistics.Total(language) + "</b></td>");
                Output.WriteLine("      <td><b>" + hits + "</td>");
                Output.WriteLine("      <td><b>" + sessions + "</td>");
                Output.WriteLine("      <td><b>" + mainPages + "</td>");
                Output.WriteLine("      <td><b>" + browses + "</td>");
                Output.WriteLine("      <td><b>" + searchResults + "</td>");
                Output.WriteLine("      <td><b>" + titleHits + "</td>");
                Output.WriteLine("      <td><b>" + itemHits + "</td>");
                Output.WriteLine("    </tr>");
                Output.WriteLine("  </table>");
            }

            Output.WriteLine("  <br /> <br />");
            Output.WriteLine("</center>");
        }

        #endregion

        #region Method to add item usage history as html

        private void add_item_usage_history(TextWriter Output, DataTable StatsCount, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace("Usage_Statistics_AggregationViewer.add_collection_history", "Rendering HTML");

            string language = RequestSpecificValues.Current_Mode.Language;

            Output.WriteLine("<div class=\"SobekText\">");
            Output.WriteLine("<p>" + Localization_Gateway.Usage_Statistics.Item_History_Intro(language) + "</p>");

            RequestSpecificValues.Current_Mode.Info_Browse_Mode = "definitions";
            Output.WriteLine("<p>" + String.Format(Localization_Gateway.Usage_Statistics.Definitions_Link_Sentence(language), UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode)) + "</p>");
            Output.WriteLine("</div>");
            Output.WriteLine("<center>");


            int jpegViews = 0;
            int zoomViews = 0;
            int thumbViews = 0;
            int flashViews = 0;
            int googleMapViews = 0;
            int downloadViews = 0;
            int citationViews = 0;
            int textSearchViews = 0;
            int staticViews = 0;

            Output.WriteLine("  <table border=\"0px\" cellspacing=\"0px\" class=\"statsTable\">");
            Output.WriteLine("    <tr align=\"right\" bgcolor=\"#0022a7\" >");
            Output.WriteLine("      <th width=\"120px\" align=\"left\"><span style=\"color: White\">" + Localization_Gateway.Usage_Statistics.Date(language) + "</span></th>");
            Output.WriteLine("      <th width=\"90px\" align=\"right\"><span style=\"color: White\">" + Localization_Gateway.Usage_Statistics.Jpeg_Views_Html(language) + "</span></th>");
            Output.WriteLine("      <th width=\"90px\" align=\"right\"><span style=\"color: White\">" + Localization_Gateway.Usage_Statistics.Zoomable_Views_Html(language) + "</span></th>");
            Output.WriteLine("      <th width=\"90px\" align=\"right\"><span style=\"color: White\">" + Localization_Gateway.Usage_Statistics.Citation_Views_Html(language) + "</span></th>");
            Output.WriteLine("      <th width=\"90px\" align=\"right\"><span style=\"color: White\">" + Localization_Gateway.Usage_Statistics.Thumbnail_Views_Html(language) + "</span></th>");
            Output.WriteLine("      <th width=\"90px\" align=\"right\"><span style=\"color: White\">" + Localization_Gateway.Usage_Statistics.Text_Searches_Html(language) + "</span></th>");
            Output.WriteLine("      <th width=\"90px\" align=\"right\"><span style=\"color: White\">" + Localization_Gateway.Usage_Statistics.Flash_Views_Html(language) + "</span></th>");
            Output.WriteLine("      <th width=\"90px\" align=\"right\"><span style=\"color: White\">" + Localization_Gateway.Usage_Statistics.Map_Views_Html(language) + "</span></th>");
            Output.WriteLine("      <th width=\"90px\" align=\"right\"><span style=\"color: White\">" + Localization_Gateway.Usage_Statistics.Download_Views_Html(language) + "</span></th>");
            Output.WriteLine("      <th width=\"90px\" align=\"right\"><span style=\"color: White\">" + Localization_Gateway.Usage_Statistics.Static_Views_Html(language) + "</span></th>");
            Output.WriteLine("    </tr>");

            const int COLUMNS = 10;
            string lastYear = String.Empty;
            if (StatsCount != null)
            {
                foreach (DataRow thisRow in StatsCount.Rows)
                {
                    if (thisRow[0].ToString() != lastYear)
                    {
                        Output.WriteLine("    <tr><td bgcolor=\"#7d90d5\" colspan=\"" + COLUMNS + "\"><span style=\"color: White\"><b> " + thisRow[0] + Localization_Gateway.Usage_Statistics.Year_Statistics_Suffix(language) + "</b></span></td></tr>");
                        lastYear = thisRow[0].ToString();
                    }
                    else
                    {
                        Output.WriteLine("    <tr><td bgcolor=\"#e7e7e7\" colspan=\"" + COLUMNS + "\"></td></tr>");
                    }
                    Output.WriteLine("    <tr align=\"right\" >");
                    Output.WriteLine("      <td align=\"left\">" + Month_From_Int(Convert.ToInt32(thisRow[1])) + " " + thisRow[0] + "</td>");

                    if (thisRow[10] != DBNull.Value)
                    {
                        jpegViews += Convert.ToInt32(thisRow[10]);
                        Output.WriteLine("      <td>" + thisRow[10] + "</td>");
                    }
                    else
                    {
                        Output.WriteLine("      <td>0</td>");
                    }
                    if (thisRow[11] != DBNull.Value)
                    {
                        zoomViews += Convert.ToInt32(thisRow[11]);
                        Output.WriteLine("      <td>" + thisRow[11] + "</td>");
                    }
                    else
                    {
                        Output.WriteLine("      <td>0</td>");
                    }
                    if (thisRow[12] != DBNull.Value)
                    {
                        citationViews += Convert.ToInt32(thisRow[12]);
                        Output.WriteLine("      <td>" + thisRow[12] + "</td>");
                    }
                    else
                    {
                        Output.WriteLine("      <td>0</td>");
                    }
                    if (thisRow[13] != DBNull.Value)
                    {
                        thumbViews += Convert.ToInt32(thisRow[13]);
                        Output.WriteLine("      <td>" + thisRow[13] + "</td>");
                    }
                    else
                    {
                        Output.WriteLine("      <td>0</td>");
                    }
                    if (thisRow[14] != DBNull.Value)
                    {
                        textSearchViews += Convert.ToInt32(thisRow[14]);
                        Output.WriteLine("      <td>" + thisRow[14] + "</td>");
                    }
                    else
                    {
                        Output.WriteLine("      <td>0</td>");
                    }
                    if (thisRow[15] != DBNull.Value)
                    {
                        flashViews += Convert.ToInt32(thisRow[15]);
                        Output.WriteLine("      <td>" + thisRow[15] + "</td>");
                    }
                    else
                    {
                        Output.WriteLine("      <td>0</td>");
                    }
                    if (thisRow[16] != DBNull.Value)
                    {
                        googleMapViews += Convert.ToInt32(thisRow[16]);
                        Output.WriteLine("      <td>" + thisRow[16] + "</td>");
                    }
                    else
                    {
                        Output.WriteLine("      <td>0</td>");
                    }
                    if (thisRow[17] != DBNull.Value)
                    {
                        downloadViews += Convert.ToInt32(thisRow[17]);
                        Output.WriteLine("      <td>" + thisRow[17] + "</td>");
                    }
                    else
                    {
                        Output.WriteLine("      <td>0</td>");
                    }
                    if (thisRow[18] != DBNull.Value)
                    {
                        staticViews += Convert.ToInt32(thisRow[18]);
                        Output.WriteLine("      <td>" + thisRow[18] + "</td>");
                    }
                    else
                    {
                        Output.WriteLine("      <td>0</td>");
                    }
                    Output.WriteLine("    </tr>");
                }

                Output.WriteLine("    <tr><td bgcolor=\"Black\" colspan=\"" + COLUMNS + "\"></td></tr>");
                Output.WriteLine("    <tr align=\"right\" >");
                Output.WriteLine("      <td align=\"left\"><b>" + Localization_Gateway.Usage_Statistics.Total(language) + "</b></td>");
                Output.WriteLine("      <td><b>" + jpegViews + "</td>");
                Output.WriteLine("      <td><b>" + zoomViews + "</td>");
                Output.WriteLine("      <td><b>" + citationViews + "</td>");
                Output.WriteLine("      <td><b>" + thumbViews + "</td>");
                Output.WriteLine("      <td><b>" + textSearchViews + "</td>");
                Output.WriteLine("      <td><b>" + flashViews + "</td>");
                Output.WriteLine("      <td><b>" + googleMapViews + "</td>");
                Output.WriteLine("      <td><b>" + downloadViews + "</td>");
                Output.WriteLine("      <td><b>" + staticViews + "</td>");
                Output.WriteLine("    </tr>");
                Output.WriteLine("  </table>");
            }
            Output.WriteLine("  <br /> <br />");
            Output.WriteLine("</center>");
        }

        #endregion

        #region Method to add the usage defintions

        private void add_usage_definitions(TextWriter Output, Custom_Tracer Tracer)
        {
            // See if the FAQ is present for this collection
            string directory = UI_ApplicationCache_Gateway.Settings.Servers.Base_Design_Location + "\\extra\\stats";
            string usageDefinitions = String.Empty;
            if (Directory.Exists(directory))
            {
                if (File.Exists(directory + "\\stats_usage_definitions.txt"))
                {
                    Tracer?.Add_Trace("Usage_Statistics_AggregationViewer.add_usage_definitions", "Loading usage definitions");

                    try
                    {
                        var faqReader = new StreamReader(directory + "\\stats_usage_definitions.txt");
                        usageDefinitions = faqReader.ReadToEnd();
                        faqReader.Close();
                    }
                    catch (Exception)
                    {
                        // If there is an error here, no problem.. just uses the default
                    }
                }
            }

            if (usageDefinitions.Length > 0)
            {
                string urloptions = UrlWriterHelper.URL_Options(RequestSpecificValues.Current_Mode);
                if (urloptions.Length > 0)
                    urloptions = "?" + urloptions;

                string sysName = RequestSpecificValues.Current_Mode.Portal_Name;

                Tracer?.Add_Trace("Usage_Statistics_AggregationViewer.add_usage_definitions", "Rendering HTML read from source file");
                Output.WriteLine("<div class=\"SobekText\">");
                Output.WriteLine(usageDefinitions.Replace("<%BASEURL%>", RequestSpecificValues.Current_Mode.Base_URL).Replace("<%?URLOPTS%>", urloptions).Replace("<%SYSNAME%>", sysName));
                Output.WriteLine("</div>");

            }
            else
            {
                Tracer?.Add_Trace("Usage_Statistics_AggregationViewer.add_usage_definitions", "Rendering Default HTML");
                Output.WriteLine("<div class=\"SobekText\">");
                Output.WriteLine(String.Format(Localization_Gateway.Usage_Statistics.Definitions_Html(RequestSpecificValues.Current_Mode.Language), RequestSpecificValues.Current_Mode.Portal_Abbreviation));
                Output.WriteLine("</div>");
            }
        }

        #endregion

        #region Method to add the list of most used items by collection

        private void add_items_by_collection(TextWriter Output, string Collection, Custom_Tracer Tracer)
        {
            DataSet itemsListSet = SobekCM_Database.Statistics_Aggregation_Titles(Collection, Tracer);
            DataTable itemsList = itemsListSet.Tables[0];

            Tracer.Add_Trace("Usage_Statistics_AggregationViewer.add_items_by_collection", "Rendering HTML");

            string language = RequestSpecificValues.Current_Mode.Language;

            Output.WriteLine("<div class=\"SobekText\">");
            Output.WriteLine("<p>" + Localization_Gateway.Usage_Statistics.Items_By_Collection_Intro(language) + "</p>");

            RequestSpecificValues.Current_Mode.Info_Browse_Mode = "definitions";
            Output.WriteLine("<p>" + String.Format(Localization_Gateway.Usage_Statistics.Definitions_Link_Sentence(language), UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode)) + "</p>");

            Output.WriteLine();

            Output.WriteLine("<center>");
            Output.WriteLine("<table border=\"0px\" cellspacing=\"0px\" class=\"statsTable\">");
            Output.WriteLine("  <tr align=\"left\" bgcolor=\"#0022a7\" >");
            Output.WriteLine("    <th width=\"90px\" align=\"left\"><span style=\"color: White\">" + Localization_Gateway.Usage_Statistics.Bibid(language) + "</span></th>");
            Output.WriteLine("    <th width=\"50px\" align=\"left\"><span style=\"color: White\">" + Localization_Gateway.Usage_Statistics.Vid(language) + "</span></th>");
            Output.WriteLine("    <th width=\"430px\" align=\"left\"><span style=\"color: White\">" + Localization_Gateway.Usage_Statistics.Title(language) + "</span></th>");
            Output.WriteLine("    <th width=\"90px\" align=\"right\"><span style=\"color: White\">" + Localization_Gateway.Usage_Statistics.Views(language) + "</span></th>");
            Output.WriteLine("  </tr>");

            if (itemsList != null)
            {
                int itemCount = 0;
                foreach (DataRow thisRow in itemsList.Rows)
                {
                    if (itemCount == 100)
                        break;

                    Output.WriteLine("  <tr align=\"left\" >");
                    Output.WriteLine("    <td>" + thisRow[0] + "</td>");
                    Output.WriteLine("    <td>" + thisRow[1] + "</td>");
                    Output.WriteLine("    <td><a href=\"" + RequestSpecificValues.Current_Mode.Base_URL + thisRow[0] + "/" + thisRow[1] + "\">" + thisRow[2] + "</a></td>");
                    Output.WriteLine("    <td align=\"right\">" + thisRow[3] + "</td>");
                    Output.WriteLine("  </tr>");
                    Output.WriteLine("  <tr><td bgcolor=\"#e7e7e7\" colspan=\"4\"></td></tr>");
                    itemCount++;
                }
            }

            Output.WriteLine("</table>");
            Output.WriteLine("</center>");
            Output.WriteLine("<br /> <br />");
            Output.WriteLine("</div>");
        }
        #endregion

        #region Method to add the list of most used titles by collection

        private void add_titles_by_collection(TextWriter Output, string Collection, Custom_Tracer Tracer)
        {
            DataSet itemsListSet = SobekCM_Database.Statistics_Aggregation_Titles(Collection, Tracer);
            DataTable titleList = itemsListSet.Tables[1];

            Tracer.Add_Trace("Usage_Statistics_AggregationViewer.add_titles_by_collection", "Rendering HTML");

            string language = RequestSpecificValues.Current_Mode.Language;

            Output.WriteLine("<div class=\"SobekText\">");
            Output.WriteLine("<p>" + Localization_Gateway.Usage_Statistics.Titles_By_Collection_Intro(language) + "</p>");

            RequestSpecificValues.Current_Mode.Info_Browse_Mode = "definitions";
            Output.WriteLine("<p>" + String.Format(Localization_Gateway.Usage_Statistics.Definitions_Link_Sentence(language), UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode)) + "</p>");
            Output.WriteLine();

            Output.WriteLine("<center>");
            Output.WriteLine("<table border=\"0px\" cellspacing=\"0px\" class=\"statsTable\">");
            Output.WriteLine("  <tr align=\"left\" bgcolor=\"#0022a7\" >");
            Output.WriteLine("    <th width=\"90px\" align=\"left\"><span style=\"color: White\">" + Localization_Gateway.Usage_Statistics.Bibid(language) + "</span></th>");
            Output.WriteLine("    <th width=\"480px\" align=\"left\"><span style=\"color: White\">" + Localization_Gateway.Usage_Statistics.Title(language) + "</span></th>");
            Output.WriteLine("    <th width=\"90px\" align=\"right\"><span style=\"color: White\">" + Localization_Gateway.Usage_Statistics.Views(language) + "</span></th>");
            Output.WriteLine("  </tr>");

            if (titleList != null)
            {
                int itemCount = 0;
                foreach (DataRow thisRow in titleList.Rows)
                {
                    if (itemCount == 100)
                        break;

                    Output.WriteLine("  <tr align=\"left\" >");
                    Output.WriteLine("    <td>" + thisRow[0] + "</td>");
                    Output.WriteLine("    <td><a href=\"" + RequestSpecificValues.Current_Mode.Base_URL + thisRow[0] + "\">" + thisRow[1] + "</a></td>");
                    Output.WriteLine("    <td align=\"right\">" + thisRow[2] + "</td>");
                    Output.WriteLine("  </tr>");
                    Output.WriteLine("  <tr><td bgcolor=\"#e7e7e7\" colspan=\"3\"></td></tr>");
                    itemCount++;
                }
            }

            Output.WriteLine("</table>");
            Output.WriteLine("</center>");
            Output.WriteLine("<br /> <br />");
            Output.WriteLine("</div>");
        }


        #endregion
    }
}
