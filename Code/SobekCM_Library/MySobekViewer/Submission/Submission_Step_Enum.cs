namespace SobekCM.Library.MySobekViewer.Submission
{
    /// <summary> The fixed sequence of steps in the new Type-driven submission wizard </summary>
    /// <remarks> Every submission walks this same sequence in order -- <see cref="Permissions"/> and
    /// <see cref="SeriesFinder"/> are simply skipped (never rendered) when they don't apply to this
    /// submission, rather than being removed from the enum. Widgets are NOT steps in this enum -- an
    /// Inline widget embeds inside whichever of these steps it names as its host, and a Standalone
    /// widget is inserted into the sequence at its own anchor point (see
    /// <c>SobekCM_Item_Type_Widget</c>'s eventual Mode/anchor columns), not represented here. </remarks>
    public enum Submission_Step_Enum
    {
        /// <summary> "Before you submit" gate -- shown once per assigned agreement, not once per submission </summary>
        Permissions = 1,

        /// <summary> The Type grid -- always the same, first real step of every submission </summary>
        TypeSelection = 2,

        /// <summary> Upload -- shape varies by Type, resolved through <see cref="Upload_Step_Factory"/> </summary>
        Upload = 3,

        /// <summary> "Does this belong to an existing title?" -- only when the chosen Type's
        /// ShowSeriesFinder flag is set (Newspaper, MultiVolume) </summary>
        SeriesFinder = 4,

        /// <summary> The one metadata entry page, assembled from the Type's ordered metadata blocks </summary>
        Metadata = 5,

        /// <summary> Review &amp; submit -- hosts any Standalone widgets and the final Submit action.
        /// Skipped (like Permissions/SeriesFinder) whenever the chosen Type has no Standalone widgets,
        /// since there is nothing to review that Metadata's own screen didn't already show; today that
        /// means it is always skipped, because no Type can yet declare a Standalone widget. </summary>
        Confirm = 6,

        /// <summary> Terminal success/failure landing page -- always shown, never skipped, and never
        /// backed into. This is where the actual item save happens (see
        /// <see cref="New_Submission_MySobekViewer"/>'s <c>perform_final_submission</c>), triggered once
        /// on the postback that first advances into this step. </summary>
        Congratulations = 7
    }
}
