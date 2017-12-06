using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SobekCM.Engine_Library.Solr;
using SobekCM.Resource_Object;

namespace Solr_Test
{
    class Program
    {
        static void Main(string[] args)
        {
            SobekCM_Item testItem = new SobekCM_Item();

            Solr_Controller.Update_Index("http://localhost:8983/solr/documents2", String.Empty, testItem, false);
        }
    }
}
