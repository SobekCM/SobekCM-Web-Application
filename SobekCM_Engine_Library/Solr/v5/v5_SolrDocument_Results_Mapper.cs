using System;
using System.Collections.Generic;
using SobekCM.Core.Aggregations;
using SobekCM.Core.ApplicationState;
using SobekCM.Core.Results;
using SobekCM.Engine_Library.ApplicationState;
using SobekCM.Engine_Library.Solr.Legacy;

namespace SobekCM.Engine_Library.Solr.v5
{
    public class v5_SolrDocument_Results_Mapper
    {
        public v5_Solr_Title_Result Map_To_Result(v5_SolrDocument solrDocument, List<Complete_Item_Aggregation_Metadata_Type> DisplayFields)
        {
            // Create the results
            v5_Solr_Title_Result resultConverted = new v5_Solr_Title_Result();
            resultConverted.MaterialType = solrDocument.Type;

            // Get the bibid
            resultConverted.BibID = solrDocument.DID.Substring(0, 10);

            // For now...
            resultConverted.GroupThumbnail = solrDocument.MainThumbnail;
            resultConverted.GroupTitle = solrDocument.Title ?? "NO TITLE";

            // These should not really be necessary
            resultConverted.Primary_Identifier = String.Empty;
            resultConverted.Primary_Identifier_Type = String.Empty;
            resultConverted.Snippet = String.Empty;

            // Add the item
            v5_Solr_Item_Result itemResult = new v5_Solr_Item_Result();
            itemResult.VID = solrDocument.DID.Substring(11, 5);
            itemResult.Title = solrDocument.Title ?? "NO TITLE";
            itemResult.MainThumbnail = solrDocument.MainThumbnail;
            resultConverted.Items.Add(itemResult);

            // Check for access
            if (solrDocument.Hidden)
                itemResult.AccessType = "dark";
            else if ((solrDocument.Discover_IPs != null ) && (solrDocument.Discover_IPs.Count > 0 ))
            {
                if (solrDocument.Discover_IPs[0] == -1)
                    itemResult.AccessType = "private";
            }

            // Build the display results values
            List<string> display_result_fields = new List<string>();
            foreach (Complete_Item_Aggregation_Metadata_Type metadataField in DisplayFields)
            {
                display_result_fields.Add(data_from_display_field(solrDocument, metadataField.SolrCode) ?? String.Empty);
            }

            resultConverted.Metadata_Display_Values = display_result_fields.ToArray();

            return resultConverted;
        }

        public v5_Solr_Title_Result Map_To_Result(SolrNet.Group<v5_SolrDocument> Grouping, List<Complete_Item_Aggregation_Metadata_Type> DisplayFields )
        {
            // Create the results
            v5_Solr_Title_Result resultConverted = new v5_Solr_Title_Result();

            // These should not really be necessary
            resultConverted.Primary_Identifier = String.Empty;
            resultConverted.Primary_Identifier_Type = String.Empty;
            resultConverted.Snippet = String.Empty;

            // Now add all the item info
            bool first_item = true;
            foreach(v5_SolrDocument solrDocument in Grouping.Documents)
            {
                if ( first_item)
                {
                    resultConverted.MaterialType = solrDocument.Type;

                    // Get the bibid
                    resultConverted.BibID = solrDocument.DID.Substring(0, 10);

                    // Look for information about this bibid first
                    Multiple_Volume_Item titleInfo = Engine_ApplicationCache_Gateway.Title_List.Get_Title(resultConverted.BibID);
                    if (titleInfo == null)
                    {
                        resultConverted.GroupThumbnail = solrDocument.MainThumbnail;
                        resultConverted.GroupTitle = solrDocument.Title ?? "NO TITLE";
                    }
                    else
                    {
                        if ((titleInfo.GroupThumbnailType == Group_Thumbnail_Enum.Custom_Thumbnail) && (!String.IsNullOrEmpty(titleInfo.CustomThumbnail)))
                            resultConverted.GroupThumbnail = titleInfo.CustomThumbnail;
                        else
                            resultConverted.GroupThumbnail = solrDocument.MainThumbnail;
                        resultConverted.GroupTitle = titleInfo.GroupTitle;
                    }

                    // Build the display results values
                    List<string> display_result_fields = new List<string>();
                    foreach (Complete_Item_Aggregation_Metadata_Type metadataField in DisplayFields)
                    {
                        display_result_fields.Add(data_from_display_field(solrDocument, metadataField.SolrCode) ?? String.Empty);
                    }
                    resultConverted.Metadata_Display_Values = display_result_fields.ToArray();

                    // Done with this first item
                    first_item = false;
                }

                // Add the item
                v5_Solr_Item_Result itemResult = new v5_Solr_Item_Result();
                itemResult.VID = solrDocument.DID.Substring(11, 5);
                itemResult.Title = solrDocument.Title ?? "NO TITLE";
                itemResult.MainThumbnail = solrDocument.MainThumbnail;

                // Check for access
                if (solrDocument.Hidden)
                    itemResult.AccessType = "dark";
                else if ((solrDocument.Discover_IPs != null) && (solrDocument.Discover_IPs.Count > 0))
                {
                    if (solrDocument.Discover_IPs[0] == -1)
                        itemResult.AccessType = "private";
                }

                resultConverted.Items.Add(itemResult);
            }          

            return resultConverted;
        }

        private static string data_from_display_field(v5_SolrDocument SolrDocument, string SolrDisplayField)
        {
            switch (SolrDisplayField.ToLower())
            {
                case "date":
                case "date.display":
                    return SolrDocument.DateDisplay;

                case "timeline_date":
                    return SolrDocument.TimelineDate.ToString();

                case "timeline_date.display":
                    return SolrDocument.TimelineDateDisplay;

                case "title":
                    return SolrDocument.Title;

                case "type":
                    return SolrDocument.Type;

                case "language":
                    return collection_to_string(SolrDocument.Language);

                case "creator.display":
                    return collection_to_string(SolrDocument.Creator_Display);

                case "publisher.display":
                    return collection_to_string(SolrDocument.Publisher_Display);

                case "publication_place":
                    return collection_to_string(SolrDocument.PubPlace);

                case "subject.display":
                    return collection_to_string(SolrDocument.SubjectDisplay);

                case "genre.display":
                    return collection_to_string(SolrDocument.GenreDisplay);

                case "audience":
                    return collection_to_string(SolrDocument.Audience);

                case "spatial_standard.display":
                    return collection_to_string(SolrDocument.SpatialDisplay);

                case "country":
                    return collection_to_string(SolrDocument.Country);

                case "state":
                    return collection_to_string(SolrDocument.State);

                case "county":
                    return collection_to_string(SolrDocument.County);

                case "city":
                    return collection_to_string(SolrDocument.City);

                case "source":
                    return SolrDocument.Source;

                case "holding":
                    return SolrDocument.Holding;

                case "identifier.display":
                    return collection_to_string(SolrDocument.IdentifierDisplay);

                case "notes":
                    return collection_to_string(SolrDocument.Notes);

                case "other":
                    return collection_to_string(SolrDocument.OtherCitation);

                case "tickler":
                    return collection_to_string(SolrDocument.Tickler);

                case "donor":
                    return SolrDocument.Donor;

                case "format":
                    return SolrDocument.Format;

                case "bibid":
                    return SolrDocument.BibID;

                case "publication date":
                    return SolrDocument.Date;

                case "affiliation":
                    return collection_to_string(SolrDocument.Affiliation);

                case "frequency":
                    return collection_to_string(SolrDocument.Frequency);

                case "name_as_subject.display":
                    return collection_to_string(SolrDocument.NameAsSubjectDisplay);

                case "title_as_subject.display":
                    return collection_to_string(SolrDocument.TitleAsSubjectDisplay);

                case "all subjects":
                    return collection_to_string(SolrDocument.SubjectDisplay);

                case "temporal subject":
                    return String.Empty;

                case "attribution":
                    return SolrDocument.Attribution;

                case "user description":
                    return String.Empty;

                case "temporal decade":
                    return String.Empty;

                case "mime_type":
                    return collection_to_string(SolrDocument.MimeType);

                case "tracking_box":
                    return collection_to_string(SolrDocument.TrackingBox);

                case "abstract":
                    return collection_to_string(SolrDocument.Abstract);

                case "edition":
                    return SolrDocument.Edition;

                case "zt_kingdom":
                    return collection_to_string(SolrDocument.ZoologicalKingdom);

                case "zt_phylum":
                    return collection_to_string(SolrDocument.ZoologicalPhylum);

                case "zt_class":
                    return collection_to_string(SolrDocument.ZoologicalClass);

                case "zt_order":
                    return collection_to_string(SolrDocument.ZoologicalOrder);

                case "zt_family":
                    return collection_to_string(SolrDocument.ZoologicalFamily);

                case "zt_genus":
                    return collection_to_string(SolrDocument.ZoologicalGenus);

                case "zt_species":
                    return collection_to_string(SolrDocument.ZoologicalSpecies);

                case "zt_common_name":
                    return collection_to_string(SolrDocument.ZoologicalCommonName);

                case "zt_scientific_name":
                    return collection_to_string(SolrDocument.ZoologicalScientificName);

                case "zt all taxonomy":
                    return collection_to_string(SolrDocument.ZoologicalHierarchical);

                case "cultural_context":
                    return collection_to_string(SolrDocument.CulturalContext);

                case "inscription":
                    return collection_to_string(SolrDocument.Inscription);

                case "material":
                    return collection_to_string(SolrDocument.Material);

                case "style_period":
                    return collection_to_string(SolrDocument.StylePeriod);

                case "technique":
                    return collection_to_string(SolrDocument.Technique);

                case "accession_number.display":
                    return SolrDocument.AccessionNumberDisplay;

                case "etd_committee":
                    return collection_to_string(SolrDocument.EtdCommittee);

                case "etd_degree":
                    return SolrDocument.EtdDegree;

                case "etd_degree_discipline":
                    return SolrDocument.EtdDegreeDiscipline;

                case "etd_degree_grantor":
                    return SolrDocument.EtdDegreeGrantor;

                case "etd_degree_level":
                    return SolrDocument.EtdDegreeLevel;

                case "temporal year":
                    return String.Empty;

                case "interviewee":
                    return collection_to_string(SolrDocument.Interviewee);

                case "interviewer":
                    return collection_to_string(SolrDocument.Interviewer);

                case "user_defined_01.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay01);

                case "user_defined_02.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay02);

                case "user_defined_03.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay03);

                case "user_defined_04.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay04);

                case "user_defined_05.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay05);

                case "user_defined_06.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay06);

                case "user_defined_07.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay07);

                case "user_defined_08.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay08);

                case "user_defined_09.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay09);

                case "user_defined_10.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay10);

                case "user_defined_11.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay11);

                case "user_defined_12.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay12);

                case "user_defined_13.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay13);

                case "user_defined_14.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay14);

                case "user_defined_15.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay15);

                case "user_defined_16.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay16);

                case "user_defined_17.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay17);

                case "user_defined_18.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay18);

                case "user_defined_19.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay19);

                case "user_defined_20.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay20);

                case "user_defined_21.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay21);

                case "user_defined_22.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay22);

                case "user_defined_23.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay23);

                case "user_defined_24.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay24);

                case "user_defined_25.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay25);

                case "user_defined_26.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay26);

                case "user_defined_27.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay27);

                case "user_defined_28.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay28);

                case "user_defined_29.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay29);

                case "user_defined_30.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay30);

                case "user_defined_31.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay31);

                case "user_defined_32.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay32);

                case "user_defined_33.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay33);

                case "user_defined_34.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay34);

                case "user_defined_35.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay35);

                case "user_defined_36.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay36);

                case "user_defined_37.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay37);

                case "user_defined_38.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay38);

                case "user_defined_39.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay39);

                case "user_defined_40.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay40);

                case "user_defined_41.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay41);

                case "user_defined_42.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay42);

                case "user_defined_43.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay43);

                case "user_defined_44.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay44);

                case "user_defined_45.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay45);

                case "user_defined_46.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay46);

                case "user_defined_47.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay47);

                case "user_defined_48.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay48);

                case "user_defined_49.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay49);

                case "user_defined_50.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay50);

                case "user_defined_51.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay51);

                case "user_defined_52.display":
                    return collection_to_string(SolrDocument.UserDefinedDisplay52);

                case "measurements":
                    return collection_to_string(SolrDocument.Measurements);

                case "aggregations":
                    return collection_to_string(SolrDocument.Aggregations);

                case "lom_aggregation":
                    return SolrDocument.LomAggregation;

                case "lom_context.display":
                    return collection_to_string(SolrDocument.LomContextDisplay);

                case "lom_classification.display":
                    return collection_to_string(SolrDocument.LomClassification);

                case "lom_difficulty":
                    return SolrDocument.LomDifficulty;

                case "lom_intended_end_user.display":
                    return SolrDocument.LomIntendedEndUserDisplay;

                case "lom_interactivity_level":
                    return SolrDocument.LomInteractivityLevel;

                case "lom_interactivity_type":
                    return SolrDocument.LomInteractivityType;

                case "lom_status":
                    return SolrDocument.LomStatus;

                case "lom_requirement.display":
                    return collection_to_string(SolrDocument.LomRequirementDisplay);

                case "lom_age_range":
                    return collection_to_string(SolrDocument.LomAgeRange);

                case "etd_degree_division":
                    return SolrDocument.EtdDegreeDivision;

                case "performance.display":
                    return SolrDocument.PerformanceDisplay;

                case "performance_date":
                    return SolrDocument.PerformanceDate;

                case "performer.display":
                    return collection_to_string(SolrDocument.PerformerDisplay);

                case "lom_resource_type.display":
                    return collection_to_string(SolrDocument.LomResourceTypeDisplay);

                case "lom_learning_time":
                    return SolrDocument.LomLearningTime;
            }

            return String.Empty;
        }

        private static string collection_to_string(List<string> Values)
        {
            if ((Values == null) || (Values.Count == 0)) return String.Empty;

            if (Values.Count == 1) return Values[0];

            if (Values.Count == 2) return Values[0] + " | " + Values[1];

            if (Values.Count == 3) return Values[0] + " | " + Values[1] + " | " + Values[2];

            if (Values.Count == 4) return Values[0] + " | " + Values[1] + " | " + Values[2] + " | " + Values[3];

            return Values[0] + " | " + Values[1] + " | " + Values[2] + " | " + Values[3] + " | " + Values[4];
        }
    }
}
