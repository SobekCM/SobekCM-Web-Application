#region Using directives

using System.Collections.Generic;

#endregion

namespace SobekCM.Engine_Library.Solr
{
    /// <summary> Single sort clause for a Solr query ( field name plus direction ) </summary>
    public class Solr_Sort_Clause
    {
        /// <summary> Constructor for a new instance of the Solr_Sort_Clause class </summary>
        /// <param name="Field"> Name of the Solr field to sort by </param>
        /// <param name="Descending"> Flag indicates if this should be sorted descending, rather than ascending </param>
        public Solr_Sort_Clause(string Field, bool Descending)
        {
            this.Field = Field;
            this.Descending = Descending;
        }

        /// <summary> Name of the Solr field to sort by </summary>
        public string Field { get; set; }

        /// <summary> Flag indicates if this should be sorted descending, rather than ascending </summary>
        public bool Descending { get; set; }
    }

    /// <summary> Flat collection of all the optional parameters which can accompany a Solr select ( query ) request,
    /// used by <see cref="Solr_Http_Client"/> in place of SolrNet's QueryOptions/HighlightingParameters/FacetParameters/
    /// GroupingParameters/SortOrder class hierarchy </summary>
    public class Solr_Query_Options
    {
        /// <summary> Number of rows ( results ) to return </summary>
        public int Rows { get; set; } = 10;

        /// <summary> Zero-based offset into the complete result set at which to start returning rows </summary>
        public int Start { get; set; }

        /// <summary> List of field names to return for each matching document ( maps to the 'fl' parameter ) </summary>
        public List<string> Fields { get; set; }

        /// <summary> List of sort clauses to apply, in priority order ( maps to the 'sort' parameter ) </summary>
        public List<Solr_Sort_Clause> Sort { get; set; }

        /// <summary> Flag indicates whether highlighting should be requested at all </summary>
        public bool Highlight { get; set; }

        /// <summary> List of field names to highlight ( maps to 'hl.fl' ) </summary>
        public List<string> HighlightFields { get; set; }

        /// <summary> Approximate size, in characters, of each highlighted snippet ( maps to 'hl.fragsize' ) </summary>
        public int HighlightFragsize { get; set; } = 255;

        /// <summary> List of field names to facet upon ( maps to repeated 'facet.field' parameters ) </summary>
        public List<string> FacetFields { get; set; }

        /// <summary> Minimum count a facet value must have to be returned ( maps to 'facet.mincount' ) </summary>
        public int FacetMinCount { get; set; } = 1;

        /// <summary> Maximum number of facet values to return per field, or -1 for unlimited ( maps to 'facet.limit' ) </summary>
        public int FacetLimit { get; set; } = 100;

        /// <summary> List of raw facet.query clauses ( maps to repeated 'facet.query' parameters ) </summary>
        /// <remarks> Use Solr's <c>{!key=...}</c> local-params syntax on each clause when the response needs to be keyed
        /// by something other than the literal query string </remarks>
        public List<string> FacetQueries { get; set; }

        /// <summary> List of field names to group ( collapse ) results by ( maps to repeated 'group.field' parameters ) </summary>
        public List<string> GroupFields { get; set; }

        /// <summary> Maximum number of documents to return within each group ( maps to 'group.limit' ) </summary>
        public int GroupLimit { get; set; } = 10;

        /// <summary> Flag indicates whether the total number of groups should be computed and returned ( maps to 'group.ngroups' ) </summary>
        public bool GroupNgroups { get; set; }

        /// <summary> Escape hatch for any additional raw Solr request parameters not otherwise exposed here </summary>
        public Dictionary<string, string> ExtraParams { get; set; }
    }
}
