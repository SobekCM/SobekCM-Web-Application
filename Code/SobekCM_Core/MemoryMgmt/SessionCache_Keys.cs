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

        /// <summary> UI language chosen for this session, via an explicit "l=xx" in the URL or seeded from a logged-on user's preference </summary>
        public const string Language = "Language";

        // Object keys — stored via Context.SessionObject()[key]
        public const string LastException = "Last_Exception";
        public const string User = "User";

        /// <summary> Pending news (a <see cref="SobekCM.Core.Users.User_Pending_News"/>) for the logged-on user, loaded from the database on the first page after logging on, until each item is closed </summary>
        public const string PendingNews = "PendingNews";

        /// <summary> Holds the JSON-serialized map search results (item ID / latitude / longitude points) used to plot markers on the Google Map results viewer. Formerly known by the abbreviation "DSR" (Display Search Results). </summary>
        public const string DisplaySearchResults = "DisplaySearchResults";
    }
}
