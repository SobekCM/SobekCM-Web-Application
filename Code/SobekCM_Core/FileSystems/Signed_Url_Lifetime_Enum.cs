namespace SobekCM.Core.FileSystems
{
    /// <summary> How long a signed GCS URL should stay valid, chosen by how the call site actually uses the URL
    /// rather than by the file's type -- the same JPG is a page image fetched instantly in one viewer and a
    /// link clicked minutes later in another, and the same PDF is streamed in one and downloaded from another. </summary>
    /// <remarks> GCS checks a signed URL's expiration when each request starts, not for the life of the
    /// transfer, so what a lifetime has to cover is the time until the LAST request using that URL begins --
    /// not the size of the file or how long it takes to arrive. Shorter lifetimes don't limit what can be pulled
    /// within the rate limits (every URL still has to be minted by a page view); what they limit is reuse: a
    /// harvested list of URLs, or links pasted somewhere else, stop working quickly. </remarks>
    public enum Signed_Url_Lifetime_Enum
    {
        /// <summary> The URL keeps being requested for as long as the page stays open -- PDFs, audio and video
        /// (range requests on every scroll, seek and buffer) and the page turner (a whole book of page URLs baked
        /// into the page at once). The longest lifetime, and the default, so a call site that never considers
        /// this keeps working. Uses GCS Signed URL Expiration Minutes. </summary>
        Continuous,

        /// <summary> A link the user clicks at some point after the page renders, such as the Downloads list.
        /// Only has to cover the time until the click, since a download already under way isn't cut off when
        /// its URL expires. A forced download is always treated as at most this. Uses GCS Download URL
        /// Expiration Minutes. </summary>
        Download,

        /// <summary> The browser fetches the URL immediately while the page renders, such as an &lt;img&gt;
        /// page image or thumbnail, and nothing reuses it afterwards. Can be very short. Uses GCS Page Load URL
        /// Expiration Minutes. Not safe for an image that loads later (a lazy-loaded image, or one swapped in by
        /// script), which should use <see cref="Continuous"/> instead. </summary>
        Page_Load
    }
}
