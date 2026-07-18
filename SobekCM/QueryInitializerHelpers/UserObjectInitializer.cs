using Microsoft.AspNetCore.Http;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.Navigation;
using SobekCM.Library;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System;

namespace SobekCM.QueryInitializerHelpers
{
    public class UserObjectInitializer : IQueryInitializerHelper
    {
        public QueryInitializerHelperResponse Initialize(HttpContext context, RequestCache request, Custom_Tracer tracer)
        {
            var currentMode = request.Current_Mode;

            if (currentMode == null)
            {
                return new QueryInitializerHelperResponse(false, "The UserObjectInitializer must be called after the NavigationObjectInitializer has configured the NavigationObject");
            }

            if (currentMode.Is_Robot)
            {
                return QueryInitializerHelperResponse.Successful;
            }

            try
            {

                // If this was an error, redirect now
                if (currentMode.Mode == Display_Mode_Enum.Error)
                {
                    return;
                }

                // All the user stuff can be skipped if this was from a robot
                if (!currentMode.Is_Robot)
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
                    if (page_name == "SOBEKCM")
                    {
                        tracer.Add_Trace("QueryInitializer.Constructor", "Checking for logged on user by cookie or session");
                        perform_user_checks(requestSpecificValues.Current_Mode.isPostBack);
                    }

                    // If this is a system admin, they can run as a different user actually
                    if ((currentUser != null) && (currentUser.Is_System_Admin) && (requestSpecificValues.QueryString["userid"] != null))
                    {
                        try
                        {
                            int userid = Convert.ToInt32(requestSpecificValues.QueryString["userid"]);
                            User_Object mirroredUser = Engine_Database.Get_User(userid, tracer);
                            if (mirroredUser != null)
                            {
                                // Replace the user information in the session state
                                context.Items["user"] = mirroredUser;
                                currentUser = mirroredUser;
                            }
                        }
                        catch (Exception)
                        {
                            // Nothing to do here.. shouldn't ever really be here..
                        }
                    }


                }

            }
            catch( Exception ee )
            {



            }
        }

        #region Method performs user checks against session, cookie, database, etc..

        private void perform_user_checks(bool isPostBack)
        {
            // If the mode is NULL or the request was already completed, do nothing
            if ((currentMode == null) || (currentMode.Request_Completed))
                return;

            tracer.Add_Trace("QueryInitializer.Perform_User_Checks", "In user checks portion");

            // If this is to log out of my sobekcm, clear user id and forward back to sobekcm
            if ((currentMode.Mode == Display_Mode_Enum.My_Sobek) && (currentMode.My_Sobek_Type == My_Sobek_Type_Enum.Log_Out))
            {
                tracer.Add_Trace("QueryInitializer.Perform_User_Checks", "User logged out");

                // Delete any user cookie
                HttpCookie userCookie = new HttpCookie("SobekUser");
                userCookie.Values["userid"] = String.Empty;
                userCookie.Values["security_hash"] = String.Empty;
                userCookie.Expires = DateTime.Now.AddDays(-1);
                HttpContext.Current.Response.Cookies.Add(userCookie);

                // Delete from memory
                HttpContext.Current.Session["userid"] = 0;
                HttpContext.Current.Session["user"] = null;

                // Determine new redirect location
                string redirect = currentMode.Base_URL;
                if (!String.IsNullOrEmpty(currentMode.Return_URL))
                {
                    redirect = currentMode.Base_URL + currentMode.Return_URL;

                    if (((currentMode.Return_URL.IndexOf("admin") >= 0) && (currentMode.Return_URL.IndexOf("admin") <= 1)) ||
                        ((currentMode.Return_URL.IndexOf("mysobek") >= 0) && (currentMode.Return_URL.IndexOf("mysobek") <= 1)))
                        redirect = currentMode.Base_URL;
                }

                HttpContext.Current.Response.Redirect(redirect, false);
                HttpContext.Current.ApplicationInstance.CompleteRequest();
                currentMode.Request_Completed = true;
                return;
            }

            // If there is already a user logged on, do nothing here
            if (HttpContext.Current.Session["user"] == null)
            {
                // If this is a responce from Shibboleth, get the user information and register them if necessary
                if ((UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth != null) && (UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Enabled))
                {
                    string shibboleth_id = null;
                    try { shibboleth_id = HttpContext.Current.Request.ServerVariables[UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.UserIdentityAttribute]; }
                    catch (InvalidOperationException) { /* IServerVariablesFeature unavailable (Kestrel); Shibboleth via server variables requires IIS hosting */ }
                    if (shibboleth_id == null)
                    {
                        if (UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Debug)
                        {
                            tracer.Add_Trace("QueryInitializer.Constructor", UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.UserIdentityAttribute + " server variable NOT found");

                            // For debugging purposes, if this SHOULD have included SHibboleth information, show in the trace route
                            if (HttpContext.Current.Request.Url.AbsoluteUri.Contains("shibboleth"))
                            {
                                foreach (string var in HttpContext.Current.Request.ServerVariables)
                                {
                                    tracer.Add_Trace("QueryInitializer.Constructor", "Server Variables: " + var + " --> " + HttpContext.Current.Request.ServerVariables[var]);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Debug)
                        {
                            tracer.Add_Trace("QueryInitializer.Constructor", UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.UserIdentityAttribute + " server variable found");

                            tracer.Add_Trace("QueryInitializer.Constructor", UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.UserIdentityAttribute + " server variable = '" + shibboleth_id + "'");

                            // For debugging purposes, if this SHOULD have included SHibboleth information, show in the trace route
                            if (HttpContext.Current.Request.Url.AbsoluteUri.Contains("shibboleth"))
                            {
                                foreach (string var in HttpContext.Current.Request.ServerVariables)
                                {
                                    tracer.Add_Trace("QueryInitializer.Constructor", "Server Variables: " + var + " --> " + HttpContext.Current.Request.ServerVariables[var]);
                                }
                            }
                        }

                        if (shibboleth_id.Length > 0)
                        {
                            tracer.Add_Trace("QueryInitializer.Constructor", "Pulling from database by shibboleth id");

                            User_Object possible_user_by_shibboleth_id = Engine_Database.Get_User(shibboleth_id, tracer);

                            // Check to see if we got a valid user back
                            if (possible_user_by_shibboleth_id != null)
                            {
                                if (UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Debug)
                                {
                                    // Set the user information from the server variables here 
                                    foreach (string var in HttpContext.Current.Request.ServerVariables)
                                    {
                                        User_Object_Attribute_Mapping_Enum mapping = UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Get_User_Object_Mapping(var);
                                        if (mapping != User_Object_Attribute_Mapping_Enum.NONE)
                                        {
                                            string value = HttpContext.Current.Request.ServerVariables[var];

                                            if (UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Debug)
                                            {
                                                tracer.Add_Trace("QueryInitializer.Constructor", "Server Variable " + var + " ( " + value + " ) would have been mapped to " + User_Object_Attribute_Mapping_Enum_Converter.ToString(mapping));
                                            }
                                        }
                                        else if (UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Debug)
                                        {
                                            tracer.Add_Trace("QueryInitializer.Constructor", "Server Variable " + var + " is not mapped to a user attribute");
                                        }
                                    }

                                    // Set any constants as well
                                    foreach (Shibboleth_Configuration_Mapping constantMapping in UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Constants)
                                    {
                                        if (constantMapping.Mapping != User_Object_Attribute_Mapping_Enum.NONE)
                                        {
                                            if (UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Debug)
                                            {
                                                tracer.Add_Trace("QueryInitializer.Constructor", "Constant value ( " + constantMapping.Value + " ) would have been set to " + User_Object_Attribute_Mapping_Enum_Converter.ToString(constantMapping.Mapping));
                                            }
                                        }
                                    }
                                }

                                tracer.Add_Trace("QueryInitializer.Constructor", "Setting session user from shibboleth id");
                                possible_user_by_shibboleth_id.Authentication_Type = User_Authentication_Type_Enum.Shibboleth;
                                HttpContext.Current.Session["user"] = possible_user_by_shibboleth_id;
                            }
                            else
                            {
                                tracer.Add_Trace("QueryInitializer.Constructor", "User from shibboleth id was null.. adding user");

                                // Now build the user object
                                User_Object newUser = new User_Object();
                                if ((HttpContext.Current.Request.ServerVariables["HTTP_PRIMARY-AFFILIATION"] != null) && (HttpContext.Current.Request.ServerVariables["HTTP_PRIMARY-AFFILIATION"].IndexOf("F") >= 0))
                                    newUser.Can_Submit = true;
                                else
                                    newUser.Can_Submit = false;
                                newUser.Send_Email_On_Submission = true;
                                newUser.Email = String.Empty;
                                newUser.Family_Name = String.Empty;
                                newUser.Given_Name = String.Empty;
                                newUser.Organization = String.Empty;
                                newUser.ShibbID = shibboleth_id;
                                newUser.UserID = -1;

                                // Set the user information from the server variables here 
                                foreach (string var in HttpContext.Current.Request.ServerVariables)
                                {
                                    User_Object_Attribute_Mapping_Enum mapping = UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Get_User_Object_Mapping(var);
                                    if (mapping != User_Object_Attribute_Mapping_Enum.NONE)
                                    {
                                        string value = HttpContext.Current.Request.ServerVariables[var];
                                        newUser.Set_Value_By_Mapping(mapping, value);

                                        if (UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Debug)
                                        {
                                            tracer.Add_Trace("QueryInitializer.Constructor", "Server Variable " + var + " ( " + value + " ) mapped to " + User_Object_Attribute_Mapping_Enum_Converter.ToString(mapping));
                                        }
                                    }
                                    else if (UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Debug)
                                    {
                                        tracer.Add_Trace("QueryInitializer.Constructor", "Server Variable " + var + " is not mapped to a user attribute");
                                    }
                                }

                                // Set any constants as well
                                foreach (Shibboleth_Configuration_Mapping constantMapping in UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Constants)
                                {
                                    if (constantMapping.Mapping != User_Object_Attribute_Mapping_Enum.NONE)
                                    {
                                        newUser.Set_Value_By_Mapping(constantMapping.Mapping, constantMapping.Value);

                                        if (UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Debug)
                                        {
                                            tracer.Add_Trace("QueryInitializer.Constructor", "Setting constant value ( " + constantMapping.Value + " ) to " + User_Object_Attribute_Mapping_Enum_Converter.ToString(constantMapping.Mapping));
                                        }
                                    }
                                }

                                // Set the username
                                if (String.IsNullOrEmpty(newUser.UserName))
                                {
                                    if (newUser.Email.Length > 0)
                                        newUser.UserName = newUser.Email;
                                    else
                                        newUser.UserName = newUser.Family_Name + shibboleth_id;
                                }

                                // Set a random password
                                StringBuilder passwordBuilder = new StringBuilder();
                                Random randomGenerator = new Random(DateTime.Now.Millisecond);
                                for (int i = 0; i < 5; i++)
                                {
                                    int randomNumber = randomGenerator.Next(97, 122);
                                    passwordBuilder.Append((char)randomNumber);

                                    int randomNumber2 = randomGenerator.Next(65, 90);
                                    passwordBuilder.Append((char)randomNumber2);
                                }
                                string password = passwordBuilder.ToString();

                                // Now, save this user
                                SobekCM_Database.Save_User(newUser, password, newUser.Authentication_Type, tracer);

                                // Now, pull back out of the database
                                User_Object possible_user_by_shib2 = Engine_Database.Get_User(shibboleth_id, tracer);
                                possible_user_by_shib2.Is_Just_Registered = true;
                                possible_user_by_shib2.Authentication_Type = User_Authentication_Type_Enum.Shibboleth;
                                HttpContext.Current.Session["user"] = possible_user_by_shib2;
                            }

                            if (HttpContext.Current.Session["user"] != null)
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

                            if (!UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Debug)
                            {
                                UrlWriterHelper.Redirect(currentMode);
                            }
                        }
                    }
                }

                // If the user information is still missing , but the SobekUser cookie exists, then pull 
                // the user information from the SobekUser cookie in the current requests
                if ((HttpContext.Current.Session["user"] == null) && (HttpContext.Current.Request.Cookies["SobekUser"] != null))
                {
                    string userid_string = HttpContext.Current.Request.Cookies["SobekUser"]["userid"];
                    int userid = -1;

                    bool valid_perhaps = userid_string.All(Char.IsNumber);
                    if (valid_perhaps)
                        Int32.TryParse(userid_string, out userid);

                    if (userid > 0)
                    {
                        User_Object possible_user = Engine_Database.Get_User(userid, tracer);
                        if (possible_user != null)
                        {
                            string cookie_security_hash = HttpContext.Current.Request.Cookies["SobekUser"]["security_hash"];
                            if (cookie_security_hash == possible_user.Security_Hash(HttpContext.Current.Request.UserHostAddress))
                            {
                                HttpContext.Current.Session["user"] = possible_user;
                            }
                            else
                            {
                                // Security hash did not match, so clear the cookie
                                HttpCookie userCookie = new HttpCookie("SobekUser");
                                userCookie.Values["userid"] = String.Empty;
                                userCookie.Values["security_hash"] = String.Empty;
                                userCookie.Expires = DateTime.Now.AddDays(-1);
                                HttpContext.Current.Response.Cookies.Add(userCookie);
                            }
                        }
                    }
                }
            }

            // If this is not a post back, set the html writer code to 'l' or 'h' depending on whether logged on
            if (!isPostBack)
            {
                if (HttpContext.Current.Session["user"] != null)
                {
                    if (currentMode.Writer_Type == Writer_Type_Enum.HTML)
                    {
                        // If this is really a deprecated URL, don't try to forwaard
                        if ((currentMode.Mode != Display_Mode_Enum.Item_Display) || (currentMode.BibID.Length > 0) || (currentMode.ItemID_DEPRECATED <= 0))
                        {
                            currentMode.Writer_Type = Writer_Type_Enum.HTML_LoggedIn;
                            UrlWriterHelper.Redirect(currentMode);
                            return;
                        }
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
                                // If this is really a deprecated URL, don't try to forwaard
                                if ((currentMode.BibID.Length > 0) || (currentMode.ItemID_DEPRECATED <= 0))
                                {
                                    UrlWriterHelper.Redirect(currentMode);
                                    return;
                                }
                                break;

                            default:
                                currentMode.Writer_Type = Writer_Type_Enum.HTML;
                                UrlWriterHelper.Redirect(currentMode);
                                return;

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
                if ((currentMode.Mode == Display_Mode_Enum.Internal) && (HttpContext.Current.Session["user"] == null))
                {
                    currentMode.Mode = Display_Mode_Enum.My_Sobek;
                    currentMode.My_Sobek_Type = My_Sobek_Type_Enum.Logon;
                }
            }

            // Set the internal DLC flag
            if (HttpContext.Current.Session["user"] != null)
            {
                currentUser = (User_Object)HttpContext.Current.Session["user"];

                // Check if this is an administrative task that the current user does not have access to
                if ((!currentUser.Is_System_Admin) && (!currentUser.Is_Portal_Admin) && (!currentUser.Is_User_Admin) && (currentMode.Mode == Display_Mode_Enum.Administrative) && (currentMode.Admin_Type != Admin_Type_Enum.Aggregation_Single))
                {
                    if (currentUser.LoggedOn)
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

            //// Create the empty user
            //if (currentUser == null)
            //{
            //    currentUser = new User_Object();
            //    HttpContext.Current.Session["user"] = currentUser;
            //}
        }

        #endregion
    }
}
