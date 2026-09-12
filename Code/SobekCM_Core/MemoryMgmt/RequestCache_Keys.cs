namespace SobekCM.Core.MemoryMgmt
{
    public class RequestCache_Keys
    {
        public const string BaseUrl = "Base_URL";

        public const string RequestUrl = "Request_URL";

        public const string UserIP = "User_IP";

        /// <summary> The requester's /24 (IPv4) or /48 (IPv6) subnet key, derived from <see cref="UserIP"/>
        /// by UserIpInitializer -- see <see cref="ClientSubnetKey"/> for the masking itself, and
        /// JP2RateLimiting_Gateway for what it's keyed against. Computed once per request here rather than
        /// on demand, since the rate-limiting work keys progressively more of its budgets on this. Read it
        /// from the request cache rather than re-deriving it from UserIP. </summary>
        public const string UserSubnetKey = "User_Subnet_Key";

        public const string PageName = "PageName";

        public const string OriginalUrl = "Original_URL";

        /// <summary> Holds the JSON-serialized map search results (item ID / latitude / longitude points) used to plot markers on the Google Map results viewer. Formerly known by the abbreviation "DSR" (Display Search Results). </summary>
        public const string DisplaySearchResults = "DisplaySearchResults";

        public const string ShowToc = "Show TOC";

        /// <summary> Holds the current request's Custom_Tracer, stashed as soon as it's created in
        /// QueryInitializer's constructor so the global exception-handler middleware in Program.cs can
        /// still log the full trace route for exceptions that reach it unwrapped (i.e. not bundled into
        /// a SobekCM_Traced_Exception), which is the common case. </summary>
        public const string Tracer = "Tracer";
    }
}
