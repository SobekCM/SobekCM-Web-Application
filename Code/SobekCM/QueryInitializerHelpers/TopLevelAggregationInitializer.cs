using Microsoft.AspNetCore.Http;
using SobekCM.Core.Client;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Library;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System.Net;
using System.Net.Sockets;

namespace SobekCM.QueryInitializerHelpers
{
    public class TopLevelAggregationInitializer : IQueryInitializerHelper
    {
        public QueryInitializerHelperResponse Initialize(HttpContext context, RequestCache request, Custom_Tracer tracer)
        {
            tracer.Add_Trace("TopLevelAggregationInitializer.Initialize");

            // Always pull TOP level collection
            request.Top_Collection = SobekEngineClient.Aggregations.Get_Aggregation("all", request.Current_Mode.Language, (UI_ApplicationCache_Gateway.Configuration.Languages.Default_Language?.Code ?? "en"), tracer);

            return QueryInitializerHelperResponse.Successful;

        }
    }
}
