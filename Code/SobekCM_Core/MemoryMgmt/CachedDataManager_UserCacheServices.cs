#region Using directives

using Microsoft.AspNetCore.Http;
using ProtoBuf;
using SobekCM.Core.Users;
using System;
using System.IO;

#endregion

namespace SobekCM.Core.MemoryMgmt
{
    /// <summary> User session services for serializing and deserializing <see cref="User_Object"/> instances
    /// for storage in the ASP.NET Core string-based session (ISession.SetString / GetString) </summary>
    /// <remarks> This is purposely omitted from the CachedDataManager main class, and should not be 
    /// cleared when the application state memory is cleared.  </remarks>
    public static class CachedDataManager_UserCacheServices
    {
        /// <summary> Serializes a <see cref="User_Object"/> to a Base64 string via Protobuf </summary>
        /// <param name="User"> User object to serialize </param>
        /// <returns> Base64-encoded Protobuf bytes, or null if User is null </returns>
        public static string UserToString(User_Object User)
        {
            if (User == null) return null;

            using MemoryStream ms = new MemoryStream();
            Serializer.Serialize(ms, User);
            return Convert.ToBase64String(ms.ToArray());
        }

        /// <summary> Deserializes a <see cref="User_Object"/> from a Base64 Protobuf string </summary>
        /// <param name="Value"> Base64-encoded Protobuf string previously produced by <see cref="UserToString"/> </param>
        /// <returns> Deserialized User_Object, or null if Value is null or empty </returns>
        public static User_Object StringToUser(string Value)
        {
            if (String.IsNullOrEmpty(Value)) return null;

            byte[] bytes = Convert.FromBase64String(Value);
            using MemoryStream ms = new MemoryStream(bytes);
            return Serializer.Deserialize<User_Object>(ms);
        }

        /// <summary> Writes the (already modified) user back into the session </summary>
        /// <param name="Session"> Current request's session </param>
        /// <param name="User"> Logged-on user, after changes such as <see cref="User_Object.Add_Setting(string, string)"/> </param>
        /// <remarks> The request's user is deserialized fresh from the session on every request, so any change to it
        /// (a remembered preference, for instance) is lost on the next request unless it's saved back with this --
        /// even when the same change was also written to the database, which is only re-read at logon. </remarks>
        public static void Save_To_Session(ISession Session, User_Object User)
        {
            if ((Session == null) || (User == null)) return;

            Session.SetString(SessionCache_Keys.User, UserToString(User));
        }
    }
}
