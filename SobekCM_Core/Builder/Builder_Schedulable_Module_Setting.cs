#region Using directives

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using ProtoBuf;

#endregion

namespace SobekCM.Core.Builder
{
    /// <summary> Setting information for a single schedulable builder module </summary>
    [Serializable, DataContract, ProtoContract]
    [XmlRoot("builderSchedulableModule")]
    public class Builder_Schedulable_Module_Setting : Builder_Module_Setting
    {
        /// <summary> Schedules of when this module should be run </summary>
        [DataMember(Name = "schedules")]
        [XmlArray("schedules")]
        [XmlArrayItem("schedule", typeof(Builder_Module_Schedule))]
        [ProtoMember(8)]
        public List<Builder_Module_Schedule> Schedules { get; set; }
    }
}
