using System;

namespace ArchivingTool
{
    public class ArchivedFile
    {
        public long Size { get; set; }

        public string FileName { get; set; }

        public DateTime CreationDate { get; set; }

        public string Sha256Hash { get; set; }

        public ArchivedFile() { }

        public override string ToString()
        {
            return FileName + "\t" + Size + "\t" + CreationDate + "\t" + Sha256Hash;
        }
    }
}
