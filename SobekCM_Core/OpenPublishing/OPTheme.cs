using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace SobekCM.Core.OpenPublishing
{
    /// <summary> OpenPublishing Theme which dictates how the content is displayed within
    /// the system </summary>
    [Serializable, DataContract, ProtoContract]
    [XmlRoot("Theme")]
    public class OPTheme
    {
        /// <summary> Name of this theme </summary>
        [DataMember(Name = "name", EmitDefaultValue = false)]
        [XmlAttribute("name")]
        [ProtoMember(1)]
        public string ThemeName { get; set; }

        /// <summary> Primary key for this theme from the database </summary>
        [DataMember(Name = "key", EmitDefaultValue = false)]
        [XmlAttribute("key")]
        [ProtoMember(2)]
        public int PrimaryKey { get; set; }

        /// <summary> Location where the html template files are found, relative to this instance </summary>
        [DataMember(Name = "location", EmitDefaultValue = false)]
        [XmlElement("location")]
        [ProtoMember(3)]
        public string Location { get; set; }

        /// <summary> Author of this theme file </summary>
        [DataMember(Name = "author", EmitDefaultValue = false)]
        [XmlAttribute("author")]
        [ProtoMember(4)]
        public string Author { get; set; }

        /// <summary> Description of this theme file, describing its main 
        /// characteristics </summary>
        [DataMember(Name = "description", EmitDefaultValue = false)]
        [XmlElement("description")]
        [ProtoMember(5)]
        public string Description { get; set; }

        /// <summary> CSS file to be included when using this theme, which dictates
        /// how material using this font is displayed </summary>
        [DataMember(Name = "cssFile", EmitDefaultValue = false)]
        [XmlElement("cssFile")]
        [ProtoMember(6)]
        public string CssFile { get; set; }

        /// <summary> List of the front pages to be included within every resource </summary>
        [DataMember(Name = "frontPages")]
        [XmlArray("frontPages")]
        [XmlArrayItem("themePage", typeof(OPThemePage))]
        [ProtoMember(7)]
        public List<OPThemePage> FrontPages { get; set; }

        /// <summary> Template page for the first page of a chapter </summary>
        [DataMember(Name = "chapterStartPage")]
        [XmlElement("chapterStartPage")]
        [ProtoMember(8)]
        public OPThemePage ChapterStartPage { get; set; }

        /// <summary> List of any end pages which appear at the end of the resource </summary>
        [DataMember(Name = "backPages")]
        [XmlArray("backPages")]
        [XmlArrayItem("themePage", typeof(OPThemePage))]
        [ProtoMember(9)]
        public List<OPThemePage> EndPages { get; set; }

        /// <summary> Image for this theme which appears when it is selected </summary>
        [DataMember(Name = "image", EmitDefaultValue = false)]
        [XmlElement("image")]
        [ProtoMember(10)]
        public string Image { get; set; }

        /// <summary> Flag which indicates if this theme is available for users to select </summary>
        /// <remarks> This allows a theme to remain active for existing OER, while not being selected for
        /// new OER </remarks>
        [DataMember(Name = "availableForSelection")]
        [XmlAttribute("availableForSelection")]
        [ProtoMember(11)]
        public bool AvailableForSelection { get; set; }

        /// <summary> Default constructor for a new instance of the OPTheme class </summary>
        public OPTheme()
        {
            AvailableForSelection = true;
        }
    }
}
