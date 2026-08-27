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
    /// <summary> Administrative screen provides a list of all existing Item Types and allows an admin
    /// to reach the screen to add a new one </summary>
    /// <remarks> This class extends the <see cref="abstract_AdminViewer"/> class. Holds no add/edit
    /// form of its own -- both adding and editing are handled entirely by
    /// <see cref="Item_Type_Single_AdminViewer"/>, the same "Mgmt lists, Single adds-or-edits" split
    /// used by <see cref="Permission_Agreement_Mgmt_AdminViewer"/>/<see cref="Builder_Folder_Mgmt_AdminViewer"/>. </remarks>
    public class Item_Types_Mgmt_AdminViewer : abstract_AdminViewer
    {
        /// <summary> Constructor for a new instance of the Item_Types_Mgmt_AdminViewer class </summary>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        public Item_Types_Mgmt_AdminViewer(RequestCache RequestSpecificValues, HttpContext Context) : base(RequestSpecificValues, Context)
        {
            RequestSpecificValues.Tracer.Add_Trace("Item_Types_Mgmt_AdminViewer.Constructor", String.Empty);

            // If the user cannot edit this, go back
            if ((!RequestSpecificValues.Current_User.Is_System_Admin) && (!RequestSpecificValues.Current_User.Is_Portal_Admin))
            {
                RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.My_Sobek;
                RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Home;
                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode, Context);
            }
        }

        /// <summary> Title for the page that displays this viewer, this is shown in the search box at the top of the page, just below the banner </summary>
        /// <value> This always returns the value 'Item Types' </value>
        public override string Web_Title
        {
            get { return "Item Types"; }
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
            Tracer.Add_Trace("Item_Types_Mgmt_AdminViewer.Write_HTML");

            // Open the item nav form
            Write_ItemNavForm_Opening(Output);

            // Add banner
            Banner_HtmlHelper.Add_Banner(Output, "sbkAhs_BannerDiv", "System Administration", RequestSpecificValues.Current_Mode, RequestSpecificValues.HTML_Skin, RequestSpecificValues.Top_Collection);

            Output.WriteLine("<div class=\"sbkAdm_HomeText\">");

            Output.WriteLine("  <p>Define what submitters see on the first screen, and what each Type bundles -- upload behavior and defaults. This first-pass screen covers basic details only; metadata block/widget assignment is a follow-up.</p>");
            Output.WriteLine("  <p>Standard Types can only be disabled here, not deleted; custom Types created by your institution can be deleted outright.</p>");

            // Build the URL to add a new Type
            string last_admin_type = RequestSpecificValues.Current_Mode.Admin_Type;
            string last_submode = RequestSpecificValues.Current_Mode.My_Sobek_SubMode;
            RequestSpecificValues.Current_Mode.Admin_Type = Admin_View_Codes.Item_Type_Single;
            RequestSpecificValues.Current_Mode.My_Sobek_SubMode = "new";
            string new_type_url = UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode);
            RequestSpecificValues.Current_Mode.Admin_Type = last_admin_type;
            RequestSpecificValues.Current_Mode.My_Sobek_SubMode = last_submode;

            Output.WriteLine("  <a title=\"Add a new Item Type\" class=\"sbkAdm_RoundButton\" href=\"" + new_type_url + "\">NEW TYPE</a>");
            Output.WriteLine("  <br /><br />");

            Output.WriteLine("  <h2>Existing Item Types</h2>");
            Output.WriteLine("  <table class=\"sbkAdm_Table\">");
            Output.WriteLine("    <tr>");
            Output.WriteLine("      <th>ACTIONS</th>");
            Output.WriteLine("      <th>NAME</th>");
            Output.WriteLine("      <th>KIND</th>");
            Output.WriteLine("      <th>ASSIGNED TO</th>");
            Output.WriteLine("      <th>STATUS</th>");
            Output.WriteLine("     </tr>");
            Output.WriteLine("     <tr><td class=\"sbkAdm_TableRule\" colspan=\"5\"></td></tr>");

            DataSet typesSet = SobekCM_Database.Get_All_Item_Types_Mgmt(Tracer);
            if ((typesSet != null) && (typesSet.Tables.Count > 0))
            {
                foreach (DataRow thisType in typesSet.Tables[0].Rows)
                {
                    int typeId = Convert.ToInt32(thisType["TypeID"]);
                    string name = thisType["Name"].ToString();
                    bool isSystemType = Convert.ToBoolean(thisType["IsSystemType"]);
                    bool enabled = Convert.ToBoolean(thisType["Enabled"]);
                    int userCount = Convert.ToInt32(thisType["AssignedUserCount"]);
                    int groupCount = Convert.ToInt32(thisType["AssignedGroupCount"]);

                    // Build the edit URL for this Type
                    RequestSpecificValues.Current_Mode.Admin_Type = Admin_View_Codes.Item_Type_Single;
                    RequestSpecificValues.Current_Mode.My_Sobek_SubMode = typeId.ToString();
                    string edit_url = UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode);
                    RequestSpecificValues.Current_Mode.Admin_Type = last_admin_type;
                    RequestSpecificValues.Current_Mode.My_Sobek_SubMode = last_submode;

                    string assigned_to = "Everyone";
                    if ((userCount > 0) || (groupCount > 0))
                    {
                        assigned_to = userCount + " user" + (userCount == 1 ? "" : "s");
                        if (groupCount > 0)
                            assigned_to = assigned_to + ", " + groupCount + " group" + (groupCount == 1 ? "" : "s");
                    }

                    Output.WriteLine("    <tr style=\"text-align:left;" + (enabled ? String.Empty : " opacity:0.55;") + "\">");
                    Output.WriteLine("      <td class=\"sbkAdm_ActionLink\">( <a title=\"Click to edit this Item Type\" href=\"" + edit_url + "\">edit</a> )</td>");
                    Output.WriteLine("      <td>" + System.Net.WebUtility.HtmlEncode(name) + "</td>");
                    Output.WriteLine("      <td>" + (isSystemType ? "Standard" : "Custom") + "</td>");
                    Output.WriteLine("      <td>" + assigned_to + "</td>");
                    Output.WriteLine("      <td>" + (enabled ? "Enabled" : "Disabled") + "</td>");
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
