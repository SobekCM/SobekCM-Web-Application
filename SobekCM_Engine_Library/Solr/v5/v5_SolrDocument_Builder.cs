﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SobekCM.Core.Settings;
using SobekCM.Engine_Library.ApplicationState;
using SobekCM.Resource_Object;
using SobekCM.Resource_Object.Behaviors;
using SobekCM.Resource_Object.Divisions;
using SobekCM.Resource_Object.Metadata_Modules;
using SobekCM.Resource_Object.Metadata_Modules.GeoSpatial;
using SobekCM.Resource_Object.Solr;

namespace SobekCM.Engine_Library.Solr.v5
{
    /// <summary> Class builds the beta/version 5 solr document from the SobekCM digital resource object </summary>
    public class v5_SolrDocument_Builder
    {
        /// <summary> Build the solr document from the SobekCM Digital Resource object  </summary>
        /// <param name="Digital_Object"> Digital object to create an easily indexable view object for </param>
        /// <param name="File_Location"> Location for all of the text files associated with this item </param>
        /// <returns> Fully built (v5) solr document </returns>
        public v5_SolrDocument Build_Solr_Document(SobekCM_Item Digital_Object, string File_Location)
        {
            // Start the return object
            v5_SolrDocument returnValue = new v5_SolrDocument();
            returnValue.FileLocation = File_Location;

            // Set the unique key
            returnValue.DID = Digital_Object.BibID + ":" + Digital_Object.VID;

            // Add the administrative fields
            returnValue.Aggregations = new List<string>();
            returnValue.Aggregations.AddRange(Digital_Object.Behaviors.Aggregation_Code_List);
            returnValue.BibID = Digital_Object.BibID;
            returnValue.VID = Digital_Object.VID;
            returnValue.MainThumbnail = Digital_Object.Behaviors.Main_Thumbnail;
            returnValue.RestrictedThumbnail = Digital_Object.Behaviors.Restricted_Thumbnail;

            returnValue.Instances = new List<string>();
            returnValue.Instances.Add(Engine_ApplicationCache_Gateway.Settings.Servers.Instance_Code);

            returnValue.Group_Restrictions = String.Empty;
            if ( Digital_Object.Behaviors.User_Group_Access_Count > 0 )
            {
                // If any group has CanView access, it is assumed that noone else can
                List<int> matches = Digital_Object.Behaviors.User_Group_Access.Where(p => p.CanView).Select(p => p.UserGroupId).ToList<int>();

                if (matches.Count == 1)
                    returnValue.Group_Restrictions = "|" + matches[0] + "|";
                else
                {
                    StringBuilder groupBuilder = new StringBuilder("|");
                    foreach(int groupid in matches)
                    {
                        groupBuilder.Append(groupid + "|");
                    }
                    returnValue.Group_Restrictions = groupBuilder.ToString();
                }
            }           

            // Add the made public field
            if (Digital_Object.Web.MadePublicDate.HasValue)
                returnValue.MadePublicDate = Digital_Object.Web.MadePublicDate.Value;
            else
            {
                // If this is public and non-dark, but no date for made
                // public exists, make it today
                if ((!Digital_Object.Behaviors.Dark_Flag) && (Digital_Object.Behaviors.IP_Restriction_Membership >= 0))
                {
                    Digital_Object.Web.MadePublicDate = DateTime.Now;
                    returnValue.MadePublicDate = Digital_Object.Web.MadePublicDate.Value;
                }
            }

            // Add Serial hierarchy fields
            returnValue.Level1_Text = String.Empty;
            returnValue.Level1_Index = -1;
            returnValue.Level1_Facet = "NONE";
            returnValue.Level2_Text = String.Empty;
            returnValue.Level2_Index = -1;
            returnValue.Level2_Facet = "NONE";
            returnValue.Level3_Text = String.Empty;
            returnValue.Level3_Index = -1;
            returnValue.Level3_Facet = "NONE";
            returnValue.Level4_Text = String.Empty;
            returnValue.Level4_Index = -1;
            returnValue.Level4_Facet = "NONE";
            returnValue.Level5_Text = String.Empty;
            returnValue.Level5_Index = -1;
            returnValue.Level5_Facet = "NONE";
            returnValue.Hidden = false;

            if (Digital_Object.Behaviors != null)
            {
                if (Digital_Object.Behaviors.Serial_Info.Count > 0)
                {
                    returnValue.Level1_Index = Digital_Object.Behaviors.Serial_Info[0].Order;
                    returnValue.Level1_Text = Digital_Object.Behaviors.Serial_Info[0].Display;
                    returnValue.Level1_Text_Display = Digital_Object.Behaviors.Serial_Info[0].Display;
                    returnValue.Level1_Facet = Digital_Object.Behaviors.Serial_Info[0].Order.ToString().PadLeft(5, '0') + '|' + Digital_Object.Behaviors.Serial_Info[0].Display;
                }
                if (Digital_Object.Behaviors.Serial_Info.Count > 1)
                {
                    returnValue.Level2_Index = Digital_Object.Behaviors.Serial_Info[1].Order;
                    returnValue.Level2_Text = Digital_Object.Behaviors.Serial_Info[1].Display;
                    returnValue.Level2_Text_Display = Digital_Object.Behaviors.Serial_Info[1].Display;
                    returnValue.Level2_Facet = Digital_Object.Behaviors.Serial_Info[1].Order.ToString().PadLeft(5, '0') + '|' + Digital_Object.Behaviors.Serial_Info[1].Display;
                }
                if (Digital_Object.Behaviors.Serial_Info.Count > 2)
                {
                    returnValue.Level3_Index = Digital_Object.Behaviors.Serial_Info[2].Order;
                    returnValue.Level3_Text = Digital_Object.Behaviors.Serial_Info[2].Display;
                    returnValue.Level3_Text_Display = Digital_Object.Behaviors.Serial_Info[2].Display;
                    returnValue.Level3_Facet = Digital_Object.Behaviors.Serial_Info[2].Order.ToString().PadLeft(5, '0') + '|' + Digital_Object.Behaviors.Serial_Info[2].Display;
                }
                if (Digital_Object.Behaviors.Serial_Info.Count > 3)
                {
                    returnValue.Level4_Index = Digital_Object.Behaviors.Serial_Info[3].Order;
                    returnValue.Level4_Text = Digital_Object.Behaviors.Serial_Info[3].Display;
                    returnValue.Level4_Text_Display = Digital_Object.Behaviors.Serial_Info[3].Display;
                    returnValue.Level4_Facet = Digital_Object.Behaviors.Serial_Info[3].Order.ToString().PadLeft(5, '0') + '|' + Digital_Object.Behaviors.Serial_Info[3].Display;
                }
                if (Digital_Object.Behaviors.Serial_Info.Count > 4)
                {
                    returnValue.Level5_Index = Digital_Object.Behaviors.Serial_Info[4].Order;
                    returnValue.Level5_Text = Digital_Object.Behaviors.Serial_Info[4].Display;
                    returnValue.Level5_Text_Display = Digital_Object.Behaviors.Serial_Info[4].Display;
                    returnValue.Level5_Facet = Digital_Object.Behaviors.Serial_Info[4].Order.ToString().PadLeft(5, '0') + '|' + Digital_Object.Behaviors.Serial_Info[4].Display;
                }

                returnValue.Hidden = Digital_Object.Behaviors.Dark_Flag;

                // Some defaults
                returnValue.Discover_Groups = new List<int> { 0 };
                returnValue.Discover_Users = new List<int> { 0 };

                // Set the restricted message
                returnValue.RestrictedMsg = Digital_Object.Behaviors.RestrictionMessage;

                // Check for specific restrictions
                bool is_restricted = false;
                if ( Digital_Object.Behaviors.HasUserRestriction)
                {
                    is_restricted = true;
                    returnValue.Restricted = true;
                }
                
                // Set the IP restrictions based on PRIVATE or NOT
                if (Digital_Object.Behaviors.IP_Restriction_Membership == -1)
                    returnValue.Discover_IPs = new List<int> { -1 };
                else
                {
                    is_restricted = true;
                    returnValue.Discover_IPs = new List<int> { 0 };
                }

                // Provide a blanket restriction statement
                if (( is_restricted ) && ( String.IsNullOrEmpty(returnValue.RestrictedMsg)))
                {
                    returnValue.RestrictedMsg = "Access Restrictions Apply";
                }
            }



            // Set the spatial KML
            GeoSpatial_Information geo = Digital_Object.Get_Metadata_Module(GlobalVar.GEOSPATIAL_METADATA_MODULE_KEY) as GeoSpatial_Information;
            if (geo != null)
            {
                if (returnValue.SpatialFootprintKml == null) returnValue.SpatialFootprintKml = new List<string>();
                returnValue.SpatialFootprintKml.Add(geo.SobekCM_Main_Spatial_String);

                returnValue.SpatialDistance = (int)geo.SobekCM_Main_Spatial_Distance;
            }

            // Get the rest of the metadata, from the item
            List<KeyValuePair<string, string>> searchTerms = Digital_Object.Search_Terms;

            // Loop through and add each data field
            foreach (KeyValuePair<string, string> searchTerm in searchTerms)
            {
                // Ensure there is a value here
                if (String.IsNullOrWhiteSpace(searchTerm.Value))
                    continue;

                // Assign based on the key term
                switch (searchTerm.Key.ToLower())
                {
                    case "title":
                        returnValue.Title = searchTerm.Value;
                        break;

                    case "sort title":
                        returnValue.SortTitle = searchTerm.Value;
                        break;

                    case "other title":
                        if (returnValue.AltTitle == null) returnValue.AltTitle = new List<string>();
                        returnValue.AltTitle.Add(searchTerm.Value);
                        break;

                    case "translated title":
                        if (returnValue.TranslatedTitle == null) returnValue.TranslatedTitle = new List<string>();
                        returnValue.TranslatedTitle.Add(searchTerm.Value);
                        break;

                    case "course title":
                        returnValue.CourseTitle = searchTerm.Value;
                        break;

                    case "series title":
                        returnValue.SeriesTitle = searchTerm.Value;
                        break;

                    case "other citation":
                        if (returnValue.OtherCitation == null) returnValue.OtherCitation = new List<string>();
                        returnValue.OtherCitation.Add(searchTerm.Value);
                        break;

                    case "tickler":
                        if (returnValue.Tickler == null) returnValue.Tickler = new List<string>();
                        returnValue.Tickler.Add(searchTerm.Value);
                        break;

                    case "abstract":
                        if (returnValue.Abstract == null) returnValue.Abstract = new List<string>();
                        returnValue.Abstract.Add(searchTerm.Value);
                        break;

                    case "affililation":
                        if (returnValue.Affiliation == null) returnValue.Affiliation = new List<string>();
                        returnValue.Affiliation.Add(searchTerm.Value);

                        // For now, also put this in the display
                        if (returnValue.AffiliationDisplay == null) returnValue.AffiliationDisplay = new List<string>();
                        returnValue.AffiliationDisplay.Add(searchTerm.Value);
                        break;

                    case "affililation display":
                        if (returnValue.AffiliationDisplay == null) returnValue.AffiliationDisplay = new List<string>();
                        returnValue.AffiliationDisplay.Add(searchTerm.Value);
                        break;

                    case "genre":
                        if (returnValue.Genre == null) returnValue.Genre = new List<string>();
                        returnValue.Genre.Add(searchTerm.Value);
                        break;

                    case "genre display":
                        if (returnValue.GenreDisplay == null) returnValue.GenreDisplay = new List<string>();
                        returnValue.GenreDisplay.Add(searchTerm.Value);
                        break;

                    case "donor":
                        returnValue.Donor = searchTerm.Value;
                        break;

                    case "identifier":
                        if (returnValue.Identifier == null) returnValue.Identifier = new List<string>();
                        returnValue.Identifier.Add(searchTerm.Value);
                        break;

                    case "identifier display":
                        if (returnValue.IdentifierDisplay == null) returnValue.IdentifierDisplay = new List<string>();
                        returnValue.IdentifierDisplay.Add(searchTerm.Value);
                        break;

                    case "accession number":
                        // Set the display value (also used for faceting) to the full term
                        returnValue.AccessionNumberDisplay = searchTerm.Value;

                        // Make sure the list is built
                        if (returnValue.AccessionNumber == null) returnValue.AccessionNumber = new List<string>();

                        // If there are any periods, represeting a hierarchical identifier, split it
                        if (searchTerm.Value.IndexOf(".") > 0)
                        {
                            // Add each segment of the identifier
                            string[] split = searchTerm.Value.Split(".".ToCharArray());
                            StringBuilder builder = new StringBuilder(split[0]);
                            returnValue.AccessionNumber.Add(builder.ToString());
                            for (int i = 1; i < split.Length; i++)
                            {
                                builder.Append("." + split[i]);
                                returnValue.AccessionNumber.Add(builder.ToString());
                            }
                        }
                        else
                        {
                            returnValue.AccessionNumber.Add(searchTerm.Value);
                        }

                        break;

                    case "language":
                        if (returnValue.Language == null) returnValue.Language = new List<string>();
                        returnValue.Language.Add(searchTerm.Value);
                        break;

                    case "creator":
                        if (returnValue.Creator == null) returnValue.Creator = new List<string>();
                        returnValue.Creator.Add(searchTerm.Value);
                        break;

                    case "creator.display":
                        if (returnValue.Creator_Display == null) returnValue.Creator_Display = new List<string>();
                        returnValue.Creator_Display.Add(searchTerm.Value);
                        break;

                    case "publisher":
                        if ( returnValue.Publisher == null) returnValue.Publisher = new List<string>();
                        returnValue.Publisher.Add(searchTerm.Value);
                        break;

                    case "publisher.display":
                        if ( returnValue.Publisher_Display == null) returnValue.Publisher_Display = new List<string>();
                        returnValue.Publisher_Display.Add(searchTerm.Value);
                        break;

                    case "holding location":
                        returnValue.Holding = searchTerm.Value;
                        break;

                    case "notes":
                        if ( returnValue.Notes == null) returnValue.Notes = new List<string>();
                        returnValue.Notes.Add(searchTerm.Value);
                        break;

                    case "frequency":
                        if ( returnValue.Frequency == null) returnValue.Frequency = new List<string>();
                        returnValue.Frequency.Add(searchTerm.Value);
                        break;

                    case "edition":
                        returnValue.Edition = searchTerm.Value;
                        break;

                    case "publication place":
                        if ( returnValue.PubPlace == null) returnValue.PubPlace = new List<string>();
                        returnValue.PubPlace.Add(searchTerm.Value);
                        break;

                    case "format":
                        returnValue.Format = searchTerm.Value;
                        break;

                    case "source institution":
                        returnValue.Source = searchTerm.Value;
                        break;

                    case "target audience":
                        if ( returnValue.Audience == null) returnValue.Audience = new List<string>();
                        returnValue.Audience.Add(searchTerm.Value);
                        break;

                    case "type":
                        returnValue.Type = searchTerm.Value;
                        break;

                    case "name as subject":
                        if ( returnValue.NameAsSubject == null) returnValue.NameAsSubject = new List<string>();
                        returnValue.NameAsSubject.Add(searchTerm.Value);
                        break;

                    case "name as subject dispay":
                        if ( returnValue.NameAsSubjectDisplay == null) returnValue.NameAsSubjectDisplay = new List<string>();
                        returnValue.NameAsSubjectDisplay.Add(searchTerm.Value);
                        break;

                    case "title as subject":
                        if ( returnValue.TitleAsSubject == null) returnValue.TitleAsSubject = new List<string>();
                        returnValue.TitleAsSubject.Add(searchTerm.Value);
                        break;

                    case "title as subject display":
                        if ( returnValue.TitleAsSubjectDisplay == null) returnValue.TitleAsSubjectDisplay = new List<string>();
                        returnValue.TitleAsSubjectDisplay.Add(searchTerm.Value);
                        break;

                    case "spatial coverage":
                        if ( returnValue.Spatial == null) returnValue.Spatial = new List<string>();
                        returnValue.Spatial.Add(searchTerm.Value);
                        break;

                    case "spatial coverage.display":
                        if ( returnValue.SpatialDisplay == null) returnValue.SpatialDisplay = new List<string>();
                        returnValue.SpatialDisplay.Add(searchTerm.Value);
                        break;

                    case "country":
                        if ( returnValue.Country == null) returnValue.Country = new List<string>();
                        returnValue.Country.Add(searchTerm.Value);
                        break;

                    case "state":
                        if ( returnValue.State == null) returnValue.State = new List<string>();
                        returnValue.State.Add(searchTerm.Value);
                        break;

                    case "county":
                        if ( returnValue.County == null) returnValue.County = new List<string>();
                        returnValue.County.Add(searchTerm.Value);
                        break;

                    case "city":
                        if ( returnValue.City == null) returnValue.City = new List<string>();
                        returnValue.City.Add(searchTerm.Value);
                        break;

                    case "subject keyword":
                        if ( returnValue.Subject == null) returnValue.Subject = new List<string>();
                        returnValue.Subject.Add(searchTerm.Value.Trim());
                        break;

                    case "subjects.display":
                        if ( returnValue.SubjectDisplay == null) returnValue.SubjectDisplay = new List<string>();
                        returnValue.SubjectDisplay.Add(searchTerm.Value.Trim());
                        break;

                    case "publication date":
                        returnValue.Date = searchTerm.Value;
                        returnValue.DateDisplay = searchTerm.Value;
                        break;

                    case "date year":
                        returnValue.DateYear = searchTerm.Value;
                        break;

                    case "toc":
                        if ( returnValue.TableOfContents == null) returnValue.TableOfContents = new List<string>();
                        returnValue.TableOfContents.Add(searchTerm.Value.Trim());
                        break;

                    case "mime type":
                        if ( returnValue.MimeType == null) returnValue.MimeType = new List<string>();
                        returnValue.MimeType.Add(searchTerm.Value.Trim());
                        break;

                    case "cultural context":
                        if ( returnValue.CulturalContext == null) returnValue.CulturalContext = new List<string>();
                        returnValue.CulturalContext.Add(searchTerm.Value.Trim());
                        break;

                    case "inscription":
                        if ( returnValue.Inscription == null) returnValue.Inscription = new List<string>();
                        returnValue.Inscription.Add(searchTerm.Value.Trim());
                        break;

                    case "materials":
                    case "material":
                        if ( returnValue.Material == null) returnValue.Material = new List<string>();
                        returnValue.Material.Add(searchTerm.Value.Trim());
                        break;

                    case "materials display":
                    case "material display":
                        if (returnValue.MaterialDisplay == null) returnValue.MaterialDisplay = new List<string>();
                        returnValue.MaterialDisplay.Add(searchTerm.Value.Trim());
                        break;

                    case "measurements":
                        if ( returnValue.Measurements == null) returnValue.Measurements = new List<string>();
                        returnValue.Measurements.Add(searchTerm.Value.Trim());
                        break;

                    case "measurements display":
                        if (returnValue.MeasurementsDisplay == null) returnValue.MeasurementsDisplay = new List<string>();
                        returnValue.MeasurementsDisplay.Add(searchTerm.Value.Trim());
                        break;

                    case "style period":
                        if ( returnValue.StylePeriod == null) returnValue.StylePeriod = new List<string>();
                        returnValue.StylePeriod.Add(searchTerm.Value.Trim());
                        break;

                    case "technique":
                        if ( returnValue.Technique == null) returnValue.Technique = new List<string>();
                        returnValue.Technique.Add(searchTerm.Value.Trim());
                        break;

                    case "interviewee":
                        if ( returnValue.Interviewee == null) returnValue.Interviewee = new List<string>();
                        returnValue.Interviewee.Add(searchTerm.Value.Trim());
                        break;

                    case "interviewer":
                        if ( returnValue.Interviewer == null) returnValue.Interviewer = new List<string>();
                        returnValue.Interviewer.Add(searchTerm.Value.Trim());
                        break;

                    case "performance":
                        returnValue.Performance = searchTerm.Value.Trim();

                        // For now, we aren't setting the performance display any differently than performance
                        returnValue.PerformanceDisplay = searchTerm.Value.Trim();
                        break;

                    case "performance date":
                        returnValue.PerformanceDate = searchTerm.Value.Trim();
                        break;

                    case "performer":
                        if ( returnValue.Performer == null) returnValue.Performer = new List<string>();
                        returnValue.Performer.Add(searchTerm.Value.Trim());

                        // For now, we aren't setting the performer display any differently than performer
                        if (returnValue.PerformerDisplay == null) returnValue.PerformerDisplay = new List<string>();
                        returnValue.PerformerDisplay.Add(searchTerm.Value.Trim());
                        break;

                    case "etd committee":
                        if ( returnValue.EtdCommittee == null) returnValue.EtdCommittee = new List<string>();
                        returnValue.EtdCommittee.Add(searchTerm.Value.Trim());
                        break;

                    case "etd degree":
                        returnValue.EtdDegree = searchTerm.Value.Trim();
                        break;

                    case "etd degree discipline":
                        returnValue.EtdDegreeDiscipline = searchTerm.Value.Trim();
                        break;

                    case "etd degree division":
                        returnValue.EtdDegreeDivision = searchTerm.Value.Trim();
                        break;

                    case "etd degree grantor":
                        returnValue.EtdDegreeGrantor = searchTerm.Value.Trim();
                        break;

                    case "etd degree level":
                        returnValue.EtdDegreeLevel = searchTerm.Value.Trim();
                        break;

                    case "zt kingdom":
                        if ( returnValue.ZoologicalKingdom == null) returnValue.ZoologicalKingdom = new List<string>();
                        returnValue.ZoologicalKingdom.Add(searchTerm.Value.Trim());
                        break;

                    case "zt phylum":
                        if ( returnValue.ZoologicalPhylum == null) returnValue.ZoologicalPhylum = new List<string>();
                        returnValue.ZoologicalPhylum.Add(searchTerm.Value.Trim());
                        break;

                    case "zt class":
                        if ( returnValue.ZoologicalClass == null) returnValue.ZoologicalClass = new List<string>();
                        returnValue.ZoologicalClass.Add(searchTerm.Value.Trim());
                        break;

                    case "zt order":
                        if ( returnValue.ZoologicalOrder == null) returnValue.ZoologicalOrder = new List<string>();
                        returnValue.ZoologicalOrder.Add(searchTerm.Value.Trim());
                        break;

                    case "zt family":
                        if ( returnValue.ZoologicalFamily == null) returnValue.ZoologicalFamily = new List<string>();
                        returnValue.ZoologicalFamily.Add(searchTerm.Value.Trim());
                        break;

                    case "zt genus":
                        if ( returnValue.ZoologicalGenus == null) returnValue.ZoologicalGenus = new List<string>();
                        returnValue.ZoologicalGenus.Add(searchTerm.Value.Trim());
                        break;

                    case "zt species":
                        if ( returnValue.ZoologicalSpecies == null) returnValue.ZoologicalSpecies = new List<string>();
                        returnValue.ZoologicalSpecies.Add(searchTerm.Value.Trim());
                        break;

                    case "zt common name":
                        if ( returnValue.ZoologicalCommonName == null) returnValue.ZoologicalCommonName = new List<string>();
                        returnValue.ZoologicalCommonName.Add(searchTerm.Value.Trim());
                        break;

                    case "zt scientific name":
                        if ( returnValue.ZoologicalScientificName == null) returnValue.ZoologicalScientificName = new List<string>();
                        returnValue.ZoologicalScientificName.Add(searchTerm.Value.Trim());
                        break;

                    case "zt hierarchical":
                        if ( returnValue.ZoologicalHierarchical == null) returnValue.ZoologicalHierarchical = new List<string>();
                        returnValue.ZoologicalHierarchical.Add(searchTerm.Value.Trim());
                        break;

                    // Solr already rolls up to a zt_all field, so ignore this
                    case "zt all taxonomy":
                        break;

                    case "lom aggregation":
                        returnValue.LomAggregation = searchTerm.Value.Trim();
                        break;

                    case "lom context":
                        if ( returnValue.LomContext == null) returnValue.LomContext = new List<string>();
                        returnValue.LomContext.Add(searchTerm.Value.Trim());
                        break;

                    case "lom context display":
                        if ( returnValue.LomContextDisplay == null) returnValue.LomContextDisplay = new List<string>();
                        returnValue.LomContextDisplay.Add(searchTerm.Value.Trim());
                        break;

                    case "lom difficulty":
                        returnValue.LomDifficulty = searchTerm.Value.Trim();
                        break;

                    case "lom intended end user":
                        if ( returnValue.LomIntendedEndUser == null) returnValue.LomIntendedEndUser = new List<string>();
                        returnValue.LomIntendedEndUser.Add(searchTerm.Value.Trim());
                        break;

                    case "lom intended end user display":
                        returnValue.LomIntendedEndUserDisplay = searchTerm.Value.Trim();
                        break;

                    case "lom interactivity level":
                        returnValue.LomInteractivityLevel = searchTerm.Value.Trim();
                        break;

                    case "lom interactivity type":
                        returnValue.LomInteractivityType = searchTerm.Value.Trim();
                        break;

                    case "lom status":
                        returnValue.LomStatus = searchTerm.Value.Trim();
                        break;

                    case "lom requirement":
                        if ( returnValue.LomRequirement == null) returnValue.LomRequirement = new List<string>();
                        returnValue.LomRequirement.Add(searchTerm.Value.Trim());
                        break;

                    case "lom requirement display":
                        if ( returnValue.LomRequirementDisplay == null) returnValue.LomRequirementDisplay = new List<string>();
                        returnValue.LomRequirementDisplay.Add(searchTerm.Value.Trim());
                        break;

                    case "lom age range":
                        if ( returnValue.LomAgeRange == null) returnValue.LomAgeRange = new List<string>();
                        returnValue.LomAgeRange.Add(searchTerm.Value.Trim());
                        break;

                    case "lom resource type":
                        if ( returnValue.LomResourceType == null) returnValue.LomResourceType = new List<string>();
                        returnValue.LomResourceType.Add(searchTerm.Value.Trim());
                        break;

                    case "lom resource type display":
                        if ( returnValue.LomResourceTypeDisplay == null) returnValue.LomResourceTypeDisplay = new List<string>();
                        returnValue.LomResourceTypeDisplay.Add(searchTerm.Value.Trim());
                        break;

                    case "lom learning time":
                        returnValue.LomLearningTime = searchTerm.Value.Trim();
                        break;

                    case "temporal subject":
                        returnValue.TemporalSubject = searchTerm.Value.Trim();
                        break;

                    case "temporal subject display":
                        returnValue.TemporalSubjectDisplay = searchTerm.Value.Trim();
                        break;

                    case "accessibility":
                        returnValue.Accessibility = searchTerm.Value.Trim();
                        break;

                    case "licensing":
                        if (returnValue.Licensing == null) returnValue.Licensing = new List<string>();
                        returnValue.Licensing.Add(searchTerm.Value.Trim());
                        break;

                    case "system requirements":
                        if (returnValue.SystemRequirements == null) returnValue.SystemRequirements = new List<string>();
                        returnValue.SystemRequirements.Add(searchTerm.Value.Trim());
                        break;

                    // Not handled yet
                    case "temporal year":
                    case "ead name":
                        break;


                    // Ignore these
                    case "bibid":
                    case "vid":
                        break;

                    // Some more to ignore, since these are handled differently in solr
                    case "all subjects":
                    case "aggregation":
                        break;

                    default:
                        StreamWriter writer = new StreamWriter("missing_fields.txt", true);
                        writer.WriteLine(searchTerm.Key);
                        writer.Flush();
                        writer.Close();
                        break;

                }
            }

            // Find the Gregorian date issues value
            string pub_date = Digital_Object.Bib_Info.Origin_Info.Date_Check_All_Fields;
            returnValue.Date = pub_date;
            returnValue.DateDisplay = pub_date;
            DateTime gregDate;
            if (DateTime.TryParse(pub_date, out gregDate))
            {
                returnValue.Date = returnValue.Date;
                returnValue.GregorianDate = gregDate;
                returnValue.DateYear = gregDate.Year.ToString();

                // For now (since temporal subject isn't the best) just use this date for the timeline
                returnValue.TimelineDate = gregDate;
                returnValue.TimelineDateDisplay = pub_date;
            }
            else
            {
                int year_only;
                if ((pub_date.Length == 4) && (int.TryParse(pub_date, out year_only)))
                {
                    gregDate = new DateTime(year_only, 1, 1);
                    returnValue.Date = returnValue.Date = returnValue.Date;


                    returnValue.GregorianDate = gregDate;
                    returnValue.DateYear = gregDate.Year.ToString();

                    // For now (since temporal subject isn't the best) just use this date for the timeline
                    returnValue.TimelineDate = gregDate;
                    returnValue.TimelineDateDisplay = pub_date;
                }
            }

            // Add the empty solr pages for now
            returnValue.Solr_Pages = new List<Legacy_SolrPage>();

            // Prepare to step through all the divisions/pages in this item
            int pageorder = 1;
            List<abstract_TreeNode> divsAndPages = Digital_Object.Divisions.Physical_Tree.Divisions_PreOrder;

            // Get the list of all TXT files in this division
            string[] text_files = Directory.GetFiles(File_Location, "*.txt");
            Dictionary<string, string> text_files_existing = new Dictionary<string, string>();
            foreach (string thisTextFile in text_files)
            {
                string filename = (new FileInfo(thisTextFile)).Name.ToUpper();
                text_files_existing[filename] = filename;
            }

            // Get the list of all THM.JPG files in this division
            string[] thumbnail_files = Directory.GetFiles(File_Location, "*thm.jpg");
            Dictionary<string, string> thumbnail_files_existing = new Dictionary<string, string>();
            foreach (string thisTextFile in thumbnail_files)
            {
                string filename = (new FileInfo(thisTextFile)).Name;
                thumbnail_files_existing[filename.ToUpper().Replace("THM.JPG", "")] = filename;
            }

            // Step through all division nodes from the physical tree here
            List<string> text_files_included = new List<string>();
            foreach (abstract_TreeNode thisNode in divsAndPages)
            {
                if (thisNode.Page)
                {
                    // Cast to a page to continnue
                    Page_TreeNode pageNode = (Page_TreeNode)thisNode;

                    // Look for the root filename and then look for a matching TEXT file
                    if (pageNode.Files.Count > 0)
                    {
                        string root = pageNode.Files[0].File_Name_Sans_Extension;
                        if (text_files_existing.ContainsKey(root.ToUpper() + ".TXT"))
                        {
                            try
                            {
                                // SInce this is marked to be included, save this name
                                text_files_included.Add(root.ToUpper() + ".TXT");

                                // Read the page text
                                StreamReader reader = new StreamReader(File_Location + "\\" + root + ".txt");
                                string pageText = reader.ReadToEnd().Trim();
                                reader.Close();

                                // Look for a matching thumbnail
                                string thumbnail = String.Empty;
                                if (thumbnail_files_existing.ContainsKey(root.ToUpper()))
                                    thumbnail = thumbnail_files_existing[root.ToUpper()];

                                Legacy_SolrPage newPage = new Legacy_SolrPage(Digital_Object.BibID, Digital_Object.VID, pageorder, pageNode.Label, pageText, thumbnail);
                                returnValue.Solr_Pages.Add(newPage);
                            }
                            catch
                            {
                            }
                        }
                    }

                    // Increment the page order for the next page irregardless
                    pageorder++;
                }
            }

            // Now, check for any other valid text files 
            returnValue.AdditionalTextFiles = new List<string>();
            foreach (string thisTextFile in text_files_existing.Keys)
            {
                if ((!text_files_included.Contains(thisTextFile.ToUpper())) && (thisTextFile.ToUpper() != "AGREEMENT.TXT") && (thisTextFile.ToUpper().IndexOf("REQUEST") != 0))
                {
                    returnValue.AdditionalTextFiles.Add(thisTextFile);
                }
            }

            return returnValue;
        }
    }
}
