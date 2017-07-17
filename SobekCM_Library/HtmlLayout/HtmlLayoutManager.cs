using System;
using System.Collections.Generic;
using System.IO;
using SobekCM.Core.UI_Configuration.Viewers;
using SobekCM.Library.UI;

namespace SobekCM.Library.HtmlLayout
{
    /// <summary> Static HTML layout manager keeps all the layouts updated and allow
    /// them to be created only once and repeatedly used </summary>
    public static class HtmlLayoutManager
    {
        private static Dictionary<string, HtmlLayoutInfo> itemLayout;
        private static readonly Object itemLayoutLock = new Object();

        /// <summary> Static constructor for the <see cref="HtmlLayoutManager"/> class </summary>
        static HtmlLayoutManager()
        {
            itemLayout = new Dictionary<string, HtmlLayoutInfo>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary> Get the indicated item layour objecty </summary>
        /// <param name="LayoutConfig"> Configuration for this layout </param>
        /// <returns> Either the indicated layout, or the default, if the indicated one does not exist </returns>
        public static HtmlLayoutInfo GetItemLayout(ItemWriterLayoutConfig LayoutConfig)
        {
            lock (itemLayoutLock)
            {
                // Does this already exist?
                if (itemLayout.ContainsKey(LayoutConfig.ID))
                {
                    return itemLayout[LayoutConfig.ID];
                }

                // Get the lay

                // Did not exist, so parse it
                string source_file_name = LayoutConfig.Source;
                string source_file = Path.Combine(UI_ApplicationCache_Gateway.Settings.Servers.Application_Server_Network, "config", "html", source_file_name);

                // If the source file is missing, make an error template
                if (!File.Exists(source_file))
                {
                    // Just create a dummy here
                    HtmlLayoutInfo thisItemLayout = new HtmlLayoutInfo();
                    HtmlLayoutSection errorSection = new HtmlLayoutSection
                    {
                        Name = "Error", 
                        Type = HtmlLayoutSectionTypeEnum.Static_HTML, 
                        HTML = "Unable to find the HTML source template file under the web application ( config/html/" + source_file_name + " )"
                    };
                    thisItemLayout.Sections.Add(errorSection);
                    return thisItemLayout;
                }

                // Use the HTML layout parser to read this
                HtmlLayoutInfo readItemLayout = HtmlLayoutParser.Parse(source_file);

                // Save this
                itemLayout[LayoutConfig.ID] = readItemLayout;

                // Also return this
                return readItemLayout;
            }
        }

        /// <summary> Clear all the cached layout information </summary>
        public static void Clear()
        {
            itemLayout.Clear();
        }
    }
}
