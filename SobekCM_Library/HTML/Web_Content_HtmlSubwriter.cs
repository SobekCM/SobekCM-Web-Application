﻿#region Using directives

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Web;
using System.Web.UI.WebControls;
using SobekCM.Core.Aggregations;
using SobekCM.Core.Client;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.Message;
using SobekCM.Core.Navigation;
using SobekCM.Core.SiteMap;
using SobekCM.Core.UI_Configuration;
using SobekCM.Core.UI_Configuration.StaticResources;
using SobekCM.Core.Users;
using SobekCM.Core.WebContent;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Engine_Library.SiteMap;
using SobekCM.Library.Helpers.CKEditor;
using SobekCM.Library.UI;
using SobekCM.Library.WebContentViewer;
using SobekCM.Library.WebContentViewer.Viewers;
using SobekCM.Resource_Object;
using SobekCM.Tools;

#endregion

namespace SobekCM.Library.HTML
{
    /// <summary> Simple web content html subwriter acts like a simple content management system, reading in source
    /// html files and displaying them within the framework of this digital library and after applying
    /// the indicated web skin. </summary>
    /// <remarks> This class extends the <see cref="abstractHtmlSubwriter"/> abstract class. </remarks>
    public class Web_Content_HtmlSubwriter : abstractHtmlSubwriter
    {
        private string breadcrumbs;
        private bool canEdit;
        private bool excludeSiteMap;
        private bool adminMissingScreen;

        private abstractWebContentViewer viewer;

        private HTML_Based_Content staticWebContent;
        private SobekCM_SiteMap siteMap;

        /// <summary> Constructor for a new instance of the Web_Content_HtmlSubwriter class </summary>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        public Web_Content_HtmlSubwriter(RequestCache RequestSpecificValues) : base(RequestSpecificValues)
        {
            // Try to pull the basic web content information to display
            if (!Get_Simple_Web_Content_Text(RequestSpecificValues.Current_Mode, RequestSpecificValues.Tracer, out staticWebContent, out siteMap))
            {
                RequestSpecificValues.Tracer.Add_Trace("Web_Content_HtmlSubwriter.Constructor", "Error pulling the simple web page content to display");
            }

            // If this was missing and the user is an admin, show a special message
            adminMissingScreen = false;
            if ((RequestSpecificValues.Current_Mode.Missing.HasValue) && (RequestSpecificValues.Current_Mode.Missing.Value))
            {
                if ((RequestSpecificValues.Current_User != null) && (RequestSpecificValues.Current_User.LoggedOn) && ((RequestSpecificValues.Current_User.Is_Portal_Admin) || (RequestSpecificValues.Current_User.Is_System_Admin) || (RequestSpecificValues.Current_User.Is_Host_Admin)))
                {
                    adminMissingScreen = true;
                }
                else
                {
                    // Set this to allow us to have our own error messages, without IIS jumping into it
                    HttpContext.Current.Response.TrySkipIisCustomErrors = true;
                    HttpContext.Current.Response.StatusCode = 404;
                }
            }

            // If there is a sitemap, check if this is a robot request and then if the URL
            // for the sitemap pages is URL restricted
            if ((siteMap != null) && (siteMap.Is_URL_Restricted_For_Robots) && (RequestSpecificValues.Current_Mode.Is_Robot))
            {
                if (RequestSpecificValues.Current_Mode.Base_URL != siteMap.Restricted_Robot_URL)
                {
                    RequestSpecificValues.Current_Mode.Base_URL = siteMap.Restricted_Robot_URL;
                    string redirect_url = UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode);

                    HttpContext.Current.Response.Status = "301 Moved Permanently";
                    HttpContext.Current.Response.AddHeader("Location", redirect_url);
                    HttpContext.Current.ApplicationInstance.CompleteRequest();
                    RequestSpecificValues.Current_Mode.Request_Completed = true;
                    return;
                }
            }

            // Look to see if this user can edit the pages
            canEdit = false;
            // This is very simple for now, but should change soon
            if ((RequestSpecificValues.Current_User != null) && (RequestSpecificValues.Current_User.LoggedOn))
            {
                canEdit = staticWebContent.Can_Edit(RequestSpecificValues.Current_User);
            }

            NameValueCollection form = HttpContext.Current.Request.Form;
            if ((canEdit) && (RequestSpecificValues.Current_Mode.WebContent_Type == WebContent_Type_Enum.Edit) && (form["sbkWchs_TextEdit"] != null))
            {
                string newSource = form["sbkWchs_TextEdit"];
                if (!String.IsNullOrEmpty(newSource))
                {
                    // Set the source to the new source
                    staticWebContent.Content = newSource;
                    staticWebContent.ContentSource = newSource;

                    // Set the date on the page to today
                    staticWebContent.Date = DateTime.Now.ToShortDateString();

                    // Send the update to the endgine
                    RestResponseMessage response = SobekEngineClient.WebContent.Update_HTML_Based_Content(staticWebContent, RequestSpecificValues.Current_User.Full_Name, RequestSpecificValues.Tracer);

                    // Clear the cache
                    CachedDataManager.WebContent.Clear_Page_Details(staticWebContent.WebContentID.Value);

                    // Forward along
                    RequestSpecificValues.Current_Mode.Request_Completed = true;
                    HttpContext.Current.Response.Redirect(staticWebContent.URL(RequestSpecificValues.Current_Mode.Base_URL), false);
                    HttpContext.Current.ApplicationInstance.CompleteRequest();

                    return;
                }
            }

            // In certain modes, the sitemap should not be displayed
            excludeSiteMap = false;
            
            // Build the viewer, if there should be one
            if ((RequestSpecificValues.Current_Mode.WebContent_Type != WebContent_Type_Enum.Edit) && (RequestSpecificValues.Current_Mode.WebContent_Type != WebContent_Type_Enum.Display))
            {
                viewer = WebContentViewer_Factory.Get_Viewer(RequestSpecificValues.Current_Mode.WebContent_Type, RequestSpecificValues, staticWebContent);
                if (viewer != null)
                    excludeSiteMap = true;
            }
        }

        /// <summary> Gets the collection of special behaviors which this subwriter
        /// requests from the main HTML subwriter. </summary>
        /// <remarks> By default, this returns an empty list </remarks>
        public override List<HtmlSubwriter_Behaviors_Enum> Subwriter_Behaviors
        {
            get
            {
                return new List<HtmlSubwriter_Behaviors_Enum>
                    {
                        HtmlSubwriter_Behaviors_Enum.Suppress_Banner
                    };
            }
        }

        #region Method to pull the static web information


        /// <summary> Gets the simple CMS/info object and text to display </summary>
        /// <param name="Current_Mode"> Mode / navigation information for the current request</param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <param name="Simple_Web_Content"> [OUT] Built browse object which contains information like title, banner, etc.. and the entire text to be displayed </param>
        /// <param name="Site_Map"> [OUT] Optional navigational site map object related to this page </param>
        /// <returns>TRUE if successful, otherwise FALSE </returns>
        /// <remarks> This always pulls the data directly from disk; this text is not cached. </remarks>
        private bool Get_Simple_Web_Content_Text(Navigation_Object Current_Mode, Custom_Tracer Tracer, out HTML_Based_Content Simple_Web_Content, out SobekCM_SiteMap Site_Map)
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("Web_Content_HtmlSubwriter.Get_Simple_Web_Content_Text", String.Empty);
            }

            Site_Map = null;
            Simple_Web_Content = null;

            // Get the web content object
            if (((Current_Mode.WebContentID.HasValue) && (Current_Mode.WebContentID.Value > 0)) && ((!Current_Mode.Missing.HasValue) || (!Current_Mode.Missing.Value)))
                Simple_Web_Content = SobekEngineClient.WebContent.Get_HTML_Based_Content(Current_Mode.WebContentID.Value, true, Tracer);

            // If somehow this is null and this was for DEFAULT, just add the page
            if (Simple_Web_Content == null)
            {
                Simple_Web_Content = SobekEngineClient.WebContent.Get_Special_Missing_Page(Tracer);
            }

            if (Simple_Web_Content == null)
            {
                Current_Mode.Error_Message = "Unable to retrieve simple text item '" + Current_Mode.Info_Browse_Mode.Replace("_", "\\") + "'";
                return false;
            }

            // If this is a redirect, just return 
            if (!String.IsNullOrEmpty(Simple_Web_Content.Redirect))
                return true;

            if (String.IsNullOrEmpty(Simple_Web_Content.Content))
            {
                Current_Mode.Error_Message = "Unable to read the file for display";
                return false;
            }

            // Look for a site map
            if (!String.IsNullOrEmpty(Simple_Web_Content.SiteMap))
            {
                // Look in the cache first
                Site_Map = CachedDataManager.Retrieve_Site_Map(Simple_Web_Content.SiteMap, Tracer);

                // If this was NULL, pull it
                if (Site_Map == null)
                {
                    string sitemap_file = Simple_Web_Content.SiteMap;
                    if (!sitemap_file.ToLower().Contains(".sitemap"))
                        sitemap_file = sitemap_file + ".sitemap";

                    // Only continue if the file exists
                    if (File.Exists(UI_ApplicationCache_Gateway.Settings.Servers.Base_Directory + "design\\webcontent\\sitemaps\\" + sitemap_file))
                    {
                        if (Tracer != null)
                        {
                            Tracer.Add_Trace("Web_Content_HtmlSubwriter.Get_Simple_Web_Content_Text", "Reading site map file");
                        }

                        // Try to read this sitemap file
                        Site_Map = SobekCM_SiteMap_Reader.Read_SiteMap_File(UI_ApplicationCache_Gateway.Settings.Servers.Base_Directory + "design\\webcontent\\sitemaps\\" + sitemap_file);

                        // If the sitemap file was succesfully read, cache it
                        if (Site_Map != null)
                        {
                            CachedDataManager.Store_Site_Map(Site_Map, Simple_Web_Content.SiteMap, Tracer);
                        }
                    }
                    else if (File.Exists(UI_ApplicationCache_Gateway.Settings.Servers.Base_Directory + "design\\webcontent\\" + sitemap_file))
                    {
                        // This is just for some legacy material
                        if (Tracer != null)
                        {
                            Tracer.Add_Trace("Web_Content_HtmlSubwriter.Get_Simple_Web_Content_Text", "Reading site map file");
                        }

                        // Try to read this sitemap file
                        Site_Map = SobekCM_SiteMap_Reader.Read_SiteMap_File(UI_ApplicationCache_Gateway.Settings.Servers.Base_Directory + "design\\webcontent\\" + sitemap_file);

                        // If the sitemap file was succesfully read, cache it
                        if (Site_Map != null)
                        {
                            CachedDataManager.Store_Site_Map(Site_Map, Simple_Web_Content.SiteMap, Tracer);
                        }
                    }
                }
            }

            // Since this is not cached, we can apply the individual user settings to the static text which was read right here
            Simple_Web_Content.Content = Simple_Web_Content.Apply_Settings_To_Static_Text(Simple_Web_Content.Content, null, Current_Mode.Skin, Current_Mode.Base_Skin, Current_Mode.Base_URL, UrlWriterHelper.URL_Options(Current_Mode), Tracer);

            return true;
        }

        #endregion

        #region Method to add the sitemap tree controls

        /// <summary> Add the sitemap tree-view control, if there is a site map included in this object </summary>
        /// <param name="placeHolder"> Main place holder ( &quot;mainPlaceHolder&quot; ) in the itemNavForm form, widely used throughout the application</param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        public void Add_Controls(PlaceHolder placeHolder, Custom_Tracer Tracer)
        {
            if ((siteMap == null) || (RequestSpecificValues.Current_Mode.Is_Robot) || (excludeSiteMap))
                return;

            Tracer.Add_Trace("Web_Content_HtmlSubwriter.Add_Controls", "Adding site map tree nav view");

            // Create the treeview
            TreeView treeView1 = new TreeView
                {
                    CssClass = "SobekSiteMapTreeView",
                    ExpandDepth = 0,
                    NodeIndent = 15,
                    ShowLines = true,
                    EnableClientScript = true,
                    PopulateNodesFromClient = true
                };

            // Set some tree view properties
            treeView1.TreeNodePopulate += treeView1_TreeNodePopulate;


            // Determine the base URL
            string base_url = RequestSpecificValues.Current_Mode.Base_URL;
            if (RequestSpecificValues.Current_Mode.Writer_Type == Writer_Type_Enum.HTML_LoggedIn)
            {
                base_url = base_url + "l/";
            }

            // Find the selected node
            int selected_node = siteMap.Selected_NodeValue(RequestSpecificValues.Current_Mode.Info_Browse_Mode);

            foreach (SobekCM_SiteMap_Node rootSiteMapNode in siteMap.RootNodes)
            {
                // Add the sitemaps root node first
                TreeNode rootNode = new TreeNode
                    {
                        SelectAction = TreeNodeSelectAction.None,
                        Text = string.Format("<a href='{0}{1}' title='{2}'>{3}</a>", base_url, rootSiteMapNode.URL, rootSiteMapNode.Description, rootSiteMapNode.Title)
                    };
                treeView1.Nodes.Add(rootNode);

                // Was this node currently selected?
                if (rootSiteMapNode.URL == RequestSpecificValues.Current_Mode.Info_Browse_Mode)
                {
                    rootNode.Text = string.Format("<span Title='{0}'>{1}</span>", rootSiteMapNode.Description, rootSiteMapNode.Title);
                    rootNode.Expand();
                }

                // Now add all the children recursively
                add_child_nodes(rootNode, rootSiteMapNode, base_url, selected_node);
            }

            // Always expand the top node
            //rootNode.Expand();

            // Add the tree to the view
            placeHolder.Controls.Add(treeView1);

        }

        private void add_child_nodes(TreeNode treeNode, SobekCM_SiteMap_Node siteNode, string base_url, int selected_node )
        {
            // Only do anything if there are child nodes defined
            if (siteNode.Child_Nodes_Count > 0)
            {
                // Step through each child node
                ReadOnlyCollection<SobekCM_SiteMap_Node> childNodes = siteNode.Child_Nodes;
                int child_node_counter = 0;
                while ( child_node_counter < childNodes.Count )
                {
                    // Get this child node
                    SobekCM_SiteMap_Node childNode = childNodes[child_node_counter];

                    // Add this child node to the tree view
                    TreeNode childTreeNode = new TreeNode
                                                 {
                                                     SelectAction = TreeNodeSelectAction.None,
                                                     Value = childNode.NodeValue.ToString()
                                                 };

                    if (childNode.URL.Length > 0)
                    {
                        childTreeNode.Text = string.Format("<a href='{0}' title='{1}'>{2}</a>", base_url + childNode.URL, childNode.Description, childNode.Title);
                    }
                    else
                    {
                        childTreeNode.Text = string.Format("<span Title='{0}' class='SobekSiteMapNoLink' >{1}</span>", childNode.Description, childNode.Title);
                        childTreeNode.SelectAction = TreeNodeSelectAction.Expand;
                    }
                    treeNode.ChildNodes.Add(childTreeNode);

                    if (childNode.Child_Nodes_Count > 0)
                    {
                        // Determine if the selected node is in the child nodes...
                        if ((childNode.NodeValue < selected_node) && ((child_node_counter + 1 == childNodes.Count) || (childNodes[child_node_counter + 1].NodeValue > selected_node)))
                        {
                            // Recurse through any children
                            add_child_nodes(childTreeNode, childNode, base_url, selected_node);
                        }
                        else
                        {
                            childTreeNode.PopulateOnDemand = true;
                        }
                    }

                    // Was this node currently selected?
                    if (childNode.NodeValue == selected_node )
                    {
                        childTreeNode.Text = string.Format("<span Title='{0}'>{1}</span>", childNode.Description, childNode.Title);
                        childTreeNode.Expand();

                        // Create the breadcrumbs now
                        StringBuilder breadcrumbBuilder = new StringBuilder();
                        breadcrumbBuilder.Append(childTreeNode.Text);

                        // Add each parent next, if they have a URL and also expand each parent
                        TreeNode selectedNodeExpander = childTreeNode;
                        while (selectedNodeExpander.Parent != null) 
                        {
                            // expand this node
                            (selectedNodeExpander.Parent).Expand();

                            // add to breadcrumb, if a link
                            if (selectedNodeExpander.Parent.SelectAction == TreeNodeSelectAction.None)
                            {
                                string text = selectedNodeExpander.Parent.Text.Replace(" Namespace</a>","</a>").Replace(" Sub-Namespace</a>","</a>");

                                breadcrumbBuilder.Insert(0, text + " <img src=\"" + RequestSpecificValues.Current_Mode.Base_URL + "design/skins/" + RequestSpecificValues.Current_Mode.Base_Skin_Or_Skin + "/breadcrumbimg.gif\" alt=\">\" /><img src=\"" + RequestSpecificValues.Current_Mode.Base_URL + "design/skins/" + RequestSpecificValues.Current_Mode.Base_Skin_Or_Skin + "/breadcrumbimg.gif\" alt=\">\" /> ");
                            }

                            // step up another level
                            selectedNodeExpander = selectedNodeExpander.Parent;
                        }
                        breadcrumbs = breadcrumbBuilder.ToString();
                    }

                    child_node_counter++;
                }
            }
        }

        /// <summary> Event handler loads the nodes on request to the serial hierarchy trees when the user requests them
        /// by expanding a node </summary>
        /// <param name="sender"> TreeView object that fired this event </param>
        /// <param name="e"> Event arguments includes the tree node which was expanded </param>
        void treeView1_TreeNodePopulate(object sender, TreeNodeEventArgs e)
        {
            SobekCM_SiteMap_Node retrieved_node = siteMap.Node_By_Value(Convert.ToInt32(e.Node.Value));

            // Determine the base URL
            string base_url = RequestSpecificValues.Current_Mode.Base_URL;
            if (RequestSpecificValues.Current_Mode.Writer_Type == Writer_Type_Enum.HTML_LoggedIn)
            {
                base_url = base_url + "l/";
            }

            if ((retrieved_node != null) && (retrieved_node.Child_Nodes_Count > 0))
            {
                foreach (SobekCM_SiteMap_Node childNode in retrieved_node.Child_Nodes)
                {
                    // Add this child node to the tree view
                    TreeNode childTreeNode = new TreeNode
                                                 {
                                                     SelectAction = TreeNodeSelectAction.None,
                                                     Value = childNode.NodeValue.ToString()
                                                 };
                    if (childNode.Child_Nodes_Count > 0)
                    {
                        childTreeNode.PopulateOnDemand = true;
                    }
                    if (childNode.URL.Length > 0)
                    {
                        childTreeNode.Text = string.Format("<a href='{0}' title='{1}'>{2}</a>", base_url + childNode.URL, childNode.Description, childNode.Title);
                    }
                    else
                    {
                        childTreeNode.Text = string.Format("<span Title='{0}' class='SobekSiteMapNoLink' >{1}</span>", childNode.Description, childNode.Title);
                        childTreeNode.SelectAction = TreeNodeSelectAction.Expand;
                    }
                    e.Node.ChildNodes.Add(childTreeNode);
                }
            }
        }

        #endregion 

        /// <summary> Writes the HTML generated by this simple text / CMS html subwriter directly to the response stream </summary>
        /// <param name="Output"> Stream to which to write the HTML for this subwriter </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> FALSE -- Value indicating if html writer should finish the page immediately after this, or if there are other controls or routines which need to be called first </returns>
        /// <remarks> This just begins the page and gets ready for site map tree view to possibly be added </remarks>
        public override bool Write_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace("Web_Content_HtmlSubwriter.Write_HTML", "Rendering HTML");

            if (adminMissingScreen)
            {

                // Constants for the brief explanations
                const string ADD_COLLECTION_WIZARD_BRIEF = "Add a new '{0}' collection using the Add New Collection Wizard.  This will guide you the process of adding a new collection and uploading the banner and button.";
                const string ADD_WEB_CONTENT_BRIEF = "Add a new top-level web content page to this instance.  These pages exist outside of any collections, but still utilize the web skin look and feel of the instance.";
                const string ADD_NEW_ITEM_BRIEF = "Add a new digital resource with a BibID of '{0}' using your default online submission template and default metadata set.";
                const string ALIASES_BRIEF = "Add this as a new aggregation alias pointing to an existing aggregation.";


                Output.WriteLine("<div id=\"sbkWchs_Panel\">");
                Add_Banner(Output, "sbkAhs_BannerDiv", WebPage_Title.Replace("{0} ", ""), RequestSpecificValues.Current_Mode, RequestSpecificValues.HTML_Skin, RequestSpecificValues.Top_Collection);

                Output.WriteLine("<div id=\"sbkWchs_InnerPanel\">");


                // Start the page container
                Output.WriteLine("<div id=\"pagecontainer\">");
                Output.WriteLine("<br />");

                // Add the title
                Output.WriteLine("<div class=\"sbkAdm_TitleDiv sbkAdm_TitleDivBorder\">");
                Output.WriteLine("  <img id=\"sbkAdm_TitleDivImg\" src=\"" + Static_Resources_Gateway.Warning_Img + "\" alt=\"\" />");
                Output.WriteLine("  <h1>Page Not Found</h1>");
                Output.WriteLine("</div>");
                Output.WriteLine();

                Output.WriteLine("<div class=\"sbkHav_MainText\" >");

                Output.WriteLine("  <h1>The page you requested does not exist.  Would you like to add it?</h1>");

                Output.WriteLine("  <table id=\"sbkHav_OptionsTable3\" style=\"padding-top:10px;\">");

                // Does this have a slash in it, which would make it ONLY a web content page
                bool contains_slash = RequestSpecificValues.Current_Mode.Info_Browse_Mode.Contains("/");

                // Add collection wizard
                if (!contains_slash)
                {
                    RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Administrative;
                    RequestSpecificValues.Current_Mode.Admin_Type = Admin_Type_Enum.Add_Collection_Wizard;
                    string add_collection_url = UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode);
                    if (add_collection_url.IndexOf("?") < 0)
                        add_collection_url = add_collection_url + "?code=" + RequestSpecificValues.Current_Mode.Info_Browse_Mode;
                    else
                        add_collection_url = add_collection_url + "&code=" + RequestSpecificValues.Current_Mode.Info_Browse_Mode;

                    Output.WriteLine("    <tr>");
                    Output.WriteLine("      <td>&nbsp;</td>");
                    Output.WriteLine("      <td><a href=\"" + add_collection_url + "\"><img src=\"" + Static_Resources_Gateway.Wizard_Img_Large + "\" /></a></td>");
                    Output.WriteLine("      <td>");
                    Output.WriteLine("        <a href=\"" + add_collection_url + "\">Add New Collection</a>");
                    Output.WriteLine("        <div class=\"sbkMmav_Desc\">" + String.Format(ADD_COLLECTION_WIZARD_BRIEF, RequestSpecificValues.Current_Mode.Info_Browse_Mode) + "</div>");
                    Output.WriteLine("      </td>");
                    Output.WriteLine("    </tr>");
                }


                // Add new digital resource
                if (( !contains_slash ) && ( UI_ApplicationCache_Gateway.Settings.Resources.Online_Item_Submit_Enabled))
                {
                    if ((RequestSpecificValues.Current_Mode.Info_Browse_Mode.Length == 10) && (SobekCM_Item.is_bibid_format(RequestSpecificValues.Current_Mode.Info_Browse_Mode)))
                    {
                        RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.My_Sobek;
                        RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.New_Item;
                        RequestSpecificValues.Current_Mode.My_Sobek_SubMode = "1";
                        ;
                        string add_new_item_url = UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode);
                        Output.WriteLine("    <tr>");
                        Output.WriteLine("      <td>&nbsp;</td>");
                        Output.WriteLine("      <td><a href=\"" + add_new_item_url + "\"><img src=\"" + Static_Resources_Gateway.New_Item_Img_Large + "\" /></a></td>");
                        Output.WriteLine("      <td>");
                        Output.WriteLine("        <a href=\"" + add_new_item_url + "\">Add New Item</a>");
                        Output.WriteLine("        <div class=\"sbkMmav_Desc\">" + String.Format(ADD_NEW_ITEM_BRIEF, RequestSpecificValues.Current_Mode.Info_Browse_Mode.ToUpper()) + "</div>");
                        Output.WriteLine("      </td>");
                        Output.WriteLine("    </tr>");
                    }
                }

                // Add web content page
                RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Administrative;
                RequestSpecificValues.Current_Mode.Admin_Type = Admin_Type_Enum.WebContent_Add_New;
                StringBuilder add_webcontent_url = new StringBuilder(UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode));
                string[] splitter = RequestSpecificValues.Current_Mode.Info_Browse_Mode.Split("/".ToCharArray());
                int level = 1;
                foreach (string thisSplitter in splitter)
                {
                    if ( level == 1 )
                        add_webcontent_url.Append("?l1=" + thisSplitter);
                    else
                        add_webcontent_url.Append("&l" + level + "=" + thisSplitter);

                    level++;
                }

                Output.WriteLine("    <tr>");
                Output.WriteLine("      <td>&nbsp;</td>");
                Output.WriteLine("      <td><a href=\"" + add_webcontent_url + "\"><img src=\"" + Static_Resources_Gateway.WebContent_Img_Large + "\" /></a></td>");
                Output.WriteLine("      <td>");
                Output.WriteLine("        <a href=\"" + add_webcontent_url + "\">Add New Web Content Page</a>");
                Output.WriteLine("        <div class=\"sbkMmav_Desc\">" + ADD_WEB_CONTENT_BRIEF + "</div>");
                Output.WriteLine("      </td>");
                Output.WriteLine("    </tr>");


                // Edit aggregation aliases
                if (!contains_slash)
                {
                    RequestSpecificValues.Current_Mode.Admin_Type = Admin_Type_Enum.Aliases;
                    string alias_url = UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode);

                    if (alias_url.IndexOf("?") < 0)
                        alias_url = alias_url + "?code=" + RequestSpecificValues.Current_Mode.Info_Browse_Mode;
                    else
                        alias_url = alias_url + "&code=" + RequestSpecificValues.Current_Mode.Info_Browse_Mode;


                    Output.WriteLine("    <tr>");
                    Output.WriteLine("      <td>&nbsp;</td>");
                    Output.WriteLine("      <td><a href=\"" + alias_url + "\"><img src=\"" + Static_Resources_Gateway.Aliases_Img_Large + "\" /></a></td>");
                    Output.WriteLine("      <td>");
                    Output.WriteLine("        <a href=\"" + alias_url + "\">Add New Aggregation Alias</a>");
                    Output.WriteLine("        <div class=\"sbkMmav_Desc\">" + ALIASES_BRIEF + "</div>");
                    Output.WriteLine("      </td>");
                    Output.WriteLine("    </tr>");
                }

                Output.WriteLine("  </table>");

                Output.WriteLine("</div>");
                Output.WriteLine("</div>");
 

                Output.WriteLine("</div>");
                Output.WriteLine("</div>");
                return true;
            }

            // The header is already drawn, so just start the main table here
            if ((siteMap != null) && ( !excludeSiteMap ))
            {
                Output.WriteLine("<table width=\"100%\" id=\"sbkWchs_SiteMapOuterContainer\">");
                Output.WriteLine("<tr>");
                if (siteMap.Width > 0)
                {
                    Output.WriteLine("<td valign=\"top\" width=\"" + siteMap.Width + "px\">");
                }
                else
                {
                    Output.WriteLine("<td valign=\"top\">");
                }

                // If this is a robot, just draw the links
                if (RequestSpecificValues.Current_Mode.Is_Robot)
                {
                    Output.WriteLine("<div class=\"sbkWchs_SiteMapTree\">");
                    foreach (SobekCM_SiteMap_Node rootNode in siteMap.RootNodes)
                    {
                        recursively_draw_sitemap_for_robots(Output, rootNode, String.Empty);
                    }
                    Output.WriteLine("</div>");
                }
            }

            return false;
        }

        private void recursively_draw_sitemap_for_robots(TextWriter Output, SobekCM_SiteMap_Node Node, string Indent)
        {

            // Add this text
            if ((Node.URL.Length > 0) && (Node.URL != RequestSpecificValues.Current_Mode.Info_Browse_Mode))
            {
                Output.WriteLine(Indent + "<a href='" + RequestSpecificValues.Current_Mode.Base_URL + Node.URL + "' title='" + Node.Description + "'>" + Node.Title + "</a><br />");
            }
            else
            {
                Output.WriteLine(Indent + "<span Title='" + Node.Description + "' class='sbkWchs_SiteMapNoLink' >" + Node.Title + "</span><br />");
            }

            // Add all the children
            if (Node.Child_Nodes_Count > 0)
            {
                foreach (SobekCM_SiteMap_Node childNode in Node.Child_Nodes)
                {
                    recursively_draw_sitemap_for_robots(Output, childNode, Indent + " &nbsp; &nbsp; &nbsp; ");
                }
            }
        }


        /// <summary> Writes the HTML generated by this simple text / CMS html subwriter directly to the response stream </summary>
        /// <param name="Output"> Stream to which to write the HTML for this subwriter </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <remarks> This finishes out the page and includes all the static content in this static web content file </remarks>
        public override void Write_Final_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace("Web_Content_HtmlSubwriter.Write_Final_HTML", "Rendering HTML");

            if (adminMissingScreen)
                return;

            // If there is a sitemap, move to the second part of the table
            if ((siteMap != null) && (!excludeSiteMap))
            {
                Output.WriteLine("</td>");
                Output.WriteLine("<td id=\"sbkWchs_MainTd\">");
            }

            // Depending on mode, display the information
            switch (RequestSpecificValues.Current_Mode.WebContent_Type)
            {
                case WebContent_Type_Enum.NONE:
                case WebContent_Type_Enum.Display:
                    write_standard_display(Output, Tracer);
                    break;

                case WebContent_Type_Enum.Edit:
                    write_edit_display(Output, Tracer);
                    break;

                default:
                    if (viewer != null)
                    {
                        Output.WriteLine("<div class=\"sbkWchw_AdminPage\">");
                        Output.WriteLine("<div class=\"sbkAdm_TitleDiv_Wchs sbkAdm_TitleDivBorder\">");
                        Output.WriteLine("  <img id=\"sbkAdm_TitleDivImg_Wchs\" src=\"" + viewer.Viewer_Icon + "\" alt=\"\" />");
                        Output.WriteLine("  <h1>" + viewer.Viewer_Title + "</h1>");
                        Output.WriteLine("  <h2>" + staticWebContent.Title + "</h2>");
                        Output.WriteLine("</div>");

                        // Get the URL for the home page
                        RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Aggregation;
                        RequestSpecificValues.Current_Mode.Aggregation_Type = Aggregation_Type_Enum.Home;
                        RequestSpecificValues.Current_Mode.Home_Type = Home_Type_Enum.List;
                        string home_url = UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode);

                        if (viewer is Manage_Menu_WebContentViewer)
                        {
                            // Render the breadcrumbns
                            Output.WriteLine("<div class=\"sbkAdm_Breadcrumbs\">");
                            Output.WriteLine("  <a href=\"" + home_url + "\">" + RequestSpecificValues.Current_Mode.Instance_Abbreviation + " Home</a> > ");
                            Output.WriteLine("  Web Content Management");
                            Output.WriteLine("</div>");
                        }
                        else
                        {
                            // Get the URL for the system admin menu
                            WebContent_Type_Enum current = RequestSpecificValues.Current_Mode.WebContent_Type;
                            RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Simple_HTML_CMS;
                            RequestSpecificValues.Current_Mode.WebContent_Type = WebContent_Type_Enum.Manage_Menu;
                            string menu_url = UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode);
                            RequestSpecificValues.Current_Mode.WebContent_Type = current;

                            // Render the breadcrumbns
                            Output.WriteLine("<div class=\"sbkAdm_Breadcrumbs\">");
                            Output.WriteLine("  <a href=\"" + home_url + "\">" + RequestSpecificValues.Current_Mode.Instance_Abbreviation + " Home</a> > ");
                            Output.WriteLine("  <a href=\"" + menu_url + "\">Web Content Management</a> > ");
                            Output.WriteLine("  " + viewer.Viewer_Title);
                            Output.WriteLine("</div>");
                        }

                        RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Simple_HTML_CMS;
                        viewer.Add_HTML(Output, Tracer);
                        Output.WriteLine("</div>");
                    }
                    break;

            }

        }
                    
        private void write_banner_and_menu(TextWriter Output, Custom_Tracer Tracer)
        {
            // Save the current mode and browse
            Display_Mode_Enum thisMode = RequestSpecificValues.Current_Mode.Mode;

            if ((!String.IsNullOrEmpty(staticWebContent.Banner)) && (staticWebContent.Banner.ToUpper().Trim() != "NONE"))
            {
                if (staticWebContent.Banner.ToUpper().Trim() == "DEFAULT")
                {
                    if ((RequestSpecificValues.HTML_Skin != null) && (!String.IsNullOrEmpty(RequestSpecificValues.HTML_Skin.Banner_HTML)))
                    {
                        Output.WriteLine(RequestSpecificValues.HTML_Skin.Banner_HTML);
                    }
                    else
                    {
                        Item_Aggregation allCollection;
                        if ( Get_Top_Level_Collection(RequestSpecificValues.Current_Mode, Tracer, out allCollection))
                        {
                            Output.WriteLine("<img id=\"mainBanner\" src=\"" + RequestSpecificValues.Current_Mode.Base_URL + allCollection.Get_Banner_Image(RequestSpecificValues.HTML_Skin) + "\" alt=\"MISSING BANNER\" />");
                        }
                    }
                }
                else
                {
                    Output.WriteLine("<img id=\"mainBanner\" src=\"" + staticWebContent.Banner.Replace("<%BASEURL%>", RequestSpecificValues.Current_Mode.Base_URL) + "\" alt=\"MISSING BANNER\" />");
                }
            }

            // Should a menu be included, from the sitemaps?
            if ((siteMap != null) && (staticWebContent.IncludeMenu.HasValue) && (staticWebContent.IncludeMenu.Value))
            {
                // Determine the base URL
                string base_url = RequestSpecificValues.Current_Mode.Base_URL;
                if (RequestSpecificValues.Current_Mode.Writer_Type == Writer_Type_Enum.HTML_LoggedIn)
                {
                    base_url = base_url + "l/";
                }

                // Start the menu
                Output.WriteLine("<!-- Add the top-level static page menu -->");
                Output.WriteLine("<nav id=\"sbkAgm_MenuBar\" class=\"sbkMenu_Bar\">");
                Output.WriteLine("  <ul class=\"sf-menu\" id=\"sbkAgm_Menu\">");

                // Step through each of the root nodes and add it
                int counter = 1;
                foreach (SobekCM_SiteMap_Node rootSiteMapNode in siteMap.RootNodes)
                {
                    if (rootSiteMapNode.Child_Nodes_Count == 0)
                    {
                        Output.WriteLine("    <li id=\"sbkAgm_TopMenu{0}\"><a href=\"{1}{2}\">{3}</a></li>", counter, base_url, rootSiteMapNode.URL, rootSiteMapNode.Title);
                    }
                    else
                    {
                        Output.Write("    <li id=\"sbkAgm_TopMenu{0}\"><a href=\"{1}{2}\">{3}</a><ul id=\"sbkAgm_SubMenu{0}\">", counter, base_url, rootSiteMapNode.URL, rootSiteMapNode.Title);
                        int middle_counter = 1;
                        foreach (SobekCM_SiteMap_Node childNode in rootSiteMapNode.Child_Nodes)
                        {
                            Output.Write("<li id=\"sbkAgm_MiddleMenu{0}\"><a href=\"{1}{2}\">{3}</a></li>", counter + "_" + middle_counter, base_url, childNode.URL, childNode.Title);
                            middle_counter++;
                        }

                        Output.WriteLine("</ul></li>");
                    }
                    counter++;
                }

                Output.WriteLine("  </ul>");
                Output.WriteLine("</nav>");
                Output.WriteLine();

                Output.WriteLine("<!-- Initialize the main user menu -->");
                Output.WriteLine("<script>");
                Output.WriteLine("  jQuery(document).ready(function () {");
                Output.WriteLine("     jQuery('ul.sf-menu').superfish({");

                Output.WriteLine("          onBeforeShow: function() { ");
                Output.WriteLine("               if ( $(this).attr('id') == 'sbkAgm_FinalMenu')");
                Output.WriteLine("               {");
                Output.WriteLine("                 var thisWidth = $(this).width();");
                Output.WriteLine("                 var parent = $('#sbkAgm_Final');");
                Output.WriteLine("                 var offset = $('#sbkAgm_Final').offset();");
                Output.WriteLine("                 if ( $(window).width() < offset.left + thisWidth )");
                Output.WriteLine("                 {");
                Output.WriteLine("                   var newleft = thisWidth - parent.width();");
                Output.WriteLine("                   $(this).css('left', '-' + newleft + 'px');");
                Output.WriteLine("                 }");
                Output.WriteLine("               }");
                Output.WriteLine("          }");

                Output.WriteLine("    });");
                Output.WriteLine("  });");
                Output.WriteLine("</script>");
                Output.WriteLine();
            }


            // Add the breadcrumbs
            if (!String.IsNullOrEmpty(breadcrumbs))
            {
                Output.WriteLine("<div class=\"sbkWchs_Breadcrumbs\">" + breadcrumbs + "</div>");
            }
        }


        private void write_standard_display(TextWriter Output, Custom_Tracer Tracer)
        {
            // Start this panel
            Output.WriteLine("<div id=\"sbkWchs_Panel\">");

            // Write the banner and main menu
            write_banner_and_menu(Output, Tracer);

            if (canEdit)
            {
                Output.WriteLine("<div id=\"sbkWchs_InnerPanelEditable\">");
            }
            else
            {
                Output.WriteLine("<div id=\"sbkWchs_InnerPanel\">");
            }

            // Add the secondary HTML ot the home page
            Output.WriteLine(staticWebContent.Content);

            if (canEdit)
            {
                RequestSpecificValues.Current_Mode.WebContent_Type = WebContent_Type_Enum.Edit;
                string url = UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode);
                RequestSpecificValues.Current_Mode.WebContent_Type = WebContent_Type_Enum.Display;

                Output.WriteLine("<div id=\"sbkWchs_EditableLink\"><a href=\"" + url + "\" title=\"Edit this page\"><img src=\"" + Static_Resources_Gateway.Edit_Gif + "\" alt=\"\" />edit content</a></div>");
            }
            Output.WriteLine("</div>");

            if (canEdit)
            {
                Output.WriteLine("<script>");
                Output.WriteLine("  $(\"#sbkWchs_InnerPanelEditable\").mouseover(function() { $(\"#sbkWchs_EditableLink\").css(\"display\",\"inline-block\"); });");
                Output.WriteLine("  $(\"#sbkWchs_InnerPanelEditable\").mouseout(function() { $(\"#sbkWchs_EditableLink\").css(\"display\",\"none\"); });");
                Output.WriteLine("</script>");
                Output.WriteLine();
            }

            Output.WriteLine("</div>");
            Output.WriteLine("<br />");
            Output.WriteLine();

            // If there is a sitemap, finish the main table
            if (siteMap != null)
            {
                Output.WriteLine("</td>");
                Output.WriteLine("</tr>");
                Output.WriteLine("</table>");
            }
        }

        private void write_edit_display(TextWriter Output, Custom_Tracer Tracer)
        {
            // Start this panel
            Output.WriteLine("<div id=\"sbkWchs_Panel\">");

            // Write the banner and main menu
            write_banner_and_menu(Output, Tracer);

            string post_url = HttpUtility.HtmlEncode(HttpContext.Current.Items["Original_URL"].ToString());
            Output.WriteLine("<form name=\"home_edit_form\" method=\"post\" action=\"" + post_url + "\" id=\"addedForm\" >");

            //const string TITLE_HELP = "Help for the title place holder";
            //const string AUTHOR_HELP = "Help for the author place holder";
            //const string DATE_HELP = "Help for the date place holder";
            //const string DESCRIPTION_HELP = "Help for the description place holder";
            //const string KEYWORDS_HELP = "Help for the keywords place holder";
            //const string EXTRA_HEAD_HELP = "Help for the extra head place holder";

            //Output.WriteLine("  <a href=\"\" onclick=\"return show_header_info()\" id=\"sbkSbia_HeaderInfoDivShowLink\">show header data (advanced)</a><br />");
            //Output.WriteLine("  <div id=\"sbkSbia_HeaderInfoDiv\" style=\"display:none;\">");
            //Output.WriteLine("    <div style=\"font-style:italic; padding:0 5px 5px 5px; text-align:left;\">The data below describes the content of this static child page and is used by some search engine indexing algorithms.  By default, it will not show in text of the page, but will be included in the head tag of the page.</div>");

            //Output.WriteLine("    <table id=\"sbkSbia_HeaderTable\">");
            //Output.WriteLine("      <tr>");
            //Output.WriteLine("        <td style=\"width:50px\">&nbsp;</td>");
            //Output.WriteLine("        <td class=\"sbkSbia_HeaderTableLabel\"><label for=\"admin_childpage_title\">Title:</label></td>");
            //Output.WriteLine("        <td><input class=\"sbkSbia_HeaderInput sbk_Focusable\" name=\"admin_childpage_title\" id=\"admin_childpage_title\" type=\"text\" value=\"" + HttpUtility.HtmlEncode(staticWebContent.Title) + "\" /></td>");
            //Output.WriteLine("        <td><img class=\"sbkSbia_HelpButton\" src=\"" + Static_Resources.Help_Button_Jpg + "\" onclick=\"alert('" + TITLE_HELP + "');\"  title=\"" + TITLE_HELP + "\" /></td>");
            //Output.WriteLine("      </tr>");
            //Output.WriteLine("      <tr>");
            //Output.WriteLine("        <td>&nbsp;</td>");
            //Output.WriteLine("        <td class=\"sbkSbia_HeaderTableLabel\"><label for=\"admin_childpage_author\">Author:</label></td>");
            //Output.WriteLine("        <td><input class=\"sbkSbia_HeaderInput sbk_Focusable\" name=\"admin_childpage_author\" id=\"admin_childpage_author\" type=\"text\" value=\"" + HttpUtility.HtmlEncode(staticWebContent.Author) + "\" /></td>");
            //Output.WriteLine("        <td><img class=\"sbkSbia_HelpButton\" src=\"" + Static_Resources.Help_Button_Jpg + "\" onclick=\"alert('" + AUTHOR_HELP + "');\"  title=\"" + AUTHOR_HELP + "\" /></td>");
            //Output.WriteLine("      </tr>");
            //Output.WriteLine("      <tr>");
            //Output.WriteLine("        <td>&nbsp;</td>");
            //Output.WriteLine("        <td class=\"sbkSbia_HeaderTableLabel\"><label for=\"admin_childpage_date\">Date:</label></td>");
            //Output.WriteLine("        <td><input class=\"sbkSbia_HeaderInput sbk_Focusable\" name=\"admin_childpage_date\" id=\"admin_childpage_date\" type=\"text\" value=\"" + HttpUtility.HtmlEncode(staticWebContent.Date) + "\" /></td>");
            //Output.WriteLine("        <td><img class=\"sbkSbia_HelpButton\" src=\"" + Static_Resources.Help_Button_Jpg + "\" onclick=\"alert('" + DATE_HELP + "');\"  title=\"" + DATE_HELP + "\" /></td>");
            //Output.WriteLine("      </tr>");
            //Output.WriteLine("      <tr>");
            //Output.WriteLine("        <td>&nbsp;</td>");
            //Output.WriteLine("        <td class=\"sbkSbia_HeaderTableLabel\"><label for=\"admin_childpage_description\">Description:</label></td>");
            //Output.WriteLine("        <td><input class=\"sbkSbia_HeaderInput sbk_Focusable\" name=\"admin_childpage_description\" id=\"admin_childpage_description\" type=\"text\" value=\"" + HttpUtility.HtmlEncode(staticWebContent.Description) + "\" /></td>");
            //Output.WriteLine("        <td><img class=\"sbkSbia_HelpButton\" src=\"" + Static_Resources.Help_Button_Jpg + "\" onclick=\"alert('" + DESCRIPTION_HELP + "');\"  title=\"" + DESCRIPTION_HELP + "\" /></td>");
            //Output.WriteLine("      </tr>");
            //Output.WriteLine("      <tr>");
            //Output.WriteLine("        <td>&nbsp;</td>");
            //Output.WriteLine("        <td class=\"sbkSbia_HeaderTableLabel\"><label for=\"admin_childpage_keywords\">Keywords:</label></td>");
            //Output.WriteLine("        <td><input class=\"sbkSbia_HeaderInput sbk_Focusable\" name=\"admin_childpage_keywords\" id=\"admin_childpage_keywords\" type=\"text\" value=\"" + HttpUtility.HtmlEncode(staticWebContent.Keywords) + "\" /></td>");
            //Output.WriteLine("        <td><img class=\"sbkSbia_HelpButton\" src=\"" + Static_Resources.Help_Button_Jpg + "\" onclick=\"alert('" + KEYWORDS_HELP + "');\"  title=\"" + KEYWORDS_HELP + "\" /></td>");
            //Output.WriteLine("      </tr>");
            //Output.WriteLine("      <tr style=\"vertical-align:top;\">");
            //Output.WriteLine("        <td>&nbsp;</td>");
            //Output.WriteLine("        <td class=\"sbkSbia_HeaderTableLabel\" style=\"padding-top:5px\"><label for=\"admin_childpage_extrahead\">HTML Head Info:</label></td>");
            //string extra_head_info = staticWebContent.Extra_Head_Info ?? String.Empty;
            //Output.WriteLine("        <td><textarea rows=\"3\" class=\"sbkSbia_HeaderTextArea sbk_Focusable\" name=\"admin_childpage_extrahead\" id=\"admin_childpage_extrahead\" type=\"text\">" + HttpUtility.HtmlEncode(extra_head_info) + "</textarea></td>");
            //Output.WriteLine("        <td><img class=\"sbkSbia_HelpButton\" src=\"" + Static_Resources.Help_Button_Jpg + "\" onclick=\"alert('" + EXTRA_HEAD_HELP + "');\"  title=\"" + EXTRA_HEAD_HELP + "\" /></td>");
            //Output.WriteLine("      </tr>");
            //Output.WriteLine("    </table>");
            //Output.WriteLine("    <br />");
            //Output.WriteLine("  </div>");

            Output.WriteLine("  <textarea id=\"sbkWchs_TextEdit\" name=\"sbkWchs_TextEdit\" style=\"height:400px;\" >");
            Output.WriteLine(staticWebContent.Content.Replace("<%", "[%").Replace("%>", "%]"));
            Output.WriteLine("  </textarea>");
            Output.WriteLine();

            Output.WriteLine("<div id=\"sbkAghsw_HomeEditButtons\">");
            RequestSpecificValues.Current_Mode.WebContent_Type = WebContent_Type_Enum.Display;
            Output.WriteLine("  <button title=\"Do not apply changes\" class=\"roundbutton\" onclick=\"window.location.href='" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "';return false;\"><img src=\"" + Static_Resources_Gateway.Button_Previous_Arrow_Png + "\" class=\"roundbutton_img_left\" alt=\"\" /> CANCEL</button> &nbsp; &nbsp; ");
            RequestSpecificValues.Current_Mode.WebContent_Type = WebContent_Type_Enum.Edit;
            Output.WriteLine("  <button title=\"Save changes to this web content page text\" class=\"roundbutton\" type=\"submit\" onclick=\"for(var i in CKEDITOR.instances) { CKEDITOR.instances[i].updateElement(); }\">SAVE <img src=\"" + Static_Resources_Gateway.Button_Next_Arrow_Png + "\" class=\"roundbutton_img_right\" alt=\"\" /></button>");
            Output.WriteLine("</div>");
            Output.WriteLine("</form>");
            Output.WriteLine("<br /><br /><br />");
            Output.WriteLine();

 
            Output.WriteLine("</div>");
            Output.WriteLine("<br />");
            Output.WriteLine();

            // If there is a sitemap, finish the main table
            if (siteMap != null)
            {
                Output.WriteLine("</td>");
                Output.WriteLine("</tr>");
                Output.WriteLine("</table>");
            }
        }

        /// <summary> Title for this web page </summary>
        public override string WebPage_Title
        {
            get {
                return (( staticWebContent != null ) && ( staticWebContent.Title != null )) ? staticWebContent.Title : "{0}";
            }
        }

        /// <summary> Write any additional values within the HTML Head of the
        /// final served page </summary>
        /// <param name="Output"> Output stream currently within the HTML head tags </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        public override void Write_Within_HTML_Head(TextWriter Output, Custom_Tracer Tracer)
        {
            Output.WriteLine("  <meta name=\"robots\" content=\"index, nofollow\" />");

            // Add any other meta tags here as well
            if (staticWebContent != null)
            {
                if (!String.IsNullOrEmpty(staticWebContent.Description))
                {
                    Output.WriteLine("  <meta name=\"description\" content=\"" + staticWebContent.Description.Replace("\"", "'") + "\" />");
                }
                if (!String.IsNullOrEmpty(staticWebContent.Keywords))
                {
                    Output.WriteLine("  <meta name=\"keywords\" content=\"" + staticWebContent.Keywords.Replace("\"", "'") + "\" />");
                }
                if (!String.IsNullOrEmpty(staticWebContent.Author))
                {
                    Output.WriteLine("  <meta name=\"author\" content=\"" + staticWebContent.Author.Replace("\"", "'") + "\" />");
                }
                if (!String.IsNullOrEmpty(staticWebContent.Date))
                {
                    Output.WriteLine("  <meta name=\"date\" content=\"" + staticWebContent.Date.Replace("\"", "'") + "\" />");
                }
            }

            // If this is an admin and the page was not present, give some options
            if (adminMissingScreen)
            {
                Output.WriteLine("  <link href=\"" + Static_Resources_Gateway.Sobekcm_Admin_Css + "\" rel=\"stylesheet\" type=\"text/css\" />");
            }
            else
            {
                // Write the style sheet to use 
                Output.WriteLine("  <link href=\"" + Static_Resources_Gateway.Sobekcm_Metadata_Css + "\" rel=\"stylesheet\" type=\"text/css\" />");
            }

            // Include the interface's style sheet if it has one
            if ((RequestSpecificValues.HTML_Skin != null) && ( !String.IsNullOrEmpty(RequestSpecificValues.HTML_Skin.CSS_Style)))
            {
                Output.WriteLine("  <link href=\"" + RequestSpecificValues.Current_Mode.Base_URL + RequestSpecificValues.HTML_Skin.CSS_Style + "\" rel=\"stylesheet\" type=\"text/css\" />");
            }

            // Include the interface's javascript file if it has one
            if ((RequestSpecificValues.HTML_Skin != null) && (!String.IsNullOrEmpty(RequestSpecificValues.HTML_Skin.Javascript)))
            {
                Output.WriteLine("   <script type=\"text/javascript\" src=\"" + RequestSpecificValues.Current_Mode.Base_URL + RequestSpecificValues.HTML_Skin.Javascript + "\"  id=\"SobekCmWebSkinJavascript\" ></script>");
            }

            // Add the special CSS here
            if (staticWebContent != null)
            {
                if (!String.IsNullOrEmpty(staticWebContent.CssFile))
                {
                    Output.WriteLine("  <link href=\"" + RequestSpecificValues.Current_Mode.Base_URL + "/design/webcontent/css/" + staticWebContent.CssFile + "\" rel=\"stylesheet\" type=\"text/css\" id=\"SobekCmControlledCss\" />");
                }

                // Add the special javascript here
                if (!String.IsNullOrEmpty(staticWebContent.JavascriptFile))
                {
                    Output.WriteLine("  <script type=\"text/javascript\" src=\"" + RequestSpecificValues.Current_Mode.Base_URL + "/design/webcontent/javascript/" + staticWebContent.JavascriptFile + "\" id=\"SobekCmControlledJavascript\" ></script>");
                }

                // If this is the static html web content view, add any special text which came from the original
                // static html file which was already read, which can include style sheets, etc..
                if (!String.IsNullOrEmpty(staticWebContent.Extra_Head_Info))
                {
                    Output.WriteLine("  " + staticWebContent.Extra_Head_Info.Trim());
                }

                // Add the javascript for the HTML Editor
                if ((canEdit) && (RequestSpecificValues.Current_Mode.WebContent_Type == WebContent_Type_Enum.Edit))
                {
                    // Build the folder which will include the uploads
                    HTML_Based_Content webContent = staticWebContent;
                    string urlSegments = webContent.UrlSegments;
                    string webcontent_upload_dir = UI_ApplicationCache_Gateway.Settings.Servers.Base_Design_Location + "webcontent\\" + urlSegments.Replace("/", "\\");
                    string webcontent_upload_url = UI_ApplicationCache_Gateway.Settings.Servers.System_Base_URL + "design/webcontent/" + urlSegments.Replace("\\","/") + "/";

                    // Create the CKEditor object
                    CKEditor editor = new CKEditor
                    {
                        BaseUrl = RequestSpecificValues.Current_Mode.Base_URL,
                        Language = RequestSpecificValues.Current_Mode.Language,
                        TextAreaID = "sbkWchs_TextEdit",
                        FileBrowser_ImageUploadUrl = RequestSpecificValues.Current_Mode.Base_URL + "HtmlEditFileHandler.ashx",
                        UploadPath = webcontent_upload_dir,
                        UploadURL = webcontent_upload_url
                    };

                    // If there are existing files, add a reference to the URL for the image browser
                    if ((Directory.Exists(webcontent_upload_dir)) && (Directory.GetFiles(webcontent_upload_dir).Length > 0))
                    {
                        // Is there an endpoint defined for looking at uploaded files?
                        string upload_files_json_url = SobekEngineClient.WebContent.Uploaded_Files_URL;
                        if (!String.IsNullOrEmpty(upload_files_json_url))
                        {
                            editor.ImageBrowser_ListUrl = String.Format(upload_files_json_url, urlSegments);
                        }
                    }

                    if ((staticWebContent.Content.IndexOf("<script", StringComparison.OrdinalIgnoreCase) >= 0) || (staticWebContent.Content.IndexOf("<input", StringComparison.OrdinalIgnoreCase) >= 0))
                        editor.Start_In_Source_Mode = true;
                    else 
                        editor.Start_In_Source_Mode = false;

                    // Add the HTML from the CKEditor object
                    editor.Add_To_Stream(Output);
                }
            }

            // If this has a viewer than it is a special, non-public view, add the admin CSS
            if (viewer != null)
            {
                Output.WriteLine("  <link rel=\"stylesheet\" type=\"text/css\" href=\"" + Static_Resources_Gateway.Sobekcm_Admin_Css + "\" /> ");
            }
        }

		/// <summary> Gets the CSS class of the container that the page is wrapped within </summary>
		public override string Container_CssClass
		{
			get
			{
                return siteMap != null ? String.Empty : base.Container_CssClass;
			}
		}


        #region Public method to write the internal header

        /// <summary> Adds the internal header HTML for this specific HTML writer </summary>
        /// <param name="Output"> Stream to which to write the HTML for the internal header information </param>
        /// <param name="Current_User"> Currently logged on user, to determine specific rights </param>
        public override void Write_Internal_Header_HTML(TextWriter Output, User_Object Current_User)
        {

            if ((Current_User.Is_Portal_Admin) || (Current_User.Is_System_Admin) || (Current_User.Is_Host_Admin))
            {
                Output.WriteLine("  <table id=\"sbk_InternalHeader\">");
                Output.WriteLine("    <tr style=\"height:45px;\">");
                Output.WriteLine("      <td style=\"text-align:left; width:100px;\">");
                Output.WriteLine("          <button title=\"Hide Internal Header\" class=\"intheader_button_aggr hide_intheader_button_aggr\" onclick=\"return hide_internal_header();\" alt=\"Hide Internal Header\"></button>");
                Output.WriteLine("      </td>");

                Output.WriteLine("      <td style=\"text-align:center; vertical-align:middle\">");

                // Keep current mode, submode, etc..
                Display_Mode_Enum displayMode = RequestSpecificValues.Current_Mode.Mode;
                WebContent_Type_Enum webType = RequestSpecificValues.Current_Mode.WebContent_Type;
                string submode = RequestSpecificValues.Current_Mode.Info_Browse_Mode;

                // Add button to edit this page
                RequestSpecificValues.Current_Mode.WebContent_Type = WebContent_Type_Enum.Edit;
                Output.WriteLine("          <button title=\"View history of use\" class=\"intheader_button_aggr edit_page_button\" onclick=\"window.location.href='" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "';return false;\" ></button>");

                // Add button to view usage statistics information
                RequestSpecificValues.Current_Mode.WebContent_Type = WebContent_Type_Enum.Usage;
                Output.WriteLine("          <button title=\"View history of use\" class=\"intheader_button_aggr show_usage_statistics\" onclick=\"window.location.href='" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "';return false;\" ></button>");

                // Add button to view work log history
                RequestSpecificValues.Current_Mode.WebContent_Type = WebContent_Type_Enum.Milestones;
                Output.WriteLine("          <button title=\"View change log of work history\" class=\"intheader_button_aggr view_worklog_page_button\" onclick=\"window.location.href='" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "';return false;\"></button>");

                if ((Current_User.Is_System_Admin) || (Current_User.Is_Host_Admin))
                {
                    // Add button to delete this page
                    RequestSpecificValues.Current_Mode.WebContent_Type = WebContent_Type_Enum.Delete_Verify;
                    Output.WriteLine("          <button title=\"Delete web content page\" class=\"intheader_button_aggr delete_page_button\" onclick=\"window.location.href='" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "';return false;\"></button>");
                }

                // Add button to view manage menu
                RequestSpecificValues.Current_Mode.WebContent_Type = WebContent_Type_Enum.Manage_Menu;
                Output.WriteLine("          <button title=\"View all management options\" class=\"intheader_button_aggr manage_page_button\" onclick=\"window.location.href='" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "';return false;\"></button>");

                // Add the admin button
                RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Administrative;
                RequestSpecificValues.Current_Mode.Admin_Type = Admin_Type_Enum.WebContent_Single;
                Output.WriteLine("          <button title=\"Edit administrative information\" class=\"intheader_button_aggr admin_view_button\" onclick=\"window.location.href='" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "';return false;\" ></button>");

                Output.WriteLine("      </td>");

                RequestSpecificValues.Current_Mode.Info_Browse_Mode = submode;
                RequestSpecificValues.Current_Mode.Mode = displayMode;
                RequestSpecificValues.Current_Mode.WebContent_Type = webType;

                // Add the HELP icon next
                Output.WriteLine("      <td style=\"text-align:left; width:30px;\">");
                Output.WriteLine("        <span id=\"sbk_InternalHeader_Help\"><a href=\"" + UI_ApplicationCache_Gateway.Settings.System.Help_URL(RequestSpecificValues.Current_Mode.Base_URL) + "help/aggrheader\" title=\"Help regarding this header\" ><img src=\"" + Static_Resources_Gateway.Help_Button_Darkgray_Jpg + "\" alt=\"?\" title=\"Help regarding this header\" /></a></span>");
                Output.WriteLine("      </td>");

                Write_Internal_Header_Search_Box(Output);

                Output.WriteLine("    </tr>");

                Output.WriteLine("  </table>");
            }
            else
            {
                base.Write_Internal_Header_HTML(Output, Current_User);
            }
        }

        #endregion
    }
}
