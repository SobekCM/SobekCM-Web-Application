using System.Collections.Generic;

namespace SobekCM.Library.HtmlLayout
{
    public class HtmlLayoutInfo
    {

        public List<HtmlLayoutSection> Sections { get; set; }

        public HtmlLayoutInfo()
        {
            Sections = new List<HtmlLayoutSection>();
        }

    }
}
