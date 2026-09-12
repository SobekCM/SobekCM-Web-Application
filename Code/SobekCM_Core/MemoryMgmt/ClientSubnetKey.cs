using Microsoft.AspNetCore.Http;
using System.Net;
using System.Net.Sockets;

namespace SobekCM.Core.MemoryMgmt
{
    /// <summary> Masks a client IP down to the /24 (IPv4) or /48 (IPv6) subnet key that the rate-limiting
    /// plan's per-subnet budgets (see <see cref="JP2RateLimiting_Gateway"/>) are keyed on -- a single
    /// client's own traffic still moves around within a /24 or /48, but the subnet itself is what a slow,
    /// distributed, non-bursting crawl can't hide from the way it can hide from a per-exact-IP counter. </summary>
    /// <remarks> In the web app this runs once per request, in UserIpInitializer, and the result is parked
    /// in the request cache under <see cref="RequestCache_Keys.UserSubnetKey"/> -- read it from there rather
    /// than calling this again, so every subnet-keyed limiter is working from the same value (and from the
    /// same IP, including UserIpInitializer's DEBUG loopback rewrite). A second, independent copy of this
    /// exact algorithm also exists in SobekCM_ImageServer's Program.cs (subnet_key_for) -- that project is
    /// deliberately kept free of any reference to SobekCM_Core (see its own remarks), so the two copies
    /// can't share code, only the documented algorithm. Keep them in lockstep if this ever changes. </remarks>
    public static class ClientSubnetKey
    {
        /// <summary> Reads back the subnet key already computed for this request by UserIpInitializer </summary>
        /// <param name="Context"> Current HTTP context </param>
        /// <returns> The subnet key, or NULL if there is no context or no resolvable IP </returns>
        /// <remarks> This is the normal way to get at the key -- <see cref="For"/> does the actual masking
        /// and should only be called by UserIpInitializer itself, once per request. </remarks>
        public static string From(HttpContext Context)
        {
            return Context?.Items[RequestCache_Keys.UserSubnetKey]?.ToString();
        }

        /// <summary> Computes the subnet key for a client IP address string (as read from
        /// <see cref="RequestCache_Keys.UserIP"/>). </summary>
        /// <returns> The subnet key, or NULL if <paramref name="IpAddress"/> doesn't parse as an IP </returns>
        public static string For(string IpAddress)
        {
            if ((string.IsNullOrEmpty(IpAddress)) || (!IPAddress.TryParse(IpAddress, out IPAddress ip)))
                return null;

            if (ip.IsIPv4MappedToIPv6)
                ip = ip.MapToIPv4();

            byte[] addressBytes = ip.GetAddressBytes();

            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                addressBytes[3] = 0;
                return new IPAddress(addressBytes) + "/24";
            }

            for (int i = 6; i < addressBytes.Length; i++)
                addressBytes[i] = 0;
            return new IPAddress(addressBytes) + "/48";
        }
    }
}
