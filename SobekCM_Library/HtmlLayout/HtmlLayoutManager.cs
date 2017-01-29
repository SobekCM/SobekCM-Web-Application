using System;
using System.IO;
using SobekCM.Library.UI;

namespace SobekCM.Library.HtmlLayout
{
    /// <summary> Static HTML layout manager keeps all the layouts updated and allow
    /// them to be created only once and repeatedly used </summary>
    public static class HtmlLayoutManager
    {
        private static HtmlLayoutInfo itemLayout;
        private static readonly Object itemLayoutLock = new Object();

        /// <summary> Get the indicated item layour objecty </summary>
        /// <param name="LayoutName"> Name of the layout to get </param>
        /// <returns> Either the indicated layout, or the default, if the indicated one does not exist </returns>
        public static HtmlLayoutInfo GetItemLayout(string LayoutName)
        {
            lock (itemLayoutLock)
            {
                // Does this already exist?
                if (itemLayout != null)
                {
                    return itemLayout;
                }

                // Did not exist, so parse it
                string source_file_name = UI_ApplicationCache_Gateway.Configuration.UI.WriterViewers.Items.Layout.Source;
                string source_file = Path.Combine(UI_ApplicationCache_Gateway.Settings.Servers.Application_Server_Network, "config", "html", source_file_name);

                // If the source file is missing, make an error template
                if (!File.Exists(source_file))
                {
                    // Just create a dummy here
                    itemLayout = new HtmlLayoutInfo();
                    HtmlLayoutSection errorSection = new HtmlLayoutSection
                    {
                        Name = "Error", 
                        Type = HtmlLayoutSectionTypeEnum.Static_HTML, 
                        HTML = "Unable to find the HTML source template file under the web application ( config/html/" + source_file_name + " )"
                    };
                    itemLayout.Sections.Add(errorSection);
                    return itemLayout;
                }

                // Use the HTML layout parser to read this
                itemLayout = HtmlLayoutParser.Parse(source_file);
                return itemLayout;
            }
        }

        /// <summary> Clear all the cached layout information </summary>
        public static void Clear()
        {
            itemLayout = null;
        }
    }
}
