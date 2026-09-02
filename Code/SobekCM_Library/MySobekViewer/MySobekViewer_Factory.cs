using Microsoft.AspNetCore.Http;
using SobekCM.Core.Navigation;
using SobekCM.Library.AdminViewer;
using SobekCM.Library.UI;


namespace SobekCM.Library.MySobekViewer
{
    /// <summary> Factory class returns the appropriate mySobek viewer </summary>
    public static class MySobekViewer_Factory
    {
        /// <summary> Returns the appropriate mySobek viewer, based on requst and system settings </summary>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request  </param>
        /// <returns> Built mySobek viewer </returns>
        public static iMySobek_Admin_Viewer Get_MySobekViewer(RequestCache RequestSpecificValues, HttpContext Context)
        {
            RequestSpecificValues.Tracer.Add_Trace("MySobekViewer_Factory.Get_MySobekViewer", "Building the mySobek viewer object");

            My_Sobek_Type_Enum type = RequestSpecificValues.Current_Mode.My_Sobek_Type;

            // Server-side enforcement of "Can Submit Items Online" / "Can Submit Edit Online" -- this is the
            // single choke point every create/edit mySobek viewer is built through, so gating here (rather
            // than duplicating the check into every viewer's constructor) covers a direct URL to one of these
            // viewers too, not just the menu links that hide when the relevant flag is off.
            if ((Requires_Online_Submit(type) && !UI_ApplicationCache_Gateway.Settings.Resources.Online_Item_Submit_Enabled) ||
                (Requires_Online_Edit(type) && !UI_ApplicationCache_Gateway.Settings.Resources.Online_Item_Edit_Enabled))
            {
                Redirect_Disabled(RequestSpecificValues, Context);
                return null;
            }

            switch (RequestSpecificValues.Current_Mode.My_Sobek_Type)
            {
                case My_Sobek_Type_Enum.Import_Spreadsheet:
                    return new Import_Spreadsheet_MySobekViewer(RequestSpecificValues, Context);

                case My_Sobek_Type_Enum.Home:
                    return new Home_MySobekViewer(RequestSpecificValues, Context);

                case My_Sobek_Type_Enum.New_Item:
                    return new New_Submission_MySobekViewer(RequestSpecificValues, Context);

                case My_Sobek_Type_Enum.New_TEI_Item:
                    // New_TEI_MySobekViewer is retired (excluded from the project) -- TEI submission is
                    // now just another Type in the same wizard as New_Item
                    return new New_Submission_MySobekViewer(RequestSpecificValues, Context);

                case My_Sobek_Type_Enum.Folder_Management:
                    return new Folder_Mgmt_MySobekViewer(RequestSpecificValues, Context);

                case My_Sobek_Type_Enum.Saved_Searches:
                    return new Saved_Searches_MySobekViewer(RequestSpecificValues, Context);

                case My_Sobek_Type_Enum.Preferences:
                    return new Preferences_MySobekViewer(RequestSpecificValues, Context);

                case My_Sobek_Type_Enum.Process:
                    return new Process_mySobekViewer(RequestSpecificValues, Context);

                case My_Sobek_Type_Enum.Register:
                    if (UI_ApplicationCache_Gateway.URL_Portals.Default_Portal.Abbreviation.Equals("OpenNJ", System.StringComparison.OrdinalIgnoreCase))
                        return new OpenNJ_Register_MySobekViewer(RequestSpecificValues, Context);
                    else
                        return new Register_MySobekViewer(RequestSpecificValues, Context);

                case My_Sobek_Type_Enum.Logon:
                    return new Logon_MySobekViewer(RequestSpecificValues, Context);

                case My_Sobek_Type_Enum.OIDC_Landing:
                    return new Oidc_Landing_MySobekViewer(RequestSpecificValues, Context);

                case My_Sobek_Type_Enum.SAML_Landing:
                    return new Saml_Landing_MySobekViewer(RequestSpecificValues, Context);

                case My_Sobek_Type_Enum.New_Password:
                    return new NewPassword_MySobekViewer(RequestSpecificValues, Context);

                case My_Sobek_Type_Enum.Delete_Item:
                    return new Delete_Item_MySobekViewer(RequestSpecificValues, Context);

                case My_Sobek_Type_Enum.Edit_Item_Behaviors:
                    return new Edit_Item_Behaviors_MySobekViewer(RequestSpecificValues, Context);

                case My_Sobek_Type_Enum.Edit_Item_Metadata:
                    return new Edit_Item_Metadata_MySobekViewer(null, RequestSpecificValues, Context);

                case My_Sobek_Type_Enum.Edit_TEI_Item:
                    // Edit_TEI_Item_MySobekViewer is retired (excluded from the project) -- editing a
                    // TEI item's ordinary metadata now falls through to the same generic editor every
                    // other Type uses; the re-upload/mapping/XSLT/CSS reselection it used to own belongs
                    // in its own specialized screen, not built yet
                    return new Edit_Item_Metadata_MySobekViewer(null, RequestSpecificValues, Context);

                case My_Sobek_Type_Enum.Edit_Item_Permissions:
                    return new Edit_Item_Permissions_MySobekViewer(RequestSpecificValues, Context);

                case My_Sobek_Type_Enum.File_Management:
                    return new File_Management_MySobekViewer(RequestSpecificValues, Context);

                case My_Sobek_Type_Enum.Edit_Group_Behaviors:
                    return new Edit_Group_Behaviors_MySobekViewer(RequestSpecificValues, Context);

                case My_Sobek_Type_Enum.Edit_Group_Serial_Hierarchy:
                    return new Edit_Serial_Hierarchy_MySobekViewer(RequestSpecificValues, Context);

                case My_Sobek_Type_Enum.Item_Tracking:
                    return new Track_Item_MySobekViewer(RequestSpecificValues, Context);

                case My_Sobek_Type_Enum.Group_Add_Volume:
                    return new Group_Add_Volume_MySobekViewer(RequestSpecificValues, Context);

                case My_Sobek_Type_Enum.Group_AutoFill_Volumes:
                    return new Group_AutoFill_Volume_MySobekViewer(RequestSpecificValues, Context);

                case My_Sobek_Type_Enum.Group_Mass_Update_Items:
                    return new Mass_Update_Items_MySobekViewer(RequestSpecificValues, Context);

                case My_Sobek_Type_Enum.Open_Publishing_Tool:
                    return new Edit_Item_OpenPublisher_MySobekViewer(RequestSpecificValues, Context);

                case My_Sobek_Type_Enum.Page_Images_Management:
                    return new Page_Image_Upload_MySobekViewer(RequestSpecificValues, Context);

                case My_Sobek_Type_Enum.Rights_Management:
                    return new Rights_Management_MySobekViewer(RequestSpecificValues, Context);

                case My_Sobek_Type_Enum.User_Tags:
                    return new User_Tags_MySobekViewer(RequestSpecificValues, Context);

                case My_Sobek_Type_Enum.User_Usage_Stats:
                    return new User_Usage_Stats_MySobekViewer(RequestSpecificValues, Context);
            }

            return null;
        }

        /// <summary> Viewer types that create a brand-new item or volume -- gated by "Can Submit Items Online" </summary>
        private static bool Requires_Online_Submit(My_Sobek_Type_Enum Type)
        {
            switch (Type)
            {
                case My_Sobek_Type_Enum.New_Item:
                case My_Sobek_Type_Enum.New_TEI_Item:
                case My_Sobek_Type_Enum.Group_Add_Volume:
                case My_Sobek_Type_Enum.Group_AutoFill_Volumes:
                case My_Sobek_Type_Enum.Import_Spreadsheet:
                    return true;

                default:
                    return false;
            }
        }

        /// <summary> Viewer types that change an existing item -- gated by "Can Submit Edit Online" </summary>
        private static bool Requires_Online_Edit(My_Sobek_Type_Enum Type)
        {
            switch (Type)
            {
                case My_Sobek_Type_Enum.Delete_Item:
                case My_Sobek_Type_Enum.Edit_Item_Behaviors:
                case My_Sobek_Type_Enum.Edit_Item_Metadata:
                case My_Sobek_Type_Enum.Edit_TEI_Item:
                case My_Sobek_Type_Enum.Edit_Item_Permissions:
                case My_Sobek_Type_Enum.File_Management:
                case My_Sobek_Type_Enum.Edit_Group_Behaviors:
                case My_Sobek_Type_Enum.Edit_Group_Serial_Hierarchy:
                case My_Sobek_Type_Enum.Group_Mass_Update_Items:
                case My_Sobek_Type_Enum.Open_Publishing_Tool:
                case My_Sobek_Type_Enum.Page_Images_Management:
                case My_Sobek_Type_Enum.Rights_Management:
                case My_Sobek_Type_Enum.Import_Spreadsheet:
                    return true;

                default:
                    return false;
            }
        }

        /// <summary> Redirects to the configured "Disabled Online Changes Link", or the site's main home
        /// page if that link isn't configured </summary>
        private static void Redirect_Disabled(RequestCache RequestSpecificValues, HttpContext Context)
        {
            string link = UI_ApplicationCache_Gateway.Settings.Resources.Disabled_Online_Changes_Link;
            Context.Response.Redirect(string.IsNullOrWhiteSpace(link) ? RequestSpecificValues.Current_Mode.Base_URL : link);
        }
    }
}
