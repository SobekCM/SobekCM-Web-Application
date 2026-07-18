#region Using directives

using System.Collections.Generic;
using System.Web;
using SobekCM.Core.Aggregations;
using SobekCM.Core.Items;
using SobekCM.Core.Navigation;
using SobekCM.Core.Results;
using SobekCM.Core.SiteMap;
using SobekCM.Core.Skins;
using SobekCM.Core.Users;
using SobekCM.Core.WebContent;
using SobekCM.Resource_Object;
using SobekCM.Resource_Object.Divisions;
using SobekCM.Tools;

#endregion

namespace SobekCM.Library
{
    /// <summary> Class holds all the information about one specific request and the non-global data that is specifically
    /// needed to resolve the request. </summary>
    public class RequestCache
    {
        public RequestCache(HttpContext Context)
        {
            this.Context = Context;
            Flags = new RequestCache_RequestFlags();
        }

        ///// <summary> Constructor for a new instance of the RequestCache class </summary>
        ///// <param name="Current_Mode"> Mode / navigation information for the current request</param>
        ///// <param name="Results_Statistics"> Information about the entire set of results for a search or browse </param>
        ///// <param name="Paged_Results"> Single page of results for a search or browse, within the entire set </param>
        ///// <param name="HTML_Skin"> HTML Web skin which controls the overall appearance of this digital library </param>
        ///// <param name="Current_User"> Currently logged on user </param>
        ///// <param name="Public_Folder"> Object contains the information about the public folder to display </param>
        ///// <param name="Top_Collection"> Item aggregation for the top-level collection, which is used in a number of places, for example 
        ///// showing the correct banner, even when it is not the "current" aggregation </param>
        ///// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        //public RequestCache(Navigation_Object Current_Mode,
        //    Search_Results_Statistics Results_Statistics,
        //    List<iSearch_Title_Result> Paged_Results,
        //    Web_Skin_Object HTML_Skin,
        //    User_Object Current_User,
        //    Public_User_Folder Public_Folder,
        //    Item_Aggregation Top_Collection,
        //    Custom_Tracer Tracer)
        //{
        //    this.Current_Mode = Current_Mode;
        //    this.Results_Statistics = Results_Statistics;
        //    this.Paged_Results = Paged_Results;
        //    this.HTML_Skin = HTML_Skin;
        //    this.Current_User = Current_User;
        //    this.Public_Folder = Public_Folder;
        //    this.Top_Collection = Top_Collection;
        //    this.Tracer = Tracer;

        //    Flags = new RequestCache_RequestFlags();
        //}

        ///// <summary> Constructor for a new instance of the RequestCache class </summary>
        ///// <param name="Current_Mode"> Mode / navigation information for the current request</param>
        ///// <param name="Results_Statistics"> Information about the entire set of results for a search or browse </param>
        ///// <param name="Paged_Results"> Single page of results for a search or browse, within the entire set </param>
        ///// <param name="Current_User"> Currently logged on user </param>
        ///// <param name="Public_Folder"> Object contains the information about the public folder to display </param>
        ///// <param name="Top_Collection"> Item aggregation for the top-level collection, which is used in a number of places, for example 
        ///// showing the correct banner, even when it is not the "current" aggregation </param>
        ///// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        //public RequestCache(Navigation_Object Current_Mode,
        //    Search_Results_Statistics Results_Statistics,
        //    List<iSearch_Title_Result> Paged_Results,
        //    User_Object Current_User,
        //    Public_User_Folder Public_Folder,
        //    Item_Aggregation Top_Collection,
        //    Custom_Tracer Tracer)
        //{
        //    this.Current_Mode = Current_Mode;
        //    this.Results_Statistics = Results_Statistics;
        //    this.Paged_Results = Paged_Results;
        //    this.Current_User = Current_User;
        //    this.Public_Folder = Public_Folder;
        //    this.Top_Collection = Top_Collection;
        //    this.Tracer = Tracer;

        //    Flags = new RequestCache_RequestFlags();
        //}

        public readonly HttpContext Context;

        /// <summary> Mode / navigation information for the current request </summary>
        public Navigation_Object Current_Mode { get; set; }

        /// <summary> Information about the entire set of results for a search or browse </summary>
        public Search_Results_Statistics Results_Statistics { get; set; }

        /// <summary> Single page of results for a search or browse, within the entire set </summary>
        public List<iSearch_Title_Result> Paged_Results { get; set; }

        /// <summary> HTML Web skin which controls the overall appearance of this digital library </summary>
        public Web_Skin_Object HTML_Skin { get; set; }

        /// <summary> Currently logged on user </summary>
        public User_Object Current_User { get; set; }

        /// <summary> Object contains the information about the public folder to display </summary>
        public Public_User_Folder Public_Folder { get; set; }

        /// <summary> Item aggregation for the top-level collection, which is used in a number of places, for example 
        /// showing the correct banner, even when it is not the "current" aggregation </summary>
        public Item_Aggregation Top_Collection { get; set; }

        /// <summary>  Trace object keeps a list of each method executed and important milestones in rendering  </summary>
        public Custom_Tracer Tracer { get; set; }

        /// <summary> Flags for this individual execution, used for cross-class communication
        /// and to stop the saem sort of calculation from being completed multiple times 
        /// within a single request  </summary>
        public RequestCache_RequestFlags Flags { get; private set; }

        public Dictionary<string, string> QueryString { get; set; }

        public string Page_Name { get; set; } = string.Empty;           
    }

    /// <summary> Flags for this individual execution, used for cross-class communication
    /// and to stop the saem sort of calculation from being completed multiple times 
    /// within a single request </summary>
    public class RequestCache_RequestFlags
    {
        /// <summary> Constructor for a new instance of the RequestCache_RequestFlags class </summary>
        public RequestCache_RequestFlags()
        {
            ItemCheckedOutByOtherUser = false;
            ItemCheckedOutByOtherUser = false;
        }

        /// <summary> Flag indicates if the current item is restricted from the
        /// current user by IP address </summary>
        public bool ItemRestrictedFromUser { get; set; }

        /// <summary> Flag indicates if the current item is checked out by 
        /// another user and is a single-use item </summary>
        public bool ItemCheckedOutByOtherUser { get; set; }

        /// <summary> Restriction message to display </summary>
        public string RestrictionMessage { get; set; }
    }
}
