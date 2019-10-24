﻿#region Using directives

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using SobekCM.Core.Aggregations;
using SobekCM.Core.Client;
using SobekCM.Core.Configuration.Localization;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.Navigation;
using SobekCM.Core.Results;
using SobekCM.Core.UI_Configuration;
using SobekCM.Core.UI_Configuration.StaticResources;
using SobekCM.Core.Users;
using SobekCM.Core.WebContent;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Library.AggregationViewer;
using SobekCM.Library.AggregationViewer.HtmlHeadWriters;
using SobekCM.Library.AggregationViewer.Viewers;
using SobekCM.Library.Database;
using SobekCM.Library.Email;
using SobekCM.Library.Helpers.CKEditor;
using SobekCM.Library.UI;
using SobekCM.Tools;

#endregion

namespace SobekCM.Library.HTML
{
    /// <summary> Aggregation html subwriter renders all views of item aggregationPermissions, including home pages, searches, and browses </summary>
    /// <remarks> This class extends the <see cref="abstractHtmlSubwriter"/> abstract class. </remarks>
    public class Aggregation_HtmlSubwriter : abstractHtmlSubwriter
    {
        private readonly abstractAggregationViewer collectionViewer;
        private List<HtmlSubwriter_Behaviors_Enum> behaviors;
        private string leftButtons;
        private string rightButtons;
        private const int RESULTS_PER_PAGE = 20;
        private bool children_icons_added;

        private readonly Item_Aggregation hierarchyObject;
        private readonly HTML_Based_Content staticBrowse;
        private string browse_info_display_text;
        private readonly Item_Aggregation_Child_Page thisBrowseObject;
        private readonly Search_Results_Statistics datasetBrowseResultsStats;
        private readonly List<iSearch_Title_Result> pagedResults;

        private bool canEditHomePage;
        private bool ifEditNoCkEditor;

        /// <summary> Constructor creates a new instance of the Aggregation_HtmlSubwriter class </summary>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        public Aggregation_HtmlSubwriter(RequestCache RequestSpecificValues) : base(RequestSpecificValues)
        {
            // Get the item aggregation from the SobekCM engine endpoints
            // If the mode is NULL or the request was already completed, do nothing
            if ((RequestSpecificValues.Current_Mode == null) || (RequestSpecificValues.Current_Mode.Request_Completed))
                return;

            if (RequestSpecificValues.Current_Mode.Info_Browse_Mode==null || RequestSpecificValues.Current_Mode.Info_Browse_Mode == String.Empty)
            {
                RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Constructor", "No Current_Mode.Info_Browse_Mode.");
            }
            else
            {
                RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Constructor", "Current_Mode.Info_Browse_Mode=[" + RequestSpecificValues.Current_Mode.Info_Browse_Mode + "] (" + RequestSpecificValues.Current_Mode.Info_Browse_Mode.Length + ").");
            }

            RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Constructor", "Retrieving aggregation info");

            if (RequestSpecificValues.Current_Mode.Aggregation == String.Empty)
            {
                RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Constructor", "No Current_Mode.Aggregation.");
            }
            else
            {
                RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Constructor", "Current_Mode.Aggregation=[" + RequestSpecificValues.Current_Mode.Aggregation + "].");
            }

            // Check that the current aggregation code is valid
            if (!UI_ApplicationCache_Gateway.Aggregations.isValidCode(RequestSpecificValues.Current_Mode.Aggregation))
            {
                // Is there a "forward value"
                if (UI_ApplicationCache_Gateway.Collection_Aliases.ContainsKey(RequestSpecificValues.Current_Mode.Aggregation))
                {
                    RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Constructor", "There is a forward.");

                    RequestSpecificValues.Current_Mode.Aggregation = UI_ApplicationCache_Gateway.Collection_Aliases[RequestSpecificValues.Current_Mode.Aggregation];
                }
            }
            else
            {
                RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Constructor", "Current aggregation code is valid.");
            }

            // Use the method in the base class to actually pull the entire hierarchy
            if (!Get_Collection(RequestSpecificValues.Current_Mode, RequestSpecificValues.Tracer, out hierarchyObject))
            {
                RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Error;
                return;
            }

            // Determine if the user can edit this
            canEditHomePage = (RequestSpecificValues.Current_User != null) && (RequestSpecificValues.Current_User.Is_Aggregation_Admin(hierarchyObject.Code));

            // Special code to determine if CKEditor should be used since it kind of destroys some more custom HTML
            ifEditNoCkEditor = false;
            if ((UI_ApplicationCache_Gateway.Settings.System.System_Abbreviation == "OPENNJ") &&
                ((RequestSpecificValues.Current_Mode.Aggregation.Length == 0) || (RequestSpecificValues.Current_Mode.Aggregation.ToLower() == "all")))
                ifEditNoCkEditor = true;

                // Look for a user setting for 'Aggregation_HtmlSubwriter.Can_Edit_Home_Page' and if that included the aggregation code,
                // this non-admin user can edit the home page.
                if ((!canEditHomePage) && (RequestSpecificValues.Current_User != null) && (RequestSpecificValues.Current_Mode.Aggregation.Length > 0))
            {
                string possible_setting = "|" + (RequestSpecificValues.Current_User.Get_Setting("Aggregation_HtmlSubwriter.Can_Edit_Home_Page", String.Empty)).ToUpper() + "|";

                if (possible_setting.Contains("|" + RequestSpecificValues.Current_Mode.Aggregation.ToUpper() + "|"))
                {
                    RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Constructor", "User can edit home page.");
                    canEditHomePage = true;
                }
            }

            //// Check if a differente skin should be used if this is a collection display
            //string current_skin_code = RequestSpecificValues.Current_Mode.Skin.ToUpper();
            //if ((hierarchyObject != null) && (hierarchyObject.Web_Skins != null) && (hierarchyObject.Web_Skins.Count > 0))
            //{
            //    // Do NOT do this replacement if the web skin is in the URL and this is admin mode
            //    if ((!RequestSpecificValues.Current_Mode.Skin_In_URL) || (RequestSpecificValues.Current_Mode.Mode != Display_Mode_Enum.Administrative))
            //    {
            //        if (!hierarchyObject.Web_Skins.Contains(current_skin_code.ToLower()))
            //        {
            //            RequestSpecificValues.Current_Mode.Skin = hierarchyObject.Web_Skins[0];
            //        }
            //    }
            //}
            
            // Run the browse/info work if it is of those modes
            if (((RequestSpecificValues.Current_Mode.Aggregation_Type == Aggregation_Type_Enum.Browse_Info) || (RequestSpecificValues.Current_Mode.Aggregation_Type == Aggregation_Type_Enum.Child_Page_Edit)))
            {
                RequestSpecificValues.Tracer.Add_Trace("SobekCM_Page_Globals.Browse_Info_Block", "Retrieiving Browse/Info Object");

                // If this is a robot, then get the text from the static page
                if ((RequestSpecificValues.Current_Mode.Is_Robot) && (RequestSpecificValues.Current_Mode.Info_Browse_Mode == "all"))
                {
                    SobekCM_Assistant assistant = new SobekCM_Assistant();
                    browse_info_display_text = assistant.Get_All_Browse_Static_HTML(RequestSpecificValues.Current_Mode, RequestSpecificValues.Tracer);
                    RequestSpecificValues.Current_Mode.Writer_Type = Writer_Type_Enum.HTML_Echo;
                }
                else
                {
                    if (!Get_Browse_Info(RequestSpecificValues.Current_Mode, hierarchyObject, UI_ApplicationCache_Gateway.Settings.Servers.Base_Directory, RequestSpecificValues.Current_User, RequestSpecificValues.Tracer, out thisBrowseObject, out datasetBrowseResultsStats, out pagedResults, out staticBrowse))
                    {
                        RequestSpecificValues.Current_Mode.Aggregation_Type = Aggregation_Type_Enum.Home;
                        //RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Error;
                    }
                }
            }

            // Set some basic values
            leftButtons = String.Empty;
            rightButtons = String.Empty;
            children_icons_added = false;

			// Check to see if the user should be able to edit the home page
			if ((RequestSpecificValues.Current_Mode.Mode == Display_Mode_Enum.Aggregation) && (RequestSpecificValues.Current_Mode.Aggregation_Type == Aggregation_Type_Enum.Home_Edit))
			{
				if ( RequestSpecificValues.Current_User == null )
					RequestSpecificValues.Current_Mode.Aggregation_Type = Aggregation_Type_Enum.Home;
				else
				{
					if (!canEditHomePage )
					{
						RequestSpecificValues.Current_Mode.Aggregation_Type = Aggregation_Type_Enum.Home;
					}
				}
			}
			else if ( RequestSpecificValues.Current_Mode.Aggregation_Type == Aggregation_Type_Enum.Home_Edit )
				RequestSpecificValues.Current_Mode.Aggregation_Type = Aggregation_Type_Enum.Home;

            #region Handle post backs from the mySobek sharing, emailin, etc.. buttons

            RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Constructor", "Handling post backs from mySobek.");

            NameValueCollection form = HttpContext.Current.Request.Form;

            if ( form["item_action"] != null)
            {
                string action = form["item_action"].ToLower().Trim();

                if ((action == "add_aggregation") && ( RequestSpecificValues.Current_User != null ))
                {
                    SobekCM_Database.User_Set_Aggregation_Home_Page_Flag(RequestSpecificValues.Current_User.UserID, hierarchyObject.ID, true, RequestSpecificValues.Tracer);
                    RequestSpecificValues.Current_User.Set_Aggregation_Home_Page_Flag(hierarchyObject.Code, hierarchyObject.Name, true);
                    HttpContext.Current.Session.Add("ON_LOAD_MESSAGE", "Added aggregation to your home page");
                }

                if (( action == "remove_aggregation") && ( RequestSpecificValues.Current_User != null ))
                {
                    int removeAggregationID = hierarchyObject.ID;
                    string remove_code = hierarchyObject.Code;
                    string remove_name = hierarchyObject.Name;

                    if ((form["aggregation"] != null) && (form["aggregation"].Length > 0))
                    {
                        Item_Aggregation_Related_Aggregations aggrInfo = UI_ApplicationCache_Gateway.Aggregations[form["aggregation"]];
                        if (aggrInfo != null)
                        {
                            remove_code = aggrInfo.Code;
                            removeAggregationID = aggrInfo.ID;
                        }
                    }

                    SobekCM_Database.User_Set_Aggregation_Home_Page_Flag(RequestSpecificValues.Current_User.UserID, removeAggregationID, false, RequestSpecificValues.Tracer);
                    RequestSpecificValues.Current_User.Set_Aggregation_Home_Page_Flag(remove_code, remove_name, false);

                    if (RequestSpecificValues.Current_Mode.Home_Type != Home_Type_Enum.Personalized)
                    {
                        HttpContext.Current.Session.Add("ON_LOAD_MESSAGE", "Removed aggregation from your home page");
                    }
                }

                if ((action == "private_folder") && ( RequestSpecificValues.Current_User != null ))
                {
                    User_Folder thisFolder = RequestSpecificValues.Current_User.Get_Folder(form["aggregation"]);
                    if (SobekCM_Database.Edit_User_Folder(thisFolder.Folder_ID, RequestSpecificValues.Current_User.UserID, -1, thisFolder.Folder_Name, false, String.Empty, RequestSpecificValues.Tracer) >= 0)
                        thisFolder.IsPublic = false;
                }

                if ((action == "email") && ( RequestSpecificValues.Current_User != null ))
                {
                    string address = form["email_address"].Replace(";", ",").Trim();
                    string comments = form["email_comments"].Trim();
                    string format = form["email_format"].Trim().ToUpper();

                    if (address.Length > 0)
                    {
                        // Determine the email format
                        bool is_html_format = format != "TEXT";

                        // CC: the user, unless they are already on the list
                        string cc_list = RequestSpecificValues.Current_User.Email;
                        if (address.ToUpper().IndexOf(RequestSpecificValues.Current_User.Email.ToUpper()) >= 0)
                            cc_list = String.Empty;

                        // Send the email
                        string any_error = URL_Email_Helper.Send_Email(address, cc_list, comments, RequestSpecificValues.Current_User.Full_Name, RequestSpecificValues.Current_Mode.Instance_Abbreviation, is_html_format, HttpContext.Current.Items["Original_URL"].ToString(), hierarchyObject.Name, "Collection", RequestSpecificValues.Current_User.UserID);
                        HttpContext.Current.Session.Add("ON_LOAD_MESSAGE", any_error.Length > 0 ? any_error : "Your email has been sent");

                        RequestSpecificValues.Current_Mode.isPostBack = true;

                        // Do this to force a return trip (cirumnavigate cacheing)
                        string original_url = HttpContext.Current.Items["Original_URL"].ToString();
                        if (original_url.IndexOf("?") < 0)
                            HttpContext.Current.Response.Redirect(original_url + "?p=" + DateTime.Now.Millisecond, false);
                        else
                            HttpContext.Current.Response.Redirect(original_url + "&p=" + DateTime.Now.Millisecond, false);

                        HttpContext.Current.ApplicationInstance.CompleteRequest();
                        RequestSpecificValues.Current_Mode.Request_Completed = true;
                        return;
                    }
                }
            }

            #endregion

            #region Handle post backs from editing the home page text 

            RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Constructor", "Handle post backs from editing home page text.");

            if (( RequestSpecificValues.Current_Mode.Aggregation_Type == Aggregation_Type_Enum.Home_Edit ) && ( form["sbkAghsw_HomeTextEdit"] != null))
			{
				string aggregation_folder = UI_ApplicationCache_Gateway.Settings.Servers.Base_Design_Location + "aggregations\\" + hierarchyObject.Code + "\\";
			    if (!Directory.Exists(aggregation_folder))
			        Directory.CreateDirectory(aggregation_folder);

                string file = hierarchyObject.HomePageHtml.Source;

				// Make a backup from today, if none made yet
				if (File.Exists(file))
				{
					DateTime lastWrite = (new FileInfo(file)).LastWriteTime;
					string new_file = file.ToLower().Replace(".txt", "").Replace(".html", "").Replace(".htm", "") + lastWrite.Year + lastWrite.Month.ToString().PadLeft(2, '0') + lastWrite.Day.ToString() .PadLeft(2, '0')+ ".bak";
					if (File.Exists(new_file))
						File.Delete(new_file);
					File.Move(file, new_file);
				}
				else
				{
				    // Ensure the folder exists
                    string directory = Path.GetDirectoryName(file);
				    if (!Directory.Exists(directory))
				        Directory.CreateDirectory(directory);
				}

				// Write to the file now
				StreamWriter homeWriter = new StreamWriter(file, false);
			    homeWriter.WriteLine(form["sbkAghsw_HomeTextEdit"].Replace("%]", "%>").Replace("[%", "<%"));
				homeWriter.Flush();
				homeWriter.Close();

				// Also save this change
				SobekCM_Database.Save_Item_Aggregation_Milestone(hierarchyObject.Code, "Home page edited (" + Web_Language_Enum_Converter.Enum_To_Name(RequestSpecificValues.Current_Mode.Language) + ")", RequestSpecificValues.Current_User.Full_Name);

				// Clear this aggreation from the cache
                CachedDataManager.Aggregations.Remove_Item_Aggregation(hierarchyObject.Code, RequestSpecificValues.Tracer);

                // If this is all, save the new text as well
                if (String.Compare("all", hierarchyObject.Code, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    string home_app_key = "SobekCM_Home_" + RequestSpecificValues.Current_Mode.Language_Code;
                    HttpContext.Current.Application[home_app_key] = form["sbkAghsw_HomeTextEdit"].Replace("%]", "%>").Replace("[%", "<%");
                }

				// Forward along
				RequestSpecificValues.Current_Mode.Aggregation_Type = Aggregation_Type_Enum.Home;
				string redirect_url = UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode);
				if (redirect_url.IndexOf("?") > 0)
					redirect_url = redirect_url + "&refresh=always";
				else
					redirect_url = redirect_url + "?refresh=always";
				RequestSpecificValues.Current_Mode.Request_Completed = true;
				HttpContext.Current.Response.Redirect(redirect_url, false);
				HttpContext.Current.ApplicationInstance.CompleteRequest();

				return;
            }

            #endregion

            // If this is a search, verify it is a valid search type
            if (RequestSpecificValues.Current_Mode.Mode == Display_Mode_Enum.Search)
            {
                RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Constructor", "Is a search.");

                // Not every collection has every search type...
                ReadOnlyCollection<Search_Type_Enum> possibleSearches = hierarchyObject.Search_Types;

                if (!possibleSearches.Contains(RequestSpecificValues.Current_Mode.Search_Type))
                {
                    bool found_valid = false;

                    if ((RequestSpecificValues.Current_Mode.Search_Type == Search_Type_Enum.Full_Text) && (possibleSearches.Contains(Search_Type_Enum.dLOC_Full_Text)))
                    {
                        found_valid = true;
                        RequestSpecificValues.Current_Mode.Search_Type = Search_Type_Enum.dLOC_Full_Text;
                        RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Constructor", "Search_Type_Enum.dLOC_Full_Text.");
                    }

                    if ((!found_valid) && (RequestSpecificValues.Current_Mode.Search_Type == Search_Type_Enum.Basic) && (possibleSearches.Contains(Search_Type_Enum.Newspaper)))
                    {
                        found_valid = true;
                        RequestSpecificValues.Current_Mode.Search_Type = Search_Type_Enum.Newspaper;
                        RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Constructor", "Search_Type_Enum.Newspaper.");
                    }

                    if (( !found_valid ) && ( possibleSearches.Count > 0 ))
                    {
                        found_valid = true;
                        RequestSpecificValues.Current_Mode.Search_Type = possibleSearches[0];
                        RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Constructor", "serch_type possibleSearchs[0].");
                    }

                    if ( !found_valid )
                    {
						RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Aggregation;
						RequestSpecificValues.Current_Mode.Aggregation_Type = Aggregation_Type_Enum.Home;
                        RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Constructor", "Not found valid, going to home.");
                    }
                }
            }

            #region Create the new subviewer to handle this request

            RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Constructor", "Creating the new subviewer to handle the request.");

            // First, create the aggregation view bag
            AggregationViewBag viewBag = new AggregationViewBag(hierarchyObject, datasetBrowseResultsStats, pagedResults, thisBrowseObject, staticBrowse);

            if (RequestSpecificValues.Current_Mode.Mode == Display_Mode_Enum.Search)
            {
                collectionViewer = AggregationViewer_Factory.Get_Viewer(RequestSpecificValues.Current_Mode.Search_Type, RequestSpecificValues, viewBag);
            }

			if (RequestSpecificValues.Current_Mode.Mode == Display_Mode_Enum.Aggregation)
			{
                RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Constructor", "Current_Mode.Mode is Display_Mode_Enum.Aggregation.");

                switch (RequestSpecificValues.Current_Mode.Aggregation_Type)
				{
					case Aggregation_Type_Enum.Home:
					case Aggregation_Type_Enum.Home_Edit:
                        RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Constructor", "Home or Home_Edit agg type.");

                        if (!hierarchyObject.Custom_Home_Page)
                        {
                            RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Constructor", "Not a hierarchyObject.Custom_Home_Page.");
                            // Are there tiles here?
                            string aggregation_tile_directory = Path.Combine(UI_ApplicationCache_Gateway.Settings.Servers.Base_Design_Location, hierarchyObject.ObjDirectory, "images", "tiles");
                            if (Directory.Exists(aggregation_tile_directory))
                            {
                                RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Constructor", "Not a hierarchyObject.Custom_Home_Page and aggregation_title_directory=[" + aggregation_tile_directory + "].");
                                string[] jpeg_tiles = Directory.GetFiles(aggregation_tile_directory, "*.jpg");
                                if (jpeg_tiles.Length > 0)
                                {
                                    collectionViewer = new Tiles_Home_AggregationViewer(RequestSpecificValues, viewBag);
                                    RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Constructor", "There are " + jpeg_tiles.Length + " jpeg files.");
                                }
                            }

                            // If the tiles home page was not built, build the standard viewer
                            if (collectionViewer == null)
                            {
                                RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Constructor", "collectionViewer was not built, building.");
                                collectionViewer = AggregationViewer_Factory.Get_Viewer(hierarchyObject.Views_And_Searches[0], RequestSpecificValues, viewBag);
                            }
                        }
                        else
                        {
                            RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Constructor", "Is a hierarchyObject.Custom_Home_Page.");
                            collectionViewer = new Custom_Home_Page_AggregationViewer(RequestSpecificValues, viewBag);
                        }
						break;

					case Aggregation_Type_Enum.Browse_Info:
                        RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Constructor", "Browse_Info agg type.");

                        if (datasetBrowseResultsStats == null)
						{
                            RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Constructor", "datasetBrowseResultsStats was null, creating new static browse. ");
                            collectionViewer = new Static_Browse_Info_AggregationViewer(RequestSpecificValues, viewBag);
						}
						else
						{
                            RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Constructor", "datasetBrowseResultsStats was not null, creating new dataset browse. " + datasetBrowseResultsStats.Total_Items + " total items.");
                            collectionViewer = new DataSet_Browse_Info_AggregationViewer(RequestSpecificValues, viewBag);
						}
						break;

					case Aggregation_Type_Enum.Child_Page_Edit:
                        RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Constructor", "Child_Page_Edit agg type.");

                       collectionViewer = new Static_Browse_Info_AggregationViewer(RequestSpecificValues, viewBag);
						break;

					case Aggregation_Type_Enum.Browse_By:
                        RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Constructor", "Browse_By agg type.");

                       collectionViewer = new Metadata_Browse_AggregationViewer(RequestSpecificValues, viewBag);
						break;

					case Aggregation_Type_Enum.Browse_Map:
                        RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Constructor", "Browse_Map agg type.");

                       collectionViewer = new Map_Browse_AggregationViewer(RequestSpecificValues, viewBag);
						break;

                    case Aggregation_Type_Enum.Browse_Map_Beta:
                        RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Constructor", "Browse_Map_Beta agg type.");

                       collectionViewer = new Map_Browse_AggregationViewer_Beta(RequestSpecificValues, viewBag);
                        break;

					case Aggregation_Type_Enum.Item_Count:
                        RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Constructor", "Item_Count agg type.");

                       collectionViewer = new Item_Count_AggregationViewer(RequestSpecificValues, viewBag);
						break;

					case Aggregation_Type_Enum.Usage_Statistics:
                        RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Constructor", "Usage_Statistics agg type.");

                       collectionViewer = new Usage_Statistics_AggregationViewer(RequestSpecificValues, viewBag);
						break;

					case Aggregation_Type_Enum.Private_Items:
                        RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Constructor", "Private_Items agg type.");

                       collectionViewer = new Private_Items_AggregationViewer(RequestSpecificValues, viewBag);
						break;

                    case Aggregation_Type_Enum.Manage_Menu:
                        RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Constructor", "Manage_Menu agg type.");

                       collectionViewer = new Manage_Menu_AggregationViewer(RequestSpecificValues, viewBag);
                        break;

                    case Aggregation_Type_Enum.User_Permissions:
                        RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Constructor", "User_Permissions agg type.");

                       collectionViewer = new User_Permissions_AggregationViewer(RequestSpecificValues, viewBag);
                        break;

                    case Aggregation_Type_Enum.Work_History:
                        RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Constructor", "Work_History agg type.");

                       collectionViewer = new Work_History_AggregationViewer(RequestSpecificValues, viewBag);
                        break;

                    case Aggregation_Type_Enum.Empty:
                        RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Constructor", "Empty agg type.");

                       collectionViewer = new Empty_AggregationViewer(RequestSpecificValues, viewBag);
                        break;
				}
			}

            // If execution should end, do it now
            if (RequestSpecificValues.Current_Mode.Request_Completed)
                return;

            if (collectionViewer != null)
            {
                // Pull the standard values
                switch (collectionViewer.Selection_Panel_Display)
                {
                    case Selection_Panel_Display_Enum.Selectable:
                        if (form["show_subaggrs"] != null)
                        {
                            string show_subaggrs = form["show_subaggrs"].ToUpper();
                            if (show_subaggrs == "TRUE")
                                RequestSpecificValues.Current_Mode.Show_Selection_Panel = true;
                        }
                        break;

                    case Selection_Panel_Display_Enum.Always:
                        RequestSpecificValues.Current_Mode.Show_Selection_Panel = true;
                        break;
                }

                behaviors = collectionViewer.AggregationViewer_Behaviors;
            }
            else
            {
                behaviors = emptybehaviors;
            }

            #endregion
        }

        /// <summary> Gets the collection of special behaviors which this subwriter
        /// requests from the main HTML subwriter. </summary>
        /// <remarks> By default, this returns an empty list </remarks>
        public override List<HtmlSubwriter_Behaviors_Enum> Subwriter_Behaviors
        {
            get
            {
                // When editing the aggregation details, the banner should be included here
                if (  collectionViewer == null )
                    return new List<HtmlSubwriter_Behaviors_Enum> { HtmlSubwriter_Behaviors_Enum.Include_Skip_To_Main_Content_Link };

                List<HtmlSubwriter_Behaviors_Enum> subviewer_behavior = collectionViewer.AggregationViewer_Behaviors;

                if (!subviewer_behavior.Contains(HtmlSubwriter_Behaviors_Enum.Include_Skip_To_Main_Content_Link))
                    subviewer_behavior.Add(HtmlSubwriter_Behaviors_Enum.Include_Skip_To_Main_Content_Link);
                return subviewer_behavior;
            }
        }

        /// <summary> Write any additional values within the HTML Head of the
        /// final served page </summary>
        /// <param name="Output"> Output stream currently within the HTML head tags </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <remarks> By default this does nothing, but can be overwritten by all the individual html subwriters </remarks>
        public override void Write_Within_HTML_Head(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace("Aggregation_HtmlSubwriter.Write_Within_HTML_Header", "Entered...");
            Output.WriteLine("<!-- Start of Aggregation_HtmlSubwriter.Write_Within_HTML_Header -->");

            // Based on display mode, add ROBOT instructions
            switch (RequestSpecificValues.Current_Mode.Mode)
            {
                case Display_Mode_Enum.Search:
                    Output.WriteLine("  <meta name=\"robots\" content=\"index, follow\" />");
                    break;

				case Display_Mode_Enum.Aggregation:
					if (( RequestSpecificValues.Current_Mode.Aggregation_Type == Aggregation_Type_Enum.Home ) || ( RequestSpecificValues.Current_Mode.Aggregation_Type == Aggregation_Type_Enum.Browse_Info ))
						Output.WriteLine("  <meta name=\"robots\" content=\"index, follow\" />");
					else
						Output.WriteLine("  <meta name=\"robots\" content=\"noindex, nofollow\" />");
		            break;

                default:
                    Output.WriteLine("  <meta name=\"robots\" content=\"noindex, nofollow\" />");
                    break;
            }

			// If this was to display the static page, include that info in the header as well
            if (staticBrowse != null)
			{
                if ( !String.IsNullOrEmpty(staticBrowse.Description))
				{
                    Output.WriteLine("  <meta name=\"description\" content=\"" + staticBrowse.Description.Replace("\"", "'") + "\" />");
				}
                if (!String.IsNullOrEmpty(staticBrowse.Keywords))
				{
                    Output.WriteLine("  <meta name=\"keywords\" content=\"" + staticBrowse.Keywords.Replace("\"", "'") + "\" />");
				}
                if (!String.IsNullOrEmpty(staticBrowse.Author))
				{
                    Output.WriteLine("  <meta name=\"author\" content=\"" + staticBrowse.Author.Replace("\"", "'") + "\" />");
				}
                if (!String.IsNullOrEmpty(staticBrowse.Date))
				{
                    Output.WriteLine("  <meta name=\"date\" content=\"" + staticBrowse.Date.Replace("\"", "'") + "\" />");
				}

                if (!String.IsNullOrEmpty(staticBrowse.Extra_Head_Info))
				{
                    Output.WriteLine("  " + staticBrowse.Extra_Head_Info.Trim());
				}
			}

            // In the home mode, add the open search XML file to allow users to add SobekCM as a default search in browsers
            if (( RequestSpecificValues.Current_Mode.Mode == Display_Mode_Enum.Aggregation ) && (RequestSpecificValues.Current_Mode.Aggregation_Type == Aggregation_Type_Enum.Home))
            {
                Output.WriteLine("  <link rel=\"search\" href=\"" + RequestSpecificValues.Current_Mode.Base_URL + "default/opensearch.xml\" type=\"application/opensearchdescription+xml\"  title=\"Add " + RequestSpecificValues.Current_Mode.Instance_Abbreviation + " Search\" />");

                if (RequestSpecificValues.Current_Mode.Home_Type == Home_Type_Enum.Tree)
                {
                    Output.WriteLine("  <link rel=\"stylesheet\" href=\"" + Static_Resources_Gateway.Jstree_Css + "\" />");
                }
            }

			// If this is to edit the home page, add the html editor
	        if ((RequestSpecificValues.Current_Mode.Mode == Display_Mode_Enum.Aggregation) && (RequestSpecificValues.Current_Mode.Aggregation_Type == Aggregation_Type_Enum.Home_Edit))
	        {
                // In some cases, we skip CKEditor
                if (!ifEditNoCkEditor)
                {
                    // Determine the aggregation upload directory
                    string aggregation_upload_dir = UI_ApplicationCache_Gateway.Settings.Servers.Base_Design_Location + "aggregations\\" + hierarchyObject.Code + "\\uploads";
                    string aggregation_upload_url = UI_ApplicationCache_Gateway.Settings.Servers.System_Base_URL + "design/aggregations/" + hierarchyObject.Code + "/uploads/";

                    // Create the CKEditor object
                    CKEditor editor = new CKEditor
                    {
                        BaseUrl = RequestSpecificValues.Current_Mode.Base_URL,
                        Language = RequestSpecificValues.Current_Mode.Language,
                        TextAreaID = "sbkAghsw_HomeTextEdit",
                        FileBrowser_ImageUploadUrl = RequestSpecificValues.Current_Mode.Base_URL + "HtmlEditFileHandler.ashx",
                        UploadPath = aggregation_upload_dir,
                        UploadURL = aggregation_upload_url
                    };

                    // If there are existing files, add a reference to the URL for the image browser
                    if ((Directory.Exists(aggregation_upload_dir)) && (Directory.GetFiles(aggregation_upload_dir).Length > 0))
                    {
                        // Is there an endpoint defined for looking at uploaded files?
                        string upload_files_json_url = SobekEngineClient.Aggregations.Aggregation_Uploaded_Files_URL;
                        if (!String.IsNullOrEmpty(upload_files_json_url))
                        {
                            editor.ImageBrowser_ListUrl = String.Format(upload_files_json_url, hierarchyObject.Code);
                        }
                    }

                    // Add the HTML from the CKEditor object
                    editor.Add_To_Stream(Output);
                }
	        }

			// If this is to edit the child page page, add the html editor
			if ((RequestSpecificValues.Current_Mode.Mode == Display_Mode_Enum.Aggregation) && (RequestSpecificValues.Current_Mode.Aggregation_Type == Aggregation_Type_Enum.Child_Page_Edit))
			{
                // In some cases, we skip CKEditor
                if (!ifEditNoCkEditor)
                {
                    // Determine the aggregation upload directory
                    string aggregation_upload_dir = UI_ApplicationCache_Gateway.Settings.Servers.Base_Design_Location + "aggregations\\" + hierarchyObject.Code + "\\uploads";
                    string aggregation_upload_url = UI_ApplicationCache_Gateway.Settings.Servers.System_Base_URL + "design/aggregations/" + hierarchyObject.Code + "/uploads/";

                    // Create the CKEditor object
                    CKEditor editor = new CKEditor
                    {
                        BaseUrl = RequestSpecificValues.Current_Mode.Base_URL,
                        Language = RequestSpecificValues.Current_Mode.Language,
                        TextAreaID = "sbkSbia_ChildTextEdit",
                        FileBrowser_ImageUploadUrl = RequestSpecificValues.Current_Mode.Base_URL + "HtmlEditFileHandler.ashx",
                        UploadPath = aggregation_upload_dir,
                        UploadURL = aggregation_upload_url
                    };

                    // Should this start as SOURCE?  
                    // Start in source mode, if this has a script or input reference
                    if ((staticBrowse != null) && (!String.IsNullOrEmpty(staticBrowse.Content)))
                    {
                        if ((staticBrowse.Content.IndexOf("<script", StringComparison.OrdinalIgnoreCase) >= 0) || (staticBrowse.Content.IndexOf("<input", StringComparison.OrdinalIgnoreCase) >= 0))
                            editor.Start_In_Source_Mode = true;
                    }

                    // If there are existing files, add a reference to the URL for the image browser
                    if ((Directory.Exists(aggregation_upload_dir)) && (Directory.GetFiles(aggregation_upload_dir).Length > 0))
                    {
                        // Is there an endpoint defined for looking at uploaded files?
                        string upload_files_json_url = SobekEngineClient.Aggregations.Aggregation_Uploaded_Files_URL;
                        if (!String.IsNullOrEmpty(upload_files_json_url))
                        {
                            editor.ImageBrowser_ListUrl = String.Format(upload_files_json_url, hierarchyObject.Code);
                        }
                    }

                    // Add the HTML from the CKEditor object
                    editor.Add_To_Stream(Output);
                }
			}

            // If this is the thumbnails results, add the QTIP script and css
            if ((datasetBrowseResultsStats != null ) && 
                ( datasetBrowseResultsStats.Total_Items > 0) &&
                ( String.Equals(RequestSpecificValues.Current_Mode.Result_Display_Type, "thumbs", StringComparison.OrdinalIgnoreCase)))
            {
                Output.WriteLine("  <script type=\"text/javascript\" src=\"" + Static_Resources_Gateway.Jquery_Qtip_Js + "\"></script>");
                Output.WriteLine("  <link rel=\"stylesheet\" type=\"text/css\" href=\"" + Static_Resources_Gateway.Jquery_Qtip_Css + "\" /> ");
            }

            // If this is an internal (semi-admin) view, add the admin CSS
            if ((collectionViewer != null) && (collectionViewer.Is_Internal_View))
            {
                Output.WriteLine("  <link rel=\"stylesheet\" type=\"text/css\" href=\"" + Static_Resources_Gateway.Sobekcm_Admin_Css + "\" /> ");
            }

            if ((collectionViewer != null) && (collectionViewer.AggregationViewer_Behaviors.Contains(HtmlSubwriter_Behaviors_Enum.Use_Jquery_DataTables)))
            {
                Output.WriteLine("  <link href=\"" + Static_Resources_Gateway.Sobekcm_Datatables_Css + "\" rel=\"stylesheet\" type=\"text/css\" />");
                Output.WriteLine("  <script type=\"text/javascript\" src=\"" + Static_Resources_Gateway.Jquery_Datatables_Js + "\" ></script>");
            }

            if ((collectionViewer != null) && (collectionViewer.AggregationViewer_Behaviors.Contains(HtmlSubwriter_Behaviors_Enum.Use_Jquery_Qtip)))
            {
                Output.WriteLine("  <link href=\"" + Static_Resources_Gateway.Jquery_Qtip_Css + "\" rel=\"stylesheet\" type=\"text/css\" />");
                Output.WriteLine("  <script type=\"text/javascript\" src=\"" + Static_Resources_Gateway.Jquery_Qtip_Js + "\" ></script>");
            }

            Output.WriteLine();

            // Add the aggregation html head writers
            DublinCore_AggregationHtmlHeadWriter dcWriter = new DublinCore_AggregationHtmlHeadWriter();
            Output.WriteLine("<!-- Start of DublinCore_AggregationHtmlHeaderWriter -->");
            dcWriter.Write_Within_HTML_Head(Output, hierarchyObject, RequestSpecificValues);
            Output.WriteLine("<!-- End of DublinCore_AggregationHtmlHeaderWriter -->");
            Output.WriteLine();

            JSON_AggregationHtmlHeadWriter jsonWriter = new JSON_AggregationHtmlHeadWriter();
            Output.WriteLine("<!-- Start of JSON_AggregationHtmlHeaderWriter -->");
            jsonWriter.Write_Within_HTML_Head(Output, hierarchyObject, RequestSpecificValues);
            Output.WriteLine("<!-- End of JSON_AggregationHtmlHeaderWriter -->");
            Output.WriteLine();

            Output.WriteLine("<!-- End of Aggregation_HtmlSubwriter.Write_Within_HTML_Header -->");
        }

        /// <summary> Chance for a final, final CSS which can override anything else, including the web skin </summary>
        public override string Final_CSS
        {
            get
            {
                // Finally add the aggregation-level CSS if it exists
                if ((hierarchyObject != null) && (!String.IsNullOrEmpty(hierarchyObject.CSS_File)))
                {
                    return "  <link href=\"" + RequestSpecificValues.Current_Mode.Base_Design_URL + "aggregations/" + hierarchyObject.Code + "/" + hierarchyObject.CSS_File + "\" rel=\"stylesheet\" type=\"text/css\" />";
                }
                return String.Empty;
            }
        }

        /// <summary> Title for this web page </summary>
        public override string WebPage_Title
        {
            get
            {
	            if (RequestSpecificValues.Current_Mode.Mode == Display_Mode_Enum.Search)
	            {
		            return (hierarchyObject != null) ? "{0} Search - " + hierarchyObject.Name : "{0} Search";
	            }
	            
				if (RequestSpecificValues.Current_Mode.Mode == Display_Mode_Enum.Aggregation)
	            {
		            switch (RequestSpecificValues.Current_Mode.Aggregation_Type)
		            {
			            case Aggregation_Type_Enum.Home:
				            if (hierarchyObject != null)
				            {
					            return (hierarchyObject.Code == "ALL") ? "{0} Home" : "{0} Home - " + hierarchyObject.Name;
				            }
				            return "{0} Home";

			            case Aggregation_Type_Enum.Browse_Info:
							if ( staticBrowse != null)
							{
                                if (staticBrowse.Title.Length > 0)
                                    return "{0} - " + staticBrowse.Title + " - " + hierarchyObject.Name;
								return "{0} - " + RequestSpecificValues.Current_Mode.Info_Browse_Mode + " - " + hierarchyObject.Name;
							}
							
							if (hierarchyObject != null)
							{
								return "{0} - " + hierarchyObject.Name;
							}

				            break;

						case Aggregation_Type_Enum.Child_Page_Edit:
							if (hierarchyObject != null)
							{
								return "{0} - Edit " + hierarchyObject.Name;
							}
							break;

						default:
				            return "{0} - " + hierarchyObject.Name;
		            }
	            }

	            // default
                return (hierarchyObject != null) ? "{0} - " + hierarchyObject.Name : "{0}";
            }
        }

        /// <summary> Gets the collection of body attributes to be included 
        /// within the HTML body tag (usually to add events to the body) </summary>
        public override List<Tuple<string, string>> Body_Attributes
        {
            get
            {
                List<Tuple<string, string>> returnValue = new List<Tuple<string, string>>();
	            if (RequestSpecificValues.Current_Mode.Mode == Display_Mode_Enum.Search)
	            {
		            if (RequestSpecificValues.Current_Mode.Search_Type == Search_Type_Enum.Map)
		            {
			            returnValue.Add(new Tuple<string, string>("onload", "load();"));
		            }
                    if (RequestSpecificValues.Current_Mode.Search_Type == Search_Type_Enum.Map_Beta)
                    {
                        returnValue.Add(new Tuple<string, string>("onload", "load();"));
                    }
				}
				else if (RequestSpecificValues.Current_Mode.Mode == Display_Mode_Enum.Aggregation)
				{
					switch (RequestSpecificValues.Current_Mode.Aggregation_Type)
					{
						case Aggregation_Type_Enum.Browse_Info:
							if ( String.Equals(RequestSpecificValues.Current_Mode.Result_Display_Type, "map", StringComparison.OrdinalIgnoreCase))
							{
								returnValue.Add(new Tuple<string, string>("onload", "load();"));
							}
							break;

						case Aggregation_Type_Enum.Browse_Map:
							returnValue.Add(new Tuple<string, string>("onload", "load();"));
							break;

                        case Aggregation_Type_Enum.Browse_Map_Beta:
                            returnValue.Add(new Tuple<string, string>("onload", "load();"));
                            break;
					}
				}

                return returnValue;
            }
        }

        /// <summary> Add the header to the output </summary>
        /// <param name="Output"> Stream to which to write the HTML for this header </param>
        public override void Add_Header(TextWriter Output)
        {
            RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Add_Header", "Entered. WebPage_Title=[" + WebPage_Title + "].");
            HeaderFooter_Helper_HtmlSubWriter.Add_Header(Output, RequestSpecificValues, Container_CssClass, WebPage_Title, Subwriter_Behaviors, hierarchyObject, null);
            RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Add_Header", "Leaving.");
        }

        #region Public method to write the internal header

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

                if ((RequestSpecificValues.Current_User.Is_Aggregation_Curator(RequestSpecificValues.Current_Mode.Aggregation)) || (RequestSpecificValues.Current_User.Can_Edit_All_Items(RequestSpecificValues.Current_Mode.Aggregation)))
                    return true;

                // Otherwise, do not show
                return false;
            }
        }

        /// <summary> Adds the internal header HTML for this specific HTML writer </summary>
        /// <param name="Output"> Stream to which to write the HTML for the internal header information </param>
        /// <param name="Current_User"> Currently logged on user, to determine specific rights </param>
        public override void Write_Internal_Header_HTML(TextWriter Output, User_Object Current_User)
        {
            RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Write_Internal_Header_HTML", "Entered...");
            Output.WriteLine("<!-- Start of Aggregation_HtmlSubwriter.Write_Internal_Header_HTML -->");

            if ((Current_User != null) && (    (Current_User.Is_Aggregation_Curator(RequestSpecificValues.Current_Mode.Aggregation)) 
                                            || (Current_User.Is_Internal_User) 
                                            || ( Current_User.Can_Edit_All_Items( RequestSpecificValues.Current_Mode.Aggregation ))))
	        {
                bool isAll = (RequestSpecificValues.Current_Mode.Aggregation.Length == 0) || (RequestSpecificValues.Current_Mode.Aggregation.ToUpper() == "ALL");

				Output.WriteLine("  <table id=\"sbk_InternalHeader\">");
                Output.WriteLine("    <tr style=\"height:45px;\">");
                Output.WriteLine("      <td style=\"text-align:left; width:100px;\">");
                Output.WriteLine("          <button title=\"Hide Internal Header\" class=\"intheader_button_aggr hide_intheader_button_aggr\" onclick=\"return hide_internal_header();\" alt=\"Hide Internal Header\"></button>");
                Output.WriteLine("      </td>");

                Output.WriteLine("      <td style=\"text-align:center; vertical-align:middle\">");

                // Add button to view private items
                Display_Mode_Enum displayMode = RequestSpecificValues.Current_Mode.Mode;
	            Aggregation_Type_Enum aggrType = RequestSpecificValues.Current_Mode.Aggregation_Type;
                string submode = RequestSpecificValues.Current_Mode.Info_Browse_Mode;

				RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Aggregation;
				RequestSpecificValues.Current_Mode.Aggregation_Type = Aggregation_Type_Enum.Private_Items;
                RequestSpecificValues.Current_Mode.Info_Browse_Mode = String.Empty;
                Output.WriteLine("          <button title=\"View Private Items\" class=\"intheader_button_aggr view_private_items\" onclick=\"window.location.href='" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "';return false;\" ></button>");

                // For the ALL top-level collectin, just send them to the top-level existing stats pages
	            if (isAll)
	            {
	                RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Statistics;

	                // Add button to view item count information
                    RequestSpecificValues.Current_Mode.Statistics_Type = Statistics_Type_Enum.Item_Count_Standard_View;
	                Output.WriteLine("          <button title=\"View Item Count\" class=\"intheader_button_aggr show_item_count\" onclick=\"window.location.href='" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "';return false;\"></button>");

                    // Add button to view usage statistics information
                    RequestSpecificValues.Current_Mode.Statistics_Type = Statistics_Type_Enum.Usage_Overall;
                    Output.WriteLine("          <button title=\"View Usage Statistics\" class=\"intheader_button_aggr show_usage_statistics\" onclick=\"window.location.href='" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "';return false;\"></button>");

                    // Add admin view is system administrator
                    if ((Current_User.Is_System_Admin) || (Current_User.Is_Portal_Admin) || (Current_User.Is_Aggregation_Curator(hierarchyObject.Code)))
                    {
                        // Add button to view manage menu
                        RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Aggregation;
                        RequestSpecificValues.Current_Mode.Aggregation_Type = Aggregation_Type_Enum.Manage_Menu;
                        Output.WriteLine("          <button title=\"View All Management Options\" class=\"intheader_button_aggr manage_aggr_button\" onclick=\"window.location.href='" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "';return false;\"></button>");

                        RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Administrative;
                        RequestSpecificValues.Current_Mode.Admin_Type = Admin_Type_Enum.Aggregation_Single;
                        string prevAggrCode = RequestSpecificValues.Current_Mode.Aggregation;
                        RequestSpecificValues.Current_Mode.Aggregation = "all";
                        Output.WriteLine("          <button title=\"Edit Administrative Information\" class=\"intheader_button_aggr admin_view_button\" onclick=\"window.location.href='" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "';return false;\" ></button>");
                        RequestSpecificValues.Current_Mode.Aggregation = prevAggrCode;
                    }
	            }
	            else
	            {
                    // Add button to view item count information
                    RequestSpecificValues.Current_Mode.Aggregation_Type = Aggregation_Type_Enum.Item_Count;
                    Output.WriteLine("          <button title=\"View Item Count\" class=\"intheader_button_aggr show_item_count\" onclick=\"window.location.href='" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "';return false;\"></button>");

                    // Add button to view usage statistics information
                    RequestSpecificValues.Current_Mode.Aggregation_Type = Aggregation_Type_Enum.Usage_Statistics;
                    Output.WriteLine("          <button title=\"View Usage Statistics\" class=\"intheader_button_aggr show_usage_statistics\" onclick=\"window.location.href='" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "';return false;\"></button>");

                    // Add admin view if user is a system administrator
                    if ((Current_User.Is_System_Admin) || (Current_User.Is_Portal_Admin) ||  (Current_User.Is_Aggregation_Curator(hierarchyObject.Code)))
                    {
                        // Add button to view manage menu
                        RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Aggregation;
                        RequestSpecificValues.Current_Mode.Aggregation_Type = Aggregation_Type_Enum.Manage_Menu;
                        Output.WriteLine("          <button title=\"View All Management Options\" class=\"intheader_button_aggr manage_aggr_button\" onclick=\"window.location.href='" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "';return false;\"></button>");

                        // Add the admin button
                        RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Administrative;
                        RequestSpecificValues.Current_Mode.Admin_Type = Admin_Type_Enum.Aggregation_Single;
                        Output.WriteLine("          <button title=\"Edit Administrative Information\" class=\"intheader_button_aggr admin_view_button\" onclick=\"window.location.href='" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "';return false;\" ></button>");
                    }
	            }

                Output.WriteLine("      </td>");

                RequestSpecificValues.Current_Mode.Info_Browse_Mode = submode;
                RequestSpecificValues.Current_Mode.Mode = displayMode;
	            RequestSpecificValues.Current_Mode.Aggregation_Type = aggrType;

                // Add the HELP icon next
                Output.WriteLine("      <td style=\"text-align:left; width:30px;\">");
				Output.WriteLine("        <span id=\"sbk_InternalHeader_Help\"><a href=\"" + UI_ApplicationCache_Gateway.Settings.System.Help_URL(RequestSpecificValues.Current_Mode.Base_URL) + "help/aggrheader\" title=\"Help regarding this header\" ><img src=\""+ Static_Resources_Gateway.Help_Button_Darkgray_Jpg + "\" alt=\"?\" title=\"Help regarding this header\" /></a></span>");
                Output.WriteLine("      </td>");

                Write_Internal_Header_Search_Box(Output);

                Output.WriteLine("    </tr>");
                
                Output.WriteLine("  </table>");
            }
            else
            {
                base.Write_Internal_Header_HTML(Output, Current_User);
            }

            Output.WriteLine("<!-- End of Aggregation_HtmlSubwriter.Write_Internal_Header_HTML -->");
        }

        #endregion

        #region Public method to write HTML to the output stream

        /// <summary> Writes the HTML generated by this aggregation html subwriter directly to the response stream </summary>
        /// <param name="Output"> Stream to which to write the HTML for this subwriter </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> Value indicating if html writer should finish the page immediately after this, or if there are other controls or routines which need to be called first </returns>
        public override bool Write_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace("Aggregation_HtmlSubwriter.Write_HTML", "Rendering HTML");
            Output.WriteLine("<!-- Start of Aggregation_HtmlSubwriter.Write_HTML -->");

            // Is this the custom home page viewer?
            if ((collectionViewer == null) && (hierarchyObject.Custom_Home_Page))
            {
                Tracer.Add_Trace("Aggregation_HtmlSubwriter.Write_HTML", "Custom home page viewer, writing hierarchyObject.HomePageHtml.Content.");

                Output.Write( hierarchyObject.HomePageHtml.Content);
                return true;
            }
			
			// Draw the banner and add links to the other views first
	        if ((collectionViewer.Type != Item_Aggregation_Views_Searches_Enum.Rotating_Highlight_Search) && (collectionViewer.Type != Item_Aggregation_Views_Searches_Enum.Custom_Home_Page))
	        {
                if (collectionViewer.Type != Item_Aggregation_Views_Searches_Enum.DataSet_Browse) 
		        {
			        // Add the main aggrgeation menu here
                    if (((!RequestSpecificValues.HTML_Skin.Suppress_Top_Navigation.HasValue) || (!RequestSpecificValues.HTML_Skin.Suppress_Top_Navigation.Value)) && (!behaviors.Contains(HtmlSubwriter_Behaviors_Enum.Suppress_MainMenu)))
                        MainMenus_Helper_HtmlSubWriter.Add_Aggregation_Main_Menu(Output, RequestSpecificValues, hierarchyObject);

                    // Start the page container
                    RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Write_HTML", "Start the page container div.");
					Output.WriteLine("<div id=\"pagecontainer\">");
					Output.WriteLine("<br />");
		        }
				else
				{
					// Add the main aggrgeation menu here
                    if (((!RequestSpecificValues.HTML_Skin.Suppress_Top_Navigation.HasValue) || (!RequestSpecificValues.HTML_Skin.Suppress_Top_Navigation.Value)) && (!behaviors.Contains(HtmlSubwriter_Behaviors_Enum.Suppress_MainMenu)))
                        MainMenus_Helper_HtmlSubWriter.Add_Aggregation_Search_Results_Menu(Output, RequestSpecificValues, hierarchyObject, false);

					// Start the (optional) page container
					Output.WriteLine("<div id=\"sbkAhs_ResultsPageContainer\">");
				}
	        }

            // If this is the map browse, end the page container here
            if (( RequestSpecificValues.Current_Mode.Mode == Display_Mode_Enum.Aggregation ) && ( RequestSpecificValues.Current_Mode.Aggregation_Type == Aggregation_Type_Enum.Browse_Map))
                Output.WriteLine("</div>");

            // If this is the map browse beta, end the page container here
            if ((RequestSpecificValues.Current_Mode.Mode == Display_Mode_Enum.Aggregation) && (RequestSpecificValues.Current_Mode.Aggregation_Type == Aggregation_Type_Enum.Browse_Map_Beta))
                Output.WriteLine("</div>");

            // Is there a script to be included?
            if ( !String.IsNullOrEmpty(collectionViewer.Search_Script_Reference))
                Output.WriteLine(collectionViewer.Search_Script_Reference);

            Tracer.Add_Trace("Aggregation_HtmlSubwriter.Write_HTML", "Write the search box.");

            // Write the search box
            if ((!behaviors.Contains(HtmlSubwriter_Behaviors_Enum.Suppress_SearchForm)) && ((collectionViewer == null ) || ( !collectionViewer.Is_Internal_View )))
            {
                string post_url = HttpUtility.HtmlEncode(HttpContext.Current.Items["Original_URL"].ToString());
                RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Write_HTML", "search box post_url=[" + post_url + "].");

                if ( !String.IsNullOrEmpty(collectionViewer.Search_Script_Action))
                {
                    Output.WriteLine("<form name=\"search_form\" method=\"post\" action=\"" + post_url + "\" id=\"addedForm\" >");
                }
                else
                {
                    Output.WriteLine("<form name=\"search_form\" method=\"post\" action=\"" + post_url + "\" id=\"addedForm\" >");
                }

                const string FORM_NAME = "search_form";

                if (( RequestSpecificValues.Current_Mode.Mode == Display_Mode_Enum.Aggregation ) && (( RequestSpecificValues.Current_Mode.Aggregation_Type == Aggregation_Type_Enum.Home ) || ( RequestSpecificValues.Current_Mode.Aggregation_Type == Aggregation_Type_Enum.Home_Edit )))
                {
                    // Determine the number of columns for text areas, depending on browser
                    int actual_cols = 50;

                    if (( !String.IsNullOrEmpty(RequestSpecificValues.Current_Mode.Browser_Type)) && (RequestSpecificValues.Current_Mode.Browser_Type.ToUpper().IndexOf("FIREFOX") >= 0))
                        actual_cols = 45;

                    // Add the hidden field
                    Output.WriteLine("<!-- Hidden field is used for postbacks to indicate what to save and reset -->");
                    Output.WriteLine("<input type=\"hidden\" id=\"item_action\" name=\"item_action\" value=\"\" />");
                    Output.WriteLine("<input type=\"hidden\" id=\"aggregation\" name=\"aggregation\" value=\"\" />");
                    Output.WriteLine("<input type=\"hidden\" id=\"show_subaggrs\" name=\"show_subaggrs\" value=\"" + RequestSpecificValues.Current_Mode.Show_Selection_Panel.ToString() + "\" />");

                    #region Email form

                    Tracer.Add_Trace("Aggregation_HtmlSubwriter.Write_HTML", "Email form.");

                    if ((RequestSpecificValues.Current_User != null) && ( RequestSpecificValues.Current_User.LoggedOn ))
                    {
                        Output.WriteLine("<!-- Email form -->");
						Output.WriteLine("<div class=\"form_email sbk_PopupForm\" id=\"form_email\" style=\"display:none;\">");
						Output.WriteLine("  <div class=\"sbk_PopupTitle\"><table style=\"width:100%\"><tr><td style=\"text-align:left\">Send this Collection to a Friend</td><td style=\"text-align:right\"> <a href=\"#template\" alt=\"CLOSE\" onclick=\"email_form_close()\">X</a> &nbsp; </td></tr></table></div>");
                        Output.WriteLine("  <br />");
                        Output.WriteLine("  <fieldset><legend>Enter the email information below &nbsp; </legend>");
                        Output.WriteLine("    <br />");
						Output.WriteLine("    <table class=\"sbk_PopupTable\">");

                        // Add email address line
                        Output.Write("      <tr><td style=\"width:80px\"><label for=\"email_address\">To:</label></td>");
						Output.WriteLine("<td><input class=\"email_input sbk_Focusable\" name=\"email_address\" id=\"email_address\" type=\"text\" value=\"" + RequestSpecificValues.Current_User.Email + "\" /></td></tr>");

                        // Add comments area
                        Output.Write("      <tr style=\"vertical-align:top\"><td><br /><label for=\"email_comments\">Comments:</label></td>");
						Output.WriteLine("<td><textarea rows=\"6\" cols=\"" + actual_cols + "\" name=\"email_comments\" id=\"email_comments\" class=\"email_textarea sbk_Focusable\" ></textarea></td></tr>");

                        // Add format area
						Output.Write("      <tr style=\"vertical-align:top\"><td>Format:</td>");
                        Output.Write("<td><input type=\"radio\" name=\"email_format\" id=\"email_format_html\" value=\"html\" checked=\"checked\" /> <label for=\"email_format_html\">HTML</label> &nbsp; &nbsp; ");
                        Output.WriteLine("<input type=\"radio\" name=\"email_format\" id=\"email_format_text\" value=\"text\" /> <label for=\"email_format_text\">Plain Text</label></td></tr>");

                        Output.WriteLine("    </table>");
                        Output.WriteLine("    <br />");
						Output.WriteLine("  </fieldset><br />");
						Output.WriteLine("  <div style=\"text-align:center; font-size:1.3em;\">");
						Output.WriteLine("    <button title=\"Send\" class=\"roundbutton\" onclick=\"return email_form_close();\"> CANCEL </button> &nbsp; &nbsp; ");
						Output.WriteLine("    <button title=\"Send\" class=\"roundbutton\" type=\"submit\"> SEND </button>");
						Output.WriteLine("  </div><br />");
						Output.WriteLine("</div>");
                        Output.WriteLine();
                    }

                    #endregion

                    #region Share form

                    Output.WriteLine("<!-- Share form (empty, filled by javascript) -->");
                    Output.WriteLine("<div class=\"share_popup_div\" id=\"share_form\" style=\"display:none;\"></div>");
                    Output.WriteLine();

                    #endregion

                    Tracer.Add_Trace("Aggregation_HtmlSubwriter.Write_HTML", "Adding search panel.");

                    if (collectionViewer.Type != Item_Aggregation_Views_Searches_Enum.Rotating_Highlight_Search)
                    {
                        Output.WriteLine("<!-- Adding SobekSearchPanel div then sharing buttons -->");
                        Output.WriteLine("<div class=\"SobekSearchPanel\">");
                        Tracer.Add_Trace("Aggregation_HtmlSubwriter.Write_HTML", "Adding sharing buttons.");
                        Add_Sharing_Buttons(Output, FORM_NAME, "SobekResultsSort");
                    }
                }
                else
                {
                    if ((RequestSpecificValues.Current_Mode.Mode != Display_Mode_Enum.Aggregation) || (RequestSpecificValues.Current_Mode.Aggregation_Type != Aggregation_Type_Enum.Browse_Map))
                    {
                        Output.WriteLine("<!-- Adding SobekSearchPanel div without sharing buttons -->");
                        Output.WriteLine("<div class=\"SobekSearchPanel\">");
                    }
                }

                if (collectionViewer.Type == Item_Aggregation_Views_Searches_Enum.Rotating_Highlight_Search)
                {
                    StringBuilder builder = new StringBuilder(2000);
                    StringWriter writer = new StringWriter(builder);
                    Add_Sharing_Buttons(writer, FORM_NAME, "SobekHomeBannerButton");
                    ((Rotating_Highlight_Search_AggregationViewer)collectionViewer).Sharing_Buttons_HTML = builder.ToString();

                    collectionViewer.Add_Search_Box_HTML(Output, Tracer);

                    Output.WriteLine("<!-- Aggregation_HtmlSubwriter.Write_HTML - MainMenus_Helper_HtmlSubWriter.Add_Aggregation_Main_menu -->");
                    MainMenus_Helper_HtmlSubWriter.Add_Aggregation_Main_Menu(Output, RequestSpecificValues, hierarchyObject);

					// Start the page container
					Output.WriteLine("<div id=\"pagecontainer\">");
                }
                else
                {
                    collectionViewer.Add_Search_Box_HTML(Output, Tracer);

	                Output.WriteLine((( RequestSpecificValues.Current_Mode.Mode != Display_Mode_Enum.Aggregation ) || ( RequestSpecificValues.Current_Mode.Aggregation_Type != Aggregation_Type_Enum.Browse_Map )) ? "</div>" : "<div id=\"pagecontainer_resumed\">");
                }

                Output.WriteLine("</form>");
                Output.WriteLine();
            }
            else if ((collectionViewer != null) && (collectionViewer.Is_Internal_View))
            {
                Output.WriteLine("<div class=\"sbkAdm_TitleDiv sbkAdm_TitleDivBorder\">");
                Output.WriteLine("  <img id=\"sbkAdm_TitleDivImg\" src=\"" + collectionViewer.Viewer_Icon + "\" alt=\"\" />");
                Output.WriteLine("  <h1>" + collectionViewer.Viewer_Title + "</h1>");
                Output.WriteLine("</div>");
            }
            else
            {
                collectionViewer.Add_Search_Box_HTML(Output, Tracer);
            }

            Tracer.Add_Trace("Aggregation_HtmlSubwriter.Write_HTML", "Add the secondary HTML to the home page.");

            // Add the secondary HTML to the home page
            bool finish_page = true;

            if ((RequestSpecificValues.Current_Mode.Mode == Display_Mode_Enum.Aggregation) && ((RequestSpecificValues.Current_Mode.Aggregation_Type == Aggregation_Type_Enum.Home) || (RequestSpecificValues.Current_Mode.Aggregation_Type == Aggregation_Type_Enum.Home_Edit)) && (!behaviors.Contains(HtmlSubwriter_Behaviors_Enum.Aggregation_Suppress_Home_Text)))
            {
                finish_page = add_home_html(Output, Tracer);
            }
            else
            {
                collectionViewer.Add_Secondary_HTML(Output, Tracer);
            }

            if (( RequestSpecificValues.Current_Mode.Mode == Display_Mode_Enum.Aggregation ) && ( RequestSpecificValues.Current_Mode.Aggregation_Type == Aggregation_Type_Enum.Browse_Info ))
            {
                if (datasetBrowseResultsStats != null)
                    finish_page = false;
            }

            Tracer.Add_Trace("Aggregation_HtmlSubwriter.Write_HTML", "End of Write_HTML.");
            Output.WriteLine("<!-- End of Aggregation_HtmlSubwriter.Write_HTML -->");

            return finish_page;
        }

        private void Add_Sharing_Buttons( TextWriter Output, string FormName, string Style )
        {
            #region Add the buttons for sharing, emailing, etc..

            RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Add_Sharing_Buttons", "Entered...");
            Output.WriteLine("<!-- Start of Aggregation_HtmlSubwriter.Add_Sharing_Buttons -->");

            Output.Write("  <span class=\"" + Style + "\">");
            Output.Write("<a href=\"\" onmouseover=\"document." + FormName + ".print_button.src='" + RequestSpecificValues.Current_Mode.Base_URL + "design/skins/" + RequestSpecificValues.HTML_Skin.Base_Skin_Code + "/buttons/print_rect_button_h.gif'\" onfocus=\"document." + FormName + ".print_button.src='" + RequestSpecificValues.Current_Mode.Base_URL + "design/skins/" + RequestSpecificValues.HTML_Skin.Base_Skin_Code + "/buttons/print_rect_button_h.gif'\" onmouseout=\"document." + FormName + ".print_button.src='" + RequestSpecificValues.Current_Mode.Base_URL + "design/skins/" + RequestSpecificValues.HTML_Skin.Base_Skin_Code + "/buttons/print_rect_button.gif'\" onblur=\"document." + FormName + ".print_button.src='" + RequestSpecificValues.Current_Mode.Base_URL + "design/skins/" + RequestSpecificValues.HTML_Skin.Base_Skin_Code + "/buttons/print_rect_button.gif'\" onclick=\"window.print(); return false;\"><img class=\"ResultSavePrintButtons\" border=\"0px\" name=\"print_button\" id=\"print_button\" src=\"" + RequestSpecificValues.Current_Mode.Base_URL + "design/skins/" + RequestSpecificValues.HTML_Skin.Base_Skin_Code + "/buttons/print_rect_button.gif\" title=\"Print this page\" alt=\"PRINT\" /></a>");

            if (RequestSpecificValues.Current_User != null)
            {
                if ((RequestSpecificValues.Current_Mode.Home_Type == Home_Type_Enum.Personalized) && (RequestSpecificValues.Current_Mode.Aggregation.Length == 0))
                {
                    Output.Write("<img class=\"ResultSavePrintButtons\" border=\"0px\" name=\"send_button\" id=\"send_button\" src=\"" + RequestSpecificValues.Current_Mode.Base_URL + "design/skins/" + RequestSpecificValues.HTML_Skin.Base_Skin_Code + "/buttons/send_rect_button.gif\" title=\"Send this to someone\" alt=\"SEND\" />");
                }
                else
                {
                    Output.Write("<a href=\"\" onmouseover=\"document." + FormName + ".send_button.src='" + RequestSpecificValues.Current_Mode.Base_URL + "design/skins/" + RequestSpecificValues.HTML_Skin.Base_Skin_Code + "/buttons/send_rect_button_h.gif'\" onfocus=\"document." + FormName + ".send_button.src='" + RequestSpecificValues.Current_Mode.Base_URL + "design/skins/" + RequestSpecificValues.HTML_Skin.Base_Skin_Code + "/buttons/send_rect_button_h.gif'\" onmouseout=\"document." + FormName + ".send_button.src='" + RequestSpecificValues.Current_Mode.Base_URL + "design/skins/" + RequestSpecificValues.HTML_Skin.Base_Skin_Code + "/buttons/send_rect_button.gif'\" onblur=\"document." + FormName + ".send_button.src='" + RequestSpecificValues.Current_Mode.Base_URL + "design/skins/" + RequestSpecificValues.HTML_Skin.Base_Skin_Code + "/buttons/send_rect_button.gif'\" onclick=\"return email_form_open2();\"><img class=\"ResultSavePrintButtons\" border=\"0px\" name=\"send_button\" id=\"send_button\" src=\"" + RequestSpecificValues.Current_Mode.Base_URL + "design/skins/" + RequestSpecificValues.HTML_Skin.Base_Skin_Code + "/buttons/send_rect_button.gif\" title=\"Send this to someone\" alt=\"SEND\" /></a>");

                }
                if ((hierarchyObject.ID > 0) && ( String.Compare(hierarchyObject.Code,"all", StringComparison.OrdinalIgnoreCase) != 0 ))
                {
                    if (RequestSpecificValues.Current_User.Is_On_Home_Page(RequestSpecificValues.Current_Mode.Aggregation))
                    {
                        Output.Write("<a href=\"\" onmouseover=\"document." + FormName + ".remove_button.src='" + RequestSpecificValues.Current_Mode.Base_URL + "design/skins/" + RequestSpecificValues.HTML_Skin.Base_Skin_Code + "/buttons/remove_rect_button_h.gif'\" onfocus=\"document." + FormName + ".remove_button.src='" + RequestSpecificValues.Current_Mode.Base_URL + "design/skins/" + RequestSpecificValues.HTML_Skin.Base_Skin_Code + "/buttons/remove_rect_button_h.gif'\" onmouseout=\"document." + FormName + ".remove_button.src='" + RequestSpecificValues.Current_Mode.Base_URL + "design/skins/" + RequestSpecificValues.HTML_Skin.Base_Skin_Code + "/buttons/remove_rect_button.gif'\" onblur=\"document." + FormName + ".remove_button.src='" + RequestSpecificValues.Current_Mode.Base_URL + "design/skins/" + RequestSpecificValues.HTML_Skin.Base_Skin_Code + "/buttons/remove_rect_button.gif'\" onclick=\"return remove_aggregation();\"><img class=\"ResultSavePrintButtons\" border=\"0px\" name=\"remove_button\" id=\"remove_button\" src=\"" + RequestSpecificValues.Current_Mode.Base_URL + "design/skins/" + RequestSpecificValues.HTML_Skin.Base_Skin_Code + "/buttons/remove_rect_button.gif\" title=\"Remove this from my collections home page\" alt=\"REMOVE\" /></a>");
                    }
                    else
                    {
                        Output.Write("<a href=\"\" onmouseover=\"document." + FormName + ".add_button.src='" + RequestSpecificValues.Current_Mode.Base_URL + "design/skins/" + RequestSpecificValues.HTML_Skin.Base_Skin_Code + "/buttons/add_rect_button_h.gif'\" onfocus=\"document." + FormName + ".add_button.src='" + RequestSpecificValues.Current_Mode.Base_URL + "design/skins/" + RequestSpecificValues.HTML_Skin.Base_Skin_Code + "/buttons/add_rect_button_h.gif'\" onmouseout=\"document." + FormName + ".add_button.src='" + RequestSpecificValues.Current_Mode.Base_URL + "design/skins/" + RequestSpecificValues.HTML_Skin.Base_Skin_Code + "/buttons/add_rect_button.gif'\" onclick=\"return add_aggregation();\" onblur=\"document." + FormName + ".add_button.src='" + RequestSpecificValues.Current_Mode.Base_URL + "design/skins/" + RequestSpecificValues.HTML_Skin.Base_Skin_Code + "/buttons/add_rect_button.gif'\" onclick=\"return add_aggregation();\"><img class=\"ResultSavePrintButtons\" border=\"0px\" name=\"add_button\" id=\"add_button\" src=\"" + RequestSpecificValues.Current_Mode.Base_URL + "design/skins/" + RequestSpecificValues.HTML_Skin.Base_Skin_Code + "/buttons/add_rect_button.gif\" title=\"Add this to my collections home page\" alt=\"ADD\" /></a>");
                    }
                }
            }
            else
            {
                string returnUrl = UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode);

                RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.My_Sobek;
                RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Logon;
                RequestSpecificValues.Current_Mode.Return_URL = returnUrl;
                string logOnUrl = UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode);

                Output.Write("<a href=\"" + logOnUrl + "\" onmouseover=\"document." + FormName + ".send_button.src='" + RequestSpecificValues.Current_Mode.Base_URL + "design/skins/" + RequestSpecificValues.HTML_Skin.Base_Skin_Code + "/buttons/send_rect_button_h.gif'\" onfocus=\"document." + FormName + ".send_button.src='" + RequestSpecificValues.Current_Mode.Base_URL + "design/skins/" + RequestSpecificValues.HTML_Skin.Base_Skin_Code + "/buttons/send_rect_button_h.gif'\" onmouseout=\"document." + FormName + ".send_button.src='" + RequestSpecificValues.Current_Mode.Base_URL + "design/skins/" + RequestSpecificValues.HTML_Skin.Base_Skin_Code + "/buttons/send_rect_button.gif'\" onblur=\"document." + FormName + ".send_button.src='" + RequestSpecificValues.Current_Mode.Base_URL + "design/skins/" + RequestSpecificValues.HTML_Skin.Base_Skin_Code + "/buttons/send_rect_button.gif'\"><img class=\"ResultSavePrintButtons\" border=\"0px\" name=\"send_button\" id=\"send_button\" src=\"" + RequestSpecificValues.Current_Mode.Base_URL + "design/skins/" + RequestSpecificValues.HTML_Skin.Base_Skin_Code + "/buttons/send_rect_button.gif\" title=\"Send this to someone\" alt=\"SEND\" /></a>");

                if (String.Compare(hierarchyObject.Code, "all", StringComparison.OrdinalIgnoreCase) != 0)
                    Output.Write("<a href=\"" + logOnUrl + "\" onmouseover=\"document." + FormName + ".add_button.src='" + RequestSpecificValues.Current_Mode.Base_URL + "design/skins/" + RequestSpecificValues.HTML_Skin.Base_Skin_Code + "/buttons/add_rect_button_h.gif'\" onfocus=\"document." + FormName + ".add_button.src='" + RequestSpecificValues.Current_Mode.Base_URL + "design/skins/" + RequestSpecificValues.HTML_Skin.Base_Skin_Code + "/buttons/add_rect_button_h.gif'\" onmouseout=\"document." + FormName + ".add_button.src='" + RequestSpecificValues.Current_Mode.Base_URL + "design/skins/" + RequestSpecificValues.HTML_Skin.Base_Skin_Code + "/buttons/add_rect_button.gif'\" onblur=\"document." + FormName + ".add_button.src='" + RequestSpecificValues.Current_Mode.Base_URL + "design/skins/" + RequestSpecificValues.HTML_Skin.Base_Skin_Code + "/buttons/add_rect_button.gif'\"><img class=\"ResultSavePrintButtons\" border=\"0px\" name=\"add_button\" id=\"add_button\" src=\"" + RequestSpecificValues.Current_Mode.Base_URL + "design/skins/" + RequestSpecificValues.HTML_Skin.Base_Skin_Code + "/buttons/add_rect_button.gif\" title=\"Save this to my collections home page\" alt=\"ADD\" /></a>");
                RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Aggregation;
                RequestSpecificValues.Current_Mode.Return_URL = String.Empty;
            }

            if ((RequestSpecificValues.Current_Mode.Home_Type == Home_Type_Enum.Personalized) && (RequestSpecificValues.Current_Mode.Aggregation.Length == 0))
            {
                Output.Write("<img class=\"ResultSavePrintButtons\" border=\"0px\" name=\"share_button\" id=\"share_button\" src=\"" + RequestSpecificValues.Current_Mode.Base_URL + "design/skins/" + RequestSpecificValues.HTML_Skin.Base_Skin_Code + "/buttons/share_rect_button.gif\" title=\"Share this collection\" />");
            }
            else
            {
				// Calculate the title and url
				string title = HttpUtility.HtmlEncode(hierarchyObject.Name.Replace("'",""));
				string share_url = HttpContext.Current.Items["Original_URL"].ToString().Replace("&", "%26").Replace("?", "%3F").Replace("http://", "").Replace("=", "%3D").Replace("\"", "&quot;");

                // Figure out where the files are being found
                string facebookImage = Static_Resources_Gateway.Facebook_Share_Gif;
                string url = Path.GetDirectoryName(facebookImage).Replace("http:\\", "http://").Replace("\\","/") + "/";

                Output.Write("<a href=\"\" onmouseover=\"document." + FormName + ".share_button.src='" + RequestSpecificValues.Current_Mode.Base_URL + "design/skins/" + RequestSpecificValues.HTML_Skin.Base_Skin_Code + "/buttons/share_rect_button_h.gif'\" onfocus=\"document." + FormName + ".share_button.src='" + RequestSpecificValues.Current_Mode.Base_URL + "design/skins/" + RequestSpecificValues.HTML_Skin.Base_Skin_Code + "/buttons/share_rect_button_h.gif'\" onmouseout=\"document." + FormName + ".share_button.src='" + RequestSpecificValues.Current_Mode.Base_URL + "design/skins/" + RequestSpecificValues.HTML_Skin.Base_Skin_Code + "/buttons/share_rect_button.gif'\" onblur=\"document." + FormName + ".share_button.src='" + RequestSpecificValues.Current_Mode.Base_URL + "design/skins/" + RequestSpecificValues.HTML_Skin.Base_Skin_Code + "/buttons/share_rect_button.gif'\" onclick=\"return toggle_share_form2('" + title.Replace("'", "") + "','" + share_url + "','" + url + "');\"><img class=\"ResultSavePrintButtons\" border=\"0px\" name=\"share_button\" id=\"share_button\" src=\"" + RequestSpecificValues.Current_Mode.Base_URL + "design/skins/" + RequestSpecificValues.HTML_Skin.Base_Skin_Code + "/buttons/share_rect_button.gif\" title=\"Share this collection\" alt=\"SHARE\" /></a>");
            }

            Output.WriteLine("</span>");

            Output.WriteLine("<!-- End of Aggregation_HtmlSubwriter.Add_Sharing_Buttons -->");

            #endregion
        }

        #endregion

        #region Public method to add controls to the place holder 

        /// <summary> Adds the tree view control to the provided place holder if this is the tree view main home page </summary>
        /// <param name="MainPlaceHolder"> Place holder into which to place the built tree control </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns>Flag indicates if secondary text contains controls </returns>
        public bool Add_Controls(PlaceHolder MainPlaceHolder, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace("Aggregation_HtmlSubwriter.Add_Controls", "Entered...");
            
            if (collectionViewer.Secondary_Text_Requires_Controls)
            {
                Tracer.Add_Trace("Aggregation_HtmlSubwriter.Add_Controls", "Secondary text requires controls.");
                collectionViewer.Add_Secondary_Controls(MainPlaceHolder, Tracer);
                return true;
            }
            else
            {
                Tracer.Add_Trace("Aggregation_HtmlSubwriter.Add_Controls","NO secondary text requires controls.");
            }

            return false;
        }

        #endregion

        #region Methods to add home page text

        /// <summary> Adds the home page text to the output for this collection view </summary>
        /// <param name="Output"> Stream to which to write the HTML for this subwriter </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> TRUE unless this is tree view mode, in which case the tree control needs to be added before the page can be finished </returns>
        protected internal bool add_home_html(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace("Aggregation_HtmlSubwriter.add_home_html", "Entered...");
            Output.WriteLine("<!-- Start of Aggregation_HtmlSubwriter.add_home_html -->");

            Output.WriteLine();
        	Output.WriteLine("<div class=\"SobekText\" role=\"main\" id=\"main-content\">");
            
            // If this is a normal aggregation type ( i.e., not the library home ) just display the home text normally
            if ((RequestSpecificValues.Current_Mode.Aggregation.Length != 0) && (hierarchyObject.ID > 0))
            {
                string url_options = UrlWriterHelper.URL_Options(RequestSpecificValues.Current_Mode);
                string urlOptions1 = String.Empty;
                string urlOptions2 = String.Empty;

                if (url_options.Length > 0)
                {
                    urlOptions1 = "?" + url_options;
                    urlOptions2 = "&" + url_options;
                }

				// Get the raw home hteml text
                string home_html = hierarchyObject.HomePageHtml.Content;

                if ((canEditHomePage) && (RequestSpecificValues.Current_Mode.Aggregation_Type == Aggregation_Type_Enum.Home_Edit))
	            {
					string post_url = HttpUtility.HtmlEncode(HttpContext.Current.Items["Original_URL"].ToString());
					Output.WriteLine("<form name=\"home_edit_form\" method=\"post\" action=\"" + post_url + "\" id=\"addedForm\" >");
					Output.WriteLine("  <textarea id=\"sbkAghsw_HomeTextEdit\" name=\"sbkAghsw_HomeTextEdit\" >");
					Output.WriteLine(home_html.Replace("<%","[%").Replace("%>","%]"));
					Output.WriteLine("  </textarea>");
					Output.WriteLine();

					Output.WriteLine("<div id=\"sbkAghsw_HomeEditButtons\">");
					RequestSpecificValues.Current_Mode.Aggregation_Type = Aggregation_Type_Enum.Home;
					Output.WriteLine("  <button title=\"Do not apply changes\" class=\"roundbutton\" onclick=\"window.location.href='" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "';return false;\"><img src=\"" + Static_Resources_Gateway.Button_Previous_Arrow_Png + "\" class=\"roundbutton_img_left\" alt=\"\" /> CANCEL</button> &nbsp; &nbsp; ");

                    // In some cases, we don't want the HTML editing to use CKEditor, since it can damage the HTML editing from source
                    if (ifEditNoCkEditor)
                    {
                        Output.WriteLine("  <button title=\"Save changes to this aggregation home page text\" class=\"roundbutton\" type=\"submit\">SAVE <img src=\"" + Static_Resources_Gateway.Button_Next_Arrow_Png + "\" class=\"roundbutton_img_right\" alt=\"\" /></button>");
                    }
                    else
                    {
                        Output.WriteLine("  <button title=\"Save changes to this aggregation home page text\" class=\"roundbutton\" type=\"submit\" onclick=\"for(var i in CKEDITOR.instances) { CKEDITOR.instances[i].updateElement(); }\">SAVE <img src=\"" + Static_Resources_Gateway.Button_Next_Arrow_Png + "\" class=\"roundbutton_img_right\" alt=\"\" /></button>");
                    }
                    Output.WriteLine("</div>");
					Output.WriteLine("</form>");
					Output.WriteLine("<br /><br /><br />");
					Output.WriteLine();
	            }
	            else
	            {
		            // Add the highlights
		            if (( hierarchyObject.Highlights != null ) && (hierarchyObject.Highlights.Count > 0) && (collectionViewer.Type != Item_Aggregation_Views_Searches_Enum.Rotating_Highlight_Search))
		            {
			            Output.WriteLine( Highlight_To_Html( hierarchyObject.Highlights[0], RequestSpecificValues.Current_Mode.Base_Design_URL + hierarchyObject.ObjDirectory).Replace("<%URLOPTS%>", url_options).Replace("<%?URLOPTS%>", urlOptions1).Replace("<%&URLOPTS%>", urlOptions2));
		            }

		            // Determine the different counts as strings and replace if they exist
		            if ((home_html.Contains("<%PAGES%>")) || (home_html.Contains("<%TITLES%>")) || (home_html.Contains("<%ITEMS%>")))
		            {
			            if (hierarchyObject.Statistics == null )
			            {
		                    home_html = home_html.Replace("<%PAGES%>", String.Empty).Replace("<%ITEMS%>", String.Empty).Replace("<%TITLES%>", String.Empty);
			            }
			            else
			            {
				            string page_count = Int_To_Comma_String(hierarchyObject.Statistics.Page_Count);
                            string item_count = Int_To_Comma_String(hierarchyObject.Statistics.Item_Count);
                            string title_count = Int_To_Comma_String(hierarchyObject.Statistics.Title_Count);

				            home_html = home_html.Replace("<%PAGES%>", page_count).Replace("<%ITEMS%>", item_count).Replace("<%TITLES%>", title_count);
			            }
		            }

		            // Replace any item aggregation specific custom directives
		            string original_home = home_html;

                    if ((hierarchyObject.Custom_Directives != null) && (hierarchyObject.Custom_Directives.Count > 0 ))
    		            home_html = hierarchyObject.Custom_Directives.Keys.Where(original_home.Contains).Aggregate(home_html, (Current, ThisKey) => Current.Replace(ThisKey, hierarchyObject.Custom_Directives[ThisKey].Replacement_HTML));

		            // Replace any standard directives last
		            home_html = home_html.Replace("<%BASEURL%>", RequestSpecificValues.Current_Mode.Base_URL).Replace("<%URLOPTS%>", url_options).Replace("<%?URLOPTS%>", urlOptions1).Replace("<%&URLOPTS%>", urlOptions2);

		            // Output the adjusted home html
                    if (canEditHomePage)
		            {
						Output.WriteLine("<div id=\"sbkAghsw_EditableHome\">");
			            Output.WriteLine(home_html);
						RequestSpecificValues.Current_Mode.Aggregation_Type = Aggregation_Type_Enum.Home_Edit;
						Output.WriteLine("<div id=\"sbkAghsw_EditableHomeLink\"><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\" title=\"Edit this home text\"><img src=\"" + Static_Resources_Gateway.Edit_Gif + "\" alt=\"\" />edit content</a></div>");
						RequestSpecificValues.Current_Mode.Aggregation_Type = Aggregation_Type_Enum.Home;
						Output.WriteLine("</div>");
						Output.WriteLine();

						Output.WriteLine("<script>");
						Output.WriteLine("  $(\"#sbkAghsw_EditableHome\").mouseover(function() { $(\"#sbkAghsw_EditableHomeLink\").css(\"display\",\"inline-block\"); });");
						Output.WriteLine("  $(\"#sbkAghsw_EditableHome\").mouseout(function() { $(\"#sbkAghsw_EditableHomeLink\").css(\"display\",\"none\"); });");
						Output.WriteLine("</script>");
						Output.WriteLine();
		            }
		            else
		            {
						Output.WriteLine("<div id=\"sbkAghsw_Home\">");
						Output.WriteLine(home_html);
						Output.WriteLine("</div>");
		            }
	            }

	            // If there are sub aggregations here, show them
	            if (hierarchyObject.Children_Count > 0)
	            {
                    RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.add_home_html", "There are sub-aggregations here.");
	                children_icons_added = Add_SubCollection_Buttons(Output, RequestSpecificValues, hierarchyObject);
	            }

                RequestSpecificValues.Current_Mode.Aggregation = hierarchyObject.Code;
            }
            else
            {
                if ((RequestSpecificValues.Current_Mode.Home_Type != Home_Type_Enum.Personalized) && (RequestSpecificValues.Current_Mode.Home_Type != Home_Type_Enum.Partners_List) && (RequestSpecificValues.Current_Mode.Home_Type != Home_Type_Enum.Partners_Thumbnails))
                {
					// Should this person be able to edit this page?
	                bool isAdmin = (RequestSpecificValues.Current_User != null) && ((RequestSpecificValues.Current_User.Is_System_Admin) || (RequestSpecificValues.Current_User.Is_Portal_Admin));

                    if ((isAdmin) && (UI_ApplicationCache_Gateway.Settings.Contains_Additional_Setting("Portal Admins Can Edit Home Page")))
					{
                        if (UI_ApplicationCache_Gateway.Settings.Get_Additional_Setting("Portal Admins Can Edit Home Page").ToUpper().Trim() == "FALSE")
						{
							isAdmin = RequestSpecificValues.Current_User.Is_System_Admin;
						}
					}

					// This is the main home page, so call one of the special functions to draw the home
                    // page types ( i.e., icon view, brief view, or tree view )
                    string sobekcm_home_page_text;
                    string home_app_key = "SobekCM_Home_" + RequestSpecificValues.Current_Mode.Language_Code;
                    object sobekcm_home_page_obj = HttpContext.Current.Application[home_app_key];

                    if (sobekcm_home_page_obj == null)
                    {
                        if (Tracer != null)
                        {
                            Tracer.Add_Trace("Aggregation_HtmlSubwriter.add_home_html", "Reading main library home text source file");
                        }

                        sobekcm_home_page_text = hierarchyObject.HomePageHtml.Content; //.Get_Home_HTML(RequestSpecificValues.Current_Mode.Language, Tracer);

                        HttpContext.Current.Application[home_app_key] = sobekcm_home_page_text;
                    }
                    else
                    {
                        sobekcm_home_page_text = (string)sobekcm_home_page_obj;
                    }

	                if ((isAdmin) && (RequestSpecificValues.Current_Mode.Aggregation_Type == Aggregation_Type_Enum.Home_Edit))
	                {
		                string post_url = HttpUtility.HtmlEncode(HttpContext.Current.Items["Original_URL"].ToString());
		                Output.WriteLine("<form name=\"home_edit_form\" method=\"post\" action=\"" + post_url + "\" id=\"addedForm\" >");
		                Output.WriteLine("  <textarea id=\"sbkAghsw_HomeTextEdit\" name=\"sbkAghsw_HomeTextEdit\" >");
                        Output.WriteLine(sobekcm_home_page_text.Replace("<%", "[%").Replace("%>", "%]"));
		                Output.WriteLine("  </textarea>");
		                Output.WriteLine();

		                Output.WriteLine("<div id=\"sbkAghsw_HomeEditButtons\">");
		                RequestSpecificValues.Current_Mode.Aggregation_Type = Aggregation_Type_Enum.Home;
		                Output.WriteLine("  <button title=\"Do not apply changes\" class=\"roundbutton\" onclick=\"window.location.href='" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "';return false;\"><img src=\"" + Static_Resources_Gateway.Button_Previous_Arrow_Png + "\" class=\"roundbutton_img_left\" alt=\"\" /> CANCEL</button> &nbsp; &nbsp; ");

                        if (ifEditNoCkEditor)
                        {
                            // In this case, we won't use CKEDITOR, since it does too much damage when converting the HTML soruce cod
                            Output.WriteLine("  <button title=\"Save changes to this aggregation home page text\" class=\"roundbutton\" type=\"submit\">SAVE <img src=\"" + Static_Resources_Gateway.Button_Next_Arrow_Png + "\" class=\"roundbutton_img_right\" alt=\"\" /></button>");
                        }
                        else
                        {
                            Output.WriteLine("  <button title=\"Save changes to this aggregation home page text\" class=\"roundbutton\" type=\"submit\" onclick=\"for(var i in CKEDITOR.instances) { CKEDITOR.instances[i].updateElement(); }\">SAVE <img src=\"" + Static_Resources_Gateway.Button_Next_Arrow_Png + "\" class=\"roundbutton_img_right\" alt=\"\" /></button>");

                        }

                        Output.WriteLine("</div>");
		                Output.WriteLine("</form>");
						Output.WriteLine("<br /><br /><br />");
		                Output.WriteLine();
	                }
	                else
	                {
		                int index = sobekcm_home_page_text.IndexOf("<%END%>");

		                // Determine the different counts as strings
                        string page_count = "0";
                        string item_count = "0";
	                    string title_count = "0";

	                    if (hierarchyObject.Statistics != null )
	                    {
	                        page_count = Int_To_Comma_String(hierarchyObject.Statistics.Page_Count);
                            item_count = Int_To_Comma_String(hierarchyObject.Statistics.Item_Count);
                            title_count = Int_To_Comma_String(hierarchyObject.Statistics.Title_Count);
	                    }

	                    string url_options = UrlWriterHelper.URL_Options(RequestSpecificValues.Current_Mode);
		                string urlOptions1 = String.Empty;
		                string urlOptions2 = String.Empty;

		                if (url_options.Length > 0)
		                {
			                urlOptions1 = "?" + url_options;
			                urlOptions2 = "&" + url_options;
		                }

						string adjusted_home = index > 0 ? sobekcm_home_page_text.Substring(0, index).Replace("<%BASEURL%>", RequestSpecificValues.Current_Mode.Base_URL).Replace("<%URLOPTS%>", url_options).Replace("<%?URLOPTS%>", urlOptions1).Replace("<%&URLOPTS%>", urlOptions2).Replace("<%INTERFACE%>", RequestSpecificValues.Current_Mode.Base_Skin_Or_Skin).Replace("<%WEBSKIN%>", RequestSpecificValues.Current_Mode.Base_Skin_Or_Skin).Replace("<%PAGES%>", page_count).Replace("<%ITEMS%>", item_count).Replace("<%TITLES%>", title_count)
			                                 : sobekcm_home_page_text.Replace("<%BASEURL%>", RequestSpecificValues.Current_Mode.Base_URL).Replace("<%URLOPTS%>", url_options).Replace("<%?URLOPTS%>", urlOptions1).Replace("<%&URLOPTS%>", urlOptions2).Replace("<%INTERFACE%>", RequestSpecificValues.Current_Mode.Base_Skin_Or_Skin).Replace("<%WEBSKIN%>", RequestSpecificValues.Current_Mode.Base_Skin_Or_Skin).Replace("<%PAGES%>", page_count).Replace("<%ITEMS%>", item_count).Replace("<%TITLES%>", title_count);

						// Output the adjusted home html
						if (isAdmin)
						{
							Output.WriteLine("<div id=\"sbkAghsw_EditableHome\">");
							Output.WriteLine(adjusted_home);
							RequestSpecificValues.Current_Mode.Aggregation_Type = Aggregation_Type_Enum.Home_Edit;
							Output.WriteLine("  <div id=\"sbkAghsw_EditableHomeLink\"><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\" title=\"Edit this home text\"><img src=\"" + Static_Resources_Gateway.Edit_Gif + "\" alt=\"\" />edit content</a></div>");
							RequestSpecificValues.Current_Mode.Aggregation_Type = Aggregation_Type_Enum.Home;
							Output.WriteLine("</div>");
							Output.WriteLine();

							Output.WriteLine("<script>");

							Output.WriteLine("  $(\"#sbkAghsw_EditableHome\").mouseover(function() { $(\"#sbkAghsw_EditableHomeLink\").css(\"display\",\"inline-block\"); });");
							Output.WriteLine("  $(\"#sbkAghsw_EditableHome\").mouseout(function() { $(\"#sbkAghsw_EditableHomeLink\").css(\"display\",\"none\"); });");
							Output.WriteLine("</script>");
							Output.WriteLine();
						}
						else
						{
                            RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.add_home_html", "Adding sbkAghsw_Home div and adjusted_home.");
							Output.WriteLine("<div id=\"sbkAghsw_Home\">");
							Output.WriteLine(adjusted_home);
							Output.WriteLine("</div>");
						}
	                }
                }
 
                if ((RequestSpecificValues.Current_Mode.Home_Type == Home_Type_Enum.Partners_List) || (RequestSpecificValues.Current_Mode.Home_Type == Home_Type_Enum.Partners_Thumbnails))
                {
					Output.WriteLine("<br />");
                    Output.WriteLine("<p>Partners collaborating and contributing to digital collections and libraries include:</p>");
                }

                if (RequestSpecificValues.Current_Mode.Home_Type != Home_Type_Enum.Personalized)
                {
                    // See if there are actually aggregationPermissions linked to the thematic headings
                    bool aggrsLinkedToThemes = false;
                    if ((!UI_ApplicationCache_Gateway.Settings.System.Include_TreeView_On_System_Home) && ( UI_ApplicationCache_Gateway.Thematic_Headings.Count > 0 ))
                    {
                        if (UI_ApplicationCache_Gateway.Thematic_Headings.Any(ThisTheme => UI_ApplicationCache_Gateway.Aggregations.Aggregations_By_ThemeID(ThisTheme.ID).Count > 0))
                        {
                            aggrsLinkedToThemes = true;
                        } 
                    }

                    // If aggregationPermissions are linked to themes, or if the tree view should always be displayed on home
                    if ((aggrsLinkedToThemes) || (UI_ApplicationCache_Gateway.Settings.System.Include_TreeView_On_System_Home))
                    {
                        string listText = "LIST VIEW";
                        string descriptionText = "BRIEF VIEW";
                        string treeText = "TREE VIEW";
                        const string THUMBNAIL_TEXT = "THUMBNAIL VIEW";

                        if (RequestSpecificValues.Current_Mode.Language == Web_Language_Enum.Spanish)
                        {
                            listText = "LISTADO";
                            descriptionText = "VISTA BREVE";
                            treeText = "JERARQUIA";
                        }

                        Output.WriteLine("<!-- Aggregation_HtmlSubwriter.add_home_html - adding sbkAghsw_HomeTYpeLinks -->");
						Output.WriteLine("<ul class=\"sbk_FauxUpwardTabsList\" id=\"sbkAghsw_HomeTypeLinks\">");

                        Home_Type_Enum startHomeType = RequestSpecificValues.Current_Mode.Home_Type;

                        if ((startHomeType != Home_Type_Enum.Partners_List) && (startHomeType != Home_Type_Enum.Partners_Thumbnails))
                        {
                            if (UI_ApplicationCache_Gateway.Thematic_Headings.Count > 0)
	                        {
		                        if (startHomeType == Home_Type_Enum.List)
		                        {
									Output.WriteLine("  <li class=\"current\">" + listText + "</li>");
		                        }
		                        else
		                        {
			                        RequestSpecificValues.Current_Mode.Home_Type = Home_Type_Enum.List;
			                        Output.WriteLine("  <li><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">" + listText + "</a></li>");
		                        }

		                        if (startHomeType == Home_Type_Enum.Descriptions)
		                        {
									Output.WriteLine("  <li class=\"current\">" + descriptionText + "</li>");
		                        }
		                        else
		                        {
			                        RequestSpecificValues.Current_Mode.Home_Type = Home_Type_Enum.Descriptions;
			                        Output.WriteLine("  <li><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">" + descriptionText + "</a></li>");
		                        }

		                        if (UI_ApplicationCache_Gateway.Settings.System.Include_TreeView_On_System_Home)
		                        {
			                        if (startHomeType == Home_Type_Enum.Tree) 
			                        {
										Output.WriteLine("  <li class=\"current\">" + treeText + "</li>");
			                        }
			                        else
			                        {
				                        RequestSpecificValues.Current_Mode.Home_Type = Home_Type_Enum.Tree;
				                        Output.WriteLine("  <li><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">" + treeText + "</a></li>");
			                        }
		                        }
	                        }
                        }
                        else
                        {
                            if (startHomeType == Home_Type_Enum.Partners_List)
                            {
								Output.WriteLine("  <li class=\"current\">" + listText + "</li>");
                            }
                            else
                            {
                                RequestSpecificValues.Current_Mode.Home_Type = Home_Type_Enum.Partners_List;
                                Output.WriteLine("  <li><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">" + listText + "</a></li>" );
                            }

                            if (startHomeType == Home_Type_Enum.Partners_Thumbnails)
                            {
								Output.WriteLine("  <li class=\"current\">" + THUMBNAIL_TEXT + "</li>");
                            }
                            else
                            {
                                RequestSpecificValues.Current_Mode.Home_Type = Home_Type_Enum.Partners_Thumbnails;
                                Output.WriteLine("  <li><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">" + THUMBNAIL_TEXT + "</a></li>" );
                            }

							if (UI_ApplicationCache_Gateway.Settings.System.Include_TreeView_On_System_Home)
							{
								if (startHomeType == Home_Type_Enum.Tree) 
								{
									Output.WriteLine("  <li class=\"current\">" + treeText + "</li>");
								}
								else
								{
									RequestSpecificValues.Current_Mode.Home_Type = Home_Type_Enum.Tree;
									Output.WriteLine("  <li><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">" + treeText + "</a></li>");
								}
							}
                        }

                        RequestSpecificValues.Current_Mode.Home_Type = startHomeType;

                        Output.WriteLine("</ul>");
                        Output.WriteLine();
                    }
                }

                Tracer.Add_Trace("Aggregation_HtmlSubwriter.add_home_html", "Switch on RequestSpecificValues.Current_Mode.Home_Type");

                switch (RequestSpecificValues.Current_Mode.Home_Type)
                {
                    case Home_Type_Enum.List:
                        Tracer.Add_Trace("Aggregation_HtmlSubwriter.add_home_html", "Home_Type_Enum.List, calling write_list_home.");
                        write_list_home(Output, Tracer);
                        break;

                    case Home_Type_Enum.Descriptions:
                        Tracer.Add_Trace("Aggregation_HtmlSubwriter.add_home_html", "Home_Type_Enum.Descriptions, calling write_description_home.");
                        write_description_home(Output, Tracer);
                        break;

                    case Home_Type_Enum.Personalized:
                        Tracer.Add_Trace("Aggregation_HtmlSubwriter.add_home_html", "Home_Type_Enum.Personalize, calling write_personalize_home.");
                        write_personalized_home(Output, Tracer);
                        break;

                    case Home_Type_Enum.Partners_List:
                        Tracer.Add_Trace("Aggregation_HtmlSubwriter.add_home_html", "Home_Type_Enum.Partners_List, calling write_institution_list.");
                        write_institution_list(Output, Tracer);
                        break;

                    case Home_Type_Enum.Partners_Thumbnails:
                        Tracer.Add_Trace("Aggregation_HtmlSubwriter.add_home_html", "Home_Type_Enum.Partners_Thumbnails, calling write_institution_icons.");
                        write_institution_icons(Output, Tracer);
                        break;

                    case Home_Type_Enum.Tree:
                        Tracer.Add_Trace("Aggregation_HtmlSubwriter.add_home_html", "Home_Type_Enum.Tree, calling write_treeview.");
                        write_treeview(Output, Tracer);
                        break;
                }
            }

			Output.WriteLine("</div>");

            Output.WriteLine("<!-- End of Aggregation_HtmlSubwriter.add_home_html -->");

            return true;
        }

        /// <summary> Adds the add the sub collection buttons </summary>
        /// <param name="Output"> Stream to which to write the HTML for this subwriter </param>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        /// <param name="hierarchyObject"> The sub collection hierarchy object </param>
        /// <returns> TRUE if the sub collection buttons were successfully added </returns>
        public static bool Add_SubCollection_Buttons(TextWriter Output, RequestCache RequestSpecificValues, Item_Aggregation hierarchyObject)
        {
            RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Add_SubCollection_Buttons", "Entered...");
            Output.WriteLine("<!-- Start of Aggregation_HtmlSubwriter.Add_SubCollection_Buttons -->");

            bool children_icons_added = false;

            // Verify some of the children are active and not hidden
            // Keep the last aggregation alias
            string lastAlias = RequestSpecificValues.Current_Mode.Aggregation_Alias;
            RequestSpecificValues.Current_Mode.Aggregation_Alias = String.Empty;

            // Collect the html to write (this alphabetizes the children)
            List<string> html_list = new List<string>();
            int aggreCount = -1;
            foreach (Item_Aggregation_Related_Aggregations childAggr in hierarchyObject.Children)
            {
                children_icons_added = true;
                aggreCount++;
                Item_Aggregation_Related_Aggregations latest = UI_ApplicationCache_Gateway.Aggregations[childAggr.Code];
                if ((latest != null) && (!latest.Hidden) && (latest.Active))
                {
                    string name = childAggr.Name;
                    string thisDescription = latest.Description;

                    if (name.ToUpper() == "ADDED AUTOMATICALLY")
                        name = childAggr.Code + " ( Added Automatically )";

                    RequestSpecificValues.Current_Mode.Aggregation = childAggr.Code.ToLower();
                    string image_url = RequestSpecificValues.Current_Mode.Base_URL + "design/aggregations/" + childAggr.Code + "/images/buttons/coll.gif";
                    if ((name.IndexOf("The ") == 0) && (name.Length > 4))
                    {
                        html_list.Add("  <li class=\"sbkAghsw_CollectionButton\">" + Environment.NewLine + "      <span id=\"sbkAghsw_CollectionButtonImg" + aggreCount + "\" class=\"sbkAghsw_CollectionButtonImg\"><span class=\"spanHoverText\" style=\"display:none\">" + thisDescription + "</span><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\"><img src=\"" + image_url + "\" alt=\"" + UI_ApplicationCache_Gateway.Translation.Get_Translation(name, RequestSpecificValues.Current_Mode.Language).Replace("&", "&amp;").Replace("\"", "&quot;") + "\" /></a></span>" + Environment.NewLine + "      <span class=\"sbkAghsw_CollectionButtonTxt\"><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">" + UI_ApplicationCache_Gateway.Translation.Get_Translation(name, RequestSpecificValues.Current_Mode.Language).Replace("&", "&amp;").Replace("\"", "&quot;") + "</a></span>" + Environment.NewLine + "  </li>");
                    }
                    else
                    {
                        html_list.Add("  <li class=\"sbkAghsw_CollectionButton\">" + Environment.NewLine + "      <span id=\"sbkAghsw_CollectionButtonImg" + aggreCount + "\" class=\"sbkAghsw_CollectionButtonImg\"><span class=\"spanHoverText\" style=\"display:none\">" + thisDescription + "</span><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\"><img src=\"" + image_url + "\" alt=\"" + UI_ApplicationCache_Gateway.Translation.Get_Translation(name, RequestSpecificValues.Current_Mode.Language).Replace("&", "&amp;").Replace("\"", "&quot;") + "\" /></a></span>" + Environment.NewLine + "      <span class=\"sbkAghsw_CollectionButtonTxt\"><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">" + UI_ApplicationCache_Gateway.Translation.Get_Translation(name, RequestSpecificValues.Current_Mode.Language).Replace("&", "&amp;").Replace("\"", "&quot;") + "</a></span>" + Environment.NewLine + "  </li>");
                    }
                }
            }

            if ((html_list.Count > 0) && (String.Compare(hierarchyObject.Code, "all", StringComparison.OrdinalIgnoreCase) != 0))
            {
                string childTypes = hierarchyObject.Child_Types.Trim();
                if (childTypes.IndexOf(" ") > 0)
                {
                    TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                    childTypes = textInfo.ToTitleCase(childTypes);
                }

                string translatedChild = UI_ApplicationCache_Gateway.Translation.Get_Translation(childTypes, RequestSpecificValues.Current_Mode.Language);
                Output.WriteLine("<!-- Aggregation_HtmlSubwriter.Add_SubCollectionButtons  adding section -->");
                Output.WriteLine("<section id=\"sbkAghsw_Children\" role=\"navigation\" aria-label=\"" + translatedChild + "\">");
                Output.WriteLine("<h2 id=\"subcolls\">" + translatedChild + "</h2>");
                
                Output.WriteLine("<ul class=\"sbkAghsw_CollectionButtonList\">");

                foreach (string thisHtml in html_list)
                {
                    Output.WriteLine(thisHtml);
                }

                Output.WriteLine("</ul>");
                Output.WriteLine("</section>");

                // Restore the old alias
                RequestSpecificValues.Current_Mode.Aggregation_Alias = lastAlias;
            }

            RequestSpecificValues.Current_Mode.Aggregation = hierarchyObject.Code;

            Output.WriteLine("<!-- End of Aggregation_HtmlSubwriter.Add_SubCollection_Buttons -->");

            return children_icons_added;
        }

        private string Int_To_Comma_String(int Value)
        {
            if (Value < 1000)
                return Value.ToString();

            string value_string = Value.ToString();
            if ((Value >= 1000) && (Value < 1000000))
            {
                return value_string.Substring(0, value_string.Length - 3) + "," + value_string.Substring(value_string.Length - 3);
            }

            return value_string.Substring(0, value_string.Length - 6) + "," + value_string.Substring(value_string.Length - 6, 3) + "," + value_string.Substring(value_string.Length - 3);
        }

        #region Main Home Page Methods

        #region Method to create the tree view

        private void add_children_to_tree(string LeadingSpaces, TextWriter Output, Item_Aggregation_Related_Aggregations Aggr )
        {
            RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.add_children_to_tree", "Entered...");
            Output.WriteLine("<!-- Start of Aggregation_HtmlSubwriter.add_children_to_tree -->");

            // Step through each node under this
            if (Aggr.Children_Count > 0)
            {
                Output.WriteLine(LeadingSpaces + "<ul>");
                foreach (Item_Aggregation_Related_Aggregations childAggr in Aggr.Children)
                {
                    if ((!childAggr.Hidden) && (childAggr.Active))
                    {
                        // Set the aggregation value, for the redirect URL
                        RequestSpecificValues.Current_Mode.Aggregation = childAggr.Code.ToLower();

                        if (childAggr.Children_Count > 0)
                        {
                            Output.WriteLine(LeadingSpaces + "  <li><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\"><abbr title=\"" + childAggr.Description + "\">" + childAggr.Name + "</abbr></a>");

                            // Check the children nodes recursively
                            add_children_to_tree(LeadingSpaces + "   ", Output, childAggr);

                            Output.WriteLine(LeadingSpaces + "  </li>");
                        }
                        else
                        {
                            Output.WriteLine(LeadingSpaces + "  <li><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\"><abbr title=\"" + childAggr.Description + "\">" + childAggr.Name + "</abbr></a></li>");
                        }
                    }
                }

                Output.WriteLine(LeadingSpaces + "</ul>");
            }

            Output.WriteLine("<!-- End of Aggregation_HtmlSubwriter.add_children_to_tree -->");
        }

        #endregion

        /// <summary> Write the treeview to the specified output </summary>
        /// <param name="Output"> Stream to the HTML response output </param>
        /// <param name="Tracer">The tracer.</param>
        protected internal void write_treeview(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace("Aggregation_HtmlSubwriter.write_treeview", "Entered...");
            Output.WriteLine("<!-- Start of Aggregation_HtmlSubwriter.write_treeview -->");

            Output.WriteLine("<script type=\"text/javascript\" src=\"" + Static_Resources_Gateway.Jstree_Js + "\"></script>");

            // Get the hierarchy
            Aggregation_Hierarchy hierarchy = SobekEngineClient.Aggregations.Get_Aggregation_Hierarchy(Tracer);

            if (hierarchy != null)
            {
                // Add the text
                Output.WriteLine("<div class=\"SobekText\">");
                Output.WriteLine("<h2 style=\"margin-top:0;\">All Collections</h2>");
                Output.WriteLine("<blockquote>");
                Output.WriteLine("  <div style=\"text-align:right;\">");
                Output.WriteLine("    <a onclick=\"$('#aggregationTree').jstree('close_all');return false;\">Collapse All</a> | ");
                Output.WriteLine("    <a onclick=\"$('#aggregationTree').jstree('open_all');return false;\">Expand All</a>");
                Output.WriteLine("  </div>");

                Output.WriteLine("  <div id=\"aggregationTree\">");
                Output.WriteLine("    <ul>");
                Output.WriteLine("      <li>Collection Hierarchy");

                // Step through each node under this
                if (hierarchy.Collections.Count > 0)
                {
                    Output.WriteLine("        <ul>");
                    foreach (Item_Aggregation_Related_Aggregations childAggr in hierarchy.Collections)
                    {
                        if ((!childAggr.Hidden) && (childAggr.Active))
                        {
                            // Set the aggregation value, for the redirect URL
                            RequestSpecificValues.Current_Mode.Aggregation = childAggr.Code.ToLower();

                            Output.WriteLine("          <li><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\"><abbr title=\"" + childAggr.Description + "\">" + childAggr.Name + "</abbr></a>");

                            // Check the children nodes recursively
                            add_children_to_tree("            ", Output, childAggr);

                            Output.WriteLine("          </li>");
                        }
                    }

                    Output.WriteLine("        </ul>");
                }

                Output.WriteLine("      </li>");

                if (hierarchy.Institutions.Count > 0)
                {
                    Output.WriteLine("      <li>Institutions");
                    Output.WriteLine("        <ul>");
                    foreach (Item_Aggregation_Related_Aggregations childAggr in hierarchy.Institutions)
                    {
                        if ((!childAggr.Hidden) && (childAggr.Active))
                        {
                            // Set the aggregation value, for the redirect URL
                            RequestSpecificValues.Current_Mode.Aggregation = childAggr.Code.ToLower();

                            Output.WriteLine("          <li><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\"><abbr title=\"" + childAggr.Description + "\">" + childAggr.Name + "</abbr></a>");

                            // Check the children nodes recursively
                            add_children_to_tree("            ", Output, childAggr);

                            Output.WriteLine("          </li>");
                        }
                    }

                    Output.WriteLine("        </ul>");
                    Output.WriteLine("      </li>");
                }

                Output.WriteLine("    </ul>");
                Output.WriteLine("  </div>");
                Output.WriteLine("</blockquote>");
                Output.WriteLine("</div>");
                Output.WriteLine();

                Output.WriteLine("<script type=\"text/javascript\">");
                Output.WriteLine("   $('#aggregationTree').jstree().bind(\"select_node.jstree\", function (e, data) { var href = data.node.a_attr.href; document.location.href = href; });");
                Output.WriteLine("</script>");
                Output.WriteLine();

                // Restore the mode (simple since this only appears on the very top level aggregation)
                RequestSpecificValues.Current_Mode.Aggregation = String.Empty;
            }

            Output.WriteLine("<!-- End of Aggregation_HtmlSubwriter.write_treeview -->");
        }

        #region Method to create the descriptive home page

        /// <summary> Adds the main library home page with short descriptions about each highlighted item aggregation</summary>
        /// <param name="Output"> Stream to which to write the HTML for this subwriter </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        protected internal void write_description_home(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace("Aggregation_HtmlSubwriter.write_description_home", "Entered...");
            Output.WriteLine("<!-- Start of Aggregation_HtmlSubwriter.write_description_home -->");

            if (UI_ApplicationCache_Gateway.Thematic_Headings.Count == 0)
                return;

            Output.WriteLine("<section id=\"sbkAghsw_Children\" role=\"navigation\" aria-label=\"Collections\">");

            // Step through each thematic heading and add all the needed aggreagtions
	        bool first = true;
            foreach (Thematic_Heading thisTheme in UI_ApplicationCache_Gateway.Thematic_Headings)
            {
                // Build the list of html to display, first adding collections and subcollections
                SortedList<string, string> html_list = new SortedList<string, string>();
                ReadOnlyCollection<Item_Aggregation_Related_Aggregations> thisThemesAggrs = UI_ApplicationCache_Gateway.Aggregations.Aggregations_By_ThemeID(thisTheme.ID);
                foreach (Item_Aggregation_Related_Aggregations thisAggr in thisThemesAggrs)
                {
                    string image_url = RequestSpecificValues.Current_Mode.Base_URL + "design/aggregations/" + thisAggr.Code + "/images/buttons/coll.gif";
                    RequestSpecificValues.Current_Mode.Aggregation = thisAggr.Code.ToLower();

                    if (RequestSpecificValues.Current_Mode.Aggregation == "dloc1")
                        RequestSpecificValues.Current_Mode.Skin = "dloc";
                    if (RequestSpecificValues.Current_Mode.Aggregation == "edlg")
                        RequestSpecificValues.Current_Mode.Skin = "edl";

                    string aggrNam = thisAggr.Name;
                    string description = thisAggr.Description ?? String.Empty;

                    if (aggrNam.IndexOf("The ") == 0)
                    {
						html_list[aggrNam.Substring(4)] = "    <td class=\"sbkAghsw_CollectionDescription\">" + Environment.NewLine + "      <br /><span class=\"sbkAghsw_CollectionDesciptionImg\"><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\"><img src=\"" + image_url + "\" alt=\"" + aggrNam.Replace("&", "&amp;").Replace("\"", "&quot;") + "\" /></a></span>" + Environment.NewLine + "      <span class=\"sbkAghsw_CollectionButtonTxt\"><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">" + aggrNam.Replace("&", "&amp;").Replace("\"", "&quot;") + "</a>" + Environment.NewLine + "      <p>" + description.Replace("&", "&amp;").Replace("\"", "&quot;") + "</p></span>" + Environment.NewLine + "    <br />" + Environment.NewLine + "    </td>";
                    }
                    else
                    {
						html_list[aggrNam] = "   <td class=\"sbkAghsw_CollectionDescription\"" + Environment.NewLine + "      <br /><span class=\"sbkAghsw_CollectionDesciptionImg\"><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\"><img src=\"" + image_url + "\" alt=\"" + aggrNam.Replace("&", "&amp;").Replace("\"", "&quot;") + "\" /></a></span>" + Environment.NewLine + "      <span class=\"sbkAghsw_CollectionButtonTxt\"><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">" + aggrNam.Replace("&", "&amp;").Replace("\"", "&quot;") + "</a>" + Environment.NewLine + "      <p>" + description.Replace("&", "&amp;").Replace("\"", "&quot;") + "</p></span>" + Environment.NewLine + "    <br />" + Environment.NewLine + "    </td>";

                        if (thisAggr.Code == "EPC")
                        {
							html_list[aggrNam] = "    <td class=\"sbkAghsw_CollectionDescription\"" + Environment.NewLine + "      <br /><span class=\"sbkAghsw_CollectionDesciptionImg\"><a href=\"http://www.uflib.ufl.edu/epc/\"><img src=\"" + image_url + "\" alt=\"" + aggrNam.Replace("&", "&amp;").Replace("\"", "&quot;") + "\" /></a></span>" + Environment.NewLine + "      <span class=\"sbkAghsw_CollectionButtonTxt\"><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">" + aggrNam.Replace("&", "&amp;").Replace("\"", "&quot;") + "</a>" + Environment.NewLine + "      <p>" + description.Replace("&", "&amp;").Replace("\"", "&quot;") + "</p></span>" + Environment.NewLine + "    <br />" + Environment.NewLine + "    </td>";
                        }
                        if (thisAggr.Code == "UFHERB")
                        {
							html_list[aggrNam] = "    <td class=\"sbkAghsw_CollectionDescription\"" + Environment.NewLine + "      <br /><span class=\"sbkAghsw_CollectionDesciptionImg\"><a href=\"http://www.flmnh.ufl.edu/natsci/herbarium/cat/\"><img src=\"" + image_url + "\" alt=\"" + aggrNam.Replace("&", "&amp;").Replace("\"", "&quot;") + "\" /></a></span>" + Environment.NewLine + "      <span class=\"sbkAghsw_CollectionButtonTxt\"><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">" + aggrNam.Replace("&", "&amp;").Replace("\"", "&quot;") + "</a>" + Environment.NewLine + "      <p>" + description.Replace("&", "&amp;").Replace("\"", "&quot;") + "</p></span>" + Environment.NewLine + "    <br />" + Environment.NewLine + "    </td>";
                        }
                        if (thisAggr.Code == "EXHIBITMATERIALS")
                        {
                            RequestSpecificValues.Current_Mode.Aggregation = "exhibits";
							html_list[aggrNam] = "    <td class=\"sbkAghsw_CollectionDescription\"" + Environment.NewLine + "      <br /><span class=\"sbkAghsw_CollectionDesciptionImg\"><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\"><img src=\"" + image_url + "\" alt=\"" + aggrNam.Replace("&", "&amp;").Replace("\"", "&quot;") + "\" /></a></span>" + Environment.NewLine + "      <span class=\"sbkAghsw_CollectionButtonTxt\"><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">" + aggrNam.Replace("&", "&amp;").Replace("\"", "&quot;") + "</a>" + Environment.NewLine + "      <p>" + description.Replace("&", "&amp;").Replace("\"", "&quot;") + "</p></span>" + Environment.NewLine + "    <br />" + Environment.NewLine + "    </td>";
                        }
                    }

                    RequestSpecificValues.Current_Mode.Skin = String.Empty;
                }

                if (html_list.Count > 0)
                {
                    // Write this theme
					if (first)
					{
						Output.WriteLine("<h2 style=\"margin-top:0;margin-bottom:0;\">" + thisTheme.Text + "</h2>");
						first = false;
					}
					else
						Output.WriteLine("<h2 style=\"margin-bottom:0;\">" + thisTheme.Text + "</h2>");

					Output.WriteLine("<table id=\"sbkAghsw_CollectionDescriptionTbl\">");
                    int column_spot = 0;
                    Output.WriteLine("  <tr>");

                    foreach (string thisHtml in html_list.Values)
                    {
                        if (column_spot == 2)
                        {
                            Output.WriteLine("  </tr>");
                            Output.WriteLine("  <tr>");
                            column_spot = 0;
                        }

                        if (column_spot == 1)
                        {
                            Output.WriteLine("    <td style=\"width:15px;\"></td>");
                            Output.WriteLine("    <td style=\"width:1px;background-color:#cccccc;\"></td>");
							Output.WriteLine("    <td style=\"width:15px;\"></td>");
                        }

                        Output.WriteLine(thisHtml);
                        column_spot++;
                    }

                    if (column_spot == 1)
                    {
						Output.WriteLine("    <td style=\"width:15px;\"></td>");
						Output.WriteLine("    <td style=\"width:1px;background-color:#cccccc;\"></td>");
						Output.WriteLine("    <td style=\"width:15px;\"></td>");
						Output.WriteLine("    <td class=\"sbkAghsw_CollectionDescription\">&nbsp;</td>");
                    }

                    Output.WriteLine("  </tr>");
                    Output.WriteLine("</table>");
                    Output.WriteLine("<br />");
                }
            }

            Output.WriteLine("</section>");

            Output.WriteLine("<!-- End of Aggregation_HtmlSubwriter.write_description_home -->");

            RequestSpecificValues.Current_Mode.Aggregation = String.Empty;
        }

        #endregion

        #region Method to show the icon list

        /// <summary> Adds the main library home page with icons and names about each highlighted item aggregation</summary>
        /// <param name="Output"> Stream to which to write the HTML for this subwriter </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        protected internal void write_list_home(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace("Aggregation_HtmlSubwriter.write_list_home", "Thematic_Headings.Count=[" + UI_ApplicationCache_Gateway.Thematic_Headings.Count + "].");
            Output.WriteLine("<!-- Start of Aggregation_HtmlSubwriter.write_list_home -->");

            if (UI_ApplicationCache_Gateway.Thematic_Headings.Count == 0)
            {
                Output.WriteLine("<!-- End of Aggregation_HtmlSubwriter.write_list_home (no thematic headings) -->");
                return;
            }

            Output.WriteLine("<section id=\"sbkAghsw_Children\" role=\"navigation\" aria-label=\"Collections\">");

            // Step through each thematic heading and add all the needed aggregations
	        bool first = true;
            int aggreCount = -1;
            foreach (Thematic_Heading thisTheme in UI_ApplicationCache_Gateway.Thematic_Headings)
            {
                // Build the list of html to display
                SortedList<string, string> html_list = new SortedList<string, string>();
                ReadOnlyCollection<Item_Aggregation_Related_Aggregations> thisThemesAggrs = UI_ApplicationCache_Gateway.Aggregations.Aggregations_By_ThemeID(thisTheme.ID);
                foreach (Item_Aggregation_Related_Aggregations thisAggr in thisThemesAggrs)
                {
                    aggreCount++;
                    string image_url = RequestSpecificValues.Current_Mode.Base_URL + "design/aggregations/" + thisAggr.Code + "/images/buttons/coll.gif";
                    string thisDescription = thisAggr.Description;
                    RequestSpecificValues.Current_Mode.Aggregation = thisAggr.Code.ToLower();

                    if (RequestSpecificValues.Current_Mode.Aggregation == "dloc1")
                        RequestSpecificValues.Current_Mode.Skin = "dloc";
                    if (RequestSpecificValues.Current_Mode.Aggregation == "edlg")
                        RequestSpecificValues.Current_Mode.Skin = "edl";

                    //string hoverHiddenDivTitle = "<div id=\"hoverDivTitle\" style=\"display:none\" class=\"tooltipTitle\">" + UI_ApplicationCache_Gateway.Translation.Get_Translation(thisAggr.Name, RequestSpecificValues.Current_Mode.Language).Replace("&", "&amp;").Replace("\"", "&quot;") + "</div>";
                    //string hoverHiddenDiv = "<div id=\"hoverDiv\" style=\"display:none\" class=\"tooltipText\"><img src=\"" + image_url + "\" alt=\"" + UI_ApplicationCache_Gateway.Translation.Get_Translation(thisAggr.Name, RequestSpecificValues.Current_Mode.Language).Replace("&", "&amp;").Replace("\"", "&quot;") + "\" />"+thisDescription+"</div>";

                    if (thisAggr.Name.IndexOf("The ") == 0)
                    {
                        html_list[thisAggr.Name.Substring(4)] = "  <li class=\"sbkAghsw_CollectionButton\">" + Environment.NewLine + "      <span id=\"sbkAghsw_CollectionButtonImg" + aggreCount + "\" class=\"sbkAghsw_CollectionButtonImg\"><span class=\"spanHoverText\" style=\"display:none\">" + thisDescription + "</span><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\"><img src=\"" + image_url + "\" alt=\"" + UI_ApplicationCache_Gateway.Translation.Get_Translation(thisAggr.Name, RequestSpecificValues.Current_Mode.Language).Replace("&", "&amp;").Replace("\"", "&quot;") + "\" /></a></span>" + Environment.NewLine + "      <span class=\"sbkAghsw_CollectionButtonTxt\"><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">" + UI_ApplicationCache_Gateway.Translation.Get_Translation(thisAggr.Name, RequestSpecificValues.Current_Mode.Language).Replace("&", "&amp;").Replace("\"", "&quot;") + "</a></span>" + Environment.NewLine + "  </li>";
                    }
                    else
                    {
                        html_list[thisAggr.Name] = "  <li class=\"sbkAghsw_CollectionButton\">" + Environment.NewLine + "      <span id=\"sbkAghsw_CollectionButtonImg" + aggreCount + "\" class=\"sbkAghsw_CollectionButtonImg\"><span class=\"spanHoverText\" style=\"display:none\">" + thisDescription + "</span><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\"><img src=\"" + image_url + "\" alt=\"" + UI_ApplicationCache_Gateway.Translation.Get_Translation(thisAggr.Name, RequestSpecificValues.Current_Mode.Language).Replace("&", "&amp;").Replace("\"", "&quot;") + "\" /></a></span>" + Environment.NewLine + "      <span class=\"sbkAghsw_CollectionButtonTxt\"><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">" + UI_ApplicationCache_Gateway.Translation.Get_Translation(thisAggr.Name, RequestSpecificValues.Current_Mode.Language).Replace("&", "&amp;").Replace("\"", "&quot;") + "</a></span>" + Environment.NewLine + "  </li>";

                        if (thisAggr.Code == "EPC")
                        {
                            html_list[thisAggr.Name] = "    <li class=\"sbkAghsw_CollectionButton\">" + Environment.NewLine + "      <span id=\"sbkAghsw_CollectionButtonImg" + aggreCount + "\" class=\"sbkAghsw_CollectionButtonImg\"><span class=\"spanHoverText\" style=\"display:none\">" + thisDescription + "</span><a href=\"http://www.uflib.ufl.edu/epc/\"><img src=\"" + image_url + "\" alt=\"" + UI_ApplicationCache_Gateway.Translation.Get_Translation(thisAggr.Name, RequestSpecificValues.Current_Mode.Language).Replace("&", "&amp;").Replace("\"", "&quot;") + "\" /></a></span>" + Environment.NewLine + "      <span class=\"sbkAghsw_CollectionButtonTxt\"><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">" + UI_ApplicationCache_Gateway.Translation.Get_Translation(thisAggr.Name, RequestSpecificValues.Current_Mode.Language).Replace("&", "&amp;").Replace("\"", "&quot;") + "</a></span>" + Environment.NewLine + "    </li>";
                        }
                        if (thisAggr.Code == "UFHERB")
                        {
                            html_list[thisAggr.Name] = "    <li class=\"sbkAghsw_CollectionButton\">" + Environment.NewLine + "      <span id=\"sbkAghsw_CollectionButtonImg" + aggreCount + "\" class=\"sbkAghsw_CollectionButtonImg\"><span class=\"spanHoverText\" style=\"display:none\">" + thisDescription + "</span><a href=\"http://www.flmnh.ufl.edu/natsci/herbarium/cat/\"><img src=\"" + image_url + "\" alt=\"" + UI_ApplicationCache_Gateway.Translation.Get_Translation(thisAggr.Name, RequestSpecificValues.Current_Mode.Language).Replace("&", "&amp;").Replace("\"", "&quot;") + "\" /></a></span>" + Environment.NewLine + "      <span class=\"sbkAghsw_CollectionButtonTxt\"><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">" + UI_ApplicationCache_Gateway.Translation.Get_Translation(thisAggr.Name, RequestSpecificValues.Current_Mode.Language).Replace("&", "&amp;").Replace("\"", "&quot;") + "</a></span>" + Environment.NewLine + "    </li>";
                        }
                        if (thisAggr.Code == "EXHIBITMATERIALS")
                        {
                            RequestSpecificValues.Current_Mode.Aggregation = "exhibits";
                            html_list[thisAggr.Name] = "    <li class=\"sbkAghsw_CollectionButton\">" + Environment.NewLine + "      <span id=\"sbkAghsw_CollectionButtonImg" + aggreCount + "\" class=\"sbkAghsw_CollectionButtonImg\"><span class=\"spanHoverText\" style=\"display:none\">" + thisDescription + "</span><a href=\"" + RequestSpecificValues.Current_Mode.Base_URL + "\"><img src=\"" + image_url + "\" alt=\"" + UI_ApplicationCache_Gateway.Translation.Get_Translation(thisAggr.Name, RequestSpecificValues.Current_Mode.Language).Replace("&", "&amp;").Replace("\"", "&quot;") + "\" /></a></span>" + Environment.NewLine + "      <span class=\"sbkAghsw_CollectionButtonTxt\"><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">" + UI_ApplicationCache_Gateway.Translation.Get_Translation(thisAggr.Name, RequestSpecificValues.Current_Mode.Language).Replace("&", "&amp;").Replace("\"", "&quot;") + "</a></span>" + Environment.NewLine + "    </li>";
                        }
                    }

                    RequestSpecificValues.Current_Mode.Skin = String.Empty;
                }

                if (html_list.Count > 0)
                {
                    // Write this theme
					if (first)
					{
                        children_icons_added = true;
						Output.WriteLine("<h2 style=\"margin-top:0;\">" + UI_ApplicationCache_Gateway.Translation.Get_Translation(thisTheme.Text, RequestSpecificValues.Current_Mode.Language) + "</h2>");
						first = false;
					}
	                else
						Output.WriteLine("<h2>" + UI_ApplicationCache_Gateway.Translation.Get_Translation(thisTheme.Text, RequestSpecificValues.Current_Mode.Language) + "</h2>");

					Output.WriteLine("<ul class=\"sbkAghsw_CollectionButtonList\">");

                    foreach (string thisHtml in html_list.Values)
                    {
                        Output.WriteLine(thisHtml);
                    }

                    Output.WriteLine("</ul>");
                    Output.WriteLine();
                }
            }

            RequestSpecificValues.Current_Mode.Aggregation = String.Empty;

            Output.WriteLine("</section>");

            Output.WriteLine("<!-- End of Aggregation_HtmlSubwriter.write_list_home -->");
        }

        #endregion

        #region Method to show the personalized home page

        /// <summary> Adds the personalized main library home page for logged on users</summary>
        /// <param name="Output"> Stream to which to write the HTML for this subwriter </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        protected internal void write_personalized_home(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace("Aggregation_HtmlSubwriter.write_personalized_home", "Entered...");
            Output.WriteLine("<!-- Start of Aggregation_HtmlSubwriter.write_personalized_home -->");

            // Build the list of html to display, first adding collections and subcollections
            SortedList<string, string> html_list = new SortedList<string, string>();

            //Output.WriteLine("<script type=\"text/javascript\" src=\"" + Static_Resources.Jquery_1_10_2_Js + "\"></script>");
            //Output.WriteLine("<script type=\"text/javascript\" src=\"" + Static_Resources.Jquery_Qtip_Js + "\"></script>");
            ////NOTE: The jquery.hovercard.min.js file included below has been modified for SobekCM, and also includes bug fixes. DO NOT REPLACE with another version
            //Output.WriteLine("<script type=\"text/javascript\" src=\"" + Static_Resources.Jquery_Hovercard_Js + "\"></script>");
            //Output.WriteLine("  <link rel=\"stylesheet\" type=\"text/css\" href=\"" + Static_Resources.Jquery_Qtip_Css + "\" /> ");

            if(RequestSpecificValues.Current_User.PermissionedAggregations !=null )
              foreach (User_Permissioned_Aggregation thisAggregation in RequestSpecificValues.Current_User.PermissionedAggregations.Where(ThisAggregation => ThisAggregation.OnHomePage))
              {
                  children_icons_added = true;
                RequestSpecificValues.Current_Mode.Aggregation = thisAggregation.Code.ToLower();
                string image_url = RequestSpecificValues.Current_Mode.Base_URL + "design/aggregations/" + thisAggregation.Code + "/images/buttons/coll.gif";

                if ((thisAggregation.Name.IndexOf("The ") == 0) && (thisAggregation.Name.Length > 4))
                {
					html_list[thisAggregation.Name.Substring(4)] = "    <td class=\"sbkAghsw_CollectionButton\">" + Environment.NewLine + "      <span class=\"sbkAghsw_CollectionButtonImg\"><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\"><img src=\"" + image_url + "\" alt=\"" + thisAggregation.Name.Replace("&", "&amp;").Replace("\"", "&quot;") + "\" /></a></span>" + Environment.NewLine + "      <span class=\"sbkAghsw_CollectionButtonTxt\"><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">" + thisAggregation.Name.Replace("&", "&amp;").Replace("\"", "&quot;") + "</a><br /><span class=\"MyHomeActionLink\" >( <a href=\"\" onclick=\"return remove_aggr_from_myhome('" + thisAggregation.Code + "');\">remove</a> )</span></span>" + Environment.NewLine + "    </td>";
                }
                else
                {
					html_list[thisAggregation.Name] = "    <td class=\"sbkAghsw_CollectionButton\">" + Environment.NewLine + "      <span class=\"sbkAghsw_CollectionButtonImg\"><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\"><img src=\"" + image_url + "\" alt=\"" + thisAggregation.Name.Replace("&", "&amp;").Replace("\"", "&quot;") + "\" /></a></span>" + Environment.NewLine + "      <span class=\"sbkAghsw_CollectionButtonTxt\"><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">" + thisAggregation.Name.Replace("&", "&amp;").Replace("\"", "&quot;") + "</a><br /><span class=\"MyHomeActionLink\" >( <a href=\"\" onclick=\"return remove_aggr_from_myhome('" + thisAggregation.Code + "');\">remove</a> )</span></span>" + Environment.NewLine + "    </td>";

                    if (thisAggregation.Code == "EPC")
                    {
						html_list[thisAggregation.Name] = "    <td class=\"sbkAghsw_CollectionButton\">" + Environment.NewLine + "      <span class=\"sbkAghsw_CollectionButtonImg\"><a href=\"http://www.uflib.ufl.edu/epc/\"><img src=\"" + image_url + "\" alt=\"" + thisAggregation.Name.Replace("&", "&amp;").Replace("\"", "&quot;") + "\" /></a></span>" + Environment.NewLine + "      <span class=\"sbkAghsw_CollectionButtonTxt\"><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">" + thisAggregation.Name.Replace("&", "&amp;").Replace("\"", "&quot;") + "</a><br /><span class=\"MyHomeActionLink\" >( <a href=\"\" onclick=\"return remove_aggr_from_myhome('" + thisAggregation.Code + "');\">remove</a> )</span></span>" + Environment.NewLine + "    </td>";
                    }
                    if (thisAggregation.Code == "UFHERB")
                    {
						html_list[thisAggregation.Name] = "    <td class=\"sbkAghsw_CollectionButton\">" + Environment.NewLine + "      <span class=\"sbkAghsw_CollectionButtonImg\"><a href=\"http://www.flmnh.ufl.edu/natsci/herbarium/cat/\"><img src=\"" + image_url + "\" alt=\"" + thisAggregation.Name.Replace("&", "&amp;").Replace("\"", "&quot;") + "\" /></a></span>" + Environment.NewLine + "      <span class=\"sbkAghsw_CollectionButtonTxt\"><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">" + thisAggregation.Name.Replace("&", "&amp;").Replace("\"", "&quot;") + "</a><br /><span class=\"MyHomeActionLink\" >( <a href=\"\" onclick=\"return remove_aggr_from_myhome('" + thisAggregation.Code + "');\">remove</a> )</span></span>" + Environment.NewLine + "    </td>";
                    }
                    if (thisAggregation.Code == "EXHIBITMATERIALS")
                    {
                        RequestSpecificValues.Current_Mode.Aggregation = "exhibits";
						html_list[thisAggregation.Name] = "    <td class=\"sbkAghsw_CollectionButton\">" + Environment.NewLine + "      <span class=\"sbkAghsw_CollectionButtonImg\"><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\"><img src=\"" + image_url + "\" alt=\"" + thisAggregation.Name.Replace("&", "&amp;").Replace("\"", "&quot;") + "\" /></a></span>" + Environment.NewLine + "      <span class=\"sbkAghsw_CollectionButtonTxt\"><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">" + thisAggregation.Name.Replace("&", "&amp;").Replace("\"", "&quot;") + "</a><br /><span class=\"MyHomeActionLink\" >( <a href=\"\" onclick=\"return remove_aggr_from_myhome('EXHIBITMATERIALS');\">remove</a> )</span></span>" + Environment.NewLine + "    </td>";
                    }
                }
            }

            // Write this theme
			Output.WriteLine("<br />");
            Output.WriteLine("<p>Welcome to your personalized " + RequestSpecificValues.Current_Mode.Instance_Abbreviation + " home page.  This page displays any collections you have added, as well as any of your bookshelves you have made public.</p>");
			Output.WriteLine("<h2>My Collections</h2>");

            // If there were any saves collections, show them here
            if (html_list.Count > 0)
            {
				Output.WriteLine("<table id=\"sbkAghsw_CollectionButtonTbl\">");
                int column_spot = 0;
                Output.WriteLine("  <tr>");

                foreach (string thisHtml in html_list.Values)
                {
                    if (column_spot == 3)
                    {
                        Output.WriteLine("  </tr>");
                        Output.WriteLine("  <tr>");
                        column_spot = 0;
                    }

                    Output.WriteLine(thisHtml);
                    column_spot++;
                }

				if (column_spot == 2)
				{
					Output.WriteLine("    <td class=\"sbkAghsw_CollectionButton\">&nbsp;</td>");
				}
				if (column_spot == 1)
				{
					Output.WriteLine("    <td class=\"sbkAghsw_CollectionButton\">&nbsp;</td>");
					Output.WriteLine("    <td class=\"sbkAghsw_CollectionButton\">&nbsp;</td>");
				}
                Output.WriteLine("  </tr>");
                Output.WriteLine("</table>");
                Output.WriteLine("<br />");
            }
            else
            {
                Output.WriteLine("<p>You do not have any collections added to your home page.<p>");
                Output.WriteLine("<p>To add a collection, use the <img src=\"" + RequestSpecificValues.Current_Mode.Base_URL + "design/skins/" + RequestSpecificValues.Current_Mode.Base_Skin_Or_Skin + "/buttons/add_rect_button.gif\" alt=\"ADD\" /> button from that collection's home page.</p>");
            }

            // Were there any public folders
            SortedList<string, string> public_folder_list = new SortedList<string, string>();
            RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Public_Folder;
            RequestSpecificValues.Current_Mode.Result_Display_Type = "brief";
            RequestSpecificValues.Current_Mode.Aggregation = String.Empty;

            foreach (User_Folder thisFolder in RequestSpecificValues.Current_User.All_Folders.Where(ThisFolder => ThisFolder.IsPublic))
            {
                children_icons_added = true;
                RequestSpecificValues.Current_Mode.FolderID = thisFolder.Folder_ID;
                if ((thisFolder.Folder_Name.IndexOf("The ") == 0) && (thisFolder.Folder_Name.Length > 4))
                {
					public_folder_list[thisFolder.Folder_Name.Substring(4)] = "    <td class=\"sbkAghsw_CollectionButton\">" + Environment.NewLine + "      <span class=\"sbkAghsw_CollectionButtonImg\"><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\"><img src=\"" + RequestSpecificValues.Current_Mode.Base_URL + "default/images/closed_folder_public_big.jpg\" alt=\"" + thisFolder.Folder_Name.Replace("&", "&amp;").Replace("\"", "&quot;") + "\" /></a></span>" + Environment.NewLine + "      <span class=\"sbkAghsw_CollectionButtonTxt\"><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">" + thisFolder.Folder_Name.Replace("&", "&amp;").Replace("\"", "&quot;") + "</a><br /><span class=\"MyHomeActionLink\" >( <a href=\"\" onclick=\"return make_folder_private('" + thisFolder.Folder_Name.Replace("&", "&amp;").Replace("\"", "&quot;") + "');\">make private</a> )</span></span>" + Environment.NewLine + "    </td>";
                }
                else
                {
					public_folder_list[thisFolder.Folder_Name] = "    <td class=\"sbkAghsw_CollectionButton\">" + Environment.NewLine + "      <span class=\"sbkAghsw_CollectionButtonImg\"><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\"><img src=\"" + RequestSpecificValues.Current_Mode.Base_URL + "default/images/closed_folder_public_big.jpg\" alt=\"" + thisFolder.Folder_Name.Replace("&", "&amp;").Replace("\"", "&quot;") + "\" /></a></span>" + Environment.NewLine + "      <span class=\"sbkAghsw_CollectionButtonTxt\"><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">" + thisFolder.Folder_Name.Replace("&", "&amp;").Replace("\"", "&quot;") + "</a><br /><span class=\"MyHomeActionLink\" >( <a href=\"\" onclick=\"return make_folder_private('" + thisFolder.Folder_Name.Replace("&", "&amp;").Replace("\"", "&quot;") + "');\">make private</a> )</span></span>" + Environment.NewLine + "    </td>";
                }
            }

			RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Aggregation;
			RequestSpecificValues.Current_Mode.Aggregation_Type = Aggregation_Type_Enum.Home;

            // if there were any public folders
            if (public_folder_list.Count > 0)
            {
                // Write this theme
				Output.WriteLine("<h2>My Public Bookshelves</h2>");

				Output.WriteLine("<table id=\"sbkAghsw_PublicBookshelvesTbl\">");
                int column_spot2 = 0;
                Output.WriteLine("  <tr>");

                foreach (string thisHtml in public_folder_list.Values)
                {
                    if (column_spot2 == 3)
                    {
                        Output.WriteLine("  </tr>");
                        Output.WriteLine("  <tr>");
                        column_spot2 = 0;
                    }

                    Output.WriteLine(thisHtml);
                    column_spot2++;
                }

				if (column_spot2 == 2)
				{
					Output.WriteLine("    <td class=\"sbkAghsw_CollectionButton\">&nbsp;</td>");
				}
				if (column_spot2 == 1)
				{
					Output.WriteLine("    <td class=\"sbkAghsw_CollectionButton\">&nbsp;</td>");
					Output.WriteLine("    <td class=\"sbkAghsw_CollectionButton\">&nbsp;</td>");
				}
                Output.WriteLine("  </tr>");
                Output.WriteLine("</table>");
                Output.WriteLine("<br />");
            }

            // Add some of the static links
			Output.WriteLine("<h2>My Links</h2>");
			Output.WriteLine("<table id=\"sbkAghsw_MyLinksTbl\">");
            Output.WriteLine("  <tr>");

            children_icons_added = true;
            RequestSpecificValues.Current_Mode.Aggregation = String.Empty;
            RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.My_Sobek;
            RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Home;
			Output.WriteLine("    <td class=\"sbkAghsw_CollectionButton\">" + Environment.NewLine + "      <span class=\"sbkAghsw_CollectionButtonImg\"><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\"><img src=\"" + Static_Resources_Gateway.Home_Button_Gif + "\" alt=\"Go to my" + RequestSpecificValues.Current_Mode.Instance_Abbreviation + " home\" /></a></span>" + Environment.NewLine + "      <span class=\"sbkAghsw_CollectionButtonTxt\"><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">my" + RequestSpecificValues.Current_Mode.Instance_Abbreviation + " Home</a></span>" + Environment.NewLine + "    </td>");

            RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Folder_Management;
			Output.WriteLine("    <td class=\"sbkAghsw_CollectionButton\">" + Environment.NewLine + "      <span class=\"sbkAghsw_CollectionButtonImg\"><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\"><img src=\"" + Static_Resources_Gateway.Big_Bookshelf_Img + "\" alt=\"Go to my bookshelf\" /></a></span>" + Environment.NewLine + "      <span class=\"sbkAghsw_CollectionButtonTxt\"><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">My Library</a></span>" + Environment.NewLine + "    </td>");

            RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Saved_Searches;
			Output.WriteLine("    <td class=\"sbkAghsw_CollectionButton\">" + Environment.NewLine + "      <span class=\"sbkAghsw_CollectionButtonImg\"><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\"><img src=\"" + Static_Resources_Gateway.Saved_Searches_Img + "\" alt=\"Go to my saved searches\" /></a></span>" + Environment.NewLine + "      <span class=\"sbkAghsw_CollectionButtonTxt\"><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">My Saved Searches</a></span>" + Environment.NewLine + "    </td>");

			RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Aggregation;
			RequestSpecificValues.Current_Mode.Aggregation_Type = Aggregation_Type_Enum.Home;

            Output.WriteLine("  </tr>");
            Output.WriteLine("</table>");
            Output.WriteLine("<br />");

            Output.WriteLine("<!-- End of Aggregation_HtmlSubwriter.write_personalized_home -->");

            RequestSpecificValues.Current_Mode.Aggregation = String.Empty;
        }

        #endregion

        #region Methods to show the institution home page

        /// <summary> Adds the partner institution page from the main library home page as small icons and html names </summary>
        /// <param name="Output"> Stream to which to write the HTML for this subwriter </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        protected internal void write_institution_list(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace("Aggregation_HtmlSubwriter.write_institution_list", "Entered...");
            Output.WriteLine("<!-- Start of Aggregation_HtmlSubwriter.write_institution_list -->");

            // Build the list of html to display, first adding collections and subcollections
            SortedList<string, string> html_list = new SortedList<string, string>();

            // Get the institutions
            ReadOnlyCollection<Item_Aggregation_Related_Aggregations> institutions = UI_ApplicationCache_Gateway.Aggregations.Aggregations_By_Type("Institution");
            int aggreCount = -1;
            foreach (Item_Aggregation_Related_Aggregations thisAggr in institutions)
            {
                if ( thisAggr.Active )
                {
                    aggreCount++;
                    string name = thisAggr.ShortName.Replace("&", "&amp;").Replace("\"", "&quot;");
                    if (name.ToUpper() == "ADDED AUTOMATICALLY")
                        name = thisAggr.Code + " ( Added Automatically )";

                    if ((thisAggr.Code.Length > 2) && (thisAggr.Code[0] == 'I'))
                    {
                        RequestSpecificValues.Current_Mode.Aggregation = thisAggr.Code.ToLower();
                        string image_url = RequestSpecificValues.Current_Mode.Base_URL + "design/aggregations/" + thisAggr.Code + "/images/buttons/coll.gif";
                        if ((name.IndexOf("The ") == 0) && (name.Length > 4))
                        {
                            html_list[name.Substring(4)] = "    <li class=\"sbkAghsw_CollectionButton\">" + Environment.NewLine + "      <span id=\"sbkAghsw_CollectionButtonImg" + aggreCount + "\" class=\"sbkAghsw_CollectionButtonImg\"><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\"><img src=\"" + image_url + "\" alt=\"" + name + "\" /></a></span>" + Environment.NewLine + "      <span class=\"sbkAghsw_CollectionButtonTxt\"><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">" + name + "</a></span>" + Environment.NewLine + "    </li>";
                        }
                        else
                        {
							html_list[name] = "    <li class=\"sbkAghsw_CollectionButton\">" + Environment.NewLine + "      <span class=\"sbkAghsw_CollectionButtonImg\"><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\"><img src=\"" + image_url + "\" alt=\"" + name + "\" /></a></span>" + Environment.NewLine + "      <span class=\"sbkAghsw_CollectionButtonTxt\"><a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">" + name + "</a></span>" + Environment.NewLine + "    </li>";
                        }
                    }
                }
            }

            RequestSpecificValues.Current_Mode.Aggregation = String.Empty;

            if (html_list.Count > 0)
            {
                // Write this theme
				Output.WriteLine("<h2 style=\"margin-top:0;\">Partners and Contributing Institutions</h2>");

				Output.WriteLine("<ul class=\"sbkAghsw_CollectionButtonList\">");

                foreach (string thisHtml in html_list.Values)
                {
                    Output.WriteLine(thisHtml);
                }

                Output.WriteLine("</ul>");
            }

            Output.WriteLine("<br />");

            Output.WriteLine("<!-- Start of Aggregation_HtmlSubwriter.write_institution_list -->");
        }

        /// <summary> Adds the partner institution page from the main library home page as large icons </summary>
        /// <param name="Output"> Stream to which to write the HTML for this subwriter </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        protected internal void write_institution_icons(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace("Aggregation_HtmlSubwriter.write_institution_icons", "Entered...");
            Output.WriteLine("<!-- Start of Aggregation_HtmlSubwriter.write_institution_icons -->");

            // Build the list of html to display, first adding collections and subcollections
            SortedList<string, string> html_list = new SortedList<string, string>();

            // Get the institutions
            ReadOnlyCollection<Item_Aggregation_Related_Aggregations> institutions = UI_ApplicationCache_Gateway.Aggregations.Aggregations_By_Type("Institution");

            foreach (Item_Aggregation_Related_Aggregations thisAggr in institutions)
            {
                if (thisAggr.Active)
                {
                    string name = thisAggr.ShortName.Replace("&", "&amp;").Replace("\"", "&quot;");
                    if (name.ToUpper() == "ADDED AUTOMATICALLY")
                        name = thisAggr.Code + " ( Added Automatically )";

                    if ((thisAggr.Code.Length > 2) && (thisAggr.Code[0] == 'I'))
                    {
                        RequestSpecificValues.Current_Mode.Aggregation = thisAggr.Code.ToLower();
                        string image_url = RequestSpecificValues.Current_Mode.Base_URL + "design/aggregations/" + thisAggr.Code + "/images/buttons/" + thisAggr.Code.Substring(1) + ".gif";
                        html_list[name] = "    <td>" + Environment.NewLine + "      <a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode).Replace("&", "&amp;") + "\"><img src=\"" + image_url + "\" alt=\"" + name + "\" /></a></td>";
                    }
                }
            }

            RequestSpecificValues.Current_Mode.Aggregation = String.Empty;

            if (html_list.Count > 0)
            {
                // Write this theme
				Output.WriteLine("<h2 style=\"margin-top:0;\">Partners and Contributing Institutions</h2>");

				Output.WriteLine("<table id=\"sbkAghsw_CollectionButtonTbl\">");
                int column_spot = 0;
                Output.WriteLine("  <tr style=\"text-align:center;vertical-align:middle;\">");

                foreach (string thisHtml in html_list.Values)
                {
                    if (column_spot == 4)
                    {
                        Output.WriteLine("  </tr>");
						Output.WriteLine("  <tr style=\"text-align:center;vertical-align:middle;\">");
                        column_spot = 0;
                    }

                    Output.WriteLine(thisHtml);
                    column_spot++;
                }

                if (column_spot == 3)
                {
					Output.WriteLine("    <td class=\"sbkAghsw_CollectionButton\">&nbsp;</td>");
                }
				if (column_spot == 2)
				{
					Output.WriteLine("    <td class=\"sbkAghsw_CollectionButton\">&nbsp;</td>");
					Output.WriteLine("    <td class=\"sbkAghsw_CollectionButton\">&nbsp;</td>");
				}
				if (column_spot == 1)
				{
					Output.WriteLine("    <td class=\"sbkAghsw_CollectionButton\">&nbsp;</td>");
					Output.WriteLine("    <td class=\"sbkAghsw_CollectionButton\">&nbsp;</td>");
					Output.WriteLine("    <td class=\"sbkAghsw_CollectionButton\">&nbsp;</td>");
				}

                Output.WriteLine("  </tr>");
                Output.WriteLine("</table>");
            }

            Output.WriteLine("<br />");

            Output.WriteLine("<!-- End of Aggregation_HtmlSubwriter.write_institution_icons -->");
        }

        #endregion

        #endregion

        #endregion

        /// <summary> Writes final HTML after all the forms </summary>
        /// <param name="Output">Stream to directly write to</param>
        /// <param name="Tracer">Trace object keeps a list of each method executed and important milestones in rendering</param>
        public override void Write_Final_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace("Aggregation_HtmlSubwriter.Write_Final_HTML", "Entered...");
            Output.WriteLine("<!-- Start of Aggregation_HtmlSubwriter.Write_Final_HTML -->");

            if ((collectionViewer != null) && (collectionViewer.Type != Item_Aggregation_Views_Searches_Enum.Custom_Home_Page))
            {
                Output.WriteLine("<!-- Close the pagecontainer div -->");
                Output.WriteLine("</div>");
                Output.WriteLine();

                // Add the scripts needed
                Output.WriteLine("<script type=\"text/javascript\" src=\"" + Static_Resources_Gateway.Jquery_Ui_1_10_3_Draggable_Js + "\"></script>");

                // NOTE: The jquery.hovercard.min.js file included below has been modified for SobekCM, and also includes a bug fix. DO NOT REPLACE with another version

                if (children_icons_added)
                {
                    Tracer.Add_Trace("Aggregation_HtmlSubwriter.Write_Final_HTML", "Child icons were added, adding script support for jQuery Hovercard.");

                    Output.WriteLine("<script type=\"text/javascript\" src=\"" + Static_Resources_Gateway.Jquery_Hovercard_Js + "\"></script>");
                    Output.WriteLine("<script type=\"text/javascript\">");
                    Output.WriteLine("    $(document).ready(function () {");
                    Output.WriteLine("        $('[id*=sbkAghsw_CollectionButtonImg]').each(function () {");
                    Output.WriteLine("            var $this = $(this);");
                    Output.WriteLine("            var hovercardTitle = '<div style=\"display:inline; float:left; font-weight:bold;margin-left:70px;margin-top:-10px;\" class=\"sbkAghsw_CollectionButtonTxt\"><a href=' + $this.find('a').attr('href') + '>' + $this.find('img').attr('alt') + '</a></div><br/>';");
                    Output.WriteLine("            var hovercardHTML = '<div style=\"display:inline;margin:70px;\">' + $this.find('.spanHoverText').text() + '</div><br/>';");
                    Output.WriteLine("            $this.hovercard({detailsHTML: hovercardTitle+hovercardHTML, width: 300, openOnLeft: false,autoAdjust: false, delay:0 }); ");
                    Output.WriteLine("        });");
                    Output.WriteLine("    });");
                    Output.WriteLine("</script>");
                    Output.WriteLine();
                }

                if (datasetBrowseResultsStats != null && datasetBrowseResultsStats.Total_Items > 0)
                {
                    RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Write_Final_HTML", "datasetBrowseResultsStats is not null and has items, adding navigation buttons.");
                    // Get the values for the <%LEFTBUTTONS%> and <%RIGHTBUTTONS%>
                    string LEFT_BUTTONS = String.Empty;
                    string RIGHT_BUTTONS = String.Empty;
                    string first_page = "First Page";
                    string previous_page = "Previous Page";
                    string next_page = "Next Page";
                    string last_page = "Last Page";
                    string first_page_text = "First";
                    string previous_page_text = "Previous";
                    string next_page_text = "Next";
                    string last_page_text = "Last";

                    #region Determine the Next, Last, First, Previous buttons display

                    //if(datasetBrowseResultsStats.)
                    ushort current_page = RequestSpecificValues.Current_Mode.Page.HasValue ? RequestSpecificValues.Current_Mode.Page.Value : (ushort) 1;
                    StringBuilder buttons_builder = new StringBuilder(1000);

                    if (current_page > 1)
                    {
                        buttons_builder.Append("<div class=\"sbkPrsw_LeftButtons\">");
                        RequestSpecificValues.Current_Mode.Page = 1;
                        buttons_builder.Append("<button title=\"" + first_page + "\" class=\"sbkPrsw_RoundButton\" onclick=\"window.location='" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode).Replace("&", "&amp;") + "'; return false;\"><img src=\"" + Static_Resources_Gateway.Button_First_Arrow_Png + "\" class=\"roundbutton_img_left\" alt=\"\" />" + first_page_text + "</button>&nbsp;");
                        RequestSpecificValues.Current_Mode.Page = (ushort) (current_page - 1);
                        buttons_builder.Append("<button title=\"" + previous_page + "\" class=\"sbkPrsw_RoundButton\" onclick=\"window.location='" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode).Replace("&", "&amp;") + "'; return false;\"><img src=\"" + Static_Resources_Gateway.Button_Previous_Arrow_Png + "\" class=\"roundbutton_img_left\" alt=\"\" />" + previous_page_text + "</button>");
                        buttons_builder.Append("</div>");
                        LEFT_BUTTONS = buttons_builder.ToString();
                        buttons_builder.Clear();
                    }
                    else
                    {
                        LEFT_BUTTONS = "<div class=\"sbkPrsw_NoLeftButtons\">&nbsp;</div>";
                    }

                    // Should the next and last buttons be enabled?
                    if ((current_page*RESULTS_PER_PAGE) < datasetBrowseResultsStats.Total_Titles)
                    {
                        buttons_builder.Append("<div class=\"sbkPrsw_RightButtons\">");
                        RequestSpecificValues.Current_Mode.Page = (ushort) (current_page + 1);
                        buttons_builder.Append("<button title=\"" + next_page + "\" class=\"sbkPrsw_RoundButton\" onclick=\"window.location='" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode).Replace("&", "&amp;") + "'; return false;\">" + next_page_text + "<img src=\"" + Static_Resources_Gateway.Button_Next_Arrow_Png + "\" class=\"roundbutton_img_right\" alt=\"\" /></button>&nbsp;");
                        RequestSpecificValues.Current_Mode.Page = (ushort) (datasetBrowseResultsStats.Total_Titles/RESULTS_PER_PAGE);
                        if (datasetBrowseResultsStats.Total_Titles%RESULTS_PER_PAGE > 0)
                            RequestSpecificValues.Current_Mode.Page = (ushort) (RequestSpecificValues.Current_Mode.Page + 1);
                        buttons_builder.Append("<button title=\"" + last_page + "\" class=\"sbkPrsw_RoundButton\" onclick=\"window.location='" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode).Replace("&", "&amp;") + "'; return false;\">" + last_page_text + "<img src=\"" + Static_Resources_Gateway.Button_Last_Arrow_Png + "\" class=\"roundbutton_img_right\" alt=\"\" /></button>");
                        buttons_builder.Append("</div>");
                        RIGHT_BUTTONS = buttons_builder.ToString();
                    }
                    else
                    {
                        RIGHT_BUTTONS = "<div class=\"sbkPrsw_NoRightButtons\">&nbsp;</div>";
                    }
                    // Save the buttons for later, to be used at the bottom of the page
                    leftButtons = LEFT_BUTTONS;
                    rightButtons = RIGHT_BUTTONS;

                    RequestSpecificValues.Current_Mode.Page = current_page;

                    #endregion

                    Output.WriteLine("<div class=\"sbkPrsw_ResultsNavBar\">");
                    Output.Write(leftButtons);
                    //Output.WriteLine("  " + Showing_Text);
                    Output.Write(rightButtons);
                    Output.WriteLine("</div>");
                    Output.WriteLine("<br />");
                    Output.WriteLine();
                }
                else
                {
                    RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Write_Final_HTML", "datasetBrowseResultsStats is null or has no items.");
                }
            }

            Output.WriteLine("<!-- End of Aggregation_HtmlSubwriter.Write_Final_HTML -->");
        }

        /// <summary> Gets the CSS class of the container that the page is wrapped within </summary>
        public override string Container_CssClass
		{
			get
			{
			    if (collectionViewer is Tiles_Home_AggregationViewer)
			        return "container-tiles";

				switch (RequestSpecificValues.Current_Mode.Aggregation_Type)
				{
					case Aggregation_Type_Enum.Browse_By:
						return "container-facets";

					case Aggregation_Type_Enum.Browse_Map:
						return "container-inner1000";

                    case Aggregation_Type_Enum.Browse_Map_Beta:
                        return "container-inner1000";

					case Aggregation_Type_Enum.Private_Items:
						return "container-inner1215";

					case Aggregation_Type_Enum.Browse_Info:
						if (pagedResults != null)
						{
							return "container-facets";
						}
						break;

                    case Aggregation_Type_Enum.User_Permissions:
				        if (UI_ApplicationCache_Gateway.Settings.System.Detailed_User_Aggregation_Permissions)
				        {
				            return "container-inner1215";
				        }
                        break;
				}

				return base.Container_CssClass;
			}
		}

        private string Highlight_To_Html( Item_Aggregation_Highlights Highlight, string Directory )
        {
            RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Highlight_To_Html", "Entered...");

            StringBuilder highlightBldr = new StringBuilder(500);
            highlightBldr.Append("<span id=\"SobekHighlight\">" + Environment.NewLine );
            highlightBldr.Append("  <table>" + Environment.NewLine );
            highlightBldr.Append("    <tr><td>" + Environment.NewLine );

            if ( !String.IsNullOrEmpty(Highlight.Link))
            {
                if (Highlight.Link.IndexOf("?") > 0)
                {
                    highlightBldr.Append("      <a href=\"" + Highlight.Link + "<%&URLOPTS%>\" title=\"" + Highlight.Tooltip + "\">" + Environment.NewLine);
                }
                else
                {
                    highlightBldr.Append("      <a href=\"" + Highlight.Link + "<%?URLOPTS%>\" title=\"" + Highlight.Tooltip + "\">" + Environment.NewLine);
                }

                highlightBldr.Append("        <img src=\"" + Directory + Highlight.Image + "\" alt=\"" + Highlight.Tooltip + "\"/>" + Environment.NewLine);
                highlightBldr.Append("      </a>" + Environment.NewLine );
            }
            else
            {
                highlightBldr.Append("      <img src=\"" + Directory + Highlight.Image + "\" alt=\"" + Highlight.Tooltip + "\"/>" + Environment.NewLine);
            }

            highlightBldr.Append("    </td></tr>" + Environment.NewLine );

            if ( !String.IsNullOrEmpty(Highlight.Text))
            {
                highlightBldr.Append("    <tr><td>" + Environment.NewLine );

                if ( !String.IsNullOrEmpty(Highlight.Link))
                {
                    if (Highlight.Link.IndexOf("?") > 0)
                    {
                        highlightBldr.Append("      <a href=\"" + Highlight.Link + "<%&URLOPTS%>\" title=\"" + Highlight.Tooltip + "\">" + Environment.NewLine);
                    }
                    else
                    {
                        highlightBldr.Append("      <a href=\"" + Highlight.Link + "<%?URLOPTS%>\" title=\"" + Highlight.Tooltip + "\">" + Environment.NewLine);
                    }

                    highlightBldr.Append("        <span class=\"SobekHighlightText\"> " + Highlight.Text + " </span>" + Environment.NewLine );
                    highlightBldr.Append("      </a>" + Environment.NewLine );
                }
                else
                {
                    highlightBldr.Append("      <span class=\"SobekHighlightText\"> " + Highlight.Text + " </span>" + Environment.NewLine );
                }

                highlightBldr.Append("    </td></tr>" + Environment.NewLine );
            }

            highlightBldr.Append("  </table>" + Environment.NewLine );
            highlightBldr.Append("</span>");

            return highlightBldr.ToString();
        }

        /// <summary> Add the footer to the output </summary>
        /// <param name="Output"> Stream to which to write the HTML for this footer </param>
        public override void Add_Footer(TextWriter Output)
        {
            RequestSpecificValues.Tracer.Add_Trace("Aggregation_HtmlSubwriter.Add_Footer", "Entered...");

            Output.WriteLine("<!-- Start of Aggregation_HtmlSubwriter.Add_Footer -->");
            HeaderFooter_Helper_HtmlSubWriter.Add_Footer(Output, RequestSpecificValues, Subwriter_Behaviors, hierarchyObject, null);
            Output.WriteLine("<!-- End of Aggregation_HtmlSubwriter.Add_Footer -->");
        }
    }
}
