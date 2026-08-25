using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using System.IO;

namespace SobekCM.Startup
{
    /// <summary> Serves static assets: the standard wwwroot pipeline, plus the legacy IIS-era
    /// content folders that live directly under the project root (no wwwroot), e.g.
    /// SobekCM/design/skins/open/OPEN.css. </summary>
    public static class StaticFilesStartup
    {
        // Legacy IIS-era static content lives directly under the project root (no wwwroot).
        // Map each known asset folder explicitly -- rather than pointing static files at the
        // whole content root -- so bin/, obj/, and the source tree are never exposed over HTTP.
        // config/ is deliberately excluded: it holds sobekcm.config, which has a plaintext DB
        // connection string -- read server-side only, never served.
        private static readonly string[] LegacyContentFolders = { "data", "default", "design", "iipimage", "mySobek", "plugins", "temp" };

        public static void Configure(WebApplication app)
        {
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

            foreach (string folder in LegacyContentFolders)
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
        }
    }
}
