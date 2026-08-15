namespace SobekCM.Core.MemoryMgmt
{
    public class RequestCache_Keys
    {
        public const string BaseUrl = "Base_URL";

        public const string RequestUrl = "Request_URL";

        public const string UserIP = "User_IP";

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
