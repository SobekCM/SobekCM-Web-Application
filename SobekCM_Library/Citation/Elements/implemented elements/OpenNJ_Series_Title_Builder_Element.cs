using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SobekCM.Core.Users;
using SobekCM.Resource_Object;
using SobekCM.Resource_Object.Bib_Info;

namespace SobekCM.Library.Citation.Elements
{
    /// <summary> Element has no visibility on the template, but combines the title and
    /// creator to create the series title during initial save </summary>
    /// <remarks> This class extends the <see cref="Hidden_Element"/> class. </remarks>
    public class OpenNJ_Series_Title_Builder_Element : Hidden_Element
    {
        /// <summary> Prepares the bib object for the save, by clearing any existing data in this element's related field(s) </summary>
        /// <param name="Bib"> Existing digital resource object which may already have values for this element's data field(s) </param>
        /// <param name="Current_User"> Current user, who's rights may impact the way an element is rendered </param>
        /// <remarks> This does nothing, since there is only one main series title </remarks>
        public override void Prepare_For_Save(SobekCM_Item Bib, User_Object Current_User)
        {
            // Do nothing
        }

        /// <summary> Saves the data rendered by this element to the provided bibliographic object during postback </summary>
        /// <param name="Bib"> Object into which to save the user's data, entered into the html rendered by this element </param>
        public override void Save_To_Bib(SobekCM_Item Bib)
        {
            string title = Bib.Bib_Info.Main_Title.ToString();
            foreach (Title_Info titleObj in Bib.Bib_Info.Other_Titles)
            {
                if (titleObj.Title_Type == Title_Type_Enum.Course)
                {
                    title = titleObj.ToString();
                    break;
                }

            }

            string creator = Bib.Bib_Info.Main_Entity_Name.ToString();

            if ( String.IsNullOrWhiteSpace(creator) || ( creator.Equals("unknown", StringComparison.OrdinalIgnoreCase)))
            {
                if ((Bib.Bib_Info.Names_Count > 0) && (!Bib.Bib_Info.Names[0].ToString().Equals("unknown", StringComparison.OrdinalIgnoreCase)))
                {
                    creator = Bib.Bib_Info.Names[0].ToString();
                }
            }

            if ( !String.IsNullOrWhiteSpace(title))
            {
                if ( !String.IsNullOrWhiteSpace(creator))
                {
                    if ( creator.IndexOf(",") > 0 )
                    {
                        creator = creator.Substring(0, creator.IndexOf(",")).Trim();
                    }

                    Bib.Bib_Info.SeriesTitle.Title = title.Trim() + " - " + creator.Trim();
                }
                else
                {
                    Bib.Bib_Info.SeriesTitle.Title = title.Trim();
                }
            }
        }
    }
}
