#region Using directives

using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using SobekCM.Core.Aggregations;
using SobekCM.Core.Configuration.Localization;
using SobekCM.Core.Navigation;
using SobekCM.Core.UI_Configuration;
using SobekCM.Core.UI_Configuration.StaticResources;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Library.HTML;
using SobekCM.Library.MainWriters;
using SobekCM.Tools;

#endregion

namespace SobekCM.Library.AggregationViewer.Viewers
{
    public class Banner_Search_AggregationViewer : abstractAggregationViewer
    {
        private readonly string arg1;
        private readonly string arg2;
        private readonly string browse_url;
        private readonly string textBoxValue;
        private readonly string frontBannerInfo;

        /// <summary> Constructor for a new instance of the Banner_Search_AggregationViewer class </summary>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        /// <param name="ViewBag"> Aggregation-specific request information, such as aggregation object and any browse object requested </param>
        public Banner_Search_AggregationViewer(RequestCache RequestSpecificValues, AggregationViewBag ViewBag)
            : base(RequestSpecificValues, ViewBag)
        {
            // Determine the sub text to use
            const string SUB_CODE = "s=";
            Sharing_Buttons_HTML = String.Empty;

            // Save the search term
            if (RequestSpecificValues.Current_Mode.Search_String.Length > 0)
            {
                textBoxValue = RequestSpecificValues.Current_Mode.Search_String;
            }

            // Determine the complete script action name
            Display_Mode_Enum displayMode = RequestSpecificValues.Current_Mode.Mode;
            Aggregation_Type_Enum aggrType = RequestSpecificValues.Current_Mode.Aggregation_Type;
            Search_Type_Enum searchType = RequestSpecificValues.Current_Mode.Search_Type;
            RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Results;
            RequestSpecificValues.Current_Mode.Search_Type = Search_Type_Enum.Basic;
            RequestSpecificValues.Current_Mode.Search_Precision = Search_Precision_Type_Enum.Inflectional_Form;
            string search_string = RequestSpecificValues.Current_Mode.Search_String;
            RequestSpecificValues.Current_Mode.Search_String = String.Empty;
            RequestSpecificValues.Current_Mode.Search_Fields = String.Empty;
            arg2 = String.Empty;
            arg1 = UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode);

            RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Aggregation;
            RequestSpecificValues.Current_Mode.Aggregation_Type = Aggregation_Type_Enum.Browse_Info;
            RequestSpecificValues.Current_Mode.Info_Browse_Mode = "all";
            browse_url = UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode);

            RequestSpecificValues.Current_Mode.Info_Browse_Mode = String.Empty;
            RequestSpecificValues.Current_Mode.Aggregation_Type = Aggregation_Type_Enum.Home;
            if ((!RequestSpecificValues.Current_Mode.Show_Selection_Panel.HasValue) || (!RequestSpecificValues.Current_Mode.Show_Selection_Panel.Value) || (ViewBag.Hierarchy_Object.Children_Count == 0))
            {
                Search_Script_Action = "basic_search_sobekcm('" + arg1 + "', '" + browse_url + "')";
            }
            else
            {
                Search_Script_Action = "basic_select_search_sobekcm('" + arg1 + "', '" + SUB_CODE + "')";
                arg2 = SUB_CODE;
            }
            RequestSpecificValues.Current_Mode.Mode = displayMode;
            RequestSpecificValues.Current_Mode.Aggregation_Type = aggrType;
            RequestSpecificValues.Current_Mode.Search_Type = searchType;
            RequestSpecificValues.Current_Mode.Search_String = search_string;

            // Get the front banner
            frontBannerInfo = ViewBag.Hierarchy_Object.BannerImage.Replace("\\", "/");
        }

        /// <summary> Sets the sharing buttons HTML to display over the banner </summary>
        public string Sharing_Buttons_HTML { private get; set; }

        /// <summary> Gets the type of collection view or search supported by this collection viewer </summary>
        /// <value> This returns the <see cref="Item_Aggregation_Views_Searches_Enum.Banner_Search"/> enumerational value </value>
        public override Item_Aggregation_Views_Searches_Enum Type
        {
            get { return Item_Aggregation_Views_Searches_Enum.Banner_Search; }
        }

        /// <summary>Flag indicates whether the subaggregation selection panel is displayed for this collection viewer</summary>
        /// <value> This property always returns the <see cref="Selection_Panel_Display_Enum.Selectable"/> enumerational value </value>
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
                            HtmlSubwriter_Behaviors_Enum.Suppress_Banner
                        };
            }
        }

        /// <summary> Add the HTML to be displayed in the search box </summary>
        /// <param name="Output"> Textwriter to write the HTML for this viewer</param>
        /// <param name="Tracer">Trace object keeps a list of each method executed and important milestones in rendering</param>
        public override void Add_Search_Box_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("Banner_Search_AggregationViewer.Add_Search_Box_HTML", "Adding html for search box");
            }

            string search_collection = "Search Collection";
            //string include_privates = "Include non-public items";
            if (RequestSpecificValues.Current_Mode.Language == Web_Language_Enum.Spanish)
            {
                search_collection = "Buscar en la colección";
            }

            if (RequestSpecificValues.Current_Mode.Language == Web_Language_Enum.French)
            {
                search_collection = "Recherche dans la collection";
            }

            string banner_image = RequestSpecificValues.Current_Mode.Base_URL + frontBannerInfo;
            string collection = RequestSpecificValues.Current_Mode.Aggregation;

            if (string.IsNullOrEmpty(collection))
                collection = "all";

            if (collection.ToLower() == "all")
                search_collection = "Search All Collections";

            Output.WriteLine("<div style=\"text-align:left;\">");
            Output.WriteLine("<div id=\"sbkBhs_OuterDiv_" + collection + "\" class=\"sbkBhs_OuterDiv\" style=\"background-image: url( " + banner_image + ");\">");

            Output.WriteLine("<div id=\"sbkBhs_ButtonArea_" + collection + "\" class=\"sbkBhs_ButtonArea\">");
            Output.WriteLine(Sharing_Buttons_HTML);  //.Replace("span", "div")
            Output.WriteLine("</div>");

            Output.WriteLine("  <div id=\"sbkBhs_SearchArea_" + collection + "\" class=\"sbkBhs_SearchArea\">");
            Output.WriteLine("    <label for=\"SobekHomeBannerSearchBox\" id=\"sbkBhs_SearchPrompt_" + collection + "\" class=\"sbkRhav_SearchPrompt\">" + search_collection + ":</label>");

            Output.WriteLine("    <input name=\"u_search\" type=\"text\" id=\"SobekHomeBannerSearchBox\" class=\"sbkRhav_SearchBox sbk_Focusable\" value=\"" + textBoxValue + "\" onkeydown=\"return fnTrapKD(event, 'basic', '" + arg1 + "', '" + arg2 + "','" + browse_url + "');\" />");
            Output.WriteLine("    <button class=\"sbk_GoButton\" title=\"" + search_collection + "\" onclick=\"" + Search_Script_Action + ";return false;\">Go</button>");
            Output.WriteLine("  </div>");

            Output.WriteLine("</div>");
            Output.WriteLine("</div>");

            //if ((currentUser != null) && (currentUser.Is_System_Admin))
            //{
            //    Output.WriteLine("    <tr align=\"right\">");
            //    Output.WriteLine("      <td>&nbsp;</td>");
            //    Output.WriteLine("      <td align=\"left\" colspan=\"4\">");
            //    Output.WriteLine("          &nbsp; &nbsp; &nbsp; &nbsp; <input type=\"checkbox\" value=\"PRIVATE_ITEMS\" name=\"privatecheck\" id=\"privatecheck\" unchecked onclick=\"focus_element( 'SobekHomeSearchBox');\" /><label for=\"privatecheck\">" + include_privates + "</label>");
            //    Output.WriteLine("      </td>");
            //    Output.WriteLine("    </tr>");
            //}

            Output.WriteLine();
            Output.WriteLine("<!-- Focus on search box -->");
            Output.WriteLine("<script type=\"text/javascript\">focus_element('SobekHomeBannerSearchBox');</script>");
            Output.WriteLine();
        }
    }
}