using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Caching;
using System.Web.ModelBinding;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Win32;
using SobekCM.Core.Aggregations;
using SobekCM.Core.Search;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Engine_Library.Database;
using SobekCM.Library.HTML;
using SobekCM.Library.UI;
using SobekCM.Resource_Object;
using SobekCM.Tools;

namespace SobekCM.Library.AggregationViewer.Viewers
{
    public class Tiles_Home_Single_Tile
    {
        public string JpegUri { get; set; }

        public string LinkUri { get; set; }

        public string BibID { get; set; }

        public string VID { get; set; }
    }

    public class Tiles_Home_AggregationViewer : abstractAggregationViewer
    {
        protected List<Tiles_Home_Single_Tile> selectedTiles = new List<Tiles_Home_Single_Tile>();
        protected Database_Results_Info tileMetadata;

        public Tiles_Home_AggregationViewer(RequestCache RequestSpecificValues, AggregationViewBag ViewBag) : base(RequestSpecificValues, ViewBag)
        {
            // Get the list of tiles
            string aggregation_tile_directory = Path.Combine(UI_ApplicationCache_Gateway.Settings.Servers.Base_Design_Location, ViewBag.Hierarchy_Object.ObjDirectory, "images", "tiles");
            string[] jpeg_tiles = Directory.GetFiles(aggregation_tile_directory, "*.jpg");

            // Compute the URL for these images
            string aggregation_tile_uri = RequestSpecificValues.Current_Mode.Base_URL + "design/aggregations/" + ViewBag.Hierarchy_Object.Code + "/images/tiles/";

            // Get the list of all potential tiles, by checking name
            List<Tiles_Home_Single_Tile> allTiles = new List<Tiles_Home_Single_Tile>();
            string bibid = null;
            string vid = null;
            foreach (string thisJpegTile in jpeg_tiles)
            {
                // Get the filename
                string thisFileName = Path.GetFileName(thisJpegTile);
                string thisFileNameSansExtension = Path.GetFileNameWithoutExtension(thisJpegTile);


                // Check for a link to the bibid/vid
                string bib_vid_for_link = String.Empty;

                if ((thisFileNameSansExtension.Length == 10) && ( SobekCM_Item.is_bibid_format(thisFileNameSansExtension)))
                {
                    bib_vid_for_link = thisFileNameSansExtension;
                    bibid = bib_vid_for_link;
                    vid = "00001";
                }
                else if ((thisFileNameSansExtension.Length == 15) && (SobekCM_Item.is_bibid_format(thisFileNameSansExtension.Substring(0, 10))) && (SobekCM_Item.is_vids_format(thisFileNameSansExtension.Substring(10))))
                {
                    bib_vid_for_link = thisFileNameSansExtension.Substring(0, 10) + "/" + thisFileNameSansExtension.Substring(10);
                    bibid = thisFileNameSansExtension.Substring(0, 10);
                    vid = thisFileNameSansExtension.Substring(10);
                }
                else if ((thisFileNameSansExtension.Length == 16) && (SobekCM_Item.is_bibid_format(thisFileNameSansExtension.Substring(0, 10))) && (SobekCM_Item.is_vids_format(thisFileNameSansExtension.Substring(11))))
                {
                    bib_vid_for_link = thisFileNameSansExtension.Substring(0, 10) + "/" + thisFileNameSansExtension.Substring(11);
                    bibid = thisFileNameSansExtension.Substring(0, 10);
                    vid = thisFileNameSansExtension.Substring(11);
                }

                // If there was a link calculated, then use this jpeg
                if (!String.IsNullOrEmpty(bib_vid_for_link))
                {
                    allTiles.Add(new Tiles_Home_Single_Tile
                    {
                        JpegUri = aggregation_tile_uri + thisFileName, 
                        LinkUri = RequestSpecificValues.Current_Mode.Base_URL + bib_vid_for_link,
                        BibID = bibid ?? String.Empty,
                        VID = vid ?? String.Empty
                    });
                }
            }

            // Is the metadata cached?
            string cache_key = ViewBag.Hierarchy_Object.Code + ":TILE METADATA";
            tileMetadata = HttpContext.Current.Cache.Get(cache_key) as Database_Results_Info;
            if (tileMetadata == null)
            {
                // Look for the metadat file
                string metadata_file = Path.Combine(aggregation_tile_directory, "tile_metadata.xml");

                // If no file, try to create it
                if (!File.Exists(metadata_file))
                {
                    // Step through each image, collecting the bibs and vids
                    List<string> bibs = new List<string>();
                    List<string> vids = new List<string>();
                    foreach (Tiles_Home_Single_Tile thisTile in allTiles)
                    {
                        bibs.Add(thisTile.BibID);
                        vids.Add(thisTile.VID);
                    }

                    // Now, look up each of these bib/vids

                    // First, pad the lists correctly
                    while (bibs.Count % 10 != 0)
                    {
                        bibs.Add(String.Empty);
                        vids.Add(String.Empty);
                    }

                    // Get the aggregation code
                    string aggrCode = ViewBag.Hierarchy_Object.Code;

                    // FOR TESTING DON'T USE COLLECTION CODE
                    aggrCode = "";

                    // Start the results object
                    Database_Results_Info allResults = new Database_Results_Info();

                    // Now, get the results
                    int offset = 0;
                    while (offset < bibs.Count)
                    {
                        //Get the results from the database
                        Database_Results_Info results = Engine_Library.Database.Engine_Database.Metadata_By_Bib_Vid(aggrCode, bibs[offset + 0], vids[offset + 0], bibs[offset + 1], vids[offset + 1],
                            bibs[offset + 2], vids[offset + 2], bibs[offset + 3], vids[offset + 3],
                            bibs[offset + 4], vids[offset + 4], bibs[offset + 5], vids[offset + 5],
                            bibs[offset + 6], vids[offset + 6], bibs[offset + 7], vids[offset + 7],
                            bibs[offset + 8], vids[offset + 8], bibs[offset + 9], vids[offset + 9], null);

                        // Combine into full results
                        if ((results != null) && (results.Metadata_Labels != null) && (results.Metadata_Labels.Count > 0))
                            allResults.Metadata_Labels = results.Metadata_Labels;

                        if ((results != null) && (results.Results != null) && (results.Results.Count > 0))
                            allResults.Results.AddRange(results.Results);

                        offset += 10;
                    }


                    // Save these results
                    StreamWriter writer = new StreamWriter(metadata_file, false);

                    XmlSerializer x = new XmlSerializer(allResults.GetType());
                    x.Serialize(writer, allResults);

                    writer.Close();
                }


                // Check again, and let's read the metadata
                if (File.Exists(metadata_file))
                {
                    try
                    {
                        FileStream fs = new FileStream(metadata_file, FileMode.Open);
                        XmlReader reader = XmlReader.Create(fs);

                        XmlSerializer x = new XmlSerializer(typeof (Database_Results_Info));

                        // Use the Deserialize method to restore the object's state.
                        tileMetadata = (Database_Results_Info) x.Deserialize(reader);
                        fs.Close();

                        // Also put this in the cache
                        HttpContext.Current.Cache.Insert(cache_key, tileMetadata, null, Cache.NoAbsoluteExpiration, TimeSpan.FromMinutes(5));
                    }
                    catch (Exception ee)
                    {
                        tileMetadata = null;
                    }
                }
            }

            // If less than or equal to fifteen, copy over as is.. otherwise, choose random ones
            if (allTiles.Count <= 15)
                selectedTiles.AddRange(allTiles);
            else
            {
                Random randomGen = new Random();
                while (selectedTiles.Count < 15)
                {
                    int random_index = randomGen.Next(0, allTiles.Count);
                    selectedTiles.Add(allTiles[random_index]);
                    allTiles.RemoveAt(random_index);
                }
            }
        }

        /// <summary> Gets the type of collection view or search supported by this collection viewer </summary>
        /// <value> This returns the <see cref="Item_Aggregation_Views_Searches_Enum.Custom_Home_Page"/> enumerational value </value>
        public override Item_Aggregation_Views_Searches_Enum Type
        {
            get { return Item_Aggregation_Views_Searches_Enum.Custom_Home_Page; }
        }

        /// <summary> Gets the collection of special behaviors which this aggregation viewer  requests from the main HTML subwriter. </summary>
        public override List<HtmlSubwriter_Behaviors_Enum> AggregationViewer_Behaviors
        {
            get
            {
                return new List<HtmlSubwriter_Behaviors_Enum>
                        {
                            HtmlSubwriter_Behaviors_Enum.Aggregation_Suppress_Home_Text,
                            HtmlSubwriter_Behaviors_Enum.Suppress_SearchForm,
                            HtmlSubwriter_Behaviors_Enum.Use_Jquery_Qtip
                        };
            }
        }

        /// <summary> Add the HTML to be displayed in the search box </summary>
        /// <param name="Output"> Textwriter to write the HTML for this viewer</param>
        /// <param name="Tracer">Trace object keeps a list of each method executed and important milestones in rendering</param>
        public override void Add_Search_Box_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            // do nothing
        }

        /// <summary> Add the HTML to be displayed below the search box </summary>
        /// <param name="Output"> Textwriter to write the HTML for this viewer</param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
        /// <remarks> This adds the search tips by calling the base method <see cref="abstractAggregationViewer.Add_Simple_Search_Tips"/> </remarks>
        public override void Add_Secondary_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("Tiles_Home_AggregationViewer.Add_Secondary_HTML", "Add the entire tiled home page");
            }

            const string VARIES_STRING = "<span style=\"color:Gray\">( varies )</span>";

            Output.WriteLine("<div id=\"sbkThav_TileContainer\">");

            //Add the necessary JavaScript, CSS files
            Output.WriteLine("  <script type=\"text/javascript\" src=\"" + Static_Resources_Gateway.Sobekcm_Thumb_Results_Js + "\"></script>");

            int title_count = 1;
            foreach (Tiles_Home_Single_Tile thisTile in selectedTiles)
            {
                // Do we have metadat for this?
                Database_Item_Result itemResult = null;
                Database_Title_Result titleResult = null;
                if (tileMetadata != null)
                {
                    // Find the matching tile, by BibID
                    foreach (Database_Title_Result metadataTitle in tileMetadata.Results)
                    {
                        // Matching BibID?
                        if (String.Compare(metadataTitle.BibID, thisTile.BibID, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            // Only continue, if we have items
                            if (metadataTitle.Items != null)
                            {
                                // Title matched
                                titleResult = metadataTitle;

                                // Look for matching VID
                                foreach (Database_Item_Result metadataItem in metadataTitle.Items)
                                {
                                    // Matching VID?
                                    if (String.Compare(metadataItem.VID, thisTile.VID, StringComparison.OrdinalIgnoreCase) == 0)
                                    {
                                        itemResult = metadataItem;
                                        break;
                                    }
                                }

                                // If no match by VID, use whatever is in there
                                if (itemResult == null)
                                    itemResult = titleResult.Items[0];
                            }
                        }
                    }
                }

                // Write the tile
                Output.WriteLine("  <div class=\"sbkThav_Tile\">");
                Output.WriteLine("    <a href=\"" + thisTile.LinkUri + "\">");
                Output.WriteLine("      <img id=\"sbkThumbnailImg" + title_count + "\" src=\"" + thisTile.JpegUri + "\" />");
                Output.WriteLine("    </a>");

                // Was metadta found?
                if ((titleResult != null) && (itemResult != null))
                {

                    //Add the hidden item values for display in the tooltip
                 //   Output.WriteLine("<tr style=\"display:none;\"><td colspan=\"100%\"><div  id=\"descThumbnail" + title_count + "\" >");

                    Output.WriteLine("<div  id=\"descThumbnail" + title_count + "\" style=\"display:none;\" >");
                    // Add each element to this table
                    Output.WriteLine("\t\t\t<table cellspacing=\"0px\">");

                        Output.WriteLine(
                            "\t\t\t\t<tr style=\"height:40px;\" valign=\"middle\"><td colspan=\"3\"><span class=\"qtip_BriefTitle\" style=\"color: #a5a5a5;font-weight: bold;font-size:13px;\">" + itemResult.Title.Replace("<", "&lt;").Replace(">", "&gt;") +
                            "</span> &nbsp; </td></tr><br/>");
                        Output.WriteLine("<tr><td colspan=\"100%\"><br/></td></tr>");


                    if ((!String.IsNullOrEmpty(titleResult.Primary_Identifier_Type)) && (!String.IsNullOrEmpty(titleResult.Primary_Identifier)))
                    {
                        Output.WriteLine("\t\t\t\t<tr><td>" + UI_ApplicationCache_Gateway.Translation.Get_Translation(titleResult.Primary_Identifier_Type, RequestSpecificValues.Current_Mode.Language) + ":</td><td>&nbsp;</td><td>" + HttpUtility.HtmlDecode(titleResult.Primary_Identifier) + "</td></tr>");
                    }

                    if ((RequestSpecificValues.Current_User != null) && (RequestSpecificValues.Current_User.LoggedOn) && (RequestSpecificValues.Current_User.Is_Internal_User))
                    {
                        Output.WriteLine("\t\t\t\t<tr><td>BibID:</td><td>&nbsp;</td><td>" + titleResult.BibID + "</td></tr>");

                        if (titleResult.OPAC_Number > 1)
                        {
                            Output.WriteLine("\t\t\t\t<tr><td>OPAC:</td><td>&nbsp;</td><td>" + titleResult.OPAC_Number + "</td></tr>");
                        }

                        if (titleResult.OCLC_Number > 1)
                        {
                            Output.WriteLine("\t\t\t\t<tr><td>OCLC:</td><td>&nbsp;</td><td>" + titleResult.OCLC_Number + "</td></tr>");
                        }
                    }

                    for (int i = 0; i < tileMetadata.Metadata_Labels.Count; i++)
                    {
                        string field = tileMetadata.Metadata_Labels[i];

                        // Somehow the metadata for this item did not fully save in the database.  Break out, rather than
                        // throw the exception
                        if ((titleResult.Metadata_Display_Values == null) || (titleResult.Metadata_Display_Values.Length <= i))
                            break;

                        string value = titleResult.Metadata_Display_Values[i];
                        Metadata_Search_Field thisField = UI_ApplicationCache_Gateway.Settings.Metadata_Search_Field_By_Name(field);
                        string display_field = string.Empty;
                        if (thisField != null)
                            display_field = thisField.Display_Term;
                        if (display_field.Length == 0)
                            display_field = field.Replace("_", " ");

                        if (value == "*")
                        {
                            Output.WriteLine("\t\t\t\t<tr><td>" + UI_ApplicationCache_Gateway.Translation.Get_Translation(display_field, RequestSpecificValues.Current_Mode.Language) + ":</td><td>&nbsp;</td><td>" + HttpUtility.HtmlDecode(VARIES_STRING) + "</td></tr>");
                        }
                        else if (value.Trim().Length > 0)
                        {
                            if (value.IndexOf("|") > 0)
                            {
                                bool value_found = false;
                                string[] value_split = value.Split("|".ToCharArray());

                                foreach (string thisValue in value_split)
                                {
                                    if (thisValue.Trim().Trim().Length > 0)
                                    {
                                        if (!value_found)
                                        {
                                            Output.WriteLine("\t\t\t\t<tr valign=\"top\"><td>" + UI_ApplicationCache_Gateway.Translation.Get_Translation(display_field, RequestSpecificValues.Current_Mode.Language) + ":</td><td>&nbsp;</td><td>");
                                            value_found = true;
                                        }
                                        Output.Write(HttpUtility.HtmlDecode(thisValue) + "<br />");
                                    }
                                }

                                if (value_found)
                                {
                                    Output.WriteLine("</td></tr>");
                                }
                            }
                            else
                            {
                                Output.WriteLine("\t\t\t\t<tr><td>" + UI_ApplicationCache_Gateway.Translation.Get_Translation(display_field, RequestSpecificValues.Current_Mode.Language) + ":</td><td>&nbsp;</td><td>" + HttpUtility.HtmlDecode(value) + "</td></tr>");
                            }
                        }
                    }

                    Output.WriteLine("\t\t\t</table>");
                    Output.WriteLine("  </div>");

                }
                Output.WriteLine("  </div>");



                title_count++;
            }

            Output.WriteLine("</div>");

            // If there are sub aggregations here, show them
            if (ViewBag.Hierarchy_Object.Children_Count > 0)
            {
                Output.WriteLine("<div class=\"SobekText\">");
                Aggregation_HtmlSubwriter.Add_SubCollection_Buttons(Output, RequestSpecificValues, ViewBag.Hierarchy_Object);
                Output.WriteLine("</div>");
            }
            RequestSpecificValues.Current_Mode.Aggregation = ViewBag.Hierarchy_Object.Code;

        }
    }
}
