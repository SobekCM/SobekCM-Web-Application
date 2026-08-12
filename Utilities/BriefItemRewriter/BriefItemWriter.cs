using ProtoBuf;
using SobekCM.Core.BriefItem;
using SobekCM.Engine_Library.Items.BriefItems;
using SobekCM.Resource_Object;
using SobekCM.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BriefItemRewriter
{
    public class BriefItemWriter
    {
        public bool WriteBriefItemProtobuf( SobekCM_Item Item, string FilePath)
        {
            try
            {
                var tracer = new Custom_Tracer();

                DateTime start = DateTime.Now;
                var brief = BriefItem_Factory.Create(Item, tracer);
                Console.WriteLine("     Created brief item in " + (DateTime.Now - start).TotalSeconds + " seconds");

                if (!File.Exists(FilePath))
                {
                    using var stream = File.Create(FilePath);
                    Serializer.Serialize(stream, brief);
                    Console.WriteLine("     Wrote protobuf version of brief item");
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public BriefItemInfo ReadBriefItemProtobuf(string FilePath)
        {
            try
            {
                using var stream = File.OpenRead(FilePath);
                return Serializer.Deserialize<BriefItemInfo>(stream);
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
