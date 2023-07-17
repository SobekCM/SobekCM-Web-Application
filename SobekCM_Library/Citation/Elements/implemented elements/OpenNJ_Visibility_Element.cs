﻿#region Using directives

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;
using SobekCM.Core.ApplicationState;
using SobekCM.Core.Configuration;
using SobekCM.Core.Configuration.Localization;
using SobekCM.Core.Users;
using SobekCM.Engine_Library.Database;
using SobekCM.Resource_Object;

#endregion

namespace SobekCM.Library.Citation.Elements
{
    /// <summary> Element allows simple entry of the visibility of this item ( i.e., PRIVATE, PUBLIC, RESTRICTED ) </summary>
    /// <remarks> This class extends the <see cref="ComboBox_Element"/> class. </remarks>
    public class OpenNJ_Visibility_Element : ComboBox_Element
    {
        private const string EVERYONE = "EVERYONE";
        private const string INSTRUCTORS = "INSTRUCTORS ONLY";

        /// <summary> Constructor for a new instance of the Visibility_Element class </summary>
        public OpenNJ_Visibility_Element() : base("Visibility", "visibility")
        {
            Repeatable = false;

            Items.Add(EVERYONE);
            Items.Add(INSTRUCTORS);
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
        public override void Render_Template_HTML(TextWriter Output, SobekCM_Item Bib, string Skin_Code, bool IsMozilla, StringBuilder PopupFormBuilder, User_Object Current_User, Web_Language_Enum CurrentLanguage, Language_Support_Info Translator, string Base_URL)
        {
            // Check that an acronym exists
            if (Acronym.Length == 0)
            {
                const string defaultAcronym = "Select whether this item should be restricted to teachers only or be available publicly for everyone.";
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

            string value = EVERYONE;
            if (Bib.Behaviors.User_Group_Access_Count > 0 )
            {
                foreach( var access in Bib.Behaviors.User_Group_Access)
                {
                    if ( access.GroupName.Equals("Instructors", StringComparison.OrdinalIgnoreCase))
                    {
                        value = INSTRUCTORS;
                        break;
                    }
                }
            }
            
            render_helper(Output, value, Skin_Code, Current_User, CurrentLanguage, Translator, Base_URL, false);
        }

        /// <summary> Prepares the bib object for the save, by clearing any existing data in this element's related field(s) </summary>
        /// <param name="Bib"> Existing digital resource object which may already have values for this element's data field(s) </param>
        /// <param name="Current_User"> Current user, who's rights may impact the way an element is rendered </param>
        /// <remarks> This does nothing since there is only one visibility value </remarks>
        public override void Prepare_For_Save(SobekCM_Item Bib, User_Object Current_User)
        {
            // Do nothing since there is only one visibility value
        }

        /// <summary> Saves the data rendered by this element to the provided bibliographic object during postback </summary>
        /// <param name="Bib"> Object into which to save the user's data, entered into the html rendered by this element </param>
        public override void Save_To_Bib(SobekCM_Item Bib)
        {
            string[] getKeys = HttpContext.Current.Request.Form.AllKeys;
            foreach (string thisKey in getKeys)
            {
                if (thisKey.IndexOf(html_element_name.Replace("_", "")) != 0) continue;

                string type_value = HttpContext.Current.Request.Form[thisKey];
                string thisType = String.Empty;
                if (type_value.IndexOf("Select") < 0)
                {
                    thisType = type_value;
                }
                switch (thisType)
                {
                    case EVERYONE:
                        Bib.Behaviors.Clear_User_Group_Access();
                        Bib.Behaviors.RestrictionMessage = String.Empty;
                        break;

                    case INSTRUCTORS:
                        bool already_linked = false;
                        foreach (var access in Bib.Behaviors.User_Group_Access)
                        {
                            if (access.GroupName.Equals("Instructors", StringComparison.OrdinalIgnoreCase))
                            {
                                already_linked = true;
                                break;
                            }
                        }
                        if ( !already_linked)
                        {
                            List<User_Group> userGroup2 = Engine_Database.Get_All_User_Groups(null);
                            int instructor_id = -1;
                            foreach (var group in userGroup2)
                            {
                                if (group.Name.Equals("Instructors", StringComparison.OrdinalIgnoreCase))
                                {
                                    instructor_id = group.UserGroupID;
                                    break;
                                }
                            }

                            if (instructor_id > 0)
                            {
                                Bib.Behaviors.Add_User_Group_Access(instructor_id, "Instructors", true, false, false);
                            }

                            Bib.Behaviors.RestrictionMessage = "This OER content is restricted to instructors.";
                        }
                        break;
                }
                return;
            }
        }
    }
}
