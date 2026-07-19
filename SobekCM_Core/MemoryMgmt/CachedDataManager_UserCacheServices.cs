#region Using directives

using System;
using System.IO;
using ProtoBuf;
using SobekCM.Core.Users;

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
    }
}
