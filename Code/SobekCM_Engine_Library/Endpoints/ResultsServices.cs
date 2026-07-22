#region Using directives

using SobekCM.Core.Aggregations;
using SobekCM.Core.Configuration.Engine;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.Navigation;
using SobekCM.Core.Results;
using SobekCM.Core.ResultTitle;
using SobekCM.Core.Search;
using SobekCM.Engine_Library.Aggregations;
using SobekCM.Engine_Library.ApplicationState;
using SobekCM.Engine_Library.Database;
using SobekCM.Engine_Library.Solr.v5;
using SobekCM.Tools;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;

#endregion

namespace SobekCM.Engine_Library.Endpoints
{
    /// <summary> Endpoint supports services related to search results (or item browses) across the 
    /// entire instance or a subset of aggregations </summary>
    public class ResultsServices : EndpointBase
    {
        /// <summary> Enumeration of errors that may be encountered during these processes </summary>
        public enum ResultsEndpointErrorEnum : byte
        {
            /// <summary> No exception or error detected. (Everything is OK) </summary>
            NONE,

            /// <summary> An unknown exception was caught during this process </summary>
            Unknown,

            /// <summary> Unknown database exception was caught during this process </summary>
            Database_Exception,

            /// <summary> Timeout occurred while querying the database  </summary>
            Database_Timeout_Exception,

            /// <summary> Exception was encountered while querying the solr/lucene indexes </summary>
            Solr_Exception
        }

        protected const bool INCLUDE_PRIVATE = false;

       

        #region Code to support the legacy XML and JSON reports supported prior to v5.0


        /// <summary> Writes the search or browse information in JSON format directly to the output stream  </summary>
        /// <param name="Output"> Stream to which to write the JSON search or browse information </param>
        /// <param name="Args"></param>
        /// <param name="ResultsStats"></param>
        /// <param name="ResultsPage"></param>
        protected internal void legacy_json_display_search_results(TextWriter Output, Results_Arguments Args, Search_Results_Statistics ResultsStats, List<iSearch_Title_Result> ResultsPage)
        {
            // If results are null, or no results, return empty string
            if ((ResultsPage == null) || (ResultsStats == null) || (ResultsStats.Total_Items <= 0))
                return;

            // Get the URL and network roots
            string image_url = Engine_ApplicationCache_Gateway.Settings.Servers.Image_URL;
            string base_url = Engine_ApplicationCache_Gateway.Settings.Servers.Base_URL;
            if ((base_url.Length > 0) && (base_url[base_url.Length - 1] != '/'))
                base_url = base_url + "/";
            if ((image_url.Length > 0) && (image_url[image_url.Length - 1] != '/'))
                image_url = image_url + "/";

            Output.Write("[");

            // Step through all the results
            int i = 1;
            foreach (iSearch_Title_Result titleResult in ResultsPage)
            {
                // Always get the first item for things like the main link and thumbnail
                iSearch_Item_Result firstItemResult = titleResult.Get_Item(0);

                // Determine a thumbnail
                string thumb = image_url + titleResult.BibID.Substring(0, 2) + "/" + titleResult.BibID.Substring(2, 2) + "/" + titleResult.BibID.Substring(4, 2) + "/" + titleResult.BibID.Substring(6, 2) + "/" + titleResult.BibID.Substring(8) + "/" + firstItemResult.VID + "/" + firstItemResult.MainThumbnail;
                if ((thumb.ToUpper().IndexOf(".JPG") < 0) && (thumb.ToUpper().IndexOf(".GIF") < 0))
                {
                    thumb = String.Empty;
                }
                thumb = thumb.Replace("\\", "/").Replace("//", "/").Replace("http:/", "http://");

                // Was a previous item/title included here?
                if (i > 1)
                    Output.Write(",");
                Output.Write("{\"collection_item\":{\"name\":\"" + firstItemResult.Title.Trim().Replace("\"", "'") + "\",\"url\":\"" + base_url + titleResult.BibID + "/" + firstItemResult.VID + "\",\"collection_code\":\"\",\"id\":\"" + titleResult.BibID + "_" + firstItemResult.VID + "\",\"thumb_url\":\"" + thumb + "\"}}");

                i++;
            }

            Output.Write("]");
        }

        /// <summary> Display search results in simple XML format </summary>
        /// <param name="Output"> Stream to which to write the text for this main writer </param>
        /// <param name="Args"></param>
        /// <param name="ResultsStats"></param>
        /// <param name="ResultsPage"></param>
        protected internal void legacy_xml_display_search_results(TextWriter Output, Results_Arguments Args, Search_Results_Statistics ResultsStats, List<iSearch_Title_Result> ResultsPage)
        {
            // Get the URL and network roots
            string image_url = Engine_ApplicationCache_Gateway.Settings.Servers.Image_URL;
            string network = Engine_ApplicationCache_Gateway.Settings.Servers.Image_Server_Network;
            string base_url = Engine_ApplicationCache_Gateway.Settings.Servers.Base_URL;
            if ((base_url.Length > 0) && (base_url[base_url.Length - 1] != '/'))
                base_url = base_url + "/";
            if ((image_url.Length > 0) && (image_url[image_url.Length - 1] != '/'))
                image_url = image_url + "/";

            // Write the header first
            Output.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" ?> ");
            Output.WriteLine("<ResultSet Page=\"" + Args.Page + "\" Total=\"" + ResultsStats.Total_Titles + "\">");

            // Now, add XML for each title
            string lastBibID = string.Empty;
            foreach (iSearch_Title_Result thisResult in ResultsPage)
            {
                if (thisResult.BibID != lastBibID)
                {
                    if (lastBibID.Length > 0)
                        Output.WriteLine("</TitleResult>");
                    Output.WriteLine("<TitleResult ID=\"" + thisResult.BibID + "\">");
                    lastBibID = thisResult.BibID;
                }

                // Determine folder from BibID
                string folder = thisResult.BibID.Substring(0, 2) + "/" + thisResult.BibID.Substring(2, 2) + "/" + thisResult.BibID.Substring(4, 2) + "/" + thisResult.BibID.Substring(6, 2) + "/" + thisResult.BibID.Substring(8);

                // Now, add XML for each item
                for (int i = 0; i < thisResult.Item_Count; i++)
                {
                    iSearch_Item_Result itemResult = thisResult.Get_Item(i);
                    Output.WriteLine("\t<ItemResult ID=\"" + thisResult.BibID + "_" + itemResult.VID + "\">");
                    Output.Write("\t\t<Title>");
                    Write_XML(Output, itemResult.Title);
                    Output.WriteLine("</Title>");
                    if (!String.IsNullOrEmpty(itemResult.PubDate))
                    {
                        Output.Write("\t\t<Date>");
                        Write_XML(Output, itemResult.PubDate);
                        Output.WriteLine("</Date>");
                    }
                    Output.WriteLine("\t\t<Location>");
                    Output.WriteLine("\t\t\t<URL>" + base_url + thisResult.BibID + "/" + itemResult.VID + "</URL>");

                    if (!String.IsNullOrEmpty(itemResult.MainThumbnail))
                    {
                        Output.WriteLine("\t\t\t<MainThumb>" + image_url + folder + "/" + itemResult.VID + "/" + itemResult.MainThumbnail + "</MainThumb>");
                    }

                    Output.WriteLine("\t\t\t<Folder type=\"web\">" + image_url + folder + "/" + itemResult.VID + "</Folder>");
                    Output.WriteLine("\t\t\t<Folder type=\"network\">" + network + folder.Replace("/", "\\") + "\\" + itemResult.VID + "</Folder>");
                    Output.WriteLine("\t\t</Location>");
                    Output.WriteLine("\t</ItemResult>");
                }
            }

            if (ResultsPage.Count > 0)
                Output.WriteLine("</TitleResult>");
            Output.WriteLine("</ResultSet>");
        }

        protected static void Write_XML(TextWriter Output, string Value)
        {
            foreach (char thisChar in Value)
            {
                switch (thisChar)
                {
                    case '>':
                        Output.Write("&gt;");
                        break;

                    case '<':
                        Output.Write("&lt;");
                        break;

                    case '"':
                        Output.Write("&quot;");
                        break;

                    case '&':
                        Output.Write("&amp;");
                        break;

                    default:
                        Output.Write(thisChar);
                        break;
                }
            }
        }

        #endregion

        #region Helper methods that perform the actual searching

        

        protected static void Perform_Solr_Search(Custom_Tracer Tracer, List<string> Terms, List<string> Web_Fields, DateTime? StartDate, DateTime? EndDate, int ActualCount, Complete_Item_Aggregation Aggregation_Object, int Current_Page, int Current_Sort, int Results_Per_Page, out Search_Results_Statistics Complete_Result_Set_Info, out List<iSearch_Title_Result> Paged_Results)
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("SobekCM_Assistant.Perform_Solr_Search", "Build the Solr query");
            }

            // Use this built query to query against Solr
            var searchOptions = new Search_Options_Info();
            searchOptions.Page = Current_Page;
            searchOptions.ResultsPerPage = Results_Per_Page;
            searchOptions.AggregationCode = Aggregation_Object.Code;
            searchOptions.Facets = Aggregation_Object.Facets;
            searchOptions.Fields = Aggregation_Object.Results_Fields;
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
            searchOptions.GroupItemsByTitle = (Aggregation_Object.GroupResults && !contains_full_text);

            v5_Solr_Searcher.Search(Terms, Web_Fields, StartDate, EndDate, searchOptions, null, Tracer, out Complete_Result_Set_Info, out Paged_Results);
        }

        #endregion
    }
}
