using Microsoft.AspNetCore.Http;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Library;
using SobekCM.Tools;
using System;
using System.Collections.Generic;

namespace SobekCM.QueryInitializerHelpers
{
    public class UrlInitializer : IQueryInitializerHelper
    {
        public QueryInitializerHelperResponse Initialize(HttpContext context, RequestCache request, Custom_Tracer tracer)
        {
            tracer.Add_Trace("UserIpInitializer.Initialize");

            // Pull out the http request
            HttpRequest httpRequest = context.Request;

            // Get the base url (site root, not the current page's path — this feeds Base_Design_URL
            // and must stay a stable prefix for design/skin/aggregation asset links on every page)
            var base_url = $"{httpRequest.Scheme}://{httpRequest.Host}{httpRequest.PathBase}/";

            // Add the base url to the reqeust cache for use later
            context.Items.Add(RequestCache_Keys.BaseUrl, base_url);

            // Build the full request url 
            var request_url = $"{httpRequest.Scheme}://{httpRequest.Host}{httpRequest.PathBase}{httpRequest.Path}{httpRequest.QueryString}";

            // Add the full request url to the request cache for use later
            context.Items[RequestCache_Keys.RequestUrl] = request_url;
            context.Items[RequestCache_Keys.OriginalUrl] = request_url;

            // Check that something is saved for the original requested URL (may not exist if not forwarded)
            if (String.IsNullOrEmpty(context.Session.GetString(SessionCache_Keys.OriginalUrl)))
                context.Session.SetString(SessionCache_Keys.OriginalUrl, request_url);

            request.QueryString = get_query_string_dict(httpRequest.QueryString);

            return QueryInitializerHelperResponse.Successful;
        }

        private static Dictionary<string, string> get_query_string_dict(QueryString QueryString)
        {
            // Convert QueryString to Dictionary for easier access
            var queryParams = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (QueryString.HasValue)
            {
                string queryString = QueryString.Value.Replace("?", "");
                foreach (var kvp in queryString.Split('&'))
                {
                    var parts = kvp.Split('=');
                    if (parts.Length == 2)
                    {
                        queryParams[parts[0]] = System.Net.WebUtility.UrlDecode(parts[1]);
                    }
                    else if (parts.Length == 1 && !string.IsNullOrEmpty(parts[0]))
                    {
                        queryParams[parts[0]] = string.Empty;
                    }
                }
            }
            return queryParams;
        }
    }
}
