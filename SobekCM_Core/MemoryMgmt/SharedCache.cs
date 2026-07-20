#region Using directives

using Microsoft.Extensions.Caching.Memory;
using System.Collections;
using System.Collections.Generic;

#endregion

namespace SobekCM.Core.MemoryMgmt
{
    /// <summary> Single in-process cache shared by <see cref="CachedDataManager"/> and application-wide
    /// state that previously lived in a separate store. </summary>
    /// <remarks> Backed by <see cref="Microsoft.Extensions.Caching.Memory.MemoryCache"/>, but exposes the
    /// same shape as the old <c>System.Runtime.Caching.MemoryCache.Default</c> singleton (indexer, <see cref="Get"/>,
    /// <see cref="Set"/>, <see cref="Remove"/>, and enumeration as key/value pairs) so the many existing call
    /// sites — including LINQ queries written against it — needed only a name swap, not a rewrite. </remarks>
    public sealed class SharedCache : IEnumerable<KeyValuePair<string, object>>
    {
        /// <summary> The single shared cache instance </summary>
        public static readonly SharedCache Instance = new SharedCache();

        private readonly MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

        private SharedCache() { }

        /// <summary> Retrieves an object from the cache by key, or NULL if not present </summary>
        public object this[string key] => _cache.TryGetValue(key, out object value) ? value : null;

        /// <summary> Retrieves an object from the cache by key, or NULL if not present </summary>
        public object Get(string key) => this[key];

        /// <summary> Stores an object in the cache under the given key, with the given expiration/priority options </summary>
        public void Set(string key, object value, MemoryCacheEntryOptions options) => _cache.Set(key, value, options);

        /// <summary> Removes a single object from the cache, if present </summary>
        public void Remove(string key) => _cache.Remove(key);

        /// <summary> Removes every object currently in the cache </summary>
        public void Clear()
        {
            foreach (object keyObj in _cache.Keys)
            {
                _cache.Remove((string)keyObj);
            }
        }

        /// <summary> Enumerates all key/value pairs currently held in the cache </summary>
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            foreach (object keyObj in _cache.Keys)
            {
                string key = (string)keyObj;
                if (_cache.TryGetValue(key, out object value))
                    yield return new KeyValuePair<string, object>(key, value);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
