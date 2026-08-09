#region Using directives

using SobekCM.Builder_Library;
using SobekCM.Engine_Library.ApplicationState;
using Solr_Reindexer;
using System;
using System.Text;

#endregion

namespace SobekCM.Builder
{
    public class Program
    {
 
        static void Main(string[] Args)
        {
            // Controller always runs in background mode
            var controller = new Solr_Reindexer_Controller(true );
            controller.Execute();

#if DEBUG
            Console.WriteLine();
            Console.WriteLine("Detected DEBUG mode.. press any key to continue");
            Console.ReadKey();
#endif
        }
    }
}
