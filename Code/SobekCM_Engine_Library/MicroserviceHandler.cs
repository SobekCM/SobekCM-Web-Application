#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.Configuration.Engine;
using SobekCM.Engine_Library.ApplicationState;
using SobekCM.Engine_Library.Database;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

#endregion

namespace SobekCM.Engine_Library
{
    /// <summary> Handles incoming requests for a microservice exposed by the engine.
    /// Originally IHttpHandler; converted to a plain class for use with ASP.NET Core routing. </summary>
    public class MicroserviceHandler
    {
        private static Dictionary<string, object> restApiObjectsDictionary;
        private static Dictionary<string, MethodInfo> restApiMethodDictionary;

        /// <summary> Static constructor </summary>
        static MicroserviceHandler()
        {
            restApiObjectsDictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            restApiMethodDictionary = new Dictionary<string, MethodInfo>();
        }

        /// <summary> Clear the collection dictionaries used to save having to reload objects, etc.. </summary>
        public static void Clear()
        {
            restApiObjectsDictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            restApiMethodDictionary = new Dictionary<string, MethodInfo>();
        }

        /// <summary> Processes the request </summary>
        /// <param name="Context">The ASP.NET Core HTTP context for the current request </param>
        public async Task ProcessRequest(HttpContext Context)
        {
            Engine_Database.Connection_String = Engine_ApplicationCache_Gateway.Settings.Database_Connection.Connection_String;

            var compat = new CompatHttpResponse(Context.Response);
            try
            {
                await ProcessRequest_Internal(Context, compat);
            }
            finally
            {
                await compat.FlushToResponseAsync();
            }
        }

        private static Task ProcessRequest_Internal(HttpContext Context, CompatHttpResponse compat)
        {
            // Get the original query string
            string queryString = Context.Request.Query["urlrelative"].ToString();
            if (!String.IsNullOrEmpty(queryString))
            {
                // Set the encoding as part of content type
                Context.Response.ContentType = "text/plain; charset=utf-8";

                // Make sure the microservices configuration has been read
                if (Engine_ApplicationCache_Gateway.Configuration.Source.ErrorEncountered)
                {
                    Context.Response.ContentType = "text/plain";
                    Context.Response.StatusCode = 500;
                    compat.Output.WriteLine("Error reading the configuration files!");
                    compat.Output.WriteLine();

                    foreach (string thisLine in Engine_ApplicationCache_Gateway.Configuration.Source.ReadingLog)
                        compat.Output.WriteLine(thisLine);
                    return Task.CompletedTask;
                }

                // Collect the requested paths
                string[] splitter;
                if ((queryString.IndexOf("/") == 0) && (queryString.Length > 1))
                    splitter = queryString.Substring(1).Split("/".ToCharArray());
                else
                    splitter = queryString.Split("/".ToCharArray());
                List<string> paths = splitter.ToList();

                // Get any matching endpoint configuration
                Engine_Path_Endpoint endpoint = Engine_ApplicationCache_Gateway.Configuration.Engine.Get_Endpoint(paths);
                if (endpoint == null)
                {
                    Context.Response.ContentType = "text/plain";
                    Context.Response.StatusCode = 501;
                    compat.Output.WriteLine("No endpoint found");
                    compat.Output.WriteLine();

                    foreach (string thisLine in Engine_ApplicationCache_Gateway.Configuration.Source.ReadingLog)
                        compat.Output.WriteLine(thisLine);
                }
                else
                {
                    string method = Context.Request.Method.ToUpper();
                    if (!endpoint.VerbMappingExists(method))
                    {
                        Context.Response.ContentType = "text/plain";
                        Context.Response.StatusCode = 406;
                        compat.Write("HTTP method " + method + " is not supported by this URL");
                        return Task.CompletedTask;
                    }

                    // Get the specific verb mapping
                    Engine_VerbMapping verbMapping = endpoint[method];

                    // Ensure this is allowed in the range
                    string requestIp = Context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    if (!verbMapping.AccessPermitted(requestIp))
                    {
                        Context.Response.ContentType = "text/plain";
                        Context.Response.StatusCode = 403;
                        compat.Write("You are forbidden from accessing this endpoint ( " + requestIp + " )");
                        return Task.CompletedTask;
                    }

                    // Set the protocol
                    switch (verbMapping.Protocol)
                    {
                        case Microservice_Endpoint_Protocol_Enum.JSON:
                            Context.Response.ContentType = "application/json";
                            break;

                        case Microservice_Endpoint_Protocol_Enum.JSON_P:
                            Context.Response.ContentType = "application/javascript";
                            break;

                        case Microservice_Endpoint_Protocol_Enum.PROTOBUF:
                            Context.Response.ContentType = "application/octet-stream";
                            break;

                        case Microservice_Endpoint_Protocol_Enum.XML:
                            Context.Response.ContentType = "text/xml";
                            break;

                        case Microservice_Endpoint_Protocol_Enum.SOAP:
                            Context.Response.ContentType = "text/xml";
                            break;

                        case Microservice_Endpoint_Protocol_Enum.BINARY:
                            Context.Response.ContentType = "application/octet-stream";
                            break;

                        case Microservice_Endpoint_Protocol_Enum.TEXT:
                            Context.Response.ContentType = "text/plain";
                            break;
                    }

                    // Determine if this is currently in a valid DEBUG mode
                    bool debug = (Context.Request.Query["debug"].ToString() == "debug");

                    // Get the component information
                    if (verbMapping.Component == null)
                    {
                        Context.Response.ContentType = "text/plain";
                        compat.Output.WriteLine("No component listed or found for this valid endpoint");
                        Context.Response.StatusCode = 500;
                        return Task.CompletedTask;
                    }

                    // Convert IQueryCollection → NameValueCollection for endpoint methods
                    var queryNVC = new NameValueCollection();
                    foreach (var kvp in Context.Request.Query)
                        queryNVC.Add(kvp.Key, kvp.Value.ToString());

                    // Look for this component
                    object restApiObject = null;
                    if (restApiObjectsDictionary.ContainsKey(verbMapping.Component.ID))
                    {
                        restApiObject = restApiObjectsDictionary[verbMapping.Component.ID];
                    }
                    else
                    {
                        try
                        {
                            Assembly dllAssembly = Assembly.GetExecutingAssembly();
                            if (!String.IsNullOrEmpty(verbMapping.Component.Assembly))
                            {
                                string assemblyFilePath = Engine_ApplicationCache_Gateway.Configuration.Extensions.Get_Assembly(verbMapping.Component.Assembly);
                                if (assemblyFilePath != null)
                                {
                                    dllAssembly = Assembly.LoadFrom(assemblyFilePath);
                                }
                            }

                            string className = verbMapping.Component.Class;
                            if (className.IndexOf(".") < 0)
                                className = "SobekCM.Engine_Library.Endpoints." + verbMapping.Component.Class;

                            Type restApiClassType = dllAssembly.GetType(className);
                            restApiObject = Activator.CreateInstance(restApiClassType);

                            restApiObjectsDictionary[verbMapping.Component.ID] = restApiObject;
                        }
                        catch (Exception ee)
                        {
                            Context.Response.ContentType = "text/plain";
                            compat.Output.WriteLine("Error creating the endpoint object " + verbMapping.Component.Class);
                            compat.Output.WriteLine(ee.Message);
                            Context.Response.StatusCode = 500;
                            return Task.CompletedTask;
                        }
                    }

                    if (verbMapping.Component.Class == null)
                    {
                        Context.Response.ContentType = "text/plain";
                        compat.Output.WriteLine("Error creating the endpoint object " + verbMapping.Component.Class);
                        Context.Response.StatusCode = 500;
                        return Task.CompletedTask;
                    }

                    try
                    {
                        MethodInfo methodInfo = null;
                        if (restApiMethodDictionary.ContainsKey(verbMapping.Component.ID + "|" + verbMapping.Method))
                        {
                            methodInfo = restApiMethodDictionary[verbMapping.Component.ID + "|" + verbMapping.Method];
                        }
                        else
                        {
                            Type restApiClassType = restApiObject.GetType();
                            methodInfo = restApiClassType.GetMethod(verbMapping.Method);

                            restApiMethodDictionary[verbMapping.Component.ID + "|" + verbMapping.Method] = methodInfo;
                        }

                        if (methodInfo == null)
                        {
                            Context.Response.ContentType = "text/plain";
                            compat.Output.WriteLine("Error invoking the endpoint method: No Method Found");
                            Context.Response.StatusCode = 500;
                            return Task.CompletedTask;
                        }

                        if (verbMapping.RequestType == Microservice_Endpoint_RequestType_Enum.GET)
                        {
                            methodInfo.Invoke(restApiObject, new object[] { compat, paths, queryNVC, verbMapping.Protocol, debug });
                        }
                        else
                        {
                            // Convert IFormCollection → NameValueCollection for POST endpoints
                            var formNVC = new NameValueCollection();
                            foreach (var kvp in Context.Request.Form)
                                formNVC.Add(kvp.Key, kvp.Value.ToString());

                            methodInfo.Invoke(restApiObject, new object[] { compat, paths, queryNVC, verbMapping.Protocol, formNVC, debug });
                        }
                    }
                    catch (Exception ee)
                    {
                        Context.Response.ContentType = "text/plain";
                        compat.Output.WriteLine("Error invoking the endpoint method: " + ee.Message);
                        Context.Response.StatusCode = 500;
                    }
                }
            }
            else
            {
                Context.Response.ContentType = "text/plain";
                Context.Response.StatusCode = 400;
                compat.Write("Invalid URI - No endpoint requested");
            }

            return Task.CompletedTask;
        }
    }
}
