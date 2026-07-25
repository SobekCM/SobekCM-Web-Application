#region Using directives

using SobekCM.Core.ApplicationState;
using SobekCM.Core.Configuration.Localization;
using SobekCM.Core.Users;
using SobekCM.Resource_Object;
using System.Collections.Generic;
using System.IO;
using System.Text;

using SobekCM.Library.UI;
#endregion

namespace SobekCM.Library.Citation.Elements
{
    /// <summary> Element allows entry of the primary alternate identifier ( identifier and identifier type) associated with an item group </summary>
    /// <remarks> This class extends the <see cref="TextBox_TextBox_Element"/> class. </remarks>
    public class Primary_Alt_Identifier_Element : TextBox_TextBox_Element
    {
        /// <summary> Constructor for a new instance of the Primary_Alt_Identifier_Element class </summary>
        public Primary_Alt_Identifier_Element() : base("Primary Identifier", "primid")
        {
            SecondLabel = "Identifier Type";
            Repeatable = false;
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
        public override void Render_Template_HTML(TextWriter Output, SobekCM_Item Bib, string Skin_Code, bool IsMozilla, StringBuilder PopupFormBuilder, User_Object Current_User, string CurrentLanguage, Language_Support_Info Translator, string Base_URL)
        {
            // Check that an acronym exists
            if (Acronym.Length == 0)
            {
                const string defaultAcronym = "Primary alternate identifier associated with this item group.  This may range from a locally defined identifier to an identifier established by a standard committe.";
                switch (CurrentLanguage)
                {
                    case "en":
                        Acronym = defaultAcronym;
                        break;

                    case "es":
                        Acronym = defaultAcronym;
                        break;

                    case "fr":
                        Acronym = defaultAcronym;
                        break;

                    default:
                        Acronym = defaultAcronym;
                        break;
                }
            }

            // NOTE: This part isn't optimized for this element, but rather kept as similar to the
            // standard identifier class as possible to support any later changes.
            var terms = new List<string>();
            var schemes = new List<string>();
            terms.Add(Bib.Behaviors.Primary_Identifier.Identifier);
            schemes.Add(Bib.Behaviors.Primary_Identifier.Type);

            render_helper(Output, terms, schemes, Skin_Code, Current_User, CurrentLanguage, Translator, Base_URL);
        }

        /// <summary> Prepares the bib object for the save, by clearing any existing data in this element's related field(s) </summary>
        /// <param name="Bib"> Existing digital resource object which may already have values for this element's data field(s) </param>
        /// <param name="Current_User"> Current user, who's rights may impact the way an element is rendered </param>
        /// <remarks> This does nothing since there is only one primary alternate id per item group </remarks>
        public override void Prepare_For_Save(SobekCM_Item Bib, User_Object Current_User)
        {
            // Do nothing
        }

        /// <summary> Saves the data rendered by this element to the provided bibliographic object during postback </summary>
        /// <param name="Bib"> Object into which to save the user's data, entered into the html rendered by this element </param>
        public override void Save_To_Bib(SobekCM_Item Bib)
        {
            // NOTE: This part isn't optimized for this element, but rather kept as similar to the
            // standard identifier class as possible to support any later changes.

            var terms = new Dictionary<string, string>();
            var schemes = new Dictionary<string, string>();

            var getKeys = Context.Request.Form.Keys;
            foreach (string thisKey in getKeys)
            {
                if (thisKey.IndexOf(html_element_name.Replace("_", "") + "_first") == 0)
                {
                    string term = Context.Request.Form[thisKey];
                    string index = thisKey.Replace(html_element_name.Replace("_", "") + "_first", "");
                    terms[index] = term;
                }

                if (thisKey.IndexOf(html_element_name.Replace("_", "") + "_second") == 0)
                {
                    string scheme = Context.Request.Form[thisKey];
                    string index = thisKey.Replace(html_element_name.Replace("_", "") + "_second", "");
                    schemes[index] = scheme;
                }
            }

            foreach (string index in terms.Keys)
            {
                Bib.Behaviors.Set_Primary_Identifier(schemes.ContainsKey(index) ? schemes[index] : "Primary Identifier", terms[index]);
            }
        }
    }
}
