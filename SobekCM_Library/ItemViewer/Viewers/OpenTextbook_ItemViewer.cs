using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI.WebControls;
using SobekCM.Core.BriefItem;
using SobekCM.Core.FileSystems;
using SobekCM.Core.Navigation;
using SobekCM.Core.Users;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Library.ItemViewer.Menu;
using SobekCM.Tools;
using SobekCM.Library.Helpers.CKEditor;
using System.Collections.Specialized;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.Client;

namespace SobekCM.Library.ItemViewer.Viewers
{
    /// <summary> OpenTextbook viewer prototyper, which is used to check to see if a (non-thumbnail) OpenTextbook file exists, 
    /// to create the link in the main menu, and to create the viewer itself if the user selects that option </summary>
    public class OpenTextbook_ItemViewer_Prototyper : abstractItemViewerPrototyper
    {
        public const string VIEWER_TYPE = "OPEN_TEXTBOOK";

        /// <summary> Constructor for a new instance of the OpenTextbook_ItemViewer_Prototyper class </summary>
        public OpenTextbook_ItemViewer_Prototyper()
        {
            ViewerType = VIEWER_TYPE;
            ViewerCode = "#o";
        }

        /// <summary> Indicates if the specified item matches the basic requirements for this viewer, or
        /// if this viewer should be ignored for this item </summary>
        /// <param name="CurrentItem"> Digital resource to examine to see if this viewer really should be included </param>
        /// <returns> TRUE if this viewer should generally be included with this item, otherwise FALSE </returns>
        public override bool Include_Viewer(BriefItemInfo CurrentItem)
        {
            return true;
        }

        /// <summary> Flag indicates if this viewer should be override on checkout </summary>
        /// <param name="CurrentItem"> Digital resource to examine to see if this viewer should really be overriden </param>
        /// <returns> TRUE always, since PDFs should never be shown if an item is checked out </returns>
        public override bool Override_On_Checkout(BriefItemInfo CurrentItem)
        {
            return true;
        }

        /// <summary> Flag indicates if the current user has access to this viewer for the item </summary>
        /// <param name="CurrentItem"> Digital resource to see if the current user has correct permissions to use this viewer </param>
        /// <param name="CurrentUser"> Current user, who may or may not be logged on </param>
        /// <param name="IsRestricted"> Flag indicates if this item is restricted AND the current user is outside the ranges or not in the proper groups</param>
        /// <returns> TRUE if the user has access to use this viewer, otherwise FALSE </returns>
        public override bool Has_Access(BriefItemInfo CurrentItem, User_Object CurrentUser, bool IsRestricted)
        {
            return !IsRestricted;
        }

        /// <summary> Gets the menu items related to this viewer that should be included on the main item (digital resource) menu </summary>
        /// <param name="CurrentItem"> Digital resource object, which can be used to ensure if and how this viewer should appear 
        /// in the main item (digital resource) menu </param>
        /// <param name="CurrentUser"> Current user, who may or may not be logged on </param>
        /// <param name="CurrentRequest"> Information about the current request </param>
        /// <param name="MenuItems"> List of menu items, to which this method may add one or more menu items </param>
        /// <param name="IsRestricted"> Flag indicates if this item is restricted AND the current user is outside the ranges or not in the proper groups</param>
        public override void Add_Menu_Items(BriefItemInfo CurrentItem, User_Object CurrentUser, Navigation_Object CurrentRequest, List<Item_MenuItem> MenuItems, bool IsRestricted)
        {
            int current_page = 1;
            string previous_code = "1";

            // Get the URL for this
            if (CurrentRequest.ViewerCode != null)
            {
                previous_code = CurrentRequest.ViewerCode.Replace("o", "");
                if (!int.TryParse(previous_code, out current_page))
                    current_page = 1;
            }

            CurrentRequest.ViewerCode = ViewerCode.Replace("#", current_page.ToString());
            string url = UrlWriterHelper.Redirect_URL(CurrentRequest);
            CurrentRequest.ViewerCode = previous_code;

            // Add the item menu information
            Item_MenuItem menuItem = new Item_MenuItem("Open Textbook", null, null, url, ViewerCode);
            MenuItems.Add(menuItem);
        }

        /// <summary> Creates and returns the an instance of the <see cref="OpenTextbook_ItemViewer"/> class for showing a  
        /// OpenTextbook image from a page within a digital resource during execution of a single HTTP request. </summary>
        /// <param name="CurrentItem"> Digital resource object </param>
        /// <param name="CurrentUser"> Current user, who may or may not be logged on </param>
        /// <param name="CurrentRequest"> Information about the current request </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> Fully built and initialized <see cref="OpenTextbook_ItemViewer"/> object </returns>
        /// <remarks> This method is called whenever a request requires the actual viewer to be created to render the HTML for
        /// the digital resource requested.  The created viewer is then destroyed at the end of the request </remarks>
        public override iItemViewer Create_Viewer(BriefItemInfo CurrentItem, User_Object CurrentUser, Navigation_Object CurrentRequest, Custom_Tracer Tracer)
        {
            return new OpenTextbook_ItemViewer(CurrentItem, CurrentUser, CurrentRequest, Tracer, ViewerCode.ToLower(), FileExtensions);
        }
    }

    /// <summary> Item page viewer displays the a OpenTextbook from the page images within a digital resource. </summary>
    /// <remarks> This class extends the abstract class <see cref="abstractPageFilesItemViewer"/> and implements the 
    /// <see cref="iItemViewer" /> interface. </remarks>
    public class OpenTextbook_ItemViewer : abstractPageFilesItemViewer
    {
        // information about the page to display
        private readonly int page;
        private readonly bool isEditMode;
        private readonly bool canEdit;
        private string filename;

        /// <summary> Constructor for a new instance of the OpenTextbook_ItemViewer class, used to display OpenTextbooks linked to
        /// pages in a digital resource </summary>
        /// <param name="BriefItem"> Digital resource object </param>
        /// <param name="CurrentUser"> Current user, who may or may not be logged on </param>
        /// <param name="CurrentRequest"> Information about the current request </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <param name="OpenTextbook_ViewerCode"> OpenTextbook viewer code, as determined by configuration files </param>
        /// <param name="FileExtensions"> File extensions that this viewer allows, as determined by configuration files </param>
        public OpenTextbook_ItemViewer(BriefItemInfo BriefItem, User_Object CurrentUser, Navigation_Object CurrentRequest, Custom_Tracer Tracer, string OpenTextbook_ViewerCode, string[] FileExtensions)
        {
            // Add the trace
            if (Tracer != null)
                Tracer.Add_Trace("OpenTextbook_ItemViewer.Constructor");

            // Save the arguments for use later
            this.BriefItem = BriefItem;
            this.CurrentUser = CurrentUser;
            this.CurrentRequest = CurrentRequest;

            // Set the behavior properties
            Behaviors = EmptyBehaviors;

            // Determine the page
            page = 1;
            if (!String.IsNullOrEmpty(CurrentRequest.ViewerCode))
            {
                int tempPageParse;
                if (Int32.TryParse(CurrentRequest.ViewerCode.Replace(OpenTextbook_ViewerCode.Replace("#", ""), ""), out tempPageParse))
                    page = tempPageParse;
            }

            

            // Just a quick range check
            if (( BriefItem.OpenTextbook_Pages != null ) && (page > BriefItem.OpenTextbook_Pages.Count))
                page = 1;

            // Since this is a paging viewer, set the viewer code
            if (String.IsNullOrEmpty(CurrentRequest.ViewerCode))
                CurrentRequest.ViewerCode = OpenTextbook_ViewerCode.Replace("#", page.ToString());

            // Can the user edit this?
            canEdit = CurrentUser != null && CurrentUser.LoggedOn;

            // Is this in edit mode?
            isEditMode = false;
            if ((!String.IsNullOrEmpty(CurrentRequest.ViewerSubCode)) && ( CurrentRequest.ViewerSubCode == "edit"))
            {
                isEditMode = true;
            }

            // Get the file info
            set_file_information(new string[] { "HTML" });

            // Handle postbacks
            NameValueCollection form = HttpContext.Current.Request.Form;
            if ((canEdit) && (isEditMode) && (form["sbkOeriv_HtmlEdit"] != null))
            {
                string newSource = form["sbkOeriv_HtmlEdit"];
                if (!String.IsNullOrEmpty(newSource))
                {
                    // Get the file/path for the HTML file
                    string file = SobekFileSystem.Resource_Network_Uri(BriefItem, filename);

                    string folder = SobekFileSystem.Resource_Network_Uri(BriefItem, "oer");
                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);

                    // Set the source to the new source
                    StreamWriter writer = new StreamWriter(file);
                    writer.Write(newSource);
                    writer.Flush();
                    writer.Close();

                    // Clear the cache
                    CachedDataManager.Items.Remove_Digital_Resource_Object(CurrentUser.UserID, BriefItem.BibID, BriefItem.VID, null);

                    // Also clear the engine
                    SobekEngineClient.Items.Clear_Item_Cache(BriefItem.BibID, BriefItem.VID, Tracer);

                    // Forward along
                    CurrentRequest.Request_Completed = true;
                    CurrentRequest.ViewerSubCode = String.Empty;
                    HttpContext.Current.Response.Redirect(UrlWriterHelper.Redirect_URL(CurrentRequest), false);
                    HttpContext.Current.ApplicationInstance.CompleteRequest();

                    return;
                }
            }
        }

        /// <summary> Gets the number of pages for this viewer </summary>
        /// <value> This value can be overriden, but by default this returns the number of pages within the digital resource </value>
        public override int PageCount
        {
            get
            {
                if (isEditMode) return 1;

                return BriefItem.OpenTextbook_Pages != null ? BriefItem.OpenTextbook_Pages.Count : 0;
            }
        }

        /// <summary> Gets the names to show in the Go To combo box </summary>
        /// <value> This returns the labels assigned to each page, or 'Page 1', 'Page 2, etc.. if none
        /// of the pages have an existing label </value>
        public override string[] Go_To_Names
        {
            get
            {
                // If somehow no images (shouldn't be here) safely return empty string
                if (BriefItem.OpenTextbook_Pages == null)
                    return new string[0];

                // Start to build the return array and keep track of it some pages are numbered
                bool some_pages_named = false;
                string[] page_names = new string[BriefItem.OpenTextbook_Pages.Count];
                for (int i = 0; i < page_names.Length; i++)
                {
                    if (!String.IsNullOrEmpty(BriefItem.OpenTextbook_Pages[i].Label))
                    {
                        page_names[i] = BriefItem.OpenTextbook_Pages[i].Label;
                        some_pages_named = true;
                    }
                    else
                    {
                        page_names[i] = "Unnumbered " + (i + 1).ToString();
                    }
                }

                // If none of the pages were named, just name like 'Page 1', 'Page 2', etc..
                if (!some_pages_named)
                {
                    for (int i = 0; i < page_names.Length; i++)
                    {
                        page_names[i] = "Page " + (i + 1).ToString();
                    }
                }
                return page_names;
            }
        }

        private bool set_file_information(string[] FileExtensions)
        {
            if ( BriefItem.OpenTextbook_Pages == null )
            {
                filename = "oer\\" + Guid.NewGuid().ToString() + ".html";
                return true;
            }

            // Find the page information
            BriefItem_FileGrouping imagePage = BriefItem.OpenTextbook_Pages[page - 1];
            if (imagePage.Files != null)
            {
                // Step through each file in this page
                foreach (BriefItem_File thisFile in imagePage.Files)
                {
                    // Find the .html file
                    string extension = thisFile.File_Extension.Replace(".", "");

                    // Step through all permissable file extensions
                    foreach (string thisPossibleFileExtension in FileExtensions)
                    {
                        if (String.Compare(extension, thisPossibleFileExtension, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            // Get the OpenTextbooks information
                            filename = thisFile.Name?.Replace("\\", "/");
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary> Any additional inline style for this viewer that affects the main box around this</summary>
        /// <remarks> This returns the width of the image for the width of the viewer port </remarks>
        public override string ViewerBox_InlineStyle
        {
            get
            {
                return "width:85%;";
            }
        }

        /// <summary> Adds references to the HTML edit libraries into the head if this is in edit mode </summary>
        /// <param name="Output"> Output stream currently within the HTML head tags </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <remarks> By default this does nothing, but can be overwritten by all the individual item viewers </remarks>
        public override void Write_Within_HTML_Head(TextWriter Output, Custom_Tracer Tracer)
        {
            if (!isEditMode) return;

            if (Tracer != null)
            {
                Tracer.Add_Trace("OpenTextbook_ItemViewer.Write_Within_HTML_Head", "Add html editing libraries and javascript to HTML head");
            }

            // Create the CKEditor object
            CKEditor editor = new CKEditor
            {
                BaseUrl = CurrentRequest.Base_URL,
                Language = CurrentRequest.Language,
                TextAreaID = "sbkOeriv_HtmlEdit",
                FileBrowser_ImageUploadUrl = CurrentRequest.Base_URL + "HtmlEditFileHandler.ashx",
                //UploadPath = webcontent_upload_dir,
                //UploadURL = webcontent_upload_url
            };

            //// If there are existing files, add a reference to the URL for the image browser
            //if ((Directory.Exists(webcontent_upload_dir)) && (Directory.GetFiles(webcontent_upload_dir).Length > 0))
            //{
            //    // Is there an endpoint defined for looking at uploaded files?
            //    string upload_files_json_url = SobekEngineClient.WebContent.Uploaded_Files_URL;
            //    if (!String.IsNullOrEmpty(upload_files_json_url))
            //    {
            //        editor.ImageBrowser_ListUrl = String.Format(upload_files_json_url, urlSegments);
            //    }
            //}

            editor.Start_In_Source_Mode = false;

            // Add the HTML from the CKEditor object
            editor.Add_To_Stream(Output);
        }

        /// <summary> Adds any viewer_specific information to the item viewer above the standard pagination buttons </summary>
        /// <param name="Output"> Response stream for the item viewer to write directly to </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <remarks> By default this does nothing, but can be overwritten by all the individual item viewers </remarks>
        public override void Write_Top_Additional_Navigation_Row(TextWriter Output, Custom_Tracer Tracer)
        {
            if ( !isEditMode )
            {
                Output.WriteLine("\t<tr>");
                Output.WriteLine("\t\t<td>");

                Output.WriteLine("\t\t\t<div id=\"sbkOeriv_NavBar\">");

                // Add search box
                Output.WriteLine("\t\t\t\t<div id=\"sbkOeriv_SearchDiv\">");
                Output.WriteLine("\t\t\t\t\tSearch: ");
                Output.WriteLine("\t\t\t\t\t<input type=\"text\" id=\"oerSearchBox\" name=\"oerSearchBox\" />");
                Output.WriteLine("\t\t\t\t</div>");

                // Add zoom buttons
                Output.WriteLine("\t\t\t\t<div id=\"sbkOeriv_ZoomDiv\">");
                Output.WriteLine("\t\t\t\t\tZoom: ");
                Output.WriteLine("\t\t\t\t\t<a href=\"\" alt=\"small\" class=\"SbkOeriv_ZoomButtons\" id=\"sbkOeriv_SmallZoom\">T</a>&nbsp;");
                Output.WriteLine("\t\t\t\t\t<a href=\"\" alt=\"medium\" class=\"SbkOeriv_ZoomButtons\" id=\"sbkOeriv_MediumZoom\">T</a>&nbsp;");
                Output.WriteLine("\t\t\t\t\t<a href=\"\" alt=\"large\" class=\"SbkOeriv_ZoomButtons\" id=\"sbkOeriv_LargeZoom\">T</a>");
                Output.WriteLine("\t\t\t\t</div>");


                Output.WriteLine("\t\t\t</div>");

                Output.WriteLine("\t\t</td>");
                Output.WriteLine("\t</tr>");
            }
        }

        /// <summary> Write the item viewer main section as HTML directly to the HTTP output stream </summary>
        /// <param name="Output"> Response stream for the item viewer to write directly to </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        public override void Write_Main_Viewer_Section(TextWriter Output, Custom_Tracer Tracer)
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("OpenTextbook_ItemViewer.Write_Main_Viewer_Section", "");
            }

            string displayFileName = SobekFileSystem.Resource_Web_Uri(BriefItem, filename);

            // MAKE THIS USE THE FILES.ASPX WEB PAGE if this is restricted (or dark)
            if ((BriefItem.Behaviors.Dark_Flag) || (BriefItem.Behaviors.IP_Restriction_Membership > 0))
                displayFileName = CurrentRequest.Base_URL + "files/" + BriefItem.BibID + "/" + BriefItem.VID + "/" + filename;


            string name_for_image = HttpUtility.HtmlEncode(BriefItem.Title);


            if ((BriefItem.Images != null) && (BriefItem.Images.Count > 1) && (Current_Page - 1 < BriefItem.Images.Count))
            {
                string name_of_page = BriefItem.Images[Current_Page - 1].Label;
                name_for_image = name_for_image + " - " + HttpUtility.HtmlEncode(name_of_page);
            }

            // Read the text
            // Determine the source string
            string sourceString = SobekFileSystem.Resource_Web_Uri(BriefItem) + filename;
            if ((filename.IndexOf("http://") == 0) || (filename.IndexOf("https://") == 0) || (filename.IndexOf("[%BASEURL%]") == 0) || (filename.IndexOf("<%BASEURL%>") == 0))
            {
                sourceString = filename.Replace("[%BASEURL%]", CurrentRequest.Base_URL).Replace("<%BASEURL%>", CurrentRequest.Base_URL);
            }

            // Try to get the HTML for this
            if (Tracer != null)
            {
                Tracer.Add_Trace("HTML_ItemViewer.Write_Main_Viewer_Section", "Reading html for this view from static page");
            }
            string html;
            try
            {
                html = SobekFileSystem.ReadToEnd(BriefItem, sourceString);

            }
            catch
            {
                StringBuilder builder = new StringBuilder();

                if (page == 1)
                {
                    builder.AppendLine("<h2>Welcome to OpenPublishing</h2>");
                    builder.AppendLine();
                    builder.AppendLine("<p> You may edit your item here, by selecting <i>Edit Content</i> in the upper right corner of this page.</p>");
                    builder.AppendLine();
                    builder.AppendLine("<p> To update your table of contents and create chapters, use the OpenPublishing tool, available in the internal header or through the <i>MANAGE</i> option in the item menu.</p>");
                }
                else
                {
                    string chapterLabel = BriefItem.OpenTextbook_Pages[page - 1].Label;

                    builder.AppendLine("<h2>" + chapterLabel + "</h2>");
                    builder.AppendLine();
                    builder.AppendLine("<p> You may edit this chapter here, by selecting <i>Edit Content</i> in the upper right corner of this page.</p>");
                 }

                html = builder.ToString();
            }

            // Start the output area
            Output.WriteLine("\t\t<td>");

            // Add the edit link
            if (canEdit && !isEditMode)
            {
                CurrentRequest.ViewerSubCode = "edit";
                string edit_url = UrlWriterHelper.Redirect_URL(CurrentRequest);
                CurrentRequest.ViewerSubCode = null;

                Output.WriteLine("\t\t\t<div id=\"sbkOeriv_EditContent\">");
                Output.WriteLine("\t\t\t\t<div id=\"sbkOeriv_EditContentInner\">");
                Output.WriteLine("\t\t\t\t\t<a href=\"" + edit_url + "\" title=\"Edit this section\"><img src=\"" + Static_Resources_Gateway.Edit_Gif + "\" alt=\"\"> edit content</a>");
                Output.WriteLine("\t\t\t\t</div>");
                Output.WriteLine("\t\t\t</div>");
                
            }

            // Is this in edit mode?
            if (isEditMode)
            {
                const string TITLE_HELP = "Help for the title place holder";
                const string AUTHOR_HELP = "Help for the author place holder";
                const string DATE_HELP = "Help for the date place holder";
                const string KEYWORDS_HELP = "Help for the keywords place holder";
                const string ABSTRACT_HELP = "Help for the abstract place holder";

                // Start the main content
                Output.WriteLine("\t\t\t<div id=\"sbkOeriv_ContentEdit\">");

                // Add the SAVE and CANCEL buttons
                Output.WriteLine("\t\t\t\t<div class=\"sbkOeriv_EditButtons\">");
                Output.WriteLine("\t\t\t\t\t<div style=\"padding-right:30px;\">");
                CurrentRequest.ViewerSubCode = "";
                Output.WriteLine("\t\t\t\t\t\t<button title=\"Do not apply changes\" class=\"roundbutton sbkOeriv_EditButton\" onclick=\"window.location.href='" + UrlWriterHelper.Redirect_URL(CurrentRequest) + "';return false;\">CANCEL</button> &nbsp; &nbsp; ");
                CurrentRequest.ViewerSubCode = "edit";
                Output.WriteLine("\t\t\t\t\t\t<button title=\"Save changes to this section\" class=\"roundbutton sbkOeriv_EditButton\" type=\"submit\" onclick=\"for(var i in CKEDITOR.instances) { CKEDITOR.instances[i].updateElement(); }\">SAVE</button>");
                Output.WriteLine("\t\t\t\t\t</div>");
                Output.WriteLine("\t\t\t\t</div>");

                Output.WriteLine("  <a href=\"\" onclick=\"return show_header_info()\" id=\"sbkSbia_HeaderInfoDivShowLink\">show section data</a><br />");
                Output.WriteLine("  <div id=\"sbkSbia_HeaderInfoDiv\" style=\"display:none;\">");
                Output.WriteLine("    <div style=\"font-style:italic; padding:0 5px 5px 5px; text-align:left;\">The data below describes the content of this section.</div>");

                Output.WriteLine("    <table id=\"sbkSbia_HeaderTable\">");
                Output.WriteLine("      <tr>");
                Output.WriteLine("        <td style=\"width:50px\">&nbsp;</td>");
                Output.WriteLine("        <td class=\"sbkSbia_HeaderTableLabel\"><label for=\"admin_childpage_title\">Title:</label></td>");
                Output.WriteLine("        <td><input class=\"sbkSbia_HeaderInput sbk_Focusable\" name=\"admin_childpage_title\" id=\"admin_childpage_title\" type=\"text\" value=\"\" /></td>");
                Output.WriteLine("        <td><img class=\"sbkSbia_HelpButton\" src=\"" + Static_Resources_Gateway.Help_Button_Jpg + "\" onclick=\"alert('" + TITLE_HELP + "');\"  title=\"" + TITLE_HELP + "\" /></td>");
                Output.WriteLine("      </tr>");
                Output.WriteLine("      <tr>");
                Output.WriteLine("        <td>&nbsp;</td>");
                Output.WriteLine("        <td class=\"sbkSbia_HeaderTableLabel\"><label for=\"admin_childpage_author\">Author:</label></td>");
                Output.WriteLine("        <td><input class=\"sbkSbia_HeaderInput sbk_Focusable\" name=\"admin_childpage_author\" id=\"admin_childpage_author\" type=\"text\" value=\"\" /></td>");
                Output.WriteLine("        <td><img class=\"sbkSbia_HelpButton\" src=\"" + Static_Resources_Gateway.Help_Button_Jpg + "\" onclick=\"alert('" + AUTHOR_HELP + "');\"  title=\"" + AUTHOR_HELP + "\" /></td>");
                Output.WriteLine("      </tr>");
                Output.WriteLine("      <tr>");
                Output.WriteLine("        <td>&nbsp;</td>");
                Output.WriteLine("        <td class=\"sbkSbia_HeaderTableLabel\"><label for=\"admin_childpage_date\">Date:</label></td>");
                Output.WriteLine("        <td><input class=\"sbkSbia_HeaderInput sbk_Focusable\" name=\"admin_childpage_date\" id=\"admin_childpage_date\" type=\"text\" value=\"\" /></td>");
                Output.WriteLine("        <td><img class=\"sbkSbia_HelpButton\" src=\"" + Static_Resources_Gateway.Help_Button_Jpg + "\" onclick=\"alert('" + DATE_HELP + "');\"  title=\"" + DATE_HELP + "\" /></td>");
                Output.WriteLine("      </tr>");
                Output.WriteLine("      <tr>");
                Output.WriteLine("        <td>&nbsp;</td>");
                Output.WriteLine("        <td class=\"sbkSbia_HeaderTableLabel\"><label for=\"admin_childpage_keywords\">Keywords:</label></td>");
                Output.WriteLine("        <td><input class=\"sbkSbia_HeaderInput sbk_Focusable\" name=\"admin_childpage_keywords\" id=\"admin_childpage_keywords\" type=\"text\" value=\"\" /></td>");
                Output.WriteLine("        <td><img class=\"sbkSbia_HelpButton\" src=\"" + Static_Resources_Gateway.Help_Button_Jpg + "\" onclick=\"alert('" + KEYWORDS_HELP + "');\"  title=\"" + KEYWORDS_HELP + "\" /></td>");
                Output.WriteLine("      </tr>");
                Output.WriteLine("      <tr>");
                Output.WriteLine("        <td>&nbsp;</td>");
                Output.WriteLine("        <td class=\"sbkSbia_HeaderTableLabel\"><label for=\"admin_childpage_description\">Abstract:</label></td>");
                Output.WriteLine("        <td><input class=\"sbkSbia_HeaderInput sbk_Focusable\" name=\"admin_childpage_description\" id=\"admin_childpage_description\" type=\"text\" value=\"\" /></td>");
                Output.WriteLine("        <td><img class=\"sbkSbia_HelpButton\" src=\"" + Static_Resources_Gateway.Help_Button_Jpg + "\" onclick=\"alert('" + ABSTRACT_HELP + "');\"  title=\"" + ABSTRACT_HELP + "\" /></td>");
                Output.WriteLine("      </tr>");

                Output.WriteLine("    </table>");
                Output.WriteLine("    <br />");
                Output.WriteLine("  </div>");

                // Text area is converted to HTML editing area
                Output.WriteLine("\t\t\t\t\t<textarea id=\"sbkOeriv_HtmlEdit\" name=\"sbkOeriv_HtmlEdit\" >");

                // Add the HTML read from the file
                Output.WriteLine(html);

                // End the main content
                Output.WriteLine("\t\t\t\t\t</textarea>");

                // Add the SAVE and CANCEL buttons
                Output.WriteLine("\t\t\t\t<div class=\"sbkOeriv_EditButtons\">");
                Output.WriteLine("\t\t\t\t\t<div style=\"padding-right:30px;\">");
                CurrentRequest.ViewerSubCode = "";
                Output.WriteLine("\t\t\t\t\t\t<button title=\"Do not apply changes\" class=\"roundbutton sbkOeriv_EditButton\" onclick=\"window.location.href='" + UrlWriterHelper.Redirect_URL(CurrentRequest) + "';return false;\">CANCEL</button> &nbsp; &nbsp; ");
                CurrentRequest.ViewerSubCode = "edit";
                Output.WriteLine("\t\t\t\t\t\t<button title=\"Save changes to this section\" class=\"roundbutton sbkOeriv_EditButton\" type=\"submit\" onclick=\"for(var i in CKEDITOR.instances) { CKEDITOR.instances[i].updateElement(); }\">SAVE</button>");
                Output.WriteLine("\t\t\t\t\t</div>");
                Output.WriteLine("\t\t\t\t</div>");

                Output.WriteLine("\t\t\t</div>");
            }
            else
            {
                // Start the main content
                Output.WriteLine("\t\t\t<div id=\"sbkOeriv_Content\">");
                Output.WriteLine("\t\t\t\t<div id=\"sbkOeriv_ContentInner\">");

                // Add the HTML read from the file
                Output.WriteLine(html);

                // End the main content
                Output.WriteLine("\t\t\t\t</div>");
                Output.WriteLine("\t\t\t</div>");
            }

            // Add the bar to go to the previous page
            if ((page <= 1) || ( isEditMode))
            {
                Output.WriteLine("\t\t\t<div id=\"sbkOeriv_PrevBarInactive\"></div>");
            }
            else
            {
                string url = base.Previous_Page_URL;
                Output.WriteLine("\t\t\t<a href=\"" + url + "\" alt=\"Go to previous section\">");
                Output.WriteLine("\t\t\t\t<div id=\"sbkOeriv_PrevBar\">");
                Output.WriteLine("\t\t\t\t\t<span class=\"sbkOeriv_BarSpacerSpan\"></span>");
                Output.WriteLine("\t\t\t\t\t<img src=\"" + Static_Resources_Gateway.OpenTextBook_PrevButton_Img + "\" class=\"sbkOeriv_BarButton\" />");
                Output.WriteLine("\t\t\t\t</div>");
                Output.WriteLine("\t\t\t</a>");
            }

            // Add the bar to go to the next page
            if ((page >= PageCount) || ( isEditMode ))
            {
                Output.WriteLine("\t\t\t<div id=\"sbkOeriv_NextBarInactive\"></div>");
            }
            else
            {
                string url = base.Next_Page_URL;
                Output.WriteLine("\t\t\t<a href=\"" + url + "\" alt=\"Go to next section\">");
                Output.WriteLine("\t\t\t\t<div id=\"sbkOeriv_NextBar\">");
                Output.WriteLine("\t\t\t\t\t<span class=\"sbkOeriv_BarSpacerSpan\"></span>");
                Output.WriteLine("\t\t\t\t\t<img src=\"" + Static_Resources_Gateway.OpenTextBook_NextButton_Img + "\" class=\"sbkOeriv_BarButton\" />");
                Output.WriteLine("\t\t\t\t</div>");
                Output.WriteLine("\t\t\t</a>");
            }

            Output.WriteLine("\t\t</td>");
        }

        /// <summary> Allows controls to be added directory to a place holder, rather than just writing to the output HTML stream </summary>
        /// <param name="MainPlaceHolder"> Main place holder ( &quot;mainPlaceHolder&quot; ) in the itemNavForm form into which the bulk of the item viewer's output is displayed</param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <remarks> This method does nothing, since nothing is added to the place holder as a control for this item viewer </remarks>
        public override void Add_Main_Viewer_Section(PlaceHolder MainPlaceHolder, Custom_Tracer Tracer)
        {
            // Do nothing
        }
    }
}
