#region Using directives

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using SobekCM.Core.ApplicationState;
using SobekCM.Core.Configuration;
using SobekCM.Core.Configuration.Localization;
using SobekCM.Core.Users;
using SobekCM.Resource_Object;
using SobekCM.Resource_Object.Bib_Info;

#endregion

namespace SobekCM.Library.Citation.Elements.implemented_elements
{
    /// <summary> Element allows selection of one or more genres from a controller list for an item </summary>
    /// <remarks> This class extends the <see cref="MultipleComboBox_Element"/> class. </remarks>
    public class Genre_Select_Element : MultipleComboBox_Element
    {
        /// <summary> Constructor for a new instance of the Genre_Select_Element class </summary>
        public Genre_Select_Element()
            : base("Genre", "genre")
        {
            Repeatable = true;
            ViewChoicesString = String.Empty;

            MaxBoxes = -1;
            BoxesPerLine = 3;
        }

        public override void Prepare_For_Save(SobekCM_Item Bib, User_Object Current_User)
        {
            Bib.Bib_Info.Clear_Genres();
        }

        public override void Render_Template_HTML(TextWriter Output, SobekCM_Item Bib, string Skin_Code, bool IsMozilla, StringBuilder PopupFormBuilder, User_Object Current_User, Web_Language_Enum CurrentLanguage, Language_Support_Info Translator, string Base_URL)
        {
            // Check that an acronym exists
            if (Acronym.Length == 0)
            {
                const string defaultAcronym = "Select the material type";
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

            List<string> genres = new List<string>();
            foreach (Genre_Info genre in Bib.Bib_Info.Genres)
            {
                if (!genres.Contains(genre.Genre_Term))
                    genres.Add(genre.Genre_Term);
            }
            render_helper(Output, genres, Skin_Code, Current_User, CurrentLanguage, Translator, Base_URL);
        }

        public override void Save_To_Bib(SobekCM_Item Bib)
        {
            string id = html_element_name.Replace("_", "");
            string[] getKeys = HttpContext.Current.Request.Form.AllKeys;
            foreach (string thisKey in getKeys)
            {
                if (thisKey.IndexOf(id) != 0) continue;

                string genre = HttpContext.Current.Request.Form[thisKey].Trim();
                
                Bib.Bib_Info.Add_Genre(genre);
            }
        }
    }
}
