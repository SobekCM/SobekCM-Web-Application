#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.Navigation;
using SobekCM.Core.Users;
using SobekCM.Library.MainWriters;
using SobekCM.Tools;
using SobekCM_Resource_Database;
using System;
using System.Data;
using System.IO;

#endregion

namespace SobekCM.Library.MySobekViewer
{
    /// <summary> Shows a user their own tracked long-running processes (item reprocessing, Google Drive imports,
    /// and anything else written into SobekCM_User_Process), or -- for a system/portal admin -- every user's
    /// processes system-wide </summary>
    /// <remarks> This class extends the <see cref="abstract_MySobekViewer"/> class.<br /><br />
    /// First, deliberately minimal pass -- a plain table, no paging/filtering beyond the active-only default and
    /// the admin my-vs-all toggle. This is the same table meant to eventually back a "running processes" icon in
    /// the site chrome (see SobekCM_User_Process's schema comments); that chrome integration is separate,
    /// not part of this viewer. </remarks>
    public class Process_mySobekViewer : abstract_MySobekViewer
    {
        private readonly User_Object user;
        private readonly bool isAdmin;
        private readonly bool viewingAll;
        private readonly DataTable processes;
        private readonly string notificationMode;

        /// <summary> Constructor for a new instance of the Process_mySobekViewer class </summary>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        public Process_mySobekViewer(RequestCache RequestSpecificValues, HttpContext Context) : base(RequestSpecificValues, Context)
        {
            RequestSpecificValues.Tracer.Add_Trace("Process_mySobekViewer.Constructor", String.Empty);

            // Anonymous access to this viewer is already redirected to Logon before construction
            // (see UserObjectInitializer's gate), so there is always a session user here
            user = RequestSpecificValues.Current_User;

            isAdmin = (user.Is_System_Admin) || (user.Is_Portal_Admin);

            // Admins can toggle to see every user's processes; everyone else always sees just their own.
            // Read from the query string (not form) so this stays correct even on the notification-mode
            // save postback below, since query string values survive a POST just like a GET.
            viewingAll = (isAdmin) && (Context.Request.Query["view"] == "all");

            // Its own Save button (separate from the rest of this read-only screen) posts back with
            // "save_notification_mode" present -- piggy-backs on the same itemNavForm this viewer already
            // wraps its whole body in, rather than needing a second <form>
            if ((RequestSpecificValues.Current_Mode.isPostBack) && (Context.Request.HasFormContentType) && (Context.Request.Form.ContainsKey("save_notification_mode")))
            {
                string submittedMode = Context.Request.Form["notification_mode"];
                if ((submittedMode == "On") || (submittedMode == "Paused") || (submittedMode == "Skip"))
                    user.Add_Setting("ProcessNotificationMode", submittedMode);

                // Post-redirect-get, so a page refresh does not resubmit the save. A plain Response.Redirect
                // (rather than UrlWriterHelper.Redirect) since the "?view=all" toggle is a lightweight query
                // param specific to this viewer, not part of the Navigation_Object model UrlWriterHelper builds from
                Context.Response.Redirect(RequestSpecificValues.Current_Mode.Base_URL + "my/processes" + (viewingAll ? "?view=all" : String.Empty));
                return;
            }

            notificationMode = user.Get_Setting("ProcessNotificationMode", "On");

            processes = viewingAll
                ? SobekCM_Item_Database.Get_User_Process_Mgmt_List(true)
                : SobekCM_Item_Database.Get_User_Process_List_For_User(user.UserID, false);
        }

        /// <summary> Title for the page that displays this viewer, this is shown in the search box at the top of the page, just below the banner </summary>
        public override string Web_Title
        {
            get { return viewingAll ? "All Processes" : "My Processes"; }
        }

        /// <summary> Add the HTML to be displayed in the main SobekCM viewer area </summary>
        /// <param name="Output">Textwriter to write the HTML for this viewer</param>
        /// <param name="Tracer">Trace object keeps a list of each method executed and important milestones in rendering</param>
        public override void Write_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace("Process_mySobekViewer.Write_HTML");

            // Open the item nav form
            Write_ItemNavForm_Opening(Output);

            Output.WriteLine("<h1>" + Web_Title + "</h1>");

            Output.WriteLine("<div class=\"sbkPmsv_NotificationPref\">");
            Output.WriteLine("  <label for=\"notification_mode\">Notifications:</label>");
            Output.WriteLine("  <select name=\"notification_mode\" id=\"notification_mode\" class=\"sbk_Focusable\">");
            Output.WriteLine("    <option value=\"On\"" + (notificationMode == "On" ? " selected=\"selected\"" : "") + ">On</option>");
            Output.WriteLine("    <option value=\"Paused\"" + (notificationMode == "Paused" ? " selected=\"selected\"" : "") + ">Paused</option>");
            Output.WriteLine("    <option value=\"Skip\"" + (notificationMode == "Skip" ? " selected=\"selected\"" : "") + ">Skip</option>");
            Output.WriteLine("  </select>");
            Output.WriteLine("  <button type=\"submit\" name=\"save_notification_mode\" value=\"1\" class=\"sbkMySobek_BigButton\">Save</button>");
            Output.WriteLine("</div>");

            if (isAdmin)
            {
                string baseUrl = UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode);
                Output.WriteLine("<div class=\"sbkPmsv_ViewToggle\">");
                if (viewingAll)
                    Output.WriteLine("  <a href=\"" + baseUrl + "\">My Processes</a> | <b>All Processes</b>");
                else
                    Output.WriteLine("  <b>My Processes</b> | <a href=\"" + baseUrl + "?view=all\">All Processes</a>");
                Output.WriteLine("</div>");
            }

            if ((processes == null) || (processes.Rows.Count == 0))
            {
                Output.WriteLine("<blockquote>No processes to show.</blockquote>");
                Write_ItemNavForm_Closing(Output);
                return;
            }

            Output.WriteLine("<table class=\"sbkPmsv_ProcessTable\" cellpadding=\"5px\">");
            Output.WriteLine("  <tr>");
            if (viewingAll)
                Output.WriteLine("    <th>User</th>");
            Output.WriteLine("    <th>Title</th><th>Type</th><th>Status</th><th>%</th><th>Started</th><th>Completed</th>");
            Output.WriteLine("  </tr>");

            foreach (DataRow row in processes.Rows)
            {
                Output.WriteLine("  <tr>");
                if (viewingAll)
                    Output.WriteLine("    <td>" + row["UserName"] + "</td>");
                Output.WriteLine("    <td>" + row["Title"] + "</td>");
                Output.WriteLine("    <td>" + row["ProcessType"] + "</td>");
                Output.WriteLine("    <td>" + row["Status"] + "</td>");
                Output.WriteLine("    <td>" + (row["PercentComplete"] == DBNull.Value ? "&nbsp;" : row["PercentComplete"] + "%") + "</td>");
                Output.WriteLine("    <td>" + (row["DateProcessStarted"] == DBNull.Value ? "&nbsp;" : Convert.ToDateTime(row["DateProcessStarted"]).ToString("g")) + "</td>");
                Output.WriteLine("    <td>" + (row["DateCompleted"] == DBNull.Value ? "&nbsp;" : Convert.ToDateTime(row["DateCompleted"]).ToString("g")) + "</td>");
                Output.WriteLine("  </tr>");
            }

            Output.WriteLine("</table>");

            // Close the item nav form
            Write_ItemNavForm_Closing(Output);
        }
    }
}
