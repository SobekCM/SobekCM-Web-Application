using Microsoft.AspNetCore.Http;
using SobekCM.Core.Navigation;
using SobekCM.Library;
using SobekCM.Tools;
using System;

namespace SobekCM.QueryInitializerHelpers
{
    public class SearchEngineRobotNavigationInitializer : IQueryInitializerHelper
    {
        public QueryInitializerHelperResponse Initialize(HttpContext context, RequestCache request, Custom_Tracer tracer)
        {
            tracer.Add_Trace("SearchEngineRobotNavigationInitializer.Initialize");

            var currentMode = request.Current_Mode;

            if (currentMode == null)
            {
                return new QueryInitializerHelperResponse(false, "The SearchEngineRobotNavigationInitializer must be called after the NavigationObjectInitializer has configured the NavigationObject");
            }

            if (!currentMode.Is_Robot)
            {
                return QueryInitializerHelperResponse.Successful;
            }

            // Special checks for search engine robot URL behaviors

            // Some writers should not be selected yet
            if ((currentMode.Writer_Type != Writer_Codes.HTML) && (currentMode.Writer_Type != Writer_Codes.HTML_Echo) && (currentMode.Writer_Type != Writer_Codes.OAI))
            {
                return error_and_redirect(context, currentMode);
            }

            // There are some spots which robots are never allowed to go, just
            // by virtue of the fact they don't logon
            if ((currentMode.Mode == Display_Mode_Enum.Internal) || (currentMode.Mode == Display_Mode_Enum.My_Sobek) || (currentMode.Mode == Display_Mode_Enum.Administrative) || (currentMode.Mode == Display_Mode_Enum.Reset) || (currentMode.Mode == Display_Mode_Enum.Item_Cache_Reload) || (currentMode.Mode == Display_Mode_Enum.Results) || (currentMode.Mode == Display_Mode_Enum.Public_Folder) || ((currentMode.Mode == Display_Mode_Enum.Aggregation) && (currentMode.Aggregation_Type == Aggregation_Type_Enum.Browse_By)) || (currentMode.Mode == Display_Mode_Enum.Item_Print))
            {
                return error_and_redirect(context, currentMode);
            }

            // Browse are okay, except when it is the NEW
            if ((currentMode.Mode == Display_Mode_Enum.Aggregation) && (currentMode.Aggregation_Type == Aggregation_Type_Enum.Browse_Info) && (!String.IsNullOrEmpty(currentMode.Info_Browse_Mode)) && (currentMode.Info_Browse_Mode == "new"))
            {
                currentMode.Info_Browse_Mode = "all";

                return error_and_redirect(context, currentMode);
            }

            // Going to the search page is okay, except for ADVANCED searches ( results aren't okay, but going to the search page is okay )
            if ((currentMode.Mode == Display_Mode_Enum.Search) && (currentMode.Search_Type == Search_Type_Enum.Advanced))
            {
                currentMode.Mode = Display_Mode_Enum.Aggregation;
                currentMode.Aggregation_Type = Aggregation_Type_Enum.Home;

                return error_and_redirect(context, currentMode);
            }

            int url_relative_depth = 0;
            string[] url_relative_info = null;
            if (request.QueryString != null)
            {
                var QueryString = request.QueryString;

                // If this was a legacy type request, forward to the new URL
                if ((request.HasNonEmptyQueryString("b")) || (request.HasNonEmptyQueryString("m")) || (request.HasNonEmptyQueryString("g")) || (request.HasNonEmptyQueryString("c")) || (request.HasNonEmptyQueryString("s")) || (request.HasNonEmptyQueryString("a")))
                {
                    return error_and_redirect(context, currentMode);
                }

                // Get the depth of the url relative
                // Try to determine if this is a legacy type URL and how deep the urlrelative is
                if (QueryString.TryGetValue("urlrelative", out string urlrelativeValue) && (urlrelativeValue != null))
                {
                    string urlrewrite = urlrelativeValue.ToLower();
                    if (urlrewrite.Length > 0)
                    {
                        // Split the url relative list
                        url_relative_info = urlrewrite.Split("/".ToCharArray());
                        url_relative_depth = url_relative_info.Length;
                    }
                }
            }

            // For STATISTICS, handle some specific cases and enforce appropriate URLs
            if (currentMode.Mode == Display_Mode_Enum.Statistics)
            {
                // Some submodes are off limites
                if ((currentMode.Statistics_Type != Statistics_Type_Enum.Item_Count_Growth_View) && (currentMode.Statistics_Type != Statistics_Type_Enum.Item_Count_Standard_View) && (currentMode.Statistics_Type != Statistics_Type_Enum.Item_Count_Text) && (currentMode.Statistics_Type != Statistics_Type_Enum.Usage_Definitions) && (currentMode.Statistics_Type != Statistics_Type_Enum.Usage_Overall))
                {
                    currentMode.Statistics_Type = Statistics_Type_Enum.Usage_Overall;
                    return error_and_redirect(context, currentMode);
                }

                // Ensure the URL behaved correctly
                switch (currentMode.Statistics_Type)
                {
                    case Statistics_Type_Enum.Item_Count_Text:
                    case Statistics_Type_Enum.Item_Count_Growth_View:
                    case Statistics_Type_Enum.Usage_Definitions:
                        if (url_relative_depth > 3)
                        {
                            return error_and_redirect(context, currentMode);
                        }
                        break;

                    case Statistics_Type_Enum.Usage_Overall:
                        if (url_relative_depth > 2)
                        {
                            return error_and_redirect(context, currentMode);
                        }
                        break;

                    case Statistics_Type_Enum.Item_Count_Standard_View:
                        if (url_relative_depth > 2)
                        {
                            return error_and_redirect(context, currentMode);
                        }
                        else if (url_relative_depth == 2)
                        {
                            if ((url_relative_info != null) && (url_relative_info.Length > 1) && (url_relative_info[1] != "itemcount"))
                            {
                                return error_and_redirect(context, currentMode);
                            }
                        }
                        break;
                }
            }

            // For AGGREGATION HOME handle some cases
            if ((currentMode.Mode == Display_Mode_Enum.Aggregation) && ((currentMode.Aggregation_Type == Aggregation_Type_Enum.Home) || (currentMode.Aggregation_Type == Aggregation_Type_Enum.Home_Edit)))
            {
                // Different code depending on if this is an aggregation or not
                if ((String.IsNullOrEmpty(currentMode.Aggregation)) || (currentMode.Aggregation == "all"))
                {
                    switch (currentMode.Home_Type)
                    {
                        case Home_Type_Enum.List:
                            if (url_relative_depth > 0)
                            {
                                return error_and_redirect(context, currentMode);
                            }
                            break;

                        case Home_Type_Enum.Descriptions:
                        case Home_Type_Enum.Tree:
                        case Home_Type_Enum.Partners_List:
                            if (url_relative_depth > 1)
                            {
                                return error_and_redirect(context, currentMode);
                            }
                            break;

                        case Home_Type_Enum.Partners_Thumbnails:
                            if (url_relative_depth > 2)
                            {
                                return error_and_redirect(context, currentMode);
                            }
                            break;

                        case Home_Type_Enum.Personalized:
                            return error_and_redirect(context, currentMode);
                    }
                }
            }

            // Ensure this is requesting the item without a viewercode and without extraneous information
            if (currentMode.Mode == Display_Mode_Enum.Item_Display)
            {
                if ((!String.IsNullOrEmpty(currentMode.ViewerCode)) || (url_relative_depth > 2))

                {
                    currentMode.ViewerCode = String.Empty;

                    return error_and_redirect(context, currentMode);
                }
            }

            return QueryInitializerHelperResponse.Successful;
        }

        private QueryInitializerHelperResponse error_and_redirect(HttpContext context, Navigation_Object currentMode)
        {
            // A disallowed robot URL is an expected, routine redirect -- not a failure -- so this
            // reports Success (matching the RedirectUrl convention used elsewhere, e.g.
            // UserObjectInitializer) rather than routing through handle_error, which logs and throws.
            return new QueryInitializerHelperResponse(true) { RedirectUrl = UrlWriterHelper.Redirect_URL(currentMode) };
        }
    }
}
