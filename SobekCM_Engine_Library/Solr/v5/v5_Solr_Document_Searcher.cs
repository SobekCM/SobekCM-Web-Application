using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SobekCM.Core.Results;
using SobekCM.Core.Search;
using SobekCM.Core.Settings;
using SobekCM.Engine_Library.ApplicationState;
using SobekCM.Engine_Library.Solr.Legacy;
using SobekCM.Tools;
using SolrNet;
using SolrNet.Commands.Parameters;

namespace SobekCM.Engine_Library.Solr.v5
{
    public class v5_Solr_Document_Searcher
    {
        #region Static method to query the Solr/Lucene for a collection of results

        /// <summary> Perform an search for documents with matching parameters </summary>
        /// <param name="AggregationCode"> Aggregation code within which to search </param>
        /// <param name="Terms"> List of the search terms </param>
        /// <param name="Web_Fields"> List of the web fields associate with the search terms </param>        /// <param name="ResultsPerPage"> Number of results to display per a "page" of results </param>
        /// <param name="Page_Number"> Which page of results to return ( one-based, so the first page is page number of one )</param>
        /// <param name="Sort"> Sort to apply before returning the results of the search </param>
        /// <param name="Need_Search_Statistics"> Flag indicates if the search statistics are needed </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <param name="Complete_Result_Set_Info"> [OUT] Information about the entire set of results </param>
        /// <param name="Paged_Results"> [OUT] List of search results for the requested page of results </param>
        /// <returns> Page search result object with all relevant result information </returns>
        public static bool Search(string AggregationCode, List<string> Terms, List<string> Web_Fields, int ResultsPerPage, int Page_Number, ushort Sort, bool Need_Search_Statistics, Custom_Tracer Tracer, out Search_Results_Statistics Complete_Result_Set_Info, out List<iSearch_Title_Result> Paged_Results)
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("v5_Solr_Documents_Searcher.Search", "Build the Solr query");
            }

            // Step through all the terms and fields
            StringBuilder queryStringBuilder = new StringBuilder();
            for (int i = 0; i < Math.Min(Terms.Count, Web_Fields.Count); i++)
            {
                string web_field = Web_Fields[i];
                string searchTerm = Terms[i];
                string solr_field;

                if (i == 0)
                {
                    // Skip any joiner for the very first field indicated
                    if ((web_field[0] == '+') || (web_field[0] == '=') || (web_field[0] == '-'))
                    {
                        web_field = web_field.Substring(1);
                    }

                    // Try to get the solr field
                    if (web_field == "TX")
                    {
                        solr_field = "fulltext:";
                    }
                    else
                    {
                        Metadata_Search_Field field = Engine_ApplicationCache_Gateway.Settings.Metadata_Search_Field_By_Code(web_field.ToUpper());
                        if (field != null)
                        {
                            solr_field = field.Solr_Field + ":";
                        }
                        else
                        {
                            solr_field = String.Empty;
                        }
                    }

                    // Add the solr search string
                    if (searchTerm.IndexOf(" ") > 0)
                    {
                        queryStringBuilder.Append("(" + solr_field + "\"" + searchTerm.Replace(":", "") + "\")");
                    }
                    else
                    {
                        queryStringBuilder.Append("(" + solr_field + searchTerm.Replace(":", "") + ")");
                    }
                }
                else
                {
                    // Add the joiner for this subsequent terms
                    if ((web_field[0] == '+') || (web_field[0] == '=') || (web_field[0] == '-'))
                    {
                        switch (web_field[0])
                        {
                            case '=':
                                queryStringBuilder.Append(" OR ");
                                break;

                            case '+':
                                queryStringBuilder.Append(" AND ");
                                break;

                            case '-':
                                queryStringBuilder.Append(" NOT ");
                                break;

                            default:
                                queryStringBuilder.Append(" AND ");
                                break;
                        }
                        web_field = web_field.Substring(1);
                    }
                    else
                    {
                        queryStringBuilder.Append(" AND ");
                    }

                    // Try to get the solr field
                    if (web_field == "TX")
                    {
                        solr_field = "fulltext:";
                    }
                    else
                    {
                        Metadata_Search_Field field = Engine_ApplicationCache_Gateway.Settings.Metadata_Search_Field_By_Code(web_field.ToUpper());
                        if (field != null)
                        {
                            solr_field = field.Solr_Field + ":";
                        }
                        else
                        {
                            solr_field = String.Empty;
                        }
                    }

                    // Add the solr search string
                    if (searchTerm.IndexOf(" ") > 0)
                    {
                        queryStringBuilder.Append("(" + solr_field + "\"" + searchTerm.Replace(":", "") + "\")");
                    }
                    else
                    {
                        queryStringBuilder.Append("(" + solr_field + searchTerm.Replace(":", "") + ")");
                    }
                }
            }

            if (Tracer != null)
            {
                Tracer.Add_Trace("v5_Solr_Documents_Searcher.Search", "Perform the search");
            }

            // Get the query string value
            string queryString = queryStringBuilder.ToString();

            // Set output initially to null
            Paged_Results = new List<iSearch_Title_Result>();
            Complete_Result_Set_Info = null;

            try
            {
                // Ensure page is not erroneously set to zero or negative
                if (Page_Number <= 0)
                    Page_Number = 1;

                // Get and clean the solr document url
                string solrDocumentUrl = Engine_ApplicationCache_Gateway.Settings.Servers.Document_Solr_Index_URL;
                if ((!String.IsNullOrEmpty(solrDocumentUrl)) && (solrDocumentUrl[solrDocumentUrl.Length - 1] == '/'))
                    solrDocumentUrl = solrDocumentUrl.Substring(0, solrDocumentUrl.Length - 1);

                // Create the solr worker to query the document index
                var solrWorker = Solr_Operations_Cache<v5_SolrDocument>.GetSolrOperations(solrDocumentUrl);

                // Create the query options
                QueryOptions options = new QueryOptions
                {
                    Rows = ResultsPerPage,
                    Start = (Page_Number - 1)*ResultsPerPage,
                    Fields = new[] {"did", "mainthumb", "title", "donor", "edition", "format", "holding", "source", "type", "creator.display", "publisher.display", "genre", "subject"} //,
//                    Highlight = new HighlightingParameters { Fields = new[] { "fulltext" }, },
                    //                  ExtraParams = new Dictionary<string, string> { { "hl.useFastVectorHighlighter", "true" } }
                };

                // If the search stats are needed, let's get the facets
                List<Metadata_Search_Field> facets = new List<Metadata_Search_Field>();
                if (Need_Search_Statistics)
                {
                    // Hard-coded for now
                    facets.Add(new Metadata_Search_Field(3, "Language", "Language", "LA", "language", "Language", "language_facets", String.Empty, String.Empty));
                    facets.Add(new Metadata_Search_Field(4, "Creator", "Creator", "AU", "creator", "Creator", "creator_facets", String.Empty, String.Empty));
                    facets.Add(new Metadata_Search_Field(5, "Publisher", "Publisher", "PU", "publisher", "Publisher", "publisher_facets", String.Empty, String.Empty));
                    facets.Add(new Metadata_Search_Field(8, "Genre", "Genre", "GE", "genre", "Genre", "genre_facets", String.Empty, String.Empty));
                    facets.Add(new Metadata_Search_Field(7, "Subject: Topic", "Subject Keyword", "TO", "subject", "Subject Keyword", "subject_facets", String.Empty, String.Empty));
                    facets.Add(new Metadata_Search_Field(10, "Subject: Geographic Area", "Spatial Coverage", "SP", "spatial_standard", "Spatial Coverage", "spatial_standard_facets", String.Empty, String.Empty));

                    // Create the query facters
                    options.Facet = new FacetParameters();
                    foreach (Metadata_Search_Field facet in facets)
                    {
                        options.Facet.Queries.Add(new SolrFacetFieldQuery(facet.Solr_Facet_Code) {MinCount = 1});
                    }
                }

                //// Set the sort value
                //if (Sort != 0)
                //{
                //    options.OrderBy.Clear();
                //    switch (Sort)
                //    {
                //        case 1:
                //            options.OrderBy.Add(new SortOrder("title"));
                //            break;

                //        case 2:
                //            options.OrderBy.Add(new SortOrder("bibid", Order.ASC));
                //            break;

                //        case 3:
                //            options.OrderBy.Add(new SortOrder("bibid", Order.DESC));
                //            break;

                //        case 10:
                //            options.OrderBy.Add(new SortOrder("pubdate", Order.ASC));
                //            break;

                //        case 11:
                //            options.OrderBy.Add(new SortOrder("pubdate", Order.DESC));
                //            break;

                //    }
                //}

                // If there was an aggregation code included, put that at the beginning of the search
                if ((AggregationCode.Length > 0) && (AggregationCode.ToUpper() != "ALL"))
                {
                    queryString = "(aggregations:" + AggregationCode.ToUpper() + ")AND(" + queryString + ")";
                }

                // We need to instruct SOLR to return the results as XML for solr to parse it
                options.ExtraParams = new KeyValuePair<string, string>[] { new KeyValuePair<string,string>("wt", "xml") };

                // Perform this search
                SolrQueryResults<v5_SolrDocument> results = solrWorker.Query(queryString, options);

                // Create the search statistcs
                List<string> metadataLabels = new List<string> { "Creator", "Publisher", "Type", "Format", "Edition", "Institution", "Holding Location", "Donor", "Genre", "Subject" };

                Complete_Result_Set_Info = new Search_Results_Statistics(metadataLabels)
                {
                    Total_Titles = results.NumFound,
                    Total_Items = results.NumFound,
                    QueryTime = results.Header.QTime
                };

                // If the search stats were needed, get the facets out
                if (Need_Search_Statistics)
                {
                    // Copy over all the facets
                    foreach (Metadata_Search_Field facetTerm in facets)
                    {
                        // Create the collection and and assifn the metadata type id
                        Search_Facet_Collection thisCollection = new Search_Facet_Collection(facetTerm.ID);

                        // Add each value
                        foreach (var facet in results.FacetFields[facetTerm.Solr_Facet_Code])
                        {
                            thisCollection.Facets.Add(new Search_Facet(facet.Key, facet.Value));
                        }

                        // If there was an id and facets added, save this to the search statistics
                        if ((thisCollection.MetadataTypeID > 0) && (thisCollection.Facets.Count > 0))
                        {
                            Complete_Result_Set_Info.Facet_Collections.Add(thisCollection);
                        }
                    }
                }

                // Pass all the results into the List and add the highlighted text to each result as well
                foreach (v5_SolrDocument thisResult in results)
                {
                    // Create the results
                    Legacy_Solr_Document_Result resultConverted = new Legacy_Solr_Document_Result();
                    resultConverted.DID = thisResult.DID;
                    resultConverted.Title = thisResult.Title ?? "NO TITLE";
                    resultConverted.HoldingLocation = thisResult.Holding;
                    resultConverted.SourceInstitution = thisResult.Source;
                    resultConverted.MaterialType = thisResult.Type;
                    resultConverted.MainThumbnail = thisResult.MainThumbnail;

                    resultConverted.Metadata_Display_Values = new string[]
                    {
                        collection_to_string(thisResult.Creator_Display),
                        collection_to_string(thisResult.Publisher_Display),
                        (thisResult.Type ?? String.Empty ),
                        (thisResult.Format ?? String.Empty),
                        (thisResult.Edition ?? String.Empty),
                        (thisResult.Source ?? String.Empty),
                        (thisResult.Holding ?? String.Empty),
                        (thisResult.Donor ?? String.Empty),
                        collection_to_string(thisResult.Genre),
                        collection_to_string(thisResult.Subject)
                    };

                    //// Add the highlight snipper
                    //if ((results.Highlights.ContainsKey(thisResult.DID)) && (results.Highlights[thisResult.DID].Count > 0) && (results.Highlights[thisResult.DID].ElementAt(0).Value.Count > 0))
                    //{
                    //    thisResult.Snippet = results.Highlights[thisResult.DID].ElementAt(0).Value.ElementAt(0);
                    //}

                    // Add this results as is for now
                    Paged_Results.Add(resultConverted);
                }



                return true;
            }
            catch ( Exception ee )
            {
                return false;
            }
        }

        #endregion

        private static string collection_to_string(List<string> Values)
        {
            if ((Values == null) || ( Values.Count == 0 )) return String.Empty;

            if (Values.Count == 1) return Values[0];

            if (Values.Count == 2) return Values[0] + " | " + Values[1];

            if (Values.Count == 3) return Values[0] + " | " + Values[1] + " | " + Values[2];

            if (Values.Count == 4) return Values[0] + " | " + Values[1] + " | " + Values[2] + " | " + Values[3];

            return Values[0] + " | " + Values[1] + " | " + Values[2] + " | " + Values[3] + " | " + Values[4];
        }

        #region Method to split the complex search term string into a collection of search terms and fields

        /// <summary> Method splits the search string and field string into seperate collections of strings </summary>
        /// <param name="TermString"> String containing all of the search terms</param>
        /// <param name="Field"> String containing all of the search field codes</param>
        /// <param name="TermsBuilder"> Collection of seperate search terms</param>
        /// <param name="FieldsBuilder"> Collection of seperate saerch field codes</param>
        /// <remarks> This code is here to handle quotation marks, so quoted terms are not erroneously split </remarks>
        public static void Split_Multi_Terms(string TermString, string Field, List<string> TermsBuilder, List<string> FieldsBuilder)
        {
            string termsStr = TermString + " ";
            int first_term = TermsBuilder.Count;
            //		int current_index = 0;


            int last_index = 0;
            int quote_index = termsStr.IndexOf("\"", last_index, StringComparison.Ordinal);
            int space_index = termsStr.IndexOf(" ", last_index, StringComparison.Ordinal);
            while ((last_index < termsStr.Length) && ((quote_index >= 0) || (space_index >= 0)))
            {
                // If there is a quote, and it is before the space, find the end quote
                string thisTerm;
                if ((quote_index >= 0) && (quote_index < space_index))
                {
                    last_index = quote_index;
                    quote_index = termsStr.IndexOf("\"", last_index + 1, StringComparison.Ordinal);
                    if (quote_index < 0)
                        quote_index = termsStr.Length - 1;
                    thisTerm = termsStr.Substring(last_index, quote_index - last_index + 1).Trim().Replace(" ", "+");
                    last_index = quote_index + 1;
                    if (thisTerm.Length > 0)
                    {
                        TermsBuilder.Add(thisTerm);
                    }
                }
                else
                {
                    // Divide at the space then
                    // Was there a first term to include?
                    thisTerm = termsStr.Substring(last_index, space_index - last_index).Trim();
                    last_index = space_index;
                    // Only add this if there is a character or number
                    bool isValid = thisTerm.Any(Char.IsLetterOrDigit);
                    if (isValid)
                    {
                        TermsBuilder.Add(thisTerm);
                    }
                }

                // Find the next spaces or quotes
                if (last_index < termsStr.Length)
                {
                    quote_index = termsStr.IndexOf("\"", last_index + 1, StringComparison.Ordinal);
                    space_index = termsStr.IndexOf(" ", last_index + 1, StringComparison.Ordinal);
                }
            }


            // If there was no fields provided, search anywhere
            if (Field.Trim().Length == 0)
            {
                Field = "+ZZ";
            }

            // Now, build the fields and string
            for (int i = first_term; i < TermsBuilder.Count; i++)
            {
                if ((Field[0] != '-') && (Field[0] != '=') && (Field[0] != '+'))
                {
                    FieldsBuilder.Add("+" + Field);
                }
                else
                {
                    FieldsBuilder.Add(Field);
                }

                // If this should have been NOT, utilize that
                if (TermsBuilder[i][0] == '-')
                {
                    TermsBuilder[i] = TermsBuilder[i].Substring(1);
                    if ((FieldsBuilder[i][0] == '-') || (FieldsBuilder[i][0] == '=') || (FieldsBuilder[i][0] == '+'))
                    {
                        switch (FieldsBuilder[i][0])
                        {
                            case '-':
                                FieldsBuilder[i] = "+" + FieldsBuilder[i].Substring(1);
                                break;
                            case '+':
                                FieldsBuilder[i] = "-" + FieldsBuilder[i].Substring(1);
                                break;
                            case '=':
                                FieldsBuilder[i] = "-" + FieldsBuilder[i].Substring(1);
                                break;
                        }
                    }
                    else
                    {
                        FieldsBuilder[i] = "-" + Field;
                    }
                }

                // If this should have been OR, utilize that
                if (TermsBuilder[i][0] == '=')
                {
                    TermsBuilder[i] = TermsBuilder[i].Substring(1);
                    if ((FieldsBuilder[i][0] == '-') || (FieldsBuilder[i][0] == '=') || (FieldsBuilder[i][0] == '+'))
                    {
                        switch (FieldsBuilder[i][0])
                        {
                            case '-':
                                FieldsBuilder[i] = "-" + FieldsBuilder[i].Substring(1);
                                break;
                            case '+':
                                FieldsBuilder[i] = "=" + FieldsBuilder[i].Substring(1);
                                break;
                            case '=':
                                FieldsBuilder[i] = "=" + FieldsBuilder[i].Substring(1);
                                break;
                        }
                    }
                    else
                    {
                        FieldsBuilder[i] = "=" + Field;
                    }
                }
            }
        }

        #endregion
    }
}
