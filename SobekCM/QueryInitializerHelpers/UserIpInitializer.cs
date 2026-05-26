using Microsoft.AspNetCore.Http;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Library;
using SobekCM.Tools;

namespace SobekCM.QueryInitializerHelpers
{
    public class UserIpInitializer : IQueryInitializerHelper
    {
        public QueryInitializerHelperResponse Initialize(HttpContext context, RequestCache request, Custom_Tracer tracer)
        {
            // Get the user IP
            string remoteAddr = context.Connection?.RemoteIpAddress?.ToString() ?? string.Empty;

            // Add the user ip to the reqeust cache for use later
            context.Items.Add(RequestCache_Keys.UserIP, remoteAddr);

            return QueryInitializerHelperResponse.Successful;

        }
    }
}
