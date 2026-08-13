#region Using directives

using System;

#endregion

namespace BriefItemRewriter
{
    public class Program
    {
        static void Main(string[] Args)
        {
            var controller = new BriefItemRewriter_Controller();
            controller.Execute();

            Console.WriteLine();
            Console.WriteLine("Complete.. press any key to continue");
            Console.ReadKey();
        }
    }
}
