﻿#region Using directives

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using SobekCM.Core.ApplicationState;
using SobekCM.Core.Configuration;
using SobekCM.Core.Configuration.Localization;
using SobekCM.Core.UI_Configuration;
using SobekCM.Core.UI_Configuration.StaticResources;
using SobekCM.Core.Users;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Resource_Object;

#endregion

namespace SobekCM.Library.Citation.Elements
{
    /// <summary> Element allows IR-specific entry of the rights for an item </summary>
    /// <remarks> This class extends the <see cref="TextArea_Element"/> class. </remarks>
    public class IR_Rights_Element : TextArea_Element
    {
        private string baseURL;

        private const string DEFAULT_PREFIX = "Permissions granted to the University of Florida Institutional Repository and University of Florida Digital Collections to allow use by the submitter.";

        /// <summary> Constructor for a new instance of the IR_Rights_Element class </summary>
        public IR_Rights_Element()
            : base("Rights Management", "rights_mgmt")
        {
            Repeatable = false;
            Rows = 5;
            baseURL = String.Empty;
	        help_page = "typeir";
        }

        /// <summary> Sets the base url for the current request </summary>
        /// <param name="Base_URL"> Current Base URL for this request </param>
        public override void Set_Base_URL(string Base_URL)
        {
            baseURL = Base_URL;
        }

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
        /// <remarks> This simple element does not append any popup form to the popup_form_builder</remarks>
        public override void Render_Template_HTML(TextWriter Output, SobekCM_Item Bib, string Skin_Code, bool IsMozilla, StringBuilder PopupFormBuilder, User_Object Current_User, Web_Language_Enum CurrentLanguage, Language_Support_Info Translator, string Base_URL )
        {
            // Check that an acronym exists
            if (Acronym.Length == 0)
            {
                const string defaultAcronym = "Enter the rights you give for sharing, repurposing, or remixing your item to other users.  You may also select a creative commons license below.";
                switch (CurrentLanguage)
                {
                    case Web_Language_Enum.English:
                        Acronym = defaultAcronym;
                        break;

                    case Web_Language_Enum.Spanish:
                        Acronym = defaultAcronym;
                        break;

                    case Web_Language_Enum.French:
                        Acronym = defaultAcronym;
                        break;

                    default:
                        Acronym = defaultAcronym;
                        break;
                }
            }

            string id_name = html_element_name.Replace("_", "");

            int actual_cols = Cols;
            if (IsMozilla)
                actual_cols = ColsMozilla;

            Output.WriteLine("  <!-- " + Title + " Element -->");
            Output.WriteLine("  <tr align=\"left\">");
            Output.WriteLine("    <td width=\"" + LEFT_MARGIN + "px\">&nbsp;</td>");
            if (Read_Only)
            {
                Output.WriteLine("    <td valign=\"top\" class=\"metadata_label\">" + Title + ":</b></td>");
            }
            else
            {
                if (Acronym.Length > 0)
                {
                    Output.WriteLine("    <td valign=\"top\" class=\"metadata_label\"><a href=\"" + Help_URL(Skin_Code, Base_URL) + "\" target=\"_" + html_element_name.ToUpper() + "\"><acronym title=\"" + Acronym + "\">" + Title + ":</acronym></a></td>");
                }
                else
                {
                    Output.WriteLine("    <td valign=\"top\" class=\"metadata_label\"><a href=\"" + Help_URL(Skin_Code, Base_URL) + "\" target=\"_" + html_element_name.ToUpper() + "\">" + Title + ":</a></td>");
                }
            }
            Output.WriteLine("    <td>");

            Output.WriteLine("      <div id=\"" + html_element_name + "_div\">");
            Output.WriteLine("        <textarea rows=\"" + Rows + "\" cols=\"" + actual_cols + "\" name=\"" + id_name + "1\" id=\"" + id_name + "1\" class=\"" + html_element_name + "_input\" onfocus=\"javascript:textbox_enter('" + id_name + "1','" + html_element_name + "_input_focused')\" onblur=\"javascript:textbox_leave('" + id_name + "1','" + html_element_name + "_input')\">" + HttpUtility.HtmlEncode(Bib.Bib_Info.Access_Condition.Text.Replace(DEFAULT_PREFIX, "").Trim()) + "</textarea>");
            Output.WriteLine("        <div class=\"ShowOptionsRow\">");
			Output.WriteLine("                <ul class=\"sbk_FauxDownwardTabsList\">");
			Output.WriteLine("                  <li><a href=\"\" onclick=\"return open_cc_rights();\">CREATIVE COMMONS</a></li>");
			Output.WriteLine("                </ul>");
            Output.WriteLine("        </div>");
            Output.WriteLine("      </div>");

            Output.WriteLine("    </td>");
            Output.WriteLine("  </tr>");
            Output.WriteLine();


            Output.WriteLine("  <tr align=\"left\">");
            Output.WriteLine("    <td colspan=\"2\">&nbsp;</td>");
            Output.WriteLine("    <td>");
            Output.WriteLine("      <table id=\"cc_rights\" cellpadding=\"3px\" cellspacing=\"3px\" style=\"display:none;\">");
            Output.WriteLine("        <tr><td colspan=\"2\">You may also select a <a title=\"Explanation of different creative commons licenses.\" href=\"http://creativecommons.org/about/licenses/\">Creative Commons License</a> option below.<br /></td></tr>");
            Output.WriteLine("        <tr><td> &nbsp; <a href=\"\" onclick=\"return set_cc_rights('rightsmgmt1','[cc0] The author dedicated the work to the Commons by waiving all of his or her rights to the work worldwide under copyright law and all related or neighboring legal rights he or she had in the work, to the extent allowable by law.');\"><img title=\"You dedicate the work to the Commons by waiving all of your rights to the work worldwide under copyright law and all related or neighboring legal rights you had in the work, to the extent allowable by law.\" src=\"" + Static_Resources_Gateway.Cc_Zero_Img + "\" /></a></td><td><b>No Copyright</b><br /><i>cc0</i></td></tr>");
            Output.WriteLine("        <tr><td> &nbsp; <a href=\"\" onclick=\"return set_cc_rights('rightsmgmt1','[cc by] This item is licensed with the Creative Commons Attribution License.  This license lets others distribute, remix, tweak, and build upon this work, even commercially, as long as they credit the author for the original creation.');\"><img title=\"This license lets others distribute, remix, tweak, and build upon your work, even commercially, as long as they credit you for the original creation.\" src=\"" + Static_Resources_Gateway.Cc_By_Img + "\" /></a></td><td><b>Attribution</b><br /><i>cc by</i></td></tr>");
            Output.WriteLine("        <tr><td> &nbsp; <a href=\"\" onclick=\"return set_cc_rights('rightsmgmt1','[cc by-sa] This item is licensed with the Creative Commons Attribution Share Alike License.  This license lets others remix, tweak, and build upon this work even for commercial reasons, as long as they credit the author and license their new creations under the identical terms.');\"><img title=\"This license lets others remix, tweak, and build upon your work even for commercial reasons, as long as they credit you and license their new creations under the identical terms.\" src=\"" + Static_Resources_Gateway.Cc_By_Sa_Img + "\" /></a></td><td><b>Attribution Share Alike</b><br /><i>cc by-sa</i></td></tr>");
            Output.WriteLine("        <tr><td> &nbsp; <a href=\"\" onclick=\"return set_cc_rights('rightsmgmt1','[cc by-nd] This item is licensed with the Creative Commons Attribution No Derivatives License.  This license allows for redistribution, commercial and non-commercial, as long as it is passed along unchanged and in whole, with credit to the author.');\"><img title=\"This license allows for redistribution, commercial and non-commercial, as long as it is passed along unchanged and in whole, with credit to you.\" src=\"" + Static_Resources_Gateway.Cc_By_Nd_Img + "\" /></a></td><td><b>Attribution No Derivatives</b><br /><i>cc by-nd</i></td></tr>");
            Output.WriteLine("        <tr><td> &nbsp; <a href=\"\" onclick=\"return set_cc_rights('rightsmgmt1','[cc by-nc] This item is licensed with the Creative Commons Attribution Non-Commerical License.  This license lets others remix, tweak, and build upon this work non-commercially, and although their new works must also acknowledge the author and be non-commercial, they don’t have to license their derivative works on the same terms.');\"><img title=\"This license lets others remix, tweak, and build upon your work non-commercially, and although their new works must also acknowledge you and be non-commercial, they don’t have to license their derivative works on the same terms.\" src=\"" + Static_Resources_Gateway.Cc_By_Nc_Img + "\" /></a></td><td><b>Attribution Non-Commercial</b><br /><i>cc by-nc</i></td></tr>");
            Output.WriteLine("        <tr><td> &nbsp; <a href=\"\" onclick=\"return set_cc_rights('rightsmgmt1','[cc by-nc-sa] This item is licensed with the Creative Commons Attribution Non-Commercial Share Alike License.  This license lets others remix, tweak, and build upon this work non-commercially, as long as they credit the author and license their new creations under the identical terms.');\"><img title=\"This license lets others remix, tweak, and build upon your work non-commercially, as long as they credit you and license their new creations under the identical terms.\" src=\"" + Static_Resources_Gateway.Cc_By_Nc_Sa_Img + "\" /></a></td><td><b>Attribution Non-Commercial Share Alike</b><br /><i>cc by-nc-sa</i></td></tr>");
            Output.WriteLine("        <tr><td> &nbsp; <a href=\"\" onclick=\"return set_cc_rights('rightsmgmt1','[cc by-nc-nd] This item is licensed with the Creative Commons Attribution Non-Commercial No Derivative License.  This license allows others to download this work and share them with others as long as they mention the author and link back to the author, but they can’t change them in any way or use them commercially.');\"><img title=\"This license allows others to download your works and share them with others as long as they mention you and link back to you, but they can’t change them in any way or use them commercially.\" src=\"" + Static_Resources_Gateway.Cc_By_Nc_Nd_Img + "\" /></a></td><td><b>Attribution Non-Commercial No Derivatives</b><br /><i>cc by-nc-nd</i></td></tr>");
            Output.WriteLine("      </table>");
            Output.WriteLine("    </td>");
            Output.WriteLine("  </tr>");
            Output.WriteLine("");
        }

        /// <summary> Prepares the bib object for the save, by clearing any existing data in this element's related field(s) </summary>
        /// <param name="Bib"> Existing digital resource object which may already have values for this element's data field(s) </param>
        /// <param name="Current_User"> Current user, who's rights may impact the way an element is rendered </param>
        /// <remarks> Clears existing rights statements </remarks>
        public override void Prepare_For_Save(SobekCM_Item Bib, User_Object Current_User)
        {
            Bib.Bib_Info.Clear_AccessConditions();
        }

        /// <summary> Saves the data rendered by this element to the provided bibliographic object during postback </summary>
        /// <param name="Bib"> Object into which to save the user's data, entered into the html rendered by this element </param>
        public override void Save_To_Bib(SobekCM_Item Bib)
        {
            string[] getKeys = HttpContext.Current.Request.Form.AllKeys;
            foreach (string thisKey in getKeys.Where(thisKey => thisKey.IndexOf(html_element_name.Replace("_", "")) == 0))
            {
                Bib.Bib_Info.Add_AccessCondition(DEFAULT_PREFIX + " " + HttpContext.Current.Request.Form[thisKey]);
                return;
            }
        }
    }
}
