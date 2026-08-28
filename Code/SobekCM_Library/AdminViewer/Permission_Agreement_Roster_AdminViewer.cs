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
    /// <summary> Read-only roster of every user who has accepted a single permissions agreement </summary>
    /// <remarks> This class extends the <see cref="abstract_AdminViewer"/> class. Reached from
    /// <see cref="Permission_Agreement_Mgmt_AdminViewer"/>'s "roster" link, keyed by AgreementID in
    /// <c>My_Sobek_SubMode</c> the same way <see cref="Permission_Agreement_Single_AdminViewer"/> is.
    /// Holds no postback of its own -- acceptance rows are a frozen snapshot
    /// (<c>SobekCM_User_Permissions_Agreement_Acceptance</c>) and nothing here is editable. </remarks>
    public class Permission_Agreement_Roster_AdminViewer : abstract_AdminViewer
    {
        private readonly int agreementId;
        private readonly string agreementName;
        private readonly DataTable acceptances;

        /// <summary> Constructor for a new instance of the Permission_Agreement_Roster_AdminViewer class </summary>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        public Permission_Agreement_Roster_AdminViewer(RequestCache RequestSpecificValues, HttpContext Context) : base(RequestSpecificValues, Context)
        {
            RequestSpecificValues.Tracer.Add_Trace("Permission_Agreement_Roster_AdminViewer.Constructor", String.Empty);

            // If the user cannot edit this, go back
            if ((!RequestSpecificValues.Current_User.Is_System_Admin) && (!RequestSpecificValues.Current_User.Is_Portal_Admin))
            {
                RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.My_Sobek;
                RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Home;
                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode, Context);
                return;
            }

            // The submode must be a valid AgreementID -- there is no "new" case for this viewer
            if (!Int32.TryParse(RequestSpecificValues.Current_Mode.My_Sobek_SubMode, out agreementId))
            {
                RequestSpecificValues.Current_Mode.Admin_Type = Admin_View_Codes.Permission_Agreements_Mgmt;
                RequestSpecificValues.Current_Mode.My_Sobek_SubMode = String.Empty;
                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode, Context);
                return;
            }

            DataRow agreementRow = SobekCM_Database.Get_Permissions_Agreement(agreementId, RequestSpecificValues.Tracer);
            if (agreementRow == null)
            {
                RequestSpecificValues.Current_Mode.Admin_Type = Admin_View_Codes.Permission_Agreements_Mgmt;
                RequestSpecificValues.Current_Mode.My_Sobek_SubMode = String.Empty;
                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode, Context);
                return;
            }

            agreementName = agreementRow["Name"].ToString();

            DataSet acceptancesSet = SobekCM_Database.Get_Permissions_Agreement_Acceptances(agreementId, RequestSpecificValues.Tracer);
            acceptances = ((acceptancesSet != null) && (acceptancesSet.Tables.Count > 0)) ? acceptancesSet.Tables[0] : null;
        }

        /// <summary> Title for the page that displays this viewer, this is shown in the search box at the top of the page, just below the banner </summary>
        /// <value> This always returns the value 'Acceptance Roster' </value>
        public override string Web_Title
        {
            get { return "Acceptance Roster"; }
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
            Tracer.Add_Trace("Permission_Agreement_Roster_AdminViewer.Write_HTML");

            // Open the item nav form
            Write_ItemNavForm_Opening(Output);

            // Add banner
            Banner_HtmlHelper.Add_Banner(Output, "sbkAhs_BannerDiv", "System Administration", RequestSpecificValues.Current_Mode, RequestSpecificValues.HTML_Skin, RequestSpecificValues.Top_Collection);

            // Build the URL back to the Mgmt list
            string last_admin_type = RequestSpecificValues.Current_Mode.Admin_Type;
            string last_submode = RequestSpecificValues.Current_Mode.My_Sobek_SubMode;
            RequestSpecificValues.Current_Mode.Admin_Type = Admin_View_Codes.Permission_Agreements_Mgmt;
            RequestSpecificValues.Current_Mode.My_Sobek_SubMode = String.Empty;
            string back_url = UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode);
            RequestSpecificValues.Current_Mode.Admin_Type = last_admin_type;
            RequestSpecificValues.Current_Mode.My_Sobek_SubMode = last_submode;

            Output.WriteLine("<div class=\"sbkAdm_HomeText\">");

            Output.WriteLine("  <a href=\"" + back_url + "\">&lt; back to Permissions Agreements</a>");
            Output.WriteLine("  <h1>Acceptance Roster</h1>");
            Output.WriteLine("  <p>" + System.Net.WebUtility.HtmlEncode(agreementName) + " &middot; <i>read-only</i></p>");

            if ((acceptances == null) || (acceptances.Rows.Count == 0))
            {
                Output.WriteLine("  <p><i>No one has accepted this agreement yet.</i></p>");
            }
            else
            {
                Output.WriteLine("  <table class=\"sbkAdm_Table\">");
                Output.WriteLine("    <tr>");
                Output.WriteLine("      <th>USER</th>");
                Output.WriteLine("      <th>ACCEPTED</th>");
                Output.WriteLine("      <th></th>");
                Output.WriteLine("     </tr>");
                Output.WriteLine("     <tr><td class=\"sbkAdm_TableRule\" colspan=\"3\"></td></tr>");

                int rowIndex = 0;
                foreach (DataRow thisRow in acceptances.Rows)
                {
                    rowIndex++;
                    string fullName = thisRow["FullName"].ToString();
                    DateTime acceptedDate = Convert.ToDateTime(thisRow["AcceptedDate"]);
                    bool earlierWording = Convert.ToBoolean(thisRow["IsEarlierWording"]);
                    string agreementText = thisRow["AgreementText"].ToString();
                    string detailRowId = "sbkPar_acceptance_detail_" + rowIndex;

                    Output.WriteLine("    <tr style=\"text-align:left;\">");
                    Output.WriteLine("      <td>" + System.Net.WebUtility.HtmlEncode(fullName) + "</td>");
                    Output.WriteLine("      <td>" + acceptedDate.ToString("yyyy-MM-dd") + "</td>");
                    Output.Write("      <td class=\"sbkAdm_ActionLink\">( <a href=\"#\" onclick=\"var d=document.getElementById('" + detailRowId + "'); d.style.display = (d.style.display === 'none' ? '' : 'none'); return false;\">view agreed text</a>");
                    if (earlierWording)
                        Output.Write(" &nbsp; <span style=\"color:#8a3f06;\">EARLIER WORDING</span>");
                    Output.WriteLine(" )</td>");
                    Output.WriteLine("    </tr>");
                    Output.WriteLine("    <tr id=\"" + detailRowId + "\" style=\"display:none;\">");
                    Output.WriteLine("      <td colspan=\"3\">");
                    if (earlierWording)
                    {
                        Output.WriteLine("        <div><i>This agreement's wording has changed since " + System.Net.WebUtility.HtmlEncode(fullName) + " accepted it on " + acceptedDate.ToString("yyyy-MM-dd") + ". The exact text accepted that day is shown below, not the agreement's current text.</i></div>");
                    }
                    Output.WriteLine("        <blockquote style=\"white-space:pre-wrap;\">" + System.Net.WebUtility.HtmlEncode(agreementText) + "</blockquote>");
                    Output.WriteLine("      </td>");
                    Output.WriteLine("    </tr>");
                    Output.WriteLine("    <tr><td class=\"sbkAdm_TableRule\" colspan=\"3\"></td></tr>");
                }

                Output.WriteLine("  </table>");
            }

            Output.WriteLine("  <br />");
            Output.WriteLine("</div>");

            // Close the item nav form
            Write_ItemNavForm_Closing(Output);
        }
    }
}
