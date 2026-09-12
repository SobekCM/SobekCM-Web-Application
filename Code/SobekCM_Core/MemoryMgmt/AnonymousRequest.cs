using Microsoft.AspNetCore.Http;
using SobekCM.Core.Users;

namespace SobekCM.Core.MemoryMgmt
{
    /// <summary> Resolves the subnet key that the anonymous-only rate limiters budget against, or NULL when
    /// this request isn't subject to them at all. </summary>
    /// <remarks> Every per-subnet limiter needs the same two things first -- "is this an anonymous request?"
    /// and "what subnet is it from?" -- so they share this rather than each repeating the pair. The subnet
    /// key itself is computed once per request by UserIpInitializer (see
    /// <see cref="RequestCache_Keys.UserSubnetKey"/>); this only reads it back. </remarks>
    public static class AnonymousRequest
    {
        /// <summary> Subnet key to budget this request against, or NULL if it isn't subject to an
        /// anonymous per-subnet budget </summary>
        /// <param name="CurrentUser"> Current user, who may or may not be logged on </param>
        /// <param name="Context"> Current HTTP context, holding the already-computed subnet key </param>
        /// <returns> The /24 or /48 subnet key, or NULL for a logged-on user (never budgeted) or a request
        /// with no resolvable IP </returns>
        public static string Subnet_Key(User_Object CurrentUser, HttpContext Context)
        {
            // Logged-on users are never subject to an anonymous per-subnet budget
            if ((CurrentUser != null) && (CurrentUser.LoggedOn))
                return null;

            return Context?.Items[RequestCache_Keys.UserSubnetKey]?.ToString();
        }
    }
}
