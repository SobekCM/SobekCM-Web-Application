#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.Navigation;
using SobekCM.Tools;
using System.IO;

#endregion

namespace SobekCM.Library.MySobekViewer.Submission.UploadSteps
{
    /// <summary> Default Upload step -- a generic multi-file drop zone, used by any Item Type that
    /// doesn't need a more specialized upload shape </summary>
    /// <remarks> STUB -- scaffolding only, no real upload handling yet. This is what
    /// <see cref="Upload_Step_Factory"/> falls back to for an unrecognized or blank upload code. </remarks>
    public class Generic_Upload_SubmissionStep : iUploadSubmissionStep
    {
        /// <summary> Code this implementation registers itself under </summary>
        public string Upload_Code => "GENERIC";

        /// <summary> Title shown for this step in the wizard header/stepper </summary>
        public string Step_Title => "Upload";

        /// <summary> Renders this upload step's HTML </summary>
        public void Render_HTML(TextWriter Output, Submission_State State, RequestCache RequestSpecificValues, Custom_Tracer Tracer)
        {
            Output.WriteLine("<h1>Upload</h1>");
            Output.WriteLine("<p><i>GENERIC UPLOAD HERE</i> -- a plain multi-file drop zone for the '" + State.ItemTypeName + "' type.</p>");
        }

        /// <summary> Handles a postback from this upload step </summary>
        public bool Handle_Postback(IFormCollection Form, Submission_State State, RequestCache RequestSpecificValues, Custom_Tracer Tracer)
        {
            return true;
        }
    }
}
