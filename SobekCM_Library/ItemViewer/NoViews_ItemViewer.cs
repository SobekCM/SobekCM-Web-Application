using SobekCM.Library.ItemViewer.Viewers;
using SobekCM.Tools;
using System.IO;

namespace SobekCM.Library.ItemViewer
{
    /// <summary> A special item viewer that is used when an item has NO VIEWS attached to it </summary>
    /// <remarks> This class extends the abstract class <see cref="abstractNoPaginationItemViewer"/> and implements the
    /// <see cref="iItemViewer" /> interface. </remarks>
    public class NoViews_ItemViewer : abstractNoPaginationItemViewer
    {
        /// <summary> Write the item viewer main section as HTML directly to the HTTP output stream </summary>
        /// <param name="Output"> Response stream for the item viewer to write directly to </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        public override void Write_Main_Viewer_Section(TextWriter Output, Custom_Tracer Tracer)
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("NoViews_ItemViewer.Write_Main_Viewer_Section", "");
            }

            Output.WriteLine("\t\t<td align=\"center\" id=\"sbkNviv_Image\">");
            Output.WriteLine("\t\t\tThis item has no views associated with it");
            Output.WriteLine("\t\t</td>");
        }
    }
}
