using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Engine_Library.ApplicationState;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace SobekCM.Startup
{
    /// <summary> Wires up OpenTelemetry tracing, exported to Google Cloud Trace's native OTLP
    /// endpoint, when the "Enable OpenTelemetry" server setting is on. </summary>
    public static class OpenTelemetryStartup
    {
        private const int MAX_OTEL_SPAN_NAME_LENGTH = 200;

        /// <summary> Registers OpenTelemetry tracing on the builder's service collection, if the
        /// "Enable OpenTelemetry" server setting is on; when it's off, AddOpenTelemetry() is never
        /// called, so there's no exporter trying to reach anything and no instrumentation overhead
        /// at all. The OTLP endpoint and GCP project live in appsettings.json (ops-tunable per
        /// environment), not in the DB-backed settings, since they need to be available independent
        /// of DB connectivity.
        ///
        /// Exports go straight to Google Cloud Trace's native OTLP endpoint rather than to a local
        /// collector -- there's no local collector to manage/monitor, and it doesn't cost request
        /// latency either way: the SDK batches spans in-memory and exports them from a background
        /// timer thread (default every 5s), completely decoupled from request handling. </summary>
        public static void Configure(WebApplicationBuilder builder)
        {
            if (!Engine_ApplicationCache_Gateway.Settings.Servers.Enable_OpenTelemetry)
                return;

            string serviceName = String.IsNullOrEmpty(Engine_ApplicationCache_Gateway.Settings.Servers.Instance_Code)
                ? "SobekCM" : Engine_ApplicationCache_Gateway.Settings.Servers.Instance_Code;
            string otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "https://telemetry.googleapis.com/v1/traces";
            string googleCloudProjectId = builder.Configuration["OpenTelemetry:GoogleCloudProjectId"] ?? String.Empty;

            builder.Services.AddOpenTelemetry()
                .ConfigureResource(resource =>
                {
                    resource.AddService(serviceName);

                    // Cloud Trace's OTLP endpoint requires the destination project identified via
                    // this resource attribute -- unlike the general Telemetry (metrics) API, it does
                    // not reliably infer the project from the authenticated service account alone
                    // (confirmed 2026-08-11: omitting this produced "Resource is missing required
                    // attribute 'gcp.project_id'" 400s once the X-Goog-User-Project header, which
                    // Google's docs otherwise discourage, was removed).
                    if (!String.IsNullOrEmpty(googleCloudProjectId))
                        resource.AddAttributes(new[] { new KeyValuePair<string, object>("gcp.project_id", googleCloudProjectId) });
                })
                .WithTracing(tracing => tracing
                    .AddAspNetCoreInstrumentation(otelAspNetCore =>
                    {
                        // EnrichWithHttpResponse (not EnrichWithHttpRequest) -- see
                        // Build_Otel_Span_Name's doc comment for why.
                        otelAspNetCore.EnrichWithHttpResponse = (activity, httpResponse) =>
                        {
                            string spanName = Build_Otel_Span_Name(httpResponse);
                            if (!String.IsNullOrEmpty(spanName))
                                activity.DisplayName = spanName;

                            // Tag the span with the caller-supplied traceid (see Navigation_Object.
                            // TraceID / QueryString_Analyzer / UrlWriterHelper) so a specific call or
                            // series of calls can be searched for directly in Cloud Trace, not just
                            // visually matched via the span name.
                            string traceId = httpResponse.HttpContext.Request.Query["traceid"];
                            if (!String.IsNullOrEmpty(traceId))
                                activity.SetTag("sobekcm.traceid", traceId);
                        };
                    })
                    .AddHttpClientInstrumentation()
                    .AddSqlClientInstrumentation()
                    // Custom sub-spans emitted by application code via ActivitySource -- registered
                    // explicitly since OTel only samples sources it's told about. Add further
                    // AddSource(...) calls here as more code gets custom instrumentation.
                    .AddSource("SobekCM.METS_Based_ItemBuilder")
                    .AddOtlpExporter(otlp =>
                    {
                        otlp.Endpoint = new Uri(otlpEndpoint);
                        otlp.Protocol = OtlpExportProtocol.HttpProtobuf;
                        otlp.HttpClientFactory = Create_GoogleCloud_Authenticated_HttpClient;
                    }));
        }

        /// <summary> Builds a concise, request-specific OpenTelemetry span display name (HTTP
        /// method + path + query) so traces in Cloud Trace Explorer can be matched back to the
        /// actual request, instead of showing the generic ASP.NET Core route template. Must be
        /// called from EnrichWithHttpResponse (not EnrichWithHttpRequest) -- the AspNetCore
        /// instrumentation overwrites Activity.DisplayName with the route template after
        /// EnrichWithHttpRequest runs, but EnrichWithHttpResponse runs last. </summary>
        private static string Build_Otel_Span_Name(HttpResponse httpResponse)
        {
            HttpContext context = httpResponse.HttpContext;

            string originalUrl = context.Items[RequestCache_Keys.OriginalUrl]?.ToString();
            string pathAndQuery = Extract_PathAndQuery(originalUrl)
                ?? Extract_PathAndQuery(context.Request.GetDisplayUrl());

            if (String.IsNullOrEmpty(pathAndQuery))
                return null; // caller leaves the existing DisplayName (route template) alone

            string name = $"{context.Request.Method} {pathAndQuery}";
            if (name.Length > MAX_OTEL_SPAN_NAME_LENGTH)
                name = name.Substring(0, MAX_OTEL_SPAN_NAME_LENGTH - 1) + "…";

            return name;
        }

        private static string Extract_PathAndQuery(string absoluteUrl)
        {
            if (String.IsNullOrEmpty(absoluteUrl))
                return null;

            return Uri.TryCreate(absoluteUrl, UriKind.Absolute, out Uri parsed)
                ? parsed.PathAndQuery
                : null;
        }

        /// <summary> Builds the HttpClient the OTLP exporter uses to reach Google Cloud Trace's native
        /// OTLP endpoint, which requires Google Cloud auth the plain OTLP exporter doesn't provide.
        /// Application Default Credentials (the GCE VM's own service account when hosted on Google
        /// Cloud) are resolved once and reused -- the underlying credential caches and transparently
        /// refreshes its access token on its own, so a fresh token is fetched per export batch without
        /// this code needing to track expiry itself. </summary>
        private static HttpClient Create_GoogleCloud_Authenticated_HttpClient()
        {
            GoogleCredential credential = GoogleCredential.GetApplicationDefault()
                .CreateScoped("https://www.googleapis.com/auth/cloud-platform");

            var authHandler = new GoogleCloud_Auth_DelegatingHandler(credential)
            {
                InnerHandler = new HttpClientHandler()
            };

            return new HttpClient(authHandler);
        }

        /// <summary> Injects a fresh Google Cloud Application Default Credentials bearer token into each
        /// outgoing OTLP export request. The target project is auto-identified from the service account
        /// credential itself -- Google's docs explicitly discourage setting X-Goog-User-Project manually
        /// for service-account auth, so it's deliberately not set here. Overrides both Send() and
        /// SendAsync() -- the OTLP exporter's HttpProtobuf export path can use either, and a
        /// DelegatingHandler that only overrides one silently skips the auth header for calls made via
        /// the other, which is what caused the initial "unregistered callers" 403s from Google. </summary>
        private sealed class GoogleCloud_Auth_DelegatingHandler : DelegatingHandler
        {
            private readonly GoogleCredential credential;

            public GoogleCloud_Auth_DelegatingHandler(GoogleCredential Credential)
            {
                credential = Credential;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                string accessToken = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync(cancellationToken: cancellationToken);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                return await base.SendAsync(request, cancellationToken);
            }

            protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                string accessToken = credential.UnderlyingCredential.GetAccessTokenForRequestAsync(cancellationToken: cancellationToken).GetAwaiter().GetResult();
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                return base.Send(request, cancellationToken);
            }
        }
    }
}
