using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace SobekCM.Core.UI_Configuration.MaterializeClasses
{
    /// <summary> Class holds all the Materialize classes values for the user interface to use </summary>
    [Serializable, DataContract, ProtoContract]
    [XmlRoot("MaterializeClassesConfig")]
    public class MaterializeClasses_Configuration
    {
        public MaterializeClasses_Configuration()
        {
            Materialize_Classes = new Dictionary<string, string>();
        }

        public Dictionary<string, string> Materialize_Classes { get; set; }
    }
}
