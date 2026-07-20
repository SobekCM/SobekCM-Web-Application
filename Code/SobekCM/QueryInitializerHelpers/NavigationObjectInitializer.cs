using Microsoft.AspNetCore.Http;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.Navigation;
using SobekCM.Engine_Library.Navigation;
using SobekCM.Library;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SobekCM.QueryInitializerHelpers
{
    public class NavigationObjectInitializer : IQueryInitializerHelper
    {
        public QueryInitializerHelperResponse Initialize(HttpContext context, RequestCache request, Custom_Tracer tracer)
        {
            tracer.Add_Trace("NavigationObjectInitializer.Initialize", "About to parse the URL for the navigation object");

            if (!context.Items.ContainsKey(RequestCache_Keys.BaseUrl) || !context.Items.ContainsKey(RequestCache_Keys.RequestUrl))
            {
                return new QueryInitializerHelperResponse(false, "The NavigationObjectInitializer must be used after the BaseUrlInitializer in the query initializer list.");
            }

            string base_url = context.Items[RequestCache_Keys.BaseUrl].ToString();
            string request_url = context.Items[RequestCache_Keys.RequestUrl].ToString();

            if (!context.Items.ContainsKey(RequestCache_Keys.UserIP))
            {
                return new QueryInitializerHelperResponse(false, "The NavigationObjectInitializer must be used after the UserIpInitializer in the query initializer list.");
            }

            string userip = context.Items[RequestCache_Keys.UserIP].ToString();

            // Analyze the response and get the mode
            var currentMode = new Navigation_Object();
            request.Current_Mode = currentMode;

            try
            {
                var contextRequest = context.Request;

                var languages = get_user_languages(contextRequest);

                QueryString_Analyzer.Parse_Query(request.QueryString, currentMode, base_url, languages, UI_ApplicationCache_Gateway.Aggregations, UI_ApplicationCache_Gateway.Collection_Aliases, UI_ApplicationCache_Gateway.URL_Portals, UI_ApplicationCache_Gateway.WebContent_Hierarchy, UI_ApplicationCache_Gateway.Settings.System.Custom_BibID_RegEx, tracer);

                currentMode.Base_URL = base_url;
                currentMode.isPostBack = string.Equals(context.Request.Method, "POST", StringComparison.OrdinalIgnoreCase); ;
                currentMode.Browser_Type = get_browser_type(contextRequest.Headers.UserAgent);
                currentMode.Set_Robot_Flag(contextRequest.Headers.UserAgent, userip);
            }
            catch (Exception ee)
            {
                tracer.Add_Trace("NavigationObjectInitializer.Initialize", "Exception caught while parsing the query string: " + ee.Message);
                tracer.Add_Trace("NavigationObjectInitializer.Initialize", ee.StackTrace);

                context.Response.StatusCode = 301;
                return new QueryInitializerHelperResponse(false, "Exception caught while parsing the query string.", ee);
            }

            // If this was for HTML, but was at the data, just convert to XML 
            /// TODO: I think this can be removed
            //if ((page_name == "SOBEKCM_DATA") && (currentMode.Writer_Type != Writer_Type_Enum.XML) && (currentMode.Writer_Type != Writer_Type_Enum.JSON) && (currentMode.Writer_Type != Writer_Type_Enum.DataSet) && (currentMode.Writer_Type != Writer_Type_Enum.Data_Provider))
            //    currentMode.Writer_Type = Writer_Type_Enum.XML;

            tracer.Add_Trace("NavigationObjectInitializer.Initialize", "Navigation parse completed");

            return QueryInitializerHelperResponse.Successful;
        }


        private static string get_browser_type(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent)) return "UNKNOWN";
            string ua = userAgent.ToUpper();
            // Order matters: Edge/IE must precede Chrome/Safari which share UA tokens
            if (ua.Contains("TRIDENT") || ua.Contains("MSIE")) return "IE";
            if (ua.Contains("EDG/") || ua.Contains("EDGHTML")) return "EDGE";
            if (ua.Contains("FIREFOX")) return "FIREFOX";
            if (ua.Contains("CHROME")) return "CHROME";
            if (ua.Contains("SAFARI")) return "SAFARI";
            return "UNKNOWN";
        }

        private static IEnumerable<string> get_user_languages(HttpRequest request)
        {
            return request.GetTypedHeaders()
                                .AcceptLanguage?
                                .OrderByDescending(x => x.Quality ?? 1)
                                .Select(x => x.Value.Value)
                                .ToList();
        }
    }
}
