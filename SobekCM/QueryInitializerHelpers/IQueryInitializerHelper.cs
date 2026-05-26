using Microsoft.AspNetCore.Http;
using SobekCM.Library;
using SobekCM.Tools;

namespace SobekCM.QueryInitializerHelpers
{
    public interface IQueryInitializerHelper
    {
        QueryInitializerHelperResponse Initialize(HttpContext context, RequestCache request, Custom_Tracer tracer);
    }
}