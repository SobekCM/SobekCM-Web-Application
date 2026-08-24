#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.Navigation;
using SobekCM.Engine_Library.ApplicationState;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Engine_Library.Database;
using SobekCM.Library.HTML;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

#endregion

namespace SobekCM.Library.AdminViewer
{
    /// <summary> Class allows the host admin to configure the OpenID Connect (OIDC) identity providers
    /// available for this instance, entering each provider's Authority/ClientId/ClientSecret through
    /// a GUI rather than a config file </summary>
    /// <remarks> This class extends the <see cref="abstract_AdminViewer"/> class. Settings entered here are
    /// stored in the database (see <see cref="Engine_Database.Set_Extension_Setting"/>), scoped to the
    /// "oidc_auth" extension, using namespaced keys like "OIDC|{Provider_Code}|ClientId" so more than one
    /// provider can be configured at once. </remarks>
    public class Oidc_Auth_AdminViewer : abstract_AdminViewer
    {
        private const string EXTENSION_CODE = "oidc_auth";

        private string actionMessage;
        private List<Provider_Row> providerRows;

        /// <summary> Constructor for a new instance of the Oidc_Auth_AdminViewer class </summary>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        /// <remarks> Postback from saving provider settings is handled here in the constructor </remarks>
        public Oidc_Auth_AdminViewer(RequestCache RequestSpecificValues, HttpContext Context) : base(RequestSpecificValues, Context)
        {
            // Ensure the user is at least a system or portal admin
            if ((RequestSpecificValues.Current_User == null) || ((!RequestSpecificValues.Current_User.Is_System_Admin) && (!RequestSpecificValues.Current_User.Is_Portal_Admin)))
            {
                RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.My_Sobek;
                RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Home;
                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode, Context);
                return;
            }

            // Ensure the extension list exists and contains the OIDC auth extension, and that it is enabled
            // ( this settings page only becomes reachable once the extension has been activated )
            if ((UI_ApplicationCache_Gateway.Configuration.Extensions == null) ||
                (UI_ApplicationCache_Gateway.Configuration.Extensions.Get_Extension(EXTENSION_CODE) == null) ||
                (!UI_ApplicationCache_Gateway.Configuration.Extensions.Get_Extension(EXTENSION_CODE).Enabled))
            {
                RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.My_Sobek;
                RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Home;
                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode, Context);
                return;
            }

            // On a hosted instance, only the host admin may see or change these settings - they
            // include another organization's IdP secrets, not just this site's own configuration
            if ((UI_ApplicationCache_Gateway.Settings.Servers.isHosted) && (!RequestSpecificValues.Current_User.Is_Host_Admin))
            {
                RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.My_Sobek;
                RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Home;
                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode, Context);
                return;
            }

            // Look for a post back
            if ((RequestSpecificValues.Current_Mode.isPostBack) && (Context.Request.HasFormContentType))
            {
                var form = Context.Request.Form;
                string action = form["oidc_admin_action"].TrimFirst();
                if (action == "save")
                {
                    // Walk every posted row ( one per existing provider, plus the fixed "new" row )
                    // and upsert anything with a non-empty provider code
                    var rowIndexes = new List<string>();
                    for (int i = 0; i < 25; i++)
                        rowIndexes.Add(i.ToString());
                    rowIndexes.Add("new");

                    foreach (string rowIndex in rowIndexes)
                    {
                        string code = form["oidc_row_" + rowIndex + "_code"].TrimFirst();
                        if (String.IsNullOrEmpty(code))
                            continue;

                        string label = form["oidc_row_" + rowIndex + "_label"].TrimFirst();
                        string authority = form["oidc_row_" + rowIndex + "_authority"].TrimFirst();
                        string clientId = form["oidc_row_" + rowIndex + "_clientid"].TrimFirst();
                        string clientSecret = form["oidc_row_" + rowIndex + "_clientsecret"].TrimFirst();

                        Engine_Database.Set_Extension_Setting(EXTENSION_CODE, "OIDC|" + code + "|DisplayLabel", label);
                        Engine_Database.Set_Extension_Setting(EXTENSION_CODE, "OIDC|" + code + "|Authority", authority);
                        Engine_Database.Set_Extension_Setting(EXTENSION_CODE, "OIDC|" + code + "|ClientId", clientId);

                        // Leaving the secret field blank on an existing provider preserves the
                        // already-saved secret rather than blanking it out - the secret is never
                        // echoed back into the form, so a blank submission means "unchanged"
                        if (!String.IsNullOrEmpty(clientSecret))
                            Engine_Database.Set_Extension_Setting(EXTENSION_CODE, "OIDC|" + code + "|ClientSecret", clientSecret);
                    }

                    actionMessage = "All changes saved";

                    // Editing a live provider's settings has no effect until the ASP.NET Core
                    // AuthenticationBuilder options are re-read at next startup
                    AppLifetime_Gateway.RequestRestart("OIDC provider settings changed");
                }
            }

            // Load ( or reload, if just saved ) the current provider rows from the database
            providerRows = load_provider_rows();
        }

        private List<Provider_Row> load_provider_rows()
        {
            Dictionary<string, string> settings = Engine_Database.Get_Extension_Settings(EXTENSION_CODE, RequestSpecificValues.Tracer);

            var byCode = new SortedDictionary<string, Provider_Row>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, string> setting in settings)
            {
                // Expected shape: "OIDC|{Provider_Code}|{Field}"
                string[] parts = setting.Key.Split('|');
                if ((parts.Length != 3) || (parts[0] != "OIDC"))
                    continue;

                string code = parts[1];
                string field = parts[2];

                if (!byCode.TryGetValue(code, out Provider_Row row))
                {
                    row = new Provider_Row { Code = code };
                    byCode[code] = row;
                }

                switch (field)
                {
                    case "DisplayLabel":
                        row.Label = setting.Value;
                        break;

                    case "Authority":
                        row.Authority = setting.Value;
                        break;

                    case "ClientId":
                        row.ClientId = setting.Value;
                        break;
                    // ClientSecret is intentionally never read back into the form
                }
            }

            return byCode.Values.ToList();
        }

        /// <summary> Gets the CSS class of the container that the page is wrapped within </summary>
        public override string Container_CssClass
        {
            get { return "container-inner1215"; }
        }

        public override void Write_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            Write_ItemNavForm_Opening(Output);

            Output.WriteLine("<!-- Oidc_Auth_AdminViewer.Write_HTML -->");
            Output.WriteLine("<input type=\"hidden\" id=\"oidc_admin_action\" name=\"oidc_admin_action\" value=\"\" />");
            Output.WriteLine("<script src=\"" + Static_Resources_Gateway.Sobekcm_Admin_Js + "\" type=\"text/javascript\"></script>");

            Output.WriteLine("<div class=\"sbkAdm_HomeText\">");
            if (!String.IsNullOrWhiteSpace(actionMessage))
            {
                Output.WriteLine("  <div id=\"sbkAdm_ActionMessageSuccess\">" + actionMessage + "</div>");
            }
            Output.WriteLine("  <p style=\"text-align: left; padding:0 20px 0 70px;width:800px;\">Configure the OpenID Connect (OIDC) identity providers users may sign in through, in addition to the built-in username/password logon. Each provider needs its Authority, Client ID, and Client Secret from the identity provider's own configuration.</p>");
            Output.WriteLine("</div>");

            Output.WriteLine("  <table class=\"sbkAdm_Table\" id=\"sbkOidcAv_Table\">");
            Output.WriteLine("    <tr>");
            Output.WriteLine("      <th>Provider Code</th>");
            Output.WriteLine("      <th>Display Label</th>");
            Output.WriteLine("      <th>Authority</th>");
            Output.WriteLine("      <th>Client ID</th>");
            Output.WriteLine("      <th>Client Secret</th>");
            Output.WriteLine("    </tr>");

            int rowIndex = 0;
            foreach (Provider_Row row in providerRows)
            {
                write_provider_row(Output, rowIndex.ToString(), row.Code, row.Label, row.Authority, row.ClientId, false);
                rowIndex++;
            }

            Output.WriteLine("    <tr><td class=\"sbkAdm_TableRule\" colspan=\"5\"></td></tr>");
            Output.WriteLine("    <tr><td colspan=\"5\"><h3>Add a New Provider</h3></td></tr>");
            write_provider_row(Output, "new", String.Empty, String.Empty, String.Empty, String.Empty, true);

            Output.WriteLine("  </table>");

            Output.WriteLine("  <div class=\"sbkSeav_ButtonsDiv\">");
            Output.WriteLine("    <button title=\"Save changes\" class=\"sbkAdm_RoundButton\" onclick=\"return set_hidden_value_postback('oidc_admin_action', 'save');return false;\">SAVE</button>");
            Output.WriteLine("  </div>");

            Write_ItemNavForm_Closing(Output);
        }

        private void write_provider_row(TextWriter Output, string RowIndex, string Code, string Label, string Authority, string ClientId, bool CodeEditable)
        {
            Output.WriteLine("    <tr>");
            Output.WriteLine("      <td>" + (CodeEditable
                ? "<input type=\"text\" name=\"oidc_row_" + RowIndex + "_code\" placeholder=\"e.g. azuread\" />"
                : "<input type=\"text\" name=\"oidc_row_" + RowIndex + "_code\" value=\"" + Code + "\" readonly=\"readonly\" />") + "</td>");
            Output.WriteLine("      <td><input type=\"text\" name=\"oidc_row_" + RowIndex + "_label\" value=\"" + Label + "\" /></td>");
            Output.WriteLine("      <td><input type=\"text\" name=\"oidc_row_" + RowIndex + "_authority\" value=\"" + Authority + "\" size=\"40\" /></td>");
            Output.WriteLine("      <td><input type=\"text\" name=\"oidc_row_" + RowIndex + "_clientid\" value=\"" + ClientId + "\" /></td>");
            Output.WriteLine("      <td><input type=\"password\" name=\"oidc_row_" + RowIndex + "_clientsecret\" value=\"\" placeholder=\"" + (CodeEditable ? "" : "(unchanged)") + "\" autocomplete=\"new-password\" /></td>");
            Output.WriteLine("    </tr>");
        }

        /// <summary> Title for the page that displays this viewer, this is shown in the search box at the top of the page, just below the banner </summary>
        public override string Web_Title
        {
            get { return "OIDC Sign-In Settings"; }
        }

        /// <summary> Gets the URL for the icon related to this administrative task </summary>
        public override string Viewer_Icon
        {
            get { return Static_Resources_Gateway.Settings_Img; }
        }

        private class Provider_Row
        {
            public string Code { get; set; }
            public string Label { get; set; }
            public string Authority { get; set; }
            public string ClientId { get; set; }
        }
    }
}
