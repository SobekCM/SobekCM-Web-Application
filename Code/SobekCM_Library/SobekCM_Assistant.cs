#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.Aggregations;
using SobekCM.Core.Client;
using SobekCM.Core.Configuration.Localization;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.Navigation;
using SobekCM.Core.Results;
using SobekCM.Core.Search;
using SobekCM.Core.SiteMap;
using SobekCM.Core.Skins;
using SobekCM.Core.Users;
using SobekCM.Core.WebContent;
using SobekCM.Engine_Library.Database;
using SobekCM.Engine_Library.SiteMap;
using SobekCM.Engine_Library.Solr.v5;
using SobekCM.Library.Database;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

#endregion

namespace SobekCM.Library
{
    /// <summary> Class is a helper class that pulls much of the data needed for the processing of requests.  Tries to retrieve
    /// from the cache, and if the data is not there, it will then build the object and try to store on the cache  </summary>
    public class SobekCM_Assistant
    {
        #region Method to get the translation set for a single language

        /// <summary> Gets the entire translation set (English term -&gt; translated value) for a single language </summary>
        /// <param name="Language"> Language to retrieve the translation set for </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> Dictionary of every English term translated into the requested language ( never NULL, may be empty ) </returns>
        /// <remarks> This attempts to pull the translation set from the cache first ( <see cref="CachedDataManager.Localization"/>, a
        /// few minutes' sliding expiration ).  If unsuccessful, it builds the set from the database ( <see cref="Engine_Database.Get_Translations_By_Language"/> )
        /// and hands off to the <see cref="CachedDataManager" /> to store in the cache </remarks>
        public Dictionary<string, string> Get_Translation_Set(Web_Language_Enum Language, Custom_Tracer Tracer)
        {
            Tracer?.Add_Trace("SobekCM_Assistant.Get_Translation_Set", String.Empty);

            string languageCode = Web_Language_Enum_Converter.Enum_To_Code(Language);
            if (String.IsNullOrEmpty(languageCode))
                return new Dictionary<string, string>();

            // Try to get this from the cache first
            Dictionary<string, string> translationSet = CachedDataManager.Localization.Retrieve_Translation_Set(languageCode, Tracer);
            if (translationSet != null)
                return translationSet;

            // Not cached (or expired) -- pull from the database and cache it
            translationSet = Engine_Database.Get_Translations_By_Language(languageCode, Tracer);
            CachedDataManager.Localization.Store_Translation_Set(languageCode, translationSet, Tracer);

            return translationSet;
        }

        #endregion

        #region Method to retrieve simple web content text to view within a skin

        /// <summary> Gets the simple CMS/info object and text to display </summary>
        /// <param name="Current_Mode"> Mode / navigation information for the current request</param>
        /// <param name="Base_Directory"> Base directory location under which the the CMS/info source file will be found</param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <param name="Simple_Web_Content"> [OUT] Built browse object which contains information like title, banner, etc.. and the entire text to be displayed </param>
        /// <param name="Site_Map"> [OUT] Optional navigational site map object related to this page </param>
        /// <returns>TRUE if successful, otherwise FALSE </returns>
        /// <remarks> This always pulls the data directly from disk; this text is not cached. </remarks>
        public bool Get_Simple_Web_Content_Text(Navigation_Object Current_Mode, string Base_Directory, Custom_Tracer Tracer, out HTML_Based_Content Simple_Web_Content, out SobekCM_SiteMap Site_Map)
        {
            Tracer?.Add_Trace("SobekCM_Assistant.Get_Simple_Web_Content_Text", String.Empty);

            Site_Map = null;
            Simple_Web_Content = null;

            // Get the web content object
            if (((Current_Mode.WebContentID.HasValue) && (Current_Mode.WebContentID.Value > 0)) && ((!Current_Mode.Missing.HasValue) || (!Current_Mode.Missing.Value)))
                Simple_Web_Content = SobekEngineClient.WebContent.Get_HTML_Based_Content(Current_Mode.WebContentID.Value, true, Tracer);

            // If somehow this is null and this was for DEFAULT, just add the page
            if (Simple_Web_Content == null)
            {
                Simple_Web_Content = SobekEngineClient.WebContent.Get_Special_Missing_Page(Tracer);
            }

            if (Simple_Web_Content == null)
            {
                Current_Mode.Error_Message = "Unable to retrieve simple text item '" + Current_Mode.Info_Browse_Mode.Replace("_", "\\") + "'";
                return false;
            }

            // If this is a redirect, just return 
            if (!String.IsNullOrEmpty(Simple_Web_Content.Redirect))
                return true;

            if (String.IsNullOrEmpty(Simple_Web_Content.Content))
            {
                Current_Mode.Error_Message = "Unable to read the file for display";
                return false;
            }

            // Look for a site map
            if (!String.IsNullOrEmpty(Simple_Web_Content.SiteMap))
            {
                // Look in the cache first
                Site_Map = CachedDataManager.Retrieve_Site_Map(Simple_Web_Content.SiteMap, Tracer);

                // If this was NULL, pull it
                if (Site_Map == null)
                {
                    string sitemap_file = Simple_Web_Content.SiteMap;
                    if (!sitemap_file.ToLower().Contains(".sitemap"))
                        sitemap_file = sitemap_file + ".sitemap";

                    // Only continue if the file exists
                    if (File.Exists(UI_ApplicationCache_Gateway.Settings.Servers.Base_Directory + "design\\webcontent\\sitemaps\\" + sitemap_file))
                    {
                        Tracer?.Add_Trace("SobekCM_Assistant.Get_Simple_Web_Content_Text", "Reading site map file");

                        // Try to read this sitemap file
                        Site_Map = SobekCM_SiteMap_Reader.Read_SiteMap_File(UI_ApplicationCache_Gateway.Settings.Servers.Base_Directory + "design\\webcontent\\sitemaps\\" + sitemap_file);

                        // If the sitemap file was succesfully read, cache it
                        if (Site_Map != null)
                        {
                            CachedDataManager.Store_Site_Map(Site_Map, Simple_Web_Content.SiteMap, Tracer);
                        }
                    }
                    else if (File.Exists(UI_ApplicationCache_Gateway.Settings.Servers.Base_Directory + "design\\webcontent\\" + sitemap_file))
                    {
                        // This is just for some legacy material
                        Tracer?.Add_Trace("SobekCM_Assistant.Get_Simple_Web_Content_Text", "Reading site map file");

                        // Try to read this sitemap file
                        Site_Map = SobekCM_SiteMap_Reader.Read_SiteMap_File(UI_ApplicationCache_Gateway.Settings.Servers.Base_Directory + "design\\webcontent\\" + sitemap_file);

                        // If the sitemap file was succesfully read, cache it
                        if (Site_Map != null)
                        {
                            CachedDataManager.Store_Site_Map(Site_Map, Simple_Web_Content.SiteMap, Tracer);
                        }
                    }
                }
            }

            // Since this is not cached, we can apply the individual user settings to the static text which was read right here
            Simple_Web_Content.Content = Simple_Web_Content.Apply_Settings_To_Static_Text(Simple_Web_Content.Content, null, Current_Mode.Skin, Current_Mode.Base_Skin, Current_Mode.Base_URL, UrlWriterHelper.URL_Options(Current_Mode), Tracer);

            return true;
        }

        #endregion

        #region Method to get the user folders for that particular user 

        /// <summary> Retrieve the (assummed private) user folder browse by user and folder name </summary>
        /// <param name="Folder_Name"> Name of the folder to retieve the browse for </param>
        /// <param name="User_ID"> ID for the user </param>
        /// <param name="Results_Per_Page"> Number of results to display in this page (set higher if EXPORT is chosen)</param>
        /// <param name="ResultsPage">Which page of results to return ( one-based, so the first page is page number of one )</param>
        /// <param name="Language"> Current user interface language, used to pull the display field labels in the right language </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <param name="Complete_Result_Set_Info"> [OUT] Information about the entire set of results </param>
        /// <param name="Paged_Results"> [OUT] List of search results for the requested page of results </param>
        /// <returns> TRUE if successful, otherwise FALSE </returns>
        /// <remarks> This attempts to pull the objects from the cache.  If unsuccessful, it builds the objects from the
        /// database and hands off to the <see cref="CachedDataManager" /> to store in the cache </remarks>
        public bool Get_User_Folder(string Folder_Name, int User_ID, int Results_Per_Page, int ResultsPage, Web_Language_Enum Language, Custom_Tracer Tracer, out Search_Results_Statistics Complete_Result_Set_Info, out List<iSearch_Title_Result> Paged_Results)
        {
            Tracer?.Add_Trace("SobekCM_Assistant.Get_User_Folder", String.Empty);

            // Look to see if the browse statistics are available on any cache for this browse
            bool need_browse_statistics = true;
            Complete_Result_Set_Info = CachedDataManager.Retrieve_User_Folder_Browse_Statistics(User_ID, Folder_Name, Tracer);
            if (Complete_Result_Set_Info != null)
                need_browse_statistics = false;

            // Look to see if the paged results are available on any cache..
            bool need_paged_results = true;
            Paged_Results = CachedDataManager.Retrieve_User_Folder_Browse(User_ID, Folder_Name, ResultsPage, Results_Per_Page, Tracer);
            if (Paged_Results != null)
                need_paged_results = false;

            // Was a copy found in the cache?
            if ((!need_browse_statistics) && (!need_paged_results))
            {
                Tracer?.Add_Trace("SobekCM_Assistant.Get_User_Folder", "Browse statistics and paged results retrieved from cache");
            }
            else
            {
                Tracer?.Add_Trace("SobekCM_Assistant.Get_User_Folder", "Building results information");

                // Pull the folder's BibID/VID list (with order and notes) from the database, then look up the
                // current display metadata for those items in Solr -- using the same display fields the special
                // "all" aggregation uses for its own browse/search results, since folder items can span many
                // different collections and there's no single aggregation's Results_Fields to draw from otherwise
                List<Complete_Item_Aggregation_Metadata_Type> displayFields = Get_All_Aggregation_Display_Fields(Language, Tracer);
                DataSet folderItemSet = Engine_Database.Get_User_Folder_Items(User_ID, Folder_Name, Tracer);
                DataTable folderItemsTable = ((folderItemSet != null) && (folderItemSet.Tables.Count > 0)) ? folderItemSet.Tables[0] : null;
                List<iSearch_Title_Result> allFolderResults = v5_Solr_Searcher.Get_Folder_Item_Results(folderItemsTable, displayFields, Tracer);

                if (need_browse_statistics)
                {
                    Complete_Result_Set_Info = new Search_Results_Statistics(displayFields.Select(field => field.DisplayTerm).ToList())
                    {
                        Total_Items = allFolderResults.Count,
                        Total_Titles = allFolderResults.Count
                    };
                }

                // Solr returns every folder item in one pass, so page the results here rather than in the query.
                // Only overwrite Paged_Results if this page wasn't already cached -- e.g. if we're only here because
                // the statistics had expired, the cached page (if any) is still perfectly valid
                if (need_paged_results)
                {
                    Paged_Results = allFolderResults.Skip((ResultsPage - 1) * Results_Per_Page).Take(Results_Per_Page).ToList();
                }

                // Save the overall result set statistics to the cache if something was pulled
                if ((need_browse_statistics) && (Complete_Result_Set_Info != null))
                {
                    CachedDataManager.Store_User_Folder_Browse_Statistics(User_ID, Folder_Name, Complete_Result_Set_Info, Tracer);
                }

                // Save the overall result set statistics to the cache if something was pulled
                if ((need_paged_results) && (Paged_Results != null))
                {
                    CachedDataManager.Store_User_Folder_Browse(User_ID, Folder_Name, ResultsPage, Results_Per_Page, Paged_Results, Tracer);
                }
            }

            return true;
        }

        #endregion

        #region Method to get a public user folder

        /// <summary> Retrieve the public user folder information and browse by user folder id </summary>
        /// <param name="UserFolderID"> Primary key for the public user folder to retrieve </param>
        /// <param name="ResultsPage">Which page of results to return ( one-based, so the first page is page number of one ) </param>
        /// <param name="Language"> Current user interface language, used to pull the display field labels in the right language </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <param name="Folder_Info"> [OUT] Information about this public user folder including name and owner</param>
        /// <param name="Complete_Result_Set_Info"> [OUT] Information about the entire set of results </param>
        /// <param name="Paged_Results"> [OUT] List of search results for the requested page of results </param>
        /// <returns> TRUE if successful, otherwise FALSE </returns>
        /// <remarks> This attempts to pull the objects from the cache.  If unsuccessful, it builds the objects from the
        /// database and hands off to the <see cref="CachedDataManager" /> to store in the cache </remarks>
        public bool Get_Public_User_Folder(int UserFolderID, int ResultsPage, Web_Language_Enum Language, Custom_Tracer Tracer, out Public_User_Folder Folder_Info, out Search_Results_Statistics Complete_Result_Set_Info, out List<iSearch_Title_Result> Paged_Results)
        {
            Tracer?.Add_Trace("SobekCM_Assistant.Get_Public_User_Folder", String.Empty);

            // Set output initially to null
            Paged_Results = null;
            Complete_Result_Set_Info = null;

            // Try to get this from the cache first, otherwise get from database and store in cache
            Folder_Info = CachedDataManager.Retrieve_Public_Folder_Info(UserFolderID, Tracer);
            if (Folder_Info == null)
            {
                Folder_Info = SobekCM_Database.Get_Public_User_Folder(UserFolderID, Tracer);
                if ((Folder_Info != null) && (Folder_Info.IsPublic))
                {
                    CachedDataManager.Store_Public_Folder_Info(Folder_Info, Tracer);
                }
            }

            // If this folder is invalid or private, return false
            if ((Folder_Info == null) || (!Folder_Info.IsPublic))
            {
                return false;
            }

            // Look to see if the browse statistics are available on any cache for this browse
            bool need_browse_statistics = true;
            Complete_Result_Set_Info = CachedDataManager.Retrieve_Public_Folder_Statistics(UserFolderID, Tracer);
            if (Complete_Result_Set_Info != null)
                need_browse_statistics = false;

            // Look to see if the paged results are available on any cache..
            bool need_paged_results = true;
            Paged_Results = CachedDataManager.Retrieve_Public_Folder_Browse(UserFolderID, ResultsPage, Tracer);
            if (Paged_Results != null)
                need_paged_results = false;

            // Was a copy found in the cache?
            if ((!need_browse_statistics) && (!need_paged_results))
            {
                Tracer?.Add_Trace("SobekCM_Assistant.Get_User_Folder", "Browse statistics and paged results retrieved from cache");
            }
            else
            {
                Tracer?.Add_Trace("SobekCM_Assistant.Get_User_Folder", "Building results information");

                // Pull the folder's BibID/VID list (with order and notes) from the database, then look up the
                // current display metadata for those items in Solr -- using the same display fields the special
                // "all" aggregation uses for its own browse/search results, since folder items can span many
                // different collections and there's no single aggregation's Results_Fields to draw from otherwise
                const int resultsPerPage = 20;
                List<Complete_Item_Aggregation_Metadata_Type> displayFields = Get_All_Aggregation_Display_Fields(Language, Tracer);
                DataSet folderItemSet = Engine_Database.Get_User_Folder_Items(Folder_Info.UserID, Folder_Info.FolderName, Tracer);
                DataTable folderItemsTable = ((folderItemSet != null) && (folderItemSet.Tables.Count > 0)) ? folderItemSet.Tables[0] : null;
                List<iSearch_Title_Result> allFolderResults = v5_Solr_Searcher.Get_Folder_Item_Results(folderItemsTable, displayFields, Tracer);

                if (need_browse_statistics)
                {
                    Complete_Result_Set_Info = new Search_Results_Statistics(displayFields.Select(field => field.DisplayTerm).ToList())
                    {
                        Total_Items = allFolderResults.Count,
                        Total_Titles = allFolderResults.Count
                    };
                }

                // Solr returns every folder item in one pass, so page the results here rather than in the query.
                // Only overwrite Paged_Results if this page wasn't already cached -- e.g. if we're only here because
                // the statistics had expired, the cached page (if any) is still perfectly valid
                if (need_paged_results)
                {
                    Paged_Results = allFolderResults.Skip((ResultsPage - 1) * resultsPerPage).Take(resultsPerPage).ToList();
                }

                // Save the overall result set statistics to the cache if something was pulled
                if ((need_browse_statistics) && (Complete_Result_Set_Info != null))
                {
                    CachedDataManager.Store_Public_Folder_Statistics(UserFolderID, Complete_Result_Set_Info, Tracer);
                }

                // Save the overall result set statistics to the cache if something was pulled
                if ((need_paged_results) && (Paged_Results != null))
                {
                    CachedDataManager.Store_Public_Folder_Browse(UserFolderID, ResultsPage, Paged_Results, Tracer);
                }
            }

            return true;
        }

        /// <summary> Gets the display fields configured for the special "all" aggregation, for use when building
        /// results that can't be tied to any one specific aggregation -- e.g., a user's bookshelf, where items can
        /// come from many different collections </summary>
        /// <param name="Language"> Current user interface language, used to pull the display field labels in the right language </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> The "all" aggregation's configured Results_Fields, or an empty list if it could not be retrieved </returns>
        private static List<Complete_Item_Aggregation_Metadata_Type> Get_All_Aggregation_Display_Fields(Web_Language_Enum Language, Custom_Tracer Tracer)
        {
            Item_Aggregation allAggregation = SobekEngineClient.Aggregations.Get_Aggregation("all", Language, UI_ApplicationCache_Gateway.Settings.System.Default_UI_Language, Tracer);

            return ((allAggregation != null) && (allAggregation.Results_Fields != null)) ? allAggregation.Results_Fields : new List<Complete_Item_Aggregation_Metadata_Type>();
        }

        #endregion

        #region Method to pull the static HTML for an all items browse

        /// <summary> Pulls the static html url for a static html browse of all items in an aggregation, used for search engine robot requests </summary>
        /// <param name="Current_Mode"> Mode / navigation information for the current request</param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> File name to read for the static browse HTML to display </returns>
        public string Get_All_Browse_Static_HTML(Navigation_Object Current_Mode, Custom_Tracer Tracer)
        {
            string base_image_url = UI_ApplicationCache_Gateway.Settings.Servers.Base_Data_Directory + Current_Mode.Aggregation + "_all.html";
            return base_image_url;
        }

        #endregion

        #region Method to perform a search

        /// <summary> Performs a search ( or retrieves the search results from the cache ) and outputs the results and search url used  </summary>
        /// <param name="Current_Mode"> Mode / navigation information for the current request</param>
        /// <param name="Aggregation_Object"> Object for the current aggregation object, against which this search is performed </param>
        /// <param name="Search_Stop_Words"> List of search stop workds </param>
        /// <param name="Current_User"> Current user which determines which items to display </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <param name="Complete_Result_Set_Info"> [OUT] Information about the entire set of results </param>
        /// <param name="Paged_Results"> [OUT] List of search results for the requested page of results </param>
        public void Get_Search_Results(Navigation_Object Current_Mode,
                                       Item_Aggregation Aggregation_Object,
                                       List<string> Search_Stop_Words,
                                       User_Object Current_User,
                                       Custom_Tracer Tracer,
                                       out Search_Results_Statistics Complete_Result_Set_Info,
                                       out List<iSearch_Title_Result> Paged_Results,
                                       HttpContext context = null)
        {
            Tracer?.Add_Trace("SobekCM_Assistant.Get_Search_Results", String.Empty);

            // Set output initially to null
            Paged_Results = null;
            Complete_Result_Set_Info = null;

            // Get the sort
            int sort = Current_Mode.Sort.HasValue ? Math.Max(Current_Mode.Sort.Value, ((ushort)1)) : 0;
            if ((sort != 0) && (sort != 1) && (sort != 2) && (sort != 10) && (sort != 11))
                sort = 0;


            // Depending on type of search, either go to database or Greenstone
            if (Current_Mode.Search_Type == Search_Type_Enum.Map)
            {
                // If this is showing in the map, only allow sot zero, which is by coordinates
                if ((Current_Mode.Result_Display_Type == "map") || (String.IsNullOrEmpty(Current_Mode.Result_Display_Type)))
                {
                    Current_Mode.Sort = 0;
                    sort = 0;
                }

                try
                {
                    double lat1 = 1000;
                    double long1 = 1000;
                    double lat2 = 1000;
                    double long2 = 1000;
                    double pointBufferKm = 0;
                    string[] terms = Current_Mode.Coordinates.Split(",".ToCharArray());
                    if (terms.Length < 2)
                    {
                        Current_Mode.Mode = Display_Mode_Enum.Search;
                        UrlWriterHelper.Redirect(Current_Mode, context);
                        return;
                    }
                    if (terms.Length < 4)
                    {
                        lat1 = Convert.ToDouble(terms[0]);
                        lat2 = lat1;
                        long1 = Convert.ToDouble(terms[1]);
                        long2 = long1;
                    }
                    if (terms.Length >= 4)
                    {
                        if (terms[0].Length > 0)
                            lat1 = Convert.ToDouble(terms[0]);
                        if (terms[1].Length > 0)
                            long1 = Convert.ToDouble(terms[1]);
                        if (terms[2].Length > 0)
                            lat2 = Convert.ToDouble(terms[2]);
                        if (terms[3].Length > 0)
                            long2 = Convert.ToDouble(terms[3]);
                    }

                    // An optional fifth term specifies a buffer (in kilometers) to search around a single
                    // point, rather than requiring an exact coordinate match.  Ignored for a rectangle search.
                    if ((terms.Length >= 5) && (terms[4].Length > 0))
                        Double.TryParse(terms[4], out pointBufferKm);

                    // If neither point is valid, return
                    if (((lat1 == 1000) || (long1 == 1000)) && ((lat2 == 1000) || (long2 == 1000)))
                    {
                        Current_Mode.Mode = Display_Mode_Enum.Search;
                        UrlWriterHelper.Redirect(Current_Mode, context);
                        return;
                    }

                    // If just the first point is valid, use that
                    if ((lat2 == 1000) || (long2 == 1000))
                    {
                        lat2 = lat1;
                        long2 = long1;
                    }

                    // If just the second point is valid, use that
                    if ((lat1 == 1000) || (long1 == 1000))
                    {
                        lat1 = lat2;
                        long1 = long2;
                    }

                    // Perform the coordinate search against solr
                    try
                    {
                        // Get the page count in the results
                        int current_page_index = Current_Mode.Page.HasValue ? Math.Max(Current_Mode.Page.Value, ((ushort)1)) : 1;

                        Search_User_Membership_Info userInfo = Build_User_Membership_Info(Current_User, Aggregation_Object.Code);

                        var searchOptions = new Search_Options_Info
                        {
                            Page = current_page_index,
                            ResultsPerPage = 20,
                            AggregationCode = Aggregation_Object.Code,
                            Facets = Aggregation_Object.Facets,
                            Fields = Aggregation_Object.Results_Fields,
                            Sort = (ushort)sort,
                            GroupItemsByTitle = Aggregation_Object.GroupResults
                        };

                        // A rectangle whose corners are identical is really just a point search
                        bool isPointSearch = (lat1 == lat2) && (long1 == long2);
                        double? lat2Param = isPointSearch ? (double?)null : lat2;
                        double? long2Param = isPointSearch ? (double?)null : long2;

                        v5_Solr_Searcher.Coordinate_Search(lat1, long1, lat2Param, long2Param, pointBufferKm, searchOptions, userInfo, Tracer, out Search_Results_Statistics recomputed_search_statistics, out Paged_Results);
                        Complete_Result_Set_Info = recomputed_search_statistics;
                    }
                    catch (Exception ee)
                    {
                        // Next, show the message to the user
                        Current_Mode.Mode = Display_Mode_Enum.Error;
                        string error_message = ee.Message;
                        if (error_message.ToUpper().IndexOf("TIMEOUT") >= 0)
                        {
                            error_message = "Database Timeout Occurred<br /><br />Try again in a few minutes.<br /><br />";
                        }
                        Current_Mode.Error_Message = error_message;
                        Current_Mode.Caught_Exception = ee;
                    }
                }
                catch
                {
                    Current_Mode.Mode = Display_Mode_Enum.Search;
                    UrlWriterHelper.Redirect(Current_Mode, context);
                }
            }
            else
            {
                var terms = new List<string>();
                var web_fields = new List<string>();

                // Split the terms correctly 
                Split_Clean_Search_Terms_Fields(Current_Mode.Search_String, Current_Mode.Search_Fields, Current_Mode.Search_Type, terms, web_fields, null, Current_Mode.Search_Precision, ',');

                // Get the count that will be used
                int actualCount = Math.Min(terms.Count, web_fields.Count);

                // Determine if this is a special search type which returns more rows and is not cached.
                // This is used to return the results as XML and DATASET
                bool special_search_type = false;
                int results_per_page = 20;

                if ((Current_Mode.Writer_Type == Writer_Type_Enum.XML) || (Current_Mode.Writer_Type == Writer_Type_Enum.DataSet))
                {
                    results_per_page = 1000000;
                    special_search_type = true;
                    sort = 2; // Sort by BibID always for these
                }
                if (String.Equals(Current_Mode.Result_Display_Type, "timeline", StringComparison.OrdinalIgnoreCase))
                {
                    Tracer.Add_Trace("Get_Search_Results", "Is timeline, setting results_per_page and sort.");

                    results_per_page = 1000;
                    sort = 10;
                }

                // Get any included date range
                Nullable<DateTime> date_start = null;
                Nullable<DateTime> date_end = null;
                if (Current_Mode.DateRange_Date1.HasValue) date_start = Current_Mode.DateRange_Date1.Value;
                else if (Current_Mode.DateRange_Year1.HasValue) date_start = new DateTime(Current_Mode.DateRange_Year1.Value, 1, 1);
                if (Current_Mode.DateRange_Date2.HasValue) date_end = Current_Mode.DateRange_Date2.Value;
                else if (Current_Mode.DateRange_Year2.HasValue) date_end = new DateTime(Current_Mode.DateRange_Year2.Value, 12, 31);


                // Set the flags for how much data is needed.  (i.e., do we need to pull ANYTHING?  or
                // perhaps just the next page of results ( as opposed to pulling facets again).
                bool need_search_statistics = true;
                bool need_paged_results = true;
                if ((!special_search_type) && (Current_User == null))
                {
                    // Look to see if the search statistics are available on any cache..
                    Complete_Result_Set_Info = CachedDataManager.Retrieve_Search_Result_Statistics(Current_Mode, actualCount, web_fields, terms, date_start, date_end, Tracer);
                    if (Complete_Result_Set_Info != null)
                        need_search_statistics = false;

                    // Look to see if the paged results are available on any cache..
                    Paged_Results = CachedDataManager.Retrieve_Search_Results(Current_Mode, sort, actualCount, web_fields, terms, date_start, date_end, results_per_page, Tracer);
                    if (Paged_Results != null)
                        need_paged_results = false;
                }

                // If both were retrieved, do nothing else
                if ((need_paged_results) || (need_search_statistics))
                {
                    // Should this pull the search from the database, or from greenstone?
                    if ((Current_Mode.Search_Type == Search_Type_Enum.Full_Text) || (Current_Mode.Search_Fields.IndexOf("TX") >= 0))
                    {
                        try
                        {
                            // Get the page count in the results
                            int current_page_index = Current_Mode.Page.HasValue ? Math.Max(Current_Mode.Page.Value, ((ushort)1)) : 1;

                            // Perform the search against solr
                            Search_Results_Statistics recomputed_search_statistics;
                            Perform_Solr_Search(Tracer, terms, web_fields, date_start, date_end, Aggregation_Object, current_page_index, sort, results_per_page, Current_User, out recomputed_search_statistics, out Paged_Results, need_search_statistics);
                            if (need_search_statistics)
                                Complete_Result_Set_Info = recomputed_search_statistics;
                        }
                        catch (Exception ee)
                        {
                            Current_Mode.Mode = Display_Mode_Enum.Error;
                            Current_Mode.Error_Message = "Unable to perform search at this time";
                            Current_Mode.Caught_Exception = ee;
                        }

                        // If this was a special search, don't cache this
                        if ((!special_search_type) && (Current_User == null))
                        {
                            // Cache the search statistics, if it was needed
                            if ((need_search_statistics) && (Complete_Result_Set_Info != null))
                            {
                                CachedDataManager.Store_Search_Result_Statistics(Current_Mode, actualCount, web_fields, terms, date_start, date_end, Complete_Result_Set_Info, Tracer);
                            }

                            // Cache the search results
                            if ((need_paged_results) && (Paged_Results != null))
                            {
                                CachedDataManager.Store_Search_Results(Current_Mode, sort, actualCount, web_fields, terms, date_start, date_end, results_per_page, Paged_Results, Tracer);
                            }
                        }
                    }
                    else
                    {
                        // Try to pull more than one page, so we can cache the next page or so
                        var pagesOfResults = new List<List<iSearch_Title_Result>>();

                        // Perform the search against the database
                        try
                        {
                            Search_Results_Statistics recomputed_search_statistics;

                            // Get the page count in the results
                            int current_page_index = Current_Mode.Page.HasValue ? Math.Max(Current_Mode.Page.Value, ((ushort)1)) : 1;

                            // Perform the solr search
                            Perform_Solr_Search(Tracer, terms, web_fields, date_start, date_end, Aggregation_Object, current_page_index, sort, results_per_page, Current_User, out recomputed_search_statistics, out Paged_Results, need_search_statistics);

                            if (need_search_statistics)
                                Complete_Result_Set_Info = recomputed_search_statistics;

                            // if ((pagesOfResults != null) && (pagesOfResults.Count > 0))
                            //     Paged_Results = pagesOfResults[0];
                        }
                        catch (Exception ee)
                        {
                            // Next, show the message to the user
                            Current_Mode.Mode = Display_Mode_Enum.Error;
                            string error_message = ee.Message;
                            if (error_message.ToUpper().IndexOf("TIMEOUT") >= 0)
                            {
                                error_message = "Database Timeout Occurred<br /><br />Try narrowing your search by adding more terms <br />or putting quotes around your search.<br /><br />";
                            }
                            Current_Mode.Error_Message = error_message;
                            Current_Mode.Caught_Exception = ee;
                        }

                        // If this was a special search, don't cache this
                        if ((!special_search_type) && (Current_User == null))
                        {
                            // Cache the search statistics, if it was needed
                            if ((need_search_statistics) && (Complete_Result_Set_Info != null))
                            {
                                CachedDataManager.Store_Search_Result_Statistics(Current_Mode, actualCount, web_fields, terms, date_start, date_end, Complete_Result_Set_Info, Tracer);
                            }

                            // Cache the search results
                            if ((need_paged_results) && (pagesOfResults != null) && (pagesOfResults.Count > 0))
                            {
                                // CachedDataManager.Store_Search_Results(Current_Mode, sort, actualCount, web_fields, terms, date1, date2, pagesOfResults, Tracer);

                                CachedDataManager.Store_Search_Results(Current_Mode, sort, actualCount, web_fields, terms, date_start, date_end, results_per_page, pagesOfResults, Tracer);
                            }
                        }
                    }
                }
            }


            ////create search results json object and place into session state
            //DataTable TEMPsearchResults = new DataTable();
            //TEMPsearchResults.Columns.Add("BibID", typeof(string));
            //TEMPsearchResults.Columns.Add("Spatial_Coordinates", typeof(string));
            //foreach (iSearch_Title_Result searchTitleResult in Paged_Results)
            //{
            //	TEMPsearchResults.Rows.Add(searchTitleResult.BibID, searchTitleResult.Spatial_Coordinates);
            //}
            //HttpContext.Current.Session["TEMPSearchResultsJSON"] = Google_Map_ResultsViewer_Beta.Create_JSON_Search_Results_Object(TEMPsearchResults);
        }

        /// <summary> Takes the search string and search fields from the URL and parses them, according to the search type,
        /// into a collection of terms and a collection of fields. Stop words are also suppressed here </summary>
        /// <param name="Search_String">Search string from the SobekCM search results URL </param>
        /// <param name="Search_Fields">Search fields from the SobekCM search results URL </param>
        /// <param name="Search_Type"> Type of search currently being performed (sets how it is parsed and default index)</param>
        /// <param name="Output_Terms"> List takes the results of the parsing of the actual search terms </param>
        /// <param name="Output_Fields"> List takes the results of the parsing of the actual (and implied) search fields </param>
        /// <param name="Search_Stop_Words"> List of all stop words ignored during metadata searching (such as 'The', 'A', etc..) </param>
        /// <param name="Search_Precision"> Search precision for this search ( i.e., exact, contains, stemmed, thesaurus lookup )</param>
        /// <param name="Delimiter_Character"> Character used as delimiter between different components of an advanced search</param>
        public static void Split_Clean_Search_Terms_Fields(string Search_String, string Search_Fields, Search_Type_Enum Search_Type, List<string> Output_Terms, List<string> Output_Fields, List<string> Search_Stop_Words, Search_Precision_Type_Enum Search_Precision, char Delimiter_Character)
        {
            // Find default index
            string default_index = "ZZ";
            if (Search_Type == Search_Type_Enum.Full_Text)
                default_index = "TX";

            // Split the parts
            string[] fieldSplitTemp = Search_Fields.Split(new[] { Delimiter_Character });
            var fieldSplit = new List<string>();
            var searchSplit = new List<string>();
            int first_index = 0;
            int second_index = 0;
            int field_index = 0;
            bool in_quotes = false;
            while (second_index < Search_String.Length)
            {
                if (in_quotes)
                {
                    if (Search_String[second_index] == '"')
                    {
                        in_quotes = false;
                    }
                }
                else
                {
                    if (Search_String[second_index] == '"')
                    {
                        in_quotes = true;
                    }
                    else
                    {
                        if (Search_String[second_index] == Delimiter_Character)
                        {
                            if (first_index < second_index)
                            {
                                string possible_add = Search_String.Substring(first_index, second_index - first_index);
                                if (possible_add.Trim().Length > 0)
                                {
                                    searchSplit.Add(possible_add);
                                    fieldSplit.Add(field_index < fieldSplitTemp.Length ? fieldSplitTemp[field_index] : default_index);
                                }
                            }
                            first_index = second_index + 1;
                            field_index++;
                        }
                        else if (Search_String[second_index] == ' ')
                        {
                            if (first_index < second_index)
                            {
                                string possible_add = Search_String.Substring(first_index, second_index - first_index);
                                if (possible_add.Trim().Length > 0)
                                {
                                    searchSplit.Add(possible_add);
                                    fieldSplit.Add(field_index < fieldSplitTemp.Length ? fieldSplitTemp[field_index] : default_index);
                                }
                            }
                            first_index = second_index + 1;
                        }
                    }
                }
                second_index++;
            }
            if (second_index > first_index)
            {
                searchSplit.Add(Search_String.Substring(first_index));
                fieldSplit.Add(field_index < fieldSplitTemp.Length ? fieldSplitTemp[field_index] : default_index);
            }

            // If this is basic, do some other preparation
            if (Search_Type == Search_Type_Enum.Full_Text)
            {
                v5_Solr_Searcher.Split_Multi_Terms(Search_String, default_index, Output_Terms, Output_Fields);
            }
            else
            {
                // For advanced, just add all the terms
                Output_Terms.AddRange(searchSplit.Select(ThisTerm => ThisTerm.Trim().Replace("\"", "").Replace("+", " ")));
                Output_Fields.AddRange(fieldSplit.Select(ThisField => ThisField.Trim()));
            }

            // Some special work for basic searches here
            if (Search_Type == Search_Type_Enum.Basic)
            {
                while (Output_Fields.Count < Output_Terms.Count)
                {
                    Output_Fields.Add("ZZ");
                }
            }

            // Now, remove any stop words by themselves
            if (Search_Stop_Words != null)
            {
                int index = 0;
                while ((index < Output_Terms.Count) && (index < Output_Fields.Count))
                {
                    if ((Output_Terms[index].Length == 0) || (Search_Stop_Words.Contains(Output_Terms[index].ToLower())))
                    {
                        Output_Terms.RemoveAt(index);
                        Output_Fields.RemoveAt(index);
                    }
                    else
                    {
                        if (Search_Precision != Search_Precision_Type_Enum.Exact_Match)
                        {
                            Output_Terms[index] = Output_Terms[index].Replace("\"", "").Replace("+", " ").Replace("&amp;", " ").Replace("&", "");
                        }
                        if (Output_Fields[index].Length == 0)
                            Output_Fields[index] = default_index;
                        index++;
                    }
                }
            }
        }


        /// <summary> Builds the user membership information used to determine which items a user can discover in Solr </summary>
        /// <param name="Current_User"> Current user, who may or may not be logged on </param>
        /// <param name="AggregationCode"> Code for the aggregation currently being searched, used to check aggregation-level admin/curator rights </param>
        /// <returns> Search user membership info reflecting this user's login state, groups, and admin status </returns>
        private static Search_User_Membership_Info Build_User_Membership_Info(User_Object Current_User, string AggregationCode)
        {
            var userInfo = new Search_User_Membership_Info();
            if ((Current_User == null) || (!Current_User.LoggedOn))
            {
                userInfo.LoggedIn = false;
            }
            else
            {
                userInfo.LoggedIn = true;
                userInfo.UserID = userInfo.UserID;
                if (Current_User.User_Groups != null)
                {
                    foreach (Simple_User_Group_Info groupInfo in Current_User.User_Groups)
                    {
                        userInfo.Add_User_Group(groupInfo.UserGroupID);
                    }
                }
                if ((Current_User.Is_Host_Admin) || (Current_User.Is_System_Admin) || (Current_User.Is_Portal_Admin))
                    userInfo.Admin = true;
                else if ((Current_User.Is_Aggregation_Admin(AggregationCode)) || (Current_User.Is_Aggregation_Curator(AggregationCode)))
                {
                    userInfo.Admin = true;
                }
            }

            return userInfo;
        }

        private static void Perform_Solr_Search(Custom_Tracer Tracer, List<string> Terms, List<string> Web_Fields, Nullable<DateTime> StartDate, Nullable<DateTime> EndDate, Item_Aggregation Current_Aggregation, int Current_Page, int Current_Sort, int Results_Per_Page, User_Object Current_User, out Search_Results_Statistics Complete_Result_Set_Info, out List<iSearch_Title_Result> Paged_Results, bool Need_Search_Statistics)
        {
            Tracer?.Add_Trace("SobekCM_Assistant.Perform_Solr_Search", "Build the Solr query");

            // Build the user membership information
            Search_User_Membership_Info userInfo = Build_User_Membership_Info(Current_User, Current_Aggregation.Code);

            // Build the search options
            var searchOptions = new Search_Options_Info();
            searchOptions.Page = Current_Page;
            searchOptions.ResultsPerPage = Results_Per_Page;
            searchOptions.AggregationCode = Current_Aggregation.Code;
            searchOptions.Facets = Current_Aggregation.Facets;
            searchOptions.Fields = Current_Aggregation.Results_Fields;
            searchOptions.Sort = (ushort)Current_Sort;

            // Should results be grouped?  Aggregation must be set and for the moment, full text
            // must have been NOT searched
            bool contains_full_text = false;
            foreach (string field in Web_Fields)
            {
                if (field.IndexOf("TX", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    contains_full_text = true;
                    break;
                }
            }
            searchOptions.GroupItemsByTitle = (Current_Aggregation.GroupResults && !contains_full_text);
            searchOptions.IncludeFullTextSnippets = contains_full_text;

            v5_Solr_Searcher.Search(Terms, Web_Fields, null, null, searchOptions, userInfo, Tracer, out Complete_Result_Set_Info, out Paged_Results);
        }

        #endregion

        #region Method to get the html skin

        /// <summary> Gets the HTML skin indicated in the current navigation mode </summary>
        /// <param name="Current_Mode"> Mode / navigation information for the current request</param>
        /// <param name="Skin_Collection"> Collection of the most common skins and source information for all the skins made on the fly </param>
        /// <param name="Cache_On_Build"> Flag indicates if this should be added to the ASP.net (or caching server) cache </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> Fully-built object used to "skin" this digital library </returns>
        public Web_Skin_Object Get_HTML_Skin(Navigation_Object Current_Mode, Web_Skin_Collection Skin_Collection, bool Cache_On_Build, Custom_Tracer Tracer)
        {
            return Get_HTML_Skin(Current_Mode.Skin, Current_Mode, Skin_Collection, Cache_On_Build, Tracer);
        }

        /// <summary> Gets the HTML skin indicated in the current navigation mode </summary>
        /// <param name="Web_Skin_Code"> Web skin code </param>
        /// <param name="Current_Mode"> Mode / navigation information for the current request</param>
        /// <param name="Skin_Collection"> Collection of the most common skins and source information for all the skins made on the fly </param>
        /// <param name="Cache_On_Build"> Flag indicates if this should be added to the ASP.net (or caching server) cache </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> Fully-built object used to "skin" this digital library </returns>
        public Web_Skin_Object Get_HTML_Skin(string Web_Skin_Code, Navigation_Object Current_Mode, Web_Skin_Collection Skin_Collection, bool Cache_On_Build, Custom_Tracer Tracer)
        {
            // Get the interface object
            Web_Skin_Object htmlSkin = SobekEngineClient.WebSkins.Get_LanguageSpecific_Web_Skin(Web_Skin_Code, Current_Mode.Language, UI_ApplicationCache_Gateway.Settings.System.Default_UI_Language, Cache_On_Build, Tracer);

            // If there is still no interface, this is an ERROR
            if (htmlSkin != null)
            {
                if ((!String.IsNullOrEmpty(htmlSkin.Base_Skin_Code)) && (htmlSkin.Base_Skin_Code != htmlSkin.Skin_Code))
                    Current_Mode.Base_Skin = htmlSkin.Base_Skin_Code;
            }
            else
            {
                Tracer.Add_Trace("SobekCM_Assistant.Get_HTML_Skin", "SobekEngineClient returned NULL for the requested web skin");
            }

            // Return the value
            return htmlSkin;
        }

        #endregion
    }
}
