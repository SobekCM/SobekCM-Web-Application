#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.Aggregations;
using SobekCM.Core.Navigation;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Library.AggregationViewer.Viewers;
using SobekCM.Library.Database;
using SobekCM.Library.Localization;
using SobekCM.Tools;
using System;
using System.Data;
using System.IO;

#endregion

namespace SobekCM.Library.AggregationViewer
{
    /// <summary> Aggregation viewer displays all the work history related to an item aggregation </summary>
    public class Work_History_AggregationViewer : abstractAggregationViewer
    {
        /// <summary> Constructor for a new instance of the Work_History_AggregationViewer class </summary>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        /// <param name="ViewBag"> Aggregation-specific request information, such as aggregation object and any browse object requested </param>
        public Work_History_AggregationViewer(RequestCache RequestSpecificValues, AggregationViewBag ViewBag, HttpContext Context)
            : base(RequestSpecificValues, ViewBag, Context)
        {
            // User must AT LEAST be logged on, return
            if ((RequestSpecificValues.Current_User == null) || (!RequestSpecificValues.Current_User.LoggedOn))
            {
                RequestSpecificValues.Current_Mode.Aggregation_Type = Aggregation_Type_Enum.Home;
                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode, Context);
                return;
            }

            // If the user is not an admin of some type, also return
            if ((!RequestSpecificValues.Current_User.Is_System_Admin) && (!RequestSpecificValues.Current_User.Is_Portal_Admin) && (!RequestSpecificValues.Current_User.Is_Aggregation_Curator(ViewBag.Hierarchy_Object.Code)))
            {
                RequestSpecificValues.Current_Mode.Aggregation_Type = Aggregation_Type_Enum.Home;
                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode, Context);
            }
        }

        /// <summary> Gets the type of collection view or search supported by this collection viewer </summary>
        public override Item_Aggregation_Views_Searches_Enum Type
        {
            get { return Item_Aggregation_Views_Searches_Enum.Work_History; }
        }

        /// <summary> Flag which indicates whether the selection panel should be displayed </summary>
        /// <value> This defaults to <see cref="Selection_Panel_Display_Enum.Selectable"/> but is overwritten by most collection viewers </value>
        public override Selection_Panel_Display_Enum Selection_Panel_Display
        {
            get { return Selection_Panel_Display_Enum.Never; }
        }

        /// <summary> Gets flag which indicates whether this is an internal view, which may have a 
        /// slightly different design feel </summary>
        /// <remarks> This returns FALSE by default, but can be overriden by individual viewer implementations</remarks>
        public override bool Is_Internal_View
        {
            get { return true; }
        }

        /// <summary> Title for the page that displays this viewer, this is shown in the search box at the top of the page, just below the banner </summary>
        public override string Viewer_Title
        {
            get { return Localization_Gateway.Work_History_Aggregation.Viewer_Title(RequestSpecificValues.Current_Mode.Language); }
        }

        /// <summary> Gets the URL for the icon related to this aggregational viewer task </summary>
        public override string Viewer_Icon
        {
            get { return Static_Resources_Gateway.View_Work_Log_Img; }
        }

        /// <summary> Add the HTML to be displayed in the search box </summary>
        /// <param name="Output"> Textwriter to write the HTML for this viewer</param>
        /// <param name="Tracer">Trace object keeps a list of each method executed and important milestones in rendering</param>
        /// <remarks> This does nothing - as an internal type view, this will not be called </remarks>
        public override void Write_Search_Box_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            // Do nothing
        }

        /// <summary> Add the main HTML to be added to the page </summary>
        /// <param name="Output"> Textwriter to write the HTML for this viewer</param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
        /// <remarks> This writes the HTML from the static browse or info page here  </remarks>
        public override void Write_Main_HTML(TextWriter Output, Custom_Tracer Tracer)
        {

            string language = RequestSpecificValues.Current_Mode.Language;
            DataTable historyTbl = SobekCM_Database.Get_Aggregation_Change_Log(ViewBag.Hierarchy_Object.Code, RequestSpecificValues.Tracer);

            if ((historyTbl == null) || (historyTbl.Rows.Count == 0))
            {
                Output.WriteLine("<p>" + Localization_Gateway.Work_History_Aggregation.No_History_Message(language) + "</p>");

                Output.WriteLine("<p>" + Localization_Gateway.Work_History_Aggregation.No_History_Explanation(language) + "</p>");

                return;
            }

            Output.WriteLine("<p style=\"text-align: left; padding:0 20px 0 20px;\">" + Localization_Gateway.Work_History_Aggregation.Change_Log_Intro(language) + "</p>");

            Output.WriteLine("  <table class=\"sbkWhav_Table\">");
            Output.WriteLine("    <tr>");
            Output.WriteLine("      <th style=\"width:100px;\">" + Localization_Gateway.Work_History_Aggregation.Date_Column(language) + "</th>");
            Output.WriteLine("      <th style=\"width:180px;\">" + Localization_Gateway.Work_History_Aggregation.User_Column(language) + "</th>");
            Output.WriteLine("      <th style=\"width:500px;\">" + Localization_Gateway.Work_History_Aggregation.Change_Description_Column(language) + "</th>");
            Output.WriteLine("    </tr>");

            foreach (DataRow thisChange in historyTbl.Rows)
            {
                Output.WriteLine("    <tr>");
                Output.WriteLine("      <td>" + Convert.ToDateTime(thisChange[1]).ToShortDateString() + "</td>");
                Output.WriteLine("      <td>" + thisChange[2] + "</td>");
                Output.WriteLine("      <td>" + thisChange[0].ToString().Replace("\n", "<br />") + "</td>");
                Output.WriteLine("    </tr>");
                Output.WriteLine("    <tr class=\"sbkWhav_TableRule\"><td colspan=\"3\"></td></tr>");
            }

            Output.WriteLine("  </table>");
            Output.WriteLine("  <br /><br />");
        }
    }
}
