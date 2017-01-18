using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using SobekCM.Core.BriefItem;
using SobekCM.Core.Navigation;
using SobekCM.Core.Users;
using SobekCM.Engine_Library.ApplicationState;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Library.ItemViewer.Viewers;
using SobekCM.Library.UI;
using SobekCM.Tools;

namespace SobekCM.Library.ItemViewer.Menu
{
    public class StandardItemMenuProvider : iItemMenuProvider
    {

        public void Add_Main_Menu(TextWriter Output, string CurrentCode, bool ItemRestrictedFromUserByIP, bool ItemCheckedOutByOtherUser, BriefItemInfo CurrentItem, Navigation_Object CurrentMode, User_Object CurrentUser, bool Include_Links, Custom_Tracer Tracer)
        {
            // Can this user (if there is one) edit this item?
            bool canManage = (CurrentUser != null) && (CurrentUser.Can_Edit_This_Item(CurrentItem.BibID, CurrentItem.Type, CurrentItem.Behaviors.Source_Institution_Aggregation, CurrentItem.Behaviors.Holding_Location_Aggregation, CurrentItem.Behaviors.Aggregation_Code_List));

            // Add the item views
            Output.WriteLine("<!-- Add the different view and social options -->");
            Output.WriteLine("<nav class=\"sbkMenu_Bar\" id=\"sbkIsw_MenuBar\" role=\"navigation\" aria-label=\"Item menu\">");
            Output.WriteLine("\t<h2 class=\"hidden-element\">Item menu</h2>");

            // Add the sharing buttons if this is not restricted by IP address or checked out
            if ((!ItemRestrictedFromUserByIP) && (!ItemCheckedOutByOtherUser) && (!CurrentMode.Is_Robot))
            {
                string add_text = "Add";
                string remove_text = "Remove";
                string send_text = "Send";
                string print_text = "Print";
                if (canManage)
                {
                    add_text = String.Empty;
                    remove_text = String.Empty;
                    send_text = String.Empty;
                    print_text = String.Empty;
                }

                string logOnUrl = String.Empty;
                bool isLoggedOn = CurrentUser != null && CurrentUser.LoggedOn;
                if (!isLoggedOn)
                {
                    string returnUrl = UrlWriterHelper.Redirect_URL(CurrentMode);

                    CurrentMode.Mode = Display_Mode_Enum.My_Sobek;
                    CurrentMode.My_Sobek_Type = My_Sobek_Type_Enum.Logon;
                    CurrentMode.Return_URL = returnUrl;
                    logOnUrl = UrlWriterHelper.Redirect_URL(CurrentMode);
                    CurrentMode.Mode = Display_Mode_Enum.Item_Display;
                    CurrentMode.Return_URL = String.Empty;
                }

                Output.WriteLine("\t<div id=\"menu-right-actions\">");

                if (( CurrentItem.Web != null ) && ( CurrentItem.Web.ItemID > 0))
                {
                    string print_item_form_open_url = UI_ApplicationCache_Gateway.Settings.Servers.Engine_URL + "items/html/print/" + CurrentItem.BibID + "/" + CurrentItem.VID + "/" + CurrentMode.ViewerCode;
                    Output.WriteLine("\t\t<span id=\"printbuttonitem\" class=\"action-sf-menu-item\" onclick=\"print_form_open('" + print_item_form_open_url + "');\"><img src=\"" + Static_Resources_Gateway.Printer_Png + "\" alt=\"\" style=\"vertical-align:middle\" /><span id=\"printbuttonspan\">" + print_text + "</span></span>");
                }
                else
                {
                    Output.WriteLine("\t\t<span id=\"printbuttonitem\" class=\"action-sf-menu-item\" onclick=\"window.print();return false;\"><img src=\"" + Static_Resources_Gateway.Printer_Png + "\" alt=\"\" style=\"vertical-align:middle\" /><span id=\"printbuttonspan\">" + print_text + "</span></span>");
                }


                if (isLoggedOn)
                {
                    string email_item_form_open_url = UI_ApplicationCache_Gateway.Settings.Servers.Engine_URL + "items/html/send/" + CurrentItem.BibID + "/" + CurrentItem.VID;
                    Output.WriteLine("\t\t<span id=\"sendbuttonitem\" class=\"action-sf-menu-item\" onclick=\"email_form_open('" + email_item_form_open_url + "');\"><img src=\"" + Static_Resources_Gateway.Email_Png + "\" alt=\"\" style=\"vertical-align:middle\" /><span id=\"sendbuttonspan\">" + send_text + "</span></span>");


                    if ((CurrentItem.Web != null) && (CurrentItem.Web.ItemID > 0))
                    {
                        if (CurrentUser.Is_In_Bookshelf(CurrentItem.BibID, CurrentItem.VID))
                        {
                            Output.WriteLine("\t\t<span id=\"addbuttonitem\" class=\"action-sf-menu-item\" onclick=\"return remove_item_itemviewer();\"><img src=\"" + Static_Resources_Gateway.Minussign_Png + "\" alt=\"\" style=\"vertical-align:middle\" /><span id=\"addbuttonspan\">" + remove_text + "</span></span>");
                        }
                        else
                        {
                            // Determine the URL
                            string add_item_form_open_url = UI_ApplicationCache_Gateway.Settings.Servers.Engine_URL + "items/html/bookshelf/" + CurrentUser.UserID + "/" + CurrentItem.BibID + "/" + CurrentItem.VID;
                            Output.WriteLine("\t\t<span id=\"addbuttonitem\" class=\"action-sf-menu-item\" onclick=\"add_item_form_open('" + add_item_form_open_url + "');return false;\"><img src=\"" + Static_Resources_Gateway.Plussign_Png + "\" alt=\"\" style=\"vertical-align:middle\" /><span id=\"addbuttonspan\">" + add_text + "</span></span>");
                        }
                    }
                }
                else
                {
                    Output.WriteLine("\t\t<span id=\"sendbuttonitem\" class=\"action-sf-menu-item\" onclick=\"window.location='" + logOnUrl + "';return false;\"><img src=\"" + Static_Resources_Gateway.Email_Png + "\" alt=\"\" style=\"vertical-align:middle\" /><span id=\"sendbuttonspan\">" + send_text + "</span></span>");

                    if ((CurrentItem.Web != null) && (CurrentItem.Web.ItemID > 0))
                        Output.WriteLine("\t\t<span id=\"addbuttonitem\" class=\"action-sf-menu-item\" onclick=\"window.location='" + logOnUrl + "';return false;\"><img src=\"" + Static_Resources_Gateway.Plussign_Png + "\" alt=\"\" style=\"vertical-align:middle\" /><span id=\"addbuttonspan\">" + add_text + "</span></span>");
                }

                string share_item_form_open_url = UI_ApplicationCache_Gateway.Settings.Servers.Engine_URL + "items/html/share/" + CurrentItem.BibID + "/" + CurrentItem.VID;
                Output.WriteLine("\t\t<span id=\"sharebuttonitem\" class=\"action-sf-menu-item\" onclick=\"toggle_share_form('share_button', '" + share_item_form_open_url + "');\"><span id=\"sharebuttonspan\">Share</span></span>");


                Output.WriteLine("\t</div>");
                Output.WriteLine();
            }


            Output.WriteLine("\t<ul class=\"sf-menu\" id=\"sbkIhs_Menu\">");


            // Save the current view type
            ushort page = CurrentMode.Page.HasValue ? CurrentMode.Page.Value : (ushort)1;
            ushort subpage = CurrentMode.SubPage.HasValue ? CurrentMode.SubPage.Value : (ushort)1;
            string viewerCode = CurrentMode.ViewerCode;
            CurrentMode.SubPage = 0;

            // Add any PRE-MENU instance options
            string first_pre_menu_option = String.Empty;
            string second_pre_menu_option = String.Empty;
            string third_pre_menu_option = String.Empty;
            if (UI_ApplicationCache_Gateway.Settings.Contains_Additional_Setting("Item Viewer.Static First Menu Item"))
                first_pre_menu_option = UI_ApplicationCache_Gateway.Settings.Get_Additional_Setting("Item Viewer.Static First Menu Item");
            if (UI_ApplicationCache_Gateway.Settings.Contains_Additional_Setting("Item Viewer.Static Second Menu Item"))
                second_pre_menu_option = UI_ApplicationCache_Gateway.Settings.Get_Additional_Setting("Item Viewer.Static Second Menu Item");
            if (UI_ApplicationCache_Gateway.Settings.Contains_Additional_Setting("Item Viewer.Static Third Menu Item"))
                third_pre_menu_option = UI_ApplicationCache_Gateway.Settings.Get_Additional_Setting("Item Viewer.Static Third Menu Item");
            if ((first_pre_menu_option.Length > 0) || (second_pre_menu_option.Length > 0) || (third_pre_menu_option.Length > 0))
            {
                if (first_pre_menu_option.Length > 0)
                {
                    string[] first_splitter = first_pre_menu_option.Replace("[", "").Replace("]", "").Split(";".ToCharArray());
                    if (first_splitter.Length > 0)
                    {
                        Output.WriteLine("\t\t<li><a href=\"" + first_splitter[1] + "\" title=\"" + HttpUtility.HtmlEncode(first_splitter[0]) + "\">" + HttpUtility.HtmlEncode(first_splitter[0]) + "</a></li>");
                    }
                }
                if (second_pre_menu_option.Length > 0)
                {
                    string[] second_splitter = second_pre_menu_option.Replace("[", "").Replace("]", "").Split(";".ToCharArray());
                    if (second_splitter.Length > 0)
                    {
                        Output.WriteLine("\t\t<li><a href=\"" + second_splitter[1] + "\" title=\"" + HttpUtility.HtmlEncode(second_splitter[0]) + "\">" + HttpUtility.HtmlEncode(second_splitter[0]) + "</a></li>");
                    }
                }
                if (third_pre_menu_option.Length > 0)
                {
                    string[] third_splitter = third_pre_menu_option.Replace("[", "").Replace("]", "").Split(";".ToCharArray());
                    if (third_splitter.Length > 0)
                    {
                        Output.WriteLine("\t\t<li><a href=\"" + third_splitter[1] + "\" title=\"" + HttpUtility.HtmlEncode(third_splitter[0]) + "\">" + HttpUtility.HtmlEncode(third_splitter[0]) + "</a></li>");
                    }
                }
            }

            // Add the item level viewers - collect the menu portions
            List<Item_MenuItem> menuItems = new List<Item_MenuItem>();
            foreach (string viewType in CurrentItem.UI.Viewers_Menu_Order)
            {
                iItemViewerPrototyper prototyper = ItemViewer_Factory.Get_Viewer_By_ViewType(viewType);
                if (prototyper.Has_Access(CurrentItem, CurrentUser, ItemRestrictedFromUserByIP))
                {
                    prototyper.Add_Menu_items(CurrentItem, CurrentUser, CurrentMode, menuItems, ItemRestrictedFromUserByIP);
                }
            }

            // Now, get ready to start adding the menu items
            Dictionary<string, List<Item_MenuItem>> topMenuToChildren = new Dictionary<string, List<Item_MenuItem>>(StringComparer.OrdinalIgnoreCase);
            foreach (Item_MenuItem menuItem in menuItems)
            {
                if (topMenuToChildren.ContainsKey(menuItem.MenuStripText))
                    topMenuToChildren[menuItem.MenuStripText].Add(menuItem);
                else
                {
                    topMenuToChildren[menuItem.MenuStripText] = new List<Item_MenuItem> {menuItem};
                }
            }
            
            // Now, step through the menu items
            foreach (Item_MenuItem topMenuItem in menuItems)
            {
                HTML_Helper(Output, topMenuItem, CurrentMode, CurrentCode, topMenuToChildren, Include_Links );
            }

            // Set current submode back
            CurrentMode.Page = page;
            CurrentMode.ViewerCode = viewerCode;
            CurrentMode.SubPage = subpage;

            Output.WriteLine("\t</ul>");
            Output.WriteLine("</nav>");
            Output.WriteLine();


            Output.WriteLine("<!-- Initialize the main item menu -->");
            Output.WriteLine("<script>");
            Output.WriteLine("\tjQuery(document).ready(function () { jQuery('ul.sf-menu').superfish(); });");
            Output.WriteLine("</script>");
            Output.WriteLine();
        }

        private static void HTML_Helper(TextWriter Output, Item_MenuItem MenuItem, Navigation_Object Current_Mode, string CurrentViewerCode, Dictionary<string, List<Item_MenuItem>> TopMenuToChildren, bool Include_Links )
        {
            // If there are NO matches, left this top-level menu part was already taken care of
            if ((!TopMenuToChildren.ContainsKey(MenuItem.MenuStripText)) || (TopMenuToChildren[MenuItem.MenuStripText].Count == 0))
                return;

            // Is there only one menu part here
            List<Item_MenuItem> children = TopMenuToChildren[MenuItem.MenuStripText];
            if ((children.Count == 1) && ( String.IsNullOrEmpty(MenuItem.MidMenuText )))
            {
                if (String.Compare(MenuItem.Code, CurrentViewerCode, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    Output.WriteLine("\t\t<li class=\"selected-sf-menu-item\">" + MenuItem.MenuStripText + "</li>");
                    return;
                }

                // When rendering for robots, provide the text and image, but not the link
                if (!Include_Links)
                {
                    Output.WriteLine("\t\t<li><a href=\"\">" + MenuItem.MenuStripText + "</a></li>");
                }
                else
                {
                    Output.WriteLine("\t\t<li><a href=\"" + MenuItem.Link + "\">" + MenuItem.MenuStripText + "</a></li>");
                }
            }
            else
            {
                // Step through and see if this is selected
                bool selected = false;
                foreach (Item_MenuItem childMenu in children)
                {
                    if (String.Compare(childMenu.Code, CurrentViewerCode, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        selected = true;
                        break;
                    }
                }

                // Add the top-level
                string url = children[0].Link;
                if (selected)
                {
                    if (!Include_Links)
                    {
                        Output.WriteLine("\t\t<li class=\"selected-sf-menu-item-link\"><a href=\"\">" + MenuItem.MenuStripText + "</a><ul>");
                    }
                    else
                    {
                        Output.WriteLine("\t\t<li class=\"selected-sf-menu-item-link\"><a href=\"" + url + "\">" + MenuItem.MenuStripText + "</a><ul>");
                    }
                }
                else
                {
                    if (!Include_Links)
                    {
                        Output.WriteLine("\t\t<li><a href=\"\">" + MenuItem.MenuStripText + "</a><ul>");
                    }
                    else
                    {
                        Output.WriteLine("\t\t<li><a href=\"" + url + "\">" + MenuItem.MenuStripText + "</a><ul>");
                    }
                }

                // Add all the children
                foreach (Item_MenuItem childMenu in children)
                {
                    if (!Include_Links)
                    {
                        Output.WriteLine("\t\t\t<li><a href=\"\">" + childMenu.MidMenuText + "</a></li>");
                    }
                    else
                    {
                        Output.WriteLine("\t\t\t<li><a href=\"" + childMenu.Link + "\">" + childMenu.MidMenuText + "</a></li>");
                    }
                }

                // Close this top-level list and menu
                Output.WriteLine("\t\t</ul></li>");
            }

            // Clear this
            TopMenuToChildren[MenuItem.MenuStripText].Clear();
        }
    }
}
