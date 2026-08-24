using ProtoBuf;
using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace SobekCM.Core.Configuration.Extensions
{
    /// <summary> Registers a single main writer supplied by an extension, resolved by
    /// <see cref="SobekCM.Library.MainWriters.MainWriter_Factory"/> the same way
    /// <c>AdminViewer_Factory</c> resolves plugin-registered admin viewers - by class name, optionally
    /// loaded from an assembly referenced elsewhere in the same extension (see <see cref="ExtensionInfo.Assemblies"/>) </summary>
    [Serializable, DataContract, ProtoContract]
    [XmlRoot("ExtensionMainWriterInfo")]
    public class ExtensionMainWriterInfo
    {
        /// <summary> Writer code this main writer answers to (see
        /// <see cref="SobekCM.Core.Navigation.Writer_Codes"/> for the built-in codes) </summary>
        [DataMember(Name = "code")]
        [XmlAttribute("code")]
        [ProtoMember(1)]
        public string Code { get; set; }

        /// <summary> Fully-qualified class name of the main writer, which must extend
        /// <c>abstractMainWriter</c> and expose a public constructor taking (HttpContext, RequestCache) </summary>
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

        /// <summary> First "urlrelative" segment that selects this writer (e.g. "iiif") - empty for a
        /// writer that isn't triggered by a URL segment at all (see <see cref="EarlyExitQueryParam"/>) </summary>
        [DataMember(Name = "urlSegment", EmitDefaultValue = false)]
        [XmlAttribute("urlSegment")]
        [ProtoMember(4)]
        public string UrlSegment { get; set; }

        /// <summary> Query string parameter whose mere presence selects this writer immediately, before any
        /// other query parsing runs (e.g. "verb" for OAI-PMH) - empty for a writer selected by
        /// <see cref="UrlSegment"/> instead </summary>
        [DataMember(Name = "earlyExitQueryParam", EmitDefaultValue = false)]
        [XmlAttribute("earlyExitQueryParam")]
        [ProtoMember(5)]
        public string EarlyExitQueryParam { get; set; }

        /// <summary> Constructor for a new instance of the ExtensionMainWriterInfo class </summary>
        public ExtensionMainWriterInfo()
        {
            // Do nothing.. primarily for serialization purposes
        }
    }
}
