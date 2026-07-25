#region Using directives

using SobekCM.Core.ApplicationState;
using SobekCM.Core.Configuration.Localization;
using SobekCM.Core.Users;
using SobekCM.Library.UI;
using SobekCM.Resource_Object;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

#endregion

namespace SobekCM.Library.Citation.Elements
{
    /// <summary> Element allows entry of the wordmarks/icons for an item </summary>
    /// <remarks> This class extends the <see cref="MultipleTextBox_Element"/> class. </remarks>
    public class Wordmark_Element : MultipleTextBox_Element
    {
        /// <summary> Constructor for a new instance of the Wordmark_Element class </summary>
        public Wordmark_Element()
            : base("Wordmark", "wordmark")
        {
            Repeatable = true;
            ViewChoicesString = String.Empty;

            MaxBoxes = 5;
            BoxesPerLine = 5;
        }

        /// <summary> Sets the base url for the current request </summary>
        /// <param name="Base_URL"> Current Base URL for this request </param>
        public override void Set_Base_URL(string Base_URL)
        {
            ViewChoicesString = "<a href=\"" + Base_URL + "l/internal/wordmarks<%?URLOPTS%>\" title=\"View all wordmarks\" target=\"_WORDMARKLIST\"><img src=\"" + Base_URL + "design/skins/<%WEBSKIN%>/buttons/magnify.jpg\" /></a>";
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
                const string defaultAcronym = "Enter the code for any icons or wordmarks which should appear beside this item when viewed on the web.";
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

            if (Bib.Behaviors.Wordmark_Count == 0)
            {
                render_helper(Output, String.Empty, Skin_Code, Current_User, CurrentLanguage, Translator, Base_URL);
                return;
            }

            if (Bib.Behaviors.Wordmark_Count == 1)
            {
                render_helper(Output, Bib.Behaviors.Wordmarks[0].Code, Skin_Code, Current_User, CurrentLanguage, Translator, Base_URL);
                return;
            }

            var allWordmarks = new List<string>();
            if (Bib.Behaviors.Wordmark_Count > 0)
            {
                allWordmarks.AddRange(Bib.Behaviors.Wordmarks.Select(ThisIcon => ThisIcon.Code));
            }
            render_helper(Output, new ReadOnlyCollection<string>(allWordmarks), Skin_Code, Current_User, CurrentLanguage, Translator, Base_URL);
        }

        /// <summary> Prepares the bib object for the save, by clearing any existing data in this element's related field(s) </summary>
        /// <param name="Bib"> Existing digital resource object which may already have values for this element's data field(s) </param>
        /// <param name="Current_User"> Current user, who's rights may impact the way an element is rendered </param>
        /// <remarks> This clears any preexisting wordmarks </remarks>
        public override void Prepare_For_Save(SobekCM_Item Bib, User_Object Current_User)
        {
            Bib.Behaviors.Clear_Wordmarks();
        }

        /// <summary> Saves the data rendered by this element to the provided bibliographic object during postback </summary>
        /// <param name="Bib"> Object into which to save the user's data, entered into the html rendered by this element </param>
        public override void Save_To_Bib(SobekCM_Item Bib)
        {
            var getKeys = Context.Request.Form.Keys;
            foreach (string thisKey in getKeys)
            {
                if (thisKey.IndexOf(html_element_name.Replace("_", "")) != 0) continue;

                string code = Context.Request.Form[thisKey].TrimFirst().ToLower();
                bool found = false;
                if (Bib.Behaviors.Wordmark_Count > 0)
                {
                    if (Bib.Behaviors.Wordmarks.Any(thisIcon => thisIcon.Code.ToLower() == code))
                    {
                        found = true;
                    }
                }
                if (!found)
                {
                    Bib.Behaviors.Add_Wordmark(code);
                }
            }
        }

        /// <summary> Saves the constants to the bib id </summary>
        /// <param name="Bib"> Object into which to save this element's constant data </param>
        public override void Save_Constant_To_Bib(SobekCM_Item Bib)
        {
            if (DefaultValues.Count > 0)
            {
                string[] splits = DefaultValues[0].Split(',');
                foreach (string split in splits)
                {
                    string code = split.Trim().ToLower();

                    bool found = false;
                    if (Bib.Behaviors.Wordmark_Count > 0)
                    {
                        if (Bib.Behaviors.Wordmarks.Any(thisIcon => thisIcon.Code.ToLower() == code))
                        {
                            found = true;
                        }
                    }
                    if (!found)
                    {
                        Bib.Behaviors.Add_Wordmark(code);
                    }
                }
            }
        }
    }
}