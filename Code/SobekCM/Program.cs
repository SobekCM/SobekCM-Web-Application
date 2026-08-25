using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Endpoints;
using SobekCM.Engine_Library.ApplicationState;
using SobekCM.Engine_Library.Aggregations;
using SobekCM.Engine_Library.Items.BriefItems;
using SobekCM.Library.Database;
using SobekCM.Library.UI;
using SobekCM.Startup;
using System;
using System.IO;
using System.Runtime.Versioning;
using System.Threading;

namespace SobekCM
{
    public class Program
    {
        // This app is always IIS-hosted on Windows (see ProtectKeysWithDpapi below), so the
        // Windows-only Data Protection APIs used here are safe to call unconditionally.
        [SupportedOSPlatform("windows")]
        public static void Main(string[] args)
        {
            // The ThreadPool only grows by ~1 thread per ~500ms once exhausted, so a sudden burst of
            // concurrent requests holding threads on blocking calls (e.g. Solr_Http_Client's
            // GetAwaiter().GetResult(), safe from deadlock under Kestrel but still occupies a thread
            // for the call's duration) stalls badly before the pool ramps up. Raising the minimum
            // avoids that ramp-up lag under real traffic.
            ThreadPool.SetMinThreads(200, 200);

            var builder = WebApplication.CreateBuilder(args);

            // Data Protection keys default to IIS's per-app-pool-identity storage (registry or user
            // profile), which breaks with "Error occurred during a cryptographic operation" whenever the
            // app pool identity/SID changes across a recycle or redeploy. Persisting to a stable folder
            // outside the deployed app path (so redeploys never touch it) and protecting at machine scope
            // rather than the app pool's identity avoids that churn.
            builder.Services.AddDataProtection()
                .SetApplicationName("SobekCM")
                .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "SobekCM", "DataProtection-Keys")))
                .ProtectKeysWithDpapi(protectToLocalMachine: true);

            // Session requires a distributed cache backing store
            var sessionIdleTimeout = TimeSpan.FromMinutes(builder.Configuration.GetValue<int>("Session:IdleTimeoutMinutes", 90));
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = sessionIdleTimeout;
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // SessionObjectStore (complex-object session storage) uses the same idle timeout as
            // ISession itself, so both expire together rather than drifting out of sync
            SessionObjectStore.IdleTimeout = sessionIdleTimeout;

            builder.Services.AddHttpContextAccessor();

            builder.Services.AddHealthChecks()
                .AddCheck<Database_HealthCheck>("database");

            // Capture the real content root once, before any request is served. AppDomain.CurrentDomain.BaseDirectory
            // no longer equals the site root under Kestrel (see AppRoot_Gateway remarks), so library code that
            // needs to locate on-disk site content reads this instead. Moved up from after builder.Build() (where
            // it used to live) because config now needs to be loadable before AddAuthentication() runs below —
            // ASP.NET Core authentication schemes must be registered before the app is built.
            AppRoot_Gateway.AppRootPath = builder.Environment.ContentRootPath + "/";

            // Lets a known, already-diagnosed issue (e.g. a customer's SSL cert problem) be silenced
            // without losing the lighter-weight exceptions.txt summary entries -- flip this on in
            // appsettings.json when trace files are accumulating for something that's already understood.
            ExceptionLog_Gateway.SuppressTraceFiles = builder.Configuration.GetValue<bool>("ErrorHandling:SuppressTraceFiles");

            // Eagerly load configuration — including Authentication_Configuration — so one OIDC/SAML
            // authentication scheme can be registered per configured provider before the app is built.
            // UI_ApplicationCache_Gateway.ResetAll() also runs this lazily on first request; calling it
            // again there is harmless (it's the same idempotent refresh).
            Engine_ApplicationCache_Gateway.RefreshAll();

            // One-time protobuf-net model compilation for the BriefItemInfo cache (cache.protobuf) --
            // see BriefItem_Cache.CompileProtobufModel's doc comment for why this must compile the whole
            // model, not just BriefItemInfo. Belongs here rather than somewhere that could run per-request
            // or more than once: compiling is a fairly expensive one-time cost that only pays for itself
            // amortized across many later (de)serializations.
            BriefItem_Cache.CompileProtobufModel();

            // Same one-time compilation, for the language-specific Item_Aggregation cache (cache_[lang].protobuf)
            // -- see Item_Aggregation_Cache.CompileProtobufModel's doc comment.
            Item_Aggregation_Cache.CompileProtobufModel();

            OpenTelemetryStartup.Configure(builder);
            FederatedAuthenticationStartup.Configure(builder);

            var app = builder.Build();

            // Captured here (rather than in FederatedAuthenticationStartup itself) since the real
            // IHttpContextAccessor instance only exists once the service provider is built --
            // read by the SAML sign-in notification, which lacks direct HttpContext access.
            FederatedAuthenticationStartup.HttpContextAccessor = app.Services.GetRequiredService<IHttpContextAccessor>();
            AppLifetime_Gateway.Lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

            // Must run before session/authentication (and everything else, really) so ASP.NET Core
            // knows the original request was HTTPS when IIS terminates TLS and forwards to Kestrel
            // over plain HTTP. Without this, the app thinks every request is HTTP, which breaks the
            // OIDC/SAML correlation cookie (SameSite=None requires Secure) and produces an opaque
            // "An error was encountered while handling the remote login." (inner: "Correlation
            // failed.") on the callback. KnownNetworks/KnownProxies cleared since IIS is the only
            // hop here and its forwarding doesn't come from a fixed, individually-known address.
            var forwardedHeadersOptions = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            };
            forwardedHeadersOptions.KnownIPNetworks.Clear();
            forwardedHeadersOptions.KnownProxies.Clear();
            app.UseForwardedHeaders(forwardedHeadersOptions);

            ExceptionHandlingMiddleware.Configure(app);

            app.UseSession();

            // Must run before PrettyUrlRewriteMiddleware below, so each OIDC/SAML scheme's CallbackPath
            // is matched against the pristine incoming request path. Handles the OIDC/SAML identity
            // provider return leg directly and short-circuits the rest of the pipeline for it — see
            // IFederated_Authentication_Provider's remarks.
            app.UseAuthentication();

            // Static files (wwwroot, plus the legacy IIS-era content folders)
            StaticFilesStartup.Configure(app);

            // Forward-to-HTTPS + base-URL/SobekFileSystem-init middleware. Registered after
            // StaticFilesStartup so static asset requests never reach it — only "real" application
            // requests do.
            RequestContextMiddleware.Configure(app);
                if (!SobekEngineClient.Config_Read_Attempted && UI_ApplicationCache_Gateway.Settings?.Servers != null)
                {
                    string configPath = Path.Combine(app.Environment.ContentRootPath, "config", "default", "sobekcm_microservices.config");
                    SobekEngineClient.Read_Config_File(configPath, UI_ApplicationCache_Gateway.Settings.Servers.System_Base_URL);
                }

                SobekFileSystem.Initialize(UI_ApplicationCache_Gateway.Settings?.Servers);

                await next();
            });

            // ── Pretty URL rewriting (replaces the old SobekCM_URL_Rewriter IHttpModule) ──
            // Runs after UseStaticFiles, so requests for files that actually exist on disk
            // never reach it; only handles requests that fall through as bare item/aggregation
            // paths, e.g. /AA00008275/00001/3j
            app.Use(PrettyUrlRewriteMiddleware.Invoke);

            // ── Basic health check endpoint ──────────────────────────────────────────
            app.MapHealthChecks("/health");

            // ── File serving endpoint (replaces Files.aspx) ──────────────────────────
            app.Map("/files/{**urlrelative}", FilesEndpoint.Invoke);

            // ── Engine microservice endpoint (replaces sobekcm.svc IHttpHandler) ────
            app.Map("/engine/{**urlrelative}", EngineEndpoint.Invoke);

            // ── HTML editor file upload (replaces HtmlEditFileHandler.ashx) ──────────
            app.MapPost("/htmleditfilehandler.ashx", HtmlEditUploadEndpoint.Invoke);

            // ── UploadiFive file upload (replaces UploadiFiveFileHandler.ashx) ───────
            app.MapPost("/uploadifivefilehandler.ashx", UploadiFiveUploadEndpoint.Invoke);

            // ── Dashboard (replaces Dashboard.aspx) ──────────────────────────────────
            app.Map("/dashboard.aspx", DashboardEndpoint.Invoke);

            // ── Main SobekCM catch-all (replaces SobekCM.aspx, SobekCM_data.aspx and SobekCM_oai.aspx) ──
            // Every main writer (HTML, JSON, XML, IIIF, OAI-PMH, etc.) sets its own Content-Type and produces
            // its own complete response body, so this single handler no longer needs to know which writer
            // type it's dealing with - see abstractMainWriter subclasses' constructors/Write_Body.
            app.MapFallback(SobekCmFallbackEndpoint.Invoke);

            app.Run();
        }
    }
}
