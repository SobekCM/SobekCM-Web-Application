using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using SobekCM.Core.MemoryMgmt;

namespace SobekCM.Library.UI
{
    /// <summary>
    /// In-memory replacement for HttpSessionState object storage.
    /// ASP.NET Core ISession only serializes byte arrays/strings, so complex objects are kept
    /// here instead, in the same <see cref="SharedCache"/> used by CachedDataManager and
    /// SobekCM_Application. Entries use a sliding expiration — <see cref="IdleTimeout"/> after
    /// last access — so abandoned sessions no longer accumulate forever.
    /// Usage: Context.SessionObject()["key"] — mirrors the old HttpContext.Current.Session["key"] pattern.
    /// </summary>
    public static class SessionObjectStore
    {
        /// <summary> How long an entry survives without being re-accessed. Set once at startup from
        /// the same Session:IdleTimeoutMinutes config value ISession itself uses, so the two expire together. </summary>
        public static TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMinutes(90);

        private const string KEY_PREFIX = "SESSION|";

        /// <summary> Returns the object bag for the current session </summary>
        public static SessionObjectBag SessionObject(this HttpContext context)
            => new SessionObjectBag(context.Session.Id);

        /// <summary> Removes all object entries for a session (call on logout/session end) </summary>
        public static void ClearSession(string sessionId)
        {
            string prefix = KEY_PREFIX + sessionId + "|";

            List<string> keysToRemove = SharedCache.Instance
                .Where(kvp => kvp.Key.StartsWith(prefix, StringComparison.Ordinal))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (string key in keysToRemove)
                SharedCache.Instance.Remove(key);
        }

        internal static string Compose_Key(string sessionId, string key) => KEY_PREFIX + sessionId + "|" + key;
    }

    /// <summary> Provides indexer-based access to the in-memory per-session object store </summary>
    public class SessionObjectBag
    {
        private readonly string _sessionId;

        internal SessionObjectBag(string sessionId)
        {
            _sessionId = sessionId;
        }

        public object this[string key]
        {
            get => SharedCache.Instance[SessionObjectStore.Compose_Key(_sessionId, key)];
            set
            {
                string fullKey = SessionObjectStore.Compose_Key(_sessionId, key);
                if (value == null)
                    SharedCache.Instance.Remove(fullKey);
                else
                    SharedCache.Instance.Set(fullKey, value, new MemoryCacheEntryOptions { SlidingExpiration = SessionObjectStore.IdleTimeout });
            }
        }
    }
}
