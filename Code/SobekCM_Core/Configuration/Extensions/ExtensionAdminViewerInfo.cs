using ProtoBuf;
using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace SobekCM.Core.Configuration.Extensions
{
    /// <summary> Registers a single admin viewer supplied by an extension, resolved by
    /// <see cref="SobekCM.Library.AdminViewer.AdminViewer_Factory"/> the same way
    /// <c>ItemViewer_Factory</c> resolves item viewer prototypers - by class name, optionally loaded
    /// from an assembly referenced elsewhere in the same extension (see <see cref="ExtensionInfo.Assemblies"/>) </summary>
    [Serializable, DataContract, ProtoContract]
    [XmlRoot("ExtensionAdminViewerInfo")]
    public class ExtensionAdminViewerInfo
    {
        /// <summary> Admin view code this viewer answers to - the "admin/{code}" URL segment
        /// (see <see cref="SobekCM.Core.Navigation.Admin_View_Codes"/> for the built-in codes) </summary>
        [DataMember(Name = "code")]
        [XmlAttribute("code")]
        [ProtoMember(1)]
        public string Code { get; set; }

        /// <summary> Fully-qualified class name of the admin viewer, which must extend
        /// <c>abstract_AdminViewer</c> and expose a public constructor taking (RequestCache, HttpContext) </summary>
        [DataMember(Name = "class")]
        [XmlAttribute("class")]
        [ProtoMember(2)]
        public string Class { get; set; }

        /// <summary> ID of the assembly this class is defined in (see <see cref="ExtensionInfo.Assemblies"/>
        /// / <see cref="Extension_Configuration.Get_Assembly"/>) - empty if the class lives in a core assembly </summary>
        [DataMember(Name = "assembly", EmitDefaultValue = false)]
        [XmlAttribute("assembly")]
        [ProtoMember(3)]
        public string Assembly { get; set; }

        /// <summary> Constructor for a new instance of the ExtensionAdminViewerInfo class </summary>
        public ExtensionAdminViewerInfo()
        {
            // Do nothing.. primarily for serialization purposes
        }
    }
}
