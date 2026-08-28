#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.Navigation;
using SobekCM.Tools;
using System.IO;

#endregion

namespace SobekCM.Library.MySobekViewer.Submission.Steps
{
    /// <summary> The Type grid -- "what are you adding to the library?" -- always the same, first real
    /// step of every submission </summary>
    /// <remarks> STUB -- scaffolding only. Not polymorphic -- called directly by
    /// <see cref="New_Submission_MySobekViewer"/>, no interface. </remarks>
    public class TypeSelection_SubmissionStep
    {
        /// <summary> Title shown for this step in the wizard header/stepper </summary>
        public string Step_Title => "Choose Type";

        /// <summary> Renders this step's HTML </summary>
        public void Render_HTML(TextWriter Output, Submission_State State, RequestCache RequestSpecificValues, Custom_Tracer Tracer)
        {
            Output.WriteLine("<h1>What are you adding to the library?</h1>");
            Output.WriteLine("<p><i>TYPE GRID HERE</i> -- one card per enabled Item Type this user/group can select (unrestricted unless <c>SobekCM_Item_Type_Assignment</c> says otherwise).</p>");
        }

        /// <summary> Handles a postback from this step </summary>
        /// <returns> TRUE if this step is complete and the wizard should advance </returns>
        public bool Handle_Postback(IFormCollection Form, Submission_State State, RequestCache RequestSpecificValues, Custom_Tracer Tracer)
        {
            // TODO: read the chosen TypeID from the form, load the Item Type row, and populate
            // State.ItemTypeID / State.ItemTypeName / State.ShowSeriesFinder / State.UploadCode /
            // State.PermissionsAgreementID from it
            return true;
        }
    }
}
