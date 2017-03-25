#region Using directives

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.UI.WebControls;
using SobekCM.Core.ApplicationState;
using SobekCM.Core.BriefItem;
using SobekCM.Core.Client;
using SobekCM.Core.Configuration.Localization;
using SobekCM.Core.Items;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.Navigation;
using SobekCM.Core.UI_Configuration;
using SobekCM.Core.UI_Configuration.StaticResources;
using SobekCM.Core.UI_Configuration.Viewers;
using SobekCM.Core.Users;
using SobekCM.Engine_Library.ApplicationState;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Engine_Library.Database;
using SobekCM.Library.Database;
using SobekCM.Library.Email;
using SobekCM.Library.HtmlLayout;
using SobekCM.Library.ItemViewer;
using SobekCM.Library.ItemViewer.HtmlHeadWriters;
using SobekCM.Library.ItemViewer.HtmlSectionWriters;
using SobekCM.Library.ItemViewer.Menu;
using SobekCM.Library.ItemViewer.Viewers;
using SobekCM.Library.UI;
using SobekCM.Resource_Object.Behaviors;
using SobekCM.Tools;
using SobekCM_Resource_Database;

#endregion

namespace SobekCM.Library.HTML
{
    /// <summary> Item html subwriter renders views on a single digital resource </summary>
    /// <remarks> This class extends the <see cref="abstractHtmlSubwriter"/> abstract class. </remarks>
    public class Item_HtmlSubwriter : abstractHtmlSubwriter
    {
        #region Private class members 
        
        private readonly int searchResultsCount;
        private readonly bool showZoomable;
        private bool tocSelectedComplete;
        private TreeView treeView1;
        private readonly bool userCanEditItem;
        private readonly List<HtmlSubwriter_Behaviors_Enum> behaviors;
        private string buttonsHtml;
        private string pageLinksHtml;
        private readonly string restriction_message;

        private readonly bool is_ead;
        private readonly bool is_bib_level;
        private readonly bool is_tei;

        private BriefItemInfo currentItem;
        private SobekCM_Items_In_Title itemsInTitle;

        private iItemViewerPrototyper prototyper;
        private iItemViewer pageViewer;
        private List<HtmlSubwriter_Behaviors_Enum> pageViewerBehaviors;

        private HtmlLayoutInfo itemLayout;
        private int itemLayoutIndex;
        private ItemWriterLayoutConfig itemLayoutConfig;

        #endregion

        #region Constructor(s)

        /// <summary> Constructor for a new instance of the Item_HtmlSubwriter class </summary>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        public Item_HtmlSubwriter( RequestCache RequestSpecificValues) : base(RequestSpecificValues)
        {

            // Add the trace 
            if (RequestSpecificValues.Tracer != null)
                RequestSpecificValues.Tracer.Add_Trace("Item_HtmlSubwriter.Constructor");

            showZoomable = (String.IsNullOrEmpty(UI_ApplicationCache_Gateway.Settings.Servers.JP2ServerUrl));

            searchResultsCount = 0;

            // Try to get the current item
            RequestSpecificValues.Tracer.Add_Trace("Item_HtmlSubwriter.Constructor", "Get the item information from the engine");
            int status_code = 0;
            try
            {
                currentItem = SobekEngineClient.Items.Get_Item_Brief(RequestSpecificValues.Current_Mode.BibID, RequestSpecificValues.Current_Mode.VID, true, RequestSpecificValues.Tracer, out status_code);
            }
            catch (Exception ee)
            {
                string ee_message = ee.Message;
                if (ee_message.IndexOf("404") == 0)
                {
                    string base_source = Engine_ApplicationCache_Gateway.Settings.Servers.Base_Directory + "design\\webcontent";

                    // Set the source location
                    RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Simple_HTML_CMS;
                    RequestSpecificValues.Current_Mode.Missing = true;
                    RequestSpecificValues.Current_Mode.Info_Browse_Mode = RequestSpecificValues.Current_Mode.BibID;
                    RequestSpecificValues.Current_Mode.WebContent_Type = WebContent_Type_Enum.Display;
                    RequestSpecificValues.Current_Mode.Page_By_FileName = base_source + "\\missing.html";
                    RequestSpecificValues.Current_Mode.WebContentID = -1;
                    return;
                }

                if (ee_message.IndexOf("303") == 0)
                {
                    string vid = ee_message.Substring(6, 5);
                    RequestSpecificValues.Current_Mode.VID = vid;

                    UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode);
                    return;
                }

                RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Error;
                RequestSpecificValues.Current_Mode.Error_Message = ee_message;
                return;
            }

            // Finally, if no error but it is NULL, return
            if (currentItem == null)
            {
                string base_source = Engine_ApplicationCache_Gateway.Settings.Servers.Base_Directory + "design\\webcontent";

                // Set the source location
                RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Simple_HTML_CMS;
                RequestSpecificValues.Current_Mode.Missing = true;
                RequestSpecificValues.Current_Mode.Info_Browse_Mode = RequestSpecificValues.Current_Mode.BibID;
                RequestSpecificValues.Current_Mode.WebContent_Type = WebContent_Type_Enum.Display;
                RequestSpecificValues.Current_Mode.Page_By_FileName = base_source + "\\missing.html";
                RequestSpecificValues.Current_Mode.WebContentID = -1;
                return;
            }


            // If this is an empty item, than an error occurred
            if (String.IsNullOrEmpty(currentItem.BibID))
            {
                currentItem.Title = "ERROR READING METADATA FILE";
                currentItem.BibID = RequestSpecificValues.Current_Mode.BibID;
                currentItem.VID = RequestSpecificValues.Current_Mode.VID;
            }

            RequestSpecificValues.Current_Mode.VID = currentItem.VID;

            // Ensure the UI portion has been configured for this user interface
            ItemViewer_Factory.Configure_Brief_Item_Viewers(currentItem);

            // Set some flags based on the resource type
            is_bib_level = (String.Compare(currentItem.VID, "00000", StringComparison.OrdinalIgnoreCase) == 0);
            is_ead = (String.Compare(currentItem.Type, "EAD", StringComparison.OrdinalIgnoreCase) == 0);

            // Look for TEI-type item
            is_tei = false;
            if ((UI_ApplicationCache_Gateway.Configuration.Extensions != null) &&
                (UI_ApplicationCache_Gateway.Configuration.Extensions.Get_Extension("TEI") != null) &&
                (UI_ApplicationCache_Gateway.Configuration.Extensions.Get_Extension("TEI").Enabled))
            {
                string tei_file = currentItem.Behaviors.Get_Setting("TEI.Source_File");
                string xslt_file = currentItem.Behaviors.Get_Setting("TEI.XSLT");
                if ((tei_file != null) && (xslt_file != null))
                {
                    is_tei = true;
                }
            }

            // Determine if this user can edit this item
            userCanEditItem = false;
            if (RequestSpecificValues.Current_User != null)
            {
                userCanEditItem = RequestSpecificValues.Current_User.Can_Edit_This_Item(currentItem.BibID, currentItem.Type, currentItem.Behaviors.Source_Institution_Aggregation, currentItem.Behaviors.Holding_Location_Aggregation, currentItem.Behaviors.Aggregation_Code_List );
            }

            // Check that this item is not checked out by another user
            RequestSpecificValues.Flags.ItemCheckedOutByOtherUser = false;
            if (currentItem.Behaviors.Single_Use)
            {
                if (!Engine_ApplicationCache_Gateway.Checked_List.Check_Out(currentItem.Web.ItemID, HttpContext.Current.Request.UserHostAddress))
                {
                    RequestSpecificValues.Flags.ItemCheckedOutByOtherUser = true;
                }
            }

            // Check to see if this is IP restricted
            restriction_message = String.Empty;
            if (currentItem.Behaviors.IP_Restriction_Membership > 0)
            {
                if (HttpContext.Current != null)
                {
                    int user_mask = (int)HttpContext.Current.Session["IP_Range_Membership"];
                    int comparison = currentItem.Behaviors.IP_Restriction_Membership & user_mask;
                    if (comparison == 0)
                    {
                        RequestSpecificValues.Flags.ItemRestrictedFromUserByIp = true;

                        int restriction = currentItem.Behaviors.IP_Restriction_Membership;
                        int restriction_counter = 1;
                        while (restriction % 2 != 1)
                        {
                            restriction = restriction >> 1;
                            restriction_counter++;
                        }
                        if (Engine_ApplicationCache_Gateway.IP_Restrictions[restriction_counter] != null)
                            restriction_message = Engine_ApplicationCache_Gateway.IP_Restrictions[restriction_counter].Item_Restricted_Statement;
                        else
                            restriction_message = "Restricted Item";
                    }
                }
            }

            // If this item is restricted by IP than alot of the upcoming code is unnecessary
            if ((RequestSpecificValues.Current_User != null) && ((!RequestSpecificValues.Flags.ItemRestrictedFromUserByIp) || (userCanEditItem) || (RequestSpecificValues.Current_User.Is_Internal_User)))
            {
                #region Region suppressed currently - was for adding feature to a map image?

                //// Searching for EAD/EAC type items is different from others
                //if (!isEadTypeItem)
                //{
                //    // If there is a coordinate search, and polygons, do that
                //    // GEt the geospatial metadata module
                //    GeoSpatial_Information geoInfo = RequestSpecificValues.Current_Item.Get_Metadata_Module(GlobalVar.GEOSPATIAL_METADATA_MODULE_KEY) as GeoSpatial_Information;
                //    if ((geoInfo != null) && (geoInfo.hasData))
                //    {
                //        if ((currentMode.Coordinates.Length > 0) && (geoInfo.Polygon_Count > 1))
                //        {
                //            // Determine the coordinates in this search
                //            string[] splitter = currentMode.Coordinates.Split(",".ToCharArray());

                //            if (((splitter.Length > 1) && (splitter.Length < 4)) || ((splitter.Length == 4) && (splitter[2].Length == 0) && (splitter[3].Length == 0)))
                //            {
                //                Double.TryParse(splitter[0], out providedMaxLat);
                //                Double.TryParse(splitter[1], out providedMaxLong);
                //                providedMinLat = providedMaxLat;
                //                providedMinLong = providedMaxLong;
                //            }
                //            else if (splitter.Length >= 4)
                //            {
                //                Double.TryParse(splitter[0], out providedMaxLat);
                //                Double.TryParse(splitter[1], out providedMaxLong);
                //                Double.TryParse(splitter[2], out providedMinLat);
                //                Double.TryParse(splitter[3], out providedMinLong);
                //            }


                //            // Now, if there is length, determine the count of results
                //            searchResultsString = new List<string>();
                //            if (searchResultsString.Count > 0)
                //            {
                //                searchResultsCount = searchResultsString.Count;

                //                // Also, look to see where the current point lies in the matching, current polygon
                //                if ((providedMaxLong == providedMinLong) && (providedMaxLat == providedMinLat))
                //                {
                //                    foreach (Coordinate_Polygon itemPolygon in geoInfo.Polygons)
                //                    {
                //                        // Is this the current page?
                //                        if (itemPolygon.Page_Sequence == currentMode.Page)
                //                        {
                //                            if (itemPolygon.is_In_Bounding_Box(providedMaxLat, providedMaxLong))
                //                            {
                //                                searchMatchOnThisPage = true;
                //                                ReadOnlyCollection<Coordinate_Point> boundingBox = itemPolygon.Bounding_Box;
                //                                featureYRatioLocation = Math.Abs(((providedMaxLat - boundingBox[0].Latitude)/(boundingBox[0].Latitude - boundingBox[1].Latitude)));
                //                                featureXRatioLocation = Math.Abs(((providedMaxLong - boundingBox[0].Longitude)/(boundingBox[0].Longitude - boundingBox[1].Longitude)));
                //                            }
                //                        }
                //                    }
                //                }
                //            }
                //        }
                //    }
                //}

                #endregion

                // Is this a postback?
                if (RequestSpecificValues.Current_Mode.isPostBack)
                {
                    // Handle any actions from standard user action (i.e., email, add to bookshelf, etc. )
                    if (HttpContext.Current.Request.Form["item_action"] != null)
                    {
                        string action = HttpContext.Current.Request.Form["item_action"].ToLower().Trim();

                        if (action == "email")
                        {
                            string address = HttpContext.Current.Request.Form["email_address"].Replace(";", ",").Trim();
                            string comments = HttpContext.Current.Request.Form["email_comments"].Trim();
                            string format = HttpContext.Current.Request.Form["email_format"].Trim().ToUpper();
                            if (address.Length > 0)
                            {
                                // Determine the email format
                                bool is_html_format = format != "TEXT";

                                // CC: the user, unless they are already on the list
                                string cc_list = RequestSpecificValues.Current_User.Email;
                                if (address.ToUpper().IndexOf(RequestSpecificValues.Current_User.Email.ToUpper()) >= 0)
                                    cc_list = String.Empty;

                                // Send the email
                                HttpContext.Current.Session.Add("ON_LOAD_MESSAGE", !Item_Email_Helper.Send_Email(address, cc_list, comments, RequestSpecificValues.Current_User.Full_Name, RequestSpecificValues.Current_Mode.Instance_Abbreviation, currentItem, is_html_format, HttpContext.Current.Items["Original_URL"].ToString(), RequestSpecificValues.Current_User.UserID)
                                    ? "Error encountered while sending email" : "Your email has been sent");

                                HttpContext.Current.Response.Redirect(HttpContext.Current.Items["Original_URL"].ToString(), false);
                                HttpContext.Current.ApplicationInstance.CompleteRequest();
                                RequestSpecificValues.Current_Mode.Request_Completed = true;
                                return;
                            }
                        }

                        if (action == "add_item")
                        {
                            string usernotes = HttpContext.Current.Request.Form["add_notes"].Trim();
                            string foldername = HttpContext.Current.Request.Form["add_bookshelf"].Trim();
                            bool open_bookshelf = HttpContext.Current.Request.Form["open_bookshelf"] != null;

                            if (SobekCM_Database.Add_Item_To_User_Folder(RequestSpecificValues.Current_User.UserID, foldername, currentItem.BibID, currentItem.VID, 0, usernotes, RequestSpecificValues.Tracer))
                            {
                                RequestSpecificValues.Current_User.Add_Bookshelf_Item(currentItem.BibID, currentItem.VID);

                                // Ensure this user folder is not sitting in the cache
                                CachedDataManager.Remove_User_Folder_Browse(RequestSpecificValues.Current_User.UserID, foldername, RequestSpecificValues.Tracer);

                                HttpContext.Current.Session.Add("ON_LOAD_MESSAGE", "Item was saved to your bookshelf.");

                                if (open_bookshelf)
                                {
                                    HttpContext.Current.Session.Add("ON_LOAD_WINDOW", "?m=lmfl" + foldername.Replace("\"", "%22").Replace("'", "%27").Replace("=", "%3D").Replace("&", "%26") + "&vp=1");
                                }
                            }
                            else
                            {
                                HttpContext.Current.Session.Add("ON_LOAD_MESSAGE", "ERROR encountered while trying to save to your bookshelf.");
                            }

                            HttpContext.Current.Response.Redirect(HttpContext.Current.Items["Original_URL"].ToString(), false);
                            HttpContext.Current.ApplicationInstance.CompleteRequest();
                            RequestSpecificValues.Current_Mode.Request_Completed = true;
                            return;
                        }

                        if (action == "remove")
                        {
                            if (SobekCM_Database.Delete_Item_From_User_Folders(RequestSpecificValues.Current_User.UserID, currentItem.BibID, currentItem.VID, RequestSpecificValues.Tracer))
                            {
                                RequestSpecificValues.Current_User.Remove_From_Bookshelves(currentItem.BibID, currentItem.VID);
                                CachedDataManager.Remove_All_User_Folder_Browses(RequestSpecificValues.Current_User.UserID, RequestSpecificValues.Tracer);
                                HttpContext.Current.Session.Add("ON_LOAD_MESSAGE", "Item was removed from your bookshelves.");
                            }
                            else
                            {
                                HttpContext.Current.Session.Add("ON_LOAD_MESSAGE", "ERROR encountered while trying to remove item from your bookshelves.");
                            }

                            HttpContext.Current.Response.Redirect(HttpContext.Current.Items["Original_URL"].ToString(), false);
                            HttpContext.Current.ApplicationInstance.CompleteRequest();
                            RequestSpecificValues.Current_Mode.Request_Completed = true;
                            return;
                        }

                        if (action.IndexOf("add_tag") == 0)
                        {
                            int tagid = -1;
                            if (action.Replace("add_tag", "").Length > 0)
                            {
                                tagid = Convert.ToInt32(action.Replace("add_tag_", ""));
                            }
                            string description = HttpContext.Current.Request.Form["add_tag"].Trim();
                            int new_tagid = SobekCM_Database.Add_Description_Tag(RequestSpecificValues.Current_User.UserID, tagid, currentItem.Web.ItemID, description, RequestSpecificValues.Tracer);
                            if (new_tagid > 0)
                            {
                                currentItem.Web.Add_User_Tag(RequestSpecificValues.Current_User.UserID, RequestSpecificValues.Current_User.Full_Name, description, DateTime.Now, new_tagid);
                                RequestSpecificValues.Current_User.Has_Descriptive_Tags = true;
                            }

                            HttpContext.Current.Response.Redirect(HttpContext.Current.Items["Original_URL"].ToString(), false);
                            HttpContext.Current.ApplicationInstance.CompleteRequest();
                            RequestSpecificValues.Current_Mode.Request_Completed = true;
                            return;
                        }

                        if (action.IndexOf("delete_tag") == 0)
                        {
                            if (action.Replace("delete_tag", "").Length > 0)
                            {
                                int tagid = Convert.ToInt32(action.Replace("delete_tag_", ""));
                                if (currentItem.Web.Delete_User_Tag(tagid, RequestSpecificValues.Current_User.UserID))
                                {
                                    SobekCM_Database.Delete_Description_Tag(tagid, RequestSpecificValues.Tracer);
                                }
                            }
                            HttpContext.Current.Response.Redirect(HttpContext.Current.Items["Original_URL"].ToString(), false);
                            HttpContext.Current.ApplicationInstance.CompleteRequest();
                            RequestSpecificValues.Current_Mode.Request_Completed = true;
                            return;
                        }
                    }
                }

                // Handle any request from the internal header for the item
                if ((HttpContext.Current != null) && (HttpContext.Current.Request.Form["internal_header_action"] != null) && (RequestSpecificValues.Current_User != null))
                {
                    // Pull the action value
                    string internalHeaderAction = HttpContext.Current.Request.Form["internal_header_action"].Trim();

                    // Was this to save the item comments?
                    if (internalHeaderAction == "save_comments")
                    {
                        string new_comments = HttpContext.Current.Request.Form["intheader_internal_notes"].Trim();
                        if (SobekCM_Item_Database.Save_Item_Internal_Comments(currentItem.Web.ItemID, new_comments))
                            currentItem.Web.Internal_Comments = new_comments;
                    }
                }
            }

            // Set the code for bib level mets to show the volume tree by default
            if ((is_bib_level) && (String.IsNullOrEmpty(RequestSpecificValues.Current_Mode.ViewerCode)))
            {
                RequestSpecificValues.Current_Mode.ViewerCode = "allvolumes1";
            }

            // If there is a file name included, look for the sequence of that file
            if (!String.IsNullOrEmpty(RequestSpecificValues.Current_Mode.Page_By_FileName))
            {
                int page_sequence = currentItem.Page_Sequence_By_FileName(RequestSpecificValues.Current_Mode.Page_By_FileName);
                if (page_sequence > 0)
                {
                    RequestSpecificValues.Current_Mode.ViewerCode = page_sequence.ToString();
                    RequestSpecificValues.Current_Mode.Page = (ushort)page_sequence;
                }
            }

            // Get the valid viewer code
            RequestSpecificValues.Tracer.Add_Trace("Item_HtmlSubwriter.Constructor", "Getting the appropriate item viewer");
            prototyper = ItemViewer_Factory.Get_Item_Viewer(currentItem, RequestSpecificValues.Current_Mode.ViewerCode);
            if (( prototyper != null ) && ( prototyper.Has_Access(currentItem, RequestSpecificValues.Current_User, !String.IsNullOrEmpty(restriction_message))))
                pageViewer = prototyper.Create_Viewer(currentItem, RequestSpecificValues.Current_User, RequestSpecificValues.Current_Mode, RequestSpecificValues.Tracer );
            else
            {
                // Since the user did not have access to THAT viewer, try to find one that he does have access to
                if (currentItem.UI.Viewers_By_Priority != null)
                {
                    foreach (string viewerType in currentItem.UI.Viewers_By_Priority)
                    {
                        prototyper = ItemViewer_Factory.Get_Viewer_By_ViewType(viewerType);
                        if ((prototyper != null) && (prototyper.Has_Access(currentItem, RequestSpecificValues.Current_User, !String.IsNullOrEmpty(restriction_message))))
                        {
                            pageViewer = prototyper.Create_Viewer(currentItem, RequestSpecificValues.Current_User, RequestSpecificValues.Current_Mode, RequestSpecificValues.Tracer);
                            break;
                        }
                    }
                }

            }

            // If execution should end, do it now
            if (RequestSpecificValues.Current_Mode.Request_Completed)
                return;

            // If there were NO views, then pageViewer could be null
            if (pageViewer == null)
                pageViewer = new NoViews_ItemViewer();

            RequestSpecificValues.Tracer.Add_Trace("Item_HtmlSubwriter.Constructor", "Created " + pageViewer.GetType().ToString().Replace("SobekCM.Library.ItemViewer.Viewers.", ""));

            // Assign the rest of the information, if a page viewer was created
            behaviors = new List<HtmlSubwriter_Behaviors_Enum>();
            if (pageViewer != null)
            {
                // Get the list of any special behaviors
                pageViewerBehaviors = pageViewer.ItemViewer_Behaviors;
                if ( pageViewerBehaviors != null )
                    behaviors.AddRange(pageViewerBehaviors);
                else
                    pageViewerBehaviors = new List<HtmlSubwriter_Behaviors_Enum>();
            }
            else
            {
                pageViewerBehaviors = new List<HtmlSubwriter_Behaviors_Enum>();
            }

            // ALways suppress the banner and skip to main content
            if (behaviors == null)
                behaviors = new List<HtmlSubwriter_Behaviors_Enum>();

            if (!behaviors.Contains(HtmlSubwriter_Behaviors_Enum.Suppress_Banner))
                behaviors.Add(HtmlSubwriter_Behaviors_Enum.Suppress_Banner);
            if (!behaviors.Contains(HtmlSubwriter_Behaviors_Enum.Include_Skip_To_Main_Content_Link))
                behaviors.Add(HtmlSubwriter_Behaviors_Enum.Include_Skip_To_Main_Content_Link);

            //if ((searchMatchOnThisPage) && ((PageViewer.ItemViewer_Type == ItemViewer_Type_Enum.JPEG) || (PageViewer.ItemViewer_Type == ItemViewer_Type_Enum.JPEG2000)))
            //{
            //    if (PageViewer.ItemViewer_Type == ItemViewer_Type_Enum.JPEG2000)
            //    {
            //        Aware_JP2_ItemViewer jp2_viewer = (Aware_JP2_ItemViewer) PageViewer;
            //        jp2_viewer.Add_Feature("Red", "DrawEllipse", ((int) (featureXRatioLocation*jp2_viewer.Width)), ((int) (featureYRatioLocation*jp2_viewer.Height)), 800, 800);

            //    }
            //}


            // Get the item layout configuration information (from config files)
            itemLayoutConfig = UI_ApplicationCache_Gateway.Configuration.UI.WriterViewers.Items.Layout;


            // Get the item layout and set the index (from the HTMl template file)
            RequestSpecificValues.Tracer.Add_Trace("Item_HtmlSubwriter.Constructor", "Get the item layout from the HTML template");
            itemLayout = HtmlLayoutManager.GetItemLayout(itemLayoutConfig.ID);
            itemLayoutIndex = 0;


        }

        #endregion

        /// <summary> Gets the collection of special behaviors which this subwriter
        /// requests from the main HTML subwriter. </summary>
        /// <remarks> By default, this returns an empty list </remarks>
        public override List<HtmlSubwriter_Behaviors_Enum> Subwriter_Behaviors
        {
            get
            {
                return behaviors;
            }
        }

        /// <summary> Flag indicates if the internal header should included </summary>
        public override bool Include_Internal_Header
        {
            get
            {
                // If no user, do not show
                if ((RequestSpecificValues.Current_User == null) || (!RequestSpecificValues.Current_User.LoggedOn))
                    return false;

                // Always show for admins
                if ((RequestSpecificValues.Current_User.Is_System_Admin) || (RequestSpecificValues.Current_User.Is_Portal_Admin))
                    return true;

                if (RequestSpecificValues.Current_User.Can_Edit_This_Item(currentItem.BibID, currentItem.Type, currentItem.Behaviors.Source_Institution_Aggregation, currentItem.Behaviors.Holding_Location_Aggregation, currentItem.Behaviors.Aggregation_Code_List))
                    return true;

                // Otherwise, do not show
                return false;
            }
        }

        #region Write the internal management header (above the web skin header)

        /// <summary> Adds the internal header HTML for this specific HTML writer </summary>
        /// <param name="Output"> Stream to which to write the HTML for the internal header information </param>
        /// <param name="Current_User"> Currently logged on user, to determine specific rights </param>
        public override void Write_Internal_Header_HTML(TextWriter Output, User_Object Current_User)
        {
            // If this is for a fragment, do nothing
            if (!String.IsNullOrEmpty(RequestSpecificValues.Current_Mode.Fragment))
                return;

            string currentViewerCode = RequestSpecificValues.Current_Mode.ViewerCode;

            Output.WriteLine("  <table id=\"sbk_InternalHeader\">");
            Output.WriteLine("    <tr style=\"height:30px;\">");
            Output.WriteLine("      <td style=\"text-align:left\">");
            Output.WriteLine("          <button title=\"Hide Internal Header\" class=\"sbkIsw_intheader_button hide_intheader_button2\" onclick=\"return hide_internal_header();\"></button>");
            Output.WriteLine("      </td>");

            if (is_bib_level)
            {
                Output.WriteLine("      <td style=\"text-align:center;\"><h2>" + currentItem.BibID + "</h2></td>");
            }
            else
            {
                Output.WriteLine("      <td style=\"text-align:center;\"><h2><a href=\"" + RequestSpecificValues.Current_Mode.Base_URL + currentItem.BibID + "/00000\">" + currentItem.BibID + "</a> : " + currentItem.VID + "</h2></td>");
            }

            Write_Internal_Header_Search_Box(Output);
            Output.WriteLine("    </tr>");

            if (!is_bib_level)
            {
                Output.WriteLine("    <tr style=\"height:40px;\">");
                Output.WriteLine("      <td colspan=\"3\" style=\"text-align:center;vertical-align:middle;\">");

                // Should we add ability to edit this item to the quick links?
                if (userCanEditItem)
                {
                    // Add ability to edit metadata for this item
                    if (is_tei)
                    {
                        RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.My_Sobek;
                        RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Edit_TEI_Item;
                        RequestSpecificValues.Current_Mode.My_Sobek_SubMode = "1";
                        Output.WriteLine("          <button title=\"Edit TEI\" class=\"sbkIsw_intheader_button edit_tei_button\" onclick=\"window.location.href='" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "';return false;\"></button>");
                    }
                    else
                    {
                        RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.My_Sobek;
                        RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Edit_Item_Metadata;
                        RequestSpecificValues.Current_Mode.My_Sobek_SubMode = "1";
                        Output.WriteLine("          <button title=\"Edit Metadata\" class=\"sbkIsw_intheader_button edit_metadata_button\" onclick=\"window.location.href='" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "';return false;\"></button>");
 
                    }

                    // Add ability to edit behaviors for this item
                    RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Edit_Item_Behaviors;
                    RequestSpecificValues.Current_Mode.My_Sobek_SubMode = "1";
                    Output.WriteLine("          <button title=\"Edit Behaviors\" class=\"sbkIsw_intheader_button edit_behaviors_button\" onclick=\"window.location.href='" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "';return false;\"></button>");
                    RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Item_Display;


                    // Add ability to edit behaviors for this item
                    if ((currentItem.Images == null ) || ( currentItem.Images.Count == 0 ))
                    {
                        RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.My_Sobek;
                        RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Page_Images_Management;
                        Output.WriteLine("          <button title=\"Perform Quality Control\" class=\"sbkIsw_intheader_button qualitycontrol_button\" onclick=\"window.location.href='" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "';return false;\"></button>");
                        RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Item_Display;
                    }
                    else
                    {
                        RequestSpecificValues.Current_Mode.ViewerCode = "qc";
                        Output.WriteLine("          <button title=\"Perform Quality Control\" class=\"sbkIsw_intheader_button qualitycontrol_button\" onclick=\"window.location.href='" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "';return false;\"></button>");
                    }

                    // Get ready to send to item permissions
                    RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.My_Sobek;
                    RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Edit_Item_Permissions;
                    RequestSpecificValues.Current_Mode.My_Sobek_SubMode = "1";

                    // Check if this item is DARK first
                    if (currentItem.Behaviors.Dark_Flag)
                    {
                        Output.WriteLine("          <button title=\"Dark Resource\" class=\"sbkIsw_intheader_button dark_resource_button_fixed\" onclick=\"window.location.href='" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "';return false;\"></button>");
                    }
                    else
                    {
                        // If the item is currently PUBLIC, only internal or system admins can reset back to PRIVATE
                        if (currentItem.Behaviors.IP_Restriction_Membership >= 0)
                        {
                            if ((RequestSpecificValues.Current_User.Is_Internal_User) || (RequestSpecificValues.Current_User.Is_System_Admin))
                            {
                                Output.WriteLine(currentItem.Behaviors.IP_Restriction_Membership == 0
                                                     ? "          <button title=\"Change Access Restriction\" class=\"sbkIsw_intheader_button public_resource_button\" onclick=\"window.location.href='" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "';return false;\"></button>"
                                                     : "          <button title=\"Change Access Restriction\" class=\"sbkIsw_intheader_button restricted_resource_button\" onclick=\"window.location.href='" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "';return false;\"></button>");
                            }
                            else
                            {
                                Output.WriteLine(currentItem.Behaviors.IP_Restriction_Membership == 0
                                                     ? "          <button title=\"Public Resource\" class=\"sbkIsw_intheader_button public_resource_button_fixed\" onclick=\"window.location.href='" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "';return false;\"></button>"
                                                     : "          <button title=\"IP Restriced Resource\" class=\"sbkIsw_intheader_button restricted_resource_button_fixed\" onclick=\"window.location.href='" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "';return false;\"></button>");
                            }
                        }
                        else
                        {
                            Output.WriteLine("          <button title=\"Change Access Restriction\" class=\"sbkIsw_intheader_button private_resource_button\" onclick=\"window.location.href='" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "';return false;\"></button>");
                        }
                    }
                }
                else
                {
                    // Check if this item is DARK first
                    if (currentItem.Behaviors.Dark_Flag)
                    {
                        Output.WriteLine("          <button title=\"Dark Resource\" class=\"sbkIsw_intheader_button dark_resource_button_fixed\" onclick=\"window.location.href='" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "';return false;\"></button>");
                    }
                    else
                    {
                        // Still show that the item is public, private, restricted
                        if (currentItem.Behaviors.IP_Restriction_Membership > 0)
                        {
                            Output.WriteLine("          <button title=\"IP Restriced Resource\" class=\"sbkIsw_intheader_button restricted_resource_button_fixed\" onclick=\"return false;\"></button>");
                        }
                        if (currentItem.Behaviors.IP_Restriction_Membership == 0)
                        {
                            Output.WriteLine("          <button title=\"Public Resource\" class=\"sbkIsw_intheader_button public_resource_button_fixed\" onclick=\"return false;\"></button>");
                        }
                        if (currentItem.Behaviors.IP_Restriction_Membership < 0)
                        {
                            Output.WriteLine("          <button title=\"Private Resource\" class=\"sbkIsw_intheader_button private_resource_button_fixed\" onclick=\"return false;\"></button>");
                        }
                    }
                }

                RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Item_Display;
                RequestSpecificValues.Current_Mode.ViewerCode = "tracking";
                Output.WriteLine("          <button title=\"View Work Log\" class=\"sbkIsw_intheader_button view_worklog_button\" onclick=\"window.location.href='" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "';return false;\"></button>");
                RequestSpecificValues.Current_Mode.ViewerCode = currentViewerCode;

                // Add ability to edit behaviors for this item
                if (userCanEditItem)
                {
                    RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.My_Sobek;
                    RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.File_Management;
                    Output.WriteLine("          <button title=\"Manage Files\" class=\"sbkIsw_intheader_button manage_files_button\" onclick=\"window.location.href='" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "';return false;\"></button>");
                    RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Item_Display;
                }

                // Add the HELP icon next
                Output.WriteLine("<span id=\"sbk_InternalHeader_Help\"><a href=\"" + UI_ApplicationCache_Gateway.Settings.System.Help_URL(RequestSpecificValues.Current_Mode.Base_URL) + "help/itemheader\" title=\"Help regarding this header\"><img src=\"" + Static_Resources_Gateway.Help_Button_Darkgray_Jpg + "\" alt=\"?\" title=\"Help regarding this header\" /></a></span>");

                Output.WriteLine("      </td>");
                Output.WriteLine("    </tr>");

                // Display the comments and allow change?
                if (currentItem.Web != null)
                {
                    string internal_comments_normalized = currentItem.Web.Internal_Comments ?? String.Empty;
                    if ((userCanEditItem) || (RequestSpecificValues.Current_User.Is_Internal_User) || (RequestSpecificValues.Current_User.Is_System_Admin))
                    {
                        const int ROWS = 1;
                        const int ACTUAL_COLS = 70;

                        // Add the internal comments row ( hidden content initially )
                        Output.WriteLine("    <tr style=\"text-align:center; height:14px;\">");
                        Output.WriteLine("      <td colspan=\"3\" style=\"text-align:center;\">");
                        Output.WriteLine("        <table id=\"internal_notes_div\">");
                        Output.WriteLine("          <tr style=\"text-align:left; height:14px;\">");
                        Output.WriteLine("            <td class=\"intheader_label\">COMMENTS:</td>");
                        Output.WriteLine("            <td>");
                        Output.WriteLine("              <textarea rows=\"" + ROWS + "\" cols=\"" + ACTUAL_COLS + "\" name=\"intheader_internal_notes\" id=\"intheader_internal_notes\" class=\"intheader_comments_input sbkIsw_Focusable\">" + HttpUtility.HtmlEncode(internal_comments_normalized) + "</textarea>");
                        Output.WriteLine("            </td>");
                        Output.WriteLine("            <td>");
                        Output.WriteLine("              <button title=\"Save new internal comments\" class=\"internalheader_button\" onclick=\"save_internal_notes(); return false;\">SAVE</button>");
                        Output.WriteLine("            </td>");
                        Output.WriteLine("          </tr>");
                        Output.WriteLine("        </table>");
                        Output.WriteLine("      </td>");
                        Output.WriteLine("    </tr>");
                    }
                    else
                    {
                        const int ROWS = 1;
                        const int ACTUAL_COLS = 80;

                        // Add the internal comments row ( hidden content initially )
                        Output.WriteLine("    <tr style=\"text-align:center; height:14px;\">");
                        Output.WriteLine("      <td colspan=\"2\">");
                        Output.WriteLine("        <table id=\"internal_notes_div\">");
                        Output.WriteLine("          <tr style=\"text-align:left; height:14px;\">");
                        Output.WriteLine("            <td class=\"intheader_label\">COMMENTS:</td>");
                        Output.WriteLine("            <td>");
                        Output.WriteLine("              <textarea readonly=\"readonly\" rows=\"" + ROWS + "\" cols=\"" + ACTUAL_COLS + "\" name=\"intheader_internal_notes\" id=\"intheader_internal_notes\" class=\"intheader_comments_input\" onfocus=\"javascript:textbox_enter('intheader_internal_notes','intheader_comments_input_focused')\" onblur=\"javascript:textbox_leave('intheader_internal_notes','intheader_comments_input')\">" + HttpUtility.HtmlEncode(internal_comments_normalized) + "</textarea>");
                        Output.WriteLine("            </td>");
                        Output.WriteLine("          </tr>");
                        Output.WriteLine("        </table>");
                        Output.WriteLine("      </td>");
                        Output.WriteLine("    </tr>");
                    }
                }
            }
            else
            {

                if (userCanEditItem)
                {
                    Output.WriteLine("    <tr style=\"height:45px;\">");
                    Output.WriteLine("      <td colspan=\"3\" style=\"text-align:center;vertical-align:middle;\">");

                    // Add ability to edit behaviors for this item group
                    RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.My_Sobek;
                    RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Edit_Group_Behaviors;
                    RequestSpecificValues.Current_Mode.My_Sobek_SubMode = "1";
                    Output.WriteLine("          <button title=\"Edit Behaviors\" class=\"sbkIsw_intheader_button edit_behaviors_button\" onclick=\"window.location.href='" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "';return false;\"></button>");

                    // Add ability to add a new item/volume to this title
                    RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Group_Add_Volume;
                    Output.WriteLine("          <button title=\"Add Volume\" class=\"sbkIsw_intheader_button add_volume_button\" onclick=\"window.location.href='" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "';return false;\"></button>");

                    // Add ability to auto-fill a number of new items/volumes to this title
                    RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Group_AutoFill_Volumes;
                    Output.WriteLine("          <button title=\"Auto-Fill Volumes\" class=\"sbkIsw_intheader_button autofill_volumes_button\" onclick=\"window.location.href='" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "';return false;\"></button>");

                    // Add ability to edit the serial hierarchy online
                    RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Edit_Group_Serial_Hierarchy;
                    Output.WriteLine("          <button title=\"Edit Serial Hierarchy\" class=\"sbkIsw_intheader_button serial_hierarchy_button\" onclick=\"window.location.href='" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "';return false;\"></button>");

                    // Add ability to mass update the items behaviors under this title
                    RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Group_Mass_Update_Items;
                    Output.WriteLine("          <button title=\"Mass Update Volumes\" class=\"sbkIsw_intheader_button mass_update_button\" onclick=\"window.location.href='" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "';return false;\"></button>");

                    Output.WriteLine("      </td>");
                    Output.WriteLine("    </tr>");
                }

            }

            Output.WriteLine("  </table>");

            RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Item_Display;
            RequestSpecificValues.Current_Mode.ViewerCode = currentViewerCode;
        }

        #endregion

        /// <summary> Add the header to the output </summary>
        /// <param name="Output"> Stream to which to write the HTML for this header </param>
        public override void Add_Header(TextWriter Output)
        {
            HeaderFooter_Helper_HtmlSubWriter.Add_Header(Output, RequestSpecificValues, Container_CssClass, WebPage_Title, Subwriter_Behaviors, null, currentItem);
        }

	    /// <summary> Writes the HTML generated by this item html subwriter directly to the response stream </summary>
	    /// <param name="Output"> Stream to which to write the HTML for this subwriter </param>
	    /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
	    /// <returns> Value indicating if html writer should finish the page immediately after this, or if there are other controls or routines which need to be called first </returns>
	    /// <remarks> This begins writing this page, up to the item-level main menu</remarks>
	    public override bool Write_HTML(TextWriter Output, Custom_Tracer Tracer)
	    {
		    Tracer.Add_Trace("Item_HtmlSubwriter.Write_HTML", "Do Nothing");

            return true;
	    }


        /// <summary> Writes the html to the output stream open the itemNavForm, which appears just before the TocPlaceHolder </summary>
        /// <param name="Output">Stream to directly write to</param>
        /// <param name="Tracer">Trace object keeps a list of each method executed and important milestones in rendering</param>
        public override void Write_ItemNavForm_Opening(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace("Item_HtmlSubwriter.Write_ItemNavForm_Opening", "Write the area up and including the start of the viewer area");

            // Write from the layout
            if (itemLayout == null) return;

            // Step through all the sections 
            while (itemLayoutIndex < itemLayout.Sections.Count)
            {
                if (itemLayout.Sections[itemLayoutIndex].Type == HtmlLayoutSectionTypeEnum.Viewer_Section)
                {
                    add_viewer_area_start(Output, Tracer);
                    return;
                }
                else if (itemLayout.Sections[itemLayoutIndex].Type == HtmlLayoutSectionTypeEnum.Static_HTML)
                {
                    Output.WriteLine(itemLayout.Sections[itemLayoutIndex].HTML);
                }
                else if (itemLayout.Sections[itemLayoutIndex].Type == HtmlLayoutSectionTypeEnum.Dynamic_Section)
                {
                    string section_name = itemLayout.Sections[itemLayoutIndex].Name;
                    Tracer.Add_Trace("Item_HtmlSubwriter.Write_ItemNavForm_Opening", "Adding html into the " + section_name + " section");

                    // Get the writer list to write here
                    SectionWriterGroupConfig config = itemLayoutConfig.GetSection(section_name);
                    if ((config != null) && (config.Writers != null))
                    {
                        // Step through each writer in the config
                        foreach (SectionWriterConfig thisWriterConfig in config.Writers)
                        {
                            // Only continue if it is enabled
                            if (!thisWriterConfig.Enabled) continue;

                            // Get the writer
                            iItemSectionWriter writer = ItemSectionWriter_Factory.Get_ItemSectionWriter(thisWriterConfig.Assembly, thisWriterConfig.Class);
                            if (writer == null)
                            {
                                Tracer.Add_Trace("Item_HtmlSubwriter.Write_ItemNavForm_Opening", "Writer returned from factory was null for " + thisWriterConfig.ID);
                                continue;
                            }

                            // Add the HTML
                            writer.Write_HTML(Output, prototyper, pageViewer, currentItem, RequestSpecificValues, behaviors);
                        }
                    }
                }

                itemLayoutIndex++;
            }
        }

        private void add_viewer_area_start(TextWriter Output, Custom_Tracer Tracer)
        {
            if (behaviors.Contains(HtmlSubwriter_Behaviors_Enum.Item_Subwriter_NonWindowed_Mode))
            {
                //if (pageViewer != null && pageViewer.Viewer_Height > 0)
                //    Output.WriteLine("<table id=\"sbkIsw_DocumentNonWindowed\" style=\"height:" + pageViewer.Viewer_Height + "px;\" >");
                //else
                Output.WriteLine("<table id=\"sbkIsw_DocumentNonWindowed\" >");
            }
            else
            {
                // Start the table
                Output.Write("<table class=\"sbkIsw_DocumentDisplay2\" ");

                // Was an ID included for this viewer?
                if ((pageViewer != null) && (!String.IsNullOrEmpty(pageViewer.ViewerBox_CssId)))
                    Output.Write("id=\"" + pageViewer.ViewerBox_CssId + "\" ");

                // Were there inline styles as well?
                if ((pageViewer != null) && (!String.IsNullOrEmpty(pageViewer.ViewerBox_InlineStyle)))
                    Output.Write("style=\"" + pageViewer.ViewerBox_InlineStyle + "\" ");

                Output.WriteLine(">");

                // In this format, add the DARK and RESTRICTED information
                if (currentItem.Behaviors.Dark_Flag)
                {
                    Output.WriteLine("\t<tr id=\"sbkIsw_RestrictedRow\">");
                    Output.WriteLine("\t\t<td>");
                    Output.WriteLine("\t\t\t<span style=\"font-size:larger; font-weight: bold;\">DARK ITEM</span>");
                    Output.WriteLine("\t\t</td>");
                    Output.WriteLine("\t</tr>");
                }
                else if (currentItem.Behaviors.IP_Restriction_Membership < 0)
                {
                    Output.WriteLine("\t<tr id=\"sbkIsw_RestrictedRow\">");
                    Output.WriteLine("\t\t<td>");
                    Output.WriteLine("\t\t\t<span style=\"font-size:larger; font-weight: bold;\">PRIVATE ITEM</span>");
                    Output.WriteLine("\t\t\tDigitization of this item is currently in progress.");
                    Output.WriteLine("\t\t</td>");
                    Output.WriteLine("\t</tr>");
                }
            }

            #region Add navigation rows here (buttons for first, previous, next, last, etc.)

            // Add navigation row here (buttons and viewer specific)
            if (pageViewer != null)
            {
                // Allow the pageviewer to add any special elements to the main 
                // item viewer above the pagination
                pageViewer.Write_Top_Additional_Navigation_Row(Output, Tracer);

                // Should buttons be included here?
                if (pageViewer.PageCount != 1)
                {
                    Output.WriteLine("\t<tr>");
                    Output.WriteLine("\t\t<td>");

                    // ADD NAVIGATION BUTTONS
                    if (pageViewer.PageCount != 1)
                    {
                        string go_to = "Go To:";
                        string first_page = "First Page";
                        string previous_page = "Previous Page";
                        string next_page = "Next Page";
                        string last_page = "Last Page";
                        string first_page_text = "First";
                        string previous_page_text = "Previous";
                        string next_page_text = "Next";
                        string last_page_text = "Last";

                        if (RequestSpecificValues.Current_Mode.Language == Web_Language_Enum.Spanish)
                        {
                            go_to = "Ir a:";
                            first_page = "Primera Página";
                            previous_page = "Página Anterior";
                            next_page = "Página Siguiente";
                            last_page = "Última Página";
                            first_page_text = "Primero";
                            previous_page_text = "Anterior";
                            next_page_text = "Proximo";
                            last_page_text = "Último";
                        }

                        if (RequestSpecificValues.Current_Mode.Language == Web_Language_Enum.French)
                        {
                            go_to = "Aller à:";
                            first_page = "Première Page";
                            previous_page = "Page Précédente";
                            next_page = "Page Suivante";
                            last_page = "Dernière Page";
                            first_page_text = "Première";
                            previous_page_text = "Précédente";
                            next_page_text = "Suivante";
                            last_page_text = "Derniere";
                        }

                        Output.WriteLine("\t\t\t<div class=\"sbkIsw_PageNavBar\">");
                        StringBuilder buttonsHtmlBuilder = new StringBuilder(1000);

                        // Get the URL for the first and previous buttons
                        string firstButtonURL = pageViewer.First_Page_URL;
                        string prevButtonURL = pageViewer.Previous_Page_URL;

                        // Only continue if there is an item and mode, and there is previous pages to go to
                        if ((pageViewer.Current_Page > 1) && ((firstButtonURL.Length > 0) || (prevButtonURL.Length > 0)))
                        {
                            buttonsHtmlBuilder.AppendLine("\t\t\t\t<span class=\"sbkIsw_LeftPaginationButtons\">");
                            buttonsHtmlBuilder.AppendLine("\t\t\t\t\t<button title=\"" + first_page + "\" class=\"sbkIsw_RoundButton\" onclick=\"window.location='" + firstButtonURL + "'; return false;\"><img src=\"" + Static_Resources_Gateway.Button_First_Arrow_Png + "\" class=\"roundbutton_img_left\" alt=\"\" />" + first_page_text + "</button>&nbsp;");
                            buttonsHtmlBuilder.AppendLine("\t\t\t\t\t<button title=\"" + previous_page + "\" class=\"sbkIsw_RoundButton\" onclick=\"window.location='" + prevButtonURL + "'; return false;\"><img src=\"" + Static_Resources_Gateway.Button_Previous_Arrow_Png + "\" class=\"roundbutton_img_left\" alt=\"\" />" + previous_page_text + "</button>");
                            buttonsHtmlBuilder.AppendLine("\t\t\t\t</span>");
                        }

                        // Get the URL for the first and previous buttons
                        string lastButtonURL = pageViewer.Last_Page_URL;
                        string nextButtonURL = pageViewer.Next_Page_URL;

                        // Only continue if there is an item and mode, and there is previous pages to go to
                        if ((pageViewer.Current_Page < pageViewer.PageCount) && ((lastButtonURL.Length > 0) || (nextButtonURL.Length > 0)))
                        {
                            buttonsHtmlBuilder.AppendLine("\t\t\t\t<span class=\"sbkIsw_RightPaginationButtons\">");
                            buttonsHtmlBuilder.AppendLine("\t\t\t\t\t<button title=\"" + next_page + "\" class=\"sbkIsw_RoundButton\" onclick=\"window.location='" + nextButtonURL + "'; return false;\">" + next_page_text + "<img src=\"" + Static_Resources_Gateway.Button_Next_Arrow_Png + "\" class=\"roundbutton_img_right\" alt=\"\" /></button>&nbsp;");
                            buttonsHtmlBuilder.AppendLine("\t\t\t\t\t<button title=\"" + last_page + "\" class=\"sbkIsw_RoundButton\" onclick=\"window.location='" + lastButtonURL + "'; return false;\">" + last_page_text + "<img src=\"" + Static_Resources_Gateway.Button_Last_Arrow_Png + "\" class=\"roundbutton_img_right\" alt=\"\" /></button>");
                            buttonsHtmlBuilder.AppendLine("\t\t\t\t</span>");
                        }

                        // Write the buttons and save the HTML for the bottom of the page
                        buttonsHtml = buttonsHtmlBuilder.ToString();
                        Output.WriteLine(buttonsHtml);

                        // Show a pageselector, if one was selected
                        switch (pageViewer.Page_Selector)
                        {
                            case ItemViewer_PageSelector_Type_Enum.DropDownList:
                                string[] pageNames = pageViewer.Go_To_Names;
                                if (pageNames.Length > 0)
                                {
                                    // Determine if these page names are very long at all
                                    if (pageNames.Any(ThisName => ThisName.Length > 25))
                                    {
                                        // Long page names, so move the Go To: to the next line (new div)
                                        Output.WriteLine("\t\t\t</div>");
                                        Output.WriteLine("\t\t\t<div class=\"sbkIsw_PageNavBar2\">");
                                    }

                                    Output.WriteLine("\t\t\t\t<span id=\"sbkIsw_GoToSpan\"><label for=\"page_select\">" + go_to + "</label></span>");
                                    string orig_viewercode = RequestSpecificValues.Current_Mode.ViewerCode;
                                    string viewercode_only = RequestSpecificValues.Current_Mode.ViewerCode.Replace(RequestSpecificValues.Current_Mode.Page.ToString(), "");
                                    RequestSpecificValues.Current_Mode.ViewerCode = "XX1234567890XX";
                                    string url = UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode);
                                    RequestSpecificValues.Current_Mode.ViewerCode = orig_viewercode;

                                    Output.WriteLine("\t\t\t\t<select id=\"page_select\" name=\"page_select\" onchange=\"javascript:item_jump_sobekcm('" + url + "')\">");

                                    // Add all the page selection items to the combo box
                                    int page_index = 1;
                                    foreach (string thisName in pageNames)
                                    {
                                        if (thisName.Length > 75)
                                        {
                                            if (RequestSpecificValues.Current_Mode.Page == page_index)
                                            {
                                                Output.WriteLine("\t\t\t\t\t<option value=\"" + page_index + viewercode_only + "\" selected=\"selected\" >" + thisName.Substring(0, 75) + "..</option>");
                                            }
                                            else
                                            {
                                                Output.WriteLine("\t\t\t\t\t<option value=\"" + page_index + viewercode_only + "\">" + thisName.Substring(0, 75) + "..</option>");
                                            }
                                        }
                                        else
                                        {
                                            if (RequestSpecificValues.Current_Mode.Page == page_index)
                                            {
                                                Output.WriteLine("\t\t\t\t\t<option value=\"" + page_index + viewercode_only + "\" selected=\"selected\" >" + thisName + "</option>");
                                            }
                                            else
                                            {
                                                Output.WriteLine("\t\t\t\t\t<option value=\"" + page_index + viewercode_only + "\">" + thisName + "</option>");
                                            }
                                        }
                                        page_index++;
                                    }

                                    Output.WriteLine("\t\t\t\t</select>");
                                }
                                break;

                            case ItemViewer_PageSelector_Type_Enum.PageLinks:
                                // Create the page selection if that is the type to display.  This is where it is actually
                                // built as well, althouogh it is subsequently used further up the page
                                if (pageViewer.Page_Selector == ItemViewer_PageSelector_Type_Enum.PageLinks)
                                {
                                    StringBuilder pageLinkBuilder = new StringBuilder();

                                    //Get the total page count
                                    int num_of_pages = pageViewer.PageCount;
                                    string[] page_urls = pageViewer.Go_To_Names;

                                    pageLinkBuilder.AppendLine("\t\t\t\t<div class=\"sbkIsw_PageLinks\">");

                                    //Display the first, last, current page numbers, and 2 pages before and after the current page
                                    if (num_of_pages <= 7 && num_of_pages > 1)
                                    {
                                        for (int i = 1; i <= num_of_pages; i++)
                                        {
                                            if (i == pageViewer.Current_Page)
                                                pageLinkBuilder.AppendLine("\t\t\t\t\t" + i + "&nbsp;");
                                            else
                                                pageLinkBuilder.AppendLine("\t\t\t\t\t<a href=\"" + page_urls[i - 1] + "\">" + i + "</a>&nbsp;");
                                        }
                                    }
                                    else if (num_of_pages > 7)
                                    {
                                        if (pageViewer.Current_Page > 4 && pageViewer.Current_Page < num_of_pages - 3)
                                        {
                                            pageLinkBuilder.AppendLine("\t\t\t\t\t<a href=\"" + page_urls[0] + "\">" + 1 + "</a>" + "..");
                                            for (int i = pageViewer.Current_Page - 2; i <= pageViewer.Current_Page + 2; i++)
                                            {
                                                if (i == pageViewer.Current_Page)
                                                    pageLinkBuilder.AppendLine("\t\t\t\t\t" + i + "&nbsp;");
                                                else
                                                    pageLinkBuilder.AppendLine("\t\t\t\t\t<a href=\"" + page_urls[i - 1] + "\">" + i + "</a>&nbsp;");
                                            }
                                            pageLinkBuilder.AppendLine("\t\t\t\t\t.." + "<a href=\"" + page_urls[page_urls.Length - 1] + "\">" + num_of_pages + "</a>");
                                        }

                                        else if (pageViewer.Current_Page <= 4 && pageViewer.Current_Page < num_of_pages - 3)
                                        {
                                            for (int i = 1; i <= (pageViewer.Current_Page + 2); i++)
                                            {
                                                if (i == pageViewer.Current_Page)
                                                    pageLinkBuilder.AppendLine("\t\t\t\t\t" + i + "&nbsp;");
                                                else
                                                    pageLinkBuilder.AppendLine("\t\t\t\t\t<a href=\"" + page_urls[i - 1] + "\">" + i + "</a>&nbsp;");
                                            }
                                            pageLinkBuilder.AppendLine("\t\t\t\t\t.." + "<a href=\"" + page_urls[page_urls.Length - 1] + "\">" + num_of_pages + "</a>");
                                        }

                                        else if (pageViewer.Current_Page > 4 && pageViewer.Current_Page >= num_of_pages - 3)
                                        {
                                            pageLinkBuilder.AppendLine("\t\t\t\t\t<a href=\"" + page_urls[0] + "\">" + 1 + "</a>" + "..");
                                            for (int i = pageViewer.Current_Page - 2; i <= num_of_pages; i++)
                                            {
                                                if (i == pageViewer.Current_Page)
                                                    pageLinkBuilder.AppendLine("\t\t\t\t\t" + i + "&nbsp;");
                                                else
                                                    pageLinkBuilder.AppendLine("\t\t\t\t\t<a href=\"" + page_urls[i - 1] + "\">" + i + "</a>&nbsp;");
                                            }

                                        }
                                    }

                                    pageLinkBuilder.AppendLine("\t\t\t\t</div>");

                                    pageLinksHtml = pageLinkBuilder.ToString();
                                    Output.WriteLine(pageLinksHtml);
                                }
                                break;
                        }

                        Output.WriteLine("\t\t\t</div>");
                    }


                    Output.WriteLine("\t\t</td>");
                    Output.WriteLine("\t</tr>");
                }
            }

            #endregion

            Output.WriteLine("\t<tr>");

            // Add the HTML from the pageviewer, the main viewer section
            Tracer.Add_Trace("Item_MainWriter.Write_Additional_HTML", "Allowing page viewer to write directly to the output to add main viewer section");
            pageViewer.Write_Main_Viewer_Section(Output, Tracer);
        }

        /// <summary> Performs the final HTML writing which completes the item table and adds the final page navigation buttons at the bottom of the page </summary>
        /// <param name="Main_PlaceHolder"> Main place holder ( &quot;mainPlaceHolder&quot; ) in the itemNavForm form, widely used throughout the application</param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        public void Add_Main_Viewer_Section(PlaceHolder Main_PlaceHolder, Custom_Tracer Tracer)
        {
            // Add the main viewer section
            if (pageViewer != null)
            {
                Tracer.Add_Trace("Item_HtmlSubwriter.Add_Main_Viewer_Section", "Allowing page viewer to add main viewer section to <i>mainPlaceHolder</i>");
                pageViewer.Add_Main_Viewer_Section(Main_PlaceHolder, Tracer);
            }
        }

        private void add_viewer_area_end(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace("Item_HtmlSubwriter.add_viewer_area_end", "Close the item viewer and add final pagination");

            Output.WriteLine("\t</tr>");

            if ((pageViewer != null) && (pageViewer.PageCount != 1) && (!behaviors.Contains(HtmlSubwriter_Behaviors_Enum.Item_Subwriter_Suppress_Bottom_Pagination)))
            {
                Output.WriteLine("\t<tr>");
                Output.WriteLine("\t\t<td>");

                // ADD NAVIGATION BUTTONS
                if (pageViewer.PageCount != 1)
                {
                    Output.WriteLine("\t\t\t<div class=\"sbkIsw_PageNavBar\">");
                    Output.WriteLine(buttonsHtml);

                    // Create the page selection if that is the type to display.  This is where it is actually
                    // built as well, althouogh it is subsequently used further up the page
                    if (pageViewer.Page_Selector == ItemViewer_PageSelector_Type_Enum.PageLinks)
                    {
                        Output.WriteLine(pageLinksHtml);
                    }

                    Output.WriteLine("\t\t\t</div>");
                }

                Output.WriteLine("\t\t</td>");
                Output.WriteLine("\t</tr>");
            }

            // Close the item viewer table and section for main viewering
            Output.WriteLine("</table>");
        }

        /// <summary> Writes final HTML to the output stream after all the placeholders and just before the itemNavForm is closed.  </summary>
        /// <param name="Output"> Stream to which to write the text for this main writer </param>
        /// <param name="Tracer">Trace object keeps a list of each method executed and important milestones in rendering</param>
        public override void Write_ItemNavForm_Closing(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace("Item_HtmlSubwriter.Write_ItemNavForm_Closing", "Write the area after the controls placeholder of the viewer area");

            // Write from the layout
            if (itemLayout == null) return;

            // Step through all the sections 
            while (itemLayoutIndex < itemLayout.Sections.Count)
            {
                if (itemLayout.Sections[itemLayoutIndex].Type == HtmlLayoutSectionTypeEnum.Viewer_Section)
                {
                    add_viewer_area_end(Output, Tracer);
                }
                else if (itemLayout.Sections[itemLayoutIndex].Type == HtmlLayoutSectionTypeEnum.Static_HTML)
                {
                    Output.WriteLine(itemLayout.Sections[itemLayoutIndex].HTML);
                }
                else if (itemLayout.Sections[itemLayoutIndex].Type == HtmlLayoutSectionTypeEnum.Dynamic_Section)
                {
                    string section_name = itemLayout.Sections[itemLayoutIndex].Name;
                    Tracer.Add_Trace("Item_HtmlSubwriter.Write_ItemNavForm_Opening", "Adding html into the " + section_name + " section");

                    // Get the writer list to write here
                    SectionWriterGroupConfig config = itemLayoutConfig.GetSection(section_name);
                    if ((config != null) && (config.Writers != null))
                    {
                        // Step through each writer in the config
                        foreach (SectionWriterConfig thisWriterConfig in config.Writers)
                        {
                            // Only continue if it is enabled
                            if (!thisWriterConfig.Enabled) continue;

                            // Get the writer
                            iItemSectionWriter writer = ItemSectionWriter_Factory.Get_ItemSectionWriter(thisWriterConfig.Assembly, thisWriterConfig.Class);
                            if (writer == null)
                            {
                                Tracer.Add_Trace("Item_HtmlSubwriter.Write_ItemNavForm_Opening", "Writer returned from factory was null for " + thisWriterConfig.ID);
                                continue;
                            }

                            // Add the HTML
                            writer.Write_HTML(Output, prototyper, pageViewer, currentItem, RequestSpecificValues, behaviors);
                        }
                    }
                }

                itemLayoutIndex++;
            }

            if (!behaviors.Contains(HtmlSubwriter_Behaviors_Enum.Item_Subwriter_Full_JQuery_UI))
            {
                Output.WriteLine("<script type=\"text/javascript\" src=\"" + Static_Resources_Gateway.Jquery_Ui_1_10_3_Draggable_Js + "\"></script>");
            }
        }


        /// <summary> Gets the collection of body attributes to be included 
        /// within the HTML body tag (usually to add events to the body) </summary>
        public override List<Tuple<string, string>> Body_Attributes
        {
            get
            {
                List<Tuple<string, string>> returnValue = new List<Tuple<string, string>>
                    {
                        new Tuple<string, string>("onload", "itemwriter_load();"), 
                        new Tuple<string, string>("onresize", "itemwriter_load();"),
						new Tuple<string, string>("id", "itembody")
                    };

                // Add default script attachments

                // Add any viewer specific body attributes
                if (pageViewer != null)
                    pageViewer.Add_ViewerSpecific_Body_Attributes(returnValue);
                return returnValue;
            }
        }

        /// <summary> Title for this web page </summary>
        public override string WebPage_Title
        {
            get
            {
                return currentItem != null ? currentItem.Title : "{0} Item";
            }
        }

        /// <summary> Write any additional values within the HTML Head of the
        /// final served page </summary>
        /// <param name="Output"> Output stream currently within the HTML head tags </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        public override void Write_Within_HTML_Head(TextWriter Output, Custom_Tracer Tracer)
        {
            // ROBOTS SHOULD BE SENT TO THE CMS PAGE FOR THIS
            if ( String.Compare(RequestSpecificValues.Current_Mode.ViewerCode, "robot", StringComparison.OrdinalIgnoreCase) != 0 )
                Output.WriteLine("  <meta name=\"robots\" content=\"noindex, nofollow\" />");

            // Write the main SobekCM item style sheet to use 
            Output.WriteLine("  <link href=\"" + Static_Resources_Gateway.Sobekcm_Item_Css + "\" rel=\"stylesheet\" type=\"text/css\" />");

            // Add any viewer specific tags that need to reside within the HTML head
            if (pageViewer != null)
                pageViewer.Write_Within_HTML_Head(Output, Tracer);

            // Add a thumbnail to this item
            if ((currentItem != null) && ( !String.IsNullOrEmpty(currentItem.Behaviors.Main_Thumbnail)))
            {
                string image_src = currentItem.Web.Source_URL + "/" + currentItem.Behaviors.Main_Thumbnail;

                Output.WriteLine("  <link rel=\"image_src\" href=\"" + image_src.Replace("\\", "/").Replace("//", "/").Replace("http:/", "http://") + "\" />");
            }

            // This is used for the TOC
            Output.WriteLine("  <link rel=\"stylesheet\" href=\"" + UI_ApplicationCache_Gateway.Configuration.UI.StaticResources.Jstree_Css + "\" />");

            // Also, add any stylehsheets in the layout
            if ((itemLayoutConfig != null) && (itemLayoutConfig.Stylesheets != null) && (itemLayoutConfig.Stylesheets.Count > 0))
            {
                Output.WriteLine();

                // Add each stylesheet
                foreach (StylesheetConfig cssConfig in itemLayoutConfig.Stylesheets)
                {
                    if (!String.IsNullOrEmpty(cssConfig.Media))
                    {
                        Output.WriteLine("  <link rel=\"stylesheet\" href=\"" + cssConfig.Source + "\" media=\"" + cssConfig.Media + "\" />");
                    }
                    else
                    {
                        Output.WriteLine("  <link rel=\"stylesheet\" href=\"" + cssConfig.Source + "\" />");
                    }
                }
            }

            // Add any additional things into the head
            if ((UI_ApplicationCache_Gateway.Configuration.UI.WriterViewers.Items.HtmlHeadWriters != null) && (UI_ApplicationCache_Gateway.Configuration.UI.WriterViewers.Items.HtmlHeadWriters.Count > 0 ))
            {
                Output.WriteLine();

                // Step through each config
                foreach (HtmlHeadWriterConfig thisWriterConfig in UI_ApplicationCache_Gateway.Configuration.UI.WriterViewers.Items.HtmlHeadWriters)
                {
                    // If disabled, skip it
                    if (!thisWriterConfig.Enabled) continue;

                    // Get the writer
                    iItemHtmlHeadWriter writer = ItemHtmlHeadWriter_Factory.Get_ItemHtmlHeadWriter(thisWriterConfig.Assembly, thisWriterConfig.Class);
                    if (writer == null)
                    {
                        Tracer.Add_Trace("Item_HtmlSubwriter.Write_Within_HTML_Head", "Writer returned from factory was null for " + thisWriterConfig.ID);
                        continue;
                    }

                    // Add the HTML
                    writer.Write_Within_HTML_Head(Output, pageViewer, currentItem, RequestSpecificValues);
                }
            }
        }

		/// <summary> Gets the CSS class of the container that the page is wrapped within </summary>
		/// <value> Always returns an empty string </value>
		public override string Container_CssClass
		{
			get { return String.Empty; }
		}
    }
}
