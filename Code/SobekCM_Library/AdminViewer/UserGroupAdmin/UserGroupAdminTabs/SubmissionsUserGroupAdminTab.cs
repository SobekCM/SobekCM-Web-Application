using Microsoft.AspNetCore.Http;
using SobekCM.Core.Users;
using SobekCM.Library.Database;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System;
using System.Data;
using System.IO;

namespace SobekCM.Library.AdminViewer.UserGroupAdmin.UserGroupAdminTabs
{
    /// <summary> Submissions tab for editing a single user group -- mirrors
    /// <c>SubmissionsUserAdminTab</c> exactly, but sets DEFAULTS stamped onto newly provisioned
    /// members rather than live per-person state (a group doesn't accept a permissions agreement
    /// itself -- only individual people do). </summary>
    public class SubmissionsUserGroupAdminTab : iUserGroupAdminTab
    {
        public string TabName => "Submissions";

        public bool HandlePostback(IFormCollection form, User_Group editGroup, RequestCache RequestSpecificValues)
        {
            editGroup.CanSubmit = !String.IsNullOrEmpty(form["admin_submissions_can_submit"].TrimFirst());

            string visibility = form["admin_submissions_visibility"].TrimFirst();
            editGroup.Default_Visibility = visibility switch
            {
                "public" => (short)0,
                "private" => (short)-1,
                _ => null
            };

            string agreementValue = form["admin_submissions_agreement"].TrimFirst();
            editGroup.Permissions_Agreement_Id = (!String.IsNullOrEmpty(agreementValue) && Int32.TryParse(agreementValue, out int agreementId)) ? agreementId : (int?)null;

            bool restrictTypes = !String.IsNullOrEmpty(form["admin_submissions_restrict_types"].TrimFirst());
            editGroup.Clear_Restricted_Item_Types();
            if (restrictTypes)
            {
                foreach (string thisKey in form.Keys)
                {
                    if (thisKey.IndexOf("admin_submissions_type_") == 0)
                    {
                        if (Int32.TryParse(thisKey.Replace("admin_submissions_type_", ""), out int typeId))
                            editGroup.Add_Restricted_Item_Type(typeId);
                    }
                }
            }

            // No immediate save necessary
            return false;
        }

        public void RenderHtml(TextWriter Output, User_Group editGroup, RequestCache RequestSpecificValues, Custom_Tracer Tracer)
        {
            Output.WriteLine("  <span class=\"SobekEditItemSectionTitle_first\"> &nbsp; Submission Ability</span>");
            Output.WriteLine("  <blockquote>");
            Output.Write("    <input class=\"admin_user_checkbox\" type=\"checkbox\" name=\"admin_submissions_can_submit\" id=\"admin_submissions_can_submit\"");
            if (editGroup.CanSubmit)
                Output.Write(" checked=\"checked\"");
            Output.WriteLine(" /> <label for=\"admin_submissions_can_submit\">Members can submit new material by default</label>");
            Output.WriteLine("    <div><i>Individual members can still be adjusted afterward on their own user record.</i></div>");
            Output.WriteLine("  </blockquote>");
            Output.WriteLine("  <br />");

            Output.WriteLine("  <span class=\"SobekEditItemSectionTitle\"> &nbsp; Default Visibility (for new members)</span>");
            Output.WriteLine("  <blockquote>");
            Output.WriteLine("    <select class=\"admin_small_input sbk_Focusable\" name=\"admin_submissions_visibility\" id=\"admin_submissions_visibility\">");
            Output.WriteLine("      <option value=\"\"" + (editGroup.Default_Visibility == null ? " selected=\"selected\"" : "") + ">Not set</option>");
            Output.WriteLine("      <option value=\"public\"" + (editGroup.Default_Visibility == 0 ? " selected=\"selected\"" : "") + ">Public</option>");
            Output.WriteLine("      <option value=\"private\"" + (editGroup.Default_Visibility == -1 ? " selected=\"selected\"" : "") + ">Private</option>");
            Output.WriteLine("    </select>");
            Output.WriteLine("  </blockquote>");
            Output.WriteLine("  <br />");

            Output.WriteLine("  <span class=\"SobekEditItemSectionTitle\"> &nbsp; Default Permissions Agreement (for new members)</span>");
            Output.WriteLine("  <blockquote>");
            Output.WriteLine("    <select class=\"admin_large_input sbk_Focusable\" name=\"admin_submissions_agreement\" id=\"admin_submissions_agreement\">");
            Output.WriteLine("      <option value=\"\">None required</option>");
            DataSet agreementsSet = SobekCM_Database.Get_All_Permissions_Agreements(Tracer);
            if ((agreementsSet != null) && (agreementsSet.Tables.Count > 0))
            {
                foreach (DataRow thisAgreement in agreementsSet.Tables[0].Rows)
                {
                    int agreementId = Convert.ToInt32(thisAgreement["AgreementID"]);
                    string name = thisAgreement["Name"].ToString();
                    bool selected = editGroup.Permissions_Agreement_Id == agreementId;
                    Output.WriteLine("      <option value=\"" + agreementId + "\"" + (selected ? " selected=\"selected\"" : "") + ">" + System.Net.WebUtility.HtmlEncode(name) + "</option>");
                }
            }
            Output.WriteLine("    </select>");
            Output.WriteLine("    <div><i>Assigned the first time each new member submits -- groups don't accept agreements themselves, only individual people do.</i></div>");
            Output.WriteLine("  </blockquote>");
            Output.WriteLine("  <br />");

            Output.WriteLine("  <span class=\"SobekEditItemSectionTitle\"> &nbsp; Item Types</span>");
            Output.WriteLine("  <blockquote>");
            bool restricted = (editGroup.Restricted_Item_Types != null) && (editGroup.Restricted_Item_Types.Count > 0);
            Output.Write("    <input class=\"admin_user_checkbox\" type=\"checkbox\" name=\"admin_submissions_restrict_types\" id=\"admin_submissions_restrict_types\" onchange=\"document.getElementById('admin_submissions_type_list').style.display = this.checked ? 'block' : 'none';\"");
            if (restricted)
                Output.Write(" checked=\"checked\"");
            Output.WriteLine(" /> <label for=\"admin_submissions_restrict_types\">Restrict members to specific Types</label>");
            Output.WriteLine("    <div><i>Off means members can select any enabled Type -- the default.</i></div>");

            Output.WriteLine("    <div id=\"admin_submissions_type_list\"" + (restricted ? "" : " style=\"display:none;\"") + ">");
            DataSet typesSet = SobekCM_Database.Get_All_Item_Types(Tracer);
            if ((typesSet != null) && (typesSet.Tables.Count > 0))
            {
                foreach (DataRow thisType in typesSet.Tables[0].Rows)
                {
                    int typeId = Convert.ToInt32(thisType["TypeID"]);
                    string name = thisType["Name"].ToString();
                    bool selected = (editGroup.Restricted_Item_Types != null) && editGroup.Restricted_Item_Types.Contains(typeId);

                    Output.Write("      <input type=\"checkbox\" name=\"admin_submissions_type_" + typeId + "\" id=\"admin_submissions_type_" + typeId + "\"");
                    if (selected)
                        Output.Write(" checked=\"checked\"");
                    Output.WriteLine(" /> <label for=\"admin_submissions_type_" + typeId + "\">" + System.Net.WebUtility.HtmlEncode(name) + "</label><br />");
                }
            }
            Output.WriteLine("    </div>");
            Output.WriteLine("  </blockquote>");
        }
    }
}
