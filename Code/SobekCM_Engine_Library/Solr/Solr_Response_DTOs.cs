#region Using directives

using System.Collections.Generic;
using System.Text.Json.Serialization;

#endregion

namespace SobekCM.Engine_Library.Solr
{
    /// <summary> Deserialization target for Solr's 'responseHeader' block </summary>
    public class Solr_Response_Header
    {
        /// <summary> Time, in milliseconds, the query took to execute on the Solr server </summary>
        [JsonPropertyName("QTime")]
        public int QTime { get; set; }
    }

    /// <summary> Deserialization target for a Solr 'response' ( or grouped 'doclist' ) block, containing the
    /// matching documents themselves </summary>
    /// <typeparam name="T"> Document type this result body contains ( e.g. <see cref="v5.v5_SolrDocument"/> ) </typeparam>
    public class Solr_Response_Body<T>
    {
        /// <summary> Total number of documents which matched the query, across all pages of results </summary>
        [JsonPropertyName("numFound")]
        public long NumFound { get; set; }

        /// <summary> Zero-based offset into the complete result set at which this page of results starts </summary>
        [JsonPropertyName("start")]
        public int Start { get; set; }

        /// <summary> The matching documents included in this page of results </summary>
        [JsonPropertyName("docs")]
        public List<T> Docs { get; set; }
    }

    /// <summary> A single group of documents within a grouped ( collapsed ) Solr search result </summary>
    /// <typeparam name="T"> Document type contained within this group </typeparam>
    public class Solr_Group<T>
    {
        /// <summary> Value of the group-by field shared by every document in this group </summary>
        [JsonPropertyName("groupValue")]
        public string GroupValue { get; set; }

        /// <summary> The documents within this single group ( up to the requested group.limit ) </summary>
        [JsonPropertyName("doclist")]
        public Solr_Response_Body<T> Doclist { get; set; }
    }

    /// <summary> Grouped results for a single group.field, within the overall 'grouped' response block </summary>
    /// <typeparam name="T"> Document type contained within each group </typeparam>
    public class Solr_Group_Field_Result<T>
    {
        /// <summary> Total number of documents ( across all groups ) which matched the query </summary>
        [JsonPropertyName("matches")]
        public int Matches { get; set; }

        /// <summary> Total number of distinct groups which matched the query ( only present when group.ngroups=true ) </summary>
        [JsonPropertyName("ngroups")]
        public int? Ngroups { get; set; }

        /// <summary> The individual groups returned for this field </summary>
        [JsonPropertyName("groups")]
        public List<Solr_Group<T>> Groups { get; set; }
    }

    /// <summary> Top-level result of a Solr '/select' query, deserialized directly from the native JSON response
    /// ( <c>wt=json</c> ), replacing SolrNet's SolrQueryResults&lt;T&gt; </summary>
    /// <typeparam name="T"> Document type returned by this query ( e.g. <see cref="v5.v5_SolrDocument"/>,
    /// <see cref="v5.v5_Solr_Page_Result"/> ) </typeparam>
    public class Solr_Query_Result<T>
    {
        /// <summary> Response header, including the query execution time </summary>
        [JsonPropertyName("responseHeader")]
        public Solr_Response_Header ResponseHeader { get; set; }

        /// <summary> The matching documents, when this query was not grouped </summary>
        [JsonPropertyName("response")]
        public Solr_Response_Body<T> Response { get; set; }

        /// <summary> Facet field counts, keyed by field name, then by facet value ( requires the request to include
        /// 'json.nl=map' so this arrives as a map rather than Solr's default flat alternating array ) </summary>
        [JsonPropertyName("facet_counts")]
        public Solr_Facet_Counts FacetCounts { get; set; }

        /// <summary> Highlighted snippets, keyed by each document's unique-key value, then by field name </summary>
        [JsonPropertyName("highlighting")]
        public Dictionary<string, Dictionary<string, List<string>>> Highlighting { get; set; }

        /// <summary> Grouped ( collapsed ) results, keyed by group.field name, when this query requested grouping </summary>
        [JsonPropertyName("grouped")]
        public Dictionary<string, Solr_Group_Field_Result<T>> Grouped { get; set; }
    }

    /// <summary> Deserialization target for Solr's 'facet_counts' response block </summary>
    public class Solr_Facet_Counts
    {
        /// <summary> Facet field counts, keyed by field name then facet value ( requires 'json.nl=map' ) </summary>
        [JsonPropertyName("facet_fields")]
        public Dictionary<string, Dictionary<string, int>> FacetFields { get; set; }

        /// <summary> Facet query counts, keyed by the facet.query clause itself, or by its <c>{!key=...}</c>
        /// local-params key when one was supplied </summary>
        [JsonPropertyName("facet_queries")]
        public Dictionary<string, int> FacetQueries { get; set; }
    }
}
