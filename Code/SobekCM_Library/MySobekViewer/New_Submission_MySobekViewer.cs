#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.Navigation;
using SobekCM.Library.MySobekViewer.Submission;
using SobekCM.Library.MySobekViewer.Submission.Steps;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System;
using System.IO;

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
    /// through <see cref="Upload_Step_Factory"/> instead. THIS ENTIRE CLASS IS SCAFFOLDING: every step's
    /// actual content is a placeholder ("... HERE") -- what's real here is the postback routing, the
    /// step sequence with its two conditional skips, and <see cref="Submission_State"/> threading. </remarks>
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

                // TODO: check SobekCM_User_Permissions_Agreement_Acceptance for an existing acceptance
                // of this exact AgreementID before defaulting to "not yet accepted" -- nothing writes to
                // that table yet, so there is nothing to check against today
                if (state.PermissionsAgreementID == null)
                    state.CurrentStep = Submission_Step_Enum.TypeSelection;

                Context.SessionObject()[sessionKey] = state;
            }

            // Handle any post backs
            if ((RequestSpecificValues.Current_Mode.isPostBack) && (Context.Request.HasFormContentType))
            {
                var form = Context.Request.Form;
                string action = form["submission_action"];

                if (action == "back")
                {
                    move_to_previous_step();
                }
                else
                {
                    bool advance = handle_current_step_postback(form, RequestSpecificValues);
                    if (advance)
                        move_to_next_step();
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
                    return Upload_Step_Factory.Get_Upload_Step(state.UploadCode).Handle_Postback(Form, state, RequestSpecificValues, RequestSpecificValues.Tracer);

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
            while ((candidate < (int)Submission_Step_Enum.Confirm) && should_skip((Submission_Step_Enum)candidate));

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

            switch (state.CurrentStep)
            {
                case Submission_Step_Enum.Permissions:
                    new Permissions_SubmissionStep().Render_HTML(Output, state, RequestSpecificValues, Tracer);
                    break;

                case Submission_Step_Enum.TypeSelection:
                    new TypeSelection_SubmissionStep().Render_HTML(Output, state, RequestSpecificValues, Tracer);
                    break;

                case Submission_Step_Enum.Upload:
                    Upload_Step_Factory.Get_Upload_Step(state.UploadCode).Render_HTML(Output, state, RequestSpecificValues, Tracer);
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
                default: return step.ToString();
            }
        }

        /// <summary> Renders the Back/Continue buttons shared by every step </summary>
        /// <remarks> Self-contained inline JS -- <c>set_hidden_value_postback</c> is an AdminViewer-only
        /// helper (loaded from <c>sobekcm_admin.js</c>) not present on MySobekViewer pages, so this does
        /// not assume it exists. </remarks>
        private void write_footer_buttons(TextWriter Output)
        {
            Output.WriteLine("<div class=\"sbkNsub_Footer\">");
            if (state.CurrentStep != Submission_Step_Enum.Permissions)
                Output.WriteLine("  <button onclick=\"document.getElementById(&#39;submission_action&#39;).value=&#39;back&#39;; document.itemNavForm.submit(); return false;\">&larr; Back</button>");

            Output.WriteLine("  <button onclick=\"document.getElementById(&#39;submission_action&#39;).value=&#39;next&#39;; document.itemNavForm.submit(); return false;\">" +
                              (state.CurrentStep == Submission_Step_Enum.Confirm ? "Submit" : "Continue") + " &rarr;</button>");
            Output.WriteLine("</div>");
        }
    }
}
