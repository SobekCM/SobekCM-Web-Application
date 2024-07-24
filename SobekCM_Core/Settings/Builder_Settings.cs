﻿#region Using directives

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using ProtoBuf;
using SobekCM.Core.Builder;

#endregion

namespace SobekCM.Core.Settings
{
    /// <summary> [DataContract] Class stores the all the settings used by the builder </summary>
    [Serializable, DataContract, ProtoContract]
    [XmlRoot("builderSettings")]
    public class Builder_Settings
    {
        /// <summary> Constructor for a new instance of the Builder_Settings class </summary>
        public Builder_Settings()
        {
            // Set some defaults
            Send_Usage_Emails = false;
            Add_PageTurner_ItemViewer = false;
            Verbose_Flag = false;
            Seconds_Between_Polls = 60;

            // Initialized the collections
            IncomingFolders = new List<Builder_Source_Folder>();
            PreProcessModulesSettings = new List<Builder_Module_Setting>();
            PostProcessModulesSettings = new List<Builder_Module_Setting>();
            ItemProcessModulesSettings = new List<Builder_Module_Setting>();
            ItemDeleteModulesSettings = new List<Builder_Module_Setting>();
            ScheduledModulesSettings = new List<Builder_Schedulable_Module_Setting>();
        }
        
        /// <summary> [DataMember] List of all the incoming folders which should be checked for new resources </summary>
        [DataMember(Name = "folders")]
        [XmlArray("folders")]
        [XmlArrayItem("folder", typeof(Builder_Source_Folder))]
        [ProtoMember(1)]
        public List<Builder_Source_Folder> IncomingFolders { get; set; }

        /// <summary> [DataMember] List of modules to run before doing any additional processing </summary>
        [DataMember(Name = "preProcessModules")]
        [XmlArray("preProcessModules")]
        [XmlArrayItem("module", typeof(Builder_Module_Setting))]
        [ProtoMember(2)]
        public List<Builder_Module_Setting> PreProcessModulesSettings { get; set; }

        /// <summary> [DataMember] List of modules to run after doing any additional processing </summary>
        [DataMember(Name = "postProcessModules")]
        [XmlArray("postProcessModules")]
        [XmlArrayItem("module", typeof(Builder_Module_Setting))]
        [ProtoMember(3)]
        public List<Builder_Module_Setting> PostProcessModulesSettings { get; set; }

        /// <summary> [DataMember] List of all the builder modules used for new packages or updates (processed by the builder) </summary>
        [DataMember(Name = "itemProcessModules")]
        [XmlArray("itemProcessModules")]
        [XmlArrayItem("module", typeof(Builder_Module_Setting))]
        [ProtoMember(4)]
        public List<Builder_Module_Setting> ItemProcessModulesSettings { get; set; }

        /// <summary> [DataMember] List of all the builder modules to use for item deletes (processed by the builder) </summary>
        [DataMember(Name = "itemDeleteModules")]
        [XmlArray("itemDeleteModules")]
        [XmlArrayItem("module", typeof(Builder_Module_Setting))]
        [ProtoMember(5)]
        public List<Builder_Module_Setting> ItemDeleteModulesSettings { get; set; }

        /// <summary> [DataMember] List of all the builder modules scheduled to run at certain times </summary>
        [DataMember(Name = "scheduledModules")]
        [XmlArray("scheduledModules")]
        [XmlArrayItem("scheduledModule", typeof(Builder_Schedulable_Module_Setting))]
        [ProtoMember(21)]
        public List<Builder_Schedulable_Module_Setting> ScheduledModulesSettings { get; set; }

        /// <summary> Clear all these settings </summary>
        public virtual void Clear()
        {
            IncomingFolders.Clear();
            PreProcessModulesSettings.Clear();
            PostProcessModulesSettings.Clear();
            ItemProcessModulesSettings.Clear();
            ItemDeleteModulesSettings.Clear();
            ScheduledModulesSettings.Clear();
        }


        /// <summary> Flag indicates if the page turner should be added automatically </summary>
        [DataMember(Name="addPageTurner")]
        [XmlElement("addPageTurner")]
        [ProtoMember(6)]
        public bool Add_PageTurner_ItemViewer { get; set; }

        /// <summary> Flag indicates if the builder should try to convert office files (Word and Powerpoint) to PDF during load and post-processing </summary>
        [DataMember(Name = "convertOfficeFilesToPdf")]
        [XmlElement("convertOfficeFilesToPdf")]
        [ProtoMember(7)]
        public bool Convert_Office_Files_To_PDF { get; set; }

        /// <summary> IIS web log location (usually a network share) for the builder
        /// to read the logs and add the usage statistics to the database </summary>
        [DataMember(Name = "iisLogsDirectory", EmitDefaultValue = false)]
        [XmlElement("iisLogsDirectory")]
        [ProtoMember(8)]
        public string IIS_Logs_Directory { get; set; }

        /// <summary> Number of days builder logs remain before the builder will try to delete it </summary>
        [DataMember(Name = "logExpirationDays")]
        [XmlElement("logExpirationDays")]
        [ProtoMember(9)]
        public int Log_Expiration_Days { get; set; }

        /// <summary> Number of seconds the builder waits between polls </summary>
        [DataMember(Name = "secondsBetweenPolls")]
        [XmlElement("secondsBetweenPolls")]
        [ProtoMember(10)]
        public int Seconds_Between_Polls { get; set; }

        /// <summary> Flag indicates is usage emails should be sent automatically
        /// after the stats usage has been calculated and added to the database </summary>
        [DataMember(Name = "sendUsageEmails")]
        [XmlElement("sendUsageEmails")]
        [ProtoMember(11)]
        public bool Send_Usage_Emails { get; set; }

        /// <summary> Flag indicates if the builder should be extra verbose in the log files (used for debugging purposes mostly) </summary>
        [DataMember(Name = "verboseFlag")]
        [XmlElement("verboseFlag")]
        [ProtoMember(12)]
        public bool Verbose_Flag { get; set; }

        /// <summary> Number of ticks that a complete package must age before being processed </summary>
        /// <value> This is currently set to 15 minutes (in ticks) </value>
        [DataMember(Name = "completePackageRequiredAging")]
        [XmlElement("completePackageRequiredAging")]
        [ProtoMember(13)]
        public long Complete_Package_Required_Aging { get; set; }

        /// <summary> Ghostscript executable file </summary>
        [DataMember(Name = "ghostscriptExecutable", EmitDefaultValue = false)]
        [XmlElement("ghostscriptExecutable")]
        [ProtoMember(14)]
        public string Ghostscript_Executable { get; set; }

        /// <summary> ImageMagick executable file </summary>
        [DataMember(Name = "imageMagickExecutable", EmitDefaultValue = false)]
        [XmlElement("imageMagickExecutable")]
        [ProtoMember(15)]
        public string ImageMagick_Executable { get; set; }

        /// <summary> Kakadu JPEG2000 script will override the specifications used when creating zoomable images </summary>
        [DataMember(Name = "kakaduJp2CreateCommand", EmitDefaultValue = false)]
        [XmlElement("kakaduJp2CreateCommand")]
        [ProtoMember(16)]
        public string Kakadu_JP2_Create_Command { get; set; }

        /// <summary> Returns the network location for the main builder, which runs essentially
        /// without restrictions </summary>
        [DataMember(Name = "mainBuilderInputFolder", EmitDefaultValue = false)]
        [XmlElement("mainBuilderInputFolder")]
        [ProtoMember(17)]
        public string Main_Builder_Input_Folder { get; set; }

        /// <summary> Number of ticks that a metadata only package must age before being processed </summary>
        /// <value> This is currently set to 1 minute (in ticks) </value>
        [DataMember(Name = "metsOnlyPackageRequiredAging")]
        [XmlElement("metsOnlyPackageRequiredAging")]
        [ProtoMember(18)]
        public long METS_Only_Package_Required_Aging { get; set; }

        /// <summary> Command to launch the OCR engine against a single TIFF to produce a single TEXT file </summary>
        [DataMember(Name = "ocrCommandPrompt", EmitDefaultValue = false)]
        [XmlElement("ocrCommandPrompt")]
        [ProtoMember(19)]
        public string OCR_Command_Prompt { get; set; }

        /// <summary> Number of seconds between polls, from the configuration file (not the database) </summary>
        /// <remarks> This is used if the SobekCM Builder is working between multiple instances. If the SobekCM
        /// Builder is only servicing a single instance, then the data can be pulled from the database. </remarks>
        [DataMember(Name = "overrideSecondsBetweenPolls", EmitDefaultValue = false)]
        [XmlElement("overrideSecondsBetweenPolls")]
        [ProtoMember(20)]
        public int? Override_Seconds_Between_Polls { get; set; }

        /// <summary> Flag indicates whether checksums should be verified </summary>
        [XmlIgnore]
        public bool VerifyCheckSum { get; set; }

        #region Methods that controls XML serialization for commonly null or empty values

        /// <summary> Method suppresses XML Serialization of the Override_Seconds_Between_Polls flag property if it is NULL </summary>
        /// <returns> TRUE if the property should be serialized, otherwise FALSE </returns>
        public bool ShouldSerializeOverride_Seconds_Between_Polls()
        {
            return Override_Seconds_Between_Polls.HasValue;
        }

        /// <summary> Method suppresses XML Serialization of the OCR_Command_Prompt flag property if it is NULL or empty </summary>
        /// <returns> TRUE if the property should be serialized, otherwise FALSE </returns>
        public bool ShouldSerializeOCR_Command_Prompt()
        {
            return !String.IsNullOrEmpty(OCR_Command_Prompt);
        }

        /// <summary> Method suppresses XML Serialization of the IncomingFolders collection if it is empty </summary>
        /// <returns> TRUE if the property should be serialized, otherwise FALSE </returns>
        public bool ShouldSerializeIncomingFolders()
        {
            return ((IncomingFolders != null) && (IncomingFolders.Count > 0));
        }


        /// <summary> Method suppresses XML Serialization of the PreProcessModulesSettings collection if it is empty </summary>
        /// <returns> TRUE if the property should be serialized, otherwise FALSE </returns>
        public bool ShouldSerializePreProcessModulesSettings()
        {
            return ((PreProcessModulesSettings != null) && (PreProcessModulesSettings.Count > 0));
        }


        /// <summary> Method suppresses XML Serialization of the PostProcessModulesSettings collection if it is empty </summary>
        /// <returns> TRUE if the property should be serialized, otherwise FALSE </returns>
        public bool ShouldSerializePostProcessModulesSettings()
        {
            return ((PostProcessModulesSettings != null) && (PostProcessModulesSettings.Count > 0));
        }


        /// <summary> Method suppresses XML Serialization of the ItemProcessModulesSettings collection if it is empty </summary>
        /// <returns> TRUE if the property should be serialized, otherwise FALSE </returns>
        public bool ShouldSerializeItemProcessModulesSettings()
        {
            return ((ItemProcessModulesSettings != null) && (ItemProcessModulesSettings.Count > 0));
        }


        /// <summary> Method suppresses XML Serialization of the ItemDeleteModulesSettings collection if it is empty </summary>
        /// <returns> TRUE if the property should be serialized, otherwise FALSE </returns>
        public bool ShouldSerializeItemDeleteModulesSettings()
        {
            return ((ItemDeleteModulesSettings != null) && (ItemDeleteModulesSettings.Count > 0));
        }

        #endregion

    }
}
