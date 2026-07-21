namespace SobekCM.Core.MemoryMgmt
{
    public static class SessionCache_Keys
    {
        // String keys — stored via ISession.SetString / GetString
        public const string OriginalUrl = "OriginalURL";
        public const string IpRangeMembership = "IpRangeMembership";
        public const string InternalHeader = "InternalHeader";
        public const string LastSearch = "LastSearch";
        public const string LastResults = "LastResults";
        public const string LastMode = "LastMode";
        public const string OnLoadMessage = "OnLoadMessage";
        public const string OnLoadWindow = "OnLoadWindow";
        public const string UploadedFile = "UploadedFile";

        // Object keys — stored via Context.SessionObject()[key]
        public const string LastException = "Last_Exception";
        public const string User = "User";

        /// <summary> Holds the JSON-serialized map search results (item ID / latitude / longitude points) used to plot markers on the Google Map results viewer. Formerly known by the abbreviation "DSR" (Display Search Results). </summary>
        public const string DisplaySearchResults = "DisplaySearchResults";
    }
}
