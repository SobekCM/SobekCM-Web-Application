#region Using directives

using ProtoBuf;
using ProtoBuf.Meta;
using SobekCM.Core.Aggregations;
using SobekCM.Core.Configuration;
using SobekCM.Core.Configuration.Localization;
using SobekCM.Core.WebContent;
using SobekCM.Engine_Library.ApplicationState;
using SobekCM.Tools;
using System;
using System.IO;

#endregion

namespace SobekCM.Engine_Library.Aggregations
{
    /// <summary> Reads and writes the on-disk protobuf cache of a language-specific <see cref="Item_Aggregation"/>,
    /// sitting alongside that aggregation's other design folder content (i.e. <c>design\aggregations\[code]\</c>) </summary>
    /// <remarks> Unlike the analogous <see cref="SobekCM.Engine_Library.Items.BriefItems.BriefItem_Cache"/> for items,
    /// there is no global invalidation-date setting here -- aggregation edits only ever happen through
    /// <c>Aggregation_Single_AdminViewer</c> and the inline home-page-text editor in <c>Aggregation_HtmlSubwriter</c>,
    /// both of which explicitly call <see cref="Delete_Cache"/> on save. A direct edit to files in the aggregation's
    /// design folder (bypassing both viewers) requires manually deleting the cache file(s). </remarks>
    public static class Item_Aggregation_Cache
    {
        /// <summary> Every <c>[ProtoContract]</c> type reachable from an <see cref="Item_Aggregation"/> </summary>
        private static readonly Type[] AllItemAggregationTypes = {
            typeof(Item_Aggregation), typeof(Item_Aggregation_Metadata_Type), typeof(Complete_Item_Aggregation_Metadata_Type),
            typeof(Item_Aggregation_Statistics), typeof(Item_Aggregation_Highlights), typeof(Item_Aggregation_Map_Coverage_Info),
            typeof(Item_Aggregation_Related_Aggregations), typeof(Item_Aggregation_Child_Page), typeof(ContactForm_Configuration),
            typeof(ContactForm_Configuration_Element), typeof(Item_Aggregation_Front_Banner), typeof(HTML_Based_Content),
            typeof(StringKeyValuePair), typeof(Web_Language_Translation_Lookup), typeof(Web_Language_Translation_Value)
        };

        /// <summary> Registers every <see cref="Item_Aggregation"/>-related type with protobuf-net's default
        /// model and compiles the whole model in place -- same rationale as
        /// <see cref="SobekCM.Engine_Library.Items.BriefItems.BriefItem_Cache.CompileProtobufModel"/>, registering
        /// every type explicitly rather than relying on a warm-up round trip. </summary>
        /// <remarks> Call exactly once, early during application startup. </remarks>
        public static void CompileProtobufModel()
        {
            foreach (Type aggregationType in AllItemAggregationTypes)
                RuntimeTypeModel.Default.Add(aggregationType);

            RuntimeTypeModel.Default.CompileInPlace();
        }

        /// <summary> Builds the on-disk cache file path for a given aggregation code and language </summary>
        private static string Cache_File_Path(string AggregationCode, string Language)
        {
            string safeLanguage = String.IsNullOrEmpty(Language) ? "default" : Language.ToLower();
            return Engine_ApplicationCache_Gateway.Settings.Servers.Base_Design_Location + "aggregations\\" + AggregationCode + "\\cache_" + safeLanguage + ".protobuf";
        }

        /// <summary> Attempts to read a cached <see cref="Item_Aggregation"/> for an aggregation code and language </summary>
        /// <param name="AggregationCode"> Code for the aggregation </param>
        /// <param name="Language"> Language code for this language-specific version of the aggregation </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones </param>
        /// <param name="CachedAggregation"> [OUT] The cached item aggregation, if a valid cache was found </param>
        /// <returns> TRUE if a valid cache file was found and successfully read </returns>
        public static bool TryReadCache(string AggregationCode, string Language, Custom_Tracer Tracer, out Item_Aggregation CachedAggregation)
        {
            CachedAggregation = null;
            string cacheFile = Cache_File_Path(AggregationCode, Language);

            try
            {
                if (!File.Exists(cacheFile))
                    return false;

                using (var stream = File.OpenRead(cacheFile))
                {
                    CachedAggregation = Serializer.Deserialize<Item_Aggregation>(stream);
                }

                Tracer?.Add_Trace("Item_Aggregation_Cache.TryReadCache", "Loaded item aggregation from cache_" + Language + ".protobuf");
                return CachedAggregation != null;
            }
            catch (Exception ee)
            {
                // Corrupt, partial, or locked cache file -- treat as a miss, not a hard failure.  The
                // caller will rebuild from the XML config / design folder and this method's write
                // counterpart will overwrite it.
                Tracer?.Add_Trace("Item_Aggregation_Cache.TryReadCache", "Error reading cache file: " + ee.Message);
                CachedAggregation = null;
                return false;
            }
        }

        /// <summary> Writes (or overwrites) the cached <see cref="Item_Aggregation"/> for an aggregation code and language </summary>
        /// <param name="AggregationCode"> Code for the aggregation </param>
        /// <param name="Language"> Language code for this language-specific version of the aggregation </param>
        /// <param name="Item"> Freshly built item aggregation to cache </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones </param>
        public static void WriteCache(string AggregationCode, string Language, Item_Aggregation Item, Custom_Tracer Tracer)
        {
            string cacheFile = Cache_File_Path(AggregationCode, Language);

            try
            {
                string directory = Path.GetDirectoryName(cacheFile);
                if ((!String.IsNullOrEmpty(directory)) && (!Directory.Exists(directory)))
                    Directory.CreateDirectory(directory);

                using (var stream = File.Create(cacheFile))
                {
                    Serializer.Serialize(stream, Item);
                }

                Tracer?.Add_Trace("Item_Aggregation_Cache.WriteCache", "Wrote item aggregation to cache_" + Language + ".protobuf");
            }
            catch (Exception ee)
            {
                // Best-effort -- same "not critical, self-corrects" philosophy as BriefItem_Cache.WriteCache.
                Tracer?.Add_Trace("Item_Aggregation_Cache.WriteCache", "Error writing cache file (non-critical): " + ee.Message);
            }
        }

        /// <summary> Deletes every cached language variant of an <see cref="Item_Aggregation"/> for an aggregation code </summary>
        /// <param name="AggregationCode"> Code for the aggregation </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones </param>
        public static void Delete_Cache(string AggregationCode, Custom_Tracer Tracer)
        {
            string aggregationFolder = Engine_ApplicationCache_Gateway.Settings.Servers.Base_Design_Location + "aggregations\\" + AggregationCode + "\\";

            try
            {
                if (!Directory.Exists(aggregationFolder))
                    return;

                foreach (string cacheFile in Directory.GetFiles(aggregationFolder, "cache_*.protobuf"))
                {
                    try
                    {
                        File.Delete(cacheFile);
                    }
                    catch (Exception ee)
                    {
                        // Non-critical, self-corrects on next save or manual cleanup
                        Tracer?.Add_Trace("Item_Aggregation_Cache.Delete_Cache", "Error deleting cache file '" + cacheFile + "': " + ee.Message);
                    }
                }
            }
            catch (Exception ee)
            {
                Tracer?.Add_Trace("Item_Aggregation_Cache.Delete_Cache", "Error deleting cache files: " + ee.Message);
            }
        }
    }
}
