using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using ProtoBuf;

namespace SobekCM.Core.UI_Configuration.Viewers
{
    /// <summary> Configuration for a layout for the item writer </summary>
    [Serializable, DataContract, ProtoContract]
    [XmlRoot("ItemWriterLayoutConfig")]
    public class ItemWriterLayoutConfig
    {
        /// <summary> Identifier for this digital resource item display layout </summary>
        [DataMember(Name = "id")]
        [XmlAttribute("id")]
        [ProtoMember(1)]
        public string ID { get; set; }

        /// <summary> HTML source file for this layout of the digital resource item display </summary>
        [DataMember(Name = "source")]
        [XmlAttribute("source")]
        [ProtoMember(2)]
        public string Source { get; set; }

        /// <summary> Collection of stylesheets which should be included with this layout
        /// of the digital resource item display </summary>
        [DataMember(Name = "stylesheets")]
        [XmlArray("stylesheets")]
        [XmlArrayItem("stylesheet", typeof(StylesheetConfig))]
        [ProtoMember(3)]
        public List<StylesheetConfig> Stylesheets { get; set; }

        /// <summary> Collection of sections to write within for this item display which should
        /// match the section directives within the item display layout HTML file </summary>
        [DataMember(Name = "sections")]
        [XmlArray("sections")]
        [XmlArrayItem("section", typeof(StylesheetConfig))]
        [ProtoMember(4)]
        public List<SectionWriterGroupConfig> Sections { get; set; }

        /// <summary> Constructor for a new instance of the ItemWriterLayoutConfig class </summary>
        public ItemWriterLayoutConfig()
        {
            Stylesheets = new List<StylesheetConfig>();
            Sections = new List<SectionWriterGroupConfig>();
        }

        /// <param name="Name"> Name of this section, which can include section writers and should 
        /// match a section directive in the item writer source HTML file </param>
        /// <returns> Either the existing section, or NULL  </returns>
        public SectionWriterGroupConfig GetSection(string Name)
        {
            // Look for a match
            foreach (SectionWriterGroupConfig thisSection in Sections)
            {
                if (String.Compare(Name, thisSection.Name, StringComparison.Ordinal) == 0)
                    return thisSection;
            }

            return null;
        }

        /// <param name="Name"> Name of this section, which can include section writers and should 
        /// match a section directive in the item writer source HTML file </param>
        /// <returns> Either the existing, or a new, section  </returns>
        public SectionWriterGroupConfig GetOrCreateSection(string Name)
        {
            // Look for a match
            foreach (SectionWriterGroupConfig thisSection in Sections)
            {
                if (String.Compare(Name, thisSection.Name, StringComparison.Ordinal) == 0)
                    return thisSection;

            }

            // Didn't return anything, so must be a new ID
            SectionWriterGroupConfig newSection = new SectionWriterGroupConfig(Name);
            Sections.Add(newSection);
            return newSection;
        }
    }
}
