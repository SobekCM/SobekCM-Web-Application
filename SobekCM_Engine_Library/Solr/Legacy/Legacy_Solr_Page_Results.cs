#region Using directives

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using SobekCM.Engine_Library.ApplicationState;
using SolrNet;
using SolrNet.Commands.Parameters;

#endregion

namespace SobekCM.Engine_Library.Solr.Legacy
{
    /// <summary> Stores a group of page results from an in-document search against a Solr full text index </summary>
    [Serializable]
    public class Legacy_Solr_Page_Results
    {
        private readonly List<Legacy_Solr_Page_Result> results;

        /// <summary> Constructor for a new instance of the Solr_Page_Results class </summary>
        public Legacy_Solr_Page_Results()
        {
            results = new List<Legacy_Solr_Page_Result>();
            Page_Number = -1;
            TotalResults = 0;
            QueryTime = -1;
            Sort_By_Score = true;
        }

        /// <summary> Gets the collection of single page search results associated with this search </summary>
        public ReadOnlyCollection<Legacy_Solr_Page_Result> Results
        {
            get
            {
                return new ReadOnlyCollection<Legacy_Solr_Page_Result>(results);
            }
        }

        /// <summary> Time, in millseconds, required for this query on the Solr search engine </summary>
        public int QueryTime { get; internal set; }

        /// <summary> Number of total results in the complete result set </summary>
        public int TotalResults { get; internal set; }

        /// <summary> Actual query string conducted by the SobekCM user and then processed against the Solr search engine </summary>
        public string Query { get; internal set; }

        /// <summary> Flag indicates if these search results were sorted by score, rather than the default sorting by page order </summary>
        public bool Sort_By_Score { get; internal set; }

        /// <summary> Page number of these results within the complete results </summary>
        /// <remarks> The Solr/Lucene searches only return a "page" of results at a time.  The page number is a one-based indexed, with the first page being number 1 (not zero).</remarks>
        public int Page_Number { get; internal set; }

        /// <summary> Add the next single page result from an in-document search against a Solr full-text index </summary>
        /// <param name="Result"></param>
        internal void Add_Result(Legacy_Solr_Page_Result Result)
        {
            results.Add(Result);
        }
    }
}
