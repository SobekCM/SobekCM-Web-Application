#region Using directives

using ProtoBuf;
using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;

#endregion

namespace SobekCM.Core.Archiving
{
    /// <summary> Flattened information about a single archived file, including which snapshot
    /// and stored copy the row represents </summary>
    /// <remarks> This maps directly to the 'Archive_Get_Item_History_Public' stored procedure's
    /// result set -- one row per (file, snapshot, copy) combination, so the same FileName can
    /// appear more than once if it has more than one stored copy (e.g. GCS and Glacier) </remarks>
    [Serializable, DataContract, ProtoContract]
    [XmlRoot("archivedFile")]
    public class Archived_File
    {
        /// <summary> Primary key for the stable archived file identity </summary>
        [DataMember(Name = "id")]
        [XmlAttribute("id")]
        [ProtoMember(1)]
        public int ArchivedFileID { get; set; }

        /// <summary> Name of the archived file </summary>
        [DataMember(Name = "fileName")]
        [XmlAttribute("fileName")]
        [ProtoMember(2)]
        public string FileName { get; set; }

        /// <summary> Extension of the archived file </summary>
        [DataMember(Name = "fileExtension")]
        [XmlAttribute("fileExtension")]
        [ProtoMember(3)]
        public string FileExtension { get; set; }

        /// <summary> Size, in bytes, of this archived file at the time of this snapshot </summary>
        [DataMember(Name = "fileSize")]
        [XmlAttribute("fileSize")]
        [ProtoMember(4)]
        public long FileSize { get; set; }

        /// <summary> Original filesystem creation date of the file, prior to archiving </summary>
        [DataMember(Name = "originalCreationDate")]
        [XmlAttribute("originalCreationDate")]
        [ProtoMember(5)]
        public DateTime OriginalCreationDate { get; set; }

        /// <summary> Date this particular copy was stored </summary>
        [DataMember(Name = "storedDate")]
        [XmlAttribute("storedDate")]
        [ProtoMember(6)]
        public DateTime StoredDate { get; set; }

        /// <summary> Status of this stored copy ( i.e., Pending, Stored, Verified, Failed, Deleted ) </summary>
        [DataMember(Name = "status")]
        [XmlAttribute("status")]
        [ProtoMember(7)]
        public string Status { get; set; }

        /// <summary> Name of the archive location where this copy is stored ( i.e., 'GCS Cold Storage', 'AWS Glacier' ) </summary>
        [DataMember(Name = "locationName")]
        [XmlAttribute("locationName")]
        [ProtoMember(8)]
        public string LocationName { get; set; }
    }
}
