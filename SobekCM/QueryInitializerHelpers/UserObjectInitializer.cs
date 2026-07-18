using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using SobekCM.Core.Configuration.Authentication;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.Navigation;
using SobekCM.Core.Users;
using SobekCM.Engine_Library.Database;
using SobekCM.Library;
using SobekCM.Library.Database;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace SobekCM.QueryInitializerHelpers
{
    public class UserObjectInitializer : IQueryInitializerHelper
    {
        public QueryInitializerHelperResponse Initialize(HttpContext context, RequestCache request, Custom_Tracer tracer)
        {
            tracer.Add_Trace("UserObjectInitializer.Initialize", "Checking for user in cookie and running some validation");

            var currentMode = request.Current_Mode;

            if (currentMode == null)
            {
                return new QueryInitializerHelperResponse(false, "The UserObjectInitializer must be called after the NavigationObjectInitializer has configured the NavigationObject");
            }

            // If there was previously an error and it somehow got to here, return as well
            // Is this really needed?
            if (currentMode.Mode == Display_Mode_Enum.Error)
            {
                return QueryInitializerHelperResponse.Successful;
            }

            if (currentMode.Is_Robot)
            {
                return QueryInitializerHelperResponse.Successful;
            }

            try
            {
                // Determine which IP Ranges this IP address belongs to, if not already determined.
                if (context.Session.GetString(SessionCache_Keys.IpRangeMembership) == null)
                {
                    string userAddress = context.Items[RequestCache_Keys.UserIP].ToString();

                    int ip_mask = UI_ApplicationCache_Gateway.IP_Restrictions.Restrictive_Range_Membership(userAddress);
                    context.Session.SetString(SessionCache_Keys.IpRangeMembership, ip_mask.ToString());
                }

                // Set the Session TOC, if provided
                if (currentMode.TOC_Display != TOC_Display_Type_Enum.Undetermined)
                {
                    if (currentMode.TOC_Display == TOC_Display_Type_Enum.Hide)
                    {
                        context.Items["Show TOC"] = false;
                    }
                    else
                    {
                        context.Items["Show TOC"] = true;
                    }
                }

                // Only do any of the user stuff if this is from the main SobekCM page
                if (request.Page_Name == "SOBEKCM")
                {
                    tracer.Add_Trace("QueryInitializer.Constructor", "Checking for logged on user by cookie or session");
                    var result = perform_user_checks(context, request, tracer);
                    if (result != null)
                        return result;
                }

                // If this is a system admin, they can run as a different user actually
                if ((request.Current_User != null) && (request.Current_User.Is_System_Admin) && (request.QueryString["userid"] != null))
                {
                    try
                    {
                        int userid = Convert.ToInt32(request.QueryString["userid"]);
                        User_Object mirroredUser = Engine_Database.Get_User(userid, tracer);
                        if (mirroredUser != null)
                        {
                            // Replace the user information in the session state
                            using var sessionMs = new MemoryStream();
                            ProtoBuf.Serializer.Serialize(sessionMs, mirroredUser);
                            context.Session.Set(SessionCache_Keys.User, sessionMs.ToArray());
                            request.Current_User = mirroredUser; 
                        }
                    }
                    catch (Exception)
                    {
                        // Nothing to do here.. shouldn't ever really be here..
                    }
                }

                return QueryInitializerHelperResponse.Successful;
            }
            catch (Exception ee)
            {
                tracer.Add_Trace("UserObjectInitializer.Initialize", "Exception caught: " + ee.Message);
                tracer.Add_Trace("UserObjectInitializer.Initialize", ee.StackTrace);

                context.Response.StatusCode = 301;
                return new QueryInitializerHelperResponse(false, "Exception caught while running user checks.", ee);
            }
        }

        #region Method performs user checks against session, cookie, database, etc..

        private QueryInitializerHelperResponse perform_user_checks(HttpContext context, RequestCache request, Custom_Tracer tracer)
        {
            var currentMode = request.Current_Mode;

            // If the mode is NULL or the request was already completed, do nothing
            if ((currentMode == null) || (currentMode.Request_Completed))
                return null;

            bool isPostBack = request.Current_Mode.isPostBack;

            tracer.Add_Trace("QueryInitializer.Perform_User_Checks", "In user checks portion");

            // If this is to log out of my sobekcm, clear user id and forward back to sobekcm
            if ((currentMode.Mode == Display_Mode_Enum.My_Sobek) && (currentMode.My_Sobek_Type == My_Sobek_Type_Enum.Log_Out))
            {
                tracer.Add_Trace("QueryInitializer.Perform_User_Checks", "User logged out");

                // Delete any user cookie
                context.Response.Cookies.Delete("SobekUser");

                // Delete from memory
                context.Session.Remove(SessionCache_Keys.User);
                context.Session.Remove(SessionCache_Keys.UserId);

                // Determine new redirect location
                string redirect = currentMode.Base_URL;
                if (!String.IsNullOrEmpty(currentMode.Return_URL))
                {
                    redirect = currentMode.Base_URL + currentMode.Return_URL;

                    if (((currentMode.Return_URL.IndexOf("admin") >= 0) && (currentMode.Return_URL.IndexOf("admin") <= 1)) ||
                        ((currentMode.Return_URL.IndexOf("mysobek") >= 0) && (currentMode.Return_URL.IndexOf("mysobek") <= 1)))
                        redirect = currentMode.Base_URL;
                }

                return new QueryInitializerHelperResponse(true) { RedirectUrl = redirect };
            }

            // If there is already a user logged on, do nothing here
            byte[] sessionUserBytes = context.Session.Get(SessionCache_Keys.User);
            User_Object sessionUser = null;
            if (sessionUserBytes != null && sessionUserBytes.Length > 0)
            {
                using var ms = new MemoryStream(sessionUserBytes);
                sessionUser = ProtoBuf.Serializer.Deserialize<User_Object>(ms);
            }

            if (sessionUser == null)
            {
                check_shibboleth();

                // If the user information is still missing , but the SobekUser cookie exists, then pull 
                // the user information from the SobekUser cookie in the current requests
                if (context.Request.Cookies["SobekUser"] != null)
                {
                    string readCookieValue = context.Request.Cookies["SobekUser"] ?? "";
                    var parts = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(readCookieValue);
                    string userid_string = parts.TryGetValue("userid", out var v) ? v.ToString() : "";
                    string hash = parts.TryGetValue("security_hash", out var h) ? h.ToString() : "";

                    int userid = -1;

                    bool valid_perhaps = userid_string.All(Char.IsNumber);
                    if (valid_perhaps)
                        _ = Int32.TryParse(userid_string, out userid);

                    if (userid > 0)
                    {
                        sessionUser = Engine_Database.Get_User(userid, tracer);
                        if (sessionUser != null)
                        {
                            string userIp = context.Connection.RemoteIpAddress?.ToString();
                            string cookieValue = $"userid={sessionUser.UserID}&security_hash={sessionUser.Security_Hash(userIp)}";
                            context.Response.Cookies.Append("SobekUser", cookieValue, new CookieOptions
                            {
                                Expires = DateTimeOffset.Now.AddDays(30),
                                HttpOnly = true
                            });

                            // Also add user to session
                            using var sessionMs = new MemoryStream();
                            ProtoBuf.Serializer.Serialize(sessionMs, sessionUser);
                            context.Session.Set(SessionCache_Keys.User, sessionMs.ToArray());
                            request.Current_User = sessionUser;
                        }
                    }
                }
            }

            // If this is not a post back, set the html writer code to 'l' or 'h' depending on whether logged on
            if (!isPostBack)
            {
                if (sessionUser != null)
                {
                    if (currentMode.Writer_Type == Writer_Type_Enum.HTML)
                    {
                        currentMode.Writer_Type = Writer_Type_Enum.HTML_LoggedIn;
                        return new QueryInitializerHelperResponse(true) { RedirectUrl = UrlWriterHelper.Redirect_URL(currentMode) };
                    }
                    else
                    {
                        if ((currentMode.Mode == Display_Mode_Enum.My_Sobek) && (currentMode.My_Sobek_Type == My_Sobek_Type_Enum.Logon))
                        {
                            currentMode.My_Sobek_Type = My_Sobek_Type_Enum.Home;
                        }
                    }
                }
                else
                {
                    if ((currentMode.Writer_Type == Writer_Type_Enum.HTML_LoggedIn) && (currentMode.My_Sobek_Type != My_Sobek_Type_Enum.Logon) && (currentMode.My_Sobek_Type != My_Sobek_Type_Enum.Register))
                    {
                        switch (currentMode.Mode)
                        {
                            case Display_Mode_Enum.My_Sobek:
                                currentMode.My_Sobek_Type = My_Sobek_Type_Enum.Logon;
                                break;

                            case Display_Mode_Enum.Item_Display:
                                currentMode.Writer_Type = Writer_Type_Enum.HTML;
                                return new QueryInitializerHelperResponse(true) { RedirectUrl = UrlWriterHelper.Redirect_URL(currentMode) };

                            default:
                                currentMode.Writer_Type = Writer_Type_Enum.HTML;
                                return new QueryInitializerHelperResponse(true) { RedirectUrl = UrlWriterHelper.Redirect_URL(currentMode) };

                        }
                    }

                    // If this is requesting an internal page and there is no user, send to the logon page
                    if (currentMode.Mode == Display_Mode_Enum.Internal)
                    {
                        currentMode.Mode = Display_Mode_Enum.My_Sobek;
                        currentMode.My_Sobek_Type = My_Sobek_Type_Enum.Logon;
                    }
                }
            }
            else // This IS a postback
            {
                // If this is a postback from the logon page being inserted in front of the INTERNAL pages,
                // then the postback request needs to be handled by the logon page
                if ((currentMode.Mode == Display_Mode_Enum.Internal) && (sessionUser == null))
                {
                    currentMode.Mode = Display_Mode_Enum.My_Sobek;
                    currentMode.My_Sobek_Type = My_Sobek_Type_Enum.Logon;
                }
            }

            // Set the internal DLC flag
            if (sessionUser != null)
            {
                request.Current_User = sessionUser;

                // Check if this is an administrative task that the current user does not have access to
                if ((!sessionUser.Is_System_Admin) && (!sessionUser.Is_Portal_Admin) && (!sessionUser.Is_User_Admin) && (currentMode.Mode == Display_Mode_Enum.Administrative) && (currentMode.Admin_Type != Admin_Type_Enum.Aggregation_Single))
                {
                    if (sessionUser.LoggedOn)
                    {
                        currentMode.Mode = Display_Mode_Enum.My_Sobek;
                        currentMode.My_Sobek_Type = My_Sobek_Type_Enum.Home;
                    }
                    else
                    {
                        currentMode.Mode = Display_Mode_Enum.Aggregation;
                        currentMode.Aggregation_Type = Aggregation_Type_Enum.Home;
                        currentMode.Aggregation = String.Empty;
                    }
                }
            }
            else
            {
                if ((currentMode.Mode == Display_Mode_Enum.My_Sobek) && (currentMode.My_Sobek_Type != My_Sobek_Type_Enum.Register))
                {
                    currentMode.Logon_Required = true;
                }

                if ((currentMode.Mode == Display_Mode_Enum.Aggregation) && (currentMode.Aggregation_Type == Aggregation_Type_Enum.Home) && (currentMode.Home_Type == Home_Type_Enum.Personalized))
                    currentMode.Home_Type = Home_Type_Enum.List;
            }

            return null;
        }

        #endregion

        #region Shibboleth user code (commented out currently)

        private void check_shibboleth()
        {
            //// If this is a responce from Shibboleth, get the user information and register them if necessary
            //if ((UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth != null) && (UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Enabled))
            //{
            //    string shibboleth_id = null;
            //    try { shibboleth_id = context.Request.ServerVariables[UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.UserIdentityAttribute]; }
            //    catch (InvalidOperationException) { /* IServerVariablesFeature unavailable (Kestrel); Shibboleth via server variables requires IIS hosting */ }
            //    if (shibboleth_id == null)
            //    {
            //        if (UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Debug)
            //        {
            //            tracer.Add_Trace("QueryInitializer.Constructor", UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.UserIdentityAttribute + " server variable NOT found");

            //            // For debugging purposes, if this SHOULD have included SHibboleth information, show in the trace route
            //            if (context.Request.Url.AbsoluteUri.Contains("shibboleth"))
            //            {
            //                foreach (string var in context.Request.ServerVariables)
            //                {
            //                    tracer.Add_Trace("QueryInitializer.Constructor", "Server Variables: " + var + " --> " + context.Request.ServerVariables[var]);
            //                }
            //            }
            //        }
            //    }
            //    else
            //    {
            //        if (UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Debug)
            //        {
            //            tracer.Add_Trace("QueryInitializer.Constructor", UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.UserIdentityAttribute + " server variable found");

            //            tracer.Add_Trace("QueryInitializer.Constructor", UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.UserIdentityAttribute + " server variable = '" + shibboleth_id + "'");

            //            // For debugging purposes, if this SHOULD have included SHibboleth information, show in the trace route
            //            if (context.Request.Url.AbsoluteUri.Contains("shibboleth"))
            //            {
            //                foreach (string var in context.Request.ServerVariables)
            //                {
            //                    tracer.Add_Trace("QueryInitializer.Constructor", "Server Variables: " + var + " --> " + context.Request.ServerVariables[var]);
            //                }
            //            }
            //        }

            //        if (shibboleth_id.Length > 0)
            //        {
            //            tracer.Add_Trace("QueryInitializer.Constructor", "Pulling from database by shibboleth id");

            //            User_Object possible_user_by_shibboleth_id = Engine_Database.Get_User(shibboleth_id, tracer);

            //            // Check to see if we got a valid user back
            //            if (possible_user_by_shibboleth_id != null)
            //            {
            //                if (UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Debug)
            //                {
            //                    // Set the user information from the server variables here 
            //                    foreach (string var in context.Request.ServerVariables)
            //                    {
            //                        User_Object_Attribute_Mapping_Enum mapping = UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Get_User_Object_Mapping(var);
            //                        if (mapping != User_Object_Attribute_Mapping_Enum.NONE)
            //                        {
            //                            string value = context.Request.ServerVariables[var];

            //                            if (UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Debug)
            //                            {
            //                                tracer.Add_Trace("QueryInitializer.Constructor", "Server Variable " + var + " ( " + value + " ) would have been mapped to " + User_Object_Attribute_Mapping_Enum_Converter.ToString(mapping));
            //                            }
            //                        }
            //                        else if (UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Debug)
            //                        {
            //                            tracer.Add_Trace("QueryInitializer.Constructor", "Server Variable " + var + " is not mapped to a user attribute");
            //                        }
            //                    }

            //                    // Set any constants as well
            //                    foreach (Shibboleth_Configuration_Mapping constantMapping in UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Constants)
            //                    {
            //                        if (constantMapping.Mapping != User_Object_Attribute_Mapping_Enum.NONE)
            //                        {
            //                            if (UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Debug)
            //                            {
            //                                tracer.Add_Trace("QueryInitializer.Constructor", "Constant value ( " + constantMapping.Value + " ) would have been set to " + User_Object_Attribute_Mapping_Enum_Converter.ToString(constantMapping.Mapping));
            //                            }
            //                        }
            //                    }
            //                }

            //                tracer.Add_Trace("QueryInitializer.Constructor", "Setting session user from shibboleth id");
            //                possible_user_by_shibboleth_id.Authentication_Type = User_Authentication_Type_Enum.Shibboleth;
            //                context.Session["user"] = possible_user_by_shibboleth_id;
            //            }
            //            else
            //            {
            //                tracer.Add_Trace("QueryInitializer.Constructor", "User from shibboleth id was null.. adding user");

            //                // Now build the user object
            //                User_Object newUser = new User_Object();
            //                if ((context.Request.ServerVariables["HTTP_PRIMARY-AFFILIATION"] != null) && (context.Request.ServerVariables["HTTP_PRIMARY-AFFILIATION"].IndexOf("F") >= 0))
            //                    newUser.Can_Submit = true;
            //                else
            //                    newUser.Can_Submit = false;
            //                newUser.Send_Email_On_Submission = true;
            //                newUser.Email = String.Empty;
            //                newUser.Family_Name = String.Empty;
            //                newUser.Given_Name = String.Empty;
            //                newUser.Organization = String.Empty;
            //                newUser.ShibbID = shibboleth_id;
            //                newUser.UserID = -1;

            //                // Set the user information from the server variables here 
            //                foreach (string var in context.Request.ServerVariables)
            //                {
            //                    User_Object_Attribute_Mapping_Enum mapping = UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Get_User_Object_Mapping(var);
            //                    if (mapping != User_Object_Attribute_Mapping_Enum.NONE)
            //                    {
            //                        string value = context.Request.ServerVariables[var];
            //                        newUser.Set_Value_By_Mapping(mapping, value);

            //                        if (UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Debug)
            //                        {
            //                            tracer.Add_Trace("QueryInitializer.Constructor", "Server Variable " + var + " ( " + value + " ) mapped to " + User_Object_Attribute_Mapping_Enum_Converter.ToString(mapping));
            //                        }
            //                    }
            //                    else if (UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Debug)
            //                    {
            //                        tracer.Add_Trace("QueryInitializer.Constructor", "Server Variable " + var + " is not mapped to a user attribute");
            //                    }
            //                }

            //                // Set any constants as well
            //                foreach (Shibboleth_Configuration_Mapping constantMapping in UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Constants)
            //                {
            //                    if (constantMapping.Mapping != User_Object_Attribute_Mapping_Enum.NONE)
            //                    {
            //                        newUser.Set_Value_By_Mapping(constantMapping.Mapping, constantMapping.Value);

            //                        if (UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Debug)
            //                        {
            //                            tracer.Add_Trace("QueryInitializer.Constructor", "Setting constant value ( " + constantMapping.Value + " ) to " + User_Object_Attribute_Mapping_Enum_Converter.ToString(constantMapping.Mapping));
            //                        }
            //                    }
            //                }

            //                // Set the username
            //                if (String.IsNullOrEmpty(newUser.UserName))
            //                {
            //                    if (newUser.Email.Length > 0)
            //                        newUser.UserName = newUser.Email;
            //                    else
            //                        newUser.UserName = newUser.Family_Name + shibboleth_id;
            //                }

            //                // Set a random password
            //                StringBuilder passwordBuilder = new StringBuilder();
            //                Random randomGenerator = new Random(DateTime.Now.Millisecond);
            //                for (int i = 0; i < 5; i++)
            //                {
            //                    int randomNumber = randomGenerator.Next(97, 122);
            //                    passwordBuilder.Append((char)randomNumber);

            //                    int randomNumber2 = randomGenerator.Next(65, 90);
            //                    passwordBuilder.Append((char)randomNumber2);
            //                }
            //                string password = passwordBuilder.ToString();

            //                // Now, save this user
            //                SobekCM_Database.Save_User(newUser, password, newUser.Authentication_Type, tracer);

            //                // Now, pull back out of the database
            //                User_Object possible_user_by_shib2 = Engine_Database.Get_User(shibboleth_id, tracer);
            //                possible_user_by_shib2.Is_Just_Registered = true;
            //                possible_user_by_shib2.Authentication_Type = User_Authentication_Type_Enum.Shibboleth;
            //                context.Session["user"] = possible_user_by_shib2;
            //            }

            //            if (context.Session["user"] != null)
            //            {
            //                currentMode.Mode = Display_Mode_Enum.My_Sobek;
            //                currentMode.My_Sobek_Type = My_Sobek_Type_Enum.Home;
            //            }
            //            else
            //            {
            //                currentMode.Mode = Display_Mode_Enum.Aggregation;
            //                currentMode.Aggregation_Type = Aggregation_Type_Enum.Home;
            //                currentMode.Aggregation = String.Empty;
            //            }

            //            if (!UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Debug)
            //            {
            //                UrlWriterHelper.Redirect(currentMode);
            //            }
            //        }
            //    }
            //}
        }

        #endregion
    }
}
