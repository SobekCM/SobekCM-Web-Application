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
    /// <summary> Administrative screen allows an existing permissions agreement to be
    /// edited, or a new permissions agreement to be added </summary>
    /// <remarks> This class extends the <see cref="abstract_AdminViewer"/> class. Mirrors the
    /// add-or-edit-in-one-class shape of <see cref="Builder_Folder_Mgmt_AdminViewer"/>, distinguishing
    /// "new" from "editing an existing row" via <see cref="RequestCache.Current_Mode"/>'s
    /// <c>My_Sobek_SubMode</c> ("new", or the agreement's integer ID) rather than a URL segment, so no
    /// changes were needed to <c>UrlWriterHelper</c>/<c>QueryString_Analyzer</c> to support this screen. </remarks>
    public class Permission_Agreement_Single_AdminViewer : abstract_AdminViewer
    {
        private string actionMessage;

        private readonly int agreementId;
        private string agreementName;
        private string agreementText;
        private bool enabled;

        /// <summary> Constructor for a new instance of the Permission_Agreement_Single_AdminViewer class </summary>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        /// <remarks> Postback from handling an edit or new permissions agreement is handled here in the constructor </remarks>
        public Permission_Agreement_Single_AdminViewer(RequestCache RequestSpecificValues, HttpContext Context) : base(RequestSpecificValues, Context)
        {
            RequestSpecificValues.Tracer.Add_Trace("Permission_Agreement_Single_AdminViewer.Constructor", String.Empty);

            actionMessage = String.Empty;

            // If the user cannot edit this, go back
            if ((!RequestSpecificValues.Current_User.Is_System_Admin) && (!RequestSpecificValues.Current_User.Is_Portal_Admin))
            {
                RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.My_Sobek;
                RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Home;
                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode, Context);
                return;
            }

            // Is there an agreement specified?  ( submode is either "new" or an integer AgreementID )
            agreementId = -1;
            string submode = RequestSpecificValues.Current_Mode.My_Sobek_SubMode;
            if ((!String.IsNullOrEmpty(submode)) && (String.Compare(submode, "new", StringComparison.OrdinalIgnoreCase) != 0))
            {
                if (!Int32.TryParse(submode, out agreementId))
                    agreementId = -1;
            }

            // Handle any post backs
            if ((RequestSpecificValues.Current_Mode.isPostBack) && (Context.Request.HasFormContentType))
            {
                try
                {
                    var form = Context.Request.Form;
                    string action_value = form["admin_permagreement_action"];

                    agreementName = form["admin_permagreement_name"];
                    agreementText = form["admin_permagreement_text"];
                    enabled = !String.IsNullOrEmpty(form["admin_permagreement_enabled"].TrimFirst());

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
                        if (String.IsNullOrWhiteSpace(agreementName)) errors.Add("NAME is required and missing");
                        if (String.IsNullOrWhiteSpace(agreementText)) errors.Add("AGREEMENT TEXT is required and missing");

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
                            bool result = SobekCM_Database.Edit_Permissions_Agreement(agreementId, agreementName, agreementText, enabled, RequestSpecificValues.Tracer);
                            if (!result)
                            {
                                actionMessage = "Unknown error encountered while saving this permissions agreement";
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
                agreementName = String.Empty;
                agreementText = String.Empty;
                enabled = true;

                if (agreementId > 0)
                {
                    DataRow row = SobekCM_Database.Get_Permissions_Agreement(agreementId, RequestSpecificValues.Tracer);
                    if (row != null)
                    {
                        agreementName = row["Name"].ToString();
                        agreementText = row["AgreementText"].ToString();
                        enabled = Convert.ToBoolean(row["Enabled"]);
                    }
                    else
                    {
                        agreementId = -1;
                    }
                }
            }
        }

        /// <summary> Builds the URL back to the permissions agreements management screen, without disturbing
        /// the Admin_Type / My_Sobek_SubMode this viewer itself was constructed with </summary>
        private static string build_mgmt_url(RequestCache RequestSpecificValues, HttpContext Context)
        {
            string last_admin_type = RequestSpecificValues.Current_Mode.Admin_Type;
            string last_submode = RequestSpecificValues.Current_Mode.My_Sobek_SubMode;

            RequestSpecificValues.Current_Mode.Admin_Type = Admin_View_Codes.Permission_Agreements_Mgmt;
            RequestSpecificValues.Current_Mode.My_Sobek_SubMode = String.Empty;
            string url = UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode);

            RequestSpecificValues.Current_Mode.Admin_Type = last_admin_type;
            RequestSpecificValues.Current_Mode.My_Sobek_SubMode = last_submode;

            return url;
        }

        /// <summary> Title for the page that displays this viewer, this is shown in the search box at the top of the page, just below the banner </summary>
        public override string Web_Title
        {
            get { return agreementId > 0 ? "Edit Permissions Agreement" : "Add New Permissions Agreement"; }
        }

        /// <summary> Gets the URL for the icon related to this administrative task </summary>
        public override string Viewer_Icon
        {
            get { return Static_Resources_Gateway.User_Permission_Img; }
        }

        /// <summary> Add the HTML to be displayed in the main SobekCM viewer area </summary>
        /// <param name="Output"> Textwriter to write the HTML for this viewer</param>
        /// <param name="Tracer">Trace object keeps a list of each method executed and important milestones in rendering</param>
        public override void Write_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace("Permission_Agreement_Single_AdminViewer.Write_HTML");

            // Open the item nav form
            Write_ItemNavForm_Opening(Output);

            // Add the banner
            Banner_HtmlHelper.Add_Banner(Output, "sbkAhs_BannerDiv", "System Administration", RequestSpecificValues.Current_Mode, RequestSpecificValues.HTML_Skin, RequestSpecificValues.Top_Collection);

            Output.WriteLine("<script src=\"" + Static_Resources_Gateway.Sobekcm_Admin_Js + "\" type=\"text/javascript\"></script>");

            Output.WriteLine("<!-- Hidden field is used for postbacks to indicate what to save and reset -->");
            Output.WriteLine("<input type=\"hidden\" id=\"admin_permagreement_action\" name=\"admin_permagreement_action\" value=\"\" />");
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

            Output.WriteLine("  <p>The text a submitter agrees to before their first submission. Assigned per user or user group elsewhere &mdash; not tied to this screen.</p>");
            Output.WriteLine("  <p>Editing the text below does not affect anyone who has already accepted this agreement &mdash; their acceptance keeps the exact wording they agreed to, on file.</p>");

            Output.WriteLine("  <table class=\"sbkAdm_PopupTable\">");

            Output.WriteLine("    <tr><td style=\"width: 145px\" class=\"sbkSaav_TableLabel\"><label for=\"admin_permagreement_name\">Name:</label></td>");
            Output.WriteLine("        <td><input class=\"sbkSaav_medium_input sbkAdmin_Focusable\" name=\"admin_permagreement_name\" id=\"admin_permagreement_name\" type=\"text\" value=\"" + System.Net.WebUtility.HtmlEncode(agreementName ?? String.Empty) + "\" /></td></tr>");

            Output.WriteLine("    <tr><td class=\"sbkSaav_TableLabel\">Active:</td>");
            Output.Write("        <td><input class=\"sbkSav_checkbox\" type=\"checkbox\" name=\"admin_permagreement_enabled\" id=\"admin_permagreement_enabled\" ");
            if (enabled)
                Output.Write("checked=\"checked\" ");
            Output.WriteLine("/> <label for=\"admin_permagreement_enabled\">Uncheck to retire this agreement, without deleting it</label></td></tr>");

            Output.WriteLine("    <tr><td class=\"sbkSaav_TableLabel2\" style=\"vertical-align:top;\"><label for=\"admin_permagreement_text\">Agreement Text:</label></td>");
            Output.WriteLine("        <td><textarea class=\"sbkSaav_large_textbox sbkAdmin_Focusable\" rows=\"14\" name=\"admin_permagreement_text\" id=\"admin_permagreement_text\">" + System.Net.WebUtility.HtmlEncode(agreementText ?? String.Empty) + "</textarea></td></tr>");

            string button_title = agreementId > 0 ? "Save changes to this permissions agreement" : "Add this new permissions agreement";

            Output.WriteLine("    <tr><td></td><td>");
            Output.WriteLine("      <button title=\"Do not apply changes\" class=\"sbkAdm_RoundButton\" onclick=\"set_hidden_value_postback('admin_permagreement_action', 'cancel'); return false;\"><img src=\"" + Static_Resources_Gateway.Button_Previous_Arrow_Png + "\" class=\"sbkAdm_RoundButton_LeftImg\" alt=\"\" /> CANCEL</button> &nbsp; &nbsp; ");
            Output.WriteLine("      <button title=\"" + button_title + "\" class=\"sbkAdm_RoundButton\" onclick=\"set_hidden_value_postback('admin_permagreement_action', 'save'); return false;\">SAVE <img src=\"" + Static_Resources_Gateway.Button_Next_Arrow_Png + "\" class=\"sbkAdm_RoundButton_RightImg\" alt=\"\" /></button>");
            Output.WriteLine("    </td></tr>");

            Output.WriteLine("  </table>");
            Output.WriteLine("</div>");

            // Close the item nav form
            Write_ItemNavForm_Closing(Output);
        }
    }
}
