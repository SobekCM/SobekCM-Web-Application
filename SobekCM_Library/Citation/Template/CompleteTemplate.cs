#region Using directives

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using SobekCM.Core.ApplicationState;
using SobekCM.Core.Configuration;
using SobekCM.Core.Configuration.Localization;
using SobekCM.Core.Navigation;
using SobekCM.Core.UI_Configuration;
using SobekCM.Core.UI_Configuration.StaticResources;
using SobekCM.Core.Users;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Library.Citation.Elements;
using SobekCM.Resource_Object;

#endregion

namespace SobekCM.Library.Citation.Template
{
    /// <summary> CompleteTemplate object stores all the information about an individual metadata template and all 
    /// of the elements contained within the panels and pages of the template </summary>
    [Serializable]
    public class CompleteTemplate
    {
        #region Template_Upload_Types enum

        /// <summary> Enumeration tells the types of uploads this template allows </summary>
        public enum Template_Upload_Types : byte 
        {
            /// <summary> A file must be included with this template </summary>
            File = 1,

            /// <summary> A URL must be included with this template </summary>
            URL,

            /// <summary> A File or URL must be included with this template </summary>
            File_or_URL,

            /// <summary> No upload is required with this template </summary>
            None
        }

        #endregion

        private readonly List<abstract_Element> constants;
        private readonly List<Template_Page> templatePages;

        /// <summary> Options dictionary allows template elements to register certain options or information
        /// which may be used by other template elements </summary>
        private Dictionary<string, string> options;

        #region Constructors

        /// <summary> Constructor for a new instance of the CompleteTemplate class </summary>
        public CompleteTemplate()
        {
            // Set some defaults
            Title = String.Empty;
            Notes = String.Empty;
            Creator = String.Empty;
            Permissions_Agreement = String.Empty;
            Code = String.Empty;
            Banner = String.Empty;
            BibID_Root = String.Empty;
            DateCreated = DateTime.Now;
            LastModified = new DateTime( 1, 1, 1);
            templatePages = new List<Template_Page>();
            constants = new List<abstract_Element>();
            Upload_Types = Template_Upload_Types.File;
            Include_User_As_Author = false;
            Upload_Mandatory = true;
            Default_Visibility = 0;
            Email_Upon_Receipt = String.Empty;
            AdditionalPrompts = new List<string>();

            options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary> Constructor for a new instance of the CompleteTemplate class </summary>
        /// <param name="Name">Name of this template</param>
        public CompleteTemplate( string Name )
        {
            // Set some defaults
            Title = Name;
            Notes = String.Empty;
            Creator = String.Empty;
            Permissions_Agreement = String.Empty;
            Code = String.Empty;
            Banner = String.Empty;
            BibID_Root = String.Empty;
            DateCreated = DateTime.Now;
            LastModified = new DateTime( 1, 1, 1);
            templatePages = new List<Template_Page>();
            constants = new List<abstract_Element>();
            Upload_Types = Template_Upload_Types.File;
            Include_User_As_Author = false;
            Upload_Mandatory = true;
            Default_Visibility = 0;
            Email_Upon_Receipt = String.Empty;
            AdditionalPrompts = new List<string>();

            options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        #endregion

        #region Basic Properties

        /// <summary> Email address which receives an automatic email anytime a new 
        /// item is submitted through this template </summary>
        public string Email_Upon_Receipt { get; set; }

        /// <summary> Value indicates the IP restriction / visibility setting by default for items
        /// being entered through this template </summary>
        public short Default_Visibility { get; set; }

        /// <summary> Root used for the Bibliographic Identifier (BibID) for new items added with this template </summary>
        public string BibID_Root { get; set; }

        /// <summary> Flag indicates if a new item using this template must have an upload of some type </summary>
        public bool Upload_Mandatory { get; set; }

        /// <summary> Flag indicates if the user should be marked as the default creator 
        /// with this template </summary>
        /// <remarks> This is primarily used by Institutional Repositories </remarks>
        public bool Include_User_As_Author { get; set; }

        /// <summary> Indicates the types of uploads that are required with this item </summary>
        public Template_Upload_Types Upload_Types { get; set; }

        /// <summary> Banner which overrides the default banner whenever using this template </summary>
        public string Banner { get; set; }

        /// <summary> Code for this template </summary>
        public string Code { get; set; }

        /// <summary> Current title of this template </summary>
        public string Title { get; set; }

        /// <summary> Notes about the creation and maintenance of this template </summary>
        public string Notes { get; set; }

        /// <summary> Name of the creator of this template </summary>
        public string Creator { get; set; }

        /// <summary> Permissions agreement for this template </summary>
        public string Permissions_Agreement { get; set; }

        /// <summary> Date this template was created </summary>
        public DateTime DateCreated { get; set; }

        /// <summary> Date this template was last modified </summary>
        public DateTime LastModified { get; set; }

        /// <summary> Additional prompts to be shown at the top of the edit form </summary>
        public List<string> AdditionalPrompts { get; set; }
       
        /// <summary> Help URL provided when users are editing with this template </summary>
        public string HelpUrl { get; set; }

        /// <summary> Gets the number of input pages in this template </summary>
        public int InputPages_Count
        {
            get { return templatePages.Count; }
        }

        /// <summary> Read-only collection of <see cref="Template_Page"/> objects for this template </summary>
        public ReadOnlyCollection<Template_Page> InputPages
        {
            get	{	return new ReadOnlyCollection<Template_Page>( templatePages );		}
        }

        /// <summary> Read-only collection of constant elements for this template </summary>
        public ReadOnlyCollection<abstract_Element> Constants
        {
            get { return new ReadOnlyCollection<abstract_Element>( constants ); }
        }

        #endregion

        #region Methods to read and write in the template XML format

        /// <summary> Static method reads a template XML configuraton file and creates the <see cref="CompleteTemplate"/> object  </summary>
        /// <param name="XmlFile"> Filename of the template XML configuraiton file to read </param>
        /// <returns> Fully built template object </returns>
        /// <remarks> This utilizes the <see cref="Template_XML_Reader"/> class to do the actual reading</remarks>
        public static CompleteTemplate Read_XML_Template(string XmlFile)
        {
            return Read_XML_Template(XmlFile, false);
        }

        /// <summary> Static method reads a template XML configuraton file and creates the <see cref="CompleteTemplate"/> object  </summary>
        /// <param name="XmlFile"> Filename of the template XML configuraiton file to read </param>
        /// <param name="ExcludeDivisions"> Flag indicates whether to include the structure map, if included in the template file </param>
        /// <returns> Fully built template object </returns>
        /// <remarks> This utilizes the <see cref="Template_XML_Reader"/> class to do the actual reading</remarks>
        public static CompleteTemplate Read_XML_Template( string XmlFile, bool ExcludeDivisions )
        {
            CompleteTemplate returnValue = new CompleteTemplate();
            Template_XML_Reader reader = new Template_XML_Reader();
            reader.Read_XML( XmlFile, returnValue, ExcludeDivisions );
            returnValue.Build_Final_Adjustment_And_Checks();
            return returnValue;
        }

        /// <summary> Steps through each element after reading the template information and ensures that
        /// any element that must be aware of a comparable element's existence is notified ( such as
        /// creator/contributor ). </summary>
        public void Build_Final_Adjustment_And_Checks()
        {
            // Go through each of the elements and add the options dictionary used for cross-element communication via flags
            foreach (abstract_Element thisElement in templatePages.SelectMany(ThisPage => ThisPage.Panels.SelectMany(ThisPanel => ThisPanel.Elements)))
            {
                thisElement.Options = options;
            }
        }

        #endregion

        #region Methods to save and read to/from a bib package

        /// <summary> Saves the data entered by the user through this template to the provided bibliographic object </summary>
        /// <param name="Bib"> Object into which to save the user-entered data </param>
        /// <param name="Current_User"> Current user, who's rights may impact the way an element is rendered </param>
        public void Save_To_Bib( SobekCM_Item Bib, User_Object Current_User )
        {
            // Go through each of the elements and prepare to save
            foreach (abstract_Element thisElement in templatePages.SelectMany(ThisPage => ThisPage.Panels.SelectMany(ThisPanel => ThisPanel.Elements)))
            {
                thisElement.Prepare_For_Save(Bib, Current_User);
            }

            // Now, step through and save the constants
            foreach (abstract_Element thisElement in constants )
            {
                thisElement.Save_Constant_To_Bib(Bib);
            }

            // Now, step through and save them all
            foreach (abstract_Element thisElement in templatePages.SelectMany(ThisPage => ThisPage.Panels.SelectMany(ThisPanel => ThisPanel.Elements)))
            {
                thisElement.Save_To_Bib(Bib);
            }
        }

        /// <summary> Saves the data entered by the user through one page of this template to the provided bibliographic object </summary>
        /// <param name="Bib"> Object into which to save the user-entered data </param>
        /// <param name="Current_User"> Current user, who's rights may impact the way an element is rendered </param>
        /// <param name="Page">Page number of the template to save</param>
        public void Save_To_Bib(SobekCM_Item Bib, User_Object Current_User, int Page)
        {
            // If this is not a valid page, just return
            if ((Page < 1) || (Page > templatePages.Count))
                return;

            // Get the page
            Template_Page thisPage = templatePages[Page - 1];

            // Go through each of the elements and prepare to save
            foreach (abstract_Element thisElement in thisPage.Panels.SelectMany(ThisPanel => ThisPanel.Elements))
            {
                thisElement.Prepare_For_Save(Bib, Current_User);
            }

            // Now, step through and save the constants
            foreach (abstract_Element thisElement in constants)
            {
                thisElement.Save_Constant_To_Bib(Bib);
            }

            // Now, step through and save them all
            foreach (abstract_Element thisElement in thisPage.Panels.SelectMany(ThisPanel => ThisPanel.Elements))
            {
                thisElement.Save_To_Bib(Bib);
            }
        }

        #endregion

        #region Methods to write this template as HTML

        /// <summary> Displays an item as HTML using this template </summary>
        /// <param name="Output">Text writer to write all of the HTML for this template</param>
        /// <param name="Bib">Bibliographic identifier for the item to display</param>
        /// <param name="Skin_Code">Current base skin code</param>
        /// <param name="isMozilla">Flag indicates if this is Mozilla</param>
        /// <param name="Current_User"> Current user, who's rights may impact the way an element is rendered </param>
        /// <param name="CurrentLanguage"> Current user-interface language </param>
        /// <param name="Translator"> Language support object which handles simple translational duties </param>
        /// <param name="Current_Mode"> Mode / navigation information for the current request</param>
        /// <returns>HTML code for any pop-up forms, which must be placed in a different DIV on the web page</returns>
        public string Render_Template_HTML(TextWriter Output, SobekCM_Item Bib, string Skin_Code, bool isMozilla, User_Object Current_User, Web_Language_Enum CurrentLanguage, Language_Support_Info Translator, Navigation_Object Current_Mode )
        {
            Output.WriteLine("<script src=\"" + Static_Resources_Gateway.Sobekcm_Metadata_Js + "\" type=\"text/javascript\"></script>");
            Output.WriteLine("<a name=\"template\"> </a>");
            // Start to build the return value
            StringBuilder returnValue = new StringBuilder();

            // TEMPORARY CODE TO SUPPORT MULTI-PAGE TEMPLATES IN ONE PAGE (WILL CHANGE)
	        bool multiple_page = templatePages.Count > 1;

	        // Set the current base url on all the elements
            foreach (abstract_Element thisElement in templatePages.SelectMany(ThisPage => ThisPage.Panels.SelectMany(ThisPanel => ThisPanel.Elements)))
            {
                thisElement.Set_Base_URL(Current_Mode.Base_URL);
            }

            // Now, render these pages
            bool first_title = true;
            foreach (Template_Page thisPage in templatePages )
            {
                if (multiple_page)
                {
                    Output.WriteLine("<br /><h1>" + thisPage.Title + "</h1><br />");
                }

                foreach (Template_Panel thisPanel in thisPage.Panels)
                {
                    Output.WriteLine("<table cellpadding=\"4px\" border=\"0px\" cellspacing=\"0px\" class=\"SobekEditItemSection\" >");
                    Output.WriteLine("  <tr align=\"left\">");
                    if (first_title)
                    {
                        Output.WriteLine("    <td colspan=\"3\" class=\"SobekEditItemSectionTitle_first\">&nbsp;" + thisPanel.Title + "</td>");
                        first_title = false;
                    }
                    else
                    {
                        Output.WriteLine("    <td colspan=\"3\" class=\"SobekEditItemSectionTitle\">&nbsp;" + thisPanel.Title + "</td>");
                    }
                    Output.WriteLine("  </tr>");
                    Output.WriteLine();

                    foreach (abstract_Element thisElement in thisPanel.Elements)
                    {
                        thisElement.Render_Template_HTML(Output, Bib, Skin_Code, isMozilla, returnValue, Current_User, CurrentLanguage, Translator, Current_Mode.Base_URL);
                    }

                    Output.WriteLine("</table><br />");
                }
            }
            return returnValue.ToString();
        }

        /// <summary> Displays one page worth of elements from an item as HTML using this template </summary>
        /// <param name="Output">Text writer to write all of the HTML for this template</param>
        /// <param name="Bib">Bibliographic identifier for the item to display</param>
        /// <param name="Skin_Code">Current base skin code</param>
        /// <param name="isMozilla">Flag indicates if this is Mozilla</param>
        /// <param name="Current_User"> Current user, which can dictate how certain elements within this template render</param>
        /// <param name="CurrentLanguage"> Current language of the user interface </param>
        /// <param name="Translator"> Language support object is used to help translate common user interface terms into the current language</param>
        /// <param name="Base_URL"> Base URL for the current request</param>
        /// <param name="page">Page number to display from this template</param>
        /// <returns>HTML code for any pop-up forms, which must be placed in a different DIV on the web page</returns>
        public string Render_Template_HTML(TextWriter Output, SobekCM_Item Bib, string Skin_Code, bool isMozilla, User_Object Current_User, Web_Language_Enum CurrentLanguage, Language_Support_Info Translator, string Base_URL, int page)
        {
            // If this is not a valid page, just return
            if ((page < 1) || (page > templatePages.Count))
                return String.Empty;

            // Start to build the return value
            StringBuilder returnValue = new StringBuilder();

            // Get the page
            Template_Page thisPage = templatePages[page - 1];

            // Set the current base url on all the elements
            foreach (abstract_Element thisElement in templatePages.SelectMany(ThisPage2 => ThisPage2.Panels.SelectMany(ThisPanel => ThisPanel.Elements)))
            {
                thisElement.Set_Base_URL(Base_URL);
            }

            // Now, render these pages
            Output.WriteLine("<!-- Begin to render '" + Title + "' template -->");
			Output.WriteLine("<table class=\"sbkMySobek_TemplateTbl\" cellpadding=\"4px\" >");
            Output.WriteLine();

            bool first_title = true;
            foreach (Template_Panel thisPanel in thisPage.Panels)
            {
                Output.WriteLine("  <!-- '" + thisPanel.Title + "' Panel -->");
                Output.WriteLine("  <tr>");
                if (first_title)
                {
                    Output.WriteLine("    <td colspan=\"3\" class=\"sbkMySobek_TemplateTblTitle_first\">" + thisPanel.Title + "</td>");
                    first_title = false;
                }
                else
                {
					Output.WriteLine("    <td colspan=\"3\" class=\"sbkMySobek_TemplateTblTitle\">" + thisPanel.Title + "</td>");
                }
                Output.WriteLine("  </tr>");
                Output.WriteLine();

                foreach (abstract_Element thisElement in thisPanel.Elements)
                {
                    thisElement.Render_Template_HTML(Output, Bib, Skin_Code, isMozilla, returnValue, Current_User, CurrentLanguage, Translator, Base_URL);
                }
            }

            Output.WriteLine("</table>");
            return returnValue.ToString();
        }

        #endregion

        /// <summary> Adds a new template page to the collection of pages contained within this template </summary>
        /// <param name="NewPage"> New template page to add </param>
        internal void Add_Page(Template_Page NewPage)
        {
            templatePages.Add(NewPage);
        }

        /// <summary> Adds a new constant to the collection of constants contained within this template </summary>
        /// <param name="NewConstant"> New constant to add </param>
        internal void Add_Constant(abstract_Element NewConstant)
        {
            constants.Add(NewConstant);
        }
    }
}
