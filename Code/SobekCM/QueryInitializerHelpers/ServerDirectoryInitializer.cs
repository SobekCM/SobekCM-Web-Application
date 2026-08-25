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
#if DEBUG
        // Tracks the content root we last refreshed settings from, so repeated local debug requests
        // don't pay for a full RefreshAll() every single time -- just the first request (or the first
        // one after the content root changes, e.g. a different debug profile)
        private static string lastRefreshedContentRoot;
#endif

        public QueryInitializerHelperResponse Initialize(HttpContext context, RequestCache request, Custom_Tracer tracer)
        {
            tracer.Add_Trace("ServerDirectoryInitializer.Initialize");

            if (!context.Items.ContainsKey(RequestCache_Keys.BaseUrl) || !context.Items.ContainsKey(RequestCache_Keys.RequestUrl))
            {
                return new QueryInitializerHelperResponse(false, "The ServerDirectoryInitializer must be used after the BaseUrlInitializer in the query initializer list.");
            }

            string base_url = context.Items[RequestCache_Keys.BaseUrl].ToString();
            string request_url = context.Items[RequestCache_Keys.RequestUrl].ToString();

            // If this is running on localhost, and in debug, reload from the local config directory.
            // (System_Base_URL/Base_URL localhost override now happens earlier, in Program.cs, before
            // the microservices config is resolved.)
#if DEBUG
            tracer.Add_Trace("ServerDirectoryInitializer.Initialize", "In debug mode");
            if (base_url.IndexOf("localhost:") > 0)
            {
                // Need to pass in the local directory to load THOSE configuration files
                string mainDir = AppRoot_Gateway.AppRootPath;

                // Only refresh once per content root -- otherwise every single local debug request pays
                // for a full settings/config reload, which gets old fast when stepping through breakpoints
                if (lastRefreshedContentRoot != mainDir)
                {
                    tracer.Add_Trace("ServerDirectoryInitializer.Initialize", "In debug mode - refreshing settings from local directory");
                    Engine_ApplicationCache_Gateway.RefreshAll(mainDir);
                    lastRefreshedContentRoot = mainDir;
                }
            }
#endif

            // Ensure the settings base directory is set correctly
            tracer.Add_Trace("ServerDirectoryInitializer.Initialize", "First call to settings?");
            if (String.IsNullOrEmpty(UI_ApplicationCache_Gateway.Settings.Servers.Base_Directory))
            {
                tracer.Add_Trace("ServerDirectoryInitializer.Initialize", "Setting value for base directory on first time launch");

                string baseDir = AppRoot_Gateway.AppRootPath;
                UI_ApplicationCache_Gateway.Settings.Servers.Base_Directory = baseDir;
                tracer.Add_Trace($"SobekCM_Page_Globals.Constructor", "No base directory set, so seting to {baseDir}");
                Engine_Database.Set_Setting("Application Server Network", baseDir);
            }

            // Ensure the web server IP address is set correctly
            tracer.Add_Trace("ServerDirectoryInitializer.Initialize", "Ensure the server IP is set correctly");
            if (String.IsNullOrEmpty(UI_ApplicationCache_Gateway.Settings.Servers.SobekCM_Web_Server_IP))
            {
                tracer.Add_Trace("ServerDirectoryInitializer.Initialize", "Setting value for server IP on first time launch");

                string ip = get_server_ip();
                if (ip.Length > 0)
                {
                    UI_ApplicationCache_Gateway.Settings.Servers.SobekCM_Web_Server_IP = ip;

                    Engine_Database.Set_Setting("SobekCM Web Server IP", ip);
                }
            }

            // (TEMPORARY FOR UF)
            if ((!String.IsNullOrEmpty(UI_ApplicationCache_Gateway.Settings.System.System_Code)) && (UI_ApplicationCache_Gateway.Settings.System.System_Code.IndexOf("UFDC") == 0))
            {
                UI_ApplicationCache_Gateway.Settings.Servers.Base_Directory = AppRoot_Gateway.AppRootPath;
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