#region Using directives

using ProtoBuf;
using SobekCM.Core.Aggregations;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Engine_Library.ApplicationState;
using SobekCM.Tools;
using System;
using System.IO;
using System.Threading;

#endregion

namespace SobekCM.Engine_Library.Aggregations
{
    /// <summary> Reads and writes the on-disk protobuf cache of an aggregation's <see cref="Item_Aggregation_Statistics"/>
    /// (item/title/page counts), sitting alongside that aggregation's other design folder content
    /// (i.e. <c>design\aggregations\[code]\</c>) </summary>
    /// <remarks> Deliberately independent of <see cref="Item_Aggregation_Cache"/> -- these counts are pulled
    /// fresh from the database (via <see cref="SobekCM.Engine_Library.Database.Engine_Database.Get_Item_Aggregation_Statistics"/>),
    /// not built alongside the rest of the aggregation, and have their own invalidation triggers (a new item
    /// being added to a collection, or an item's collection membership being edited -- see
    /// <c>New_Group_And_Item_MySobekViewer</c> and <c>Edit_Item_Behaviors_MySobekViewer</c>) plus a one-hour
    /// hard expiration, rather than being invalidated only when the aggregation itself is edited. </remarks>
    public static class Item_Aggregation_Statistics_Cache
    {
        /// <summary> Every cache entry (memory or disk) older than this is treated as a miss and rebuilt from
        /// the database, regardless of whether anything explicitly invalidated it </summary>
        public static readonly TimeSpan Max_Age = TimeSpan.FromHours(1);

        /// <summary> Only aggregation codes matching this shape are ever used to build a cache file/folder
        /// path -- real aggregation codes (e.g. "all", "IUF", "cbs") are always short alphanumeric identifiers
        /// with no path separators or dots, so this also blocks path traversal via AggregationCode. </summary>
        private static readonly System.Text.RegularExpressions.Regex ValidAggregationCode =
            new System.Text.RegularExpressions.Regex("^[a-zA-Z0-9_-]{1,64}$", System.Text.RegularExpressions.RegexOptions.Compiled);

        /// <summary> Builds the on-disk cache file path for a given aggregation code, or NULL if AggregationCode
        /// doesn't look like a real aggregation code -- callers must treat a NULL path as "cache unavailable
        /// for this request" rather than attempting the file operation </summary>
        private static string Cache_File_Path(string AggregationCode)
        {
            if (String.IsNullOrEmpty(AggregationCode) || !ValidAggregationCode.IsMatch(AggregationCode))
                return null;

            return Engine_ApplicationCache_Gateway.Settings.Servers.Base_Design_Location + "aggregations\\" + AggregationCode + "\\stats.protobuf";
        }

        /// <summary> Attempts to read a cached <see cref="Item_Aggregation_Statistics"/> for an aggregation code </summary>
        /// <param name="AggregationCode"> Code for the aggregation </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones </param>
        /// <param name="CachedStatistics"> [OUT] The cached statistics, if a valid, non-expired cache was found </param>
        /// <returns> TRUE if a valid, non-expired cache file was found and successfully read </returns>
        public static bool TryReadCache(string AggregationCode, Custom_Tracer Tracer, out Item_Aggregation_Statistics CachedStatistics)
        {
            CachedStatistics = null;
            string cacheFile = Cache_File_Path(AggregationCode);
            if (cacheFile == null)
                return false;

            try
            {
                if (!File.Exists(cacheFile))
                    return false;

                // Older than the max age -- delete it outright (rather than just ignoring it) so the stale
                // bytes don't linger, then fall through to a database rebuild
                if (DateTime.UtcNow - File.GetLastWriteTimeUtc(cacheFile) > Max_Age)
                {
                    Tracer?.Add_Trace("Item_Aggregation_Statistics_Cache.TryReadCache", "Cache file older than " + Max_Age.TotalHours + " hour(s); deleting and treating as a miss");
                    File.Delete(cacheFile);
                    return false;
                }

                using (var stream = File.OpenRead(cacheFile))
                {
                    CachedStatistics = Serializer.Deserialize<Item_Aggregation_Statistics>(stream);
                }

                Tracer?.Add_Trace("Item_Aggregation_Statistics_Cache.TryReadCache", "Loaded item aggregation statistics from stats.protobuf");
                return CachedStatistics != null;
            }
            catch (Exception ee)
            {
                // Corrupt, partial, or locked cache file -- treat as a miss, not a hard failure.  The
                // caller will rebuild from the database and this method's write counterpart will overwrite it.
                Tracer?.Add_Trace("Item_Aggregation_Statistics_Cache.TryReadCache", "Error reading cache file: " + ee.Message);
                CachedStatistics = null;
                return false;
            }
        }

        /// <summary> Writes (or overwrites) the cached <see cref="Item_Aggregation_Statistics"/> for an aggregation code </summary>
        /// <param name="AggregationCode"> Code for the aggregation </param>
        /// <param name="Statistics"> Freshly built statistics to cache </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones </param>
        public static void WriteCache(string AggregationCode, Item_Aggregation_Statistics Statistics, Custom_Tracer Tracer)
        {
            string cacheFile = Cache_File_Path(AggregationCode);
            if (cacheFile == null)
                return;

            try
            {
                string directory = Path.GetDirectoryName(cacheFile);
                if ((!String.IsNullOrEmpty(directory)) && (!Directory.Exists(directory)))
                    Directory.CreateDirectory(directory);

                using (var stream = File.Create(cacheFile))
                {
                    Serializer.Serialize(stream, Statistics);
                }

                Tracer?.Add_Trace("Item_Aggregation_Statistics_Cache.WriteCache", "Wrote item aggregation statistics to stats.protobuf");
            }
            catch (Exception ee)
            {
                // Best-effort -- same "not critical, self-corrects" philosophy as Item_Aggregation_Cache.WriteCache.
                Tracer?.Add_Trace("Item_Aggregation_Statistics_Cache.WriteCache", "Error writing cache file (non-critical): " + ee.Message);
            }
        }

        // Invalidation generation -- same race-safety rationale as Item_Aggregation_Cache.Current_Generation /
        // Store_If_Current, but tracked completely independently since this cache invalidates on different
        // triggers (item added / item's collection membership edited) than the aggregation object cache does.
        private static readonly object invalidationLock = new object();
        private static long generation;

        /// <summary> Current invalidation generation -- capture this before reading statistics from any
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

        /// <summary> Purges a single aggregation's statistics from both the memory cache and the on-disk
        /// protobuf cache </summary>
        /// <param name="AggregationCode"> Code for the aggregation </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones </param>
        /// <remarks> Call whenever an item is added to, or removed from, this aggregation -- see
        /// <c>New_Group_And_Item_MySobekViewer.complete_item_submission</c> and
        /// <c>Edit_Item_Behaviors_MySobekViewer</c>'s "save" postback. </remarks>
        public static void Invalidate(string AggregationCode, Custom_Tracer Tracer)
        {
            lock (invalidationLock)
            {
                string cacheFile = Cache_File_Path(AggregationCode);
                if (cacheFile != null)
                {
                    try
                    {
                        if (File.Exists(cacheFile))
                            File.Delete(cacheFile);
                    }
                    catch (Exception ee)
                    {
                        // Non-critical, self-corrects on next save or manual cleanup
                        Tracer?.Add_Trace("Item_Aggregation_Statistics_Cache.Invalidate", "Error deleting cache file '" + cacheFile + "': " + ee.Message);
                    }
                }

                if (!String.IsNullOrEmpty(AggregationCode))
                    CachedDataManager.Aggregations.Remove_Item_Aggregation_Statistics(AggregationCode, Tracer);

                // Bumped LAST, once the purge is complete -- see Item_Aggregation_Cache.Invalidate for the full
                // rationale (a request that captured the old generation at any point during the purge can't
                // store; one that captures the new value can only ever read post-purge data)
                Interlocked.Increment(ref generation);
            }
        }
    }
}
