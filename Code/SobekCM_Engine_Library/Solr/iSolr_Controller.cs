using SobekCM.Resource_Object;

namespace SobekCM.Engine_Library.Solr
{
    /// <summary> Interface for the solr controller </summary>
    public interface iSolr_Controller
    {
        /// <summary> Indexes a single digital resource within a SobekCM library </summary>
        /// <param name="SolrDocumentUrl"> URL for the solr/lucene core used for searching for a single document within the library </param>
        /// <param name="SolrPageUrl"> URL for the solr/lucene core used for searching within a single document for matching pages </param>
        /// <param name="Resource"> Digital resource to index</param>
        /// <param name="Include_Text"> Flag indicates whether to look for and include full text </param>
        /// <param name="Update_Pages"> Flag indicates whether to update the page index as well; FALSE when the page text
        /// cannot have changed, since the page index holds nothing else </param>
        void Update_Index(string SolrDocumentUrl, string SolrPageUrl, SobekCM_Item Resource, bool Include_Text, bool Update_Pages = true);

        /// <summary> Updates every field of a single digital resource in the document index, except its full text,
        /// without reading any of the resource's files </summary>
        /// <param name="SolrDocumentUrl"> URL for the solr/lucene core used for searching for a single document within the library </param>
        /// <param name="Resource"> Digital resource to update in the index </param>
        void Update_Index_Metadata_Only(string SolrDocumentUrl, SobekCM_Item Resource);

        /// <summary> Re-adds a single digital resource to the document index, reusing the full text already stored
        /// there, so none of the resource's files are read </summary>
        /// <param name="SolrDocumentUrl"> URL for the solr/lucene core used for searching for a single document within the library </param>
        /// <param name="Resource"> Digital resource to update in the index </param>
        /// <returns> TRUE if updated, or FALSE if the resource is not in the index yet </returns>
        bool Update_Index_Using_Stored_Text(string SolrDocumentUrl, SobekCM_Item Resource);

        /// <summary> Deletes an existing resource from both solr/lucene core indexes </summary>
        /// <param name="SolrDocumentUrl"> URL for the solr/lucene core used for searching for a single document within the library </param>
        /// <param name="SolrPageUrl"> URL for the solr/lucene core used for searching within a single document for matching pages </param>
        /// <param name="BibID"> Bibliographic identifier for the item to remove from the solr/lucene indexes </param>
        /// <param name="VID"> Volume identifier for the item to remove from the solr/lucene indexes </param>
        /// <returns> TRUE if successful, otherwise FALSE </returns>
        bool Delete_Resource_From_Index(string SolrDocumentUrl, string SolrPageUrl, string BibID, string VID);

        /// <summary> Optimize the solr/lucene core used for searching for a single document </summary>
        /// <param name="SolrDocumentUrl"> URL for the solr/lucene core used for searching for a single document within the library </param>
        void Optimize_Document_Index(string SolrDocumentUrl);

        /// <summary> Optimize the solr/lucene core used for searching within a single document </summary>
        /// <param name="SolrPageUrl"> URL for the solr/lucene core used for searching within a single document for matching pages </param>
        void Optimize_Page_Index(string SolrPageUrl);

    }
}
