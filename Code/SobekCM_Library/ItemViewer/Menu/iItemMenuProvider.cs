using SobekCM.Core.BriefItem;
using SobekCM.Core.Navigation;
using SobekCM.Core.Users;
using SobekCM.Tools;
using System.IO;

namespace SobekCM.Library.ItemViewer.Menu
{
    public interface iItemMenuProvider
    {
        void Add_Main_Menu(TextWriter Output, string CurrentCode, BriefItemInfo CurrentItem, RequestCache RequestSpecificValues, bool Include_Links, Custom_Tracer Tracer);

    }
}
