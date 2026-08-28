using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchivingTool
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var archiver = new Archiver(@"\\sobek-frontend\e$\uoc", @"\\sobek-frontend\sobek-coldline-archive\UOC");
        }
    }
}
