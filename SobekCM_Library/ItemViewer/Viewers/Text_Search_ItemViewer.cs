﻿#region Using directives

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI.WebControls;
using SobekCM.Library.Application_State;
using SobekCM.Library.Configuration;
using SobekCM.Library.Navigation;
using SobekCM.Library.Solr;

#endregion

namespace SobekCM.Library.ItemViewer.Viewers
{
    /// <summary> Item viewer displays the search options and any search results for full-text
    /// searching within a single document </summary>
    /// <remarks> This class extends the abstract class <see cref="abstractItemViewer"/> and implements the 
    /// <see cref="iItemViewer" /> interface. </remarks>
    public class Text_Search_ItemViewer : abstractItemViewer
    {
        private Solr_Page_Results results;

        /// <summary> Gets the type of item viewer this object represents </summary>
        /// <value> This property always returns the enumerational value <see cref="ItemViewer_Type_Enum.Search"/>. </value>
        public override ItemViewer_Type_Enum ItemViewer_Type
        {
            get { return ItemViewer_Type_Enum.Search; }
        }

        /// <summary> Width for the main viewer section to adjusted to accomodate this viewer</summary>
        /// <value> This value depends on the current submode being displayed (i.e., MARC, metadata links, etc..) </value>
        public override int Viewer_Width
        {
            get
            {
                return 750;
            }
        }

        /// <summary> Gets the number of pages for this viewer </summary>
        /// <value> This is a single page viewer, so this property always returns the value 1</value>
        public override int PageCount
        {
            get
            {
                return 1;
            }
        }

        /// <summary> Gets the flag that indicates if the page selector should be shown </summary>
        /// <value> This is a single page viewer, so this property always returns NONE</value>
        public override ItemViewer_PageSelector_Type_Enum Page_Selector
        {
            get
            {
                return ItemViewer_PageSelector_Type_Enum.NONE;
            }
        }

        /// <summary> Stream to which to write the HTML for this subwriter  </summary>
        /// <param name="Output"> Response stream for the item viewer to write directly to </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        public override void Write_Main_Viewer_Section(TextWriter Output, Custom_Tracer Tracer)
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("Text_Search_ItemViewer.Write_Main_Viewer_Section", "");
            }

            string search_this_document = "Search this document";

            if (CurrentMode.Language == Web_Language_Enum.French)
            {
                search_this_document = "Rechercher sur ce Document";
            }

            if (CurrentMode.Language == Web_Language_Enum.Spanish)
            {
                search_this_document = "Buscar en este Objeto";
            }

            // Save the original search string
            string originalSearchString = CurrentMode.Text_Search;

            Output.WriteLine("       <!-- TEXT SEARCH ITEM VIEWER OUTPUT -->");

            // Determine the value without any search
            string currentSearch = CurrentMode.Text_Search;
            CurrentMode.Text_Search = String.Empty;
            string redirect_url = CurrentMode.Redirect_URL();
            CurrentMode.Text_Search = currentSearch;
            string button_text = String.Empty;

            // Add the search this document portion
            Output.WriteLine("    <td align=\"center\">");
            Output.WriteLine("      <div style=\"padding:10px 0px 10px 0px;\" >");
            Output.WriteLine("        <label for=\"searchTextBox\">" + search_this_document + ":</label> &nbsp;");
            Output.WriteLine("        <input class=\"SobekSearchBox\" id=\"searchTextBox\" name=\"searchTextBox\" type=\"text\" value=\"" + CurrentMode.Text_Search.Replace(" =", " or ") + "\" onfocus=\"javascript:textbox_enter('searchTextBox', 'SobekSearchBox_focused')\" onblur=\"javascript:textbox_leave('searchTextBox', 'SobekSearchBox')\" onkeydown=\"item_search_keytrap(event, '" + redirect_url + "');\" /> &nbsp; ");
            Output.WriteLine("        <button title=\"" + search_this_document + "\" class=\"go_button2\" onclick=\"item_search_sobekcm('" + redirect_url + "'); return false;\"></button>");
            Output.WriteLine("      </div>");
            if (results != null)
            {
                // Display the explanation string, and possibly paging options if there are more results
                Output.WriteLine("      <hr style=\"width:100%; color:#dddddd; background-color:#dddddd\" />");
                Output.WriteLine("    </td>");
                Output.WriteLine("  </tr>");
                Output.WriteLine("  <tr>");
                Output.WriteLine("    <td align=\"center\">");
                Output.WriteLine("      <div style=\"width: 600px; padding:5px 0px 12px 0px\">");
                Output.WriteLine(Compute_Search_Explanation());
                Output.WriteLine("</div>");

                if (results.TotalResults > 20)
                {
                    int current_page = CurrentMode.SubPage;
                    if (current_page == 0)
                        current_page = 1;

                    // ADD NAVIGATION BUTTONS
                    string first_page = "First Page";
                    string previous_page = "Previous Page";
                    string next_page = "Next Page";
                    string last_page = "Last Page";

                    if (CurrentMode.Language == Web_Language_Enum.Spanish)
                    {
                        first_page = "Primera Página";
                        previous_page = "Página Anterior";
                        next_page = "Página Siguiente";
                        last_page = "Última Página";
                    }

                    if (CurrentMode.Language == Web_Language_Enum.French)
                    {
                        first_page = "Première Page";
                        previous_page = "Page Précédente";
                        next_page = "Page Suivante";
                        last_page = "Dernière Page";
                    }

                    string language_suffix = CurrentMode.Language_Code;
                    if (language_suffix.Length > 0)
                        language_suffix = "_" + language_suffix;

                    // Use a stringbuilder here
                    StringBuilder buttonWriter = new StringBuilder(2000);

                    buttonWriter.AppendLine("            <div class=\"SobekPageNavBar\">");

                    // Should the first and previous buttons be shown?
                    if (current_page > 1)
                    {
                        // Get the URL for the first and previous buttons
                        CurrentMode.SubPage = 1;
                        string firstButtonURL = CurrentMode.Redirect_URL();
                        CurrentMode.SubPage = (ushort)(current_page - 1);
                        string prevButtonURL = CurrentMode.Redirect_URL();

                        buttonWriter.AppendLine("              <span class=\"leftButtons\">");
                        buttonWriter.AppendLine("                <a href=\"" + firstButtonURL + "\"><img src=\"" + CurrentMode.Base_URL + "design/skins/" + CurrentMode.Base_Skin + "/buttons/first_button" + language_suffix + ".gif\" alt=\"" + first_page + "\" border=\"0\"></a>&nbsp;");
                        buttonWriter.AppendLine("                <a href=\"" + prevButtonURL + "\"><img src=\"" + CurrentMode.Base_URL + "design/skins/" + CurrentMode.Base_Skin + "/buttons/previous_button" + language_suffix + ".gif\" alt=\"" + previous_page + "\" border=\"0\"></a>");
                        buttonWriter.AppendLine("              </span>");
                    }



                    // Only continue if there is an item and mode, and there is previous pages to go to
                    int total_pages = (int)Math.Ceiling((((decimal)results.TotalResults) / 20));
                    if (current_page < total_pages)
                    {
                        // Get the URL for the first and previous buttons
                        CurrentMode.SubPage = (ushort)total_pages;
                        string lastButtonURL = CurrentMode.Redirect_URL();
                        CurrentMode.SubPage = (ushort)(current_page + 1);
                        string nextButtonURL = CurrentMode.Redirect_URL();

                        buttonWriter.AppendLine("              <span class=\"rightButtons\">");
                        buttonWriter.AppendLine("                <a href=\"" + nextButtonURL + "\"><img src=\"" + CurrentMode.Base_URL + "design/skins/" + CurrentMode.Base_Skin + "/buttons/next_button" + language_suffix + ".gif\" alt=\"" + next_page + "\" border=\"0\"></a>&nbsp;");
                        buttonWriter.AppendLine("                <a href=\"" + lastButtonURL + "\"><img src=\"" + CurrentMode.Base_URL + "design/skins/" + CurrentMode.Base_Skin + "/buttons/last_button" + language_suffix + ".gif\" alt =\"" + last_page + "\" border=\"0\"></a>");
                        buttonWriter.AppendLine("              </span>");
                    }

                    buttonWriter.AppendLine("            </div>");

                    button_text = buttonWriter.ToString();
                    Output.WriteLine(button_text);
                    CurrentMode.SubPage = (ushort)current_page;
                }
            }
            Output.WriteLine("    </td>");
            Output.WriteLine("  </tr>");

            if ((results != null) && (results.TotalResults > 0))
            {
                // Look that some of these have thumbnails
                char columns = '2';
                bool hasThumbs = false;
                if (results.Results.Any(result => result.Thumbnail.Length > 0))
                {
                    columns = '3';
                    hasThumbs = true;
                }

                Output.WriteLine("  <tr>");
                Output.WriteLine("    <td class=\"SobekCitationDisplay\">");
                Output.WriteLine("      <div style=\"background-color: White;\">");
                Output.WriteLine("        <table align=\"center\" width=\"100%\" cellspacing=\"15px\" height=\"1px\">");

                string thumbnail_root = CurrentItem.Web.Source_URL;
                string url_options = CurrentMode.URL_Options();
                if (url_options.Length > 0)
                {
                    url_options = url_options + "&search=" + HttpUtility.UrlEncode(originalSearchString);
                }
                else
                {
                    url_options = "?search=" + HttpUtility.UrlEncode(originalSearchString);
                }
                int current_displayed_result = ((results.Page_Number - 1) * 20) + 1;
                bool first = true;
                foreach (Solr_Page_Result result in results.Results)
                {
                    // If this is not the first results drawn, add a seperating line
                    if (!first)
                    {
                        Output.WriteLine("          <tr><td bgcolor=\"#cccccc\" colspan=\"" + columns + "\"></td></tr>");
                    }
                    else
                    {
                        first = false;
                    }

                    Output.WriteLine("          <tr valign=\"middle\">");
                    Output.WriteLine("            <td width=\"5px\" valign=\"top\"><b>" + current_displayed_result + "</b></td>");

                    // Only include the thumbnail column if some exist
                    if (hasThumbs)
                    {
                        if (result.Thumbnail.Length > 0)
                        {
                            Output.WriteLine("            <td align=\"left\" width=\"150px\"><a href=\"" + CurrentMode.Base_URL + CurrentItem.BibID + "/" + CurrentItem.VID + "/" + result.PageOrder + url_options + "\"><img src=\"" + thumbnail_root + "/" + result.Thumbnail + "\" border=\"1\" /></a></td>");
                        }
                        else
                        {
                            Output.WriteLine("            <td align=\"left\" width=\"150px\"><a href=\"" + CurrentMode.Base_URL + CurrentItem.BibID + "/" + CurrentItem.VID + "/" + result.PageOrder + url_options + "\"><img src=\"" + CurrentMode.Default_Images_URL + "NoThumb.jpg\" border=\"1\" /></a></td>");
                        }
                    }

                    Output.WriteLine("            <td align=\"left\">");
                    Output.WriteLine("              <a href=\"" + CurrentMode.Base_URL + CurrentItem.BibID + "/" + CurrentItem.VID + "/" + result.PageOrder + url_options + "\"><strong>" + result.PageName + "</strong></a>");
                    if (result.Snippet.Length > 0)
                    {
                        Output.WriteLine("              <br /><br />");
                        Output.WriteLine("              &ldquo;..." + result.Snippet.Replace("<em>", "<span style=\"background-color:Yellow; font-weight:bold\">").Replace("</em>", "</span>") + "...&rdquo;");
                    }
                    Output.WriteLine("            </td>");
                    Output.WriteLine("          </tr>");

                    current_displayed_result++;
                }

                Output.WriteLine("        </table>");
                Output.WriteLine("      </div>");

                Output.WriteLine(button_text);
                Output.WriteLine("    </td>");
            }
            else
            {
                Output.WriteLine("  <tr>");
                Output.WriteLine("    <td class=\"SobekCitationDisplay\">");
                Output.WriteLine("      <div style=\"background-color: White;\">");
                Output.WriteLine("        <br />");
                Output.WriteLine("        <h2>Quick Tips</h2>");
                Output.WriteLine("        <div id=\"SobekQuickTips\">");
                Output.WriteLine("          <ul>");
                Output.WriteLine("            <li><strong>Document Searching</strong>");
                Output.WriteLine("              <p class=\"tagline\"> This option searches the full-text of the document and returns any pages which match<br />");
                Output.WriteLine("              the conditions of your search.</p>");
                Output.WriteLine("            </li>");
                Output.WriteLine("            <li><strong>Boolean Searches</strong>");
                Output.WriteLine("              <p class=\"tagline\"> Use <b>+</b> or <i><b>and</b></i> between terms to find records with <b>all</b> the terms.<br />");
                Output.WriteLine("              Use <b>-</b> or <i><b>or</b></i> between terms to find records with <b>any</b> of the terms.<br />");
                Output.WriteLine("              Use <b>!</b> or <i><b>and not</b></i> between terms to exclude records with terms.<br />");
                Output.WriteLine("              If nothing is indicated, <b><i>and</i></b> is the default.<br />");
                Output.WriteLine("              EXAMPLE: natural and not history");
                Output.WriteLine("              </p>");
                Output.WriteLine("            </li>");
                Output.WriteLine("            <li><strong>Phrase Searching</strong>");
                Output.WriteLine("              <p class=\"tagline\"> Placing quotes around a phrase will search for the exact phrase.<br />");
                Output.WriteLine("              EXAMPLE: &quot;natural history&quot;</p>");
                Output.WriteLine("            </li>");
                Output.WriteLine("            <li><strong>Capitalization</strong>");
                Output.WriteLine("              <p class=\"tagline\"> Searches are not capitalization sensitive.<br />");
                Output.WriteLine("              EXAMPLE: Searching for <i>NATURAL</i> will return the same results as searching for <i>natural</i></p>");
                Output.WriteLine("            </li>");
                Output.WriteLine("            <li><strong>Diacritics</strong>");
                Output.WriteLine("              <p class=\"tagline\"> To search for words with diacritics, the character must be entered into the search box.<br />");
                Output.WriteLine("              EXAMPLE: Searching <i>Précédent</i> is a different search than <i>Precedent</i></p>");
                Output.WriteLine("            </li>");
                Output.WriteLine("          </ul>");
                Output.WriteLine("        </div>");
                Output.WriteLine("        <br />");
                Output.WriteLine("      </div>");
                Output.WriteLine("    </td>");
            }
        }

        /// <summary> This provides an opportunity for the viewer to perform any pre-display work
        /// which is necessary before entering any of the rendering portions </summary>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <remarks> This method his class pulls any full-text search results for this single item from the Solr/Lucene engine </remarks>
        public override void Perform_PreDisplay_Work(Custom_Tracer Tracer)
        {
            if (CurrentMode.Text_Search.Length > 0)
            {
                List<string> terms = new List<string>();
                List<string> web_fields = new List<string>();

                // Split the terms correctly
                SobekCM_Assistant.Split_Clean_Search_Terms_Fields(CurrentMode.Text_Search, "ZZ", Search_Type_Enum.Basic, terms, web_fields, null, Search_Precision_Type_Enum.Contains, '|');

                Tracer.Add_Trace("Text_Search_Item_Viewer.Perform_PreDisplay_Work", "Performing Solr/Lucene search");

                results = Solr_Page_Results.Search(CurrentItem.BibID, CurrentItem.VID, terms, 20, CurrentMode.SubPage, false);

                Tracer.Add_Trace("Text_Search_Item_Viewer.Perform_PreDisplay_Work", "Completed Solr/Lucene search in " + results.QueryTime + "ms");
            }


        }

        /// <summary> Returns the textual explanation of the item-level search </summary>
        protected string Compute_Search_Explanation()
        {
            StringBuilder output = new StringBuilder();

            // Split the parts
            List<string> terms = new List<string>();
            List<string> fields = new List<string>();

            // If this is basic, do some other preparation
            string complete_search = CurrentMode.Text_Search;
            ushort subpage = CurrentMode.SubPage;
            CurrentMode.SubPage =1;
            Solr_Documents_Searcher.Split_Multi_Terms(CurrentMode.Text_Search, "ZZ", terms, fields);

            string your_search_language = "Your search within this document for ";
            string and_not_language = " AND NOT ";
            string and_language = " AND ";
            string or_language = " OR ";
            string not_language = "not ";
            string resulted_in_language = " resulted in ";
            string matching_pages_language = " matching pages";
            string no_matches_language = "no matching pages";
            string expand_language = "You can expand your results by searching for";
            string restrict_language = "You can restrict your results by searching for";


            if (CurrentMode.Language == Web_Language_Enum.French)
            {
                your_search_language = "Votre recherche dans les textes intégrals pour les pages contenant ";
                and_not_language = " ET PAS ";
                and_language = " ET ";
                or_language = " OU ";
                not_language = "pas ";
                resulted_in_language = " corresponde a ";
                matching_pages_language = " pages de résultats";
                no_matches_language = "pas pages de résultats";
                expand_language = "Vous pouvez elaborer votre rechereche en cherchant par";
                restrict_language = "Vous pouvez limiter votre rechereche en cherchant par";
            }

            if (CurrentMode.Language == Web_Language_Enum.Spanish)
            {
                your_search_language = "Su búsqueda dentro de el texto completo por paginas conteniendo ";
                and_not_language = " Y NO ";
                and_language = " Y ";
                or_language = " O ";
                not_language = "no ";
                resulted_in_language = " resulto en ";
                matching_pages_language = " paginas correspondientes";
                no_matches_language = "no paginas correspondientes";
                expand_language = "Usted puede ampliar sus resultados buscando por";
                restrict_language = "Usted puede disminuir sus resultados buscando por";
            }

            output.Append(your_search_language);
            bool first = true;
            bool allOr = true;
            bool allAnd = true;
            StringBuilder allAndBldr = new StringBuilder(1000);
            StringBuilder allOrBldr = new StringBuilder(1000);
            StringBuilder allAndURL = new StringBuilder(1000);
            StringBuilder allOrURL = new StringBuilder(1000);
            for (int i = 0; i < terms.Count; i++)
            {
                string thisTerm = terms[i];
                if (!first)
                {
                    switch (fields[i][0])
                    {
                        case '-':
                            allOr = false;
                            allAnd = false;
                            output.Append(and_not_language);
                            allAndBldr.Append(and_not_language);
                            allAndURL.Append("+-");
                            break;

                        case '=':
                            output.Append(or_language);
                            allAndBldr.Append(and_language);
                            allOrBldr.Append(or_language);
                            allAnd = false;
                            allAndURL.Append("+");
                            allOrURL.Append("+=");
                            break;

                        default:
                            output.Append(and_language);
                            allAndBldr.Append(and_language);
                            allOrBldr.Append(or_language);
                            allOr = false;
                            allAndURL.Append("+");
                            allOrURL.Append("+=");
                            break;
                    }
                }
                else
                {
                    first = false;
                    if (fields[i][0] == '-')
                    {
                        output.Append(not_language);
                        allAndURL.Append("-");
                    }
                }

                // Write the term
                if (thisTerm[0] == '"')
                {
                    CurrentMode.Text_Search = thisTerm;
                    output.Append("<a href=\"" + CurrentMode.Redirect_URL() + "\">" + thisTerm.Replace("+", " ") + "</a>");

                    if (fields[i][0] == '-')
                    {
                        allAndBldr.Append(thisTerm.Replace("+", " "));
                        allAndURL.Append(thisTerm.Replace("\"", "%22"));
                    }
                    else
                    {
                        allAndBldr.Append(thisTerm.Replace("+", " "));
                        allAndURL.Append(thisTerm.Replace("\"", "%22"));
                        allOrBldr.Append(thisTerm.Replace("+", " "));
                        allOrURL.Append(thisTerm.Replace("\"", "%22"));
                    }
                }
                else
                {
                    CurrentMode.Text_Search = thisTerm;
                    output.Append("<a href=\"" + CurrentMode.Redirect_URL() + "\">'" + thisTerm + "'</a>");

                    if (fields[i][0] == '-')
                    {
                        allAndBldr.Append(thisTerm.Replace("+", " "));
                        allAndURL.Append(thisTerm.Replace("\"", "%22"));
                    }
                    else
                    {
                        allAndBldr.Append(thisTerm.Replace("+", " "));
                        allAndURL.Append(thisTerm.Replace("\"", "%22"));
                        allOrBldr.Append(thisTerm.Replace("+", " "));
                        allOrURL.Append(thisTerm.Replace("\"", "%22"));
                    }
                }
            }
            output.Append(resulted_in_language);

            if (results.TotalResults > 0)
            {
                output.AppendLine(number_to_string(results.TotalResults) + matching_pages_language + ".");
            }
            else
            {
                output.AppendLine("<b>" + no_matches_language + "</b>.");
            }

            if (!allOr)
            {
                CurrentMode.Text_Search = allOrURL.ToString();
                output.AppendLine("<br /><br />" + expand_language + " <a href=\"" + CurrentMode.Redirect_URL() + "\">" + allOrBldr + "</a>.");
            }

            if ((!allAnd) && (results.TotalResults > 0))
            {
                CurrentMode.Text_Search = allAndURL.ToString();
                output.AppendLine("<br /><br />" + restrict_language + " <a href=\"" + CurrentMode.Redirect_URL() + "\">" + allAndBldr + "</a>.");
            }

            // Restore the original values
            CurrentMode.Text_Search = complete_search;
            CurrentMode.SubPage = subpage;

            return output.ToString();
        }


        private static string number_to_string(int Number)
        {
            switch (Number)
            {
                case 1: return "one";
                case 2: return "two";
                case 3: return "three";
                case 4: return "four";
                case 5: return "five";
                case 6: return "six";
                case 7: return "seven";
                case 8: return "eight";
                case 9: return "nine";
                case 10: return "ten";
                case 11: return "eleven";
                case 12: return "twelve";
                default: return Number.ToString();
            }
        }
    }
}