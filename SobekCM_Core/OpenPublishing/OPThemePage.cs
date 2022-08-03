using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace SobekCM.Core.OpenPublishing
{
    /// <summary> Information about a single page in an OpenPublishing theme
    /// which is read from the theme's HTML template files </summary>
    [Serializable, DataContract, ProtoContract]
    [XmlRoot("ThemePage")]
    public class OPThemePage
    {
        public OPThemePage()
        {
            TagsList = new List<string>();
        }

        /// <summary> Name of the actual HTML file which was read </summary>
        [DataMember(Name = "file", EmitDefaultValue = false)]
        [XmlAttribute("file")]
        [ProtoMember(1)]
        public string FileName { get; set; }

        /// <summary> Name for this page </summary>
        [DataMember(Name = "pageName", EmitDefaultValue = false)]
        [XmlAttribute("pageName")]
        [ProtoMember(2)]
        public string PageName { get; set; }

        /// <summary> Description of this page, describing its purpose </summary>
        [DataMember(Name = "description", EmitDefaultValue = false)]
        [XmlElement("description")]
        [ProtoMember(3)]
        public string Description { get; set; }

        /// <summary> Display Html for this page </summary>
        [DataMember(Name = "html", EmitDefaultValue = false)]
        [XmlElement("html")]
        [ProtoMember(4)]
        public string Html { get; set; }

        private HashSet<string> tags;

        /// <summary> List of replaceable tags included in this page's display html </summary>
        [XmlIgnore]
        [IgnoreDataMember]
        public HashSet<string> Tags
        {
            get
            {
                if ( tags == null )
                {
                    tags = new HashSet<string>();
                    if(TagsList != null )
                    {
                        foreach( string tag in TagsList)
                        {
                            tags.Add(tag);
                        }
                    }
                }
                return tags;
            }
        }

        [DataMember(Name = "tags")]
        [XmlArray("tags")]
        [XmlArrayItem("tag", typeof(string))]
        [ProtoMember(5)]
        public List<string> TagsList { get; set; }
    }
}
