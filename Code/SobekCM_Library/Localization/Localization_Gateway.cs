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

        public static class General
        {
            public static string Get(string Term, string Language)
            {
                var dictionary = Localization_Store.Get("general", "General", Language);

                if ((dictionary == null) || (!dictionary.ContainsKey(Term))) return Term;

                return dictionary[Term];
            }
        }

        /// <summary> Phrases shared across multiple item viewers — currently just the pagination bar,
        /// defined once in Item_HtmlSubwriter and reused by every paginated item viewer, deduplicated
        /// from what used to be a near-verbatim copy inside Text_Search_ItemViewer </summary>
        public static class Common
        {
            public static string Go_To(string Language) => Localization_Store.Get("items", "Common", "Go_To", Language);
            public static string First_Page(string Language) => Localization_Store.Get("items", "Common", "First_Page", Language);
            public static string Previous_Page(string Language) => Localization_Store.Get("items", "Common", "Previous_Page", Language);
            public static string Next_Page(string Language) => Localization_Store.Get("items", "Common", "Next_Page", Language);
            public static string Last_Page(string Language) => Localization_Store.Get("items", "Common", "Last_Page", Language);
            public static string First(string Language) => Localization_Store.Get("items", "Common", "First", Language);
            public static string Previous(string Language) => Localization_Store.Get("items", "Common", "Previous", Language);
            public static string Next(string Language) => Localization_Store.Get("items", "Common", "Next", Language);
            public static string Last(string Language) => Localization_Store.Get("items", "Common", "Last", Language);
        }

        /// <summary> Phrases shared across multiple aggregation viewers (the "Go" search button, the
        /// basic search box, and the quick-tips help block defined once in abstractAggregationViewer
        /// and reused by every basic-search-flavored viewer) </summary>
        public static class Aggregation_Common
        {
            public static string Go(string Language) => Localization_Store.Get("aggregations", "Aggregation_Common", "Go", Language);
            public static string Search_Collection(string Language) => Localization_Store.Get("aggregations", "Aggregation_Common", "Search_Collection", Language);
            public static string Include_Non_Public_Items(string Language) => Localization_Store.Get("aggregations", "Aggregation_Common", "Include_Non_Public_Items", Language);
            public static string Limit_By_Year(string Language) => Localization_Store.Get("aggregations", "Aggregation_Common", "Limit_By_Year", Language);
            public static string Show_Only_Media(string Language) => Localization_Store.Get("aggregations", "Aggregation_Common", "Show_Only_Media", Language);
            public static string Through(string Language) => Localization_Store.Get("aggregations", "Aggregation_Common", "Through", Language);
            public static string Include_Full_Text(string Language) => Localization_Store.Get("aggregations", "Aggregation_Common", "Include_Full_Text", Language);
            public static string Search_All_Collections(string Language) => Localization_Store.Get("aggregations", "Aggregation_Common", "Search_All_Collections", Language);
            public static string Home(string Language) => Localization_Store.Get("aggregations", "Aggregation_Common", "Home", Language);

            /// <summary> Whole quick-tips help block (heading, list, boolean/phrase/capitalization/diacritics
            /// tips) as one static HTML fragment per language — entirely static markup+text with no dynamic
            /// data, so it's localized as a single unit rather than broken into per-sentence keys. Too large
            /// to embed in the XML config, so it's read (and cached) from design/extra/aggregations/quick_tips_{LANG}.html
            /// instead — see <see cref="SobekCM_Assistant.Get_Localized_Html_Fragment"/>. </summary>
            public static string Quick_Tips_Html(string Language) => new SobekCM_Assistant().Get_Localized_Html_Fragment("quick_tips", Language, null);
        }

        /// <summary> Phrases for the search-type tabs shown over an aggregation's search box
        /// (<see cref="SobekCM.Library.AggregationViewer.Aggregation_Nav_Bar_HTML_Factory.Menu_Get_Nav_Bar_HTML"/>) —
        /// distinct from the "Advanced Search" top-nav link in <see cref="MainMenus"/>, which is a different
        /// UI element even though the English wording overlaps </summary>
        public static class Aggregation_Nav_Bar
        {
            public static string Advanced_Search(string Language) => Localization_Store.Get("aggregations", "Aggregation_Nav_Bar", "Advanced_Search", Language);
            public static string Basic_Search(string Language) => Localization_Store.Get("aggregations", "Aggregation_Nav_Bar", "Basic_Search", Language);
            public static string Map_Search(string Language) => Localization_Store.Get("aggregations", "Aggregation_Nav_Bar", "Map_Search", Language);
            public static string Newspaper_Search(string Language) => Localization_Store.Get("aggregations", "Aggregation_Nav_Bar", "Newspaper_Search", Language);
            public static string Text_Search(string Language) => Localization_Store.Get("aggregations", "Aggregation_Nav_Bar", "Text_Search", Language);
        }

        /// <summary> Phrases for the LIST VIEW / BRIEF VIEW / TREE VIEW / THUMBNAIL VIEW tab strip shown
        /// directly on an aggregation home page body, for switching how child subcollections are displayed
        /// (<see cref="SobekCM.Library.HTML.Aggregation_HtmlSubwriter.add_home_html"/>) — distinct from the
        /// same-named List_View/Brief_View/Tree_View keys in <see cref="MainMenus"/>, which back the
        /// top-nav "Home" submenu, a different UI element </summary>
        public static class Aggregation_Home
        {
            public static string List_View(string Language) => Localization_Store.Get("aggregations", "Aggregation_Home", "List_View", Language);
            public static string Brief_View(string Language) => Localization_Store.Get("aggregations", "Aggregation_Home", "Brief_View", Language);
            public static string Tree_View(string Language) => Localization_Store.Get("aggregations", "Aggregation_Home", "Tree_View", Language);
            public static string Thumbnail_View(string Language) => Localization_Store.Get("aggregations", "Aggregation_Home", "Thumbnail_View", Language);
        }

        /// <summary> Phrases for the print/send/share buttons and "send to a friend" popup shown in the
        /// aggregation home-page search-box banner
        /// (<see cref="SobekCM.Library.HTML.Aggregation_HtmlSubwriter.Add_Sharing_Buttons"/>) </summary>
        public static class Aggregation_Sharing
        {
            public static string Print_Alt(string Language) => Localization_Store.Get("aggregations", "Aggregation_Sharing", "Print_Alt", Language);
            public static string Print_Title(string Language) => Localization_Store.Get("aggregations", "Aggregation_Sharing", "Print_Title", Language);
            public static string Send_Alt(string Language) => Localization_Store.Get("aggregations", "Aggregation_Sharing", "Send_Alt", Language);
            public static string Send_Title(string Language) => Localization_Store.Get("aggregations", "Aggregation_Sharing", "Send_Title", Language);
            public static string Share_Alt(string Language) => Localization_Store.Get("aggregations", "Aggregation_Sharing", "Share_Alt", Language);
            public static string Share_Title(string Language) => Localization_Store.Get("aggregations", "Aggregation_Sharing", "Share_Title", Language);
            public static string Remove_Alt(string Language) => Localization_Store.Get("aggregations", "Aggregation_Sharing", "Remove_Alt", Language);
            public static string Remove_Title(string Language) => Localization_Store.Get("aggregations", "Aggregation_Sharing", "Remove_Title", Language);
            public static string Add_Alt(string Language) => Localization_Store.Get("aggregations", "Aggregation_Sharing", "Add_Alt", Language);
            public static string Add_Title(string Language) => Localization_Store.Get("aggregations", "Aggregation_Sharing", "Add_Title", Language);
            public static string Save_Title(string Language) => Localization_Store.Get("aggregations", "Aggregation_Sharing", "Save_Title", Language);

            // "Send this Collection to a Friend" popup form
            public static string Email_Popup_Title(string Language) => Localization_Store.Get("aggregations", "Aggregation_Sharing", "Email_Popup_Title", Language);
            public static string Email_Popup_Close_Alt(string Language) => Localization_Store.Get("aggregations", "Aggregation_Sharing", "Email_Popup_Close_Alt", Language);
            public static string Email_Info_Prompt(string Language) => Localization_Store.Get("aggregations", "Aggregation_Sharing", "Email_Info_Prompt", Language);
            public static string To_Label(string Language) => Localization_Store.Get("aggregations", "Aggregation_Sharing", "To_Label", Language);
            public static string Comments_Label(string Language) => Localization_Store.Get("aggregations", "Aggregation_Sharing", "Comments_Label", Language);
            public static string Format_Label(string Language) => Localization_Store.Get("aggregations", "Aggregation_Sharing", "Format_Label", Language);
            public static string Html_Label(string Language) => Localization_Store.Get("aggregations", "Aggregation_Sharing", "Html_Label", Language);
            public static string Plain_Text_Label(string Language) => Localization_Store.Get("aggregations", "Aggregation_Sharing", "Plain_Text_Label", Language);
            public static string Cancel_Button(string Language) => Localization_Store.Get("aggregations", "Aggregation_Sharing", "Cancel_Button", Language);
            public static string Send_Button(string Language) => Localization_Store.Get("aggregations", "Aggregation_Sharing", "Send_Button", Language);
        }

        /// <summary> Phrases for the full-text-search aggregation viewer </summary>
        public static class Full_Text_Search_Aggregation
        {
            public static string Search_Full_Text(string Language) => Localization_Store.Get("aggregations", "Full_Text_Search", "Search_Full_Text", Language);
        }

        /// <summary> Phrases for the dLOC search aggregation viewer </summary>
        public static class DLOC_Search
        {
            public static string Include_Newspapers(string Language) => Localization_Store.Get("aggregations", "DLOC_Search", "Include_Newspapers", Language);
        }

        /// <summary> Phrases shared across the Google-Maps-based viewers (Map_Search, Map_Browse) </summary>
        public static class Map_Common
        {
            /// <summary> Error block shown in place of the map when no Google Maps API key is configured —
            /// identical markup was duplicated in both Map_Search_AggregationViewer and Map_Browse_AggregationViewer </summary>
            public static string Google_Maps_Not_Enabled_Html(string Language) => Localization_Store.Get("aggregations", "Map_Common", "Google_Maps_Not_Enabled_Html", Language);
        }

        /// <summary> Phrases for the Google-map-based search aggregation viewer </summary>
        public static class Map_Search
        {
            public static string Find_Address(string Language) => Localization_Store.Get("aggregations", "Map_Search", "Find_Address", Language);
            public static string Address(string Language) => Localization_Store.Get("aggregations", "Map_Search", "Address", Language);
            public static string Locate(string Language) => Localization_Store.Get("aggregations", "Map_Search", "Locate", Language);

            /// <summary> Instructions shown when point-searching is disabled — English text only, reused for
            /// every language, matching the original code (its Spanish branch never actually translated
            /// this fragment; both languages rendered the same English text) </summary>
            public static string Point_Disabled_Instructions_Html(string Language) => Localization_Store.Get("aggregations", "Map_Search", "Point_Disabled_Instructions_Html", Language);

            /// <summary> Normal (point-searching-enabled) map search instructions block. Too large to embed in
            /// the XML config, so it's read (and cached) from design/extra/aggregations/map_search_instructions_{LANG}.html
            /// instead — see <see cref="SobekCM_Assistant.Get_Localized_Html_Fragment"/>. </summary>
            public static string Instructions_Html(string Language) => new SobekCM_Assistant().Get_Localized_Html_Fragment("map_search_instructions", Language, null);

            /// <summary> Fallback "Map Search FAQ" block shown when no aggregation/instance-specific FAQ
            /// file exists, point-searching-enabled variant. Too large to embed in the XML config, so it's read
            /// (and cached) from design/extra/aggregations/map_search_faq_points_{LANG}.html instead — see
            /// <see cref="SobekCM_Assistant.Get_Localized_Html_Fragment"/>. </summary>
            public static string Faq_Point_Enabled_Html(string Language) => new SobekCM_Assistant().Get_Localized_Html_Fragment("map_search_faq_points", Language, null);

            /// <summary> Same fallback FAQ block, point-searching-disabled variant </summary>
            public static string Faq_Point_Disabled_Html(string Language) => Localization_Store.Get("aggregations", "Map_Search", "Faq_Point_Disabled_Html", Language);
        }

        /// <summary> Phrases for the Google-map-based browse aggregation viewer </summary>
        public static class Map_Browse
        {
            public static string Select_Point_Instructions_Html(string Language) => Localization_Store.Get("aggregations", "Map_Browse", "Select_Point_Instructions_Html", Language);

            /// <summary> Fallback "Map Browse FAQ" block. Too large to embed in the XML config, so it's read
            /// (and cached) from design/extra/aggregations/map_browse_faq_{LANG}.html instead — see
            /// <see cref="SobekCM_Assistant.Get_Localized_Html_Fragment"/>. </summary>
            public static string Faq_Html(string Language) => new SobekCM_Assistant().Get_Localized_Html_Fragment("map_browse_faq", Language, null);
            public static string More_Info_Single_Title(string Language) => Localization_Store.Get("aggregations", "Map_Browse", "More_Info_Single_Title", Language);

            /// <summary> Format string with a "{0}" placeholder for the title count — e.g.
            /// string.Format(Localization_Gateway.Map_Browse.More_Info_Multiple_Titles(language), count) </summary>
            public static string More_Info_Multiple_Titles(string Language) => Localization_Store.Get("aggregations", "Map_Browse", "More_Info_Multiple_Titles", Language);
        }

        /// <summary> Phrases for the newspaper-search aggregation viewer </summary>
        public static class Newspaper_Search
        {
            public static string Full_Citation(string Language) => Localization_Store.Get("aggregations", "Newspaper_Search", "Full_Citation", Language);
            public static string Full_Text(string Language) => Localization_Store.Get("aggregations", "Newspaper_Search", "Full_Text", Language);
            public static string Newspaper_Title(string Language) => Localization_Store.Get("aggregations", "Newspaper_Search", "Newspaper_Title", Language);
            public static string Location(string Language) => Localization_Store.Get("aggregations", "Newspaper_Search", "Location", Language);
        }

        /// <summary> Phrases for the advanced search aggregation viewer </summary>
        public static class Advanced_Search
        {
            public static string Search_For(string Language) => Localization_Store.Get("aggregations", "Advanced_Search", "Search_For", Language);
            public static string In(string Language) => Localization_Store.Get("aggregations", "Advanced_Search", "In", Language);
            public static string Search(string Language) => Localization_Store.Get("aggregations", "Advanced_Search", "Search", Language);
            public static string Search_Options(string Language) => Localization_Store.Get("aggregations", "Advanced_Search", "Search_Options", Language);
            public static string Precision(string Language) => Localization_Store.Get("aggregations", "Advanced_Search", "Precision", Language);
            public static string Contains_Exactly(string Language) => Localization_Store.Get("aggregations", "Advanced_Search", "Contains_Exactly", Language);
            public static string Contains_Any_Form(string Language) => Localization_Store.Get("aggregations", "Advanced_Search", "Contains_Any_Form", Language);
            public static string Contains_Meaning(string Language) => Localization_Store.Get("aggregations", "Advanced_Search", "Contains_Meaning", Language);
            public static string And(string Language) => Localization_Store.Get("aggregations", "Advanced_Search", "And", Language);
            public static string Or(string Language) => Localization_Store.Get("aggregations", "Advanced_Search", "Or", Language);
            public static string And_Not(string Language) => Localization_Store.Get("aggregations", "Advanced_Search", "And_Not", Language);
        }

        /// <summary> Phrases for the full-text-search item viewer </summary>
        public static class Text_Search
        {
            public static string Search_This_Document(string Language) => Localization_Store.Get("items", "Text_Search", "Search_This_Document", Language);
            public static string Go(string Language) => Localization_Store.Get("items", "Text_Search", "Go", Language);

            /// <summary> Whole "Quick Tips" help block (document/boolean/phrase/capitalization/diacritics
            /// searching tips) as one static HTML fragment per language, same reasoning as
            /// Aggregation_Common.Quick_Tips_Html. Too large to embed in the XML config, so it's read (and
            /// cached) from design/extra/aggregations/text_search_quick_tips_{LANG}.html instead — see
            /// <see cref="SobekCM_Assistant.Get_Localized_Html_Fragment"/>. </summary>
            public static string Quick_Tips_Html(string Language) => new SobekCM_Assistant().Get_Localized_Html_Fragment("text_search_quick_tips", Language, null);

            // Sentence-composition fragments used by Compute_Search_Explanation() — concatenated in
            // sequence around dynamic search terms/counts, so each connector/fragment is its own key
            // rather than one templated sentence
            public static string Your_Search_For(string Language) => Localization_Store.Get("items", "Text_Search", "Your_Search_For", Language);
            public static string And_Not(string Language) => Localization_Store.Get("items", "Text_Search", "And_Not", Language);
            public static string And(string Language) => Localization_Store.Get("items", "Text_Search", "And", Language);
            public static string Or(string Language) => Localization_Store.Get("items", "Text_Search", "Or", Language);
            public static string Not(string Language) => Localization_Store.Get("items", "Text_Search", "Not", Language);
            public static string Resulted_In(string Language) => Localization_Store.Get("items", "Text_Search", "Resulted_In", Language);
            public static string Matching_Pages(string Language) => Localization_Store.Get("items", "Text_Search", "Matching_Pages", Language);
            public static string No_Matching_Pages(string Language) => Localization_Store.Get("items", "Text_Search", "No_Matching_Pages", Language);
            public static string Expand_Results(string Language) => Localization_Store.Get("items", "Text_Search", "Expand_Results", Language);
            public static string Restrict_Results(string Language) => Localization_Store.Get("items", "Text_Search", "Restrict_Results", Language);
        }

        /// <summary> Phrases for the JPEG2000 (OpenSeadragon zoomable image) item viewer </summary>
        public static class JPEG2000
        {
            public static string Thumbnail(string Language) => Localization_Store.Get("items", "JPEG2000", "Thumbnail", Language);
        }

        /// <summary> Phrases for the item-count aggregation viewer </summary>
        public static class Item_Count
        {
            public static string Resource_Count_In_Collection(string Language) => Localization_Store.Get("aggregations", "Item_Count", "Resource_Count_In_Collection", Language);
            public static string Description(string Language) => Localization_Store.Get("aggregations", "Item_Count", "Description", Language);
            public static string Visibility(string Language) => Localization_Store.Get("aggregations", "Item_Count", "Visibility", Language);
            public static string Title_Count(string Language) => Localization_Store.Get("aggregations", "Item_Count", "Title_Count", Language);
            public static string Items(string Language) => Localization_Store.Get("aggregations", "Item_Count", "Item_Count", Language);
            public static string Page_Count(string Language) => Localization_Store.Get("aggregations", "Item_Count", "Page_Count", Language);
            public static string File_Count(string Language) => Localization_Store.Get("aggregations", "Item_Count", "File_Count", Language);
        }

        /// <summary> Phrases for the metadata-browse aggregation viewer </summary>
        public static class Metadata_Browse
        {
            /// <summary> Prefix before the (separately, legacy-translated) field name, e.g. "Browse by " + fieldName </summary>
            public static string Browse_By(string Language) => Localization_Store.Get("aggregations", "Metadata_Browse", "Browse_By", Language);
            public static string Browse_By_Colon(string Language) => Localization_Store.Get("aggregations", "Metadata_Browse", "Browse_By_Colon", Language);
            public static string Public_Browses(string Language) => Localization_Store.Get("aggregations", "Metadata_Browse", "Public_Browses", Language);
            public static string Internal_Browses(string Language) => Localization_Store.Get("aggregations", "Metadata_Browse", "Internal_Browses", Language);
            public static string Browses(string Language) => Localization_Store.Get("aggregations", "Metadata_Browse", "Browses", Language);
            public static string Select_Field_Prompt(string Language) => Localization_Store.Get("aggregations", "Metadata_Browse", "Select_Field_Prompt", Language);
            public static string No_Matching_Values(string Language) => Localization_Store.Get("aggregations", "Metadata_Browse", "No_Matching_Values", Language);
        }

        /// <summary> Phrases for the tiles-home aggregation viewer </summary>
        public static class Tiles_Home
        {
            public static string Varies(string Language) => Localization_Store.Get("aggregations", "Tiles_Home", "Varies", Language);
        }

        /// <summary> Phrases for the thumbnails-home aggregation viewer </summary>
        public static class Thumbnails_Home
        {
            public static string Collection_Items(string Language) => Localization_Store.Get("aggregations", "Thumbnails_Home", "Collection_Items", Language);

            /// <summary> Format string with a "{0}" placeholder for the total item count </summary>
            public static string Showing_Items_Out_Of(string Language) => Localization_Store.Get("aggregations", "Thumbnails_Home", "Showing_Items_Out_Of", Language);
            public static string View_All(string Language) => Localization_Store.Get("aggregations", "Thumbnails_Home", "View_All", Language);
        }

        /// <summary> Phrases for the usage-statistics aggregation viewer </summary>
        public static class Usage_Statistics
        {
            public static string Title_Collection_Views(string Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Title_Collection_Views", Language);
            public static string Title_Item_Views(string Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Title_Item_Views", Language);
            public static string Title_Top_Titles(string Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Title_Top_Titles", Language);
            public static string Title_Top_Items(string Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Title_Top_Items", Language);
            public static string Title_Definitions(string Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Title_Definitions", Language);

            public static string Tab_Collection_Views(string Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Tab_Collection_Views", Language);
            public static string Tab_Item_Views(string Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Tab_Item_Views", Language);
            public static string Tab_Top_Titles(string Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Tab_Top_Titles", Language);
            public static string Tab_Top_Items(string Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Tab_Top_Items", Language);
            public static string Tab_Definitions(string Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Tab_Definitions", Language);

            /// <summary> Full month name for a 1-12 month number, matching the original Month_From_Int switch </summary>
            public static string Month(int Month_Int, string Language)
            {
                if ((Month_Int < 1) || (Month_Int > 12))
                    return "Invalid";
                return Localization_Store.Get("aggregations", "Usage_Statistics", "Month_" + Month_Int, Language);
            }

            public static string Collection_History_Intro(string Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Collection_History_Intro", Language);
            public static string Item_History_Intro(string Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Item_History_Intro", Language);
            public static string Items_By_Collection_Intro(string Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Items_By_Collection_Intro", Language);
            public static string Titles_By_Collection_Intro(string Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Titles_By_Collection_Intro", Language);

            /// <summary> Format string with a "{0}" placeholder for the definitions page URL </summary>
            public static string Definitions_Link_Sentence(string Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Definitions_Link_Sentence", Language);

            public static string Year_Statistics_Suffix(string Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Year_Statistics_Suffix", Language);
            public static string Total(string Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Total", Language);

            public static string Date(string Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Date", Language);
            public static string Total_Views_Html(string Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Total_Views_Html", Language);
            public static string Visits(string Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Visits", Language);
            public static string Main_Pages_Html(string Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Main_Pages_Html", Language);
            public static string Browses(string Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Browses", Language);
            public static string Search_Results_Html(string Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Search_Results_Html", Language);
            public static string Title_Views_Html(string Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Title_Views_Html", Language);
            public static string Item_Views_Html(string Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Item_Views_Html", Language);

            public static string Jpeg_Views_Html(string Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Jpeg_Views_Html", Language);
            public static string Zoomable_Views_Html(string Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Zoomable_Views_Html", Language);
            public static string Citation_Views_Html(string Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Citation_Views_Html", Language);
            public static string Thumbnail_Views_Html(string Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Thumbnail_Views_Html", Language);
            public static string Text_Searches_Html(string Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Text_Searches_Html", Language);
            public static string Flash_Views_Html(string Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Flash_Views_Html", Language);
            public static string Map_Views_Html(string Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Map_Views_Html", Language);
            public static string Download_Views_Html(string Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Download_Views_Html", Language);
            public static string Static_Views_Html(string Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Static_Views_Html", Language);

            public static string Bibid(string Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Bibid", Language);
            public static string Vid(string Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Vid", Language);
            public static string Title(string Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Title", Language);
            public static string Views(string Language) => Localization_Store.Get("aggregations", "Usage_Statistics", "Views", Language);

            /// <summary> Whole "Definitions of Terms" static HTML page (terms table of contents + all definitions),
            /// used as the fallback when no per-instance definitions text file exists. One static fragment per
            /// language with three "{0}" placeholders for the instance abbreviation (same value reused). Too large
            /// to embed in the XML config, so it's read (and cached) from
            /// design/extra/aggregations/usage_stats_definitions_{LANG}.html instead — see
            /// <see cref="SobekCM_Assistant.Get_Localized_Html_Fragment"/>. </summary>
            public static string Definitions_Html(string Language) => new SobekCM_Assistant().Get_Localized_Html_Fragment("usage_stats_definitions", Language, null);
        }

        /// <summary> Phrases shared across the citation-family item viewers (Citation_Standard, Citation_MARC,
        /// Metadata_Links, Usage_Stats, SearchEngineIndexing) — the view-selector tabs and the "dark item"
        /// suppression message are defined once and reused verbatim by every sibling viewer </summary>
        public static class Citation_Common
        {
            public static string Standard_View(string Language) => Localization_Store.Get("items", "Citation_Common", "Standard_View", Language);
            public static string Marc_View(string Language) => Localization_Store.Get("items", "Citation_Common", "Marc_View", Language);
            public static string Metadata_View(string Language) => Localization_Store.Get("items", "Citation_Common", "Metadata_View", Language);
            public static string Usage_Statistics_View(string Language) => Localization_Store.Get("items", "Citation_Common", "Usage_Statistics_View", Language);
            public static string Dark_Item_Message(string Language) => Localization_Store.Get("items", "Citation_Common", "Dark_Item_Message", Language);

            // Main-menu link text for each of these viewers (title case, distinct from the ALL CAPS tab labels above)
            public static string Menu_Standard_View(string Language) => Localization_Store.Get("items", "Citation_Common", "Menu_Standard_View", Language);
            public static string Menu_Marc_View(string Language) => Localization_Store.Get("items", "Citation_Common", "Menu_Marc_View", Language);
            public static string Menu_Metadata(string Language) => Localization_Store.Get("items", "Citation_Common", "Menu_Metadata", Language);
            public static string Menu_Usage_Statistics(string Language) => Localization_Store.Get("items", "Citation_Common", "Menu_Usage_Statistics", Language);
        }

        /// <summary> Phrases specific to the standard citation item viewer </summary>
        public static class Citation_Standard
        {
            public static string Please_Log_On_Suffix(string Language) => Localization_Store.Get("items", "Citation_Standard", "Please_Log_On_Suffix", Language);
            public static string Please_Request_Access_Suffix(string Language) => Localization_Store.Get("items", "Citation_Standard", "Please_Request_Access_Suffix", Language);
            public static string Restricted_Item_Alt(string Language) => Localization_Store.Get("items", "Citation_Standard", "Restricted_Item_Alt", Language);
        }

        /// <summary> Phrases specific to the MARC21 citation item viewer </summary>
        public static class Citation_MARC
        {
            public static string Auto_Generated_Notice(string Language) => Localization_Store.Get("items", "Citation_MARC", "Auto_Generated_Notice", Language);
        }

        /// <summary> Phrases for the metadata-links item viewer </summary>
        public static class Metadata_Links
        {
            public static string Intro_Html(string Language) => Localization_Store.Get("items", "Metadata_Links", "Intro_Html", Language);
            public static string Ead_View_Link(string Language) => Localization_Store.Get("items", "Metadata_Links", "Ead_View_Link", Language);
            public static string Ead_Description(string Language) => Localization_Store.Get("items", "Metadata_Links", "Ead_Description", Language);
            public static string Mets_View_Link(string Language) => Localization_Store.Get("items", "Metadata_Links", "Mets_View_Link", Language);
            public static string Mets_Description(string Language) => Localization_Store.Get("items", "Metadata_Links", "Mets_Description", Language);
            public static string Marc_Xml_View_Link(string Language) => Localization_Store.Get("items", "Metadata_Links", "Marc_Xml_View_Link", Language);

            /// <summary> Format string with a "{0}" placeholder for the MARC view link URL </summary>
            public static string Marc_Xml_Description(string Language) => Localization_Store.Get("items", "Metadata_Links", "Marc_Xml_Description", Language);
            public static string Tei_View_Link(string Language) => Localization_Store.Get("items", "Metadata_Links", "Tei_View_Link", Language);
            public static string Tei_Description(string Language) => Localization_Store.Get("items", "Metadata_Links", "Tei_Description", Language);
        }

        /// <summary> Phrases for the item-level usage-statistics item viewer </summary>
        public static class Item_Usage_Stats
        {
            public static string Compiled_Monthly_Notice(string Language) => Localization_Store.Get("items", "Item_Usage_Stats", "Compiled_Monthly_Notice", Language);

            /// <summary> Format string with a "{0}" placeholder for the definitions-page URL. Keeps the literal
            /// "&lt;%HITS%&gt;"/"&lt;%SESSIONS%&gt;" tokens from the original, which are substituted afterward once
            /// the totals are known (matching the original two-phase token-replace design) </summary>
            public static string Viewed_Times_Sentence(string Language) => Localization_Store.Get("items", "Item_Usage_Stats", "Viewed_Times_Sentence", Language);
            public static string Date(string Language) => Localization_Store.Get("items", "Item_Usage_Stats", "Date", Language);
            public static string Views(string Language) => Localization_Store.Get("items", "Item_Usage_Stats", "Views", Language);
            public static string Visits(string Language) => Localization_Store.Get("items", "Item_Usage_Stats", "Visits", Language);
            public static string Year_Statistics_Suffix(string Language) => Localization_Store.Get("items", "Item_Usage_Stats", "Year_Statistics_Suffix", Language);
            public static string Total(string Language) => Localization_Store.Get("items", "Item_Usage_Stats", "Total", Language);
            public static string No_Stats_Yet_Message(string Language) => Localization_Store.Get("items", "Item_Usage_Stats", "No_Stats_Yet_Message", Language);

            /// <summary> Full month name for a 1-12 month number, matching the original Month_From_Int switch </summary>
            public static string Month(int Month_Int, string Language)
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
            public static string Citation_Label(string Language) => Localization_Store.Get("items", "SearchEngineIndexing", "Citation_Label", Language);
            public static string Downloads_Label(string Language) => Localization_Store.Get("items", "SearchEngineIndexing", "Downloads_Label", Language);
            public static string Full_Text_Label(string Language) => Localization_Store.Get("items", "SearchEngineIndexing", "Full_Text_Label", Language);

            /// <summary> Format string with a "{0}" placeholder for the filename that failed to read </summary>
            public static string Unable_To_Read_File(string Language) => Localization_Store.Get("items", "SearchEngineIndexing", "Unable_To_Read_File", Language);
        }

        /// <summary> Phrases for the JPEG page-image item viewer </summary>
        public static class JPEG
        {
            public static string Menu_Standard(string Language) => Localization_Store.Get("items", "JPEG", "Menu_Standard", Language);
            public static string Zoomable_Switch_Prompt(string Language) => Localization_Store.Get("items", "JPEG", "Zoomable_Switch_Prompt", Language);
            public static string Zoomable_Switch_Title(string Language) => Localization_Store.Get("items", "JPEG", "Zoomable_Switch_Title", Language);
        }

        /// <summary> Phrases for the PDF item viewer </summary>
        public static class PDF
        {
            public static string Menu_Label_Default(string Language) => Localization_Store.Get("items", "PDF", "Menu_Label_Default", Language);
            public static string Error_No_Pdf_Found(string Language) => Localization_Store.Get("items", "PDF", "Error_No_Pdf_Found", Language);
            public static string Download_This_Pdf(string Language) => Localization_Store.Get("items", "PDF", "Download_This_Pdf", Language);
            public static string Download_Adobe_Reader_Alt(string Language) => Localization_Store.Get("items", "PDF", "Download_Adobe_Reader_Alt", Language);
        }

        /// <summary> Phrases for the raw page-text item viewer </summary>
        public static class Text_Viewer
        {
            public static string Menu_Text(string Language) => Localization_Store.Get("items", "Text_Viewer", "Menu_Text", Language);
            public static string Unknown_Error(string Language) => Localization_Store.Get("items", "Text_Viewer", "Unknown_Error", Language);
            public static string No_Text_File(string Language) => Localization_Store.Get("items", "Text_Viewer", "No_Text_File", Language);
            public static string No_Text_Recorded(string Language) => Localization_Store.Get("items", "Text_Viewer", "No_Text_Recorded", Language);
        }

        /// <summary> Phrases for the TEI item viewer </summary>
        public static class TEI
        {
            public static string Menu_Default_Label(string Language) => Localization_Store.Get("items", "TEI", "Menu_Default_Label", Language);

            /// <summary> Format string with a "{0}" placeholder for the XSLT transform error message </summary>
            public static string Transform_Error_Html(string Language) => Localization_Store.Get("items", "TEI", "Transform_Error_Html", Language);
        }

        /// <summary> Phrases shared by the HTML and HTML website item viewers </summary>
        public static class HTML_Viewer
        {
            public static string Menu_Default_Label(string Language) => Localization_Store.Get("items", "HTML_Viewer", "Menu_Default_Label", Language);

            /// <summary> Format string with a "{0}" placeholder for the source-file link URL </summary>
            public static string Unable_To_Pull_Html_Sentence(string Language) => Localization_Store.Get("items", "HTML_Viewer", "Unable_To_Pull_Html_Sentence", Language);
            public static string Apologize_Sentence(string Language) => Localization_Store.Get("items", "HTML_Viewer", "Apologize_Sentence", Language);

            /// <summary> Format string with a "{0}" placeholder for the contact-page URL </summary>
            public static string Report_Problem_Sentence(string Language) => Localization_Store.Get("items", "HTML_Viewer", "Report_Problem_Sentence", Language);
        }

        /// <summary> Phrases for the embedded-web-content item viewer </summary>
        public static class Embedded_Web_Content
        {
            public static string Menu_Content_Label(string Language) => Localization_Store.Get("items", "Embedded_Web_Content", "Menu_Content_Label", Language);
            public static string Viewer_Title(string Language) => Localization_Store.Get("items", "Embedded_Web_Content", "Viewer_Title", Language);
        }

        /// <summary> Phrases for the embedded-video item viewer </summary>
        public static class EmbeddedVideo
        {
            public static string Menu_Video_Label(string Language) => Localization_Store.Get("items", "EmbeddedVideo", "Menu_Video_Label", Language);
            public static string Viewer_Title(string Language) => Localization_Store.Get("items", "EmbeddedVideo", "Viewer_Title", Language);
        }

        /// <summary> Phrases for the locally-hosted video item viewer </summary>
        public static class Video
        {
            public static string Menu_Video_Label(string Language) => Localization_Store.Get("items", "Video", "Menu_Video_Label", Language);
        }

        /// <summary> Phrases for the Google-map item viewer </summary>
        public static class Google_Map
        {
            public static string Menu_Default_Label(string Language) => Localization_Store.Get("items", "Google_Map", "Menu_Default_Label", Language);
            public static string Menu_Map_Search(string Language) => Localization_Store.Get("items", "Google_Map", "Menu_Map_Search", Language);
            public static string Menu_Search_Results(string Language) => Localization_Store.Get("items", "Google_Map", "Menu_Search_Results", Language);
            public static string Menu_Map_Coverage(string Language) => Localization_Store.Get("items", "Google_Map", "Menu_Map_Coverage", Language);
            public static string Search_Button(string Language) => Localization_Store.Get("items", "Google_Map", "Search_Button", Language);
            public static string Find_Address_Button(string Language) => Localization_Store.Get("items", "Google_Map", "Find_Address_Button", Language);
            public static string Instructions_Step1_Html(string Language) => Localization_Store.Get("items", "Google_Map", "Instructions_Step1_Html", Language);
            public static string Instructions_Step2_Html(string Language) => Localization_Store.Get("items", "Google_Map", "Instructions_Step2_Html", Language);
            public static string Address_Label(string Language) => Localization_Store.Get("items", "Google_Map", "Address_Label", Language);
            public static string Address_Placeholder(string Language) => Localization_Store.Get("items", "Google_Map", "Address_Placeholder", Language);
            public static string No_Matches_Message(string Language) => Localization_Store.Get("items", "Google_Map", "No_Matches_Message", Language);
            public static string Modify_Item_Search(string Language) => Localization_Store.Get("items", "Google_Map", "Modify_Item_Search", Language);
            public static string Modify_Search_Within_Flight(string Language) => Localization_Store.Get("items", "Google_Map", "Modify_Search_Within_Flight", Language);
            public static string Search_Other_Items_Link(string Language) => Localization_Store.Get("items", "Google_Map", "Search_Other_Items_Link", Language);
            public static string Matching_Results_Intro(string Language) => Localization_Store.Get("items", "Google_Map", "Matching_Results_Intro", Language);
            public static string Zoom_To_Extent(string Language) => Localization_Store.Get("items", "Google_Map", "Zoom_To_Extent", Language);
            public static string Zoom_To_Matches(string Language) => Localization_Store.Get("items", "Google_Map", "Zoom_To_Matches", Language);
            public static string Search_All_Flights(string Language) => Localization_Store.Get("items", "Google_Map", "Search_All_Flights", Language);
            public static string Search_Entire_Collection(string Language) => Localization_Store.Get("items", "Google_Map", "Search_Entire_Collection", Language);
            public static string Google_Maps_Not_Enabled_Html(string Language) => Localization_Store.Get("items", "Google_Map", "Google_Maps_Not_Enabled_Html", Language);
        }

        /// <summary> Phrases for the GnuBooks page-turner item viewer </summary>
        public static class GnuBooks_PageTurner
        {
            public static string Menu_Default_Label(string Language) => Localization_Store.Get("items", "GnuBooks_PageTurner", "Menu_Default_Label", Language);
            public static string No_Javascript_Message(string Language) => Localization_Store.Get("items", "GnuBooks_PageTurner", "No_Javascript_Message", Language);
        }

        /// <summary> Phrases for the multi-volumes item viewer </summary>
        public static class MultiVolumes
        {
            public static string Menu_All_Volumes(string Language) => Localization_Store.Get("items", "MultiVolumes", "Menu_All_Volumes", Language);
            public static string Menu_All_Issues(string Language) => Localization_Store.Get("items", "MultiVolumes", "Menu_All_Issues", Language);
            public static string Menu_Related_Maps(string Language) => Localization_Store.Get("items", "MultiVolumes", "Menu_Related_Maps", Language);
            public static string Menu_Related_Flights(string Language) => Localization_Store.Get("items", "MultiVolumes", "Menu_Related_Flights", Language);

            // Viewer-title heading variants, by resource type (distinct wording from the menu labels above)
            public static string All_Volumes(string Language) => Localization_Store.Get("items", "MultiVolumes", "All_Volumes", Language);
            public static string All_Issues(string Language) => Localization_Store.Get("items", "MultiVolumes", "All_Issues", Language);
            public static string Related_Map_Sets(string Language) => Localization_Store.Get("items", "MultiVolumes", "Related_Map_Sets", Language);
            public static string Related_Flights(string Language) => Localization_Store.Get("items", "MultiVolumes", "Related_Flights", Language);

            public static string Related_Titles_Heading(string Language) => Localization_Store.Get("items", "MultiVolumes", "Related_Titles_Heading", Language);
            public static string Vid_Header(string Language) => Localization_Store.Get("items", "MultiVolumes", "Vid_Header", Language);
            public static string Level_1_Header(string Language) => Localization_Store.Get("items", "MultiVolumes", "Level_1_Header", Language);
            public static string Level_2_Header(string Language) => Localization_Store.Get("items", "MultiVolumes", "Level_2_Header", Language);
            public static string Level_3_Header(string Language) => Localization_Store.Get("items", "MultiVolumes", "Level_3_Header", Language);
            public static string Access_Header(string Language) => Localization_Store.Get("items", "MultiVolumes", "Access_Header", Language);
            public static string Access_Dark(string Language) => Localization_Store.Get("items", "MultiVolumes", "Access_Dark", Language);
            public static string Access_Private(string Language) => Localization_Store.Get("items", "MultiVolumes", "Access_Private", Language);
            public static string Access_Public(string Language) => Localization_Store.Get("items", "MultiVolumes", "Access_Public", Language);
            public static string Access_Restricted(string Language) => Localization_Store.Get("items", "MultiVolumes", "Access_Restricted", Language);
            public static string Missing_Thumbnail_Alt(string Language) => Localization_Store.Get("items", "MultiVolumes", "Missing_Thumbnail_Alt", Language);

            // Tree-view access-status suffixes appended after a volume's title
            public static string Dark_Suffix(string Language) => Localization_Store.Get("items", "MultiVolumes", "Dark_Suffix", Language);
            public static string Private_Suffix(string Language) => Localization_Store.Get("items", "MultiVolumes", "Private_Suffix", Language);
            public static string Restricted_Suffix(string Language) => Localization_Store.Get("items", "MultiVolumes", "Restricted_Suffix", Language);
            public static string All_Private_Or_Dark_Suffix(string Language) => Localization_Store.Get("items", "MultiVolumes", "All_Private_Or_Dark_Suffix", Language);
        }

        /// <summary> Phrases shared by the OpenTextbook and OpenTextbook_Divisions item viewers </summary>
        public static class OpenTextbook_Common
        {
            public static string Search_Label(string Language) => Localization_Store.Get("items", "OpenTextbook_Common", "Search_Label", Language);
            public static string Zoom_Label(string Language) => Localization_Store.Get("items", "OpenTextbook_Common", "Zoom_Label", Language);
            public static string Unnumbered_Page_Prefix(string Language) => Localization_Store.Get("items", "OpenTextbook_Common", "Unnumbered_Page_Prefix", Language);
            public static string Page_Prefix(string Language) => Localization_Store.Get("items", "OpenTextbook_Common", "Page_Prefix", Language);
            public static string Previous_Section_Alt(string Language) => Localization_Store.Get("items", "OpenTextbook_Common", "Previous_Section_Alt", Language);
            public static string Next_Section_Alt(string Language) => Localization_Store.Get("items", "OpenTextbook_Common", "Next_Section_Alt", Language);
        }

        /// <summary> Phrases for the OpenTextbook item viewer </summary>
        public static class OpenTextbook
        {
            public static string Menu_Default_Label(string Language) => Localization_Store.Get("items", "OpenTextbook", "Menu_Default_Label", Language);
            public static string Welcome_Heading(string Language) => Localization_Store.Get("items", "OpenTextbook", "Welcome_Heading", Language);
            public static string Welcome_Edit_Instructions(string Language) => Localization_Store.Get("items", "OpenTextbook", "Welcome_Edit_Instructions", Language);
            public static string Welcome_Toc_Instructions(string Language) => Localization_Store.Get("items", "OpenTextbook", "Welcome_Toc_Instructions", Language);
            public static string Chapter_Edit_Instructions(string Language) => Localization_Store.Get("items", "OpenTextbook", "Chapter_Edit_Instructions", Language);
        }

        /// <summary> Phrases for the OpenTextbook_Divisions item viewer </summary>
        public static class OpenTextbook_Divisions
        {
            public static string Menu_Default_Label(string Language) => Localization_Store.Get("items", "OpenTextbook_Divisions", "Menu_Default_Label", Language);
        }

        /// <summary> Phrases for the related-images (thumbnails) item viewer </summary>
        public static class Related_Images
        {
            public static string Menu_Default_Label(string Language) => Localization_Store.Get("items", "Related_Images", "Menu_Default_Label", Language);
            public static string Thumbnails_Per_Page_Suffix(string Language) => Localization_Store.Get("items", "Related_Images", "Thumbnails_Per_Page_Suffix", Language);
            public static string Go_To_Thumbnail_Label(string Language) => Localization_Store.Get("items", "Related_Images", "Go_To_Thumbnail_Label", Language);
            public static string Switch_To_Small(string Language) => Localization_Store.Get("items", "Related_Images", "Switch_To_Small", Language);
            public static string Switch_To_Medium(string Language) => Localization_Store.Get("items", "Related_Images", "Switch_To_Medium", Language);
            public static string Switch_To_Large(string Language) => Localization_Store.Get("items", "Related_Images", "Switch_To_Large", Language);
            public static string All_Thumbnails(string Language) => Localization_Store.Get("items", "Related_Images", "All_Thumbnails", Language);
            public static string Small_Alt(string Language) => Localization_Store.Get("items", "Related_Images", "Small_Alt", Language);
            public static string Medium_Alt(string Language) => Localization_Store.Get("items", "Related_Images", "Medium_Alt", Language);
            public static string Large_Alt(string Language) => Localization_Store.Get("items", "Related_Images", "Large_Alt", Language);
            public static string Missing_Thumbnail_Alt(string Language) => Localization_Store.Get("items", "Related_Images", "Missing_Thumbnail_Alt", Language);

            /// <summary> Format string with a "{0}" placeholder for the page number </summary>
            public static string Page_Number_Placeholder(string Language) => Localization_Store.Get("items", "Related_Images", "Page_Number_Placeholder", Language);
        }

        /// <summary> Phrases for the logged-on user's mySobek home page (Home_MySobekViewer) </summary>
        public static class Home
        {
            /// <summary> Format string with "{0}" (instance abbreviation, immediately following "my" with no
            /// space) and "{1}" (nickname or given name) placeholders </summary>
            public static string Welcome_New_User_Format(string Language) => Localization_Store.Get("mysobek", "Home", "Welcome_New_User_Format", Language);

            /// <summary> Format string with a "{0}" placeholder for the nickname or given name </summary>
            public static string Welcome_Back_Format(string Language) => Localization_Store.Get("mysobek", "Home", "Welcome_Back_Format", Language);

            /// <summary> Format string with a "{0}" placeholder for the "my{Instance}" text </summary>
            public static string Welcome_Intro_Format(string Language) => Localization_Store.Get("mysobek", "Home", "Welcome_Intro_Format", Language);

            public static string What_Would_You_Like(string Language) => Localization_Store.Get("mysobek", "Home", "What_Would_You_Like", Language);
            public static string Upload_Existing_Oer(string Language) => Localization_Store.Get("mysobek", "Home", "Upload_Existing_Oer", Language);
            public static string Create_New_Oer_Online(string Language) => Localization_Store.Get("mysobek", "Home", "Create_New_Oer_Online", Language);
            public static string Upload_New_Item(string Language) => Localization_Store.Get("mysobek", "Home", "Upload_New_Item", Language);
            public static string Add_New_Tei_Item(string Language) => Localization_Store.Get("mysobek", "Home", "Add_New_Tei_Item", Language);
            public static string Submittals_Disabled(string Language) => Localization_Store.Get("mysobek", "Home", "Submittals_Disabled", Language);
            public static string View_All_Submitted_Items(string Language) => Localization_Store.Get("mysobek", "Home", "View_All_Submitted_Items", Language);
            public static string View_Usage_For_My_Items(string Language) => Localization_Store.Get("mysobek", "Home", "View_Usage_For_My_Items", Language);
            public static string View_My_Descriptive_Tags(string Language) => Localization_Store.Get("mysobek", "Home", "View_My_Descriptive_Tags", Language);
            public static string View_And_Organize_Bookshelves(string Language) => Localization_Store.Get("mysobek", "Home", "View_And_Organize_Bookshelves", Language);
            public static string View_My_Saved_Searches(string Language) => Localization_Store.Get("mysobek", "Home", "View_My_Saved_Searches", Language);
            public static string Edit_My_Preferences(string Language) => Localization_Store.Get("mysobek", "Home", "Edit_My_Preferences", Language);
            public static string Track_Item_Scanning(string Language) => Localization_Store.Get("mysobek", "Home", "Track_Item_Scanning", Language);

            /// <summary> Format string with a "{0}" placeholder for the instance abbreviation </summary>
            public static string Return_To_Previous_Page_Format(string Language) => Localization_Store.Get("mysobek", "Home", "Return_To_Previous_Page_Format", Language);

            public static string Log_Out(string Language) => Localization_Store.Get("mysobek", "Home", "Log_Out", Language);
            public static string Contact_Us_Link_Text(string Language) => Localization_Store.Get("mysobek", "Home", "Contact_Us_Link_Text", Language);

            /// <summary> Format string with a "{0}" placeholder for the "contact us" link HTML </summary>
            public static string Comments_Recommendations_Format(string Language) => Localization_Store.Get("mysobek", "Home", "Comments_Recommendations_Format", Language);

            /// <summary> Format string with a "{0}" placeholder for the "contact us" link HTML </summary>
            public static string Contribute_Materials_Format(string Language) => Localization_Store.Get("mysobek", "Home", "Contribute_Materials_Format", Language);
        }

        /// <summary> Phrases for the saved-searches mySobek viewer (Saved_Searches_MySobekViewer) </summary>
        public static class Saved_Searches
        {
            public static string Page_Title(string Language) => Localization_Store.Get("mysobek", "Saved_Searches", "Page_Title", Language);
            public static string Actions_Header(string Language) => Localization_Store.Get("mysobek", "Saved_Searches", "Actions_Header", Language);
            public static string Saved_Search_Header(string Language) => Localization_Store.Get("mysobek", "Saved_Searches", "Saved_Search_Header", Language);
            public static string Delete_Link_Title(string Language) => Localization_Store.Get("mysobek", "Saved_Searches", "Delete_Link_Title", Language);
            public static string Delete_Link_Text(string Language) => Localization_Store.Get("mysobek", "Saved_Searches", "Delete_Link_Text", Language);
            public static string View_Link_Title(string Language) => Localization_Store.Get("mysobek", "Saved_Searches", "View_Link_Title", Language);
            public static string View_Link_Text(string Language) => Localization_Store.Get("mysobek", "Saved_Searches", "View_Link_Text", Language);
            public static string No_Saved_Searches_Html(string Language) => Localization_Store.Get("mysobek", "Saved_Searches", "No_Saved_Searches_Html", Language);
        }

        /// <summary> Phrases for the logon mySobek viewer (Logon_MySobekViewer) </summary>
        public static class Logon
        {
            public static string Default_Disabled_Message(string Language) => Localization_Store.Get("mysobek", "Logon", "Default_Disabled_Message", Language);
            public static string Invalid_Credentials(string Language) => Localization_Store.Get("mysobek", "Logon", "Invalid_Credentials", Language);

            /// <summary> Format string with a "{0}" placeholder for the instance abbreviation (no "my" prefix) </summary>
            public static string Page_Title_Format(string Language) => Localization_Store.Get("mysobek", "Logon", "Page_Title_Format", Language);

            /// <summary> Format string with a "{0}" placeholder for the "my{Instance}" text </summary>
            public static string Heading_Format(string Language) => Localization_Store.Get("mysobek", "Logon", "Heading_Format", Language);

            public static string Feature_Requires_Logon(string Language) => Localization_Store.Get("mysobek", "Logon", "Feature_Requires_Logon", Language);
            public static string Choose_Logon_Below(string Language) => Localization_Store.Get("mysobek", "Logon", "Choose_Logon_Below", Language);
            public static string Dloc_Valid_Logon_Text(string Language) => Localization_Store.Get("mysobek", "Logon", "Dloc_Valid_Logon_Text", Language);
            public static string Dloc_Sign_On_Text(string Language) => Localization_Store.Get("mysobek", "Logon", "Dloc_Sign_On_Text", Language);

            /// <summary> Format string with a "{0}" placeholder for the Shibboleth provider label </summary>
            public static string Shibboleth_Valid_Id_Format(string Language) => Localization_Store.Get("mysobek", "Logon", "Shibboleth_Valid_Id_Format", Language);

            /// <summary> Format string with a "{0}" placeholder for the Shibboleth provider label </summary>
            public static string Shibboleth_Sign_On_Format(string Language) => Localization_Store.Get("mysobek", "Logon", "Shibboleth_Sign_On_Format", Language);

            /// <summary> Format string with a "{0}" placeholder for the instance abbreviation (no "my" prefix) </summary>
            public static string Instance_Valid_Logon_Format(string Language) => Localization_Store.Get("mysobek", "Logon", "Instance_Valid_Logon_Format", Language);

            /// <summary> Format string with a "{0}" placeholder for the instance abbreviation (no "my" prefix) </summary>
            public static string Instance_Sign_On_Format(string Language) => Localization_Store.Get("mysobek", "Logon", "Instance_Sign_On_Format", Language);

            /// <summary> Format string with a "{0}" placeholder for the federated provider's display label </summary>
            public static string Federated_Valid_Account_Format(string Language) => Localization_Store.Get("mysobek", "Logon", "Federated_Valid_Account_Format", Language);

            /// <summary> Format string with a "{0}" placeholder for the federated provider's display label </summary>
            public static string Federated_Sign_In_Format(string Language) => Localization_Store.Get("mysobek", "Logon", "Federated_Sign_In_Format", Language);

            public static string Not_Registered_Yet(string Language) => Localization_Store.Get("mysobek", "Logon", "Not_Registered_Yet", Language);
            public static string Register_Now(string Language) => Localization_Store.Get("mysobek", "Logon", "Register_Now", Language);
            public static string Contact_Us(string Language) => Localization_Store.Get("mysobek", "Logon", "Contact_Us", Language);
            public static string Popup_Title_Log_In(string Language) => Localization_Store.Get("mysobek", "Logon", "Popup_Title_Log_In", Language);
            public static string Username_Or_Email_Label(string Language) => Localization_Store.Get("mysobek", "Logon", "Username_Or_Email_Label", Language);
            public static string Password_Label(string Language) => Localization_Store.Get("mysobek", "Logon", "Password_Label", Language);
            public static string Remember_Me(string Language) => Localization_Store.Get("mysobek", "Logon", "Remember_Me", Language);
            public static string Close_Button_Title(string Language) => Localization_Store.Get("mysobek", "Logon", "Close_Button_Title", Language);
            public static string Cancel_Button_Text(string Language) => Localization_Store.Get("mysobek", "Logon", "Cancel_Button_Text", Language);
            public static string Login_Button_Title(string Language) => Localization_Store.Get("mysobek", "Logon", "Login_Button_Title", Language);
            public static string Login_Button_Text(string Language) => Localization_Store.Get("mysobek", "Logon", "Login_Button_Text", Language);

            /// <summary> Format string with a "{0}" placeholder for the "Register now" link HTML </summary>
            public static string Popup_Not_Registered_Format(string Language) => Localization_Store.Get("mysobek", "Logon", "Popup_Not_Registered_Format", Language);

            /// <summary> Format string with a "{0}" placeholder for the "contact us" link HTML </summary>
            public static string Popup_Forgot_Password_Format(string Language) => Localization_Store.Get("mysobek", "Logon", "Popup_Forgot_Password_Format", Language);

            public static string Popup_Contact_Us_Link_Text(string Language) => Localization_Store.Get("mysobek", "Logon", "Popup_Contact_Us_Link_Text", Language);
        }

        /// <summary> Phrases for the Open-NJ registration/preferences mySobek viewer (OpenNJ_Register_MySobekViewer) </summary>
        public static class OpenNJ_Register
        {
            public static string Account_Info(string Language) => Localization_Store.Get("mysobek", "OpenNJ_Register", "Account_Info", Language);
            public static string Username_Label(string Language) => Localization_Store.Get("mysobek", "OpenNJ_Register", "Username_Label", Language);
            public static string Personal_Info(string Language) => Localization_Store.Get("mysobek", "OpenNJ_Register", "Personal_Info", Language);
            public static string Family_Names_Label(string Language) => Localization_Store.Get("mysobek", "OpenNJ_Register", "Family_Names_Label", Language);
            public static string Given_Names_Label(string Language) => Localization_Store.Get("mysobek", "OpenNJ_Register", "Given_Names_Label", Language);
            public static string Nickname_Label(string Language) => Localization_Store.Get("mysobek", "OpenNJ_Register", "Nickname_Label", Language);
            public static string Email_Label(string Language) => Localization_Store.Get("mysobek", "OpenNJ_Register", "Email_Label", Language);
            public static string Email_Stats_Label(string Language) => Localization_Store.Get("mysobek", "OpenNJ_Register", "Email_Stats_Label", Language);
            public static string Affiliation_Info(string Language) => Localization_Store.Get("mysobek", "OpenNJ_Register", "Affiliation_Info", Language);
            public static string Organization_Label(string Language) => Localization_Store.Get("mysobek", "OpenNJ_Register", "Organization_Label", Language);
            public static string Password_Label(string Language) => Localization_Store.Get("mysobek", "OpenNJ_Register", "Password_Label", Language);
            public static string Confirm_Password_Label(string Language) => Localization_Store.Get("mysobek", "OpenNJ_Register", "Confirm_Password_Label", Language);

            public static string Username_Required(string Language) => Localization_Store.Get("mysobek", "OpenNJ_Register", "Username_Required", Language);
            public static string Username_Min_Length(string Language) => Localization_Store.Get("mysobek", "OpenNJ_Register", "Username_Min_Length", Language);
            public static string Select_Confirm_Password(string Language) => Localization_Store.Get("mysobek", "OpenNJ_Register", "Select_Confirm_Password", Language);
            public static string Passwords_Do_Not_Match(string Language) => Localization_Store.Get("mysobek", "OpenNJ_Register", "Passwords_Do_Not_Match", Language);
            public static string Password_Min_Length(string Language) => Localization_Store.Get("mysobek", "OpenNJ_Register", "Password_Min_Length", Language);
            public static string Select_Instructor_Status(string Language) => Localization_Store.Get("mysobek", "OpenNJ_Register", "Select_Instructor_Status", Language);
            public static string Instructor_Institution_Required(string Language) => Localization_Store.Get("mysobek", "OpenNJ_Register", "Instructor_Institution_Required", Language);
            public static string Ufid_Length(string Language) => Localization_Store.Get("mysobek", "OpenNJ_Register", "Ufid_Length", Language);
            public static string Ufid_Numeric(string Language) => Localization_Store.Get("mysobek", "OpenNJ_Register", "Ufid_Numeric", Language);
            public static string Family_Name_Required(string Language) => Localization_Store.Get("mysobek", "OpenNJ_Register", "Family_Name_Required", Language);
            public static string Given_Name_Required(string Language) => Localization_Store.Get("mysobek", "OpenNJ_Register", "Given_Name_Required", Language);
            public static string Valid_Email_Required(string Language) => Localization_Store.Get("mysobek", "OpenNJ_Register", "Valid_Email_Required", Language);
            public static string Rights_Truncated(string Language) => Localization_Store.Get("mysobek", "OpenNJ_Register", "Rights_Truncated", Language);
            public static string Email_Already_Exists(string Language) => Localization_Store.Get("mysobek", "OpenNJ_Register", "Email_Already_Exists", Language);
            public static string Username_Taken(string Language) => Localization_Store.Get("mysobek", "OpenNJ_Register", "Username_Taken", Language);

            /// <summary> Format string with a "{0}" placeholder for the instance abbreviation (no "my" prefix) </summary>
            public static string Register_Page_Title_Format(string Language) => Localization_Store.Get("mysobek", "OpenNJ_Register", "Register_Page_Title_Format", Language);

            public static string Edit_Preferences_Page_Title(string Language) => Localization_Store.Get("mysobek", "OpenNJ_Register", "Edit_Preferences_Page_Title", Language);

            /// <summary> Format string with a "{0}" placeholder for the "my{Instance}" text </summary>
            public static string Registration_Intro_Format(string Language) => Localization_Store.Get("mysobek", "OpenNJ_Register", "Registration_Intro_Format", Language);

            public static string Account_Required_Note(string Language) => Localization_Store.Get("mysobek", "OpenNJ_Register", "Account_Required_Note", Language);

            /// <summary> Format string with a "{0}" placeholder for the "Log on" link HTML </summary>
            public static string Already_Registered_Format(string Language) => Localization_Store.Get("mysobek", "OpenNJ_Register", "Already_Registered_Format", Language);

            public static string Log_On_Link_Text(string Language) => Localization_Store.Get("mysobek", "OpenNJ_Register", "Log_On_Link_Text", Language);
            public static string Errors_Detected_Header(string Language) => Localization_Store.Get("mysobek", "OpenNJ_Register", "Errors_Detected_Header", Language);
            public static string Account_Type_Header(string Language) => Localization_Store.Get("mysobek", "OpenNJ_Register", "Account_Type_Header", Language);
            public static string Instructor_Question(string Language) => Localization_Store.Get("mysobek", "OpenNJ_Register", "Instructor_Question", Language);
            public static string I_Am_Instructor(string Language) => Localization_Store.Get("mysobek", "OpenNJ_Register", "I_Am_Instructor", Language);
            public static string I_Am_Not_Instructor(string Language) => Localization_Store.Get("mysobek", "OpenNJ_Register", "I_Am_Not_Instructor", Language);
            public static string Submit_Materials_Instructions(string Language) => Localization_Store.Get("mysobek", "OpenNJ_Register", "Submit_Materials_Instructions", Language);
            public static string Allow_Submit_Label(string Language) => Localization_Store.Get("mysobek", "OpenNJ_Register", "Allow_Submit_Label", Language);
            public static string Application_Reviewed_Notice(string Language) => Localization_Store.Get("mysobek", "OpenNJ_Register", "Application_Reviewed_Notice", Language);
            public static string Username_Hint(string Language) => Localization_Store.Get("mysobek", "OpenNJ_Register", "Username_Hint", Language);
            public static string Password_Hint(string Language) => Localization_Store.Get("mysobek", "OpenNJ_Register", "Password_Hint", Language);
            public static string Institutional_Email_Hint(string Language) => Localization_Store.Get("mysobek", "OpenNJ_Register", "Institutional_Email_Hint", Language);
            public static string Cancel_Button(string Language) => Localization_Store.Get("mysobek", "OpenNJ_Register", "Cancel_Button", Language);
            public static string Submit_Button(string Language) => Localization_Store.Get("mysobek", "OpenNJ_Register", "Submit_Button", Language);
        }

        /// <summary> Phrases for the registration/preferences mySobek viewer (Preferences_MySobekViewer) </summary>
        /// <remarks> Scoped to rendered form content - the Creative Commons license picker block and the
        /// outbound registration/welcome emails are left as-is: the CC block mixes long English descriptions
        /// into quoted JavaScript strings (high risk to extract safely) and the emails have no language
        /// plumbed through today, same as OpenNJ_Register_MySobekViewer </remarks>
        public static class Preferences
        {
            public static string Account_Info(string Language) => Localization_Store.Get("mysobek", "Preferences", "Account_Info", Language);
            public static string Username_Label(string Language) => Localization_Store.Get("mysobek", "Preferences", "Username_Label", Language);
            public static string Personal_Info(string Language) => Localization_Store.Get("mysobek", "Preferences", "Personal_Info", Language);
            public static string Family_Names_Label(string Language) => Localization_Store.Get("mysobek", "Preferences", "Family_Names_Label", Language);
            public static string Given_Names_Label(string Language) => Localization_Store.Get("mysobek", "Preferences", "Given_Names_Label", Language);
            public static string Nickname_Label(string Language) => Localization_Store.Get("mysobek", "Preferences", "Nickname_Label", Language);
            public static string Email_Label(string Language) => Localization_Store.Get("mysobek", "Preferences", "Email_Label", Language);

            /// <summary> Format string with a "{0}" placeholder for the account's Authentication_Source
            /// (e.g. "SAML (SobekDigital AD)") - shown in place of the Given/Family Name and Email inputs
            /// for a federated account, which can't edit those fields here </summary>
            public static string Federated_Name_Email_Note_Format(string Language) => Localization_Store.Get("mysobek", "Preferences", "Federated_Name_Email_Note_Format", Language);

            /// <summary> No French/Spanish translation was ever authored for this in the original code either -
            /// falls back to English for every language, same as the original behavior </summary>
            public static string Email_Stats_Label(string Language) => Localization_Store.Get("mysobek", "Preferences", "Email_Stats_Label", Language);

            public static string Affiliation_Info(string Language) => Localization_Store.Get("mysobek", "Preferences", "Affiliation_Info", Language);
            public static string Organization_Label(string Language) => Localization_Store.Get("mysobek", "Preferences", "Organization_Label", Language);
            public static string College_Label(string Language) => Localization_Store.Get("mysobek", "Preferences", "College_Label", Language);
            public static string Department_Label(string Language) => Localization_Store.Get("mysobek", "Preferences", "Department_Label", Language);
            public static string Unit_Label(string Language) => Localization_Store.Get("mysobek", "Preferences", "Unit_Label", Language);
            public static string Self_Submittal_Pref_Label(string Language) => Localization_Store.Get("mysobek", "Preferences", "Self_Submittal_Pref_Label", Language);
            public static string Send_Email_Label(string Language) => Localization_Store.Get("mysobek", "Preferences", "Send_Email_Label", Language);
            public static string Template_Label(string Language) => Localization_Store.Get("mysobek", "Preferences", "Template_Label", Language);
            public static string Project_Label(string Language) => Localization_Store.Get("mysobek", "Preferences", "Project_Label", Language);
            public static string Default_Rights_Label(string Language) => Localization_Store.Get("mysobek", "Preferences", "Default_Rights_Label", Language);
            public static string Rights_Explanation_Label(string Language) => Localization_Store.Get("mysobek", "Preferences", "Rights_Explanation_Label", Language);

            /// <summary> Includes an embedded Creative Commons link, matching the original inline HTML </summary>
            public static string Rights_Instruction_Label(string Language) => Localization_Store.Get("mysobek", "Preferences", "Rights_Instruction_Label", Language);

            public static string Other_Preferences_Label(string Language) => Localization_Store.Get("mysobek", "Preferences", "Other_Preferences_Label", Language);
            public static string Language_Label(string Language) => Localization_Store.Get("mysobek", "Preferences", "Language_Label", Language);
            public static string Password_Label(string Language) => Localization_Store.Get("mysobek", "Preferences", "Password_Label", Language);
            public static string Confirm_Password_Label(string Language) => Localization_Store.Get("mysobek", "Preferences", "Confirm_Password_Label", Language);

            public static string Username_Required(string Language) => Localization_Store.Get("mysobek", "Preferences", "Username_Required", Language);
            public static string Username_Min_Length(string Language) => Localization_Store.Get("mysobek", "Preferences", "Username_Min_Length", Language);
            public static string Select_Confirm_Password(string Language) => Localization_Store.Get("mysobek", "Preferences", "Select_Confirm_Password", Language);
            public static string Passwords_Do_Not_Match(string Language) => Localization_Store.Get("mysobek", "Preferences", "Passwords_Do_Not_Match", Language);
            public static string Password_Min_Length(string Language) => Localization_Store.Get("mysobek", "Preferences", "Password_Min_Length", Language);
            public static string Ufid_Length(string Language) => Localization_Store.Get("mysobek", "Preferences", "Ufid_Length", Language);
            public static string Ufid_Numeric(string Language) => Localization_Store.Get("mysobek", "Preferences", "Ufid_Numeric", Language);
            public static string Family_Name_Required(string Language) => Localization_Store.Get("mysobek", "Preferences", "Family_Name_Required", Language);
            public static string Given_Name_Required(string Language) => Localization_Store.Get("mysobek", "Preferences", "Given_Name_Required", Language);
            public static string Valid_Email_Required(string Language) => Localization_Store.Get("mysobek", "Preferences", "Valid_Email_Required", Language);
            public static string Rights_Truncated(string Language) => Localization_Store.Get("mysobek", "Preferences", "Rights_Truncated", Language);
            public static string Email_Already_Exists(string Language) => Localization_Store.Get("mysobek", "Preferences", "Email_Already_Exists", Language);
            public static string Username_Taken(string Language) => Localization_Store.Get("mysobek", "Preferences", "Username_Taken", Language);

            /// <summary> Format string with a "{0}" placeholder for the instance abbreviation (no "my" prefix) </summary>
            public static string Register_Page_Title_Format(string Language) => Localization_Store.Get("mysobek", "Preferences", "Register_Page_Title_Format", Language);

            public static string Edit_Preferences_Page_Title(string Language) => Localization_Store.Get("mysobek", "Preferences", "Edit_Preferences_Page_Title", Language);

            /// <summary> Format string with a "{0}" placeholder for the "my{Instance}" text </summary>
            public static string Registration_Intro_Format(string Language) => Localization_Store.Get("mysobek", "Preferences", "Registration_Intro_Format", Language);

            public static string Account_Required_Note(string Language) => Localization_Store.Get("mysobek", "Preferences", "Account_Required_Note", Language);

            /// <summary> Format string with a "{0}" placeholder for the "Log on" link HTML </summary>
            public static string Already_Registered_Format(string Language) => Localization_Store.Get("mysobek", "Preferences", "Already_Registered_Format", Language);

            public static string Log_On_Link_Text(string Language) => Localization_Store.Get("mysobek", "Preferences", "Log_On_Link_Text", Language);
            public static string Errors_Detected_Header(string Language) => Localization_Store.Get("mysobek", "Preferences", "Errors_Detected_Header", Language);
            public static string Username_Hint(string Language) => Localization_Store.Get("mysobek", "Preferences", "Username_Hint", Language);
            public static string Password_Hint(string Language) => Localization_Store.Get("mysobek", "Preferences", "Password_Hint", Language);
            public static string Gatorlink_Hint(string Language) => Localization_Store.Get("mysobek", "Preferences", "Gatorlink_Hint", Language);
            public static string Allow_Submit_With_Notice_Label(string Language) => Localization_Store.Get("mysobek", "Preferences", "Allow_Submit_With_Notice_Label", Language);
            public static string Cancel_Button(string Language) => Localization_Store.Get("mysobek", "Preferences", "Cancel_Button", Language);
            public static string Submit_Button(string Language) => Localization_Store.Get("mysobek", "Preferences", "Submit_Button", Language);
        }

        /// <summary> Phrases for the bookshelf/folder-management mySobek viewer (Folder_Mgmt_MySobekViewer) </summary>
        public static class Folder_Mgmt
        {
            public static string Page_Title(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "Page_Title", Language);
            public static string Manage_Library_Tooltip(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "Manage_Library_Tooltip", Language);
            public static string Manage_Library_Link_Text(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "Manage_Library_Link_Text", Language);
            public static string View_Collections_Home_Tooltip(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "View_Collections_Home_Tooltip", Language);
            public static string My_Collections_Home_Text(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "My_Collections_Home_Text", Language);
            public static string View_Saved_Searches_Tooltip(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "View_Saved_Searches_Tooltip", Language);
            public static string My_Saved_Searches_Text(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "My_Saved_Searches_Text", Language);
            public static string Public_Folder_Tooltip(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "Public_Folder_Tooltip", Language);
            public static string Private_Folder_Tooltip(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "Private_Folder_Tooltip", Language);
            public static string Bookshelf_Empty_Message(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "Bookshelf_Empty_Message", Language);
            public static string Manage_Bookshelves_Heading(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "Manage_Bookshelves_Heading", Language);
            public static string Add_New_Bookshelf_Tooltip(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "Add_New_Bookshelf_Tooltip", Language);
            public static string Add_New_Bookshelf_Text(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "Add_New_Bookshelf_Text", Language);
            public static string Refresh_Bookshelf_Tooltip(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "Refresh_Bookshelf_Tooltip", Language);
            public static string Refresh_Bookshelves_Text(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "Refresh_Bookshelves_Text", Language);
            public static string Actions_Header(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "Actions_Header", Language);
            public static string Bookshelf_Name_Header(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "Bookshelf_Name_Header", Language);
            public static string Delete_Bookshelf_Tooltip(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "Delete_Bookshelf_Tooltip", Language);
            public static string Delete_Link_Text(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "Delete_Link_Text", Language);
            public static string Cannot_Delete_Last_Alert(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "Cannot_Delete_Last_Alert", Language);
            public static string Cannot_Delete_Nested_Alert(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "Cannot_Delete_Nested_Alert", Language);
            public static string Make_Private_Tooltip(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "Make_Private_Tooltip", Language);
            public static string Make_Private_Text(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "Make_Private_Text", Language);
            public static string Make_Public_Tooltip(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "Make_Public_Tooltip", Language);
            public static string Make_Public_Text(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "Make_Public_Text", Language);
            public static string Manage_Bookshelf_Tooltip(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "Manage_Bookshelf_Tooltip", Language);
            public static string Manage_Link_Text(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "Manage_Link_Text", Language);
            public static string Email_Popup_Title(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "Email_Popup_Title", Language);
            public static string Email_Popup_Legend(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "Email_Popup_Legend", Language);
            public static string To_Label(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "To_Label", Language);
            public static string Comments_Label(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "Comments_Label", Language);
            public static string Format_Label(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "Format_Label", Language);
            public static string Html_Format_Label(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "Html_Format_Label", Language);
            public static string Plain_Text_Format_Label(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "Plain_Text_Format_Label", Language);
            public static string Cancel_Button(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "Cancel_Button", Language);
            public static string Send_Button(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "Send_Button", Language);
            public static string Save_Button(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "Save_Button", Language);

            /// <summary> Stylized (small-caps effect via nested spans) popup title, matching the original inline markup </summary>
            public static string Move_Popup_Title_Html(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "Move_Popup_Title_Html", Language);

            public static string Move_Popup_Legend(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "Move_Popup_Legend", Language);
            public static string Bookshelf_Label(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "Bookshelf_Label", Language);

            /// <summary> Stylized (small-caps effect via nested spans) popup title, matching the original inline markup </summary>
            public static string Edit_Notes_Popup_Title_Html(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "Edit_Notes_Popup_Title_Html", Language);

            public static string Edit_Notes_Popup_Legend(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "Edit_Notes_Popup_Legend", Language);
            public static string Notes_Label(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "Notes_Label", Language);

            /// <summary> Stylized (small-caps effect via nested spans) popup title, matching the original inline markup </summary>
            public static string New_Bookshelf_Popup_Title_Html(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "New_Bookshelf_Popup_Title_Html", Language);

            public static string New_Bookshelf_Popup_Legend(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "New_Bookshelf_Popup_Legend", Language);
            public static string Name_Label(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "Name_Label", Language);
            public static string Parent_Label(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "Parent_Label", Language);
            public static string No_Parent_Option(string Language) => Localization_Store.Get("mysobek", "Folder_Mgmt", "No_Parent_Option", Language);
        }

        /// <summary> Phrases for the header/footer chrome rendered on every page (HeaderFooter_HtmlHelper) </summary>
        public static class HeaderFooter
        {
            /// <summary> Format string with a "{0}" placeholder for the instance abbreviation or "my{Instance}"
            /// text this suffix is appended to, e.g. string.Format(Home_Suffix_Format(language), "UFDC") </summary>
            public static string Home_Suffix_Format(string Language) => Localization_Store.Get("chrome", "HeaderFooter", "Home_Suffix_Format", Language);

            /// <summary> Link text used for the footer's mySobek link when logged out (distinct from the
            /// header's own "my{Instance} Home" text) </summary>
            public static string Staff_Login(string Language) => Localization_Store.Get("chrome", "HeaderFooter", "Staff_Login", Language);

            public static string Log_Out(string Language) => Localization_Store.Get("chrome", "HeaderFooter", "Log_Out", Language);

            /// <summary> Format string with "{0}" (user's nickname or given name) and "{1}" ("my{Instance}" text)
            /// placeholders, e.g. string.Format(My_Account_Possessive_Format(language), "Jane", "myUFDC") -
            /// kept as a whole-sentence format rather than a literal "'s" since possessive word order varies by
            /// language (e.g. Spanish would read "myUFDC de Jane", not "Jane's myUFDC") </summary>
            public static string My_Account_Possessive_Format(string Language) => Localization_Store.Get("chrome", "HeaderFooter", "My_Account_Possessive_Format", Language);
        }

        /// <summary> Phrases for the three collapsible top-navigation menus built by MainMenus_HtmlHelper:
        /// the aggregation-level menu, the search-results menu, and the logged-in user/admin menu </summary>
        public static class MainMenus
        {
            public static string Aggregation_Menu_Aria(string Language) => Localization_Store.Get("chrome", "MainMenus", "Aggregation_Menu_Aria", Language);
            public static string Search_Results_Menu_Aria(string Language) => Localization_Store.Get("chrome", "MainMenus", "Search_Results_Menu_Aria", Language);
            public static string User_Menu_Aria(string Language) => Localization_Store.Get("chrome", "MainMenus", "User_Menu_Aria", Language);

            public static string Home(string Language) => Localization_Store.Get("chrome", "MainMenus", "Home", Language);

            /// <summary> Format string with a "{0}" placeholder for the aggregation's translated short name,
            /// e.g. string.Format(Collection_Home_Format(language), "Baseball"). Word order (suffix vs. prefix)
            /// varies by language. </summary>
            public static string Collection_Home_Format(string Language) => Localization_Store.Get("chrome", "MainMenus", "Collection_Home_Format", Language);

            /// <summary> Format string with a "{0}" placeholder for the instance abbreviation,
            /// e.g. string.Format(Sobek_Home_Format(language), "UFDC") </summary>
            public static string Sobek_Home_Format(string Language) => Localization_Store.Get("chrome", "MainMenus", "Sobek_Home_Format", Language);

            /// <summary> Format string with a "{0}" placeholder for the instance abbreviation, used for the
            /// logged-in "my{Instance} Home" menu entry; the leading "my" keeps its own lowercase span markup
            /// regardless of language </summary>
            public static string My_Sobek_Home_Format(string Language) => Localization_Store.Get("chrome", "MainMenus", "My_Sobek_Home_Format", Language);

            public static string View_Items(string Language) => Localization_Store.Get("chrome", "MainMenus", "View_Items", Language);
            public static string View_All_Items(string Language) => Localization_Store.Get("chrome", "MainMenus", "View_All_Items", Language);
            public static string View_New_Items(string Language) => Localization_Store.Get("chrome", "MainMenus", "View_New_Items", Language);
            public static string My_Collections(string Language) => Localization_Store.Get("chrome", "MainMenus", "My_Collections", Language);
            public static string Browse_Partners(string Language) => Localization_Store.Get("chrome", "MainMenus", "Browse_Partners", Language);
            public static string Browse_By(string Language) => Localization_Store.Get("chrome", "MainMenus", "Browse_By", Language);
            public static string Map_Browse(string Language) => Localization_Store.Get("chrome", "MainMenus", "Map_Browse", Language);
            public static string List_View(string Language) => Localization_Store.Get("chrome", "MainMenus", "List_View", Language);
            public static string Brief_View(string Language) => Localization_Store.Get("chrome", "MainMenus", "Brief_View", Language);
            public static string Tree_View(string Language) => Localization_Store.Get("chrome", "MainMenus", "Tree_View", Language);

            public static string Other_Searches(string Language) => Localization_Store.Get("chrome", "MainMenus", "Other_Searches", Language);
            public static string Print_Action(string Language) => Localization_Store.Get("chrome", "MainMenus", "Print_Action", Language);
            public static string Send_Action(string Language) => Localization_Store.Get("chrome", "MainMenus", "Send_Action", Language);
            public static string Save_Action(string Language) => Localization_Store.Get("chrome", "MainMenus", "Save_Action", Language);
            public static string Share_Action(string Language) => Localization_Store.Get("chrome", "MainMenus", "Share_Action", Language);
            public static string Add_Action(string Language) => Localization_Store.Get("chrome", "MainMenus", "Add_Action", Language);
            public static string Remove_Action(string Language) => Localization_Store.Get("chrome", "MainMenus", "Remove_Action", Language);
            public static string Revise_Action(string Language) => Localization_Store.Get("chrome", "MainMenus", "Revise_Action", Language);

            public static string My_Library(string Language) => Localization_Store.Get("chrome", "MainMenus", "My_Library", Language);
            public static string My_Account(string Language) => Localization_Store.Get("chrome", "MainMenus", "My_Account", Language);
            public static string Internal(string Language) => Localization_Store.Get("chrome", "MainMenus", "Internal", Language);
            public static string System_Admin(string Language) => Localization_Store.Get("chrome", "MainMenus", "System_Admin", Language);
            public static string Portal_Admin(string Language) => Localization_Store.Get("chrome", "MainMenus", "Portal_Admin", Language);
            public static string User_Admin(string Language) => Localization_Store.Get("chrome", "MainMenus", "User_Admin", Language);
            public static string Advanced_Search(string Language) => Localization_Store.Get("chrome", "MainMenus", "Advanced_Search", Language);
            public static string Collection_List(string Language) => Localization_Store.Get("chrome", "MainMenus", "Collection_List", Language);
            public static string Collection_Hierarchy(string Language) => Localization_Store.Get("chrome", "MainMenus", "Collection_Hierarchy", Language);
            public static string New_Items(string Language) => Localization_Store.Get("chrome", "MainMenus", "New_Items", Language);
            public static string Memory_Management(string Language) => Localization_Store.Get("chrome", "MainMenus", "Memory_Management", Language);
            public static string Wordmarks(string Language) => Localization_Store.Get("chrome", "MainMenus", "Wordmarks", Language);
            public static string Build_Failures(string Language) => Localization_Store.Get("chrome", "MainMenus", "Build_Failures", Language);

            public static string Start_New_Item(string Language) => Localization_Store.Get("chrome", "MainMenus", "Start_New_Item", Language);
            public static string View_Submitted_Items(string Language) => Localization_Store.Get("chrome", "MainMenus", "View_Submitted_Items", Language);
            public static string View_Item_Usage(string Language) => Localization_Store.Get("chrome", "MainMenus", "View_Item_Usage", Language);
            public static string View_Descriptive_Tags(string Language) => Localization_Store.Get("chrome", "MainMenus", "View_Descriptive_Tags", Language);
            public static string View_Bookshelves(string Language) => Localization_Store.Get("chrome", "MainMenus", "View_Bookshelves", Language);
            public static string View_Saved_Searches(string Language) => Localization_Store.Get("chrome", "MainMenus", "View_Saved_Searches", Language);
            public static string Account_Preferences(string Language) => Localization_Store.Get("chrome", "MainMenus", "Account_Preferences", Language);
            public static string Log_Out(string Language) => Localization_Store.Get("chrome", "MainMenus", "Log_Out", Language);

            public static string Common_Tasks(string Language) => Localization_Store.Get("chrome", "MainMenus", "Common_Tasks", Language);
            public static string Add_Collection_Wizard(string Language) => Localization_Store.Get("chrome", "MainMenus", "Add_Collection_Wizard", Language);
            public static string Edit_Current_Web_Skin(string Language) => Localization_Store.Get("chrome", "MainMenus", "Edit_Current_Web_Skin", Language);
            public static string Users_And_Groups(string Language) => Localization_Store.Get("chrome", "MainMenus", "Users_And_Groups", Language);
            public static string Appearance(string Language) => Localization_Store.Get("chrome", "MainMenus", "Appearance", Language);
            public static string Url_Portals(string Language) => Localization_Store.Get("chrome", "MainMenus", "Url_Portals", Language);
            public static string Web_Skins(string Language) => Localization_Store.Get("chrome", "MainMenus", "Web_Skins", Language);
            public static string Collections(string Language) => Localization_Store.Get("chrome", "MainMenus", "Collections", Language);
            public static string Aggregation_Aliases(string Language) => Localization_Store.Get("chrome", "MainMenus", "Aggregation_Aliases", Language);
            public static string Aggregation_Management(string Language) => Localization_Store.Get("chrome", "MainMenus", "Aggregation_Management", Language);
            public static string Thematic_Headings(string Language) => Localization_Store.Get("chrome", "MainMenus", "Thematic_Headings", Language);
            public static string Items(string Language) => Localization_Store.Get("chrome", "MainMenus", "Items", Language);
            public static string Default_Metadata(string Language) => Localization_Store.Get("chrome", "MainMenus", "Default_Metadata", Language);
            public static string Wordmarks_Icons(string Language) => Localization_Store.Get("chrome", "MainMenus", "Wordmarks_Icons", Language);
            public static string Builder_Status(string Language) => Localization_Store.Get("chrome", "MainMenus", "Builder_Status", Language);
            public static string Settings(string Language) => Localization_Store.Get("chrome", "MainMenus", "Settings", Language);
            public static string Ip_Restriction_Ranges(string Language) => Localization_Store.Get("chrome", "MainMenus", "Ip_Restriction_Ranges", Language);
            public static string System_Wide_Settings(string Language) => Localization_Store.Get("chrome", "MainMenus", "System_Wide_Settings", Language);
            public static string Reset_Cache(string Language) => Localization_Store.Get("chrome", "MainMenus", "Reset_Cache", Language);
            public static string Users_And_Permissions(string Language) => Localization_Store.Get("chrome", "MainMenus", "Users_And_Permissions", Language);
            public static string User_Permissions_Reports(string Language) => Localization_Store.Get("chrome", "MainMenus", "User_Permissions_Reports", Language);
            public static string Users_Requests(string Language) => Localization_Store.Get("chrome", "MainMenus", "Users_Requests", Language);
            public static string Web_Content_Pages(string Language) => Localization_Store.Get("chrome", "MainMenus", "Web_Content_Pages", Language);
            public static string Manage_Web_Content_Pages(string Language) => Localization_Store.Get("chrome", "MainMenus", "Manage_Web_Content_Pages", Language);
            public static string Web_Content_Recent_Updates(string Language) => Localization_Store.Get("chrome", "MainMenus", "Web_Content_Recent_Updates", Language);
            public static string Web_Content_Usage_Reports(string Language) => Localization_Store.Get("chrome", "MainMenus", "Web_Content_Usage_Reports", Language);
            public static string Extensions(string Language) => Localization_Store.Get("chrome", "MainMenus", "Extensions", Language);
            public static string Manage_Tei_Plugin(string Language) => Localization_Store.Get("chrome", "MainMenus", "Manage_Tei_Plugin", Language);
        }

        /// <summary> Phrases shared by the Downloads and Downloads_JP2s item viewers </summary>
        public static class Downloads
        {
            public static string Menu_Default_Label(string Language) => Localization_Store.Get("items", "Downloads", "Menu_Default_Label", Language);

            /// <summary> "This item has the following downloads:" — always used for French/Spanish regardless of
            /// whether the item also has page images, matching the original code's switch-default-only distinction </summary>
            public static string Explanation_Has_Downloads(string Language) => Localization_Store.Get("items", "Downloads", "Explanation_Has_Downloads", Language);

            /// <summary> "This item is only available as the following downloads:" — English only, shown instead of
            /// Explanation_Has_Downloads when the item has no page images, for any language without its own
            /// translation (the original code's French/Spanish branches never used this alternate phrasing) </summary>
            public static string Explanation_Only_Downloads(string Language) => Localization_Store.Get("items", "Downloads", "Explanation_Only_Downloads", Language);

            public static string Tiles_Available_Heading(string Language) => Localization_Store.Get("items", "Downloads", "Tiles_Available_Heading", Language);
            public static string Jpeg2000_Available_Heading(string Language) => Localization_Store.Get("items", "Downloads", "Jpeg2000_Available_Heading", Language);
            public static string Save_Link_As_Instructions(string Language) => Localization_Store.Get("items", "Downloads", "Save_Link_As_Instructions", Language);
            public static string Save_Target_As_Instructions(string Language) => Localization_Store.Get("items", "Downloads", "Save_Target_As_Instructions", Language);
        }
    }
}
