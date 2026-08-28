#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.Navigation;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Library.Database;
using SobekCM.Library.HTML.Helpers;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;

#endregion

namespace SobekCM.Library.AdminViewer
{
    /// <summary> Administrative screen allows an existing metadata block to be edited, or a new one
    /// to be added </summary>
    /// <remarks> This class extends the <see cref="abstract_AdminViewer"/> class. First pass only --
    /// a plain list/edit screen with the block's XML edited as raw text in a big textarea, not the
    /// field-by-field block-authoring UI (add/remove individual elements inside a block) that stays
    /// out of scope until 6.0, same as the original schema comment on
    /// <c>SobekCM_Metadata_Block</c> says. No delete -- this table has no system/custom distinction
    /// the way <see cref="Item_Type_Single_AdminViewer"/>'s does, and blocks are referenced by
    /// <c>SobekCM_Item_Type_Block</c>, so disabling is the only option offered here, matching
    /// <see cref="Permission_Agreement_Single_AdminViewer"/>'s convention rather than Item Type's.
    /// Modeled on both of those for its add-or-edit-in-one-class shape, distinguishing "new" from
    /// "editing an existing row" via <c>My_Sobek_SubMode</c>. </remarks>
    public class Metadata_Block_Single_AdminViewer : abstract_AdminViewer
    {
        private string actionMessage;

        private readonly int blockId;
        private string blockName;
        private string description;
        private string category;
        private string blockXml;
        private bool enabled;

        /// <summary> Constructor for a new instance of the Metadata_Block_Single_AdminViewer class </summary>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        public Metadata_Block_Single_AdminViewer(RequestCache RequestSpecificValues, HttpContext Context) : base(RequestSpecificValues, Context)
        {
            RequestSpecificValues.Tracer.Add_Trace("Metadata_Block_Single_AdminViewer.Constructor", String.Empty);

            actionMessage = String.Empty;

            // If the user cannot edit this, go back
            if ((!RequestSpecificValues.Current_User.Is_System_Admin) && (!RequestSpecificValues.Current_User.Is_Portal_Admin))
            {
                RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.My_Sobek;
                RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Home;
                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode, Context);
                return;
            }

            // Is there a block specified?  ( submode is either "new" or an integer BlockID )
            blockId = -1;
            string submode = RequestSpecificValues.Current_Mode.My_Sobek_SubMode;
            if ((!String.IsNullOrEmpty(submode)) && (String.Compare(submode, "new", StringComparison.OrdinalIgnoreCase) != 0))
            {
                if (!Int32.TryParse(submode, out blockId))
                    blockId = -1;
            }

            // Handle any post backs
            if ((RequestSpecificValues.Current_Mode.isPostBack) && (Context.Request.HasFormContentType))
            {
                try
                {
                    var form = Context.Request.Form;
                    string action_value = form["admin_metadatablock_action"];

                    blockName = form["admin_metadatablock_name"];
                    description = form["admin_metadatablock_description"];
                    category = form["admin_metadatablock_category"];
                    blockXml = form["admin_metadatablock_xml"];
                    enabled = !String.IsNullOrEmpty(form["admin_metadatablock_enabled"].TrimFirst());

                    if (action_value == "cancel")
                    {
                        string returnUrl1 = build_mgmt_url(RequestSpecificValues, Context);
                        RequestSpecificValues.Current_Mode.Request_Completed = true;
                        Context.Response.Redirect(returnUrl1);
                        return;
                    }

                    if (action_value == "save")
                    {
                        var errors = new List<string>();
                        if (String.IsNullOrWhiteSpace(blockName)) errors.Add("NAME is required and missing");
                        if (String.IsNullOrWhiteSpace(blockXml)) errors.Add("BLOCK XML is required and missing");

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
                            bool result = SobekCM_Database.Edit_Metadata_Block(blockId, blockName, description, category, blockXml, enabled, RequestSpecificValues.Tracer);
                            if (!result)
                            {
                                actionMessage = "Unknown error encountered while saving this metadata block";
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
                }
                catch
                {
                    actionMessage = "Unable to correctly parse postback data.";
                }
            }
            else // NOT A POST BACK
            {
                blockName = String.Empty;
                description = String.Empty;
                category = String.Empty;
                blockXml = String.Empty;
                enabled = true;

                if (blockId > 0)
                {
                    DataRow row = SobekCM_Database.Get_Metadata_Block(blockId, RequestSpecificValues.Tracer);
                    if (row != null)
                    {
                        blockName = row["Name"].ToString();
                        description = row["Description"] == DBNull.Value ? String.Empty : row["Description"].ToString();
                        category = row["Category"] == DBNull.Value ? String.Empty : row["Category"].ToString();
                        blockXml = row["BlockXml"].ToString();
                        enabled = Convert.ToBoolean(row["Enabled"]);
                    }
                    else
                    {
                        blockId = -1;
                    }
                }
            }
        }

        /// <summary> Builds the URL back to the Metadata Blocks management screen </summary>
        private static string build_mgmt_url(RequestCache RequestSpecificValues, HttpContext Context)
        {
            string last_admin_type = RequestSpecificValues.Current_Mode.Admin_Type;
            string last_submode = RequestSpecificValues.Current_Mode.My_Sobek_SubMode;

            RequestSpecificValues.Current_Mode.Admin_Type = Admin_View_Codes.Metadata_Blocks_Mgmt;
            RequestSpecificValues.Current_Mode.My_Sobek_SubMode = String.Empty;
            string url = UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode);

            RequestSpecificValues.Current_Mode.Admin_Type = last_admin_type;
            RequestSpecificValues.Current_Mode.My_Sobek_SubMode = last_submode;

            return url;
        }

        /// <summary> Title for the page that displays this viewer, this is shown in the search box at the top of the page, just below the banner </summary>
        public override string Web_Title
        {
            get { return blockId > 0 ? "Edit Metadata Block" : "Add New Metadata Block"; }
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
            Tracer.Add_Trace("Metadata_Block_Single_AdminViewer.Write_HTML");

            // Open the item nav form
            Write_ItemNavForm_Opening(Output);

            // Add the banner
            Banner_HtmlHelper.Add_Banner(Output, "sbkAhs_BannerDiv", "System Administration", RequestSpecificValues.Current_Mode, RequestSpecificValues.HTML_Skin, RequestSpecificValues.Top_Collection);

            Output.WriteLine("<script src=\"" + Static_Resources_Gateway.Sobekcm_Admin_Js + "\" type=\"text/javascript\"></script>");

            Output.WriteLine("<!-- Hidden field is used for postbacks to indicate what to save and reset -->");
            Output.WriteLine("<input type=\"hidden\" id=\"admin_metadatablock_action\" name=\"admin_metadatablock_action\" value=\"\" />");
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

            Output.WriteLine("  <p>A metadata block is a reusable chunk of input fields an Item Type can bundle in. This first-pass screen edits the block's XML as raw text -- a real field-by-field block-authoring UI is 6.0 scope.</p>");

            Output.WriteLine("  <table class=\"sbkAdm_PopupTable\">");

            Output.WriteLine("    <tr class=\"sbkSaav_TitleRow\"><td colspan=\"3\">Basic Info</td></tr>");
            Output.WriteLine("    <tr><td style=\"width: 145px\" class=\"sbkSaav_TableLabel\"><label for=\"admin_metadatablock_name\">Name:</label></td>");
            Output.WriteLine("        <td colspan=\"2\"><input class=\"sbkSaav_medium_input sbkAdmin_Focusable\" name=\"admin_metadatablock_name\" id=\"admin_metadatablock_name\" type=\"text\" value=\"" + System.Net.WebUtility.HtmlEncode(blockName ?? String.Empty) + "\" /></td></tr>");

            Output.WriteLine("    <tr><td class=\"sbkSaav_TableLabel\"><label for=\"admin_metadatablock_description\">Description:</label></td>");
            Output.WriteLine("        <td colspan=\"2\"><input class=\"sbkSaav_large_input sbkAdmin_Focusable\" name=\"admin_metadatablock_description\" id=\"admin_metadatablock_description\" type=\"text\" value=\"" + System.Net.WebUtility.HtmlEncode(description ?? String.Empty) + "\" /></td></tr>");

            Output.WriteLine("    <tr><td class=\"sbkSaav_TableLabel\"><label for=\"admin_metadatablock_category\">Category:</label></td>");
            Output.WriteLine("        <td colspan=\"2\"><input class=\"sbkSaav_small_input sbkAdmin_Focusable\" name=\"admin_metadatablock_category\" id=\"admin_metadatablock_category\" type=\"text\" value=\"" + System.Net.WebUtility.HtmlEncode(category ?? String.Empty) + "\" /> <i>(grouping for a future block-builder UI; free text for now)</i></td></tr>");

            Output.WriteLine("    <tr><td class=\"sbkSaav_TableLabel\">Active:</td>");
            Output.Write("        <td colspan=\"2\"><input class=\"sbkSav_checkbox\" type=\"checkbox\" name=\"admin_metadatablock_enabled\" id=\"admin_metadatablock_enabled\" ");
            if (enabled)
                Output.Write("checked=\"checked\" ");
            Output.WriteLine("/> <label for=\"admin_metadatablock_enabled\">Available for an Item Type to include</label></td></tr>");

            Output.WriteLine("    <tr class=\"sbkSaav_TitleRow\"><td colspan=\"3\">Block XML</td></tr>");
            Output.WriteLine("    <tr><td colspan=\"3\">");
            Output.WriteLine("      <textarea class=\"sbkAdmin_Focusable\" name=\"admin_metadatablock_xml\" id=\"admin_metadatablock_xml\" rows=\"24\" style=\"width:98%; font-family:monospace; font-size:12px;\">" + System.Net.WebUtility.HtmlEncode(blockXml ?? String.Empty) + "</textarea>");
            Output.WriteLine("    </td></tr>");

            string button_title = blockId > 0 ? "Save changes to this metadata block" : "Add this new metadata block";

            Output.WriteLine("    <tr><td></td><td colspan=\"2\">");
            Output.WriteLine("      <button title=\"Do not apply changes\" class=\"sbkAdm_RoundButton\" onclick=\"set_hidden_value_postback('admin_metadatablock_action', 'cancel'); return false;\"><img src=\"" + Static_Resources_Gateway.Button_Previous_Arrow_Png + "\" class=\"sbkAdm_RoundButton_LeftImg\" alt=\"\" /> CANCEL</button> &nbsp; &nbsp; ");
            Output.WriteLine("      <button title=\"" + button_title + "\" class=\"sbkAdm_RoundButton\" onclick=\"set_hidden_value_postback('admin_metadatablock_action', 'save'); return false;\">SAVE <img src=\"" + Static_Resources_Gateway.Button_Next_Arrow_Png + "\" class=\"sbkAdm_RoundButton_RightImg\" alt=\"\" /></button>");
            Output.WriteLine("    </td></tr>");

            Output.WriteLine("  </table>");
            Output.WriteLine("</div>");

            // Close the item nav form
            Write_ItemNavForm_Closing(Output);
        }
    }
}
