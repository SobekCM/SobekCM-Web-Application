#region Using directives

using System;
using System.Collections.Generic;
using SobekCM.Core.MicroservicesClient;
using SobekCM.Core.OpenPublishing;
using SobekCM.Tools;

#endregion

namespace SobekCM.Engine_Library.Client
{
    public class SobekEngineClient_OpenPublishing : MicroservicesClientBase
    {
        /// <summary> Constructor for a new instance of the SobekEngineClient_WebSkinEndpoints class </summary>
        /// <param name="ConfigObj"> Fully constructed microservices client configuration </param>
        public SobekEngineClient_OpenPublishing(MicroservicesClient_Configuration ConfigObj) : base(ConfigObj)
        {
            // All work done in the base constructor
        }

        public List<OPTheme> Available_Themes(Custom_Tracer Tracer)
        {
            // Add a beginning trace
            Tracer.Add_Trace("SobekEngineClient_OpenPublishing.Available_Themes", "Get all available themes");

            // Get the endpoint
            MicroservicesClient_Endpoint endpoint = GetEndpointConfig("OpenPublishing.Available_Themes", Tracer);

            // Call out to the endpoint and deserialize the object
            return Deserialize<List<OPTheme>>(endpoint.URL, endpoint.Protocol, Tracer);
        }

        public OPTheme Theme_By_ID(int Id, Custom_Tracer Tracer)
        {
            // Add a beginning trace
            Tracer.Add_Trace("SobekEngineClient_OpenPublishing.Theme_By_ID", "Get theme by id " + Id);

            // Get the endpoint
            MicroservicesClient_Endpoint endpoint = GetEndpointConfig("OpenPublishing.Theme_By_ID", Tracer);

            // Format the URL
            string url = String.Format(endpoint.URL, Id);

            // Call out to the endpoint and deserialize the object
            return Deserialize<OPTheme>(endpoint.URL, endpoint.Protocol, Tracer);
        }

    }
}
