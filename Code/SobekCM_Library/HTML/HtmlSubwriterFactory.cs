#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.Navigation;
using SobekCM.Library.MainWriters;
using SobekCM.Tools;
using System;
using System.Text;

#endregion

namespace SobekCM.Library.HTML
{
    /// <summary> Factory which builds the appropriate <see cref="abstractHtmlSubwriter"/> instance for the
    /// current request, based on the <see cref="Display_Mode_Enum"/> value on the current mode. </summary>
    public static class HtmlSubwriterFactory
    {
        /// <summary> Builds the html subwriter instance appropriate for the current request mode </summary>
        /// <param name="Context"> Context for this individual HTTP request </param>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        /// <returns> Newly constructed html subwriter appropriate to the current mode </returns>
        public static abstractHtmlSubwriter Create(HttpContext Context, RequestCache RequestSpecificValues)
        {
            abstractHtmlSubwriter subwriter = null;

            // Create the html sub writer now
            switch (RequestSpecificValues.Current_Mode.Mode)
            {
                case Display_Mode_Enum.Internal:
                    RequestSpecificValues.Tracer.Add_Trace("HtmlSubwriterFactory.Create", "Internal html sub writer.");
                    subwriter = new Internal_HtmlSubwriter(RequestSpecificValues);
                    break;

                case Display_Mode_Enum.Statistics:
                    RequestSpecificValues.Tracer.Add_Trace("HtmlSubwriterFactory.Create", "Statistics html sub writer.");
                    subwriter = new Statistics_HtmlSubwriter(RequestSpecificValues);
                    break;

                case Display_Mode_Enum.Preferences:
                    RequestSpecificValues.Tracer.Add_Trace("HtmlSubwriterFactory.Create", "Preferences html sub writer.");
                    subwriter = new Preferences_HtmlSubwriter(RequestSpecificValues);
                    break;

                case Display_Mode_Enum.Empty:
                    RequestSpecificValues.Tracer.Add_Trace("HtmlSubwriterFactory.Create", "Empty html sub writer.");
                    subwriter = new Empty_HtmlSubwriter(RequestSpecificValues);
                    break;

                case Display_Mode_Enum.Error:
                    RequestSpecificValues.Tracer.Add_Trace("HtmlSubwriterFactory.Create", "Error html sub writer.");
                    subwriter = new Error_HtmlSubwriter(false, RequestSpecificValues);
                    // Send the email now
                    if (RequestSpecificValues.Current_Mode.Caught_Exception != null)
                    {
                        if (String.IsNullOrEmpty(RequestSpecificValues.Current_Mode.Error_Message))
                            RequestSpecificValues.Current_Mode.Error_Message = "Unknown exception caught";
                        Html_MainWriter.Email_Information(RequestSpecificValues.Current_Mode.Error_Message, RequestSpecificValues.Current_Mode.Caught_Exception, RequestSpecificValues.Tracer, false, Context);
                    }
                    break;

                case Display_Mode_Enum.Legacy_URL:
                    RequestSpecificValues.Tracer.Add_Trace("HtmlSubwriterFactory.Create", "Legacy URL html sub writer.");
                    subwriter = new LegacyUrl_HtmlSubwriter(RequestSpecificValues);
                    break;

                case Display_Mode_Enum.Item_Print:
                    RequestSpecificValues.Tracer.Add_Trace("HtmlSubwriterFactory.Create", "Item print html sub writer.");
                    subwriter = new Print_Item_HtmlSubwriter(RequestSpecificValues);
                    break;

                case Display_Mode_Enum.Contact:
                    RequestSpecificValues.Tracer.Add_Trace("HtmlSubwriterFactory.Create", "Contact html sub writer.");
                    var builder = new StringBuilder();
                    builder.Append("\n\nSUBMISSION INFORMATION\n");
                    builder.Append("\tDate:\t\t\t\t" + DateTime.Now.ToString() + "\n");
                    string lastMode = String.Empty;
                    try
                    {
                        if (Context.Session.GetString(SessionCache_Keys.LastMode) != null)
                            lastMode = Context.Session.GetString(SessionCache_Keys.LastMode);

                        builder.Append("\tIP Address:\t\t\t" + (Context.Connection.RemoteIpAddress?.ToString() ?? "") + "\n");
                        builder.Append("\tHost Name:\t\t\t" + (Context.Connection.RemoteIpAddress?.ToString() ?? "") + "\n");
                        string contactUserAgent = Context.Request.Headers["User-Agent"].ToString();
                        builder.Append("\tBrowser:\t\t\t" + contactUserAgent + "\n");
                        builder.Append("\tBrowser Platform:\t\t" + "UNKNOWN" + "\n");
                        builder.Append("\tBrowser Version:\t\t" + "UNKNOWN" + "\n");
                        builder.Append("\tBrowser Language:\t\t");
                        bool first = true;
                        string acceptLangHeader = Context.Request.Headers["Accept-Language"].ToString();
                        string[] languages = string.IsNullOrEmpty(acceptLangHeader) ? null : acceptLangHeader.Split(',');

                        if (languages != null)
                            foreach (string thisLanguage in languages)
                            {
                                if (first)
                                {
                                    builder.Append(thisLanguage);
                                    first = false;
                                }
                                else
                                {
                                    builder.Append(", " + thisLanguage);
                                }
                            }

                        builder.Append("\n\nHISTORY\n");
                        if (Context.Session.GetString(SessionCache_Keys.LastSearch) != null)
                            builder.Append("\tLast Search:\t\t" + Context.Session.GetString(SessionCache_Keys.LastSearch) + "\n");
                        if (Context.Session.GetString(SessionCache_Keys.LastResults) != null)
                            builder.Append("\tLast Results:\t\t" + Context.Session.GetString(SessionCache_Keys.LastResults) + "\n");
                        if (Context.Session.GetString(SessionCache_Keys.LastMode) != null)
                            builder.Append("\tLast Mode:\t\t\t" + Context.Session.GetString(SessionCache_Keys.LastMode) + "\n");
                        builder.Append("\tURL:\t\t\t\t" + Context.Items[RequestCache_Keys.OriginalUrl]);
                    }
                    catch
                    {

                    }
                    subwriter = new Contact_HtmlSubwriter(lastMode, builder.ToString(), RequestSpecificValues);
                    break;

                case Display_Mode_Enum.Contact_Sent:
                    RequestSpecificValues.Tracer.Add_Trace("HtmlSubwriterFactory.Create", "Contact sent html sub writer.");
                    subwriter = new Contact_HtmlSubwriter(String.Empty, String.Empty, RequestSpecificValues);
                    break;

                case Display_Mode_Enum.Simple_HTML_CMS:
                    RequestSpecificValues.Tracer.Add_Trace("HtmlSubwriterFactory.Create", "Simple html cms html sub writer.");
                    subwriter = new Web_Content_HtmlSubwriter(RequestSpecificValues);
                    break;

                case Display_Mode_Enum.My_Sobek:
                    RequestSpecificValues.Tracer.Add_Trace("HtmlSubwriterFactory.Create", "My sobek html sub writer.");
                    subwriter = new MySobek_HtmlSubwriter(RequestSpecificValues);
                    break;

                case Display_Mode_Enum.Administrative:
                    RequestSpecificValues.Tracer.Add_Trace("HtmlSubwriterFactory.Create", "Administrative html sub writer.");
                    subwriter = new Admin_HtmlSubwriter(RequestSpecificValues);
                    break;

                case Display_Mode_Enum.Results:
                    RequestSpecificValues.Tracer.Add_Trace("HtmlSubwriterFactory.Create", "Results html sub writer.");
                    subwriter = new Search_Results_HtmlSubwriter(RequestSpecificValues);
                    break;

                case Display_Mode_Enum.Public_Folder:
                    RequestSpecificValues.Tracer.Add_Trace("HtmlSubwriterFactory.Create", "Public folder html sub writer.");
                    subwriter = new Public_Folder_HtmlSubwriter(RequestSpecificValues);
                    break;

                case Display_Mode_Enum.Search:
                case Display_Mode_Enum.Aggregation:
                    RequestSpecificValues.Tracer.Add_Trace("HtmlSubwriterFactory.Create", "Search or Aggregation html sub writer.");
                    subwriter = new Aggregation_HtmlSubwriter(RequestSpecificValues);
                    break;

                case Display_Mode_Enum.Item_Display:
                    RequestSpecificValues.Tracer.Add_Trace("HtmlSubwriterFactory.Create", "Item display html sub writer.");
                    if ((!RequestSpecificValues.Current_Mode.Invalid_Item.HasValue || !RequestSpecificValues.Current_Mode.Invalid_Item.Value))
                    {
                        // Create the item viewer writer
                        subwriter = new Item_HtmlSubwriter(RequestSpecificValues);
                    }
                    else
                    {
                        // Create the invalid item html subwrite and write the HTML
                        subwriter = new Error_HtmlSubwriter(true, RequestSpecificValues);
                    }
                    break;
            }

            // Now, look for error, which is also often used for resource missing type errors
            if (RequestSpecificValues.Current_Mode.Mode == Display_Mode_Enum.Error)
            {
                RequestSpecificValues.Tracer.Add_Trace("HtmlSubwriterFactory.Create", "Error html sub writer.");
                subwriter = new Error_HtmlSubwriter(false, RequestSpecificValues);
                // Send the email now
                if (RequestSpecificValues.Current_Mode.Caught_Exception != null)
                {
                    if (String.IsNullOrEmpty(RequestSpecificValues.Current_Mode.Error_Message))
                        RequestSpecificValues.Current_Mode.Error_Message = "Unknown exception caught";
                    Html_MainWriter.Email_Information(RequestSpecificValues.Current_Mode.Error_Message, RequestSpecificValues.Current_Mode.Caught_Exception, RequestSpecificValues.Tracer, false, Context);
                }
            }

            // Item_HtmlSubwriter's constructor can discover -- only partway through construction,
            // once it asks the engine for the item and gets back nothing -- that the requested
            // BibID/VID doesn't exist, and reacts by switching the mode to Simple_HTML_CMS so the
            // "missing resource" web content page displays instead. But by that point the switch
            // above has already created (and returned as far as this method is concerned) the
            // Item_HtmlSubwriter itself, which stops initializing as soon as it flips the mode, so
            // fields like its Subwriter_Behaviors list are left null. Re-create the subwriter here
            // so the fully-initialized Web_Content_HtmlSubwriter actually handles the request.
            if ((RequestSpecificValues.Current_Mode.Mode == Display_Mode_Enum.Simple_HTML_CMS) && (!(subwriter is Web_Content_HtmlSubwriter)))
            {
                RequestSpecificValues.Tracer.Add_Trace("HtmlSubwriterFactory.Create", "Item html sub writer switched to simple html cms html sub writer.");
                subwriter = new Web_Content_HtmlSubwriter(RequestSpecificValues);
            }

            return subwriter;
        }
    }
}
