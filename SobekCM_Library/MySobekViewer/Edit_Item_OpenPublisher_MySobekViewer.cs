using SobekCM.Core.Client;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.Navigation;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Engine_Library.Items;
using SobekCM.Library.AdminViewer;
using SobekCM.Library.HTML;
using SobekCM.Resource_Object;
using SobekCM.Resource_Object.Divisions;
using SobekCM.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace SobekCM.Library.MySobekViewer
{
    /// <summary> Class allows an authenticated user to edit a single digital resource's open textbook (oer)
    /// divisions within this digital library </summary>
    /// <remarks> This class extends the <see cref="abstract_MySobekViewer"/> class.</remarks>
    public class Edit_Item_OpenPublisher_MySobekViewer : abstract_MySobekViewer
    {
        private readonly SobekCM_Item currentItem;
        private readonly bool isMozilla;

        //private readonly

        /// <summary> Constructor for a new instance of the Edit_Item_OpenPublisher_MySobekViewer class </summary>
        ///  <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        public Edit_Item_OpenPublisher_MySobekViewer(RequestCache RequestSpecificValues) : base(RequestSpecificValues)
        {
            isMozilla = ((!String.IsNullOrEmpty(RequestSpecificValues.Current_Mode.Browser_Type)) && (RequestSpecificValues.Current_Mode.Browser_Type.ToUpper().IndexOf("FIREFOX") >= 0));

            RequestSpecificValues.Tracer.Add_Trace("Edit_Item_OpenPublisher_MySobekViewer.Constructor", String.Empty);

            // If no user then that is an error
            if ((RequestSpecificValues.Current_User == null) || (!RequestSpecificValues.Current_User.LoggedOn))
            {
                RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Aggregation;
                RequestSpecificValues.Current_Mode.Aggregation = String.Empty;
                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode);
                return;
            }

            // Ensure BibID and VID provided
            RequestSpecificValues.Tracer.Add_Trace("Edit_Item_OpenPublisher_MySobekViewer.Constructor", "Validate provided bibid / vid");
            if ((String.IsNullOrEmpty(RequestSpecificValues.Current_Mode.BibID)) || (String.IsNullOrEmpty(RequestSpecificValues.Current_Mode.VID)))
            {
                RequestSpecificValues.Tracer.Add_Trace("Edit_Item_OpenPublisher_MySobekViewer.Constructor", "BibID or VID was not provided!");
                RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Error;
                RequestSpecificValues.Current_Mode.Error_Message = "Invalid Request : BibID/VID missing in item behavior request";
                return;
            }

            RequestSpecificValues.Tracer.Add_Trace("Edit_Item_OpenPublisher_MySobekViewer.Constructor", "Try to pull this sobek complete item");
            currentItem = SobekEngineClient.Items.Get_Sobek_Item(RequestSpecificValues.Current_Mode.BibID, RequestSpecificValues.Current_Mode.VID, RequestSpecificValues.Current_User.UserID, RequestSpecificValues.Tracer);
            if (currentItem == null)
            {
                RequestSpecificValues.Tracer.Add_Trace("Edit_Item_OpenPublisher_MySobekViewer.Constructor", "Unable to build complete item");
                RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Error;
                RequestSpecificValues.Current_Mode.Error_Message = "Invalid Request : Unable to build complete item";
                return;
            }


            // If no item, then an error occurred
            if (currentItem == null)
            {
                RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Error;
                RequestSpecificValues.Current_Mode.Error_Message = "Invalid item indicated";
                return;
            }

            // If the RequestSpecificValues.Current_User cannot edit this currentItem, go back
            if (!RequestSpecificValues.Current_User.Can_Edit_This_Item(currentItem.BibID, currentItem.Bib_Info.SobekCM_Type_String, currentItem.Bib_Info.Source.Code, currentItem.Bib_Info.HoldingCode, currentItem.Behaviors.Aggregation_Code_List))
            {
                RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Home;
                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode);
                return;
            }

            // Ensure item has a divisions object (should really always have this)
            if (currentItem.Divisions.OpenTextbook_Tree.Roots?.Count == 0)
            {
                Division_TreeNode divNode = new Division_TreeNode("Chapter", "Main Body");
                currentItem.Divisions.OpenTextbook_Tree.Roots.Add(divNode);
            }

            // Handle post backs
            if (RequestSpecificValues.Current_Mode.isPostBack)
            {
                // See if there was a hidden request
                string action_requested = HttpContext.Current.Request.Form["action_requested"] ?? String.Empty;

                if ( action_requested == "cancel")
                {
                    RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Item_Display;
                    UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode);
                    return;
                }

                if ( action_requested == "save")
                {
                    string error_message;
                    SobekCM_Item_Updater.Update_Item(currentItem, RequestSpecificValues.Current_User, out error_message);

                    // Set the flag to rebuild the item
                    SobekCM_Item_Updater.Set_Item_Rebuild_Flag(currentItem, true);

                    // Clear this digital resource locally
                    CachedDataManager.Items.Remove_Digital_Resource_Object(RequestSpecificValues.Current_User.UserID, currentItem.BibID, currentItem.VID, null);

                    // Also clear the engine
                    SobekEngineClient.Items.Clear_Item_Cache(currentItem.BibID, currentItem.VID, RequestSpecificValues.Tracer);

                    // Forward to the display item again
                    RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Item_Display;
                    UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode);
                }

                string action_value = HttpContext.Current.Request.Form["action_value"] ?? String.Empty;
                string action_index = HttpContext.Current.Request.Form["action_index"] ?? String.Empty;

                if ( action_requested == "new_chapter")
                {
                    if (String.IsNullOrEmpty(action_value))
                        action_value = "Untitled";
                    
                    if ((!String.IsNullOrEmpty(action_index)) && ( Int32.TryParse(action_index, out int chapter_index )))
                    {
                        var divNode = new Division_TreeNode("Chapter", action_value);

                        if (chapter_index > currentItem.Divisions.OpenTextbook_Tree.Roots.Count)
                        {
                            currentItem.Divisions.OpenTextbook_Tree.Roots.Add(divNode);
                        }
                        else
                        {
                            currentItem.Divisions.OpenTextbook_Tree.Roots.Insert(chapter_index, divNode);
                        }
                    }
                }

                if ( action_requested == "new_division")
                {
                    if (String.IsNullOrEmpty(action_value))
                        action_value = "Untitled";

                    if ((!String.IsNullOrEmpty(action_index)) && (Int32.TryParse(action_index, out int chapter_index)))
                    {
                        var divisionNode = new Division_TreeNode("Division", action_value);

                        if (chapter_index < currentItem.Divisions.OpenTextbook_Tree.Roots.Count)
                        {
                            var rootNode = (Division_TreeNode)currentItem.Divisions.OpenTextbook_Tree.Roots[chapter_index];
                            rootNode.Nodes.Add(divisionNode);
                        }
                    }
                }

                if ( action_requested == "delete_chapter")
                {
                    if ((!String.IsNullOrEmpty(action_index)) && (Int32.TryParse(action_index, out int chapter_index)))
                    {
                        if (chapter_index < currentItem.Divisions.OpenTextbook_Tree.Roots.Count)
                        {
                            currentItem.Divisions.OpenTextbook_Tree.Roots.RemoveAt(chapter_index);
                        }
                    }
                }

                if (action_requested == "delete_division")
                {
                    if ((!String.IsNullOrEmpty(action_index)) && (action_index.IndexOf("|") > 0 ))
                    {
                        string[] split = action_index.Split('|');
                        if ((Int32.TryParse(split[0], out int chapter_index)) && (Int32.TryParse(split[1], out int division_index)))
                        {
                            if (chapter_index < currentItem.Divisions.OpenTextbook_Tree.Roots.Count)
                            {
                                var chapNode = (Division_TreeNode)currentItem.Divisions.OpenTextbook_Tree.Roots[chapter_index];
                                chapNode.Nodes.RemoveAt(division_index);
                            }
                        }
                    }
                }
            }
        }

        /// <summary> Navigation type to be displayed (mostly used by the mySobek viewers) </summary>
        /// <value> This returns none since this viewer writes all the necessary navigational elements </value>
        /// <remarks> This is set to NONE if the viewer will write its own navigation and ADMIN if the standard
        /// administrative tabs should be included as well.  </remarks>
        public override MySobek_Admin_Included_Navigation_Enum Standard_Navigation_Type => MySobek_Admin_Included_Navigation_Enum.NONE;

        /// <summary> Property indicates if this mySobek viewer can contain pop-up forms</summary>
        /// <remarks> If the mySobek viewer contains pop-up forms the overall page renders differently, 
        /// allowing for the blanket division and the popup forms near the top of the rendered HTML </remarks>
        ///<value> This mySobek viewer always returns the value TRUE </value>
        public override bool Contains_Popup_Forms => true;


        /// <summary> Title for the page that displays this viewer, this is shown in the search box at the top of the page, just below the banner </summary>
        /// <value> This returns the value 'OpenPublishing Tool' </value>
        public override string Web_Title => "OpenPublishing Tool";


        /// <summary> Add the HTML to be displayed in the main SobekCM viewer area (outside of the forms)</summary>
        /// <param name="Output"> Textwriter to write the HTML for this viewer</param>
        /// <param name="Tracer">Trace object keeps a list of each method executed and important milestones in rendering</param>
        /// <remarks> This class does nothing, since the CompleteTemplate html is added in the <see cref="Write_ItemNavForm_Closing" /> method </remarks>
        public override void Write_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace("Edit_Item_OpenPublisher_MySobekViewer.Write_HTML", "Do nothing");
        }

        /// <summary> Add the HTML to be displayed in the main SobekCM viewer area </summary>
        /// <param name="Output"> Textwriter to write the HTML for this viewer</param>
        /// <param name="Tracer">Trace object keeps a list of each method executed and important milestones in rendering</param>
        public override void Write_ItemNavForm_Closing(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace("Edit_Item_OpenPublisher_MySobekViewer.Write_ItemNavForm_Closing", "");


            Output.WriteLine("<!-- Edit_Item_OpenPublisher_MySobekViewer.Write_ItemNavForm_Closing -->");

            // Find the link for this item
            var lastMode = RequestSpecificValues.Current_Mode.Mode;
            RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Item_Display;
            string item_url = UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode);
            RequestSpecificValues.Current_Mode.Mode = lastMode;

            // Write the top item mimic html portion
            Write_Item_Type_Top(Output, currentItem);


            Output.WriteLine("<div id=\"container-openpublisher\">");

            Output.WriteLine("<div id=\"pagecontainer\">");

            Output.WriteLine("<script type=\"text/javascript\">");
            Output.WriteLine("   jQuery(document).ready(function() {");


            Output.WriteLine("     jQuery(document).on('mouseenter', '.oer_div_outer', function() {");
            Output.WriteLine("        jQuery(this).children('.oer_div_toolbox_outer').children('.oer_div_toolbox').show();");
            Output.WriteLine("      });");
            Output.WriteLine("     jQuery(document).on('mouseleave', '.oer_div_outer', function() {");
            Output.WriteLine("        jQuery(this).children('.oer_div_toolbox_outer').children('.oer_div_toolbox').hide();");
            Output.WriteLine("     });");
            Output.WriteLine("     jQuery(document).on('mouseenter', '.oer_div_inner', function() {");
            Output.WriteLine("        jQuery(this).children('.oer_div_inner_toolbox_outer').children('.oer_div_inner_toolbox').show();");
            Output.WriteLine("     });");
            Output.WriteLine("     jQuery(document).on('mouseleave', '.oer_div_inner', function() {");
            Output.WriteLine("        jQuery(this).children('.oer_div_inner_toolbox_outer').children('.oer_div_inner_toolbox').hide();");
            Output.WriteLine("     });");
            Output.WriteLine("   });");

            Output.WriteLine("</script>");


            Output.WriteLine("<div class=\"sbkMySobek_HomeText\">");
            Output.WriteLine("  <br />");
            Output.WriteLine("  <h2>Resource Structure</h2>");

            
            Output.WriteLine("  <div id=\"oer_button_div\">");
            Output.WriteLine("        <button onclick=\"op_div_cancel_form(); return false;\" class=\"sbkMySobek_BigButton\"><img src=\"" + Static_Resources_Gateway.Button_Previous_Arrow_Png + "\" class=\"sbkMySobek_RoundButton_LeftImg\" alt=\"\" /> CANCEL </button> &nbsp; &nbsp; ");
            Output.WriteLine("        <button onclick=\"op_div_save_form(); return false;\" class=\"sbkMySobek_BigButton\"> SAVE <img src=\"" + Static_Resources_Gateway.Button_Next_Arrow_Png + "\" class=\"sbkMySobek_RoundButton_RightImg\" alt=\"\" /></button>");
            Output.WriteLine("  </div>");

            Output.WriteLine("  <ul>");
            Output.WriteLine("    <li>This page allows you to edit the structure your resource.</li>");
            Output.WriteLine("    <li>You can add new chapters or divisions within a chapter from this screen.</li>");
            Output.WriteLine("    <li>You can also drag divisions around to reorder anything within your resource.</li>");
            Output.WriteLine("    <li>Click <a href=\"http://sobekrepository.org/helpyhelphelp/editinstructions\" target=\"_EDIT_INSTRUCTIONS\">here for detailed instructions</a> on this functionality.</li>");
            Output.WriteLine("  </ul>");



            Output.WriteLine("</div>");



            Output.WriteLine("<div id=\"oer_div_container\">");

            bool allowDelete = (currentItem.Divisions.OpenTextbook_Tree.Roots.Count != 1);

            string javascript_req = RequestSpecificValues.Current_Mode.Base_URL + "l/technical/javascriptrequired";
            int chapter_index = 0;
            int division_index = 0;
            int overall_index = 0;
            foreach( Division_TreeNode rootnode in currentItem.Divisions.OpenTextbook_Tree.Roots )
            {
                Output.WriteLine("  <div class=\"oer_div_outer\">");
                Output.WriteLine("    <div class=\"oer_div_outer_title\">" + HttpUtility.HtmlEncode(rootnode.Label) + "</div>");


                if ((rootnode.Nodes != null ) && (rootnode.Nodes.Count > 0))
                {
                    division_index = 0;
                    bool foundDivisions = false;
                    foreach (abstract_TreeNode childNode in rootnode.Nodes)
                    {
                        if (!childNode.Page)
                        {
                            foundDivisions = true;
                        }
                    }

                    if (foundDivisions)
                    {
                        Output.WriteLine("    <div class=\"oer_div_inner_wrapper\">");
                        foreach (abstract_TreeNode childNode in rootnode.Nodes)
                        {
                            if (!childNode.Page)
                            {
                                Output.WriteLine("      <div class=\"oer_div_inner\">");

                                string label = childNode.Label;
                                if (label.Length > 50)
                                    label = label.Substring(0, 45) + "...";
                                    
                                Output.WriteLine("        <div class=\"oer_div_inner_title\">" + HttpUtility.HtmlEncode(label) + "</div>");

                                Output.WriteLine("        <div class=\"oer_div_inner_toolbox_outer\">");
                                Output.WriteLine("          <div class=\"oer_div_inner_toolbox\" style=\"display:none;\">");
                                Output.WriteLine("            <a href=\"" + javascript_req + "\" onkeypress=\"\" onclick=\"\">view</a> &nbsp;");
                                Output.WriteLine("            <a href=\"" + javascript_req + "\" onkeypress=\"\" onclick=\"\">edit</a> &nbsp;");
                                Output.WriteLine($"            <a title=\"Delete this division\" href=\"{javascript_req}\" onkeypress=\"return delete_division('{chapter_index}', '{division_index}', '{HttpUtility.HtmlEncode(childNode.Label).Replace("'", "")}');\" onclick=\"return delete_division('{chapter_index}', '{division_index}', '{HttpUtility.HtmlEncode(childNode.Label).Replace("'", "")}');\">delete</a>");
                                Output.WriteLine("          </div>");
                                Output.WriteLine("        </div>");

                                Output.WriteLine("      </div>");

                                division_index++;
                                overall_index++;
                            }                            
                        }

                        // Add the plus sign
                        Output.WriteLine($"      <div class=\"oer_div_add_button\" onclick=\"return show_division_form('{chapter_index}');\">");
                        Output.WriteLine( "        <div class=\"oer_div_inner_title\">+</div>");
                        Output.WriteLine( "      </div>");

                        Output.WriteLine("    </div>");
                    }
                }

                Output.WriteLine("    <div class=\"oer_div_toolbox_outer\">");
                Output.WriteLine("      <div class=\"oer_div_toolbox\" style=\"display:none;\">");
                Output.WriteLine($"        <a title=\"Add a new division in this chapter\" href=\"{javascript_req}\" onkeypress=\"return show_division_form_keypress('{chapter_index}', '{isMozilla.ToString()}');\" onclick=\"return show_division_form('{chapter_index}');\">new division</a> &nbsp;");
                Output.WriteLine($"        <a title=\"Add a new chapter BEFORE this chapter\" href=\"{javascript_req}\" onkeypress=\"return show_chapter_form_keypress('{chapter_index}', '{isMozilla.ToString()}');\" onclick=\"return show_chapter_form('{chapter_index}');\">add before</a> &nbsp;");
                Output.WriteLine($"        <a title=\"Add a new chapter AFTER this chapter\" href=\"{javascript_req}\" onkeypress=\"return show_chapter_form_keypress('{chapter_index + 1}', '{isMozilla.ToString()}');\" onclick=\"return show_chapter_form('{chapter_index + 1}');\">add after</a> &nbsp;");
                Output.WriteLine("        <a href=\"" + javascript_req + "\" onkeypress=\"\" onclick=\"\">view</a> &nbsp;");
                Output.WriteLine("        <a href=\"" + javascript_req + "\" onkeypress=\"\" onclick=\"\">edit</a> &nbsp;");
                if (allowDelete)
                {
                    Output.WriteLine($"        <a title=\"Delete this chapter\" href=\"{javascript_req}\" onkeypress=\"return delete_chapter('{chapter_index}', '{HttpUtility.HtmlEncode(rootnode.Label).Replace("'","")}');\" onclick=\"return delete_chapter('{chapter_index}', '{HttpUtility.HtmlEncode(rootnode.Label).Replace("'", "")}');\">delete</a>");
                }
                Output.WriteLine("      </div>");
                Output.WriteLine("    </div>");
                Output.WriteLine("  </div>");

                chapter_index++;
                overall_index++;
            }

            Output.WriteLine("</div>");
            


            
            Output.WriteLine("</div>");
            Output.WriteLine("</div>");
            Output.WriteLine();
        }


        /// <summary> Add the HTML to be added near the top of the page for those viewers that implement pop-up forms for data retrieval </summary>
        /// <param name="Output"> Textwriter to write the pop-up form HTML for this viewer </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
        /// <remarks> This adds any popup divisions for form metadata elements </remarks>
        public override void Add_Popup_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace("Edit_Item_OpenPublisher_MySobekViewer.Add_Popup_HTML", "Add any popup divisions for form elements");

            // Add the hidden field
            Output.WriteLine("<!-- Hidden field is used for postbacks to add new form elements (i.e., new name, new other titles, etc..) -->");
            Output.WriteLine("<input type=\"hidden\" id=\"action_requested\" name=\"action_requested\" value=\"\" />");
            Output.WriteLine("<input type=\"hidden\" id=\"action_value\" name=\"action_value\" value=\"\" />");
            Output.WriteLine("<input type=\"hidden\" id=\"action_index\" name=\"action_index\" value=\"\" />");
            Output.WriteLine("<input type=\"hidden\" id=\"new_structure\" name=\"new_structure\" value=\"\"/>");
            Output.WriteLine();
            Output.WriteLine("<script type=\"text/javascript\" src=\"" + Static_Resources_Gateway.Jquery_Ui_1_10_3_Custom_Js + "\"></script>");
            Output.WriteLine("<script src=\"" + Static_Resources_Gateway.Sobekcm_OpenPublisher_Js + "\" type=\"text/javascript\"></script>");

            // Add form for new chapter (and title)
            Output.WriteLine("<!-- New chapter form -->");
            Output.WriteLine("<div class=\"related_url_popup_div sbkMetadata_PopupDiv\" id=\"form_new_chapter\" style=\"display:none;\">");
            Output.WriteLine("  <div class=\"sbkMetadata_PopupTitle\"><table style=\"width:100%\"><tr><td style=\"text-align:left\">New Chapter</td><td style=\"text-align:right\">");
            //Output.Write("<a href=\"" + Help_URL(Skin_Code, Base_URL) + "\" alt=\"HELP\" target=\"_" + html_element_name.ToUpper() + "\" >?</a> &nbsp;");
            Output.Write("<a href=\"#template\" alt=\"CANCEL\" onclick=\"return cancel_new_chapter_form()\">X</a> &nbsp; </td></tr></table></div>");
            Output.WriteLine("  <br />");
            Output.WriteLine("  <table class=\"sbkMetadata_PopupTable\">");

            // Add the rows of data
            Output.WriteLine("    <tr><td style=\"width:70px\"><label for=\"form_chapter_type\">Type:</label></td><td><input class=\"form_new_chapter_input sbk_Focusable\" name=\"form_chapter_type\" id=\"form_chapter_type\" type=\"text\" value=\"\" /></td></tr>");
            Output.WriteLine("    <tr><td><label for=\"form_chapter_title\">Title:</label></td><td><input class=\"form_new_chapter_input sbk_Focusable\" name=\"form_chapter_title\" id=\"form_chapter_title\" type=\"text\" value=\"\" onkeypress=\"op_handle_title_keypress(event)\"/></td></tr>");
            

            // Finish the popup form and add the CLOSE button
            Output.WriteLine("    <tr style=\"height:35px; text-align: center; vertical-align: bottom;\">");
            Output.WriteLine("      <td colspan=\"2\">");
            Output.WriteLine("         <button title=\"Cancel\" class=\"sbkMetadata_RoundButton\" onclick=\"return cancel_new_chapter_form();\">CANCEL</button> &nbsp;");
            Output.WriteLine("         <button title=\"Save\" class=\"sbkMetadata_RoundButton\" onclick=\"return save_new_chapter_form();\">SAVE</button> &nbsp;");
            Output.WriteLine("       </td>");
            Output.WriteLine("    </tr>");
            Output.WriteLine("  </table>");
            Output.WriteLine("</div>");
            Output.WriteLine();

            // Add form for new division (and title)
            Output.WriteLine("<!-- New chapter form -->");
            Output.WriteLine("<div class=\"related_url_popup_div sbkMetadata_PopupDiv\" id=\"form_new_division\" style=\"display:none;\">");
            Output.WriteLine("  <div class=\"sbkMetadata_PopupTitle\"><table style=\"width:100%\"><tr><td style=\"text-align:left\">New Division</td><td style=\"text-align:right\">");
            //Output.Write("<a href=\"" + Help_URL(Skin_Code, Base_URL) + "\" alt=\"HELP\" target=\"_" + html_element_name.ToUpper() + "\" >?</a> &nbsp;");
            Output.Write("<a href=\"#template\" alt=\"CANCEL\" onclick=\"return cancel_new_division_form()\">X</a> &nbsp; </td></tr></table></div>");
            Output.WriteLine("  <br />");
            Output.WriteLine("  <table class=\"sbkMetadata_PopupTable\">");

            // Add the rows of data
            Output.WriteLine("    <tr><td style=\"width:70px\">Type:</td><td>Division</td></tr>");
            Output.WriteLine("    <tr><td><label for=\"form_division_title\">Title:</label></td><td><input class=\"form_new_chapter_input sbk_Focusable\" name=\"form_division_title\" id=\"form_division_title\" type=\"text\" value=\"\" onkeypress=\"op_handle_divtitle_keypress(event)\" /></td></tr>");


            // Finish the popup form and add the CLOSE button
            Output.WriteLine("    <tr style=\"height:35px; text-align: center; vertical-align: bottom;\">");
            Output.WriteLine("      <td colspan=\"2\">");
            Output.WriteLine("         <button title=\"Cancel\" class=\"sbkMetadata_RoundButton\" onclick=\"return cancel_new_division_form();\">CANCEL</button> &nbsp;");
            Output.WriteLine("         <button title=\"Save\" class=\"sbkMetadata_RoundButton\" onclick=\"return save_new_division_form();\">SAVE</button> &nbsp;");
            Output.WriteLine("       </td>");
            Output.WriteLine("    </tr>");
            Output.WriteLine("  </table>");
            Output.WriteLine("</div>");
            Output.WriteLine();
        }

        /// <summary> Gets the collection of special behaviors which this admin or mySobek viewer
        /// requests from the main HTML subwriter. </summary>
        /// <value> This tells the HTML and mySobek writers to mimic the currentItem viewer </value>
        public override List<HtmlSubwriter_Behaviors_Enum> Viewer_Behaviors
        {
            get
            {
                return new List<HtmlSubwriter_Behaviors_Enum>
                {
                    HtmlSubwriter_Behaviors_Enum.MySobek_Subwriter_Mimic_Item_Subwriter,
                    HtmlSubwriter_Behaviors_Enum.Suppress_Banner
                };
            }
        }

        /// <summary> Write any additional values within the HTML Head of the final served page </summary>
        /// <param name="Output"> Output stream currently within the HTML head tags </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> TRUE if this should completely override the default added by the admin or mySobek viewer </returns>
        public override bool Write_Within_HTML_Head(TextWriter Output, Custom_Tracer Tracer)
        {
            Output.WriteLine("  <link href=\"" + Static_Resources_Gateway.Sobekcm_Metadata_Css + "\" rel=\"stylesheet\" type=\"text/css\" />");
            Output.WriteLine("  <link href=\"" + Static_Resources_Gateway.Sobekcm_OpenPublisher_Css + "\" rel=\"stylesheet\" type=\"text/css\" />");
            Output.WriteLine("  <link href=\"" + Static_Resources_Gateway.Sobekcm_Mysobek_Css + "\" rel=\"stylesheet\" type=\"text/css\" title=\"standard\" />");
            Output.WriteLine("  <link href=\"" + Static_Resources_Gateway.Sobekcm_Item_Css + "\" rel=\"stylesheet\" type=\"text/css\" />");



            return true;
        }


        /// <summary> Gets the CSS class of the container that the page is wrapped within </summary>
        public override string Container_CssClass => "container-inner1000";
    }
}
