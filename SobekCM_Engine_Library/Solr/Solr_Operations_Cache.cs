#region Using directives

using Microsoft.Practices.ServiceLocation;
using SolrNet;
using System;

#endregion

namespace SobekCM.Engine_Library.Solr
{
    internal static class Solr_Operations_Cache<T> where T : new() 
    { 
        private static ISolrOperations<T> solrOperations;
        private static string solrUrl;

        public static ISolrOperations<T> GetSolrOperations(string SolrURL) 
        {
            try
            {
                if ((solrOperations == null) || (solrUrl != SolrURL))
                {
                    Startup.InitContainer();
                    Startup.Init<T>(SolrURL);
                    solrOperations = ServiceLocator.Current.GetInstance<ISolrOperations<T>>();
                    solrUrl = SolrURL;
                }
                return solrOperations;
            }
            catch ( Exception )
            {
                return null;
            }
        } 
    }
}
