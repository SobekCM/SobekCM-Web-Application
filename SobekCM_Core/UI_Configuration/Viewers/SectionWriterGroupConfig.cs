using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using ProtoBuf;

namespace SobekCM.Core.UI_Configuration.Viewers
{
    /// <summary> Configuration information for the a piece of code that writes HTML in a section of the page </summary>
    /// <remarks> This config object is used for all writers ( i.e., digital resource and aggregation writers ) </remarks>
    [Serializable, DataContract, ProtoContract]
    [XmlRoot("SectionWriterGroupConfig")]
    public class SectionWriterGroupConfig
    {
        /// <summary> Name of this section, which can include section writers and should 
        /// match a section directive in the item writer source HTML file </summary>
        [DataMember(Name = "name")]
        [XmlAttribute("name")]
        [ProtoMember(1)]
        public string Name { get; set; }

        /// <summary> Gets the collection of section writers which write within this section of
        /// the digital resource item display </summary>
        [DataMember(Name = "writers")]
        [XmlArray("writers")]
        [XmlArrayItem("writer", typeof(SectionWriterConfig))]
        [ProtoMember(2)]
        public List<SectionWriterConfig> Writers { get; set; }

        /// <summary> Constructor for a new instance of the <see cref="SectionWriterGroupConfig"/> class </summary>
        public SectionWriterGroupConfig()
        {
            Writers = new List<SectionWriterConfig>();
        }

        /// <summary> Constructor for a new instance of the <see cref="SectionWriterGroupConfig"/> class </summary>
        /// <param name="Name"> Name of this section, which can include section writers and should 
        /// match a section directive in the item writer source HTML file </param>
        public SectionWriterGroupConfig( string Name )
        {
            this.Name = Name;
            Writers = new List<SectionWriterConfig>();
        }
    }
}
