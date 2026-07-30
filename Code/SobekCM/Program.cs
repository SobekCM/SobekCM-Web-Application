using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using SobekCM.Core.Client;
using SobekCM.Core.Configuration.Authentication;
using SobekCM.Core.FileSystems;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.Navigation;
using SobekCM.Core.Users;
using SobekCM.Engine_Library;
using SobekCM.Engine_Library.ApplicationState;
using SobekCM.Library.Authentication;
using SobekCM.Library.Database;
using SobekCM.Library.Helpers.CKEditor;
using SobekCM.Library.Helpers.UploadiFive;
using SobekCM.Library.MainWriters;
using SobekCM.Library.UI;
using SobekCM.Tools;
using Sustainsys.Saml2;
using Sustainsys.Saml2.AspNetCore2;
using Sustainsys.Saml2.Metadata;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Saxon.Eej.functions.extfn.VendorFunctionSetPE;

namespace SobekCM
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

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

            // Capture the real content root once, before any request is served. AppDomain.CurrentDomain.BaseDirectory
            // no longer equals the site root under Kestrel (see ContentRoot_Gateway remarks), so library code that
            // needs to locate on-disk site content reads this instead. Moved up from after builder.Build() (where
            // it used to live) because config now needs to be loadable before AddAuthentication() runs below —
            // ASP.NET Core authentication schemes must be registered before the app is built.
            ContentRoot_Gateway.ContentRootPath = builder.Environment.ContentRootPath + "/";

            // Eagerly load configuration — including Authentication_Configuration — so one OIDC/SAML
            // authentication scheme can be registered per configured provider before the app is built.
            // UI_ApplicationCache_Gateway.ResetAll() also runs this lazily on first request; calling it
            // again there is harmless (it's the same idempotent refresh).
            Engine_ApplicationCache_Gateway.RefreshAll();

            Authentication_Configuration authConfig = Engine_ApplicationCache_Gateway.Configuration?.Authentication;

            // Used only to carry short-lived state (nonce/returnUrl) across the redirect round-trip for
            // the OIDC/SAML providers registered below — never read anywhere else in the app as identity.
            // Everything downstream of a successful sign-in uses the existing User_Object-in-session model
            // (see IFederated_Authentication_Provider remarks), not this cookie or its ClaimsPrincipal.
            const string AUTH_CORRELATION_SCHEME = "SobekCM.AuthCorrelation";

            // Captured by the SAML notification closure below (which lacks direct HttpContext access,
            // unlike the OIDC handler's events); assigned once the real value exists, after app.Build().
            IHttpContextAccessor httpContextAccessor = null;

            AuthenticationBuilder authBuilder = builder.Services.AddAuthentication();
            authBuilder.AddCookie(AUTH_CORRELATION_SCHEME, options =>
            {
                options.Cookie.Name = AUTH_CORRELATION_SCHEME;
            });

            if (authConfig != null)
            {
                foreach (Oidc_Configuration oidcConfig in authConfig.Oidc)
                {
                    if ((!oidcConfig.Enabled) || (String.IsNullOrEmpty(oidcConfig.Provider_Code)))
                        continue;

                    string providerCode = oidcConfig.Provider_Code;
                    authBuilder.AddOpenIdConnect(providerCode, options =>
                    {
                        options.SignInScheme = AUTH_CORRELATION_SCHEME;
                        options.Authority = oidcConfig.Authority;
                        options.ClientId = oidcConfig.ClientId;
                        options.ClientSecret = oidcConfig.ClientSecret;
                        options.ResponseType = "code";
                        options.CallbackPath = "/my/oidc/" + providerCode + "/callback";
                        options.SaveTokens = false;

                        // Keep raw OIDC claim names (e.g. "sub", "given_name") instead of ASP.NET Core's
                        // default long-URI claim-type remapping, matching what admins configure in
                        // sobekcm_authentication.config's <mapping Name="..."> entries
                        options.MapInboundClaims = false;

                        options.Scope.Add("profile");
                        options.Scope.Add("email");

                        options.Events = new OpenIdConnectEvents
                        {
                            // Fires after the OIDC handler has already validated the token (signature,
                            // issuer, audience, nonce). This is the ONLY place Complete_SignIn is called
                            // for OIDC — the callback request never reaches QueryInitializer/a viewer,
                            // since UseAuthentication() intercepts CallbackPath directly. See
                            // IFederated_Authentication_Provider's remarks for the full explanation.
                            OnTokenValidated = async ctx =>
                            {
                                var provider = Authentication_Provider_Gateway.Get_Federated_Provider(providerCode) as Oidc_Authentication_Provider;
                                var tracer = new Custom_Tracer();
                                User_Object user = provider != null ? await provider.Complete_SignIn(ctx.Principal, tracer) : null;

                                if (user == null)
                                {
                                    ctx.Fail("Unable to establish a user account for this identity");
                                    try
                                    {
                                        string logPath = Path.Combine(ContentRoot_Gateway.ContentRootPath, "temp", "exceptions.txt");
                                        await File.AppendAllTextAsync(logPath, "\n\n" + tracer.Text_Trace + "\n\n");
                                    }
                                    catch
                                    {
                                        // Nothing else to do here.. no other known way to log this error
                                    }
                                    return;
                                }

                                ctx.HttpContext.Session.SetString(SessionCache_Keys.User, CachedDataManager_UserCacheServices.UserToString(user));

                                string returnUrl = (ctx.Properties?.Items != null) && ctx.Properties.Items.TryGetValue("returnUrl", out string r) && (!String.IsNullOrEmpty(r))
                                    ? r : "/";
                                ctx.Properties.RedirectUri = returnUrl;
                            }
                        };
                    });
                }

                // SAML providers, via Sustainsys.Saml2. Verified against the actual installed package
                // (Sustainsys.Saml2.AspNetCore2 2.11.0) rather than guessed, but the returnUrl round-trip
                // through CommandResult.RelayData specifically has not been confirmed against a real IdP —
                // Saml2Handler doesn't derive from ASP.NET Core's RemoteAuthenticationHandler<T>, so it
                // doesn't necessarily propagate AuthenticationProperties.Items the same way the OIDC
                // handler does. Verify this with a real IdP before relying on it.
                foreach (Saml_Configuration samlConfig in authConfig.Saml)
                {
                    if ((!samlConfig.Enabled) || (String.IsNullOrEmpty(samlConfig.Provider_Code)))
                        continue;

                    string providerCode = samlConfig.Provider_Code;
                    authBuilder.AddSaml2(providerCode, options =>
                    {
                        options.SignInScheme = AUTH_CORRELATION_SCHEME;

                        // ModulePath defaults to a fixed "/Saml2" shared across every scheme, which would
                        // collide as soon as a second SAML provider is configured (all of them would answer
                        // to the same Acs/Metadata/etc. URLs). Scope it per provider, mirroring the OIDC
                        // CallbackPath pattern above - but NOT to "/my/saml/{ProviderCode}" alone: that's the
                        // exact same path Saml_Landing_MySobekViewer's "Sign in" link already points at to
                        // *begin* the login (see UrlWriterHelper's SAML_Landing case). Since Sustainsys.Saml2's
                        // Saml2Handler (registered via UseAuthentication(), which runs before this app's own
                        // routing - see PrettyUrl_Rewrite's comment below) claims every request whose path
                        // starts with ModulePath, an exact-match collision means Sustainsys itself swallows
                        // the landing click before Saml_Landing_MySobekViewer/ChallengeAsync ever runs - it
                        // falls back to its own default command (observed as an unwanted metadata-file
                        // download) instead of redirecting to the IdP. The "/sso" suffix keeps ModulePath a
                        // strict sub-path so the bare landing URL falls through to this app's own routing.
                        // This changes the Reply URL (ACS) each IdP must have registered to
                        // "https://<host>/my/saml/{ProviderCode}/sso/Acs".
                        options.SPOptions.ModulePath = "/my/saml/" + providerCode + "/sso";

                        options.SPOptions.EntityId = new EntityId(samlConfig.EntityId);
                        options.IdentityProviders.Add(new IdentityProvider(new EntityId(samlConfig.IdpEntityId), options.SPOptions)
                        {
                            MetadataLocation = samlConfig.IdpMetadataUrl,
                            LoadMetadata = true
                        });

                        // Sustainsys.Saml2's older "Notifications" delegate API doesn't hand this event an
                        // HttpContext (unlike the OIDC handler's Events), so the ambient HttpContext is
                        // resolved via IHttpContextAccessor instead.
                        options.Notifications.AcsCommandResultCreated = (commandResult, samlResponse) =>
                        {
                            HttpContext currentContext = httpContextAccessor?.HttpContext;
                            if ((commandResult.Principal == null) || (currentContext == null))
                                return;

                            var provider = Authentication_Provider_Gateway.Get_Federated_Provider(providerCode) as Saml_Authentication_Provider;
                            if (provider == null)
                                return;

                            var tracer = new Custom_Tracer();
                            User_Object user = provider.Complete_SignIn(commandResult.Principal, tracer).GetAwaiter().GetResult();
                            if (user == null)
                                return;

                            currentContext.Session.SetString(SessionCache_Keys.User, CachedDataManager_UserCacheServices.UserToString(user));

                            if ((commandResult.RelayData != null) && commandResult.RelayData.TryGetValue("returnUrl", out string returnUrl) && (!String.IsNullOrEmpty(returnUrl)))
                                commandResult.Location = new Uri(returnUrl);
                        };
                    });
                }
            }

            var app = builder.Build();

            httpContextAccessor = app.Services.GetRequiredService<IHttpContextAccessor>();

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
            forwardedHeadersOptions.KnownNetworks.Clear();
            forwardedHeadersOptions.KnownProxies.Clear();
            app.UseForwardedHeaders(forwardedHeadersOptions);

            // Global last-resort exception log — replaces Global.asax's Application_Error.
            // Most request paths (sobekcm_data.aspx, sobekcm_oai.aspx, the SobekCM fallback route)
            // already catch their own exceptions and route through Html_MainWriter's Error display,
            // which logs to temp\exceptions.txt itself. This middleware only catches what those miss
            // (other endpoints, or anything thrown before/outside page-load handling). The old
            // Application_Error email-on-error branch is intentionally not ported — it never worked.
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    Exception ee = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
                    if (ee != null)
                    {
                        try
                        {
                            string logPath = Path.Combine(app.Environment.ContentRootPath, "temp", "exceptions.txt");
                            await File.AppendAllTextAsync(logPath,
                                "\nError caught in global exception handler ( " + DateTime.Now + " )\n" +
                                "User Host Address: " + (context.Connection.RemoteIpAddress?.ToString() ?? "") + "\n" +
                                "Requested URL: " + context.Request.GetDisplayUrl() + "\n" +
                                "Error Message: " + ee.Message + "\n" +
                                "Stack Trace: " + ee.StackTrace + "\n" +
                                "Inner Exception: " + (ee.InnerException != null ? ee.InnerException.Message + "\n" + ee.InnerException.StackTrace : "(none)") + "\n" +
                                "------------------------------------------------------------------\n");
                        }
                        catch
                        {
                            // Nothing else to do here.. no other known way to log this error
                        }
                    }

                    string errorUrl = UI_ApplicationCache_Gateway.Settings?.Servers?.System_Error_URL;
                    if (!string.IsNullOrEmpty(errorUrl))
                    {
                        context.Response.Redirect(errorUrl);
                    }
                    else
                    {
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        context.Response.ContentType = "text/plain";
                        await context.Response.WriteAsync("An unexpected error occurred.");
                    }
                });
            });

            app.UseSession();

            // Must run before PrettyUrl_Rewrite below, so each OIDC/SAML scheme's CallbackPath is
            // matched against the pristine incoming request path. Handles the OIDC/SAML identity
            // provider return leg directly and short-circuits the rest of the pipeline for it — see
            // IFederated_Authentication_Provider's remarks.
            app.UseAuthentication();

            // Static files (CSS, JS, images) served from wwwroot
            var contentTypeProvider = new FileExtensionContentTypeProvider();
            contentTypeProvider.Mappings[".glsl"] = "application/octet-stream";
            app.UseStaticFiles(new StaticFileOptions
            {
                ContentTypeProvider = contentTypeProvider,
                OnPrepareResponse = ctx =>
                {
                    // 1-day client cache
                    ctx.Context.Response.Headers.CacheControl = "public,max-age=86400";
                }
            });

            // Legacy IIS-era static content lives directly under the project root (no wwwroot),
            // e.g. SobekCM/design/skins/open/OPEN.css. Map each known asset folder explicitly —
            // rather than pointing static files at the whole content root — so bin/, obj/, and
            // the source tree are never exposed over HTTP. config/ is deliberately excluded: it
            // holds sobekcm.config, which has a plaintext DB connection string — read server-side
            // only, never served.
            string[] staticContentFolders = { "data", "default", "design", "iipimage", "mySobek", "plugins", "temp" };
            foreach (string folder in staticContentFolders)
            {
                string physicalPath = Path.Combine(app.Environment.ContentRootPath, folder);
                if (Directory.Exists(physicalPath))
                {
                    app.UseStaticFiles(new StaticFileOptions
                    {
                        FileProvider = new PhysicalFileProvider(physicalPath),
                        RequestPath = "/" + folder,
                        ContentTypeProvider = contentTypeProvider,
                        OnPrepareResponse = ctx =>
                        {
                            ctx.Context.Response.Headers.CacheControl = "public,max-age=86400";
                        }
                    });
                }
            }

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

                if (!SobekEngineClient.Config_Read_Attempted && UI_ApplicationCache_Gateway.Settings?.Servers != null)
                {
                    string configPath = Path.Combine(app.Environment.ContentRootPath, "config", "default", "sobekcm_microservices.config");
                    SobekEngineClient.Read_Config_File(configPath, UI_ApplicationCache_Gateway.Settings.Servers.System_Base_URL);
                }

                SobekFileSystem.Initialize(
                    UI_ApplicationCache_Gateway.Settings?.Servers?.Image_Server_Network ?? "",
                    UI_ApplicationCache_Gateway.Settings?.Servers?.Image_URL ?? "");

                await next();
            });

            // ── Pretty URL rewriting (replaces the old SobekCM_URL_Rewriter IHttpModule) ──
            // Runs after UseStaticFiles, so requests for files that actually exist on disk
            // never reach it; only handles requests that fall through as bare item/aggregation
            // paths, e.g. /AA00008275/00001/3j
            app.Use(async (context, next) =>
            {
                await PrettyUrl_Rewrite(context, next);
            });

            // ── File serving endpoint (replaces Files.aspx) ──────────────────────────
            app.Map("/files/{**urlrelative}", async (HttpContext context, string urlrelative) =>
            {
                await Files_Handler(context, urlrelative ?? "");
            });

            // ── Engine microservice endpoint (replaces sobekcm.svc IHttpHandler) ────
            // In the old app, an IHttpModule rewrote /engine/... requests to
            // ~/sobekcm.svc?urlrelative=... within this SAME application — sobekcm.svc
            // was never a separately deployed site. MicroserviceHandler is that same
            // handler, just invoked directly and in-process here instead of via rewrite.
            app.Map("/engine/{**urlrelative}", async (HttpContext context, string urlrelative) =>
            {
                await Engine_Handler(context, urlrelative ?? "");
            });

            // ── HTML editor file upload (replaces HtmlEditFileHandler.ashx) ──────────
            app.MapPost("/htmleditfilehandler.ashx", async (HttpContext context) =>
            {
                await HtmlEdit_Upload_Handler(context);
            });

            // ── UploadiFive file upload (replaces UploadiFiveFileHandler.ashx) ───────
            app.MapPost("/uploadifivefilehandler.ashx", async (HttpContext context) =>
            {
                await UploadiFive_Upload_Handler(context);
            });

            //// ── Map search callback (replaces CallBacks.aspx WebMethod) ──────────────
            //app.MapPost("/default/callbacks/callbacks.aspx/MapSearch", async (HttpContext context) =>
            //{
            //    string sendData = "";
            //    using var reader = new StreamReader(context.Request.Body);
            //    sendData = await reader.ReadToEndAsync();
            //    SobekCM_Database.Connection_String = UI_ApplicationCache_Gateway.Settings.Database_Connection.Connection_String;
            //    object result = Google_Map_ResultsViewer.Process_MapSearch_Callback(sendData);
            //    await context.Response.WriteAsJsonAsync(result);
            //});

            // ── Dashboard (replaces Dashboard.aspx) ──────────────────────────────────
            app.Map("/dashboard.aspx", async (HttpContext context) =>
            {
                context.Response.ContentType = "text/html; charset=utf-8";
                using var writer = new StringWriter();

                writer.WriteLine("<!DOCTYPE html><html><head><title>SobekCM Dashboard</title></head><body>");

                if (context.SessionObject()[SessionCache_Keys.LastException] is Exception lastException)
                {
                    if (lastException is SobekCM_Traced_Exception traced)
                    {
                        writer.WriteLine("<h1>EXCEPTION CAUGHT</h1>");
                        writer.WriteLine("<h2>SobekCM Message</h2><blockquote>" + traced.Message + "</blockquote>");
                        writer.WriteLine("<h2>Inner Message</h2><blockquote>" + traced.InnerException?.Message + "</blockquote>");
                        if (!string.IsNullOrEmpty(traced.InnerException?.StackTrace))
                            writer.WriteLine("<h2>Stack Trace</h2><blockquote>" + traced.InnerException.StackTrace.Replace("\n", "<br />") + "</blockquote>");
                        writer.WriteLine("<h2>SobekCM Tracer</h2><blockquote>" + traced.Trace_Route_HTML + "</blockquote>");
                    }
                    else
                    {
                        writer.WriteLine("<h1>EXCEPTION CAUGHT</h1>");
                        writer.WriteLine("<h2>Message</h2><blockquote>" + lastException.Message + "</blockquote>");
                        if (!string.IsNullOrEmpty(lastException.StackTrace))
                            writer.WriteLine("<h2>Stack Trace</h2><blockquote>" + lastException.StackTrace.Replace("\n", "<br />") + "</blockquote>");
                    }
                    context.SessionObject()[SessionCache_Keys.LastException] = null;
                }
                else
                {
                    writer.WriteLine("<h1>SobekCM Dashboard</h1>");
                    writer.WriteLine("This dashboard displays exceptions when the application is run locally.");
                }

                writer.WriteLine("</body></html>");

                await context.Response.WriteAsync(writer.ToString(), Encoding.UTF8);
            });

            // ── Data/JSON/XML endpoint (replaces SobekCM_data.aspx) ─────────────────
            app.Map("/sobekcm_data.aspx", async (HttpContext context) =>
            {
                var pageGlobals = new QueryInitializer(context, "SOBEKCM_DATA");
                try
                {
                    pageGlobals.On_Page_Load();
                }
                catch (OutOfMemoryException ee)
                {
                    if (pageGlobals.currentMode != null)
                    {
                        pageGlobals.currentMode.Mode = Display_Mode_Enum.Error;
                        pageGlobals.currentMode.Error_Message = "SobekCM Out of Memory Exception";
                        pageGlobals.currentMode.Caught_Exception = ee;
                    }
                }
                catch (Exception ee)
                {
                    if (pageGlobals.currentMode != null)
                    {
                        pageGlobals.currentMode.Mode = Display_Mode_Enum.Error;
                        pageGlobals.currentMode.Error_Message = ee.Message;
                        pageGlobals.currentMode.Caught_Exception = ee;
                    }
                }

                if (pageGlobals.mainWriter != null)
                {
                    using var writer = new StringWriter();
                    pageGlobals.mainWriter.Write_Body(writer, pageGlobals.tracer);
                    await context.Response.WriteAsync(writer.ToString(), Encoding.UTF8);
                }
            });

            // ── OAI-PMH endpoint (replaces SobekCM_oai.aspx) ────────────────────────
            app.Map("/sobekcm_oai.aspx", async (HttpContext context) =>
            {
                var pageGlobals = new QueryInitializer(context, "SOBEKCM_OAI");
                if (pageGlobals.currentMode != null)
                    pageGlobals.currentMode.Writer_Type = Writer_Type_Enum.OAI;

                try
                {
                    pageGlobals.On_Page_Load();
                }
                catch (OutOfMemoryException ee)
                {
                    if (pageGlobals.currentMode != null)
                    {
                        pageGlobals.currentMode.Mode = Display_Mode_Enum.Error;
                        pageGlobals.currentMode.Error_Message = "SobekCM Out of Memory Exception";
                        pageGlobals.currentMode.Caught_Exception = ee;
                    }
                }
                catch (Exception ee)
                {
                    if (pageGlobals.currentMode != null)
                    {
                        pageGlobals.currentMode.Mode = Display_Mode_Enum.Error;
                        pageGlobals.currentMode.Error_Message = ee.Message;
                        pageGlobals.currentMode.Caught_Exception = ee;
                    }
                }

                if (pageGlobals.mainWriter != null)
                {
                    using var writer = new StringWriter();
                    pageGlobals.mainWriter.Write_Body(writer, pageGlobals.tracer);
                    await context.Response.WriteAsync(writer.ToString(), Encoding.UTF8);
                }
            });

            // ── Main SobekCM catch-all (replaces SobekCM.aspx) ──────────────────────
            app.MapFallback(async (HttpContext context) =>
            {
                var request_url = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}";

                //context.Response.Redirect("/sobekcm.aspx" + context.Request.QueryString);
                bool isPostBack = string.Equals(context.Request.Method, "POST", StringComparison.OrdinalIgnoreCase);
                var pageGlobals = new QueryInitializer(context, "SOBEKCM");

                // The constructor may have already redirected (e.g. logoff) and have nothing left to do
                if (pageGlobals.currentMode == null || pageGlobals.currentMode.Request_Completed)
                    return;

                try
                {
                    pageGlobals.On_Page_Load();
                }
                catch (OutOfMemoryException ee)
                {
                    if (pageGlobals.currentMode != null)
                    {
                        pageGlobals.currentMode.Mode = Display_Mode_Enum.Error;
                        pageGlobals.currentMode.Error_Message = "SobekCM Out of Memory Exception";
                        pageGlobals.currentMode.Caught_Exception = ee;
                    }
                }
                catch (Exception ee)
                {
                    if (pageGlobals.currentMode != null)
                    {
                        pageGlobals.currentMode.Mode = Display_Mode_Enum.Error;
                        pageGlobals.currentMode.Error_Message = "Unknown error caught while executing your request";
                        pageGlobals.currentMode.Caught_Exception = ee;
                    }
                }

                // No HTML rendering is needed, a redirect was likely called
                if (pageGlobals.currentMode.Request_Completed)
                    return;

                // Save the current URL to session for "back" navigation
                string originalUrl = context.Items[RequestCache_Keys.OriginalUrl]?.ToString() ?? context.Request.GetDisplayUrl();

                if (pageGlobals.currentMode.Mode != Display_Mode_Enum.Preferences &&
                    pageGlobals.currentMode.Mode != Display_Mode_Enum.Contact)
                {
                    context.Session.SetString(SessionCache_Keys.LastMode, originalUrl);
                }

                context.Response.ContentType = "text/html; charset=utf-8";
                using var writer = new StringWriter();

                // Mirrors the SobekCM.aspx template structure
                writer.Write("<!DOCTYPE html>");

                writer.Write("<html lang=\"");
                if (pageGlobals.currentMode.Language == "default")
                    writer.Write(UI_ApplicationCache_Gateway.Configuration.Languages.Default_Language?.Code ?? "en");
                else
                    writer.Write(pageGlobals.currentMode.Language);
                writer.Write("\">");

                writer.Write("<head>");
                writer.Write("<title>");
                if ((pageGlobals.mainWriter.Writer_Type == Writer_Type_Enum.HTML) || (pageGlobals.mainWriter.Writer_Type == Writer_Type_Enum.HTML_LoggedIn))
                    writer.Write(((Html_MainWriter)pageGlobals.mainWriter).Get_Page_Title(pageGlobals.tracer));
                else if (pageGlobals.mainWriter.Writer_Type == Writer_Type_Enum.HTML_Echo)
                    writer.Write(pageGlobals.currentMode.Info_Browse_Mode);
                writer.Write("</title>");

                if ((pageGlobals.mainWriter.Writer_Type == Writer_Type_Enum.HTML) || (pageGlobals.mainWriter.Writer_Type == Writer_Type_Enum.HTML_LoggedIn))
                    ((Html_MainWriter)pageGlobals.mainWriter).Write_Within_HTML_Head(writer, pageGlobals.tracer);
                else if (pageGlobals.mainWriter.Writer_Type == Writer_Type_Enum.HTML_Echo)
                    ((Html_Echo_MainWriter)pageGlobals.mainWriter).Write_Within_HTML_Head(writer, pageGlobals.tracer);

                writer.Write("</head>");

                writer.Write("<body");
                if ((pageGlobals.mainWriter.Writer_Type == Writer_Type_Enum.HTML) || (pageGlobals.mainWriter.Writer_Type == Writer_Type_Enum.HTML_LoggedIn))
                    writer.Write(" " + ((Html_MainWriter)pageGlobals.mainWriter).Get_Body_Attributes(pageGlobals.tracer));
                else if ((pageGlobals.mainWriter.Writer_Type == Writer_Type_Enum.HTML_Echo) && (pageGlobals.currentMode.Mode == Display_Mode_Enum.Item_Display))
                    writer.Write(" id=\"itembody\"");
                writer.Write(">");

                pageGlobals.mainWriter.Write_Body(writer, pageGlobals.tracer);

                writer.Write("</body></html>");

                await context.Response.WriteAsync(writer.ToString(), Encoding.UTF8);
            });

            app.Run();
        }

        /// <summary> Rewrites bare item/aggregation paths (e.g. /AA00008275/00001/3j) into the
        /// urlrelative query parameter the rest of the pipeline expects, and handles a handful of
        /// passthroughs and special cases that used to live in the SobekCM_URL_Rewriter IHttpModule. </summary>
        /// <remarks> Portal resolution no longer needs to be threaded through here — <see cref="SobekCM.QueryInitializerHelpers.UrlInitializer"/>
        /// already derives Base_URL directly from the request host. The old rewriter's static-file
        /// extension checks are also gone: UseStaticFiles, registered earlier in the pipeline, already
        /// serves anything that exists on disk before this middleware ever runs. </remarks>
        private static async Task PrettyUrl_Rewrite(HttpContext context, Func<Task> next)
        {
            if (context.Request.Path.Value.Contains(".css") || context.Request.Path.Value.Contains(".js") || 
                context.Request.Path.Value.Contains(".jpg") || context.Request.Path.Value.Contains(".gif") ||
                context.Request.Path.Value.Contains(".png"))
                return;

            string relative = (context.Request.Path.Value ?? "").Trim('/').ToLower();
            string host = context.Request.Host.Host;

            // Leave requests for already-mapped routes alone — otherwise a direct hit to e.g.
            // /sobekcm_data.aspx would fall into the generic rewrite below and get a bogus
            // urlrelative=sobekcm_data.aspx injected into its query string.
            if (relative == "robots.txt" || relative == "htmleditfilehandler.ashx" || relative == "uploadifivefilehandler.ashx" ||
                relative == "dashboard.aspx" || relative == "sobekcm_data.aspx" || relative == "sobekcm_oai.aspx" ||
                relative.StartsWith("files/") || relative == "engine" || relative.StartsWith("engine/"))
            {
                await next();
                return;
            }

            // Block AmazonBot entirely
            string userAgent = context.Request.Headers.UserAgent.ToString();
            if (userAgent.IndexOf("amazonbot", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                context.Response.Redirect("https://sobekdigital.com/about/", true);
                return;
            }

            // Per-portal favicon, e.g. design/favicons/dcdp.uoc.cw/favicon.ico
            if (relative == "favicon.ico")
            {
                string faviconPath = Path.Combine(UI_ApplicationCache_Gateway.Settings.Servers.Base_Design_Location, "favicons", host, "favicon.ico");
                if (File.Exists(faviconPath))
                {
                    context.Response.ContentType = "image/x-icon";
                    await context.Response.SendFileAsync(faviconPath);
                    return;
                }
                await next();
                return;
            }

            // Nothing to rewrite for the site root
            if (relative.Length == 0)
            {
                await next();
                return;
            }

            // Save the pre-rewrite URL for later reference (e.g. "back" links, email logs)
            context.Items[RequestCache_Keys.OriginalUrl] = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}";

            // dataset/, xml/, json/, dataprovider/, iiif/ prefixed paths route to the data endpoint
            if (relative.StartsWith("dataset/") || relative.StartsWith("xml/") || relative.StartsWith("json/") || relative.StartsWith("dataprovider/") || relative.StartsWith("iiif/"))
            {
                Add_UrlRelative_To_QueryString(context, relative);
                context.Request.Path = "/sobekcm_data.aspx";
                await next();
                return;
            }

            // Everything else: fold the path into urlrelative and let the main fallback handler resolve it
            Add_UrlRelative_To_QueryString(context, relative);
            await next();
        }

        private static void Add_UrlRelative_To_QueryString(HttpContext context, string relative)
        {
            string existing = context.Request.QueryString.HasValue ? context.Request.QueryString.Value.TrimStart('?') : "";
            string merged = "urlrelative=" + Uri.EscapeDataString(relative);
            if (!string.IsNullOrEmpty(existing))
                merged += "&" + existing;
            context.Request.QueryString = new QueryString("?" + merged);
        }

        /// <summary> Dispatches a request to the engine's MicroserviceHandler, translating the
        /// route-captured path (everything after /engine/) into the "urlrelative" query parameter
        /// it expects — the same value the old rewriter produced via appRelative.Substring(6). </summary>
        private static async Task Engine_Handler(HttpContext context, string urlrelative)
        {
            string existing = context.Request.QueryString.HasValue ? context.Request.QueryString.Value.TrimStart('?') : "";
            string merged = "urlrelative=" + Uri.EscapeDataString(urlrelative);
            if (!string.IsNullOrEmpty(existing))
                merged += "&" + existing;
            context.Request.QueryString = new QueryString("?" + merged);

            await new MicroserviceHandler().ProcessRequest(context);
        }

        private static async Task Files_Handler(HttpContext context, string urlrelative)
        {
            // Robot check
            string userAgent = context.Request.Headers.UserAgent.ToString();
            string userHostAddress = context.Connection.RemoteIpAddress?.ToString() ?? "";
            if (Navigation_Object.Is_UserAgent_IP_Robot(userAgent, userHostAddress))
            {
                context.Response.Clear();
                await context.Response.WriteAsync("RESTRICTED ITEM");
                return;
            }

            if (urlrelative.Length <= 4)
                return;

            string[] urlParts = urlrelative.ToLower().Split('/');
            var urlList = urlParts.Where(p => p.Length > 0).ToList();

            if (urlList.Count <= 2 || urlList[2].Length != 10)
                return;

            string bibID = urlList[2].ToUpper();
            string vid = null;
            if (urlList.Count > 3)
            {
                string possibleVid = urlList[3].Trim().PadLeft(5, '0');
                if (int.TryParse(possibleVid, out _))
                    vid = possibleVid;
            }

            if (string.IsNullOrEmpty(bibID) || string.IsNullOrEmpty(vid) || urlList.Count <= 4)
                return;

            var filePathBuilder = new StringBuilder(
                UI_ApplicationCache_Gateway.Settings.Servers.Image_Server_Network +
                bibID[..2] + "\\" + bibID[2..4] + "\\" + bibID[4..6] + "\\" + bibID[6..8] + "\\" + bibID[8..] +
                "\\" + vid + "\\" + urlList[4]);
            for (int i = 5; i < urlList.Count; i++)
                filePathBuilder.Append("\\" + urlList[i]);

            string filePath = filePathBuilder.ToString();
            string extension = Path.GetExtension(filePath)?.ToLower();
            if (string.IsNullOrEmpty(extension))
                return;

            if (!UI_ApplicationCache_Gateway.Mime_Types.TryGetValue(extension, out var mimeType) || mimeType.isBlocked)
                return;

            SobekCM.Library.Database.SobekCM_Database.Get_Item_Restrictions(bibID, vid, null, out bool isDark, out short restrictions);

            if (!isDark && restrictions > 0)
            {
                if (context.Session.GetString(SessionCache_Keys.IpRangeMembership) == null)
                {
                    int ipMask = UI_ApplicationCache_Gateway.IP_Restrictions.Restrictive_Range_Membership(userHostAddress);
                    context.Session.SetString(SessionCache_Keys.IpRangeMembership, ipMask.ToString());
                }
                if (int.TryParse(context.Session.GetString(SessionCache_Keys.IpRangeMembership), out int userMask) && (restrictions & userMask) == 0)
                {
                    var possibleUser = CachedDataManager_UserCacheServices.StringToUser(context.Session.GetString(SessionCache_Keys.User));
                    if (possibleUser == null || possibleUser.Authentication_Type != SobekCM.Core.Users.User_Authentication_Type_Enum.Shibboleth)
                        isDark = true;
                }
            }

            if (isDark)
            {
                context.Response.Clear();
                await context.Response.WriteAsync("RESTRICTED ITEM");
                return;
            }

            if (mimeType.shouldForward)
            {
                var forwardBuilder = new StringBuilder(
                    UI_ApplicationCache_Gateway.Settings.Servers.Image_URL +
                    bibID[..2] + "/" + bibID[2..4] + "/" + bibID[4..6] + "/" + bibID[6..8] + "/" + bibID[8..] +
                    "/" + vid + "/" + urlList[4]);
                for (int i = 5; i < urlList.Count; i++)
                    forwardBuilder.Append("/" + urlList[i]);
                context.Response.Redirect(forwardBuilder.ToString());
                return;
            }

            if (!File.Exists(filePath))
                return;

            context.Response.ContentType = mimeType.MIME_Type;
            await using var fileStream = File.OpenRead(filePath);
            await fileStream.CopyToAsync(context.Response.Body);
        }

        private static async Task HtmlEdit_Upload_Handler(HttpContext context)
        {
            string token = context.Request.Query["token"];
            if (string.IsNullOrEmpty(token))
                return;

            if (context.SessionObject()["#CKEDITOR::" + token] is not CKEditor_Security_Token tokenObj)
                return;

            if (!Directory.Exists(tokenObj.UploadPath))
                Directory.CreateDirectory(tokenObj.UploadPath);

            IFormFile upload = context.Request.Form.Files["upload"];
            if (upload == null)
                return;

            string file = SobekCM.Tools.PathTraversalGuard.SanitizeFileName(upload.FileName);
            string savePath = Path.Combine(tokenObj.UploadPath, file);
            await using var stream = File.Create(savePath);
            await upload.CopyToAsync(stream);

            string ckFuncNum = context.Request.Form["CKEditorFuncNum"];
            string url = tokenObj.UploadURL + file;
            await context.Response.WriteAsync($"<script>window.parent.CKEDITOR.tools.callFunction({ckFuncNum}, \"{url}\");</script>");
        }

        private static async Task UploadiFive_Upload_Handler(HttpContext context)
        {
            context.Response.ContentType = "text/plain";

            string tokenKey = context.Request.Form["token"];
            if (string.IsNullOrEmpty(tokenKey))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("No token provided with this request");
                return;
            }

            if (context.SessionObject()["#UPLOADIFIVE::" + tokenKey] is not UploadiFive_Security_Token tokenObj)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("No matching server-side token found for this request");
                return;
            }

            try
            {
                IFormFile postedFile = context.Request.Form.Files[tokenObj.FileObjName];
                if (postedFile == null)
                    return;

                if (!Directory.Exists(tokenObj.UploadPath))
                    Directory.CreateDirectory(tokenObj.UploadPath);

                string extension = Path.GetExtension(postedFile.FileName).ToLower();
                string filename = SobekCM.Tools.PathTraversalGuard.SanitizeFileName(postedFile.FileName);
                string filenameSansExt = Path.GetFileNameWithoutExtension(filename);

                if (filenameSansExt.Contains('.'))
                    filename = filenameSansExt.Replace(".", "_") + extension;
                if (filename.Contains('&'))
                    filename = filename.Replace("&", "");

                if (!string.IsNullOrEmpty(tokenObj.ServerSideFileName))
                {
                    string serverSideFileName = SobekCM.Tools.PathTraversalGuard.SanitizeFileName(tokenObj.ServerSideFileName);
                    filename = serverSideFileName.Contains('.') ? serverSideFileName : serverSideFileName + extension;
                }

                if (!string.IsNullOrEmpty(tokenObj.AllowedFileExtensions))
                {
                    var allowed = tokenObj.AllowedFileExtensions.Split(new[] { '|', ',' }).ToList();
                    if (!allowed.Contains(extension))
                    {
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsync("Invalid extension");
                        return;
                    }
                }

                filename = SobekCM.Tools.PathTraversalGuard.SanitizeFileName(filename);
                string newPath = Path.Combine(tokenObj.UploadPath, filename);
                if (File.Exists(newPath))
                    File.Delete(newPath);

                await using var fileStream = File.Create(newPath);
                await postedFile.CopyToAsync(fileStream);

                if (!string.IsNullOrEmpty(tokenObj.ReturnToken))
                {
                    string existing = context.Session.GetString(tokenObj.ReturnToken);
                    string newToken = string.IsNullOrEmpty(existing) ? filename : existing + "|" + filename;
                    context.Session.SetString(tokenObj.ReturnToken, newToken);
                }

                context.Response.StatusCode = 200;
                await context.Response.WriteAsync(filename);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("Error: " + ex.Message);
            }
        }
    }
}
