﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using SobekCM.Core.ApplicationState;
using SobekCM.Core.Configuration.Localization;
using SobekCM.Core.Users;
using SobekCM.Resource_Object;
using SobekCM.Resource_Object.Bib_Info;

namespace SobekCM.Library.Citation.Elements
{
    /// <summary> Element allows entry of an accession number, which is a type of identifier </summary>
    /// <remarks> This class extends the <see cref="SimpleTextBox_Element"/> class. </remarks>
    public class Accession_Number_Element : MultipleTextBox_Element
    {
        /// <summary> Constructor for a new instance of the Accession_Number_Element class </summary>
        public Accession_Number_Element() : base("Accession Number", "accno")
        {
            Repeatable = true;

            MaxBoxes = 3;
            BoxesPerLine = 3;
        }

        /// <summary> Options dictionary allows template elements to register certain options or information
        /// which may be used by other template elements </summary>
        /// <remarks> This adds a special flag to indicate there is a seperate contributor element ( title_form_included = true ) </remarks>
        public override Dictionary<string, string> Options
        {
            get { return base.Options; }
            set
            {
                base.Options = value;

                Options["accession_number_included"] = "true";
            }
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
                const string defaultAcronym = "Enter the accession number for this item.";
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

            List<string> terms = new List<string>();
            if (Bib.Bib_Info.Identifiers_Count > 0)
            {
                foreach (Identifier_Info thisIdentifier in Bib.Bib_Info.Identifiers)
                {
                    if ((thisIdentifier.Type.IndexOf("ACCESSION", StringComparison.OrdinalIgnoreCase) >= 0) || ((thisIdentifier.Type.IndexOf("ACCN", StringComparison.OrdinalIgnoreCase) >= 0)))
                        terms.Add(thisIdentifier.Identifier);
                }
            }


            render_helper(Output, terms, Skin_Code, Current_User, CurrentLanguage, Translator, Base_URL);
        }

        /// <summary> Prepares the bib object for the save, by clearing any existing data in this element's related field(s) </summary>
        /// <param name="Bib"> Existing digital resource object which may already have values for this element's data field(s) </param>
        /// <param name="Current_User"> Current user, who's rights may impact the way an element is rendered </param>
        /// <remarks> This clears any preexisting identifiers </remarks>
        public override void Prepare_For_Save(SobekCM_Item Bib, User_Object Current_User)
        {
            // Find any existing accession numbers
            List<Identifier_Info> existing_accno = new List<Identifier_Info>();
            foreach (Identifier_Info thisIdentifier in Bib.Bib_Info.Identifiers)
            {
                if ((thisIdentifier.Type.IndexOf("ACCESSION", StringComparison.OrdinalIgnoreCase) >= 0) || ((thisIdentifier.Type.IndexOf("ACCN", StringComparison.OrdinalIgnoreCase) >= 0)))
                    existing_accno.Add(thisIdentifier);
            }

            // Remove this existing accession number
            foreach (Identifier_Info thisIdentifier in existing_accno)
                Bib.Bib_Info.Remove_Identifier(thisIdentifier);
        }

        /// <summary> Saves the data rendered by this element to the provided bibliographic object during postback </summary>
        /// <param name="Bib"> Object into which to save the user's data, entered into the html rendered by this element </param>
        public override void Save_To_Bib(SobekCM_Item Bib)
        {
            string[] getKeys = HttpContext.Current.Request.Form.AllKeys;
            foreach (string thisKey in getKeys.Where(thisKey => thisKey.IndexOf("accnumber") == 0))
            {
                Bib.Bib_Info.Add_Identifier(HttpContext.Current.Request.Form[thisKey], "Accession Number");
            }
        }
    }
}
