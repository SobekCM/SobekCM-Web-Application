using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web;
using SobekCM.Engine_Library.Database;
using SobekCM.Core.Configuration.Engine;
using SobekCM.Tools;

namespace SobekCM.Engine_Library.Endpoints
{
    /// <summary> Class supports all the open publishing services provided by the SobekCM engine </summary>
    public class OpenPublishingServices : EndpointBase
    {
        /// <summary> Gets an OpenPublishing theme, by primary key </summary>
        /// <param name="Response"></param>
        /// <param name="UrlSegments"></param>
        /// <param name="QueryString"></param>
        /// <param name="Protocol"></param>
        /// <param name="IsDebug"></param>
        public void GetTheme(HttpResponse Response, List<string> UrlSegments, NameValueCollection QueryString, Microservice_Endpoint_Protocol_Enum Protocol, bool IsDebug)
        {
            // Must at least have one URL segment for the BibID
            if (UrlSegments.Count == 0)
                return;

            Custom_Tracer tracer = new Custom_Tracer();

            // Get the theme id
            if ( !Int32.TryParse(UrlSegments[0], out int id))
            {
                tracer.Add_Trace("OpenPublishingServices.GetTheme", "Error converting url segment to integer to get theme by id");
                id = -1;
            }
            
            try
            {
                tracer.Add_Trace("OpenPublishingServices.GetTheme", "Pulling theme from the database");

                // Get the theme
                var theme = Engine_Database.Get_OpenPublishing_Theme(id, tracer);

                // If the returned value from the database was NULL, there was an error
                if (theme == null)
                {
                    Response.ContentType = "text/plain";
                    Response.Output.WriteLine("Error completing request");

                    if (IsDebug)
                    {
                        Response.Output.WriteLine("DataSet returned from the database was either NULL or empty");
                        Response.Output.WriteLine("Attempted to retrieve theme by id " + id);
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
                string json_callback = "getTheme";
                if ((Protocol == Microservice_Endpoint_Protocol_Enum.JSON_P) && (!String.IsNullOrEmpty(QueryString["callback"])))
                {
                    json_callback = QueryString["callback"];
                }

                // Use the base class to serialize the object according to request protocol
                Serialize(theme, Response, Protocol, json_callback);
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

        /// <summary> Gets all the available themes </summary>
        /// <param name="Response"></param>
        /// <param name="UrlSegments"></param>
        /// <param name="QueryString"></param>
        /// <param name="Protocol"></param>
        /// <param name="IsDebug"></param>
        public void GetAvailableThemes(HttpResponse Response, List<string> UrlSegments, NameValueCollection QueryString, Microservice_Endpoint_Protocol_Enum Protocol, bool IsDebug)
        {
            // Must at least have one URL segment for the BibID
            if (UrlSegments.Count == 0)
                return;

            Custom_Tracer tracer = new Custom_Tracer();

            try
            {
                tracer.Add_Trace("OpenPublishingServices.GetAvailableThemes", "Pulling themes from the database");

                // Get the theme
                var themes = Engine_Database.Get_Available_OpenPublishing_Themes(tracer);

                // If the returned value from the database was NULL, there was an error
                if (themes == null)
                {
                    Response.ContentType = "text/plain";
                    Response.Output.WriteLine("Error completing request");

                    if (IsDebug)
                    {
                        Response.Output.WriteLine("List of themes returned from the database was NULL");

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
                string json_callback = "getTheme";
                if ((Protocol == Microservice_Endpoint_Protocol_Enum.JSON_P) && (!String.IsNullOrEmpty(QueryString["callback"])))
                {
                    json_callback = QueryString["callback"];
                }

                // Use the base class to serialize the object according to request protocol
                Serialize(themes, Response, Protocol, json_callback);
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
    }
}
