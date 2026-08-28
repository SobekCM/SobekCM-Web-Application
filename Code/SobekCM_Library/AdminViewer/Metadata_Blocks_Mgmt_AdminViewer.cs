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
    /// <summary> Administrative screen provides a list of all existing metadata blocks and allows
    /// admin to reach the screen to add a new one </summary>
    /// <remarks> This class extends the <see cref="abstract_AdminViewer"/> class. Holds no add/edit
    /// form of its own -- both adding and editing are handled entirely by
    /// <see cref="Metadata_Block_Single_AdminViewer"/>, the same "Mgmt lists, Single adds-or-edits"
    /// split used by <see cref="Item_Types_Mgmt_AdminViewer"/>/<see cref="Permission_Agreement_Mgmt_AdminViewer"/>. </remarks>
    public class Metadata_Blocks_Mgmt_AdminViewer : abstract_AdminViewer
    {
        /// <summary> Constructor for a new instance of the Metadata_Blocks_Mgmt_AdminViewer class </summary>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        public Metadata_Blocks_Mgmt_AdminViewer(RequestCache RequestSpecificValues, HttpContext Context) : base(RequestSpecificValues, Context)
        {
            RequestSpecificValues.Tracer.Add_Trace("Metadata_Blocks_Mgmt_AdminViewer.Constructor", String.Empty);

            // If the user cannot edit this, go back
            if ((!RequestSpecificValues.Current_User.Is_System_Admin) && (!RequestSpecificValues.Current_User.Is_Portal_Admin))
            {
                RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.My_Sobek;
                RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Home;
                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode, Context);
            }
        }

        /// <summary> Title for the page that displays this viewer, this is shown in the search box at the top of the page, just below the banner </summary>
        /// <value> This always returns the value 'Metadata Blocks' </value>
        public override string Web_Title
        {
            get { return "Metadata Blocks"; }
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
            Tracer.Add_Trace("Metadata_Blocks_Mgmt_AdminViewer.Write_HTML");

            // Open the item nav form
            Write_ItemNavForm_Opening(Output);

            // Add banner
            Banner_HtmlHelper.Add_Banner(Output, "sbkAhs_BannerDiv", "System Administration", RequestSpecificValues.Current_Mode, RequestSpecificValues.HTML_Skin, RequestSpecificValues.Top_Collection);

            Output.WriteLine("<div class=\"sbkAdm_HomeText\">");

            Output.WriteLine("  <p>A metadata block is a reusable chunk of input fields (e.g. Title/Creator, Rights, Geographic) that an Item Type bundles together. This first-pass screen edits each block's XML as raw text; a real field-by-field block-authoring UI is 6.0 scope.</p>");

            // Build the URL to add a new block
            string last_admin_type = RequestSpecificValues.Current_Mode.Admin_Type;
            string last_submode = RequestSpecificValues.Current_Mode.My_Sobek_SubMode;
            RequestSpecificValues.Current_Mode.Admin_Type = Admin_View_Codes.Metadata_Block_Single;
            RequestSpecificValues.Current_Mode.My_Sobek_SubMode = "new";
            string new_block_url = UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode);
            RequestSpecificValues.Current_Mode.Admin_Type = last_admin_type;
            RequestSpecificValues.Current_Mode.My_Sobek_SubMode = last_submode;

            Output.WriteLine("  <a title=\"Add a new metadata block\" class=\"sbkAdm_RoundButton\" href=\"" + new_block_url + "\">NEW BLOCK</a>");
            Output.WriteLine("  <br /><br />");

            Output.WriteLine("  <h2>Existing Metadata Blocks</h2>");
            Output.WriteLine("  <table class=\"sbkAdm_Table\">");
            Output.WriteLine("    <tr>");
            Output.WriteLine("      <th>ACTIONS</th>");
            Output.WriteLine("      <th>NAME</th>");
            Output.WriteLine("      <th>CATEGORY</th>");
            Output.WriteLine("      <th>USED BY</th>");
            Output.WriteLine("      <th>STATUS</th>");
            Output.WriteLine("     </tr>");
            Output.WriteLine("     <tr><td class=\"sbkAdm_TableRule\" colspan=\"5\"></td></tr>");

            DataSet blocksSet = SobekCM_Database.Get_All_Metadata_Blocks(Tracer);
            if ((blocksSet != null) && (blocksSet.Tables.Count > 0))
            {
                foreach (DataRow thisBlock in blocksSet.Tables[0].Rows)
                {
                    int blockId = Convert.ToInt32(thisBlock["BlockID"]);
                    string name = thisBlock["Name"].ToString();
                    string category = thisBlock["Category"] == DBNull.Value ? "&mdash;" : System.Net.WebUtility.HtmlEncode(thisBlock["Category"].ToString());
                    bool enabled = Convert.ToBoolean(thisBlock["Enabled"]);
                    int assignedTypeCount = Convert.ToInt32(thisBlock["AssignedTypeCount"]);

                    // Build the edit URL for this block
                    RequestSpecificValues.Current_Mode.Admin_Type = Admin_View_Codes.Metadata_Block_Single;
                    RequestSpecificValues.Current_Mode.My_Sobek_SubMode = blockId.ToString();
                    string edit_url = UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode);
                    RequestSpecificValues.Current_Mode.Admin_Type = last_admin_type;
                    RequestSpecificValues.Current_Mode.My_Sobek_SubMode = last_submode;

                    string used_by = assignedTypeCount == 0 ? "&mdash;" : (assignedTypeCount + " Type" + (assignedTypeCount == 1 ? "" : "s"));

                    Output.WriteLine("    <tr style=\"text-align:left;" + (enabled ? String.Empty : " opacity:0.55;") + "\">");
                    Output.WriteLine("      <td class=\"sbkAdm_ActionLink\">( <a title=\"Click to edit this metadata block\" href=\"" + edit_url + "\">edit</a> )</td>");
                    Output.WriteLine("      <td>" + System.Net.WebUtility.HtmlEncode(name) + "</td>");
                    Output.WriteLine("      <td>" + category + "</td>");
                    Output.WriteLine("      <td>" + used_by + "</td>");
                    Output.WriteLine("      <td>" + (enabled ? "Active" : "Disabled") + "</td>");
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
