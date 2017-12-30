#region Using directives

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using SobekCM.Core.Aggregations;
using SobekCM.Core.Client;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.Navigation;
using SobekCM.Core.Results;
using SobekCM.Core.Search;
using SobekCM.Core.Settings;
using SobekCM.Core.SiteMap;
using SobekCM.Core.Skins;
using SobekCM.Core.Users;
using SobekCM.Core.WebContent;
using SobekCM.Engine_Library.Aggregations;
using SobekCM.Engine_Library.Database;
using SobekCM.Engine_Library.SiteMap;
using SobekCM.Engine_Library.Solr.Legacy;
using SobekCM.Engine_Library.Solr.v5;
using SobekCM.Library.Database;
using SobekCM.Library.UI;
using SobekCM.Tools;

#endregion

namespace SobekCM.Library
{
    /// <summary> Class is a helper class that pulls much of the data needed for the processing of requests.  Tries to retrieve
    /// from the cache, and if the data is not there, it will then build the object and try to store on the cache  </summary>
    public class SobekCM_Assistant
    {
        #region Method to retrieve simple web content text to view within a skin

        /// <summary> Gets the simple CMS/info object and text to display </summary>
        /// <param name="Current_Mode"> Mode / navigation information for the current request</param>
        /// <param name="Base_Directory"> Base directory location under which the the CMS/info source file will be found</param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <param name="Simple_Web_Content"> [OUT] Built browse object which contains information like title, banner, etc.. and the entire text to be displayed </param>
        /// <param name="Site_Map"> [OUT] Optional navigational site map object related to this page </param>
        /// <returns>TRUE if successful, otherwise FALSE </returns>
        /// <remarks> This always pulls the data directly from disk; this text is not cached. </remarks>
        public bool Get_Simple_Web_Content_Text(Navigation_Object Current_Mode, string Base_Directory, Custom_Tracer Tracer, out HTML_Based_Content Simple_Web_Content, out SobekCM_SiteMap Site_Map ) 
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("SobekCM_Assistant.Get_Simple_Web_Content_Text", String.Empty);
            }

            Site_Map = null;
            Simple_Web_Content = null;

            // Get the web content object
            if ((( Current_Mode.WebContentID.HasValue ) && ( Current_Mode.WebContentID.Value > 0 )) && (( !Current_Mode.Missing.HasValue ) || ( !Current_Mode.Missing.Value )))
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

            if ( String.IsNullOrEmpty(Simple_Web_Content.Content))
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
                        if (Tracer != null)
                        {
                            Tracer.Add_Trace("SobekCM_Assistant.Get_Simple_Web_Content_Text", "Reading site map file");
                        }

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
                        if (Tracer != null)
                        {
                            Tracer.Add_Trace("SobekCM_Assistant.Get_Simple_Web_Content_Text", "Reading site map file");
                        }

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
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <param name="Complete_Result_Set_Info"> [OUT] Information about the entire set of results </param>
        /// <param name="Paged_Results"> [OUT] List of search results for the requested page of results </param>
        /// <returns> TRUE if successful, otherwise FALSE </returns>
        /// <remarks> This attempts to pull the objects from the cache.  If unsuccessful, it builds the objects from the
        /// database and hands off to the <see cref="CachedDataManager" /> to store in the cache </remarks>
        public bool Get_User_Folder( string Folder_Name, int User_ID, int Results_Per_Page, int ResultsPage, Custom_Tracer Tracer, out Search_Results_Statistics Complete_Result_Set_Info, out List<iSearch_Title_Result> Paged_Results )
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("SobekCM_Assistant.Get_User_Folder", String.Empty);
            }

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
                if (Tracer != null)
                {
                    Tracer.Add_Trace("SobekCM_Assistant.Get_User_Folder", "Browse statistics and paged results retrieved from cache");
                }
            }
            else
            {
                if (Tracer != null)
                {
                    Tracer.Add_Trace("SobekCM_Assistant.Get_User_Folder", "Building results information");
                }


                Single_Paged_Results_Args returnArgs = Engine_Database.Get_User_Folder_Browse(User_ID, Folder_Name, Results_Per_Page, ResultsPage, false, new List<short>(), need_browse_statistics, Tracer);
                if (need_browse_statistics)
                {
                    Complete_Result_Set_Info = returnArgs.Statistics;
                }
                Paged_Results = returnArgs.Paged_Results;

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
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <param name="Folder_Info"> [OUT] Information about this public user folder including name and owner</param>
        /// <param name="Complete_Result_Set_Info"> [OUT] Information about the entire set of results </param>
        /// <param name="Paged_Results"> [OUT] List of search results for the requested page of results </param>
        /// <returns> TRUE if successful, otherwise FALSE </returns>
        /// <remarks> This attempts to pull the objects from the cache.  If unsuccessful, it builds the objects from the
        /// database and hands off to the <see cref="CachedDataManager" /> to store in the cache </remarks>
        public bool Get_Public_User_Folder(int UserFolderID, int ResultsPage, Custom_Tracer Tracer, out Public_User_Folder Folder_Info, out Search_Results_Statistics Complete_Result_Set_Info, out List<iSearch_Title_Result> Paged_Results)
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("SobekCM_Assistant.Get_Public_User_Folder", String.Empty);
            }

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
                if (Tracer != null)
                {
                    Tracer.Add_Trace("SobekCM_Assistant.Get_User_Folder", "Browse statistics and paged results retrieved from cache");
                }
            }
            else
            {
                if (Tracer != null)
                {
                    Tracer.Add_Trace("SobekCM_Assistant.Get_User_Folder", "Building results information");
                }

                Single_Paged_Results_Args returnArgs = Engine_Database.Get_Public_Folder_Browse(UserFolderID, 20, ResultsPage, false, new List<short>(), need_browse_statistics, Tracer);
                if (need_browse_statistics)
                {
                    Complete_Result_Set_Info = returnArgs.Statistics;
                }
                Paged_Results = returnArgs.Paged_Results;

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

        #region Method to get a browse or info object, table, or text

        /// <summary> Gets the browse or info object and any other needed data for display ( resultset or text to display) </summary>
        /// <param name="Current_Mode"> Mode / navigation information for the current request</param>
        /// <param name="Aggregation_Object"> Item Aggregation object</param>
        /// <param name="Base_Directory"> Base directory location under which the the CMS/info source file will be found</param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <param name="Browse_Object"> [OUT] Stores all the information about this browse or info </param>
        /// <param name="Complete_Result_Set_Info"> [OUT] Information about the entire set of results </param>
        /// <param name="Paged_Results"> [OUT] List of search results for the requested page of results </param>
        /// <param name="Browse_Info_Display_Text"> [OUT] Static HTML-based content to be displayed if this is browing a staticly created html source file </param>
        /// <returns> TRUE if successful, otherwise FALSE </returns>
        /// <remarks> This attempts to pull the objects from the cache.  If unsuccessful, it builds the objects from the
        /// database and hands off to the <see cref="CachedDataManager" /> to store in the cache </remarks>
        public bool Get_Browse_Info(Navigation_Object Current_Mode,
                                    Item_Aggregation Aggregation_Object,
                                    string Base_Directory,
                                    Custom_Tracer Tracer,
                                    out Item_Aggregation_Child_Page Browse_Object,
                                    out Search_Results_Statistics Complete_Result_Set_Info,
                                    out List<iSearch_Title_Result> Paged_Results,
                                    out HTML_Based_Content Browse_Info_Display_Text )
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("SobekCM_Assistant.Get_Browse_Info", "RRB: Start of Get_Browse_Info...");
            }

            // Set output initially to null
            Browse_Object = null;
            Paged_Results = null;
            Complete_Result_Set_Info = null;
            Browse_Info_Display_Text = null;

            // First, make sure the browse submode is valid
            if ((Aggregation_Object.ID == -1) && (Current_Mode.Mode == Display_Mode_Enum.Simple_HTML_CMS))
            {
                string source = Base_Directory + "design\\info";
                string[] matching_file = Directory.GetFiles(source, Current_Mode.Info_Browse_Mode + ".*");
                if (matching_file.Length > 0)
                {
                    Browse_Object = new Item_Aggregation_Child_Page( Item_Aggregation_Child_Visibility_Enum.None, Item_Aggregation_Child_Source_Data_Enum.Static_HTML, Current_Mode.Info_Browse_Mode, matching_file[0], Current_Mode.Info_Browse_Mode);
                }
            }
            else
            {
                Browse_Object = Aggregation_Object.Child_Page_By_Code(Current_Mode.Info_Browse_Mode);
            }
            if (Browse_Object == null)
            {
                Current_Mode.Error_Message = "Unable to retrieve browse/info item '" + Current_Mode.Info_Browse_Mode + "'";
                return false;
            }

            // Is this a table result, or a string?
            switch (Browse_Object.Source_Data_Type)
            {
                case Item_Aggregation_Child_Source_Data_Enum.Database_Table:

                    Tracer.Add_Trace("SobekCM_Assistant.Get_Browse_Info", "Browse_Object.Source_Data_Type is Item_Aggregation_Child_Source_Data_Enum.Database_Table.");

                    // Set the current sort to ZERO, if currently set to ONE and this is an ALL BROWSE.
                    // Those two sorts are the same in this case
                    int sort = Current_Mode.Sort.HasValue ? Math.Max(Current_Mode.Sort.Value, ((ushort)1)) : 1;
                    if ((sort == 0) && (Browse_Object.Code == "all"))
                        sort = 1;

                    // Special code if this is a JSON browse
                    string browse_code = Current_Mode.Info_Browse_Mode;
                    if (Current_Mode.Writer_Type == Writer_Type_Enum.JSON)
                    {
                        browse_code = browse_code + "_JSON";
                        sort = 12;
                    }

                    // Get the page count in the results
                    int current_page_index = Current_Mode.Page.HasValue ? Math.Max(Current_Mode.Page.Value, ((ushort)1)) : 1;

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

                    Tracer.Add_Trace("SobekCM_Assistant.Get_Browse_Info", "Current_Mode.Writer_Type=[" + Current_Mode.Writer_Type.ToString() + "].");
                    Tracer.Add_Trace("SobekCM_Assistant.Get_Browse_Info", "Current_Mode.Results_Display_Type=[" + Current_Mode.Result_Display_Type + "].");

                    if (String.Equals(Current_Mode.Result_Display_Type, "timeline", StringComparison.OrdinalIgnoreCase))
                    {
                        Tracer.Add_Trace("SobekCM_Assistant.Get_Browse_Info", "Is timeline, setting browse results_per_page and sort.");

                        results_per_page = 1000;
                        sort = 10;
                    }

                    // Set the flags for how much data is needed.  (i.e., do we need to pull ANYTHING?  or
                    // perhaps just the next page of results ( as opposed to pulling facets again).
                    bool need_browse_statistics = true;
                    bool need_paged_results = true;
                    if (!special_search_type)
                    {
                        // Look to see if the browse statistics are available on any cache for this browse
                        Complete_Result_Set_Info = CachedDataManager.Retrieve_Browse_Result_Statistics(Aggregation_Object.Code, browse_code, Tracer);
                        if (Complete_Result_Set_Info != null)
                            need_browse_statistics = false;

                        // Look to see if the paged results are available on any cache..
                        Paged_Results = CachedDataManager.Retrieve_Browse_Results(Aggregation_Object.Code, browse_code, current_page_index, sort, (uint)results_per_page,Tracer);
                        if (Paged_Results != null)
                            need_paged_results = false;
                    }

                    // Was a copy found in the cache?
                    if ((!need_browse_statistics) && ( !need_paged_results ))
                    {
                        if (Tracer != null)
                        {
                            Tracer.Add_Trace("SobekCM_Assistant.Get_Browse_Info", "Browse statistics and paged results retrieved from cache");
                        }
                    }
                    else
                    {
                        if (Tracer != null)
                        {
                            Tracer.Add_Trace("SobekCM_Assistant.Get_Browse_Info", "Building results information");
                        }

                        // Try to pull more than one page, so we can cache the next page or so
                        List<List<iSearch_Title_Result>> pagesOfResults;

                        // Get from the hierarchy object
                        if (Current_Mode.Writer_Type == Writer_Type_Enum.JSON)
                        {
                            List<short> facetsList = new List<short>();
                            foreach(Complete_Item_Aggregation_Metadata_Type facet in Aggregation_Object.Facets)
                                facetsList.Add(facet.ID);

                            Multiple_Paged_Results_Args returnArgs = Engine_Database.Get_Item_Aggregation_Browse_Paged(Current_Mode.Aggregation, "1900-01-01", false, 20, current_page_index, 0, need_browse_statistics, facetsList, need_browse_statistics, Tracer);
                            if (need_browse_statistics)
                            {
                                Complete_Result_Set_Info = returnArgs.Statistics;
                            }
                            pagesOfResults = returnArgs.Paged_Results;
                            if ((pagesOfResults != null) && (pagesOfResults.Count > 0))
                                Paged_Results = pagesOfResults[0];
                        }
                        else
                        {
                            Multiple_Paged_Results_Args returnArgs = Item_Aggregation_Utilities.Get_Browse_Results(Aggregation_Object, Browse_Object, current_page_index, sort, results_per_page, !special_search_type, need_browse_statistics, Tracer);
                            if (need_browse_statistics)
                            {
                                Complete_Result_Set_Info = returnArgs.Statistics;
                            }
                            pagesOfResults = returnArgs.Paged_Results;
                            if ((pagesOfResults != null) && (pagesOfResults.Count > 0))
                                Paged_Results = pagesOfResults[0];
                        }

                        // Save the overall result set statistics to the cache if something was pulled
                        if (!special_search_type)
                        {
                            if ((need_browse_statistics) && (Complete_Result_Set_Info != null))
                            {
                                CachedDataManager.Store_Browse_Result_Statistics(Aggregation_Object.Code, browse_code, Complete_Result_Set_Info, Tracer);
                            }

                            // Save the overall result set statistics to the cache if something was pulled
                            if ((need_paged_results) && (Paged_Results != null))
                            {
                                CachedDataManager.Store_Browse_Results(Aggregation_Object.Code, browse_code, current_page_index, sort, (uint)results_per_page,pagesOfResults, Tracer);
                            }
                        }
                    }
                    break;

                case Item_Aggregation_Child_Source_Data_Enum.Static_HTML:
                    Tracer.Add_Trace("SobekCM_Assistant.Get_Browse_Info", "Browse_Object.Source_Data_Type is Item_Aggregation_Child_Source_Data_Enum.Static_HTML.");
                    Browse_Info_Display_Text = SobekEngineClient.Aggregations.Get_Aggregation_HTML_Child_Page(Aggregation_Object.Code, Aggregation_Object.Language, UI_ApplicationCache_Gateway.Settings.System.Default_UI_Language, Browse_Object.Code, Tracer);
                    break;
            }
            return true;
        }

        #endregion

        #region Method to pull the static HTML for an item when a robot requests a single item for view

        ///// <summary> Find the static html file to display for an item view request, when requested by a search engine robot for indexing </summary>
        ///// <param name="Current_Mode"> Mode / navigation information for the current request</param>
        ///// <param name="All_Items_Lookup"> Lookup object used to pull basic information about any item loaded into this library </param>
        ///// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        ///// <returns> Location for the static html file to display for this item view request </returns>
        //public string Get_Item_Static_HTML(Navigation_Object Current_Mode, Item_Lookup_Object All_Items_Lookup, Custom_Tracer Tracer)
        //{
        //    // Must at least have a bib id of the proper length
        //    if (Current_Mode.BibID.Length < 10)
        //    {
        //        Current_Mode.Invalid_Item = true;
        //        return String.Empty;
        //    }

        //    // Get the title object for this
        //    Multiple_Volume_Item dbTitle = All_Items_Lookup.Title_By_Bib(Current_Mode.BibID);
        //    if (dbTitle == null)
        //    {
        //        Current_Mode.Invalid_Item = true;
        //        return String.Empty;
        //    }

        //    // Try to get the very basic information about this item, to determine if the 
        //    // bib / vid combination is valid
        //    Single_Item selected_item = null;
        //    if (Current_Mode.VID.Length > 0)
        //    {
        //        selected_item = All_Items_Lookup.Item_By_Bib_VID(Current_Mode.BibID, Current_Mode.VID, Tracer);
        //    }
        //    else
        //    {
        //        if (dbTitle.Item_Count == 1)
        //        {
        //            selected_item = All_Items_Lookup.Item_By_Bib_Only(Current_Mode.BibID);
        //        }
        //    }

        //    // If no valid item and not a valid item group display either, return
        //    if (selected_item == null)
        //    {
        //        Current_Mode.Invalid_Item = true;
        //        return String.Empty;
        //    }

        //    // Set the title to the info browse, just to store it somewhere
        //    Current_Mode.Info_Browse_Mode = selected_item.Title;

        //    // Get the text for this item
        //    string bibid = Current_Mode.BibID;
        //    string vid = selected_item.VID.PadLeft(5, '0');
        //    Current_Mode.VID = vid;
        //    string base_image_url = UI_ApplicationCache_Gateway.Settings.Servers.Base_Data_Directory + bibid.Substring(0, 2) + "\\" + bibid.Substring(2, 2) + "\\" + bibid.Substring(4, 2) + "\\" + bibid.Substring(6, 2) + "\\" + bibid.Substring(8, 2) + "\\" + bibid + "_" + vid + ".html";
        //    return base_image_url;
        //}

        #endregion

        #region Method to pull an item for display (or edit)

        ///// <summary> Get a digital resource for display or for editing </summary>
        ///// <param name="Current_Mode"> Mode / navigation information for the current request</param>
        ///// <param name="All_Items_Lookup"> Lookup object used to pull basic information about any item loaded into this library </param>
        ///// <param name="Base_URL"> Base URL for all the digital resource files for items to display </param>
        ///// <param name="Icon_Table"> Dictionary of all the wordmark/icons which can be tagged to the items </param>
        ///// <param name="Current_User"> Currently logged on user information (used when editing an item)</param>
        ///// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        ///// <param name="Current_Item"> [OUT] Built single digital resource ready for displaying or editing </param>
        ///// <param name="Current_Page"> [OUT] Build current page for display </param>
        ///// <param name="Items_In_Title"> [OUT] List of all the items in this title </param>
        ///// <returns> TRUE if successful, otherwise FALSE </returns>
        ///// <remarks> This attempts to pull the objects from the cache.  If unsuccessful, it builds the objects from the
        ///// database and hands off to the <see cref="CachedDataManager" /> to store in the cache.  If the item must be 
        ///// built from scratch, the <see cref="SobekCM_Item_Factory"/> class is utilized. </remarks>
        //public bool Get_Item(Navigation_Object Current_Mode,
        //                     string Base_URL, 
        //                     Dictionary<string, Wordmark_Icon> Icon_Table,
        //                     User_Object Current_User,
        //                     Custom_Tracer Tracer, 
        //                     out SobekCM_Item Current_Item, 
        //                     out Page_TreeNode Current_Page,
        //                     out SobekCM_Items_In_Title Items_In_Title )
        //{
        //    return Get_Item(String.Empty, Current_Mode, All_Items_Lookup, Base_URL, Icon_Table, Tracer, Current_User, out Current_Item, out Current_Page, out Items_In_Title);
        //}

        ///// <summary> Get a digital resource for display or for editing </summary>
        ///// <param name="Collection_Code"> Collection code to which this item must belong </param>
        ///// <param name="Current_Mode"> Mode / navigation information for the current request</param>
        ///// <param name="All_Items_Lookup"> Lookup object used to pull basic information about any item loaded into this library </param>
        ///// <param name="Base_URL"> Base URL for all the digital resource files for items to display </param>
        ///// <param name="Icon_Table"> Dictionary of all the wordmark/icons which can be tagged to the items </param>
        ///// <param name="Current_User"> Currently logged on user information (used when editing an item)</param>
        ///// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        ///// <param name="Current_Item"> [OUT] Built single digital resource ready for displaying or editing </param>
        ///// <param name="Current_Page"> [OUT] Build current page for display </param>
        ///// <param name="Items_In_Title"> [OUT] List of all the items in this title </param>
        ///// <returns> TRUE if successful, otherwise FALSE </returns>
        ///// <remarks> This attempts to pull the objects from the cache.  If unsuccessful, it builds the objects from the
        ///// database and hands off to the <see cref="CachedDataManager" /> to store in the cache.  If the item must be 
        ///// built from scratch, the <see cref="SobekCM_Item_Factory"/> class is utilized. </remarks>
        //public bool Get_Item(string Collection_Code, 
        //                     Navigation_Object Current_Mode, 
        //                     Item_Lookup_Object All_Items_Lookup, 
        //                     string Base_URL, 
        //                     Dictionary<string, Wordmark_Icon> Icon_Table, 
        //                     Custom_Tracer Tracer, 
        //                     User_Object Current_User,
        //                     out SobekCM_Item Current_Item,
        //                     out Page_TreeNode Current_Page,
        //                     out SobekCM_Items_In_Title Items_In_Title)
        //{
        //    if (Tracer != null)
        //    {
        //        Tracer.Add_Trace("SobekCM_Assistant.Get_Item", String.Empty);
        //    }

        //    // Initially assign nulls
        //    Current_Item = null;
        //    Current_Page = null;
        //    Items_In_Title = null;

        //    // Check for legacy reference by itemid
        //    if ((Current_Mode.BibID.Length == 0) && (Current_Mode.ItemID_DEPRECATED.HasValue))
        //    {
        //        DataRow thisRowInfo = SobekCM_Database.Lookup_Item_By_ItemID(Current_Mode.ItemID_DEPRECATED.Value, Tracer);
        //        if (thisRowInfo == null)
        //        {
        //            Current_Mode.Invalid_Item = true;
        //            return false;
        //        }
                
        //        Current_Mode.Mode = Display_Mode_Enum.Legacy_URL;
        //        Current_Mode.Error_Message = Current_Mode.Base_URL + thisRowInfo["BibID"] + "/" + thisRowInfo["VID"];
        //        return false;
        //    }

        //    // Must at least have a bib id of the proper length
        //    if (Current_Mode.BibID.Length < 10)
        //    {
        //        Current_Mode.Invalid_Item = true;
        //        return false;
        //    }

        //    // Get the title object for this
        //    bool item_group_display = false;
        //    Multiple_Volume_Item dbTitle = All_Items_Lookup.Title_By_Bib(Current_Mode.BibID);
        //    if (dbTitle == null) 
        //    {
        //        Current_Mode.Invalid_Item = true;
        //        return false;
        //    }

        //    // Try to get the very basic information about this item, to determine if the 
        //    // bib / vid combination is valid
        //    Single_Item selected_item = null;


        //    // Certain mySobek modes only need the item group
        //    if (( Current_Mode.Mode == Display_Mode_Enum.My_Sobek ) && (( Current_Mode.My_Sobek_Type == My_Sobek_Type_Enum.Edit_Group_Behaviors ) || ( Current_Mode.My_Sobek_Type == My_Sobek_Type_Enum.Edit_Group_Serial_Hierarchy ) || ( Current_Mode.My_Sobek_Type == My_Sobek_Type_Enum.Group_Add_Volume ) || ( Current_Mode.My_Sobek_Type == My_Sobek_Type_Enum.Group_AutoFill_Volumes ) || ( Current_Mode.My_Sobek_Type == My_Sobek_Type_Enum.Group_Mass_Update_Items )))
        //    {
        //        item_group_display = true;
        //    }

        //    // If this is not a mode that is only item group display, try to pull the item
        //    if (!item_group_display)
        //    {
        //        if (( !String.IsNullOrEmpty(Current_Mode.VID)) && (Current_Mode.VID != "00000"))
        //        {
        //            selected_item = All_Items_Lookup.Item_By_Bib_VID(Current_Mode.BibID, Current_Mode.VID, Tracer);
        //        }
        //        else
        //        {
        //            if ((dbTitle.Item_Count == 1) && (( String.IsNullOrEmpty(Current_Mode.VID)) || ( Current_Mode.VID != "00000")))
        //            {
        //                selected_item = All_Items_Lookup.Item_By_Bib_Only(Current_Mode.BibID);
        //            }
        //            else
        //            {
        //                item_group_display = true;
        //            }
        //        }
        //    }

        //    // If no valid item and not a valid item group display either, return
        //    if ((selected_item == null) && (!item_group_display))
        //    {                
        //        Current_Mode.Invalid_Item = true;
        //        return false;
        //    }

        //    // If this is for a single item, return that
        //    if (selected_item != null)
        //    {
        //        // Make sure the VID is set
        //        Current_Mode.VID = selected_item.VID;

        //        // Try to get this from the cache
        //        if ((Current_Mode.Mode == Display_Mode_Enum.My_Sobek) && ( Current_Mode.My_Sobek_Type == My_Sobek_Type_Enum.Edit_Item_Metadata ) && (Current_User != null))
        //            Current_Item = CachedDataManager.Items.Retrieve_Digital_Resource_Object(Current_User.UserID, Current_Mode.BibID, Current_Mode.VID, Tracer);
        //        else
        //            Current_Item = CachedDataManager.Items.Retrieve_Digital_Resource_Object( Current_Mode.BibID, Current_Mode.VID, Tracer);

        //        // If not pulled from the cache, then we will have to build the item
        //        if (Current_Item == null)
        //        {
        //            if (Tracer != null)
        //            {
        //                Tracer.Add_Trace("SobekCM_Assistant.Get_Item", "Build the item");
        //            }

        //            Current_Item = SobekCM_Item_Factory.Get_Item(Current_Mode.BibID, Current_Mode.VID, Icon_Table, Tracer);
        //            if (Current_Item != null)
        //            {
        //                if ((Current_Mode.Mode == Display_Mode_Enum.My_Sobek) && (Current_Mode.My_Sobek_Type == My_Sobek_Type_Enum.Edit_Item_Metadata) && (Current_User != null))
        //                {
        //                    string note_to_add = "Online edit by " + Current_User.Full_Name + " ( " + DateTime.Now.ToShortDateString() + " )";
        //                    Current_Item.METS_Header.Add_Creator_Individual_Notes( note_to_add );
        //                    CachedDataManager.Items.Store_Digital_Resource_Object(Current_User.UserID, Current_Mode.BibID, Current_Mode.VID, Current_Item, Tracer);
        //                }
        //                else
        //                    CachedDataManager.Items.Store_Digital_Resource_Object(Current_Mode.BibID, Current_Mode.VID, Current_Item, Tracer);
        //            }
        //        }
        //        else
        //        {
        //            if (Tracer != null)
        //            {
        //                Tracer.Add_Trace("SobekCM_Assistant.Get_Item", "Item found in the cache");
        //            }
        //        }

        //        // If an item was requested and none was found, go to the current home
        //        if ((Current_Item == null))
        //        {
        //            if (Tracer != null && !Tracer.Enabled)
        //            {
        //                Current_Mode.Mode = Display_Mode_Enum.Aggregation;
        //                Current_Mode.Aggregation_Type = Aggregation_Type_Enum.Home;
        //                return false;
        //            }
        //            return false;
        //        }

        //        // Get the page to display (printing has its own specification of page(s) to display)
        //        if (Current_Mode.Mode != Display_Mode_Enum.Item_Print)
        //        {
        //            if (Tracer != null)
        //            {
        //                Tracer.Add_Trace("SobekCM_Assistant.Get_Item", "Get the current page");
        //            }

        //            // Get the page count in the results
        //            int current_page_index = Current_Mode.Page.HasValue ? Math.Max(Current_Mode.Page.Value, ((ushort)1)) : 1;
        //            Current_Page = SobekCM_Item_Factory.Get_Current_Page(Current_Item, current_page_index, Tracer);
        //        }
        //    }
        //    else
        //    {
        //        // Try to get this from the cache
        //        Current_Item = CachedDataManager.Items.Retrieve_Digital_Resource_Object(Current_Mode.BibID, Tracer);

        //        // Have to build this item group information then
        //        if (Current_Item == null)
        //        {
        //            string bibID = Current_Mode.BibID;
        //            Current_Item = SobekCM_Item_Factory.Get_Item_Group(bibID, Icon_Table, Tracer);
        //            if (Tracer != null)
        //            {
        //                Tracer.Add_Trace("SobekCM_Assistant.Get_Item", "TEST LOG ENTRY");
        //            }

        //            if (Current_Item == null)
        //            {
        //                Exception ee = SobekCM_Database.Last_Exception;
        //                if (Tracer != null)
        //                    Tracer.Add_Trace("SobekCM_Assistant.Get_Item", ee != null ? ee.Message : "NO DATABASE EXCEPTION", Custom_Trace_Type_Enum.Error);

        //                Current_Mode.Invalid_Item = true;
        //                return false;
        //            }



        //            // Put this back on the cache
        //            Current_Item.METS_Header.RecordStatus_Enum = METS_Record_Status.BIB_LEVEL;
        //            CachedDataManager.Items.Store_Digital_Resource_Object(bibID, Current_Item, Tracer);
        //            CachedDataManager.Items.Store_Items_In_Title(bibID, Items_In_Title, Tracer);
        //        }
        //    }

        //    return true;
        //}

        #endregion

        #region Method to perform a search

        /// <summary> Performs a search ( or retrieves the search results from the cache ) and outputs the results and search url used  </summary>
        /// <param name="Current_Mode"> Mode / navigation information for the current request</param>
        /// <param name="Aggregation_Object"> Object for the current aggregation object, against which this search is performed </param>
        /// <param name="Search_Stop_Words"> List of search stop workds </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <param name="Complete_Result_Set_Info"> [OUT] Information about the entire set of results </param>
        /// <param name="Paged_Results"> [OUT] List of search results for the requested page of results </param>
        public void Get_Search_Results(Navigation_Object Current_Mode,
                                       Item_Aggregation Aggregation_Object, 
                                       List<string> Search_Stop_Words,
                                       Custom_Tracer Tracer,
                                       out Search_Results_Statistics Complete_Result_Set_Info,
                                       out List<iSearch_Title_Result> Paged_Results )
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("SobekCM_Assistant.Get_Search_Results", String.Empty);
            }

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
	            if ((Current_Mode.Result_Display_Type == "map") || ( String.IsNullOrEmpty(Current_Mode.Result_Display_Type)))
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
			        string[] terms = Current_Mode.Coordinates.Split(",".ToCharArray());
			        if (terms.Length < 2)
			        {
				        Current_Mode.Mode = Display_Mode_Enum.Search;
				        UrlWriterHelper.Redirect(Current_Mode);
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

			        // If neither point is valid, return
			        if (((lat1 == 1000) || (long1 == 1000)) && ((lat2 == 1000) || (long2 == 1000)))
			        {
				        Current_Mode.Mode = Display_Mode_Enum.Search;
				        UrlWriterHelper.Redirect(Current_Mode);
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

			        // Perform the coordinate search against the database
			        try
			        {
                        // Get the page count in the results
                        int current_page_index = Current_Mode.Page.HasValue ? Math.Max(Current_Mode.Page.Value, ((ushort)1)) : 1;

				        // Try to pull more than one page, so we can cache the next page or so
                        Multiple_Paged_Results_Args returnArgs = Engine_Database.Get_Items_By_Coordinates(Current_Mode.Aggregation, lat1, long1, lat2, long2, false, 20, current_page_index, sort, false, new List<short>(), true, Tracer);
				        List<List<iSearch_Title_Result>> pagesOfResults = returnArgs.Paged_Results;
				        Complete_Result_Set_Info = returnArgs.Statistics;

				        if ((pagesOfResults != null) && (pagesOfResults.Count > 0))
					        Paged_Results = pagesOfResults[0];
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
			        UrlWriterHelper.Redirect(Current_Mode);
		        }
	        }
	        else
	        {
		        List<string> terms = new List<string>();
		        List<string> web_fields = new List<string>();

		        // Split the terms correctly ( only use the database stop words for the split if this will go to the database ultimately)
		        if (((Current_Mode.Search_Type == Search_Type_Enum.Full_Text) || (Current_Mode.Search_Fields.IndexOf("TX") >= 0)) || ( UI_ApplicationCache_Gateway.Settings.System.Search_System == Search_System_Enum.Beta ))
		        {
			        Split_Clean_Search_Terms_Fields(Current_Mode.Search_String, Current_Mode.Search_Fields, Current_Mode.Search_Type, terms, web_fields, null, Current_Mode.Search_Precision, ',');
		        }
		        else
		        {
                    // This is a database search, so use the stop-words here
                    Split_Clean_Search_Terms_Fields(Current_Mode.Search_String, Current_Mode.Search_Fields, Current_Mode.Search_Type, terms, web_fields, Search_Stop_Words, Current_Mode.Search_Precision, ',');
		        }

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
                if (!special_search_type)
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
                            Perform_Solr_Search(Tracer, terms, web_fields, date_start, date_end, Aggregation_Object, current_page_index, sort, results_per_page, out recomputed_search_statistics, out Paged_Results, need_search_statistics);
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
                        if (!special_search_type)
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
                        List<List<iSearch_Title_Result>> pagesOfResults = new List<List<iSearch_Title_Result>>();

                        // Perform the search against the database
                        try
                        {
                            Search_Results_Statistics recomputed_search_statistics;

                            // Get the page count in the results
                            int current_page_index = Current_Mode.Page.HasValue ? Math.Max(Current_Mode.Page.Value, ((ushort)1)) : 1;

                            // Use solr or database, depending on the search type
                            if ( UI_ApplicationCache_Gateway.Settings.System.Search_System == Search_System_Enum.Beta )
                                Perform_Solr_Search(Tracer, terms, web_fields, date_start, date_end, Aggregation_Object, current_page_index, sort, results_per_page, out recomputed_search_statistics, out Paged_Results, need_search_statistics);
                            else
                                Perform_Database_Search(Tracer, terms, web_fields, date_start, date_end, actualCount, Current_Mode, sort, Aggregation_Object, results_per_page, !special_search_type, out recomputed_search_statistics, out pagesOfResults, need_search_statistics);

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
                        if (!special_search_type)
                        {
                            // Cache the search statistics, if it was needed
                            if ((need_search_statistics) && (Complete_Result_Set_Info != null))
                            {
                                CachedDataManager.Store_Search_Result_Statistics(Current_Mode, actualCount, web_fields, terms, date_start, date_end, Complete_Result_Set_Info, Tracer);
                            }

                            // Cache the search results
                            if ((need_paged_results) && (pagesOfResults != null))
                            {
                               // CachedDataManager.Store_Search_Results(Current_Mode, sort, actualCount, web_fields, terms, date1, date2, pagesOfResults, Tracer);

                                CachedDataManager.Store_Search_Results(Current_Mode, sort, actualCount, web_fields, terms, date_start, date_end, results_per_page, Paged_Results, Tracer);
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
            string[] fieldSplitTemp = Search_Fields.Split( new[] { Delimiter_Character });
            List<string> fieldSplit = new List<string>();
            List<string> searchSplit = new List<string>();
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
            if ( second_index > first_index )
            {
                searchSplit.Add(Search_String.Substring(first_index));
                fieldSplit.Add(field_index < fieldSplitTemp.Length ? fieldSplitTemp[field_index] : default_index);
            }

            // If this is basic, do some other preparation
            if ( Search_Type == Search_Type_Enum.Full_Text )
            {
                Legacy_Solr_Searcher.Split_Multi_Terms(Search_String, default_index, Output_Terms, Output_Fields);
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

        private void Perform_Database_Search(Custom_Tracer Tracer, List<string> Terms, List<string> Web_Fields, Nullable<DateTime> StartDate, Nullable<DateTime> EndDate, int ActualCount, Navigation_Object Current_Mode, int Current_Sort, Item_Aggregation Aggregation_Object, int Results_Per_Page, bool Potentially_Include_Facets, out Search_Results_Statistics Complete_Result_Set_Info, out List<List<iSearch_Title_Result>> Paged_Results, bool Need_Search_Statistics)
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("SobekCM_Assistant.Perform_Database_Search", "Query the database for search results");
            }

            // Convert the dates to longs
            long Date1 = -1;
            if (StartDate.HasValue)
            {
                TimeSpan timeElapsed = StartDate.Value.Subtract(new DateTime(1, 1, 1));
                Date1 = (long) timeElapsed.TotalDays;
            }
            long Date2 = -1;
            if (EndDate.HasValue)
            {
                TimeSpan timeElapsed = EndDate.Value.Subtract(new DateTime(1, 1, 1));
                Date2 = (long)timeElapsed.TotalDays;
            }

            // Get the list of facets first
            List<short> facetsList = new List<short>();
            foreach (Complete_Item_Aggregation_Metadata_Type facet in Aggregation_Object.Facets)
                facetsList.Add(facet.ID);
            if (!Potentially_Include_Facets)
                facetsList.Clear();

            // Set the return values to NULL initially
            Complete_Result_Set_Info = null;

            const bool INCLUDE_PRIVATE = false;
           
            // Special code for searching by bibid, oclc, or aleph
            if (ActualCount == 1)
            {
                // Is this a BIBID search?
                if ((Web_Fields[0] == "BI") && ( Terms[0].IndexOf("*") < 0 ) && ( Terms[0].Length >= 10 ))
                {
                    string bibid = Terms[0].ToUpper();
                    string vid = String.Empty;
                    if (bibid.Length > 10)
                    {
                        if ((bibid.IndexOf("_") == 10) && ( bibid.Length > 11 ))
                        {
                            vid = bibid.Substring(11).PadLeft(5, '0');
                            bibid = bibid.Substring(0, 10);
                        }
                        else if ((bibid.IndexOf(":") == 10) && ( bibid.Length > 11 ))
                        {
                            vid = bibid.Substring(11).PadLeft(5, '0');
                            bibid = bibid.Substring(0, 10);
                        }
                        else if (bibid.Length == 15)
                        {
                            vid = bibid.Substring(10);
                            bibid = bibid.Substring(0, 10);
                        }
                    }

                    if (bibid.Length == 10)
                    {
                        if (vid.Length == 5)
                        {
                                string redirect_url = Current_Mode.Base_URL + bibid + "/" + vid;
                                if ( Current_Mode.Writer_Type == Writer_Type_Enum.HTML_LoggedIn )
                                    redirect_url = Current_Mode.Base_URL + "l/" + bibid + "/" + vid;
                                HttpContext.Current.Response.Redirect(redirect_url, false);
                                HttpContext.Current.ApplicationInstance.CompleteRequest();
                                Current_Mode.Request_Completed = true;
                                Paged_Results = null;
                                return;
                        }
                        else
                        {
                                string redirect_url = Current_Mode.Base_URL + bibid;
                                if (Current_Mode.Writer_Type == Writer_Type_Enum.HTML_LoggedIn)
                                    redirect_url = Current_Mode.Base_URL + "l/" + bibid;
                                HttpContext.Current.Response.Redirect(redirect_url, false);
                                HttpContext.Current.ApplicationInstance.CompleteRequest();
                                Current_Mode.Request_Completed = true;
                                Paged_Results = null;
                                return;
                        }
                    }
                }

                // Was this a OCLC search?
                if ((Web_Fields[0] == "OC") && (Terms[0].Length > 0))
                {
                    bool is_number = Terms[0].All(Char.IsNumber);

                    if (is_number)
                    {
                        long oclc = Convert.ToInt64(Terms[0]);
                        Multiple_Paged_Results_Args returnArgs = Engine_Database.Items_By_OCLC_Number(oclc, false, Results_Per_Page, Current_Sort, Need_Search_Statistics, Tracer);
                        if (Need_Search_Statistics)
                            Complete_Result_Set_Info = returnArgs.Statistics;
                        Paged_Results = returnArgs.Paged_Results;
                        return;
                    }
                }

                // Was this a ALEPH search?
                if ((Web_Fields[0] == "AL") && (Terms[0].Length > 0))
                {
                    bool is_number = Terms[0].All(Char.IsNumber);

                    if (is_number)
                    {
                        int aleph = Convert.ToInt32(Terms[0]);
                        Multiple_Paged_Results_Args returnArgs = Engine_Database.Items_By_ALEPH_Number(aleph, false, Results_Per_Page, Current_Sort, Need_Search_Statistics, Tracer);
                        if (Need_Search_Statistics)
                            Complete_Result_Set_Info = returnArgs.Statistics;
                        Paged_Results = returnArgs.Paged_Results;
                        return;
                    }
                }
            }

            List<short> links = new List<short>();
            List<short> db_fields = new List<short>();
            List<string> db_terms = Terms.ToList();

            // Step through all the web fields and convert to db fields
            for (int i = 0; i < ActualCount; i++)
            {
                if (Web_Fields[i].Length > 1)
                {
                    // Find the joiner
                    if ((Web_Fields[i][0] == '+') || (Web_Fields[i][0] == '=') || (Web_Fields[i][0] == '-'))
                    {
                        if (Web_Fields[i][0] == '+')
                            links.Add(0);
                        if (Web_Fields[i][0] == '=')
                            links.Add(1);
                        if (Web_Fields[i][0] == '-')
                            links.Add(2);

                        Web_Fields[i] = Web_Fields[i].Substring(1);
                    }
                    else
                    {
                        links.Add(0);
                    }

                    // Find the db field number
                    db_fields.Add(Metadata_Field_Number(Web_Fields[i]));
                }



                // Also add starting and ending quotes to all the valid searches
                if (db_terms[i].Length > 0)
                {
                    if ((db_terms[i].IndexOf("\"") < 0) && (db_terms[i].IndexOf(" ") < 0))
                    {
                        // Since this is a single word, see what type of special codes to include
                        switch (Current_Mode.Search_Precision)
                        {
                            case Search_Precision_Type_Enum.Contains:
                                db_terms[i] = "\"" + db_terms[i] + "\"";
                                break;

                            case Search_Precision_Type_Enum.Inflectional_Form:
                                // If there are any non-characters, don't use inflectional for this term
                                bool inflectional = db_terms[i].All(Char.IsLetter);
                                if (inflectional)
                                {
                                    db_terms[i] = "FORMSOF(inflectional," + db_terms[i] + ")";
                                }
                                else
                                {
                                    db_terms[i] = "\"" + db_terms[i] + "\"";
                                }
                                break;

                            case Search_Precision_Type_Enum.Synonmic_Form:
                                // If there are any non-characters, don't use thesaurus for this term
                                bool thesaurus = db_terms[i].All(Char.IsLetter);
                                if (thesaurus)
                                {
                                    db_terms[i] = "FORMSOF(thesaurus," + db_terms[i] + ")";
                                }
                                else
                                {
                                    db_terms[i] = "\"" + db_terms[i] + "\"";
                                }
                                break;
                        }
                    }
                    else
                    {
                        if (Current_Mode.Search_Precision != Search_Precision_Type_Enum.Exact_Match)
                        {
                            db_terms[i] = "\"" + db_terms[i] + "\"";
                        }
                    }
                }
            }

            // Get the page count in the results
            int current_page_index = Current_Mode.Page.HasValue ? Math.Max(Current_Mode.Page.Value, ((ushort)1)) : 1;

            // If this is an exact match, just do the search
            if (Current_Mode.Search_Precision == Search_Precision_Type_Enum.Exact_Match)
            {
                Multiple_Paged_Results_Args returnArgs = Engine_Database.Perform_Metadata_Exact_Search_Paged(db_terms[0], db_fields[0], INCLUDE_PRIVATE, Current_Mode.Aggregation, Date1, Date2, Results_Per_Page, current_page_index, Current_Sort, Need_Search_Statistics, facetsList, Need_Search_Statistics, Tracer);
                if (Need_Search_Statistics)
                    Complete_Result_Set_Info = returnArgs.Statistics;
                Paged_Results = returnArgs.Paged_Results;
            }
            else
            {
                // Finish filling up the fields and links
                while (links.Count < 10)
                    links.Add(0);
                while (db_fields.Count < 10)
                    db_fields.Add(-1);
                while (db_terms.Count < 10)
                    db_terms.Add(String.Empty);

                // See if this is a simple search, which can use a more optimized search routine
                bool simplified_search = db_fields.All(Field => (Field <= 0));

                // Perform either the simpler metadata search, or the more complex
                if (simplified_search)
                {
                    StringBuilder searchBuilder = new StringBuilder();
                    for (int i = 0; i < db_terms.Count; i++)
                    {
                        if (db_terms[i].Length > 0)
                        {
                            if (i > 0)
                            {
                                if (i > links.Count)
                                {
                                    searchBuilder.Append(" AND ");
                                }
                                else
                                {
                                    switch (links[i])
                                    {
                                        case 0:
                                            searchBuilder.Append(" AND ");
                                            break;

                                        case 1:
                                            searchBuilder.Append(" OR ");
                                            break;

                                        case 2:
                                            searchBuilder.Append(" AND NOT ");
                                            break;
                                    }
                                }
                            }

                            searchBuilder.Append(db_terms[i]);
                        }
                    }



                    Multiple_Paged_Results_Args returnArgs = Engine_Database.Perform_Metadata_Search_Paged(searchBuilder.ToString(), INCLUDE_PRIVATE, Current_Mode.Aggregation, Date1, Date2, Results_Per_Page, current_page_index, Current_Sort, Need_Search_Statistics, facetsList, Need_Search_Statistics, Tracer);
                    if (Need_Search_Statistics)
                        Complete_Result_Set_Info = returnArgs.Statistics;
                    Paged_Results = returnArgs.Paged_Results;
                }
                else
                {
                    // Perform search in the database
                    Multiple_Paged_Results_Args returnArgs = Engine_Database.Perform_Metadata_Search_Paged(links[0], db_terms[0], db_fields[0], links[1], db_terms[1], db_fields[1], links[2], db_terms[2], db_fields[2], links[3], db_terms[3],
                                                                                                            db_fields[3], links[4], db_terms[4], db_fields[4], links[5], db_terms[5], db_fields[5], links[6], db_terms[6], db_fields[6], links[7], db_terms[7], db_fields[7], links[8], db_terms[8], db_fields[8],
                                                                                                            links[9], db_terms[9], db_fields[9], INCLUDE_PRIVATE, Current_Mode.Aggregation, Date1, Date2, Results_Per_Page, current_page_index, Current_Sort, Need_Search_Statistics, facetsList, Need_Search_Statistics, Tracer);
					if (Need_Search_Statistics)
                        Complete_Result_Set_Info = returnArgs.Statistics;
                    Paged_Results = returnArgs.Paged_Results;
                }
            }
        }

        private static short Metadata_Field_Number( string FieldCode )
        {
            Metadata_Search_Field field = UI_ApplicationCache_Gateway.Settings.Metadata_Search_Field_By_Code(FieldCode);
            return (field == null) ? (short) -1 : field.ID;
        }

        private static void Perform_Solr_Search(Custom_Tracer Tracer, List<string> Terms, List<string> Web_Fields, Nullable<DateTime> StartDate, Nullable<DateTime> EndDate, Item_Aggregation Current_Aggregation, int Current_Page, int Current_Sort, int Results_Per_Page, out Search_Results_Statistics Complete_Result_Set_Info, out List<iSearch_Title_Result> Paged_Results, bool Need_Search_Statistics)
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("SobekCM_Assistant.Perform_Solr_Search", "Build the Solr query");
            }

            // Use this built query to query against Solr
            if (UI_ApplicationCache_Gateway.Settings.System.Search_System == Search_System_Enum.Beta)
                v5_Solr_Searcher.Search(Current_Aggregation.Code, Current_Aggregation.Facets, Current_Aggregation.Results_Fields, Terms, Web_Fields, StartDate, EndDate, Results_Per_Page, Current_Page, (ushort)Current_Sort, Need_Search_Statistics, Tracer, out Complete_Result_Set_Info, out Paged_Results);
            else
                Legacy_Solr_Searcher.Search(Current_Aggregation.Code, Terms, Web_Fields, Results_Per_Page, Current_Page, (ushort)Current_Sort, Need_Search_Statistics, Tracer, out Complete_Result_Set_Info, out Paged_Results);

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
