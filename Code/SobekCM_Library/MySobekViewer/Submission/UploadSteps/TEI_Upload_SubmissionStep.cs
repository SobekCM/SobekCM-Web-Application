#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.Navigation;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Library.Helpers.UploadiFive;
using SobekCM.Library.UI;
using SobekCM.Resource_Object.Utilities;
using SobekCM.Tools;
using System;
using System.Collections.Generic;
using System.IO;

#endregion

namespace SobekCM.Library.MySobekViewer.Submission.UploadSteps
{
    /// <summary> TEI's Upload step -- the TEI XML file itself (schema-validated on postback) plus any
    /// supporting files, alongside three selections (metadata mapping, display XSLT, optional CSS) that
    /// the old <c>New_TEI_MySobekViewer</c> split across two separate steps (upload, then select) </summary>
    /// <remarks> The XSLT/CSS/mapping files themselves are never uploaded by the submitter -- they are
    /// admin-curated files already sitting under <c>plugins/tei/{mapping,xslt,css}</c> on the server;
    /// per-user approval is encoded as <c>mySobek_User_Settings</c> rows shaped
    /// <c>TEI.MAPPING.&lt;name&gt;</c>/<c>TEI.XSLT.&lt;name&gt;</c>/<c>TEI.CSS.&lt;name&gt;</c> (value
    /// "true"/"false"), read via <c>User_Object.SettingsKeys</c>/<c>Get_Setting</c>. A posted selection
    /// is always re-checked against that same approved list on <see cref="Handle_Postback"/>, never
    /// trusted outright -- same pattern <see cref="Steps.TypeSelection_SubmissionStep"/> uses for the
    /// chosen Type. There is no explicit "which uploaded file is the TEI document" control -- like the
    /// old viewer, whichever staged file has an ".xml" extension is it (most-recently-modified wins if
    /// there happens to be more than one). Registered under upload code "TEI" in
    /// <see cref="Upload_Step_Factory"/>. Deliberately NOT ported here: writing the
    /// <c>TEI.Source_File</c>/<c>TEI.CSS</c>/<c>TEI.XSLT</c> item settings and registering the dedicated
    /// "TEI" viewer -- those are final-save concerns (<c>New_Submission_MySobekViewer.perform_final_submission</c>),
    /// which isn't Type-aware yet. This step only stages files and records the three selections onto
    /// <see cref="Submission_State"/>. </remarks>
    public class TEI_Upload_SubmissionStep : iUploadSubmissionStep
    {
        /// <summary> Code this implementation registers itself under </summary>
        public string Upload_Code => "TEI";

        /// <summary> Title shown for this step in the wizard header/stepper </summary>
        public string Step_Title => "Upload";

        /// <summary> Same "ShibbID if present, else sanitized username" staging convention every upload
        /// step in this wizard uses </summary>
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

        /// <summary> Finds the most-recently-modified staged file with an ".xml" extension -- same
        /// tie-break the old viewer used to decide which uploaded file is the TEI document </summary>
        private static string find_tei_file(string StagingDirectory)
        {
            string[] xmlFiles = Directory.GetFiles(StagingDirectory, "*.xml");
            if (xmlFiles.Length == 0)
                return null;

            string latest = xmlFiles[0];
            foreach (string thisFile in xmlFiles)
            {
                if (File.GetLastWriteTime(thisFile) > File.GetLastWriteTime(latest))
                    latest = thisFile;
            }
            return Path.GetFileName(latest);
        }

        /// <summary> Every value this user is approved for under a given setting-key prefix (e.g.
        /// "TEI.XSLT."), filtered to ones whose backing file still actually exists on disk </summary>
        private static List<string> get_approved_files(RequestCache RequestSpecificValues, string SettingPrefix, Func<string, bool> FileExistsCheck)
        {
            var approved = new List<string>();
            foreach (string thisSettingKey in RequestSpecificValues.Current_User.SettingsKeys)
            {
                if (thisSettingKey.IndexOf(SettingPrefix, StringComparison.OrdinalIgnoreCase) != 0)
                    continue;

                if (!String.Equals(RequestSpecificValues.Current_User.Get_Setting(thisSettingKey, "false"), "true", StringComparison.OrdinalIgnoreCase))
                    continue;

                string name = thisSettingKey.Substring(SettingPrefix.Length);
                if (FileExistsCheck(name))
                    approved.Add(name);
            }
            return approved;
        }

        private static bool mapping_file_exists(string Name)
        {
            return File.Exists(Path.Combine(UI_ApplicationCache_Gateway.Settings.Servers.Application_Server_Network, "plugins", "tei", "mapping", Name + ".xml"));
        }

        private static bool xslt_file_exists(string Name)
        {
            string path = Path.Combine(UI_ApplicationCache_Gateway.Settings.Servers.Application_Server_Network, "plugins", "tei", "xslt", Name);
            return File.Exists(path + ".xslt") || File.Exists(path + ".xsl");
        }

        private static bool css_file_exists(string Name)
        {
            return File.Exists(Path.Combine(UI_ApplicationCache_Gateway.Settings.Servers.Application_Server_Network, "plugins", "tei", "css", Name + ".css"));
        }

        /// <summary> Renders this upload step's HTML </summary>
        public void Render_HTML(TextWriter Output, Submission_State State, RequestCache RequestSpecificValues, HttpContext Context, Custom_Tracer Tracer)
        {
            string stagingDirectory = get_staging_directory(RequestSpecificValues);

            Output.WriteLine("<h1>Upload your TEI file</h1>");
            Output.WriteLine("<p>Upload the TEI XML file for this " + System.Net.WebUtility.HtmlEncode(State.ItemTypeName) + ", plus any supporting files (images, PDFs).</p>");

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
                AllowedFileExtensions = "xml," + UI_ApplicationCache_Gateway.Settings.Resources.Upload_Image_Types + "," + UI_ApplicationCache_Gateway.Settings.Resources.Upload_File_Types,
                RemoveCompleted = false,
                SubmitWhenQueueCompletes = false
            };
            uploadControl.Add_To_Stream(Output, Context);

            Output.WriteLine("<h2>Display &amp; Metadata Options</h2>");

            render_select(Output, "Metadata Mapping", "submission_tei_mapping",
                get_approved_files(RequestSpecificValues, "TEI.MAPPING.", mapping_file_exists), State.TeiMappingCode,
                "This mapping file reads the header information from your TEI file into the system, to facilitate searching and discovery.",
                "You are not approved for any TEI mapping file. Please contact your system administrator.", false);

            render_select(Output, "XSLT File", "submission_tei_xslt",
                get_approved_files(RequestSpecificValues, "TEI.XSLT.", xslt_file_exists), State.TeiXsltCode,
                "The XSLT transforms your TEI into HTML for display.",
                "You are not approved for any TEI XSLT file. Please contact your system administrator.", false);

            render_select(Output, "CSS File (optional)", "submission_tei_css",
                get_approved_files(RequestSpecificValues, "TEI.CSS.", css_file_exists), State.TeiCssCode,
                "The CSS file can add additional style to the resulting display.",
                null, true);
        }

        /// <summary> Renders one of the three TEI selection dropdowns -- a single approved, required
        /// option collapses to a hidden field (matching the old viewer), multiple render as a real
        /// select, and none renders an explanatory message instead (fatal only when not
        /// <paramref name="AllowBlankOption"/> -- CSS is simply absent rather than blocking) </summary>
        private static void render_select(TextWriter Output, string Label, string FieldName, List<string> Options, string SelectedValue, string Description, string NoneMessage, bool AllowBlankOption)
        {
            if (Options.Count == 0)
            {
                if (NoneMessage != null)
                    Output.WriteLine("<p><strong>" + System.Net.WebUtility.HtmlEncode(Label) + ":</strong> <span style=\"color:red\">" + System.Net.WebUtility.HtmlEncode(NoneMessage) + "</span></p>");
                return;
            }

            Output.WriteLine("<p>" + System.Net.WebUtility.HtmlEncode(Description) + "</p>");
            Output.WriteLine("<label>" + System.Net.WebUtility.HtmlEncode(Label) + ":</label> ");

            if ((Options.Count == 1) && (!AllowBlankOption))
            {
                Output.WriteLine(System.Net.WebUtility.HtmlEncode(Options[0]));
                Output.WriteLine("<input type=\"hidden\" id=\"" + FieldName + "\" name=\"" + FieldName + "\" value=\"" + System.Net.WebUtility.HtmlEncode(Options[0]) + "\" />");
                return;
            }

            Output.WriteLine("<select name=\"" + FieldName + "\" id=\"" + FieldName + "\">");
            if (AllowBlankOption)
                Output.WriteLine("  <option value=\"\">(none)</option>");
            foreach (string option in Options)
            {
                bool selected = String.Equals(option, SelectedValue, StringComparison.OrdinalIgnoreCase);
                Output.WriteLine("  <option value=\"" + System.Net.WebUtility.HtmlEncode(option) + "\"" + (selected ? " selected=\"selected\"" : "") + ">" + System.Net.WebUtility.HtmlEncode(option) + "</option>");
            }
            Output.WriteLine("</select>");
        }

        /// <summary> Handles a postback from this upload step </summary>
        /// <returns> TRUE if a TEI file, mapping, and XSLT are all present and valid </returns>
        public bool Handle_Postback(IFormCollection Form, Submission_State State, RequestCache RequestSpecificValues, HttpContext Context, Custom_Tracer Tracer)
        {
            string stagingDirectory = get_staging_directory(RequestSpecificValues);

            string teiFileName = find_tei_file(stagingDirectory);
            if (teiFileName == null)
            {
                State.ValidationMessage = "Please upload a TEI XML file before continuing.";
                return false;
            }

            var validator = new XmlValidator();
            if (!validator.IsValid(Path.Combine(stagingDirectory, teiFileName)))
            {
                State.ValidationMessage = "The uploaded TEI file is not valid XML: " + validator.Errors;
                return false;
            }

            List<string> approvedMappings = get_approved_files(RequestSpecificValues, "TEI.MAPPING.", mapping_file_exists);
            string postedMapping = Form["submission_tei_mapping"];
            if ((approvedMappings.Count == 0) || (String.IsNullOrEmpty(postedMapping)) || (!approvedMappings.Exists(m => String.Equals(m, postedMapping, StringComparison.OrdinalIgnoreCase))))
            {
                State.ValidationMessage = "Please select a metadata mapping before continuing.";
                return false;
            }

            List<string> approvedXslt = get_approved_files(RequestSpecificValues, "TEI.XSLT.", xslt_file_exists);
            string postedXslt = Form["submission_tei_xslt"];
            if ((approvedXslt.Count == 0) || (String.IsNullOrEmpty(postedXslt)) || (!approvedXslt.Exists(x => String.Equals(x, postedXslt, StringComparison.OrdinalIgnoreCase))))
            {
                State.ValidationMessage = "Please select an XSLT file before continuing.";
                return false;
            }

            string postedCss = Form["submission_tei_css"];
            string validatedCss = String.Empty;
            if (!String.IsNullOrEmpty(postedCss))
            {
                List<string> approvedCss = get_approved_files(RequestSpecificValues, "TEI.CSS.", css_file_exists);
                if (approvedCss.Exists(c => String.Equals(c, postedCss, StringComparison.OrdinalIgnoreCase)))
                    validatedCss = postedCss;
            }

            State.TeiMappingCode = postedMapping;
            State.TeiXsltCode = postedXslt;
            State.TeiCssCode = validatedCss;

            State.Submitted_Files.Clear();
            foreach (string thisFile in Directory.GetFiles(stagingDirectory))
            {
                string fileName = Path.GetFileName(thisFile);
                string role = String.Equals(fileName, teiFileName, StringComparison.OrdinalIgnoreCase) ? "tei" : String.Empty;
                State.Submitted_Files.Add(new Submitted_File(fileName, role));
            }

            return true;
        }
    }
}
