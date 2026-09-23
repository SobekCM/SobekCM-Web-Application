#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.Navigation;
using SobekCM.Core.Users;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Engine_Library.Database;
using SobekCM.Library.Database;
using SobekCM.Library.Helpers.CKEditor5;
using SobekCM.Library.HTML.Helpers;
using SobekCM.Library.Localization;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;

#endregion

namespace SobekCM.Library.AdminViewer
{
    /// <summary> Class allows a system administrator or news administrator to write, target, retire and delete the
    /// news shown in a banner at the top of every page (see <see cref="News_HtmlHelper"/>) </summary>
    /// <remarks> This class extends the <see cref="abstract_AdminViewer"/> class.<br /><br />
    /// A news item is shown to everyone it targets, from its start date through its expiration date, until each
    /// person closes it.  It can target everyone (including visitors who are not logged on, e.g. "The library will be
    /// closed on Labor Day"), all logged-on users, the administrators, the collection managers, or the members of
    /// certain user groups.  Upgrade scripts add release news for the administrators the same way.  A news administrator
    /// can reach this screen (from their mySobek home page) but no other admin screen. </remarks>
    public class News_AdminViewer : abstract_AdminViewer
    {
        private readonly string actionMessage;
        private readonly bool actionIsError;
        private readonly List<User_News_Item> allNews;
        private readonly List<User_Group> userGroups;

        // News item currently in the add / edit form
        private readonly User_News_Item formItem;

        /// <summary> Constructor for a new instance of the News_AdminViewer class </summary>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        /// <param name="Context"> Context for this individual HTTP request </param>
        /// <remarks> Postback from saving, editing or deleting news is handled here in the constructor </remarks>
        public News_AdminViewer(RequestCache RequestSpecificValues, HttpContext Context) : base(RequestSpecificValues, Context)
        {
            RequestSpecificValues.Tracer.Add_Trace("News_AdminViewer.Constructor", String.Empty);

            actionMessage = String.Empty;
            formItem = new User_News_Item { NewsID = -1 };

            // Only system administrators and news administrators can manage the news
            if ((RequestSpecificValues.Current_User == null) || ((!RequestSpecificValues.Current_User.Is_System_Admin) && (!RequestSpecificValues.Current_User.Is_News_Admin)))
            {
                RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.My_Sobek;
                RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Home;
                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode, Context);
                return;
            }

            userGroups = Engine_Database.Get_All_User_Groups(RequestSpecificValues.Tracer) ?? new List<User_Group>();

            // Handle any post backs
            if ((RequestSpecificValues.Current_Mode.isPostBack) && (Context.Request.HasFormContentType))
            {
                try
                {
                    var form = Context.Request.Form;
                    string action = (form["admin_news_action"].ToString() ?? String.Empty).Trim().ToLower();
                    Int32.TryParse(form["admin_news_id"], out int newsId);

                    switch (action)
                    {
                        case "save":
                            User_News_Item toSave = read_form(form, newsId);
                            formItem = toSave;
                            string problem = validate(toSave);
                            if (problem.Length > 0)
                            {
                                actionMessage = problem;
                                actionIsError = true;
                            }
                            else if (SobekCM_Database.Save_News(toSave, RequestSpecificValues.Current_User.Full_Name, RequestSpecificValues.Tracer) < 1)
                            {
                                actionMessage = "Unable to save the news.  The database may not have been upgraded to 5.2.0 yet.";
                                actionIsError = true;
                            }
                            else
                            {
                                actionMessage = (toSave.NewsID > 0) ? "News saved." : "News added.";
                                formItem = new User_News_Item { NewsID = -1 };
                                news_changed();
                            }
                            break;

                        case "edit":
                            User_News_Item toEdit = (SobekCM_Database.Get_All_News(RequestSpecificValues.Tracer) ?? new List<User_News_Item>()).FirstOrDefault(Item => Item.NewsID == newsId);
                            if (toEdit != null)
                                formItem = toEdit;
                            break;

                        case "delete":
                            if (SobekCM_Database.Delete_News(newsId, RequestSpecificValues.Tracer))
                            {
                                actionMessage = "News deleted.";
                                news_changed();
                            }
                            else
                            {
                                actionMessage = "Unable to delete the news.";
                                actionIsError = true;
                            }
                            break;

                        case "reset":
                            if (SobekCM_Database.Reset_News_Dismissals(newsId, RequestSpecificValues.Tracer))
                            {
                                actionMessage = "This news will be shown again to everyone who closed it (logged-on users only; visitors who are not logged on keep it closed in their browser).";
                                news_changed();
                            }
                            else
                            {
                                actionMessage = "Unable to reset who closed this news.";
                                actionIsError = true;
                            }
                            break;
                    }
                }
                catch (Exception ee)
                {
                    RequestSpecificValues.Tracer.Add_Trace("News_AdminViewer.Constructor", "Exception caught: " + ee.Message, Custom_Trace_Type_Enum.Error);
                    actionMessage = "Unknown error caught while handling your request";
                    actionIsError = true;
                }
            }

            allNews = SobekCM_Database.Get_All_News(RequestSpecificValues.Tracer);
        }

        /// <summary> Title for the page that displays this viewer, this is shown in the search box at the top of the page, just below the banner </summary>
        /// <value> This always returns the value 'Site News' </value>
        public override string Web_Title
        {
            get { return "Site News"; }
        }

        /// <summary> Gets the URL for the icon related to this administrative task </summary>
        public override string Viewer_Icon
        {
            get { return Static_Resources_Gateway.WebContent_Img; }
        }

        /// <summary> Add the HTML to be displayed in the main SobekCM viewer area </summary>
        /// <param name="Output"> Textwriter to write the HTML for this viewer</param>
        /// <param name="Tracer">Trace object keeps a list of each method executed and important milestones in rendering</param>
        public override void Write_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace("News_AdminViewer.Write_HTML");

            string language = RequestSpecificValues.Current_Mode.Language;

            // Open the item nav form
            Write_ItemNavForm_Opening(Output);

            Output.WriteLine("<!-- News_AdminViewer.Write_HTML -->");
            Output.WriteLine("<style>");
            Output.WriteLine("  .sbkNwav_Form { border: 1px solid #ccc; background: #fafafa; padding: 12px 16px; margin: 0 0 20px 0; }");
            Output.WriteLine("  .sbkNwav_Form td { padding: 5px 8px 5px 0; vertical-align: top; text-align: left; }");
            Output.WriteLine("  .sbkNwav_Form td.sbkNwav_Label { width: 110px; font-weight: bold; padding-top: 8px; }");
            Output.WriteLine("  .sbkNwav_Title { width: 600px; max-width: 95%; }");
            Output.WriteLine("  .sbkNwav_Body { width: 100%; height: 160px; }");
            Output.WriteLine("  .sbkNwav_Audience label { display: inline-block; min-width: 230px; margin: 2px 12px 2px 0; }");
            Output.WriteLine("  .sbkNwav_Audience .sbkNwav_Everyone { font-weight: bold; }");
            Output.WriteLine("  .sbkNwav_Hint { color: #666; font-size: 0.9em; }");
            Output.WriteLine("  .sbkNwav_Error { color: #b00020; font-weight: bold; }");
            Output.WriteLine("  .sbkNwav_Table td { vertical-align: top; }");
            Output.WriteLine("  .sbkNwav_Status_Showing { color: #1b7a1b; font-weight: bold; }");
            Output.WriteLine("  .sbkNwav_Status_Other { color: #777; }");
            Output.WriteLine("</style>");

            Output.WriteLine("<input type=\"hidden\" id=\"admin_news_action\" name=\"admin_news_action\" value=\"\" />");
            Output.WriteLine("<input type=\"hidden\" id=\"admin_news_id\" name=\"admin_news_id\" value=\"" + formItem.NewsID + "\" />");
            Output.WriteLine();

            Output.WriteLine("<script type=\"text/javascript\">");
            Output.WriteLine("  function news_action(action, id) {");
            Output.WriteLine("    if ((action == 'delete') && (!confirm('Delete this news?  This cannot be undone.'))) return false;");
            Output.WriteLine("    if ((action == 'reset') && (!confirm('Show this news again to every logged-on user who already closed it?'))) return false;");
            Output.WriteLine("    document.getElementById('admin_news_action').value = action;");
            Output.WriteLine("    document.getElementById('admin_news_id').value = id;");
            Output.WriteLine("    document.getElementById('admin_news_id').form.submit();");
            Output.WriteLine("    return false;");
            Output.WriteLine("  }");
            Output.WriteLine("  function news_save() {");
            Output.WriteLine("    document.getElementById('admin_news_action').value = 'save';");
            Output.WriteLine("    return true;");
            Output.WriteLine("  }");
            Output.WriteLine("</script>");

            Output.WriteLine("<div class=\"sbkAdm_HomeText\">");

            if (actionMessage.Length > 0)
            {
                Output.WriteLine("  <br />");
                Output.WriteLine("  <div id=\"sbkAdm_ActionMessage\"" + (actionIsError ? " class=\"sbkNwav_Error\"" : String.Empty) + ">" + WebUtility.HtmlEncode(actionMessage) + "</div>");
            }

            Output.WriteLine("  <p>News appears in a banner at the very top of every page for the people it is meant for, from its start date through the day it expires, until each person closes it.  News for <strong>everyone</strong> is also shown to visitors who are not logged on, which suits notices such as <em>&quot;The library will be closed on Labor Day&quot;</em>.</p>");
            Output.WriteLine("  <p>Upgrades may add news for the administrators here as well.</p>");
            Output.WriteLine();

            // Add / edit form
            Output.WriteLine("  <h2>" + ((formItem.NewsID > 0) ? "Edit News" : "Add News") + "</h2>");
            Output.WriteLine("  <div class=\"sbkNwav_Form\">");
            Output.WriteLine("    <table style=\"width:100%;\">");

            Output.WriteLine("      <tr><td class=\"sbkNwav_Label\"><label for=\"news_title\">Title:</label></td><td><input class=\"sbkNwav_Title sbkAdmin_Focusable\" name=\"news_title\" id=\"news_title\" type=\"text\" maxlength=\"255\" value=\"" + WebUtility.HtmlEncode(formItem.Title) + "\" /></td></tr>");

            Output.WriteLine("      <tr><td class=\"sbkNwav_Label\"><label for=\"news_body\">Message:</label></td><td>");
            Output.WriteLine("        <textarea class=\"sbkNwav_Body\" name=\"news_body\" id=\"news_body\">" + WebUtility.HtmlEncode(formItem.Body) + "</textarea>");
            CKEditor5 editor = new CKEditor5
            {
                Context = Context,
                BaseUrl = RequestSpecificValues.Current_Mode.Base_URL,
                TextAreaID = "news_body",
                UploadPath = Path.Combine(UI_ApplicationCache_Gateway.Settings.Servers.Base_Design_Location, "news", "uploads"),
                UploadURL = UI_ApplicationCache_Gateway.Settings.Servers.System_Base_URL + "design/news/uploads/",
                Start_In_Source_Mode = false
            };
            editor.Add_To_Stream(Output, true);
            Output.WriteLine("      </td></tr>");

            Output.WriteLine("      <tr><td class=\"sbkNwav_Label\">Show to:</td><td class=\"sbkNwav_Audience\">");
            write_audience_checkbox(Output, "news_everyone", "Everyone, including visitors who are not logged on", formItem.For_Everyone, null);
            Output.WriteLine("        <br />");
            write_audience_checkbox(Output, "news_allusers", "All logged-on users", formItem.For_All_Users, null);
            write_audience_checkbox(Output, "news_admins", "Administrators (system, portal, user and news administrators)", formItem.For_Admins, null);
            write_audience_checkbox(Output, "news_collmanagers", "Collection managers", formItem.For_Collection_Managers, null);
            if (userGroups.Count > 0)
            {
                Output.WriteLine("        <div style=\"margin-top:6px;\">Members of these user groups:</div>");
                foreach (User_Group thisGroup in userGroups.OrderBy(Group => Group.Name))
                    write_audience_checkbox(Output, "news_group_" + thisGroup.UserGroupID, thisGroup.Name, formItem.User_Group_IDs.Contains(thisGroup.UserGroupID), null);
            }
            Output.WriteLine("        <div class=\"sbkNwav_Hint\">(Shown to anyone who matches at least one of the checked choices.)</div>");
            Output.WriteLine("      </td></tr>");

            Output.WriteLine("      <tr><td class=\"sbkNwav_Label\"><label for=\"news_start\">Starts:</label></td><td><input type=\"date\" name=\"news_start\" id=\"news_start\" value=\"" + formItem.Start_Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "\" /></td></tr>");
            Output.WriteLine("      <tr><td class=\"sbkNwav_Label\"><label for=\"news_end\">Expires:</label></td><td><input type=\"date\" name=\"news_end\" id=\"news_end\" value=\"" + (formItem.End_Date.HasValue ? formItem.End_Date.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : String.Empty) + "\" /> <span class=\"sbkNwav_Hint\">Last day it is shown.  Leave blank to show it until each person closes it.</span></td></tr>");
            Output.WriteLine("      <tr><td class=\"sbkNwav_Label\">Active:</td><td><label><input type=\"checkbox\" name=\"news_active\" id=\"news_active\"" + (formItem.Is_Active ? " checked=\"checked\"" : String.Empty) + " /> Show this news</label> <span class=\"sbkNwav_Hint\">(Uncheck to hide it without deleting it).</span></td></tr>");

            Output.WriteLine("      <tr><td></td><td style=\"padding-top:10px;\">");
            if (formItem.NewsID > 0)
            {
                RequestSpecificValues.Current_Mode.Admin_Type = Admin_View_Codes.News;
                Output.WriteLine("        <button title=\"Do not save these changes\" class=\"sbkAdm_RoundButton\" onclick=\"window.location.href='" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "'; return false;\"><img src=\"" + Static_Resources_Gateway.Button_Previous_Arrow_Png + "\" class=\"sbkAdm_RoundButton_LeftImg\" alt=\"\" /> " + Localization_Gateway.Buttons.Cancel(language) + "</button> &nbsp; &nbsp; ");
            }
            Output.WriteLine("        <button title=\"Save this news\" class=\"sbkAdm_RoundButton\" type=\"submit\" onclick=\"return news_save();\">" + Localization_Gateway.Buttons.Save(language) + " <img src=\"" + Static_Resources_Gateway.Button_Next_Arrow_Png + "\" class=\"sbkAdm_RoundButton_RightImg\" alt=\"\" /></button>");
            Output.WriteLine("      </td></tr>");
            Output.WriteLine("    </table>");
            Output.WriteLine("  </div>");

            // Existing news
            Output.WriteLine("  <h2>Existing News</h2>");
            if (allNews == null)
            {
                Output.WriteLine("  <p class=\"sbkNwav_Error\">Unable to read the news from the database.  The database may not have been upgraded to 5.2.0 yet.</p>");
            }
            else if (allNews.Count == 0)
            {
                Output.WriteLine("  <p>There is no news yet.</p>");
            }
            else
            {
                Output.WriteLine("  <table class=\"sbkNwav_Table sbkAdm_Table\">");
                Output.WriteLine("    <tr>");
                Output.WriteLine("      <th>ACTIONS</th>");
                Output.WriteLine("      <th>NEWS</th>");
                Output.WriteLine("      <th>SHOWN TO</th>");
                Output.WriteLine("      <th>DATES</th>");
                Output.WriteLine("      <th>STATUS</th>");
                Output.WriteLine("      <th title=\"Logged-on users who closed this news\">CLOSED BY</th>");
                Output.WriteLine("    </tr>");
                Output.WriteLine("    <tr><td class=\"sbkAdm_TableRule\" colspan=\"6\"></td></tr>");

                string jsRequired = RequestSpecificValues.Current_Mode.Base_URL + "l/technical/javascriptrequired";
                foreach (User_News_Item newsItem in allNews)
                {
                    Output.WriteLine("    <tr style=\"text-align:left;\">");
                    Output.Write("      <td class=\"sbkAdm_ActionLink\">( ");
                    Output.Write("<a title=\"Edit this news\" href=\"" + jsRequired + "\" onclick=\"return news_action('edit', " + newsItem.NewsID + ");\">edit</a> | ");
                    Output.Write("<a title=\"Delete this news\" href=\"" + jsRequired + "\" onclick=\"return news_action('delete', " + newsItem.NewsID + ");\">delete</a>");
                    if (newsItem.Dismissed_Count > 0)
                        Output.Write(" | <a title=\"Show this news again to the logged-on users who closed it\" href=\"" + jsRequired + "\" onclick=\"return news_action('reset', " + newsItem.NewsID + ");\">show again</a>");
                    Output.WriteLine(" )</td>");

                    Output.WriteLine("      <td><strong>" + WebUtility.HtmlEncode(newsItem.Title) + "</strong><div class=\"sbkNwav_Hint\">" + WebUtility.HtmlEncode(brief_text(newsItem.Body)) + "</div></td>");
                    Output.WriteLine("      <td>" + WebUtility.HtmlEncode(audience_summary(newsItem)) + "</td>");
                    Output.WriteLine("      <td style=\"white-space:nowrap;\">" + newsItem.Start_Date.ToShortDateString() + " to<br />" + (newsItem.End_Date.HasValue ? newsItem.End_Date.Value.ToShortDateString() : "(no expiration)") + "</td>");
                    Output.WriteLine("      <td>" + status_html(newsItem) + "</td>");
                    Output.WriteLine("      <td style=\"text-align:center;\">" + newsItem.Dismissed_Count + "</td>");
                    Output.WriteLine("    </tr>");
                    Output.WriteLine("    <tr><td class=\"sbkAdm_TableRule\" colspan=\"6\"></td></tr>");
                }

                Output.WriteLine("  </table>");
            }

            Output.WriteLine("  <br />");
            Output.WriteLine("</div>");
            Output.WriteLine();

            // Close the item nav form
            Write_ItemNavForm_Closing(Output);
        }

        #region Private helper methods

        /// <summary> Reads the news item from the posted add / edit form </summary>
        private User_News_Item read_form(IFormCollection Form, int NewsID)
        {
            User_News_Item item = new User_News_Item
            {
                NewsID = (NewsID > 0) ? NewsID : -1,
                Title = Form["news_title"].ToString().Trim(),
                Body = Form["news_body"].ToString().Trim(),
                For_Everyone = Form.ContainsKey("news_everyone"),
                For_All_Users = Form.ContainsKey("news_allusers"),
                For_Admins = Form.ContainsKey("news_admins"),
                For_Collection_Managers = Form.ContainsKey("news_collmanagers"),
                Is_Active = Form.ContainsKey("news_active")
            };

            foreach (User_Group thisGroup in userGroups)
            {
                if (Form.ContainsKey("news_group_" + thisGroup.UserGroupID))
                    item.User_Group_IDs.Add(thisGroup.UserGroupID);
            }

            if (DateTime.TryParseExact(Form["news_start"].ToString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime start))
                item.Start_Date = start;
            if (DateTime.TryParseExact(Form["news_end"].ToString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime end))
                item.End_Date = end;

            return item;
        }

        /// <summary> Returns a message describing why this news item cannot be saved, or an empty string </summary>
        private static string validate(User_News_Item Item)
        {
            if ((Item.Title.Length == 0) && (Item.Body.Length == 0))
                return "Enter a title or a message.";
            if (Item.Title.Length > 255)
                return "The title cannot be longer than 255 characters.";
            if (Item.Has_No_Audience)
                return "Choose who should see this news.";
            if ((Item.End_Date.HasValue) && (Item.End_Date.Value.Date < Item.Start_Date.Date))
                return "The expiration date cannot be before the start date.";
            return String.Empty;
        }

        /// <summary> After any change, drop the cached news so the change shows up right away </summary>
        private void news_changed()
        {
            // Visitors who are not logged on share one cached copy of the news for everyone
            News_HtmlHelper.Clear_Public_News_Cache();

            // This administrator's own pending news, so they can see the result.  Other logged-on users pick
            // up changes the next time they log on.
            Context.SessionObject()[SessionCache_Keys.PendingNews] = null;
        }

        private static void write_audience_checkbox(TextWriter Output, string Name, string Label, bool Checked, string CssClass)
        {
            Output.WriteLine("        <label" + (String.IsNullOrEmpty(CssClass) ? String.Empty : " class=\"" + CssClass + "\"") + "><input type=\"checkbox\" name=\"" + Name + "\" id=\"" + Name + "\"" + (Checked ? " checked=\"checked\"" : String.Empty) + " /> " + WebUtility.HtmlEncode(Label) + "</label>");
        }

        /// <summary> Plain-text summary of who a news item is shown to </summary>
        private string audience_summary(User_News_Item Item)
        {
            if (Item.For_Everyone)
                return "Everyone";

            List<string> audience = new List<string>();
            if (Item.For_All_Users) audience.Add("All logged-on users");
            if (Item.For_Admins) audience.Add("Administrators");
            if (Item.For_Collection_Managers) audience.Add("Collection managers");
            foreach (int groupId in Item.User_Group_IDs)
            {
                User_Group group = userGroups.FirstOrDefault(Group => Group.UserGroupID == groupId);
                if (group != null)
                    audience.Add("Group: " + group.Name);
            }

            return (audience.Count > 0) ? String.Join(", ", audience) : "(nobody)";
        }

        private static string status_html(User_News_Item Item)
        {
            if (!Item.Is_Active)
                return "<span class=\"sbkNwav_Status_Other\">Inactive</span>";
            if (Item.Start_Date.Date > DateTime.Today)
                return "<span class=\"sbkNwav_Status_Other\">Scheduled</span>";
            if ((Item.End_Date.HasValue) && (Item.End_Date.Value.Date < DateTime.Today))
                return "<span class=\"sbkNwav_Status_Other\">Expired</span>";
            return "<span class=\"sbkNwav_Status_Showing\">Showing</span>";
        }

        /// <summary> First part of a news item's message as plain text, for the list </summary>
        private static string brief_text(string Html)
        {
            if (String.IsNullOrEmpty(Html))
                return String.Empty;

            string text = WebUtility.HtmlDecode(System.Text.RegularExpressions.Regex.Replace(Html, "<[^>]+>", " "));
            text = System.Text.RegularExpressions.Regex.Replace(text, "\\s+", " ").Trim();
            return (text.Length > 140) ? text.Substring(0, 137) + "..." : text;
        }

        #endregion
    }
}
