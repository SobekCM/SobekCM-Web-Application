#region Using directives

using SobekCM.Core.ApplicationState;
using SobekCM.Core.Configuration.Localization;
using SobekCM.Core.Users;
using SobekCM.Library.UI;
using SobekCM.Resource_Object;
using System;
using System.IO;
using System.Text;

#endregion

namespace SobekCM.Library.Citation.Elements
{
    /// <summary> Element allows an administrator to flag an item as needing its whole file folder kept and
    /// served from local disk, even under the GCS Hybrid / GCS Full file system modes </summary>
    /// <remarks> This class extends the <see cref="CheckBox_Element"/> class.  Only rendered for system-level
    /// administrators.  The value lives only in the database (SobekCM_Item.Serve_Files_Locally), so unlike most
    /// elements this one does not save anything itself -- <c>Edit_Item_Behaviors_MySobekViewer</c> reads the
    /// hidden <see cref="PRESENT_FIELD_NAME"/> marker and the checkbox out of the postback and writes the
    /// database directly, after checking the user's rights again. </remarks>
    public class Serve_Files_Locally_Element : CheckBox_Element
    {
        /// <summary> Name of the hidden form field that says this element was actually rendered (only
        /// administrators see it), so an unrendered checkbox is never mistaken for an unchecked one </summary>
        public const string PRESENT_FIELD_NAME = "serveFilesLocallyPresent";

        /// <summary> Name of the checkbox form field </summary>
        public const string CHECKBOX_FIELD_NAME = "serveFilesLocally";

        /// <summary> Constructor for a new instance of the Serve_Files_Locally_Element class  </summary>
        public Serve_Files_Locally_Element() : base("Serve Files Locally", CHECKBOX_FIELD_NAME, "Keep this item's files on local disk (for a web site, HTML file with its own images, or open textbook)")
        {
            DefaultValue = false;
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
        /// <remarks> Renders nothing at all unless the user is a system-level administrator </remarks>
        public override void Render_Template_HTML(TextWriter Output, SobekCM_Item Bib, string Skin_Code, bool IsMozilla, StringBuilder PopupFormBuilder, User_Object Current_User, string CurrentLanguage, Language_Support_Info Translator, string Base_URL)
        {
            if (!Can_Set_Flag(Current_User))
                return;

            Output.WriteLine("  <input type=\"hidden\" name=\"" + PRESENT_FIELD_NAME + "\" value=\"1\" />");
            render_helper(Output, Bib.Behaviors.Serve_Files_Locally, Skin_Code, Current_User, CurrentLanguage, Translator, Base_URL);
        }

        /// <summary> Prepares the bib object for the save </summary>
        /// <param name="Bib"> Existing digital resource object </param>
        /// <param name="Current_User"> Current user, who's rights may impact the way an element is rendered </param>
        /// <remarks> Does nothing.  Unlike most checkbox elements, this must NOT reset the flag here -- the
        /// element is not rendered for most users, and their saves must leave the value alone. </remarks>
        public override void Prepare_For_Save(SobekCM_Item Bib, User_Object Current_User)
        {
            // Intentionally empty
        }

        /// <summary> Saves the data rendered by this element to the provided bibliographic object during postback </summary>
        /// <param name="Bib"> Object into which to save the user's data, entered into the html rendered by this element </param>
        /// <remarks> Only updates the in-memory flag, and only if the element was rendered for this postback </remarks>
        public override void Save_To_Bib(SobekCM_Item Bib)
        {
            if (!String.IsNullOrEmpty(Context.Request.Form[PRESENT_FIELD_NAME].TrimFirst()))
                Bib.Behaviors.Serve_Files_Locally = !String.IsNullOrEmpty(Context.Request.Form[html_element_name].TrimFirst());
        }

        /// <summary> Checks if the given user is allowed to see and change this flag </summary>
        /// <param name="Current_User"> User to check </param>
        /// <returns> TRUE if the user is a host, system or portal administrator </returns>
        public static bool Can_Set_Flag(User_Object Current_User)
        {
            return (Current_User != null) && (Current_User.LoggedOn) && ((Current_User.Is_System_Admin) || (Current_User.Is_Portal_Admin) || (Current_User.Is_Host_Admin));
        }
    }
}
