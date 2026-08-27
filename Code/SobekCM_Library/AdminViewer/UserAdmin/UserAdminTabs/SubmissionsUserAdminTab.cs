using Microsoft.AspNetCore.Http;
using SobekCM.Core.Users;
using SobekCM.Library.Database;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System;
using System.Data;
using System.IO;

namespace SobekCM.Library.AdminViewer.UserAdmin.UserAdminTabs
{
    /// <summary> Submissions tab for editing a single user -- whether they can submit new material,
    /// their default visibility, which permissions agreement (if any) they must accept, and whether
    /// they're restricted to a specific set of Item Types </summary>
    /// <remarks> "Can submit items" moved here from Basic Info -- it no longer lives there at all --
    /// so this tab is the single place submission-related settings are granted. Item Type restriction
    /// is an allowlist that defaults OPEN: an empty <see cref="User_Object.Restricted_Item_Types"/>
    /// means this user can select any enabled Type. </remarks>
    public class SubmissionsUserAdminTab : iUserAdminTab
    {
        public string TabName => "Submissions";

        public bool HandlePostback(IFormCollection form, User_Object editUser, RequestCache RequestSpecificValues)
        {
            editUser.Can_Submit = !String.IsNullOrEmpty(form["admin_submissions_can_submit"].TrimFirst());

            string visibility = form["admin_submissions_visibility"].TrimFirst();
            editUser.Default_Visibility = visibility switch
            {
                "public" => (short)0,
                "private" => (short)-1,
                _ => null
            };

            string agreementValue = form["admin_submissions_agreement"].TrimFirst();
            editUser.Permissions_Agreement_Id = (!String.IsNullOrEmpty(agreementValue) && Int32.TryParse(agreementValue, out int agreementId)) ? agreementId : (int?)null;

            bool restrictTypes = !String.IsNullOrEmpty(form["admin_submissions_restrict_types"].TrimFirst());
            editUser.Clear_Restricted_Item_Types();
            if (restrictTypes)
            {
                foreach (string thisKey in form.Keys)
                {
                    if (thisKey.IndexOf("admin_submissions_type_") == 0)
                    {
                        if (Int32.TryParse(thisKey.Replace("admin_submissions_type_", ""), out int typeId))
                            editUser.Add_Restricted_Item_Type(typeId);
                    }
                }
            }

            // No immediate save necessary
            return false;
        }

        public void RenderHtml(TextWriter Output, User_Object editUser, RequestCache RequestSpecificValues, Custom_Tracer Tracer)
        {
            Output.WriteLine("  <span class=\"SobekEditItemSectionTitle_first\"> &nbsp; Submission Ability</span>");
            Output.WriteLine("  <blockquote>");
            Output.Write("    <input class=\"admin_user_checkbox\" type=\"checkbox\" name=\"admin_submissions_can_submit\" id=\"admin_submissions_can_submit\"");
            if (editUser.Can_Submit)
                Output.Write(" checked=\"checked\"");
            Output.WriteLine(" /> <label for=\"admin_submissions_can_submit\">Can submit new material</label>");
            Output.WriteLine("  </blockquote>");
            Output.WriteLine("  <br />");

            Output.WriteLine("  <span class=\"SobekEditItemSectionTitle\"> &nbsp; Default Visibility</span>");
            Output.WriteLine("  <blockquote>");
            Output.WriteLine("    <select class=\"admin_small_input sbk_Focusable\" name=\"admin_submissions_visibility\" id=\"admin_submissions_visibility\">");
            Output.WriteLine("      <option value=\"\"" + (editUser.Default_Visibility == null ? " selected=\"selected\"" : "") + ">Not set</option>");
            Output.WriteLine("      <option value=\"public\"" + (editUser.Default_Visibility == 0 ? " selected=\"selected\"" : "") + ">Public</option>");
            Output.WriteLine("      <option value=\"private\"" + (editUser.Default_Visibility == -1 ? " selected=\"selected\"" : "") + ">Private</option>");
            Output.WriteLine("    </select>");
            Output.WriteLine("    <div><i>Applies to new items this user submits, unless a missing-file rule overrides it.</i></div>");
            Output.WriteLine("  </blockquote>");
            Output.WriteLine("  <br />");

            Output.WriteLine("  <span class=\"SobekEditItemSectionTitle\"> &nbsp; Permissions Agreement</span>");
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
                    bool selected = editUser.Permissions_Agreement_Id == agreementId;
                    Output.WriteLine("      <option value=\"" + agreementId + "\"" + (selected ? " selected=\"selected\"" : "") + ">" + System.Net.WebUtility.HtmlEncode(name) + "</option>");
                }
            }
            Output.WriteLine("    </select>");
            Output.WriteLine("    <div><i>Changing this to a different agreement asks the user to accept it again the next time they submit.</i></div>");
            Output.WriteLine("  </blockquote>");
            Output.WriteLine("  <br />");

            Output.WriteLine("  <span class=\"SobekEditItemSectionTitle\"> &nbsp; Item Types</span>");
            Output.WriteLine("  <blockquote>");
            bool restricted = (editUser.Restricted_Item_Types != null) && (editUser.Restricted_Item_Types.Count > 0);
            Output.Write("    <input class=\"admin_user_checkbox\" type=\"checkbox\" name=\"admin_submissions_restrict_types\" id=\"admin_submissions_restrict_types\" onchange=\"document.getElementById('admin_submissions_type_list').style.display = this.checked ? 'block' : 'none';\"");
            if (restricted)
                Output.Write(" checked=\"checked\"");
            Output.WriteLine(" /> <label for=\"admin_submissions_restrict_types\">Restrict to specific Types</label>");
            Output.WriteLine("    <div><i>Off means this user can select any enabled Type -- the default for most users.</i></div>");

            Output.WriteLine("    <div id=\"admin_submissions_type_list\"" + (restricted ? "" : " style=\"display:none;\"") + ">");
            DataSet typesSet = SobekCM_Database.Get_All_Item_Types(Tracer);
            if ((typesSet != null) && (typesSet.Tables.Count > 0))
            {
                foreach (DataRow thisType in typesSet.Tables[0].Rows)
                {
                    int typeId = Convert.ToInt32(thisType["TypeID"]);
                    string name = thisType["Name"].ToString();
                    bool selected = (editUser.Restricted_Item_Types != null) && editUser.Restricted_Item_Types.Contains(typeId);

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
