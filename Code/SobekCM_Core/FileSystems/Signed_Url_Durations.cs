using SobekCM.Core.Settings;
using System;

namespace SobekCM.Core.FileSystems
{
    /// <summary> The configured signed GCS URL lifetimes, plus the one rule for which of them applies to a given URL </summary>
    /// <remarks> Shared by GCS_FileSystem, which signs the URL, and by any viewer that needs to know when that URL
    /// will expire, such as PDF_ItemViewer's expired-link overlay. Keeping a single copy of the rule matters: the
    /// overlay used to work out the expiration on its own, which is how it came to disagree with how the URL was
    /// actually signed. </remarks>
    public class Signed_Url_Durations
    {
        /// <summary> Constructor for a new instance of the Signed_Url_Durations class </summary>
        /// <param name="Continuous_Duration"> Lifetime for a file the page keeps requesting while open </param>
        /// <param name="Page_Load_Duration"> Lifetime for a file fetched immediately while the page renders </param>
        /// <param name="Download_Duration"> Lifetime for a link the user clicks later </param>
        /// <param name="Restricted_Duration"> Cap on page-load and download lifetimes for a restricted item </param>
        /// <param name="Restricted_Streaming_Duration"> Cap on the continuous lifetime for a restricted item </param>
        public Signed_Url_Durations(TimeSpan Continuous_Duration, TimeSpan Page_Load_Duration, TimeSpan Download_Duration, TimeSpan Restricted_Duration, TimeSpan Restricted_Streaming_Duration)
        {
            this.Continuous_Duration = Continuous_Duration;
            this.Page_Load_Duration = Page_Load_Duration;
            this.Download_Duration = Download_Duration;
            this.Restricted_Duration = Restricted_Duration;
            this.Restricted_Streaming_Duration = Restricted_Streaming_Duration;
        }

        /// <summary> Lifetime for a file the page keeps requesting while it's open, and for any call site that
        /// doesn't choose a lifetime -- see <see cref="Signed_Url_Lifetime_Enum.Continuous"/> </summary>
        public TimeSpan Continuous_Duration { get; }

        /// <summary> Lifetime for a file the browser fetches immediately while the page renders -- see
        /// <see cref="Signed_Url_Lifetime_Enum.Page_Load"/> </summary>
        public TimeSpan Page_Load_Duration { get; }

        /// <summary> Lifetime for a link the user clicks after the page renders -- see
        /// <see cref="Signed_Url_Lifetime_Enum.Download"/> </summary>
        public TimeSpan Download_Duration { get; }

        /// <summary> Cap on the page-load and download lifetimes for an IP- or user-group-restricted item </summary>
        public TimeSpan Restricted_Duration { get; }

        /// <summary> Cap on the continuous lifetime for an IP- or user-group-restricted item </summary>
        public TimeSpan Restricted_Streaming_Duration { get; }

        /// <summary> Builds the durations from the instance-wide server settings </summary>
        /// <param name="Servers"> Server settings holding the configured expiration minutes </param>
        public static Signed_Url_Durations From_Settings(Server_Settings Servers)
        {
            return new Signed_Url_Durations(
                TimeSpan.FromMinutes(Servers.GCS_Signed_Url_Expiration_Minutes),
                TimeSpan.FromMinutes(Servers.GCS_Page_Load_Signed_Url_Expiration_Minutes),
                TimeSpan.FromMinutes(Servers.GCS_Download_Signed_Url_Expiration_Minutes),
                TimeSpan.FromMinutes(Servers.GCS_Restricted_Signed_Url_Expiration_Minutes),
                TimeSpan.FromMinutes(Servers.GCS_Restricted_Streaming_Signed_Url_Expiration_Minutes));
        }

        /// <summary> How long a signed URL should stay valid, given how the call site uses it </summary>
        /// <param name="Lifetime"> How the call site uses the URL </param>
        /// <param name="ForceDownload"> Whether the URL is a forced-download link </param>
        /// <param name="IsRestricted"> Whether the item is IP- or user-group-restricted (but not dark) </param>
        /// <remarks> A forced download is a link someone clicks later, so it never gets the long continuous
        /// lifetime even when the caller didn't say. A restricted item is always capped, but a file the page
        /// keeps requesting gets its own, longer cap: a PDF, audio or video viewer makes a fresh request on every
        /// scroll and seek, so a short cap breaks it partway through even for a user allowed to see it. </remarks>
        public TimeSpan For(Signed_Url_Lifetime_Enum Lifetime, bool ForceDownload, bool IsRestricted)
        {
            if ((ForceDownload) && (Lifetime == Signed_Url_Lifetime_Enum.Continuous))
                Lifetime = Signed_Url_Lifetime_Enum.Download;

            TimeSpan duration;
            switch (Lifetime)
            {
                case Signed_Url_Lifetime_Enum.Page_Load:
                    duration = Page_Load_Duration;
                    break;

                case Signed_Url_Lifetime_Enum.Download:
                    duration = Download_Duration;
                    break;

                default:
                    duration = Continuous_Duration;
                    break;
            }

            if (!IsRestricted)
                return duration;

            TimeSpan cap = (Lifetime == Signed_Url_Lifetime_Enum.Continuous) ? Restricted_Streaming_Duration : Restricted_Duration;
            return (cap < duration) ? cap : duration;
        }
    }
}
