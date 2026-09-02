using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using SobekCM.Core.Client;
using SobekCM.Core.FileSystems;
using SobekCM.Core.Settings;
using SobekCM.Library.UI;
using System;
using System.IO;

namespace SobekCM.Startup
{
    /// <summary> Two early-pipeline, per-request middlewares: forwarding to HTTPS when configured,
    /// and making sure the DB-backed base URL, the microservices config, and SobekFileSystem are
    /// initialized before any request-handling code runs. </summary>
    public static class RequestContextMiddleware
    {
        public static void Configure(WebApplication app)
        {
            // Read once at startup -- appsettings.json doesn't change per-request like the DB-backed
            // server settings below do.
            string gcsServiceAccountJsonPathOverride = app.Configuration["GCS:ServiceAccountJsonPath"];

            // Same treatment as the GCS key above: a secret's location is local machine config, not a
            // DB-backed instance setting. Unlike the GCS override, nothing needs this re-applied per
            // request -- it's just set once here for JPEG2000_ItemViewer (SobekCM_Library) to read.
            ImageServerSharedKey.Path = app.Configuration["ImageServer:SharedKeyPath"];
            // Forward non-static-file requests to HTTPS when the "Forward to Https" server setting is
            // enabled. Placed after the UseStaticFiles blocks, so static asset requests (images, css,
            // js, etc.) are already served by the time this runs and never reach it -- only "real"
            // application requests do. UseForwardedHeaders (registered earlier) already normalizes
            // Request.IsHttps/Scheme/Host to reflect the original client request even when IIS
            // terminates TLS and forwards to Kestrel over plain HTTP, so this is safe behind that setup.
            // Checked per-request (not gated at startup like OpenTelemetry) so toggling the setting via
            // the admin UI takes effect on the next request, without an app restart.
            app.Use(async (context, next) =>
            {
                if ((UI_ApplicationCache_Gateway.Settings?.Servers != null) &&
                    (UI_ApplicationCache_Gateway.Settings.Servers.Forward_To_Https) &&
                    (!context.Request.IsHttps))
                {
                    string httpsUrl = "https://" + context.Request.Host + context.Request.PathBase + context.Request.Path + context.Request.QueryString;
                    context.Response.Redirect(httpsUrl, false);
                    return;
                }

                await next(context);
            });

            // Ensure base URL is populated before any request processing
            app.Use(async (context, next) =>
            {

                if ((UI_ApplicationCache_Gateway.Settings != null) && (UI_ApplicationCache_Gateway.Settings.Servers != null) &&
                    ((String.IsNullOrEmpty(UI_ApplicationCache_Gateway.Settings.Servers.System_Base_URL)) ||
                    (String.IsNullOrEmpty(UI_ApplicationCache_Gateway.Settings.Servers.Application_Server_URL))))
                {
                    string baseUrl = $"{context.Request.Scheme}://{context.Request.Host}/";

                    UI_ApplicationCache_Gateway.Settings.Servers.Application_Server_URL = baseUrl;
                    UI_ApplicationCache_Gateway.Settings.Servers.System_Base_URL = baseUrl;
                    UI_ApplicationCache_Gateway.Settings.Servers.Base_URL = baseUrl;
                }

#if DEBUG
                // DB-configured server settings point at the real remote host (e.g. test.sobekdigital.com)
                // even on a dev box; force System_Base_URL back to localhost here, before the microservices
                // config is resolved below, so Engine_URL and every [BASEURL] endpoint point at this machine.
                if (UI_ApplicationCache_Gateway.Settings?.Servers != null)
                {
                    string requestBaseUrl = $"{context.Request.Scheme}://{context.Request.Host}/";
                    if (requestBaseUrl.IndexOf("localhost:") > 0)
                    {
                        UI_ApplicationCache_Gateway.Settings.Servers.System_Base_URL = requestBaseUrl;
                        UI_ApplicationCache_Gateway.Settings.Servers.Base_URL = requestBaseUrl;
                        UI_ApplicationCache_Gateway.Settings.Servers.Application_Server_URL = requestBaseUrl;
                    }
                }
#endif

                if (!SobekEngineClient.Config_Read_Attempted && UI_ApplicationCache_Gateway.Settings?.Servers != null)
                {
                    string configPath = Path.Combine(app.Environment.ContentRootPath, "config", "default", "sobekcm_microservices.config");
                    SobekEngineClient.Read_Config_File(configPath, UI_ApplicationCache_Gateway.Settings.Servers.System_Base_URL);
                }

                SobekFileSystem.Initialize(UI_ApplicationCache_Gateway.Settings, gcsServiceAccountJsonPathOverride);

                await next();
            });
        }
    }
}
