#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.Aggregations;
using SobekCM.Core.ApplicationState;
using SobekCM.Core.Client;
using SobekCM.Core.Items;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.Navigation;
using SobekCM.Core.Results;
using SobekCM.Core.SiteMap;
using SobekCM.Core.Skins;
using SobekCM.Core.Users;
using SobekCM.Core.WebContent;
using SobekCM.Engine_Library;
using SobekCM.Library;
using SobekCM.Library.MainWriters;
using SobekCM.Library.UI;
using SobekCM.QueryInitializerHelpers;
using SobekCM.Resource_Object;
using SobekCM.Resource_Object.Divisions;
using SobekCM.Tools;
using System;
using System.Collections.Generic;

#endregion

namespace SobekCM
{
    public class QueryInitializer
    {
        #region Private class members

        public string browse_info_display_text;
        public SobekCM_Item currentItem;
        public Page_TreeNode currentPage;
        public User_Object currentUser;
        public Item_Aggregation topLevelCollection;
        public Web_Skin_Object htmlSkin;
        public SobekCM_Items_In_Title itemsInTitle;

        public List<iSearch_Title_Result> pagedSearchResults;
        public Public_User_Folder publicFolder;
        public Search_Results_Statistics searchResultStatistics;
        public SobekCM_SiteMap siteMap;
        public HTML_Based_Content staticWebContent;
        public RequestCache requestSpecificValues;


        public abstractMainWriter mainWriter;
        public HttpContext context;

        public Custom_Tracer tracer => requestSpecificValues.Tracer;
        public Navigation_Object currentMode => requestSpecificValues.Current_Mode;


        #endregion

        #region Constructor for this class

        public QueryInitializer(HttpContext context, string page_name)
        {
            this.context = context;

            requestSpecificValues = new RequestCache(context)
            {
                Page_Name = page_name,
                Tracer = new Custom_Tracer()
            };

            // Start the tracter
            tracer.Add_Trace("QueryInitializer.Constructor", "Starting");

            // Get the user IP address
            new UserIpInitializer().Initialize(context, requestSpecificValues, tracer);

            // Add the request url and base url to the request cache
            new UrlInitializer().Initialize(context, requestSpecificValues, tracer);

            // Setup the db connection, if not already setup, from the sobekcm.config file
            var result = new DatabaseConnectionInitializer().Initialize(context, requestSpecificValues, tracer);

            if (!result.Success)
            {
                handle_error(result, context);
                return;
            }

            // Determine the base directory and related information (including IP) for the server 
            // Special code for DEBUG mode
            result = new ServerDirectoryInitializer().Initialize(context, requestSpecificValues, tracer);

            if (!result.Success)
            {
                handle_error(result, context);
                return;
            }

            // Parse the URL for the the navigation requested and create the current mode object
            result = new NavigationObjectInitializer().Initialize(context, requestSpecificValues, tracer);

            if (!result.Success)
            {
                handle_error(result, context);
                return;
            }

            tracer.Add_Trace("QueryInitializer.Constructor", "Navigation Object created from URI query string");

            // Parse the URL for the the navigation requested and create the current mode object
            result = new SearchEngineRobotNavigationInitializer().Initialize(context, requestSpecificValues, tracer);

            if (!result.Success)
            {
                handle_error(result, context);
                return;
            }
            else if (!String.IsNullOrEmpty(result.RedirectUrl))
            {
                context.Response.Redirect(result.RedirectUrl, false);
                currentMode.Request_Completed = true;
                return;
            }

            result = new UserObjectInitializer().Initialize(context, requestSpecificValues, tracer);

            if (!result.Success)
            {
                handle_error(result, context);
                return;
            }
            else if (!String.IsNullOrEmpty(result.RedirectUrl))
            {
                context.Response.Redirect(result.RedirectUrl, false);
                currentMode.Request_Completed = true;
                return;
            }


            try
            {
                if (!currentMode.Is_Robot)
                    if (currentMode.Request_Completed)
                        return;

                // If this was a call for RESET, clear the memory
                if ((currentMode.Mode == Display_Mode_Enum.Administrative) && (currentMode.Admin_Type == Admin_Type_Enum.Reset))
                {
                    Reset_Memory();

                    // Since this reset, send to the admin, memory management portion
                    currentMode.Mode = Display_Mode_Enum.Internal;
                    currentMode.Internal_Type = Internal_Type_Enum.Cache;
                }

                // Always pull TOP level collection
                SobekEngineClient.Aggregations.Get_Aggregation("all", currentMode.Language, (UI_ApplicationCache_Gateway.Configuration.Languages.Default_Language?.Code ?? "en"), tracer);

                // If this is for a public folder, get the data
                if (currentMode.Mode == Display_Mode_Enum.Public_Folder)
                {
                    Public_Folder();
                }

                // Was this a robot?
                if (currentMode.Request_Completed)
                    return;

                // Run the search if this should be done now
                if (currentMode.Mode == Display_Mode_Enum.Results)
                {
                    Search_Block();
                }

                if (currentMode.Mode == Display_Mode_Enum.My_Sobek)
                {
                    MySobekCM_Block();
                }

                // Run the simple text block if this is that mode
                if (currentMode.Mode == Display_Mode_Enum.Simple_HTML_CMS)
                {
                    Simple_Web_Content_Text_Block();
                }
            }
            catch (OutOfMemoryException ee)
            {
                if (currentMode != null)
                {
                    currentMode.Mode = Display_Mode_Enum.Error;
                    currentMode.Error_Message = "Out of memory exception caught";
                    currentMode.Caught_Exception = ee;
                }
                else
                {
                    Email_Information("Fatal Out of memory exception caught", ee);
                }
            }
            catch (Exception ee)
            {
                if (currentMode != null)
                {
                    currentMode.Mode = Display_Mode_Enum.Error;
                    currentMode.Error_Message = "Unknown error occurred";
                    currentMode.Caught_Exception = ee;
                }
                else
                {
                    Email_Information("Unknown Fatal Error Occurred", ee);
                }
            }
        }

        private void handle_error(QueryInitializerHelperResponse response, HttpContext context)
        {
            // Get user IP to determine if they should be sent to the dashboard or not
            string userip = context.Items[RequestCache_Keys.UserIP].ToString();

            // Wrap this into the SobekCM Exception
            var newException = new SobekCM_Traced_Exception(response.Message, response.InnerException, tracer);

            // Save this to the session state
            context.SessionObject()[SessionCache_Keys.LastException] = newException;

            // Forward to the dashboard if appropriate
            if (userip == "127.0.0.1" || userip == "::1" || context.Request.Host.ToString().IndexOf("localhost") >= 0)
            {
                context.Response.Redirect("dashboard.aspx", false);
                return;
            }

            if (!String.IsNullOrEmpty(response.RedirectUrl)) context.Response.Redirect(response.RedirectUrl, false);

            throw newException;
        }

        #endregion

        #region Method called during Page Load

        public void On_Page_Load()
        {
            if ((currentMode != null) && (!currentMode.Request_Completed))
            {
                // If this is not a post back, log it
                if (!currentMode.isPostBack)
                {
                    tracer.Add_Trace("QueryInitializer.Constructor.On_Page_Load", String.Empty);
                }

                Set_Main_Writer();
            }
        }

        #endregion

        public void Set_Main_Writer()
        {
            // If this is for HTML or HTML logged in, try to get the web skin object
            string current_skin_code = currentMode.Skin.ToUpper();
            if ((currentMode.Writer_Type == Writer_Type_Enum.HTML) || (currentMode.Writer_Type == Writer_Type_Enum.HTML_LoggedIn))
            {
                // Check if a different skin should be used if this is an item display
                if ((currentMode.Mode == Display_Mode_Enum.Item_Display) || (currentMode.Mode == Display_Mode_Enum.Item_Print))
                {
                    if ((currentItem != null) && (currentItem.Behaviors.Web_Skin_Count > 0))
                    {
                        if (!currentItem.Behaviors.Web_Skins.Contains(current_skin_code))
                        {
                            string new_skin_code = currentItem.Behaviors.Web_Skins[0];
                            current_skin_code = new_skin_code;
                        }
                    }
                }
            }

            //      // Build the RequestCache object
            //RequestCache RequestSpecificValues = new RequestCache(currentMode, searchResultStatistics, pagedSearchResults, currentUser, publicFolder, topLevelCollection, tracer);

            if ((currentMode.Writer_Type == Writer_Type_Enum.HTML) || (currentMode.Writer_Type == Writer_Type_Enum.HTML_LoggedIn))
            {
                mainWriter = new Html_MainWriter(context, requestSpecificValues);
            }

            // Load the OAI writer
            if (currentMode.Writer_Type == Writer_Type_Enum.OAI)
            {
                mainWriter = new Oai_MainWriter(context, requestSpecificValues.QueryString, requestSpecificValues);
            }

            // Load the DataSet writer
            if (currentMode.Writer_Type == Writer_Type_Enum.DataSet)
            {
                mainWriter = new Dataset_MainWriter(context, requestSpecificValues);
            }

            // Load the DataProvider writer
            if (currentMode.Writer_Type == Writer_Type_Enum.Data_Provider)
            {
                mainWriter = new DataProvider_MainWriter(context, requestSpecificValues);
            }

            // Load the XML writer
            if (currentMode.Writer_Type == Writer_Type_Enum.XML)
            {
                mainWriter = new Xml_MainWriter(context, requestSpecificValues);
            }

            // Load the JSON writer
            if (currentMode.Writer_Type == Writer_Type_Enum.JSON)
            {
                mainWriter = new Json_MainWriter(context, requestSpecificValues, UI_ApplicationCache_Gateway.Settings.Servers.Image_URL);
            }

            // Load the IIIF writer
            if (currentMode.Writer_Type == Writer_Type_Enum.IIIF)
            {
                mainWriter = new IIIF_MainWriter(context, requestSpecificValues);
            }

            // Load the HTML ECHO writer
            if (currentMode.Writer_Type == Writer_Type_Enum.HTML_Echo)
            {
                mainWriter = new Html_Echo_MainWriter(context, requestSpecificValues, browse_info_display_text);
            }

            // Default to HTML
            if (mainWriter == null)
            {
                mainWriter = new Html_MainWriter(context, requestSpecificValues);
            }
        }

        #region Block for displaying a public folder

        private void Public_Folder()
        {
            tracer.Add_Trace("QueryInitializer.Public_Folder", "Retrieving public folder information and browse");

            var assistant = new SobekCM_Assistant();
            int currentPageIndex = currentMode.Page.HasValue ? currentMode.Page.Value : 1;
            int currentFolderId = currentMode.FolderID.HasValue ? currentMode.FolderID.Value : -1;
            bool result = assistant.Get_Public_User_Folder(currentFolderId, currentPageIndex, currentMode.Language, tracer, out publicFolder, out searchResultStatistics, out pagedSearchResults);

            if ((!result) || (!publicFolder.IsPublic))
            {
                currentMode.Error_Message = "Invalid or private bookshelf";
                currentMode.Mode = Display_Mode_Enum.Error;
            }
        }

        #endregion

        #region Block for displaying simple text with an interface

        private void Simple_Web_Content_Text_Block()
        {
            tracer.Add_Trace("QueryInitializer.Simple_Web_Content_Text_Block", "Retrieiving Simple Web Content Object");

            var assistant = new SobekCM_Assistant();
            if (!assistant.Get_Simple_Web_Content_Text(currentMode, UI_ApplicationCache_Gateway.Settings.Servers.Base_Directory, tracer,
                                                       out staticWebContent, out siteMap))
            {
                currentMode.Mode = Display_Mode_Enum.Error;
                return;
            }

            // IF this is display mode and this is a redirect, do the redirect
            if ((currentMode.Mode == Display_Mode_Enum.Simple_HTML_CMS) && (currentMode.WebContent_Type == WebContent_Type_Enum.Display) && (staticWebContent != null) && (!String.IsNullOrEmpty(staticWebContent.Redirect)))
            {
                currentMode.Request_Completed = true;
                context.Response.Redirect(staticWebContent.Redirect, false);
                return;
            }

            // If the web skin is indicated in the browse file, set that
            if (!String.IsNullOrEmpty(staticWebContent.Web_Skin))
            {
                currentMode.Default_Skin = staticWebContent.Web_Skin;
                currentMode.Skin = staticWebContent.Web_Skin;
            }
        }

        #endregion

        #region Block for searching

        private void Search_Block()
        {
            tracer.Add_Trace("QueryInitializer.Search_Block", "Retreiving search results");

            // Here just pull the hierarchy object then (later this will be pused out of here)
            Item_Aggregation hierarchyObject = SobekEngineClient.Aggregations.Get_Aggregation(currentMode.Aggregation, currentMode.Language, (UI_ApplicationCache_Gateway.Configuration.Languages.Default_Language?.Code ?? "en"), tracer);


            try
            {
                // If there is no search term, forward back to the collection
                if ((String.IsNullOrEmpty(currentMode.Search_String)) && (String.IsNullOrEmpty(currentMode.Coordinates)))
                {
                    currentMode.Mode = Display_Mode_Enum.Aggregation;
                    currentMode.Aggregation_Type = Aggregation_Type_Enum.Home;
                    UrlWriterHelper.Redirect(currentMode, context);
                    return;
                }

                var assistant = new SobekCM_Assistant();
                assistant.Get_Search_Results(currentMode, hierarchyObject, UI_ApplicationCache_Gateway.Search_Stop_Words, currentUser, tracer, out searchResultStatistics, out pagedSearchResults, context);

                requestSpecificValues.Results_Statistics = searchResultStatistics;
                requestSpecificValues.Paged_Results = pagedSearchResults;

                if ((!currentMode.isPostBack) && (UI_ApplicationCache_Gateway.Search_History != null))
                {
                    string userAddress = context.Items[RequestCache_Keys.UserIP].ToString();
                    UI_ApplicationCache_Gateway.Search_History.Add_New_Search(Get_Search_From_Mode(currentMode, userAddress, currentMode.Search_Type, hierarchyObject.Name, currentMode.Search_String));
                }
            }
            catch (Exception ee)
            {
                currentMode.Mode = Display_Mode_Enum.Error;
                currentMode.Error_Message = "Unable to perform search at this time ";
                if (hierarchyObject == null)
                    currentMode.Error_Message = "Unable to perform search - hierarchyObject = null";
                currentMode.Caught_Exception = ee;
            }
        }

        private Recent_Searches.Search Get_Search_From_Mode(Navigation_Object currentMode, string SessionIP, Search_Type_Enum Search_Type, string Aggregation, string Search_Terms)
        {
            var returnValue = new Recent_Searches.Search();

            returnValue.Time = DateTime.Now.ToShortDateString().Replace("/", "-") + " " + DateTime.Now.ToShortTimeString().Replace(" ", "");

            // Save some of the values
            returnValue.SessionIP = SessionIP;
            switch (Search_Type)
            {
                case Search_Type_Enum.Advanced:
                    returnValue.Search_Type = "Advanced";
                    break;

                case Search_Type_Enum.Basic:
                    returnValue.Search_Type = "Basic";
                    break;

                case Search_Type_Enum.Newspaper:
                    returnValue.Search_Type = "Newspaper";
                    break;

                case Search_Type_Enum.Map:
                    returnValue.Search_Type = "Map";
                    break;

                default:
                    returnValue.Search_Type = "Unknown";
                    break;
            }

            // Save the collection as a link
            Display_Mode_Enum lastMode = currentMode.Mode;
            currentMode.Mode = Display_Mode_Enum.Aggregation;
            currentMode.Aggregation_Type = Aggregation_Type_Enum.Home;
            returnValue.Aggregation = "<a href=\"" + UrlWriterHelper.Redirect_URL(currentMode) + "\">" + Aggregation.Replace("&", "&amp;").Replace("\"", "&quot;") + "</a>";

            // Save the search terms as a link to the search
            currentMode.Mode = lastMode;
            returnValue.Search_Terms = "<a href=\"" + UrlWriterHelper.Redirect_URL(currentMode) + "\">" + Search_Terms.Replace("&", "&amp;").Replace("\"", "&quot;") + "</a>";

            return returnValue;
        }

        #endregion

        #region Block for MySobek

        private void MySobekCM_Block()
        {
            if ((currentMode.My_Sobek_Type == My_Sobek_Type_Enum.Folder_Management) && (requestSpecificValues.Current_User != null) && (!String.IsNullOrEmpty(currentMode.My_Sobek_SubMode)))
            {
                tracer.Add_Trace("QueryInitializer.MySobekCM_Block", "Retrieiving Browse/Info Object");

                // For EXPORT option, include ALL the items
                int results_per_page = 20;
                int current_page = currentMode.Page.HasValue ? currentMode.Page.Value : 1;
                if (String.Equals(currentMode.Result_Display_Type, "export", StringComparison.OrdinalIgnoreCase))
                {
                    results_per_page = 10000;
                    current_page = 1;
                }

                // Get the folder
                var assistant = new SobekCM_Assistant();
                if (!assistant.Get_User_Folder(currentMode.My_Sobek_SubMode, requestSpecificValues.Current_User.UserID, results_per_page, current_page, currentMode.Language, tracer, out searchResultStatistics, out pagedSearchResults))
                {
                    currentMode.Mode = Display_Mode_Enum.Error;
                }

                requestSpecificValues.Results_Statistics = searchResultStatistics;
                requestSpecificValues.Paged_Results = pagedSearchResults;
            }
        }


        #endregion

        #region Methods to reset the memory and the item cache

        private void Reset_Memory()
        {
            tracer.Add_Trace("QueryInitializer.Reset_Memory", "Clearing cache and application of data");

            // Clear the cache
            CachedDataManager.Clear_Cache();

            // Clear the application portions as well
            SobekCM_Application.State.RemoveAll();

            // Refresh the application settings
            UI_ApplicationCache_Gateway.ResetSettings();

            UI_ApplicationCache_Gateway.ResetAll();

            MicroserviceHandler.Clear();

            // Since this reset, send to the admin, memory management portion
            currentMode.Mode = Display_Mode_Enum.Internal;
            currentMode.Internal_Type = Internal_Type_Enum.Cache;
        }

        #endregion

        #region Method to email information during an error

        public void Email_Information(string EmailTitle, Exception ObjErr)
        {
            Email_Information(EmailTitle, ObjErr, true);
        }

        public void Email_Information(string EmailTitle, Exception ObjErr, bool Redirect)
        {
            //try
            //{
            //	StreamWriter writer = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "\\temp\\exceptions.txt", true);
            //	writer.WriteLine();
            //	writer.WriteLine("Error logged in SobekCM_Page_Globals.Email_Information ( " + DateTime.Now.ToString() + ")");
            //	writer.WriteLine("User Host Address: " + HttpContext.Current.Request.UserHostAddress);
            //	writer.WriteLine("Requested URL: " + HttpContext.Current.Request.Url);
            //	if (ObjErr is SobekCM_Traced_Exception)
            //	{
            //		SobekCM_Traced_Exception sobekException = (SobekCM_Traced_Exception) ObjErr;

            //		writer.WriteLine("Error Message: " + sobekException.InnerException.Message);
            //		writer.WriteLine("Stack Trace: " + ObjErr.StackTrace);
            //		writer.WriteLine("Error Message:" + sobekException.InnerException.StackTrace);
            //		writer.WriteLine();
            //		writer.WriteLine(sobekException.Trace_Route);
            //	}
            //	else
            //	{

            //		writer.WriteLine("Error Message: " + ObjErr.Message);
            //		writer.WriteLine("Stack Trace: " + ObjErr.StackTrace);
            //	}

            //	writer.WriteLine();
            //	writer.WriteLine("------------------------------------------------------------------");
            //	writer.Flush();
            //	writer.Close();
            //}
            //catch (Exception)
            //{
            //	// Already catching errors.. nothing else to realy do here if this causes an error as well
            //}

            //try
            //{
            //	// Build the error message
            //	string err;
            //	if (ObjErr != null)
            //	{
            //                 string referrer = (HttpContext.Current.Request.UrlReferrer != null) ? "URL Referred from: " + HttpContext.Current.Request.UrlReferrer + "<br />" : String.Empty;

            //                 err = "<b>" + HttpContext.Current.Request.UserHostAddress + "</b><br /><br />" +
            //		      "Error in: " + HttpContext.Current.Request.Url + "<br />" +
            //                       referrer +
            //                       "Error Message: " + ObjErr.Message + "<br /><br />" +
            //		      "Stack Trace: " + ObjErr.StackTrace.Replace("\r", "<br />") + "<br /><br />";

            //		if (ObjErr.Message.IndexOf("Timeout expired") >= 0)
            //			EmailTitle = "Database Timeout Expired";
            //	}
            //	else
            //	{
            //		err = "<b>" + HttpContext.Current.Request.UserHostAddress + "</b><br /><br />" +
            //		      "Error in: " + HttpContext.Current.Request.Url + "<br />" +
            //		      "Error Message: " + EmailTitle;
            //	}

            //             Email_Helper.SendEmail(UI_ApplicationCache_Gateway.Settings.Email.System_Error_Email, EmailTitle, err, true, String.Empty);
            //}
            //catch (Exception)
            //{
            //	// Already catching errors.. nothing else to realy do here if this causes an error as well
            //}

            //// Forward to our error message
            //if (Redirect)
            //{
            //	// Forward to our error message
            //             HttpContext.Current.Response.Redirect(UI_ApplicationCache_Gateway.Settings.Servers.System_Error_URL, false);
            //	HttpContext.Current.ApplicationInstance.CompleteRequest();
            //	if (currentMode != null)
            //		currentMode.Request_Completed = true;
            //}
        }

        private void send_error_email()
        {
            //try
            //{
            //	// Start the body
            //	StringBuilder builder = new StringBuilder();
            //	builder.Append("\n\nSUBMISSION INFORMATION\n");
            //	builder.Append("\tDate:\t\t\t\t" + DateTime.Now.ToString() + "\n");
            //	builder.Append("\tIP Address:\t\t\t" + HttpContext.Current.Request.UserHostAddress + "\n");
            //	builder.Append("\tHost Name:\t\t\t" + HttpContext.Current.Request.UserHostName + "\n");
            //	builder.Append("\tBrowser:\t\t\t" + HttpContext.Current.Request.Browser.Browser + "\n");
            //	builder.Append("\tBrowser Platform:\t\t" + HttpContext.Current.Request.Browser.Platform + "\n");
            //	builder.Append("\tBrowser Version:\t\t" + HttpContext.Current.Request.Browser.Version + "\n");
            //	builder.Append("\tBrowser Language:\t\t");
            //	bool first = true;
            //	string[] languages = HttpContext.Current.Request.UserLanguages;
            //	if (languages != null)
            //	{
            //		foreach (string thisLanguage in languages)
            //		{
            //			if (first)
            //			{
            //				builder.Append(thisLanguage);
            //				first = false;
            //			}
            //			else
            //			{
            //				builder.Append(", " + thisLanguage);
            //			}
            //		}
            //	}

            //	builder.AppendLine("HISTORY");
            //	if (HttpContext.Current.Session["LastSearch"] != null)
            //		builder.AppendLine("\tLast Search:\t\t" + HttpContext.Current.Session["LastSearch"]);
            //	if (HttpContext.Current.Session["LastResults"] != null)
            //		builder.AppendLine("\tLast Results:\t\t" + HttpContext.Current.Session["LastResults"]);
            //	if (HttpContext.Current.Session["Last_Mode"] != null)
            //		builder.AppendLine("\tLast Mode:\t\t\t?" + HttpContext.Current.Session["Last_Mode"]);
            //	if (HttpContext.Current.Items.Contains("Original_URL"))
            //		builder.AppendLine("\tURL:\t\t\t\t" + HttpContext.Current.Items["Original_URL"]);
            //	else
            //		builder.AppendLine("\tURL:\t\t\t\t" + HttpContext.Current.Request.Url);

            //	// Send this email
            //	try
            //	{
            //                 Email_Helper.SendEmail(UI_ApplicationCache_Gateway.Settings.Email.System_Error_Email, "SobekCM Exception Caught  [Invalid Item Requested]", builder.ToString(), false, String.Empty);
            //	}
            //	catch (Exception)
            //	{
            //		// Already catching errors.. nothing else to realy do here if this causes an error as well
            //	}

            //}
            //catch (Exception)
            //{
            //	// Already catching errors.. nothing else to realy do here if this causes an error as well
            //}

            //// Forward to our error message
            //         HttpContext.Current.Response.Redirect(UI_ApplicationCache_Gateway.Settings.Servers.System_Error_URL, false);
            //HttpContext.Current.ApplicationInstance.CompleteRequest();
            //if (currentMode != null)
            //	currentMode.Request_Completed = true;
        }

        #endregion


    }

}