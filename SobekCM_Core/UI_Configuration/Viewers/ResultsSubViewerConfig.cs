using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using ProtoBuf;

namespace SobekCM.Core.UI_Configuration.Viewers
{
    /// <summary> Configuration for a single results subviewer, including some of how
    /// this subviewer maps to the database and the actual classes to create the viewer </summary>
    [Serializable, DataContract, ProtoContract]
    [XmlRoot("resultsSubViewer")]
    public class ResultsSubViewerConfig
    {
        /// <summary> Viewer code that is mapped to this subviewer </summary>
        [DataMember(Name = "code")]
        [XmlAttribute("code")]
        [ProtoMember(1)]
        public string ViewerCode { get; set; }

        /// <summary> Flag indicates if this subviewer is enabled or disabled </summary>
        [DataMember(Name = "enabled")]
        [XmlAttribute("enabled")]
        [ProtoMember(2)]
        public bool Enabled { get; set; }

        /// <summary> Fully qualified (including namespace) name of the class used 
        /// for this subviewer </summary>
        [DataMember(Name = "class")]
        [XmlAttribute("class")]
        [ProtoMember(3)]
        public string Class { get; set; }

        /// <summary> Name of the assembly within which this class resides, unless this
        /// is one of the default subviewers included in the core code </summary>
        [DataMember(Name = "assembly", EmitDefaultValue = false)]
        [XmlAttribute("assembly")]
        [ProtoMember(4)]
        public string Assembly { get; set; }

        /// <summary> Name of this viewer, which matches the viewer name from the database and 
        /// in the configuration files as well.  This is actually populate by the configuration information </summary>
        [DataMember(Name = "type")]
        [XmlAttribute("type")]
        [ProtoMember(5)]
        public string ViewerType { get; set; }

        /// <summary> Standard label for this viewer, which appears in the main menu </summary>
        [DataMember(Name = "label")]
        [XmlAttribute("label")]
        [ProtoMember(6)]
        public string Label { get; set; }

        /// <summary> Icon for this viewer, which can allow the user to choose this viewer </summary>
        [DataMember(Name = "icon")]
        [XmlAttribute("icon")]
        [ProtoMember(7)]
        public string Icon { get; set; }

        /// <summary> Description for this results viewer, which can show as a hover-over for selection </summary>
        [DataMember(Name = "description")]
        [XmlAttribute("description")]
        [ProtoMember(8)]
        public string Description { get; set; }

    }
}
