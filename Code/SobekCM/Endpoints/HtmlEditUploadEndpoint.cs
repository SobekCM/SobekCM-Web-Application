using Microsoft.AspNetCore.Http;
using SobekCM.Library.Helpers.CKEditor;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace SobekCM.Endpoints
{
    /// <summary> Replaces HtmlEditFileHandler.ashx -- receives a CKEditor image/file upload, saves
    /// it under the token's upload path, and returns its URL as JSON. </summary>
    public static class HtmlEditUploadEndpoint
    {
        public static async Task Invoke(HttpContext context)
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

            string file = PathTraversalGuard.SanitizeFileName(upload.FileName);
            string savePath = Path.Combine(tokenObj.UploadPath, file);
            await using var stream = File.Create(savePath);
            await upload.CopyToAsync(stream);

            string url = tokenObj.UploadURL + file;

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { url }));
        }
    }
}
