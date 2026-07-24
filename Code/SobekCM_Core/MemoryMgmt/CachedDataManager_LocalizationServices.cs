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
    /// single language's translation dictionary to be cached locally for reuse </summary>
    /// <remarks> Backed by <see cref="SharedCache"/>, one entry per language, with a short sliding
    /// expiration so newly-saved translations show up within a few minutes without requiring an
    /// explicit cache reset </remarks>
    public class CachedDataManager_LocalizationServices
    {
        private readonly CachedDataManager_Settings settings;

        /// <summary> Constructor for a new instance of the <see cref="CachedDataManager_LocalizationServices"/> class.  </summary>
        /// <param name="Settings"> Cached data manager settings object </param>
        public CachedDataManager_LocalizationServices(CachedDataManager_Settings Settings)
        {
            settings = Settings;
        }

        /// <summary> Clears all cached translation sets, for every language </summary>
        public void Clear()
        {
            // Get collection of keys in the Cache
            List<string> keys = (from KeyValuePair<string, object> thisItem in SharedCache.Instance select thisItem.Key).ToList();

            // Delete all items from the Cache
            foreach (string key in keys.Where(Key => Key.StartsWith("TRANSLATION|")))
            {
                SharedCache.Instance.Remove(key);
            }
        }

        /// <summary> Retrieves the entire translation set (English term -&gt; translated value) for a single language from the cache </summary>
        /// <param name="LanguageCode"> ISO code for the language to retrieve ( see <see cref="SobekCM.Core.Configuration.Localization.Web_Language_Enum_Converter.Enum_To_Code"/> ) </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
        /// <returns> Either NULL or the translation set from the cache  </returns>
        public Dictionary<string, string> Retrieve_Translation_Set(string LanguageCode, Custom_Tracer Tracer)
        {
            // If the cache is disabled, just return before even tracing
            if (settings.Disabled)
                return null;

            Tracer?.Add_Trace("CachedDataManager.Retrieve_Translation_Set", "");

            object returnValue = SharedCache.Instance.Get("TRANSLATION|" + LanguageCode.ToLower());
            return (returnValue != null) ? (Dictionary<string, string>)returnValue : null;
        }

        /// <summary> Stores the entire translation set (English term -&gt; translated value) for a single language into the cache  </summary>
        /// <param name="LanguageCode"> ISO code for the language being stored ( see <see cref="SobekCM.Core.Configuration.Localization.Web_Language_Enum_Converter.Enum_To_Code"/> ) </param>
        /// <param name="StoreObject"> Translation set to store </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
        public void Store_Translation_Set(string LanguageCode, Dictionary<string, string> StoreObject, Custom_Tracer Tracer)
        {
            // If the cache is disabled, just return before even tracing
            if (settings.Disabled)
                return;

            string key = "TRANSLATION|" + LanguageCode.ToLower();

            Tracer?.Add_Trace("CachedDataManager.Store_Translation_Set", "Adding object '" + key + "' to the cache with expiration of 7 minutes");

            if (SharedCache.Instance[key] == null)
            {
                SharedCache.Instance.Set(key, StoreObject, new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(7) });
            }
        }
    }
}
