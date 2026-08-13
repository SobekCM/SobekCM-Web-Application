using Microsoft.AspNetCore.Http;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Library;
using SobekCM.Tools;
using System.Net;
using System.Net.Sockets;

namespace SobekCM.QueryInitializerHelpers
{
    public class UserIpInitializer : IQueryInitializerHelper
    {
        public QueryInitializerHelperResponse Initialize(HttpContext context, RequestCache request, Custom_Tracer tracer)
        {
            tracer.Add_Trace("UserIpInitializer.Initialize");

            // Get the user IP
            string remoteAddr = context.Connection?.RemoteIpAddress?.ToString() ?? string.Empty;

#if DEBUG
            // If in DEBUG mode, and it is the loopback IP address, use a local IP
            if (remoteAddr == "::1")
            {

                try
                {
                    var host = Dns.GetHostEntry(Dns.GetHostName());
                    foreach (var ip in host.AddressList)
                    {
                        if (ip.AddressFamily == AddressFamily.InterNetwork)
                        {
                            remoteAddr = ip.ToString();
                            break;
                        }
                    }
                }
                catch { }

            }
#endif

            // Add the user ip to the reqeust cache for use later
            context.Items.Add(RequestCache_Keys.UserIP, remoteAddr);

            return QueryInitializerHelperResponse.Successful;

        }
    }
}
