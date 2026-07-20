using Microsoft.AspNetCore.Http;
using SobekCM.Library;
using SobekCM.Library.Database;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System;
using System.IO;

namespace SobekCM.QueryInitializerHelpers
{
    public class DatabaseConnectionInitializer : IQueryInitializerHelper
    {
        public QueryInitializerHelperResponse Initialize(HttpContext context, RequestCache request, Custom_Tracer tracer)
        {
            try
            {
                if (string.IsNullOrEmpty(SobekCM_Database.Connection_String))
                {
                    SobekCM_Database.Connection_String = UI_ApplicationCache_Gateway.Settings.Database_Connection.Connection_String;

                    SobekCM_Database.Test_Connection();
                }

                return QueryInitializerHelperResponse.Successful;
            }
            catch (Exception ee)
            {
                tracer.Add_Trace("QueryInitializerHelperResponse.Initialize", "Exception caught: " + ee.Message);
                tracer.Add_Trace("QueryInitializerHelperResponse.Initialize", ee.StackTrace);


                // Create an error message 
                string errorMessage = $"Error caught while validating application state and testing db connection ({ee.Message})";
                if ((UI_ApplicationCache_Gateway.Settings.Database_Connection == null) || (String.IsNullOrEmpty(UI_ApplicationCache_Gateway.Settings.Database_Connection.Connection_String)))
                {
                    errorMessage = "No database connection string found!";
                    string configFileLocation = AppDomain.CurrentDomain.BaseDirectory + "config/sobekcm.xml";
                    try
                    {
                        if (!File.Exists(configFileLocation))
                        {
                            errorMessage = "Missing config/sobekcm.xml configuration file on the web server.<br />Ensure the configuration file 'sobekcm.xml' exists in a 'config' subfolder directly under the web application.<br />Example configuration is:" +
                                           "<div style=\"background-color: #bbbbbb; margin-left: 30px; margin-top:10px; padding: 3px;\">&lt;?xml version=&quot;1.0&quot; encoding=&quot;UTF-8&quot; standalone=&quot;yes&quot;  ?&gt;<br /> &lt;configuration&gt;<br /> &nbsp; &nbsp &lt;connection_string type=&quot;MSSQL&quot;&gt;data source=localhost\\instance;initial catalog=SobekCM;integrated security=Yes;&lt;/connection_string&gt;<br /> &nbsp; &nbsp &lt;error_emails&gt;marsull@uflib.ufl.edu&lt;/error_emails&gt;<br /> &nbsp; &nbsp &lt;error_page&gt;http://ufdc.ufl.edu/error.html&lt;/error_page&gt;<br />&lt;/configuration&gt;</div>";
                        }
                    }
                    catch
                    {
                        errorMessage = "No database connection string found.<br />Likely an error reading the configuration file due to permissions on the web server.<br />Ensure the configuration file 'sobekcm.xml' exists in a 'config' subfolder directly under the web application.<br />Example configuration is:" +
                                       "<div style=\"background-color: #bbbbbb; margin-left: 30px; margin-top:10px; padding: 3px;\">&lt;?xml version=&quot;1.0&quot; encoding=&quot;UTF-8&quot; standalone=&quot;yes&quot;  ?&gt;<br /> &lt;configuration&gt;<br /> &nbsp; &nbsp &lt;connection_string type=&quot;MSSQL&quot;&gt;data source=localhost\\instance;initial catalog=SobekCM;integrated security=Yes;&lt;/connection_string&gt;<br /> &nbsp; &nbsp &lt;error_emails&gt;marsull@uflib.ufl.edu&lt;/error_emails&gt;<br /> &nbsp; &nbsp &lt;error_page&gt;http://ufdc.ufl.edu/error.html&lt;/error_page&gt;<br />&lt;/configuration&gt;</div>";
                    }
                }
                else
                {
                    if (ee.Message.IndexOf("The EXECUTE permission") >= 0)
                    {
                        errorMessage = "Permissions error while connecting to the database and pulling necessary data.<br /><br />Confirm the following:<ul><li>IIS is configured correctly to use anonymous authentication</li><li>Anonymous user (or service account) is part of the sobek_users role in the database.</li></ul>";
                    }
                    else
                    {
                        errorMessage = "Error connecting to the database and pulling necessary data.<br /><br />Confirm the following:<ul><li>Database connection string is correct ( " + UI_ApplicationCache_Gateway.Settings.Database_Connection.Connection_String + ")</li><li>IIS is configured correctly to use anonymous authentication</li><li>Anonymous user (or service account) is part of the sobek_users role in the database.</li></ul>";
                    }
                }

                return new QueryInitializerHelperResponse(false, errorMessage, ee);
            }
        }
    }
}
