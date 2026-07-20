using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SobekCM.Core.Client;
using SobekCM.Core.FileSystems;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.Navigation;
using SobekCM.Library.Database;
using SobekCM.Library.Helpers.CKEditor;
using SobekCM.Library.MainWriters;
using SobekCM.Library.ResultsViewer;
using SobekCM.Library.UI;
using SobekCM.Tools;

namespace SobekCM
{
    // Minimal token class for UploadiFive security (the library helper was removed)
    internal sealed class UploadiFive_Security_Token
    {
        public string FileObjName { get; set; } = "file";
        public string UploadPath { get; set; } = "";
        public string ServerSideFileName { get; set; } = "";
        public string AllowedFileExtensions { get; set; } = "";
        public string ReturnToken { get; set; } = "";
    }

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

            // Wire System.Web.HttpContext.Current.Session to ASP.NET Core ISession
        //    builder.Services.AddSystemWebAdapters().AddWrappedAspNetCoreSession();

            var app = builder.Build();

            app.UseSession();
        //    app.UseSystemWebAdapters();

            // Static files (CSS, JS, images) served from wwwroot
            var contentTypeProvider = new FileExtensionContentTypeProvider();
            contentTypeProvider.Mappings[".glsl"] = "application/octet-stream";
            app.UseStaticFiles(new StaticFileOptions
            {
                ContentTypeProvider = contentTypeProvider,
                OnPrepareResponse = ctx =>
                {
                    // 10-day client cache, matching the previous IIS clientCache setting
                    ctx.Context.Response.Headers.CacheControl = "public,max-age=864000";
                }
            });

            // Ensure base URL is populated before any request processing
            app.Use(async (context, next) =>
            {
                if (string.IsNullOrEmpty(UI_ApplicationCache_Gateway.Settings?.Servers?.System_Base_URL))
                {
                    string baseUrl = $"{context.Request.Scheme}://{context.Request.Host}/";
                    if (UI_ApplicationCache_Gateway.Settings?.Servers != null)
                    {
                        UI_ApplicationCache_Gateway.Settings.Servers.System_Base_URL = baseUrl;
                        UI_ApplicationCache_Gateway.Settings.Servers.Base_URL = baseUrl;
                    }
                }

                if (!SobekEngineClient.Config_Read_Attempted && UI_ApplicationCache_Gateway.Settings?.Servers != null)
                {
                    string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "default", "sobekcm_microservices.config");
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
                await using var writer = new StreamWriter(context.Response.Body, Encoding.UTF8, leaveOpen: true);

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
            });

            // ── Data/JSON/XML endpoint (replaces SobekCM_data.aspx) ─────────────────
            app.Map("/sobekcm_data.aspx", async (HttpContext context) =>
            {
                var pageGlobals = new QueryInitializer(context, "SOBEKCM_DATA");
                try
                {
                    pageGlobals.On_Page_Load();
                }
                catch (OutOfMemoryException ee) { pageGlobals.Email_Information("SobekCM Out of Memory Exception", ee); }
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
                    await using var writer = new StreamWriter(context.Response.Body, Encoding.UTF8, leaveOpen: true);
                    pageGlobals.mainWriter.Write_Html(writer, pageGlobals.tracer);
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
                catch (OutOfMemoryException ee) { pageGlobals.Email_Information("SobekCM Out of Memory Exception", ee); }
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
                    await using var writer = new StreamWriter(context.Response.Body, Encoding.UTF8, leaveOpen: true);
                    pageGlobals.mainWriter.Write_Html(writer, pageGlobals.tracer);
                }
            });

            // ── Main SobekCM catch-all (replaces SobekCM.aspx) ──────────────────────
            app.MapFallback(async (HttpContext context) =>
            {
               //context.Response.Redirect("/sobekcm.aspx" + context.Request.QueryString);
                bool isPostBack = string.Equals(context.Request.Method, "POST", StringComparison.OrdinalIgnoreCase);
                var pageGlobals = new QueryInitializer(context, "SOBEKCM");

                try
                {
                    pageGlobals.On_Page_Load();
                }
                catch (OutOfMemoryException ee)
                {
                    pageGlobals.Email_Information("SobekCM Out of Memory Exception", ee);
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

                if (pageGlobals.currentMode == null || pageGlobals.currentMode.Request_Completed)
                    return;

                // Save the current URL to session for "back" navigation
                string originalUrl = context.Items[RequestCache_Keys.OriginalUrl]?.ToString() ?? context.Request.GetDisplayUrl();

                if (pageGlobals.currentMode.Mode != Display_Mode_Enum.Preferences &&
                    pageGlobals.currentMode.Mode != Display_Mode_Enum.Contact)
                {
                    context.Session.SetString(SessionCache_Keys.LastMode, originalUrl);
                }

                context.Response.ContentType = "text/html; charset=utf-8";
                await using var writer = new StreamWriter(context.Response.Body, Encoding.UTF8, leaveOpen: true);

                // Mirrors the SobekCM.aspx template structure
                writer.Write("<!DOCTYPE html>");

                writer.Write("<html lang=\"");
                if (pageGlobals.currentMode.Language == SobekCM.Core.Configuration.Localization.Web_Language_Enum.DEFAULT)
                    writer.Write(SobekCM.Core.Configuration.Localization.Web_Language_Enum_Converter.Enum_To_Code(UI_ApplicationCache_Gateway.Settings.System.Default_UI_Language));
                else
                    writer.Write(SobekCM.Core.Configuration.Localization.Web_Language_Enum_Converter.Enum_To_Code(pageGlobals.currentMode.Language));
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

                pageGlobals.mainWriter.Write_Html(writer, pageGlobals.tracer);

                if (pageGlobals.mainWriter.Include_Navigation_Form)
                {
                    string formAction = originalUrl;
                    string enctype = pageGlobals.mainWriter.File_Upload_Possible ? " enctype=\"multipart/form-data\"" : "";
                    writer.Write($"<form id=\"itemNavForm\" action=\"{formAction}\" method=\"post\"{enctype}>");

                    if ((pageGlobals.mainWriter.Writer_Type == Writer_Type_Enum.HTML) || (pageGlobals.mainWriter.Writer_Type == Writer_Type_Enum.HTML_LoggedIn))
                        ((Html_MainWriter)pageGlobals.mainWriter).Write_ItemNavForm_Opening(writer, pageGlobals.tracer);

                    if (pageGlobals.mainWriter.Include_Main_Place_Holder)
                        pageGlobals.mainWriter.Add_Controls(writer, pageGlobals.tracer);

                    if ((pageGlobals.mainWriter.Writer_Type == Writer_Type_Enum.HTML) || (pageGlobals.mainWriter.Writer_Type == Writer_Type_Enum.HTML_LoggedIn))
                        ((Html_MainWriter)pageGlobals.mainWriter).Write_ItemNavForm_Closing(writer, pageGlobals.tracer);

                    writer.Write("</form>");
                }

                if ((pageGlobals.mainWriter.Writer_Type == Writer_Type_Enum.HTML) || (pageGlobals.mainWriter.Writer_Type == Writer_Type_Enum.HTML_LoggedIn))
                    ((Html_MainWriter)pageGlobals.mainWriter).Write_Final_HTML(writer, pageGlobals.tracer);

                writer.Write("</body></html>");
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
            string relative = (context.Request.Path.Value ?? "").Trim('/').ToLower();
            string host = context.Request.Host.Host;

            // Leave requests for already-mapped routes alone — otherwise a direct hit to e.g.
            // /sobekcm_data.aspx would fall into the generic rewrite below and get a bogus
            // urlrelative=sobekcm_data.aspx injected into its query string.
            if (relative == "robots.txt" || relative == "htmleditfilehandler.ashx" || relative == "uploadifivefilehandler.ashx" ||
                relative == "dashboard.aspx" || relative == "sobekcm_data.aspx" || relative == "sobekcm_oai.aspx" ||
                relative.StartsWith("files/"))
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

            // USFLDC-specific passthrough and redirection service (OHPi is part of the same USF integration)
            if (relative.IndexOf("ohpi/") >= 0)
            {
                await next();
                return;
            }
            if ((relative.Length == 0) && context.Request.QueryString.HasValue &&
                (host.Contains("usf.edu") || host.Contains("usf.sobek.ufl.edu")))
            {
                USFLDC_Redirection_Service(context);
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

            // dataset/, xml/, json/, dataprovider/ prefixed paths route to the data endpoint
            if (relative.StartsWith("dataset/") || relative.StartsWith("xml/") || relative.StartsWith("json/") || relative.StartsWith("dataprovider/"))
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

        /// <summary> Ported from the old SobekCM_URL_Rewriter.Rewriter.USFLDC_Redirection_Service —
        /// resolves legacy USF PURL handles (item, browse/search, or collection) to the equivalent
        /// SobekCM URL. Kept for USF's benefit; may be dropped in the future. </summary>
        private static void USFLDC_Redirection_Service(HttpContext context)
        {
            const string URL_ERROR = "http://guides.lib.usf.edu/content.php?pid=87781&sid=744350";

            string purlHandle;
            try
            {
                purlHandle = context.Request.QueryString.Value.Substring(1);
            }
            catch
            {
                purlHandle = "";
            }

            if (purlHandle == "m1" || purlHandle.StartsWith("m1."))
            {
                // Courtesy permanently moved redirect for former partner MCPL for the MCPLHPC (CID=M01)
                context.Response.Redirect("http://cdm16681.contentdm.oclc.org", true);
            }
            else if (purlHandle.Contains(".") && !purlHandle.Contains("browse") && !purlHandle.Contains("search"))
            {
                // item purl
                if (purlHandle.Contains("-ead"))
                {
                    // It is an EAD item purl
                    int pos1 = purlHandle.IndexOf("-");
                    int len = pos1 - 4;
                    string doi = "U29-" + int.Parse(purlHandle.Substring(4, len)).ToString("D5") + "-" + purlHandle.Substring(pos1 + 1, 3);
                    string url = "http://dis.lib.usf.edu/aeon/eads/index.html?eadrequest=true&ead_id=" + doi;
                    context.Response.Redirect(url, true);
                    return;
                }

                string packageid = SobekCM_Database.Get_BibID_VID_From_Identifier(purlHandle);
                context.Response.Redirect(packageid != null ? packageid.ToUpper() : URL_ERROR, true);
            }
            else if (purlHandle.Contains(".browse") || purlHandle.Contains(".search"))
            {
                // browse or search purl
                string purlHandleOriginal = purlHandle;
                int pos1 = purlHandle.IndexOf(".");
                purlHandle = purlHandle.Substring(0, pos1);

                if (purlHandle.Length == 2)
                    purlHandle = purlHandle.Substring(0, 1) + "0" + purlHandle.Substring(1);

                string aggregationCode = SobekCM_Database.Get_AggregationCode_From_CID(purlHandle.ToUpper());
                if (aggregationCode != null)
                {
                    string action = purlHandleOriginal.Contains(".browse") ? "/all" : "/advanced";
                    context.Response.Redirect(aggregationCode.ToLower() + action, true);
                }
                else
                {
                    context.Response.Redirect(URL_ERROR, true);
                }
            }
            else if (purlHandle.Length == 2 || purlHandle.Length == 3)
            {
                // collection purl
                if (purlHandle.Length == 2)
                    purlHandle = purlHandle.Substring(0, 1) + "0" + purlHandle.Substring(1);

                string aggregationCode = SobekCM_Database.Get_AggregationCode_From_CID(purlHandle.ToUpper());
                context.Response.Redirect(aggregationCode != null ? aggregationCode.ToLower() : URL_ERROR, true);
            }
            else
            {
                context.Response.Redirect(URL_ERROR, true);
            }
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

            string file = Path.GetFileName(upload.FileName);
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
                string filename = Path.GetFileName(postedFile.FileName);
                string filenameSansExt = Path.GetFileNameWithoutExtension(filename);

                if (filenameSansExt.Contains('.'))
                    filename = filenameSansExt.Replace(".", "_") + extension;
                if (filename.Contains('&'))
                    filename = filename.Replace("&", "");

                if (!string.IsNullOrEmpty(tokenObj.ServerSideFileName))
                    filename = tokenObj.ServerSideFileName.Contains('.') ? tokenObj.ServerSideFileName : tokenObj.ServerSideFileName + extension;

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
