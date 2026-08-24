namespace SobekCM.Core.Navigation
{
    /// <summary> Constants for every main-writer code recognized by this instance, both core (built into
    /// <see cref="SobekCM.Library.MainWriters.MainWriter_Factory"/> directly, no reflection) and the codes
    /// used to reach a plugin-supplied main writer registered via an extension's <c>&lt;mainWriter&gt;</c>
    /// config element </summary>
    /// <remarks> Never use raw string literals for these codes — same rule as <c>Admin_View_Codes</c>/
    /// <c>SessionCache_Keys</c>/<c>RequestCache_Keys</c>. Member names deliberately match the old
    /// <c>Writer_Type_Enum</c> values exactly (not the lowercase string values) so every existing reference
    /// converts with a plain <c>Writer_Codes.</c> → <c>Writer_Codes.</c> substitution. Where a writer has
    /// a real "urlrelative" segment (see <c>QueryString_Analyzer</c>), the constant's *value* matches that
    /// segment exactly, even though the constant *name* doesn't. </remarks>
    public static class Writer_Codes
    {
        /// <summary> Response should be in HTML - the default/fallback writer, always core, never plugin-loaded </summary>
        public const string HTML = "html";

        /// <summary> Response should be in HTML, but the user is logged in - still <c>Html_MainWriter</c>,
        /// just a distinct code so a logon-state change forces a client refresh </summary>
        public const string HTML_LoggedIn = "html_loggedin";

        /// <summary> Simple writer that echoes an existing pre-rendered HTML page through this application
        /// with very little logic (used for robot search engines mostly) </summary>
        /// <remarks> Deliberately NOT resolved through <c>MainWriter_Factory</c> - its trigger mechanism is
        /// structurally different from every other writer (set mid-render from <c>Aggregation_HtmlSubwriter</c>,
        /// not from initial query parsing) and is not yet fully understood. Kept as a named constant only so
        /// the few places that still reference it don't use a bare string literal. </remarks>
        public const string HTML_Echo = "html_echo";

        /// <summary> Response should be in microsoft compliant dataset format </summary>
        public const string DataSet = "dataset";

        /// <summary> Response should be a portion of a datatable, in JSON response (jQuery Datatable plug-in) </summary>
        public const string Data_Provider = "dataprovider";

        /// <summary> Response should be in simplified JSON format </summary>
        public const string JSON = "json";

        /// <summary> Response should be in IIIF (International Image Interoperability Framework) format </summary>
        public const string IIIF = "iiif";

        /// <summary> Response should be compliant with the OAI-PMH standard - triggered by a "verb" query
        /// string parameter, not a URL segment (see <c>MainWriterConfig.EarlyExitQueryParam</c>) </summary>
        public const string OAI = "oai";

        /// <summary> Response should be simple, unformatted text </summary>
        /// <remarks> No writer implements this today - the "textonly" URL segment sets this code, and
        /// <c>MainWriter_Factory</c>'s fallback silently serves <see cref="HTML"/> instead, same as before
        /// this conversion. Kept as its own code rather than merged into <see cref="HTML"/> so anything else
        /// reading <c>Navigation_Object.Writer_Type</c> still sees the same distinct value it always has. </remarks>
        public const string Text = "text";

        /// <summary> Response should be in simplified XML format </summary>
        public const string XML = "xml";
    }
}
