using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Text;
using SobekCM.Builder_Library.Settings;
using SobekCM.Core.Aggregations;
using SobekCM.Core.Settings;
using SobekCM.Engine_Library.Aggregations;
using SobekCM.Engine_Library.ApplicationState;
using SobekCM.Engine_Library.Database;
using SobekCM.Tools;

namespace SobekCM.Builder_Library.Tools
{

    public class Aggregation_Static_Page_Writer
    {
        /// <summary> Delegate for any messages passed back out during static page creation </summary>
        /// <param name="Message"> Message </param>
        public delegate void AggregationStaticPageWriterDelegate( string Message, long UpdateID );

        /// <summary> Event is fired when an error occurs during processing </summary>
        public event AggregationStaticPageWriterDelegate Error;

        /// <summary> Event is fired to report progress during processing </summary>
        public event AggregationStaticPageWriterDelegate Process;

        public void ReCreate_Aggregation_Level_Pages(List<string> AggregationsAffected, InstanceWide_Settings Settings, long UpdateID )
        {

            // Determine, and create the local work space
            string localWorkArea = Path.Combine(MultiInstance_Builder_Settings.Builder_Executable_Directory, "temp");
            try
            {
                if (!Directory.Exists(localWorkArea))
                    Directory.CreateDirectory(localWorkArea);
            }
            catch
            {
                OnError("Error creating the temporary work area in BuildAggregationBrowsesModule: " + localWorkArea, UpdateID);
                return;
            }

            // Build the primary URL 
            string primaryUrl = Settings.Servers.Application_Server_URL;
            if (String.IsNullOrEmpty(primaryUrl))
            {
                OnError("Primary system URL is not set", UpdateID);
                return;
            }
            if (primaryUrl[primaryUrl.Length - 1] != '/')
                primaryUrl = primaryUrl + "/";

            // Create the new statics page builder
            // IN THIS CASE, WE DO NEED TO SET THE SINGLETON, SINCE THIS CALLS THE LIBRARIES
            Engine_ApplicationCache_Gateway.Settings = Settings;
            // Static_Pages_Builder staticBuilder = new Static_Pages_Builder(Settings.Servers.Application_Server_URL, Settings.Servers.Static_Pages_Location, Settings.Servers.Application_Server_Network);

            try
            {
                // Step through each aggregation with new items
                foreach (string thisAggrCode in AggregationsAffected)
                {
                    // Some aggregations can be excluded
                    if ((thisAggrCode != "ALL") && (thisAggrCode.Length > 1))
                    {
                        // Get the display aggregation code (lower leading 'i')
                        string display_code = thisAggrCode;
                        if (display_code[0] == 'I')
                            display_code = 'i' + display_code.Substring(1);

                        // Get this item aggregations
                        Complete_Item_Aggregation aggregationCompleteObj = Engine_Database.Get_Item_Aggregation(thisAggrCode, false, null);
                        Item_Aggregation aggregationObj = Item_Aggregation_Utilities.Get_Item_Aggregation(aggregationCompleteObj, Settings.System.Default_UI_Language, null);

                        // Get the list of items for this aggregation
                        DataSet aggregation_items = Engine_Database.Simple_Item_List(thisAggrCode, null);

                        // Create the XML list for this aggregation
                        OnProcess("........Building XML item list for " + display_code, UpdateID);
                        try
                        {
                            string aggregation_list_file = Settings.Servers.Static_Pages_Location + "\\" + thisAggrCode.ToLower() + ".xml";
                            if (File.Exists(aggregation_list_file))
                                File.Delete(aggregation_list_file);
                            aggregation_items.WriteXml(aggregation_list_file, XmlWriteMode.WriteSchema);
                        }
                        catch (Exception ee)
                        {
                            OnError("........Error in building XML list for " + display_code + " on " + Settings.Servers.Static_Pages_Location + "\n" + ee.Message, UpdateID);
                        }

                        OnProcess("........Building RSS feed for " + display_code, UpdateID);
                        try
                        {
                            if (Create_RSS_Feed(thisAggrCode.ToLower(), localWorkArea, aggregationObj.Name, aggregation_items, primaryUrl))
                            {
                                try
                                {
                                    // Copy the two generated RSS files over to the server
                                    File.Copy(Path.Combine(localWorkArea, thisAggrCode.ToLower() + "_rss.xml"), Path.Combine(Settings.Servers.Static_Pages_Location, "rss", thisAggrCode.ToLower() + "_rss.xml"), true);
                                    File.Copy(Path.Combine(localWorkArea, thisAggrCode.ToLower() + "_short_rss.xml"), Path.Combine(Settings.Servers.Static_Pages_Location, "rss", thisAggrCode.ToLower() + "_short_rss.xml"), true);

                                    // Delete the temporary files as well
                                    File.Delete(Path.Combine(localWorkArea, thisAggrCode.ToLower() + "_rss.xml"));
                                    File.Delete(Path.Combine(localWorkArea, thisAggrCode.ToLower() + "_short_rss.xml"));
                                }
                                catch (Exception ee)
                                {
                                    OnError("........Error in copying RSS feed for " + display_code + " to " + Settings.Servers.Static_Pages_Location + "\n" + ee.Message, UpdateID);
                                }
                            }
                        }
                        catch (Exception ee)
                        {
                            OnError("........Error in building RSS feed for " + display_code + "\n" + ee.Message, UpdateID);
                        }

                        OnProcess("........Building static HTML browse page of links for " + display_code, UpdateID);
                        try
                        {
                            string destinationFile = Path.Combine(localWorkArea, thisAggrCode.ToLower() + "_all.html");
                            if (Build_All_Browse(aggregationObj, aggregation_items, destinationFile, primaryUrl, UpdateID))
                            {
                                try
                                {
                                    File.Copy(destinationFile, Path.Combine(Settings.Servers.Static_Pages_Location, thisAggrCode.ToLower() + "_all.html"), true);
                                }
                                catch (Exception ee)
                                {
                                    OnError("........Error in copying HTML browse for " + display_code + " to " + Settings.Servers.Static_Pages_Location + "\n" + ee.Message, UpdateID);
                                }
                            }
                        }
                        catch (Exception ee)
                        {
                            OnError("........Error in building HTML browse for " + display_code + "\n" + ee.Message, UpdateID);
                        }


                    }
                }

                // Build the full instance-wide XML and RSS here as well
                Recreate_Library_XML_and_RSS(UpdateID, Settings, localWorkArea, primaryUrl);
            }
            catch (Exception ee)
            {
                OnError("Exception caught in BuildAggregationBrowsesModule", UpdateID);
                OnError(ee.Message, UpdateID);
                OnError(ee.StackTrace, UpdateID);
            }
        }


        /// <summary> Build the browse all static HTML file that includes links to all items in a particular collection </summary>
        /// <param name="Aggregation"> Aggregation object for which to build the browse ALL static html page </param>
        /// <param name="AllItems"> List of all items linked to that aggregation/collection </param>
        /// <param name="DestinationFile"> File to which to write this list of all browses </param>
        /// <param name="PrimaryUrl"> Primary URL for the SobekCM web instance </param>
        /// <param name="UpdateId"> Primary log entry primary key for this </param>
        /// <returns> TRUE if successful, otherwise FALSE </returns>
        private bool Build_All_Browse(Item_Aggregation Aggregation, DataSet AllItems, string DestinationFile, string PrimaryUrl, long UpdateId)
        {
            const string TOKEN = "<div id=\"empty\"></div>";

            // Aggregation certainly shouldn't be NULL, but let's check
            if (Aggregation == null)
                return false;

            // Try to get the collection empty page
            string aggregation_empty_page_source;
            try
            {
                using (WebClient client = new WebClient())
                {
                    string empty_page = PrimaryUrl + Aggregation.Code + "/empty";
                    aggregation_empty_page_source = client.DownloadString(empty_page);
                }
            }
            catch (Exception ee)
            {
                OnError("BuildAggregationBrowsesModule:Unable to pull the aggregation empty page for " + Aggregation.Code, UpdateId);
                return false;
            }

            // Ensure this has length and has the token to replace
            if ((String.IsNullOrEmpty(aggregation_empty_page_source)) || (aggregation_empty_page_source.IndexOf(TOKEN) < 0))
            {
                OnError("BuildAggregationBrowsesModule:Empty page content is invalid " + Aggregation.Code, UpdateId);
                return false;
            }

            // Build the list with links to each item
            StringBuilder builder = new StringBuilder(4000);
            if (AllItems.Tables[0].Rows.Count > 0)
            {
                foreach (DataRow thisRow in AllItems.Tables[0].Rows)
                {
                    // Determine the folder 
                    string bibid = thisRow["BibID"].ToString();
                    string vid = thisRow["VID"].ToString();

                    builder.AppendLine("<a href=\"" + PrimaryUrl + bibid + "/" + vid + "\">" + thisRow["Title"] + "</a><br />");
                }
            }
            else
            {
                builder.AppendLine("<h1>NO ITEMS IN AGGREGATION</h1>");
            }

            // Write this to the temporary page
            StreamWriter writer = new StreamWriter(DestinationFile, false);
            writer.WriteLine(aggregation_empty_page_source.Replace(TOKEN, builder.ToString()));
            writer.Flush();
            writer.Close();

            return true;
        }

        private void Recreate_Library_XML_and_RSS(long Builderid, InstanceWide_Settings Settings, string WorkSpaceDirectory, string PrimaryUrl)
        {
            // Update the RSS Feeds and Item Lists for ALL 
            // Build the simple XML result for this build
            OnProcess("........Building XML list for all digital resources", Builderid);
            try
            {
                DataSet simple_list = Engine_Database.Simple_Item_List(String.Empty, null);
                if (simple_list != null)
                {
                    try
                    {
                        string aggregation_list_file = Settings.Servers.Static_Pages_Location + "\\all.xml";
                        if (File.Exists(aggregation_list_file))
                            File.Delete(aggregation_list_file);
                        simple_list.WriteXml(aggregation_list_file, XmlWriteMode.WriteSchema);
                    }
                    catch (Exception ee)
                    {
                        OnError("........Error in building XML list for all digital resources on " + Settings.Servers.Static_Pages_Location + "\n" + ee.Message, Builderid);
                    }
                }
            }
            catch (Exception ee)
            {
                OnError("........Error in building XML list for all digital resources\n" + ee.Message, Builderid);
            }

            // Create the RSS feed for all ufdc items
            try
            {
                OnProcess("........Building RSS feed for all digital resources", Builderid);
                DataSet complete_list = Engine_Database.Simple_Item_List(String.Empty, null);

                Create_RSS_Feed("all", WorkSpaceDirectory, "All Items", complete_list, PrimaryUrl);
                try
                {
                    File.Copy(Path.Combine(WorkSpaceDirectory, "all_rss.xml"), Path.Combine(Settings.Servers.Static_Pages_Location, "rss", "all_rss.xml"), true);
                    File.Copy(Path.Combine(WorkSpaceDirectory, "all_short_rss.xml"), Path.Combine(Settings.Servers.Static_Pages_Location, "rss", "all_short_rss.xml"), true);
                }
                catch (Exception ee)
                {
                    OnError("........Error in copying RSS feed for all digital resources to " + Settings.Servers.Static_Pages_Location + "\n" + ee.Message, Builderid);
                }
            }
            catch (Exception ee)
            {
                OnError("........Error in building RSS feed for all digital resources\n" + ee.Message, Builderid);
            }
        }

        /// <summary> Create the RSS feed files necessary </summary>
        /// <param name="Collection_Code"> Aggregation Code for this collection </param>
        /// <param name="RssFeedLocation"> Location for the updated RSS feed to be updated </param>
        /// <param name="Collection_Title"> Title of this aggregation/collection </param>
        /// <param name="AllItems"> DataSet of all items within this aggregation </param>
        /// <param name="PrimaryUrl"> Primary URL for this web application </param>
        /// <returns> TRUE if successful, otherwise FALSE </returns>
        private bool Create_RSS_Feed(string Collection_Code, string RssFeedLocation, string Collection_Title, DataSet AllItems, string PrimaryUrl)
        {
            try
            {
                if (AllItems == null)
                    return false;

                int recordNumber = 0;
                int final_most_recent = 20;
                if (Collection_Code == "all")
                    final_most_recent = 100;

                DataView viewer = new DataView(AllItems.Tables[0]) { Sort = "CreateDate DESC" };

                StreamWriter rss_writer = new StreamWriter( Path.Combine(RssFeedLocation, Collection_Code + "_rss.xml"), false, Encoding.UTF8);
                rss_writer.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                rss_writer.WriteLine("<rss version=\"2.0\" xmlns:atom=\"http://www.w3.org/2005/Atom\">");
                rss_writer.WriteLine("<channel>");
                rss_writer.WriteLine("<title>" + Collection_Title.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;") + " (All Items) </title>");
                rss_writer.WriteLine("<link>" + PrimaryUrl + "data/rss/" + Collection_Code + "_rss.xml</link>");
                rss_writer.WriteLine("<atom:link href=\"" + PrimaryUrl + "rss/" + Collection_Code + "_rss.xml\" rel=\"self\" type=\"application/rss+xml\" />");
                rss_writer.WriteLine("<description></description>");
                rss_writer.WriteLine("<language>en</language>");
                rss_writer.WriteLine("<copyright>Copyright " + DateTime.Now.Year + "</copyright>");
                rss_writer.WriteLine("<lastBuildDate>" + DateTime_Helper.ToRfc822(DateTime.Now) + "</lastBuildDate>");

                rss_writer.WriteLine("");

                StreamWriter short_rss_writer = new StreamWriter(Path.Combine(RssFeedLocation, Collection_Code + "_short_rss.xml"), false, Encoding.UTF8);
                short_rss_writer.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                short_rss_writer.WriteLine("<rss version=\"2.0\" xmlns:atom=\"http://www.w3.org/2005/Atom\">");
                short_rss_writer.WriteLine("<channel>");
                short_rss_writer.WriteLine("<title>" + Collection_Title.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;") + " (Most Recent Items) </title>");
                short_rss_writer.WriteLine("<link>" + PrimaryUrl + "rss/" + Collection_Code + "_short_rss.xml</link>");
                short_rss_writer.WriteLine("<atom:link href=\"" + PrimaryUrl + "rss/" + Collection_Code + "_short_rss.xml\" rel=\"self\" type=\"application/rss+xml\" />");
                short_rss_writer.WriteLine("<description></description>");
                short_rss_writer.WriteLine("<language>en</language>");
                short_rss_writer.WriteLine("<copyright>Copyright " + DateTime.Now.Year + "</copyright>");
                short_rss_writer.WriteLine("<lastBuildDate>" + DateTime_Helper.ToRfc822(DateTime.Now) + "</lastBuildDate>");
                short_rss_writer.WriteLine("");

                foreach (DataRowView thisRowView in viewer)
                {
                    // Determine the folder name
                    string bibid = thisRowView.Row["BibID"].ToString();
                    string vid = thisRowView["VID"].ToString();

                    recordNumber++;
                    if (recordNumber <= final_most_recent)
                    {
                        short_rss_writer.WriteLine("<item>");
                        short_rss_writer.WriteLine("<title>" + thisRowView.Row["Title"].ToString().Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;") + "</title>");
                        short_rss_writer.WriteLine("<description></description>");

                        short_rss_writer.WriteLine("<link>" + PrimaryUrl + bibid + "/" + vid + "</link>");

                        string create_date_string = thisRowView.Row["CreateDate"].ToString();
                        DateTime dateTime;
                        if (DateTime.TryParse(create_date_string, out dateTime))
                        {
                            string formattedDate = DateTime_Helper.ToRfc822(dateTime);
                            short_rss_writer.WriteLine("<pubDate>" + formattedDate + " </pubDate>");
                        }

                        short_rss_writer.WriteLine("<guid>" + PrimaryUrl + bibid + "/" + vid + "</guid>");
                        short_rss_writer.WriteLine("</item>");
                        short_rss_writer.WriteLine("");
                    }

                    rss_writer.WriteLine("<item>");
                    rss_writer.WriteLine("<title>" + thisRowView.Row["Title"].ToString().Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;") + "</title>");
                    rss_writer.WriteLine("<description></description>");
                    rss_writer.WriteLine("<link>" + PrimaryUrl + bibid + "/" + vid + "</link>");

                    string create_date_string2 = thisRowView.Row["CreateDate"].ToString();
                    DateTime dateTime2;
                    if (DateTime.TryParse(create_date_string2, out dateTime2))
                    {
                        string formattedDate = DateTime_Helper.ToRfc822(dateTime2);
                        rss_writer.WriteLine("<pubDate>" + formattedDate + " </pubDate>");
                    }

                    rss_writer.WriteLine("<guid>" + PrimaryUrl + bibid + "/" + vid + "</guid>");
                    rss_writer.WriteLine("</item>");
                    rss_writer.WriteLine("");
                }

                rss_writer.WriteLine("</channel>");
                rss_writer.WriteLine("</rss>");
                rss_writer.Flush();
                rss_writer.Close();

                short_rss_writer.WriteLine("</channel>");
                short_rss_writer.WriteLine("</rss>");
                short_rss_writer.Flush();
                short_rss_writer.Close();

                return AllItems.Tables[0].Rows.Count > 0;
            }
            catch
            {
                // Console.WriteLine(@"ERROR BUILDING RSS FEED {0}", Collection_Code);
                return false;
            }
        }

        /// <summary> Builds all of the site map files which point to the static HTML pages </summary>
        /// <param name="DestinationPath"> Destination folder for all the generated site maps </param>
        /// <param name="PrimaryUrl"> Primary URL for this instance of SobekCM  </param>
        /// <returns> Number of site maps created ( Only 30,000 links are included in each site map ) </returns>
        public int Build_Site_Maps(string DestinationPath, string PrimaryUrl)
        {
            try
            {
                int site_map_index = 1;
                string site_map_file = "sitemap" + site_map_index + ".xml";
                int record_count = 0;

                StreamWriter writer = new StreamWriter( Path.Combine(DestinationPath, site_map_file), false);
                writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                writer.WriteLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
                DataSet item_list_table = Engine_Database.Simple_Item_List(String.Empty, null);
                foreach (DataRow thisRow in item_list_table.Tables[0].Rows)
                {
                    // Ready to start the next site map?
                    if (record_count > 30000)
                    {
                        writer.WriteLine("</urlset>");
                        writer.Flush();
                        writer.Close();

                        site_map_index++;
                        site_map_file = "sitemap" + site_map_index + ".xml";
                        writer = new StreamWriter(Path.Combine(DestinationPath, site_map_file), false);
                        record_count = 0;
                        writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                        writer.WriteLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
                    }

                    // Determine the folder 
                    string bibid = thisRow["BibID"].ToString();
                    string vid = thisRow["VID"].ToString();
                    DateTime? lastModifiedDate = null;
                    if (thisRow["LastSaved"] != DBNull.Value)
                    {
                        DateTime tryParseDate;
                        if (DateTime.TryParse(thisRow["LastSaved"].ToString(), out tryParseDate))
                            lastModifiedDate = tryParseDate;
                    }

                    writer.WriteLine("\t<url>");
                    writer.WriteLine("\t\t<loc>" + PrimaryUrl + bibid + "/" + vid + "</loc>");
                    if (lastModifiedDate.HasValue)
                    {
                        writer.WriteLine("\t\t<lastmod>" + lastModifiedDate.Value.Year + "-" + lastModifiedDate.Value.Month.ToString().PadLeft(2, '0') + "-" + lastModifiedDate.Value.Day.ToString().PadLeft(2, '0') + "</lastmod>");
                    }
                    writer.WriteLine("\t</url>");
                    record_count++;

                }

                writer.WriteLine("</urlset>");
                writer.Flush();
                writer.Close();
                return site_map_index;
            }
            catch
            {
                return -1;
            }
        }

        private void OnError(string Message, long UpdateID)
        {
            if (Error != null)
                Error(Message, UpdateID);
        }

        private void OnProcess(string Message, long UpdateID)
        {
            if (Process != null)
                Process(Message, UpdateID);
        }
    }
}
