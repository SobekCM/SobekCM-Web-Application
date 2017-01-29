using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
