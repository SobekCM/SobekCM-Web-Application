#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.Navigation;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Library.Database;
using SobekCM.Library.HTML.Helpers;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System;
using System.Data;
using System.IO;

#endregion

namespace SobekCM.Library.AdminViewer
{
    /// <summary> Administrative screen provides a list of all existing permissions agreements
    /// and allows an admin to reach the screen to add a new one </summary>
    /// <remarks> This class extends the <see cref="abstract_AdminViewer"/> class. Deliberately holds
    /// no add/edit form of its own (unlike <see cref="Aggregations_Mgmt_AdminViewer"/>'s inline "new
    /// aggregation" form) -- both adding and editing are handled entirely by
    /// <see cref="Permission_Agreement_Single_AdminViewer"/>, the same "Mgmt lists, Single adds-or-edits"
    /// split used by <see cref="Builder_Folder_Mgmt_AdminViewer"/>. </remarks>
    public class Permission_Agreement_Mgmt_AdminViewer : abstract_AdminViewer
    {
        /// <summary> Constructor for a new instance of the Permission_Agreement_Mgmt_AdminViewer class </summary>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        public Permission_Agreement_Mgmt_AdminViewer(RequestCache RequestSpecificValues, HttpContext Context) : base(RequestSpecificValues, Context)
        {
            RequestSpecificValues.Tracer.Add_Trace("Permission_Agreement_Mgmt_AdminViewer.Constructor", String.Empty);

            // If the user cannot edit this, go back
            if ((!RequestSpecificValues.Current_User.Is_System_Admin) && (!RequestSpecificValues.Current_User.Is_Portal_Admin))
            {
                RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.My_Sobek;
                RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Home;
                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode, Context);
            }
        }

        /// <summary> Title for the page that displays this viewer, this is shown in the search box at the top of the page, just below the banner </summary>
        /// <value> This always returns the value 'Permissions Agreements' </value>
        public override string Web_Title
        {
            get { return "Permissions Agreements"; }
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
            Tracer.Add_Trace("Permission_Agreement_Mgmt_AdminViewer.Write_HTML");

            // Open the item nav form
            Write_ItemNavForm_Opening(Output);

            // Add banner
            Banner_HtmlHelper.Add_Banner(Output, "sbkAhs_BannerDiv", "System Administration", RequestSpecificValues.Current_Mode, RequestSpecificValues.HTML_Skin, RequestSpecificValues.Top_Collection);

            Output.WriteLine("<div class=\"sbkAdm_HomeText\">");

            Output.WriteLine("  <p>The text a submitter agrees to before their first submission. Assigned per user or user group, independent of which Type they submit under.</p>");
            Output.WriteLine("  <p>Retiring an agreement (rather than deleting it) keeps every past acceptance's frozen text readable &mdash; deletion is not offered here.</p>");

            // Build the URL to add a new agreement
            string last_admin_type = RequestSpecificValues.Current_Mode.Admin_Type;
            string last_submode = RequestSpecificValues.Current_Mode.My_Sobek_SubMode;
            RequestSpecificValues.Current_Mode.Admin_Type = Admin_View_Codes.Permission_Agreement_Single;
            RequestSpecificValues.Current_Mode.My_Sobek_SubMode = "new";
            string new_agreement_url = UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode);
            RequestSpecificValues.Current_Mode.Admin_Type = last_admin_type;
            RequestSpecificValues.Current_Mode.My_Sobek_SubMode = last_submode;

            Output.WriteLine("  <a title=\"Add a new permissions agreement\" class=\"sbkAdm_RoundButton\" href=\"" + new_agreement_url + "\">NEW AGREEMENT</a>");
            Output.WriteLine("  <br /><br />");

            Output.WriteLine("  <h2>Existing Permissions Agreements</h2>");
            Output.WriteLine("  <table class=\"sbkAdm_Table\">");
            Output.WriteLine("    <tr>");
            Output.WriteLine("      <th>ACTIONS</th>");
            Output.WriteLine("      <th>NAME</th>");
            Output.WriteLine("      <th>ASSIGNED TO</th>");
            Output.WriteLine("      <th>ACCEPTED</th>");
            Output.WriteLine("      <th>STATUS</th>");
            Output.WriteLine("     </tr>");
            Output.WriteLine("     <tr><td class=\"sbkAdm_TableRule\" colspan=\"5\"></td></tr>");

            DataSet agreementsSet = SobekCM_Database.Get_All_Permissions_Agreements(Tracer);
            if ((agreementsSet != null) && (agreementsSet.Tables.Count > 0))
            {
                foreach (DataRow thisAgreement in agreementsSet.Tables[0].Rows)
                {
                    int agreementId = Convert.ToInt32(thisAgreement["AgreementID"]);
                    string name = thisAgreement["Name"].ToString();
                    bool enabled = Convert.ToBoolean(thisAgreement["Enabled"]);
                    int userCount = Convert.ToInt32(thisAgreement["AssignedUserCount"]);
                    int groupCount = Convert.ToInt32(thisAgreement["AssignedGroupCount"]);
                    int acceptedCount = Convert.ToInt32(thisAgreement["AcceptedCount"]);

                    // Build the edit URL for this agreement
                    RequestSpecificValues.Current_Mode.Admin_Type = Admin_View_Codes.Permission_Agreement_Single;
                    RequestSpecificValues.Current_Mode.My_Sobek_SubMode = agreementId.ToString();
                    string edit_url = UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode);
                    RequestSpecificValues.Current_Mode.Admin_Type = last_admin_type;
                    RequestSpecificValues.Current_Mode.My_Sobek_SubMode = last_submode;

                    string assigned_to = "&mdash;";
                    if ((userCount > 0) || (groupCount > 0))
                    {
                        assigned_to = userCount + " user" + (userCount == 1 ? "" : "s");
                        if (groupCount > 0)
                            assigned_to = assigned_to + ", " + groupCount + " group" + (groupCount == 1 ? "" : "s");
                    }

                    Output.WriteLine("    <tr style=\"text-align:left;" + (enabled ? String.Empty : " opacity:0.55;") + "\">");
                    Output.WriteLine("      <td class=\"sbkAdm_ActionLink\">( <a title=\"Click to edit this permissions agreement\" href=\"" + edit_url + "\">edit</a> )</td>");
                    Output.WriteLine("      <td>" + System.Net.WebUtility.HtmlEncode(name) + "</td>");
                    Output.WriteLine("      <td>" + assigned_to + "</td>");
                    Output.WriteLine("      <td>" + acceptedCount + "</td>");
                    Output.WriteLine("      <td>" + (enabled ? "Active" : "Retired") + "</td>");
                    Output.WriteLine("    </tr>");
                    Output.WriteLine("    <tr><td class=\"sbkAdm_TableRule\" colspan=\"5\"></td></tr>");
                }
            }

            Output.WriteLine("  </table>");
            Output.WriteLine("  <br />");
            Output.WriteLine("</div>");

            // Close the item nav form
            Write_ItemNavForm_Closing(Output);
        }
    }
}
