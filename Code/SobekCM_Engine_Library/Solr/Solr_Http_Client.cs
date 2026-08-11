#region Using directives

using SobekCM.Engine_Library.ApplicationState;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

#endregion

namespace SobekCM.Engine_Library.Solr
{
    /// <summary> Thin static client for talking to a Solr core's '/select' and '/update' handlers directly over
    /// HTTP, using Solr's native JSON request/response format. Replaces SolrNet's ISolrOperations&lt;T&gt; ( and
    /// the connection-caching Solr_Operations_Cache&lt;T&gt; that used to build one ) -- there is no container or
    /// per-URL connection object to maintain here, just a shared HttpClient and the core's base URL passed into
    /// every call. </summary>
    public static class Solr_Http_Client
    {
        private static readonly JsonSerializerOptions serializerOptions = Create_Serializer_Options();

        private static JsonSerializerOptions Create_Serializer_Options()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new Solr_DateTime_Converter());
            return options;
        }

        private static readonly HttpClient httpClient = Create_Http_Client();

        private static HttpClient Create_Http_Client()
        {
            var handler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(5)
            };

            return new HttpClient(handler)
            {
                Timeout = TimeSpan.FromMinutes(5)
            };
        }

        /// <summary> Execute a query against a Solr core's '/select' handler and deserialize the native JSON response </summary>
        /// <typeparam name="T"> Document type expected in the result set </typeparam>
        /// <param name="CoreUrl"> Base URL for the Solr core to query ( no trailing slash ) </param>
        /// <param name="Query"> Lucene query string ( the 'q' parameter ) </param>
        /// <param name="Options"> Additional query options ( paging, fields, sort, highlighting, facets, grouping ) </param>
        /// <returns> Deserialized query result </returns>
        public static Solr_Query_Result<T> Select<T>(string CoreUrl, string Query, Solr_Query_Options Options)
        {
            var form = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("q", Query),
                new KeyValuePair<string, string>("wt", "json"),
                new KeyValuePair<string, string>("json.nl", "map"),
                new KeyValuePair<string, string>("rows", Options.Rows.ToString()),
                new KeyValuePair<string, string>("start", Options.Start.ToString())
            };

            if ((Options.Fields != null) && (Options.Fields.Count > 0))
                form.Add(new KeyValuePair<string, string>("fl", String.Join(",", Options.Fields)));

            if ((Options.Sort != null) && (Options.Sort.Count > 0))
            {
                string sortValue = String.Join(",", Options.Sort.Select(clause => clause.Field + " " + (clause.Descending ? "desc" : "asc")));
                form.Add(new KeyValuePair<string, string>("sort", sortValue));
            }

            if (Options.Highlight)
            {
                form.Add(new KeyValuePair<string, string>("hl", "true"));

                // The FastVectorHighlighter (and Solr's newer default "unified" method) both flatten the
                // *entire* query tree to extract highlight terms, not just the clause against the highlighted
                // field -- and that fails (FastVector: throws; unified: silently returns no snippet) as soon
                // as the query also includes a Point-typed clause like "discover_ips:0", which every search
                // built by this app includes. The classic highlighter only inspects the highlighted field's
                // own clause, so it isn't affected and is what actually produces snippets on our queries.
                form.Add(new KeyValuePair<string, string>("hl.method", "original"));

                form.Add(new KeyValuePair<string, string>("hl.fragsize", Options.HighlightFragsize.ToString()));
                if ((Options.HighlightFields != null) && (Options.HighlightFields.Count > 0))
                    form.Add(new KeyValuePair<string, string>("hl.fl", String.Join(",", Options.HighlightFields)));
            }

            if ((Options.FacetFields != null) && (Options.FacetFields.Count > 0))
            {
                form.Add(new KeyValuePair<string, string>("facet", "true"));
                form.Add(new KeyValuePair<string, string>("facet.mincount", Options.FacetMinCount.ToString()));
                form.Add(new KeyValuePair<string, string>("facet.limit", Options.FacetLimit.ToString()));
                foreach (string facetField in Options.FacetFields)
                    form.Add(new KeyValuePair<string, string>("facet.field", facetField));
            }

            if ((Options.FacetQueries != null) && (Options.FacetQueries.Count > 0))
            {
                form.Add(new KeyValuePair<string, string>("facet", "true"));
                foreach (string facetQuery in Options.FacetQueries)
                    form.Add(new KeyValuePair<string, string>("facet.query", facetQuery));
            }

            if ((Options.GroupFields != null) && (Options.GroupFields.Count > 0))
            {
                form.Add(new KeyValuePair<string, string>("group", "true"));
                form.Add(new KeyValuePair<string, string>("group.format", "grouped"));
                form.Add(new KeyValuePair<string, string>("group.limit", Options.GroupLimit.ToString()));
                if (Options.GroupNgroups)
                    form.Add(new KeyValuePair<string, string>("group.ngroups", "true"));
                foreach (string groupField in Options.GroupFields)
                    form.Add(new KeyValuePair<string, string>("group.field", groupField));
            }

            if (Options.ExtraParams != null)
            {
                foreach (KeyValuePair<string, string> extraParam in Options.ExtraParams)
                    form.Add(extraParam);
            }

            string json = Post_For_String(CoreUrl + "/select", new FormUrlEncodedContent(form));
            return JsonSerializer.Deserialize<Solr_Query_Result<T>>(json, serializerOptions);
        }

        /// <summary> Add ( or update, by matching unique key ) one or more documents to a Solr core, using Solr's
        /// bare JSON-array add shorthand against the '/update' handler </summary>
        /// <typeparam name="T"> Document type being added, decorated with [JsonPropertyName] attributes matching the schema </typeparam>
        /// <param name="CoreUrl"> Base URL for the Solr core to update ( no trailing slash ) </param>
        /// <param name="Documents"> Documents to add or update </param>
        public static void AddOrUpdate<T>(string CoreUrl, IEnumerable<T> Documents)
        {
            string json = JsonSerializer.Serialize(Documents, serializerOptions);
            Post_For_String(CoreUrl + "/update?wt=json", new StringContent(json, Encoding.UTF8, "application/json"));
        }

        /// <summary> Delete a single document from a Solr core by its unique key value </summary>
        /// <param name="CoreUrl"> Base URL for the Solr core to update ( no trailing slash ) </param>
        /// <param name="Id"> Unique key value of the document to delete </param>
        public static void Delete_By_Id(string CoreUrl, string Id)
        {
            string json = "{\"delete\":{\"id\":" + JsonSerializer.Serialize(Id) + "}}";
            Post_For_String(CoreUrl + "/update?wt=json", new StringContent(json, Encoding.UTF8, "application/json"));
        }

        /// <summary> Delete every document from a Solr core which matches a Lucene query </summary>
        /// <param name="CoreUrl"> Base URL for the Solr core to update ( no trailing slash ) </param>
        /// <param name="Query"> Lucene query which selects the documents to delete </param>
        public static void Delete_By_Query(string CoreUrl, string Query)
        {
            string json = "{\"delete\":{\"query\":" + JsonSerializer.Serialize(Query) + "}}";
            Post_For_String(CoreUrl + "/update?wt=json", new StringContent(json, Encoding.UTF8, "application/json"));
        }

        /// <summary> Commit any pending adds/deletes on a Solr core, making them visible to searches </summary>
        /// <param name="CoreUrl"> Base URL for the Solr core to commit ( no trailing slash ) </param>
        public static void Commit(string CoreUrl)
        {
            Post_For_String(CoreUrl + "/update?commit=true", new StringContent("{}", Encoding.UTF8, "application/json"));
        }

        /// <summary> Optimize a Solr core's underlying index segments </summary>
        /// <param name="CoreUrl"> Base URL for the Solr core to optimize ( no trailing slash ) </param>
        public static void Optimize(string CoreUrl)
        {
            Post_For_String(CoreUrl + "/update?optimize=true", new StringContent("{}", Encoding.UTF8, "application/json"));
        }

        private static string Post_For_String(string Url, HttpContent Content)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, Url) { Content = Content };
            Apply_Basic_Auth(request);

            // Call sites are synchronous static methods used throughout the search/indexing pipeline;
            // ASP.NET Core has no SynchronizationContext, so blocking on the async call here is safe
            // (no deadlock risk) and avoids rippling async/await through every caller.
            HttpResponseMessage response = httpClient.SendAsync(request).GetAwaiter().GetResult();
            string body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            if (!response.IsSuccessStatusCode)
                throw new ApplicationException("Solr request to '" + Url + "' failed with status " + (int)response.StatusCode + ": " + body);

            return body;
        }

        /// <summary> Attaches an HTTP Basic Authentication header, read fresh from settings on every call so a
        /// credential change via the admin Settings page takes effect without an app restart, but only when a
        /// username has actually been configured -- Solr instances with no authentication requirement are
        /// unaffected, since no Authorization header is sent at all in that case </summary>
        private static void Apply_Basic_Auth(HttpRequestMessage Request)
        {
            string username = Engine_ApplicationCache_Gateway.Settings?.Servers?.Solr_Username;
            if (String.IsNullOrEmpty(username))
                return;

            string password = Engine_ApplicationCache_Gateway.Settings.Servers.Solr_Password ?? String.Empty;
            string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(username + ":" + password));
            Request.Headers.Authorization = new AuthenticationHeaderValue("Basic", encoded);
        }
    }
}
