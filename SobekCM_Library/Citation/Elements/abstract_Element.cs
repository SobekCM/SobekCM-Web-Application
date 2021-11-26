#region Using directives

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using SobekCM.Core.ApplicationState;
using SobekCM.Core.Configuration;
using SobekCM.Core.Configuration.Localization;
using SobekCM.Core.UI_Configuration;
using SobekCM.Core.UI_Configuration.StaticResources;
using SobekCM.Core.Users;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Library.UI;
using SobekCM.Resource_Object;

#endregion

namespace SobekCM.Library.Citation.Elements
{
    /// <summary> Abstract class which all html metadata element classes must extend </summary>
    /// <remarks> This implements the <see cref="iElement"/> interface. </remarks>
    public abstract class abstract_Element : iElement
    {
        /// <summary> Constant defines the url for the repeat button image for all metadata element objects </summary>
        protected string REPEAT_BUTTON_URL = Static_Resources_Gateway.New_Element_Jpg;

        /// <summary> Constant defines the url for the repeat button image for all metadata element objects </summary>
        protected string HELP_BUTTON_URL = Static_Resources_Gateway.Help_Button_Jpg;

        /// <summary> Constant defines the left margin for all metadata element objects </summary>
        /// <value>Current constant value is 15</value>
        protected const int LEFT_MARGIN = 15;

        /// <summary> Constant defines the number of columns to use for most browsers for all text area html objects in metadata elements </summary>
        protected const int TEXT_AREA_COLUMNS = 82;

        /// <summary> Constant defines the number of columns to use for Mozilla browsers for all text area html objects in metadata elements </summary>
        protected const int MOZILLA_TEXT_AREA_COLUMNS = 77;

        /// <summary> Name used for style sheet references and html id's for this subtype of this element </summary>
        protected string html_element_name;

		/// <summary> Name of the help page, if different than default </summary>
	    protected string help_page;

        /// <summary> Constructor for a new abstract element object </summary>
        protected abstract_Element()
        {
            // Set other default values
            Repeatable = false;
            Mandatory = false;
            isConstant = false;
            Read_Only = false;
            Acronym = String.Empty;
            Title = String.Empty;
            Template_Page = -1;
        }

        /// <summary> Sets the base url for the current request </summary>
        /// <param name="Base_URL"> Current Base URL for this request </param>
        /// <remarks> This does nothing, although it can be override by extending classes </remarks>
        public virtual void  Set_Base_URL( string Base_URL )
        {
            // Do nothing 
        }

        /// <summary> Page within the template that this element appears </summary>
        public int Template_Page { get; set; }

        /// <summary> Acronym help for this element which appears when the cursor hovers over the element name </summary>
        public string Acronym { get; set; }

        /// <summary> Gets and sets the flag indicating this is in readonly format, and no changes are allowed </summary>
        public bool Read_Only { get; set; }

        #region iElement Members

        /// <summary> Current title of this element </summary>
        public string Title { get; set; }

        /// <summary> Flag indicates if this is being used as a constant field, or if data can be entered by the user </summary>
        public bool isConstant { get; set; }

        /// <summary> Flag indicating this allows repeatability </summary>
        public bool Repeatable { get; set; }

        /// <summary> Flag indicating this is mandatory </summary>
        public bool Mandatory { get; set; }

        /// <summary> Options dictionary allows template elements to register certain options or information
        /// which may be used by other template elements </summary>
        public virtual Dictionary<string, string> Options { get; set; }

        /// <summary> Reads from the template XML format </summary>
        /// <param name="xmlReader"> Current template xml configuration reader </param>
        public void Read_XML( XmlReader xmlReader )
        {
            Inner_Read_Data( xmlReader );
        }

        /// <summary> Saves the constants to the bib id </summary>
        /// <param name="Bib"> Object into which to save this element's constant data </param>
        public virtual void Save_Constant_To_Bib(SobekCM_Item Bib)
        {
            // Do nothing by default
        }

        #endregion

        #region Abstract Methods and Properties

        /// <summary> Prepares the bib object for the save, by clearing any existing data in this element's related field(s) </summary>
        /// <param name="Bib"> Existing digital resource object which may already have values for this element's data field(s) </param>
        /// <param name="Current_User"> Current user, who's rights may impact the way an element is rendered </param>
        public abstract void Prepare_For_Save(SobekCM_Item Bib, User_Object Current_User);

        /// <summary> Saves the data rendered by this element to the provided bibliographic object during postback </summary>
        /// <param name="Bib"> Object into which to save the user's data, entered into the html rendered by this element </param>
        public abstract void Save_To_Bib( SobekCM_Item Bib );

        /// <summary> Renders the HTML for this element </summary>
        /// <param name="Output"> Textwriter to write the HTML for this element </param>
        /// <param name="Bib"> Object to populate this element from </param>
        /// <param name="Skin_Code"> Code for the current skin </param>
        /// <param name="IsMozilla"> Flag indicates if the current browse is Mozilla Firefox (different css choices for some elements)</param>
        /// <param name="PopupFormBuilder"> Builder for any related popup forms for this element </param>
        /// <param name="Current_User"> Current user, who's rights may impact the way an element is rendered </param>
        /// <param name="CurrentLanguage"> Current user-interface language </param>
        /// <param name="Translator"> Language support object which handles simple translational duties </param>
        /// <param name="Base_URL"> Base URL for the current request </param>
        public abstract void Render_Template_HTML(TextWriter Output, SobekCM_Item Bib, string Skin_Code, bool IsMozilla, StringBuilder PopupFormBuilder, User_Object Current_User, Web_Language_Enum CurrentLanguage, Language_Support_Info Translator, string Base_URL );

        #endregion

        /// <summary> Return the HTML for the close button image for a given html interface </summary>
        /// <param name="Skin_Code"> Code for the current html skin </param>
        /// <param name="Button_Base_URL"> Base URL for the current request </param>
        /// <returns> HTML for the close button image to display </returns>
        protected string Close_Button_URL(string Skin_Code, string Button_Base_URL )
        {
            return Button_Base_URL + "design/skins/" + Skin_Code + "/buttons/close_button_g.gif";
        }

        /// <summary> Returns the URL for the element help for a given html interface </summary>
        /// <param name="Skin_Code"> Code for the current html skin </param>
        /// <param name="Current_Base_URL"> Base URL for the current request </param>
        /// <returns> HTML for the URL for the element help </returns>
        protected string Help_URL(string Skin_Code, string Current_Base_URL)
        {
	        if ( String.IsNullOrEmpty(help_page))
	            return UI_ApplicationCache_Gateway.Settings.System.Metadata_Help_URL(Current_Base_URL) + "help/" + html_element_name.Replace("_", "");
	        return UI_ApplicationCache_Gateway.Settings.System.Metadata_Help_URL(Current_Base_URL) + "help/" + help_page;
        }

		/// <summary> Returns TRUE if this element has some data </summary>
		/// <value> By default, this returns TRUE to be on the same side </value>
		public bool HasData { get { return true; } }

	    #region Abstract Methods to be implemented by abstract_Element classes

        /// <summary> Reads the inner data from the CompleteTemplate XML format </summary>
        /// <param name="XMLReader"> Current template xml configuration reader </param>
        protected abstract void Inner_Read_Data( XmlReader XMLReader );

        #endregion
    }
}
