﻿#region Using directives

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;
using SobekCM.Core.Aggregations;
using SobekCM.Core.BriefItem;
using SobekCM.Core.Configuration.Localization;
using SobekCM.Core.Navigation;
using SobekCM.Core.Users;
using SobekCM.Library.UI;

#endregion

namespace SobekCM.Library.HTML
{
    /// <summary> Class is a helper class used for writing the header and footers for HTML responses </summary>
    public static class HeaderFooter_Helper_HtmlSubWriter
    {
        /// <summary> Add the header to the output </summary>
        /// <param name="Output"> Stream to which to write the HTML for this header </param>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        /// <param name="Container_CssClass"> Class name for the container around the page </param>
        /// <param name="Web_Page_Title"> Title for this web page, to include behind the banner possibly </param>
        /// <param name="Behaviors"> List of behaviors from the html subwriters </param>
        /// <param name="Current_Aggregation"> Current aggregation object, if there is one </param>
        /// <param name="Current_Item"> Current item object, if there is one </param>
        public static void Add_Header(TextWriter Output, RequestCache RequestSpecificValues, string Container_CssClass, string Web_Page_Title, List<HtmlSubwriter_Behaviors_Enum> Behaviors, Item_Aggregation Current_Aggregation, BriefItemInfo Current_Item )
        {
            // Get the url options
            string url_options = UrlWriterHelper.URL_Options(RequestSpecificValues.Current_Mode);
            string modified_url_options = String.Empty;
            if (url_options.Length > 0)
                modified_url_options = "?" + url_options;

            // Get the current contact URL
            Display_Mode_Enum thisMode = RequestSpecificValues.Current_Mode.Mode;
            RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Contact;
            string contact = UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode);
            RequestSpecificValues.Tracer.Add_Trace("HeaderFooter_Helper_HtmlSubWriter.Add_Header", "Curent contact URL=[" + contact + "].");

            // Restore the old mode
            RequestSpecificValues.Current_Mode.Mode = thisMode;

            // Determine which header and footer to display
            bool useItemHeader = (RequestSpecificValues.Current_Mode.Mode == Display_Mode_Enum.Item_Display) || (RequestSpecificValues.Current_Mode.Mode == Display_Mode_Enum.Item_Print) || ((Behaviors != null) && (Behaviors.Contains(HtmlSubwriter_Behaviors_Enum.MySobek_Subwriter_Mimic_Item_Subwriter)));

            if (useItemHeader)
            {
                RequestSpecificValues.Tracer.Add_Trace("HeaderFooter_Helper_HtmlSubWriter.Add_Header", "Going to use item header.");
            }
            else
            {
                RequestSpecificValues.Tracer.Add_Trace("HeaderFooter_Helper_HtmlSubWriter.Add_Header", "Will NOT use item header.");
            }
                
            // Create the breadcrumbs text
            string breadcrumbs = "&nbsp; &nbsp; ";
            if (useItemHeader)
            {
                StringBuilder breadcrumb_builder = new StringBuilder("<a href=\"" + RequestSpecificValues.Current_Mode.Base_URL + modified_url_options + "\">" + RequestSpecificValues.Current_Mode.Instance_Abbreviation + " Home</a>");

                int codes_added = 0;
                if ((RequestSpecificValues.Current_Mode.Aggregation.Length > 0) && (RequestSpecificValues.Current_Mode.Aggregation != "all"))
                {
                    breadcrumb_builder.Append(" &nbsp;|&nbsp; <a href=\"" + RequestSpecificValues.Current_Mode.Base_URL + RequestSpecificValues.Current_Mode.Aggregation + modified_url_options + "\">" + UI_ApplicationCache_Gateway.Aggregations.Get_Collection_Short_Name(RequestSpecificValues.Current_Mode.Aggregation) + "</a>");
                    codes_added++;
                }

                if (Current_Item != null)
                {
                    if (( Current_Item.Behaviors.Aggregation_Code_List != null ) && ( Current_Item.Behaviors.Aggregation_Code_List.Count > 0))
                    {
                        foreach (string aggrCode in Current_Item.Behaviors.Aggregation_Code_List)
                        {
                            if (aggrCode.ToLower() != RequestSpecificValues.Current_Mode.Aggregation)
                            {
                                if (( String.Compare(aggrCode,Current_Item.Behaviors.Source_Institution_Aggregation, StringComparison.OrdinalIgnoreCase ) != 0 ) &&
                                    ( String.Compare(aggrCode, "i" + Current_Item.Behaviors.Source_Institution_Aggregation, StringComparison.OrdinalIgnoreCase) != 0) &&
                                    ( String.Compare(aggrCode, Current_Item.Behaviors.Holding_Location_Aggregation, StringComparison.OrdinalIgnoreCase) != 0) &&
                                    (String.Compare(aggrCode, "i" + Current_Item.Behaviors.Holding_Location_Aggregation, StringComparison.OrdinalIgnoreCase) != 0))
                                {
                                    Item_Aggregation_Related_Aggregations thisAggr = UI_ApplicationCache_Gateway.Aggregations[aggrCode];
                                    if ((thisAggr != null) && (thisAggr.Active))
                                    {
                                        breadcrumb_builder.Append(" &nbsp;|&nbsp; <a href=\"" + RequestSpecificValues.Current_Mode.Base_URL +
                                                                  aggrCode.ToLower() + modified_url_options + "\">" +
                                                                  thisAggr.ShortName +
                                                                  "</a>");
                                        codes_added++;
                                    }
                                }
                            }

                            if (codes_added == 5)
                                break;
                        }
                    }

                    if (codes_added < 5)
                    {
                        if ( !String.IsNullOrEmpty(Current_Item.Behaviors.Source_Institution_Aggregation))
                        {
                            // Add source code
                            string source_code = Current_Item.Behaviors.Source_Institution_Aggregation;
                            if ((source_code[0] != 'i') && (source_code[0] != 'I'))
                                source_code = "I" + source_code;
                            Item_Aggregation_Related_Aggregations thisSourceAggr = UI_ApplicationCache_Gateway.Aggregations[source_code];
                            if ((thisSourceAggr != null) && (!thisSourceAggr.Hidden) && (thisSourceAggr.Active))
                            {
                                string source_name = thisSourceAggr.ShortName;
                                if (source_name.ToUpper() != "ADDED AUTOMATICALLY")
                                {
                                    breadcrumb_builder.Append(" &nbsp;|&nbsp; <a href=\"" + RequestSpecificValues.Current_Mode.Base_URL +
                                                              source_code.ToLower() + modified_url_options + "\">" +
                                                              source_name + "</a>");
                                }
                            }

                            // Add the holding code
                            if (( !String.IsNullOrEmpty(Current_Item.Behaviors.Holding_Location_Aggregation)) &&
                                (String.Compare(Current_Item.Behaviors.Source_Institution_Aggregation, Current_Item.Behaviors.Source_Institution_Aggregation, StringComparison.OrdinalIgnoreCase) != 0 ))
                            {
                                // Add holding code
                                string holding_code = Current_Item.Behaviors.Holding_Location_Aggregation;
                                if ((holding_code[0] != 'i') && (holding_code[0] != 'I'))
                                    holding_code = "I" + holding_code;

                                Item_Aggregation_Related_Aggregations thisAggr = UI_ApplicationCache_Gateway.Aggregations[holding_code];
                                if ((thisAggr != null) && (!thisAggr.Hidden) && (thisAggr.Active))
                                {
                                    string holding_name = thisAggr.ShortName;

                                    if (holding_name.ToUpper() != "ADDED AUTOMATICALLY")
                                    {
                                        breadcrumb_builder.Append(" &nbsp;|&nbsp; <a href=\"" + RequestSpecificValues.Current_Mode.Base_URL +
                                                                  holding_code.ToLower() + modified_url_options + "\">" +
                                                                  holding_name + "</a>");
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (!String.IsNullOrEmpty(Current_Item.Behaviors.Holding_Location_Aggregation))
                            {
                                // Add holding code
                                string holding_code = Current_Item.Behaviors.Holding_Location_Aggregation;
                                if ((holding_code[0] != 'i') && (holding_code[0] != 'I'))
                                    holding_code = "I" + holding_code;
                                string holding_name = UI_ApplicationCache_Gateway.Aggregations.Get_Collection_Short_Name(holding_code);
                                if (holding_name.ToUpper() != "ADDED AUTOMATICALLY")
                                {
                                    breadcrumb_builder.Append(" &nbsp;|&nbsp; <a href=\"" + RequestSpecificValues.Current_Mode.Base_URL +
                                                              holding_code.ToLower() + modified_url_options + "\">" +
                                                              holding_name + "</a>");
                                }
                            }
                        }
                    }
                }
                breadcrumbs = breadcrumb_builder.ToString();
            }
            else
            {
                switch (RequestSpecificValues.Current_Mode.Mode)
                {
                    case Display_Mode_Enum.Error:
                        breadcrumbs = "<a href=\"" + RequestSpecificValues.Current_Mode.Base_URL + modified_url_options + "\">" + RequestSpecificValues.Current_Mode.Instance_Abbreviation + " Home</a>";
                        break;

                    case Display_Mode_Enum.Aggregation:
                        if ((RequestSpecificValues.Current_Mode.Aggregation_Type == Aggregation_Type_Enum.Home) || (RequestSpecificValues.Current_Mode.Aggregation_Type == Aggregation_Type_Enum.Home_Edit))
                        {
                            if ((RequestSpecificValues.Current_Mode.Aggregation.Length > 0) && (RequestSpecificValues.Current_Mode.Aggregation != "all"))
                            {
                                breadcrumbs = "<a href=\"" + RequestSpecificValues.Current_Mode.Base_URL + modified_url_options + "\">" + RequestSpecificValues.Current_Mode.Instance_Abbreviation + " Home</a>";
                            }
                        }
                        else
                        {
                            breadcrumbs = "<a href=\"" + RequestSpecificValues.Current_Mode.Base_URL + modified_url_options + "\">" + RequestSpecificValues.Current_Mode.Instance_Abbreviation + " Home</a>";
                            if ((RequestSpecificValues.Current_Mode.Aggregation.Length > 0) && (RequestSpecificValues.Current_Mode.Aggregation != "all"))
                            {
                                breadcrumbs = breadcrumbs + " &nbsp;|&nbsp; <a href=\"" + RequestSpecificValues.Current_Mode.Base_URL + RequestSpecificValues.Current_Mode.Aggregation + modified_url_options + "\">" + UI_ApplicationCache_Gateway.Aggregations.Get_Collection_Short_Name(RequestSpecificValues.Current_Mode.Aggregation) + "</a>";
                            }
                        }
                        break;

                    default:
                        breadcrumbs = "<a href=\"" + RequestSpecificValues.Current_Mode.Base_URL + modified_url_options + "\">" + RequestSpecificValues.Current_Mode.Instance_Abbreviation + " Home</a>";
                        if ((RequestSpecificValues.Current_Mode.Aggregation.Length > 0) && (RequestSpecificValues.Current_Mode.Aggregation != "all"))
                        {
                            breadcrumbs = breadcrumbs + " &nbsp;|&nbsp; <a href=\"" + RequestSpecificValues.Current_Mode.Base_URL + RequestSpecificValues.Current_Mode.Aggregation + modified_url_options + "\">" + UI_ApplicationCache_Gateway.Aggregations.Get_Collection_Short_Name(RequestSpecificValues.Current_Mode.Aggregation) + "</a>";
                        }
                        break;
                }
            }

            // Create the mySobek text
            string mySobekLinks = create_mysobek_link(RequestSpecificValues, url_options, null);

            // Look for some basic mode data
            string collection_code = String.IsNullOrEmpty(RequestSpecificValues.Current_Mode.Aggregation) ? String.Empty : RequestSpecificValues.Current_Mode.Aggregation;
            if (String.Compare(collection_code, "ALL", StringComparison.OrdinalIgnoreCase) == 0)
                collection_code = String.Empty;
            string bibid = String.IsNullOrEmpty(RequestSpecificValues.Current_Mode.BibID) ? String.Empty : RequestSpecificValues.Current_Mode.BibID;
            string vid = String.IsNullOrEmpty(RequestSpecificValues.Current_Mode.BibID) ? String.Empty : RequestSpecificValues.Current_Mode.BibID;
            string mode = String.Empty;
            switch (RequestSpecificValues.Current_Mode.Mode)
            {
                case Display_Mode_Enum.Item_Display:
                    mode = "item";
                    break;

                case Display_Mode_Enum.Aggregation:
                case Display_Mode_Enum.Search:
                    mode = "aggregation";
                    break;

                case Display_Mode_Enum.Results:
                    mode = "results";
                    break;
            }

            // Get the language selections
            Web_Language_Enum language = RequestSpecificValues.Current_Mode.Language;
            RequestSpecificValues.Current_Mode.Language = Web_Language_Enum.TEMPLATE;
            string template_language = UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode);
            string english = template_language.Replace("l=XXXXX", "l=en");
            string french = template_language.Replace("l=XXXXX", "l=fr");
            string spanish = template_language.Replace("l=XXXXX", "l=es");
            RequestSpecificValues.Current_Mode.Language = language;

            if (RequestSpecificValues.Current_Mode.Is_Robot)
            {
                english = String.Empty;
                french = String.Empty;
                spanish = String.Empty;
            }

            // Determine which container to use, depending on the current mode
            string container_inner = Container_CssClass;

            // Get the skin url
            string skin_url = RequestSpecificValues.Current_Mode.Base_Design_URL + "skins/" + RequestSpecificValues.Current_Mode.Skin + "/";

            // Determine the URL options for replacement
            string urlOptions1 = String.Empty;
            string urlOptions2 = String.Empty;
            if (url_options.Length > 0)
            {
                urlOptions1 = "?" + url_options;
                urlOptions2 = "&" + url_options;
            }

            // Determine the possible banner to display
            string banner = String.Empty;
            if (( Behaviors != null ) && ( !Behaviors.Contains(HtmlSubwriter_Behaviors_Enum.Suppress_Banner)))
            {
                if ((RequestSpecificValues.HTML_Skin != null) && (RequestSpecificValues.HTML_Skin.Override_Banner.HasValue) && (RequestSpecificValues.HTML_Skin.Override_Banner.Value))
                {
                    banner = !String.IsNullOrEmpty(RequestSpecificValues.HTML_Skin.Banner_HTML) ? RequestSpecificValues.HTML_Skin.Banner_HTML : String.Empty;
                }
                else
                {
                    if (Current_Aggregation!= null)
                    {
                        string banner_image = Current_Aggregation.Get_Banner_Image( RequestSpecificValues.HTML_Skin);
                        RequestSpecificValues.Tracer.Add_Trace("HeaderFooter_Helper_HtmlSubWriter.Add_Header", "banner_image=[" + banner_image + "].");
                        RequestSpecificValues.Tracer.Add_Trace("HeaderFooter_Helper_HtmlSubWriter.Add_Header","Current_Aggregation_Shortname=[" + Current_Aggregation.ShortName + "].");

                        if (Current_Aggregation.Code != "all")
                        {
                            if (banner_image.Length > 0)
                                banner = "<section id=\"sbkHmw_BannerDiv\" role=\"banner\" title=\"" + Current_Aggregation.ShortName + "\"><h1 class=\"hidden-element\">" + Web_Page_Title + "</h1><a alt=\"" + Current_Aggregation.ShortName + "\" href=\"" + RequestSpecificValues.Current_Mode.Base_URL + Current_Aggregation.Code + urlOptions1 + "\"><img id=\"mainBanner\" src=\"" + RequestSpecificValues.Current_Mode.Base_URL + banner_image + "\"  alt=\"" + Current_Aggregation.ShortName + "\" /></a></section>";
                        }
                        else
                        {
                            if (banner_image.Length > 0)
                            {
                                banner = "<section id=\"sbkHmw_BannerDiv\" role=\"banner\" title=\"" + Current_Aggregation.ShortName + "\"><h1 class=\"hidden-element\">" + Web_Page_Title + "</h1><a alt=\"" + Current_Aggregation.ShortName + "\"  href=\"" + RequestSpecificValues.Current_Mode.Base_URL + urlOptions1 + "\"><img id=\"mainBanner\" src=\"" + RequestSpecificValues.Current_Mode.Base_URL + banner_image + "\"  alt=\"" + Current_Aggregation.ShortName + "\" /></a></section>";
                            }
                            else
                            {
                                banner = "<section id=\"sbkHmw_BannerDiv\" role=\"banner\" title=\"" + Current_Aggregation.ShortName + "\"><h1 class=\"hidden-element\">" + Web_Page_Title + "</h1><a alt=\"" + Current_Aggregation.ShortName + "\"  href=\"" + RequestSpecificValues.Current_Mode.Base_URL + urlOptions1 + "\"><img id=\"mainBanner\" src=\"" + skin_url + "default.jpg\" alt=\"" + Current_Aggregation.ShortName + "\" /></a></section>";
                            }
                        }
                    }
                }
            }

            banner = "<!-- banner start -->\r\n" + banner + "<!-- banner end -->\r\n";

            // Get the session id and user id
            string sessionId = HttpContext.Current.Session.SessionID ?? String.Empty;
            string userid = ((RequestSpecificValues.Current_User != null) && (RequestSpecificValues.Current_User.UserID > 0)) ? RequestSpecificValues.Current_User.UserID.ToString() : String.Empty;

            // Add the appropriate header
            StringBuilder headerBuilder = new StringBuilder();
            try
            {
                if (useItemHeader)
                {
                    headerBuilder.Append(RequestSpecificValues.HTML_Skin.Header_Item_HTML);
                }
                else
                {
                    headerBuilder.Append(RequestSpecificValues.HTML_Skin.Header_HTML);

                    if ((!String.IsNullOrEmpty(container_inner)) && ((!RequestSpecificValues.HTML_Skin.Header_Has_Container_Directive.HasValue) || (!RequestSpecificValues.HTML_Skin.Header_Has_Container_Directive.Value)))
                        headerBuilder.Insert(0, "<div id=\"" + container_inner + "\">" + Environment.NewLine);
                }

                // Do all the replacements
                headerBuilder.Replace("<%COLLCODE%>", collection_code);
                headerBuilder.Replace("<%BIBID%>", bibid);
                headerBuilder.Replace("<%VID%>", vid);
                headerBuilder.Replace("<%MODE%>", mode);
  //              headerBuilder.Replace("<%COLLNAME%>", collection_name);
                headerBuilder.Replace("<%CONTACT%>", contact);
                headerBuilder.Replace("<%URLOPTS%>", url_options);
                headerBuilder.Replace("<%?URLOPTS%>", urlOptions1);
                headerBuilder.Replace("<%&URLOPTS%>", urlOptions2);
                headerBuilder.Replace("<%BREADCRUMBS%>", breadcrumbs);
                headerBuilder.Replace("<%MYSOBEK%>", mySobekLinks);
                headerBuilder.Replace("<%ENGLISH%>", english);
                headerBuilder.Replace("<%FRENCH%>", french);
                headerBuilder.Replace("<%SPANISH%>", spanish);
                headerBuilder.Replace("<%BASEURL%>", RequestSpecificValues.Current_Mode.Base_URL);
                headerBuilder.Replace("\"container-inner\"", "\"" + container_inner + "\"");
                headerBuilder.Replace("<%BANNER%>", banner);
                headerBuilder.Replace("<%SKINURL%>", skin_url);
                if (( !useItemHeader) && ( !String.IsNullOrEmpty(container_inner)) && (RequestSpecificValues.HTML_Skin.Header_Has_Container_Directive.HasValue) && (RequestSpecificValues.HTML_Skin.Header_Has_Container_Directive.Value))
                    headerBuilder.Replace("<%CONTAINER%>", "<div id=\"" + container_inner + "\">");
                else
                    headerBuilder.Replace("<%CONTAINER%>", String.Empty);
                headerBuilder.Replace("<%INSTANCENAME%>", RequestSpecificValues.Current_Mode.Instance_Name);
                headerBuilder.Replace("<%SESSIONID%>", sessionId);
                headerBuilder.Replace("<%USERID%>", userid);
            }
            catch (Exception ee)
            {
                RequestSpecificValues.Tracer.Add_Trace("HeaderFooter_Helper_HtmlSubWriter.Add_Header", "EXCEPTION CAUGHT while trying to write the header.");
                if (RequestSpecificValues.HTML_Skin == null)
                    RequestSpecificValues.Tracer.Add_Trace("HeaderFooter_Helper_HtmlSubWriter.Add_Header", "HTML Skin was NULL");
                else if (RequestSpecificValues.HTML_Skin.Header_Item_HTML == null)
                    RequestSpecificValues.Tracer.Add_Trace("HeaderFooter_Helper_HtmlSubWriter.Add_Header", "HTML Skin was not NULL, but Header_Item_HTML property was NULL");
            }

            // Write the header
            Output.WriteLine(headerBuilder.ToString());
        }

        /// <summary> Add the header to the output </summary>
        /// <param name="Output"> Stream to which to write the HTML for this header </param>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        /// <param name="Behaviors"> List of behaviors from the html subwriters </param>
        /// <param name="Current_Aggregation"> Current aggregation object, if there is one </param>
        /// <param name="Current_Item"> Current item object, if there is one </param>
        public static void Add_Footer(TextWriter Output, RequestCache RequestSpecificValues, List<HtmlSubwriter_Behaviors_Enum> Behaviors, Item_Aggregation Current_Aggregation, BriefItemInfo Current_Item)
        {
            RequestSpecificValues.Tracer.Add_Trace("HeaderFooter_Helper_HtmlSubWriter.Add_Footer");

            // Determine which header and footer to display
            bool useItemFooter = (RequestSpecificValues.Current_Mode.Mode == Display_Mode_Enum.Item_Display) || (RequestSpecificValues.Current_Mode.Mode == Display_Mode_Enum.Item_Print) || ((Behaviors != null) && (Behaviors.Contains(HtmlSubwriter_Behaviors_Enum.MySobek_Subwriter_Mimic_Item_Subwriter)));

            if (useItemFooter)
            {
                RequestSpecificValues.Tracer.Add_Trace("HeaderFooter_Helper_HtmlSubWriter.Add_Footer", "WILL use item footer.");
            }
            else
            {
                RequestSpecificValues.Tracer.Add_Trace("HeaderFooter_Helper_HtmlSubWriter.Add_Footer", "Will NOT use item footer.");
            }

            // Get the current contact URL
            Display_Mode_Enum thisMode = RequestSpecificValues.Current_Mode.Mode;
            RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Contact;
            string contact = UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode);

            // Restore the old mode
            RequestSpecificValues.Current_Mode.Mode = thisMode;

            // Get the URL options
            string url_options = UrlWriterHelper.URL_Options(RequestSpecificValues.Current_Mode);
            string urlOptions1 = String.Empty;
            string urlOptions2 = String.Empty;
            if (url_options.Length > 0)
            {
                urlOptions1 = "?" + url_options;
                urlOptions2 = "&" + url_options;
            }

            // Create the mySobek text
            string mySobekLinks = create_mysobek_link(RequestSpecificValues, url_options, "staff login");

            // Get the base url
            string base_url = RequestSpecificValues.Current_Mode.Base_URL;
            if (RequestSpecificValues.Current_Mode.Writer_Type == Writer_Type_Enum.HTML_LoggedIn)
                base_url = base_url + "l/";

            // Look for the collection code and name
            string collection_code = String.IsNullOrEmpty(RequestSpecificValues.Current_Mode.Aggregation) ? String.Empty : RequestSpecificValues.Current_Mode.Aggregation;
            string bibid = String.IsNullOrEmpty(RequestSpecificValues.Current_Mode.BibID) ? String.Empty : RequestSpecificValues.Current_Mode.BibID;
            string vid = String.IsNullOrEmpty(RequestSpecificValues.Current_Mode.BibID) ? String.Empty : RequestSpecificValues.Current_Mode.BibID;
            string mode = String.Empty;
            switch (RequestSpecificValues.Current_Mode.Mode)
            {
                case Display_Mode_Enum.Item_Display:
                    mode = "item";
                    break;

                case Display_Mode_Enum.Aggregation:
                case Display_Mode_Enum.Search:
                    mode = "aggregation";
                    break;

                case Display_Mode_Enum.Results:
                    mode = "results";
                    break;
            }

            // Get the skin url
            string skin_url = RequestSpecificValues.Current_Mode.Base_Design_URL + "skins/" + RequestSpecificValues.HTML_Skin.Skin_Code + "/";

            bool end_div = true;// !((RequestSpecificValues.Current_Mode.Mode == Display_Mode_Enum.Simple_HTML_CMS) && (RequestSpecificValues.Site_Map != null));

            string version = UI_ApplicationCache_Gateway.Settings.Static.Current_Web_Version;
            if (version.IndexOf(" ") > 0)
                version = version.Split(" ".ToCharArray())[0];

            // Get the session id
            string sessionId = HttpContext.Current.Session.SessionID ?? String.Empty;
            string userid = ((RequestSpecificValues.Current_User != null) && (RequestSpecificValues.Current_User.UserID > 0)) ? RequestSpecificValues.Current_User.UserID.ToString() : String.Empty;

            StringBuilder footerBuilder = new StringBuilder();

            if (useItemFooter)
            {
                footerBuilder.Append(RequestSpecificValues.HTML_Skin.Footer_Item_HTML);
            }
            else
            {
                if (( RequestSpecificValues.HTML_Skin.Footer_Has_Container_Directive.HasValue) && (RequestSpecificValues.HTML_Skin.Footer_Has_Container_Directive.Value))
                {
                    footerBuilder.Append(RequestSpecificValues.HTML_Skin.Footer_HTML);
                }
                else
                {
                    footerBuilder.Append(RequestSpecificValues.HTML_Skin.Footer_HTML + Environment.NewLine + "</div>");
                }
            }

            // Make all the replacements
            footerBuilder.Replace("<%COLLCODE%>", collection_code);
            footerBuilder.Replace("<%BIBID%>", bibid);
            footerBuilder.Replace("<%VID%>", vid);
            footerBuilder.Replace("<%MODE%>", mode);
            footerBuilder.Replace("<%MYSOBEK%>", mySobekLinks);
            footerBuilder.Replace("<%CONTACT%>", contact);
            footerBuilder.Replace("<%URLOPTS%>", url_options);
            footerBuilder.Replace("<%?URLOPTS%>", urlOptions1);
            footerBuilder.Replace("<%&URLOPTS%>", urlOptions2);
            footerBuilder.Replace("<%VERSION%>", version);
            footerBuilder.Replace("<%BASEURL%>", base_url);
            footerBuilder.Replace("<%SKINURL%>", skin_url);
            footerBuilder.Replace("<%INSTANCENAME%>", RequestSpecificValues.Current_Mode.Instance_Name);
            footerBuilder.Replace("<%SESSIONID%>", sessionId);
            footerBuilder.Replace("<%USERID%>", userid);
            if ((!useItemFooter) && ( RequestSpecificValues.HTML_Skin.Footer_Has_Container_Directive.HasValue) && (RequestSpecificValues.HTML_Skin.Footer_Has_Container_Directive.Value))
                footerBuilder.Replace("<%CONTAINER%>", "</div>");

            // Write this to the stream
            Output.WriteLine(footerBuilder.ToString().Trim());
        }

        private static string create_mysobek_link( RequestCache RequestSpecificValues, string url_options, string login_text )
        {
            RequestSpecificValues.Tracer.Add_Trace("HeaderFooter_Helper_HtmlSubWriter.create_mysobek_link");

            string mySobekLinks = String.Empty;
            if (!RequestSpecificValues.Current_Mode.Is_Robot)
            {
                string mySobekText = "my" + RequestSpecificValues.Current_Mode.Instance_Abbreviation;
                string mySobekOptions = url_options;
                string mySobekLogoutOptions = url_options;
                string return_url = String.Empty;
                if ((HttpContext.Current != null) && (HttpContext.Current.Items["Original_URL"] != null))
                    return_url = HttpContext.Current.Items["Original_URL"].ToString().ToLower().Replace(RequestSpecificValues.Current_Mode.Base_URL.ToLower(), "");
                if (return_url.IndexOf("?") > 0)
                    return_url = return_url.Substring(0, return_url.IndexOf("?"));
                if (return_url.IndexOf("my/") == 0)
                    return_url = String.Empty;
                string logout_return_url = return_url;
                if (logout_return_url.IndexOf("l/") == 0)
                    logout_return_url = logout_return_url.Substring(2);

                return_url = HttpUtility.UrlEncode(return_url);
                logout_return_url = HttpUtility.UrlEncode(logout_return_url);

                if ((url_options.Length > 0) || (return_url.Length > 0))
                {
                    if ((url_options.Length > 0) && (return_url.Length > 0))
                    {
                        mySobekOptions = "?" + mySobekOptions + "&return=" + return_url;
                    }
                    else
                    {
                        if (url_options.Length > 0)
                            mySobekOptions = "?" + mySobekOptions;
                        else
                            mySobekOptions = "?return=" + return_url;
                    }
                }

                if ((url_options.Length > 0) || (logout_return_url.Length > 0))
                {
                    if ((url_options.Length > 0) && (logout_return_url.Length > 0))
                    {
                        mySobekLogoutOptions = "?" + mySobekOptions + "&return=" + logout_return_url;
                    }
                    else
                    {
                        if (url_options.Length > 0)
                            mySobekLogoutOptions = "?" + mySobekOptions;
                        else
                            mySobekLogoutOptions = "?return=" + logout_return_url;
                    }
                }

                if ((HttpContext.Current != null) && (HttpContext.Current.Session["user"] == null))
                {
                    if (!String.IsNullOrEmpty(login_text))
                        mySobekLinks = "<a href=\"" + RequestSpecificValues.Current_Mode.Base_URL + "my/logon" + mySobekOptions + "\">" + login_text + "</a>";
                    else
                        mySobekLinks = "<a href=\"" + RequestSpecificValues.Current_Mode.Base_URL + "my/logon" + mySobekOptions + "\">" + mySobekText + " Home</a>";
                }
                else
                {
                    User_Object tempObject = ((User_Object)HttpContext.Current.Session["user"]);
                    if (tempObject.Nickname.Length > 0)
                    {
                        mySobekLinks = "<a href=\"" + RequestSpecificValues.Current_Mode.Base_URL + "my" + mySobekOptions + "\">" + tempObject.Nickname + "'s " + mySobekText + "</a>&nbsp; | &nbsp; <a href=\"" + RequestSpecificValues.Current_Mode.Base_URL + "my/logout" + mySobekLogoutOptions + "\">Log Out</a>";
                    }
                    else
                    {
                        mySobekLinks = "<a href=\"" + RequestSpecificValues.Current_Mode.Base_URL + "my" + mySobekOptions + "\">" + tempObject.Given_Name + "'s " + mySobekText + "</a>&nbsp; | &nbsp; <a href=\"" + RequestSpecificValues.Current_Mode.Base_URL + "my/logout" + mySobekLogoutOptions + "\">Log Out</a>";
                    }
                }
            }

            return mySobekLinks;
        }
    }
}
