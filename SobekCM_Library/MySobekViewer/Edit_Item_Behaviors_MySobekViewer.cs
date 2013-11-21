﻿#region Using directives

using System;
using System.IO;
using System.Web;
using SobekCM.Library.Settings;
using SobekCM.Resource_Object;
using SobekCM.Resource_Object.Database;
using SobekCM.Library.Application_State;
using SobekCM.Library.Citation.Template;
using SobekCM.Library.HTML;
using SobekCM.Library.MainWriters;
using SobekCM.Library.MemoryMgmt;
using SobekCM.Library.Navigation;
using SobekCM.Library.Users;

#endregion

namespace SobekCM.Library.MySobekViewer
{
    /// <summary> Class allows an authenticated user to edit a single digital resource's behaviors within this digital library </summary>
    /// <remarks> This class extends the <see cref="abstract_MySobekViewer"/> class.<br /><br />
    /// MySobek Viewers are used for registration and authentication with mySobek, as well as performing any task which requires
    /// authentication, such as online submittal, metadata editing, and system administrative tasks.<br /><br />
    /// During a valid html request, the following steps occur:
    /// <ul>
    /// <li>Application state is built/verified by the <see cref="Application_State.Application_State_Builder"/> </li>
    /// <li>Request is analyzed by the <see cref="Navigation.SobekCM_QueryString_Analyzer"/> and output as a <see cref="SobekCM_Navigation_Object"/> </li>
    /// <li>Main writer is created for rendering the output, in his case the <see cref="Html_MainWriter"/> </li>
    /// <li>The HTML writer will create the necessary subwriter.  Since this action requires authentication, an instance of the  <see cref="MySobek_HtmlSubwriter"/> class is created. </li>
    /// <li>The mySobek subwriter creates an instance of this viewer to display the item's behaviors for editing</li>
    /// <li>This viewer uses the <see cref="SobekCM.Library.Citation.Template.Template"/> class to display the correct elements for editing </li>
    /// </ul></remarks>
    public class Edit_Item_Behaviors_MySobekViewer : abstract_MySobekViewer
    {
        private readonly SobekCM_Item item;
        private readonly Template template;

        #region Constructor

        /// <summary> Constructor for a new instance of the Edit_Item_Behaviors_MySobekViewer class </summary>
        /// <param name="User"> Authenticated user information </param>
        /// <param name="Current_Mode"> Mode / navigation information for the current request</param>
        /// <param name="Current_Item"> Individual digital resource to be edited by the user </param>
        /// <param name="Code_Manager"> Code manager contains the list of all valid aggregation codes </param>
        /// <param name="Tracer">Trace object keeps a list of each method executed and important milestones in rendering</param>
        public Edit_Item_Behaviors_MySobekViewer(User_Object User, SobekCM_Navigation_Object Current_Mode, SobekCM_Item Current_Item, Aggregation_Code_Manager Code_Manager, Custom_Tracer Tracer) : base(User)
        {
            Tracer.Add_Trace("Edit_Item_Behaviors_MySobekViewer.Constructor", String.Empty);

            currentMode = Current_Mode;
            item = Current_Item;

            // If the user cannot edit this item, go back
            if (!user.Can_Edit_This_Item(item))
            {
                currentMode.My_Sobek_Type = My_Sobek_Type_Enum.Home;
                currentMode.Redirect();
                return;
            }

            const string TEMPLATE_CODE = "itembehaviors";
            template = Cached_Data_Manager.Retrieve_Template(TEMPLATE_CODE, Tracer);
            if (template != null)
            {
                Tracer.Add_Trace("Edit_Item_Behaviors_MySobekViewer.Constructor", "Found template in cache");
            }
            else
            {
                Tracer.Add_Trace("Edit_Item_Behaviors_MySobekViewer.Constructor", "Reading template file");

                // Read this template
                Template_XML_Reader reader = new Template_XML_Reader();
                template = new Template();
                reader.Read_XML(SobekCM_Library_Settings.Base_MySobek_Directory + "templates\\defaults\\" + TEMPLATE_CODE + ".xml", template, true);

                // Add the current codes to this template
                template.Add_Codes(Code_Manager);

                // Save this into the cache
                Cached_Data_Manager.Store_Template(TEMPLATE_CODE, template, Tracer);
            }

            // See if there was a hidden request
            string hidden_request = HttpContext.Current.Request.Form["behaviors_request"] ?? String.Empty;

            // If this was a cancel request do that
            if (hidden_request == "cancel")
            {
                currentMode.Mode = Display_Mode_Enum.Item_Display;
                currentMode.Redirect();
            }
            else if (hidden_request == "save")
            {
                // Changes to the tracking box require the metadata search citation be rebuilt for this item
                // so save the old tracking box information first
                string oldTrackingBox = item.Tracking.Tracking_Box;

                // Save these changes to bib
                template.Save_To_Bib(item, user, 1);

                // Save the behaviors
                SobekCM_Database.Save_Behaviors(item, item.Behaviors.Text_Searchable, false );

                // Save the serial hierarchy as well (sort of a behavior)
                SobekCM_Database.Save_Serial_Hierarchy_Information(item, item.Web.GroupID, item.Web.ItemID);

                // Did the tracking box change?
                if (item.Tracking.Tracking_Box != oldTrackingBox)
                {
                    SobekCM_Database.Create_Full_Citation_Value(item.Web.ItemID);
                }

                // Remoe from the caches (to replace the other)
                Cached_Data_Manager.Remove_Digital_Resource_Object(item.BibID, item.VID, Tracer);

                // Also remove the list of volumes, since this may have changed
                Cached_Data_Manager.Remove_Items_In_Title(item.BibID, Tracer);

                // Forward
                currentMode.Mode = Display_Mode_Enum.Item_Display;
                currentMode.Redirect();
            }
        }

        #endregion

        /// <summary> Property indicates the standard navigation to be included at the top of the page by the
        /// main MySobek html subwriter. </summary>
        /// <value> This returns none since this viewer writes all the necessary navigational elements </value>
        /// <remarks> This is set to NONE if the viewer will write its own navigation and ADMIN if the standard
        /// administrative tabs should be included as well.  </remarks>
        public override MySobek_Included_Navigation_Enum Standard_Navigation_Type
        {
            get
            {
                return MySobek_Included_Navigation_Enum.NONE;
            }
        }

        /// <summary> Title for the page that displays this viewer, this is shown in the search box at the top of the page, just below the banner </summary>
        /// <value> This returns the value 'Edit Item Behaviors' </value>
        public override string Web_Title
        {
            get
            {
                return "Edit Item Behaviors";
            }
        }

        /// <summary> Add the HTML to be displayed in the main SobekCM viewer area (outside of the forms)</summary>
        /// <param name="Output"> Textwriter to write the HTML for this viewer</param>
        /// <param name="Tracer">Trace object keeps a list of each method executed and important milestones in rendering</param>
        /// <remarks> This class does nothing, since the template html is added in the <see cref="Write_ItemNavForm_Closing" /> method </remarks>
        public override void Write_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace("Edit_Item_Behaviors_MySobekViewer.Write_HTML", "Do nothing");
        }

        /// <summary> Add the HTML to be displayed in the main SobekCM viewer area </summary>
        /// <param name="Output"> Textwriter to write the HTML for this viewer</param>
        /// <param name="Tracer">Trace object keeps a list of each method executed and important milestones in rendering</param>
        public override void  Write_ItemNavForm_Closing(TextWriter Output, Custom_Tracer Tracer)
        {
			const string BEHAVIORS = "BEHAVIORS";

            Tracer.Add_Trace("Edit_Item_Behaviors_MySobekViewer.Write_ItemNavForm_Closing", "");

            Output.WriteLine("<!-- Hidden field is used for postbacks to add new form elements (i.e., new name, new other titles, etc..) -->");
            Output.WriteLine("<input type=\"hidden\" id=\"behaviors_request\" name=\"behaviors_request\" value=\"\" />");


            Output.WriteLine("<!-- Edit_Item_Behaviors_MySobekViewer.Write_ItemNavForm_Closing -->");
            Output.WriteLine("<div class=\"SobekText\">");
            Output.WriteLine("  <br />");
            Output.WriteLine("  <b>Edit this item's behaviors within this library.</b>");
            Output.WriteLine("  <ul>");
            Output.WriteLine("    <li>Enter the data for this item below and press the SAVE button when all your edits are complete.</li>");
            Output.WriteLine("    <li>Clicking on the green plus button ( <img class=\"repeat_button\" src=\"" + currentMode.Base_URL + "default/images/new_element_demo.jpg\" /> ) will add another instance of the element, if the element is repeatable.</li>");
            Output.WriteLine("    <li>Click <a href=\"" + SobekCM_Library_Settings.Help_URL(currentMode.Base_URL) + "help/behaviors\" target=\"_EDIT_INSTRUCTIONS\">here for detailed instructions</a> on editing behaviors online.</li>");
            Output.WriteLine("  </ul>");
            Output.WriteLine("</div>");
            Output.WriteLine();

			Output.WriteLine("<a name=\"template\"> </a>");
			Output.WriteLine("<br />");
			Output.WriteLine("<div id=\"tabContainer\" class=\"fulltabs\">");
			Output.WriteLine("  <div class=\"tabs\">");
			Output.WriteLine("    <ul>");
			Output.WriteLine("      <li id=\"tabHeader_1\" class=\"tabActiveHeader\">" + BEHAVIORS + "</li>");
			Output.WriteLine("    </ul>");
			Output.WriteLine("  </div>");
			Output.WriteLine("  <div class=\"graytabscontent\">");
			Output.WriteLine("    <div class=\"tabpage\" id=\"tabpage_1\">");

			Output.WriteLine("      <!-- Add SAVE and CANCEL buttons to top of form -->");
			Output.WriteLine("      <script src=\"" + currentMode.Base_URL + "default/scripts/sobekcm_metadata.js\" type=\"text/javascript\"></script>");
			Output.WriteLine();

			Output.WriteLine("      <div class=\"sbkMySobek_RightButtons\">");
			Output.WriteLine("        <button onclick=\"behaviors_cancel_form(); return false;\" class=\"sbkMySobek_BigButton\"><img src=\"" + currentMode.Base_URL + "default/images/button_previous_arrow.png\" class=\"sbkMySobek_RoundButton_LeftImg\" alt=\"\" /> CANCEL </button> &nbsp; &nbsp; ");
			Output.WriteLine("        <button onclick=\"behaviors_save_form(); return false;\" class=\"sbkMySobek_BigButton\"> SAVE <img src=\"" + currentMode.Base_URL + "default/images/button_next_arrow.png\" class=\"sbkMySobek_RoundButton_RightImg\" alt=\"\" /></button>");
			Output.WriteLine("      </div>");
			Output.WriteLine("      <br /><br />");
			Output.WriteLine();

	        bool isMozilla = currentMode.Browser_Type.ToUpper().IndexOf("FIREFOX") >= 0;
	        template.Render_Template_HTML(Output, item, currentMode.Skin == currentMode.Default_Skin ? currentMode.Skin.ToUpper() : currentMode.Skin, isMozilla, user, currentMode.Language, Translator, currentMode.Base_URL, 1);

			// Add the second buttons at the bottom of the form
			Output.WriteLine();
			Output.WriteLine("      <!-- Add SAVE and CANCEL buttons to bottom of form -->");
			Output.WriteLine("      <div class=\"sbkMySobek_RightButtons\">");
			Output.WriteLine("        <button onclick=\"behaviors_cancel_form(); return false;\" class=\"sbkMySobek_BigButton\"><img src=\"" + currentMode.Base_URL + "default/images/button_previous_arrow.png\" class=\"sbkMySobek_RoundButton_LeftImg\" alt=\"\" /> CANCEL </button> &nbsp; &nbsp; ");
			Output.WriteLine("        <button onclick=\"behaviors_save_form(); return false;\" class=\"sbkMySobek_BigButton\"> SAVE <img src=\"" + currentMode.Base_URL + "default/images/button_next_arrow.png\" class=\"sbkMySobek_RoundButton_RightImg\" alt=\"\" /></button>");
			Output.WriteLine("      </div>");
			Output.WriteLine("      <br />");
			Output.WriteLine("    </div>");
			Output.WriteLine("  </div>");
			Output.WriteLine("</div>");
			Output.WriteLine("<br />");
        }

        /// <summary> Add the HTML to be added near the top of the page for those viewers that implement pop-up forms for data retrieval </summary>
        /// <param name="Output"> Textwriter to write the pop-up form HTML for this viewer </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
        /// <remarks> This adds any popup divisions for form metadata elements </remarks>
        public override void Add_Popup_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace("Edit_Item_Behaviors_MySobekViewer.Add_Popup_HTML", "Add any popup divisions for form elements");

            // Add the hidden field
            Output.WriteLine();
        }
    }
}




