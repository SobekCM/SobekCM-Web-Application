using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;

namespace SobekCM.Library.UI
{
    /// <summary>
    /// In-memory replacement for HttpSessionState object storage.
    /// ASP.NET Core ISession only serializes byte arrays/strings, so complex objects
    /// are kept in this per-session in-process dictionary.
    /// Usage: Context.SessionObject()["key"] — mirrors the old HttpContext.Current.Session["key"] pattern.
    /// </summary>
    public static class SessionObjectStore
    {
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, object>> _store = new();

        /// <summary> Returns the object bag for the current session </summary>
        public static SessionObjectBag SessionObject(this HttpContext context)
            => new SessionObjectBag(context.Session.Id, _store);

        /// <summary> Removes all object entries for a session (call on logout/session end) </summary>
        public static void ClearSession(string sessionId) => _store.TryRemove(sessionId, out _);
    }

    /// <summary> Provides indexer-based access to the in-memory per-session object store </summary>
    public class SessionObjectBag
    {
        private readonly string _sessionId;
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, object>> _store;

        internal SessionObjectBag(string sessionId, ConcurrentDictionary<string, ConcurrentDictionary<string, object>> store)
        {
            _sessionId = sessionId;
            _store = store;
        }

        public object this[string key]
        {
            get
            {
                return _store.TryGetValue(_sessionId, out var bag)
                       && bag.TryGetValue(key, out var v) ? v : null;
            }
            set
            {
                if (value == null)
                {
                    if (_store.TryGetValue(_sessionId, out var bag))
                        bag.TryRemove(key, out _);
                }
                else
                {
                    _store.GetOrAdd(_sessionId, _ => new ConcurrentDictionary<string, object>())[key] = value;
                }
            }
        }
    }
}
