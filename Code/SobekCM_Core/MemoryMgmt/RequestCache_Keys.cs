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
    }
}
