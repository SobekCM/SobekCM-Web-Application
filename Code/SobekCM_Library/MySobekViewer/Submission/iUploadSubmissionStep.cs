#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.Navigation;
using SobekCM.Tools;
using System.IO;

#endregion

namespace SobekCM.Library.MySobekViewer.Submission
{
    /// <summary> One shape of the Upload step -- the one place in the fixed step sequence where the
    /// screen genuinely varies by Item Type (a generic multi-file drop zone vs. Oral History's fixed
    /// named slots for Transcript/Audio/Video/Supporting Materials, etc.) </summary>
    /// <remarks> Resolved from <c>SobekCM_Item_Type</c>'s upload code through
    /// <see cref="Upload_Step_Factory"/>, the same code-to-class lookup shape already used by
    /// <c>AdminViewer_Factory</c>'s plugin registry and <c>Element_Factory.getElement</c>. </remarks>
    public interface iUploadSubmissionStep
    {
        /// <summary> Code this implementation registers itself under, matched against the Item Type's
        /// upload code by <see cref="Upload_Step_Factory"/> </summary>
        string Upload_Code { get; }

        /// <summary> Title shown for this step in the wizard header/stepper </summary>
        string Step_Title { get; }

        /// <summary> Renders this upload step's HTML </summary>
        /// <param name="Output"> Textwriter to write the HTML for this step </param>
        /// <param name="State"> The in-progress submission this step is operating on </param>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        /// <param name="Context"> Current HTTP context -- needed here (unlike the fixed steps) for
        /// <c>UploadiFive</c>'s session-based security token </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        void Render_HTML(TextWriter Output, Submission_State State, RequestCache RequestSpecificValues, HttpContext Context, Custom_Tracer Tracer);

        /// <summary> Handles a postback from this upload step </summary>
        /// <param name="Form"> Posted form values </param>
        /// <param name="State"> The in-progress submission this step is operating on -- mutated in place
        /// with whatever files/behaviors this step is responsible for </param>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        /// <param name="Context"> Current HTTP context </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> TRUE if this step is complete and the wizard should advance, FALSE to redisplay this
        /// same step (e.g. a required slot is still empty) </returns>
        bool Handle_Postback(IFormCollection Form, Submission_State State, RequestCache RequestSpecificValues, HttpContext Context, Custom_Tracer Tracer);
    }
}
