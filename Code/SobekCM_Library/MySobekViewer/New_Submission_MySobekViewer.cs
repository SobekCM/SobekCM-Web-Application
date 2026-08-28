#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.FileSystems;
using SobekCM.Core.Navigation;
using SobekCM.Engine_Library.ApplicationState;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Engine_Library.Email;
using SobekCM.Engine_Library.Solr;
using SobekCM.Library.Database;
using SobekCM.Library.MySobekViewer.Submission;
using SobekCM.Library.MySobekViewer.Submission.Steps;
using SobekCM.Library.UI;
using SobekCM.Resource_Object;
using SobekCM.Resource_Object.Configuration;
using SobekCM.Resource_Object.Divisions;
using SobekCM.Resource_Object.Metadata_File_ReaderWriters;
using SobekCM.Resource_Object.Utilities;
using SobekCM.Tools;
using SobekCM_Resource_Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

#endregion

namespace SobekCM.Library.MySobekViewer
{
    /// <summary> Orchestrator for the new Type-driven submission wizard -- replaces the old monolithic
    /// <c>New_Group_And_Item_MySobekViewer</c> (excluded from compilation, kept only for reference while
    /// this rewrite is under construction; <see cref="SobekCM_Library.csproj"/>). </summary>
    /// <remarks> Holds no wizard-specific state itself beyond the current <see cref="Submission_State"/>
    /// (held in <see cref="SessionObjectStore"/>, not as private fields the way the old viewer worked).
    /// The fixed steps (Permissions, Type Selection, Series Finder, Metadata, Confirm) are plain concrete
    /// classes this orchestrator calls directly by name -- there is no shared step interface, since there
    /// is no real polymorphism among them. The one step with genuine polymorphism (Upload) is resolved
    /// through <see cref="Upload_Step_Factory"/> instead. Permissions, Type Selection, Series Finder,
    /// Congratulations, and the Generic/TEI Upload steps are real; Metadata, Confirm, and the Oral
    /// History Upload step are still placeholder content ("... HERE") pending the Block-XML assembler
    /// and a fixed-slot upload design -- Confirm is harmless to leave stubbed, since it is always
    /// skipped today (see <see cref="should_skip"/>). The actual item save lives here, in
    /// <see cref="perform_final_submission"/>, not on any step class -- it runs once, on the postback
    /// that first advances into Congratulations. </remarks>
    public class New_Submission_MySobekViewer : abstract_MySobekViewer
    {
        private readonly Submission_State state;
        private readonly string sessionKey;

        /// <summary> Constructor for a new instance of the New_Submission_MySobekViewer class </summary>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        public New_Submission_MySobekViewer(RequestCache RequestSpecificValues, HttpContext Context) : base(RequestSpecificValues, Context)
        {
            RequestSpecificValues.Tracer.Add_Trace("New_Submission_MySobekViewer.Constructor", String.Empty);

            // If the current user cannot submit items, go back
            if (!RequestSpecificValues.Current_User.Can_Submit)
            {
                RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Home;
                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode, Context);
                return;
            }

            // One in-progress submission per user per session -- matches the old viewer's one-item-
            // in-session-at-a-time assumption
            sessionKey = "Submission_State_" + RequestSpecificValues.Current_User.UserID;
            state = Context.SessionObject()[sessionKey] as Submission_State;
            if (state == null)
            {
                state = new Submission_State
                {
                    // Permissions agreement assignment is a user/group-level setting (see the
                    // Submissions tab), known immediately -- not something Type Selection determines
                    PermissionsAgreementID = RequestSpecificValues.Current_User.Permissions_Agreement_Id
                };

                // A returning user who already accepted this exact agreement in an earlier submission
                // is never asked again -- only re-prompted if the assigned AgreementID itself changes
                if ((state.PermissionsAgreementID == null) ||
                    SobekCM_Database.Has_Accepted_Permissions_Agreement(RequestSpecificValues.Current_User.UserID, state.PermissionsAgreementID.Value, RequestSpecificValues.Tracer))
                {
                    state.PermissionsAgreementAccepted = true;
                    state.CurrentStep = Submission_Step_Enum.TypeSelection;
                }

                Context.SessionObject()[sessionKey] = state;
            }

            // Handle any post backs
            if ((RequestSpecificValues.Current_Mode.isPostBack) && (Context.Request.HasFormContentType))
            {
                var form = Context.Request.Form;
                string action = form["submission_action"];

                state.ValidationMessage = null;

                if (action == "back")
                {
                    move_to_previous_step();
                }
                else
                {
                    bool advance = handle_current_step_postback(form, RequestSpecificValues);
                    if (advance)
                    {
                        move_to_next_step();

                        // Fires exactly once, the moment the wizard first lands on the terminal step --
                        // never on a later refresh/postback of Congratulations itself
                        if ((state.CurrentStep == Submission_Step_Enum.Congratulations) && (!state.SubmissionAttempted))
                            perform_final_submission(RequestSpecificValues);
                    }
                }

                Context.SessionObject()[sessionKey] = state;
            }
        }

        /// <summary> Dispatches a postback to whichever step is currently active </summary>
        /// <returns> TRUE if that step reports it is complete and the wizard should advance </returns>
        private bool handle_current_step_postback(IFormCollection Form, RequestCache RequestSpecificValues)
        {
            switch (state.CurrentStep)
            {
                case Submission_Step_Enum.Permissions:
                    return new Permissions_SubmissionStep().Handle_Postback(Form, state, RequestSpecificValues, RequestSpecificValues.Tracer);

                case Submission_Step_Enum.TypeSelection:
                    return new TypeSelection_SubmissionStep().Handle_Postback(Form, state, RequestSpecificValues, RequestSpecificValues.Tracer);

                case Submission_Step_Enum.Upload:
                    return Upload_Step_Factory.Get_Upload_Step(state.UploadCode).Handle_Postback(Form, state, RequestSpecificValues, Context, RequestSpecificValues.Tracer);

                case Submission_Step_Enum.SeriesFinder:
                    return new SeriesFinder_SubmissionStep().Handle_Postback(Form, state, RequestSpecificValues, RequestSpecificValues.Tracer);

                case Submission_Step_Enum.Metadata:
                    return new Metadata_SubmissionStep().Handle_Postback(Form, state, RequestSpecificValues, RequestSpecificValues.Tracer);

                case Submission_Step_Enum.Confirm:
                    return new Confirm_SubmissionStep().Handle_Postback(Form, state, RequestSpecificValues, RequestSpecificValues.Tracer);

                default:
                    return false;
            }
        }

        /// <summary> Advances to the next step in the fixed sequence, skipping Permissions/Series Finder
        /// when they don't apply to this submission </summary>
        private void move_to_next_step()
        {
            int candidate = (int)state.CurrentStep;
            do
            {
                candidate++;
            }
            while ((candidate < (int)Submission_Step_Enum.Congratulations) && should_skip((Submission_Step_Enum)candidate));

            state.CurrentStep = (Submission_Step_Enum)candidate;
        }

        /// <summary> Steps back to the previous non-skipped step </summary>
        private void move_to_previous_step()
        {
            int candidate = (int)state.CurrentStep;
            do
            {
                candidate--;
            }
            while ((candidate > (int)Submission_Step_Enum.Permissions) && should_skip((Submission_Step_Enum)candidate));

            state.CurrentStep = (Submission_Step_Enum)candidate;
        }

        /// <summary> Whether a given step should be skipped entirely for this submission </summary>
        private bool should_skip(Submission_Step_Enum step)
        {
            if (step == Submission_Step_Enum.Permissions)
                return (state.PermissionsAgreementID == null) || state.PermissionsAgreementAccepted;

            if (step == Submission_Step_Enum.SeriesFinder)
                return !state.ShowSeriesFinder;

            if (step == Submission_Step_Enum.Confirm)
            {
                // Confirm's only job is hosting Standalone widgets -- with none to show, it would just
                // be a redundant click before the same Submit action Congratulations already triggers.
                // No Type can declare a Standalone widget yet (Widget_Placement_Mode.Standalone has no
                // anchor/persistence built), so this always skips for now.
                return true;
            }

            return false;
        }

        /// <summary> Title for the page that displays this viewer, this is shown in the search box at the top of the page, just below the banner </summary>
        public override string Web_Title => "Add New Item";

        /// <summary> Add the HTML to be displayed in the main SobekCM viewer area </summary>
        /// <param name="Output"> Textwriter to write the HTML for this viewer</param>
        /// <param name="Tracer">Trace object keeps a list of each method executed and important milestones in rendering</param>
        public override void Write_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace("New_Submission_MySobekViewer.Write_HTML");

            Write_ItemNavForm_Opening(Output);

            Output.WriteLine("<!-- Hidden field is used for postbacks to indicate what to do next -->");
            Output.WriteLine("<input type=\"hidden\" id=\"submission_action\" name=\"submission_action\" value=\"\" />");
            Output.WriteLine();

            write_stepper(Output);

            if (!String.IsNullOrEmpty(state.ValidationMessage))
                Output.WriteLine("<div class=\"sbkNsub_ValidationMessage\">" + state.ValidationMessage + "</div>");

            switch (state.CurrentStep)
            {
                case Submission_Step_Enum.Permissions:
                    new Permissions_SubmissionStep().Render_HTML(Output, state, RequestSpecificValues, Tracer);
                    break;

                case Submission_Step_Enum.TypeSelection:
                    new TypeSelection_SubmissionStep().Render_HTML(Output, state, RequestSpecificValues, Tracer);
                    break;

                case Submission_Step_Enum.Upload:
                    Upload_Step_Factory.Get_Upload_Step(state.UploadCode).Render_HTML(Output, state, RequestSpecificValues, Context, Tracer);
                    break;

                case Submission_Step_Enum.SeriesFinder:
                    new SeriesFinder_SubmissionStep().Render_HTML(Output, state, RequestSpecificValues, Tracer);
                    break;

                case Submission_Step_Enum.Metadata:
                    new Metadata_SubmissionStep().Render_HTML(Output, state, RequestSpecificValues, Tracer);
                    break;

                case Submission_Step_Enum.Confirm:
                    new Confirm_SubmissionStep().Render_HTML(Output, state, RequestSpecificValues, Tracer);
                    break;

                case Submission_Step_Enum.Congratulations:
                    new Congratulations_SubmissionStep().Render_HTML(Output, state, RequestSpecificValues, Tracer);
                    break;
            }

            write_footer_buttons(Output);

            Write_ItemNavForm_Closing(Output);
        }

        /// <summary> Renders the simple step indicator across the top of every screen </summary>
        private void write_stepper(TextWriter Output)
        {
            Output.WriteLine("<div class=\"sbkNsub_Stepper\">");
            foreach (Submission_Step_Enum thisStep in Enum.GetValues(typeof(Submission_Step_Enum)))
            {
                if (should_skip(thisStep)) continue;

                string label = step_label(thisStep);
                Output.WriteLine((thisStep == state.CurrentStep)
                    ? "  <span class=\"sbkNsub_StepActive\">" + label + "</span>"
                    : "  <span class=\"sbkNsub_Step\">" + label + "</span>");
            }
            Output.WriteLine("</div>");
        }

        /// <summary> Gets the display label for a step, resolving the Upload step's label through the
        /// same factory used to render/handle it </summary>
        private string step_label(Submission_Step_Enum step)
        {
            switch (step)
            {
                case Submission_Step_Enum.Permissions: return new Permissions_SubmissionStep().Step_Title;
                case Submission_Step_Enum.TypeSelection: return new TypeSelection_SubmissionStep().Step_Title;
                case Submission_Step_Enum.Upload: return Upload_Step_Factory.Get_Upload_Step(state.UploadCode).Step_Title;
                case Submission_Step_Enum.SeriesFinder: return new SeriesFinder_SubmissionStep().Step_Title;
                case Submission_Step_Enum.Metadata: return new Metadata_SubmissionStep().Step_Title;
                case Submission_Step_Enum.Confirm: return new Confirm_SubmissionStep().Step_Title;
                case Submission_Step_Enum.Congratulations: return new Congratulations_SubmissionStep().Step_Title;
                default: return step.ToString();
            }
        }

        /// <summary> Renders the Back/Continue buttons shared by every step </summary>
        /// <remarks> Self-contained inline JS -- <c>set_hidden_value_postback</c> is an AdminViewer-only
        /// helper (loaded from <c>sobekcm_admin.js</c>) not present on MySobekViewer pages, so this does
        /// not assume it exists. </remarks>
        private void write_footer_buttons(TextWriter Output)
        {
            // Congratulations is terminal -- it renders its own "what's next" links in place of a
            // generic Back/Continue footer
            if (state.CurrentStep == Submission_Step_Enum.Congratulations)
                return;

            Output.WriteLine("<div class=\"sbkNsub_Footer\">");
            if (state.CurrentStep != Submission_Step_Enum.Permissions)
                Output.WriteLine("  <button onclick=\"document.getElementById(&#39;submission_action&#39;).value=&#39;back&#39;; document.itemNavForm.submit(); return false;\">&larr; Back</button>");

            Output.WriteLine("  <button onclick=\"document.getElementById(&#39;submission_action&#39;).value=&#39;next&#39;; document.itemNavForm.submit(); return false;\">" +
                              (state.CurrentStep == Submission_Step_Enum.Confirm ? "Submit" : "Continue") + " &rarr;</button>");
            Output.WriteLine("</div>");
        }

        #region Final submission -- runs once, on the way into Congratulations

        /// <summary> Performs the actual item save: assigns BibID/VID, attaches staged files, saves the
        /// item to the database, updates Solr, publishes the staged files to their destination, and
        /// archives them. Runs exactly once, right after the wizard advances into
        /// <see cref="Submission_Step_Enum.Congratulations"/>. </summary>
        /// <remarks> Ported from the old <c>New_Group_And_Item_MySobekViewer.complete_item_submission</c>
        /// -- same proven save pipeline (file grouping, JP2/JPEG attribute computation,
        /// <see cref="SobekCM_Item_Database.Save_New_Digital_Resource"/>, Solr, <see cref="Resource_File_Publisher"/>),
        /// adapted to read from <see cref="Submission_State"/> instead of instance fields. Two pieces of
        /// the old flow are not ported because nothing upstream produces them yet: per-file custom
        /// labels (the old upload screen let a user rename each file; <c>Generic_Upload_SubmissionStep</c>
        /// doesn't yet) and full <c>SobekCM_Item_Validator</c> validation (there is no Metadata screen
        /// yet whose errors it could send the user back to fix). <see cref="Submission_State.Item"/> is
        /// still NULL until the Block-XML assembler lands and <see cref="Steps.Metadata_SubmissionStep"/>
        /// becomes real, so this falls back to a bare item -- the rest of the pipeline (file handling, DB
        /// save, Solr, publish) can still be exercised end-to-end before that lands. </remarks>
        /// <summary> Human-friendly labels for the roles an upload step can assign a file (see
        /// <see cref="Submitted_File.Role"/>) -- add an entry here whenever a new upload step introduces
        /// a role that deserves better than its raw string as a label (e.g. once Oral History's
        /// fixed-slot upload step is real: "transcript", "audio", "video", "supporting"). A role with no
        /// entry here still displays, just as its raw string. </summary>
        private static readonly Dictionary<string, string> role_labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "tei", "TEI Document" }
        };

        /// <summary> Label for one staged file: the friendly label for its role if the upload step
        /// assigned one, else the filename without its extension (the old behavior, unchanged for every
        /// file a generic upload step never distinguishes) </summary>
        private static string label_for_file(FileInfo FileInfo, Dictionary<string, string> RoleByFileName)
        {
            if (RoleByFileName.TryGetValue(FileInfo.Name, out string role))
                return role_labels.TryGetValue(role, out string label) ? label : role;

            return FileInfo.Name.Replace(FileInfo.Extension, "");
        }

        private void perform_final_submission(RequestCache RequestSpecificValues)
        {
            state.SubmissionAttempted = true;

            if (state.Item == null)
                state.Item = new SobekCM_Item();

            SobekCM_Item itemToComplete = state.Item;

            // Same "ShibbID if present, else sanitized username" staging convention every upload step
            // in this wizard writes into (see Generic_Upload_SubmissionStep.get_staging_directory)
            string stagingDirectory = UI_ApplicationCache_Gateway.Settings.Servers.In_Process_Submission_Location + "\\" +
                                       RequestSpecificValues.Current_User.UserName.Replace(".", "").Replace("@", "") + "\\newitem";
            if (RequestSpecificValues.Current_User.ShibbID.Trim().Length > 0)
                stagingDirectory = UI_ApplicationCache_Gateway.Settings.Servers.In_Process_Submission_Location + "\\" +
                                    RequestSpecificValues.Current_User.ShibbID + "\\newitem";

            if (!Directory.Exists(stagingDirectory))
                Directory.CreateDirectory(stagingDirectory);

            try
            {
                itemToComplete.Source_Directory = stagingDirectory;

                // Group the staged files into page images vs. plain downloads, same as the old flow
                string[] allFiles = Directory.GetFiles(stagingDirectory);
                var imageFiles = new SortedList<string, List<string>>();
                var downloadFiles = new SortedList<string, List<string>>();
                foreach (string thisFile in allFiles)
                {
                    var thisFileInfo = new FileInfo(thisFile);
                    if (ResourceObjectSettings.Is_File_Excluded_From_Package(thisFileInfo.Name))
                        continue;

                    string extensionUpper = thisFileInfo.Extension.ToUpper();
                    string nameUpper = thisFileInfo.Name.ToUpper();
                    string filenameSansExtension = thisFileInfo.Name.Replace(thisFileInfo.Extension, "").ToLower();

                    if ((extensionUpper == ".JPG") || (extensionUpper == ".TIF") || (extensionUpper == ".JP2") || (extensionUpper == ".JPX"))
                    {
                        if (nameUpper.IndexOf(".QC.JPG") >= 0)
                            continue;

                        if (nameUpper.IndexOf("THM.JPG") > 0)
                            filenameSansExtension = filenameSansExtension.Substring(0, filenameSansExtension.Length - 3);

                        if (!imageFiles.TryGetValue(filenameSansExtension, out List<string> imageGroup))
                        {
                            imageGroup = new List<string>();
                            imageFiles[filenameSansExtension] = imageGroup;
                        }
                        imageGroup.Add(thisFileInfo.Name);
                    }
                    else
                    {
                        if (!Regex.Match(thisFileInfo.Name, UI_ApplicationCache_Gateway.Settings.Resources.Files_To_Exclude_From_Downloads, RegexOptions.IgnoreCase).Success)
                        {
                            if (!downloadFiles.TryGetValue(filenameSansExtension, out List<string> downloadGroup))
                            {
                                downloadGroup = new List<string>();
                                downloadFiles[filenameSansExtension] = downloadGroup;
                            }
                            downloadGroup.Add(thisFileInfo.Name);
                        }
                    }
                }

                // The active upload step's per-file roles (Oral History's eventual Transcript/Audio
                // Recording/etc., TEI's "tei"), keyed by filename -- gives a real label to files a
                // generic upload step never distinguishes, instead of always falling back to the raw
                // filename
                var roleByFileName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (Submitted_File submittedFile in state.Submitted_Files)
                {
                    if (!String.IsNullOrEmpty(submittedFile.Role))
                        roleByFileName[submittedFile.FileName] = submittedFile.Role;
                }

                // Add each file to the item's division trees, computing image attributes as we go
                itemToComplete.Divisions.Download_Tree.Clear();
                foreach (List<string> theseFiles in imageFiles.Values)
                {
                    foreach (string thisFile in theseFiles)
                    {
                        var fileInfo = new FileInfo(thisFile);
                        var newFile = new SobekCM_File_Info(fileInfo.Name);
                        string label = label_for_file(fileInfo, roleByFileName);
                        itemToComplete.Divisions.Physical_Tree.Add_File(newFile, label);

                        if (fileInfo.Extension.ToUpper().IndexOf("JP2") >= 0)
                            newFile.Compute_Jpeg2000_Attributes(stagingDirectory);
                        else
                            newFile.Compute_Jpeg_Attributes(stagingDirectory);
                    }
                }
                foreach (List<string> theseFiles in downloadFiles.Values)
                {
                    foreach (string thisFile in theseFiles)
                    {
                        var fileInfo = new FileInfo(thisFile);
                        var newFile = new SobekCM_File_Info(fileInfo.Name);
                        string label = label_for_file(fileInfo, roleByFileName);
                        itemToComplete.Divisions.Download_Tree.Add_File(newFile, label);
                    }
                }

                // Total package size
                string[] allFilesFinal = Directory.GetFiles(stagingDirectory);
                itemToComplete.DiskSize_KB = allFilesFinal.Aggregate<string, double>(0, (current, thisFile) => current + (new FileInfo(thisFile).Length / 1024));

                // Attach to an existing title if Series Finder chose one, else start a new one under the
                // Type's root; VID is always left blank so Save_New_Digital_Resource auto-assigns it
                itemToComplete.BibID = !String.IsNullOrEmpty(state.AttachToExistingBibID) ? state.AttachToExistingBibID : state.BibIDRoot;
                itemToComplete.VID = String.Empty;

                if (itemToComplete.Divisions.Files.Count > 0)
                    itemToComplete.Tracking.Born_Digital = true;
                itemToComplete.Tracking.VID_Source = "SobekCM:submission_wizard";

                const string userNotes = "Submitted online via the submission wizard";

                try
                {
                    SobekCM_Item_Database.Save_New_Digital_Resource(itemToComplete, false, true, RequestSpecificValues.Current_User.UserName, userNotes, RequestSpecificValues.Current_User.UserID);
                }
                catch (Exception ee)
                {
                    report_submission_error(itemToComplete, RequestSpecificValues, ee);
                    return;
                }

                if (itemToComplete.Behaviors.Views_Count > 0)
                {
                    foreach (var viewer in itemToComplete.Behaviors.Views)
                    {
                        try
                        {
                            SobekCM_Item_Database.Save_Item_Add_Viewer(itemToComplete.Web.ItemID, viewer.View_Type, viewer.Label, viewer.Attributes);
                        }
                        catch (Exception)
                        {
                            // Not critical -- the item itself already saved successfully
                        }
                    }
                }

                itemToComplete.Web.File_Root = itemToComplete.BibID.Substring(0, 2) + "\\" + itemToComplete.BibID.Substring(2, 2) + "\\" + itemToComplete.BibID.Substring(4, 2) + "\\" + itemToComplete.BibID.Substring(6, 2) + "\\" + itemToComplete.BibID.Substring(8, 2);
                itemToComplete.Web.AssocFilePath = itemToComplete.Web.File_Root + "\\" + itemToComplete.VID + "\\";

                try
                {
                    if (!String.IsNullOrEmpty(Engine_ApplicationCache_Gateway.Settings.Servers.Document_Solr_Index_URL))
                        Solr_Controller.Update_Index(Engine_ApplicationCache_Gateway.Settings.Servers.Document_Solr_Index_URL, Engine_ApplicationCache_Gateway.Settings.Servers.Page_Solr_Index_URL, itemToComplete, true);
                }
                catch (Exception)
                {
                    // Not critical -- the builder will pick this item up on its next pass regardless
                }

                itemToComplete.Save_SobekCM_METS();
                itemToComplete.Delete_Metadata_Cache();

                var marcOptions = new Dictionary<string, object>();
                if (UI_ApplicationCache_Gateway.Settings.MarcGeneration != null)
                {
                    marcOptions["MarcXML_File_ReaderWriter:MARC Cataloging Source Code"] = UI_ApplicationCache_Gateway.Settings.MarcGeneration.Cataloging_Source_Code;
                    marcOptions["MarcXML_File_ReaderWriter:MARC Location Code"] = UI_ApplicationCache_Gateway.Settings.MarcGeneration.Location_Code;
                    marcOptions["MarcXML_File_ReaderWriter:MARC Reproduction Agency"] = UI_ApplicationCache_Gateway.Settings.MarcGeneration.Reproduction_Agency;
                    marcOptions["MarcXML_File_ReaderWriter:MARC Reproduction Place"] = UI_ApplicationCache_Gateway.Settings.MarcGeneration.Reproduction_Place;
                    marcOptions["MarcXML_File_ReaderWriter:MARC XSLT File"] = UI_ApplicationCache_Gateway.Settings.MarcGeneration.XSLT_File;
                }
                marcOptions["MarcXML_File_ReaderWriter:System Name"] = UI_ApplicationCache_Gateway.Settings.System.System_Name;
                marcOptions["MarcXML_File_ReaderWriter:System Abbreviation"] = UI_ApplicationCache_Gateway.Settings.System.System_Code;

                var marcWriter = new MarcXML_File_ReaderWriter();
                marcWriter.Write_Metadata(Path.Combine(itemToComplete.Source_Directory, "marc.xml"), itemToComplete, marcOptions, out string marcError);

                string destinationDirectory = UI_ApplicationCache_Gateway.Settings.Servers.Image_Server_Network + itemToComplete.Web.AssocFilePath;

                // Reads directly from staging, so it must run before Publish_Staged_Files clears staging out
                archive_any_files(stagingDirectory, itemToComplete);

                Resource_File_Publisher.Publish_Staged_Files(stagingDirectory, itemToComplete.BibID, itemToComplete.VID,
                    destinationDirectory, UI_ApplicationCache_Gateway.Settings.Resources.Backup_Files_Folder_Name,
                    itemToComplete.BibID + "_" + itemToComplete.VID + ".html");

                RequestSpecificValues.Current_User.Items_Submitted_Count++;
                if (!RequestSpecificValues.Current_User.BibIDs.Contains(itemToComplete.BibID))
                    RequestSpecificValues.Current_User.Add_BibID(itemToComplete.BibID);

                SobekCM_Item_Database.Update_Additional_Work_Needed_Flag(itemToComplete.Web.ItemID, true);
            }
            catch (Exception ee)
            {
                report_submission_error(itemToComplete, RequestSpecificValues, ee);
            }
        }

        /// <summary> Records a failed submission: a generic message for the user, full detail emailed to
        /// the system error address -- matches the old flow's approach of never rendering a raw
        /// exception in the browser </summary>
        private void report_submission_error(SobekCM_Item ItemToComplete, RequestCache RequestSpecificValues, Exception Error)
        {
            state.SubmissionErrorMessage = "Error encountered during item save.";

            string errorBody = "<strong>ERROR ENCOUNTERED DURING ONLINE SUBMISSION</strong><br /><br />" +
                                "Title: " + ItemToComplete.Bib_Info.Main_Title.Title + "<br />" +
                                "User: " + RequestSpecificValues.Current_User.Full_Name + "<br /><br />" +
                                Error.ToString().Replace("\n", "<br />");
            string errorSubject = "Error during submission for '" + ItemToComplete.Bib_Info.Main_Title.Title + "'";
            string emailTo = UI_ApplicationCache_Gateway.Settings.Email.System_Error_Email;
            if (String.IsNullOrEmpty(emailTo))
                emailTo = UI_ApplicationCache_Gateway.Settings.Email.System_Email;
            Email_Helper.SendEmail(emailTo, errorSubject, errorBody, true, RequestSpecificValues.Current_Mode.Portal_Name);
        }

        /// <summary> Copies every newly-staged file into the archive drop box, if one is configured --
        /// ported as-is from the old flow's <c>Archive_Any_Files</c> </summary>
        private static bool archive_any_files(string StagingDirectory, SobekCM_Item Item)
        {
            if (String.IsNullOrEmpty(UI_ApplicationCache_Gateway.Settings.Archive.Archive_DropBox))
                return true;

            try
            {
                string[] newFiles = Directory.GetFiles(StagingDirectory);

                string archiveDirectory = UI_ApplicationCache_Gateway.Settings.Archive.Archive_DropBox + "\\" + Item.BibID + "_" + Item.VID + "_" +
                                           DateTime.Now.Year + DateTime.Now.Month.ToString().PadLeft(2, '0') + DateTime.Now.Day.ToString().PadLeft(2, '0');
                if (Directory.Exists(archiveDirectory))
                {
                    char append = 'A';
                    while ((Directory.Exists(archiveDirectory + append)) && (append != 'Z'))
                        append++;
                    archiveDirectory += append;
                }

                if (!Directory.Exists(archiveDirectory))
                    Directory.CreateDirectory(archiveDirectory);

                foreach (string thisFile in newFiles)
                {
                    string filename = Path.GetFileName(thisFile);
                    if (String.Compare(filename, "thumbs.db", StringComparison.OrdinalIgnoreCase) != 0)
                        File.Copy(thisFile, Path.Combine(archiveDirectory, filename), true);
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion
    }
}
