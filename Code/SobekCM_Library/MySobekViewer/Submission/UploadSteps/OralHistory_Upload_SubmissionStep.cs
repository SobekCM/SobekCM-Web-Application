#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.Navigation;
using SobekCM.Tools;
using System.IO;

#endregion

namespace SobekCM.Library.MySobekViewer.Submission.UploadSteps
{
    /// <summary> Oral History's Upload step -- fixed named slots (Transcript, Audio Recording, Video
    /// Recording, Supporting Materials) instead of a generic file picker, so the wizard always knows
    /// what each uploaded file is for </summary>
    /// <remarks> STUB -- scaffolding only, no real slot rendering/upload handling yet. Registered under
    /// upload code "ORALHISTORY" in <see cref="Upload_Step_Factory"/>. </remarks>
    public class OralHistory_Upload_SubmissionStep : iUploadSubmissionStep
    {
        /// <summary> Code this implementation registers itself under </summary>
        public string Upload_Code => "ORALHISTORY";

        /// <summary> Title shown for this step in the wizard header/stepper </summary>
        public string Step_Title => "Upload";

        /// <summary> Renders this upload step's HTML </summary>
        public void Render_HTML(TextWriter Output, Submission_State State, RequestCache RequestSpecificValues, HttpContext Context, Custom_Tracer Tracer)
        {
            Output.WriteLine("<h1>Upload files for this interview</h1>");
            Output.WriteLine("<p><i>ORAL HISTORY UPLOAD HERE</i> -- fixed slots for Transcript (required), Audio Recording, Video Recording, and Supporting Materials (repeatable).</p>");
        }

        /// <summary> Handles a postback from this upload step </summary>
        public bool Handle_Postback(IFormCollection Form, Submission_State State, RequestCache RequestSpecificValues, HttpContext Context, Custom_Tracer Tracer)
        {
            return true;
        }
    }
}
