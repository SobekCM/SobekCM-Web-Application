#region Using directives

using SobekCM.Engine_Library.Solr.v5;
using SobekCM.Resource_Object;

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
        /// <param name="Update_Pages"> Flag indicates whether to update the page index as well; FALSE when the page text
        /// cannot have changed, since the page index holds nothing else </param>
        /// <remarks> After a change that cannot affect the text, use <see cref="Update_Index_After_Metadata_Change"/>
        /// instead, which doesn't read the text files either </remarks>
        public static void Update_Index(string SolrDocumentUrl, string SolrPageUrl, SobekCM_Item Resource, bool Include_Text, bool Update_Pages = true)
        {
            solrController.Update_Index(SolrDocumentUrl, SolrPageUrl, Resource, Include_Text, Update_Pages);
        }

        /// <summary> Updates every field of a single digital resource in the document index, except its full text,
        /// without reading any of the resource's files </summary>
        /// <param name="SolrDocumentUrl"> URL for the solr/lucene core used for searching for a single document within the library </param>
        /// <param name="Resource"> Digital resource to update in the index </param>
        /// <remarks> Only safe once the document index stores every field; most callers want
        /// <see cref="Update_Index_After_Metadata_Change"/>, which checks the setting first </remarks>
        public static void Update_Index_Metadata_Only(string SolrDocumentUrl, SobekCM_Item Resource)
        {
            solrController.Update_Index_Metadata_Only(SolrDocumentUrl, Resource);
        }

        /// <summary> How <see cref="Update_Index_After_Metadata_Change"/> updated the index </summary>
        public enum Metadata_Reindex_Method : byte
        {
            /// <summary> Atomic update of the document index's metadata fields ( 'Solr Atomic Updates Enabled' is on ) </summary>
            Atomic_Update,

            /// <summary> Document re-added to the document index, reusing the full text already stored there </summary>
            Reused_Stored_Text,

            /// <summary> Whole resource indexed from its files, since it was not in the index yet </summary>
            Full_Reindex
        }

        /// <summary> Reindexes a single digital resource after a change which could not have changed its text ( such
        /// as its behaviors or aggregations ), without reading the resource's text files unless it isn't indexed yet </summary>
        /// <param name="SolrDocumentUrl"> URL for the solr/lucene core used for searching for a single document within the library </param>
        /// <param name="SolrPageUrl"> URL for the solr/lucene core used for searching within a single document for matching pages </param>
        /// <param name="Resource"> Digital resource to reindex </param>
        /// <param name="Atomic_Updates_Enabled"> Value of the instance's 'Solr Atomic Updates Enabled' setting </param>
        /// <returns> How the index was updated </returns>
        /// <remarks> Only the document index is updated, since the page index holds nothing but the page text.  With the
        /// 'Solr Atomic Updates Enabled' setting on, this is an atomic update.  Otherwise the full text stored in the
        /// document index is read back and the whole document re-added with it, which is safe whatever the schema
        /// stores.  Only a resource not in the index yet is indexed from its files, pages included </remarks>
        public static Metadata_Reindex_Method Update_Index_After_Metadata_Change(string SolrDocumentUrl, string SolrPageUrl, SobekCM_Item Resource, bool Atomic_Updates_Enabled)
        {
            if (Atomic_Updates_Enabled)
            {
                solrController.Update_Index_Metadata_Only(SolrDocumentUrl, Resource);
                return Metadata_Reindex_Method.Atomic_Update;
            }

            if (solrController.Update_Index_Using_Stored_Text(SolrDocumentUrl, Resource))
                return Metadata_Reindex_Method.Reused_Stored_Text;

            solrController.Update_Index(SolrDocumentUrl, SolrPageUrl, Resource, true);
            return Metadata_Reindex_Method.Full_Reindex;
        }

        /// <summary> Deletes an existing resource from both solr/lucene core indexes </summary>
        /// <param name="SolrDocumentUrl"> URL for the solr/lucene core used for searching for a single document within the library </param>
        /// <param name="SolrPageUrl"> URL for the solr/lucene core used for searching within a single document for matching pages </param>
        /// <param name="BibID"> Bibliographic identifier for the item to remove from the solr/lucene indexes </param>
        /// <param name="VID"> Volume identifier for the item to remove from the solr/lucene indexes </param>
        /// <returns> TRUE if successful, otherwise FALSE </returns>
        public static bool Delete_Resource_From_Index(string SolrDocumentUrl, string SolrPageUrl, string BibID, string VID)
        {
            return solrController.Delete_Resource_From_Index(SolrDocumentUrl, SolrPageUrl, BibID, VID);
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
