#region Using directives

using ProtoBuf;
using ProtoBuf.Meta;
using SobekCM.Core.Aggregations;
using SobekCM.Core.Configuration;
using SobekCM.Core.Configuration.Localization;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.WebContent;
using SobekCM.Engine_Library.ApplicationState;
using SobekCM.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

#endregion

namespace SobekCM.Engine_Library.Aggregations
{
    /// <summary> Reads and writes the on-disk protobuf cache of a language-specific <see cref="Item_Aggregation"/>,
    /// sitting alongside that aggregation's other design folder content (i.e. <c>design\aggregations\[code]\</c>) </summary>
    /// <remarks> Unlike the analogous <see cref="SobekCM.Engine_Library.Items.BriefItems.BriefItem_Cache"/> for items,
    /// there is no global invalidation-date setting here -- aggregation edits only ever happen through
    /// <c>Aggregation_Single_AdminViewer</c> and the inline home-page-text editor in <c>Aggregation_HtmlSubwriter</c>,
    /// both of which purge it on save (the admin viewer via <see cref="Invalidate_With_Related"/>, since its edits can
    /// also affect parents/children; the home-text editor via <see cref="Invalidate"/>). A direct edit to files in the aggregation's
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

        /// <summary> Only language codes matching this shape are ever used to build a cache file path
        /// -- anything else (e.g. injection probes landing in the "l=" query string param, which flows
        /// into Language completely unvalidated upstream of here) falls back to "default" instead of
        /// being written straight into a filename on disk. Real language/culture codes (e.g. "en",
        /// "es", "en-us", "zh-hans") are always short and only ever contain letters, digits, and hyphens. </summary>
        private static readonly System.Text.RegularExpressions.Regex ValidLanguageCode =
            new System.Text.RegularExpressions.Regex("^[a-z0-9-]{1,16}$", System.Text.RegularExpressions.RegexOptions.Compiled);

        /// <summary> Only aggregation codes matching this shape are ever used to build a cache file/folder
        /// path -- same rationale as <see cref="ValidLanguageCode"/>. Real aggregation codes (e.g. "all",
        /// "IUF", "cbs") are always short alphanumeric identifiers with no path separators or dots, so
        /// this also blocks path traversal via AggregationCode, not just unexpected filenames. </summary>
        private static readonly System.Text.RegularExpressions.Regex ValidAggregationCode =
            new System.Text.RegularExpressions.Regex("^[a-zA-Z0-9_-]{1,64}$", System.Text.RegularExpressions.RegexOptions.Compiled);

        /// <summary> Builds the on-disk cache file path for a given aggregation code and language, or
        /// NULL if AggregationCode doesn't look like a real aggregation code -- callers must treat a
        /// NULL path as "cache unavailable for this request" rather than attempting the file operation </summary>
        private static string Cache_File_Path(string AggregationCode, string Language)
        {
            if (String.IsNullOrEmpty(AggregationCode) || !ValidAggregationCode.IsMatch(AggregationCode))
                return null;

            string safeLanguage = String.IsNullOrEmpty(Language) ? "default" : Language.ToLower();
            if (!ValidLanguageCode.IsMatch(safeLanguage))
                safeLanguage = "default";

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
            if (cacheFile == null)
                return false;

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
            if (cacheFile == null)
                return;

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

        // Invalidation generation, bumped (under invalidationLock) by every invalidation. A request that's building or
        // reading an aggregation captures it BEFORE reading anything, and only caches its result (memory or disk) if
        // it's unchanged -- see Store_If_Current. Without this, a request that read the old protobuf/XML just before
        // an invalidation could store that stale copy right after it, and a sliding-expiration memory entry that keeps
        // getting hit would then never go away.
        private static readonly object invalidationLock = new object();
        private static long generation;

        /// <summary> Current invalidation generation -- capture this before reading an aggregation from any
        /// source, and pass it to <see cref="Store_If_Current"/> when caching the result </summary>
        public static long Current_Generation => Interlocked.Read(ref generation);

        /// <summary> Runs the given cache store only if no invalidation has happened since <paramref name="Generation"/>
        /// was captured, atomically with respect to invalidation </summary>
        /// <param name="Generation"> Value of <see cref="Current_Generation"/> captured before the data was read </param>
        /// <param name="Store"> Stores the result into the memory and/or disk cache </param>
        /// <returns> TRUE if stored; FALSE if skipped because the data may predate an invalidation (the caller
        /// should still use it for its own request -- it just isn't cached) </returns>
        public static bool Store_If_Current(long Generation, Action Store)
        {
            lock (invalidationLock)
            {
                if (generation != Generation)
                    return false;

                Store();
                return true;
            }
        }

        /// <summary> Purges a single aggregation from both the memory cache and the on-disk protobuf cache </summary>
        /// <param name="AggregationCode"> Code for the aggregation </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones </param>
        /// <remarks> For changes that only affect this aggregation's own display (e.g. its home page text). Changes
        /// that parents/children/ALL also embed need <see cref="Invalidate_With_Related"/> instead. </remarks>
        public static void Invalidate(string AggregationCode, Custom_Tracer Tracer)
        {
            lock (invalidationLock)
            {
                Interlocked.Increment(ref generation);
                delete_cache_files(AggregationCode, Tracer);
                if (!String.IsNullOrEmpty(AggregationCode))
                    CachedDataManager.Aggregations.Remove_Item_Aggregation(AggregationCode, Tracer);
            }
        }

        /// <summary> Deletes every cached language variant of an <see cref="Item_Aggregation"/> for an aggregation code </summary>
        /// <param name="AggregationCode"> Code for the aggregation </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones </param>
        /// <remarks> Only call while holding invalidationLock, via one of the Invalidate methods, so the generation
        /// is bumped along with it. </remarks>
        private static void delete_cache_files(string AggregationCode, Custom_Tracer Tracer)
        {
            if (String.IsNullOrEmpty(AggregationCode) || !ValidAggregationCode.IsMatch(AggregationCode))
                return;

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
                        Tracer?.Add_Trace("Item_Aggregation_Cache.delete_cache_files", "Error deleting cache file '" + cacheFile + "': " + ee.Message);
                    }
                }
            }
            catch (Exception ee)
            {
                Tracer?.Add_Trace("Item_Aggregation_Cache.delete_cache_files", "Error deleting cache files: " + ee.Message);
            }
        }

        /// <summary> Purges an edited aggregation, plus every other aggregation that embeds information about
        /// it, from both the memory cache and the on-disk protobuf cache </summary>
        /// <param name="AggregationCode"> Code for the aggregation that was edited </param>
        /// <param name="RelatedCodes"> Codes for every parent and child of the edited aggregation -- ideally both
        /// before AND after the edit, so a parent it was just removed from is purged too </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones </param>
        /// <remarks> Every built <see cref="Item_Aggregation"/> carries a copy of its parents' and children's
        /// code, name, type, and active/hidden flags, and the ALL collection additionally lists every
        /// collection under its thematic heading. Purging only the edited aggregation leaves those related
        /// aggregations serving their stale copies -- and, since each also has its own cache_*.protobuf,
        /// even an expired memory entry just reloads the same stale data from disk. So the related ones (and
        /// ALL) lose their disk cache too. The memory cache is cleared for every aggregation, not just the
        /// related ones, since that's cheap: anything unrelated reloads straight from its intact disk cache. </remarks>
        public static void Invalidate_With_Related(string AggregationCode, IEnumerable<string> RelatedCodes, Custom_Tracer Tracer)
        {
            var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "all" };
            if (!String.IsNullOrEmpty(AggregationCode))
                codes.Add(AggregationCode);
            if (RelatedCodes != null)
            {
                foreach (string relatedCode in RelatedCodes)
                {
                    if (!String.IsNullOrEmpty(relatedCode))
                        codes.Add(relatedCode);
                }
            }

            lock (invalidationLock)
            {
                Interlocked.Increment(ref generation);

                foreach (string code in codes)
                    delete_cache_files(code, Tracer);

                CachedDataManager.Aggregations.Clear();
            }
        }

        /// <summary> Purges every aggregation from both the memory cache and the on-disk protobuf cache </summary>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones </param>
        /// <remarks> For adding or deleting an aggregation, where the set of affected parents isn't readily known
        /// (see <see cref="Invalidate_With_Related"/> for why parents/ALL go stale). These are rare admin actions,
        /// so the one-time cost of every aggregation rebuilding its disk cache on next request is acceptable. </remarks>
        public static void Invalidate_All(Custom_Tracer Tracer)
        {
            string aggregationsFolder = Engine_ApplicationCache_Gateway.Settings.Servers.Base_Design_Location + "aggregations\\";
            lock (invalidationLock)
            {
                Interlocked.Increment(ref generation);

                try
                {
                    if (Directory.Exists(aggregationsFolder))
                    {
                        foreach (string aggregationFolder in Directory.GetDirectories(aggregationsFolder))
                            delete_cache_files(Path.GetFileName(aggregationFolder), Tracer);
                    }
                }
                catch (Exception ee)
                {
                    Tracer?.Add_Trace("Item_Aggregation_Cache.Invalidate_All", "Error deleting cache files: " + ee.Message);
                }

                CachedDataManager.Aggregations.Clear();
            }
        }
    }
}
