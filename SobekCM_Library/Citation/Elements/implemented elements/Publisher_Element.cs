﻿#region Using directives

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using SobekCM.Core.ApplicationState;
using SobekCM.Core.Configuration;
using SobekCM.Core.Configuration.Localization;
using SobekCM.Core.Users;
using SobekCM.Resource_Object;

#endregion

namespace SobekCM.Library.Citation.Elements
{
    /// <summary> Element allows simple entry of the publisher's name for an item </summary>
    /// <remarks> This class extends the <see cref="SimpleTextBox_Element"/> class. </remarks>
    public class Publisher_Element : SimpleTextBox_Element
    {
        /// <summary> Constructor for a new instance of the Publisher_Element class </summary>
        public Publisher_Element() : base("Publisher", "publisher")
        {
            Repeatable = true;
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
                const string defaultAcronym = "Enter the name(s) of the publisher(s) of the larger body of work. If your work is currently unpublished, you may enter your name as the publisher or leave the field blank. If you are adding administrative material (newsletters, handbooks, etc.) on behalf of a department within the university, enter the name of your department as the publisher.";
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

            List<string> instanceValues = new List<string>();
            if (Bib.Bib_Info.Publishers_Count > 0)
            {
                instanceValues.AddRange(Bib.Bib_Info.Publishers.Select(thisName => thisName.Name));
            }

            render_helper(Output, instanceValues, Skin_Code, Current_User, CurrentLanguage, Translator, Base_URL);
        }

        /// <summary> Prepares the bib object for the save, by clearing any existing data in this element's related field(s) </summary>
        /// <param name="Bib"> Existing digital resource object which may already have values for this element's data field(s) </param>
        /// <param name="Current_User"> Current user, who's rights may impact the way an element is rendered </param>
        /// <remarks> This clears any preexisting publishers </remarks>
        public override void Prepare_For_Save(SobekCM_Item Bib, User_Object Current_User)
        {
            Bib.Bib_Info.Clear_Publishers();
        }

        /// <summary> Saves the data rendered by this element to the provided bibliographic object during postback </summary>
        /// <param name="Bib"> Object into which to save the user's data, entered into the html rendered by this element </param>
        public override void Save_To_Bib(SobekCM_Item Bib)
        {
            string[] getKeys = HttpContext.Current.Request.Form.AllKeys;
            foreach (string name in from thisKey in getKeys where thisKey.IndexOf(html_element_name) == 0 select HttpContext.Current.Request.Form[thisKey] into name where name.Trim().Length > 0 select name)
            {
                Bib.Bib_Info.Add_Publisher(name);
            }
        }

        /// <summary> Saves the constants to the bib id </summary>
        /// <param name="Bib"> Object into which to save this element's constant data </param>
        public override void Save_Constant_To_Bib(SobekCM_Item Bib)
        {
            if (DefaultValues.Count > 0)
            {
                Bib.Bib_Info.Clear_Publishers();
                Bib.Bib_Info.Add_Publisher(DefaultValues[0]);
            }
        }
    }
}
