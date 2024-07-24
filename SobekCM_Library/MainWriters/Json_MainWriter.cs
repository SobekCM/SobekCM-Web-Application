﻿#region Using directives

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SobekCM.Core.Client;
using SobekCM.Core.Navigation;
using SobekCM.Core.Results;
using SobekCM.Core.UI_Configuration;
using SobekCM.Core.UI_Configuration.StaticResources;
using SobekCM.Engine_Library.ApplicationState;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Library.UI;
using SobekCM.Resource_Object;
using SobekCM.Resource_Object.Divisions;
using SobekCM.Tools;

#endregion

namespace SobekCM.Library.MainWriters
{
    /// <summary> Main writer writes search results, item browses, and item information in Java Simple Object
    /// Notation for interfacing with the iPhone mobile applications </summary>
    /// <remarks> This class extends the abstract class <see cref="abstractMainWriter"/>. </remarks>
    public class Json_MainWriter : abstractMainWriter
    {
        private readonly string currentGreenstoneImageRoot = String.Empty;

        /// <summary> Constructor for a new instance of the Json_MainWriter class </summary>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        /// <param name="Current_Image_Root"> Current root directory to pull images and metadata for digital resources </param>
        public Json_MainWriter(RequestCache RequestSpecificValues, string Current_Image_Root) : base(RequestSpecificValues)        
        {
            currentGreenstoneImageRoot = Current_Image_Root;
        }

        /// <summary> Gets the enumeration of the type of main writer </summary>
        /// <value> This property always returns the enumerational value <see cref="Writer_Type_Enum.JSON"/>. </value>
        public override Writer_Type_Enum Writer_Type { get { return Writer_Type_Enum.JSON; } }

        /// <summary> Perform all the work of adding text directly to the response stream back to the web user </summary>
        /// <param name="Output"> Stream to which to write the text for this main writer </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        public override void Write_Html(TextWriter Output, Custom_Tracer Tracer)
        {
            switch (RequestSpecificValues.Current_Mode.Mode)
            {
                case Display_Mode_Enum.Results:
                case Display_Mode_Enum.Aggregation:
                    if (RequestSpecificValues.Paged_Results != null)
                        display_search_results(Output );
                    break;
                case Display_Mode_Enum.Item_Display:
                    display_item_info(Output);
                    break;
                default:
                    Output.Write("JSON Writer - Unknown Mode");
                    break;
            }
        }

        /// <summary> Writes the item information in JSON format directly to the output stream  </summary>
        /// <param name="Output"> Stream to which to write the JSON item information </param>
        protected internal void display_item_info(TextWriter Output)
        {
            SobekCM_Item currentItem = SobekEngineClient.Items.Get_Sobek_Item(RequestSpecificValues.Current_Mode.BibID, RequestSpecificValues.Current_Mode.VID, RequestSpecificValues.Tracer);
            // What if the page requested is greater than pages in the book?
            // What is the ID?
            // What if an item does not have jpeg's for each page?  No jpegs at all?
            Output.Write("[");
            if (currentItem != null)
            {
                if (RequestSpecificValues.Current_Mode.ViewerCode != "text")
                {
                    int currentPageIndex = RequestSpecificValues.Current_Mode.Page.HasValue ? RequestSpecificValues.Current_Mode.Page.Value : 1;
                    int first_page_to_show = (currentPageIndex - 1) * 20;
                    int last_page_to_show = (currentPageIndex * 20) - 1;
                    if (first_page_to_show < currentItem.Web.Static_PageCount)
                    {
                        int page = first_page_to_show;
                        string jpeg_to_view = String.Empty;
                        while ((page < currentItem.Web.Static_PageCount) && (page <= last_page_to_show))
                        {
                            Page_TreeNode thisPage = currentItem.Web.Pages_By_Sequence[page];
                            bool found = false;
                            foreach (SobekCM_File_Info thisFile in thisPage.Files.Where(ThisFile => ThisFile.System_Name.ToUpper().IndexOf(".JPG") > 0))
                            {
                                jpeg_to_view = currentGreenstoneImageRoot + currentItem.Web.AssocFilePath + "/" + thisFile.System_Name;
                                found = true;
                                break;
                            }
                            if (found)
                            {
                                if (page > first_page_to_show)
                                    Output.Write(",");
                                jpeg_to_view = jpeg_to_view.Replace("\\", "/").Replace("//", "/").Replace("http:/", "http://");
                                Output.Write("{\"item_page\":{\"position\":" + (page + 1) + ",\"image_url\":\"" + jpeg_to_view + "\",\"id\":" + (page + 1) + ",\"collection_item_id\":1}}");
                            }
                            page++;
                        }
                    }
                }
                else
                {
                    // Get the list of all TEXT files
                    List<string> existing_text_files = new List<string>();
                    if (Directory.Exists(UI_ApplicationCache_Gateway.Settings.Servers.Image_Server_Network + currentItem.Web.AssocFilePath))
                    {
                        string[] allFiles = Directory.GetFiles(UI_ApplicationCache_Gateway.Settings.Servers.Image_Server_Network + currentItem.Web.AssocFilePath, "*.txt");
                        existing_text_files.AddRange(allFiles.Select(ThisFile => (new FileInfo(ThisFile)).Name.ToUpper()));
                    }


                    int page = 0;
                    string jpeg_to_view = String.Empty;
                    while (page < currentItem.Web.Static_PageCount)
                    {
                        string text_to_read = String.Empty;
                        Page_TreeNode thisPage = currentItem.Web.Pages_By_Sequence[page];
                        bool found = false;
                        foreach (SobekCM_File_Info thisFile in thisPage.Files)
                        {
                            if (thisFile.System_Name.ToUpper().IndexOf(".JPG") > 0)
                            {
                                if (existing_text_files.Contains(thisFile.System_Name.ToUpper().Replace(".JPG", "") + ".TXT"))
                                {
                                    text_to_read = currentGreenstoneImageRoot + currentItem.Web.AssocFilePath + "/" + thisFile.System_Name.Replace(".JPG", ".TXT").Replace(".jpg", ".txt");
                                }
                                jpeg_to_view = currentGreenstoneImageRoot + currentItem.Web.AssocFilePath + "/" + thisFile.System_Name;
                                found = true;
                                break;
                            }
                        }
                        if (found)
                        {
                            if (page > 0)
                                Output.Write(",");
                            jpeg_to_view = jpeg_to_view.Replace("\\", "/").Replace("//", "/").Replace("http:/", "http://");
                            text_to_read = text_to_read.Replace("\\", "/").Replace("//", "/").Replace("http:/", "http://");

                            Output.Write("{\"item_page\":{\"position\":" + (page + 1) + ",\"image_url\":\"" + jpeg_to_view + "\",\"text_url\":\"" + text_to_read + "\"}}");
                        }
                        page++;
                    }
                }
            }

            Output.Write("]");
        }

        /// <summary> Writes the search or browse information in JSON format directly to the output stream  </summary>
        /// <param name="Output"> Stream to which to write the JSON search or browse information </param>
        protected internal void display_search_results(TextWriter Output )
        {
            // If results are null, or no results, return empty string
            if ((RequestSpecificValues.Paged_Results == null) || (RequestSpecificValues.Results_Statistics == null) || (RequestSpecificValues.Results_Statistics.Total_Items <= 0))
                return;

            Output.Write("[");

            // Step through all the results
            int i = 1;
            foreach (iSearch_Title_Result titleResult in RequestSpecificValues.Paged_Results)
            {
                // Always get the first item for things like the main link and thumbnail
                iSearch_Item_Result firstItemResult = titleResult.Get_Item(0);

                // Determine a thumbnail
                string thumb = currentGreenstoneImageRoot + titleResult.BibID.Substring(0,2) + "/" + titleResult.BibID.Substring(2,2) + "/" + titleResult.BibID.Substring(4,2) + "/" + titleResult.BibID.Substring(6,2) + "/" + titleResult.BibID.Substring(8) + "/" + firstItemResult.VID + "/" + firstItemResult.MainThumbnail;
                if ((thumb.ToUpper().IndexOf(".JPG") < 0) && (thumb.ToUpper().IndexOf(".GIF") < 0))
                {
                    thumb = Static_Resources_Gateway.Nothumb_Jpg;
                }
                thumb = thumb.Replace("\\", "/").Replace("//", "/").Replace("http:/", "http://");

                // Was a previous item/title included here?
                if (i > 1)
                    Output.Write(",");
                Output.Write("{\"collection_item\":{\"name\":\"" + firstItemResult.Title.Trim().Replace("\"", "'") + "\",\"url\":\"" + UI_ApplicationCache_Gateway.Settings.Servers.System_Base_URL + titleResult.BibID + "/" + firstItemResult.VID + "\",\"collection_code\":\"\",\"id\":\"" + titleResult.BibID + "_" + firstItemResult.VID + "\",\"thumb_url\":\"" + thumb + "\"}}");

                i++;
            }

            Output.Write("]");
        }
    }
}
