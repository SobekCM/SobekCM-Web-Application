#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.Navigation;
using SobekCM.Library.Authentication;
using SobekCM.Tools;
using System;
using System.IO;

#endregion

namespace SobekCM.Library.MySobekViewer
{
    /// <summary> Begins a SAML sign-in for the provider named in the URL ("/my/saml/{provider_code}")
    /// by redirecting to that identity provider </summary>
    /// <remarks> This viewer only ever handles the BEGIN leg of the round-trip. The identity provider's
    /// Assertion Consumer Service (ACS) return leg is intercepted directly by the ASP.NET Core
    /// authentication middleware (Sustainsys.Saml2) before the request ever reaches
    /// QueryInitializer/this viewer — see IFederated_Authentication_Provider's remarks. </remarks>
    public class Saml_Landing_MySobekViewer : abstract_MySobekViewer
    {
        private readonly string errorMessage;

        /// <summary> Constructor for a new instance of the Saml_Landing_MySobekViewer class </summary>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        public Saml_Landing_MySobekViewer(RequestCache RequestSpecificValues, HttpContext Context) : base(RequestSpecificValues, Context)
        {
            string providerCode = RequestSpecificValues.Current_Mode.My_Sobek_SubMode;
            IFederated_Authentication_Provider provider = Authentication_Provider_Gateway.Get_Federated_Provider(providerCode);

            if (provider == null)
            {
                errorMessage = "Unknown or disabled sign-in provider";
                return;
            }

            string returnUrl = !String.IsNullOrEmpty(RequestSpecificValues.Current_Mode.Return_URL)
                ? RequestSpecificValues.Current_Mode.Return_URL
                : RequestSpecificValues.Current_Mode.Base_URL;

            // See Oidc_Landing_MySobekViewer for why blocking on this async call here is safe under Kestrel.
            provider.Begin_SignIn(Context, returnUrl).GetAwaiter().GetResult();

            RequestSpecificValues.Current_Mode.Request_Completed = true;
        }

        /// <summary> Title for the page that displays this viewer </summary>
        public override string Web_Title => "Signing in...";

        /// <summary> Add the HTML to be displayed in the main SobekCM viewer area </summary>
        /// <param name="Output"> Textwriter to write the HTML for this viewer</param>
        /// <param name="Tracer">Trace object keeps a list of each method executed and important milestones in rendering</param>
        public override void Write_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            if (!String.IsNullOrEmpty(errorMessage))
            {
                Output.WriteLine("<br /><h1>Sign In Error</h1>");
                Output.WriteLine("<div class=\"sbkLomv_ErrorMsg\">" + errorMessage + "</div>");
                return;
            }

            Output.WriteLine("<br /><p>Redirecting to sign in...</p>");
        }

        /// <summary> Flag indicates if a user must be logged in to access this view </summary>
        /// <value> Returns FALSE since this page is how a user signs in </value>
        public override bool Requires_Logged_In_User => false;
    }
}
