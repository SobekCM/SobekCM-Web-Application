#region Using directives

using Microsoft.Extensions.Caching.Memory;
using SobekCM.Tools;
using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace SobekCM.Core.MemoryMgmt
{
    /// <summary> Localization/translation-related services for the Cached Data Manager, which allows a
    /// single language's translation dictionary -- or a single language's standalone HTML help/FAQ
    /// fragment -- to be cached locally for reuse </summary>
    /// <remarks> Backed by <see cref="SharedCache"/>, one entry per language ( plus fragment name, for
    /// HTML fragments ), with a short sliding expiration so newly-saved translations/edited fragment
    /// files show up within a few minutes without requiring an explicit cache reset </remarks>
    public class CachedDataManager_LocalizationServices
    {
        private readonly CachedDataManager_Settings settings;

        /// <summary> Constructor for a new instance of the <see cref="CachedDataManager_LocalizationServices"/> class.  </summary>
        /// <param name="Settings"> Cached data manager settings object </param>
        public CachedDataManager_LocalizationServices(CachedDataManager_Settings Settings)
        {
            settings = Settings;
        }

        /// <summary> Clears all cached translation sets and HTML fragments, for every language </summary>
        public void Clear()
        {
            // Get collection of keys in the Cache
            List<string> keys = (from KeyValuePair<string, object> thisItem in SharedCache.Instance select thisItem.Key).ToList();

            // Delete all items from the Cache
            foreach (string key in keys.Where(Key => Key.StartsWith("TRANSLATION|") || Key.StartsWith("HTMLFRAGMENT|")))
            {
                SharedCache.Instance.Remove(key);
            }
        }

        /// <summary> Retrieves a single standalone HTML help/FAQ fragment, in a single language, from the cache </summary>
        /// <param name="FragmentName"> Base file name for this fragment ( e.g. "quick_tips" ), matching the
        /// file name under design/extra/aggregations/ without the language suffix or extension </param>
        /// <param name="LanguageCode"> ISO code for the language to retrieve </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
        /// <returns> Either NULL or the fragment's HTML from the cache  </returns>
        public string Retrieve_Html_Fragment(string FragmentName, string LanguageCode, Custom_Tracer Tracer)
        {
            // If the cache is disabled, just return before even tracing
            if (settings.Disabled)
                return null;

            Tracer?.Add_Trace("CachedDataManager.Retrieve_Html_Fragment", "");

            return (string)SharedCache.Instance.Get("HTMLFRAGMENT|" + FragmentName.ToLower() + "|" + LanguageCode.ToLower());
        }

        /// <summary> Stores a single standalone HTML help/FAQ fragment, in a single language, into the cache  </summary>
        /// <param name="FragmentName"> Base file name for this fragment ( e.g. "quick_tips" ), matching the
        /// file name under design/extra/aggregations/ without the language suffix or extension </param>
        /// <param name="LanguageCode"> ISO code for the language being stored </param>
        /// <param name="StoreObject"> Fragment HTML to store ( may be an empty string, if the file could not be found -- this is
        /// still cached, so a missing file isn't re-probed on every request within the expiration window ) </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
        public void Store_Html_Fragment(string FragmentName, string LanguageCode, string StoreObject, Custom_Tracer Tracer)
        {
            // If the cache is disabled, just return before even tracing
            if (settings.Disabled)
                return;

            string key = "HTMLFRAGMENT|" + FragmentName.ToLower() + "|" + LanguageCode.ToLower();

            Tracer?.Add_Trace("CachedDataManager.Store_Html_Fragment", "Adding object '" + key + "' to the cache with expiration of 7 minutes");

            if (SharedCache.Instance[key] == null)
            {
                SharedCache.Instance.Set(key, StoreObject, new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(7) });
            }
        }
    }
}
