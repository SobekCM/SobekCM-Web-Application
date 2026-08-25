// HTML5 10/15/2013

#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.Configuration.Localization;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.Navigation;
using SobekCM.Core.Users;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Library.Authentication;
using SobekCM.Library.HTML;
using SobekCM.Library.Localization;
using SobekCM.Library.MainWriters;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System;
using System.IO;

#endregion

namespace SobekCM.Library.MySobekViewer
{
    /// <summary> Class allows a user to logon, either with using the mySobek authentication, or clicking the link for Gatorlink authentication </summary>
    /// <remarks> This class extends the <see cref="abstract_MySobekViewer"/> class.<br /><br />
    /// MySobek Viewers are used for registration and authentication with mySobek, as well as performing any task which requires
    /// authentication, such as online submittal, metadata editing, and system administrative tasks.<br /><br />
    /// During a valid html request, the following steps occur:
    /// <ul>
    /// <li>Application state is built/verified by the Application_State_Builder </li>
    /// <li>Request is analyzed by the QueryString_Analyzer and output as a <see cref="Navigation_Object"/>  </li>
    /// <li>Main writer is created for rendering the output, in his case the <see cref="Html_MainWriter"/> </li>
    /// <li>The HTML writer will create the necessary subwriter.  Since this action requires authentication, an instance of the  <see cref="MySobek_HtmlSubwriter"/> class is created. </li>
    /// <li>The mySobek subwriter creates an instance of this viewer to allow the user to logon </li>
    /// </ul></remarks>
    public class Logon_MySobekViewer : abstract_MySobekViewer
    {
        private readonly string errorMessage;
        private readonly bool generalLogonDisabled;
        private readonly string generalLogonDisabledMsg;

        /// <summary> Constructor for a new instance of the Home_MySobekViewer class </summary>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        public Logon_MySobekViewer(RequestCache RequestSpecificValues, HttpContext Context) : base(RequestSpecificValues, Context)
        {
            // Check to see if (non-admin) logon is currently disabled
            if (UI_ApplicationCache_Gateway.Settings.System.Disable_Standard_User_Logon_Flag)
            {
                generalLogonDisabled = true;
                generalLogonDisabledMsg = String.IsNullOrEmpty(UI_ApplicationCache_Gateway.Settings.System.Disable_Standard_User_Logon_Message) ?
                    Localization_Gateway.Logon.Default_Disabled_Message(RequestSpecificValues.Current_Mode.Language) : UI_ApplicationCache_Gateway.Settings.System.Disable_Standard_User_Logon_Message;
            }
            else
            {
                generalLogonDisabled = false;
                generalLogonDisabledMsg = String.Empty;
            }

            RequestSpecificValues.Tracer.Add_Trace("Logon_MySobekViewer.Constructor", String.Empty);

            errorMessage = String.Empty;

            // If this is a postback, check to see if the user is valid
            if ((RequestSpecificValues.Current_Mode.isPostBack) && (Context.Request.HasFormContentType))
            {
                string possible_username = String.Empty;
                string possible_password = String.Empty;
                bool remember_me = false;

                var getKeys = Context.Request.Form.Keys;
                foreach (string thisKey in getKeys)
                {
                    switch (thisKey)
                    {
                        case "logon_username":
                            possible_username = Context.Request.Form[thisKey].TrimFirst();
                            break;

                        case "logon_password":
                            possible_password = Context.Request.Form[thisKey].TrimFirst();
                            break;

                        case "rememberme":
                            if (Context.Request.Form[thisKey].TrimFirst() == "rememberme")
                                remember_me = true;
                            break;
                    }
                }

                // Enforced here, not just by hiding the popup form below - a direct POST to this page could
                // otherwise still authenticate against a local account while this instance only wants sign-in
                // through OIDC/SAML
                if ((UI_ApplicationCache_Gateway.Configuration.Authentication.AllowLocalAuth) && (!String.IsNullOrEmpty(possible_password)) && (!String.IsNullOrEmpty(possible_username)))
                {
                    User_Object user = Authentication_Provider_Gateway.Get_Credential_Provider("sobek")?.Authenticate(possible_username, possible_password, RequestSpecificValues.Tracer);
                    if (user != null)
                    {
                        // If disabled for general logon,cancel
                        if ((generalLogonDisabled) && (!user.Is_Host_Admin) && (!user.Is_System_Admin))
                        {
                            errorMessage = generalLogonDisabledMsg;
                            return;
                        }

                        // The user was valid here, so save this user information
                        Context.Session.SetString(SessionCache_Keys.User, CachedDataManager_UserCacheServices.UserToString(user));
                        RequestSpecificValues.Current_User = user;

                        // Should we remember this user via cookies?
                        if (remember_me)
                        {
                            string userIp = Context.Connection.RemoteIpAddress?.ToString();
                            string cookieValue = $"userid={user.UserID}&security_hash={user.Security_Hash(userIp)}";
                            Context.Response.Cookies.Append("SobekUser", cookieValue, new CookieOptions
                            {
                                Expires = DateTimeOffset.Now.AddDays(30),
                                HttpOnly = true,
                                Secure = true
                            });
                        }

                        // Forward back to their original URL (unless the original URL was this logon page)
                        string raw_url = Context.Items[RequestCache_Keys.OriginalUrl].ToString();
                        if (raw_url.ToLower().IndexOf("my/logon") > 0)
                        {
                            if ((!String.IsNullOrEmpty(RequestSpecificValues.Current_Mode.Return_URL)) &&
                                (RedirectGuard.IsSafeRedirectTarget(RequestSpecificValues.Current_Mode.Return_URL, Context.Request.Host.Host)))
                            {
                                Context.Response.Redirect(RequestSpecificValues.Current_Mode.Return_URL);
                                RequestSpecificValues.Current_Mode.Request_Completed = true;
                                return;
                            }
                            else
                            {
                                RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Home;
                                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode, Context);
                            }
                        }
                        else
                        {
                            if (RedirectGuard.IsSafeRedirectTarget(raw_url, Context.Request.Host.Host))
                                Context.Response.Redirect(raw_url);
                            RequestSpecificValues.Current_Mode.Request_Completed = true;
                        }
                    }
                    else
                    {
                        errorMessage = Localization_Gateway.Logon.Invalid_Credentials(RequestSpecificValues.Current_Mode.Language);
                    }
                }
            }
        }

        /// <summary> Property indicates if this mySobek viewer can contain pop-up forms</summary>
        /// <remarks> If the mySobek viewer contains pop-up forms the overall page renders differently, 
        /// allowing for the blanket division and the popup forms near the top of the rendered HTML </remarks>
        ///<value> This mySobek viewer always returns the value TRUE </value>
        public override bool Contains_Popup_Forms
        {
            get
            {
                return true;
            }
        }

        /// <summary> Title for the page that displays this viewer, this is shown in the search box at the top of the page, just below the banner </summary>
        /// <value> The value of this message changes depending on which instance or by which URL the query arrives ( i.e., UDC, dLOC, etc.. )</value>
        public override string Web_Title
        {
            get
            {
                return String.Format(Localization_Gateway.Logon.Page_Title_Format(RequestSpecificValues.Current_Mode.Language), RequestSpecificValues.Current_Mode.Portal_Abbreviation);
            }
        }

        /// <summary> Add the HTML to be displayed in the main SobekCM viewer area </summary>
        /// <param name="Output"> Textwriter to write the HTML for this viewer</param>
        /// <param name="Tracer">Trace object keeps a list of each method executed and important milestones in rendering</param>
        public override void Write_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            // Get ready to draw the tabs
            string language = RequestSpecificValues.Current_Mode.Language;
            string my_sobek = "my" + RequestSpecificValues.Current_Mode.Portal_Abbreviation;

            Output.WriteLine("<br />");
            Output.WriteLine("<h1>" + String.Format(Localization_Gateway.Logon.Heading_Format(language), my_sobek) + "</h1>");
            Output.WriteLine();

            // If there was an error, show it
            if (errorMessage.Length > 0)
            {
                Output.WriteLine("<div class=\"sbkLomv_ErrorMsg\">" + errorMessage + "</div>");
            }

            Output.WriteLine("<script src=\"" + Static_Resources_Gateway.Sobekcm_Metadata_Js + "\" type=\"text/javascript\"></script>");
            Output.WriteLine("<div class=\"sbkMySobek_HomeText\" >");
            Output.WriteLine("  <br />");
            if (generalLogonDisabled)
            {
                Output.WriteLine("  <span id=\"sbkLomv_LogonDisabledMsg\">" + generalLogonDisabledMsg + "</span>");
            }
            Output.WriteLine("  <p>" + Localization_Gateway.Logon.Feature_Requires_Logon(language) + "<p>");
            Output.WriteLine("  <p>" + Localization_Gateway.Logon.Choose_Logon_Below(language) + "</p>");
            Output.WriteLine("  <ul id=\"sbkLomv_OptionsList\">");

            bool allowLocalAuth = UI_ApplicationCache_Gateway.Configuration.Authentication.AllowLocalAuth;

            if (RequestSpecificValues.Current_Mode.Portal_Abbreviation == "dLOC")
            {
                if (allowLocalAuth)
                    Output.WriteLine("    <li><span style=\"font-weight:bold\">" + Localization_Gateway.Logon.Dloc_Valid_Logon_Text(language) + "</span>, <a id=\"form_logon_term\" href=\"" + RequestSpecificValues.Current_Mode.Base_URL + "l/technical/javascriptrequired\" onclick=\"return popup_mysobek_form('form_logon', 'logon_username');\">" + Localization_Gateway.Logon.Dloc_Sign_On_Text(language) + "</a>.</li>");


                if ((!generalLogonDisabled) && (UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth != null) && (UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Enabled) && (UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Label.Length > 0) && (UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.ShibbolethURL.Length > 0))
                {
                    Output.WriteLine("    <li><span style=\"font-weight:bold\">" + String.Format(Localization_Gateway.Logon.Shibboleth_Valid_Id_Format(language), UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Label) + "</span>, <a href=\"" + UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.ShibbolethURL + "\">" + String.Format(Localization_Gateway.Logon.Shibboleth_Sign_On_Format(language), UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Label) + "</a>.</li>");
                }
            }
            else
            {
                if ((!generalLogonDisabled) && (UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth != null) && (UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Enabled) && (UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Label.Length > 0) && (UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.ShibbolethURL.Length > 0))
                {
                    Output.WriteLine("    <li><span style=\"font-weight:bold\">" + String.Format(Localization_Gateway.Logon.Shibboleth_Valid_Id_Format(language), UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Label) + "</span>, <a href=\"" + UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.ShibbolethURL + "\">" + String.Format(Localization_Gateway.Logon.Shibboleth_Sign_On_Format(language), UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Label) + "</a>.</li>");
                }

                if (allowLocalAuth)
                    Output.WriteLine("    <li><span style=\"font-weight:bold\">" + String.Format(Localization_Gateway.Logon.Instance_Valid_Logon_Format(language), RequestSpecificValues.Current_Mode.Portal_Abbreviation) + "</span>, <a id=\"form_logon_term\" href=\"" + RequestSpecificValues.Current_Mode.Base_URL + "l/technical/javascriptrequired\" onclick=\"return popup_mysobek_form('form_logon', 'logon_username');\">" + String.Format(Localization_Gateway.Logon.Instance_Sign_On_Format(language), RequestSpecificValues.Current_Mode.Portal_Abbreviation) + "</a>.</li>");
            }

            // One link per configured, enabled OIDC/SAML provider, additive to the Shibboleth link above
            if (!generalLogonDisabled)
            {
                foreach (IFederated_Authentication_Provider federatedProvider in Authentication_Provider_Gateway.All_Enabled_Federated_Providers)
                {
                    RequestSpecificValues.Current_Mode.My_Sobek_Type = federatedProvider.Authentication_Type == User_Authentication_Type_Enum.Saml
                        ? My_Sobek_Type_Enum.SAML_Landing
                        : My_Sobek_Type_Enum.OIDC_Landing;
                    RequestSpecificValues.Current_Mode.My_Sobek_SubMode = federatedProvider.Provider_Code;
                    string signInUrl = UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode);

                    Output.WriteLine("    <li><span style=\"font-weight:bold\">" + String.Format(Localization_Gateway.Logon.Federated_Valid_Account_Format(language), federatedProvider.Display_Label) + "</span>, <a href=\"" + signInUrl + "\">" + String.Format(Localization_Gateway.Logon.Federated_Sign_In_Format(language), federatedProvider.Display_Label) + "</a>.</li>");
                }
                RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Logon;
                RequestSpecificValues.Current_Mode.My_Sobek_SubMode = String.Empty;
            }

            if (!generalLogonDisabled)
            {
                if (allowLocalAuth)
                {
                    RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Register;
                    Output.Write("    <li><span style=\"font-weight:bold\">" + Localization_Gateway.Logon.Not_Registered_Yet(language) + "</span> <a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">" + Localization_Gateway.Logon.Register_Now(language) + "</a> or ");
                }
                else
                {
                    Output.Write("    <li>");
                }

                RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Contact;
                Output.WriteLine(" <a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">" + Localization_Gateway.Logon.Contact_Us(language) + "</a></li>");
            }

            RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Logon;
            RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.My_Sobek;

            Output.WriteLine("  </ul>");

            Output.WriteLine("</div>");

            Output.WriteLine("<br />");
            Output.WriteLine("<br />");

            // Open the item nav form
            Write_ItemNavForm_Opening(Output);

            #region Popup Form Html

            // Nothing links to this popup (the trigger <a id="form_logon_term"> links above are themselves
            // gated on allowLocalAuth) when local auth is disabled, but skip rendering the form entirely
            // rather than just leaving it unreachable
            if (allowLocalAuth)
            {
                Tracer.Add_Trace("Logon_MySobekViewer.Add_Popup_HTML", "Add any popup divisions for form elements");

                Output.WriteLine("<script type=\"text/javascript\" src=\"" + Static_Resources_Gateway.Jquery_Ui_1_14_2_Js + "\"></script>");

                // Add the popup form
                Output.WriteLine("<!-- mySobek Log On Form -->");
                Output.WriteLine("<div class=\"sbkLomv_PopupDiv\" id=\"form_logon\" style=\"display:none;\">");
                Output.WriteLine("  <div class=\"sbkMySobek_PopupTitle\"><table style=\"width:100%;\"><tr><td style=\"text-align:left;\">" + Localization_Gateway.Logon.Popup_Title_Log_In(language) + "</td><td style=\"text-align:right;\"><a href=\"" + RequestSpecificValues.Current_Mode.Base_URL + "logon/help\" target=\"_FORM_SIGNON_HELP\" >?</a> &nbsp; <a href=\"#template\" onclick=\"return close_mysobek_form('form_logon');\">X</a> &nbsp; </td></tr></table></div>");
                Output.WriteLine("  <br />");
                Output.WriteLine("  <table class=\"sbkMySobek_PopupTable\">");

                // Add the rows of data
                Output.WriteLine("    <tr><td style=\"width:140px;\">" + Localization_Gateway.Logon.Username_Or_Email_Label(language) + "</td><td><input class=\"sbkLomv_username_input sbkMySobek_Focusable\" name=\"logon_username\" id=\"logon_username\" type=\"text\" value=\"\" onkeydown=\"logonTrapKD(event);\" /></td></tr>");
                Output.WriteLine("    <tr><td>" + Localization_Gateway.Logon.Password_Label(language) + "</td><td><input class=\"sbkLomv_password_input sbkMySobek_Focusable\" name=\"logon_password\" id=\"logon_password\" type=\"password\" value=\"\" onkeydown=\"logonTrapKD(event);\" /></td></tr>");
                Output.WriteLine("    <tr><td>&nbsp;</td><td><input type=\"checkbox\" value=\"rememberme\" class=\"sbkMySobek_checkbox\" name=\"rememberme\" id=\"rememberme\" /> <label for=\"rememberme\">" + Localization_Gateway.Logon.Remember_Me(language) + "</label><br /><br /></td></tr>");

                // Add the buttons
                Output.WriteLine("    <tr style=\"height:35px; text-align: center; vertical-align: bottom;\">");
                Output.WriteLine("      <td colspan=\"2\">");
                Output.WriteLine("        <button title=\"" + Localization_Gateway.Logon.Close_Button_Title(language) + "\" class=\"sbkMySobek_BigButton\" onclick=\"return close_mysobek_form('form_logon');\"><img src=\"" + Static_Resources_Gateway.Button_Previous_Arrow_Png + "\" class=\"sbkMySobek_RoundButton_LeftImg\" alt=\"\" /> " + Localization_Gateway.Logon.Cancel_Button_Text(language) + " &nbsp; </button> &nbsp; &nbsp; ");
                Output.WriteLine("        <button title=\"" + Localization_Gateway.Logon.Login_Button_Title(language) + "\" class=\"sbkMySobek_BigButton\" type=\"submit\"> &nbsp; " + Localization_Gateway.Logon.Login_Button_Text(language) + " <img src=\"" + Static_Resources_Gateway.Button_Next_Arrow_Png + "\" class=\"sbkMySobek_RoundButton_RightImg\" alt=\"\" /></button>");
                Output.WriteLine("      </td>");
                Output.WriteLine("    </tr>");

                RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Register;
                string register_now_link = "<a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">" + Localization_Gateway.Logon.Register_Now(language) + "</a>";
                Output.WriteLine("    <tr><td colspan=\"2\"><br />" + String.Format(Localization_Gateway.Logon.Popup_Not_Registered_Format(language), register_now_link));
                RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Logon;

                RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Contact;
                string popup_contact_us_link = "<a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">" + Localization_Gateway.Logon.Popup_Contact_Us_Link_Text(language) + "</a>";
                Output.WriteLine("    <br /><br />" + String.Format(Localization_Gateway.Logon.Popup_Forgot_Password_Format(language), popup_contact_us_link) + "</td></tr>");
                RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.My_Sobek;

                // Finish the popup form
                Output.WriteLine("  </table>");
                Output.WriteLine("  <br />");
                Output.WriteLine("</div>");
                Output.WriteLine();
            }
            #endregion

            // Close the item nav form
            Write_ItemNavForm_Closing(Output);
        }

        /// <summary> Flag indicates if a user must be logged in to access this 
        /// admin or mySobek view.  </summary>
        /// <value> Returns FALSE since this page allows users to logon </value>
        public override bool Requires_Logged_In_User
        {
            get { return false; }
        }
    }
}
