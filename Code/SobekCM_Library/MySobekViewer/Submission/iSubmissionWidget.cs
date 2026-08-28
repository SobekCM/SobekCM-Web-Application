#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.Navigation;
using SobekCM.Tools;
using System.IO;

#endregion

namespace SobekCM.Library.MySobekViewer.Submission
{
    /// <summary> How a widget shows up in the wizard -- embedded inside an existing step, or as its own
    /// titled step with its own stepper entry </summary>
    /// <remarks> Chosen per-widget, not fixed for all widgets: a single yes/no toggle (e.g. "this photo
    /// has a specific location") doesn't deserve its own stepper entry, but a full interactive tool
    /// (drawing a map footprint, or a hypothetical future "tag people in this photo" widget) does, and
    /// doesn't belong crammed into <see cref="Submission_Step_Enum.Confirm"/> whose job is review-and-
    /// submit, not data entry. </remarks>
    public enum Widget_Placement_Mode
    {
        /// <summary> Rendered inside an existing step (see <see cref="iSubmissionWidget.Host_Step"/>) --
        /// no stepper entry of its own </summary>
        Inline,

        /// <summary> Its own titled step (see <see cref="iSubmissionWidget.Widget_Title"/>), inserted
        /// into the sequence at its own anchor point </summary>
        Standalone
    }

    /// <summary> Bespoke, non-block UI a Type can add to the submission wizard beyond ordinary metadata
    /// fields -- e.g. a map footprint drawing tool, a single geographic point picker </summary>
    /// <remarks> Resolved from <c>SobekCM_Item_Type_Widget.WidgetCode</c> through the same code-to-class
    /// lookup shape as <see cref="iUploadSubmissionStep"/>/<see cref="Upload_Step_Factory"/>. NOTE:
    /// <c>SobekCM_Item_Type_Widget.ScreenPlacement</c> as it exists today only expresses the
    /// <see cref="Widget_Placement_Mode.Inline"/> case (which of the fixed steps to embed within) --
    /// expressing a Standalone widget's anchor position needs a schema addition not yet made. </remarks>
    public interface iSubmissionWidget
    {
        /// <summary> Code this implementation registers itself under, matched against
        /// <c>SobekCM_Item_Type_Widget.WidgetCode</c> </summary>
        string Widget_Code { get; }

        /// <summary> Title used as this widget's stepper label when <see cref="Placement_Mode"/> is
        /// <see cref="Widget_Placement_Mode.Standalone"/>; unused when Inline </summary>
        string Widget_Title { get; }

        /// <summary> Whether this widget embeds inside an existing step or stands alone as its own step </summary>
        Widget_Placement_Mode Placement_Mode { get; }

        /// <summary> Which fixed step to render inside, when <see cref="Placement_Mode"/> is
        /// <see cref="Widget_Placement_Mode.Inline"/>; meaningless when Standalone </summary>
        Submission_Step_Enum Host_Step { get; }

        /// <summary> Renders this widget's HTML </summary>
        /// <param name="Output"> Textwriter to write the HTML for this widget </param>
        /// <param name="State"> The in-progress submission this widget is operating on </param>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        void Render_HTML(TextWriter Output, Submission_State State, RequestCache RequestSpecificValues, Custom_Tracer Tracer);

        /// <summary> Handles a postback from this widget </summary>
        /// <param name="Form"> Posted form values </param>
        /// <param name="State"> The in-progress submission this widget is operating on -- mutated in
        /// place with whatever this widget is responsible for </param>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> TRUE if this widget is complete and the wizard should advance (only meaningful when
        /// Standalone -- an Inline widget's host step decides its own advancement) </returns>
        bool Handle_Postback(IFormCollection Form, Submission_State State, RequestCache RequestSpecificValues, Custom_Tracer Tracer);
    }
}
