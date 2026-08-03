using Microsoft.AspNetCore.Http;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Engine_Library.ApplicationState;
using SobekCM.Engine_Library.Database;
using SobekCM.Library;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace SobekCM.QueryInitializerHelpers
{
    public class ServerDirectoryInitializer : IQueryInitializerHelper
    {
        public QueryInitializerHelperResponse Initialize(HttpContext context, RequestCache request, Custom_Tracer tracer)
        {
            if (!context.Items.ContainsKey(RequestCache_Keys.BaseUrl) || !context.Items.ContainsKey(RequestCache_Keys.RequestUrl))
            {
                return new QueryInitializerHelperResponse(false, "The ServerDirectoryInitializer must be used after the BaseUrlInitializer in the query initializer list.");
            }

            string base_url = context.Items[RequestCache_Keys.BaseUrl].ToString();
            string request_url = context.Items[RequestCache_Keys.RequestUrl].ToString();


            // If this is running on localhost, and in debug, set base directory to this one
#if DEBUG
            if (base_url.IndexOf("localhost:") > 0)
            {
                // Need to pass in the local directory to load THOSE configuration files
                string mainDir = ContentRoot_Gateway.ContentRootPath;
                Engine_ApplicationCache_Gateway.RefreshAll(mainDir);

                UI_ApplicationCache_Gateway.Settings.Servers.System_Base_URL = base_url;
                UI_ApplicationCache_Gateway.Settings.Servers.Base_URL = base_url;
            }
#endif

            // Ensure the settings base directory is set correctly
            if (String.IsNullOrEmpty(UI_ApplicationCache_Gateway.Settings.Servers.Base_Directory))
            {
                string baseDir = ContentRoot_Gateway.ContentRootPath;
                UI_ApplicationCache_Gateway.Settings.Servers.Base_Directory = baseDir;
                tracer.Add_Trace($"SobekCM_Page_Globals.Constructor", "No base directory set, so seting to {baseDir}");
                Engine_Database.Set_Setting("Application Server Network", baseDir);
            }

            // Ensure the web server IP address is set correctly
            if (String.IsNullOrEmpty(UI_ApplicationCache_Gateway.Settings.Servers.SobekCM_Web_Server_IP))
            {
                string ip = get_server_ip();
                if (ip.Length > 0)
                {
                    UI_ApplicationCache_Gateway.Settings.Servers.SobekCM_Web_Server_IP = ip;

                    Engine_Database.Set_Setting("SobekCM Web Server IP", ip);
                }
            }

            // (TEMPORARY FOR UF)
            if ((!String.IsNullOrEmpty(UI_ApplicationCache_Gateway.Settings.System.System_Abbreviation)) && (UI_ApplicationCache_Gateway.Settings.System.System_Abbreviation.IndexOf("UFDC") == 0))
            {
                UI_ApplicationCache_Gateway.Settings.Servers.Base_Directory = ContentRoot_Gateway.ContentRootPath;
            }

            return QueryInitializerHelperResponse.Successful;
        }

        private string get_server_ip()
        {
            try
            {
                IPHostEntry host;
                string localIP = "?";
                host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (IPAddress ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        localIP = ip.ToString();
                    }
                }
                return localIP;
            }
            catch
            {
                return String.Empty;
            }
        }

    }
}