#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.Navigation;
using SobekCM.Tools;
using System.IO;

#endregion

namespace SobekCM.Library.MySobekViewer.Submission.Steps
{
    /// <summary> Review &amp; submit -- the last step of every submission </summary>
    /// <remarks> STUB -- scaffolding only. Minor per-Type customization of displayed content (the
    /// summary card, the "what happens after you submit" processing manifest) is data, not code -- not
    /// polymorphic, no interface, called directly by <see cref="New_Submission_MySobekViewer"/>. Any
    /// Standalone widgets anchored here (per the wireframe, e.g. a map footprint tool) get their own
    /// step immediately before this one, not rendered inside it -- Confirm's job is review-and-submit,
    /// not data entry. </remarks>
    public class Confirm_SubmissionStep
    {
        /// <summary> Title shown for this step in the wizard header/stepper </summary>
        public string Step_Title => "Confirm";

        /// <summary> Renders this step's HTML </summary>
        public void Render_HTML(TextWriter Output, Submission_State State, RequestCache RequestSpecificValues, Custom_Tracer Tracer)
        {
            Output.WriteLine("<h1>Review &amp; Submit</h1>");
            Output.WriteLine("<p><i>CONFIRM SCREEN HERE</i> -- summary card (Type, title, files, visibility) and a \"what happens after you submit\" processing manifest built from Upload/Metadata choices.</p>");
        }

        /// <summary> Handles a postback from this step </summary>
        /// <returns> TRUE if this step is complete (the submission itself is saved elsewhere by the orchestrator) </returns>
        public bool Handle_Postback(IFormCollection Form, Submission_State State, RequestCache RequestSpecificValues, Custom_Tracer Tracer)
        {
            return true;
        }
    }
}
