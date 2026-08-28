#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.Navigation;
using SobekCM.Library.Database;
using SobekCM.Tools;
using System;
using System.Data;
using System.IO;

#endregion

namespace SobekCM.Library.MySobekViewer.Submission.Steps
{
    /// <summary> "Before you submit" permissions agreement gate -- shown once per assigned agreement,
    /// not once per submission (a user who has already accepted their assigned agreement skips straight
    /// past this step, checked once by <see cref="New_Submission_MySobekViewer"/> when the submission
    /// starts) </summary>
    /// <remarks> Same code always; the only thing that varies is which
    /// <c>SobekCM_Permissions_Agreement</c> row is assigned to the current user/group. Not polymorphic
    /// -- called directly by <see cref="New_Submission_MySobekViewer"/>, no interface. </remarks>
    public class Permissions_SubmissionStep
    {
        /// <summary> Title shown for this step in the wizard header/stepper </summary>
        public string Step_Title => "Permissions";

        /// <summary> Renders this step's HTML </summary>
        public void Render_HTML(TextWriter Output, Submission_State State, RequestCache RequestSpecificValues, Custom_Tracer Tracer)
        {
            if (State.PermissionsAgreementID == null)
            {
                // Should never actually render -- the orchestrator skips this step entirely when no
                // agreement is assigned. Defensive fallback only.
                Output.WriteLine("<p><i>No agreement is required for your account.</i></p>");
                return;
            }

            DataRow agreementRow = SobekCM_Database.Get_Permissions_Agreement(State.PermissionsAgreementID.Value, Tracer);
            if (agreementRow == null)
            {
                Output.WriteLine("<p><i>The agreement assigned to your account could not be found. Please contact your administrator.</i></p>");
                return;
            }

            string name = agreementRow["Name"].ToString();
            string text = agreementRow["AgreementText"].ToString();

            Output.WriteLine("<h1>Before You Submit</h1>");
            Output.WriteLine("<h2>" + System.Net.WebUtility.HtmlEncode(name) + "</h2>");
            Output.WriteLine("<div class=\"sbkNsub_AgreementText\">" + text + "</div>");
            Output.WriteLine("<label>");
            Output.WriteLine("  <input type=\"checkbox\" name=\"submission_permissions_agree\" id=\"submission_permissions_agree\" />");
            Output.WriteLine("  I have read and agree to the terms above.");
            Output.WriteLine("</label>");
            Output.WriteLine("<p><i>We keep a permanent copy of the exact text you agree to, dated today -- if this agreement's wording changes later, your original stays on file.</i></p>");
        }

        /// <summary> Handles a postback from this step </summary>
        /// <returns> TRUE if this step is complete and the wizard should advance </returns>
        public bool Handle_Postback(IFormCollection Form, Submission_State State, RequestCache RequestSpecificValues, Custom_Tracer Tracer)
        {
            if (State.PermissionsAgreementID == null)
                return true;

            bool agreed = !String.IsNullOrEmpty(Form["submission_permissions_agree"].ToString());
            if (!agreed)
            {
                State.ValidationMessage = "You must check the box to confirm you have read and agree to the terms before continuing.";
                return false;
            }

            SobekCM_Database.Record_Permissions_Agreement_Acceptance(RequestSpecificValues.Current_User.UserID, State.PermissionsAgreementID.Value, Tracer);
            State.PermissionsAgreementAccepted = true;
            return true;
        }
    }
}
