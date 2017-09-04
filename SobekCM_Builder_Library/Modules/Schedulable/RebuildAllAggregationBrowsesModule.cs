#region Using directives

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Text;
using SobekCM.Builder_Library.Tools;
using SobekCM.Core.Settings;
using SobekCM.Engine_Library.Database;

#endregion

namespace SobekCM.Builder_Library.Modules.Schedulable
{
    /// <summary> Schedulable builder module rebuilds the static browse pages for the instance and aggregations  </summary>
    /// <remarks> This class implements the <see cref="abstractSchedulableModule" /> abstract class and implements the <see cref="iSchedulableModule" /> interface. </remarks>
    public class RebuildAllAggregationBrowsesModule : abstractSchedulableModule
    {
        /// <summary> Rebuilds the static browse pages for the instance and aggregations </summary>
        /// <param name="Settings"> Instance-wide settings which may be required for this process </param>
        public override void DoWork(InstanceWide_Settings Settings)
        {
            // Add the primary log entry
            long updatedId = OnProcess("RebuildAllAggregationBrowsesModule:Performing some aggregation update functions", "Aggregation Updates", String.Empty, String.Empty, -1);


            try
            {
                // Create a list of all the collection codes
                DataTable allCollections = Engine_Database.Get_Codes_Item_Aggregations(null);
                DataView collectionView = new DataView(allCollections) {Sort = "Name ASC"};
                List<string> allCodes = new List<string>();
                foreach (DataRowView collectionRow in collectionView)
                {
                    bool hidden = bool.Parse(collectionRow["Hidden"].ToString());
                    bool active = bool.Parse(collectionRow["isActive"].ToString());

                    if ((!hidden) && (active))
                    {
                        allCodes.Add(collectionRow["Code"].ToString().ToLower());
                    }
                }

                // Create the (new) helper class
                Aggregation_Static_Page_Writer staticWriter = new Aggregation_Static_Page_Writer();
                staticWriter.Process += staticWriter_Process;
                staticWriter.Error += staticWriter_Error;

                // IN THIS CASE, WE DO NEED TO SET THE SINGLETON, SINCE THIS CALLS THE LIBRARIES
                //Engine_ApplicationCache_Gateway.Settings = Settings;

                // Build the aggregation browses
                staticWriter.ReCreate_Aggregation_Level_Pages(allCodes, Settings, updatedId);

                // Build the primary URL 
                string primaryUrl = Settings.Servers.Application_Server_URL;
                if (String.IsNullOrEmpty(primaryUrl))
                {
                    OnError("Primary system URL is not set", null, null, updatedId);
                    return;
                }
                if (primaryUrl[primaryUrl.Length - 1] != '/')
                    primaryUrl = primaryUrl + "/";

                // Find the data source
                string staticSobekcmDataLocation = Settings.Servers.Base_Data_Directory;

                OnProcess("Creating sitemaps and top-level link pages", "Aggregation Updates", String.Empty, String.Empty, updatedId);


                // Build the sitemap for all the collections
                StreamWriter sitemap_writer = new StreamWriter(Path.Combine(staticSobekcmDataLocation, "sitemap_collections.xml"), false);
                sitemap_writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                sitemap_writer.WriteLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
                sitemap_writer.WriteLine("\t<url>");
                sitemap_writer.WriteLine("\t\t<loc>" + primaryUrl + "</loc>");
                sitemap_writer.WriteLine("\t</url>");

                // Prepare to build all the links to static pages
                StringBuilder static_browse_links = new StringBuilder();
                StringBuilder recent_rss_link_builder = new StringBuilder();
                StringBuilder all_rss_link_builder = new StringBuilder();
                int col = 2;
                foreach (DataRowView collectionRow in collectionView)
                {
                    // If hidden or in active, skip this
                    bool hidden = bool.Parse(collectionRow["Hidden"].ToString());
                    bool active = bool.Parse(collectionRow["isActive"].ToString());
                    if ((hidden) || (!active))
                    {
                        continue;
                    }

                    string code = collectionRow["Code"].ToString().ToLower();

                    // Add this to the sitemap
                    sitemap_writer.WriteLine("\t<url>");
                    sitemap_writer.WriteLine("\t\t<loc>" + primaryUrl + code + "</loc>");
                    sitemap_writer.WriteLine("\t</url>");

                    // build the HTML section for the page that links to all the collections
                    if (File.Exists(Path.Combine(staticSobekcmDataLocation, code + "_all.html")))
                    {
                        static_browse_links.Append("<td><a href=\"" + code + "_all.html\">" + code + "</a></td>" + Environment.NewLine);
                        col++;
                        if (col > 5)
                        {
                            static_browse_links.Append("</tr>" + Environment.NewLine + "<tr>");
                            col = 1;
                        }
                    }

                    // Also, we will link to each collections' rss feed
                    if (File.Exists(Path.Combine(staticSobekcmDataLocation, "rss", code + "_rss.xml")))
                    {
                        recent_rss_link_builder.Append("<img src=\"http://cdn.sobekrepository.org/images/misc/16px-Feed-icon.svg.png\" alt=\"RSS\" width=\"16\" height=\"16\">&nbsp;<a href=\"" + primaryUrl + "rss/" + code + "_short_rss.xml\">" + collectionRow["Name"] + "</a><br />" + Environment.NewLine);
                        all_rss_link_builder.Append("<img src=\"http://cdn.sobekrepository.org/images/misc/16px-Feed-icon.svg.png\" alt=\"RSS\" width=\"16\" height=\"16\">&nbsp;<a href=\"" + primaryUrl + "rss/" + code + "_rss.xml\">" + collectionRow["Name"] + "</a><br />" + Environment.NewLine);
                    }
                }

                // Finish out the collection sitemap
                sitemap_writer.WriteLine("</urlset>");
                sitemap_writer.Flush();
                sitemap_writer.Close();

                // Build the indiviual site maps
                int sitemaps = staticWriter.Build_Site_Maps(staticSobekcmDataLocation, primaryUrl);

                // Create the sitemaps collection index
                StreamWriter sitemap_collections_writer = new StreamWriter(Path.Combine(staticSobekcmDataLocation, "sitemaps.xml"), false);
                sitemap_collections_writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                sitemap_collections_writer.WriteLine("<sitemapindex xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
                string current_date = DateTime.Now.Year + "-" + DateTime.Now.Month.ToString().PadLeft(2, '0') + "-" + DateTime.Now.Day.ToString().PadLeft(2, '0');
                sitemap_collections_writer.WriteLine("  <sitemap>");
                sitemap_collections_writer.WriteLine("    <loc>" + primaryUrl + "data/sitemap_collections.xml</loc>");
                sitemap_collections_writer.WriteLine("    <lastmod>" + current_date + "</lastmod>");
                sitemap_collections_writer.WriteLine("  </sitemap>");
                for (int i = 1; i <= sitemaps; i++)
                {
                    sitemap_collections_writer.WriteLine("  <sitemap>");
                    sitemap_collections_writer.WriteLine("    <loc>" + primaryUrl + "data/sitemap" + i + ".xml</loc>");
                    sitemap_collections_writer.WriteLine("    <lastmod>" + current_date + "</lastmod>");
                    sitemap_collections_writer.WriteLine("  </sitemap>");
                }
                sitemap_collections_writer.WriteLine("</sitemapindex>");
                sitemap_collections_writer.Flush();
                sitemap_collections_writer.Close();

                // Get the top-level html windows
                const string TOKEN = "<div id=\"empty\"></div>";

                // Try to get the collection empty page
                string empty_page_source;
                try
                {
                    using (WebClient client = new WebClient())
                    {
                        string empty_page = primaryUrl + "empty";
                        empty_page_source = client.DownloadString(empty_page);
                    }
                }
                catch (Exception ee)
                {
                    OnError("RebuildAllAggregationBrowsesModule:Unable to pull the top-level empty page", null, null, updatedId);
                    return;
                }

                // Ensure this has length and has the token to replace
                if ((String.IsNullOrEmpty(empty_page_source)) || (empty_page_source.IndexOf(TOKEN) < 0))
                {
                    OnError("RebuildAllAggregationBrowsesModule:Top-level empty page content is invalid", null, null, updatedId);
                    return;
                }

                // Build the main HTML page linking to all RSS feeds
                OnProcess("Building HTML page with links to all the RSS feeds", "Aggregation Updates", null, null, updatedId);
                using (StreamWriter main_rss_writer = new StreamWriter(Path.Combine(staticSobekcmDataLocation, "rss", "index.htm"), false))
                {
                    StringBuilder main_rss_builder = new StringBuilder(2000);
                    main_rss_builder.AppendLine("<div id=\"pagecontainer\">");
                    main_rss_builder.AppendLine("<div class=\"SobekText\" role=\"main\" id=\"main-content\">");
                    main_rss_builder.AppendLine("<h1><strong>RSS Feeds for the " + Settings.System.System_Name + "</strong></h1>");
                    main_rss_builder.AppendLine();

                    main_rss_builder.AppendLine("<div class=\"SobekHomeText\">");
                    main_rss_builder.AppendLine("  <p>This page provides links to RSS feeds for items within " + Settings.System.System_Name + ".  The first group of RSS feeds below contains the last 20 items added to the collection.  The second group of items contains links to every item in a collection.  These rss feeds can grow quite lengthy and the load time is often non-trivial.</p>");
                    main_rss_builder.AppendLine("  <p>In addition, the following three RSS feeds are provided:</p>");
                    main_rss_builder.AppendLine("  <blockquote>");
                    main_rss_builder.AppendLine("    <img src=\"http://cdn.sobekrepository.org/images/misc/16px-Feed-icon.svg.png\" alt=\"RSS\" width=\"16\" height=\"16\">&nbsp;<a href=\"" + primaryUrl + "rss/all_rss.xml\">All items in " + Settings.System.System_Abbreviation + "</a><br />");
                    main_rss_builder.AppendLine("    <img src=\"http://cdn.sobekrepository.org/images/misc/16px-Feed-icon.svg.png\" alt=\"RSS\" width=\"16\" height=\"16\">&nbsp;<a href=\"" + primaryUrl + "rss/all_short_rss.xml\">Most recently added items in " + Settings.System.System_Abbreviation + " (last 100)</a><br />");
                    main_rss_builder.AppendLine("  </blockquote>");
                    main_rss_builder.AppendLine("  <p>RSS feeds are a way to keep up-to-date on new materials that are added to the Digital Collections. RSS feeds are written in XML    and require a news reader to access.</p>");
                    main_rss_builder.AppendLine("  <p>You can download and install a <a href=\"http://dmoz.org/Reference/Libraries/Library_and_Information_Science/Technical_Services/Cataloguing/Metadata/RDF/Applications/RSS/News_Readers/\">news reader</a>.  Or, you can use a Web-based reader such as <a href=\"http://www.google.com/reader\">Google Reader </a>or <a href=\"http://my.yahoo.com/\">My Yahoo!</a>.</p>");
                    main_rss_builder.AppendLine("  <p>Follow the instructions in your reader to subscribe to the feed of   your choice. You will usually need to copy and paste the feed URL into the reader. </p>");
                    main_rss_builder.AppendLine("</div>");
                    main_rss_builder.AppendLine();
                    main_rss_builder.AppendLine("<h2>Most Recent Items (By Collection)</h2>");
                    main_rss_builder.AppendLine();
                    main_rss_builder.AppendLine("<div class=\"SobekHomeText\">");
                    main_rss_builder.AppendLine("  <blockquote>");
                    main_rss_builder.AppendLine(recent_rss_link_builder.ToString());
                    main_rss_builder.AppendLine("  </blockquote>");
                    main_rss_builder.AppendLine("</div>");

                    main_rss_builder.AppendLine("<h2>All Items (By Collection)</h2>");

                    main_rss_builder.AppendLine("<div class=\"SobekHomeText\">");
                    main_rss_builder.AppendLine("  <blockquote>");
                    main_rss_builder.AppendLine(all_rss_link_builder.ToString());
                    main_rss_builder.AppendLine("  </blockquote>");
                    main_rss_builder.AppendLine("</div>");

                    main_rss_builder.AppendLine("</div>");
                    main_rss_builder.AppendLine("</div>");

                    // Now, use this to replace the token
                    main_rss_writer.WriteLine(empty_page_source.Replace(TOKEN, main_rss_builder.ToString()));

                    main_rss_writer.Flush();
                    main_rss_writer.Close();
                }

                // Create the HTML page with links to the sitemap pages
                OnProcess("Building HTML page with links to all the sitemaps", "Aggregation Updates", null, null, updatedId);
                using (StreamWriter main_html_sitemap_writer = new StreamWriter(Path.Combine(staticSobekcmDataLocation, "sitemaps.htm"), false))
                {
                    StringBuilder main_html_subwriter_builder = new StringBuilder(2000);
                    main_html_subwriter_builder.AppendLine("<div id=\"pagecontainer\">");
                    main_html_subwriter_builder.AppendLine("<div class=\"SobekText\" role=\"main\" id=\"main-content\">");
                    main_html_subwriter_builder.AppendLine("<h1><strong>Sitemaps for the " + Settings.System.System_Name + "</strong></h1>");
                    main_html_subwriter_builder.AppendLine();


                    main_html_subwriter_builder.AppendLine("<div class=\"SobekHomeText\">");
                    main_html_subwriter_builder.AppendLine("  <p>This page provides links to all the sitemaps with links for " + Settings.System.System_Name + ".</p>");
                    main_html_subwriter_builder.AppendLine("  <p>Sitemaps are a primary source of links to index for search engines.  Inclusion of your collections materials in these sitemaps facilitates indexing by search engines and ensures your material will be included in searches from Google, Bing, etc.. </p>");
                    main_html_subwriter_builder.AppendLine("  <br />");
                    main_html_subwriter_builder.AppendLine("</div>");
                    main_html_subwriter_builder.AppendLine();
                    main_html_subwriter_builder.AppendLine("<h2>Sitemap Collection</h2>");
                    main_html_subwriter_builder.AppendLine();
                    main_html_subwriter_builder.AppendLine("<div class=\"SobekHomeText\">");
                    main_html_subwriter_builder.AppendLine("  <p>This is a sitemap which includes links to all the other sitemaps in your digital repository.  This is generally the sitemap that should be referenced in your robots.txt file.</p>");
                    main_html_subwriter_builder.AppendLine("  <blockquote>");
                    main_html_subwriter_builder.AppendLine("    <a href=\"" + primaryUrl + "data/sitemaps.xml\">Collection of all sitemaps</a><br />");
                    main_html_subwriter_builder.AppendLine("  </blockquote>");
                    main_html_subwriter_builder.AppendLine("</div>");
                    main_html_subwriter_builder.AppendLine();

                    main_html_subwriter_builder.AppendLine("<h2>Individual Sitemaps</h2>");
                    main_html_subwriter_builder.AppendLine();
                    main_html_subwriter_builder.AppendLine("<div class=\"SobekHomeText\">");
                    main_html_subwriter_builder.AppendLine("  <p>These sitemaps link to your individual collections and digital resources.  Sitemaps are generally limited to 30,000 links each, so you may have many sitemaps linked to your individual digital resources.</p>");
                    main_html_subwriter_builder.AppendLine("  <blockquote>");
                    main_html_subwriter_builder.AppendLine("  <a href=\"" + primaryUrl + "data/sitemap_collections.xml\">Sitemap pointing to all the collections</a><br />");

                    for (int i = 1; i <= sitemaps; i++)
                    {
                        main_html_subwriter_builder.AppendLine("  <a href=\"" + primaryUrl + "data/sitemap" + i + ".xml\">Sitemap #" + i + "</a><br />");
                    }

                    main_html_subwriter_builder.AppendLine("  </blockquote>");
                    main_html_subwriter_builder.AppendLine("</div>");
                    main_html_subwriter_builder.AppendLine();

                    main_html_subwriter_builder.AppendLine("</div>");
                    main_html_subwriter_builder.AppendLine("</div>");

                    // Now, use this to replace the token
                    main_html_sitemap_writer.WriteLine(empty_page_source.Replace(TOKEN, main_html_subwriter_builder.ToString()));

                    main_html_sitemap_writer.Flush();
                    main_html_sitemap_writer.Close();
                }
            }
            catch (Exception ee)
            {
                OnError("RebuildAllAggregationBrowsesModule: Top-level exception caught during procssing : " + ee.Message, null, null, updatedId);
            }

        }


        void staticWriter_Error(string Message, long UpdateID)
        {
            OnError(Message, null, null, UpdateID);
        }

        void staticWriter_Process(string Message, long UpdateID)
        {
            OnProcess(Message, "Aggregation Updates", null, null, UpdateID);
        }
    }
}
