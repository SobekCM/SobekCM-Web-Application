#region Using directives

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using SobekCM.Core.Aggregations;
using SobekCM.Core.Client;
using SobekCM.Core.Configuration.Localization;
using SobekCM.Core.Navigation;
using SobekCM.Core.Settings;
using SobekCM.Core.Skins;
using SobekCM.Engine_Library.ApplicationState;
using SobekCM.Engine_Library.Database;
using SobekCM.Library;
using SobekCM.Library.Database;
using SobekCM.Library.UI;
using SobekCM.Tools;

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
            //long staticRebuildLogId = OnProcess("RebuildAllAggregationBrowsesModule : Rebuilding all static pages", "Standard", null, null, -1);

            //// Pull out some values
            //string primaryWebServerUrl = Settings.Servers.Application_Server_URL;
            //string staticSobekcmDataLocation = Settings.Servers.Static_Pages_Location;
            //string staticSobekcmLocation = Settings.Servers.Application_Server_Network;

            //// Create some useful objects
            //Custom_Tracer tracer = new Custom_Tracer();
            //SobekCM_Assistant assistant = new SobekCM_Assistant();


            //// Is this right?
            //string defaultSkin = Engine_ApplicationCache_Gateway.URL_Portals.Default_Portal.Default_Web_Skin;

            //// Set the correct base directory
            //if (staticSobekcmLocation.Length > 0)
            //    Settings.Servers.Base_Directory = staticSobekcmLocation;

            //// Create the mode object
            //Navigation_Object currentMode = new Navigation_Object
            //{
            //    Skin = defaultSkin,
            //    Mode = Display_Mode_Enum.Aggregation,
            //    Aggregation_Type = Aggregation_Type_Enum.Home,
            //    Language = Web_Language_Enum.English,
            //    Base_URL = primaryWebServerUrl
            //};

            //// Get the default web skin
            //Web_Skin_Object defaultSkinObject = assistant.Get_HTML_Skin(currentMode, Engine_ApplicationCache_Gateway.Web_Skin_Collection, false, null);

            //// Get the list of all collections
            //DataTable allCollections = Engine_Database.Get_Codes_Item_Aggregations(null);
            //DataView collectionView = new DataView(allCollections) { Sort = "Name ASC" };

            //// Build the basic site map first
            //StreamWriter sitemap_writer = new StreamWriter(staticSobekcmDataLocation + "sitemap_collections.xml", false);
            //sitemap_writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            //sitemap_writer.WriteLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
            //sitemap_writer.WriteLine("\t<url>");
            //sitemap_writer.WriteLine("\t\t<loc>" + primaryWebServerUrl + "</loc>");
            //sitemap_writer.WriteLine("\t</url>");

            //// Prepare to build all the links to static pages
            //StringBuilder static_browse_links = new StringBuilder();
            //StringBuilder recent_rss_link_builder = new StringBuilder();
            //StringBuilder all_rss_link_builder = new StringBuilder();
            //int col = 2;
            //DataSet items;
            //List<string> processed_codes = new List<string>();
            //foreach (DataRowView thisCollectionView in collectionView)
            //{
            //    // Clear the tracer
            //    tracer.Clear();

            //    if (!Convert.ToBoolean(thisCollectionView.Row["Hidden"]))
            //    {
            //        // Build the static links pages
            //        string code = thisCollectionView.Row["Code"].ToString().ToLower();
            //        if ((!processed_codes.Contains(code)) && (code != "all"))
            //        {
            //            processed_codes.Add(code);

            //            // Add this to the sitemap
            //            sitemap_writer.WriteLine("\t<url>");
            //            sitemap_writer.WriteLine("\t\t<loc>" + primaryWebServerUrl + code + "</loc>");
            //            sitemap_writer.WriteLine("\t</url>");

            //            OnProcess("RebuildAllAggregationBrowsesModule : Building static links page for " + code, "Standard", null, null, staticRebuildLogId);

            //            //Get the list of items for this collection
            //            items = Engine_Database.Simple_Item_List(code, tracer);

            //            // Get the item aggregation object
            //            Item_Aggregation aggregation = SobekEngineClient.Aggregations.Get_Aggregation(code.ToLower(), UI_ApplicationCache_Gateway.Settings.System.Default_UI_Language, UI_ApplicationCache_Gateway.Settings.System.Default_UI_Language, tracer);


            //            // Build the static browse pages
            //            if (Build_All_Browse(aggregation, items))
            //            {
            //                static_browse_links.Append("<td><a href=\"" + code + "_all.html\">" + code + "</a></td>" + Environment.NewLine);
            //                col++;
            //            }

            //            if (col > 5)
            //            {
            //                static_browse_links.Append("</tr>" + Environment.NewLine + "<tr>");
            //                col = 1;
            //            }

            //            // Build the RSS feeds
            //            OnProcess("RebuildAllAggregationBrowsesModule : Building RSS feeds for " + code, "Standard", null, null, staticRebuildLogId);


            //            if (Create_RSS_Feed(code, staticSobekcmDataLocation + "rss\\", thisCollectionView.Row["Name"].ToString(), items))
            //            {
            //                recent_rss_link_builder.Append("<img src=\"http://cdn.sobekrepository.org/images/misc/16px-Feed-icon.svg.png\" alt=\"RSS\" width=\"16\" height=\"16\">&nbsp;<a href=\"" + primaryWebServerUrl + "rss/" + code + "_short_rss.xml\">" + thisCollectionView.Row["Name"] + "</a><br />" + Environment.NewLine);
            //                all_rss_link_builder.Append("<img src=\"http://cdn.sobekrepository.org/images/misc/16px-Feed-icon.svg.png\" alt=\"RSS\" width=\"16\" height=\"16\">&nbsp;<a href=\"" + primaryWebServerUrl + "rss/" + code + "_rss.xml\">" + thisCollectionView.Row["Name"] + "</a><br />" + Environment.NewLine);
            //            }
            //        }
            //    }
            //}

            //// Finish out the collection sitemap
            //sitemap_writer.WriteLine("</urlset>");
            //sitemap_writer.Flush();
            //sitemap_writer.Close();

            //items = Engine_Database.Simple_Item_List(String.Empty, tracer);
            //OnProcess("RebuildAllAggregationBrowsesModule : Building static links page for ALL ITEMS", "Standard", null, null, staticRebuildLogId);

            //Item_Aggregation allAggregation = SobekEngineClient.Aggregations.Get_Aggregation("all", Settings.System.Default_UI_Language, Settings.System.Default_UI_Language, tracer);

            //Build_All_Browse(allAggregation, items);

            //OnProcess("RebuildAllAggregationBrowsesModule : Building RSS feeds ALL ITEMS", "Standard", null, null, staticRebuildLogId);

            //Create_RSS_Feed("all", staticSobekcmDataLocation + "rss\\", "All " + Settings.System.System_Abbreviation + " Items", items);

            //// Build the site maps
            //OnProcess("RebuildAllAggregationBrowsesModule : Building site maps", "Standard", null, null, staticRebuildLogId);

            //int sitemaps = Build_Site_Maps();

            //// Output the main browse and rss links pages
            //OnProcess("RebuildAllAggregationBrowsesModule : Building main sitemaps and links page", "Standard", null, null, staticRebuildLogId);


            //StreamWriter allListWriter = new StreamWriter(staticSobekcmDataLocation + "index.html", false);
            //allListWriter.WriteLine("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">");
            //allListWriter.WriteLine("<html xmlns=\"http://www.w3.org/1999/xhtml\" >");
            //allListWriter.WriteLine("<head>");
            //allListWriter.WriteLine("  <title>" + UI_ApplicationCache_Gateway.Settings.System.System_Name + " Site Map Links</title>");
            //allListWriter.WriteLine();
            //allListWriter.WriteLine("  <meta name=\"robots\" content=\"index, follow\">");
            //allListWriter.WriteLine("  <link href=\"" + primaryWebServerUrl + "default/SobekCM.css\" rel=\"stylesheet\" type=\"text/css\" title=\"standard\" />");
            //if (!String.IsNullOrEmpty(defaultSkinObject.CSS_Style))
            //{
            //    allListWriter.WriteLine("  <link href=\"" + UI_ApplicationCache_Gateway.Settings.Servers.System_Base_URL + defaultSkinObject.CSS_Style + "\" rel=\"stylesheet\" type=\"text/css\" />");
            //}

            //allListWriter.WriteLine("</head>");
            //allListWriter.WriteLine("<body>");

            //allListWriter.WriteLine("<div id=\"container-inner\">");

            //string banner = "<div id=\"sbkHmw_BannerDiv\"><a alt=\"All Collections\" href=\"" + primaryWebServerUrl + "\" style=\"padding-bottom:0px;margin-bottom:0px\"><img id=\"mainBanner\" src=\"" + primaryWebServerUrl + "design/aggregations/all/images/banners/coll.jpg\" alt=\"\" /></a></div>";
            //Display_Header(allListWriter, defaultSkinObject, banner);

            //allListWriter.WriteLine("<br /><br />This page is to provide static links to all resources in " + UI_ApplicationCache_Gateway.Settings.System.System_Abbreviation + ". <br />");
            //allListWriter.WriteLine("Click <a href=\"" + primaryWebServerUrl + "\">HERE</a> to return to main library. <br />");
            //allListWriter.WriteLine("<br />");
            //allListWriter.WriteLine("<br />");
            //allListWriter.WriteLine("SITE MAPS<br />");
            //allListWriter.WriteLine("<br />");
            //allListWriter.WriteLine("<a href=\"sitemap_collections.xml\">Map to all the collection home pages</a><br />");
            //if (sitemaps > 0)
            //{
            //    for (int i = 1; i <= sitemaps; i++)
            //    {
            //        allListWriter.WriteLine("<a href=\"sitemap" + i + ".xml\">Site Map File " + i + "</a><br />");
            //    }
            //    allListWriter.WriteLine("<br />");
            //    allListWriter.WriteLine("<br />");
            //}
            //else
            //{
            //    allListWriter.WriteLine("NO SITE MAPS GENERATED!");
            //}

            //Display_Footer(allListWriter, defaultSkinObject);
            //allListWriter.WriteLine("</div>");
            //allListWriter.WriteLine("</body>");
            //allListWriter.WriteLine("</html>");
            //allListWriter.Flush();
            //allListWriter.Close();

            //// Create the list of all the RSS feeds
            //try
            //{
            //    OnProcess("RebuildAllAggregationBrowsesModule : Building main rss feed page", "Standard", null, null, staticRebuildLogId);

            //    StreamWriter writer = new StreamWriter(staticSobekcmDataLocation + "rss\\index.htm", false);
            //    writer.WriteLine("<!DOCTYPE html>");
            //    writer.WriteLine("<html>");
            //    writer.WriteLine("<head>");
            //    writer.WriteLine("  <title>RSS Feeds for " + UI_ApplicationCache_Gateway.Settings.System.System_Abbreviation + "</title>");
            //    writer.WriteLine();
            //    writer.WriteLine("  <meta name=\"robots\" content=\"index, follow\">");
            //    writer.WriteLine("  <link href=\"" + primaryWebServerUrl + "default/SobekCM.css\" rel=\"stylesheet\" type=\"text/css\" title=\"standard\" />");
            //    if (!String.IsNullOrEmpty(defaultSkinObject.CSS_Style))
            //    {
            //        writer.WriteLine("  <link href=\"" + UI_ApplicationCache_Gateway.Settings.Servers.System_Base_URL + defaultSkinObject.CSS_Style + "\" rel=\"stylesheet\" type=\"text/css\" />");
            //    }
            //    writer.WriteLine("</head>");
            //    writer.WriteLine("<body>");

            //    writer.WriteLine("<div id=\"container-inner\">");

            //    string banner2 = "<div id=\"sbkHmw_BannerDiv\"><a alt=\"All Collections\" href=\"" + primaryWebServerUrl + "\" style=\"padding-bottom:0px;margin-bottom:0px\"><img id=\"mainBanner\" src=\"" + primaryWebServerUrl + "design/aggregations/all/images/banners/coll.jpg\" alt=\"\" /></a></div>";
            //    Display_Header(writer, defaultSkinObject, banner2);

            //    writer.WriteLine("<div id=\"pagecontainer\">");


            //    writer.WriteLine("<div class=\"ViewsBrowsesRow\">");
            //    writer.WriteLine("  <ul class=\"sbk_FauxUpwardTabsList\">");
            //    writer.WriteLine("    <li><a href=\"" + primaryWebServerUrl + "\">" + UI_ApplicationCache_Gateway.Settings.System.System_Abbreviation + " HOME</a></li>");
            //    writer.WriteLine("    <li class=\"current\">RSS FEEDS</li>");
            //    writer.WriteLine("  </ul>");
            //    writer.WriteLine("</div>");
            //    writer.WriteLine();
            //    writer.WriteLine("<div class=\"SobekSearchPanel\">");
            //    writer.WriteLine("  <h1>RSS Feeds for the " + UI_ApplicationCache_Gateway.Settings.System.System_Name + "</h1>");
            //    writer.WriteLine("</div>");
            //    writer.WriteLine();

            //    writer.WriteLine("<div class=\"SobekHomeText\">");
            //    writer.WriteLine("<table width=\"700\" border=\"0\" align=\"center\">");
            //    writer.WriteLine("  <tr>");
            //    writer.WriteLine("    <td>");
            //    writer.WriteLine("      <br />");
            //    writer.WriteLine("      This page provides links to RSS feeds for items within " + UI_ApplicationCache_Gateway.Settings.System.System_Name + ".  The first group of RSS feeds below contains the last 20 items added to the collection.  The second group of items contains links to every item in a collection.  These rss feeds can grow quite lengthy and the load time is often non-trivial.<br />");
            //    writer.WriteLine("      <br />");
            //    writer.WriteLine("      In addition, the following three RSS feeds are provided:");
            //    writer.WriteLine("      <blockquote>");
            //    writer.WriteLine("        <img src=\"http://cdn.sobekrepository.org/images/misc/16px-Feed-icon.svg.png\" alt=\"RSS\" width=\"16\" height=\"16\">&nbsp;<a href=\"" + primaryWebServerUrl + "rss/all_rss.xml\">All items in " + UI_ApplicationCache_Gateway.Settings.System.System_Abbreviation + "</a><br />");
            //    writer.WriteLine("        <img src=\"http://cdn.sobekrepository.org/images/misc/16px-Feed-icon.svg.png\" alt=\"RSS\" width=\"16\" height=\"16\">&nbsp;<a href=\"" + primaryWebServerUrl + "rss/all_short_rss.xml\">Most recently added items in " + UI_ApplicationCache_Gateway.Settings.System.System_Abbreviation + " (last 100)</a><br />");
            //    writer.WriteLine("      </blockquote>");
            //    writer.WriteLine("      RSS feeds	are a way to keep up-to-date on new materials that are added to the Digital Collections. RSS feeds are written in XML    and require a news reader to access.<br />");
            //    writer.WriteLine("      <br />");
            //    writer.WriteLine("      You can download and install a <a href=\"http://dmoz.org/Reference/Libraries/Library_and_Information_Science/Technical_Services/Cataloguing/Metadata/RDF/Applications/RSS/News_Readers/\">news reader</a>.  Or, you can use a Web-based reader such as <a href=\"http://www.google.com/reader\">Google Reader </a>or <a href=\"http://my.yahoo.com/\">My Yahoo!</a>.");
            //    writer.WriteLine("      Follow the instructions in your reader to subscribe to the feed of   your choice. You will usually need to copy and paste the feed URL into the reader. <br />");
            //    writer.WriteLine("      <br />");
            //    writer.WriteLine("    </td>");
            //    writer.WriteLine("  </tr>");
            //    writer.WriteLine("</table>");

            //    writer.WriteLine("<table width=\"750\" border=\"0\" align=\"center\" cellpadding=\"1\" cellspacing=\"0\">");
            //    writer.WriteLine("	<tr>");
            //    writer.WriteLine("    <td bgcolor=\"#cccccc\">");
            //    writer.WriteLine("      <table width=\"100%\" border=\"0\" align=\"center\" cellpadding=\"2\" cellspacing=\"0\">");
            //    writer.WriteLine("		  <tr>");
            //    writer.WriteLine("          <td bgcolor=\"#f4f4f4\"><span class=\"groupname\"><span class=\"groupnamecaps\"> &nbsp; M</span>OST <span class=\"groupnamecaps\">R</span>ECENT <span class=\"groupnamecaps\">I</span>TEMS (BY COLLECTION)</span></td>");
            //    writer.WriteLine("        </tr>");
            //    writer.WriteLine("      </table>");
            //    writer.WriteLine("    </td>");
            //    writer.WriteLine("  </tr>");
            //    writer.WriteLine("<table>");

            //    writer.WriteLine("<div class=\"SobekHomeText\">");
            //    writer.WriteLine("<table width=\"700\" border=\"0\" align=\"center\">");
            //    writer.WriteLine("  <tr>");
            //    writer.WriteLine("    <td>");
            //    writer.WriteLine(recent_rss_link_builder.ToString());
            //    writer.WriteLine("      <br />");
            //    writer.WriteLine("    </td>");
            //    writer.WriteLine("  </tr>");
            //    writer.WriteLine("</table>");

            //    writer.WriteLine("<table width=\"750\" border=\"0\" align=\"center\" cellpadding=\"1\" cellspacing=\"0\">");
            //    writer.WriteLine("	<tr>");
            //    writer.WriteLine("    <td bgcolor=\"#cccccc\">");
            //    writer.WriteLine("      <table width=\"100%\" border=\"0\" align=\"center\" cellpadding=\"2\" cellspacing=\"0\">");
            //    writer.WriteLine("		  <tr>");
            //    writer.WriteLine("          <td bgcolor=\"#f4f4f4\"><span class=\"groupname\"><span class=\"groupnamecaps\"> &nbsp; A</span>LL <span class=\"groupnamecaps\">I</span>TEMS (BY COLLECTION) </span></td>");
            //    writer.WriteLine("        </tr>");
            //    writer.WriteLine("      </table>");
            //    writer.WriteLine("    </td>");
            //    writer.WriteLine("  </tr>");
            //    writer.WriteLine("<table>");

            //    writer.WriteLine("<div class=\"SobekHomeText\">");
            //    writer.WriteLine("<table width=\"700\" border=\"0\" align=\"center\">");
            //    writer.WriteLine("  <tr>");
            //    writer.WriteLine("    <td>");
            //    writer.WriteLine(all_rss_link_builder.ToString());
            //    writer.WriteLine("      <br />");
            //    writer.WriteLine("    </td>");
            //    writer.WriteLine("  </tr>");
            //    writer.WriteLine("</table>");


            //    writer.WriteLine("<br />");


            //    writer.WriteLine("</div>");
            //    writer.WriteLine("</div>");
            //    Display_Footer(writer, defaultSkinObject);
            //    writer.WriteLine("</div>");
            //    writer.WriteLine("</body>");
            //    writer.WriteLine("</html>");

            //    writer.Flush();
            //    writer.Close();
            //}
            //catch
            //{
            //    OnError("RebuildAllAggregationBrowsesModule : Error building main RSS index.htm file", null, null, staticRebuildLogId);
            //}

            //return errors;


            //Static_Pages_Builder builder = new Static_Pages_Builder(Settings.Servers.Application_Server_URL, Settings.Servers.Static_Pages_Location, Engine_ApplicationCache_Gateway.URL_Portals.Default_Portal.Default_Web_Skin);

            //builder.Rebuild_All_Static_Pages(preloader_logger, false, dbInstance.Name, staticRebuildLogId);
        }
    }
}
