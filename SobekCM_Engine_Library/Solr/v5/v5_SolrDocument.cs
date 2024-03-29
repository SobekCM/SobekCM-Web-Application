﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SobekCM.Resource_Object.Solr;
using SolrNet.Attributes;

namespace SobekCM.Engine_Library.Solr.v5
{
    /// <summary> Represents a solr document in the version 5.0 and on solr/lucene indexes </summary>
    public class v5_SolrDocument
    {
        /// <summary> Constructor for a new instance of the v5_SolrDocument class </summary>
        public v5_SolrDocument()
        {
            // Set default title and type
            Title = "Missing Title";
            Type = "Unkown";
            AdditionalTextFiles = new List<string>();

        }

        /// <summary> List of all the additional text files to include </summary>
        public List<string> AdditionalTextFiles { get; set; }

        /// <summary> Location of all the files </summary>
        public string FileLocation { get; set; }

        /// <summary> Gets the collection of page objects for Solr indexing </summary>
        public List<Legacy_SolrPage> Solr_Pages { get; set; }

        /// <summary> Returns the full text for all the pages within this document for the Solr engine to index for this document </summary>
        [SolrField("fulltext")]
        public string FullText
        {
            get
            {
                if (((Solr_Pages == null) || (Solr_Pages.Count == 0)) && (AdditionalTextFiles.Count == 0))
                    return null;

                StringBuilder builder = new StringBuilder(10000);

                // Add the text for each page 
                if (Solr_Pages != null)
                {
                    foreach (Legacy_SolrPage thisPage in Solr_Pages)
                    {
                        builder.Append(thisPage.PageText);
                        builder.Append(" ");
                    }
                }

                // Also add the text from any other text files
                if (AdditionalTextFiles != null)
                {
                    foreach (string textFile in AdditionalTextFiles)
                    {
                        try
                        {
                            StreamReader reader = new StreamReader(FileLocation + "\\" + textFile);
                            builder.Append(reader.ReadToEnd() + " ");
                            reader.Close();
                        }
                        catch
                        {
                            // do nothing
                        }
                    }
                }

                return builder.ToString();
            }
        }

        /// <summary> Unique key for this document</summary>
        [SolrUniqueKey("did")]
        public string DID { get; set; }

        #region Administrative Fields

        /// <summary> List of aggregations to which this item is linked </summary>
        [SolrField("aggregations")]
        public List<string> Aggregations { get; set; }

        /// <summary> Bibliographic identifier for this title (multiple volumes potentially) </summary>
        [SolrField("bibid")]
        public string BibID { get; set; }

        /// <summary> Volume identifier </summary>
        [SolrField("vid")]
        public string VID { get; set; }

        /// <summary> Main thumbnail for this item </summary>
        [SolrField("mainthumb")]
        public string MainThumbnail { get; set; }

        /// <summary> Main thumbnail for this item </summary>
        [SolrField("restrictedthumb")]
        public string RestrictedThumbnail { get; set; }

        /// <summary> Main thumbnail for this item </summary>
        [SolrField("made_public_date")]
        public DateTime MadePublicDate { get; set; }

        /// <summary> List of groups which have access to this item (or blank if no group-level restrictions) </summary>
        [SolrField("group_restrictions")]
        public string Group_Restrictions { get; set; }

        /// <summary> List of instances (by code) to which this item should appear </summary>
        [SolrField("instance")]
        public List<string> Instances { get; set; }

        #endregion

        #region Serial hierarchy fields

        /// <summary> Text for the serial hierarchy level 1 for this item </summary>
        [SolrField("level1text")]
        public string Level1_Text { get; set; }

        /// <summary> Display for the text for the serial hierarchy level 1 for this item </summary>
        [SolrField("level1text.display")]
        public string Level1_Text_Display { get; set; }

        /// <summary> Index for the serial hierarchy level 1 for this item </summary>
        [SolrField("level1index")]
        public int Level1_Index { get; set; }

        /// <summary> Facet for the serial hierarchy level 1 for this item, which merges the index and text ( i.e., '00001|January') </summary>
        [SolrField("level1facet")]
        public string Level1_Facet { get; set; }

        /// <summary> Text for the serial hierarchy level 2 for this item </summary>
        [SolrField("level2text")]
        public string Level2_Text { get; set; }

        /// <summary> Display for the text for the serial hierarchy level 2 for this item </summary>
        [SolrField("level2text.display")]
        public string Level2_Text_Display { get; set; }

        /// <summary> Index for the serial hierarchy level 2 for this item </summary>
        [SolrField("level2index")]
        public int Level2_Index { get; set; }

        /// <summary> Facet for the serial hierarchy level 2 for this item, which merges the index and text ( i.e., '00001|January') </summary>
        [SolrField("level2facet")]
        public string Level2_Facet { get; set; }

        /// <summary> Text for the serial hierarchy level 3 for this item </summary>
        [SolrField("level3text")]
        public string Level3_Text { get; set; }

        /// <summary> Display for the text for the serial hierarchy level 3 for this item </summary>
        [SolrField("level3text.display")]
        public string Level3_Text_Display { get; set; }

        /// <summary> Index for the serial hierarchy level 3 for this item </summary>
        [SolrField("level3index")]
        public int Level3_Index { get; set; }

        /// <summary> Facet for the serial hierarchy level 3 for this item, which merges the index and text ( i.e., '00001|January') </summary>
        [SolrField("level3facet")]
        public string Level3_Facet { get; set; }

        /// <summary> Text for the serial hierarchy level 4 for this item </summary>
        [SolrField("level4text")]
        public string Level4_Text { get; set; }

        /// <summary> Display for the text for the serial hierarchy level 4 for this item </summary>
        [SolrField("level4text.display")]
        public string Level4_Text_Display { get; set; }

        /// <summary> Index for the serial hierarchy level 4 for this item </summary>
        [SolrField("level4index")]
        public int Level4_Index { get; set; }

        /// <summary> Facet for the serial hierarchy level 4 for this item, which merges the index and text ( i.e., '00001|January') </summary>
        [SolrField("level4facet")]
        public string Level4_Facet { get; set; }

        /// <summary> Text for the serial hierarchy level 5 for this item </summary>
        [SolrField("level5text")]
        public string Level5_Text { get; set; }

        /// <summary> Display for the text for the serial hierarchy level 5 for this item </summary>
        [SolrField("level5text.display")]
        public string Level5_Text_Display { get; set; }

        /// <summary> Index for the serial hierarchy level 5 for this item </summary>
        [SolrField("level5index")]
        public int Level5_Index { get; set; }

        /// <summary> Facet for the serial hierarchy level 5 for this item, which merges the index and text ( i.e., '00001|January') </summary>
        [SolrField("level5facet")]
        public string Level5_Facet { get; set; }

        #endregion

        #region Authority system ID's for use in the local authority system

        /// <summary> Creator authority ids related to this digital resource </summary>
        [SolrField("creator.authid")]
        public List<int> Creator_AuthID { get; set; }

        /// <summary> Publisher authority ids related to this digital resource </summary>
        [SolrField("publisher.authid")]
        public List<int> Publisher_AuthID { get; set; }

        /// <summary> Subject authority ids related to this digital resource </summary>
        [SolrField("subject.authid")]
        public List<int> Subject_AuthID { get; set; }

        /// <summary> Spatial authority ids related to this digital resource </summary>
        [SolrField("spatial.authid")]
        public List<int> Spatial_AuthID { get; set; }

        /// <summary> Temporal authority ids related to this digital resource </summary>
        [SolrField("temporal.authid")]
        public List<int> Temporal_AuthID { get; set; }

        #endregion

        #region Some basic behavior fields related to discoverability and access

        /// <summary> UserIDs which can discover this digital resource ( or -1 otherwise ) </summary>
        [SolrField("discover_users")]
        public List<int> Discover_Users { get; set; }

        /// <summary> GroupIDs which can discover this digital resource ( or -1 otherwise ) </summary>
        [SolrField("discover_groups")]
        public List<int> Discover_Groups { get; set; }

        /// <summary> Primary keys to the IP address ranges which can discover this digital resource ( or -1 otherwise ) </summary>
        [SolrField("discover_ips")]
        public List<int> Discover_IPs { get; set; }

        /// <summary> Hidden flag indicates if this digital resource is completely hidden, not to be publicly discovered </summary>
        /// <remarks> Admins can get access to discover these items if they use the 'View private items' checkbox </remarks>
        [SolrField("hidden")]
        public bool Hidden { get; set; }

        /// <summary> Restricted message to display with this item - generally describes accessibility permissions </summary>
        [SolrField("restricted_msg")]
        public string RestrictedMsg { get; set; }

        /// <summary> Flag indicates that this item has user or user group restrictions </summary>
        [SolrField("restricted_access")]
        public bool Restricted { get; set; }

        #endregion

        #region Main metadata fields

        /// <summary> Main title for this document </summary>
        [SolrField("title")]
        public string Title { get; set; }

        /// <summary> Sort title for this document </summary>
        [SolrField("title.sort")]
        public string SortTitle { get; set; }

        /// <summary> Alternate titles for this document </summary>
        [SolrField("alttitle")]
        public List<string> AltTitle { get; set; }

        /// <summary> Translated titles for this document (mostly used for display purposes)</summary>
        [SolrField("transtitle")]
        public List<string> TranslatedTitle { get; set; }

        /// <summary> Series titles for this document </summary>
        [SolrField("seriestitle")]
        public string SeriesTitle { get; set; }

        /// <summary> Course title for this document </summary>
        [SolrField("coursetitle")]
        public string CourseTitle { get; set; }

        /// <summary> Overall resource type for this document </summary>
        [SolrField("type")]
        public string Type { get; set; }

        /// <summary> Languages for this document </summary>
        [SolrField("language")]
        public List<string> Language { get; set; }

        /// <summary> Creators (and contributors) for this document </summary>
        [SolrField("creator")]
        public List<string> Creator { get; set; }

        /// <summary> Creators (and contributors) for this document for display purposes, which includes the role </summary>
        [SolrField("creator.display")]
        public List<string> Creator_Display { get; set; }

        /// <summary> Affiliations for this document </summary>
        [SolrField("affiliation")]
        public List<string> Affiliation { get; set; }

        /// <summary> Display version of the affiliations for this document </summary>
        [SolrField("affiliation.display")]
        public List<string> AffiliationDisplay { get; set; }

        /// <summary> Publishers for this document </summary>
        [SolrField("publisher")]
        public List<string> Publisher { get; set; }

        /// <summary> Publisher (display format which includes publication place, etc..) for this document </summary>
        [SolrField("publisher.display")]
        public List<string> Publisher_Display { get; set; }

        /// <summary> Publication places for this document </summary>
        [SolrField("publication_place")]
        public List<string> PubPlace { get; set; }

        /// <summary> Audiences for this document </summary>
        [SolrField("audience")]
        public List<string> Audience { get; set; }

        /// <summary> Source institution (and location) for this document </summary>
        [SolrField("source")]
        public string Source { get; set; }

        /// <summary> Holding location for this document </summary>
        [SolrField("holding")]
        public string Holding { get; set; }

        /// <summary> Identifiers for this document </summary>
        [SolrField("identifier")]
        public List<string> Identifier { get; set; }

        /// <summary> Display version of the identifiers for this document, including identifier type </summary>
        [SolrField("identifier.display")]
        public List<string> IdentifierDisplay { get; set; }

        /// <summary> Notes for this document </summary>
        [SolrField("notes")]
        public List<string> Notes { get; set; }

        /// <summary> Ticklers for this document </summary>
        [SolrField("tickler")]
        public List<string> Tickler { get; set; }

        /// <summary> Donor for this document </summary>
        [SolrField("donor")]
        public string Donor { get; set; }

        /// <summary> Format for this document </summary>
        [SolrField("format")]
        public string Format { get; set; }

        /// <summary> Frequencies for this document </summary>
        [SolrField("frequency")]
        public List<string> Frequency { get; set; }

        /// <summary> Genres for this document </summary>
        [SolrField("genre")]
        public List<string> Genre { get; set; }

        /// <summary> Display version of the genres for this document including the authority </summary>
        [SolrField("genre.display")]
        public List<string> GenreDisplay { get; set; }

        /// <summary> Other citation fields </summary>
        [SolrField("other")]
        public List<string> OtherCitation { get; set; }

        #endregion

        #region Date metadata fields

        /// <summary> Date this material was published </summary>
        [SolrField("date")]
        public string Date { get; set; }

        /// <summary> Display version of the date this material was published </summary>
        [SolrField("date.display")]
        public string DateDisplay { get; set; }

        [SolrField("date.gregorian")]
        public DateTime? GregorianDate { get; set; }

        /// <summary> Year this material was published </summary>
        [SolrField("date.year")]
        public string DateYear { get; set; }

        #endregion

        #region Subject metadata fields

        /// <summary> Subjects and subject keywords for this document </summary>
        [SolrField("subject")]
        public List<string> Subject { get; set; }

        /// <summary> DIsplay version of subjects and subject keywords for this document </summary>
        [SolrField("subject.display")]
        public List<string> SubjectDisplay { get; set; }

        /// <summary> Name (corporate or personal) as subject for this document </summary>
        [SolrField("name_as_subject")]
        public List<string> NameAsSubject { get; set; }

        /// <summary> Display version of name (corporate or personal) as subject for this document </summary>
        [SolrField("name_as_subject.display")]
        public List<string> NameAsSubjectDisplay { get; set; }

        /// <summary> Title of a work as subject for this document </summary>
        [SolrField("title_as_subject")]
        public List<string> TitleAsSubject { get; set; }

        /// <summary> Dispay version of title of a work as subject for this document </summary>
        [SolrField("title_as_subject.display")]
        public List<string> TitleAsSubjectDisplay { get; set; }

        #endregion

        #region Spatial metadata fields

        /// <summary> Standard spatial subjects for this document </summary>
        [SolrField("spatial_standard")]
        public List<string> Spatial { get; set; }

        /// <summary> KML spatial footprint to display this item on a map </summary>
        [SolrField("spatial_footprint_kml")]
        public List<string> SpatialFootprintKml { get; set; }

        /// <summary> Distance of the spatial footprint for this item on the map </summary>
        [SolrField("spatial_footprint_distance")]
        public int SpatialDistance { get; set; }

        /// <summary> Display version of the standard spatial subjects for this document </summary>
        [SolrField("spatial_standard.display")]
        public List<string> SpatialDisplay { get; set; }

        /// <summary> Hierarchical spatial subjects for this document </summary>
        /// <remarks> Some individual components are also broken out below </remarks>
        [SolrField("spatial_hierarchical")]
        public List<string> HierarchicalSpatial { get; set; }

        /// <summary> Country spatial keywords for this document </summary>
        [SolrField("country")]
        public List<string> Country { get; set; }

        /// <summary> State spatial keywords for this document </summary>
        [SolrField("state")]
        public List<string> State { get; set; }

        /// <summary> County spatial keywords for this document </summary>
        [SolrField("county")]
        public List<string> County { get; set; }

        /// <summary> City spatial keywords for this document </summary>
        [SolrField("city")]
        public List<string> City { get; set; }

        #endregion

        #region Temporal subject fields


        /// <summary> Temporal subject term for indexing purposes, which only includes the term ( i.e., 'World War 2') </summary>
        [SolrField("temporal_subject")]
        public string TemporalSubject { get; set; }

        /// <summary> Temporal subject term for display purposes ( i.e., 'World War 2 ( 1940-1944)' ) </summary>
        [SolrField("temporal_subject.display")]
        public string TemporalSubjectDisplay { get; set; }

        /// <summary> If decades can be determined from the temporal date, include them here (Gregorian decades) </summary>
        [SolrField("temporal_decade")]
        public string TemporalDecade { get; set; }

        /// <summary> If years can can be determined from the temporal year, include them here (Gregorian year) </summary>
        [SolrField("temporal_year")]
        public string TemporalYear { get; set; }

        /// <summary> If a single (Gregorian) date can be determined from the temporal data, it is included here </summary>
        [SolrField("temporal_date")]
        public DateTime? TemporalDate { get; set; }

        /// <summary> A single (Gregorian) date for this item, which will be temporal year (if present) or publication date (if present) </summary>
        [SolrField("timeline_date")]
        public DateTime? TimelineDate { get; set; }

        /// <summary> Display version of the timeline date for this item, which will be temporal year (if present) or publication date (if present) </summary>
        [SolrField("timeline_date.display")]
        public string TimelineDateDisplay { get; set; }

        #endregion

        #region Other standard fields 

        /// <summary> Accessibility for this document</summary>
        [SolrField("accessibility")]
        public string Accessibility { get; set; }

        /// <summary> System requirements associated with this document </summary>
        [SolrField("system_requirements")]
        public List<string> SystemRequirements { get; set; }

        /// <summary> Licensing associated with this document </summary>
        [SolrField("licensing")]
        public List<string> Licensing { get; set; }

        /// <summary> Attribution for this document</summary>
        [SolrField("attribution")]
        public string Attribution { get; set; }

        /// <summary> MIME types associated with this document </summary>
        [SolrField("mime_type")]
        public List<string> MimeType { get; set; }

        /// <summary> Tracking box for this document </summary>
        [SolrField("tracking_box")]
        public List<string> TrackingBox { get; set; }

        /// <summary> Abstract for this document </summary>
        [SolrField("abstract")]
        public List<string> Abstract { get; set; }

        /// <summary> Edition for this document</summary>
        [SolrField("edition")]
        public string Edition { get; set; }

        /// <summary> Table of contents text for this document </summary>
        [SolrField("toc")]
        public List<string> TableOfContents { get; set; }

        /// <summary> Accession Number for this document</summary>
        [SolrField("accession_number")]
        public List<string> AccessionNumber { get; set; }

        /// <summary> Display version of the accession number for this document</summary>
        [SolrField("accession_number.display")]
        public string AccessionNumberDisplay { get; set; }

        #endregion

        #region Performing Arts metadata fields

        /// <summary> Name of the performance </summary>
        [SolrField("performance")]
        public string Performance { get; set; }

        /// <summary> Name of the performance </summary>
        [SolrField("performance.display")]
        public string PerformanceDisplay { get; set; }

        /// <summary> Date of the performance </summary>
        [SolrField("performance_date")]
        public string PerformanceDate { get; set; }

        /// <summary> Performer from a performance </summary>
        [SolrField("performer")]
        public List<string> Performer { get; set; }

        /// <summary> Performer from a performance </summary>
        [SolrField("performer.display")]
        public List<string> PerformerDisplay { get; set; }

        #endregion

        #region Oral history metadata fields 

        /// <summary> Interviewee with this oral history interview </summary>
        [SolrField("interviewee")]
        public List<string> Interviewee { get; set; }

        /// <summary> Interviewer with this oral history interview </summary>
        [SolrField("interviewer")]
        public List<string> Interviewer { get; set; }

        #endregion

        #region VRA Core(visual resource) metadata fields 
        
        /// <summary> Measurements VRACore information for this resource </summary>
        [SolrField("measurements")]
        public List<string> Measurements { get; set; }

        /// <summary> Display version of the measurements VRACore information for this resource </summary>
        [SolrField("measurements.display")]
        public List<string> MeasurementsDisplay { get; set; }

        /// <summary> Cultural context VRACore information for this resource </summary>
        [SolrField("cultural_context")]
        public List<string> CulturalContext { get; set; }

        /// <summary> Inscription VRACore information for this resource </summary>
        [SolrField("inscription")]
        public List<string> Inscription { get; set; }

        /// <summary> Material VRACore information for this resource </summary>
        [SolrField("material")]
        public List<string> Material { get; set; }

        /// <summary> Display version of the material VRACore information for this resource </summary>
        [SolrField("material.display")]
        public List<string> MaterialDisplay { get; set; }

        /// <summary> Style / Period VRACore information for this resource </summary>
        [SolrField("style_period")]
        public List<string> StylePeriod { get; set; }

        /// <summary> Technique VRACore information for this resource </summary>
        [SolrField("technique")]
        public List<string> Technique { get; set; }

        #endregion

        #region Zoological Taxonomy metadata fields 

        /// <summary> Complete hierarchical zoological taxonomic (DarwinCore) data for this resource </summary>
        [SolrField("zt_hierarchical")]
        public List<string> ZoologicalHierarchical { get; set; }

        /// <summary> Kingdom zoological taxonomic (DarwinCore) data for this resource </summary>
        [SolrField("zt_kingdom")]
        public List<string> ZoologicalKingdom { get; set; }

        /// <summary> Phylum zoological taxonomic (DarwinCore) data for this resource </summary>
        [SolrField("zt_phylum")]
        public List<string> ZoologicalPhylum { get; set; }

        /// <summary> Class zoological taxonomic (DarwinCore) data for this resource </summary>
        [SolrField("zt_class")]
        public List<string> ZoologicalClass { get; set; }

        /// <summary> Order zoological taxonomic (DarwinCore) data for this resource </summary>
        [SolrField("zt_order")]
        public List<string> ZoologicalOrder { get; set; }

        /// <summary> Family zoological taxonomic (DarwinCore) data for this resource </summary>
        [SolrField("zt_family")]
        public List<string> ZoologicalFamily { get; set; }

        /// <summary> Genus zoological taxonomic (DarwinCore) data for this resource </summary>
        [SolrField("zt_genus")]
        public List<string> ZoologicalGenus { get; set; }

        /// <summary> Species zoological taxonomic (DarwinCore) data for this resource </summary>
        [SolrField("zt_species")]
        public List<string> ZoologicalSpecies { get; set; }

        /// <summary> Common name zoological taxonomic (DarwinCore) data for this resource </summary>
        [SolrField("zt_common_name")]
        public List<string> ZoologicalCommonName { get; set; }

        /// <summary> zt_scientific_name zoological taxonomic (DarwinCore) data for this resource </summary>
        [SolrField("zt_scientific_name")]
        public List<string> ZoologicalScientificName { get; set; }

        #endregion

        #region (Electronic) Thesis and Dissertation metadata fields 

        /// <summary> Committee members information for this thesis/dissertation resource </summary>
        [SolrField("etd_committee")]
        public List<string> EtdCommittee { get; set; }

        /// <summary> Degree information for this thesis/dissertation resource </summary>
        [SolrField("etd_degree")]
        public string EtdDegree { get; set; }

        /// <summary> Degree discipline information for this thesis/dissertation resource </summary>
        [SolrField("etd_degree_discipline")]
        public string EtdDegreeDiscipline { get; set; }

        /// <summary> Degree grantor information for this thesis/dissertation resource </summary>
        [SolrField("etd_degree_grantor")]
        public string EtdDegreeGrantor { get; set; }

        /// <summary> Degree level ( i.e., masters, doctorate, etc.. ) information for this thesis/dissertation resource </summary>
        [SolrField("etd_degree_level")]
        public string EtdDegreeLevel { get; set; }

        /// <summary> Degree division information for this thesis/dissertation resource </summary>
        [SolrField("etd_degree_division")]
        public string EtdDegreeDivision { get; set; }

        #endregion

        #region Learning Object metadata fields 

        /// <summary> Aggregation information for this learning object resource </summary>
        [SolrField("lom_aggregation")]
        public string LomAggregation { get; set; }

        /// <summary> Context information for finding this learning object resource </summary>
        [SolrField("lom_context")]
        public List<string> LomContext { get; set; }

        /// <summary> Context information for displaying this learning object resource </summary>
        [SolrField("lom_context.display")]
        public List<string> LomContextDisplay { get; set; }

        /// <summary> Classification information for this learning object resource </summary>
        [SolrField("lom_classification")]
        public List<string> LomClassification { get; set; }

        /// <summary> Difficulty information for this learning object resource </summary>
        [SolrField("lom_difficulty")]
        public string LomDifficulty { get; set; }

        /// <summary> Intended end user information for this learning object resource </summary>
        [SolrField("lom_intended_end_user")]
        public List<string> LomIntendedEndUser { get; set; }

        /// <summary> Display version of all the intended end user roles for this learning object resource </summary>
        [SolrField("lom_intended_end_user.display")]
        public string LomIntendedEndUserDisplay { get; set; }

        /// <summary> Interactivity level information for this learning object resource </summary>
        [SolrField("lom_interactivity_level")]
        public string LomInteractivityLevel { get; set; }

        /// <summary> Interactivity type information for this learning object resource </summary>
        [SolrField("lom_interactivity_type")]
        public string LomInteractivityType { get; set; }

        /// <summary> Status information for this learning object resource </summary>
        [SolrField("lom_status")]
        public string LomStatus { get; set; }

        /// <summary> System requirements information for this learning object resource </summary>
        [SolrField("lom_requirement")]
        public List<string> LomRequirement { get; set; }

        /// <summary> Display version of the system requirements information for this learning object resource </summary>
        [SolrField("lom_requirement.display")]
        public List<string> LomRequirementDisplay { get; set; }

        /// <summary> Age range information for this learning object resource </summary>
        [SolrField("lom_age_range")]
        public List<string> LomAgeRange { get; set; }

        /// <summary> Resource type information for this learning object resource </summary>
        [SolrField("lom_resource_type")]
        public List<string> LomResourceType { get; set; }

        /// <summary> Display version of the resource type information for this learning object resource </summary>
        [SolrField("lom_resource_type.display")]
        public List<string> LomResourceTypeDisplay { get; set; }

        /// <summary> Learning time information for this learning object resource </summary>
        [SolrField("lom_learning_time")]
        public string LomLearningTime { get; set; }

        #endregion

        #region User defined metadata fields, used by plug-ins, etc.. 

        /// <summary> User defined metadata field (#1) for this learning object resource </summary>
        [SolrField("user_defined_01")]
        public List<string> UserDefined01 { get; set; }

        /// <summary> User defined metadata field (#2) for this learning object resource </summary>
        [SolrField("user_defined_02")]
        public List<string> UserDefined02 { get; set; }

        /// <summary> User defined metadata field (#3) for this learning object resource </summary>
        [SolrField("user_defined_03")]
        public List<string> UserDefined03 { get; set; }

        /// <summary> User defined metadata field (#4) for this learning object resource </summary>
        [SolrField("user_defined_04")]
        public List<string> UserDefined04 { get; set; }

        /// <summary> User defined metadata field (#5) for this learning object resource </summary>
        [SolrField("user_defined_05")]
        public List<string> UserDefined05 { get; set; }

        /// <summary> User defined metadata field (#6) for this learning object resource </summary>
        [SolrField("user_defined_06")]
        public List<string> UserDefined06 { get; set; }

        /// <summary> User defined metadata field (#7) for this learning object resource </summary>
        [SolrField("user_defined_07")]
        public List<string> UserDefined07 { get; set; }

        /// <summary> User defined metadata field (#8) for this learning object resource </summary>
        [SolrField("user_defined_08")]
        public List<string> UserDefined08 { get; set; }

        /// <summary> User defined metadata field (#9) for this learning object resource </summary>
        [SolrField("user_defined_09")]
        public List<string> UserDefined09 { get; set; }

        /// <summary> User defined metadata field (#10) for this learning object resource </summary>
        [SolrField("user_defined_10")]
        public List<string> UserDefined10 { get; set; }

        /// <summary> User defined metadata field (#11) for this learning object resource </summary>
        [SolrField("user_defined_11")]
        public List<string> UserDefined11 { get; set; }

        /// <summary> User defined metadata field (#12) for this learning object resource </summary>
        [SolrField("user_defined_12")]
        public List<string> UserDefined12 { get; set; }

        /// <summary> User defined metadata field (#13) for this learning object resource </summary>
        [SolrField("user_defined_13")]
        public List<string> UserDefined13 { get; set; }

        /// <summary> User defined metadata field (#14) for this learning object resource </summary>
        [SolrField("user_defined_14")]
        public List<string> UserDefined14 { get; set; }

        /// <summary> User defined metadata field (#15) for this learning object resource </summary>
        [SolrField("user_defined_15")]
        public List<string> UserDefined15 { get; set; }

        /// <summary> User defined metadata field (#16) for this learning object resource </summary>
        [SolrField("user_defined_16")]
        public List<string> UserDefined16 { get; set; }

        /// <summary> User defined metadata field (#17) for this learning object resource </summary>
        [SolrField("user_defined_17")]
        public List<string> UserDefined17 { get; set; }

        /// <summary> User defined metadata field (#18) for this learning object resource </summary>
        [SolrField("user_defined_18")]
        public List<string> UserDefined18 { get; set; }

        /// <summary> User defined metadata field (#19) for this learning object resource </summary>
        [SolrField("user_defined_19")]
        public List<string> UserDefined19 { get; set; }

        /// <summary> User defined metadata field (#20) for this learning object resource </summary>
        [SolrField("user_defined_20")]
        public List<string> UserDefined20 { get; set; }

        /// <summary> User defined metadata field (#21) for this learning object resource </summary>
        [SolrField("user_defined_21")]
        public List<string> UserDefined21 { get; set; }

        /// <summary> User defined metadata field (#22) for this learning object resource </summary>
        [SolrField("user_defined_22")]
        public List<string> UserDefined22 { get; set; }

        /// <summary> User defined metadata field (#23) for this learning object resource </summary>
        [SolrField("user_defined_23")]
        public List<string> UserDefined23 { get; set; }

        /// <summary> User defined metadata field (#24) for this learning object resource </summary>
        [SolrField("user_defined_24")]
        public List<string> UserDefined24 { get; set; }

        /// <summary> User defined metadata field (#25) for this learning object resource </summary>
        [SolrField("user_defined_25")]
        public List<string> UserDefined25 { get; set; }

        /// <summary> User defined metadata field (#26) for this learning object resource </summary>
        [SolrField("user_defined_26")]
        public List<string> UserDefined26 { get; set; }

        /// <summary> User defined metadata field (#27) for this learning object resource </summary>
        [SolrField("user_defined_27")]
        public List<string> UserDefined27 { get; set; }

        /// <summary> User defined metadata field (#28) for this learning object resource </summary>
        [SolrField("user_defined_28")]
        public List<string> UserDefined28 { get; set; }

        /// <summary> User defined metadata field (#29) for this learning object resource </summary>
        [SolrField("user_defined_29")]
        public List<string> UserDefined29 { get; set; }

        /// <summary> User defined metadata field (#30) for this learning object resource </summary>
        [SolrField("user_defined_30")]
        public List<string> UserDefined30 { get; set; }

        /// <summary> User defined metadata field (#31) for this learning object resource </summary>
        [SolrField("user_defined_31")]
        public List<string> UserDefined31 { get; set; }

        /// <summary> User defined metadata field (#32) for this learning object resource </summary>
        [SolrField("user_defined_32")]
        public List<string> UserDefined32 { get; set; }

        /// <summary> User defined metadata field (#33) for this learning object resource </summary>
        [SolrField("user_defined_33")]
        public List<string> UserDefined33 { get; set; }

        /// <summary> User defined metadata field (#34) for this learning object resource </summary>
        [SolrField("user_defined_34")]
        public List<string> UserDefined34 { get; set; }

        /// <summary> User defined metadata field (#35) for this learning object resource </summary>
        [SolrField("user_defined_35")]
        public List<string> UserDefined35 { get; set; }

        /// <summary> User defined metadata field (#36) for this learning object resource </summary>
        [SolrField("user_defined_36")]
        public List<string> UserDefined36 { get; set; }

        /// <summary> User defined metadata field (#37) for this learning object resource </summary>
        [SolrField("user_defined_37")]
        public List<string> UserDefined37 { get; set; }

        /// <summary> User defined metadata field (#38) for this learning object resource </summary>
        [SolrField("user_defined_38")]
        public List<string> UserDefined38 { get; set; }

        /// <summary> User defined metadata field (#39) for this learning object resource </summary>
        [SolrField("user_defined_39")]
        public List<string> UserDefined39 { get; set; }

        /// <summary> User defined metadata field (#40) for this learning object resource </summary>
        [SolrField("user_defined_40")]
        public List<string> UserDefined40 { get; set; }

        /// <summary> User defined metadata field (#41) for this learning object resource </summary>
        [SolrField("user_defined_41")]
        public List<string> UserDefined41 { get; set; }

        /// <summary> User defined metadata field (#42) for this learning object resource </summary>
        [SolrField("user_defined_42")]
        public List<string> UserDefined42 { get; set; }

        /// <summary> User defined metadata field (#43) for this learning object resource </summary>
        [SolrField("user_defined_43")]
        public List<string> UserDefined43 { get; set; }

        /// <summary> User defined metadata field (#44) for this learning object resource </summary>
        [SolrField("user_defined_44")]
        public List<string> UserDefined44 { get; set; }

        /// <summary> User defined metadata field (#45) for this learning object resource </summary>
        [SolrField("user_defined_45")]
        public List<string> UserDefined45 { get; set; }

        /// <summary> User defined metadata field (#46) for this learning object resource </summary>
        [SolrField("user_defined_46")]
        public List<string> UserDefined46 { get; set; }

        /// <summary> User defined metadata field (#47) for this learning object resource </summary>
        [SolrField("user_defined_47")]
        public List<string> UserDefined47 { get; set; }

        /// <summary> User defined metadata field (#48) for this learning object resource </summary>
        [SolrField("user_defined_48")]
        public List<string> UserDefined48 { get; set; }

        /// <summary> User defined metadata field (#49) for this learning object resource </summary>
        [SolrField("user_defined_49")]
        public List<string> UserDefined49 { get; set; }

        /// <summary> User defined metadata field (#50) for this learning object resource </summary>
        [SolrField("user_defined_50")]
        public List<string> UserDefined50 { get; set; }

        /// <summary> User defined metadata field (#51) for this learning object resource </summary>
        [SolrField("user_defined_51")]
        public List<string> UserDefined51 { get; set; }

        /// <summary> User defined metadata field (#52) for this learning object resource </summary>
        [SolrField("user_defined_52")]
        public List<string> UserDefined52 { get; set; }

        /// <summary> Display version of the user defined metadata field (#1) for this learning object resource </summary>
        [SolrField("user_defined_01.display")]
        public List<string> UserDefinedDisplay01 { get; set; }

        /// <summary> Display version of the user defined metadata field (#2) for this learning object resource </summary>
        [SolrField("user_defined_02.display")]
        public List<string> UserDefinedDisplay02 { get; set; }

        /// <summary> Display version of the user defined metadata field (#3) for this learning object resource </summary>
        [SolrField("user_defined_03.display")]
        public List<string> UserDefinedDisplay03 { get; set; }

        /// <summary> Display version of the user defined metadata field (#4) for this learning object resource </summary>
        [SolrField("user_defined_04.display")]
        public List<string> UserDefinedDisplay04 { get; set; }

        /// <summary> Display version of the user defined metadata field (#5) for this learning object resource </summary>
        [SolrField("user_defined_05.display")]
        public List<string> UserDefinedDisplay05 { get; set; }

        /// <summary> Display version of the user defined metadata field (#6) for this learning object resource </summary>
        [SolrField("user_defined_06.display")]
        public List<string> UserDefinedDisplay06 { get; set; }

        /// <summary> Display version of the user defined metadata field (#7) for this learning object resource </summary>
        [SolrField("user_defined_07.display")]
        public List<string> UserDefinedDisplay07 { get; set; }

        /// <summary> Display version of the user defined metadata field (#8) for this learning object resource </summary>
        [SolrField("user_defined_08.display")]
        public List<string> UserDefinedDisplay08 { get; set; }

        /// <summary> Display version of the user defined metadata field (#9) for this learning object resource </summary>
        [SolrField("user_defined_09.display")]
        public List<string> UserDefinedDisplay09 { get; set; }

        /// <summary> Display version of the user defined metadata field (#10) for this learning object resource </summary>
        [SolrField("user_defined_10.display")]
        public List<string> UserDefinedDisplay10 { get; set; }

        /// <summary> Display version of the user defined metadata field (#11) for this learning object resource </summary>
        [SolrField("user_defined_11.display")]
        public List<string> UserDefinedDisplay11 { get; set; }

        /// <summary> Display version of the user defined metadata field (#12) for this learning object resource </summary>
        [SolrField("user_defined_12.display")]
        public List<string> UserDefinedDisplay12 { get; set; }

        /// <summary> Display version of the user defined metadata field (#13) for this learning object resource </summary>
        [SolrField("user_defined_13.display")]
        public List<string> UserDefinedDisplay13 { get; set; }

        /// <summary> Display version of the user defined metadata field (#14) for this learning object resource </summary>
        [SolrField("user_defined_14.display")]
        public List<string> UserDefinedDisplay14 { get; set; }

        /// <summary> Display version of the user defined metadata field (#15) for this learning object resource </summary>
        [SolrField("user_defined_15.display")]
        public List<string> UserDefinedDisplay15 { get; set; }

        /// <summary> Display version of the user defined metadata field (#16) for this learning object resource </summary>
        [SolrField("user_defined_16.display")]
        public List<string> UserDefinedDisplay16 { get; set; }

        /// <summary> Display version of the user defined metadata field (#17) for this learning object resource </summary>
        [SolrField("user_defined_17.display")]
        public List<string> UserDefinedDisplay17 { get; set; }

        /// <summary> Display version of the user defined metadata field (#18) for this learning object resource </summary>
        [SolrField("user_defined_18.display")]
        public List<string> UserDefinedDisplay18 { get; set; }

        /// <summary> Display version of the user defined metadata field (#19) for this learning object resource </summary>
        [SolrField("user_defined_19.display")]
        public List<string> UserDefinedDisplay19 { get; set; }

        /// <summary> Display version of the user defined metadata field (#20) for this learning object resource </summary>
        [SolrField("user_defined_20.display")]
        public List<string> UserDefinedDisplay20 { get; set; }

        /// <summary> Display version of the user defined metadata field (#21) for this learning object resource </summary>
        [SolrField("user_defined_21.display")]
        public List<string> UserDefinedDisplay21 { get; set; }

        /// <summary> Display version of the user defined metadata field (#22) for this learning object resource </summary>
        [SolrField("user_defined_22.display")]
        public List<string> UserDefinedDisplay22 { get; set; }

        /// <summary> Display version of the user defined metadata field (#23) for this learning object resource </summary>
        [SolrField("user_defined_23.display")]
        public List<string> UserDefinedDisplay23 { get; set; }

        /// <summary> Display version of the user defined metadata field (#24) for this learning object resource </summary>
        [SolrField("user_defined_24.display")]
        public List<string> UserDefinedDisplay24 { get; set; }

        /// <summary> Display version of the user defined metadata field (#25) for this learning object resource </summary>
        [SolrField("user_defined_25.display")]
        public List<string> UserDefinedDisplay25 { get; set; }

        /// <summary> Display version of the user defined metadata field (#26) for this learning object resource </summary>
        [SolrField("user_defined_26.display")]
        public List<string> UserDefinedDisplay26 { get; set; }

        /// <summary> Display version of the user defined metadata field (#27) for this learning object resource </summary>
        [SolrField("user_defined_27.display")]
        public List<string> UserDefinedDisplay27 { get; set; }

        /// <summary> Display version of the user defined metadata field (#28) for this learning object resource </summary>
        [SolrField("user_defined_28.display")]
        public List<string> UserDefinedDisplay28 { get; set; }

        /// <summary> Display version of the user defined metadata field (#29) for this learning object resource </summary>
        [SolrField("user_defined_29.display")]
        public List<string> UserDefinedDisplay29 { get; set; }

        /// <summary> Display version of the user defined metadata field (#30) for this learning object resource </summary>
        [SolrField("user_defined_30.display")]
        public List<string> UserDefinedDisplay30 { get; set; }

        /// <summary> Display version of the user defined metadata field (#31) for this learning object resource </summary>
        [SolrField("user_defined_31.display")]
        public List<string> UserDefinedDisplay31 { get; set; }

        /// <summary> Display version of the user defined metadata field (#32) for this learning object resource </summary>
        [SolrField("user_defined_32.display")]
        public List<string> UserDefinedDisplay32 { get; set; }

        /// <summary> Display version of the user defined metadata field (#33) for this learning object resource </summary>
        [SolrField("user_defined_33.display")]
        public List<string> UserDefinedDisplay33 { get; set; }

        /// <summary> Display version of the user defined metadata field (#34) for this learning object resource </summary>
        [SolrField("user_defined_34.display")]
        public List<string> UserDefinedDisplay34 { get; set; }

        /// <summary> Display version of the user defined metadata field (#35) for this learning object resource </summary>
        [SolrField("user_defined_35.display")]
        public List<string> UserDefinedDisplay35 { get; set; }

        /// <summary> Display version of the user defined metadata field (#36) for this learning object resource </summary>
        [SolrField("user_defined_36.display")]
        public List<string> UserDefinedDisplay36 { get; set; }

        /// <summary> Display version of the user defined metadata field (#37) for this learning object resource </summary>
        [SolrField("user_defined_37.display")]
        public List<string> UserDefinedDisplay37 { get; set; }

        /// <summary> Display version of the user defined metadata field (#38) for this learning object resource </summary>
        [SolrField("user_defined_38.display")]
        public List<string> UserDefinedDisplay38 { get; set; }

        /// <summary> Display version of the user defined metadata field (#39) for this learning object resource </summary>
        [SolrField("user_defined_39.display")]
        public List<string> UserDefinedDisplay39 { get; set; }

        /// <summary> Display version of the user defined metadata field (#40) for this learning object resource </summary>
        [SolrField("user_defined_40.display")]
        public List<string> UserDefinedDisplay40 { get; set; }

        /// <summary> Display version of the user defined metadata field (#41) for this learning object resource </summary>
        [SolrField("user_defined_41.display")]
        public List<string> UserDefinedDisplay41 { get; set; }

        /// <summary> Display version of the user defined metadata field (#42) for this learning object resource </summary>
        [SolrField("user_defined_42.display")]
        public List<string> UserDefinedDisplay42 { get; set; }

        /// <summary> Display version of the user defined metadata field (#43) for this learning object resource </summary>
        [SolrField("user_defined_43.display")]
        public List<string> UserDefinedDisplay43 { get; set; }

        /// <summary> Display version of the user defined metadata field (#44) for this learning object resource </summary>
        [SolrField("user_defined_44.display")]
        public List<string> UserDefinedDisplay44 { get; set; }

        /// <summary> Display version of the user defined metadata field (#45) for this learning object resource </summary>
        [SolrField("user_defined_45.display")]
        public List<string> UserDefinedDisplay45 { get; set; }

        /// <summary> Display version of the user defined metadata field (#46) for this learning object resource </summary>
        [SolrField("user_defined_46.display")]
        public List<string> UserDefinedDisplay46 { get; set; }

        /// <summary> Display version of the user defined metadata field (#47) for this learning object resource </summary>
        [SolrField("user_defined_47.display")]
        public List<string> UserDefinedDisplay47 { get; set; }

        /// <summary> Display version of the user defined metadata field (#48) for this learning object resource </summary>
        [SolrField("user_defined_48.display")]
        public List<string> UserDefinedDisplay48 { get; set; }

        /// <summary> Display version of the user defined metadata field (#49) for this learning object resource </summary>
        [SolrField("user_defined_49.display")]
        public List<string> UserDefinedDisplay49 { get; set; }

        /// <summary> Display version of the user defined metadata field (#50) for this learning object resource </summary>
        [SolrField("user_defined_50.display")]
        public List<string> UserDefinedDisplay50 { get; set; }

        /// <summary> Display version of the user defined metadata field (#51) for this learning object resource </summary>
        [SolrField("user_defined_51.display")]
        public List<string> UserDefinedDisplay51 { get; set; }

        /// <summary> Display version of the user defined metadata field (#52) for this learning object resource </summary>
        [SolrField("user_defined_52.display")]
        public List<string> UserDefinedDisplay52 { get; set; }

        #endregion

        #region Fields which can be used for metadata export from the results, and retain internal hierarchy

  //      <field name = "creator_export" type="text_facets" indexed="false" stored="true" multiValued="true" />
  //<field name = "subjects_export" type="text_facets" indexed="false" stored="true" multiValued="true" />
  //<field name = "" type="text_facets" indexed="false" stored="true" multiValued="true" />
  //<field name = "" type="text_facets" indexed="false" stored="true" multiValued="true" />
  //<field name = "" type="text_facets" indexed="false" stored="true" multiValued="true" />
  //<field name = "" type="text_facets" indexed="false" stored="true" multiValued="true" />
  //<field name = "title_export" type="text_facets" indexed="false" stored="true" multiValued="true" />

        /// <summary> Allows export of the creator, while retaining internal hierarchy of data elements </summary>
        /// <remarks> This stores each individual creator, along with sub-elements, as a pipe-delimited string </remarks>
        [SolrField("creator_export")]
        public List<string> CreatorExport { get; set; }

        /// <summary> Allows export of the subjects, while retaining internal hierarchy of data elements </summary>
        /// <remarks> This stores each individual subject, along with sub-elements, as a pipe-delimited string </remarks>
        [SolrField("subjects_export")]
        public List<string> SubjectExport { get; set; }

        /// <summary> Allows export of the spatial subjects, while retaining internal hierarchy of data elements </summary>
        /// <remarks> This stores each individual spatial subject, along with sub-elements, as a pipe-delimited string </remarks>
        [SolrField("spatial_export")]
        public List<string> SpatialExport { get; set; }

        /// <summary> Allows export of the identifier, while retaining internal hierarchy of data elements </summary>
        /// <remarks> This stores each individual identifier, along with sub-elements, as a pipe-delimited string </remarks>
        [SolrField("identifier_export")]
        public List<string> IdentifierExport { get; set; }

        /// <summary> Allows export of the genre, while retaining internal hierarchy of data elements </summary>
        /// <remarks> This stores each individual genre, along with sub-elements, as a pipe-delimited string </remarks>
        [SolrField("genre_export")]
        public List<string> GenreExport { get; set; }

        /// <summary> Allows export of the notes, while retaining internal hierarchy of data elements </summary>
        /// <remarks> This stores each individual notes, along with sub-elements, as a pipe-delimited string </remarks>
        [SolrField("notes_export")]
        public List<string> NotesExport { get; set; }

        /// <summary> Allows export of the title, while retaining internal hierarchy of data elements </summary>
        /// <remarks> This stores each individual title, along with sub-elements, as a pipe-delimited string </remarks>
        [SolrField("title_export")]
        public List<string> TitleExport { get; set; }

        /// <summary> Allows export of the classification, while retaining internal hierarchy of data elements </summary>
        /// <remarks> This stores each individual classification, along with sub-elements, as a pipe-delimited string </remarks>
        [SolrField("classification_export")]
        public List<string> ClassificationExport { get; set; }

        /// <summary> Allows export of the temporal subjects, while retaining internal hierarchy of data elements </summary>
        /// <remarks> This stores each individual temporal subject, along with sub-elements, as a pipe-delimited string </remarks>
        [SolrField("temporal_export")]
        public List<string> TemporalExport { get; set; }

        #endregion 

        /// <summary> Highlighted snippet of text from this document </summary>
        public string Snippet { get; set; }



    }
}
