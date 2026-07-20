using ProtoBuf;
using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace SobekCM.Core.UI_Configuration.Viewers
{
    /// <summary> Configuration information for a stylesheet to be included </summary>
    /// <remarks> This config object is used for all writers ( i.e., digital resource and aggregation writers ) </remarks>
    [Serializable, DataContract, ProtoContract]
    [XmlRoot("StylesheetConfig")]
    public class StylesheetConfig
    {
        /// <summary> Source (URL) for the extra stylesheet to be included </summary>
        [DataMember(Name = "source")]
        [XmlAttribute("source")]
        [ProtoMember(1)]
        public string Source { get; set; }

        /// <summary> Media value associated with the stylesheet to be included </summary>
        [DataMember(Name = "media", EmitDefaultValue = false)]
        [XmlAttribute("media")]
        [ProtoMember(2)]
        public string Media { get; set; }
    }
}
