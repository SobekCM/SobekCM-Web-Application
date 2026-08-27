namespace SobekCM.Core.Navigation
{
    /// <summary> Constants for every admin-viewer code recognized by this instance, both core (built into
    /// <see cref="SobekCM.Library.AdminViewer.AdminViewer_Factory"/> directly, no reflection) and the codes
    /// used to reach a plugin-supplied admin viewer registered via an extension's <c>&lt;adminViewer&gt;</c>
    /// config element </summary>
    /// <remarks> Never use raw string literals for these codes — same rule as <c>SessionCache_Keys</c>/
    /// <c>RequestCache_Keys</c>. Each core constant's value is also the literal "admin/{code}" URL segment
    /// for that view (see <see cref="SobekCM.Core.Navigation.UrlWriterHelper"/>), so changing a value here
    /// changes that view's URL. </remarks>
    public static class Admin_View_Codes
    {
        /// <summary> Administrative home page with links to all the Admin tasks ( bare "admin" URL, not "admin/home" ) </summary>
        public const string Home = "home";

        /// <summary> Adds a single collection to this instance, via the wizard </summary>
        public const string Add_Collection_Wizard = "addcoll";

        /// <summary> Allows all the information and behaviors for a single aggregation to be viewed / edited </summary>
        public const string Aggregation_Single = "editaggr";

        /// <summary> Provides list of all existing aggregationPermissions and allows admin to enter a new aggregation </summary>
        public const string Aggregations_Mgmt = "aggregations";

        /// <summary> Provides list of all aggregation aliases and allows admin to perform some very basic tasks </summary>
        public const string Aliases = "aliases";

        /// <summary> Allows a single builder folder to be either added or edited online </summary>
        public const string Builder_Folder_Mgmt = "builderfolder";

        /// <summary> Gives the current SobekCM status and allows an authenticated system admin to temporarily halt the builder remotely via a database flag </summary>
        public const string Builder_Status = "builder";

        /// <summary> Provides list of all existing default metadata files and allows admin to enter a new default or edit an existing default </summary>
        public const string Default_Metadata = "defaults";

        /// <summary> Provides list of the IP restriction lists and allows admins to edit the single IPs within the range(s) </summary>
        public const string IP_Restrictions = "restrictions";

        /// <summary> Allows admin to perform some limited cache reset functions </summary>
        public const string Reset = "reset";

        /// <summary> Allows admins to view and edit system-wide settings from the database </summary>
        public const string Settings = "settings";

        /// <summary> Allows a single permissions agreement to be either added or edited online </summary>
        public const string Permission_Agreement_Single = "permagreement";

        /// <summary> Provides list of all existing permissions agreements and allows admin to enter a new one </summary>
        public const string Permission_Agreements_Mgmt = "permagreements";

        /// <summary> Detailed editing of a single web skin </summary>
        public const string Skins_Single = "editskin";

        /// <summary> Provides list of all existing web skins and allows admin to enter a new web skin or edit an existing web skin </summary>
        public const string Skins_Mgmt = "webskins";

        /// <summary> Administrative features related to the TEI plug-in </summary>
        public const string TEI = "tei";

        /// <summary> Allows the host admin to configure the OpenID Connect (OIDC) sign-in extension's identity providers </summary>
        public const string OIDC_Auth = "oidc";

        /// <summary> Allows the host admin to configure the SAML sign-in extension's identity providers </summary>
        public const string SAML_Auth = "saml";

        /// <summary> Allows the system administrator to add new thematic headings to the main home page </summary>
        public const string Thematic_Headings = "headings";

        /// <summary> Allows admin to perform some limited actions against the URL Portals data </summary>
        public const string URL_Portals = "portals";

        /// <summary> Provides list of all users and allows admin to perform some very basic tasks </summary>
        public const string Users = "users";

        /// <summary> Allows for editing and viewing of user groups </summary>
        public const string User_Groups = "groups";

        /// <summary> Provides top-level reports regarding permissions granted to users </summary>
        public const string User_Permissions_Reports = "permissions";

        /// <summary> Provides a list of currently pending user requests (i.e., to join a group or allow submission) </summary>
        public const string User_Requests = "requests";

        /// <summary> Form to add a new web content page to the system </summary>
        public const string WebContent_Add_New = "webadd";

        /// <summary> View recent updates to the top-level static HTML web content pages </summary>
        public const string WebContent_History = "webhistory";

        /// <summary> Manage the list of all top-level static HTML web content pages in this instance </summary>
        public const string WebContent_Mgmt = "webcontent";

        /// <summary> Allows all the information and behaviors about a single web content page to be edited </summary>
        public const string WebContent_Single = "websingle";

        /// <summary> View the usage stats for the top-level static HTML web content pages in this instance </summary>
        public const string WebContent_Usage = "webusage";

        /// <summary> Provides list of all existing wordmarks/icons and allows admin to enter a new wordmark or edit an existing wordmark </summary>
        public const string Wordmarks = "wordmarks";
    }
}
