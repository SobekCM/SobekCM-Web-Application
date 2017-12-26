#region Using directives

using System;
using SobekCM.Engine_Library.Solr.Legacy;
using SobekCM.Engine_Library.Solr.v5;
using SobekCM.Resource_Object;
using SobekCM.Resource_Object.Solr;

#endregion

namespace SobekCM.Engine_Library.Solr
{
    /// <summary> Controller class is used for indexing documents within a SobekCM library or single item aggregation within a SobekCM library </summary>
    public class Solr_Controller
    {
        private static iSolr_Controller solrController;

        /// <summary> Static constructor for the <see cref="Solr_Controller"/> class </summary>
        static Solr_Controller()
        {
            solrController = new v5_Solr_Controller();
        }


        /// <summary> Indexes a single digital resource within a SobekCM library </summary>
        /// <param name="SolrDocumentUrl"> URL for the solr/lucene core used for searching for a single document within the library </param>
        /// <param name="SolrPageUrl"> URL for the solr/lucene core used for searching within a single document for matching pages </param>
        /// <param name="Resource"> Digital resource to index</param>
        /// <param name="Include_Text"> Flag indicates whether to look for and include full text </param>
        public static void Update_Index(string SolrDocumentUrl, string SolrPageUrl, SobekCM_Item Resource, bool Include_Text )
        {
            solrController.Update_Index(SolrDocumentUrl, SolrPageUrl, Resource, Include_Text );
        }

        /// <summary> Deletes an existing resource from both solr/lucene core indexes </summary>
        /// <param name="SolrDocumentUrl"> URL for the solr/lucene core used for searching for a single document within the library </param>
        /// <param name="SolrPageUrl"> URL for the solr/lucene core used for searching within a single document for matching pages </param>
        /// <param name="BibID"> Bibliographic identifier for the item to remove from the solr/lucene indexes </param>
        /// <param name="VID"> Volume identifier for the item to remove from the solr/lucene indexes </param>
        /// <returns> TRUE if successful, otherwise FALSE </returns>
        public static bool Delete_Resource_From_Index(string SolrDocumentUrl, string SolrPageUrl, string BibID, string VID)
        {
            return solrController.Delete_Resource_From_Index(SolrDocumentUrl, SolrPageUrl, BibID, VID );
        }


        /// <summary> Optimize the solr/lucene core used for searching for a single document </summary>
        /// <param name="SolrDocumentUrl"> URL for the solr/lucene core used for searching for a single document within the library </param>
        public static void Optimize_Document_Index(string SolrDocumentUrl)
        {
            solrController.Optimize_Document_Index(SolrDocumentUrl);
        }

        /// <summary> Optimize the solr/lucene core used for searching within a single document </summary>
        /// <param name="SolrPageUrl"> URL for the solr/lucene core used for searching within a single document for matching pages </param>
        public static void Optimize_Page_Index(string SolrPageUrl)
        {
            solrController.Optimize_Page_Index(SolrPageUrl);
        }
    }
}
