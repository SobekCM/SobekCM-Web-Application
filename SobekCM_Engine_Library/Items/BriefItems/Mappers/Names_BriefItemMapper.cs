﻿#region Using directives

using System;
using System.Text;
using SobekCM.Core.BriefItem;
using SobekCM.Resource_Object;
using SobekCM.Resource_Object.Bib_Info;

#endregion

namespace SobekCM.Engine_Library.Items.BriefItems.Mappers
{
    /// <summary> Maps all the names ( i.e., creator, contributor, etc.. ) from the METS-based 
    /// SobekCM_Item object to the BriefItem, used for most the public functions of the front-end </summary>
    public class Names_BriefItemMapper : IBriefItemMapper
    {
        /// <summary> Map one or more data elements from the original METS-based object to the
        /// BriefItem object </summary>
        /// <param name="Original"> Original METS-based object </param>
        /// <param name="New"> New object to populate some data from the original </param>
        /// <returns> TRUE if successful, FALSE if an exception is encountered </returns>
        public bool MapToBriefItem(SobekCM_Item Original, BriefItemInfo New)
        {

            // Add the main entity first
            if (Original.Bib_Info.hasMainEntityName) 
            {
                // Is this a conference?
                if (Original.Bib_Info.Main_Entity_Name.Name_Type == Name_Info_Type_Enum.Conference)
                {
                    if (!String.IsNullOrWhiteSpace(Original.Bib_Info.Main_Entity_Name.Full_Name))
                        New.Add_Description("Conference", Original.Bib_Info.Main_Entity_Name.ToString());
                }
                else
                {
                    // Build the full name and info
                    Name_Info thisName = Original.Bib_Info.Main_Entity_Name;
                    StringBuilder nameBuilder = new StringBuilder();
                    if ( !String.IsNullOrWhiteSpace(thisName.Full_Name))
                    {
                        nameBuilder.Append(thisName.Full_Name.Replace("|", " -- "));
                    }
                    else
                    {
                        if ( !String.IsNullOrWhiteSpace(thisName.Family_Name))
                        {
                            if (!String.IsNullOrWhiteSpace(thisName.Given_Name))
                            {
                                nameBuilder.Append(thisName.Family_Name + ", " + thisName.Given_Name);
                            }
                            else
                            {
                                nameBuilder.Append(thisName.Family_Name);
                            }
                        }
                        else
                        {
                            nameBuilder.Append(!String.IsNullOrWhiteSpace(thisName.Given_Name) ? thisName.Given_Name : "unknown");
                        }
                    }

                    // This is all that should be searched
                    string searchTerm = nameBuilder.ToString();

                    // Add the display form and dates
                    if (!String.IsNullOrEmpty(thisName.Display_Form))
                        nameBuilder.Append(" ( " + thisName.Display_Form + " )");
                    if (!String.IsNullOrEmpty(thisName.Dates))
                        nameBuilder.Append(", " + thisName.Dates);

                    // Add affiliation
                    if ( !String.IsNullOrEmpty(thisName.Affiliation))
                    {
                        nameBuilder.Append(" ( " + thisName.Affiliation + " )");
                    }

                    // Add this now
                    BriefItem_DescTermValue descTerm = New.Add_Description("Creator", nameBuilder.ToString());

                    // Add with the sub-roles as well
                    string roles = thisName.Role_String;
                    if (!String.IsNullOrWhiteSpace(roles))
                        descTerm.SubTerm = roles;

                    // Was the search term different than the actual name?
                    if (searchTerm != nameBuilder.ToString())
                        descTerm.SearchTerm = searchTerm;
                }
            }

            // Add all the other names attached
            if (Original.Bib_Info.Names_Count > 0)
            {
                foreach (Name_Info thisName in Original.Bib_Info.Names)
                {
                    // Is this a conference?
                    if (thisName.Name_Type == Name_Info_Type_Enum.Conference)
                    {
                        if (!String.IsNullOrWhiteSpace(thisName.Full_Name))
                            New.Add_Description("Conference", thisName.ToString());
                    }
                    else
                    {
                        // Build the full name and info
                        StringBuilder nameBuilder = new StringBuilder();
                        if (!String.IsNullOrWhiteSpace(thisName.Full_Name))
                        {
                            nameBuilder.Append(thisName.Full_Name.Replace("|", " -- "));
                        }
                        else
                        {
                            if (!String.IsNullOrWhiteSpace(thisName.Family_Name))
                            {
                                if (!String.IsNullOrWhiteSpace(thisName.Given_Name))
                                {
                                    nameBuilder.Append(thisName.Family_Name + ", " + thisName.Given_Name);
                                }
                                else
                                {
                                    nameBuilder.Append(thisName.Family_Name);
                                }
                            }
                            else
                            {
                                nameBuilder.Append(!String.IsNullOrWhiteSpace(thisName.Given_Name) ? thisName.Given_Name : "unknown");
                            }
                        }

                        // This is all that should be searched
                        string searchTerm = nameBuilder.ToString();

                        // Add the display form and dates
                        if (thisName.Display_Form.Length > 0)
                            nameBuilder.Append(" ( " + thisName.Display_Form + " )");
                        if (thisName.Dates.Length > 0)
                            nameBuilder.Append(", " + thisName.Dates);

                        // Add this now
                        BriefItem_DescTermValue descTerm = New.Add_Description("Creator", nameBuilder.ToString());

                        // Add with the sub-roles as well
                        string roles = thisName.Role_String;
                        if (!String.IsNullOrWhiteSpace(roles))
                            descTerm.SubTerm = roles;

                        // Was the search term different than the actual name?
                        if (searchTerm != nameBuilder.ToString())
                            descTerm.SearchTerm = searchTerm;
                    }
                }
            }

            return true;
        }
    }
}
