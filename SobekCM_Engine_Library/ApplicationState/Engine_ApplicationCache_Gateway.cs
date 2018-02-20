﻿#region Using directives

using System;
using System.Collections.Generic;
using System.Data;
using SobekCM.Core.Aggregations;
using SobekCM.Core.ApplicationState;
using SobekCM.Core.Configuration;
using SobekCM.Core.Settings;
using SobekCM.Core.Skins;
using SobekCM.Core.UI_Configuration;
using SobekCM.Core.Users;
using SobekCM.Core.WebContent.Hierarchy;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Engine_Library.Database;
using SobekCM.Engine_Library.Settings;
using SobekCM.Engine_Library.Skins;

#endregion

namespace SobekCM.Engine_Library.ApplicationState
{
    /// <summary> Class stores all the application-wide setting information as well as basic values
    /// for most of the main object types used by the system </summary>
    public static class Engine_ApplicationCache_Gateway
    {
        /// <summary> Constructor for this gateway class, which sets the last refresh time </summary>
        static Engine_ApplicationCache_Gateway()
        {
            Last_Refresh = DateTime.Now;
        }

        /// <summary> Last time the date time value was refreshed </summary>
        public static DateTime? Last_Refresh { get; private set; }



        /// <summary> Refress all of the settings within this gateway </summary>
        /// <param name="DbInstance"> Database instance to use when pulling the new data  </param>
        /// <returns> TRUE if successful, FALSE if any errors occurred </returns>
        public static bool RefreshAll( Database_Instance_Configuration DbInstance )
        {
            bool error = !RefreshSettings(DbInstance);
            error = error | !RefreshConfiguration(DbInstance);
            error = error | !RefreshStatsDateRange();
            error = error | !RefreshTranslations();
            error = error | !RefreshWebSkins();
            error = error | !RefreshCodes();
            error = error | !RefreshStopWords();
            error = error | !RefreshIP_Restrictions();
            error = error | !RefreshThematicHeadings();
            error = error | !RefreshUserGroups();
            error = error | !RefreshCollectionAliases();
            error = error | !RefreshMimeTypes();
            error = error | !RefreshIcons();
            error = error | !RefreshDefaultMetadataTemplates();
            error = error | !RefreshUrlPortals();
            error = error | !RefreshWebContentHierarchy();
            error = error | !RefreshTitles();

            Last_Refresh = DateTime.Now;

            return !error;
        }
        
        /// <summary> Refress all of the settings within this gateway </summary>
        /// <returns> TRUE if successful, FALSE if any errors occurred </returns>
        public static bool RefreshAll()
        {
            bool error = !RefreshSettings();
            error = error | !RefreshConfiguration();
            error = error | !RefreshStatsDateRange();
            error = error | !RefreshTranslations();
            error = error | !RefreshWebSkins();
            error = error | !RefreshCodes();
            error = error | !RefreshStopWords();
            error = error | !RefreshIP_Restrictions();
            error = error | !RefreshThematicHeadings();
            error = error | !RefreshUserGroups();
            error = error | !RefreshCollectionAliases();
            error = error | !RefreshMimeTypes();
            error = error | !RefreshIcons();
            error = error | !RefreshDefaultMetadataTemplates();
            error = error | !RefreshUrlPortals();
            error = error | !RefreshWebContentHierarchy();
            error = error | !RefreshTitles();

            Last_Refresh = DateTime.Now;

            return !error;
        }

        #region Properties and methods for the statistics date range

        private static Statistics_Dates statsDates;
        private static readonly Object statsDatesLock = new Object();

        /// <summary> Refresh the statistics date range by pulling the data back from the database </summary>
        /// <returns> TRUE if successful, otherwise FALSE </returns>
        public static bool RefreshStatsDateRange()
        {
            try
            {
                lock (statsDatesLock)
                {
                    if (statsDates == null)
                    {
                        statsDates = new Statistics_Dates();
                    }

                    // Get the data from the database
                    Engine_Database.Populate_Statistics_Dates(statsDates, null);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary> Get the statistics date range (or build the object and return it) </summary>
        public static Statistics_Dates Stats_Date_Range
        {
            get
            {
                lock (statsDatesLock)
                {
                    if (statsDates == null)
                    {
                        statsDates = new Statistics_Dates();

                        Engine_Database.Populate_Statistics_Dates(statsDates, null);
                    }

                    return statsDates;
                }
            }
            set
            {
                statsDates = value;
            }
        }

        #endregion

        #region Properties and methods for the translation object 

        private static Language_Support_Info translations;
        private static readonly Object translationLock = new Object();

        /// <summary> Refresh the translation object by pulling the data back from the database </summary>
        /// <returns> TRUE if successful, otherwise FALSE </returns>
        public static bool RefreshTranslations()
        {
            try
            {
                lock (translationLock)
                {
                    if (translations == null)
                    {
                        translations = new Language_Support_Info();
                    }

                    // Get the data from the database
                    Engine_Database.Populate_Translations(translations, null);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary> Get the translation object (or build the object and return it) </summary>
        public static Language_Support_Info Translation
        {
            get
            {
                lock (translationLock)
                {
                    if (translations == null)
                    {
                        translations = new Language_Support_Info();

                        Engine_Database.Populate_Translations(translations, null);
                    }

                    return translations;
                }
            }
        }

        #endregion

        #region Properties and methods for the web skin collection

        private static Web_Skin_Collection webSkins;
        private static readonly Object webSkinsLock = new Object();

        /// <summary> Refresh the web skin collection by pulling the data back from the database </summary>
        /// <returns> TRUE if successful, otherwise FALSE </returns>
        public static bool RefreshWebSkins()
        {
            try
            {
                lock (webSkinsLock)
                {
                    if (webSkins == null)
                    {
                        webSkins = new Web_Skin_Collection();
                    }

                    Web_Skin_Utilities.Populate_Default_Skins(webSkins, null);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary> Get the web skin collection object (or build the object and return it) </summary>
        public static Web_Skin_Collection Web_Skin_Collection
        {
            get
            {
                lock (webSkinsLock)
                {
                    if (webSkins == null)
                    {
                        webSkins = new Web_Skin_Collection();

                        Web_Skin_Utilities.Populate_Default_Skins(webSkins, null);
                    }

                    return webSkins;
                }
            }
            set
            {
                webSkins = value;
            }
        }

        #endregion

        #region Properties and methods for the URL portals list

        private static Portal_List portals;
        private static readonly Object portalsLock = new Object();

        /// <summary> Refresh the URL portals list by pulling the data back from the database </summary>
        /// <returns> TRUE if successful, otherwise FALSE </returns>
        public static bool RefreshUrlPortals()
        {
            try
            {
                lock (portalsLock)
                {
                    if (portals == null)
                    {
                        portals = new Portal_List();
                    }

                    Engine_Database.Populate_URL_Portals(portals, null);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary> Get the URL portal list object (or build the object and return it) </summary>
        public static Portal_List URL_Portals
        {
            get
            {
                lock (portalsLock)
                {
                    if (portals == null)
                    {
                        portals = new Portal_List();
                        Engine_Database.Populate_URL_Portals(portals, null);
                    }

                    return portals;
                }
            }
            set
            {
                portals = value;
            }
        }

        #endregion

        #region Properties and methods for the aggregation codes list

        private static Aggregation_Code_Manager codes;

        private static readonly Object codesLock = new Object();

        /// <summary> Refresh the aggregation code list by pulling the data back from the database </summary>
        /// <returns> TRUE if successful, otherwise FALSE </returns>
        public static bool RefreshCodes()
        {
            try
            {
                lock (codesLock)
                {
                    if (codes == null)
                    {
                        codes = new Aggregation_Code_Manager();
                    }

                    Engine_Database.Populate_Code_Manager(codes, null);
                }

                return true;
            }
            catch ( Exception ee )
            {
                return ee.Message.Length > 0 ;
            }
        }

        /// <summary> Get the aggregation code list object (or build the object and return it) </summary>
        public static Aggregation_Code_Manager Codes
        {
            get
            {
                lock (codesLock)
                {
                    if (codes == null)
                    {
                        codes = new Aggregation_Code_Manager();
                        Engine_Database.Populate_Code_Manager(codes, null);
                    }

                    return codes;
                }
            }
            set
            {
                codes = value;
            }
        }

        #endregion

        #region Properties and methods for the instance-wide settings

        private static InstanceWide_Settings settings;
        private static readonly Object settingsLock = new Object();

        /// <summary> Refresh the settings object by pulling the data back from the database </summary>
        /// <returns> TRUE if successful, otherwise FALSE </returns>
        public static bool RefreshSettings(Database_Instance_Configuration DbInstance )
        {
            try
            {
                lock (settingsLock)
                {
                    if (settings == null)
                        settings = InstanceWide_Settings_Builder.Build_Settings(DbInstance);
                    else
                    {
                        InstanceWide_Settings newSettings = InstanceWide_Settings_Builder.Build_Settings(DbInstance);
                        settings = newSettings;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary> Refresh the settings object by pulling the data back from the database </summary>
        /// <returns> TRUE if successful, otherwise FALSE </returns>
        public static bool RefreshSettings()
        {
            try
            {
                lock (settingsLock)
                {
                    if (settings == null)
                        settings = InstanceWide_Settings_Builder.Build_Settings();
                    else
                    {
                        InstanceWide_Settings newSettings = InstanceWide_Settings_Builder.Build_Settings();
                        settings = newSettings;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }


        /// <summary> Get the settings object (or build the object and return it) </summary>
        public static InstanceWide_Settings Settings
        {
            get
            {
                lock (settingsLock)
                {
                    return settings ?? (settings = InstanceWide_Settings_Builder.Build_Settings());
                }
            }
            set
            {
                settings = value;
            }
        }

        #endregion

        #region Properties and methods for the instance-wide configuration

        private static InstanceWide_Configuration configuration;
        private static readonly Object configurationLock = new Object();

        /// <summary> Refresh the settings object by pulling the data back from the database </summary>
        /// <returns> TRUE if successful, otherwise FALSE </returns>
        public static bool RefreshConfiguration(Database_Instance_Configuration DbInstance)
        {
            try
            {
                lock (configurationLock)
                {
                    if (configuration == null)
                        configuration = Configuration_Files_Reader.Read_Config_Files( Settings);
                    else
                    {
                        InstanceWide_Configuration newConfig = Configuration_Files_Reader.Read_Config_Files( Settings);
                        configuration = newConfig;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary> Refresh the settings object by pulling the data back from the database </summary>
        /// <returns> TRUE if successful, otherwise FALSE </returns>
        public static bool RefreshConfiguration()
        {
            try
            {
                lock (configurationLock)
                {
                    if (configuration == null)
                        configuration = Configuration_Files_Reader.Read_Config_Files( Settings);
                    else
                    {
                        InstanceWide_Configuration newConfig = Configuration_Files_Reader.Read_Config_Files( Settings);
                        configuration = newConfig;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }


        /// <summary> Get the configuration object (or build the object and return it) </summary>
        public static InstanceWide_Configuration Configuration
        {
            get
            {
                lock (configurationLock)
                {
                    return configuration ?? (configuration = Configuration_Files_Reader.Read_Config_Files( Settings));
                }
            }
            set
            {
                configuration = value;
            }
        }

        #endregion

        #region Properties and methods about the search stop words list


        private static List<string> searchStopWords;
        private static readonly Object searchStopWordsLock = new Object();

        /// <summary> Refresh the list of search stop words for database searching by pulling the data back from the database </summary>
        /// <returns> TRUE if successful, otherwise FALSE </returns>
        public static bool RefreshStopWords()
        {
            try
            {
                lock (searchStopWordsLock)
                {
                    if ( searchStopWords != null )
                        searchStopWords.Clear();
                    searchStopWords = Engine_Database.Search_Stop_Words(null);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary> Get the list of search stop words for database searching (or build the collection and return it) </summary>
        public static List<string> StopWords
        {
            get
            {
                lock (searchStopWordsLock)
                {
                    return searchStopWords ?? (searchStopWords = Engine_Database.Search_Stop_Words(null));
                }
            }
        }


        #endregion

        #region Properties and methods about the IP restriction lists

        private static IP_Restriction_Ranges ipRestrictions;
        private static readonly Object ipRestrictionsLock = new Object();

        /// <summary> Refresh the list of ip restriction ranges by pulling the data back from the database </summary>
        /// <returns> TRUE if successful, otherwise FALSE </returns>
        public static bool RefreshIP_Restrictions()
        {
            try
            {
                lock (settingsLock)
                {
                    lock (ipRestrictionsLock)
                    {
                        DataTable ipRestrictionTbl = Engine_Database.Get_IP_Restriction_Ranges(null);
                        if (ipRestrictionTbl != null)
                        {
                            ipRestrictions = new IP_Restriction_Ranges();
                            ipRestrictions.Populate_IP_Ranges(ipRestrictionTbl);
                        }
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary> Get the list of ip restriction ranges (or build the object and return it) </summary>
        public static IP_Restriction_Ranges IP_Restrictions
        {
            get
            {
                // This just ensures another thread doesn't pull a half built list
                lock (ipRestrictionsLock)
                {
                    if (ipRestrictions == null)
                    {
                        DataTable ipRestrictionTbl = Engine_Database.Get_IP_Restriction_Ranges(null);
                        if (ipRestrictionTbl != null)
                        {
                            ipRestrictions = new IP_Restriction_Ranges();
                            ipRestrictions.Populate_IP_Ranges(ipRestrictionTbl);
                        }
                    }

                    return ipRestrictions;
                }
            }
        }


        #endregion

        #region Properties and methods about search history collection

        private static Recent_Searches searchHistory;
        private static readonly Object searchHistoryLock = new Object();

        /// <summary> Get the search history (or build the object and return it) </summary>
        public static Recent_Searches Search_History
        {
            get
            {
                // This just ensures another thread doesn't pull a half built list
                lock (searchHistoryLock)
                {
                    return searchHistory ?? (searchHistory = new Recent_Searches());
                }
            }
        }

        #endregion

        #region Properties and methods about the checked out list

        private static Checked_Out_Items_List checkedList;
        private static readonly Object checkedListLock = new Object();

        /// <summary> Get the checked out list of items object (or build the object and return it) </summary>
        public static Checked_Out_Items_List Checked_List
        {
            get
            {
                // This just ensures another thread doesn't pull a half built list
                lock (checkedListLock)
                {
                    return checkedList ?? (checkedList = new Checked_Out_Items_List());
                }
            }
        }

        #endregion

        #region Properties and methods about the thematic headings list

        private static List<Thematic_Heading> thematicHeadings;
        private static readonly Object thematicHeadingsLock = new Object();

        /// <summary> Refresh the list of thematic headings by pulling the data back from the database </summary>
        /// <returns> TRUE if successful, otherwise FALSE </returns>
        public static bool RefreshThematicHeadings()
        {
            try
            {
                lock (thematicHeadingsLock)
                {
                    if ( thematicHeadings != null )
                        thematicHeadings.Clear();
                    if (!Engine_Database.Populate_Thematic_Headings(Thematic_Headings, null))
                    {
                        thematicHeadings = null;
                        return false;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary> Get the list of thematic headings for database searching (or build the collection and return it) </summary>
        public static List<Thematic_Heading> Thematic_Headings
        {
            get
            {
                lock (thematicHeadingsLock)
                {
                    if (thematicHeadings == null)
                    {
                        thematicHeadings = new List<Thematic_Heading>();
                        if (!Engine_Database.Populate_Thematic_Headings(Thematic_Headings, null))
                        {
                            thematicHeadings = null;
                            throw Engine_Database.Last_Exception;
                        }
                    }

                    return thematicHeadings;
                }
            }
        }


        #endregion

        #region Properties and methods about the list of all user groups 

        private static List<User_Group> userGroups;
        private static readonly Object userGroupsLock = new Object();

        /// <summary> Refresh the list of all user groups by pulling the data back from the database </summary>
        /// <returns> TRUE if successful, otherwise FALSE </returns>
        public static bool RefreshUserGroups()
        {
            try
            {
                lock (userGroupsLock)
                {
                    if ( userGroups != null )
                        userGroups.Clear();
                    userGroups = Engine_Database.Get_All_User_Groups(null);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary> Get the list of all user groups (or build the collection and return it) </summary>
        public static List<User_Group> User_Groups
        {
            get
            {
                lock (userGroupsLock)
                {
                    return userGroups ?? (userGroups = Engine_Database.Get_All_User_Groups(null));
                }
            }
        }


        #endregion

        #region Properties and methods about the collection aliases dictionary

        private static Dictionary<string, string> collectionAliases;
        private static readonly Object collectionAliasesLock = new Object();

        /// <summary> Refresh the list of aggregation/collection aliases by pulling the data back from the database </summary>
        /// <returns> TRUE if successful, otherwise FALSE </returns>
        public static bool RefreshCollectionAliases()
        {
            try
            {
                lock (collectionAliasesLock)
                {
                    if (collectionAliases == null)
                        collectionAliases = new Dictionary<string, string>();

                    Engine_Database.Populate_Aggregation_Aliases(collectionAliases, null);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary> Get the dictionary of collection aliases (or build the collection and return it) </summary>
        public static Dictionary<string, string> Collection_Aliases
        {
            get
            {
                lock (collectionAliasesLock)
                {
                    if (collectionAliases == null)
                    {
                        collectionAliases = new Dictionary<string, string>();
                        Engine_Database.Populate_Aggregation_Aliases(collectionAliases, null);
                    }

                    return collectionAliases;
                }
            }
            set
            {
                collectionAliases = value;
            }
        }


        #endregion

        #region Properties and methods about the mime types dictionary

        private static Dictionary<string, Mime_Type_Info> mimeTypes;
        private static readonly Object mimeTypesLock = new Object();

        /// <summary> Refresh the list of mime types by pulling the data back from the database </summary>
        /// <returns> TRUE if successful, otherwise FALSE </returns>
        public static bool RefreshMimeTypes()
        {
            try
            {
                lock (mimeTypesLock)
                {
                    if (mimeTypes == null)
                        mimeTypes = new Dictionary<string, Mime_Type_Info>();

                    Engine_Database.Populate_MIME_List(mimeTypes, null);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary> Get the dictionary of mime types (or build the collection and return it) </summary>
        public static Dictionary<string, Mime_Type_Info> Mime_Types
        {
            get
            {
                lock (mimeTypesLock)
                {
                    if (mimeTypes == null)
                    {
                        mimeTypes = new Dictionary<string, Mime_Type_Info>();
                        Engine_Database.Populate_MIME_List(mimeTypes, null);
                    }

                    return mimeTypes;
                }
            }
        }


        #endregion

        #region Properties and methods about the icon/wordmarks dictionary

        private static Dictionary<string, Wordmark_Icon> iconList;
        private static readonly Object iconListLock = new Object();

        /// <summary> Refresh the list of icon/wordmarks by pulling the data back from the database </summary>
        /// <returns> TRUE if successful, otherwise FALSE </returns>
        public static bool RefreshIcons()
        {
            try
            {
                lock (iconListLock)
                {
                    if (iconList == null)
                        iconList = new Dictionary<string, Wordmark_Icon>(StringComparer.OrdinalIgnoreCase);

                    Engine_Database.Populate_Icon_List(iconList, null);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary> Get the dictionary of icon/wordmarks (or build the collection and return it) </summary>
        public static Dictionary<string, Wordmark_Icon> Icon_List
        {
            get
            {
                lock (iconListLock)
                {
                    if (iconList == null)
                    {
                        iconList = new Dictionary<string, Wordmark_Icon>(StringComparer.OrdinalIgnoreCase);
                        Engine_Database.Populate_Icon_List(iconList, null);
                    }

                    return iconList;
                }
            }
        }


        #endregion

        #region Properties and methods about the default metadata sets and templates

        private static List<Template> templateList;
        private static List<Default_Metadata> defaultMetadataList;
        private static readonly Object templateMetadataLock = new Object();

        private static void load_metadata_template()
        {
            // Get the list of all projects
            DataSet projectsSet = Engine_Database.Get_All_Template_DefaultMetadatas(null);
            if (projectsSet != null)
            {
                if (templateList == null)
                    templateList = new List<Template>();
                else
                    templateList.Clear();

                if (defaultMetadataList == null)
                    defaultMetadataList = new List<Default_Metadata>();
                else
                    defaultMetadataList.Clear();

                // Add each default metadata set
                foreach (DataRow thisRow in projectsSet.Tables[0].Rows)
                {
                    string code = thisRow["MetadataCode"].ToString();
                    string name = thisRow["MetadataName"].ToString();
                    string description = thisRow["Description"].ToString();

                    defaultMetadataList.Add(new Default_Metadata(code, name, description));
                }

                // Add each project
                foreach (DataRow thisRow in projectsSet.Tables[1].Rows)
                {
                    string code = thisRow["TemplateCode"].ToString();
                    string name = thisRow["TemplateName"].ToString();
                    string description = thisRow["Description"].ToString();

                    templateList.Add(new Template(code, name, description));
                }
            }  
        }

        /// <summary> List of all the globally defined default metadata sets for this instance </summary>
        public static List<Default_Metadata> Global_Default_Metadata
        {
            get
            {
                lock (templateMetadataLock)
                {
                    if ((templateList == null) || ( defaultMetadataList == null ))
                    {
                        load_metadata_template();
                    }

                    return defaultMetadataList;
                }
            }
        }

        /// <summary> List of all the globally defined metadata templates within this instance </summary>
        public static List<Template> Templates
        {
            get
            {
                lock (templateMetadataLock)
                {
                    if ((templateList == null) || (defaultMetadataList == null))
                    {
                        load_metadata_template();
                    }

                    return templateList;
                }
            }
        }

        /// <summary> Clears the lists of globally defined default metadata sets and metadata input templates, so they 
        /// will be refreshed next time they are requested </summary>
        /// <returns> TRUE </returns>
        public static bool RefreshDefaultMetadataTemplates()
        {
            defaultMetadataList = null;
            templateList = null;
            return true;
        }

        #endregion

        #region Properties and methods related to the hierarchy of non-aggregational web content pages

        private static WebContent_Hierarchy webContentHierarchy;
        private static readonly Object webContentHierarchyLock = new Object();

        /// <summary> Refresh the hierarchy of non-aggregational web content pages by pulling the data back from the database </summary>
        /// <returns> TRUE if successful, otherwise FALSE </returns>
        public static bool RefreshWebContentHierarchy()
        {
            try
            {
                lock (webContentHierarchyLock)
                {
                    // Either create a new hierarchy object , or clear the existing
                    if (webContentHierarchy == null)
                        webContentHierarchy = new WebContent_Hierarchy();
                    else
                        webContentHierarchy.Clear();

                    if (!Engine_Database.WebContent_Populate_All_Hierarchy(webContentHierarchy, null))
                    {
                        webContentHierarchy = null;
                        return false;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary> Get the complete hierarchy of non-aggregational web content pages and redirects, used for navigation </summary>
        public static WebContent_Hierarchy WebContent_Hierarchy
        {
            get
            {
                lock (webContentHierarchyLock)
                {
                    if (webContentHierarchy == null)
                    {
                        webContentHierarchy = new WebContent_Hierarchy();
                        if (!Engine_Database.WebContent_Populate_All_Hierarchy(webContentHierarchy, null))
                        {
                            webContentHierarchy = null;
                            throw Engine_Database.Last_Exception;
                        }
                    }

                    return webContentHierarchy;
                }
            }
            set
            {
                webContentHierarchy = value;
            }
        }

        #endregion

        #region Properties and methods about the multiple volume records


        private static Multiple_Volume_Collection titleList;
        private static readonly Object titleListLock = new Object();

        /// <summary> Refresh the list of titles which have multiple volumes by pulling the data back from the database </summary>
        /// <returns> TRUE if successful, otherwise FALSE </returns>
        public static bool RefreshTitles()
        {
            try
            {
                lock (titleListLock)
                {
                    if (titleList == null)
                        titleList = new Multiple_Volume_Collection();

                    Engine_Database.Populate_Multiple_Volumes(titleList, null);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary> Get the collection of all titles which have title-level metadata or have
        /// multiple volumes within the system </summary>
        public static Multiple_Volume_Collection Title_List
        {
            get
            {
                lock (titleListLock)
                {
                    if (titleList == null)
                    {
                        titleList = new Multiple_Volume_Collection();
                        Engine_Database.Populate_Multiple_Volumes(titleList, null );
                    }

                    return titleList;
                }
            }
        }

        #endregion
    }
}
