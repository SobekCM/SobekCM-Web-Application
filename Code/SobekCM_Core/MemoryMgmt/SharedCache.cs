#region Using directives

using Microsoft.Extensions.Caching.Memory;
using System;
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

        // Guards GetOrAdd's compound check-then-create against itself -- see GetOrAdd's remarks for why
        // this is needed despite MemoryCache's individual Get/Set/Remove calls already being thread-safe
        // on their own.
        private readonly object getOrAddLock = new object();

        private SharedCache() { }

        /// <summary> Retrieves an object from the cache by key, or NULL if not present </summary>
        public object this[string key] => _cache.TryGetValue(key, out object value) ? value : null;

        /// <summary> Retrieves an object from the cache by key, or NULL if not present </summary>
        public object Get(string key) => this[key];

        /// <summary> Stores an object in the cache under the given key, with the given expiration/priority options </summary>
        public void Set(string key, object value, MemoryCacheEntryOptions options) => _cache.Set(key, value, options);

        /// <summary> Retrieves an existing entry, or creates it via the given factory if absent -- genuinely
        /// atomically, under an explicit lock. </summary>
        /// <remarks> The obvious implementation, <c>_cache.GetOrCreate(key, factory)</c>, looks atomic but
        /// isn't: that extension method is just TryGetValue → (on miss) CreateEntry → factory(entry) →
        /// entry.Dispose(), with no synchronization anywhere in the sequence. Two concurrent misses on the
        /// same key each build their own entry from the factory, and whichever one disposes last silently
        /// overwrites the other in the cache -- so two callers racing to create the same missing key can
        /// each get back a *different* object, only one of which survives. For a shared mutable entry (e.g.
        /// a per-IP request counter in RateLimiting_Gateway, where the whole point is one Counter instance
        /// that every concurrent request increments), that means increments applied to the losing instance
        /// are silently lost. The explicit lock here closes that race properly; it serializes GetOrAdd
        /// calls against each other (not against Get/Set/Remove, which MemoryCache already makes safe to
        /// call concurrently on their own) -- fine given GetOrAdd is only ever called on a cache miss, not
        /// on every request. </remarks>
        public object GetOrAdd(string key, Func<ICacheEntry, object> factory)
        {
            lock (getOrAddLock)
            {
                if (_cache.TryGetValue(key, out object existing))
                    return existing;

                using ICacheEntry entry = _cache.CreateEntry(key);
                object created = factory(entry);
                entry.Value = created;
                return created;
            }
        }

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
