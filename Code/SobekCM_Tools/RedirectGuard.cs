using System;

namespace SobekCM.Tools
{
    /// <summary> Helper for confirming a possibly-tainted redirect target is a local URL
    /// before handing it to Response.Redirect, guarding against open-redirect attacks </summary>
    public static class RedirectGuard
    {
        /// <summary> Determines whether a URL is local to this site - safe to redirect to
        /// without risking an open redirect to an attacker-controlled host </summary>
        /// <param name="url"> Possibly-tainted redirect target, e.g. from a query string or the request path </param>
        /// <returns> TRUE if the URL is a same-site relative path, otherwise FALSE </returns>
        public static bool IsLocalUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;

            // Allows "/" or "/foo" but not "//" or "/\" - browsers treat a leading "//" or "/\"
            // as scheme-relative, i.e. a redirect to a different host
            if (url[0] == '/')
            {
                if (url.Length == 1)
                    return true;

                return url[1] != '/' && url[1] != '\\';
            }

            // Allows "~/" or "~/foo" but not "~//" or "~/\"
            if ((url[0] == '~') && (url.Length > 1) && (url[1] == '/'))
            {
                if (url.Length == 2)
                    return true;

                return url[2] != '/' && url[2] != '\\';
            }

            return false;
        }

        /// <summary> Determines whether a URL is safe to redirect to: either a same-site relative
        /// path, or an absolute URL whose host matches the current request's own host. SobekCM builds
        /// several redirect targets (e.g. Base_URL + path) as full absolute URLs rather than relative
        /// paths, so <see cref="IsLocalUrl"/> alone would reject those legitimately safe targets. </summary>
        /// <param name="url"> Possibly-tainted redirect target </param>
        /// <param name="trustedHost"> Host name the URL must match if it is absolute, typically the current request's own host </param>
        /// <returns> TRUE if the URL is safe to redirect to, otherwise FALSE </returns>
        public static bool IsSafeRedirectTarget(string url, string trustedHost)
        {
            if (string.IsNullOrEmpty(url))
                return false;

            if (IsLocalUrl(url))
                return true;

            if ((!string.IsNullOrEmpty(trustedHost)) &&
                (Uri.TryCreate(url, UriKind.Absolute, out Uri parsedUri)) &&
                ((parsedUri.Scheme == Uri.UriSchemeHttp) || (parsedUri.Scheme == Uri.UriSchemeHttps)))
            {
                return string.Equals(parsedUri.Host, trustedHost, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }
    }
}
