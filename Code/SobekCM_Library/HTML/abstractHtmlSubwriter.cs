#region Using directives

using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Math;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using SobekCM.Core.Aggregations;
using SobekCM.Core.Client;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.Navigation;
using SobekCM.Core.Results;
using SobekCM.Core.Skins;
using SobekCM.Core.Users;
using SobekCM.Core.WebContent;
using SobekCM.Engine_Library.Aggregations;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Library.HTML.Helpers;
using SobekCM.Library.MainWriters;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System;
using System.Collections.Generic;
using System.IO;

#endregion

namespace SobekCM.Library.HTML
{
    /// <summary> Abstract class which all HTML subwriters must extend.  This class contains some of the
    /// basic HTML-writing helper values and contains some of the values used by many of the subclasses.
    /// HTML subwriters are the top level writing classes employed by the <see cref="Html_MainWriter"/>. </summary>
    public abstract class abstractHtmlSubwriter
    {
        /// <summary> Protected field contains the information specific to the current request </summary>
        protected RequestCache RequestSpecificValues;

        /// <summary> HTTP context for the current request </summary>
        protected HttpContext Context => RequestSpecificValues?.Context;

        /// <summary> Empty list of behaviors, returned by default </summary>
        /// <remarks> This just prevents an empty set from having to be created over and over </remarks>
        protected static List<HtmlSubwriter_Behaviors_Enum> emptybehaviors = new List<HtmlSubwriter_Behaviors_Enum>();

        /// <summary> Base constructor </summary>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
	    protected abstractHtmlSubwriter(RequestCache RequestSpecificValues)
        {
            this.RequestSpecificValues = RequestSpecificValues;
        }

        /// <summary> Returns a flag indicating whether the file upload specific holder in the itemNavForm form will be utilized
        /// for the current request, or if it can be hidden. </summary>
        /// <value> This value can be override by child classes, but by default this returns FALSE </value>
        public virtual bool Upload_File_Possible
        {
            get { return false; }
        }

        /// <summary> Write any additional values within the HTML Head of the
        /// final served page </summary>
        /// <param name="Output"> Output stream currently within the HTML head tags </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <remarks> By default this does nothing, but can be overwritten by all the individual html subwriters </remarks>
        public virtual void Write_Within_HTML_Head(TextWriter Output, Custom_Tracer Tracer)
        {
            // Do nothing by default
        }

        /// <summary> Chance for a final, final CSS which can override anything else, including the web skin </summary>
	    public virtual string Final_CSS
        {
            get { return String.Empty; }
        }

        /// <summary> Add the header to the output </summary>
        /// <param name="Output"> Stream to which to write the HTML for this header </param>
	    public virtual void Add_Header(TextWriter Output)
        {
            HeaderFooter_HtmlHelper.Add_Header(Output, RequestSpecificValues, Container_CssClass, WebPage_Title, Subwriter_Behaviors, null, null, Context);
        }

        /// <summary> Flag indicates if the internal header should included </summary>
        /// <remarks> By default this return TRUE if the user is internal, or a portal/system admin, but can be 
        /// overwritten by all the individual html subwriters </remarks>
	    public virtual bool Include_Internal_Header
        {
            get
            {
                // If no user, do not show
                if ((RequestSpecificValues.Current_User == null) || (!RequestSpecificValues.Current_User.LoggedOn))
                    return false;

                return ((RequestSpecificValues.Current_User.Is_Internal_User) || (RequestSpecificValues.Current_User.Is_System_Admin) || (RequestSpecificValues.Current_User.Is_Portal_Admin));
            }
        }

        /// <summary> Adds the internal header HTML for this specific HTML writer </summary>
        /// <param name="Output"> Stream to which to write the HTML for the internal header information </param>
        /// <param name="Current_User"> Currently logged on user, to determine specific rights </param>
        public virtual void Write_Internal_Header_HTML(TextWriter Output, User_Object Current_User)
        {
            Output.WriteLine("  <table id=\"sbk_InternalHeader\">");
            Output.WriteLine("    <tr>");
            Output.WriteLine("      <td style=\"text-align:left;\">");
            Output.WriteLine("          <button title=\"Hide Internal Header\" class=\"intheader_button_aggr hide_intheader_button_aggr\" onclick=\"return hide_internal_header();\"></button>");
            Output.WriteLine("      </td>");
            Write_Internal_Header_Search_Box(Output);
            Output.WriteLine("    </tr>");
            Output.WriteLine("  </table>");
        }

        /// <summary> Adds the internal header search box to the current output stream  </summary>
        /// <param name="Output"> Output stream to write the html for the internal header search box to </param>
        protected void Write_Internal_Header_Search_Box(TextWriter Output)
        {
            Output.WriteLine("      <td style=\"text-align:right; vertical-align:middle; width:340px;\">");
            Output.WriteLine("        <table>");
            Output.WriteLine("          <tr style=\"vertical-align:top; height: 16px;\">");
            Output.WriteLine("            <td valign=\"top\">");
            Output.Write("              <input name=\"internalSearchTextBox\" type=\"text\" id=\"internalSearchTextBox\" class=\"SobekInternalSearchBox\" value=\"\" onfocus=\"javascript:textbox_enter('internalSearchTextBox', 'SobekInternalSearchBox_focused')\" onblur=\"javascript:textbox_leave('internalSearchTextBox', 'SobekInternalSearchBox')\"");
            if ((!String.IsNullOrEmpty(RequestSpecificValues.Current_Mode.Browser_Type)) && (RequestSpecificValues.Current_Mode.Browser_Type.IndexOf("IE") >= 0))
                Output.WriteLine(" onkeydown=\"internalTrapKD(event, '" + RequestSpecificValues.Current_Mode.Base_URL + "contains');\" />");
            else
                Output.WriteLine(" onkeydown=\"return internalTrapKD(event, '" + RequestSpecificValues.Current_Mode.Base_URL + "contains');\" />");
            Output.WriteLine("              <select name=\"internalDropDownList\" id=\"internalDropDownList\" class=\"SobekInternalSelectBox\" >");
            Output.WriteLine("                <option value=\"BI\" selected=\"selected\">BibID</option>");
            Output.WriteLine("                <option value=\"OC\">OCLC Number</option>");
            Output.WriteLine("                <option value=\"AL\">ALEPH Number</option>");
            Output.WriteLine("                <option value=\"ZZ\">Anywhere</option>");
            Output.WriteLine("                <option value=\"TI\">Title</option>");
            Output.WriteLine("                <option value=\"AU\">Author</option>");
            Output.WriteLine("                <option value=\"SU\">Subject Keywords</option>");
            Output.WriteLine("                <option value=\"CO\">Country</option>");
            Output.WriteLine("                <option value=\"ST\">State</option>");
            Output.WriteLine("                <option value=\"CT\">County</option>");
            Output.WriteLine("                <option value=\"CI\">City</option>");
            Output.WriteLine("                <option value=\"PP\">Place of Publication</option>");
            Output.WriteLine("                <option value=\"SP\">Spatial Coverage</option>");
            Output.WriteLine("                <option value=\"TY\">Type</option>");
            Output.WriteLine("                <option value=\"LA\">Language</option>");
            Output.WriteLine("                <option value=\"PU\">Publisher</option>");
            Output.WriteLine("                <option value=\"GE\">Genre</option>");
            Output.WriteLine("                <option value=\"TA\">Target Audience</option>");
            Output.WriteLine("                <option value=\"DO\">Donor</option>");
            Output.WriteLine("                <option value=\"AT\">Attribution</option>");
            Output.WriteLine("                <option value=\"TL\">Tickler</option>");
            Output.WriteLine("                <option value=\"NO\">Notes</option>");
            Output.WriteLine("                <option value=\"ID\">Identifier</option>");
            Output.WriteLine("                <option value=\"FR\">Frequency</option>");
            Output.WriteLine("                <option value=\"TB\">Tracking Box</option>");
            Output.WriteLine("              </select>");
            Output.WriteLine("            </td>");
            Output.WriteLine("            <td>");
            Output.WriteLine("              <a onclick=\"internal_search('" + RequestSpecificValues.Current_Mode.Base_URL + "contains')\"><img src=\"" + Static_Resources_Gateway.Go_Gray_Gif + "\" title=\"Perform search\" alt=\"Perform search\" style=\"margin-top: 1px\" /></a>");
            Output.WriteLine("              &nbsp;");
            Output.WriteLine("            </td>");
            Output.WriteLine("          </tr>");
            Output.WriteLine("        </table>");
            Output.WriteLine("      </td> ");
        }

        /// <summary> Writes the HTML generated by this abstract html subwriter directly to the response stream </summary>
        /// <param name="Output"> Stream to which to write the HTML for this subwriter </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> Value indicating if html writer should finish the page immediately after this, or if there are other controls or routines which need to be called first </returns>
        public abstract bool Write_HTML(TextWriter Output, Custom_Tracer Tracer);

        /// <summary> Writes the complete content of the itemNavForm: the opening HTML, the main viewer section, then the
        /// closing HTML. Collapses what used to be three separate calls (<see cref="Write_ItemNavForm_Opening"/>,
        /// <see cref="Add_Main_Viewer_Section"/>, <see cref="Write_ItemNavForm_Closing"/>) from the caller's perspective;
        /// subwriters needing custom per-step behavior can keep overriding those three individually, since the default
        /// implementation here just calls them in order. </summary>
        /// <param name="Output">Stream to directly write to</param>
        /// <param name="Tracer">Trace object keeps a list of each method executed and important milestones in rendering</param>
        public virtual void Add_ItemNavForm_Content(TextWriter Output, Custom_Tracer Tracer)
        {
            // Do nothing by default
        }

        /// <summary> Writes final HTML after all the forms </summary>
        /// <param name="Output">Stream to directly write to</param>
        /// <param name="Tracer">Trace object keeps a list of each method executed and important milestones in rendering</param>
        public virtual void Write_Final_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            // Do nothing by default
        }

        protected void Write_ItemNavForm_Opening(TextWriter Output)
        {
            string formAction = Context.Items[RequestCache_Keys.OriginalUrl]?.ToString() ?? Context.Request.GetDisplayUrl();
            string enctype = Upload_File_Possible ? " enctype=\"multipart/form-data\"" : "";
            Output.Write($"<form id=\"itemNavForm\" name=\"itemNavForm\" action=\"{formAction}\" method=\"post\"{enctype}>");
        }

        protected void Write_ItemNavForm_Closing(TextWriter Output)
        {
            Output.Write("</form>");
        }


        /// <summary> Add the footer to the output </summary>
        /// <param name="Output"> Stream to which to write the HTML for this footer </param>
        public virtual void Add_Footer(TextWriter Output)
        {
            HeaderFooter_HtmlHelper.Add_Footer(Output, RequestSpecificValues, Subwriter_Behaviors, null, null, Context);
        }

        /// <summary> Gets the collection of special behaviors which this subwriter
        /// requests from the main HTML writer. </summary>
        /// <remarks> By default, this returns an empty list </remarks>
        public virtual List<HtmlSubwriter_Behaviors_Enum> Subwriter_Behaviors
        {
            get { return emptybehaviors; }
        }

        /// <summary> Gets the collection of body attributes to be included 
        /// within the HTML body tag (usually to add events to the body) </summary>
        public virtual List<Tuple<string, string>> Body_Attributes
        {
            get { return null; }
        }

        /// <summary> Title for this web page </summary>
        /// <value> This value is set by each of the sub classes </value>
        public virtual string WebPage_Title
        {
            get { return "{0}"; }
        }

        /// <summary> Gets the CSS class of the container that the page is wrapped within </summary>
        /// <value> By default, returns 'container-inner' </value>
        public virtual string Container_CssClass
        {
            get { return "container-inner"; }
        }

        #region Helper methods for getting collections and itemss

        /// <summary> Gets the item aggregation and search fields for the current item aggregation </summary>
        /// <param name="Current_Mode"> Mode / navigation information for the current request</param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
        /// <param name="Aggregation_Object"> [OUT] Fully-built object for the current aggregation object </param>
        /// <returns> TRUE if successful, otherwise FALSE </returns>
        /// <remarks> This attempts to pull the objects from the cache.  If unsuccessful, it builds the objects from the
        /// database and hands off to the <see cref="CachedDataManager" /> to store in the cache. </remarks>
        protected static bool Get_Collection(Navigation_Object Current_Mode, Custom_Tracer Tracer, out Item_Aggregation Aggregation_Object)
        {
            string languageCode = Current_Mode.Language;

            Tracer?.Add_Trace("abstractHtmlSubwriter.Get_Collection", "Get aggregation (" + Current_Mode.Aggregation + ") or (" + Current_Mode.Default_Aggregation + ") for language (" + languageCode + ")");

            // If there is an aggregation listed, try to get that now
            if ((Current_Mode.Aggregation.Length > 0) && (Current_Mode.Aggregation != "all"))
            {
                // Try to pull the aggregation information
                Aggregation_Object = CachedDataManager.Aggregations.Retrieve_Item_Aggregation(Current_Mode.Aggregation, languageCode, Tracer);
                if (Aggregation_Object != null)
                {
                    set_web_skin_from_aggregation(Current_Mode, Aggregation_Object);
                    return true;
                }

                // Get the item aggregation from the Sobek Engine Client (which checks the local cache as well)
                Aggregation_Object = SobekEngineClient.Aggregations.Get_Aggregation(Current_Mode.Aggregation, languageCode, (UI_ApplicationCache_Gateway.Configuration.Languages.Default_Language?.Code ?? "en"), Tracer);

                // Return if this was valid
                if (Aggregation_Object != null)
                {
                    set_web_skin_from_aggregation(Current_Mode, Aggregation_Object);
                    return true;
                }

                Current_Mode.Error_Message = "Invalid item aggregation '" + Current_Mode.Aggregation + "' referenced.";
                return false;
            }

            return Get_Top_Level_Collection(Current_Mode, Tracer, out Aggregation_Object);
        }

        private static void set_web_skin_from_aggregation(Navigation_Object Current_Mode, Item_Aggregation Aggregation_Object)
        {
            // If th aggregation can only display under certain web skins, see if the current is acceptable
            // Do NOT do this replacement if the web skin is in the URL and this is admin mode
            if ((!Current_Mode.Skin_In_URL) || (Current_Mode.Mode != Display_Mode_Enum.Administrative))
            {
                // Is there a list of acceptible web skins?
                if ((Aggregation_Object.Web_Skins != null) && (Aggregation_Object.Web_Skins.Count > 0))
                {
                    string currentWebSkin = Current_Mode.Skin;
                    bool acceptableSkin = false;
                    foreach (string aggrWebSkin in Aggregation_Object.Web_Skins)
                    {
                        if (String.Compare(currentWebSkin, aggrWebSkin, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            acceptableSkin = true;
                            break;
                        }
                    }

                    // If no match found, assign the skin
                    if (!acceptableSkin)
                    {
                        // Use default skin if there
                        if (!String.IsNullOrWhiteSpace(Aggregation_Object.Default_Skin))
                        {
                            Current_Mode.Skin = Aggregation_Object.Default_Skin.ToLower();
                        }
                        else
                        {
                            Current_Mode.Skin = Aggregation_Object.Web_Skins[0];
                        }

                        // Let the nav object know this was a default
                        Current_Mode.Default_Skin = Current_Mode.Skin;
                    }
                }
            }
        }

        /// <summary> Gets the item aggregation and search fields for the current item aggregation </summary>
        /// <param name="Current_Mode"> Mode / navigation information for the current request</param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
        /// <param name="Aggregation_Object"> [OUT] Fully-built object for the current aggregation object </param>
        /// <returns> TRUE if successful, otherwise FALSE </returns>
        /// <remarks> This attempts to pull the objects from the cache.  If unsuccessful, it builds the objects from the
        /// database and hands off to the <see cref="CachedDataManager" /> to store in the cache. </remarks>
        protected static bool Get_Top_Level_Collection(Navigation_Object Current_Mode, Custom_Tracer Tracer, out Item_Aggregation Aggregation_Object)
        {
            Tracer?.Add_Trace("abstractHtmlSubwriter.Get_Top_Level_Collection", String.Empty);

            string languageCode = Current_Mode.Language;

            // Get the ALL collection group
            try
            {
                // Try to pull this from the cache
                Aggregation_Object = CachedDataManager.Aggregations.Retrieve_Item_Aggregation("all", languageCode, Tracer);
                if (Aggregation_Object != null)
                    return true;

                // Get the item aggregation from the Sobek Engine Client
                Aggregation_Object = SobekEngineClient.Aggregations.Get_Aggregation("all", languageCode, (UI_ApplicationCache_Gateway.Configuration.Languages.Default_Language?.Code ?? "en"), Tracer);
            }
            catch (Exception ee)
            {
                Aggregation_Object = null;
                Current_Mode.Error_Message = "Error pulling the item aggregation corresponding to all collection groups : " + ee.Message;
                return false;
            }

            // If this is null, just stop
            if (Aggregation_Object == null)
            {
                Current_Mode.Error_Message = "Unable to pull the item aggregation corresponding to all collection groups";
                return false;
            }

            return true;
        }

        /// <summary> Gets the browse or info object and any other needed data for display ( text to display) </summary>
        /// <param name="Current_Mode"> Mode / navigation information for the current request</param>
        /// <param name="Aggregation_Object"> Item Aggregation object</param>
        /// <param name="Base_Directory"> Base directory location under which the the CMS/info source file will be found</param>
        /// <param name="Current_User"> Currently logged on user, which can determine which items to show </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <param name="Browse_Object"> [OUT] Stores all the information about this browse or info </param>
        /// <param name="Complete_Result_Set_Info"> [OUT] Information about the entire set of results </param>
        /// <param name="Paged_Results"> [OUT] List of search results for the requested page of results </param>
        /// <param name="Browse_Info_Display_Text"> [OUT] Static HTML-based content to be displayed if this is browing a staticly created html source file </param>
        /// <returns> TRUE if successful, otherwise FALSE </returns>
        /// <remarks> This attempts to pull the objects from the cache.  If unsuccessful, it builds the objects from the
        /// database and hands off to the <see cref="CachedDataManager" /> to store in the cache </remarks>
        protected static bool Get_Browse_Info(Navigation_Object Current_Mode,
                                    Item_Aggregation Aggregation_Object,
                                    string Base_Directory,
                                    User_Object Current_User,
                                    Custom_Tracer Tracer,
                                    out Item_Aggregation_Child_Page Browse_Object,
                                    out Search_Results_Statistics Complete_Result_Set_Info,
                                    out List<iSearch_Title_Result> Paged_Results,
                                    out HTML_Based_Content Browse_Info_Display_Text)
        {
            Tracer?.Add_Trace("abstractHtmlSubwriter.Get_Browse_Info", String.Empty);

            // Set output initially to null
            Paged_Results = null;
            Complete_Result_Set_Info = null;
            Browse_Info_Display_Text = null;

            // First, make sure the browse submode is valid
            Browse_Object = Aggregation_Object.Child_Page_By_Code(Current_Mode.Info_Browse_Mode);

            if (Browse_Object == null)
            {
                Current_Mode.Error_Message = "Unable to retrieve browse/info item '" + Current_Mode.Info_Browse_Mode + "'";
                return false;
            }

            // Is this a table result, or a string?
            switch (Browse_Object.Source_Data_Type)
            {
                case Item_Aggregation_Child_Source_Data_Enum.Database_Table:

                    // Set the current sort to ZERO, if currently set to ONE and this is an ALL BROWSE.
                    // Those two sorts are the same in this case
                    int sort = Current_Mode.Sort.HasValue ? Math.Max(Current_Mode.Sort.Value, ((ushort)1)) : 1;
                    if ((sort == 0) && (Browse_Object.Code == "all"))
                        sort = 1;

                    // Special code if this is a JSON browse
                    string browse_code = Current_Mode.Info_Browse_Mode;
                    if (Current_Mode.Writer_Type == Writer_Type_Enum.JSON)
                    {
                        browse_code = browse_code + "_JSON";
                        sort = 12;
                    }

                    // Get the page count in the results
                    int current_page_index = Current_Mode.Page.HasValue ? Math.Max(Current_Mode.Page.Value, ((ushort)1)) : 1;

                    // Determine if this is a special search type which returns more rows and is not cached.
                    // This is used to return the results as XML and DATASET
                    bool special_search_type = false;
                    int results_per_page = 20;
                    if ((Current_Mode.Writer_Type == Writer_Type_Enum.XML) || (Current_Mode.Writer_Type == Writer_Type_Enum.DataSet))
                    {
                        results_per_page = 1000000;
                        special_search_type = true;
                        sort = 2; // Sort by BibID always for these
                    }

                    Tracer.Add_Trace("abstractHtmlSubwriter.Get_Browse_Info", "Current_Mode.Writer_Type=[" + Current_Mode.Writer_Type.ToString() + "].");
                    Tracer.Add_Trace("abstractHtmlSubwriter.Get_Browse_Info", "Current_Mode.Results_Display_Type=[" + Current_Mode.Result_Display_Type + "].");

                    if (String.Equals(Current_Mode.Result_Display_Type, "timeline", StringComparison.OrdinalIgnoreCase))
                    {
                        Tracer.Add_Trace("abstractHtmlSubwriter.Get_Browse_Info", "Is timeline, setting browse results_per_page and sort.");

                        results_per_page = 20000;
                        sort = 12;
                    }

                    // Set the flags for how much data is needed.  (i.e., do we need to pull ANYTHING?  or
                    // perhaps just the next page of results ( as opposed to pulling facets again).
                    bool need_browse_statistics = true;
                    bool need_paged_results = true;
                    if ((!special_search_type) && (Current_User == null))
                    {
                        // Look to see if the browse statistics are available on any cache for this browse
                        Complete_Result_Set_Info = CachedDataManager.Retrieve_Browse_Result_Statistics(Aggregation_Object.Code, browse_code, Tracer);
                        if (Complete_Result_Set_Info != null)
                            need_browse_statistics = false;

                        // Look to see if the paged results are available on any cache..
                        Paged_Results = CachedDataManager.Retrieve_Browse_Results(Aggregation_Object.Code, browse_code, current_page_index, sort, (uint)results_per_page, Tracer);
                        if (Paged_Results != null)
                            need_paged_results = false;
                    }

                    // Was a copy found in the cache?
                    if ((!need_browse_statistics) && (!need_paged_results))
                    {
                        Tracer?.Add_Trace("SobekCM_Assistant.Get_Browse_Info", "Browse statistics and paged results retrieved from cache");
                    }
                    else
                    {
                        Tracer?.Add_Trace("SobekCM_Assistant.Get_Browse_Info", "Building results information");

                        // Try to pull more than one page, so we can cache the next page or so
                        List<List<iSearch_Title_Result>> pagesOfResults;

                        // Get from the hierarchy object
                        Multiple_Paged_Results_Args returnArgs = Item_Aggregation_Utilities.Get_Browse_Results(Aggregation_Object, Browse_Object, current_page_index, sort, results_per_page, !special_search_type, need_browse_statistics, Current_User, Tracer);
                        if (need_browse_statistics)
                        {
                            Complete_Result_Set_Info = returnArgs.Statistics;
                        }
                        pagesOfResults = returnArgs.Paged_Results;
                        if ((pagesOfResults != null) && (pagesOfResults.Count > 0))
                            Paged_Results = pagesOfResults[0];

                        // Save the overall result set statistics to the cache if something was pulled
                        if ((!special_search_type) && (Current_User == null))
                        {
                            if ((need_browse_statistics) && (Complete_Result_Set_Info != null))
                            {
                                CachedDataManager.Store_Browse_Result_Statistics(Aggregation_Object.Code, browse_code, Complete_Result_Set_Info, Tracer);
                            }

                            // Save the overall result set statistics to the cache if something was pulled
                            if ((need_paged_results) && (Paged_Results != null))
                            {
                                CachedDataManager.Store_Browse_Results(Aggregation_Object.Code, browse_code, current_page_index, sort, (uint)results_per_page, pagesOfResults, Tracer);
                            }
                        }
                    }
                    break;

                case Item_Aggregation_Child_Source_Data_Enum.Static_HTML:
                    Browse_Info_Display_Text = SobekEngineClient.Aggregations.Get_Aggregation_HTML_Child_Page(Aggregation_Object.Code, Aggregation_Object.Language, (UI_ApplicationCache_Gateway.Configuration.Languages.Default_Language?.Code ?? "en"), Browse_Object.Code, Tracer);
                    break;
            }
            return true;
        }

        #endregion
    }
}
