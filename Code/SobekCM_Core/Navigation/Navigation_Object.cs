#region Using directives

using ProtoBuf;
using SobekCM.Core.Configuration.Localization;
using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;

#endregion

namespace SobekCM.Core.Navigation
{
    /// <summary> This object stores the current and next mode information for a single HTTP request. <br /> <br /> </summary>
    /// <remarks>  Object written by Mark V Sullivan for the University of Florida. </remarks>
    [Serializable, DataContract, ProtoContract]
    [XmlRoot("navigationObject")]
    public class Navigation_Object
    {
        #region Private members of this object

        private string searchFields;
        private string searchString;

        #endregion

        #region Constructors

        /// <summary> Constructor for a new instance of the SobekCM_Navigation_Object which stores 
        /// all of the information about an individual request. </summary>
        public Navigation_Object()
        {
            // Do general item construction
            Constructor_Helper();
        }

        private void Constructor_Helper()
        {
            // Declare some defaults
            Admin_Type = null;
            Mode = Display_Mode_Enum.Error;
            Search_Type = Search_Type_Enum.NONE;
            Statistics_Type = Statistics_Type_Enum.NONE;
            Internal_Type = Internal_Type_Enum.NONE;
            My_Sobek_Type = My_Sobek_Type_Enum.NONE;
            Language = "en";
            Default_Language = "en";
            Writer_Type = Writer_Codes.HTML;
            TOC_Display = TOC_Display_Type_Enum.Undetermined;
            Trace_Flag = Trace_Flag_Type_Enum.Unspecified;
            Search_Precision = Search_Precision_Type_Enum.Contains;
            WebContent_Type = WebContent_Type_Enum.NONE;


            Skin = "sobek";
            Default_Skin = "sobek";
            Portal_Abbreviation = "SOBEK";
            Portal_Name = "Default SobekCM Library";

            Skin_In_URL = false;
            isPostBack = false;
            Is_Robot = false;
            Logon_Required = false;
            Request_Completed = false;
        }

        #endregion

        #region Code to set the robot flag from request variables

        /// <summary> Tests the user agent against known crawlers to determine if this request is from a
        /// search engine indexer or other web crawler that identifies itself. </summary>
        /// <param name="UserAgent">User Agent string from the HTTP request</param>
        /// <returns>TRUE if the request appears to be a robot, otherwise FALSE</returns>
        /// <remarks> User agent only. The old IP-address matching (a handful of long-stale crawler addresses)
        /// was dropped: crawlers rotate addresses, and catching unidentified traffic by IP is the rate
        /// limiters' job, not this check's. </remarks>
        public static bool Is_UserAgent_Robot(string UserAgent)
        {
            if (String.IsNullOrEmpty(UserAgent))
                return false;

            if (UserAgent.AsSpan().IndexOfAny(Robot_UserAgent_Search_Values) >= 0)
                return true;

            foreach (string prefix in Robot_UserAgent_Prefixes)
            {
                if (UserAgent.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        /// <summary> Uppercase user agent substrings that identify a crawler, matched anywhere in the user agent </summary>
        /// <remarks> Being flagged as a robot doesn't block anyone; robots.txt decides who is allowed in. It
        /// changes how the request is served: the fast static item page with its full text, no zoomable
        /// viewer, and none of the pages that need a logon (mySobek, search results, print, browse-by, public
        /// folders). So every crawler that identifies itself belongs here, including AI training crawlers
        /// that robots.txt blocks, since any that ignore robots.txt still get the cheap page. Two things
        /// deliberately aren't here:
        /// <list type="bullet">
        /// <item> A bare "BOT" catch-all. Some real phones (the Cubot brand) carry "BOT" in their user agent,
        /// and a person flagged as a robot can't log on or search. </item>
        /// <item> Social link-preview fetchers (facebookexternalhit, Twitterbot, LinkedInBot, Slackbot,
        /// Discordbot, WhatsApp, TelegramBot), which fetch a page once to build a share card and need the
        /// normal page so shared links keep their title and thumbnail. Check any new token doesn't also
        /// match one of these. </item>
        /// </list>
        /// This only catches crawlers honest about who they are; anything spoofing a browser user agent is
        /// left to the rate limiters. </remarks>
        private static readonly string[] Robot_UserAgent_Tokens =
        {
            // Search engines
            "GOOGLEBOT", "ADSBOT-GOOGLE", "GOOGLEOTHER", "GOOGLE-INSPECTIONTOOL",
            "BINGBOT", "BINGPREVIEW", "MSNBOT",
            "SLURP", "YANDEX", "BAIDUSPIDER", "APPLEBOT", "DUCKDUCKBOT", "PETALBOT", "SEZNAMBOT",
            "MOJEEKBOT", "COCCOCBOT", "QWANTBOT", "SOGOU+WEB+SPIDER", "SOSOSPIDER", "YYSPIDER",
            "SHOULU.JIKE.COM/SPIDER", "ABOUT.ASK.COM", "SCOUTJET", "GIGABOT", "AISEARCHBOT",
            "NEXTGENSEARCHBOT", "WBSEARCHBOT", "SEARCHME.COM", "PICSEARCH.COM", "DISCOVERYBOT",

            // AI search and answer engines
            "OAI-SEARCHBOT", "PERPLEXITYBOT", "CLAUDE-SEARCHBOT", "YOUBOT", "DUCKASSISTBOT",

            // AI training crawlers
            "GPTBOT", "CLAUDEBOT", "CLAUDE-WEB", "ANTHROPIC-AI", "CCBOT", "BYTESPIDER", "AMAZONBOT",
            "META-EXTERNALAGENT", "META-EXTERNALFETCHER", "FACEBOOKBOT", "DIFFBOT", "OMGILIBOT",
            "COHERE-AI", "AI2BOT", "IMAGESIFTBOT", "TIMPIBOT",

            // AI fetches a person triggered by asking about one specific page
            "CHATGPT-USER", "CLAUDE-USER", "PERPLEXITY-USER", "MISTRALAI-USER",

            // SEO and marketing crawlers
            "AHREFSBOT", "HTTP://AHREFS.COM/ROBOT", "SEMRUSHBOT", "DOTBOT", "ROGERBOT", "BLEXBOT",
            "DATAFORSEOBOT", "SERPSTATBOT", "BARKROWLER", "MJ12BOT", "SEARCHMETRICBOT", "URLAPPENDBOT",

            // Web archives
            "ARCHIVE.ORG_BOT", "IA_ARCHIVER",

            // Generic crawler names, and older bots, tools and site copiers kept from the original list
            "CRAWLER", "PLONEBOT", "CAZOODLEBOT", "DISCOBOT", "ATRAXBOT", "SITEBOT", "LINGUEE+BOT", "MLBOT",
            "BENDERTHEWEBROBOT", "BENDERTHEROBOT.TUMBLR.COM", "EZOOMS.BOT", "BEWSLEBOT", "WEBVAC",
            "SITESUCKER", "XENU+LINK+SLEUTH", "CAMONTSPIDER", "ICOPYRIGHT+CONDUCTOR", "WWW.PROFOUND.NET",
            "HAVIJ", "SYNAPSE"
        };

        /// <summary> Uppercase user agent prefixes that identify a crawler, matched only at the very start of
        /// the user agent because the same text elsewhere is too common to trust </summary>
        private static readonly string[] Robot_UserAgent_Prefixes =
        {
            "LSSBOT", "JAVA/"
        };

        /// <summary> <see cref="Robot_UserAgent_Tokens"/> compiled once into a single multi-substring matcher,
        /// so checking a user agent is one case-insensitive pass over it instead of a separate scan per token,
        /// with no uppercased copy of the user agent on every request </summary>
        /// <remarks> Must stay declared after <see cref="Robot_UserAgent_Tokens"/>: static fields initialize in
        /// declaration order, so declaring this first would build it from a still-null array and fail the
        /// whole type's initialization. </remarks>
        private static readonly System.Buffers.SearchValues<string> Robot_UserAgent_Search_Values =
            System.Buffers.SearchValues.Create(Robot_UserAgent_Tokens, StringComparison.OrdinalIgnoreCase);

        /// <summary> Tests the user agent against known crawlers to determine if this request is from a
        /// search engine indexer or other web crawler that identifies itself -- see
        /// <see cref="Is_UserAgent_Robot"/>. This returns the value and also sets the internal robot flag. </summary>
        /// <param name="UserAgent">User Agent string from the HTTP request</param>
        /// <returns>TRUE if the request appears to be a robot, otherwise FALSE</returns>
        public bool Set_Robot_Flag(string UserAgent)
        {
            Is_Robot = Is_Robot || Is_UserAgent_Robot(UserAgent);
            return Is_Robot;
        }

        #endregion

        #region Public Properties


        /// <summary> Admin type of display for the current request - a plugin-extensible code (see
        /// <see cref="Admin_View_Codes"/> for the built-in ones), resolved to a viewer by
        /// <see cref="SobekCM.Library.AdminViewer.AdminViewer_Factory"/>. NULL/empty means no admin type
        /// specified, not applicable. </summary>
        [DataMember(EmitDefaultValue = false, Name = "adminType")]
        [XmlElement("adminType")]
        [ProtoMember(1)]
        public string Admin_Type { get; set; }

        /// <summary> Current aggregation code </summary>
        /// <remarks>The value returned is always lower case</remarks>
        [DataMember(EmitDefaultValue = false, Name = "aggregation")]
        [XmlElement("aggregation")]
        [ProtoMember(2)]
        public string Aggregation { get; set; }

        /// <summary> Current aggregation alias code, if there is one </summary>
        /// <remarks>The value returned is always lower case</remarks>
        [DataMember(EmitDefaultValue = false, Name = "aggrAlias")]
        [XmlElement("aggrAlias")]
        [ProtoMember(3)]
        public string Aggregation_Alias { get; set; }

        /// <summary> Submode for agrgegation views </summary>
        [DataMember(EmitDefaultValue = false, Name = "aggrType")]
        [XmlElement("aggrType")]
        [ProtoMember(4)]
        public Aggregation_Type_Enum Aggregation_Type { get; set; }

        /// <summary> Base infterface interface for display purposes </summary>
        /// <remarks> The value returned is always lower case</remarks>
        [DataMember(EmitDefaultValue = false, Name = "baseSkin")]
        [XmlElement("baseSkin")]
        [ProtoMember(5)]
        public string Base_Skin { get; set; }

        /// <summary> Base infterface interface for display purposes </summary>
        /// <remarks> The value returned is always lower case</remarks>
        [XmlIgnore]
        [IgnoreDataMember]
        public string Base_Skin_Or_Skin
        {
            get
            {
                if (!string.IsNullOrEmpty(Base_Skin))
                    return Base_Skin;
                return Skin ?? String.Empty;
            }
        }

        /// <summary> Base URL requested by the user </summary>
        /// <remarks> This is the URL post-rewriting from any URL path rewrite routine (such as SobekCM_URL_Rewriter) </remarks>
        [DataMember(EmitDefaultValue = false, Name = "baseUrl")]
        [XmlAttribute("baseUrl")]
        [ProtoMember(6)]
        public string Base_URL { get; set; }

        /// <summary> Bib id to display </summary>
        /// <remarks>The value returned is always upper case </remarks>
        [DataMember(EmitDefaultValue = false, Name = "bibid")]
        [XmlElement("bibid")]
        [ProtoMember(7)]
        public string BibID { get; set; }

        /// <summary> Browser type </summary>
        [DataMember(EmitDefaultValue = false, Name = "browser")]
        [XmlElement("browser")]
        [ProtoMember(8)]
        public string Browser_Type { get; set; }

        /// <summary> Exception generated during execution </summary>
        [DataMember(EmitDefaultValue = false, Name = "exception")]
        [XmlIgnore]
        [ProtoMember(9)]
        public Exception Caught_Exception { get; set; }

        /// <summary> Coordinate search string for a geographic search </summary>
        [DataMember(EmitDefaultValue = false, Name = "coordinates")]
        [XmlElement("coordinates")]
        [ProtoMember(10)]
        public string Coordinates { get; set; }

        /// <summary> Beginning of a date range, if the search includes
        /// a date range between two arbitrary dates </summary>
        [DataMember(EmitDefaultValue = false, Name = "dateRangeDate1")]
        [XmlElement("dateRangeDate1")]
        [ProtoMember(11)]
        public DateTime? DateRange_Date1 { get; set; }

        /// <summary> End of a date range, if the search includes
        /// a date range between two arbitrary dates </summary>
        [DataMember(EmitDefaultValue = false, Name = "dateRangeDate2")]
        [XmlElement("dateRangeDate2")]
        [ProtoMember(12)]
        public DateTime? DateRange_Date2 { get; set; }

        /// <summary> Beginning of the year range, if the search includes
        /// a date range between two years </summary>
        [DataMember(EmitDefaultValue = false, Name = "dateRangeYear1")]
        [XmlElement("dateRangeYear1")]
        [ProtoMember(13)]
        public short? DateRange_Year1 { get; set; }

        /// <summary> End of the year range, if the search includes
        /// a date range between two years </summary>
        [DataMember(EmitDefaultValue = false, Name = "dateRangeYear2")]
        [XmlElement("dateRangeYear2")]
        [ProtoMember(14)]
        public short? DateRange_Year2 { get; set; }

        /// <summary> Default aggregation (based on original URL) </summary>
        /// <remarks>The value returned is always lower case</remarks>
        [DataMember(EmitDefaultValue = false, Name = "defaultAggregation")]
        [XmlElement("defaultAggregation")]
        [ProtoMember(15)]
        public string Default_Aggregation { get; set; }

        /// <summary> Default language code for this user, from their browser settings </summary>
        [DataMember(EmitDefaultValue = false, Name = "defaultLanguage")]
        [XmlElement("defaultLanguage")]
        [ProtoMember(16)]
        public string Default_Language { get; set; }

        /// <summary> Default interface (based on original URL) </summary>
        /// <remarks>The value returned is always lower case</remarks>
        [DataMember(EmitDefaultValue = false, Name = "defaultSkin")]
        [XmlElement("defaultSkin")]
        [ProtoMember(17)]
        public string Default_Skin { get; set; }

        /// <summary> Simple error message generated during execution </summary>
        [DataMember(EmitDefaultValue = false, Name = "error")]
        [XmlElement("error")]
        [ProtoMember(18)]
        public string Error_Message { get; set; }

        /// <summary> Primary key for the folder to display </summary>
        [DataMember(EmitDefaultValue = false, Name = "folder")]
        [XmlElement("folder")]
        [ProtoMember(19)]
        public int? FolderID { get; set; }

        /// <summary> Fragment utilized when only a portion of a page needs to be rendered </summary>
        [DataMember(EmitDefaultValue = false, Name = "fragment")]
        [XmlElement("fragment")]
        [ProtoMember(20)]
        public string Fragment { get; set; }

        /// <summary>Submode for the main library home page </summary>
        [DataMember(EmitDefaultValue = false, Name = "homeType")]
        [XmlElement("homeType")]
        [ProtoMember(21)]
        public Home_Type_Enum Home_Type { get; set; }

        /// <summary> Browse or info mode to display </summary>
        /// <remarks>The value returned is always lower case</remarks>
        [DataMember(EmitDefaultValue = false, Name = "infoBrowseMode")]
        [XmlElement("infoBrowseMode")]
        [ProtoMember(22)]
        public string Info_Browse_Mode { get; set; }

        /// <summary> Returns the abbreviation for this portal ( i.e., 'UDC', 'dLOC', etc... ) </summary>
        [DataMember(EmitDefaultValue = false, Name = "portalAbbreviation")]
        [XmlElement("portalAbbreviation")]
        [ProtoMember(23)]
        public string Portal_Abbreviation { get; set; }

        /// <summary> Returns the name of the portale ( i.e., 'UDC', 'dLOC', etc... ) </summary>
        [DataMember(EmitDefaultValue = false, Name = "portalName")]
        [XmlElement("portalName")]
        [ProtoMember(24)]
        public string Portal_Name { get; set; }

        /// <summary> [DEPRECATED] Backwards-compatible alias for <see cref="Portal_Abbreviation"/>, kept for external
        /// plugins still referencing the old property name; not serialized in any format </summary>
        [XmlIgnore]
        public string Instance_Abbreviation
        {
            get { return Portal_Abbreviation; }
        }

        /// <summary> [DEPRECATED] Backwards-compatible alias for <see cref="Portal_Name"/>, kept for external
        /// plugins still referencing the old property name; not serialized in any format </summary>
        [XmlIgnore]
        public string Instance_Name
        {
            get { return Portal_Name; }
        }

        /// <summary> Submode for the internal pages </summary>
        [DataMember(EmitDefaultValue = false, Name = "internalType")]
        [XmlElement("internalType")]
        [ProtoMember(25)]
        public Internal_Type_Enum Internal_Type { get; set; }

        /// <summary> Flag that is set to indicate item requested is invalid </summary>
        [DataMember(EmitDefaultValue = false, Name = "invalidItem")]
        [XmlElement("invalidItem")]
        [ProtoMember(27)]
        public bool? Invalid_Item { get; set; }

        /// <summary> Flag indicating this is a post back </summary>
        [DataMember(Name = "isPostBack")]
        [XmlAttribute("isPostBack")]
        [ProtoMember(28)]
        public bool isPostBack { get; set; }

        /// <summary> Flag indicating if the current request is from a search engine 
        /// indexer or web site crawler bot.</summary>
        /// <remarks>This value is set in by calling the <see cref="Set_Robot_Flag"/> procedure. </remarks>
        [DataMember(Name = "isRobot")]
        [XmlAttribute("isRobot")]
        [ProtoMember(29)]
        public bool Is_Robot { get; set; }

        /// <summary> (DEPRECATED) ItemID which formerly was used for indicating items in the URL </summary>
        [DataMember(EmitDefaultValue = false, Name = "itemId")]
        [XmlElement("itemId")]
        [ProtoMember(30)]
        public int? ItemID_DEPRECATED { get; set; }

        /// <summary> ISO language code for the interface </summary>
        [DataMember(EmitDefaultValue = false, Name = "language")]
        [XmlElement("language")]
        [ProtoMember(31)]
        public string Language { get; set; }

        /// <summary> Flag indicates that logon is required to access the requested mode </summary>
        [DataMember(EmitDefaultValue = false, Name = "logonRequired")]
        [XmlElement("logonRequired")]
        [ProtoMember(32)]
        public bool Logon_Required { get; set; }

        /// <summary> Flag indicates if the requested web content page (or item) is not present (possibly a bad URL) </summary>
        [DataMember(EmitDefaultValue = false, Name = "missing")]
        [XmlElement("missing")]
        [ProtoMember(75)]
        public bool? Missing { get; set; }

        /// <summary> Method suppresses XML Serialization of the Missing flag property if it is NULL </summary>
        /// <returns> TRUE if the property should be serialized, otherwise FALSE </returns>
        public bool ShouldSerializeMissing()
        {
            return Missing.HasValue;
        }

        /// <summary> Caller-supplied correlation id, read from the 'traceid' query string parameter and
        /// carried through to every subsequently-written URL, so a single value can be used to trace a
        /// call or series of calls through the system (e.g. across distributed tracing spans) </summary>
        [DataMember(EmitDefaultValue = false, Name = "traceid")]
        [XmlElement("traceid")]
        [ProtoMember(76)]
        public string TraceID { get; set; }

        /// <summary> Mode determined by parsing the query string </summary>
        [DataMember(EmitDefaultValue = false, Name = "mode")]
        [XmlAttribute("mode")]
        [ProtoMember(33)]
        public Display_Mode_Enum Mode { get; set; }

        /// <summary> mySobek submode for the current request.</summary>
        /// <remarks>The value returned is always lower case, and is also used for the admin pages</remarks>
        [DataMember(EmitDefaultValue = false, Name = "mySobekSubmode")]
        [XmlElement("mySobekSubmode")]
        [ProtoMember(34)]
        public string My_Sobek_SubMode { get; set; }

        /// <summary> mySobek type of display for the current request.</summary>
        [DataMember(EmitDefaultValue = false, Name = "mySobekType")]
        [XmlElement("mySobekType")]
        [ProtoMember(35)]
        public My_Sobek_Type_Enum My_Sobek_Type { get; set; }

        /// <summary> Page number to be displayed (either for an item or for results )</summary>
        [DataMember(EmitDefaultValue = false, Name = "page")]
        [XmlElement("page")]
        [ProtoMember(36)]
        public ushort? Page { get; set; }

        /// <summary> Filename indicated in the URL to allow direct link by page file name, rather than sequence </summary>
        [DataMember(EmitDefaultValue = false, Name = "pageByFilename")]
        [XmlElement("pageByFilename")]
        [ProtoMember(37)]
        public string Page_By_FileName { get; set; }

        /// <summary> Gets the PURL associated with this portal which should be used for building permanent links for items </summary>
        [DataMember(EmitDefaultValue = false, Name = "portalPurl")]
        [XmlElement("portalPurl")]
        [ProtoMember(38)]
        public string Portal_PURL { get; set; }

        /// <summary> If this found a web content redirect in the system, the URL that this should be redirected to </summary>
        [DataMember(EmitDefaultValue = false, Name = "redirect")]
        [XmlElement("redirect")]
        [ProtoMember(63)]
        public string Redirect { get; set; }

        /// <summary> Name of the report requested from the reporting module </summary>
        [DataMember(EmitDefaultValue = false, Name = "reportName")]
        [XmlElement("reportName")]
        [ProtoMember(39)]
        public string Report_Name { get; set; }

        /// <summary> Submode for the results display </summary>
        [DataMember(EmitDefaultValue = false, Name = "resultType")]
        [XmlElement("resultType")]
        [ProtoMember(40)]
        public string Result_Display_Type { get; set; }

        /// <summary> Return url value from the url string </summary>
        /// <remarks>This is primarily used by the mySobek feature, to return a user to their previously
        /// requested site, once they log on.</remarks>
        [DataMember(EmitDefaultValue = false, Name = "returnUri")]
        [XmlElement("returnUri")]
        [ProtoMember(41)]
        public string Return_URL { get; set; }

        /// <summary> Flag indicates if the request was completed, so no further
        /// operations should occur </summary>
        [XmlIgnore]
        [IgnoreDataMember]
        public bool Request_Completed { get; set; }

        /// <summary> Search fields </summary>
        [DataMember(Name = "searchFields")]
        [XmlElement("searchFields")]
        [ProtoMember(43)]
        public string Search_Fields
        {
            get { return searchFields ?? String.Empty; }
            set { searchFields = value; }
        }

        /// <summary> Precision to be used while performing a metadata search in the database </summary>
        [DataMember(EmitDefaultValue = false, Name = "searchPrecision")]
        [XmlElement("searchPrecision")]
        [ProtoMember(44)]
        public Search_Precision_Type_Enum Search_Precision { get; set; }

        /// <summary> Search string </summary>
        [DataMember(EmitDefaultValue = false, Name = "searchString")]
        [XmlElement("searchString")]
        [ProtoMember(45)]
        public string Search_String
        {
            get { return searchString ?? String.Empty; }
            set { searchString = value; }
        }

        /// <summary> Submode for searching </summary>
        [DataMember(EmitDefaultValue = false, Name = "searchType")]
        [XmlElement("searchType")]
        [ProtoMember(46)]
        public Search_Type_Enum Search_Type { get; set; }

        /// <summary> Flag which determines if the selection panel is shown
        /// for an aggregation search </summary>
        [DataMember(EmitDefaultValue = false, Name = "showSelectionPanel")]
        [XmlElement("showSelectionPanel")]
        [ProtoMember(47)]
        public bool? Show_Selection_Panel { get; set; }

        /// <summary> Size of Thumbnails to appear in the related items viewer </summary>
        [DataMember(EmitDefaultValue = false, Name = "thumbnailSize")]
        [XmlElement("thumbnailSize")]
        [ProtoMember(48)]
        public short? Size_Of_Thumbnails { get; set; }

        /// <summary> Current skin for display purposes </summary>
        /// <remarks>The value returned is always lower case</remarks>
        [DataMember(EmitDefaultValue = false, Name = "skin")]
        [XmlElement("skin")]
        [ProtoMember(49)]
        public string Skin { get; set; }

        /// <summary> Flag which indicates the interface is indicated in the URL and query string </summary>
        [DataMember(EmitDefaultValue = false, Name = "skinInUrl")]
        [XmlElement("skinInUrl")]
        [ProtoMember(50)]
        public bool Skin_In_URL { get; set; }

        /// <summary> Sort type employed for displaying result sets </summary>
        [DataMember(EmitDefaultValue = false, Name = "sort")]
        [XmlElement("sort")]
        [ProtoMember(51)]
        public short? Sort { get; set; }

        /// <summary> Submode for the statistics pages </summary>
        [DataMember(EmitDefaultValue = false, Name = "statsType")]
        [XmlElement("statsType")]
        [ProtoMember(52)]
        public Statistics_Type_Enum Statistics_Type { get; set; }

        /// <summary> Information about which sub aggregationPermissions to include or exclude
        /// during a collection group search</summary>
        [DataMember(EmitDefaultValue = false, Name = "subAggregation")]
        [XmlElement("subAggregation")]
        [ProtoMember(53)]
        public string SubAggregation { get; set; }

        /// <summary> Sub page number to be displayed (either for an item or for results )</summary>
        [DataMember(EmitDefaultValue = false, Name = "subPage")]
        [XmlElement("subPage")]
        [ProtoMember(54)]
        public ushort? SubPage { get; set; }

        /// <summary> String to use for a single-item text search </summary>
        [DataMember(EmitDefaultValue = false, Name = "textSearch")]
        [XmlElement("textSearch")]
        [ProtoMember(55)]
        public string Text_Search { get; set; }

        /// <summary> Thumbnails per page to appear in the related images item viewer  </summary>
        [DataMember(EmitDefaultValue = false, Name = "thumbnailsPerPage")]
        [XmlElement("thumbnailsPerPage")]
        [ProtoMember(56)]
        public short? Thumbnails_Per_Page { get; set; }

        /// <summary> TOC Display flag, which indicates whether to display the table of contents in the item viewer </summary>
        [DataMember(EmitDefaultValue = false, Name = "tocDisplay")]
        [XmlElement("tocDisplay")]
        [ProtoMember(57)]
        public TOC_Display_Type_Enum TOC_Display { get; set; }


        /// <summary> Trace flag which indicates whether to display the trace route </summary>
        [DataMember(EmitDefaultValue = false, Name = "traceFlag")]
        [XmlElement("traceFlag")]
        [ProtoMember(58)]
        public Trace_Flag_Type_Enum Trace_Flag { get; set; }

        /// <summary> Simplified flag for displaying the trace route </summary>
        [IgnoreDataMember]
        [XmlIgnore]
        public bool Trace_Flag_Simple
        {
            get
            {
                return (Trace_Flag == Trace_Flag_Type_Enum.Explicit) || (Trace_Flag == Trace_Flag_Type_Enum.Implied);
            }
        }

        /// <summary> Volume id to display </summary>
        [DataMember(EmitDefaultValue = false, Name = "vid")]
        [XmlElement("vid")]
        [ProtoMember(59)]
        public string VID { get; set; }

        /// <summary> Viewer code which indicates which viewer to use when displaying 
        /// a single item in the item writer.  </summary>
        /// <remarks>The value returned is always lower case</remarks>
        [DataMember(EmitDefaultValue = false, Name = "viewerCode")]
        [XmlElement("viewerCode")]
        [ProtoMember(60)]
        public string ViewerCode { get; set; }

        /// <summary> Sub viewer code </summary>
        [DataMember(EmitDefaultValue = false, Name = "viewerSubCode")]
        [XmlElement("viewerSubCode")]
        [ProtoMember(65)]
        public string ViewerSubCode { get; set; }

        /// <summary> Primary key to the web content object selected </summary>
        [DataMember(EmitDefaultValue = false, Name = "webContentId")]
        [XmlElement("webContentId")]
        [ProtoMember(64)]
        public int? WebContentID { get; set; }

        /// <summary> Webcontent type of display for the current request </summary>
        [DataMember(EmitDefaultValue = false, Name = "webContentType")]
        [XmlElement("webContentType")]
        [ProtoMember(61)]
        public WebContent_Type_Enum WebContent_Type { get; set; }

        /// <summary> Writer type to be employed for rendering </summary>
        [DataMember(EmitDefaultValue = false, Name = "writerType")]
        [XmlElement("writerType")]
        [ProtoMember(62)]
        public string Writer_Type { get; set; }


        /// <summary> Remaining, unaccounted for, URL segments </summary>
        [DataMember(EmitDefaultValue = false, Name = "urlSegments")]
        [XmlElement("urlSegments")]
        [ProtoMember(77)]
        public string[] Remaining_Url_Segments { get; set; }

        #endregion

        #region Methods for XML serialization

        /// <summary> Method suppresses XML Serialization of the DateRange_Date1 property if it is NULL </summary>
        /// <returns> TRUE if the property should be serialized, otherwise FALSE </returns>
        public bool ShouldSerializeDateRange_Date1()
        {
            return DateRange_Date1 != null;
        }

        /// <summary> Method suppresses XML Serialization of the DateRange_Date2 property if it is NULL </summary>
        /// <returns> TRUE if the property should be serialized, otherwise FALSE </returns>
        public bool ShouldSerializeDateRange_Date2()
        {
            return DateRange_Date2 != null;
        }

        /// <summary> Method suppresses XML Serialization of the DateRange_Year1 property if it is NULL </summary>
        /// <returns> TRUE if the property should be serialized, otherwise FALSE </returns>
        public bool ShouldSerializeDateRange_Year1()
        {
            return DateRange_Year1 != null;
        }

        /// <summary> Method suppresses XML Serialization of the DateRange_Year2 property if it is NULL </summary>
        /// <returns> TRUE if the property should be serialized, otherwise FALSE </returns>
        public bool ShouldSerializeDateRange_Year2()
        {
            return DateRange_Year2 != null;
        }

        /// <summary> Method suppresses XML Serialization of the FolderID property if it is NULL </summary>
        /// <returns> TRUE if the property should be serialized, otherwise FALSE </returns>
        public bool ShouldSerializeFolderID()
        {
            return FolderID != null;
        }

        /// <summary> Method suppresses XML Serialization of the Invalid_Item property if it is NULL </summary>
        /// <returns> TRUE if the property should be serialized, otherwise FALSE </returns>
        public bool ShouldSerializeInvalid_Item()
        {
            return Invalid_Item != null;
        }

        /// <summary> Method suppresses XML Serialization of the ItemID_DEPRECATED property if it is NULL </summary>
        /// <returns> TRUE if the property should be serialized, otherwise FALSE </returns>
        public bool ShouldSerializeItemID_DEPRECATED()
        {
            return ItemID_DEPRECATED != null;
        }

        /// <summary> Method suppresses XML Serialization of the Page property if it is NULL </summary>
        /// <returns> TRUE if the property should be serialized, otherwise FALSE </returns>
        public bool ShouldSerializePage()
        {
            return Page != null;
        }

        /// <summary> Method suppresses XML Serialization of the Show_Selection_Panel property if it is NULL </summary>
        /// <returns> TRUE if the property should be serialized, otherwise FALSE </returns>
        public bool ShouldSerializeShow_Selection_Panel()
        {
            return Show_Selection_Panel != null;
        }

        /// <summary> Method suppresses XML Serialization of the Size_Of_Thumbnails property if it is NULL </summary>
        /// <returns> TRUE if the property should be serialized, otherwise FALSE </returns>
        public bool ShouldSerializeSize_Of_Thumbnails()
        {
            return Size_Of_Thumbnails != null;
        }

        /// <summary> Method suppresses XML Serialization of the Sort property if it is NULL </summary>
        /// <returns> TRUE if the property should be serialized, otherwise FALSE </returns>
        public bool ShouldSerializeSort()
        {
            return Sort != null;
        }

        /// <summary> Method suppresses XML Serialization of the SubPage property if it is NULL </summary>
        /// <returns> TRUE if the property should be serialized, otherwise FALSE </returns>
        public bool ShouldSerializeSubPage()
        {
            return SubPage != null;
        }

        /// <summary> Method suppresses XML Serialization of the Thumbnails_Per_Page property if it is NULL </summary>
        /// <returns> TRUE if the property should be serialized, otherwise FALSE </returns>
        public bool ShouldSerializeThumbnails_Per_Page()
        {
            return Thumbnails_Per_Page != null;
        }

        /// <summary> Method suppresses XML Serialization of the WebContentID property if it is NULL </summary>
        /// <returns> TRUE if the property should be serialized, otherwise FALSE </returns>
        public bool ShouldSerializeWebContentID()
        {
            return WebContentID != null;
        }

        #endregion

        #region Methods which return the base directory or base url with a constant ending to indicate the SobekCM standard subfolders

        ///// <summary> URL for the general image folder containing images used throughout the system, and not aggregation or item specific </summary>
        ///// <value> [Base_URL] + 'default/images/' </value>
        //public string Default_Images_URL
        //{
        //    get { return baseUrl + "default/images/"; }
        //}

        /// <summary> URL to this application's design folder </summary>
        /// <value> [Base_URL] + 'design/' </value>
        [IgnoreDataMember]
        public string Base_Design_URL
        {
            get { return Base_URL + "design/"; }
        }

        /// <summary> URL for the watermarks/icons under the design folder </summary>
        /// <value> [Base_URL] + 'design/wordmarks/' </value>
        [IgnoreDataMember]
        public string Watermarks_URL
        {
            get { return Base_URL + "design/wordmarks/"; }
        }

        #endregion

    }
}
