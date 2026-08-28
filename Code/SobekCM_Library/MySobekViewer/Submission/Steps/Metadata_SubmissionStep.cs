#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.Navigation;
using SobekCM.Tools;
using System.IO;

#endregion

namespace SobekCM.Library.MySobekViewer.Submission.Steps
{
    /// <summary> The one metadata entry page -- assembled from the chosen Type's ordered metadata
    /// blocks, rendered as a trimmed-down <c>CompleteTemplate</c> </summary>
    /// <remarks> STUB -- scaffolding only; the Block-XML-to-<c>Template_Panel</c> assembler this will
    /// drive doesn't exist yet. "Just deeply customizable" through data (which blocks the Type bundles),
    /// not through code variation -- not polymorphic, no interface, called directly by
    /// <see cref="New_Submission_MySobekViewer"/>. </remarks>
    public class Metadata_SubmissionStep
    {
        /// <summary> Title shown for this step in the wizard header/stepper </summary>
        public string Step_Title => "Metadata";

        /// <summary> Renders this step's HTML </summary>
        public void Render_HTML(TextWriter Output, Submission_State State, RequestCache RequestSpecificValues, Custom_Tracer Tracer)
        {
            Output.WriteLine("<h1>Describe this " + State.ItemTypeName + "</h1>");
            Output.WriteLine("<p><i>METADATA FORM HERE</i> -- one panel per metadata block assigned to '" + State.ItemTypeName + "', in order, plus the shared fields common to every Type, plus any Inline widgets hosted here.</p>");
        }

        /// <summary> Handles a postback from this step </summary>
        /// <returns> TRUE if this step is complete and the wizard should advance </returns>
        public bool Handle_Postback(IFormCollection Form, Submission_State State, RequestCache RequestSpecificValues, Custom_Tracer Tracer)
        {
            return true;
        }
    }
}
