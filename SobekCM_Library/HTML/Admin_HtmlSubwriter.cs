﻿#region Using directives

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.Navigation;
using SobekCM.Core.UI_Configuration;
using SobekCM.Core.UI_Configuration.StaticResources;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Engine_Library.Database;
using SobekCM.Library.AdminViewer;
using SobekCM.Library.MainWriters;
using SobekCM.Library.MySobekViewer;
using SobekCM.Library.UI;
using SobekCM.Resource_Object;
using SobekCM.Resource_Object.Bib_Info;
using SobekCM.Tools;

#endregion

namespace SobekCM.Library.HTML
{
	/// <summary> Adminl subwriter is used for administrative tasks, either collection admin, portal admin, or system admin </summary>
	/// <remarks> This class extends the <see cref="abstractHtmlSubwriter"/> abstract class. <br /><br />
	/// During a valid html request, the following steps occur:
	/// <ul>
	/// <li>Application state is built/verified by the Application_State_Builder </li>
	/// <li>Request is analyzed by the QueryString_Analyzer and output as a <see cref="Navigation_Object"/>  </li>
	/// <li>Main writer is created for rendering the output, in his case the <see cref="Html_MainWriter"/> </li>
	/// <li>The HTML writer will create this necessary subwriter since this action requires administrative rights. </li>
	/// <li>This class will create a admin subwriter (extending <see cref="AdminViewer.abstract_AdminViewer"/> ) for the specified task.The admin subwriter creates an instance of this viewer to view and edit existing item aggregationPermissions in this digital library</li>
	/// </ul></remarks>
    public class Admin_HtmlSubwriter: abstractHtmlSubwriter
    {
        private readonly iMySobek_Admin_Viewer adminViewer;

 
        #region Constructor, which also creates the applicable MySobekViewer object

        /// <summary> Constructor for a new instance of the Admin_HtmlSubwriter class </summary>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        public Admin_HtmlSubwriter( RequestCache RequestSpecificValues ) : base ( RequestSpecificValues )
        {

            RequestSpecificValues.Tracer.Add_Trace("Admin_HtmlSubwriter.Constructor", "Saving values and geting RequestSpecificValues.Current_User object back from the session");

            // All Admin pages require a RequestSpecificValues.Current_User being logged on
            if (RequestSpecificValues.Current_User == null)
			{
                RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.My_Sobek;
                RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Logon;
                RequestSpecificValues.Current_Mode.My_Sobek_SubMode = String.Empty;
                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode);
				return;
			}

            // If the user is not an admin, and admin was selected, reroute this
            if ((!RequestSpecificValues.Current_User.Is_System_Admin) && (!RequestSpecificValues.Current_User.Is_Portal_Admin) && (!RequestSpecificValues.Current_User.Is_User_Admin) && (RequestSpecificValues.Current_Mode.Admin_Type != Admin_Type_Enum.Aggregation_Single))
            {
                RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.My_Sobek;
                RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Home;
                RequestSpecificValues.Current_Mode.My_Sobek_SubMode = String.Empty;
                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode);
                return;
            }

            // Get the appropriate admin viewer from the factory
            adminViewer = AdminViewer_Factory.Get_AdminViewer(RequestSpecificValues);
        }

        #endregion

        /// <summary> Gets the collection of special behaviors which this subwriter
        /// requests from the main HTML writer. </summary>
        /// <remarks> By default, this returns an empty list </remarks>
        public override List<HtmlSubwriter_Behaviors_Enum> Subwriter_Behaviors
        {
            get
            {
                List<HtmlSubwriter_Behaviors_Enum> returnVal = new List<HtmlSubwriter_Behaviors_Enum>();

                returnVal.AddRange(adminViewer.Viewer_Behaviors);

                return returnVal;
            }
        }

        /// <summary> Property indicates if the current mySobek viewer can contain pop-up forms</summary>
        /// <remarks> If the mySobek viewer contains pop-up forms the overall page renders differently, 
        /// allowing for the blanket division and the popup forms near the top of the rendered HTML </remarks>
        public bool Contains_Popup_Forms
        {
            get
            {
                return adminViewer.Contains_Popup_Forms;
            }
        }

		/// <summary> Returns a flag indicating whether the file upload specific holder in the itemNavForm form will be utilized 
		/// for the current request, or if it can be hidden. </summary>
		public override bool Upload_File_Possible
		{
			get
			{
                // If no user, always return false (should not really get here)
				if ((RequestSpecificValues.Current_User == null) || ( !RequestSpecificValues.Current_User.LoggedOn ))
					return false;

                // If no admin viewer was found, also return false
			    if (adminViewer == null)
			        return false;

                // Return the value from the admin viewer
			    return adminViewer.Upload_File_Possible;
			}
		}

		/// <summary> Writes the html to the output stream within the main form, before the ASP.net placeholder for controls </summary>
		/// <param name="Output">Stream to directly write to</param>
		/// <param name="Tracer">Trace object keeps a list of each method executed and important milestones in rendering</param>
		public override void Write_ItemNavForm_Opening(TextWriter Output, Custom_Tracer Tracer)
		{
            Tracer.Add_Trace("Admin_HtmlSubwriter.Write_ItemNavForm_Opening", "Rendering the starting HTML for the admin HTML subwriter");

			// Add any intro html text here
			adminViewer.Write_ItemNavForm_Opening(Output, Tracer);

            Tracer.Add_Trace("Admin_HtmlSubwriter.Write_ItemNavForm_Opening", "Adding any form elements popup divs");
            if ((RequestSpecificValues.Current_Mode.Logon_Required) || (adminViewer.Contains_Popup_Forms))
            {
                adminViewer.Add_Popup_HTML(Output, Tracer);
            }
		}

        /// <summary> Writes the HTML generated by this my sobek html subwriter directly to the response stream </summary>
        /// <param name="Output"> Stream to which to write the HTML for this subwriter </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> Value indicating if html writer should finish the page immediately after this, or if there are other controls or routines which need to be called first </returns>
        public override bool Write_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace("Admin_HtmlSubwriter.Write_HTML", "Rendering HTML");

	       // if (CurrentMode.Admin_Type == Admin_Type_Enum.Wordmarks)
		   //     return false;

            if ((!adminViewer.Contains_Popup_Forms) && (!RequestSpecificValues.Current_Mode.Logon_Required))
            {
                if ((RequestSpecificValues.Current_Mode.Admin_Type != Admin_Type_Enum.Aggregation_Single) && (RequestSpecificValues.Current_Mode.Admin_Type != Admin_Type_Enum.Skins_Single) && (RequestSpecificValues.Current_Mode.Admin_Type != Admin_Type_Enum.Add_Collection_Wizard))
                {
                    // Add the banner
                    if (!adminViewer.Viewer_Behaviors.Contains(HtmlSubwriter_Behaviors_Enum.Suppress_Banner))
                    {
                        Add_Banner(Output, "sbkAhs_BannerDiv", WebPage_Title.Replace("{0} ", ""), RequestSpecificValues.Current_Mode, RequestSpecificValues.HTML_Skin, RequestSpecificValues.Top_Collection);
                    }

                    // Add the RequestSpecificValues.Current_User-specific main menu
                    MainMenus_Helper_HtmlSubWriter.Add_UserSpecific_Main_Menu(Output, RequestSpecificValues );

                    // Start the page container
                    Output.WriteLine("<div id=\"pagecontainer\">");
                    Output.WriteLine("<br />");

                    // Add the box with the title
                    if (((RequestSpecificValues.Current_Mode.My_Sobek_Type != My_Sobek_Type_Enum.Folder_Management) || (RequestSpecificValues.Current_Mode.My_Sobek_SubMode != "submitted items")) && ( RequestSpecificValues.Current_Mode.Admin_Type != Admin_Type_Enum.WebContent_Single ))
                    {
                        // Add the title
                        Output.WriteLine("<div class=\"sbkAdm_TitleDiv sbkAdm_TitleDivBorder\">");
                        if (adminViewer != null)
                        {
                            if (adminViewer.Viewer_Icon.Length > 0)
                            {
                                Output.WriteLine("  <img id=\"sbkAdm_TitleDivImg\" src=\"" + adminViewer.Viewer_Icon + "\" alt=\"\" />");
                            }
                            Output.WriteLine("  <h1>" + adminViewer.Web_Title + "</h1>");
                        }
                        else if (RequestSpecificValues.Current_User != null) Output.WriteLine("  <h1>Welcome back, " + RequestSpecificValues.Current_User.Nickname + "</h1>");
                        Output.WriteLine("</div>");
                        Output.WriteLine();

                        // Add some administrative breadcrumbs here
                        if (adminViewer != null)
                        {
                            // Keep the current values
                            Admin_Type_Enum adminType = RequestSpecificValues.Current_Mode.Admin_Type;
                            ushort page = RequestSpecificValues.Current_Mode.Page.HasValue ? RequestSpecificValues.Current_Mode.Page.Value : ((ushort) 1);
                            string browse_code = RequestSpecificValues.Current_Mode.Info_Browse_Mode;
                            //string aggregation = RequestSpecificValues.Current_Mode.Aggregation;
                            //string mySobekMode = RequestSpecificValues.Current_Mode.My_Sobek_SubMode;

                            // Get the URL for the home page
                            RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Aggregation;
                            RequestSpecificValues.Current_Mode.Aggregation_Type = Aggregation_Type_Enum.Home;
                            RequestSpecificValues.Current_Mode.Home_Type = Home_Type_Enum.List;
                            string home_url = UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode);

                            if (adminViewer is Home_AdminViewer)
                            {
                                // Render the breadcrumbns
                                Output.WriteLine("<div class=\"sbkAdm_Breadcrumbs\">");
                                Output.WriteLine("  <a href=\"" + home_url + "\">" + RequestSpecificValues.Current_Mode.Instance_Abbreviation + " Home</a> > ");
                                Output.WriteLine("  System Administrative Tasks");
                                Output.WriteLine("</div>"); 
                            }
                            else
                            {
                                // Get the URL for the system admin menu
                                RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Administrative;
                                RequestSpecificValues.Current_Mode.Admin_Type = Admin_Type_Enum.Home;
                                string menu_url = UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode);

                                // Restor everything
                                RequestSpecificValues.Current_Mode.Admin_Type = adminType;

                                // Render the breadcrumbns
                                Output.WriteLine("<div class=\"sbkAdm_Breadcrumbs\">");
                                Output.WriteLine("  <a href=\"" + home_url + "\">" + RequestSpecificValues.Current_Mode.Instance_Abbreviation + " Home</a> > ");
                                Output.WriteLine("  <a href=\"" + menu_url + "\">System Administrative Tasks</a> > ");
                                Output.WriteLine("  " + adminViewer.Web_Title);
                                Output.WriteLine("</div>"); 
                            }

                            RequestSpecificValues.Current_Mode.Page = page;
                            RequestSpecificValues.Current_Mode.Info_Browse_Mode = browse_code;
                            
                        }
                    }
                }
            }

            // Add the text here
            adminViewer.Write_HTML(Output, Tracer);
            return false;
        }


		/// <summary> Writes final HTML to the output stream after all the placeholders and just before the itemNavForm is closed.  </summary>
		/// <param name="Output"> Stream to which to write the text for this main writer </param>
		/// <param name="Tracer">Trace object keeps a list of each method executed and important milestones in rendering</param>
		public override void Write_ItemNavForm_Closing(TextWriter Output, Custom_Tracer Tracer)
		{
			Tracer.Add_Trace("Admin_HtmlSubwriter.Write_ItemNavForm_Closing", "");

			// Also, add any additional stuff here
			adminViewer.Write_ItemNavForm_Closing(Output, Tracer);
		}

		/// <summary> Add controls directly to the form in the main control area placeholder</summary>
		/// <param name="MainPlaceHolder"> Main place holder ( &quot;mainPlaceHolder&quot; ) in the itemNavForm form, widely used throughout the application</param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        public void Add_Controls(PlaceHolder MainPlaceHolder, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace("Admin_HtmlSubwriter.Add_Controls", "Build admin viewer and add controls");

            // Add the banner now
            if (((RequestSpecificValues.Current_Mode.Logon_Required) || (adminViewer.Contains_Popup_Forms)) && ( !(adminViewer is Edit_Item_Metadata_MySobekViewer)))
            {
                if (!adminViewer.Viewer_Behaviors.Contains(HtmlSubwriter_Behaviors_Enum.Suppress_Banner))
                {
                    // Start to build the result to write, with the banner
                    StringBuilder header_builder = new StringBuilder();
                    StringWriter header_writer = new StringWriter(header_builder);
                    Add_Banner(header_writer, "sbkAhs_BannerDiv", WebPage_Title.Replace("{0} ", ""), RequestSpecificValues.Current_Mode, RequestSpecificValues.HTML_Skin, RequestSpecificValues.Top_Collection);

                    // Now, add this literal
                    LiteralControl header_literal = new LiteralControl(header_builder.ToString());
                    MainPlaceHolder.Controls.Add(header_literal);
                }
            }

            // Add any controls needed
			adminViewer.Add_Controls(MainPlaceHolder, Tracer);
         }

 
        /// <summary> Title for this web page </summary>
        public override string WebPage_Title
        {
            get { return "{0} System Administration"; }
        }

        /// <summary> Write any additional values within the HTML Head of the
        /// final served page </summary>
        /// <param name="Output"> Output stream currently within the HTML head tags </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <remarks> By default this does nothing, but can be overwritten by all the individual html subwriters </remarks>
        public override void Write_Within_HTML_Head(TextWriter Output, Custom_Tracer Tracer)
        {
            // Admin viewers can override all of this
            StringBuilder builder = new StringBuilder();
            if (adminViewer != null)
            {
                using (StringWriter writer = new StringWriter(builder))
                {
                    bool overrideHead = adminViewer.Write_Within_HTML_Head(writer, Tracer);
                    if (overrideHead)
                    {
                        Output.WriteLine(builder.ToString());
                        return;
                    }
                }
            }


            Output.WriteLine("  <meta name=\"robots\" content=\"index, nofollow\" />");

            Output.WriteLine("  <link href=\"" + Static_Resources_Gateway.Sobekcm_Admin_Css + "\" rel=\"stylesheet\" type=\"text/css\" />");

            // If there was a viewer, add based on behaviors and flags
            if (adminViewer != null)
            {
                // Add the uploader libraries if editing an item
                if (adminViewer.Upload_File_Possible)
                {
                    Output.WriteLine("  <script src=\"" + Static_Resources_Gateway.Jquery_Uploadifive_Js + "\" type=\"text/javascript\"></script>");
                    Output.WriteLine("  <script src=\"" + Static_Resources_Gateway.Jquery_Uploadify_Js + "\" type=\"text/javascript\"></script>");

                    Output.WriteLine("  <link rel=\"stylesheet\" type=\"text/css\" href=\"" + Static_Resources_Gateway.Uploadifive_Css + "\">");
                    Output.WriteLine("  <link rel=\"stylesheet\" type=\"text/css\" href=\"" + Static_Resources_Gateway.Uploadify_Css + "\">");
                }

                if (adminViewer.Viewer_Behaviors.Contains(HtmlSubwriter_Behaviors_Enum.MySobek_Subwriter_Mimic_Item_Subwriter))
                {
                    Output.WriteLine("  <link href=\"" + Static_Resources_Gateway.Sobekcm_Item_Css + "\" rel=\"stylesheet\" type=\"text/css\" />");
                }

                if ((adminViewer.Viewer_Behaviors.Contains(HtmlSubwriter_Behaviors_Enum.Use_Jquery_DataTables)))
                {
                    Output.WriteLine("  <link href=\"" + Static_Resources_Gateway.Sobekcm_Datatables_Css + "\" rel=\"stylesheet\" type=\"text/css\" />");
                    Output.WriteLine("  <script type=\"text/javascript\" src=\"" + Static_Resources_Gateway.Jquery_Datatables_Js + "\" ></script>");
                }

                // Allow the admin viewer to also write into the header
                if (builder.Length > 0)
                {
                    Output.WriteLine(builder.ToString());
                }
            }
        }

        /// <summary> Writes final HTML after all the forms </summary>
        /// <param name="Output">Stream to directly write to</param>
        /// <param name="Tracer">Trace object keeps a list of each method executed and important milestones in rendering</param>
        public override void Write_Final_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            Output.WriteLine("<!-- Close the pagecontainer div -->");
            Output.WriteLine("</div>");
            Output.WriteLine();
        }

		/// <summary> Gets the CSS class of the container that the page is wrapped within </summary>
		public override string Container_CssClass
		{
			get
			{
			    if (adminViewer != null)
			    {
			        string cssStyle = adminViewer.Container_CssClass;
                    if ( !String.IsNullOrEmpty(cssStyle))
                        return cssStyle;
			    }
				
				return base.Container_CssClass;
			}
		}
    }
}
