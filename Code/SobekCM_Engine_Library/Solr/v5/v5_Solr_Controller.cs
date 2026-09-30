using SobekCM.Resource_Object;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;

namespace SobekCM.Engine_Library.Solr.v5
{
    /// <summary> New controller class is used for indexing documents within a SobekCM library or single item aggregation within a SobekCM library </summary>
    public class v5_Solr_Controller : iSolr_Controller
    {
        /// <summary> Indexes a single digital resource within a SobekCM library </summary>
        /// <param name="SolrDocumentUrl"> URL for the solr/lucene core used for searching for a single document within the library </param>
        /// <param name="SolrPageUrl"> URL for the solr/lucene core used for searching within a single document for matching pages </param>
        /// <param name="Resource"> Digital resource to index</param>
        /// <param name="Include_Text"> Flag indicates whether to look for and include full text </param>
        /// <param name="Update_Pages"> Flag indicates whether to update the page index as well; FALSE when the page text
        /// cannot have changed, since the page index holds nothing else </param>
        public void Update_Index(string SolrDocumentUrl, string SolrPageUrl, SobekCM_Item Resource, bool Include_Text, bool Update_Pages = true)
        {
            // Get rid of trailling '/' in solr document url
            SolrDocumentUrl = SolrDocumentUrl.Trim();
            if ((!String.IsNullOrEmpty(SolrDocumentUrl)) && (SolrDocumentUrl[SolrDocumentUrl.Length - 1] == '/'))
                SolrDocumentUrl = SolrDocumentUrl.Substring(0, SolrDocumentUrl.Length - 1);

            // Get rid of trailling '/' in solr page url
            SolrPageUrl = SolrPageUrl.Trim();
            if ((!String.IsNullOrEmpty(SolrPageUrl)) && (SolrPageUrl[SolrPageUrl.Length - 1] == '/'))
                SolrPageUrl = SolrPageUrl.Substring(0, SolrPageUrl.Length - 1);


            // Get the list of all items in this collection
            var index_files = new List<v5_SolrDocument>();
            var index_pages = new List<v5_SolrPage>();

            // Add this document to the list of documents to index
            var builder = new v5_SolrDocument_Builder();
            v5_SolrDocument solrDocument = builder.Build_Solr_Document(Resource, Resource.Source_Directory);
            index_files.Add(solrDocument);

            bool document_success = false;
            int document_attempts = 0;
            while (!document_success)
            {
                try
                {
                    Solr_Http_Client.AddOrUpdate(SolrDocumentUrl, index_files);
                    document_success = true;
                }
                catch (Exception)
                {
                    if (document_attempts > 5)
                    {
                        throw;
                    }
                    document_attempts++;
                    Console.WriteLine(@"ERROR {0}", document_attempts);
                    Thread.Sleep(document_attempts * 1000);
                }
            }

            // Add each page to be indexed
            foreach (v5_SolrDocument document in index_files)
            {
                index_pages.AddRange(document.Solr_Pages);
            }


            bool page_success = !Update_Pages;
            int page_attempts = 0;
            while (!page_success)
            {
                try
                {
                    Solr_Http_Client.AddOrUpdate(SolrPageUrl, index_pages);
                    page_success = true;
                }
                catch (Exception)
                {
                    if (page_attempts > 5)
                    {
                        throw;
                    }
                    page_attempts++;
                    Thread.Sleep(page_attempts * 1000);
                }
            }

            // Comit the changes to the solr/lucene index
            try
            {
                Solr_Http_Client.Commit(SolrDocumentUrl);
            }
            catch (Exception)
            {
                Thread.Sleep(10 * 60 * 1000);
            }

            if (Update_Pages)
            {
                try
                {
                    Solr_Http_Client.Commit(SolrPageUrl);
                }
                catch (Exception)
                {
                    Thread.Sleep(10 * 60 * 1000);
                }
            }
        }

        /// <summary> Updates every field of a single digital resource in the document index, except its full text,
        /// without reading any of the resource's files </summary>
        /// <param name="SolrDocumentUrl"> URL for the solr/lucene core used for searching for a single document within the library </param>
        /// <param name="Resource"> Digital resource to update in the index </param>
        /// <remarks> Uses a Solr atomic update, so the full text already in the index is kept.  The page index is not
        /// touched, since it only holds the page text.  Only safe once the document index stores every field ( see
        /// the 'Solr Atomic Updates Enabled' setting ), or unstored fields are lost </remarks>
        public void Update_Index_Metadata_Only(string SolrDocumentUrl, SobekCM_Item Resource)
        {
            // Get rid of trailling '/' in solr document url
            SolrDocumentUrl = SolrDocumentUrl.Trim();
            if ((!String.IsNullOrEmpty(SolrDocumentUrl)) && (SolrDocumentUrl[SolrDocumentUrl.Length - 1] == '/'))
                SolrDocumentUrl = SolrDocumentUrl.Substring(0, SolrDocumentUrl.Length - 1);

            // Build the document, without reading the text files
            var builder = new v5_SolrDocument_Builder();
            v5_SolrDocument solrDocument = builder.Build_Solr_Document(Resource, Resource.Source_Directory, false);

            // Replace everything except the full text, then commit so searches see it right away
            Solr_Http_Client.Atomic_Update(SolrDocumentUrl, solrDocument, "did", new[] { "fulltext" });
            Solr_Http_Client.Commit(SolrDocumentUrl);
        }

        /// <summary> Re-adds a single digital resource to the document index, reusing the full text already stored
        /// there, so none of the resource's files are read </summary>
        /// <param name="SolrDocumentUrl"> URL for the solr/lucene core used for searching for a single document within the library </param>
        /// <param name="Resource"> Digital resource to update in the index </param>
        /// <returns> TRUE if updated, or FALSE if the resource is not in the index yet, so there was no text to reuse </returns>
        /// <remarks> This is a full re-add, not an atomic update, so it is safe whatever the schema stores.  The page
        /// index is not touched, since it only holds the page text </remarks>
        public bool Update_Index_Using_Stored_Text(string SolrDocumentUrl, SobekCM_Item Resource)
        {
            // Get rid of trailling '/' in solr document url
            SolrDocumentUrl = SolrDocumentUrl.Trim();
            if ((!String.IsNullOrEmpty(SolrDocumentUrl)) && (SolrDocumentUrl[SolrDocumentUrl.Length - 1] == '/'))
                SolrDocumentUrl = SolrDocumentUrl.Substring(0, SolrDocumentUrl.Length - 1);

            // Pull the full text currently in the index
            string did = Resource.BibID + ":" + Resource.VID;
            var options = new Solr_Query_Options { Rows = 1, Fields = new List<string> { "did", "fulltext" } };
            Solr_Query_Result<Stored_Text_Document> existing = Solr_Http_Client.Select<Stored_Text_Document>(SolrDocumentUrl, "did:\"" + did + "\"", options);
            if ((existing?.Response?.Docs == null) || (existing.Response.Docs.Count == 0))
                return false;

            // Build the document, without reading the text files, and use the stored text instead
            var builder = new v5_SolrDocument_Builder();
            v5_SolrDocument solrDocument = builder.Build_Solr_Document(Resource, Resource.Source_Directory, false);
            solrDocument.Stored_FullText = existing.Response.Docs[0].FullText;

            Solr_Http_Client.AddOrUpdate(SolrDocumentUrl, new List<v5_SolrDocument> { solrDocument });
            Solr_Http_Client.Commit(SolrDocumentUrl);
            return true;
        }

        /// <summary> Just the full text of a document, as read back from the index </summary>
        private class Stored_Text_Document
        {
            [JsonPropertyName("fulltext")]
            public string FullText { get; set; }
        }

        /// <summary> Deletes an existing resource from both solr/lucene core indexes </summary>
        /// <param name="SolrDocumentUrl"> URL for the solr/lucene core used for searching for a single document within the library </param>
        /// <param name="SolrPageUrl"> URL for the solr/lucene core used for searching within a single document for matching pages </param>
        /// <param name="BibID"> Bibliographic identifier for the item to remove from the solr/lucene indexes </param>
        /// <param name="VID"> Volume identifier for the item to remove from the solr/lucene indexes </param>
        /// <returns> TRUE if successful, otherwise FALSE </returns>
        public bool Delete_Resource_From_Index(string SolrDocumentUrl, string SolrPageUrl, string BibID, string VID)
        {
            try
            {

                // Get rid of trailling '/' in solr document url
                SolrDocumentUrl = SolrDocumentUrl.Trim();
                if ((!String.IsNullOrEmpty(SolrDocumentUrl)) && (SolrDocumentUrl[SolrDocumentUrl.Length - 1] == '/'))
                    SolrDocumentUrl = SolrDocumentUrl.Substring(0, SolrDocumentUrl.Length - 1);

                // Get rid of trailling '/' in solr page url
                SolrPageUrl = SolrPageUrl.Trim();
                if ((!String.IsNullOrEmpty(SolrPageUrl)) && (SolrPageUrl[SolrPageUrl.Length - 1] == '/'))
                    SolrPageUrl = SolrPageUrl.Substring(0, SolrPageUrl.Length - 1);

                // For the object, we can use the unique identifier
                Solr_Http_Client.Delete_By_Id(SolrDocumentUrl, BibID + ":" + VID);

                // For the pages, we need to search by id
                Solr_Http_Client.Delete_By_Query(SolrPageUrl, "did:\"" + BibID + ":" + VID + "\"");

                // Comit the changes to the solr/lucene index
                try
                {
                    Solr_Http_Client.Commit(SolrDocumentUrl);
                }
                catch
                {
                    Thread.Sleep(10 * 60 * 1000);
                }

                try
                {
                    Solr_Http_Client.Commit(SolrPageUrl);
                }
                catch
                {
                    Thread.Sleep(10 * 60 * 1000);
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }



        /// <summary> Optimize the solr/lucene core used for searching for a single document </summary>
        /// <param name="SolrDocumentUrl"> URL for the solr/lucene core used for searching for a single document within the library </param>
        public void Optimize_Document_Index(string SolrDocumentUrl)
        {
            // Get rid of trailling '/' in solr document url
            SolrDocumentUrl = SolrDocumentUrl.Trim();
            if ((!String.IsNullOrEmpty(SolrDocumentUrl)) && (SolrDocumentUrl[SolrDocumentUrl.Length - 1] == '/'))
                SolrDocumentUrl = SolrDocumentUrl.Substring(0, SolrDocumentUrl.Length - 1);

            try
            {
                Solr_Http_Client.Optimize(SolrDocumentUrl);
            }
            catch (Exception)
            {
                // Do not do anything here.  It may throw an exception when it runs very longs
            }
        }

        /// <summary> Optimize the solr/lucene core used for searching within a single document </summary>
        /// <param name="SolrPageUrl"> URL for the solr/lucene core used for searching within a single document for matching pages </param>
        public void Optimize_Page_Index(string SolrPageUrl)
        {
            // Get rid of trailling '/' in solr page url
            SolrPageUrl = SolrPageUrl.Trim();
            if ((!String.IsNullOrEmpty(SolrPageUrl)) && (SolrPageUrl[SolrPageUrl.Length - 1] == '/'))
                SolrPageUrl = SolrPageUrl.Substring(0, SolrPageUrl.Length - 1);

            try
            {
                Solr_Http_Client.Optimize(SolrPageUrl);
            }
            catch (Exception)
            {
                // Do not do anything here.  It may throw an exception when it runs very longs
            }
        }
    }
}
