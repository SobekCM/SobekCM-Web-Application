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

namespace SobekCM.Library.Citation.Elements
{
    /// <summary> Element allows selection of the target audience from a controller list for an item </summary>
    /// <remarks> This class extends the <see cref="MultipleComboBox_Element"/> class. </remarks>
    public class Target_Audience_Select_Element : MultipleComboBox_Element
    {
        /// <summary> Constructor for a new instance of the Target_Audience_Select_Element class </summary>
        public Target_Audience_Select_Element()
            : base("Target Audience", "target_audience")
        {
            Repeatable = true;
            ViewChoicesString = String.Empty;

            MaxBoxes = -1;
            BoxesPerLine = 2;
        }

        public override void Prepare_For_Save(SobekCM_Item Bib, User_Object Current_User)
        {
            Bib.Bib_Info.Clear_Target_Audiences();
        }

        public override void Render_Template_HTML(TextWriter Output, SobekCM_Item Bib, string Skin_Code, bool IsMozilla, StringBuilder PopupFormBuilder, User_Object Current_User, Web_Language_Enum CurrentLanguage, Language_Support_Info Translator, string Base_URL)
        {
            // Check that an acronym exists
            if (Acronym.Length == 0)
            {
                const string defaultAcronym = "Select the applicable target audiences";
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

            List<string> audiences = new List<string>();
            foreach (TargetAudience_Info audience in Bib.Bib_Info.Target_Audiences)
            {
                if (!audiences.Contains(audience.Audience))
                    audiences.Add(audience.Audience);
            }
            render_helper(Output, audiences, Skin_Code, Current_User, CurrentLanguage, Translator, Base_URL);
        }

        public override void Save_To_Bib(SobekCM_Item Bib)
        {
            string id = html_element_name.Replace("_", "");
            string[] getKeys = HttpContext.Current.Request.Form.AllKeys;
            foreach (string thisKey in getKeys)
            {
                if (thisKey.IndexOf(id) != 0) continue;

                string audience = HttpContext.Current.Request.Form[thisKey].Trim();
                string scheme = String.Empty;
                string audience_caps = audience.ToUpper();
                if ((audience_caps == "ADOLESCENT") || (audience_caps == "ADULT") || (audience_caps == "GENERAL") || (audience_caps == "PRIMARY") || (audience_caps == "PRE-ADOLESCENT") || (audience_caps == "JUVENILE") || (audience_caps == "PRESCHOOL") || (audience_caps == "SPECIALIZED"))
                {
                    scheme = "marctarget";
                }

                Bib.Bib_Info.Add_Target_Audience(audience, scheme);
            }
        }
    }
}
