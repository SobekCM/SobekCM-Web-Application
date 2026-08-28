#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.Navigation;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Library.Helpers.UploadiFive;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System;
using System.IO;

#endregion

namespace SobekCM.Library.MySobekViewer.Submission.UploadSteps
{
    /// <summary> Default Upload step -- a generic multi-file drop zone, used by any Item Type that
    /// doesn't need a more specialized upload shape </summary>
    /// <remarks> Uses the existing <see cref="UploadiFive"/> widget/endpoint pair (also used by
    /// <c>File_Management_MySobekViewer</c>, <c>New_TEI_MySobekViewer</c>, and several admin screens) --
    /// files land directly in this submission's in-process staging directory via
    /// <c>UploadiFiveUploadEndpoint</c>, and this step just re-reads that directory on postback. Staging
    /// only, not publishing -- routing the staged files through <c>Resource_File_Publisher</c>/
    /// <c>SobekFileSystem</c> (so GCS-hybrid storage works correctly) is the eventual Confirm step's job,
    /// not this one; deliberately not copying <c>Page_Image_Upload_MySobekViewer</c>'s inconsistent
    /// direct-<c>File.Copy</c> publish approach. This is what <see cref="Upload_Step_Factory"/> falls
    /// back to for an unrecognized or blank upload code. </remarks>
    public class Generic_Upload_SubmissionStep : iUploadSubmissionStep
    {
        /// <summary> Code this implementation registers itself under </summary>
        public string Upload_Code => "GENERIC";

        /// <summary> Title shown for this step in the wizard header/stepper </summary>
        public string Step_Title => "Upload";

        /// <summary> Computes (and ensures the existence of) this user's staging directory for a new
        /// submission </summary>
        /// <remarks> Same "ShibbID if present, else sanitized username" convention and "newitem"
        /// subfolder name already used by <c>Group_Add_Volume_MySobekViewer</c> for adding a new volume
        /// under an existing title -- there is no shared helper for this in the codebase
        /// (<c>InstanceWide_Settings.User_InProcess_Directory</c> exists but is barely adopted), so this
        /// matches the inline convention every other viewer uses rather than introducing a third
        /// pattern. </remarks>
        private static string get_staging_directory(RequestCache RequestSpecificValues)
        {
            string directory = UI_ApplicationCache_Gateway.Settings.Servers.In_Process_Submission_Location + "\\" +
                                RequestSpecificValues.Current_User.UserName.Replace(".", "").Replace("@", "") + "\\newitem";
            if (RequestSpecificValues.Current_User.ShibbID.Trim().Length > 0)
                directory = UI_ApplicationCache_Gateway.Settings.Servers.In_Process_Submission_Location + "\\" +
                            RequestSpecificValues.Current_User.ShibbID + "\\newitem";

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            return directory;
        }

        /// <summary> Renders this upload step's HTML </summary>
        public void Render_HTML(TextWriter Output, Submission_State State, RequestCache RequestSpecificValues, HttpContext Context, Custom_Tracer Tracer)
        {
            string stagingDirectory = get_staging_directory(RequestSpecificValues);

            Output.WriteLine("<h1>Upload your files</h1>");
            Output.WriteLine("<p>Add one or more files for this " + System.Net.WebUtility.HtmlEncode(State.ItemTypeName) + ".</p>");

            string[] existingFiles = Directory.GetFiles(stagingDirectory);
            if (existingFiles.Length > 0)
            {
                Output.WriteLine("<div class=\"sbkNsub_UploadedFileList\">");
                foreach (string thisFile in existingFiles)
                {
                    Output.WriteLine("  <div>" + System.Net.WebUtility.HtmlEncode(Path.GetFileName(thisFile)) + "</div>");
                }
                Output.WriteLine("</div>");
            }

            var uploadControl = new UploadiFive
            {
                UploadPath = stagingDirectory,
                UploadScript = RequestSpecificValues.Current_Mode.Base_URL + "UploadiFiveFileHandler.ashx",
                AllowedFileExtensions = UI_ApplicationCache_Gateway.Settings.Resources.Upload_File_Types,
                RemoveCompleted = false,
                SubmitWhenQueueCompletes = false
            };
            uploadControl.Add_To_Stream(Output, Context);
        }

        /// <summary> Handles a postback from this upload step </summary>
        /// <returns> TRUE if at least one file has been staged, FALSE (with a validation message) if the
        /// staging directory is still empty </returns>
        public bool Handle_Postback(IFormCollection Form, Submission_State State, RequestCache RequestSpecificValues, HttpContext Context, Custom_Tracer Tracer)
        {
            string stagingDirectory = get_staging_directory(RequestSpecificValues);
            string[] existingFiles = Directory.GetFiles(stagingDirectory);

            if (existingFiles.Length == 0)
            {
                State.ValidationMessage = "Please upload at least one file before continuing.";
                return false;
            }

            State.UploadedFileNames.Clear();
            foreach (string thisFile in existingFiles)
                State.UploadedFileNames.Add(Path.GetFileName(thisFile));

            return true;
        }
    }
}
