#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.Navigation;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Engine_Library.Database;
using SobekCM.Library.Database;
using SobekCM.Library.HTML.Helpers;
using SobekCM.Library.UI;
using SobekCM.Resource_Object.Bib_Info;
using SobekCM.Tools;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

#endregion

namespace SobekCM.Library.AdminViewer
{
    /// <summary> Administrative screen allows an existing Item Type to be edited, or a new Item Type
    /// to be added </summary>
    /// <remarks> This class extends the <see cref="abstract_AdminViewer"/> class. Two tabs: Details
    /// (the original first-pass fields) and Blocks, Widgets &amp; Access -- the latter only shown once
    /// a Type has been saved and has a real TypeID (its child rows all carry a TypeID foreign key, so
    /// there is nothing to manage until the Type itself exists). The current tab is encoded as a
    /// trailing "b" on <c>My_Sobek_SubMode</c> (e.g. "5" = Details, "5b" = Blocks/Widgets/Access), the
    /// same single-letter-suffix idea <c>Users_AdminViewer</c>/<c>User_Group_AdminViewer</c> use, kept
    /// to just one letter since there are only two tabs and no separate List/View/Edit SubViewer layer
    /// here. Unlike those two, every row in the Blocks/Widgets/Access tab (add/remove/reorder a block,
    /// widget, default metadata value, access grant, or collection shortcut) is its own immediate
    /// postback that writes straight to the database and redisplays the same tab -- there is no staged
    /// in-memory batch-save for this tab the way the Details tab's SAVE button works, because these are
    /// independent child rows (their own primary keys) rather than simple flags on the Type itself.
    /// Reordering is a plain up/down link + hidden-field postback (matching
    /// <see cref="Thematic_Headings_AdminViewer"/>'s convention), not drag-and-drop JS. </remarks>
    public class Item_Type_Single_AdminViewer : abstract_AdminViewer
    {
        private string actionMessage;
        private string blocksActionMessage;

        private readonly int typeId;
        private bool isSystemType;
        private string typeName;
        private string description;
        private bool showSeriesFinder;
        private bool includeUserAsAuthor;
        private bool defaultCreateOcrFromMasters;
        private string bibIdRoot;
        private string marcTypeOfResource;
        private string helpUrl;
        private bool enabled;
        private string iconCode;

        private readonly bool blocksTabActive;
        private DataTable typeBlocks;
        private DataTable typeWidgets;
        private DataTable typeDefaultMetadata;
        private DataTable typeAssignments;
        private DataTable typeAggregationLinks;
        private List<DataRow> availableBlocksToAdd;
        private List<SobekCM.Core.Users.User_Group> allUserGroups;

        /// <summary> Constructor for a new instance of the Item_Type_Single_AdminViewer class </summary>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        public Item_Type_Single_AdminViewer(RequestCache RequestSpecificValues, HttpContext Context) : base(RequestSpecificValues, Context)
        {
            RequestSpecificValues.Tracer.Add_Trace("Item_Type_Single_AdminViewer.Constructor", String.Empty);

            actionMessage = String.Empty;
            blocksActionMessage = String.Empty;

            // If the user cannot edit this, go back
            if ((!RequestSpecificValues.Current_User.Is_System_Admin) && (!RequestSpecificValues.Current_User.Is_Portal_Admin))
            {
                RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.My_Sobek;
                RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Home;
                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode, Context);
                return;
            }

            // Is there a Type specified?  ( submode is either "new" or an integer TypeID, optionally
            // followed by a trailing "b" for the Blocks/Widgets/Access tab )
            typeId = -1;
            string submode = RequestSpecificValues.Current_Mode.My_Sobek_SubMode;
            blocksTabActive = false;
            if ((!String.IsNullOrEmpty(submode)) && (String.Compare(submode, "new", StringComparison.OrdinalIgnoreCase) != 0))
            {
                if ((submode.EndsWith("b", StringComparison.OrdinalIgnoreCase)) && (submode.Length > 1))
                {
                    blocksTabActive = true;
                    submode = submode.Substring(0, submode.Length - 1);
                }

                if (!Int32.TryParse(submode, out typeId))
                    typeId = -1;
            }

            // Handle any post backs
            if ((RequestSpecificValues.Current_Mode.isPostBack) && (Context.Request.HasFormContentType))
            {
                var form = Context.Request.Form;
                string details_action = form["admin_itemtype_action"];
                string blocks_action = form["admin_itemtype_blocks_action"];

                if (!String.IsNullOrEmpty(details_action))
                {
                    try
                    {
                        typeName = form["admin_itemtype_name"];
                        description = form["admin_itemtype_description"];
                        showSeriesFinder = !String.IsNullOrEmpty(form["admin_itemtype_seriesfinder"].TrimFirst());
                        includeUserAsAuthor = !String.IsNullOrEmpty(form["admin_itemtype_includeauthor"].TrimFirst());
                        defaultCreateOcrFromMasters = !String.IsNullOrEmpty(form["admin_itemtype_ocr"].TrimFirst());
                        bibIdRoot = form["admin_itemtype_bibidroot"];
                        marcTypeOfResource = form["admin_itemtype_marctype"];
                        helpUrl = form["admin_itemtype_helpurl"];
                        enabled = !String.IsNullOrEmpty(form["admin_itemtype_enabled"].TrimFirst());
                        iconCode = form["admin_itemtype_iconcode"];

                        if (details_action == "cancel")
                        {
                            string returnUrl1 = build_mgmt_url(RequestSpecificValues, Context);
                            RequestSpecificValues.Current_Mode.Request_Completed = true;
                            Context.Response.Redirect(returnUrl1);
                            return;
                        }

                        if (details_action == "delete")
                        {
                            if (isSystemType)
                            {
                                actionMessage = "ERROR: Standard Types can only be disabled, not deleted.";
                            }
                            else
                            {
                                bool deleteResult = SobekCM_Database.Delete_Item_Type(typeId, RequestSpecificValues.Tracer);
                                if (!deleteResult)
                                {
                                    actionMessage = "ERROR: Unable to delete this Type -- it may still be in use by existing items.";
                                }
                                else
                                {
                                    string returnUrl2 = build_mgmt_url(RequestSpecificValues, Context);
                                    RequestSpecificValues.Current_Mode.Request_Completed = true;
                                    Context.Response.Redirect(returnUrl2);
                                    return;
                                }
                            }
                        }

                        if (details_action == "save")
                        {
                            var errors = new List<string>();
                            if (String.IsNullOrWhiteSpace(typeName)) errors.Add("NAME is required and missing");

                            if (errors.Count > 0)
                            {
                                actionMessage = "ERROR: Some required fields are missing:<br /><br />";
                                foreach (string thisError in errors)
                                {
                                    actionMessage = actionMessage + "&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; " + thisError + "<br />";
                                }
                            }
                            else
                            {
                                bool result = SobekCM_Database.Edit_Item_Type(typeId, typeName, description, showSeriesFinder, includeUserAsAuthor, defaultCreateOcrFromMasters, bibIdRoot, marcTypeOfResource, helpUrl, enabled, iconCode, RequestSpecificValues.Tracer);
                                if (!result)
                                {
                                    actionMessage = "Unknown error encountered while saving this Item Type";
                                }
                                else
                                {
                                    string returnUrl3 = build_mgmt_url(RequestSpecificValues, Context);
                                    RequestSpecificValues.Current_Mode.Request_Completed = true;
                                    Context.Response.Redirect(returnUrl3);
                                    return;
                                }
                            }
                        }
                    }
                    catch
                    {
                        actionMessage = "Unable to correctly parse postback data.";
                    }
                }
                else if ((!String.IsNullOrEmpty(blocks_action)) && (typeId > 0))
                {
                    handle_blocks_tab_postback(blocks_action, form, RequestSpecificValues);
                    load_type_details(RequestSpecificValues);
                }
                else
                {
                    load_type_details(RequestSpecificValues);
                }
            }
            else // NOT A POST BACK
            {
                load_type_details(RequestSpecificValues);
            }

            if (typeId > 0)
            {
                load_blocks_tab_data(RequestSpecificValues);
            }
        }

        /// <summary> Loads the Details tab's fields from the database, or sets them to blank defaults
        /// for a brand new Type </summary>
        private void load_type_details(RequestCache RequestSpecificValues)
        {
            typeName = String.Empty;
            description = String.Empty;
            showSeriesFinder = false;
            includeUserAsAuthor = false;
            defaultCreateOcrFromMasters = false;
            bibIdRoot = String.Empty;
            marcTypeOfResource = String.Empty;
            helpUrl = String.Empty;
            enabled = true;
            iconCode = String.Empty;
            isSystemType = false;

            if (typeId > 0)
            {
                DataRow row = SobekCM_Database.Get_Item_Type(typeId, RequestSpecificValues.Tracer);
                if (row != null)
                {
                    typeName = row["Name"].ToString();
                    description = row["Description"] == DBNull.Value ? String.Empty : row["Description"].ToString();
                    isSystemType = Convert.ToBoolean(row["IsSystemType"]);
                    showSeriesFinder = Convert.ToBoolean(row["ShowSeriesFinder"]);
                    includeUserAsAuthor = Convert.ToBoolean(row["IncludeUserAsAuthor"]);
                    defaultCreateOcrFromMasters = Convert.ToBoolean(row["DefaultCreateOcrFromMasters"]);
                    bibIdRoot = row["BibIDRoot"] == DBNull.Value ? String.Empty : row["BibIDRoot"].ToString();
                    marcTypeOfResource = row["MARC_TypeOfResource"] == DBNull.Value ? String.Empty : row["MARC_TypeOfResource"].ToString();
                    helpUrl = row["HelpUrl"] == DBNull.Value ? String.Empty : row["HelpUrl"].ToString();
                    enabled = Convert.ToBoolean(row["Enabled"]);
                    iconCode = row["IconCode"] == DBNull.Value ? String.Empty : row["IconCode"].ToString();
                }
            }
        }

        /// <summary> Loads every child list the Blocks/Widgets/Access tab displays </summary>
        private void load_blocks_tab_data(RequestCache RequestSpecificValues)
        {
            DataSet blocksSet = SobekCM_Database.Get_Item_Type_Blocks(typeId, RequestSpecificValues.Tracer);
            typeBlocks = ((blocksSet != null) && (blocksSet.Tables.Count > 0)) ? blocksSet.Tables[0] : null;

            DataSet widgetsSet = SobekCM_Database.Get_Item_Type_Widgets(typeId, RequestSpecificValues.Tracer);
            typeWidgets = ((widgetsSet != null) && (widgetsSet.Tables.Count > 0)) ? widgetsSet.Tables[0] : null;

            DataSet defaultMetadataSet = SobekCM_Database.Get_Item_Type_Default_Metadata(typeId, RequestSpecificValues.Tracer);
            typeDefaultMetadata = ((defaultMetadataSet != null) && (defaultMetadataSet.Tables.Count > 0)) ? defaultMetadataSet.Tables[0] : null;

            DataSet assignmentsSet = SobekCM_Database.Get_Item_Type_Assignments(typeId, RequestSpecificValues.Tracer);
            typeAssignments = ((assignmentsSet != null) && (assignmentsSet.Tables.Count > 0)) ? assignmentsSet.Tables[0] : null;

            DataSet aggLinksSet = SobekCM_Database.Get_Item_Type_Aggregation_Links(typeId, RequestSpecificValues.Tracer);
            typeAggregationLinks = ((aggLinksSet != null) && (aggLinksSet.Tables.Count > 0)) ? aggLinksSet.Tables[0] : null;

            // Build the list of registry blocks not yet added to this Type, for the "add block" dropdown
            var alreadyAddedBlockIds = new HashSet<int>();
            if (typeBlocks != null)
            {
                foreach (DataRow thisRow in typeBlocks.Rows)
                    alreadyAddedBlockIds.Add(Convert.ToInt32(thisRow["BlockID"]));
            }

            availableBlocksToAdd = new List<DataRow>();
            DataSet allBlocksSet = SobekCM_Database.Get_All_Metadata_Blocks(RequestSpecificValues.Tracer);
            if ((allBlocksSet != null) && (allBlocksSet.Tables.Count > 0))
            {
                foreach (DataRow thisRow in allBlocksSet.Tables[0].Rows)
                {
                    if ((Convert.ToBoolean(thisRow["Enabled"])) && (!alreadyAddedBlockIds.Contains(Convert.ToInt32(thisRow["BlockID"]))))
                        availableBlocksToAdd.Add(thisRow);
                }
            }

            allUserGroups = Engine_Database.Get_All_User_Groups(RequestSpecificValues.Tracer) ?? new List<SobekCM.Core.Users.User_Group>();
        }

        /// <summary> Handles the single immediate mutation requested by whichever row action fired on
        /// the Blocks/Widgets/Access tab </summary>
        private void handle_blocks_tab_postback(string action, Microsoft.AspNetCore.Http.IFormCollection form, RequestCache RequestSpecificValues)
        {
            string target = form["admin_itemtype_blocks_target"];

            switch (action)
            {
                case "add_block":
                    if (Int32.TryParse(form["admin_itemtype_addblock_id"], out int addBlockId))
                    {
                        bool locked = !String.IsNullOrEmpty(form["admin_itemtype_addblock_locked"].TrimFirst());
                        SobekCM_Database.Add_Item_Type_Block(typeId, addBlockId, !locked, RequestSpecificValues.Tracer);
                    }
                    break;

                case "remove_block":
                    if (Int32.TryParse(target, out int removeBlockId))
                        SobekCM_Database.Remove_Item_Type_Block(typeId, removeBlockId, RequestSpecificValues.Tracer);
                    break;

                case "moveup_block":
                case "movedown_block":
                    if (Int32.TryParse(target, out int moveBlockId))
                        SobekCM_Database.Move_Item_Type_Block(typeId, moveBlockId, action == "moveup_block" ? "up" : "down", RequestSpecificValues.Tracer);
                    break;

                case "toggle_locked_block":
                    {
                        string[] parts = (target ?? String.Empty).Split(',');
                        if ((parts.Length == 2) && (Int32.TryParse(parts[0], out int toggleBlockId)) && (Int32.TryParse(parts[1], out int newRemovable)))
                            SobekCM_Database.Set_Item_Type_Block_Removable(typeId, toggleBlockId, newRemovable != 0, RequestSpecificValues.Tracer);
                    }
                    break;

                case "add_widget":
                    {
                        string widgetCode = form["admin_itemtype_addwidget_code"].TrimFirst();
                        string placement = form["admin_itemtype_addwidget_placement"];
                        if (!String.IsNullOrWhiteSpace(widgetCode))
                            SobekCM_Database.Add_Item_Type_Widget(typeId, widgetCode.ToUpper(), placement, RequestSpecificValues.Tracer);
                    }
                    break;

                case "remove_widget":
                    if (!String.IsNullOrEmpty(target))
                        SobekCM_Database.Remove_Item_Type_Widget(typeId, target, RequestSpecificValues.Tracer);
                    break;

                case "add_default_metadata":
                    {
                        string elementCode = form["admin_itemtype_adddefault_code"].TrimFirst();
                        string value = form["admin_itemtype_adddefault_value"];
                        if ((!String.IsNullOrWhiteSpace(elementCode)) && (!String.IsNullOrWhiteSpace(value)))
                            SobekCM_Database.Add_Item_Type_Default_Metadata(typeId, elementCode, value, RequestSpecificValues.Tracer);
                    }
                    break;

                case "remove_default_metadata":
                    if (Int32.TryParse(target, out int removeDefaultId))
                        SobekCM_Database.Remove_Item_Type_Default_Metadata(removeDefaultId, RequestSpecificValues.Tracer);
                    break;

                case "add_access_user":
                    {
                        string username = form["admin_itemtype_addaccess_username"].TrimFirst();
                        if (!String.IsNullOrWhiteSpace(username))
                        {
                            (int userId, string _) = Engine_Database.Get_User_Password_Hash(username, RequestSpecificValues.Tracer);
                            if (userId <= 0)
                                blocksActionMessage = "ERROR: No user found with username '" + System.Net.WebUtility.HtmlEncode(username) + "'";
                            else
                                SobekCM_Database.Add_Item_Type_Assignment_For_User(typeId, userId, RequestSpecificValues.Tracer);
                        }
                    }
                    break;

                case "add_access_group":
                    if (Int32.TryParse(form["admin_itemtype_addaccess_groupid"], out int addGroupId))
                        SobekCM_Database.Add_Item_Type_Assignment_For_Group(typeId, addGroupId, RequestSpecificValues.Tracer);
                    break;

                case "remove_access":
                    if (Int32.TryParse(target, out int removeAssignmentId))
                        SobekCM_Database.Remove_Item_Type_Assignment(removeAssignmentId, RequestSpecificValues.Tracer);
                    break;

                case "add_agglink":
                    {
                        string aggCode = form["admin_itemtype_addagglink_code"].TrimFirst();
                        var match = UI_ApplicationCache_Gateway.Aggregations.All_Aggregations.FirstOrDefault(a => String.Equals(a.Code, aggCode, StringComparison.OrdinalIgnoreCase));
                        if (match == null)
                            blocksActionMessage = "ERROR: No collection found with code '" + System.Net.WebUtility.HtmlEncode(aggCode) + "'";
                        else
                            SobekCM_Database.Add_Item_Type_Aggregation_Link(typeId, match.ID, RequestSpecificValues.Tracer);
                    }
                    break;

                case "remove_agglink":
                    if (Int32.TryParse(target, out int removeAggId))
                        SobekCM_Database.Remove_Item_Type_Aggregation_Link(typeId, removeAggId, RequestSpecificValues.Tracer);
                    break;
            }
        }

        /// <summary> Builds the URL back to the Item Types management screen </summary>
        private static string build_mgmt_url(RequestCache RequestSpecificValues, HttpContext Context)
        {
            string last_admin_type = RequestSpecificValues.Current_Mode.Admin_Type;
            string last_submode = RequestSpecificValues.Current_Mode.My_Sobek_SubMode;

            RequestSpecificValues.Current_Mode.Admin_Type = Admin_View_Codes.Item_Types_Mgmt;
            RequestSpecificValues.Current_Mode.My_Sobek_SubMode = String.Empty;
            string url = UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode);

            RequestSpecificValues.Current_Mode.Admin_Type = last_admin_type;
            RequestSpecificValues.Current_Mode.My_Sobek_SubMode = last_submode;

            return url;
        }

        /// <summary> Builds the URL to this same Type, on a specific tab </summary>
        private string build_tab_url(RequestCache RequestSpecificValues, string tabSuffix)
        {
            string last_submode = RequestSpecificValues.Current_Mode.My_Sobek_SubMode;
            RequestSpecificValues.Current_Mode.My_Sobek_SubMode = typeId + tabSuffix;
            string url = UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode);
            RequestSpecificValues.Current_Mode.My_Sobek_SubMode = last_submode;
            return url;
        }

        /// <summary> Title for the page that displays this viewer, this is shown in the search box at the top of the page, just below the banner </summary>
        public override string Web_Title
        {
            get { return typeId > 0 ? "Edit Item Type" : "Add New Item Type"; }
        }

        /// <summary> Gets the URL for the icon related to this administrative task </summary>
        public override string Viewer_Icon
        {
            get { return Static_Resources_Gateway.Users_Img; }
        }

        /// <summary> Add the HTML to be displayed in the main SobekCM viewer area </summary>
        /// <param name="Output"> Textwriter to write the HTML for this viewer</param>
        /// <param name="Tracer">Trace object keeps a list of each method executed and important milestones in rendering</param>
        public override void Write_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace("Item_Type_Single_AdminViewer.Write_HTML");

            // Open the item nav form
            Write_ItemNavForm_Opening(Output);

            // Add the banner
            Banner_HtmlHelper.Add_Banner(Output, "sbkAhs_BannerDiv", "System Administration", RequestSpecificValues.Current_Mode, RequestSpecificValues.HTML_Skin, RequestSpecificValues.Top_Collection);

            Output.WriteLine("<script src=\"" + Static_Resources_Gateway.Sobekcm_Admin_Js + "\" type=\"text/javascript\"></script>");

            Output.WriteLine("<!-- Hidden fields are used for postbacks to indicate what to save and reset -->");
            Output.WriteLine("<input type=\"hidden\" id=\"admin_itemtype_action\" name=\"admin_itemtype_action\" value=\"\" />");
            Output.WriteLine("<input type=\"hidden\" id=\"admin_itemtype_blocks_action\" name=\"admin_itemtype_blocks_action\" value=\"\" />");
            Output.WriteLine("<input type=\"hidden\" id=\"admin_itemtype_blocks_target\" name=\"admin_itemtype_blocks_target\" value=\"\" />");
            Output.WriteLine();

            Output.WriteLine("<div class=\"sbkAdm_HomeText\">");

            // Tabs -- only offered once the Type actually exists
            if (typeId > 0)
            {
                string details_url = build_tab_url(RequestSpecificValues, String.Empty);
                string blocks_url = build_tab_url(RequestSpecificValues, "b");

                Output.WriteLine("  <div class=\"sbkAdm_HomeText\">");
                Output.WriteLine("    <a href=\"" + details_url + "\"" + (blocksTabActive ? "" : " style=\"font-weight:bold;\"") + ">Details</a> &nbsp; | &nbsp; ");
                Output.WriteLine("    <a href=\"" + blocks_url + "\"" + (blocksTabActive ? " style=\"font-weight:bold;\"" : "") + ">Blocks, Widgets &amp; Access</a>");
                Output.WriteLine("  </div>");
                Output.WriteLine("  <br />");
            }

            if (blocksTabActive)
                write_blocks_tab_html(Output);
            else
                write_details_tab_html(Output);

            Output.WriteLine("</div>");

            // Close the item nav form
            Write_ItemNavForm_Closing(Output);
        }

        /// <summary> Renders the original Details tab -- unchanged from the first pass </summary>
        private void write_details_tab_html(TextWriter Output)
        {
            if (!String.IsNullOrEmpty(actionMessage))
            {
                Output.WriteLine("  <br />");
                if (actionMessage.IndexOf("ERROR", StringComparison.InvariantCultureIgnoreCase) >= 0)
                    Output.WriteLine("  <div id=\"sbkAdm_ActionMessageError\">" + actionMessage + "</div>");
                else
                    Output.WriteLine("  <div id=\"sbkAdm_ActionMessageSuccess\">" + actionMessage + "</div>");
            }

            Output.WriteLine("  <p>Types are what a submitter picks first on the new-item screen -- each Type bundles the metadata blocks, upload behavior, and defaults for that kind of material.</p>");

            Output.WriteLine("  <table class=\"sbkAdm_PopupTable\">");

            Output.WriteLine("    <tr class=\"sbkSaav_TitleRow\"><td colspan=\"3\">Basic Info</td></tr>");
            Output.WriteLine("    <tr><td style=\"width: 145px\" class=\"sbkSaav_TableLabel\"><label for=\"admin_itemtype_name\">Name:</label></td>");
            Output.WriteLine("        <td colspan=\"2\"><input class=\"sbkSaav_medium_input sbkAdmin_Focusable\" name=\"admin_itemtype_name\" id=\"admin_itemtype_name\" type=\"text\" value=\"" + System.Net.WebUtility.HtmlEncode(typeName ?? String.Empty) + "\" /></td></tr>");

            Output.WriteLine("    <tr><td class=\"sbkSaav_TableLabel\"><label for=\"admin_itemtype_description\">Description:</label></td>");
            Output.WriteLine("        <td colspan=\"2\"><input class=\"sbkSaav_large_input sbkAdmin_Focusable\" name=\"admin_itemtype_description\" id=\"admin_itemtype_description\" type=\"text\" value=\"" + System.Net.WebUtility.HtmlEncode(description ?? String.Empty) + "\" /></td></tr>");

            if (typeId > 0)
            {
                Output.WriteLine("    <tr><td class=\"sbkSaav_TableLabel\">Kind:</td>");
                Output.WriteLine("        <td colspan=\"2\">" + (isSystemType ? "Standard Type (can be disabled, not deleted)" : "Custom Type (can be deleted)") + "</td></tr>");
            }

            Output.WriteLine("    <tr><td class=\"sbkSaav_TableLabel\">Active:</td>");
            Output.Write("        <td colspan=\"2\"><input class=\"sbkSav_checkbox\" type=\"checkbox\" name=\"admin_itemtype_enabled\" id=\"admin_itemtype_enabled\" ");
            if (enabled)
                Output.Write("checked=\"checked\" ");
            Output.WriteLine("/> <label for=\"admin_itemtype_enabled\">Shown on the Type grid when submitters start a new item</label></td></tr>");

            Output.WriteLine("    <tr class=\"sbkSaav_TitleRow\"><td colspan=\"3\">Behavior</td></tr>");

            Output.WriteLine("    <tr><td class=\"sbkSaav_TableLabel\">Series Finder:</td>");
            Output.Write("        <td colspan=\"2\"><input class=\"sbkSav_checkbox\" type=\"checkbox\" name=\"admin_itemtype_seriesfinder\" id=\"admin_itemtype_seriesfinder\" ");
            if (showSeriesFinder)
                Output.Write("checked=\"checked\" ");
            Output.WriteLine("/> <label for=\"admin_itemtype_seriesfinder\">Show \"attach to an existing title\" before metadata</label></td></tr>");

            Output.WriteLine("    <tr><td class=\"sbkSaav_TableLabel\">Author:</td>");
            Output.Write("        <td colspan=\"2\"><input class=\"sbkSav_checkbox\" type=\"checkbox\" name=\"admin_itemtype_includeauthor\" id=\"admin_itemtype_includeauthor\" ");
            if (includeUserAsAuthor)
                Output.Write("checked=\"checked\" ");
            Output.WriteLine("/> <label for=\"admin_itemtype_includeauthor\">Include the submitter as author</label></td></tr>");

            Output.WriteLine("    <tr><td class=\"sbkSaav_TableLabel\">Processing:</td>");
            Output.Write("        <td colspan=\"2\"><input class=\"sbkSav_checkbox\" type=\"checkbox\" name=\"admin_itemtype_ocr\" id=\"admin_itemtype_ocr\" ");
            if (defaultCreateOcrFromMasters)
                Output.Write("checked=\"checked\" ");
            Output.WriteLine("/> <label for=\"admin_itemtype_ocr\">Default to creating OCR text from master images</label></td></tr>");

            Output.WriteLine("    <tr class=\"sbkSaav_TitleRow\"><td colspan=\"3\">Cataloging</td></tr>");

            Output.WriteLine("    <tr><td class=\"sbkSaav_TableLabel\"><label for=\"admin_itemtype_bibidroot\">BibID Root:</label></td>");
            Output.WriteLine("        <td colspan=\"2\"><input class=\"sbkSaav_small_input sbkAdmin_Focusable\" name=\"admin_itemtype_bibidroot\" id=\"admin_itemtype_bibidroot\" type=\"text\" value=\"" + System.Net.WebUtility.HtmlEncode(bibIdRoot ?? String.Empty) + "\" /></td></tr>");

            Output.WriteLine("    <tr><td class=\"sbkSaav_TableLabel\"><label for=\"admin_itemtype_marctype\">MARC / MODS Resource Type:</label></td>");
            Output.WriteLine("        <td colspan=\"2\"><select class=\"sbkSaav_medium_input sbkAdmin_Focusable\" name=\"admin_itemtype_marctype\" id=\"admin_itemtype_marctype\">");
            Output.WriteLine("          <option value=\"\">(not set)</option>");
            foreach (string thisType in Enum.GetNames(typeof(TypeOfResource_SobekCM_Enum)))
            {
                if (thisType == "UNKNOWN") continue;
                bool selected = String.Compare(thisType, marcTypeOfResource, StringComparison.OrdinalIgnoreCase) == 0;
                Output.WriteLine("          <option value=\"" + thisType + "\"" + (selected ? " selected=\"selected\"" : "") + ">" + thisType.Replace("_", " ") + "</option>");
            }
            Output.WriteLine("        </select></td></tr>");

            Output.WriteLine("    <tr><td class=\"sbkSaav_TableLabel\"><label for=\"admin_itemtype_helpurl\">Help URL:</label></td>");
            Output.WriteLine("        <td colspan=\"2\"><input class=\"sbkSaav_large_input sbkAdmin_Focusable\" name=\"admin_itemtype_helpurl\" id=\"admin_itemtype_helpurl\" type=\"text\" value=\"" + System.Net.WebUtility.HtmlEncode(helpUrl ?? String.Empty) + "\" /></td></tr>");

            Output.WriteLine("    <tr><td class=\"sbkSaav_TableLabel\"><label for=\"admin_itemtype_iconcode\">Icon Code:</label></td>");
            Output.WriteLine("        <td colspan=\"2\"><input class=\"sbkSaav_small_input sbkAdmin_Focusable\" name=\"admin_itemtype_iconcode\" id=\"admin_itemtype_iconcode\" type=\"text\" value=\"" + System.Net.WebUtility.HtmlEncode(iconCode ?? String.Empty) + "\" /></td></tr>");

            string button_title = typeId > 0 ? "Save changes to this Item Type" : "Add this new Item Type";

            Output.WriteLine("    <tr><td></td><td colspan=\"2\">");
            Output.WriteLine("      <button title=\"Do not apply changes\" class=\"sbkAdm_RoundButton\" onclick=\"set_hidden_value_postback('admin_itemtype_action', 'cancel'); return false;\"><img src=\"" + Static_Resources_Gateway.Button_Previous_Arrow_Png + "\" class=\"sbkAdm_RoundButton_LeftImg\" alt=\"\" /> CANCEL</button> &nbsp; &nbsp; ");
            if ((typeId > 0) && (!isSystemType))
                Output.WriteLine("      <button title=\"Delete this custom Item Type\" class=\"sbkAdm_RoundButton\" onclick=\"set_hidden_value_postback('admin_itemtype_action', 'delete'); return false;\"> DELETE </button> &nbsp; &nbsp; ");
            Output.WriteLine("      <button title=\"" + button_title + "\" class=\"sbkAdm_RoundButton\" onclick=\"set_hidden_value_postback('admin_itemtype_action', 'save'); return false;\">SAVE <img src=\"" + Static_Resources_Gateway.Button_Next_Arrow_Png + "\" class=\"sbkAdm_RoundButton_RightImg\" alt=\"\" /></button>");
            Output.WriteLine("    </td></tr>");

            Output.WriteLine("  </table>");
        }

        /// <summary> Builds the inline onclick JS for a row action: sets the target payload and action,
        /// then submits -- fully self-contained, no shared JS function assumed beyond the standard
        /// itemNavForm submit already used throughout this admin area </summary>
        private static string row_action_onclick(string action, string target)
        {
            // target can be admin-typed free text (a widget code) as well as plain numeric IDs -- escape
            // for both the JS string literal it sits in AND the HTML attribute this whole string is
            // written into (Output.Write does not otherwise HTML-encode onclick contents)
            string jsSafeTarget = System.Net.WebUtility.HtmlEncode(target).Replace("'", "&#39;");
            return "document.getElementById(&#39;admin_itemtype_blocks_target&#39;).value=&#39;" + jsSafeTarget + "&#39;; " +
                   "document.getElementById(&#39;admin_itemtype_blocks_action&#39;).value=&#39;" + action + "&#39;; " +
                   "document.itemNavForm.submit(); return false;";
        }

        /// <summary> Renders the Blocks, Widgets &amp; Access tab </summary>
        private void write_blocks_tab_html(TextWriter Output)
        {
            if (!String.IsNullOrEmpty(blocksActionMessage))
            {
                Output.WriteLine("  <br />");
                Output.WriteLine("  <div id=\"sbkAdm_ActionMessageError\">" + blocksActionMessage + "</div>");
            }

            // ===== Metadata Blocks =====
            Output.WriteLine("  <h2>Metadata Blocks</h2>");
            Output.WriteLine("  <p><i>A locked block cannot be removed by the submitter on the metadata screen.</i></p>");

            if ((typeBlocks != null) && (typeBlocks.Rows.Count > 0))
            {
                Output.WriteLine("  <table class=\"sbkAdm_Table\">");
                Output.WriteLine("    <tr><th>ACTIONS</th><th>BLOCK</th><th>CATEGORY</th><th>LOCKED?</th></tr>");
                Output.WriteLine("    <tr><td class=\"sbkAdm_TableRule\" colspan=\"4\"></td></tr>");

                int blockRowIndex = 0;
                int blockRowCount = typeBlocks.Rows.Count;
                foreach (DataRow thisRow in typeBlocks.Rows)
                {
                    blockRowIndex++;
                    int blockId = Convert.ToInt32(thisRow["BlockID"]);
                    string blockName = thisRow["Name"].ToString();
                    string category = thisRow["Category"] == DBNull.Value ? "&mdash;" : System.Net.WebUtility.HtmlEncode(thisRow["Category"].ToString());
                    bool isRemovable = Convert.ToBoolean(thisRow["IsRemovable"]);

                    Output.WriteLine("    <tr style=\"text-align:left;\">");
                    Output.Write("      <td class=\"sbkAdm_ActionLink\">( ");
                    if (blockRowIndex > 1)
                        Output.Write("<a href=\"#\" onclick=\"" + row_action_onclick("moveup_block", blockId.ToString()) + "\">up</a> | ");
                    if (blockRowIndex < blockRowCount)
                        Output.Write("<a href=\"#\" onclick=\"" + row_action_onclick("movedown_block", blockId.ToString()) + "\">down</a> | ");
                    Output.Write("<a href=\"#\" onclick=\"" + row_action_onclick("remove_block", blockId.ToString()) + "\">remove</a>");
                    Output.WriteLine(" )</td>");
                    Output.WriteLine("      <td>" + System.Net.WebUtility.HtmlEncode(blockName) + "</td>");
                    Output.WriteLine("      <td>" + category + "</td>");
                    Output.Write("      <td><a href=\"#\" onclick=\"" + row_action_onclick("toggle_locked_block", blockId + "," + (isRemovable ? "0" : "1")) + "\">");
                    Output.Write(isRemovable ? "Optional (click to lock)" : "Locked (click to unlock)");
                    Output.WriteLine("</a></td>");
                    Output.WriteLine("    </tr>");
                    Output.WriteLine("    <tr><td class=\"sbkAdm_TableRule\" colspan=\"4\"></td></tr>");
                }
                Output.WriteLine("  </table>");
            }
            else
            {
                Output.WriteLine("  <p><i>No blocks added to this Type yet.</i></p>");
            }

            Output.WriteLine("  <blockquote>");
            if ((availableBlocksToAdd != null) && (availableBlocksToAdd.Count > 0))
            {
                Output.WriteLine("    <select class=\"sbkSaav_medium_input\" name=\"admin_itemtype_addblock_id\" id=\"admin_itemtype_addblock_id\">");
                foreach (DataRow thisBlock in availableBlocksToAdd)
                {
                    Output.WriteLine("      <option value=\"" + thisBlock["BlockID"] + "\">" + System.Net.WebUtility.HtmlEncode(thisBlock["Name"].ToString()) + "</option>");
                }
                Output.WriteLine("    </select>");
                Output.WriteLine("    <input type=\"checkbox\" name=\"admin_itemtype_addblock_locked\" id=\"admin_itemtype_addblock_locked\" /> <label for=\"admin_itemtype_addblock_locked\">Locked</label>");
                Output.WriteLine("    <button class=\"sbkAdm_RoundButton\" onclick=\"" + row_action_onclick("add_block", String.Empty) + "\">ADD BLOCK</button>");
            }
            else
            {
                Output.WriteLine("    <i>Every registered metadata block is already on this Type.</i>");
            }
            Output.WriteLine("  </blockquote>");

            // ===== Type-Specific Widgets =====
            Output.WriteLine("  <h2>Type-Specific Widgets</h2>");
            Output.WriteLine("  <p><i>Bespoke UI beyond ordinary fields, like the map footprint drawer.</i></p>");

            if ((typeWidgets != null) && (typeWidgets.Rows.Count > 0))
            {
                Output.WriteLine("  <table class=\"sbkAdm_Table\">");
                Output.WriteLine("    <tr><th>ACTIONS</th><th>WIDGET CODE</th><th>SCREEN</th></tr>");
                Output.WriteLine("    <tr><td class=\"sbkAdm_TableRule\" colspan=\"3\"></td></tr>");
                foreach (DataRow thisRow in typeWidgets.Rows)
                {
                    string widgetCode = thisRow["WidgetCode"].ToString();
                    string placement = thisRow["ScreenPlacement"].ToString();
                    Output.WriteLine("    <tr style=\"text-align:left;\">");
                    Output.WriteLine("      <td class=\"sbkAdm_ActionLink\">( <a href=\"#\" onclick=\"" + row_action_onclick("remove_widget", widgetCode) + "\">remove</a> )</td>");
                    Output.WriteLine("      <td>" + System.Net.WebUtility.HtmlEncode(widgetCode) + "</td>");
                    Output.WriteLine("      <td>" + System.Net.WebUtility.HtmlEncode(placement) + "</td>");
                    Output.WriteLine("    </tr>");
                    Output.WriteLine("    <tr><td class=\"sbkAdm_TableRule\" colspan=\"3\"></td></tr>");
                }
                Output.WriteLine("  </table>");
            }
            else
            {
                Output.WriteLine("  <p><i>No widgets added for this Type.</i></p>");
            }

            Output.WriteLine("  <blockquote>");
            Output.WriteLine("    <label for=\"admin_itemtype_addwidget_code\">Widget Code:</label> ");
            Output.WriteLine("    <input class=\"sbkSaav_small_input sbkAdmin_Focusable\" name=\"admin_itemtype_addwidget_code\" id=\"admin_itemtype_addwidget_code\" type=\"text\" placeholder=\"e.g. MAP_FOOTPRINT\" /> ");
            Output.WriteLine("    <select class=\"sbkSaav_small_input\" name=\"admin_itemtype_addwidget_placement\" id=\"admin_itemtype_addwidget_placement\">");
            Output.WriteLine("      <option value=\"upload\">Upload screen</option>");
            Output.WriteLine("      <option value=\"metadata\">Metadata screen</option>");
            Output.WriteLine("      <option value=\"confirm\">Confirm screen</option>");
            Output.WriteLine("    </select>");
            Output.WriteLine("    <button class=\"sbkAdm_RoundButton\" onclick=\"" + row_action_onclick("add_widget", String.Empty) + "\">ADD WIDGET</button>");
            Output.WriteLine("  </blockquote>");

            // ===== Default Metadata =====
            Output.WriteLine("  <h2>Default Metadata</h2>");
            Output.WriteLine("  <p><i>Constants stamped onto every new item of this Type, never shown to the submitter.</i></p>");

            if ((typeDefaultMetadata != null) && (typeDefaultMetadata.Rows.Count > 0))
            {
                Output.WriteLine("  <table class=\"sbkAdm_Table\">");
                Output.WriteLine("    <tr><th>ACTIONS</th><th>ELEMENT</th><th>VALUE</th></tr>");
                Output.WriteLine("    <tr><td class=\"sbkAdm_TableRule\" colspan=\"3\"></td></tr>");
                foreach (DataRow thisRow in typeDefaultMetadata.Rows)
                {
                    int defaultMetadataId = Convert.ToInt32(thisRow["DefaultMetadataID"]);
                    string elementCode = thisRow["ElementCode"].ToString();
                    string value = thisRow["Value"].ToString();
                    Output.WriteLine("    <tr style=\"text-align:left;\">");
                    Output.WriteLine("      <td class=\"sbkAdm_ActionLink\">( <a href=\"#\" onclick=\"" + row_action_onclick("remove_default_metadata", defaultMetadataId.ToString()) + "\">remove</a> )</td>");
                    Output.WriteLine("      <td>" + System.Net.WebUtility.HtmlEncode(elementCode) + "</td>");
                    Output.WriteLine("      <td>" + System.Net.WebUtility.HtmlEncode(value) + "</td>");
                    Output.WriteLine("    </tr>");
                    Output.WriteLine("    <tr><td class=\"sbkAdm_TableRule\" colspan=\"3\"></td></tr>");
                }
                Output.WriteLine("  </table>");
            }
            else
            {
                Output.WriteLine("  <p><i>No default metadata values set for this Type.</i></p>");
            }

            Output.WriteLine("  <blockquote>");
            Output.WriteLine("    <label for=\"admin_itemtype_adddefault_code\">Element:</label> ");
            Output.WriteLine("    <input class=\"sbkSaav_small_input sbkAdmin_Focusable\" name=\"admin_itemtype_adddefault_code\" id=\"admin_itemtype_adddefault_code\" type=\"text\" placeholder=\"e.g. Acquisition\" /> ");
            Output.WriteLine("    <label for=\"admin_itemtype_adddefault_value\">Value:</label> ");
            Output.WriteLine("    <input class=\"sbkSaav_large_input sbkAdmin_Focusable\" name=\"admin_itemtype_adddefault_value\" id=\"admin_itemtype_adddefault_value\" type=\"text\" /> ");
            Output.WriteLine("    <button class=\"sbkAdm_RoundButton\" onclick=\"" + row_action_onclick("add_default_metadata", String.Empty) + "\">ADD VALUE</button>");
            Output.WriteLine("  </blockquote>");

            // ===== Access =====
            Output.WriteLine("  <h2>Who Can Use This Type</h2>");
            Output.WriteLine("  <p><i>No restriction means every submitter sees it -- the moment anyone is added below, it becomes visible only to those users/groups.</i></p>");

            if ((typeAssignments != null) && (typeAssignments.Rows.Count > 0))
            {
                Output.WriteLine("  <table class=\"sbkAdm_Table\">");
                Output.WriteLine("    <tr><th>ACTIONS</th><th>USER / GROUP</th><th>KIND</th></tr>");
                Output.WriteLine("    <tr><td class=\"sbkAdm_TableRule\" colspan=\"3\"></td></tr>");
                foreach (DataRow thisRow in typeAssignments.Rows)
                {
                    int assignmentId = Convert.ToInt32(thisRow["AssignmentID"]);
                    string displayName = thisRow["DisplayName"].ToString();
                    bool isGroup = Convert.ToBoolean(thisRow["IsGroup"]);
                    Output.WriteLine("    <tr style=\"text-align:left;\">");
                    Output.WriteLine("      <td class=\"sbkAdm_ActionLink\">( <a href=\"#\" onclick=\"" + row_action_onclick("remove_access", assignmentId.ToString()) + "\">remove</a> )</td>");
                    Output.WriteLine("      <td>" + System.Net.WebUtility.HtmlEncode(displayName) + "</td>");
                    Output.WriteLine("      <td>" + (isGroup ? "Group" : "User") + "</td>");
                    Output.WriteLine("    </tr>");
                    Output.WriteLine("    <tr><td class=\"sbkAdm_TableRule\" colspan=\"3\"></td></tr>");
                }
                Output.WriteLine("  </table>");
            }
            else
            {
                Output.WriteLine("  <p><b>Everyone (no restriction)</b> -- every submitter can currently select this Type.</p>");
            }

            Output.WriteLine("  <blockquote>");
            Output.WriteLine("    <label for=\"admin_itemtype_addaccess_username\">Add user by username:</label> ");
            Output.WriteLine("    <input class=\"sbkSaav_small_input sbkAdmin_Focusable\" name=\"admin_itemtype_addaccess_username\" id=\"admin_itemtype_addaccess_username\" type=\"text\" /> ");
            Output.WriteLine("    <button class=\"sbkAdm_RoundButton\" onclick=\"" + row_action_onclick("add_access_user", String.Empty) + "\">ADD USER</button>");
            Output.WriteLine("    <br /><br />");
            Output.WriteLine("    <label for=\"admin_itemtype_addaccess_groupid\">Add group:</label> ");
            Output.WriteLine("    <select class=\"sbkSaav_medium_input\" name=\"admin_itemtype_addaccess_groupid\" id=\"admin_itemtype_addaccess_groupid\">");
            if (allUserGroups != null)
            {
                foreach (SobekCM.Core.Users.User_Group thisGroup in allUserGroups)
                {
                    Output.WriteLine("      <option value=\"" + thisGroup.UserGroupID + "\">" + System.Net.WebUtility.HtmlEncode(thisGroup.Name) + "</option>");
                }
            }
            Output.WriteLine("    </select>");
            Output.WriteLine("    <button class=\"sbkAdm_RoundButton\" onclick=\"" + row_action_onclick("add_access_group", String.Empty) + "\">ADD GROUP</button>");
            Output.WriteLine("  </blockquote>");

            // ===== Aggregation shortcuts =====
            Output.WriteLine("  <h2>Collection Shortcuts</h2>");
            Output.WriteLine("  <p><i>Optional -- shows an \"Add new\" shortcut for this Type directly on a collection's home page, skipping the Type grid.</i></p>");

            if ((typeAggregationLinks != null) && (typeAggregationLinks.Rows.Count > 0))
            {
                Output.WriteLine("  <table class=\"sbkAdm_Table\">");
                Output.WriteLine("    <tr><th>ACTIONS</th><th>COLLECTION</th><th>CODE</th></tr>");
                Output.WriteLine("    <tr><td class=\"sbkAdm_TableRule\" colspan=\"3\"></td></tr>");
                foreach (DataRow thisRow in typeAggregationLinks.Rows)
                {
                    int aggregationId = Convert.ToInt32(thisRow["AggregationID"]);
                    string code = thisRow["Code"].ToString();
                    string name = thisRow["Name"].ToString();
                    Output.WriteLine("    <tr style=\"text-align:left;\">");
                    Output.WriteLine("      <td class=\"sbkAdm_ActionLink\">( <a href=\"#\" onclick=\"" + row_action_onclick("remove_agglink", aggregationId.ToString()) + "\">remove</a> )</td>");
                    Output.WriteLine("      <td>" + System.Net.WebUtility.HtmlEncode(name) + "</td>");
                    Output.WriteLine("      <td>" + System.Net.WebUtility.HtmlEncode(code) + "</td>");
                    Output.WriteLine("    </tr>");
                    Output.WriteLine("    <tr><td class=\"sbkAdm_TableRule\" colspan=\"3\"></td></tr>");
                }
                Output.WriteLine("  </table>");
            }
            else
            {
                Output.WriteLine("  <p><i>No collection shortcuts added for this Type.</i></p>");
            }

            Output.WriteLine("  <blockquote>");
            Output.WriteLine("    <label for=\"admin_itemtype_addagglink_code\">Collection Code:</label> ");
            Output.WriteLine("    <input class=\"sbkSaav_small_input sbkAdmin_Focusable\" name=\"admin_itemtype_addagglink_code\" id=\"admin_itemtype_addagglink_code\" type=\"text\" /> ");
            Output.WriteLine("    <button class=\"sbkAdm_RoundButton\" onclick=\"" + row_action_onclick("add_agglink", String.Empty) + "\">ADD COLLECTION</button>");
            Output.WriteLine("  </blockquote>");
        }
    }
}
