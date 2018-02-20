using System;
using System.Collections.Generic;

namespace SobekCM.Core.ApplicationState
{
    public class Multiple_Volume_Collection
    {
        private Dictionary<string, Dictionary<short, Multiple_Volume_Item>> lookupDictionary;

        /// <summary> Constructor for a new instance of the Multiple_Volume_Collection class </summary>
        public Multiple_Volume_Collection()
        {
            lookupDictionary = new Dictionary<string, Dictionary<short, Multiple_Volume_Item>>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary> Get information about a title by BibID </summary>
        /// <param name="BibID">BibID to look for</param>
        /// <returns> An object, or null </returns>
        public Multiple_Volume_Item Get_Title(string BibID)
        {
            // If the dictionary is null, just return null
            if (lookupDictionary == null)
                return null;

            // If the BibiD length is not valid, return null;
            if (( String.IsNullOrEmpty(BibID)) || (BibID.Length != 10))
                return null;

            // Get the first six to find first
            string short_bib = BibID.Substring(0, 6);
            if (!lookupDictionary.ContainsKey(short_bib)) return null;

            // It did find it, so go to the next step
            Int16 last_four;
            if ( Int16.TryParse(BibID.Substring(6, 4), out last_four ))
            {
                if (lookupDictionary[short_bib].ContainsKey(last_four))
                    return lookupDictionary[short_bib][last_four];
            }

            // If it got here, reutrn null
            return null;
        }

        /// <summary> Add a new title info object to this collection for searching </summary>
        /// <param name="BibID"> Bibliographic identifier for this title </param>
        /// <param name="ThumbnailUrl">URL for a custom thumbnail ( or null )</param>
        /// <param name="FlagByte">Byte contains a number of title-specific flags</param>
        /// <param name="LastFour"> Last four digits of the bibid, as a small integer </param>
        /// <param name="GroupTitle"> Group title </param>
        public void Add_Title(string BibID, string ThumbnailUrl, byte FlagByte, short LastFour, string GroupTitle)
        {
            // Create the multi-volume object
            Multiple_Volume_Item titleItem = new Multiple_Volume_Item
            {
                CustomThumbnail = ThumbnailUrl,
                FlagByte = FlagByte,
                GroupTitle = GroupTitle
            };

            // Add this to the search mechanism
            string short_bibid = (BibID.Length > 6) ? BibID.Substring(0, 6) : BibID;
            if ( !lookupDictionary.ContainsKey(short_bibid))
            {
                Dictionary<short, Multiple_Volume_Item> innerDictionary = new Dictionary<short, Multiple_Volume_Item>();
                lookupDictionary[short_bibid] = innerDictionary;
                innerDictionary[LastFour] = titleItem;
            }
            else
            {
                lookupDictionary[short_bibid][LastFour] = titleItem;
            }
        }

        /// <summary> Clear all data in this collection </summary>
        public void Clear()
        {
            // clear all
            lookupDictionary.Clear();
        }
    }
}
