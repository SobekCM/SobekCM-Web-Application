using ProtoBuf;
using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace SobekCM.Core.Configuration.Localization
{
    /// <summary> A single supported language, as read from the language support configuration file
    /// ( sobekcm_language_support.config, under the &lt;Languages&gt; element ) </summary>
    [Serializable, DataContract, ProtoContract]
    [XmlRoot("LanguageInfo")]
    public class Web_Language_Info
    {
        /// <summary> Display name for this language, as given in the configuration file (e.g. "Dutch") </summary>
        [DataMember(Name = "name")]
        [XmlAttribute("name")]
        [ProtoMember(1)]
        public string Name { get; set; }

        /// <summary> ISO code for this language (e.g. "nl"), matching the configuration file's element text </summary>
        [DataMember(Name = "code")]
        [XmlAttribute("code")]
        [ProtoMember(2)]
        public string Code { get; set; }
    }
}
