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

            // The original URL is the one the visitor asked for. PrettyUrlRewriteMiddleware records it before folding
            // the path into the internal "urlrelative" query parameter; this used to overwrite that with the rewritten
            // URL, so share links, email links and every itemNavForm action carried ?urlrelative=... to the visitor.
            string original_url = context.Items[RequestCache_Keys.OriginalUrl] as string;
            if (String.IsNullOrEmpty(original_url))
            {
                original_url = without_urlrelative(request_url);
                context.Items[RequestCache_Keys.OriginalUrl] = original_url;
            }

            // Check that something is saved for the original requested URL (may not exist if not forwarded)
            if (String.IsNullOrEmpty(context.Session.GetString(SessionCache_Keys.OriginalUrl)))
                context.Session.SetString(SessionCache_Keys.OriginalUrl, original_url);

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

        /// <summary> Removes the internal "urlrelative" query parameter (added by PrettyUrlRewriteMiddleware) from a URL,
        /// leaving the rest of the query string as it was </summary>
        private static string without_urlrelative(string Url)
        {
            int query_start = Url.IndexOf('?');
            if (query_start < 0)
                return Url;

            var kept = new List<string>();
            foreach (string pair in Url.Substring(query_start + 1).Split('&'))
            {
                if ((pair.Length > 0) && (!pair.StartsWith("urlrelative=", StringComparison.OrdinalIgnoreCase)) && (!String.Equals(pair, "urlrelative", StringComparison.OrdinalIgnoreCase)))
                    kept.Add(pair);
            }

            return (kept.Count > 0) ? Url.Substring(0, query_start + 1) + String.Join("&", kept) : Url.Substring(0, query_start);
        }
    }
}
