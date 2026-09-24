#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.MemoryMgmt;
using System;
using System.IO;

#endregion

namespace SobekCM.Library.Authentication
{
    /// <summary> Honeypot field and attempt logging shared by the two self-registration screens </summary>
    public static class Registration_Abuse_Helper
    {
        /// <summary> Name of the hidden field. Deliberately inviting to a form-filling bot, but not something a
        /// browser autofill or password manager fills in </summary>
        private const string Honeypot_Field = "prefWebsite";

        /// <summary> Writes the honeypot input, positioned off-screen (not display:none, which simple bots skip)
        /// and removed from tab order, autofill and screen readers so a real person never fills it in. Must be
        /// written where a block-level element is valid, e.g. inside a table cell </summary>
        /// <param name="Output"> Stream to write the HTML to </param>
        public static void Write_Honeypot(TextWriter Output)
        {
            Output.WriteLine("<div style=\"position:absolute;left:-10000px;top:auto;width:1px;height:1px;overflow:hidden;\" aria-hidden=\"true\">" +
                "<label for=\"" + Honeypot_Field + "\">Leave this field empty</label>" +
                "<input type=\"text\" name=\"" + Honeypot_Field + "\" id=\"" + Honeypot_Field + "\" value=\"\" tabindex=\"-1\" autocomplete=\"off\" /></div>");
        }

        /// <summary> Flag indicates if the honeypot field was filled in on the posted form </summary>
        /// <param name="Context"> Current request </param>
        public static bool Honeypot_Triggered(HttpContext Context)
        {
            return Context.Request.HasFormContentType && !String.IsNullOrWhiteSpace(Context.Request.Form[Honeypot_Field].ToString());
        }

        /// <summary> Records this registration attempt in temp/registrations.txt </summary>
        /// <param name="Context"> Current request, for the IP address and user agent </param>
        /// <param name="Email"> Email address entered (only the domain is logged) </param>
        /// <param name="Succeeded"> TRUE if the account was created </param>
        /// <param name="HoneypotTriggered"> TRUE if the honeypot was filled in </param>
        public static void Log_Attempt(HttpContext Context, string Email, bool Succeeded, bool HoneypotTriggered)
        {
            string ip = Context.Items[RequestCache_Keys.UserIP] as string;
            string userAgent = (Context.Items[RequestCache_Keys.UserAgent] as string) ?? Context.Request.Headers["User-Agent"].ToString();

            // Email verification doesn't exist yet, so it can't be known either way - logged as n/a
            RegistrationLog_Gateway.Append(ip, Email, Succeeded, HoneypotTriggered, null, userAgent);
        }
    }
}
