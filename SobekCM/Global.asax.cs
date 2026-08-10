#region Using directives

using System;
using System.IO;
using System.Net;
using System.Web;
using SobekCM.Library.UI;
using SobekCM.Tools;

#endregion

namespace SobekCM
{
	public class Global : HttpApplication
	{


		protected void Application_Start(object sender, EventArgs e)
		{
			// MicroservicesClientBase makes synchronous, blocking HttpWebRequest calls to the engine
			// for most page loads. The .NET Framework default outbound-connection-limit-per-host is
			// too low for concurrent production traffic, and requests queue up waiting for a free
			// connection to the engine host once real load hits, rather than actually failing.
			ServicePointManager.DefaultConnectionLimit = 200;

			// The ThreadPool only grows by ~1 thread per ~500ms once exhausted, so a sudden burst of
			// concurrent requests -- each holding a worker thread for the duration of a blocking engine
			// call -- stalls badly before the pool ramps up, even though the eventual max would be fine.
			// Raising the minimum avoids that slow ramp-up under real traffic.
			ThreadPool.SetMinThreads(200, 200);
		}

		protected void Session_Start(object sender, EventArgs e)
		{
            // This initializes the session, by assigning SOME value.
            // Since the session object has been accessed, the session id will now be
            // fixed for this session
            Session["init"] = true;
		}

		protected void Application_BeginRequest(object sender, EventArgs e)
		{

		}

		protected void Application_AuthenticateRequest(object sender, EventArgs e)
		{

		}

		protected void Application_Error(object Sender, EventArgs E)
		{
			// Get the exception
			Exception originalErr = Server.GetLastError();
			if (originalErr == null)
				return;

			// GetBaseException() unwraps down to the innermost exception, which loses any context
			// added by wrapping exceptions along the way (e.g., MicroservicesClientBase.Deserialize
			// wraps WebException in an ApplicationException whose message includes the endpoint URI
			// that was actually being called -- log that outer message too, not just the base one).
			Exception objErr = originalErr.GetBaseException();

			try
			{
				// Justs clear the error for a number of common errors, caused by invalid requests to the server
				if ((objErr.Message.IndexOf("potentially dangerous") >= 0) || (objErr.Message.IndexOf("a control with id ") >= 0) || (objErr.Message.IndexOf("Padding is invalid and cannot be removed") >= 0) || (objErr.Message.IndexOf("This is an invalid webresource request") >= 0) ||
					((objErr.Message.IndexOf("File") >= 0) && (objErr.Message.IndexOf("does not exist") >= 0)))
				{
					// Clear the error
					Server.ClearError();
				}
				else
				{
					try
					{
						StreamWriter writer = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "\\temp\\exceptions.txt", true);
						writer.WriteLine();
						writer.WriteLine("Error Caught in Application_Error event ( " + DateTime.Now.ToString() + ")");
						writer.WriteLine("User Host Address: " + Request.UserHostAddress);
						writer.WriteLine("Requested URL: " + Request.Url);
                        if ( Request.UrlReferrer != null )
                            writer.WriteLine("Http Referrer: " + Request.UrlReferrer);
                        if (objErr is SobekCM_Traced_Exception)
						{
							SobekCM_Traced_Exception sobekException = (SobekCM_Traced_Exception)objErr;

							writer.WriteLine("Error Message: " + sobekException.InnerException.Message);
							writer.WriteLine("Stack Trace: " + objErr.StackTrace);
							writer.WriteLine("Error Message:" + sobekException.InnerException.StackTrace);
							writer.WriteLine();
							writer.WriteLine(sobekException.Trace_Route);
						}
						else
						{

							writer.WriteLine("Error Message: " + objErr.Message);

							// Walk the full chain, not just the outermost exception -- ASP.NET wraps
							// unhandled exceptions in HttpUnhandledException, and application code may
							// add its own wrapping exception(s) in between (e.g. MicroservicesClientBase.
							// Deserialize embeds the endpoint URI in an ApplicationException), so the
							// useful context can be neither the outermost nor the innermost message.
							Exception chainEx = originalErr;
							while ((chainEx != null) && (chainEx != objErr))
							{
								if (chainEx.Message != objErr.Message)
									writer.WriteLine("Outer Error Message: " + chainEx.Message);
								chainEx = chainEx.InnerException;
							}

							writer.WriteLine("Stack Trace: " + objErr.StackTrace);
						}

						writer.WriteLine();
						writer.WriteLine("------------------------------------------------------------------");
						writer.Flush();
						writer.Close();
					}
					catch (Exception)
					{
						// Nothing else to do here.. no other known way to log this error
					}
				}
			}
			catch (Exception)
			{
				// Nothing else to do here.. no other known way to log this error
			}
			finally
			{
				// Clear the error
				Server.ClearError();

				string error_message = objErr.Message;
				if (objErr is SobekCM_Traced_Exception)
				{
					SobekCM_Traced_Exception sobekException = (SobekCM_Traced_Exception)objErr;
					error_message = sobekException.InnerException.Message;

				}

				try
				{
					if ((HttpContext.Current.Request.UserHostAddress == "127.0.0.1") || (HttpContext.Current.Request.UserHostAddress == HttpContext.Current.Request.ServerVariables["LOCAL_ADDR"]) || (HttpContext.Current.Request.Url.ToString().IndexOf("localhost") >= 0))
					{
						Response.Redirect("error_echo.html?text=" + error_message.Replace(" ", "_").Replace("&", "and").Replace("?", ""), false);
					}
					else
					{
						// Forward if there is a place to forward to.
						if (!String.IsNullOrEmpty(UI_ApplicationCache_Gateway.Settings.Servers.System_Error_URL))
						{
							Response.Redirect(UI_ApplicationCache_Gateway.Settings.Servers.System_Error_URL, false);
						}
						else
						{
							Response.Redirect("http://ufdc.ufl.edu/sobekcm/missing_config", false);
						}
					}
				}
				catch (Exception)
				{
					// Nothing else to do here.. no other known way to log this error
				}
			}

		}

		protected void Session_End(object sender, EventArgs e)
		{

		}

		protected void Application_End(object sender, EventArgs e)
		{

		}
	}
}