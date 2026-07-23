using SobekCM.Core.Aggregations;
using SobekCM.Core.Results;
using SobekCM.Core.Search;
using SobekCM.Core.Settings;
using SobekCM.Engine_Library.ApplicationState;
using SobekCM.Tools;
using SolrNet;
using SolrNet.Commands.Parameters;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace SobekCM.Engine_Library.Solr.v5
{

    public class v5_Solr_Searcher
    {
        #region Static method to query the Solr/Lucene for a collection of results

        /// <summary> Perform an search for documents with matching parameters </summary>
        /// <param name="Terms"> List of the search terms which define this search </param>
        /// <param name="Web_Fields"> List of the web fields associate with the search terms </param>
        /// <param name="StartDate"> Starting date, if this search includes a limitation by time </param>
        /// <param name="EndDate"> Ending date, if this search includes a limitation by time </param>
        /// <param name="SearchOptions"> Options related to this search, like the page, results per page, facets, fields, etc.. </param>
        /// <param name="UserMembership"> User-specific membership information, related to a search, which can be used to determine which items this user can discover</param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <param name="Complete_Result_Set_Info"> [OUT] Information about the entire set of results </param>
        /// <param name="Paged_Results"> [OUT] List of search results for the requested page of results </param>
        /// <returns> Page search result object with all relevant result information </returns>
        public static bool Search(List<string> Terms, List<string> Web_Fields, Nullable<DateTime> StartDate, Nullable<DateTime> EndDate, Search_Options_Info SearchOptions, Search_User_Membership_Info UserMembership, Custom_Tracer Tracer, out Search_Results_Statistics Complete_Result_Set_Info, out List<iSearch_Title_Result> Paged_Results)
        {
            Tracer?.Add_Trace("v5_Solr_Searcher.Search", String.Empty);

            // Get the query string
            string queryString = Create_Query_String(Terms, Web_Fields, StartDate, EndDate, Tracer);

            // Exclude hidden, if not an admin (later we will deal with user/groups and IP restrictions)
            if ((UserMembership == null) || (!UserMembership.LoggedIn) || (!UserMembership.Admin))
                queryString = "(hidden:0) AND (discover_ips:0) AND (" + queryString + ")";

            // If there was an aggregation code included, put that at the beginning of the search
            if ((!String.IsNullOrEmpty(SearchOptions.AggregationCode)) && (SearchOptions.AggregationCode.ToUpper() != "ALL"))
            {
                if (!String.IsNullOrEmpty(queryString))
                    queryString = "(aggregations:" + SearchOptions.AggregationCode + ") AND " + queryString;
                else
                    queryString = "(aggregations:" + SearchOptions.AggregationCode + ")";
            }

            // Set output initially to null
            return Run_Query(queryString, SearchOptions, UserMembership, Tracer, out Complete_Result_Set_Info, out Paged_Results);
        }

        private static string to_standard_date_string(DateTime AsDate)
        {
            return AsDate.Year + "-" + AsDate.Month.ToString().PadLeft(2, '0') + "-" + AsDate.Day.ToString().PadLeft(2, '0') + "T00:00:00Z";
        }

        /// <summary> Return the list of all items within a single aggregation (or ALL aggregations) </summary>
        /// <param name="SearchOptions"> Options related to this search, like the page, results per page, facets, fields, etc.. </param>
        /// <param name="UserMembership"> User-specific membership information, related to a search, which can be used to determine which items this user can discover</param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <param name="Complete_Result_Set_Info"> [OUT] Information about the entire set of results </param>
        /// <param name="Paged_Results"> [OUT] List of search results for the requested page of results </param>
        /// <returns> Page search result object with all relevant result information</returns>
        public static bool All_Browse(Search_Options_Info SearchOptions, Search_User_Membership_Info UserMembership, Custom_Tracer Tracer, out Search_Results_Statistics Complete_Result_Set_Info, out List<iSearch_Title_Result> Paged_Results)
        {
            // Get the query string value
            string queryString = String.Empty;

            // Exclude hidden, if not an admin (later we will deal with user/groups and IP restrictions)
            if ((UserMembership == null) || (!UserMembership.LoggedIn) || (!UserMembership.Admin))
                queryString = "(hidden:0) AND (discover_ips:0)";

            // If there was an aggregation code included, put that at the beginning of the search
            if ((!String.IsNullOrEmpty(SearchOptions.AggregationCode)) && (SearchOptions.AggregationCode.ToUpper() != "ALL"))
            {
                if (!String.IsNullOrEmpty(queryString))
                    queryString = "(aggregations:" + SearchOptions.AggregationCode + ") AND " + queryString;
                else
                    queryString = "(aggregations:" + SearchOptions.AggregationCode + ")";
            }

            // Set output initially to null
            return Run_Query(queryString, SearchOptions, UserMembership, Tracer, out Complete_Result_Set_Info, out Paged_Results);
        }

        /// <summary> Return the list of newly added items within a single aggregation (or ALL aggregations) </summary>
        /// <param name="SearchOptions"> Options related to this search, like the page, results per page, facets, fields, etc.. </param>
        /// <param name="UserMembership"> User-specific membership information, related to a search, which can be used to determine which items this user can discover</param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <param name="Complete_Result_Set_Info"> [OUT] Information about the entire set of results </param>
        /// <param name="Paged_Results"> [OUT] List of search results for the requested page of results </param>
        /// <returns> Page search result object with all relevant result information</returns>
        public static bool New_Browse(Search_Options_Info SearchOptions, Search_User_Membership_Info UserMembership, Custom_Tracer Tracer, out Search_Results_Statistics Complete_Result_Set_Info, out List<iSearch_Title_Result> Paged_Results)
        {
            // Computer the datetime for this
            DateTime two_weeks_ago = DateTime.Now.Subtract(new TimeSpan(14, 0, 0, 0));
            string date_string = two_weeks_ago.Year + "-" + two_weeks_ago.Month.ToString().PadLeft(2, '0') + "-" + two_weeks_ago.Day.ToString().PadLeft(2, '0') + "T00:00:00Z";

            // Get the query string value
            string queryString = null;

            // Exclude hidden, if not an admin (later we will deal with user/groups and IP restrictions)
            if ((UserMembership == null) || (!UserMembership.LoggedIn) || (!UserMembership.Admin))
                queryString = "(hidden:0) AND (discover_ips:0) AND ( made_public_date:[" + date_string + " TO *] )";
            else
                queryString = "( made_public_date:[" + date_string + " TO *] )";

            // If there was an aggregation code included, put that at the beginning of the search
            if ((!String.IsNullOrEmpty(SearchOptions.AggregationCode)) && (SearchOptions.AggregationCode.ToUpper() != "ALL"))
            {
                if (!String.IsNullOrEmpty(queryString))
                    queryString = "(aggregations:" + SearchOptions.AggregationCode + ") AND " + queryString;
                else
                    queryString = "(aggregations:" + SearchOptions.AggregationCode + ")";
            }

            // Set output initially to null
            return Run_Query(queryString, SearchOptions, UserMembership, Tracer, out Complete_Result_Set_Info, out Paged_Results);
        }

        /// <summary> Run a solr query against the solr document index </summary>
        /// <param name="QueryString"> Solr query string </param>
        /// <param name="SearchOptions"> Options related to this search, like the page, results per page, facets, fields, etc.. </param>
        /// <param name="UserMembership"> User-specific membership information, related to a search, which can be used to determine which items this user can discover</param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <param name="Complete_Result_Set_Info"> [OUT] Information about the entire set of results </param>
        /// <param name="Paged_Results"> [OUT] List of search results for the requested page of results </param>
        /// <returns> Page search result object with all relevant result information </returns>
        public static bool Run_Query(string QueryString, Search_Options_Info SearchOptions, Search_User_Membership_Info UserMembership, Custom_Tracer Tracer, out Search_Results_Statistics Complete_Result_Set_Info, out List<iSearch_Title_Result> Paged_Results)
        {
            // If the query string is empty, then set it back to *:*
            if (QueryString.Trim().Length == 0)
                QueryString = "*:*";

            // Set output initially to null
            Paged_Results = new List<iSearch_Title_Result>();
            Complete_Result_Set_Info = null;

            try
            {
                // Ensure page is not erroneously set to zero or negative
                int pageNumber = SearchOptions.Page;
                if (pageNumber <= 0)
                    pageNumber = 1;

                // Get and clean the solr document url
                string solrDocumentUrl = Engine_ApplicationCache_Gateway.Settings.Servers.Document_Solr_Index_URL;
                if ((!String.IsNullOrEmpty(solrDocumentUrl)) && (solrDocumentUrl[solrDocumentUrl.Length - 1] == '/'))
                    solrDocumentUrl = solrDocumentUrl.Substring(0, solrDocumentUrl.Length - 1);

                // Create the solr worker to query the document index
                var solrWorker = Solr_Operations_Cache<v5_SolrDocument>.GetSolrOperations(solrDocumentUrl);

                // Get the list of fields
                var fields = new List<string> { "did", "mainthumb", "title", "discover_ips", "hidden", "restricted_msg", "group_restrictions" };
                fields.AddRange(SearchOptions.Fields.Select(MetadataField => MetadataField.SolrCode));

                // Create the query options
                var options = new QueryOptions
                {
                    Rows = SearchOptions.ResultsPerPage,
                    StartOrCursor = new StartOrCursor.Start((pageNumber - 1) * SearchOptions.ResultsPerPage),
                    Fields = fields
                };

                // Was there full text search in that?
                if ((QueryString.Contains("(fulltext:")) && (SearchOptions.IncludeFullTextSnippets))
                {
                    options.Highlight = new HighlightingParameters { Fields = new[] { "fulltext" }, Fragsize = 255 };
                    options.ExtraParams = new Dictionary<string, string> { { "hl.useFastVectorHighlighter", "true" }, { "wt", "xml" } };
                }
                else
                {
                    // We still need to instruct SOLR to return the results as XML for solr to parse it
                    options.ExtraParams = new KeyValuePair<string, string>[] { new KeyValuePair<string, string>("wt", "xml") };
                }

                // If the search stats are needed, let's get the facets
                if ((SearchOptions.Facets != null) && (SearchOptions.Facets.Count > 0))
                {
                    // Create the query facters
                    options.Facet = new FacetParameters();
                    foreach (Complete_Item_Aggregation_Metadata_Type facet in SearchOptions.Facets)
                    {
                        if (String.IsNullOrWhiteSpace(facet.SolrCode)) continue;

                        options.Facet.Queries.Add(new SolrFacetFieldQuery(facet.SolrCode) { MinCount = 1, Limit = 100 });
                    }
                }

                // Set the sort value
                if (SearchOptions.Sort != 0)
                {
                    options.OrderBy.Clear();
                    switch (SearchOptions.Sort)
                    {
                        case 1:
                            options.OrderBy.Add(new SortOrder("title.sort", Order.ASC));
                            break;

                        case 2:
                            options.OrderBy.Add(new SortOrder("bibid", Order.ASC));
                            break;

                        case 3:
                            options.OrderBy.Add(new SortOrder("bibid", Order.DESC));
                            break;

                        case 10:
                            options.OrderBy.Add(new SortOrder("date.gregorian", Order.ASC));
                            break;

                        case 11:
                            options.OrderBy.Add(new SortOrder("date.gregorian", Order.DESC));
                            break;

                        case 12:
                            options.OrderBy.Add(new SortOrder("timeline_date", Order.ASC));

                            // If sorting by this, only get records with timeline date
                            QueryString = "(" + QueryString + ") AND timeline_date:[* TO *]";
                            break;

                    }
                }

                // Should this be grouped?
                bool grouped_results = false;
                if ((SearchOptions.GroupItemsByTitle) && (SearchOptions.Sort < 10) && (QueryString.IndexOf("fulltext") < 0))
                {
                    Tracer?.Add_Trace("v5_Solr_Searcher.Run_Query", "Grouping search request by bibid");

                    grouped_results = true;

                    var groupingParams = new GroupingParameters
                    {
                        Fields = new[] { "bibid" },

                        Format = GroupingFormat.Grouped,

                        Limit = 10,

                        Ngroups = true
                    };

                    options.Grouping = groupingParams;
                }

                // Log the search term
                Tracer?.Add_Trace("v5_Solr_Searcher.Run_Query", "Solr Query: " + QueryString);

                Tracer?.Add_Trace("v5_Solr_Searcher.Run_Query", "Perform the search");

                // Perform this search
                SolrQueryResults<v5_SolrDocument> results = solrWorker.Query(QueryString, options);


                Tracer?.Add_Trace("v5_Solr_Searcher.Run_Query", "Build the results object");

                // Create the search statistcs (this part assumes no grouping, and then we fix the count shortly)
                List<string> metadataLabels = SearchOptions.Fields.Select(MetadataType => MetadataType.DisplayTerm).ToList();
                Complete_Result_Set_Info = new Search_Results_Statistics(metadataLabels)
                {
                    Total_Titles = (int)results.NumFound,
                    Total_Items = (int)results.NumFound,
                    QueryTime = results.Header.QTime
                };

                // If the search stats were needed, get the facets out
                if ((SearchOptions.Facets != null) && (SearchOptions.Facets.Count > 0))
                {
                    // Copy over all the facets
                    foreach (Complete_Item_Aggregation_Metadata_Type facetTerm in SearchOptions.Facets)
                    {
                        // Create the collection and and assifn the metadata type id
                        var thisCollection = new Search_Facet_Collection(facetTerm.ID);

                        // Add each value
                        foreach (var facet in results.FacetFields[facetTerm.SolrCode])
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

                // Build the results mapper object
                var mapper = new v5_SolrDocument_Results_Mapper();

                // Build the results differently, depending on whether they were grouped or not
                if (grouped_results)
                {
                    Tracer?.Add_Trace("v5_Solr_Searcher.Run_Query", "Building list of results (grouped)");

                    // Get the grouped results (only grouped by bibid)
                    GroupedResults<v5_SolrDocument> title_groupings = results.Grouping["bibid"];

                    // Now step through each group (i.e., titles/bibs) in the groups
                    foreach (Group<v5_SolrDocument> grouping in title_groupings.Groups)
                    {
                        // Convert the grouping to the new result
                        v5_Solr_Title_Result newResult = mapper.Map_To_Result(grouping, SearchOptions.Fields);

                        Paged_Results.Add(newResult);
                    }

                    // Now, fix the stats as well
                    Complete_Result_Set_Info.Total_Items = title_groupings.Matches;
                    Complete_Result_Set_Info.Total_Titles = title_groupings.Ngroups.Value;
                }
                else
                {
                    Tracer?.Add_Trace("v5_Solr_Searcher.Run_Query", "Building list of results (not grouped)");

                    // Pass all the results into the List and add the highlighted text to each result as well
                    foreach (v5_SolrDocument thisResult in results)
                    {
                        // Convert to the new result
                        v5_Solr_Title_Result newResult = mapper.Map_To_Result(thisResult, SearchOptions.Fields);

                        // Add the highlight snippet, if applicable
                        if ((results.Highlights != null) && (results.Highlights.ContainsKey(thisResult.DID)) && (results.Highlights[thisResult.DID].Count > 0) && (results.Highlights[thisResult.DID].ElementAt(0).Value.Count > 0))
                        {
                            newResult.Snippet = results.Highlights[thisResult.DID].ElementAt(0).Value.ElementAt(0);
                        }

                        Paged_Results.Add(newResult);
                    }
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region Static method to create the query string from web fields, terms, and dates

        /// <summary> Creates the solr/lucene query string for the search terms and dates</summary>
        /// <param name="Terms"> List of the search terms </param>
        /// <param name="Web_Fields"> List of the web fields associate with the search terms </param>
        /// <param name="StartDate"> Starting date, if this search includes a limitation by time </param>
        /// <param name="EndDate"> Ending date, if this search includes a limitation by time </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> Fully built query string ( excluding user membership and aggreagtion membership checks ) </returns>
        public static string Create_Query_String(List<string> Terms, List<string> Web_Fields, Nullable<DateTime> StartDate, Nullable<DateTime> EndDate, Custom_Tracer Tracer)
        {
            Tracer?.Add_Trace("v5_Solr_Searcher.Create_Query_String", "Build the Solr query");

            // Start to build the query
            var queryStringBuilder = new StringBuilder();

            // If no query, this is an ALL browse
            if (((Web_Fields == null) || (Web_Fields.Count == 0)) || ((Terms == null) || (Terms.Count == 0)))
            {
                queryStringBuilder.Append("(*:*)");
            }
            else
            {
                // Step through all the terms and fields
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
                            queryStringBuilder.Append("(" + solr_field + "\"" + searchTerm.Replace(":", "").Replace("[", "").Replace("]", "") + "\")");
                        }
                        else
                        {
                            queryStringBuilder.Append("(" + solr_field + searchTerm.Replace(":", "").Replace("[", "").Replace("]", "") + ")");
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
            }

            // Get the query string value
            string queryString = queryStringBuilder.ToString();

            // If there is a date range add that
            if ((StartDate.HasValue) || (EndDate.HasValue))
            {
                if ((StartDate.HasValue) && (EndDate.HasValue))
                {
                    queryString = "(" + queryString + ") AND ( date.gregorian:[" + to_standard_date_string(StartDate.Value) + " TO " + to_standard_date_string(EndDate.Value) + "])";
                }
                else if (StartDate.HasValue) // So EndDate must not have value
                {
                    queryString = "(" + queryString + ") AND ( date.gregorian:[" + to_standard_date_string(StartDate.Value) + " TO *])";
                }
                else // End date must have value, but not start date
                {
                    queryString = "(" + queryString + ") AND ( date.gregorian:[* TO " + to_standard_date_string(EndDate.Value) + "])";
                }
            }

            return queryString;
        }


        #endregion

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

        #region Method to search for pages within a single item

        /// <summary> Perform an in-document search for pages with matching full-text </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for the item to search </param>
        /// <param name="VID"> Volume identifier for the item to search </param>
        /// <param name="Search_Terms"> Terms to search for within the page text </param>
        /// <param name="ResultsPerPage"> Number of results to display per a "page" of results </param>
        /// <param name="ResultsPage"> Which page of results to return ( one-based, so the first page is page number of one )</param>
        /// <param name="Sort_By_Score"> Flag indicates whether to sort the results by relevancy score, rather than the default page order </param>
        /// <returns> Page search result object with all relevant result information </returns>
        public static v5_Solr_Page_Results Search_Within_Document(string BibID, string VID, List<string> Search_Terms, int ResultsPerPage, int ResultsPage, bool Sort_By_Score)
        {
            // Ensure page is not erroneously set to zero or negative
            if (ResultsPage <= 0)
                ResultsPage = 1;

            // Get and clean the solr document url
            string solrPageUrl = Engine_ApplicationCache_Gateway.Settings.Servers.Page_Solr_Index_URL;
            if ((!String.IsNullOrEmpty(solrPageUrl)) && (solrPageUrl[solrPageUrl.Length - 1] == '/'))
                solrPageUrl = solrPageUrl.Substring(0, solrPageUrl.Length - 1);

            // Create the solr worker to query the page index
            var solrWorker = Solr_Operations_Cache<v5_Solr_Page_Result>.GetSolrOperations(solrPageUrl);

            // Create the query options
            var options = new QueryOptions
            {
                Rows = ResultsPerPage,
                StartOrCursor = new StartOrCursor.Start((ResultsPage - 1) * ResultsPerPage),
                Fields = new[] { "pageid", "pagename", "pageorder", "score", "thumbnail" },
                Highlight = new HighlightingParameters { Fields = new[] { "pagetext" }, Fragsize = 1000 },
                ExtraParams = new Dictionary<string, string> { { "hl.useFastVectorHighlighter", "true" }, { "wt", "xml" } }
            };

            // If this is not the default Solr sort (by score) request sort by the page order
            if (!Sort_By_Score)
                options.OrderBy = new[] { new SortOrder("pageorder", Order.ASC) };

            // Build the query string
            var queryStringBuilder = new StringBuilder("(bibid:" + BibID + ")AND(vid:" + VID + ")AND(");
            bool first_value = true;
            foreach (string searchTerm in Search_Terms)
            {
                if (searchTerm.Length > 1)
                {
                    // Skip any AND NOT for now
                    if (searchTerm[0] != '-')
                    {
                        // Find the joiner
                        if (first_value)
                        {
                            if (searchTerm.IndexOf(" ") > 0)
                            {
                                if ((searchTerm[0] == '+') || (searchTerm[0] == '=') || (searchTerm[0] == '-'))
                                {
                                    queryStringBuilder.Append("(pagetext:\"" + searchTerm.Substring(1).Replace(":", "") + "\")");
                                }
                                else
                                {
                                    queryStringBuilder.Append("(pagetext:\"" + searchTerm.Replace(":", "") + "\")");
                                }
                            }
                            else
                            {
                                if ((searchTerm[0] == '+') || (searchTerm[0] == '=') || (searchTerm[0] == '-'))
                                {
                                    queryStringBuilder.Append("(pagetext:" + searchTerm.Substring(1).Replace(":", "") + ")");
                                }
                                else
                                {
                                    queryStringBuilder.Append("(pagetext:" + searchTerm.Replace(":", "") + ")");
                                }
                            }
                            first_value = false;
                        }
                        else
                        {
                            if ((searchTerm[0] == '+') || (searchTerm[0] == '=') || (searchTerm[0] == '-'))
                            {
                                queryStringBuilder.Append(searchTerm[0] == '=' ? " OR " : " AND ");

                                if (searchTerm.IndexOf(" ") > 0)
                                {
                                    queryStringBuilder.Append("(pagetext:\"" + searchTerm.Substring(1).Replace(":", "") + "\")");
                                }
                                else
                                {
                                    queryStringBuilder.Append("(pagetext:" + searchTerm.Substring(1).Replace(":", "") + ")");
                                }
                            }
                            else
                            {
                                if (searchTerm.IndexOf(" ") > 0)
                                {
                                    queryStringBuilder.Append(" AND (pagetext:\"" + searchTerm.Replace(":", "") + "\")");
                                }
                                else
                                {
                                    queryStringBuilder.Append(" AND (pagetext:" + searchTerm.Replace(":", "") + ")");
                                }
                            }
                        }
                    }
                }
            }
            queryStringBuilder.Append(")");


            // Perform this search
            SolrQueryResults<v5_Solr_Page_Result> results = solrWorker.Query(queryStringBuilder.ToString(), options);

            // Create the results object to pass back out
            var searchResults = new v5_Solr_Page_Results
            {
                QueryTime = results.Header.QTime,
                TotalResults = (int)results.NumFound,
                Query = queryStringBuilder.ToString(),
                Sort_By_Score = Sort_By_Score,
                Page_Number = ResultsPage
            };

            // Pass all the results into the List and add the highlighted text to each result as well
            foreach (v5_Solr_Page_Result thisResult in results)
            {
                // Add the highlight snipper
                if ((results.Highlights.ContainsKey(thisResult.PageID)) && (results.Highlights[thisResult.PageID].Count > 0) && (results.Highlights[thisResult.PageID].ElementAt(0).Value.Count > 0))
                {
                    thisResult.Snippet = results.Highlights[thisResult.PageID].ElementAt(0).Value.ElementAt(0);
                }

                // Add this results
                searchResults.Add_Result(thisResult);
            }

            return searchResults;
        }

        #endregion

        #region Method to find all the fields which are linked to a collection (for advanced search drop down)

        // Call from Item_Aggregation_Utilities.Get_Complete_Item_Aggregation
        public static List<short> Get_SobekCodes_With_Data(string aggregationCode, List<Metadata_Search_Field> searchFields)
        {
            try
            {
                // Get and clean the solr document url
                string solrDocumentUrl = Engine_ApplicationCache_Gateway.Settings.Servers.Document_Solr_Index_URL;
                if ((!String.IsNullOrEmpty(solrDocumentUrl)) && (solrDocumentUrl[solrDocumentUrl.Length - 1] == '/'))
                    solrDocumentUrl = solrDocumentUrl.Substring(0, solrDocumentUrl.Length - 1);

                // Create the solr worker to query the document index
                var solrWorker = Solr_Operations_Cache<v5_SolrDocument>.GetSolrOperations(solrDocumentUrl);

                // Only fields that actually map to something in the Solr index, excluding
                // the ANYWHERE ('ZZ') and FULL TEXT ('TX') pseudo-fields
                var mappedFields = searchFields
                    .Where(f => !String.IsNullOrEmpty(f.Solr_Field))
                    .Where(f => (f.Web_Code != "ZZ") && (f.Web_Code != "TX"))
                    .ToList();

                var distinctSolrFields = mappedFields
                    .Select(f => f.Solr_Field)
                    .Distinct()
                    .ToList();

                var facetQueries = distinctSolrFields
                    .Select(sf => (ISolrFacetQuery)new SolrFacetQuery(new SolrQuery($"{sf}:[* TO *]")))
                    .ToList();

                var results = solrWorker.Query(
                    new SolrQuery($"aggregations:\"{aggregationCode}\""),
                    new QueryOptions
                    {
                        Rows = 0,
                        Facet = new FacetParameters { Queries = facetQueries }
                    });

                // solr field name -> count of matching docs with that field populated
                var countsBySolrField = results.FacetQueries
                    .ToDictionary(kv => kv.Key.Split(':')[0], kv => kv.Value);

                // Return the SobekCode for every metadata search field whose solr field had data
                return mappedFields
                    .Where(f => countsBySolrField.TryGetValue(f.Solr_Field, out int count) && count > 0)
                    .Select(f => f.ID)
                    .Distinct()
                    .ToList();
            }
            catch (Exception e)
            {
                string message = e.Message;

                return null;
            }
        }

        // Call to pull the ordered list of distinct temporal years present for an aggregation
        public static List<short> Get_Distinct_Temporal_Years(string aggregationCode)
        {
            try
            {
                // Get and clean the solr document url
                string solrDocumentUrl = Engine_ApplicationCache_Gateway.Settings.Servers.Document_Solr_Index_URL;
                if ((!String.IsNullOrEmpty(solrDocumentUrl)) && (solrDocumentUrl[solrDocumentUrl.Length - 1] == '/'))
                    solrDocumentUrl = solrDocumentUrl.Substring(0, solrDocumentUrl.Length - 1);

                // Create the solr worker to query the document index
                var solrWorker = Solr_Operations_Cache<v5_SolrDocument>.GetSolrOperations(solrDocumentUrl);

                var results = solrWorker.Query(
                    new SolrQuery($"aggregations:\"{aggregationCode}\""),
                    new QueryOptions
                    {
                        Rows = 0,
                        Facet = new FacetParameters
                        {
                            Queries = new List<ISolrFacetQuery>
                            {
                                new SolrFacetFieldQuery("temporal_year") { MinCount = 1, Limit = -1 }
                            }
                        }
                    });

                // Parse each distinct facet value into a short, skipping anything that doesn't parse
                var years = new List<short>();
                foreach (var facet in results.FacetFields["temporal_year"])
                {
                    if (short.TryParse(facet.Key, out short year))
                        years.Add(year);
                }

                years.Sort();
                return years;
            }
            catch (Exception e)
            {
                string message = e.Message;

                return null;
            }
        }

        #endregion

        #region Method to look up a specific set of items (e.g., a user's bookshelf) by BibID/VID

        /// <summary> Looks up a specific, ordered set of items by BibID/VID in Solr -- e.g., for building the display
        /// of a user's bookshelf/folder, where the folder membership, order, and notes live in the database but the
        /// display metadata for each item now only lives in Solr </summary>
        /// <param name="FolderItems"> DataTable with (in column order) BibID, VID, ItemOrder, SortDate, and UserNotes
        /// for every item in the folder -- see <see cref="Engine_Database.Get_User_Folder_Items"/> </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> List of search results for the folder's items, ordered to match the folder's stored item order,
        /// with each item's user notes attached. Items no longer found in Solr (e.g., deleted since being added to
        /// the folder) are simply omitted. </returns>
        public static List<iSearch_Title_Result> Get_Folder_Item_Results(DataTable FolderItems, Custom_Tracer Tracer)
        {
            var emptyResults = new List<iSearch_Title_Result>();
            if ((FolderItems == null) || (FolderItems.Rows.Count == 0))
                return emptyResults;

            Tracer?.Add_Trace("v5_Solr_Searcher.Get_Folder_Item_Results", "Building the list of DIDs, order, and notes for this folder");

            // Build the DID -> (order, notes) lookups from the folder's database rows
            var orderByDid = new Dictionary<string, int>();
            var notesByDid = new Dictionary<string, string>();
            var didList = new List<string>();
            foreach (DataRow row in FolderItems.Rows)
            {
                string bibid = row[0].ToString();
                string vid = row[1].ToString();
                string did = bibid + ":" + vid;

                didList.Add(did);
                orderByDid[did] = Convert.ToInt32(row[2]);
                notesByDid[did] = row[4].ToString();
            }

            try
            {
                // Get and clean the solr document url
                string solrDocumentUrl = Engine_ApplicationCache_Gateway.Settings.Servers.Document_Solr_Index_URL;
                if ((!String.IsNullOrEmpty(solrDocumentUrl)) && (solrDocumentUrl[solrDocumentUrl.Length - 1] == '/'))
                    solrDocumentUrl = solrDocumentUrl.Substring(0, solrDocumentUrl.Length - 1);

                // Create the solr worker to query the document index
                var solrWorker = Solr_Operations_Cache<v5_SolrDocument>.GetSolrOperations(solrDocumentUrl);

                // Fetch every item in the folder in a single round trip, rather than one query per item
                string didClause = String.Join(" OR ", didList.Select(did => "\"" + did + "\""));
                var query = new SolrQuery($"did:({didClause})");

                var options = new QueryOptions
                {
                    Rows = didList.Count,
                    Fields = new List<string> { "did", "mainthumb", "title", "discover_ips", "hidden", "restricted_msg", "group_restrictions" }
                };

                Tracer?.Add_Trace("v5_Solr_Searcher.Get_Folder_Item_Results", "Perform the search for " + didList.Count + " folder item(s)");

                SolrQueryResults<v5_SolrDocument> solrResults = solrWorker.Query(query, options);

                // Convert every matching document, then re-attach the folder-specific order and notes
                var mapper = new v5_SolrDocument_Results_Mapper();
                var emptyDisplayFields = new List<Complete_Item_Aggregation_Metadata_Type>();
                var scoredResults = new List<(int Order, v5_Solr_Title_Result Result)>();

                foreach (v5_SolrDocument solrDocument in solrResults)
                {
                    v5_Solr_Title_Result newResult = mapper.Map_To_Result(solrDocument, emptyDisplayFields);

                    if (notesByDid.TryGetValue(solrDocument.DID, out string notes))
                        newResult.UserNotes = notes;

                    int order = orderByDid.TryGetValue(solrDocument.DID, out int thisOrder) ? thisOrder : int.MaxValue;
                    scoredResults.Add((order, newResult));
                }

                // Solr won't preserve the folder's stored item order, so re-sort into it here
                scoredResults.Sort((a, b) => a.Order.CompareTo(b.Order));

                return scoredResults.Select(r => (iSearch_Title_Result)r.Result).ToList();
            }
            catch (Exception ee)
            {
                Tracer?.Add_Trace("v5_Solr_Searcher.Get_Folder_Item_Results", "Exception caught: " + ee.Message, Custom_Trace_Type_Enum.Error);
                return emptyResults;
            }
        }

        #endregion
    }
}
