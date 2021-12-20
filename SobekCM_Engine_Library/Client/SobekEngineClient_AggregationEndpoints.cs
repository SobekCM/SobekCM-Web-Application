﻿#region Using directives

using SobekCM.Core.Aggregations;
using SobekCM.Core.Configuration;
using SobekCM.Core.Configuration.Localization;
using SobekCM.Core.Message;
using SobekCM.Core.MicroservicesClient;
using SobekCM.Core.Results;
using SobekCM.Core.WebContent;
using SobekCM.Engine_Library.Endpoints;
using SobekCM.Tools;
using System;
using System.Collections.Generic;

#endregion

namespace SobekCM.Core.Client
{
    /// <summary> Gateway to all the aggregation-related endpoints exposed by the SobekCM engine </summary>
    public class SobekEngineClient_AggregationEndpoints : MicroservicesClientBase
    {
        /// <summary> Constructor for a new instance of the SobekEngineClient_AggregationEndpoints class </summary>
        /// <param name="ConfigObj"> Fully constructed microservices client configuration </param>
        public SobekEngineClient_AggregationEndpoints(MicroservicesClient_Configuration ConfigObj) : base(ConfigObj)
        {
            // All work done in the base constructor
        }

        /// <summary> Gets the complete (language agnostic) item aggregation, by aggregation code </summary>
        /// <param name="AggregationCode"> Code for the requested aggregation </param>
        /// <param name="UseCache"> Flag indicates if the cache should be used to check for a built copy or store the final product </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> Fully built language-agnostic aggregation, with all related configurations </returns>
        public Complete_Item_Aggregation Get_Complete_Aggregation(string AggregationCode, bool UseCache, Custom_Tracer Tracer)
        {
            return AggregationServices.get_complete_aggregation(AggregationCode, UseCache, Tracer);
        }

        /// <summary> Gets the language-specific item aggregation, by aggregation code and language code </summary>
        /// <param name="AggregationCode"> Code for the aggregation </param>
        /// <param name="RequestedLanguage"> Requested language to retrieve </param>
        /// <param name="DefaultLanguage"> Default interface language, in case the requested language does not exist </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> Built language-specific item aggregation object  </returns>
        public Item_Aggregation Get_Aggregation(string AggregationCode, Web_Language_Enum RequestedLanguage, Web_Language_Enum DefaultLanguage, Custom_Tracer Tracer)
        {
            return AggregationServices.get_item_aggregation(AggregationCode, RequestedLanguage, DefaultLanguage, Tracer);
        }

        /// <summary> Gets the all information, including the HTML, for an item aggregation child page </summary>
        /// <param name="AggregationCode"> Code for the aggregation </param>
        /// <param name="RequestedLanguage"> Requested language to retrieve </param>
        /// <param name="DefaultLanguage"> Default interface language, in case the requested language does not exist </param>
        /// <param name="ChildPageCode"> Code the requested child page </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> Fully built object, based on the aggregation configuration and reading the source HTML file </returns>
        public HTML_Based_Content Get_Aggregation_HTML_Child_Page(string AggregationCode, Web_Language_Enum RequestedLanguage, Web_Language_Enum DefaultLanguage, string ChildPageCode, Custom_Tracer Tracer )
        {
            return AggregationServices.get_item_aggregation_html_child_page(AggregationCode, RequestedLanguage, DefaultLanguage, ChildPageCode, Tracer);
        }

        /// <summary> Gets the entire collection hierarchy (used for hierarchical tree displays) </summary>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> Fully built aggregation hierarchy </returns>
        public Aggregation_Hierarchy Get_Aggregation_Hierarchy(Custom_Tracer Tracer)
        {
            return AggregationServices.get_aggregation_hierarchy(Tracer);
        }

        /// <summary> Add a new aggregation to the system </summary>
        /// <param name="NewAggregation"> Information for the new aggregation </param>
        /// <returns> Message indicating success or any errors encountered </returns>
        public RestResponseMessage Add_New_Aggregation(New_Aggregation_Arguments NewAggregation)
        {
            return AggregationServices.add_new_aggregation(NewAggregation);
        }

        public List<Item_Aggregation_Visibility_Statistic> Get_Item_Count(string AggregationCode, Custom_Tracer Tracer)
        {
            // Add a beginning trace
            Tracer.Add_Trace("SobekEngineClient_AggregationServices.Get_Item_Count", "Get by primary key");

            // Get the endpoint
            MicroservicesClient_Endpoint endpoint = GetEndpointConfig("Aggregations.Get_Item_Count", Tracer);

            // Format the URL
            string url = String.Format(endpoint.URL, AggregationCode);

            // Call out to the endpoint and deserialize the object
            var returnValue = Deserialize<List<Item_Aggregation_Visibility_Statistic>>(url, endpoint.Protocol, Tracer);

            // Return the object
            return returnValue;
        }

        public Private_Items_List Get_Private_Items(string AggregationCode, int Sort, int Page, int ResultsPerPage, Custom_Tracer Tracer)
        {
            // Add a beginning trace
            Tracer.Add_Trace("SobekEngineClient_AggregationServices.Get_Private_Items", "Get by primary key");

            // Get the endpoint
            MicroservicesClient_Endpoint endpoint = GetEndpointConfig("Aggregations.Get_Private_Items", Tracer);

            // Format the URL
            string url = String.Format(endpoint.URL, AggregationCode);

            // Add the other URL elements
            url = url + $"?p={Page}&o={Sort}";

            // Add results per page?
            if (ResultsPerPage > 0)
            {
                url = url + $"&pageSize={ResultsPerPage}";
            }

            // Call out to the endpoint and deserialize the object
            var returnValue = Deserialize<Private_Items_List>(url, endpoint.Protocol, Tracer);

            // Return the object
            return returnValue;
        }


        /// <summary> URL for the list of uploaded file JSON REST API </summary>
        /// <remarks> This is used by the CKEditor to display previously uploaded file at the aggregation level </remarks>
        public string Aggregation_Uploaded_Files_URL
        {
            get
            {
                return Config["Get_Aggregation_Uploaded_Files"] == null ? null : Config["Get_Aggregation_Uploaded_Files"].URL;
            }
        }


    }
}
