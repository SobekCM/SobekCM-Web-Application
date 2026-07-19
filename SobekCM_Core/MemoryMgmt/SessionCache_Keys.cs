namespace SobekCM.Core.MemoryMgmt
{
    public static class SessionCache_Keys
    {
        // String keys — stored via ISession.SetString / GetString
        public const string OriginalUrl = "Original_URL";
        public const string IpRangeMembership = "IP_Range_Membership";
        public const string InternalHeader = "internal_header";
        public const string LastSearch = "LastSearch";
        public const string LastResults = "LastResults";
        public const string LastMode = "Last_Mode";
        public const string OnLoadMessage = "ON_LOAD_MESSAGE";
        public const string OnLoadWindow = "ON_LOAD_WINDOW";

        // Object keys — stored via Context.SessionObject()[key]
        public const string LastException = "Last_Exception";
        public const string User = "User";

        // Legacy / other
        public const string UserId = "UserId";
    }
}
