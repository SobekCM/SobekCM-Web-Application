using SobekCM.Core.Users;
using SobekCM.Engine_Library.Database;
using SobekCM.Library.TEI;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Caching;

namespace SobekCM.Library.AdminViewer.UserAdmin.UserAdminTabs
{
    class TeiUserAdminTab : iUserAdminTab
    {
        private TEI_Configuration teiConfig;

        public string TabName => "TEI Plug-In";

        private void getOrCreateTeiConfig()
        {
            // Try to pull the configuration from the cache, otherwise create it manually
            teiConfig = HttpContext.Current.Cache.Get("TEI.Configuration") as TEI_Configuration;

            // Did not find it in the cache
            if (teiConfig == null)
            {
                // Build the new object then
                string plugin_directory = Path.Combine(UI_ApplicationCache_Gateway.Settings.Servers.Application_Server_Network, "plugins", "tei");
                teiConfig = new TEI_Configuration(plugin_directory);

                // Store on the cache for several minutes
                HttpContext.Current.Cache.Insert("TEI.Configuration", teiConfig, null, Cache.NoAbsoluteExpiration, TimeSpan.FromMinutes(2));
            }
        }

        public bool HandlePostback(NameValueCollection form, User_Object editUser, RequestCache RequestSpecificValues)
        {
            getOrCreateTeiConfig();

            // First, check to see if TEI is enabled
            if (form["admin_user_tei_enabled"] == null)
            {
                // If the setting is already the same, no need to update the database
                if (editUser.Get_Setting("TEI.Enabled", "false") != "false")
                {
                    if (Engine_Database.Set_User_Setting(editUser.UserID, "TEI.Enabled", "false"))
                        editUser.Add_Setting("TEI.Enabled", "false");
                }
            }
            else
            {
                // If the setting is already the same, no need to update the database
                if (editUser.Get_Setting("TEI.Enabled", "false") != "true")
                {
                    if (Engine_Database.Set_User_Setting(editUser.UserID, "TEI.Enabled", "true"))
                        editUser.Add_Setting("TEI.Enabled", "true");
                }
            }

            // Now, look for XSLT file links
            foreach (string thisFileName in teiConfig.XSLT_Files)
            {
                // Look for this checkbox
                if (form["admin_user_tei_xslt_" + thisFileName.ToLower()] == null)
                {
                    // If the setting is already the same, no need to update the database
                    if (editUser.Get_Setting("TEI.XSLT." + thisFileName.ToUpper(), "false") != "false")
                    {
                        if (Engine_Database.Set_User_Setting(editUser.UserID, "TEI.XSLT." + thisFileName.ToUpper(), "false"))
                            editUser.Add_Setting("TEI.XSLT." + thisFileName.ToUpper(), "false");
                    }
                }
                else
                {
                    // If the setting is already the same, no need to update the database
                    if (editUser.Get_Setting("TEI.XSLT." + thisFileName.ToUpper(), "false") != "true")
                    {
                        if (Engine_Database.Set_User_Setting(editUser.UserID, "TEI.XSLT." + thisFileName.ToUpper(), "true"))
                            editUser.Add_Setting("TEI.XSLT." + thisFileName.ToUpper(), "true");
                    }
                }
            }

            // Look for CSS file links
            foreach (string thisFileName in teiConfig.CSS_Files)
            {
                // Look for this checkbox
                if (form["admin_user_tei_css_" + thisFileName.ToLower()] == null)
                {
                    // If the setting is already the same, no need to update the database
                    if (editUser.Get_Setting("TEI.CSS." + thisFileName.ToUpper(), "false") != "false")
                    {
                        if (Engine_Database.Set_User_Setting(editUser.UserID, "TEI.CSS." + thisFileName.ToUpper(), "false"))
                            editUser.Add_Setting("TEI.CSS." + thisFileName.ToUpper(), "false");
                    }
                }
                else
                {
                    // If the setting is already the same, no need to update the database
                    if (editUser.Get_Setting("TEI.CSS." + thisFileName.ToUpper(), "false") != "true")
                    {
                        if (Engine_Database.Set_User_Setting(editUser.UserID, "TEI.CSS." + thisFileName.ToUpper(), "true"))
                            editUser.Add_Setting("TEI.CSS." + thisFileName.ToUpper(), "true");
                    }
                }
            }

            // Look for mapping file links
            foreach (string thisFileName in teiConfig.Mapping_Files)
            {
                // Look for this checkbox
                if (form["admin_user_tei_mapping_" + thisFileName.ToLower()] == null)
                {
                    // If the setting is already the same, no need to update the database
                    if (editUser.Get_Setting("TEI.MAPPING." + thisFileName.ToUpper(), "false") != "false")
                    {
                        if (Engine_Database.Set_User_Setting(editUser.UserID, "TEI.MAPPING." + thisFileName.ToUpper(), "false"))
                            editUser.Add_Setting("TEI.MAPPING." + thisFileName.ToUpper(), "false");
                    }
                }
                else
                {
                    // If the setting is already the same, no need to update the database
                    if (editUser.Get_Setting("TEI.MAPPING." + thisFileName.ToUpper(), "false") != "true")
                    {
                        if (Engine_Database.Set_User_Setting(editUser.UserID, "TEI.MAPPING." + thisFileName.ToUpper(), "true"))
                            editUser.Add_Setting("TEI.MAPPING." + thisFileName.ToUpper(), "true");
                    }
                }
            }

            // No immediate save necesary
            return false;
        }

        public void RenderHtml(TextWriter Output, User_Object editUser, RequestCache RequestSpecificValues, Custom_Tracer Tracer)
        {
            getOrCreateTeiConfig();

            // Was this user enabled?
            bool enabled_for_tei = (String.Compare(editUser.Get_Setting("TEI.Enabled", "false"), "false", StringComparison.OrdinalIgnoreCase) != 0);

            Output.WriteLine("  <h2>TEI Permissions</h2>");
            Output.WriteLine("  <p>This tab is used to control permissions for this user for the TEI plug-in and different files under the TEI plug-in.</p>");
            Output.WriteLine("  <br /><br />");

            Output.WriteLine("  <span class=\"SobekEditItemSectionTitle_first\"> &nbsp; Global Permissions</span>");
            Output.WriteLine("    <p>For the user to have the option to upload or edit TEI files, this user must first be enabled to use the plug-in.</p>");
            Output.WriteLine(enabled_for_tei
             ? "    <input class=\"admin_user_checkbox_tei\" type=\"checkbox\" name=\"admin_user_tei_enabled\" id=\"admin_user_tei_enabled\" checked=\"checked\" /> <label for=\"admin_user_tei_enabled\">Enabled to use the TEI plug-in</label> <br /><br /><br />"
             : "    <input class=\"admin_user_checkbox_tei\" type=\"checkbox\" name=\"admin_user_tei_enabled\" id=\"admin_user_tei_enabled\" /> <label for=\"admin_user_tei_enabled\">Enabled to use the TEI plug-in</label> <br /><br /><br />");
            Output.WriteLine();

            // Add XSLT permissions
            Output.WriteLine("  <span class=\"SobekEditItemSectionTitle\"> &nbsp; XSLT Permissions</span>");
            if (teiConfig.XSLT_Files.Count == 0)
            {
                Output.WriteLine("    <p>There are no XSLT files in the system!</p>");
            }
            else
            {
                Output.WriteLine("    <p>Select the XSLT transform files this user has access to select for their items.</p>");

                foreach (string thisFile in teiConfig.XSLT_Files)
                {
                    // Determine if enabled
                    bool tei_xlst_enabled = (String.Compare(editUser.Get_Setting("TEI.XSLT." + thisFile.ToUpper(), "false"), "false", StringComparison.OrdinalIgnoreCase) != 0);

                    // Show check box
                    Output.WriteLine(tei_xlst_enabled
                        ? "    <input class=\"admin_user_checkbox_tei\" type=\"checkbox\" name=\"admin_user_tei_xslt_" + thisFile.ToLower() + "\" id=\"admin_user_tei_xslt_" + thisFile.ToLower() + "\" checked=\"checked\" /> <label for=\"admin_user_tei_xslt_" + thisFile.ToLower() + "\">" + thisFile + "</label> <br />"
                        : "    <input class=\"admin_user_checkbox_tei\" type=\"checkbox\" name=\"admin_user_tei_xslt_" + thisFile.ToLower() + "\" id=\"admin_user_tei_xslt_" + thisFile.ToLower() + "\" /> <label for=\"admin_user_tei_xslt_" + thisFile.ToLower() + "\">" + thisFile + "</label> <br />");

                }
            }
            Output.WriteLine("  <br /><br />");
            Output.WriteLine();

            // Add the CSS permissions
            Output.WriteLine("  <span class=\"SobekEditItemSectionTitle\"> &nbsp; CSS Permissions</span>");
            if (teiConfig.CSS_Files.Count == 0)
            {
                Output.WriteLine("    <p>There are no CSS files in the system!</p>");
            }
            else
            {
                Output.WriteLine("    <p>Select the CSS stylesheet files this user has access to select for their items.</p>");

                foreach (string thisFile in teiConfig.CSS_Files)
                {
                    // Determine if enabled
                    bool tei_css_enabled = (String.Compare(editUser.Get_Setting("TEI.CSS." + thisFile.ToUpper(), "false"), "false", StringComparison.OrdinalIgnoreCase) != 0);

                    // Show check box
                    Output.WriteLine(tei_css_enabled
                        ? "    <input class=\"admin_user_checkbox_tei\" type=\"checkbox\" name=\"admin_user_tei_css_" + thisFile.ToLower() + "\" id=\"admin_user_tei_css_" + thisFile.ToLower() + "\" checked=\"checked\" /> <label for=\"admin_user_tei_css_" + thisFile.ToLower() + "\">" + thisFile + "</label> <br />"
                        : "    <input class=\"admin_user_checkbox_tei\" type=\"checkbox\" name=\"admin_user_tei_css_" + thisFile.ToLower() + "\" id=\"admin_user_tei_css_" + thisFile.ToLower() + "\" /> <label for=\"admin_user_tei_css_" + thisFile.ToLower() + "\">" + thisFile + "</label> <br />");

                }
            }
            Output.WriteLine("  <br /><br />");
            Output.WriteLine();

            // Add the mappings permissions
            Output.WriteLine("  <span class=\"SobekEditItemSectionTitle\"> &nbsp; Mappings Permissions</span>");
            if (teiConfig.Mapping_Files.Count == 0)
            {
                Output.WriteLine("    <p>There are no mapping files in the system!</p>");
            }
            else
            {
                Output.WriteLine("    <p>Select the XML mappnig files this user has access to select for their items.</p>");

                foreach (string thisFile in teiConfig.Mapping_Files)
                {
                    // Determine if enabled
                    bool tei_mapping_enabled = (String.Compare(editUser.Get_Setting("TEI.MAPPING." + thisFile.ToUpper(), "false"), "false", StringComparison.OrdinalIgnoreCase) != 0);

                    // Show check box
                    Output.WriteLine(tei_mapping_enabled
                        ? "    <input class=\"admin_user_checkbox_tei\" type=\"checkbox\" name=\"admin_user_tei_mapping_" + thisFile.ToLower() + "\" id=\"admin_user_tei_mapping_" + thisFile.ToLower() + "\" checked=\"checked\" /> <label for=\"admin_user_mapping_xslt_" + thisFile.ToLower() + "\">" + thisFile + "</label> <br />"
                        : "    <input class=\"admin_user_checkbox_tei\" type=\"checkbox\" name=\"admin_user_tei_mapping_" + thisFile.ToLower() + "\" id=\"admin_user_tei_mapping_" + thisFile.ToLower() + "\" /> <label for=\"admin_user_tei_mapping_" + thisFile.ToLower() + "\">" + thisFile + "</label> <br />");

                }
            }
            Output.WriteLine("  <br />");
        }
    }
}
