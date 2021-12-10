using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SobekCM.Core.Users;
using SobekCM.Library.UI;
using SobekCM.Resource_Object;

namespace SobekCM.Library.Citation.Elements
{
    public class Wordmark_Assign_Institutional_Element : Hidden_Element
    {
        /// <summary> Prepares the bib object for the save, by clearing any existing data in this element's related field(s) </summary>
        /// <param name="Bib"> Existing digital resource object which may already have values for this element's data field(s) </param>
        /// <param name="Current_User"> Current user, who's rights may impact the way an element is rendered </param>
        /// <remarks> This does nothing </remarks>
        public override void Prepare_For_Save(SobekCM_Item Bib, User_Object Current_User)
        {
            // Do nothing
        }

        /// <summary> Saves the data rendered by this element to the provided bibliographic object during postback </summary>
        /// <param name="Bib"> Object into which to save the user's data, entered into the html rendered by this element </param>
        public override void Save_To_Bib(SobekCM_Item Bib)
        {
            // See if the wordmark for source insitution exists and add it
            string source = Bib.Bib_Info.Source.Code;
            add_wordmark(source, Bib);

            // Same for holding
            string holding = Bib.Bib_Info.HoldingCode;
            add_wordmark(holding, Bib);            
        }

        private void add_wordmark(string code, SobekCM_Item Bib)
        {
            if (!String.IsNullOrEmpty(code))
            {
                if (UI_ApplicationCache_Gateway.Icon_List.ContainsKey(code))
                {
                    bool found = false;
                    foreach (var wordmark in Bib.Behaviors.Wordmarks)
                    {
                        if (wordmark.Code.Equals(code, StringComparison.OrdinalIgnoreCase))
                        {
                            found = true;
                            break;
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
