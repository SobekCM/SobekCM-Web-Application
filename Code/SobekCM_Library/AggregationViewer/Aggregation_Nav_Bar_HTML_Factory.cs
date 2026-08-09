#region Using directives

using SobekCM.Core.Aggregations;
using SobekCM.Core.ApplicationState;
using SobekCM.Core.Navigation;
using SobekCM.Library.HTML;
using SobekCM.Library.Localization;
using System;

using SobekCM.Library.UI;
#endregion

namespace SobekCM.Library.AggregationViewer
{
    /// <summary> HTML factory class that helps to render the HTML for the navigational tabs which correspond to different collection views into the same item aggregation </summary>
    /// <remarks> This is used by <see cref="Aggregation_HtmlSubwriter"/> class </remarks>
    public class Aggregation_Nav_Bar_HTML_Factory
    {
        /// <summary> Compares the collection view type and the search type from the current http request </summary>
        /// <param name="SearchType1"> Search type from the list of collection views and searches </param>
        /// <param name="SearchType2"> Search type from the current http request </param>
        /// <returns> TRUE if they are analagous, otherwise FALSE </returns>
        public static bool Do_Search_Types_Match(Item_Aggregation_Views_Searches_Enum SearchType1, Search_Type_Enum SearchType2)
        {
            switch (SearchType1)
            {
                case Item_Aggregation_Views_Searches_Enum.Advanced_Search:
                    return SearchType2 == Search_Type_Enum.Advanced;

                case Item_Aggregation_Views_Searches_Enum.Advanced_Search_YearRange:
                    return SearchType2 == Search_Type_Enum.Advanced;

                case Item_Aggregation_Views_Searches_Enum.Advanced_Search_MimeType:
                    return SearchType2 == Search_Type_Enum.Advanced;

                case Item_Aggregation_Views_Searches_Enum.Basic_Search:
                    return SearchType2 == Search_Type_Enum.Basic;

                case Item_Aggregation_Views_Searches_Enum.Banner_Search:
                    return SearchType2 == Search_Type_Enum.Basic;

                case Item_Aggregation_Views_Searches_Enum.Basic_Search_YearRange:
                    return SearchType2 == Search_Type_Enum.Basic;

                case Item_Aggregation_Views_Searches_Enum.Basic_Search_MimeType:
                    return SearchType2 == Search_Type_Enum.Basic;

                case Item_Aggregation_Views_Searches_Enum.Basic_Search_FullTextOption:
                    return SearchType2 == Search_Type_Enum.Basic;

                case Item_Aggregation_Views_Searches_Enum.FullText_Search:
                    return SearchType2 == Search_Type_Enum.Full_Text;

                case Item_Aggregation_Views_Searches_Enum.Map_Search:
                    return SearchType2 == Search_Type_Enum.Map;

                case Item_Aggregation_Views_Searches_Enum.Map_Search_Beta:
                    return SearchType2 == Search_Type_Enum.Map_Beta;

                case Item_Aggregation_Views_Searches_Enum.Newspaper_Search:
                    return SearchType2 == Search_Type_Enum.Newspaper;

                case Item_Aggregation_Views_Searches_Enum.DLOC_FullText_Search:
                    return SearchType2 == Search_Type_Enum.dLOC_Full_Text;

                default:
                    return false;
            }
        }

        #region Methods for adding the aggregation view to the main menu

        /// <summary> Returns the HTML for one element within tab which appears over the search box in the collection view </summary>
        /// <param name="ThisView"> Collection view type for this tab </param>
        /// <param name="Current_Mode"> Mode / navigation information for the current request, to see if the tab is currently selected or not and determine current skin language </param>
        /// <param name="Translations"> Language support object for writing the name of the view in the appropriate interface language </param>
        /// <returns> HTML to display the tab, including the link if it is not currently selected </returns>
        public static string Menu_Get_Nav_Bar_HTML(Item_Aggregation_Views_Searches_Enum ThisView, Navigation_Object Current_Mode, Language_Support_Info Translations)
        {
            string skinCode = Current_Mode.Base_Skin_Or_Skin;

            switch (ThisView)
            {
                case Item_Aggregation_Views_Searches_Enum.Advanced_Search:
                case Item_Aggregation_Views_Searches_Enum.Advanced_Search_YearRange:
                case Item_Aggregation_Views_Searches_Enum.Advanced_Search_MimeType:
                    return Menu_HTML_Helper(skinCode, Search_Type_Enum.Advanced, Localization_Gateway.Aggregation_Nav_Bar.Advanced_Search(Current_Mode.Language), Current_Mode);

                case Item_Aggregation_Views_Searches_Enum.Basic_Search:
                case Item_Aggregation_Views_Searches_Enum.Basic_Search_YearRange:
                case Item_Aggregation_Views_Searches_Enum.Basic_Search_MimeType:
                    return Menu_HTML_Helper(skinCode, Search_Type_Enum.Basic, Localization_Gateway.Aggregation_Nav_Bar.Basic_Search(Current_Mode.Language), Current_Mode);

                case Item_Aggregation_Views_Searches_Enum.Map_Search:
                    return Menu_HTML_Helper(skinCode, Search_Type_Enum.Map, Localization_Gateway.Aggregation_Nav_Bar.Map_Search(Current_Mode.Language), Current_Mode);

                case Item_Aggregation_Views_Searches_Enum.Map_Search_Beta:
                    return Menu_HTML_Helper(skinCode, Search_Type_Enum.Map_Beta, Localization_Gateway.Aggregation_Nav_Bar.Map_Search(Current_Mode.Language), Current_Mode);

                case Item_Aggregation_Views_Searches_Enum.Newspaper_Search:
                    return Menu_HTML_Helper(skinCode, Search_Type_Enum.Newspaper, Localization_Gateway.Aggregation_Nav_Bar.Newspaper_Search(Current_Mode.Language), Current_Mode);

                case Item_Aggregation_Views_Searches_Enum.Admin_View:
                    return String.Empty; // HTML_Helper(Skin_Code, SobekCM.Library.Navigation.Search_Type_Enum.Admin_View, Translations.Get_Translation("ADMIN", Current_Mode.Language), Current_Mode, Downward_Tabs);

                case Item_Aggregation_Views_Searches_Enum.DLOC_FullText_Search:
                    return Menu_HTML_Helper(skinCode, Search_Type_Enum.dLOC_Full_Text, Localization_Gateway.Aggregation_Nav_Bar.Text_Search(Current_Mode.Language), Current_Mode);

                case Item_Aggregation_Views_Searches_Enum.FullText_Search:
                    return Menu_HTML_Helper(skinCode, Search_Type_Enum.Full_Text, Localization_Gateway.Aggregation_Nav_Bar.Text_Search(Current_Mode.Language), Current_Mode);
            }

            return String.Empty;
        }

        private static string Menu_HTML_Helper(string SkinCode, Search_Type_Enum Search_Type, string Display_Text, Navigation_Object Current_Mode)
        {
            if (Current_Mode.Is_Robot)
            {
                if ((Current_Mode.Mode == Display_Mode_Enum.Search) && (Current_Mode.Search_Type == Search_Type))
                {
                    return "<li class=\"selected-sf-menu-item-link\"><a href=\"\">" + Display_Text + "</a></li>" + Environment.NewLine;
                }
                else
                {
                    return "<li><a href=\"\">" + Display_Text + "</a></li>" + Environment.NewLine;

                }
            }

            if ((Current_Mode.Mode == Display_Mode_Enum.Search) && (Current_Mode.Search_Type == Search_Type))
            {
                return "<li class=\"selected-sf-menu-item-link\"><a href=\"" + UrlWriterHelper.Redirect_URL(Current_Mode) + "\">" + Display_Text + "</a></li>" + Environment.NewLine;
            }

            // else...
            Search_Type_Enum currentSearchType2 = Current_Mode.Search_Type;
            Display_Mode_Enum currentMode2 = Current_Mode.Mode;
            Current_Mode.Search_Type = Search_Type;
            Current_Mode.Mode = Display_Mode_Enum.Search;
            string toReturn2 = "<li><a href=\"" + UrlWriterHelper.Redirect_URL(Current_Mode) + "\">" + Display_Text + "</a></li>" + Environment.NewLine;
            Current_Mode.Search_Type = currentSearchType2;
            Current_Mode.Mode = currentMode2;
            return toReturn2;
        }

        #endregion
    }
}
