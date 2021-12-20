﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Web.UI.WebControls;
using SobekCM.Core.BriefItem;
using SobekCM.Core.Navigation;
using SobekCM.Core.UI_Configuration;
using SobekCM.Core.UI_Configuration.StaticResources;
using SobekCM.Core.Users;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Library.ItemViewer.Menu;
using SobekCM.Library.UI;
using SobekCM.Tools;

namespace SobekCM.Library.ItemViewer.Viewers
{
    /// <summary> Manage menu item viewer prototyper, which is used to check to see if a user has access to control and
    /// edit this item, to create the link in the main menu, and to create the viewer itself if the user selects that option </summary>
    public class ManageMenu_ItemViewer_Prototyper : iItemViewerPrototyper
    {
        /// <summary> Constructor for a new instance of the ManageMenu_ItemViewer_Prototyper class </summary>
        public ManageMenu_ItemViewer_Prototyper()
        {
            ViewerType = "MANAGE_MENU";
            ViewerCode = "manage";
        }

        /// <summary> Name of this viewer, which matches the viewer name from the database and 
        /// in the configuration files as well.  This is actually populate by the configuration information </summary>
        public string ViewerType { get; set; }

        /// <summary> Code for this viewer, which can also be set from the configuration information </summary>
        public string ViewerCode { get; set; }

        /// <summary> If this viewer is tied to certain files existing in the digital resource, this lists all the 
        /// possible file extensions this supports (from the configuration file usually) </summary>
        public string[] FileExtensions { get; set; }

        /// <summary> Indicates if the specified item matches the basic requirements for this viewer, or
        /// if this viewer should be ignored for this item </summary>
        /// <param name="CurrentItem"> Digital resource to examine to see if this viewer really should be included </param>
        /// <returns> TRUE if this viewer should generally be included with this item, otherwise FALSE </returns>
        public virtual bool Include_Viewer(BriefItemInfo CurrentItem)
        {
            // This should always be included (although it won't be accessible or shown to everyone)
            return true;
        }

        /// <summary> Flag indicates if this viewer should be override on checkout </summary>
        /// <param name="CurrentItem"> Digital resource to examine to see if this viewer should really be overriden </param>
        /// <returns> TRUE always, since PDFs should never be shown if an item is checked out </returns>
        public virtual bool Override_On_Checkout(BriefItemInfo CurrentItem)
        {
            return false;
        }

        /// <summary> Flag indicates if the current user has access to this viewer for the item </summary>
        /// <param name="CurrentItem"> Digital resource to see if the current user has correct permissions to use this viewer </param>
        /// <param name="CurrentUser"> Current user, who may or may not be logged on </param>
        /// <param name="IsRestricted"> Flag indicates if this item is restricted AND the current user is outside the ranges or not in the proper groups</param>
        /// <returns> TRUE if the user has access to use this viewer, otherwise FALSE </returns>
        public virtual bool Has_Access(BriefItemInfo CurrentItem, User_Object CurrentUser, bool IsRestricted)
        {
            // If there is no user (or they aren't logged in) then obviously, they can't edit this
            if ((CurrentUser == null) || ( !CurrentUser.LoggedOn ))
            {
                return false;
            }

            // See if this user can edit this item
            bool userCanEditItem = CurrentUser.Can_Edit_This_Item(CurrentItem.BibID, CurrentItem.Type, CurrentItem.Behaviors.Source_Institution_Aggregation, CurrentItem.Behaviors.Holding_Location_Aggregation, CurrentItem.Behaviors.Aggregation_Code_List);
            if (!userCanEditItem)
            {
                // Can't edit, so don't show and return FALSE
                return false;
            }

            // Apparently it can be shown
            return true;
        }

        /// <summary> Gets the menu items related to this viewer that should be included on the main item (digital resource) menu </summary>
        /// <param name="CurrentItem"> Digital resource object, which can be used to ensure if and how this viewer should appear 
        /// in the main item (digital resource) menu </param>
        /// <param name="CurrentUser"> Current user, who may or may not be logged on </param>
        /// <param name="CurrentRequest"> Information about the current request </param>
        /// <param name="MenuItems"> List of menu items, to which this method may add one or more menu items </param>
        /// <param name="IsRestricted"> Flag indicates if this item is restricted AND the current user is outside the ranges or not in the proper groups</param> 
        public virtual void Add_Menu_Items(BriefItemInfo CurrentItem, User_Object CurrentUser, Navigation_Object CurrentRequest, List<Item_MenuItem> MenuItems, bool IsRestricted )
        {
            // Again, ensure access
            if (!Has_Access(CurrentItem, CurrentUser, false))
                return;

            // Set some basic permission flags based on the user's settings
            bool show_behaviors_if_permissioned = CurrentUser.Get_Setting(User_Setting_Constants.ItemViewer_ShowBehaviors, true);
            bool show_qc_if_permissioned = CurrentUser.Get_Setting(User_Setting_Constants.ItemViewer_ShowQc, true);
            bool show_permission_changes_if_permissions = CurrentUser.Get_Setting(User_Setting_Constants.ItemViewer_AllowPermissionChanges, true);

            // Get the URL for this
            string previous_code = CurrentRequest.ViewerCode;
            CurrentRequest.ViewerCode = ViewerCode;
            string url = UrlWriterHelper.Redirect_URL(CurrentRequest);
            CurrentRequest.ViewerCode = previous_code;
            MenuItems.Add(new Item_MenuItem("Manage", "Management Menu", null, url, "manage"));

            bool is_bib_level = (String.Compare(CurrentItem.Type, "BIB_LEVEL", StringComparison.OrdinalIgnoreCase) == 0);
            if (!is_bib_level)
            {
                // Look for TEI-type item
                bool is_tei = false;
                if ((UI_ApplicationCache_Gateway.Configuration.Extensions != null) &&
                    (UI_ApplicationCache_Gateway.Configuration.Extensions.Get_Extension("TEI") != null) &&
                    (UI_ApplicationCache_Gateway.Configuration.Extensions.Get_Extension("TEI").Enabled))
                {
                    string tei_file = CurrentItem.Behaviors.Get_Setting("TEI.Source_File");
                    string xslt_file = CurrentItem.Behaviors.Get_Setting("TEI.XSLT");
                    if ((tei_file != null) && (xslt_file != null))
                    {
                        is_tei = true;
                    }
                }

                if (!is_tei)
                {
                    // Add the menu item for editing metadatga
                    CurrentRequest.Mode = Display_Mode_Enum.My_Sobek;
                    CurrentRequest.My_Sobek_Type = My_Sobek_Type_Enum.Edit_Item_Metadata;
                    string edit_metadata_url = UrlWriterHelper.Redirect_URL(CurrentRequest);
                    MenuItems.Add(new Item_MenuItem("Manage", "Edit Metadata", null, edit_metadata_url, "nevermatchthis"));
                }
                else
                {
                    // Add the menu item for editing metadatga
                    CurrentRequest.Mode = Display_Mode_Enum.My_Sobek;
                    CurrentRequest.My_Sobek_Type = My_Sobek_Type_Enum.Edit_TEI_Item;
                    string edit_tei_url = UrlWriterHelper.Redirect_URL(CurrentRequest);
                    MenuItems.Add(new Item_MenuItem("Manage", "Edit TEI", null, edit_tei_url, "nevermatchthis"));
                }

                // Add the menu item for editing item behaviors
                if (show_behaviors_if_permissioned)
                {
                    CurrentRequest.My_Sobek_Type = My_Sobek_Type_Enum.Edit_Item_Behaviors;
                    string edit_behaviors_url = UrlWriterHelper.Redirect_URL(CurrentRequest);
                    MenuItems.Add(new Item_MenuItem("Manage", "Edit Item Behaviors", null, edit_behaviors_url, "nevermatchthis"));
                }

                // Add the menu item for managing download files
                CurrentRequest.My_Sobek_Type = My_Sobek_Type_Enum.File_Management;
                string manage_downloads = UrlWriterHelper.Redirect_URL(CurrentRequest);
                MenuItems.Add(new Item_MenuItem("Manage", "Manage Download Files", null, manage_downloads, "nevermatchthis"));

                // Add the menu item for managing pages and divisions
                if (show_qc_if_permissioned)
                {
                    if ((CurrentItem.Images == null) || (CurrentItem.Images.Count == 0))
                    {
                        CurrentRequest.My_Sobek_Type = My_Sobek_Type_Enum.Page_Images_Management;
                        string page_images_url = UrlWriterHelper.Redirect_URL(CurrentRequest);
                        MenuItems.Add(new Item_MenuItem("Manage", "Manage Pages and Divisions", null, page_images_url, "nevermatchthis"));
                    }
                    else
                    {
                        CurrentRequest.Mode = Display_Mode_Enum.Item_Display;
                        CurrentRequest.ViewerCode = "qc";
                        string qc_url = UrlWriterHelper.Redirect_URL(CurrentRequest);
                        MenuItems.Add(new Item_MenuItem("Manage", "Manage Pages and Divisions", null, qc_url, "nevermatchthis"));
                    }
                }

                // Add the manage geo-spatial data option
                if (UI_ApplicationCache_Gateway.Settings.Resources.Manage_GeoSpatial_Data)
                {
                    CurrentRequest.Mode = Display_Mode_Enum.Item_Display;
                    CurrentRequest.ViewerCode = "mapedit";
                    string mapedit_url = UrlWriterHelper.Redirect_URL(CurrentRequest);
                    MenuItems.Add(new Item_MenuItem("Manage", "Manage Geo-Spatial Data (beta)", null, mapedit_url, "mapedit"));
                }

                // Add the tracking sheet menu option
                if (UI_ApplicationCache_Gateway.Settings.Resources.Use_Tracking_Sheet)
                {
                    CurrentRequest.ViewerCode = "ts";
                    string tracking_url = UrlWriterHelper.Redirect_URL(CurrentRequest);
                    MenuItems.Add(new Item_MenuItem("Manage", "View Tracking Sheet", null, tracking_url, "ts"));
                }
            }
            else
            {
                // Get all the mySObek URLs
                CurrentRequest.Mode = Display_Mode_Enum.My_Sobek;

                // Add the group behavior edit
                CurrentRequest.My_Sobek_Type = My_Sobek_Type_Enum.Edit_Group_Behaviors;
                string edit_behaviors_url = UrlWriterHelper.Redirect_URL(CurrentRequest);
                MenuItems.Add(new Item_MenuItem("Manage", "Edit Item Group Behaviors", null, edit_behaviors_url, "nevermatchthis"));

                // Add the option to add a new volume
                CurrentRequest.My_Sobek_Type = My_Sobek_Type_Enum.Group_Add_Volume;
                string add_volume_url = UrlWriterHelper.Redirect_URL(CurrentRequest);
                MenuItems.Add(new Item_MenuItem("Manage", "Add New Volume", null, add_volume_url, "nevermatchthis"));

                // Add the option for group mass update
                if (UI_ApplicationCache_Gateway.Settings.Resources.Allow_Behavior_Mass_Update)
                {
                    CurrentRequest.My_Sobek_Type = My_Sobek_Type_Enum.Group_Mass_Update_Items;
                    string mass_update_url = UrlWriterHelper.Redirect_URL(CurrentRequest);
                    MenuItems.Add(new Item_MenuItem("Manage", "Mass Update Item Behaviors", null, mass_update_url, "nevermatchthis"));
                }
            }

            CurrentRequest.Mode = Display_Mode_Enum.Item_Display;
            CurrentRequest.ViewerCode = previous_code;
        }

        /// <summary> Creates and returns the an instance of the <see cref="ManageMenu_ItemViewer"/> class for showing the
        /// administrative management menu for a digital resource during execution of a single HTTP request. </summary>
        /// <param name="CurrentItem"> Digital resource object </param>
        /// <param name="CurrentUser"> Current user, who may or may not be logged on </param>
        /// <param name="CurrentRequest"> Information about the current request </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> Fully built and initialized <see cref="ManageMenu_ItemViewer"/> object </returns>
        /// <remarks> This method is called whenever a request requires the actual viewer to be created to render the HTML for
        /// the digital resource requested.  The created viewer is then destroyed at the end of the request </remarks>
        public virtual iItemViewer Create_Viewer(BriefItemInfo CurrentItem, User_Object CurrentUser, Navigation_Object CurrentRequest, Custom_Tracer Tracer)
        {
            return new ManageMenu_ItemViewer(CurrentItem, CurrentUser, CurrentRequest);
        }
    }

    /// <summary> Item viewer displays the administrative management menu for working with a digital resource </summary>
    /// <remarks> This class extends the abstract class <see cref="abstractNoPaginationItemViewer"/> and implements the 
    /// <see cref="iItemViewer" /> interface. </remarks>
    public class ManageMenu_ItemViewer : abstractNoPaginationItemViewer
    {
        /// <summary> Constructor for a new instance of the ManageMenu_ItemViewer class, used to display the 
        /// administrative management menu for the digital resource </summary>
        /// <param name="BriefItem"> Digital resource object </param>
        /// <param name="CurrentUser"> Current user, who may or may not be logged on </param>
        /// <param name="CurrentRequest"> Information about the current request </param>
        public ManageMenu_ItemViewer(BriefItemInfo BriefItem, User_Object CurrentUser, Navigation_Object CurrentRequest)
        {
            // Save the arguments for use later
            this.BriefItem = BriefItem;
            this.CurrentUser = CurrentUser;
            this.CurrentRequest = CurrentRequest;

            // Set the behavior properties to the empy behaviors ( in the base class )
            Behaviors = EmptyBehaviors;
        }

        /// <summary> CSS ID for the viewer viewport for this particular viewer </summary>
        /// <value> This always returns the value 'sbkMmiv_Viewer' </value>
        public override string ViewerBox_CssId
        {
            get { return "sbkMmiv_Viewer"; }
        }

        /// <summary> Write the item viewer main section as HTML directly to the HTTP output stream </summary>
        /// <param name="Output"> Response stream for the item viewer to write directly to </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        public override void Write_Main_Viewer_Section(TextWriter Output, Custom_Tracer Tracer)
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("ManageMenu_ItemViewer.Write_Main_Viewer_Section", "");
            }

            string currentViewerCode = CurrentRequest.ViewerCode;

            // Set some basic permission flags based on the user's settings
            bool show_behaviors_if_permissioned = CurrentUser.Get_Setting(User_Setting_Constants.ItemViewer_ShowBehaviors, true);
            bool show_qc_if_permissioned = CurrentUser.Get_Setting(User_Setting_Constants.ItemViewer_ShowQc, true);


            // Add the HTML for the image
            Output.WriteLine("<!-- MANAGE MENU ITEM VIEWER OUTPUT -->");

            bool is_bib_level = (String.Compare(BriefItem.Type, "BIB_LEVEL", StringComparison.OrdinalIgnoreCase) == 0);
            if (!is_bib_level)
            {
                // Look for TEI-type item
                bool is_tei = false;
                if ((UI_ApplicationCache_Gateway.Configuration.Extensions != null) &&
                    (UI_ApplicationCache_Gateway.Configuration.Extensions.Get_Extension("TEI") != null) &&
                    (UI_ApplicationCache_Gateway.Configuration.Extensions.Get_Extension("TEI").Enabled))
                {
                    string tei_file = BriefItem.Behaviors.Get_Setting("TEI.Source_File");
                    string xslt_file = BriefItem.Behaviors.Get_Setting("TEI.XSLT");
                    if ((tei_file != null) && (xslt_file != null))
                    {
                        is_tei = true;
                    }
                }


                // Start the citation table
                Output.WriteLine("  <td align=\"left\"><div class=\"sbkMmiv_ViewerTitle\">Manage this Item</div></td>");
                Output.WriteLine("</tr>");
                Output.WriteLine("<tr>");
                Output.WriteLine("  <td class=\"sbkMmiv_MainArea\">");


                Output.WriteLine("\t\t\t<table id=\"sbkMmiv_MainTable\">");
                Output.WriteLine("\t\t\t\t<tr class=\"sbkMmiv_HeaderRow\"><td colspan=\"3\">How would you like to manage this item today?</td></tr>");

                string url;
                if (!is_tei)
                {
                    // Add ability to edit metadata for this item
                    CurrentRequest.Mode = Display_Mode_Enum.My_Sobek;
                    CurrentRequest.My_Sobek_Type = My_Sobek_Type_Enum.Edit_Item_Metadata;
                    CurrentRequest.My_Sobek_SubMode = "1";
                    url = UrlWriterHelper.Redirect_URL(CurrentRequest);
                    Output.WriteLine("\t\t\t\t<tr>");
                    Output.WriteLine("\t\t\t\t\t<td style=\"width:50px\">&nbsp;</td>");
                    Output.WriteLine("\t\t\t\t\t<td style=\"width:60px\"><a href=\"" + url + "\"><img src=\"" + Static_Resources_Gateway.Edit_Metadata_Icon_Png + "\" /></a></td>");
                    Output.WriteLine("\t\t\t\t\t<td>");
                    Output.WriteLine("\t\t\t\t\t\t<a href=\"" + url + "\">Edit Item Metadata</a>");
                    Output.WriteLine("\t\t\t\t\t\t<div class=\"sbkMmiv_Desc\">Edit the information about this item which appears in the citation/description.  This is basic information about the original item and this digital manifestation.</div>");
                    Output.WriteLine("\t\t\t\t\t</td>");
                    Output.WriteLine("\t\t\t\t</tr>");
                    Output.WriteLine("\t\t\t\t<tr class=\"sbkMmiv_SpacerRow\"><td colspan=\"3\"></td></tr>");
                }
                else
                {
                    // Add ability to edit metadata for this item
                    CurrentRequest.Mode = Display_Mode_Enum.My_Sobek;
                    CurrentRequest.My_Sobek_Type = My_Sobek_Type_Enum.Edit_TEI_Item;
                    CurrentRequest.My_Sobek_SubMode = "1";
                    url = UrlWriterHelper.Redirect_URL(CurrentRequest);
                    Output.WriteLine("\t\t\t\t<tr>");
                    Output.WriteLine("\t\t\t\t\t<td style=\"width:50px\">&nbsp;</td>");
                    Output.WriteLine("\t\t\t\t\t<td style=\"width:60px\"><a href=\"" + url + "\"><img src=\"http://cdn.sobekrepository.org/images/misc/add_tei.png\" /></a></td>");
                    Output.WriteLine("\t\t\t\t\t<td>");
                    Output.WriteLine("\t\t\t\t\t\t<a href=\"" + url + "\">Edit TEI</a>");
                    Output.WriteLine("\t\t\t\t\t\t<div class=\"sbkMmiv_Desc\">Edit this TEI object, including the XSLT and CSS file used as well as uploading a new TEI file or editing the existing TEI file online.</div>");
                    Output.WriteLine("\t\t\t\t\t</td>");
                    Output.WriteLine("\t\t\t\t</tr>");
                    Output.WriteLine("\t\t\t\t<tr class=\"sbkMmiv_SpacerRow\"><td colspan=\"3\"></td></tr>");
                }

                // Add ability to edit behaviors for this item
                if (show_behaviors_if_permissioned)
                {
                    CurrentRequest.Mode = Display_Mode_Enum.My_Sobek;
                    CurrentRequest.My_Sobek_Type = My_Sobek_Type_Enum.Edit_Item_Behaviors;
                    CurrentRequest.My_Sobek_SubMode = "1";
                    url = UrlWriterHelper.Redirect_URL(CurrentRequest);
                    Output.WriteLine("\t\t\t\t<tr>");
                    Output.WriteLine("\t\t\t\t\t<td style=\"width:50px\">&nbsp;</td>");
                    Output.WriteLine("\t\t\t\t\t<td style=\"width:60px\"><a href=\"" + url + "\"><img src=\"" + Static_Resources_Gateway.Edit_Behaviors_Icon_Png + "\" /></a></td>");
                    Output.WriteLine("\t\t\t\t\t<td>");
                    Output.WriteLine("\t\t\t\t\t\t<a href=\"" + url + "\">Edit Item Behaviors</a>");
                    Output.WriteLine("\t\t\t\t\t\t<div class=\"sbkMmiv_Desc\">Change the way this item behaves in this library, including which aggregations it appears under, the wordmarks to the left, and which viewer types are publicly accessible.</div>");
                    Output.WriteLine("\t\t\t\t\t</td>");
                    Output.WriteLine("\t\t\t\t</tr>");
                    Output.WriteLine("\t\t\t\t<tr class=\"sbkMmiv_SpacerRow\"><td colspan=\"3\"></td></tr>");
                }

                // Add ability to perform QC ( manage pages and divisions) for this item
                if (show_qc_if_permissioned)
                {
                    CurrentRequest.Mode = Display_Mode_Enum.Item_Display;
                    CurrentRequest.ViewerCode = "qc";
                    url = UrlWriterHelper.Redirect_URL(CurrentRequest);
                    Output.WriteLine("\t\t\t\t<tr>");
                    Output.WriteLine("\t\t\t\t\t<td style=\"width:50px\">&nbsp;</td>");
                    Output.WriteLine("\t\t\t\t\t<td style=\"width:60px\"><a href=\"" + url + "\"><img src=\"" + Static_Resources_Gateway.Qc_Button_Img_Large + "\" /></a></td>");
                    Output.WriteLine("\t\t\t\t\t<td>");
                    Output.WriteLine("\t\t\t\t\t\t<a href=\"" + url + "\">Manage Pages and Divisions (Quality Control)</a>");
                    Output.WriteLine("\t\t\t\t\t\t<div class=\"sbkMmiv_Desc\">Reorder page images, name pages, assign divisions, and delete and add new page images to this item.</div>");
                    Output.WriteLine("\t\t\t\t\t</td>");
                    Output.WriteLine("\t\t\t\t</tr>");
                    Output.WriteLine("\t\t\t\t<tr class=\"sbkMmiv_SpacerRow\"><td colspan=\"3\"></td></tr>");
                }


                // Add ability to view work history for this item
                CurrentRequest.Mode = Display_Mode_Enum.Item_Display;
                CurrentRequest.ViewerCode = "tracking";
                url = UrlWriterHelper.Redirect_URL(CurrentRequest);
                Output.WriteLine("\t\t\t\t<tr>");
                Output.WriteLine("\t\t\t\t\t<td style=\"width:50px\">&nbsp;</td>");
                Output.WriteLine("\t\t\t\t\t<td style=\"width:60px\"><a href=\"" + url + "\"><img src=\"" + Static_Resources_Gateway.View_Work_Log_Img_Large + "\" /></a></td>");
                Output.WriteLine("\t\t\t\t\t<td>");
                Output.WriteLine("\t\t\t\t\t\t<a href=\"" + url + "\">View Work History</a>");
                Output.WriteLine("\t\t\t\t\t\t<div class=\"sbkMmiv_Desc\">View the history of all work performed on this item.  From this view, you can also see any digitization milestones and digital resource file information.</div>");
                Output.WriteLine("\t\t\t\t\t</td>");
                Output.WriteLine("\t\t\t\t</tr>");
                Output.WriteLine("\t\t\t\t<tr class=\"sbkMmiv_SpacerRow\"><td colspan=\"3\"></td></tr>");


                // Add ability to upload new download files for this item
                CurrentRequest.Mode = Display_Mode_Enum.My_Sobek;
                CurrentRequest.My_Sobek_Type = My_Sobek_Type_Enum.File_Management;
                url = UrlWriterHelper.Redirect_URL(CurrentRequest);
                Output.WriteLine("\t\t\t\t<tr>");
                Output.WriteLine("\t\t\t\t\t<td style=\"width:50px\">&nbsp;</td>");
                Output.WriteLine("\t\t\t\t\t<td style=\"width:60px\"><a href=\"" + url + "\"><img src=\"" + Static_Resources_Gateway.File_Management_Icon_Png + "\" /></a></td>");
                Output.WriteLine("\t\t\t\t\t<td>");
                Output.WriteLine("\t\t\t\t\t\t<a href=\"" + url + "\">Manage Download Files</a>");
                Output.WriteLine("\t\t\t\t\t\t<div class=\"sbkMmiv_Desc\">Upload new files for download or remove existing files that are attached to this item for download.  This generally includes everything except for the page images.</div>");
                Output.WriteLine("\t\t\t\t\t</td>");
                Output.WriteLine("\t\t\t\t</tr>");
                Output.WriteLine("\t\t\t\t<tr class=\"sbkMmiv_SpacerRow\"><td colspan=\"3\"></td></tr>");

                // Add ability to edit geo-spatial information for this item
                if (UI_ApplicationCache_Gateway.Settings.Resources.Manage_GeoSpatial_Data)
                {
                    CurrentRequest.Mode = Display_Mode_Enum.Item_Display;
                    CurrentRequest.ViewerCode = "mapedit";
                    url = UrlWriterHelper.Redirect_URL(CurrentRequest);
                    Output.WriteLine("\t\t\t\t<tr>");
                    Output.WriteLine("\t\t\t\t\t<td style=\"width:50px\">&nbsp;</td>");
                    Output.WriteLine("\t\t\t\t\t<td style=\"width:60px\"><a href=\"" + url + "\"><img src=\"" + Static_Resources_Gateway.Add_Geospatial_Img + "\" /></a></td>");
                    Output.WriteLine("\t\t\t\t\t<td>");
                    Output.WriteLine("\t\t\t\t\t\t<a href=\"" + url + "\">Manage Geo-Spatial Data (beta)</a>");
                    Output.WriteLine("\t\t\t\t\t\t<div class=\"sbkMmiv_Desc\">Add geo-spatial information for this item.  This can be as simple as a location for a photograph or can be an overlay for a map.  Points, lines, and polygons of interest can also be drawn.</div>");
                    Output.WriteLine("\t\t\t\t\t</td>");
                    Output.WriteLine("\t\t\t\t</tr>");
                    Output.WriteLine("\t\t\t\t<tr class=\"sbkMmiv_SpacerRow\"><td colspan=\"3\"></td></tr>");
                }

                // Add ability to view the tracking sheet for this item
                if (UI_ApplicationCache_Gateway.Settings.Resources.Use_Tracking_Sheet)
                {
                    CurrentRequest.Mode = Display_Mode_Enum.Item_Display;
                    CurrentRequest.ViewerCode = "ts";
                    url = UrlWriterHelper.Redirect_URL(CurrentRequest);
                    Output.WriteLine("\t\t\t\t<tr>");
                    Output.WriteLine("\t\t\t\t\t<td style=\"width:50px\">&nbsp;</td>");
                    Output.WriteLine("\t\t\t\t\t<td style=\"width:60px\"><a href=\"" + url + "\"><img src=\"" + Static_Resources_Gateway.Track2_Gif + "\" /></a></td>");
                    Output.WriteLine("\t\t\t\t\t<td>");
                    Output.WriteLine("\t\t\t\t\t\t<a href=\"" + url + "\">View Tracking Sheet</a>");
                    Output.WriteLine("\t\t\t\t\t\t<div class=\"sbkMmiv_Desc\">This can be used for printing the tracking sheet for this item, which can be used as part of the built-in digitization workflow.</div>");
                    Output.WriteLine("\t\t\t\t\t</td>");
                    Output.WriteLine("\t\t\t\t</tr>");
                    Output.WriteLine("\t\t\t\t<tr class=\"sbkMmiv_SpacerRow\"><td colspan=\"3\"></td></tr>");
                }

                Output.WriteLine("\t\t\t\t<tr class=\"sbkMmiv_HeaderRow\"><td colspan=\"3\">In addition, the following changes can be made at the item group level:</td></tr>");

                // Add ability to edit GROUP behaviors for this group
                CurrentRequest.Mode = Display_Mode_Enum.My_Sobek;
                CurrentRequest.My_Sobek_Type = My_Sobek_Type_Enum.Edit_Group_Behaviors;
                CurrentRequest.My_Sobek_SubMode = "1";
                url = UrlWriterHelper.Redirect_URL(CurrentRequest);
                Output.WriteLine("\t\t\t\t<tr>");
                Output.WriteLine("\t\t\t\t\t<td style=\"width:50px\">&nbsp;</td>");
                Output.WriteLine("\t\t\t\t\t<td style=\"width:60px\"><a href=\"" + url + "\"><img src=\"" + Static_Resources_Gateway.Edit_Behaviors_Icon_Png + "\" /></a></td>");
                Output.WriteLine("\t\t\t\t\t<td>");
                Output.WriteLine("\t\t\t\t\t\t<a href=\"" + url + "\">Edit Item Group Behaviors</a>");
                Output.WriteLine("\t\t\t\t\t\t<div class=\"sbkMmiv_Desc\">Set the title under which all of these items appear in search results and set the web skins under which all these items should appear.</div>");
                Output.WriteLine("\t\t\t\t\t</td>");
                Output.WriteLine("\t\t\t\t</tr>");
                Output.WriteLine("\t\t\t\t<tr class=\"sbkMmiv_SpacerRow\"><td colspan=\"3\"></td></tr>");

                // Add ability to add new volume for this group
                CurrentRequest.Mode = Display_Mode_Enum.My_Sobek;
                CurrentRequest.My_Sobek_Type = My_Sobek_Type_Enum.Group_Add_Volume;
                CurrentRequest.My_Sobek_SubMode = "1";
                url = UrlWriterHelper.Redirect_URL(CurrentRequest);
                Output.WriteLine("\t\t\t\t<tr>");
                Output.WriteLine("\t\t\t\t\t<td style=\"width:50px\">&nbsp;</td>");
                Output.WriteLine("\t\t\t\t\t<td style=\"width:60px\"><a href=\"" + url + "\"><img src=\"" + Static_Resources_Gateway.Add_Volume_Img + "\" /></a></td>");
                Output.WriteLine("\t\t\t\t\t<td>");
                Output.WriteLine("\t\t\t\t\t\t<a href=\"" + url + "\">Add New Volume</a>");
                Output.WriteLine("\t\t\t\t\t\t<div class=\"sbkMmiv_Desc\">Add a new, related volume to this item group.<br /><br /></div>");
                Output.WriteLine("\t\t\t\t\t</td>");
                Output.WriteLine("\t\t\t\t</tr>");
                Output.WriteLine("\t\t\t\t<tr class=\"sbkMmiv_SpacerRow\"><td colspan=\"3\"></td></tr>");

                if (( BriefItem.Web != null ) && (BriefItem.Web.Siblings.HasValue) && (BriefItem.Web.Siblings > 1) && ( UI_ApplicationCache_Gateway.Settings.Resources.Allow_Behavior_Mass_Update))
                {
                    // Add ability to mass update all items for this group
                    CurrentRequest.Mode = Display_Mode_Enum.My_Sobek;
                    CurrentRequest.My_Sobek_Type = My_Sobek_Type_Enum.Group_Mass_Update_Items;
                    CurrentRequest.My_Sobek_SubMode = "1";
                    url = UrlWriterHelper.Redirect_URL(CurrentRequest);
                    Output.WriteLine("\t\t\t\t<tr>");
                    Output.WriteLine("\t\t\t\t\t<td style=\"width:50px\">&nbsp;</td>");
                    Output.WriteLine("\t\t\t\t\t<td style=\"width:60px\"><a href=\"" + url + "\"><img src=\"" + Static_Resources_Gateway.Mass_Update_Icon_Png + "\" /></a></td>");
                    Output.WriteLine("\t\t\t\t\t<td>");
                    Output.WriteLine("\t\t\t\t\t\t<a href=\"" + url + "\">Mass Update Item Behaviors</a>");
                    Output.WriteLine("\t\t\t\t\t\t<div class=\"sbkMmiv_Desc\">This allows item-level behaviors to be set for all items within this item group, including which aggregations it appears under, the wordmarks to the left, and which viewer types are publicly accessible.</div>");
                    Output.WriteLine("\t\t\t\t\t</td>");
                    Output.WriteLine("\t\t\t\t</tr>");
                    Output.WriteLine("\t\t\t\t<tr class=\"sbkMmiv_SpacerRow\"><td colspan=\"3\"></td></tr>");
                }

                Output.WriteLine("\t\t\t</table>");

                Output.WriteLine("\t\t</td>");
            }
            else
            {
                // Start the citation table
                Output.WriteLine("  <td align=\"left\"><div class=\"sbkMmiv_ViewerTitle\">Manage this Item Group</div></td>");
                Output.WriteLine("</tr>");
                Output.WriteLine("<tr>");
                Output.WriteLine("  <td class=\"sbkMmiv_MainArea\">");


                Output.WriteLine("\t\t\t<table id=\"sbkMmiv_MainTable\">");
                Output.WriteLine("\t\t\t\t<tr class=\"sbkMmiv_HeaderRow\"><td colspan=\"3\">How would you like to manage this item group today?</td></tr>");


                // Add ability to edit GROUP behaviors for this group
                CurrentRequest.Mode = Display_Mode_Enum.My_Sobek;
                CurrentRequest.My_Sobek_Type = My_Sobek_Type_Enum.Edit_Group_Behaviors;
                CurrentRequest.My_Sobek_SubMode = "1";
                string url = UrlWriterHelper.Redirect_URL(CurrentRequest);
                Output.WriteLine("\t\t\t\t<tr>");
                Output.WriteLine("\t\t\t\t\t<td style=\"width:50px\">&nbsp;</td>");
                Output.WriteLine("\t\t\t\t\t<td style=\"width:60px\"><a href=\"" + url + "\"><img src=\"" + Static_Resources_Gateway.Edit_Behaviors_Icon_Png + "\" /></a></td>");
                Output.WriteLine("\t\t\t\t\t<td>");
                Output.WriteLine("\t\t\t\t\t\t<a href=\"" + url + "\">Edit Item Group Behaviors</a>");
                Output.WriteLine("\t\t\t\t\t\t<div class=\"sbkMmiv_Desc\">Set the title under which all of these items appear in search results and set the web skins under which all these items should appear.</div>");
                Output.WriteLine("\t\t\t\t\t</td>");
                Output.WriteLine("\t\t\t\t</tr>");
                Output.WriteLine("\t\t\t\t<tr class=\"sbkMmiv_SpacerRow\"><td colspan=\"3\"></td></tr>");

                // Add ability to add new volume for this group
                CurrentRequest.Mode = Display_Mode_Enum.My_Sobek;
                CurrentRequest.My_Sobek_Type = My_Sobek_Type_Enum.Group_Add_Volume;
                CurrentRequest.My_Sobek_SubMode = "1";
                url = UrlWriterHelper.Redirect_URL(CurrentRequest);
                Output.WriteLine("\t\t\t\t<tr>");
                Output.WriteLine("\t\t\t\t\t<td style=\"width:50px\">&nbsp;</td>");
                Output.WriteLine("\t\t\t\t\t<td style=\"width:60px\"><a href=\"" + url + "\"><img src=\"" + Static_Resources_Gateway.Add_Volume_Img + "\" /></a></td>");
                Output.WriteLine("\t\t\t\t\t<td>");
                Output.WriteLine("\t\t\t\t\t\t<a href=\"" + url + "\">Add New Volume</a>");
                Output.WriteLine("\t\t\t\t\t\t<div class=\"sbkMmiv_Desc\">Add a new, related volume to this item group.<br /><br /></div>");
                Output.WriteLine("\t\t\t\t\t</td>");
                Output.WriteLine("\t\t\t\t</tr>");
                Output.WriteLine("\t\t\t\t<tr class=\"sbkMmiv_SpacerRow\"><td colspan=\"3\"></td></tr>");

                // Add ability to mass update all items for this group
                CurrentRequest.Mode = Display_Mode_Enum.My_Sobek;
                CurrentRequest.My_Sobek_Type = My_Sobek_Type_Enum.Group_Mass_Update_Items;
                CurrentRequest.My_Sobek_SubMode = "1";
                url = UrlWriterHelper.Redirect_URL(CurrentRequest);
                Output.WriteLine("\t\t\t\t<tr>");
                Output.WriteLine("\t\t\t\t\t<td style=\"width:50px\">&nbsp;</td>");
                Output.WriteLine("\t\t\t\t\t<td style=\"width:60px\"><a href=\"" + url + "\"><img src=\"" + Static_Resources_Gateway.Mass_Update_Icon_Png + "\" /></a></td>");
                Output.WriteLine("\t\t\t\t\t<td>");
                Output.WriteLine("\t\t\t\t\t\t<a href=\"" + url + "\">Mass Update Item Behaviors</a>");
                Output.WriteLine("\t\t\t\t\t\t<div class=\"sbkMmiv_Desc\">This allows item-level behaviors to be set for all items within this item group, including which aggregations it appears under, the wordmarks to the left, and which viewer types are publicly accessible.</div>");
                Output.WriteLine("\t\t\t\t\t</td>");
                Output.WriteLine("\t\t\t\t</tr>");
                Output.WriteLine("\t\t\t\t<tr class=\"sbkMmiv_SpacerRow\"><td colspan=\"3\"></td></tr>");

                Output.WriteLine("\t\t\t</table>");

                Output.WriteLine("\t\t</td>");
            }

            // Reset the value
            CurrentRequest.Mode = Display_Mode_Enum.Item_Display;
            CurrentRequest.ViewerCode = currentViewerCode;
        }

        /// <summary> Allows controls to be added directory to a place holder, rather than just writing to the output HTML stream </summary>
        /// <param name="MainPlaceHolder"> Main place holder ( &quot;mainPlaceHolder&quot; ) in the itemNavForm form into which the bulk of the item viewer's output is displayed</param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <remarks> This method does nothing, since nothing is added to the place holder as a control for this item viewer </remarks>
        public override void Add_Main_Viewer_Section(PlaceHolder MainPlaceHolder, Custom_Tracer Tracer)
        {
            // Do nothing
        }
    }
}
