namespace SobekCM.Library.ItemViewer.Viewers
{
    /// <summary> Why the zoomable (JPEG2000) viewer is being withheld from a request, if it is </summary>
    /// <remarks> Decided once per request (see JPEG2000_ItemViewer_Prototyper.Zoom_Withheld_Reason), and used by the
    /// JPEG viewer to choose between its normal "switch to zoomable" link and a short notice saying why zoom isn't
    /// offered right now. </remarks>
    public enum JP2_Zoom_Withheld_Enum : byte
    {
        /// <summary> Zoom is available to this request </summary>
        Not_Withheld,

        /// <summary> The request is from a robot, which never gets zoom and isn't shown a notice </summary>
        Robot,

        /// <summary> The site-wide circuit breaker is open (automatic, or JP2RateLimiting:ManualDisable), so nobody
        /// gets zoom and logging on won't help </summary>
        Circuit_Breaker,

        /// <summary> This anonymous request's subnet has used up the anonymous JP2 budget; logging on gives zoom back </summary>
        Anonymous_Budget,

        /// <summary> This logged-on request's subnet has used up the logged-on JP2 budget; zoom comes back when the
        /// window resets </summary>
        Logged_On_Budget
    }
}
