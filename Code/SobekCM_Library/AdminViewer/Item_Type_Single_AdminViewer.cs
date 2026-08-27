#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.Navigation;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Library.Database;
using SobekCM.Library.HTML.Helpers;
using SobekCM.Library.UI;
using SobekCM.Resource_Object.Bib_Info;
using SobekCM.Tools;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;

#endregion

namespace SobekCM.Library.AdminViewer
{
    /// <summary> Administrative screen allows an existing Item Type to be edited, or a new Item Type
    /// to be added </summary>
    /// <remarks> This class extends the <see cref="abstract_AdminViewer"/> class. First pass only --
    /// covers the Details fields from the original wireframe (name, description, behavior flags,
    /// cataloging fields). The Blocks/Widgets/Access screen is deliberately NOT part of this pass; it
    /// needs a Metadata Block registry admin screen that doesn't exist yet either, so this viewer will
    /// likely need the same Single/Tab split <see cref="User_Group_AdminViewer"/> already went through
    /// once that lands. Modeled on <see cref="Permission_Agreement_Single_AdminViewer"/> and
    /// <see cref="Builder_Folder_Mgmt_AdminViewer"/>'s add-or-edit-in-one-class shape, distinguishing
    /// "new" from "editing an existing row" via <c>My_Sobek_SubMode</c>. </remarks>
    public class Item_Type_Single_AdminViewer : abstract_AdminViewer
    {
        private string actionMessage;

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

        /// <summary> Constructor for a new instance of the Item_Type_Single_AdminViewer class </summary>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        public Item_Type_Single_AdminViewer(RequestCache RequestSpecificValues, HttpContext Context) : base(RequestSpecificValues, Context)
        {
            RequestSpecificValues.Tracer.Add_Trace("Item_Type_Single_AdminViewer.Constructor", String.Empty);

            actionMessage = String.Empty;

            // If the user cannot edit this, go back
            if ((!RequestSpecificValues.Current_User.Is_System_Admin) && (!RequestSpecificValues.Current_User.Is_Portal_Admin))
            {
                RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.My_Sobek;
                RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Home;
                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode, Context);
                return;
            }

            // Is there a Type specified?  ( submode is either "new" or an integer TypeID )
            typeId = -1;
            string submode = RequestSpecificValues.Current_Mode.My_Sobek_SubMode;
            if ((!String.IsNullOrEmpty(submode)) && (String.Compare(submode, "new", StringComparison.OrdinalIgnoreCase) != 0))
            {
                if (!Int32.TryParse(submode, out typeId))
                    typeId = -1;
            }

            // Handle any post backs
            if ((RequestSpecificValues.Current_Mode.isPostBack) && (Context.Request.HasFormContentType))
            {
                try
                {
                    var form = Context.Request.Form;
                    string action_value = form["admin_itemtype_action"];

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

                    if (action_value == "cancel")
                    {
                        string returnUrl1 = build_mgmt_url(RequestSpecificValues, Context);
                        RequestSpecificValues.Current_Mode.Request_Completed = true;
                        Context.Response.Redirect(returnUrl1);
                        return;
                    }

                    if (action_value == "delete")
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

                    if (action_value == "save")
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
            else // NOT A POST BACK
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
                    else
                    {
                        typeId = -1;
                    }
                }
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

            Output.WriteLine("<!-- Hidden field is used for postbacks to indicate what to save and reset -->");
            Output.WriteLine("<input type=\"hidden\" id=\"admin_itemtype_action\" name=\"admin_itemtype_action\" value=\"\" />");
            Output.WriteLine();

            Output.WriteLine("<div class=\"sbkAdm_HomeText\">");

            if (!String.IsNullOrEmpty(actionMessage))
            {
                Output.WriteLine("  <br />");
                if (actionMessage.IndexOf("ERROR", StringComparison.InvariantCultureIgnoreCase) >= 0)
                    Output.WriteLine("  <div id=\"sbkAdm_ActionMessageError\">" + actionMessage + "</div>");
                else
                    Output.WriteLine("  <div id=\"sbkAdm_ActionMessageSuccess\">" + actionMessage + "</div>");
            }

            Output.WriteLine("  <p>Types are what a submitter picks first on the new-item screen -- each Type bundles the metadata blocks, upload behavior, and defaults for that kind of material. This first-pass screen covers the basic details only; block/widget/access editing is coming in a follow-up.</p>");

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
            Output.WriteLine("</div>");

            // Close the item nav form
            Write_ItemNavForm_Closing(Output);
        }
    }
}
