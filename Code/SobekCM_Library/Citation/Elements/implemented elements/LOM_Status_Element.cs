#region Using directives

using SobekCM.Core.ApplicationState;
using SobekCM.Core.Configuration.Localization;
using SobekCM.Core.Users;
using SobekCM.Library.UI;
using SobekCM.Resource_Object;
using SobekCM.Resource_Object.Metadata_Modules;
using SobekCM.Resource_Object.Metadata_Modules.LearningObjects;
using System;
using System.IO;
using System.Text;

#endregion

namespace SobekCM.Library.Citation.Elements
{
    /// <summary> Element allows entry of the learning object metadata status field </summary>
    /// <remarks> This class extends the <see cref="ComboBox_Element"/> class. </remarks>
    public class LOM_Status_Element : ComboBox_Element
    {
        private const string level1_text = "draft";
        private const string level2_text = "final";
        private const string level3_text = "revised";


        /// <summary> Constructor for a new instance of the LOM_Status_Element class </summary>
        public LOM_Status_Element()
            : base("Status", "lom_status")
        {
            Repeatable = false;

            Items.Clear();
            Items.Add(String.Empty);
            Items.Add(level1_text);
            Items.Add(level2_text);
            Items.Add(level3_text);
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
                const string defaultAcronym = "The completion status or condition of this learning object.";
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

            // Determine the value from the enum
            string value = String.Empty;

            // Try to get the learning metadata here
            LearningObjectMetadata lomInfo = Bib.Get_Metadata_Module(GlobalVar.IEEE_LOM_METADATA_MODULE_KEY) as LearningObjectMetadata;
            if (lomInfo != null)
            {
                switch (lomInfo.Status)
                {
                    case StatusEnum.draft:
                        value = level1_text;
                        break;

                    case StatusEnum.final:
                        value = level2_text;
                        break;

                    case StatusEnum.revised:
                        value = level3_text;
                        break;
                }
            }

            render_helper(Output, value, Skin_Code, Current_User, CurrentLanguage, Translator, Base_URL);
        }

        /// <summary> Prepares the bib object for the save, by clearing any existing data in this element's related field(s) </summary>
        /// <param name="Bib"> Existing digital resource object which may already have values for this element's data field(s) </param>
        /// <param name="Current_User"> Current user, who's rights may impact the way an element is rendered </param>
        /// <remarks> This does nothing since this is a singleton value, and is non-repeatable </remarks>
        public override void Prepare_For_Save(SobekCM_Item Bib, User_Object Current_User)
        {
            // Do nothing since there is only one corresponding value
        }

        /// <summary> Saves the data rendered by this element to the provided bibliographic object during postback </summary>
        /// <param name="Bib"> Object into which to save the user's data, entered into the html rendered by this element </param>
        public override void Save_To_Bib(SobekCM_Item Bib)
        {
            var getKeys = Context.Request.Form.Keys;
            foreach (string thisKey in getKeys)
            {
                if (thisKey.IndexOf(html_element_name.Replace("_", "")) == 0)
                {
                    // Get the value from the combo box
                    string value = Context.Request.Form[thisKey].TrimFirst();

                    // Try to get any existing learning object metadata module
                    LearningObjectMetadata lomInfo = Bib.Get_Metadata_Module(GlobalVar.IEEE_LOM_METADATA_MODULE_KEY) as LearningObjectMetadata;

                    if (value.Length == 0)
                    {
                        // I fhte learning object metadata does exist, set it to undefined
                        if (lomInfo != null)
                            lomInfo.Status = StatusEnum.UNDEFINED;
                    }
                    else
                    {
                        // There is a value, so ensure learning object metadata does exist
                        if (lomInfo == null)
                        {
                            lomInfo = new LearningObjectMetadata();
                            Bib.Add_Metadata_Module(GlobalVar.IEEE_LOM_METADATA_MODULE_KEY, lomInfo);
                        }

                        // Save the new value
                        switch (value)
                        {
                            case level1_text:
                                lomInfo.Status = StatusEnum.draft;
                                break;

                            case level2_text:
                                lomInfo.Status = StatusEnum.final;
                                break;

                            case level3_text:
                                lomInfo.Status = StatusEnum.revised;
                                break;
                        }
                    }
                    return;
                }
            }
        }
    }
}