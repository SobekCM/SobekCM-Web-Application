﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Text;
using System.Web;
using SobekCM.Core.Builder;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.Settings;
using SobekCM.Engine_Library.Database;
using SobekCM.Core.Configuration.Engine;
using SobekCM.Engine_Library.Settings;
using SobekCM.Tools;

namespace SobekCM.Engine_Library.Endpoints
{
    /// <summary> Class supports all the builder-related services provided by the SobekCM engine </summary>
    public class BuilderServices : EndpointBase
    {
        /// <summary> Gets the builder-specific settings, including all the builder modules and incoming folders </summary>
        /// <param name="Response"></param>
        /// <param name="UrlSegments"></param>
        /// <param name="QueryString"></param>
        /// <param name="Protocol"></param>
        /// <param name="IsDebug"></param>
        public void GetBuilderSettings(HttpResponse Response, List<string> UrlSegments, NameValueCollection QueryString, Microservice_Endpoint_Protocol_Enum Protocol, bool IsDebug)
        {
            Custom_Tracer tracer = new Custom_Tracer();

            // Should descriptions be suppressed?
            bool includeDescriptions = !((!String.IsNullOrEmpty(QueryString["IncludeDescs"])) && (QueryString["IncludeDescs"].ToUpper() == "FALSE"));
            try
            {
                tracer.Add_Trace("BuilderServices.GetBuilderSettings", "Pulling dataset from the database");

                // Get the dataset with all the builder settings
                DataSet builderSet = Engine_Database.Get_Builder_Settings(false, tracer);

                // If the returned value from the database was NULL, there was an error
                if ((builderSet == null) || ( builderSet.Tables.Count == 0 ))
                {
                    Response.ContentType = "text/plain";
                    Response.Output.WriteLine("Error completing request");

                    if (IsDebug)
                    {
                        Response.Output.WriteLine("DataSet returned from the database was either NULL or empty");
                        if (Engine_Database.Last_Exception != null)
                        {
                            Response.Output.WriteLine();
                            Response.Output.WriteLine(Engine_Database.Last_Exception.Message);
                            Response.Output.WriteLine();
                            Response.Output.WriteLine(Engine_Database.Last_Exception.StackTrace);
                        }

                        Response.Output.WriteLine();
                        Response.Output.WriteLine(tracer.Text_Trace);
                    }

                    Response.StatusCode = 500;
                    return;
                }

                tracer.Add_Trace("BuilderServices.GetBuilderSettings", "Build the builder-specific settings object");
                Builder_Settings returnValue = new Builder_Settings();
                if (!Builder_Settings_Builder.Refresh(returnValue, builderSet, includeDescriptions))
                {
                    Response.ContentType = "text/plain";
                    Response.Output.WriteLine("Error completing request");

                    if (IsDebug)
                    {
                        Response.Output.WriteLine("Error creating the Builder_Settings object from the database tables");
                        Response.Output.WriteLine();
                        Response.Output.WriteLine(tracer.Text_Trace);
                    }

                    Response.StatusCode = 500;
                    return;
                }

                // If this was debug mode, then just write the tracer
                if (IsDebug)
                {
                    Response.ContentType = "text/plain";
                    Response.Output.WriteLine("DEBUG MODE DETECTED");
                    Response.Output.WriteLine();
                    Response.Output.WriteLine(tracer.Text_Trace);

                    return;
                }

                // Get the JSON-P callback function
                string json_callback = "parseBuilderSettings";
                if ((Protocol == Microservice_Endpoint_Protocol_Enum.JSON_P) && (!String.IsNullOrEmpty(QueryString["callback"])))
                {
                    json_callback = QueryString["callback"];
                }

                // Use the base class to serialize the object according to request protocol
                Serialize(returnValue, Response, Protocol, json_callback);
            }
            catch (Exception ee)
            {
                if (IsDebug)
                {
                    Response.ContentType = "text/plain";
                    Response.Output.WriteLine("EXCEPTION CAUGHT!");
                    Response.Output.WriteLine();
                    Response.Output.WriteLine(ee.Message);
                    Response.Output.WriteLine();
                    Response.Output.WriteLine(ee.StackTrace);
                    Response.Output.WriteLine();
                    Response.Output.WriteLine(tracer.Text_Trace);
                    return;
                }

                Response.ContentType = "text/plain";
                Response.Output.WriteLine("Error completing request");
                Response.StatusCode = 500;
            }
        }

        /// <summary> Gets the latest update on the builder status, including the relevant builder
        /// setting values and updates on the scheduled tasks </summary>
        /// <param name="Response"></param>
        /// <param name="UrlSegments"></param>
        /// <param name="QueryString"></param>
        /// <param name="Protocol"></param>
        /// <param name="IsDebug"></param>
        public void GetBuilderStatus(HttpResponse Response, List<string> UrlSegments, NameValueCollection QueryString, Microservice_Endpoint_Protocol_Enum Protocol, bool IsDebug)
        {
            Custom_Tracer tracer = new Custom_Tracer();

            try
            {
                tracer.Add_Trace("BuilderServices.GetBuilderStatus", "Pulling builder status from the database");

                // Get the status
                Builder_Status builderStatus = Engine_Database.Builder_Get_Recent_Updates(tracer);

                // If the returned value from the database was NULL, there was an error
                if (builderStatus == null) 
                {
                    Response.ContentType = "text/plain";
                    Response.Output.WriteLine("Error completing request");

                    if (IsDebug)
                    {
                        Response.Output.WriteLine("Builder status object returned from the database was either NULL or empty");
                        if (Engine_Database.Last_Exception != null)
                        {
                            Response.Output.WriteLine();
                            Response.Output.WriteLine(Engine_Database.Last_Exception.Message);
                            Response.Output.WriteLine();
                            Response.Output.WriteLine(Engine_Database.Last_Exception.StackTrace);
                        }

                        Response.Output.WriteLine();
                        Response.Output.WriteLine(tracer.Text_Trace);
                    }

                    Response.StatusCode = 500;
                    return;
                }

                tracer.Add_Trace("BuilderServices.GetBuilderStatus", "Successfully built builder status");

                // If this was debug mode, then just write the tracer
                if (IsDebug)
                {
                    Response.ContentType = "text/plain";
                    Response.Output.WriteLine("DEBUG MODE DETECTED");
                    Response.Output.WriteLine();
                    Response.Output.WriteLine(tracer.Text_Trace);

                    return;
                }

                // Get the JSON-P callback function
                string json_callback = "parseBuilderStatus";
                if ((Protocol == Microservice_Endpoint_Protocol_Enum.JSON_P) && (!String.IsNullOrEmpty(QueryString["callback"])))
                {
                    json_callback = QueryString["callback"];
                }

                // Use the base class to serialize the object according to request protocol
                Serialize(builderStatus, Response, Protocol, json_callback);
            }
            catch (Exception ee)
            {
                if (IsDebug)
                {
                    Response.ContentType = "text/plain";
                    Response.Output.WriteLine("EXCEPTION CAUGHT!");
                    Response.Output.WriteLine();
                    Response.Output.WriteLine(ee.Message);
                    Response.Output.WriteLine();
                    Response.Output.WriteLine(ee.StackTrace);
                    Response.Output.WriteLine();
                    Response.Output.WriteLine(tracer.Text_Trace);
                    return;
                }

                Response.ContentType = "text/plain";
                Response.Output.WriteLine("Error completing request");
                Response.StatusCode = 500;
            }
        }

        /// <summary> Gets the information about a single incoming builder folder  </summary>
        /// <param name="Response"></param>
        /// <param name="UrlSegments"></param>
        /// <param name="QueryString"></param>
        /// <param name="Protocol"></param>
        /// <param name="IsDebug"></param>
        public void GetSingleFolder(HttpResponse Response, List<string> UrlSegments, NameValueCollection QueryString, Microservice_Endpoint_Protocol_Enum Protocol, bool IsDebug)
        {
            Custom_Tracer tracer = new Custom_Tracer();

            // Must at least have one URL segment for the BibID
            if (UrlSegments.Count > 0)
            {
                int folderid;
                if (!Int32.TryParse(UrlSegments[0], out folderid))
                {
                    if (IsDebug)
                    {
                        Response.ContentType = "text/plain";
                        Response.Output.WriteLine("Invalid request - folder id must be an integer");
                        Response.Output.WriteLine();
                        Response.Output.WriteLine(tracer.Text_Trace);
                        return;
                    }

                    Response.ContentType = "text/plain";
                    Response.Output.WriteLine("Invalid request - folder id must be an integer");
                    Response.StatusCode = 400;
                    return;
                }

                try
                {
                    tracer.Add_Trace("BuilderServices.GetSingleFolder", "Pulling builder folder information by id " + folderid);

                    // Get the status
                    Builder_Source_Folder builderFolder = Engine_Database.Builder_Get_Incoming_Folder(folderid, tracer);

                    // If the returned value from the database was NULL, there was an error
                    if (builderFolder == null)
                    {
                        Response.ContentType = "text/plain";
                        Response.Output.WriteLine("Error completing request");

                        if (IsDebug)
                        {
                            Response.Output.WriteLine("Builder folder object returned from the database was either NULL or empty");
                            if (Engine_Database.Last_Exception != null)
                            {
                                Response.Output.WriteLine();
                                Response.Output.WriteLine(Engine_Database.Last_Exception.Message);
                                Response.Output.WriteLine();
                                Response.Output.WriteLine(Engine_Database.Last_Exception.StackTrace);
                            }

                            Response.Output.WriteLine();
                            Response.Output.WriteLine(tracer.Text_Trace);
                        }

                        Response.StatusCode = 500;
                        return;
                    }

                    tracer.Add_Trace("BuilderServices.GetSingleFolder", "Successfully built builder folder");

                    // If this was debug mode, then just write the tracer
                    if (IsDebug)
                    {
                        Response.ContentType = "text/plain";
                        Response.Output.WriteLine("DEBUG MODE DETECTED");
                        Response.Output.WriteLine();
                        Response.Output.WriteLine(tracer.Text_Trace);

                        return;
                    }

                    // Get the JSON-P callback function
                    string json_callback = "parseBuilderFolder";
                    if ((Protocol == Microservice_Endpoint_Protocol_Enum.JSON_P) && (!String.IsNullOrEmpty(QueryString["callback"])))
                    {
                        json_callback = QueryString["callback"];
                    }

                    // Use the base class to serialize the object according to request protocol
                    Serialize(builderFolder, Response, Protocol, json_callback);
                }
                catch (Exception ee)
                {
                    if (IsDebug)
                    {
                        Response.ContentType = "text/plain";
                        Response.Output.WriteLine("EXCEPTION CAUGHT!");
                        Response.Output.WriteLine();
                        Response.Output.WriteLine(ee.Message);
                        Response.Output.WriteLine();
                        Response.Output.WriteLine(ee.StackTrace);
                        Response.Output.WriteLine();
                        Response.Output.WriteLine(tracer.Text_Trace);
                        return;
                    }

                    Response.ContentType = "text/plain";
                    Response.Output.WriteLine("Error completing request");
                    Response.StatusCode = 500;
                }
            }
            else
            {
                Response.ContentType = "text/plain";
                Response.Output.WriteLine("Invalid request - folder id must be included");
                Response.StatusCode = 400;
            }
        }

        /// <summary> Get the list of all the recent updates for consumption by the jQuery DataTable.net plug-in </summary>
        /// <param name="Response"></param>
        /// <param name="UrlSegments"></param>
        /// <param name="QueryString"></param>
        /// <param name="Protocol"></param>
        /// <param name="IsDebug"></param>
        public void Get_Builder_Logs_JDataTable(HttpResponse Response, List<string> UrlSegments, NameValueCollection QueryString, Microservice_Endpoint_Protocol_Enum Protocol, bool IsDebug)
        {
            Custom_Tracer tracer = new Custom_Tracer();

            // Get ready to pull the informaiton from the query string which the
            // jquery datatables library pass in
            int displayStart;
            int displayLength;

            // Get the display start and length from the DataTables generated data URL
            if (!Int32.TryParse(HttpContext.Current.Request.QueryString["iDisplayStart"], out displayStart)) displayStart = 0;
            if (!Int32.TryParse(HttpContext.Current.Request.QueryString["iDisplayLength"], out displayLength)) displayLength = 50;

            // Get the echo value
            string sEcho = HttpContext.Current.Request.QueryString["sEcho"];

            // Look for specific arguments in the URL
            DateTime? startDate = null;
            DateTime? endDate = null;
            string filter = null;
            bool include_no_work = false;

            // Check for start date
            if (!String.IsNullOrEmpty(HttpContext.Current.Request.QueryString["date1"]))
            {
                DateTime temp;
                if (DateTime.TryParse(HttpContext.Current.Request.QueryString["date1"], out temp))
                {
                    startDate = new DateTime(temp.Year, temp.Month, temp.Day, 0, 0, 0);
                }
            }

            // Check for end date
            if (!String.IsNullOrEmpty(HttpContext.Current.Request.QueryString["date2"]))
            {
                DateTime temp;
                if (DateTime.TryParse(HttpContext.Current.Request.QueryString["date2"], out temp))
                {
                    endDate = new DateTime(temp.Year, temp.Month, temp.Day, 23, 59, 59);
                }
            }

            // Check for filter
            if (!String.IsNullOrEmpty(HttpContext.Current.Request.QueryString["filter"]))
            {
                filter = HttpContext.Current.Request.QueryString["filter"];
            }

            // Check for flag
            if (!String.IsNullOrEmpty(HttpContext.Current.Request.QueryString["includeNoWork"]))
            {
                bool temp;
                if (bool.TryParse(HttpContext.Current.Request.QueryString["includeNoWork"], out temp))
                {
                    include_no_work = temp;
                }
            }

            // Get the dataset of logs
            DataSet logs = get_recent_builder_logs(startDate, endDate, filter, include_no_work, tracer);

            // If null was returned, an error occurred
            if (logs == null)
            {
                Response.ContentType = "text/plain";
                Response.Output.WriteLine("Unable to pull builder log from the database");
                Response.StatusCode = 500;

                // If this was debug mode, then just write the tracer
                if (IsDebug)
                {
                    Response.Output.WriteLine();
                    Response.Output.WriteLine();
                    Response.Output.WriteLine("DEBUG MODE DETECTED");
                    Response.Output.WriteLine();
                    Response.Output.WriteLine(tracer.Text_Trace);
                }
                return;
            }

            // Get the count of results
            DataTable logTable = logs.Tables[0];
            int total_results = logTable.Rows.Count;

            // If this was set to show ALL results, set some page/length information
            if (displayLength == -1)
            {
                displayStart = 0;
                displayLength = total_results;
            }

            // Start the JSON response
            Response.Output.WriteLine("{");
            Response.Output.WriteLine("\"sEcho\": " + sEcho + ",");
            Response.Output.WriteLine("\"iTotalRecords\": \"" + total_results + "\",");
            Response.Output.WriteLine("\"iTotalDisplayRecords\": \"" + total_results + "\",");
            Response.Output.WriteLine("\"aaData\": [");

            // Get the columns 
            DataColumn dateColumn = logTable.Columns["LogDate"];
            DataColumn bibVidColumn = logTable.Columns["BibID_VID"];
            DataColumn typeColumn = logTable.Columns["LogType"];
            DataColumn messageColumn = logTable.Columns["LogMessage"];

            // Add the data for the rows to show
            for (int i = displayStart; (i < displayStart + displayLength) && (i < total_results); i++)
            {
                // Start the JSON response for this row
                DataRow thisRow = logTable.Rows[i];

                Response.Output.Write("[\"" + thisRow[dateColumn].ToString().Replace("\"", "'") + "\", ");
                Response.Output.Write("\"" + thisRow[bibVidColumn].ToString().Replace("\"", "'") + "\", ");
                Response.Output.Write("\"" + thisRow[typeColumn].ToString().Replace("\"","'") + "\", ");
                Response.Output.Write("\"" + thisRow[messageColumn].ToString().Replace("\"", "'").Replace(@"\", @"\\").Replace("\n", "\\n").Replace("\r", "") + "\" "); 

                // Finish this row
                if ((i < displayStart + displayLength - 1) && (i < total_results - 1))
                    Response.Output.WriteLine("],");
                else
                    Response.Output.WriteLine("]");
            }

            Response.Output.WriteLine("]");
            Response.Output.WriteLine("}");
        }

        protected DataSet get_recent_builder_logs(DateTime? StartDate, DateTime? EndDate, string Filter, bool Include_No_Work, Custom_Tracer Tracer)
        {
            // Look in the cache first
            DataSet fromCache = CachedDataManager.Builder.Retrieve_Builder_Logs(StartDate, EndDate, Filter, Include_No_Work, Tracer);
            if (fromCache != null)
                return fromCache;

            // Try to pull from the database
            DataSet fromDb = Engine_Database.Builder_Log_Search(StartDate, EndDate, Filter, Include_No_Work, Tracer);

            // Store in the cache if not null
            if (fromDb != null)
            {
                CachedDataManager.Builder.Store_Builder_Logs(fromDb, StartDate, EndDate, Filter, Include_No_Work, Tracer);
            }

            return fromDb;
        }
    }
}
