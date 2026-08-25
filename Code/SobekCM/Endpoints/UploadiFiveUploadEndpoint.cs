using Microsoft.AspNetCore.Http;
using SobekCM.Library.Helpers.UploadiFive;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SobekCM.Endpoints
{
    /// <summary> Replaces UploadiFiveFileHandler.ashx -- receives an UploadiFive file upload,
    /// sanitizes and validates it against the token's allowed extensions, saves it, and appends it
    /// to the session-tracked return-token list. </summary>
    public static class UploadiFiveUploadEndpoint
    {
        public static async Task Invoke(HttpContext context)
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
                string filename = PathTraversalGuard.SanitizeFileName(postedFile.FileName);
                string filenameSansExt = Path.GetFileNameWithoutExtension(filename);

                if (filenameSansExt.Contains('.'))
                    filename = filenameSansExt.Replace(".", "_") + extension;
                if (filename.Contains('&'))
                    filename = filename.Replace("&", "");

                if (!string.IsNullOrEmpty(tokenObj.ServerSideFileName))
                {
                    string serverSideFileName = PathTraversalGuard.SanitizeFileName(tokenObj.ServerSideFileName);
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

                filename = PathTraversalGuard.SanitizeFileName(filename);
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
