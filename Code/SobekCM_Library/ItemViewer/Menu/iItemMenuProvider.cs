using SobekCM.Core.BriefItem;
using SobekCM.Core.Navigation;
using SobekCM.Core.Users;
using SobekCM.Tools;
using System.IO;

namespace SobekCM.Library.ItemViewer.Menu
{
    public interface iItemMenuProvider
    {
        void Add_Main_Menu(TextWriter Output, string CurrentCode, bool ItemRestrictedFromUserByIP, bool ItemCheckedOutByOtherUser, BriefItemInfo CurrentItem, Navigation_Object CurrentMode, User_Object Currentuser, bool Include_Links, Custom_Tracer Tracer);

    }
}
