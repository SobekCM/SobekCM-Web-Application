#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.RateLimiting;
using SobekCM.Library.Localization;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;

#endregion

namespace SobekCM.Library.Authentication
{
    /// <summary> Renders the Cloudflare Turnstile widget on anonymous forms and verifies the response on postback </summary>
    /// <remarks> Every method is a no-op (and <see cref="Verify"/> succeeds) unless a captcha is configured with
    /// both keys - see <see cref="SobekCM.Core.Configuration.Authentication.Captcha_Configuration.Is_Active"/> - so
    /// an instance with no captcha element in its authentication config behaves exactly as it did before. </remarks>
    public static class Captcha_Helper
    {
        private const string Script_Url = "https://challenges.cloudflare.com/turnstile/v0/api.js";
        private const string Verify_Url = "https://challenges.cloudflare.com/turnstile/v0/siteverify";
        private const string Response_Field = "cf-turnstile-response";

        /// <summary> Shown for both a missing and a failed captcha, and used as the localization key </summary>
        private const string Error_Term = "Please confirm you are not a robot.";

        private static readonly HttpClient httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

        /// <summary> Flag indicates if a captcha is configured and should be shown and enforced </summary>
        public static bool Is_Active
        {
            get
            {
                var captcha = UI_ApplicationCache_Gateway.Configuration.Authentication.Captcha;
                return (captcha != null) && (captcha.Is_Active);
            }
        }

        /// <summary> Localized error text for a missing or failed captcha </summary>
        /// <param name="Language"> Current UI language </param>
        public static string Error_Message(string Language)
        {
            return Localization_Gateway.General.Get(Error_Term, Language);
        }

        /// <summary> Writes the widget (and the script it needs) where the form should show it. Does nothing
        /// when no captcha is configured </summary>
        /// <param name="Output"> Stream to write the HTML to </param>
        /// <param name="Language"> Current UI language, so the widget's own text matches the page </param>
        public static void Write_Widget(TextWriter Output, string Language)
        {
            if (!Is_Active)
                return;

            // Turnstile takes a language code (or "auto"); only pass ours along if it looks like one
            string widgetLanguage = "auto";
            if ((!String.IsNullOrEmpty(Language)) && (Language.Length >= 2) && (Language.Length <= 5))
            {
                bool isCode = true;
                foreach (char c in Language)
                {
                    if (!(((c >= 'a') && (c <= 'z')) || ((c >= 'A') && (c <= 'Z')) || (c == '-')))
                    {
                        isCode = false;
                        break;
                    }
                }
                if (isCode)
                    widgetLanguage = Language.ToLowerInvariant();
            }

            Output.WriteLine("<script src=\"" + Script_Url + "\" async defer></script>");
            Output.WriteLine("<div class=\"cf-turnstile\" data-sitekey=\"" + WebUtility.HtmlEncode(UI_ApplicationCache_Gateway.Configuration.Authentication.Captcha.SiteKey) + "\" data-language=\"" + widgetLanguage + "\"></div>");
        }

        /// <summary> Verifies the captcha response posted with the current form </summary>
        /// <param name="Context"> Current request, which must be a form post </param>
        /// <param name="Tracer"> Trace object </param>
        /// <returns> TRUE if no captcha is configured or the response verified, otherwise FALSE </returns>
        /// <remarks> Fails closed: if the provider can't be reached, the submission is rejected and the problem is
        /// written to the exception log so an outage is visible. </remarks>
        public static bool Verify(HttpContext Context, Custom_Tracer Tracer)
        {
            if (!Is_Active)
                return true;

            Tracer?.Add_Trace("Captcha_Helper.Verify");

            string clientIp = Context.Items[RequestCache_Keys.UserIP] as string ?? String.Empty;
            bool loggedOn = false;

            string token = Context.Request.HasFormContentType ? Context.Request.Form[Response_Field].ToString() : String.Empty;
            if (String.IsNullOrWhiteSpace(token))
            {
                log_failure(clientIp, loggedOn, "no captcha response posted");
                return false;
            }

            try
            {
                var fields = new Dictionary<string, string>
                {
                    { "secret", UI_ApplicationCache_Gateway.Configuration.Authentication.Captcha.SecretKey },
                    { "response", token }
                };
                if (!String.IsNullOrEmpty(clientIp))
                    fields["remoteip"] = clientIp;

                using (var content = new FormUrlEncodedContent(fields))
                using (HttpResponseMessage response = httpClient.PostAsync(Verify_Url, content).GetAwaiter().GetResult())
                {
                    string json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    using (JsonDocument doc = JsonDocument.Parse(json))
                    {
                        if ((doc.RootElement.TryGetProperty("success", out JsonElement success)) && (success.ValueKind == JsonValueKind.True))
                            return true;

                        string codes = String.Empty;
                        if ((doc.RootElement.TryGetProperty("error-codes", out JsonElement errors)) && (errors.ValueKind == JsonValueKind.Array))
                            codes = errors.ToString();
                        log_failure(clientIp, loggedOn, "verification rejected " + codes);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionLog_Gateway.Append("Captcha verification could not be completed (" + ex.GetType().Name + ": " + ex.Message + ") - the form submission was rejected");
                log_failure(clientIp, loggedOn, "verification unavailable");
                return false;
            }
        }

        private static void log_failure(string ClientIp, bool LoggedOn, string Details)
        {
            RateLimitLog_Gateway.Append(RateLimitLog_Gateway.Event_Captcha_Fail, LoggedOn, ClientIp, Details.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' '));
        }
    }
}
