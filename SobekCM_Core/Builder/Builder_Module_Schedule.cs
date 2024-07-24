#region Using directives

using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using ProtoBuf;

#endregion


namespace SobekCM.Core.Builder
{
    /// <summary> Information about a single schedule for a builder module </summary>
    [Serializable, DataContract, ProtoContract]
    [XmlRoot("builderModuleSchedule")]
    public class Builder_Module_Schedule
    {
        /// <summary> [DataMember] Days of the week this module should run (on this schedule) </summary>
        [DataMember(Name = "daysOfWeek")]
        [XmlAttribute("daysOfWeek")]
        [ProtoMember(1)]
        public string DaysOfWeek { get; set; }

        /// <summary> [DataMember] Time of day this module should run (on this schedule) </summary>
        [DataMember(Name = "timeOfDay")]
        [XmlAttribute("timeOfDay")]
        [ProtoMember(2)]
        public string TimeOfDay { get; set; }

        /// <summary> [DataMember] Last time this module was run (on this schedule?) </summary>
        [DataMember(Name = "lastRun")]
        [XmlAttribute("lastRun")]
        [ProtoMember(3)]
        public DateTime? LastRun { get; set; }

        /// <summary> Method suppresses XML Serialization of the LastRun property if it is NULL </summary>
        /// <returns> TRUE if the property should be serialized, otherwise FALSE </returns>
        public bool ShouldSerializeLastRun()
        {
            return LastRun.HasValue;
        }
    }
}
