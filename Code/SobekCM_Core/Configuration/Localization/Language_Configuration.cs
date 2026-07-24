using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace SobekCM.Core.Configuration.Localization
{
    /// <summary> Collection of all the languages this instance supports, as read from
    /// sobekcm_language_support.config, along with a pointer to the configured base/default language </summary>
    [Serializable, DataContract, ProtoContract]
    [XmlRoot("LanguageConfig")]
    public class Language_Configuration
    {
        /// <summary> Constructor for a new instance of the Language_Configuration class </summary>
        public Language_Configuration()
        {
            Languages = new List<Web_Language_Info>();
        }

        /// <summary> Every language this instance supports </summary>
        [DataMember(Name = "languages", EmitDefaultValue = false)]
        [XmlArray("languages")]
        [XmlArrayItem("language", typeof(Web_Language_Info))]
        [ProtoMember(1)]
        public List<Web_Language_Info> Languages { get; set; }

        /// <summary> The configured base/default language for this instance ( the &lt;Language&gt; element
        /// with a non-empty "default" attribute ), or NULL if none was flagged as the default </summary>
        [DataMember(Name = "defaultLanguage", EmitDefaultValue = false)]
        [XmlElement("defaultLanguage")]
        [ProtoMember(2)]
        public Web_Language_Info Default_Language { get; set; }

        /// <summary> Clears all the languages and the default-language pointer, so a config file marked
        /// ClearAll="true" can start this list over from scratch rather than only ever adding to it </summary>
        public void ClearAll()
        {
            Languages.Clear();
            Default_Language = null;
        }
    }
}
