using Microsoft.Extensions.Caching.Memory;
using SobekCM.Core.MemoryMgmt;
using System.Linq;

namespace SobekCM.Library.UI
{
    /// <summary> Application-wide string/object cache, replacing System.Web.HttpApplicationState </summary>
    /// <remarks> Backed by the same <see cref="SharedCache"/> instance used by <see cref="CachedDataManager"/> —
    /// entries are stored with <see cref="CacheItemPriority.NeverRemove"/> and no expiration, so they behave
    /// like the old unexpiring ConcurrentDictionary-based store even though they now share physical space with
    /// CachedDataManager's expiring entries. </remarks>
    public static class SobekCM_Application
    {
        public static readonly ApplicationState State = new ApplicationState();

        public class ApplicationState
        {
            private static readonly MemoryCacheEntryOptions PERMANENT = new MemoryCacheEntryOptions { Priority = CacheItemPriority.NeverRemove };

            public object this[string key]
            {
                get => SharedCache.Instance[key];
                set
                {
                    if (value == null) SharedCache.Instance.Remove(key);
                    else SharedCache.Instance.Set(key, value, PERMANENT);
                }
            }

            /// <summary> All keys currently held in the shared cache </summary>
            /// <remarks> Since application state now shares its store with <see cref="CachedDataManager"/>,
            /// this includes both permanent application-state keys and CachedDataManager's expiring entries. </remarks>
            public string[] AllKeys => SharedCache.Instance.Select(kvp => kvp.Key).ToArray();

            public void RemoveAll() => SharedCache.Instance.Clear();
        }
    }
}
