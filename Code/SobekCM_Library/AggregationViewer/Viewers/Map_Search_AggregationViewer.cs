#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.Aggregations;
using SobekCM.Core.Configuration.Localization;
using SobekCM.Core.Navigation;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Library.HTML;
using SobekCM.Library.Localization;
using SobekCM.Library.MainWriters;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

#endregion

namespace SobekCM.Library.AggregationViewer.Viewers
{
    /// <summary> Renders the google map search page for a given item aggregation </summary>
    /// <remarks> This class implements the <see cref="iAggregationViewer"/> interface and extends the <see cref="abstractAggregationViewer"/> class.<br /><br />
    /// Collection viewers are used when displaying collection home pages, searches, browses, and information pages.<br /><br />
    /// During a valid html request to display the google map search page, the following steps occur:
    /// <ul>
    /// <li>Application state is built/verified by the Application_State_Builder </li>
    /// <li>Request is analyzed by the QueryString_Analyzer and output as a <see cref="Navigation_Object"/> </li>
    /// <li>Main writer is created for rendering the output, in this case the <see cref="Html_MainWriter"/> </li>
    /// <li>The HTML writer will create the necessary subwriter.  For a collection-level request, an instance of the  <see cref="Aggregation_HtmlSubwriter"/> class is created. </li>
    /// <li>To display the requested collection view, the collection subwriter will creates an instance of this class </li>
    /// </ul></remarks>
    public class Map_Search_AggregationViewer : abstractAggregationViewer
    {
        private readonly int mapHeight;
        private readonly string text1 = String.Empty;
        private readonly string text2 = String.Empty;
        private readonly string text3 = String.Empty;
        private readonly string text4 = String.Empty;

        private bool pointSearchingDisabled = false;

        /// <summary> Constructor for a new instance of the Map_Search_AggregationViewer class </summary>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        /// <param name="ViewBag"> Aggregation-specific request information, such as aggregation object and any browse object requested </param>
        public Map_Search_AggregationViewer(RequestCache RequestSpecificValues, AggregationViewBag ViewBag, HttpContext Context)
            : base(RequestSpecificValues, ViewBag, Context)
        {
            // Compute the redirect stem to use
            string fields = RequestSpecificValues.Current_Mode.Search_Fields;
            string search_string = RequestSpecificValues.Current_Mode.Search_String;
            RequestSpecificValues.Current_Mode.Search_String = String.Empty;
            RequestSpecificValues.Current_Mode.Search_Fields = String.Empty;
            RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Results;
            RequestSpecificValues.Current_Mode.Search_Type = Search_Type_Enum.Map;
            RequestSpecificValues.Current_Mode.Search_Precision = Search_Precision_Type_Enum.Inflectional_Form;
            string redirect_stem = UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode);
            RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Search;
            RequestSpecificValues.Current_Mode.Search_String = search_string;
            RequestSpecificValues.Current_Mode.Search_Fields = fields;
            // Now, populate the search terms, if there was one or some
            text1 = String.Empty;
            text2 = String.Empty;
            text3 = String.Empty;
            text4 = String.Empty;
            if (RequestSpecificValues.Current_Mode.Search_String.Length > 0)
            {
                string[] splitter = RequestSpecificValues.Current_Mode.Search_String.Split(",".ToCharArray());
                bool isNumber = true;
                foreach (char thisChar in splitter[0])
                {
                    if ((!Char.IsDigit(thisChar)) && (thisChar != '.') && (thisChar != '-'))
                        isNumber = false;
                }
                if (isNumber)
                {
                    text1 = splitter[0].Replace(" =", " or ");


                    if (splitter.Length > 1)
                    {
                        foreach (char thisChar in splitter[1])
                        {
                            if ((!Char.IsDigit(thisChar)) && (thisChar != '.') && (thisChar != '-'))
                                isNumber = false;
                        }
                        if (isNumber)
                        {
                            text2 = splitter[1].Replace(" =", " or ");

                            if (splitter.Length > 2)
                            {
                                foreach (char thisChar in splitter[2])
                                {
                                    if ((!Char.IsDigit(thisChar)) && (thisChar != '.') && (thisChar != '-'))
                                        isNumber = false;
                                }

                                if (isNumber)
                                {
                                    text3 = splitter[2].Replace(" =", " or ");

                                    foreach (char thisChar in splitter[3])
                                    {
                                        if ((!Char.IsDigit(thisChar)) && (thisChar != '.') && (thisChar != '-'))
                                            isNumber = false;
                                    }

                                    if (isNumber)
                                    {
                                        text4 = splitter[3].Replace(" =", " or ");
                                    }
                                }
                            }

                        }
                    }
                }
            }

            // Add the google script information
            mapHeight = 500;
            var scriptBuilder = new StringBuilder();

            // Only continue if there actually IS a map key
            if (!String.IsNullOrWhiteSpace(UI_ApplicationCache_Gateway.Settings.System.Google_Map_API_Key))
            {
                scriptBuilder.AppendLine("<script src=\"https://maps.googleapis.com/maps/api/js?key=" + UI_ApplicationCache_Gateway.Settings.System.Google_Map_API_Key + "\" type=\"text/javascript\"></script>");
                scriptBuilder.AppendLine("<script type=\"text/javascript\" src=\"" + Static_Resources_Gateway.Sobekcm_Map_Search_Js + "\"></script>");
                scriptBuilder.AppendLine("<script type=\"text/javascript\" src=\"" + Static_Resources_Gateway.Sobekcm_Map_Tool_Js + "\"></script>");

                scriptBuilder.AppendLine("<script type=\"text/javascript\">");
                scriptBuilder.AppendLine("  //<![CDATA[");
                scriptBuilder.AppendLine("  function load() { ");

                // Set the latitude and longitude
                int zoom = 1;
                decimal latitude = 0;
                decimal longitude = 0;
                if (ViewBag.Hierarchy_Object.Map_Search_Display != null)
                {
                    if ((ViewBag.Hierarchy_Object.Map_Search_Display.ZoomLevel.HasValue) && (ViewBag.Hierarchy_Object.Map_Search_Display.Latitude.HasValue) && (ViewBag.Hierarchy_Object.Map_Search_Display.Longitude.HasValue))
                    {
                        latitude = ViewBag.Hierarchy_Object.Map_Search_Display.Latitude.Value;
                        longitude = ViewBag.Hierarchy_Object.Map_Search_Display.Longitude.Value;
                        zoom = ViewBag.Hierarchy_Object.Map_Search_Display.ZoomLevel.Value;
                    }
                }
                scriptBuilder.AppendLine("    load_search_map(" + latitude + ", " + longitude + ", " + zoom + ", \"map1\");");

                //// If no point searching is allowed, disable it
                //if (ViewBag.Hierarchy_Object.Map_Search >= 100)
                //{
                //    pointSearchingDisabled = true;
                //    scriptBuilder.AppendLine("    disable_point_searching();");
                //}

                if ((text1.Length > 0) && (text2.Length > 0) && (text3.Length > 0) && (text4.Length > 0))
                {
                    scriptBuilder.AppendLine("    add_selection_rectangle( " + text1 + ", " + text2 + ", " + text3 + ", " + text4 + " );");
                    scriptBuilder.AppendLine("    zoom_to_bounds();");
                }
                else if ((text1.Length > 0) && (text2.Length > 0))
                {
                    scriptBuilder.AppendLine("    add_selection_point( " + text1 + ", " + text2 + ", 8 );");
                }

                scriptBuilder.AppendLine("  }");
                scriptBuilder.AppendLine("  //]]>");
                scriptBuilder.AppendLine("</script>");
            }
            else
            {
                // No Google Map API Key
                scriptBuilder.AppendLine("<script type=\"text/javascript\">");
                scriptBuilder.AppendLine("  //<![CDATA[ ");
                scriptBuilder.AppendLine("  function load() {  }");
                scriptBuilder.AppendLine("  //]]>");
                scriptBuilder.AppendLine("</script>");
            }
            Search_Script_Reference = scriptBuilder.ToString();

            // Get the action name for the button
            Search_Script_Action = "map_search_sobekcm('" + redirect_stem + "');";
        }

        /// <summary> Gets the type of collection view or search supported by this collection viewer </summary>
        /// <value> This returns the <see cref="Item_Aggregation_Views_Searches_Enum.Map_Search"/> enumerational value </value>
        public override Item_Aggregation_Views_Searches_Enum Type
        {
            get { return Item_Aggregation_Views_Searches_Enum.Map_Search; }
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

        /// <summary> Add the HTML to be displayed in the search box </summary>
        /// <param name="Output"> Textwriter to write the HTML for this viewer</param>
        /// <param name="Tracer">Trace object keeps a list of each method executed and important milestones in rendering</param>
        /// <remarks> This addes the map search panel which holds the google map, as well as the coordinate entry boxes </remarks>
        public override void Write_Search_Box_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer?.Add_Trace("Map_Search_AggregationViewer.Write_Search_Box_HTML", "Adding html for search box");

            Web_Language_Enum language = RequestSpecificValues.Current_Mode.Language;
            string search_button_text = Localization_Gateway.Advanced_Search.Search(language);
            string find_button_text = Localization_Gateway.Map_Search.Find_Address(language);
            string address_text = Localization_Gateway.Map_Search.Address(language);
            string LOCATE_TEXT = Localization_Gateway.Map_Search.Locate(language);


            bool show_coordinates = false;
            int width = 740;
            if (RequestSpecificValues.Current_Mode.Info_Browse_Mode == "1")
            {
                show_coordinates = true;
                width = 550;
            }

            Output.WriteLine("  <table id=\"sbkMsav_SearchPanel\" >");
            Output.WriteLine("  <tr>");
            Output.WriteLine("    <td colspan=\"2\">");
            if (pointSearchingDisabled)
            {
                Output.WriteLine("          <table>");
                Output.WriteLine("            <tr><td>" + Localization_Gateway.Map_Search.Point_Disabled_Instructions_Html(language) + "</td>");
                Output.WriteLine("                <td><button name=\"searchButton\" id=\"searchButton\" class=\"SobekSearchButton\" onclick=\"" + Search_Script_Action + "\">" + search_button_text + "<img id=\"sbkMsav_ButtonArrow\" src=\"" + Static_Resources_Gateway.Button_Next_Arrow2_Png + "\" alt=\"\" /></button></td></tr>");
                Output.WriteLine("          </table>");
            }
            else
            {
                Output.WriteLine(Localization_Gateway.Map_Search.Instructions_Html(language));
            }

            if (!pointSearchingDisabled)
            {
                Output.WriteLine("        <div id=\"sbkMsav_AddressDiv\">");
                Output.WriteLine("          <label for=\"AddressTextBox\">" + address_text + ":</label> &nbsp; ");
                Output.WriteLine("          <input name=\"AddressTextBox\" type=\"text\" id=\"AddressTextBox\" class=\"sbkMsav_AddressBox sbk_Focusable\" value=\"\" placeholder=\"Enter address ( i.e., 12 Main Street, Gainesville Florida )\" data-placeholder-text=\"Enter address ( i.e., 12 Main Street, Gainesville Florida )\" onleave=\"address_box_changed(this);\" onchange=\"address_box_changed(this);\" onkeydown=\"address_keydown(event, this);\" /> &nbsp; ");
                Output.WriteLine("          <button name=\"findButton\" id=\"findButton\" class=\"sbk_SearchButton\" onclick=\"map_address_geocode();return false;\" >" + find_button_text + "</button> &nbsp; ");
                Output.WriteLine("          <button name=\"searchButton\" id=\"searchButton\" class=\"sbk_SearchButton\" onclick=\"" + Search_Script_Action + ";return false;\" >" + search_button_text + "<img id=\"sbkMsav_ButtonArrow\" src=\"" + Static_Resources_Gateway.Button_Next_Arrow2_Png + "\" alt=\"\" /></button>");
                Output.WriteLine("        </div>");
            }
            Output.WriteLine("    </td>");
            Output.WriteLine("  </tr>");

            Output.WriteLine("  <tr style=\"vertical-align:top\">");
            Output.WriteLine("    <td>");
            if (!show_coordinates)
            {
                Output.WriteLine("      <div id=\"sbkMsav_ShowCoordinateTab\">");
                RequestSpecificValues.Current_Mode.Info_Browse_Mode = "1";
                Output.WriteLine("        <span class=\"sbk_FauxUpwardTab\"><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">SHOW COORDINATES</a></span>");
                RequestSpecificValues.Current_Mode.Info_Browse_Mode = "0";
                Output.WriteLine("      </div>");
            }
            else
            {
                Output.WriteLine("      <div id=\"sbkMsav_HideCoordinateTab\">");
                RequestSpecificValues.Current_Mode.Info_Browse_Mode = "0";
                Output.WriteLine("        <span class=\"sbk_FauxUpwardTab\"><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">HIDE COORDINATES</a></span>");
                RequestSpecificValues.Current_Mode.Info_Browse_Mode = "1";
                Output.WriteLine("      </div>");
            }


            // Show error message if there is no Google Map API key
            if (String.IsNullOrWhiteSpace(UI_ApplicationCache_Gateway.Settings.System.Google_Map_API_Key))
            {
                Output.WriteLine("  <div style=\"width: " + width + "px; height: " + mapHeight + "px;padding: 25px\">");
                Output.WriteLine(Localization_Gateway.Map_Common.Google_Maps_Not_Enabled_Html(language));
                Output.WriteLine("  </div>");
            }
            else
            {
                Output.WriteLine("      <div id=\"map1\" style=\"width: " + width + "px; height: " + mapHeight + "px\"></div>");
            }

            Output.WriteLine("    </td>");
            Output.WriteLine("    <td>");
            Output.WriteLine(show_coordinates ? "      <div id=\"map_coordinates_div\" >" : "      <div id=\"map_coordinates_div\" style=\"display: none;\" >");
            Output.WriteLine("        <table>");
            Output.WriteLine("          <tr><td colspan=\"2\"><br /><br /><br /><b>Search Coordinates</b><br /><br /></td></tr>");
            Output.WriteLine("          <tr><td colspan=\"2\">Point 1</td></tr>");
            Output.WriteLine("          <tr><td><label for=\"Textbox1\">Latitude:</label> </td><td><input name=\"Textbox1\" type=\"text\" id=\"Textbox1\" class=\"sbkMsav_SearchBox sbk_Focusable\" value=\"" + text1 + "\" /></td></tr>");
            Output.WriteLine("          <tr><td><label for=\"Textbox2\">Longitude:</label> </td><td><input name=\"Textbox2\" type=\"text\" id=\"Textbox2\" class=\"sbkMsav_SearchBox sbk_Focusable\" value=\"" + text2 + "\" /><br /><br /></td></tr>");
            Output.WriteLine("          <tr><td colspan=\"2\"><br />Point 2</td></tr>");
            Output.WriteLine("          <tr><td><label for=\"Textbox3\">Latitude:</label> </td><td><input name=\"Textbox3\" type=\"text\" id=\"Textbox3\" class=\"sbkMsav_SearchBox sbk_Focusable\" value=\"" + text3 + "\" /></td></tr>");
            Output.WriteLine("          <tr><td><label for=\"Textbox4\">Longitude:</label> </td><td><input name=\"Textbox4\" type=\"text\" id=\"Textbox4\" class=\"sbkMsav_SearchBox sbk_Focusable\" value=\"" + text4 + "\" ></td></tr>");
            Output.WriteLine("          <tr><td colspan=\"2\" align=\"right\"><br /></td></tr>");
            Output.WriteLine("          <tr><td colspan=\"2\" align=\"right\"><button name=\"locateButton\" id=\"locateButton\" class=\"sbk_SearchButton\" onclick=\"locate_by_coordinates();\">" + LOCATE_TEXT + "</button></td></tr>");
            Output.WriteLine("         </table>");
            Output.WriteLine("       </div>");
            Output.WriteLine("      </td>");
            Output.WriteLine("    </tr>");
            Output.WriteLine("  </table>");
            Output.WriteLine();

        }

        /// <summary> Add the HTML to be displayed below the search box </summary>
        /// <param name="Output"> Textwriter to write the HTML for this viewer</param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
        /// <remarks> This adds the search tips by calling the base method <see cref="abstractAggregationViewer.Add_Simple_Search_Tips"/> </remarks>
        public override void Write_Main_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer?.Add_Trace("Map_Search_AggregationViewer.Write_Main_HTML", "Adds map search-specific search tips");

            // Write the quick tips
            Output.WriteLine("<a name=\"FAQ\" ></a>");
            Output.WriteLine("<div id=\"sbk_QuickTips\">");

            // See if the FAQ is present for this collection
            string language_code = RequestSpecificValues.Current_Mode.Language_Code;
            if (language_code.Length > 0)
                language_code = "_" + language_code;
            string directory = UI_ApplicationCache_Gateway.Settings.Servers.Base_Design_Location + "\\aggregations\\" + RequestSpecificValues.Current_Mode.Aggregation + "\\extra";
            string aggregation_specific_faq = String.Empty;
            if (Directory.Exists(directory))
            {
                if (File.Exists(directory + "\\map_faq" + language_code + ".txt"))
                {
                    Tracer?.Add_Trace("Map_Search_AggregationViewer.Write_Main_HTML", "Reading aggregation specific map search faq");

                    try
                    {
                        var faq_reader = new StreamReader(directory + "\\map_faq" + language_code + ".txt");
                        aggregation_specific_faq = faq_reader.ReadToEnd();
                        faq_reader.Close();
                    }
                    catch (Exception)
                    {
                        // No error thrown in this case; the default value will be used
                    }
                }
            }

            // If no aggregation level FAQ was found, look for a collection wide
            if (aggregation_specific_faq.Length == 0)
            {
                directory = UI_ApplicationCache_Gateway.Settings.Servers.Base_Design_Location + "\\extra\\searchtips";
                if (Directory.Exists(directory))
                {
                    if (File.Exists(directory + "\\map_faq" + language_code + ".txt"))
                    {
                        Tracer?.Add_Trace("Map_Search_AggregationViewer.Write_Main_HTML", "Reading application-wide map search faq");

                        try
                        {
                            var faq_reader = new StreamReader(directory + "\\map_faq" + language_code + ".txt");
                            aggregation_specific_faq = faq_reader.ReadToEnd();
                            faq_reader.Close();
                        }
                        catch (Exception)
                        {
                            // No error thrown in this case; the default value will be used
                        }
                    }
                }
            }

            // Now, render the faq
            if (aggregation_specific_faq.Length > 0)
            {
                Output.WriteLine(aggregation_specific_faq);
            }
            else
            {
                Output.WriteLine(!pointSearchingDisabled
                    ? Localization_Gateway.Map_Search.Faq_Point_Enabled_Html(RequestSpecificValues.Current_Mode.Language)
                    : Localization_Gateway.Map_Search.Faq_Point_Disabled_Html(RequestSpecificValues.Current_Mode.Language));
            }

            Output.WriteLine("</div>");
            Output.WriteLine("<br />");
            Output.WriteLine();
        }
    }
}
