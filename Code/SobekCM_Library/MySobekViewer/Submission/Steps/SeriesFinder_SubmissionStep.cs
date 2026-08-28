#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.Navigation;
using SobekCM.Tools;
using System.IO;

#endregion

namespace SobekCM.Library.MySobekViewer.Submission.Steps
{
    /// <summary> "Does this belong to an existing title?" -- shown only when the chosen Type's
    /// <c>ShowSeriesFinder</c> flag is set (Newspaper, MultiVolume). Comes after Upload, before Metadata. </summary>
    /// <remarks> STUB -- scaffolding only. Only one of these is ever needed -- not polymorphic, no
    /// per-Type variation beyond the flag that gates whether it runs at all. Called directly by
    /// <see cref="New_Submission_MySobekViewer"/>, no interface. </remarks>
    public class SeriesFinder_SubmissionStep
    {
        /// <summary> Title shown for this step in the wizard header/stepper </summary>
        public string Step_Title => "Find Series";

        /// <summary> Renders this step's HTML </summary>
        public void Render_HTML(TextWriter Output, Submission_State State, RequestCache RequestSpecificValues, Custom_Tracer Tracer)
        {
            Output.WriteLine("<h1>Does this issue belong to an existing title?</h1>");
            Output.WriteLine("<p><i>SERIES FINDER HERE</i> -- search existing BibIDs of this Type, attach to one, or start a new title.</p>");
        }

        /// <summary> Handles a postback from this step </summary>
        /// <returns> TRUE if this step is complete and the wizard should advance </returns>
        public bool Handle_Postback(IFormCollection Form, Submission_State State, RequestCache RequestSpecificValues, Custom_Tracer Tracer)
        {
            return true;
        }
    }
}
