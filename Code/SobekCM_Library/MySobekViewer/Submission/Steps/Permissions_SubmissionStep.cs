#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.Navigation;
using SobekCM.Tools;
using System.IO;

#endregion

namespace SobekCM.Library.MySobekViewer.Submission.Steps
{
    /// <summary> "Before you submit" permissions agreement gate -- shown once per assigned agreement,
    /// not once per submission (a user who has already accepted their assigned agreement skips straight
    /// past this step) </summary>
    /// <remarks> STUB -- scaffolding only. Same code always; the only thing that varies is which
    /// <c>SobekCM_Permissions_Agreement</c> row is assigned to the current user/group. Not polymorphic
    /// -- called directly by <see cref="New_Submission_MySobekViewer"/>, no interface. </remarks>
    public class Permissions_SubmissionStep
    {
        /// <summary> Title shown for this step in the wizard header/stepper </summary>
        public string Step_Title => "Permissions";

        /// <summary> Renders this step's HTML </summary>
        public void Render_HTML(TextWriter Output, Submission_State State, RequestCache RequestSpecificValues, Custom_Tracer Tracer)
        {
            Output.WriteLine("<h1>Before You Submit</h1>");
            Output.WriteLine("<p><i>PERMISSIONS AGREEMENT HERE</i> -- the agreement text assigned to this user/group, with an \"I have read and agree\" checkbox.</p>");
        }

        /// <summary> Handles a postback from this step </summary>
        /// <returns> TRUE if this step is complete and the wizard should advance </returns>
        public bool Handle_Postback(IFormCollection Form, Submission_State State, RequestCache RequestSpecificValues, Custom_Tracer Tracer)
        {
            State.PermissionsAgreementAccepted = true;
            return true;
        }
    }
}
