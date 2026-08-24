#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.Navigation;
using SobekCM.Engine_Library.ApplicationState;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Engine_Library.Database;
using SobekCM.Library;
using SobekCM.Library.AdminViewer;
using SobekCM.Library.HTML;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

#endregion

namespace SobekCM.Plugins.SamlAuth
{
    /// <summary> Class allows the host admin to configure the SAML identity providers available for this
    /// instance, entering each provider's Entity ID / IdP Entity ID / IdP Metadata URL through a GUI
    /// rather than a config file </summary>
    /// <remarks> This class extends the <see cref="abstract_AdminViewer"/> class. Settings entered here are
    /// stored in the database (see <see cref="Engine_Database.Set_Extension_Setting"/>), scoped to the
    /// "saml_auth" extension, using namespaced keys like "SAML|{Provider_Code}|EntityId" so more than one
    /// provider can be configured at once. Loaded via reflection by <c>AdminViewer_Factory</c>, registered
    /// through the "saml_auth" extension's &lt;adminViewer&gt; config element - not referenced anywhere in
    /// the core SobekCM_Library assembly. </remarks>
    public class Saml_Auth_AdminViewer : abstract_AdminViewer
    {
        private const string EXTENSION_CODE = "saml_auth";

        private string actionMessage;
        private List<Provider_Row> providerRows;

        /// <summary> Constructor for a new instance of the Saml_Auth_AdminViewer class </summary>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        /// <remarks> Postback from saving provider settings is handled here in the constructor </remarks>
        public Saml_Auth_AdminViewer(RequestCache RequestSpecificValues, HttpContext Context) : base(RequestSpecificValues, Context)
        {
            // Ensure the user is at least a system or portal admin
            if ((RequestSpecificValues.Current_User == null) || ((!RequestSpecificValues.Current_User.Is_System_Admin) && (!RequestSpecificValues.Current_User.Is_Portal_Admin)))
            {
                RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.My_Sobek;
                RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Home;
                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode, Context);
                return;
            }

            // Ensure the extension list exists and contains the SAML auth extension, and that it is enabled
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
            // include another organization's IdP details, not just this site's own configuration
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
                string action = form["saml_admin_action"].TrimFirst();
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
                        string code = form["saml_row_" + rowIndex + "_code"].TrimFirst();
                        if (String.IsNullOrEmpty(code))
                            continue;

                        string label = form["saml_row_" + rowIndex + "_label"].TrimFirst();
                        string entityId = form["saml_row_" + rowIndex + "_entityid"].TrimFirst();
                        string idpEntityId = form["saml_row_" + rowIndex + "_idpentityid"].TrimFirst();
                        string idpMetadataUrl = form["saml_row_" + rowIndex + "_idpmetadataurl"].TrimFirst();

                        Engine_Database.Set_Extension_Setting(EXTENSION_CODE, "SAML|" + code + "|DisplayLabel", label);
                        Engine_Database.Set_Extension_Setting(EXTENSION_CODE, "SAML|" + code + "|EntityId", entityId);
                        Engine_Database.Set_Extension_Setting(EXTENSION_CODE, "SAML|" + code + "|IdpEntityId", idpEntityId);
                        Engine_Database.Set_Extension_Setting(EXTENSION_CODE, "SAML|" + code + "|IdpMetadataUrl", idpMetadataUrl);
                    }

                    actionMessage = "All changes saved";

                    // Editing a live provider's settings has no effect until the ASP.NET Core
                    // AuthenticationBuilder options are re-read at next startup
                    AppLifetime_Gateway.RequestRestart("SAML provider settings changed");
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
                // Expected shape: "SAML|{Provider_Code}|{Field}"
                string[] parts = setting.Key.Split('|');
                if ((parts.Length != 3) || (parts[0] != "SAML"))
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

                    case "EntityId":
                        row.EntityId = setting.Value;
                        break;

                    case "IdpEntityId":
                        row.IdpEntityId = setting.Value;
                        break;

                    case "IdpMetadataUrl":
                        row.IdpMetadataUrl = setting.Value;
                        break;
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

            Output.WriteLine("<!-- Saml_Auth_AdminViewer.Write_HTML -->");
            Output.WriteLine("<input type=\"hidden\" id=\"saml_admin_action\" name=\"saml_admin_action\" value=\"\" />");
            Output.WriteLine("<script src=\"" + Static_Resources_Gateway.Sobekcm_Admin_Js + "\" type=\"text/javascript\"></script>");

            Output.WriteLine("<div class=\"sbkAdm_HomeText\">");
            if (!String.IsNullOrWhiteSpace(actionMessage))
            {
                Output.WriteLine("  <div id=\"sbkAdm_ActionMessageSuccess\">" + actionMessage + "</div>");
            }
            Output.WriteLine("  <p style=\"text-align: left; padding:0 20px 0 70px;width:800px;\">Configure the SAML identity providers users may sign in through, in addition to the built-in username/password logon. Each provider needs its Entity ID, the identity provider's own Entity ID, and the identity provider's metadata URL.</p>");
            Output.WriteLine("</div>");

            Output.WriteLine("  <table class=\"sbkAdm_Table\" id=\"sbkSamlAv_Table\">");
            Output.WriteLine("    <tr>");
            Output.WriteLine("      <th>Provider Code</th>");
            Output.WriteLine("      <th>Display Label</th>");
            Output.WriteLine("      <th>Entity ID</th>");
            Output.WriteLine("      <th>IdP Entity ID</th>");
            Output.WriteLine("      <th>IdP Metadata URL</th>");
            Output.WriteLine("    </tr>");

            int rowIndex = 0;
            foreach (Provider_Row row in providerRows)
            {
                write_provider_row(Output, rowIndex.ToString(), row.Code, row.Label, row.EntityId, row.IdpEntityId, row.IdpMetadataUrl, false);
                rowIndex++;
            }

            Output.WriteLine("    <tr><td class=\"sbkAdm_TableRule\" colspan=\"5\"></td></tr>");
            Output.WriteLine("    <tr><td colspan=\"5\"><h3>Add a New Provider</h3></td></tr>");
            write_provider_row(Output, "new", String.Empty, String.Empty, String.Empty, String.Empty, String.Empty, true);

            Output.WriteLine("  </table>");

            Output.WriteLine("  <div class=\"sbkSeav_ButtonsDiv\">");
            Output.WriteLine("    <button title=\"Save changes\" class=\"sbkAdm_RoundButton\" onclick=\"return set_hidden_value_postback('saml_admin_action', 'save');return false;\">SAVE</button>");
            Output.WriteLine("  </div>");

            Write_ItemNavForm_Closing(Output);
        }

        private void write_provider_row(TextWriter Output, string RowIndex, string Code, string Label, string EntityId, string IdpEntityId, string IdpMetadataUrl, bool CodeEditable)
        {
            Output.WriteLine("    <tr>");
            Output.WriteLine("      <td>" + (CodeEditable
                ? "<input type=\"text\" name=\"saml_row_" + RowIndex + "_code\" placeholder=\"e.g. institution_sso\" />"
                : "<input type=\"text\" name=\"saml_row_" + RowIndex + "_code\" value=\"" + Code + "\" readonly=\"readonly\" />") + "</td>");
            Output.WriteLine("      <td><input type=\"text\" name=\"saml_row_" + RowIndex + "_label\" value=\"" + Label + "\" /></td>");
            Output.WriteLine("      <td><input type=\"text\" name=\"saml_row_" + RowIndex + "_entityid\" value=\"" + EntityId + "\" size=\"30\" /></td>");
            Output.WriteLine("      <td><input type=\"text\" name=\"saml_row_" + RowIndex + "_idpentityid\" value=\"" + IdpEntityId + "\" size=\"30\" /></td>");
            Output.WriteLine("      <td><input type=\"text\" name=\"saml_row_" + RowIndex + "_idpmetadataurl\" value=\"" + IdpMetadataUrl + "\" size=\"40\" /></td>");
            Output.WriteLine("    </tr>");
        }

        /// <summary> Title for the page that displays this viewer, this is shown in the search box at the top of the page, just below the banner </summary>
        public override string Web_Title
        {
            get { return "SAML Sign-In Settings"; }
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
            public string EntityId { get; set; }
            public string IdpEntityId { get; set; }
            public string IdpMetadataUrl { get; set; }
        }
    }
}
