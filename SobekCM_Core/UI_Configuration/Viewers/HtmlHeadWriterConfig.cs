using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using ProtoBuf;

namespace SobekCM.Core.UI_Configuration.Viewers
{
    /// <summary> Configuration information for the a piece of code that writes within the HTML head tag </summary>
    /// <remarks> This config object is used for all writers ( i.e., digital resource and aggregation writers ) </remarks>
    [Serializable, DataContract, ProtoContract]
    [XmlRoot("HtmlHeadWriterConfig")]
    public class HtmlHeadWriterConfig
    {
        /// <summary> Fully qualified (including namespace) name of the class used 
        /// for this class/assembly information </summary>
        [DataMember(Name = "class")]
        [XmlAttribute("class")]
        [ProtoMember(1)]
        public string Class { get; set; }

        /// <summary> Name of the assembly within which this class resides, unless this
        /// is one of the default class/assembly included in the core code </summary>
        [DataMember(Name = "assembly", EmitDefaultValue = false)]
        [XmlAttribute("assembly")]
        [ProtoMember(2)]
        public string Assembly { get; set; }

        /// <summary> Flag indicates if this HTML head writer section is currently enabled for writing </summary>
        [DataMember(Name = "enabled")]
        [XmlAttribute("enabled")]
        [ProtoMember(3)]
        public bool Enabled { get; set; }

        /// <summary> Identifier for this HTML head writer, used to set the enabled flag 
        /// differently in configuration files read later, such as the plug-ins or user config files </summary>
        [DataMember(Name = "id")]
        [XmlAttribute("id")]
        [ProtoMember(4)]
        public string ID { get; set; }
    }
}
