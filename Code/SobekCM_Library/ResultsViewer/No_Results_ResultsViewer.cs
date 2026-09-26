#region Using directives

using SobekCM.Core.Navigation;
using SobekCM.Library.Localization;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;

#endregion

namespace SobekCM.Library.ResultsViewer
{
    /// <summary> Results viewer displays the message (and links) in the case the result set is empty.  </summary>
    /// <remarks> This class extends the abstract class <see cref="abstract_ResultsViewer"/> and implements the 
    /// <see cref="iResultsViewer" /> interface. </remarks>
    public class No_Results_ResultsViewer : abstract_ResultsViewer
    {
        /// <summary> Constructor for a new instance of the No_Results_ResultsViewer class </summary>
        public No_Results_ResultsViewer() : base()
        {
            // Do nothing
        }

        /// <summary> Adds the controls for this result viewer to the place holder on the main form </summary>
        /// <param name="MainPlaceHolder"> Main place holder ( &quot;mainPlaceHolder&quot; ) in the itemNavForm form into which the the bulk of the result viewer's output is displayed</param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> Sorted tree with the results in hierarchical structure with volumes and issues under the titles and sorted by serial hierarchy </returns>
        public override void Add_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer?.Add_Trace("No_Results_ResultsWriter.Add_HTML", "Adding no result text");

            // Get the no results text, in the visitor's language unless the site supplies its own
            string noResultsText = Get_NoResults_Text(RequestSpecificValues.Current_Mode.Language);

            // Get the list of search terms
            string terms = RequestSpecificValues.Current_Mode.Search_String.Replace(",", " ").Trim();

            // Try to search out into the Union catalog
            int union_catalog_matches = 0;
            string susMangoSearchQuery = String.Empty;
            if ((noResultsText.Contains("[%SusMangoSpanDisplay%]")) && (UI_ApplicationCache_Gateway.Settings.Florida != null) && (!String.IsNullOrEmpty(UI_ApplicationCache_Gateway.Settings.Florida.Mango_Union_Search_Base_URL)))
            {
                try
                {
                    // the html retrieved from the page
                    String strResult;
                    using (var httpClient = new HttpClient { Timeout = TimeSpan.FromMilliseconds(2000) })
                    using (Stream responseStream = httpClient.GetStreamAsync(UI_ApplicationCache_Gateway.Settings.Florida.Mango_Union_Search_Base_URL + "&term=" + terms).GetAwaiter().GetResult())
                    using (var sr = new StreamReader(responseStream))
                    {
                        strResult = sr.ReadToEnd().Trim();
                    }
                    if (strResult.Length > 0)
                    {
                        bool isNumber = strResult.All(Char.IsNumber);
                        if (isNumber)
                        {
                            union_catalog_matches = Convert.ToInt32(strResult);
                        }
                    }
                }
                catch (Exception)
                {
                    Tracer?.Add_Trace("No_Results_ResultsWriter.Add_HTML", "Exception caught while querying Mango state union catalog", Custom_Trace_Type_Enum.Error);
                }
            }

            // Show or hide the links
            if ((union_catalog_matches > 0) || ((RequestSpecificValues.Current_Mode.Aggregation.Length > 0) && (RequestSpecificValues.Current_Mode.Aggregation.ToUpper() != "ALL") && (ResultsStats.All_Collections_Items > 0) && (RequestSpecificValues.Current_Mode.Default_Aggregation == "all")))
            {
                noResultsText = noResultsText.Replace("[%MatchesFoundDivDisplay%]", "block");

                if ((RequestSpecificValues.Current_Mode.Aggregation.Length > 0) && (RequestSpecificValues.Current_Mode.Aggregation.ToUpper() != "ALL") && (ResultsStats.All_Collections_Items.HasValue) && (ResultsStats.All_Collections_Items.Value > 0) && (RequestSpecificValues.Current_Mode.Default_Aggregation == "all"))
                {
                    string aggregation = RequestSpecificValues.Current_Mode.Aggregation;
                    RequestSpecificValues.Current_Mode.Aggregation = String.Empty;
                    string instance_search_url = UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode);
                    RequestSpecificValues.Current_Mode.Aggregation = aggregation;


                    noResultsText = noResultsText.Replace("[%WithinInstanceSpanDisplay%]", "inline-block").Replace("[%WithinInstanceUrl%]", instance_search_url).Replace("[%WithinInstanceCount%]", number_to_string(ResultsStats.All_Collections_Items));
                }
                else
                {
                    noResultsText = noResultsText.Replace("[%WithinInstanceSpanDisplay%]", "none");
                }

                if (union_catalog_matches > 0)
                {
                    susMangoSearchQuery = "?st=" + System.Net.WebUtility.HtmlEncode(terms) + "&ix=kw";
                    noResultsText = noResultsText.Replace("[%SusMangoSpanDisplay%]", "inline-block").Replace("[%SusMangoSearchEnding%]", susMangoSearchQuery).Replace("[%SusMangoCount%]", number_to_string(union_catalog_matches));
                }
                else
                {
                    noResultsText = noResultsText.Replace("[%SusMangoSpanDisplay%]", "none").Replace("[%SusMangoSearchEnding%]", String.Empty);
                }
            }
            else
            {
                noResultsText = noResultsText.Replace("[%MatchesFoundDivDisplay%]", "none").Replace("[%SusMangoSearchEnding%]", String.Empty);
            }

            // Resolve every token that's still left, whichever branch ran above. When there were no other matches,
            // only the outer [%MatchesFoundDivDisplay%] used to be filled in, so the links hidden inside it still
            // carried raw [%WithinInstanceUrl%]-style tokens in their text and hrefs.
            noResultsText = noResultsText.Replace("[%WithinInstanceSpanDisplay%]", "none").Replace("[%WithinInstanceUrl%]", String.Empty).Replace("[%WithinInstanceCount%]", String.Empty)
                .Replace("[%SusMangoSpanDisplay%]", "none").Replace("[%SusMangoSearchEnding%]", String.Empty).Replace("[%SusMangoCount%]", String.Empty)
                .Replace("[%MatchesFoundDivDisplay%]", "none");

            // Show the final data
            var noResultsTextBuilder = new StringBuilder(noResultsText.Replace("[%BaseName%]", RequestSpecificValues.Current_Mode.Portal_Name));

            noResultsTextBuilder.AppendLine("</td></tr></table>");

            noResultsTextBuilder.AppendLine();
            noResultsTextBuilder.AppendLine("<!-- place holder for the load() event in the body -->");
            noResultsTextBuilder.AppendLine("<script type=\"text/javascript\"> ");
            noResultsTextBuilder.AppendLine("  //<![CDATA[");
            noResultsTextBuilder.AppendLine("    function load() { } ");
            noResultsTextBuilder.AppendLine("  //]]>");
            noResultsTextBuilder.AppendLine("</script>");
            noResultsTextBuilder.AppendLine();

            Output.Write(noResultsTextBuilder.ToString());
        }

        /// <summary> Gets the no results text to display, from the HTML static page or uses the default </summary>
        /// <returns> HTML text </returns>
        /// <remarks> This is public (I think) so it can be pulled directly from here for the configuration display. 
        /// This should probably move into a configuration file or engine endpoint though.  </remarks>
        public static string Get_NoResults_Text()
        {
            return Get_NoResults_Text("en");
        }

        /// <summary> Gets the no results text to display: the site's own design/webcontent/noresults.html if there is
        /// one, otherwise the built-in text in the given language </summary>
        /// <param name="Language"> Language code for the built-in text </param>
        /// <returns> HTML text, still containing its [%...%] tokens </returns>
        /// <remarks> Only the site's own file is cached. The built-in text used to be cached too, the first time
        /// anyone asked for it, so every visitor after that got it in English. </remarks>
        public static string Get_NoResults_Text(string Language)
        {
            string noResultsText = SobekCM_Application.State["NORESULTS"] as string;
            if (String.IsNullOrEmpty(noResultsText))
            {
                try
                {
                    string file = Path.Combine(UI_ApplicationCache_Gateway.Settings.Servers.Base_Design_Location, "webcontent", "noresults.html");
                    if (File.Exists(file))
                    {
                        noResultsText = File.ReadAllText(file);
                        SobekCM_Application.State["NORESULTS"] = noResultsText;
                    }
                    else
                    {
                        noResultsText = "NOTPRESENT";
                        SobekCM_Application.State["NORESULTS"] = "NOTPRESENT";
                    }
                }
                catch
                {
                    noResultsText = "NOTPRESENT";
                    SobekCM_Application.State["NORESULTS"] = "NOTPRESENT";
                }
            }

            // The site has its own no results page
            if ((!String.IsNullOrEmpty(noResultsText)) && (noResultsText != "NOTPRESENT"))
                return noResultsText;

            // Otherwise build the built-in one, in this language
            var sampleFileContent = new StringBuilder();

            sampleFileContent.AppendLine("<span class=\"SobekNoResultsText\"><br />" + Localization_Gateway.PagedResults.No_Results_Message(Language) + "<br /><br /></span>");
            sampleFileContent.AppendLine("<div style=\"display:[%MatchesFoundDivDisplay%]\">");
            sampleFileContent.AppendLine("    " + Localization_Gateway.PagedResults.No_Results_Matches_Found(Language) + "<br /><br />");
            sampleFileContent.AppendLine("      <span style=\"display:[%WithinInstanceSpanDisplay%]\"><a href=\"[%WithinInstanceUrl%]\">" + String.Format(Localization_Gateway.PagedResults.No_Results_Found_In_Format(Language), "[%WithinInstanceCount%]", "[%BaseName%]") + "</a><br /><br /></span>");
            sampleFileContent.AppendLine("      <span style=\"display:[%SusMangoSpanDisplay%]\"><a href=\"http://uf.catalog.fcla.edu/uf.jsp[%SusMangoSearchEnding%]\" target=\"_BLANK\">" + String.Format(Localization_Gateway.PagedResults.No_Results_Found_In_UF_Catalog_Format(Language), "[%SusMangoCount%]") + "</a><br /><br /></span>");
            sampleFileContent.AppendLine("</div>");

            sampleFileContent.AppendLine(Localization_Gateway.PagedResults.No_Results_Consider_Searching(Language) + "<br /><br />");

            sampleFileContent.AppendLine(Localization_Gateway.PagedResults.No_Results_Online_Resource(Language) + " <a href=\"http://scholar.google.com\" target=\"_BLANK\">Google Scholar</a> " + Localization_Gateway.PagedResults.No_Results_Or(Language) + " <a href=\"http://books.google.com\" target=\"_BLANK\">Google Books</a><br />");
            sampleFileContent.AppendLine(Localization_Gateway.PagedResults.No_Results_Physical_Holdings(Language) + " <a href=\"http://www.worldcat.org\" target=\"_BLANK\">Worldcat</a><br />");
            sampleFileContent.AppendLine("  <br /><br /><br /><br />");

            return sampleFileContent.ToString();
        }

        /// <summary> Writes a count as digits, which read correctly in every language (this used to spell out
        /// one to twelve in English) </summary>
        protected static string number_to_string(int Number)
        {
            return Number.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary> Writes a count as digits, or 0 when there is none </summary>
        protected static string number_to_string(int? Number)
        {
            return number_to_string(Number ?? 0);
        }
    }
}
