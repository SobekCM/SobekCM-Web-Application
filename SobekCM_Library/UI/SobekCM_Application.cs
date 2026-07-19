using System.Collections.Concurrent;
using System.Linq;

namespace SobekCM.Library.UI
{
    /// <summary> Application-wide string/object cache, replacing System.Web.HttpApplicationState </summary>
    public static class SobekCM_Application
    {
        public static readonly ApplicationState State = new ApplicationState();

        public class ApplicationState
        {
            private readonly ConcurrentDictionary<string, object> _store = new();

            public object this[string key]
            {
                get => _store.TryGetValue(key, out var v) ? v : null;
                set
                {
                    if (value == null) _store.TryRemove(key, out _);
                    else _store[key] = value;
                }
            }

            /// <summary> All keys currently held in the application state store </summary>
            public string[] AllKeys => _store.Keys.ToArray();

            public void RemoveAll() => _store.Clear();
        }
    }
}
