#region Using directives

using SobekCM.Core.Configuration.Localization;
using System.Collections.Generic;
using System.Runtime.Serialization;

#endregion

namespace SobekCM.Core.ApplicationState
{
    /// <summary> Class stores all the common translations used in the web interface </summary>
    /// <remarks> Backed by the <see cref="SobekCM_Metadata_Translation"/> database table, one row per
    /// (English term, language) pair, loaded via <c>Engine_Database.Populate_Translations</c>. Uses
    /// <see cref="Web_Language_Translation_Lookup"/> so any language in <see cref="Web_Language_Enum"/>
    /// can be stored per term, not just a fixed pair of languages. </remarks>
    [DataContract]
    public class Language_Support_Info
    {
        /// <summary> Constructor for a new instance of the Language_Support_Info class </summary>
        public Language_Support_Info()
        {
            Translations = new Dictionary<string, Web_Language_Translation_Lookup>();
        }

        /// <summary> Every translated term, keyed by the English source text </summary>
        [DataMember]
        public Dictionary<string, Web_Language_Translation_Lookup> Translations { get; set; }

        /// <summary> Clears all the data stored in this object </summary>
        public void Clear()
        {
            Translations.Clear();
        }

        /// <summary> Add a translation to the translation dictionary </summary>
        /// <param name="English"> Term in english </param>
        /// <param name="Language"> Language of the translated value </param>
        /// <param name="Value"> Translated value, in the provided language </param>
        public void Add_Translation(string English, Web_Language_Enum Language, string Value)
        {
            Web_Language_Translation_Lookup lookup;
            if (!Translations.TryGetValue(English, out lookup))
            {
                lookup = new Web_Language_Translation_Lookup { DefaultValue = English };
                Translations[English] = lookup;
            }

            lookup.Add_Translation(Language, Value);
        }

        /// <summary> Generic method requests translation from the appropriate translation dictionary </summary>
        /// <param name="English"> Term in english </param>
        /// <param name="Language"> Current language of the web interface </param>
        /// <returns> Translation of term, if it exists, otherwise the original term </returns>
        public string Get_Translation(string English, Web_Language_Enum Language)
        {
            if (string.IsNullOrEmpty(English))
                return string.Empty;

            Web_Language_Translation_Lookup lookup;
            return Translations.TryGetValue(English, out lookup) ? lookup.Get_Value(Language) : English;
        }
    }
}
