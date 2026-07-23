#region Using directives

using SobekCM.Core.Configuration.Localization;

#endregion

namespace SobekCM.Library.Localization
{
    /// <summary> Static gateway to every localized display phrase used by the public item and
    /// aggregation viewers. Backed by per-language XML files under config/default/localization/
    /// (see <see cref="Localization_Store"/>) — English is the required default; other languages
    /// fall back to English for any section/key they don't define. </summary>
    /// <remarks> Language is passed explicitly to every call, matching this codebase's existing
    /// convention (e.g. the legacy <c>Get_Translation(key, Current_Mode.Language)</c> pattern) rather
    /// than ambient/thread-local state — callers already have <c>RequestSpecificValues.Current_Mode.Language</c>
    /// on hand at every call site. </remarks>
    public static class Localization_Gateway
    {
        /// <summary> Clears every cached language table, so edited XML files are picked up on next
        /// access without an app restart </summary>
        public static void Clear_Cache()
        {
            Localization_Store.Clear_Cache();
        }

        /// <summary> Phrases shared across multiple item viewers — currently just the pagination bar,
        /// defined once in Item_HtmlSubwriter and reused by every paginated item viewer, deduplicated
        /// from what used to be a near-verbatim copy inside Text_Search_ItemViewer </summary>
        public static class Common
        {
            public static string Go_To(Web_Language_Enum Language) => Localization_Store.Get("items", "Common", "Go_To", Language);
            public static string First_Page(Web_Language_Enum Language) => Localization_Store.Get("items", "Common", "First_Page", Language);
            public static string Previous_Page(Web_Language_Enum Language) => Localization_Store.Get("items", "Common", "Previous_Page", Language);
            public static string Next_Page(Web_Language_Enum Language) => Localization_Store.Get("items", "Common", "Next_Page", Language);
            public static string Last_Page(Web_Language_Enum Language) => Localization_Store.Get("items", "Common", "Last_Page", Language);
            public static string First(Web_Language_Enum Language) => Localization_Store.Get("items", "Common", "First", Language);
            public static string Previous(Web_Language_Enum Language) => Localization_Store.Get("items", "Common", "Previous", Language);
            public static string Next(Web_Language_Enum Language) => Localization_Store.Get("items", "Common", "Next", Language);
            public static string Last(Web_Language_Enum Language) => Localization_Store.Get("items", "Common", "Last", Language);
        }

        /// <summary> Phrases shared across multiple aggregation viewers (the "Go" search button, the
        /// basic search box, and the quick-tips help block defined once in abstractAggregationViewer
        /// and reused by every basic-search-flavored viewer) </summary>
        public static class Aggregation_Common
        {
            public static string Go(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Aggregation_Common", "Go", Language);
            public static string Search_Collection(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Aggregation_Common", "Search_Collection", Language);
            public static string Include_Non_Public_Items(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Aggregation_Common", "Include_Non_Public_Items", Language);
            public static string Limit_By_Year(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Aggregation_Common", "Limit_By_Year", Language);
            public static string Show_Only_Media(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Aggregation_Common", "Show_Only_Media", Language);
            public static string Through(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Aggregation_Common", "Through", Language);
            public static string Include_Full_Text(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Aggregation_Common", "Include_Full_Text", Language);
            public static string Search_All_Collections(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Aggregation_Common", "Search_All_Collections", Language);
            public static string Home(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Aggregation_Common", "Home", Language);

            /// <summary> Whole quick-tips help block (heading, list, boolean/phrase/capitalization/diacritics
            /// tips) as one static HTML fragment per language — entirely static markup+text with no dynamic
            /// data, so it's localized as a single unit rather than broken into per-sentence keys </summary>
            public static string Quick_Tips_Html(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Aggregation_Common", "Quick_Tips_Html", Language);
        }

        /// <summary> Phrases for the full-text-search aggregation viewer </summary>
        public static class Full_Text_Search_Aggregation
        {
            public static string Search_Full_Text(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Full_Text_Search", "Search_Full_Text", Language);
        }

        /// <summary> Phrases for the dLOC search aggregation viewer </summary>
        public static class DLOC_Search
        {
            public static string Include_Newspapers(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "DLOC_Search", "Include_Newspapers", Language);
        }

        /// <summary> Phrases shared across the Google-Maps-based viewers (Map_Search, Map_Browse) </summary>
        public static class Map_Common
        {
            /// <summary> Error block shown in place of the map when no Google Maps API key is configured —
            /// identical markup was duplicated in both Map_Search_AggregationViewer and Map_Browse_AggregationViewer </summary>
            public static string Google_Maps_Not_Enabled_Html(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Map_Common", "Google_Maps_Not_Enabled_Html", Language);
        }

        /// <summary> Phrases for the Google-map-based search aggregation viewer </summary>
        public static class Map_Search
        {
            public static string Find_Address(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Map_Search", "Find_Address", Language);
            public static string Address(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Map_Search", "Address", Language);
            public static string Locate(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Map_Search", "Locate", Language);

            /// <summary> Instructions shown when point-searching is disabled — English text only, reused for
            /// every language, matching the original code (its Spanish branch never actually translated
            /// this fragment; both languages rendered the same English text) </summary>
            public static string Point_Disabled_Instructions_Html(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Map_Search", "Point_Disabled_Instructions_Html", Language);

            /// <summary> Normal (point-searching-enabled) map search instructions block </summary>
            public static string Instructions_Html(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Map_Search", "Instructions_Html", Language);

            /// <summary> Fallback "Map Search FAQ" block shown when no aggregation/instance-specific FAQ
            /// file exists, point-searching-enabled variant </summary>
            public static string Faq_Point_Enabled_Html(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Map_Search", "Faq_Point_Enabled_Html", Language);

            /// <summary> Same fallback FAQ block, point-searching-disabled variant </summary>
            public static string Faq_Point_Disabled_Html(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Map_Search", "Faq_Point_Disabled_Html", Language);
        }

        /// <summary> Phrases for the Google-map-based browse aggregation viewer </summary>
        public static class Map_Browse
        {
            public static string Select_Point_Instructions_Html(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Map_Browse", "Select_Point_Instructions_Html", Language);
            public static string Faq_Html(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Map_Browse", "Faq_Html", Language);
            public static string More_Info_Single_Title(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Map_Browse", "More_Info_Single_Title", Language);

            /// <summary> Format string with a "{0}" placeholder for the title count — e.g.
            /// string.Format(Localization_Gateway.Map_Browse.More_Info_Multiple_Titles(language), count) </summary>
            public static string More_Info_Multiple_Titles(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Map_Browse", "More_Info_Multiple_Titles", Language);
        }

        /// <summary> Phrases for the newspaper-search aggregation viewer </summary>
        public static class Newspaper_Search
        {
            public static string Full_Citation(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Newspaper_Search", "Full_Citation", Language);
            public static string Full_Text(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Newspaper_Search", "Full_Text", Language);
            public static string Newspaper_Title(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Newspaper_Search", "Newspaper_Title", Language);
            public static string Location(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Newspaper_Search", "Location", Language);
        }

        /// <summary> Phrases for the advanced search aggregation viewer </summary>
        public static class Advanced_Search
        {
            public static string Search_For(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Advanced_Search", "Search_For", Language);
            public static string In(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Advanced_Search", "In", Language);
            public static string Search(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Advanced_Search", "Search", Language);
            public static string Search_Options(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Advanced_Search", "Search_Options", Language);
            public static string Precision(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Advanced_Search", "Precision", Language);
            public static string Contains_Exactly(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Advanced_Search", "Contains_Exactly", Language);
            public static string Contains_Any_Form(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Advanced_Search", "Contains_Any_Form", Language);
            public static string Contains_Meaning(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Advanced_Search", "Contains_Meaning", Language);
            public static string And(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Advanced_Search", "And", Language);
            public static string Or(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Advanced_Search", "Or", Language);
            public static string And_Not(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Advanced_Search", "And_Not", Language);
        }

        /// <summary> Phrases for the full-text-search item viewer </summary>
        public static class Text_Search
        {
            public static string Search_This_Document(Web_Language_Enum Language) => Localization_Store.Get("items", "Text_Search", "Search_This_Document", Language);
            public static string Go(Web_Language_Enum Language) => Localization_Store.Get("items", "Text_Search", "Go", Language);

            /// <summary> Whole "Quick Tips" help block (document/boolean/phrase/capitalization/diacritics
            /// searching tips) as one static HTML fragment per language, same reasoning as
            /// Aggregation_Common.Quick_Tips_Html </summary>
            public static string Quick_Tips_Html(Web_Language_Enum Language) => Localization_Store.Get("items", "Text_Search", "Quick_Tips_Html", Language);

            // Sentence-composition fragments used by Compute_Search_Explanation() — concatenated in
            // sequence around dynamic search terms/counts, so each connector/fragment is its own key
            // rather than one templated sentence
            public static string Your_Search_For(Web_Language_Enum Language) => Localization_Store.Get("items", "Text_Search", "Your_Search_For", Language);
            public static string And_Not(Web_Language_Enum Language) => Localization_Store.Get("items", "Text_Search", "And_Not", Language);
            public static string And(Web_Language_Enum Language) => Localization_Store.Get("items", "Text_Search", "And", Language);
            public static string Or(Web_Language_Enum Language) => Localization_Store.Get("items", "Text_Search", "Or", Language);
            public static string Not(Web_Language_Enum Language) => Localization_Store.Get("items", "Text_Search", "Not", Language);
            public static string Resulted_In(Web_Language_Enum Language) => Localization_Store.Get("items", "Text_Search", "Resulted_In", Language);
            public static string Matching_Pages(Web_Language_Enum Language) => Localization_Store.Get("items", "Text_Search", "Matching_Pages", Language);
            public static string No_Matching_Pages(Web_Language_Enum Language) => Localization_Store.Get("items", "Text_Search", "No_Matching_Pages", Language);
            public static string Expand_Results(Web_Language_Enum Language) => Localization_Store.Get("items", "Text_Search", "Expand_Results", Language);
            public static string Restrict_Results(Web_Language_Enum Language) => Localization_Store.Get("items", "Text_Search", "Restrict_Results", Language);
        }

        /// <summary> Phrases for the JPEG2000 (OpenSeadragon zoomable image) item viewer </summary>
        public static class JPEG2000
        {
            public static string Thumbnail(Web_Language_Enum Language) => Localization_Store.Get("items", "JPEG2000", "Thumbnail", Language);
        }

        /// <summary> Phrases for the item-count aggregation viewer </summary>
        public static class Item_Count
        {
            public static string Resource_Count_In_Collection(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Item_Count", "Resource_Count_In_Collection", Language);
            public static string Description(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Item_Count", "Description", Language);
            public static string Visibility(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Item_Count", "Visibility", Language);
            public static string Title_Count(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Item_Count", "Title_Count", Language);
            public static string Items(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Item_Count", "Item_Count", Language);
            public static string Page_Count(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Item_Count", "Page_Count", Language);
            public static string File_Count(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Item_Count", "File_Count", Language);
        }

        /// <summary> Phrases for the metadata-browse aggregation viewer </summary>
        public static class Metadata_Browse
        {
            /// <summary> Prefix before the (separately, legacy-translated) field name, e.g. "Browse by " + fieldName </summary>
            public static string Browse_By(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Metadata_Browse", "Browse_By", Language);
            public static string Browse_By_Colon(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Metadata_Browse", "Browse_By_Colon", Language);
            public static string Public_Browses(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Metadata_Browse", "Public_Browses", Language);
            public static string Browses(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Metadata_Browse", "Browses", Language);
            public static string Select_Field_Prompt(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Metadata_Browse", "Select_Field_Prompt", Language);
            public static string No_Matching_Values(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Metadata_Browse", "No_Matching_Values", Language);
        }

        /// <summary> Phrases for the tiles-home aggregation viewer </summary>
        public static class Tiles_Home
        {
            public static string Varies(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Tiles_Home", "Varies", Language);
        }

        /// <summary> Phrases for the thumbnails-home aggregation viewer </summary>
        public static class Thumbnails_Home
        {
            public static string Collection_Items(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Thumbnails_Home", "Collection_Items", Language);

            /// <summary> Format string with a "{0}" placeholder for the total item count </summary>
            public static string Showing_Items_Out_Of(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Thumbnails_Home", "Showing_Items_Out_Of", Language);
            public static string View_All(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Thumbnails_Home", "View_All", Language);
        }

        /// <summary> Phrases for the usage-statistics aggregation viewer </summary>
        public static class Usage_Statistics
        {
            public static string Title_Collection_Views(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Title_Collection_Views", Language);
            public static string Title_Item_Views(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Title_Item_Views", Language);
            public static string Title_Top_Titles(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Title_Top_Titles", Language);
            public static string Title_Top_Items(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Title_Top_Items", Language);
            public static string Title_Definitions(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Title_Definitions", Language);

            public static string Tab_Collection_Views(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Tab_Collection_Views", Language);
            public static string Tab_Item_Views(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Tab_Item_Views", Language);
            public static string Tab_Top_Titles(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Tab_Top_Titles", Language);
            public static string Tab_Top_Items(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Tab_Top_Items", Language);
            public static string Tab_Definitions(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Tab_Definitions", Language);

            /// <summary> Full month name for a 1-12 month number, matching the original Month_From_Int switch </summary>
            public static string Month(int Month_Int, Web_Language_Enum Language)
            {
                if ((Month_Int < 1) || (Month_Int > 12))
                    return "Invalid";
                return Localization_Store.Get("aggregations", "Usage_Statistics", "Month_" + Month_Int, Language);
            }

            public static string Collection_History_Intro(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Collection_History_Intro", Language);
            public static string Item_History_Intro(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Item_History_Intro", Language);
            public static string Items_By_Collection_Intro(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Items_By_Collection_Intro", Language);
            public static string Titles_By_Collection_Intro(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Titles_By_Collection_Intro", Language);

            /// <summary> Format string with a "{0}" placeholder for the definitions page URL </summary>
            public static string Definitions_Link_Sentence(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Definitions_Link_Sentence", Language);

            public static string Year_Statistics_Suffix(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Year_Statistics_Suffix", Language);
            public static string Total(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Total", Language);

            public static string Date(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Date", Language);
            public static string Total_Views_Html(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Total_Views_Html", Language);
            public static string Visits(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Visits", Language);
            public static string Main_Pages_Html(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Main_Pages_Html", Language);
            public static string Browses(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Browses", Language);
            public static string Search_Results_Html(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Search_Results_Html", Language);
            public static string Title_Views_Html(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Title_Views_Html", Language);
            public static string Item_Views_Html(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Item_Views_Html", Language);

            public static string Jpeg_Views_Html(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Jpeg_Views_Html", Language);
            public static string Zoomable_Views_Html(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Zoomable_Views_Html", Language);
            public static string Citation_Views_Html(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Citation_Views_Html", Language);
            public static string Thumbnail_Views_Html(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Thumbnail_Views_Html", Language);
            public static string Text_Searches_Html(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Text_Searches_Html", Language);
            public static string Flash_Views_Html(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Flash_Views_Html", Language);
            public static string Map_Views_Html(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Map_Views_Html", Language);
            public static string Download_Views_Html(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Download_Views_Html", Language);
            public static string Static_Views_Html(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Static_Views_Html", Language);

            public static string Bibid(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Bibid", Language);
            public static string Vid(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Vid", Language);
            public static string Title(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Title", Language);
            public static string Views(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Views", Language);

            /// <summary> Whole "Definitions of Terms" static HTML page (terms table of contents + all definitions),
            /// used as the fallback when no per-instance definitions text file exists. One static fragment per
            /// language with three "{0}" placeholders for the instance abbreviation (same value reused). </summary>
            public static string Definitions_Html(Web_Language_Enum Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Definitions_Html", Language);
        }

        /// <summary> Phrases shared across the citation-family item viewers (Citation_Standard, Citation_MARC,
        /// Metadata_Links, Usage_Stats, SearchEngineIndexing) — the view-selector tabs and the "dark item"
        /// suppression message are defined once and reused verbatim by every sibling viewer </summary>
        public static class Citation_Common
        {
            public static string Standard_View(Web_Language_Enum Language) => Localization_Store.Get("items", "Citation_Common", "Standard_View", Language);
            public static string Marc_View(Web_Language_Enum Language) => Localization_Store.Get("items", "Citation_Common", "Marc_View", Language);
            public static string Metadata_View(Web_Language_Enum Language) => Localization_Store.Get("items", "Citation_Common", "Metadata_View", Language);
            public static string Usage_Statistics_View(Web_Language_Enum Language) => Localization_Store.Get("items", "Citation_Common", "Usage_Statistics_View", Language);
            public static string Dark_Item_Message(Web_Language_Enum Language) => Localization_Store.Get("items", "Citation_Common", "Dark_Item_Message", Language);

            // Main-menu link text for each of these viewers (title case, distinct from the ALL CAPS tab labels above)
            public static string Menu_Standard_View(Web_Language_Enum Language) => Localization_Store.Get("items", "Citation_Common", "Menu_Standard_View", Language);
            public static string Menu_Marc_View(Web_Language_Enum Language) => Localization_Store.Get("items", "Citation_Common", "Menu_Marc_View", Language);
            public static string Menu_Metadata(Web_Language_Enum Language) => Localization_Store.Get("items", "Citation_Common", "Menu_Metadata", Language);
            public static string Menu_Usage_Statistics(Web_Language_Enum Language) => Localization_Store.Get("items", "Citation_Common", "Menu_Usage_Statistics", Language);
        }

        /// <summary> Phrases specific to the standard citation item viewer </summary>
        public static class Citation_Standard
        {
            public static string Please_Log_On_Suffix(Web_Language_Enum Language) => Localization_Store.Get("items", "Citation_Standard", "Please_Log_On_Suffix", Language);
            public static string Please_Request_Access_Suffix(Web_Language_Enum Language) => Localization_Store.Get("items", "Citation_Standard", "Please_Request_Access_Suffix", Language);
            public static string Restricted_Item_Alt(Web_Language_Enum Language) => Localization_Store.Get("items", "Citation_Standard", "Restricted_Item_Alt", Language);
        }

        /// <summary> Phrases specific to the MARC21 citation item viewer </summary>
        public static class Citation_MARC
        {
            public static string Auto_Generated_Notice(Web_Language_Enum Language) => Localization_Store.Get("items", "Citation_MARC", "Auto_Generated_Notice", Language);
        }

        /// <summary> Phrases for the metadata-links item viewer </summary>
        public static class Metadata_Links
        {
            public static string Intro_Html(Web_Language_Enum Language) => Localization_Store.Get("items", "Metadata_Links", "Intro_Html", Language);
            public static string Ead_View_Link(Web_Language_Enum Language) => Localization_Store.Get("items", "Metadata_Links", "Ead_View_Link", Language);
            public static string Ead_Description(Web_Language_Enum Language) => Localization_Store.Get("items", "Metadata_Links", "Ead_Description", Language);
            public static string Mets_View_Link(Web_Language_Enum Language) => Localization_Store.Get("items", "Metadata_Links", "Mets_View_Link", Language);
            public static string Mets_Description(Web_Language_Enum Language) => Localization_Store.Get("items", "Metadata_Links", "Mets_Description", Language);
            public static string Marc_Xml_View_Link(Web_Language_Enum Language) => Localization_Store.Get("items", "Metadata_Links", "Marc_Xml_View_Link", Language);

            /// <summary> Format string with a "{0}" placeholder for the MARC view link URL </summary>
            public static string Marc_Xml_Description(Web_Language_Enum Language) => Localization_Store.Get("items", "Metadata_Links", "Marc_Xml_Description", Language);
            public static string Tei_View_Link(Web_Language_Enum Language) => Localization_Store.Get("items", "Metadata_Links", "Tei_View_Link", Language);
            public static string Tei_Description(Web_Language_Enum Language) => Localization_Store.Get("items", "Metadata_Links", "Tei_Description", Language);
        }

        /// <summary> Phrases for the item-level usage-statistics item viewer </summary>
        public static class Item_Usage_Stats
        {
            public static string Compiled_Monthly_Notice(Web_Language_Enum Language) => Localization_Store.Get("items", "Item_Usage_Stats", "Compiled_Monthly_Notice", Language);

            /// <summary> Format string with a "{0}" placeholder for the definitions-page URL. Keeps the literal
            /// "&lt;%HITS%&gt;"/"&lt;%SESSIONS%&gt;" tokens from the original, which are substituted afterward once
            /// the totals are known (matching the original two-phase token-replace design) </summary>
            public static string Viewed_Times_Sentence(Web_Language_Enum Language) => Localization_Store.Get("items", "Item_Usage_Stats", "Viewed_Times_Sentence", Language);
            public static string Date(Web_Language_Enum Language) => Localization_Store.Get("items", "Item_Usage_Stats", "Date", Language);
            public static string Views(Web_Language_Enum Language) => Localization_Store.Get("items", "Item_Usage_Stats", "Views", Language);
            public static string Visits(Web_Language_Enum Language) => Localization_Store.Get("items", "Item_Usage_Stats", "Visits", Language);
            public static string Year_Statistics_Suffix(Web_Language_Enum Language) => Localization_Store.Get("items", "Item_Usage_Stats", "Year_Statistics_Suffix", Language);
            public static string Total(Web_Language_Enum Language) => Localization_Store.Get("items", "Item_Usage_Stats", "Total", Language);
            public static string No_Stats_Yet_Message(Web_Language_Enum Language) => Localization_Store.Get("items", "Item_Usage_Stats", "No_Stats_Yet_Message", Language);

            /// <summary> Full month name for a 1-12 month number, matching the original Month_From_Int switch </summary>
            public static string Month(int Month_Int, Web_Language_Enum Language)
            {
                if ((Month_Int < 1) || (Month_Int > 12))
                    return "Invalid";
                return Localization_Store.Get("items", "Item_Usage_Stats", "Month_" + Month_Int, Language);
            }
        }

        /// <summary> Phrases for the search-engine-indexing item viewer (the crawlable "everything about this
        /// item on one page" view used by the Builder's static-page generation) </summary>
        public static class SearchEngineIndexing
        {
            public static string Citation_Label(Web_Language_Enum Language) => Localization_Store.Get("items", "SearchEngineIndexing", "Citation_Label", Language);
            public static string Downloads_Label(Web_Language_Enum Language) => Localization_Store.Get("items", "SearchEngineIndexing", "Downloads_Label", Language);
            public static string Full_Text_Label(Web_Language_Enum Language) => Localization_Store.Get("items", "SearchEngineIndexing", "Full_Text_Label", Language);

            /// <summary> Format string with a "{0}" placeholder for the filename that failed to read </summary>
            public static string Unable_To_Read_File(Web_Language_Enum Language) => Localization_Store.Get("items", "SearchEngineIndexing", "Unable_To_Read_File", Language);
        }

        /// <summary> Phrases for the JPEG page-image item viewer </summary>
        public static class JPEG
        {
            public static string Menu_Standard(Web_Language_Enum Language) => Localization_Store.Get("items", "JPEG", "Menu_Standard", Language);
            public static string Zoomable_Switch_Prompt(Web_Language_Enum Language) => Localization_Store.Get("items", "JPEG", "Zoomable_Switch_Prompt", Language);
            public static string Zoomable_Switch_Title(Web_Language_Enum Language) => Localization_Store.Get("items", "JPEG", "Zoomable_Switch_Title", Language);
        }

        /// <summary> Phrases for the PDF item viewer </summary>
        public static class PDF
        {
            public static string Menu_Label_Default(Web_Language_Enum Language) => Localization_Store.Get("items", "PDF", "Menu_Label_Default", Language);
            public static string Error_No_Pdf_Found(Web_Language_Enum Language) => Localization_Store.Get("items", "PDF", "Error_No_Pdf_Found", Language);
            public static string Download_This_Pdf(Web_Language_Enum Language) => Localization_Store.Get("items", "PDF", "Download_This_Pdf", Language);
            public static string Download_Adobe_Reader_Alt(Web_Language_Enum Language) => Localization_Store.Get("items", "PDF", "Download_Adobe_Reader_Alt", Language);
        }

        /// <summary> Phrases for the raw page-text item viewer </summary>
        public static class Text_Viewer
        {
            public static string Menu_Text(Web_Language_Enum Language) => Localization_Store.Get("items", "Text_Viewer", "Menu_Text", Language);
            public static string Unknown_Error(Web_Language_Enum Language) => Localization_Store.Get("items", "Text_Viewer", "Unknown_Error", Language);
            public static string No_Text_File(Web_Language_Enum Language) => Localization_Store.Get("items", "Text_Viewer", "No_Text_File", Language);
            public static string No_Text_Recorded(Web_Language_Enum Language) => Localization_Store.Get("items", "Text_Viewer", "No_Text_Recorded", Language);
        }

        /// <summary> Phrases for the TEI item viewer </summary>
        public static class TEI
        {
            public static string Menu_Default_Label(Web_Language_Enum Language) => Localization_Store.Get("items", "TEI", "Menu_Default_Label", Language);

            /// <summary> Format string with a "{0}" placeholder for the XSLT transform error message </summary>
            public static string Transform_Error_Html(Web_Language_Enum Language) => Localization_Store.Get("items", "TEI", "Transform_Error_Html", Language);
        }

        /// <summary> Phrases shared by the HTML and HTML website item viewers </summary>
        public static class HTML_Viewer
        {
            public static string Menu_Default_Label(Web_Language_Enum Language) => Localization_Store.Get("items", "HTML_Viewer", "Menu_Default_Label", Language);

            /// <summary> Format string with a "{0}" placeholder for the source-file link URL </summary>
            public static string Unable_To_Pull_Html_Sentence(Web_Language_Enum Language) => Localization_Store.Get("items", "HTML_Viewer", "Unable_To_Pull_Html_Sentence", Language);
            public static string Apologize_Sentence(Web_Language_Enum Language) => Localization_Store.Get("items", "HTML_Viewer", "Apologize_Sentence", Language);

            /// <summary> Format string with a "{0}" placeholder for the contact-page URL </summary>
            public static string Report_Problem_Sentence(Web_Language_Enum Language) => Localization_Store.Get("items", "HTML_Viewer", "Report_Problem_Sentence", Language);
        }

        /// <summary> Phrases for the embedded-web-content item viewer </summary>
        public static class Embedded_Web_Content
        {
            public static string Menu_Content_Label(Web_Language_Enum Language) => Localization_Store.Get("items", "Embedded_Web_Content", "Menu_Content_Label", Language);
            public static string Viewer_Title(Web_Language_Enum Language) => Localization_Store.Get("items", "Embedded_Web_Content", "Viewer_Title", Language);
        }

        /// <summary> Phrases for the embedded-video item viewer </summary>
        public static class EmbeddedVideo
        {
            public static string Menu_Video_Label(Web_Language_Enum Language) => Localization_Store.Get("items", "EmbeddedVideo", "Menu_Video_Label", Language);
            public static string Viewer_Title(Web_Language_Enum Language) => Localization_Store.Get("items", "EmbeddedVideo", "Viewer_Title", Language);
        }

        /// <summary> Phrases for the locally-hosted video item viewer </summary>
        public static class Video
        {
            public static string Menu_Video_Label(Web_Language_Enum Language) => Localization_Store.Get("items", "Video", "Menu_Video_Label", Language);
        }

        /// <summary> Phrases for the Google-map item viewer </summary>
        public static class Google_Map
        {
            public static string Menu_Default_Label(Web_Language_Enum Language) => Localization_Store.Get("items", "Google_Map", "Menu_Default_Label", Language);
            public static string Menu_Map_Search(Web_Language_Enum Language) => Localization_Store.Get("items", "Google_Map", "Menu_Map_Search", Language);
            public static string Menu_Search_Results(Web_Language_Enum Language) => Localization_Store.Get("items", "Google_Map", "Menu_Search_Results", Language);
            public static string Menu_Map_Coverage(Web_Language_Enum Language) => Localization_Store.Get("items", "Google_Map", "Menu_Map_Coverage", Language);
            public static string Search_Button(Web_Language_Enum Language) => Localization_Store.Get("items", "Google_Map", "Search_Button", Language);
            public static string Find_Address_Button(Web_Language_Enum Language) => Localization_Store.Get("items", "Google_Map", "Find_Address_Button", Language);
            public static string Instructions_Step1_Html(Web_Language_Enum Language) => Localization_Store.Get("items", "Google_Map", "Instructions_Step1_Html", Language);
            public static string Instructions_Step2_Html(Web_Language_Enum Language) => Localization_Store.Get("items", "Google_Map", "Instructions_Step2_Html", Language);
            public static string Address_Label(Web_Language_Enum Language) => Localization_Store.Get("items", "Google_Map", "Address_Label", Language);
            public static string Address_Placeholder(Web_Language_Enum Language) => Localization_Store.Get("items", "Google_Map", "Address_Placeholder", Language);
            public static string No_Matches_Message(Web_Language_Enum Language) => Localization_Store.Get("items", "Google_Map", "No_Matches_Message", Language);
            public static string Modify_Item_Search(Web_Language_Enum Language) => Localization_Store.Get("items", "Google_Map", "Modify_Item_Search", Language);
            public static string Modify_Search_Within_Flight(Web_Language_Enum Language) => Localization_Store.Get("items", "Google_Map", "Modify_Search_Within_Flight", Language);
            public static string Search_Other_Items_Link(Web_Language_Enum Language) => Localization_Store.Get("items", "Google_Map", "Search_Other_Items_Link", Language);
            public static string Matching_Results_Intro(Web_Language_Enum Language) => Localization_Store.Get("items", "Google_Map", "Matching_Results_Intro", Language);
            public static string Zoom_To_Extent(Web_Language_Enum Language) => Localization_Store.Get("items", "Google_Map", "Zoom_To_Extent", Language);
            public static string Zoom_To_Matches(Web_Language_Enum Language) => Localization_Store.Get("items", "Google_Map", "Zoom_To_Matches", Language);
            public static string Search_All_Flights(Web_Language_Enum Language) => Localization_Store.Get("items", "Google_Map", "Search_All_Flights", Language);
            public static string Search_Entire_Collection(Web_Language_Enum Language) => Localization_Store.Get("items", "Google_Map", "Search_Entire_Collection", Language);
            public static string Google_Maps_Not_Enabled_Html(Web_Language_Enum Language) => Localization_Store.Get("items", "Google_Map", "Google_Maps_Not_Enabled_Html", Language);
        }

        /// <summary> Phrases for the GnuBooks page-turner item viewer </summary>
        public static class GnuBooks_PageTurner
        {
            public static string Menu_Default_Label(Web_Language_Enum Language) => Localization_Store.Get("items", "GnuBooks_PageTurner", "Menu_Default_Label", Language);
            public static string No_Javascript_Message(Web_Language_Enum Language) => Localization_Store.Get("items", "GnuBooks_PageTurner", "No_Javascript_Message", Language);
        }

        /// <summary> Phrases for the multi-volumes item viewer </summary>
        public static class MultiVolumes
        {
            public static string Menu_All_Volumes(Web_Language_Enum Language) => Localization_Store.Get("items", "MultiVolumes", "Menu_All_Volumes", Language);
            public static string Menu_All_Issues(Web_Language_Enum Language) => Localization_Store.Get("items", "MultiVolumes", "Menu_All_Issues", Language);
            public static string Menu_Related_Maps(Web_Language_Enum Language) => Localization_Store.Get("items", "MultiVolumes", "Menu_Related_Maps", Language);
            public static string Menu_Related_Flights(Web_Language_Enum Language) => Localization_Store.Get("items", "MultiVolumes", "Menu_Related_Flights", Language);

            // Viewer-title heading variants, by resource type (distinct wording from the menu labels above)
            public static string All_Volumes(Web_Language_Enum Language) => Localization_Store.Get("items", "MultiVolumes", "All_Volumes", Language);
            public static string All_Issues(Web_Language_Enum Language) => Localization_Store.Get("items", "MultiVolumes", "All_Issues", Language);
            public static string Related_Map_Sets(Web_Language_Enum Language) => Localization_Store.Get("items", "MultiVolumes", "Related_Map_Sets", Language);
            public static string Related_Flights(Web_Language_Enum Language) => Localization_Store.Get("items", "MultiVolumes", "Related_Flights", Language);

            public static string Related_Titles_Heading(Web_Language_Enum Language) => Localization_Store.Get("items", "MultiVolumes", "Related_Titles_Heading", Language);
            public static string Vid_Header(Web_Language_Enum Language) => Localization_Store.Get("items", "MultiVolumes", "Vid_Header", Language);
            public static string Level_1_Header(Web_Language_Enum Language) => Localization_Store.Get("items", "MultiVolumes", "Level_1_Header", Language);
            public static string Level_2_Header(Web_Language_Enum Language) => Localization_Store.Get("items", "MultiVolumes", "Level_2_Header", Language);
            public static string Level_3_Header(Web_Language_Enum Language) => Localization_Store.Get("items", "MultiVolumes", "Level_3_Header", Language);
            public static string Access_Header(Web_Language_Enum Language) => Localization_Store.Get("items", "MultiVolumes", "Access_Header", Language);
            public static string Access_Dark(Web_Language_Enum Language) => Localization_Store.Get("items", "MultiVolumes", "Access_Dark", Language);
            public static string Access_Private(Web_Language_Enum Language) => Localization_Store.Get("items", "MultiVolumes", "Access_Private", Language);
            public static string Access_Public(Web_Language_Enum Language) => Localization_Store.Get("items", "MultiVolumes", "Access_Public", Language);
            public static string Access_Restricted(Web_Language_Enum Language) => Localization_Store.Get("items", "MultiVolumes", "Access_Restricted", Language);
            public static string Missing_Thumbnail_Alt(Web_Language_Enum Language) => Localization_Store.Get("items", "MultiVolumes", "Missing_Thumbnail_Alt", Language);

            // Tree-view access-status suffixes appended after a volume's title
            public static string Dark_Suffix(Web_Language_Enum Language) => Localization_Store.Get("items", "MultiVolumes", "Dark_Suffix", Language);
            public static string Private_Suffix(Web_Language_Enum Language) => Localization_Store.Get("items", "MultiVolumes", "Private_Suffix", Language);
            public static string Restricted_Suffix(Web_Language_Enum Language) => Localization_Store.Get("items", "MultiVolumes", "Restricted_Suffix", Language);
            public static string All_Private_Or_Dark_Suffix(Web_Language_Enum Language) => Localization_Store.Get("items", "MultiVolumes", "All_Private_Or_Dark_Suffix", Language);
        }

        /// <summary> Phrases shared by the OpenTextbook and OpenTextbook_Divisions item viewers </summary>
        public static class OpenTextbook_Common
        {
            public static string Search_Label(Web_Language_Enum Language) => Localization_Store.Get("items", "OpenTextbook_Common", "Search_Label", Language);
            public static string Zoom_Label(Web_Language_Enum Language) => Localization_Store.Get("items", "OpenTextbook_Common", "Zoom_Label", Language);
            public static string Unnumbered_Page_Prefix(Web_Language_Enum Language) => Localization_Store.Get("items", "OpenTextbook_Common", "Unnumbered_Page_Prefix", Language);
            public static string Page_Prefix(Web_Language_Enum Language) => Localization_Store.Get("items", "OpenTextbook_Common", "Page_Prefix", Language);
            public static string Previous_Section_Alt(Web_Language_Enum Language) => Localization_Store.Get("items", "OpenTextbook_Common", "Previous_Section_Alt", Language);
            public static string Next_Section_Alt(Web_Language_Enum Language) => Localization_Store.Get("items", "OpenTextbook_Common", "Next_Section_Alt", Language);
        }

        /// <summary> Phrases for the OpenTextbook item viewer </summary>
        public static class OpenTextbook
        {
            public static string Menu_Default_Label(Web_Language_Enum Language) => Localization_Store.Get("items", "OpenTextbook", "Menu_Default_Label", Language);
            public static string Welcome_Heading(Web_Language_Enum Language) => Localization_Store.Get("items", "OpenTextbook", "Welcome_Heading", Language);
            public static string Welcome_Edit_Instructions(Web_Language_Enum Language) => Localization_Store.Get("items", "OpenTextbook", "Welcome_Edit_Instructions", Language);
            public static string Welcome_Toc_Instructions(Web_Language_Enum Language) => Localization_Store.Get("items", "OpenTextbook", "Welcome_Toc_Instructions", Language);
            public static string Chapter_Edit_Instructions(Web_Language_Enum Language) => Localization_Store.Get("items", "OpenTextbook", "Chapter_Edit_Instructions", Language);
        }

        /// <summary> Phrases for the OpenTextbook_Divisions item viewer </summary>
        public static class OpenTextbook_Divisions
        {
            public static string Menu_Default_Label(Web_Language_Enum Language) => Localization_Store.Get("items", "OpenTextbook_Divisions", "Menu_Default_Label", Language);
        }

        /// <summary> Phrases for the related-images (thumbnails) item viewer </summary>
        public static class Related_Images
        {
            public static string Menu_Default_Label(Web_Language_Enum Language) => Localization_Store.Get("items", "Related_Images", "Menu_Default_Label", Language);
            public static string Thumbnails_Per_Page_Suffix(Web_Language_Enum Language) => Localization_Store.Get("items", "Related_Images", "Thumbnails_Per_Page_Suffix", Language);
            public static string Go_To_Thumbnail_Label(Web_Language_Enum Language) => Localization_Store.Get("items", "Related_Images", "Go_To_Thumbnail_Label", Language);
            public static string Switch_To_Small(Web_Language_Enum Language) => Localization_Store.Get("items", "Related_Images", "Switch_To_Small", Language);
            public static string Switch_To_Medium(Web_Language_Enum Language) => Localization_Store.Get("items", "Related_Images", "Switch_To_Medium", Language);
            public static string Switch_To_Large(Web_Language_Enum Language) => Localization_Store.Get("items", "Related_Images", "Switch_To_Large", Language);
            public static string All_Thumbnails(Web_Language_Enum Language) => Localization_Store.Get("items", "Related_Images", "All_Thumbnails", Language);
            public static string Small_Alt(Web_Language_Enum Language) => Localization_Store.Get("items", "Related_Images", "Small_Alt", Language);
            public static string Medium_Alt(Web_Language_Enum Language) => Localization_Store.Get("items", "Related_Images", "Medium_Alt", Language);
            public static string Large_Alt(Web_Language_Enum Language) => Localization_Store.Get("items", "Related_Images", "Large_Alt", Language);
            public static string Missing_Thumbnail_Alt(Web_Language_Enum Language) => Localization_Store.Get("items", "Related_Images", "Missing_Thumbnail_Alt", Language);

            /// <summary> Format string with a "{0}" placeholder for the page number </summary>
            public static string Page_Number_Placeholder(Web_Language_Enum Language) => Localization_Store.Get("items", "Related_Images", "Page_Number_Placeholder", Language);
        }

        /// <summary> Phrases shared by the Downloads and Downloads_JP2s item viewers </summary>
        public static class Downloads
        {
            public static string Menu_Default_Label(Web_Language_Enum Language) => Localization_Store.Get("items", "Downloads", "Menu_Default_Label", Language);

            /// <summary> "This item has the following downloads:" — always used for French/Spanish regardless of
            /// whether the item also has page images, matching the original code's switch-default-only distinction </summary>
            public static string Explanation_Has_Downloads(Web_Language_Enum Language) => Localization_Store.Get("items", "Downloads", "Explanation_Has_Downloads", Language);

            /// <summary> "This item is only available as the following downloads:" — English only, shown instead of
            /// Explanation_Has_Downloads when the item has no page images, for any language without its own
            /// translation (the original code's French/Spanish branches never used this alternate phrasing) </summary>
            public static string Explanation_Only_Downloads(Web_Language_Enum Language) => Localization_Store.Get("items", "Downloads", "Explanation_Only_Downloads", Language);

            public static string Tiles_Available_Heading(Web_Language_Enum Language) => Localization_Store.Get("items", "Downloads", "Tiles_Available_Heading", Language);
            public static string Jpeg2000_Available_Heading(Web_Language_Enum Language) => Localization_Store.Get("items", "Downloads", "Jpeg2000_Available_Heading", Language);
            public static string Save_Link_As_Instructions(Web_Language_Enum Language) => Localization_Store.Get("items", "Downloads", "Save_Link_As_Instructions", Language);
            public static string Save_Target_As_Instructions(Web_Language_Enum Language) => Localization_Store.Get("items", "Downloads", "Save_Target_As_Instructions", Language);
        }
    }
}
