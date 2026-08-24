using Microsoft.AspNetCore.Http;
using SobekCM.Core.Configuration.Extensions;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.Navigation;
using SobekCM.Engine_Library.Database;
using SobekCM.Library.MySobekViewer;
using SobekCM.Library.UI;
using SobekCM.Resource_Object;
using SobekCM.Resource_Object.Bib_Info;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace SobekCM.Library.AdminViewer
{
    /// <summary> Factory class returns the appropriate admin viewer </summary>
    /// <remarks> Core admin views are returned directly by the switch below, with zero reflection - this
    /// mirrors <c>ItemViewer_Factory.configurePrototyper</c>'s "known classes get a hardcoded fast path"
    /// design. A code not recognized there is looked up in the plugin-registered admin viewer registry
    /// (built from every enabled extension's <see cref="ExtensionInfo.AdminViewers"/>, e.g. an extension's
    /// <c>&lt;adminViewer code="" class="" assembly=""/&gt;</c> config element) and, if found, loaded via
    /// reflection - optionally from a plugin assembly resolved through
    /// <see cref="Extension_Configuration.Get_Assembly"/>, the same mechanism <c>ItemViewer_Factory</c> uses.
    /// A still-unrecognized code falls back to the admin home page. </remarks>
    public static class AdminViewer_Factory
    {
        private static Dictionary<string, ExtensionAdminViewerInfo> pluginAdminViewers;
        private static readonly object pluginAdminViewersLock = new object();
        /// <summary> Returns the appropriate admin viewer, based on requst and system settings </summary>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request  </param>
        /// <returns> Built admin viewer </returns>
        public static iMySobek_Admin_Viewer Get_AdminViewer(RequestCache RequestSpecificValues, HttpContext Context)
        {

            RequestSpecificValues.Tracer.Add_Trace("Admin_HtmlSubwriter.Get_AdminViewer", "Building the admin viewer object");
            switch (RequestSpecificValues.Current_Mode.Admin_Type)
            {
                case Admin_View_Codes.Add_Collection_Wizard:
                    return new Add_Collection_AdminViewer(RequestSpecificValues, Context);

                case Admin_View_Codes.Aggregation_Single:
                    return new Aggregation_Single_AdminViewer(RequestSpecificValues, Context);

                case Admin_View_Codes.Aggregations_Mgmt:
                    return new Aggregations_Mgmt_AdminViewer(RequestSpecificValues, Context);

                case Admin_View_Codes.Home:
                    return new Home_AdminViewer(RequestSpecificValues, Context);

                case Admin_View_Codes.Builder_Status:
                    return new Builder_AdminViewer(RequestSpecificValues, Context);

                case Admin_View_Codes.Builder_Folder_Mgmt:
                    return new Builder_Folder_Mgmt_AdminViewer(RequestSpecificValues, Context);

                case Admin_View_Codes.Skins_Single:
                    return new Skin_Single_AdminViewer(RequestSpecificValues, Context);

                case Admin_View_Codes.Skins_Mgmt:
                    return new Skins_AdminViewer(RequestSpecificValues, Context);

                case Admin_View_Codes.Aliases:
                    return new Aliases_AdminViewer(RequestSpecificValues, Context);

                case Admin_View_Codes.WebContent_Add_New:
                    return new WebContent_Add_New_AdminViewer(RequestSpecificValues, Context);

                case Admin_View_Codes.WebContent_Mgmt:
                    return new WebContent_Mgmt_AdminViewer(RequestSpecificValues, Context);

                case Admin_View_Codes.WebContent_History:
                    return new WebContent_History_AdminViewer(RequestSpecificValues, Context);

                case Admin_View_Codes.WebContent_Single:
                    return new WebContent_Single_AdminViewer(RequestSpecificValues, Context);

                case Admin_View_Codes.WebContent_Usage:
                    return new WebContent_Usage_AdminViewer(RequestSpecificValues, Context);

                case Admin_View_Codes.Wordmarks:
                    return new Wordmarks_AdminViewer(RequestSpecificValues, Context);

                case Admin_View_Codes.URL_Portals:
                    return new Portals_AdminViewer(RequestSpecificValues, Context);

                case Admin_View_Codes.Users:
                    return new Users_AdminViewer(RequestSpecificValues, Context);

                case Admin_View_Codes.User_Groups:
                    return new User_Group_AdminViewer(RequestSpecificValues, Context);

                case Admin_View_Codes.User_Permissions_Reports:
                    return new Permissions_Reports_AdminViewer(RequestSpecificValues, Context);

                case Admin_View_Codes.User_Requests:
                    return new User_Requests_AdminViewer(RequestSpecificValues, Context);

                case Admin_View_Codes.IP_Restrictions:
                    return new IP_Restrictions_AdminViewer(RequestSpecificValues, Context);

                case Admin_View_Codes.Thematic_Headings:
                    return new Thematic_Headings_AdminViewer(RequestSpecificValues, Context);

                case Admin_View_Codes.TEI:
                    return new TEI_PlugIn_AdminViewer(RequestSpecificValues, Context);

                // OIDC_Auth/SAML_Auth deliberately NOT cased here - their viewers live in separate plugin
                // assemblies (Plugins/oidc_auth, Plugins/saml_auth) and are resolved below via the
                // plugin-registered admin viewer lookup, proving that path actually works end-to-end
                // rather than being a parallel, never-exercised mechanism

                case Admin_View_Codes.Settings:
                    return new Settings_AdminViewer(RequestSpecificValues, Context);

                case Admin_View_Codes.Default_Metadata:
                    if ((!String.IsNullOrEmpty(RequestSpecificValues.Current_Mode.My_Sobek_SubMode)) && (RequestSpecificValues.Current_Mode.My_Sobek_SubMode.Length > 1))
                    {
                        string project_code = RequestSpecificValues.Current_Mode.My_Sobek_SubMode.Substring(1);
                        RequestSpecificValues.Tracer.Add_Trace("AdminViewer_Factory.Get_AdminViewer", "Checking cache for valid project file");
                        if (RequestSpecificValues.Current_User != null)
                        {
                            SobekCM_Item projectObject = CachedDataManager.Retrieve_Project(RequestSpecificValues.Current_User.UserID, project_code, RequestSpecificValues.Tracer);
                            if (projectObject != null)
                            {
                                RequestSpecificValues.Tracer.Add_Trace("AdminViewer_Factory.Get_AdminViewer", "Valid default metadata set found in cache");
                                return new Edit_Item_Metadata_MySobekViewer(projectObject, RequestSpecificValues, Context);
                            }
                            else
                            {
                                if (Engine_Database.Get_All_Template_DefaultMetadatas(RequestSpecificValues.Tracer).Tables[0].Select("MetadataCode='" + project_code + "'").Length > 0)
                                {
                                    RequestSpecificValues.Tracer.Add_Trace("AdminViewer_Factory.Get_AdminViewer", "Building default metadata set from (possible) PMETS");
                                    string pmets_file = UI_ApplicationCache_Gateway.Settings.Servers.Base_MySobek_Directory + "projects\\" + RequestSpecificValues.Current_Mode.My_Sobek_SubMode.Substring(1) + ".pmets";
                                    SobekCM_Item pmets_item = File.Exists(pmets_file) ? SobekCM_Item.Read_METS(pmets_file) : new SobekCM_Item();
                                    pmets_item.Bib_Info.Main_Title.Title = "Default metadata set for '" + project_code + "'";
                                    pmets_item.Bib_Info.SobekCM_Type = TypeOfResource_SobekCM_Enum.Project;
                                    pmets_item.BibID = project_code.ToUpper();
                                    pmets_item.VID = "00001";
                                    pmets_item.Source_Directory = UI_ApplicationCache_Gateway.Settings.Servers.Base_MySobek_Directory + "projects\\";

                                    RequestSpecificValues.Tracer.Add_Trace("AdminViewer_Factory.Get_AdminViewer", "Adding project file to cache");

                                    CachedDataManager.Store_Project(RequestSpecificValues.Current_User.UserID, project_code, pmets_item, RequestSpecificValues.Tracer);

                                    return new Edit_Item_Metadata_MySobekViewer(pmets_item, RequestSpecificValues, Context);
                                }
                            }
                        }
                    }

                    // If it made it here, it must be manage all the default metadatas
                    return new Default_Metadata_AdminViewer(RequestSpecificValues, Context);


            }

            // Not a core admin view - check the plugin-registered admin viewers
            string admin_type = RequestSpecificValues.Current_Mode.Admin_Type;
            if (!String.IsNullOrEmpty(admin_type))
            {
                Dictionary<string, ExtensionAdminViewerInfo> pluginViewers = configurePluginAdminViewers();
                if (pluginViewers.TryGetValue(admin_type, out ExtensionAdminViewerInfo pluginViewerInfo))
                {
                    iMySobek_Admin_Viewer pluginViewer = create_plugin_admin_viewer(pluginViewerInfo, RequestSpecificValues, Context);
                    if (pluginViewer != null)
                        return pluginViewer;
                }
            }

            // Unrecognized admin type code - fall back to the admin home page
            return new Home_AdminViewer(RequestSpecificValues, Context);
        }

        /// <summary> Instantiates a plugin-registered admin viewer via reflection, loading its assembly
        /// first if one was specified </summary>
        /// <returns> The built admin viewer, or NULL if the class/assembly could not be resolved </returns>
        private static iMySobek_Admin_Viewer create_plugin_admin_viewer(ExtensionAdminViewerInfo ViewerInfo, RequestCache RequestSpecificValues, HttpContext Context)
        {
            try
            {
                Assembly dllAssembly;
                if (String.IsNullOrEmpty(ViewerInfo.Assembly))
                {
                    dllAssembly = Assembly.GetExecutingAssembly();
                }
                else
                {
                    string assemblyFilePath = UI_ApplicationCache_Gateway.Configuration.Extensions.Get_Assembly(ViewerInfo.Assembly);
                    dllAssembly = (assemblyFilePath != null) ? Assembly.LoadFrom(assemblyFilePath) : null;
                }

                Type viewerType = dllAssembly?.GetType(ViewerInfo.Class);
                return (viewerType != null) ? (iMySobek_Admin_Viewer)Activator.CreateInstance(viewerType, RequestSpecificValues, Context) : null;
            }
            catch (Exception)
            {
                // Not sure exactly what to do here, honestly ( matches ItemViewer_Factory.configurePrototyper's
                // own handling of a plugin class/assembly that fails to resolve )
                return null;
            }
        }

        /// <summary> Builds the lookup of every plugin-registered admin viewer code, from every currently
        /// enabled extension's <see cref="ExtensionInfo.AdminViewers"/> list </summary>
        private static Dictionary<string, ExtensionAdminViewerInfo> configurePluginAdminViewers()
        {
            Dictionary<string, ExtensionAdminViewerInfo> lookup = pluginAdminViewers;
            if (lookup != null)
                return lookup;

            lock (pluginAdminViewersLock)
            {
                // Another thread may have already finished building this while this thread waited for the lock
                lookup = pluginAdminViewers;
                if (lookup != null)
                    return lookup;

                var newLookup = new Dictionary<string, ExtensionAdminViewerInfo>(StringComparer.OrdinalIgnoreCase);
                Extension_Configuration extensions = UI_ApplicationCache_Gateway.Configuration.Extensions;
                if ((extensions != null) && (extensions.Extensions != null))
                {
                    foreach (ExtensionInfo extension in extensions.Extensions)
                    {
                        if ((!extension.Enabled) || (extension.AdminViewers == null))
                            continue;

                        foreach (ExtensionAdminViewerInfo adminViewer in extension.AdminViewers)
                        {
                            if (!String.IsNullOrEmpty(adminViewer.Code))
                                newLookup[adminViewer.Code] = adminViewer;
                        }
                    }
                }

                pluginAdminViewers = newLookup;
                return newLookup;
            }
        }

        /// <summary> Clears the cached plugin admin viewer lookup, used when the cache is reset either
        /// manually or automatically ( mirrors <c>ItemViewer_Factory.Clear</c> ) </summary>
        public static void Clear()
        {
            lock (pluginAdminViewersLock)
            {
                pluginAdminViewers = null;
            }
        }
    }
}
