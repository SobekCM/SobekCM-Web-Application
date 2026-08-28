#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.Navigation;
using SobekCM.Library.Database;
using SobekCM.Tools;
using System;
using System.Data;
using System.IO;

#endregion

namespace SobekCM.Library.MySobekViewer.Submission.Steps
{
    /// <summary> "Does this belong to an existing title?" -- shown only when the chosen Type's
    /// <c>ShowSeriesFinder</c> flag is set (Newspaper, MultiVolume). Comes after Upload, before Metadata. </summary>
    /// <remarks> Only one of these is ever needed -- not polymorphic, no per-Type variation beyond the
    /// flag that gates whether it runs at all. Called directly by <see cref="New_Submission_MySobekViewer"/>,
    /// no interface. Three distinct actions live on this one step (search, attach, start a new title),
    /// distinguished by <c>submission_seriesfinder_mode</c> -- a separate field from the orchestrator's
    /// own <c>submission_action</c>, since "search" must redisplay this same step rather than advance the
    /// wizard the way the generic Continue button does. </remarks>
    public class SeriesFinder_SubmissionStep
    {
        /// <summary> Title shown for this step in the wizard header/stepper </summary>
        public string Step_Title => "Find Series";

        /// <summary> Renders this step's HTML </summary>
        public void Render_HTML(TextWriter Output, Submission_State State, RequestCache RequestSpecificValues, Custom_Tracer Tracer)
        {
            Output.WriteLine("<h1>Does this belong to an existing title?</h1>");
            Output.WriteLine("<p>Search for an existing title to attach this item to, or start a new title.</p>");

            Output.WriteLine("<input type=\"hidden\" id=\"submission_seriesfinder_mode\" name=\"submission_seriesfinder_mode\" value=\"\" />");
            Output.WriteLine("<input type=\"hidden\" id=\"submission_seriesfinder_bibid\" name=\"submission_seriesfinder_bibid\" value=\"\" />");

            Output.WriteLine("<input type=\"text\" id=\"submission_seriesfinder_search\" name=\"submission_seriesfinder_search\" value=\"" +
                              System.Net.WebUtility.HtmlEncode(State.SeriesFinderSearchText ?? String.Empty) + "\" />");
            Output.WriteLine("<button onclick=\"" + mode_onclick("search", String.Empty) + "\">Search</button>");

            if (!String.IsNullOrWhiteSpace(State.SeriesFinderSearchText))
            {
                DataSet resultsSet = SobekCM_Database.Search_Item_Groups_For_Submission(State.SeriesFinderSearchText, State.ItemTypeID, State.MarcTypeOfResource, Tracer);
                if ((resultsSet == null) || (resultsSet.Tables.Count == 0) || (resultsSet.Tables[0].Rows.Count == 0))
                {
                    Output.WriteLine("<p><i>No matching titles found.</i></p>");
                }
                else
                {
                    Output.WriteLine("<div class=\"sbkNsub_SeriesResults\">");
                    foreach (DataRow thisRow in resultsSet.Tables[0].Rows)
                    {
                        string bibid = thisRow["BibID"].ToString();
                        string title = thisRow["GroupTitle"].ToString();
                        int itemCount = Convert.ToInt32(thisRow["ItemCount"]);

                        Output.WriteLine("  <div class=\"sbkNsub_SeriesResult\">");
                        Output.WriteLine("    <span>" + System.Net.WebUtility.HtmlEncode(title) + "</span>");
                        Output.WriteLine("    <span>BibID " + System.Net.WebUtility.HtmlEncode(bibid) + " &middot; " + itemCount + " item" + (itemCount == 1 ? "" : "s") + " on file</span>");
                        Output.WriteLine("    <button onclick=\"" + mode_onclick("attach", bibid) + "\">Attach to this title</button>");
                        Output.WriteLine("  </div>");
                    }
                    Output.WriteLine("</div>");
                }
            }

            Output.WriteLine("<button onclick=\"" + mode_onclick("newtitle", String.Empty) + "\">This is a new title</button>");
        }

        /// <summary> Builds the inline onclick JS for one of this step's three actions -- fully
        /// self-contained, no shared JS function assumed </summary>
        private static string mode_onclick(string mode, string bibid)
        {
            string safeBibid = System.Net.WebUtility.HtmlEncode(bibid).Replace("'", "&#39;");
            return "document.getElementById(&#39;submission_seriesfinder_mode&#39;).value=&#39;" + mode + "&#39;; " +
                   "document.getElementById(&#39;submission_seriesfinder_bibid&#39;).value=&#39;" + safeBibid + "&#39;; " +
                   "document.getElementById(&#39;submission_action&#39;).value=&#39;seriesfinder&#39;; " +
                   "document.itemNavForm.submit(); return false;";
        }

        /// <summary> Handles a postback from this step </summary>
        /// <returns> TRUE if this step is complete and the wizard should advance </returns>
        public bool Handle_Postback(IFormCollection Form, Submission_State State, RequestCache RequestSpecificValues, Custom_Tracer Tracer)
        {
            string mode = Form["submission_seriesfinder_mode"];

            if (mode == "search")
            {
                State.SeriesFinderSearchText = Form["submission_seriesfinder_search"];
                return false;
            }

            if (mode == "attach")
            {
                string bibid = Form["submission_seriesfinder_bibid"];
                if (String.IsNullOrWhiteSpace(bibid))
                {
                    State.ValidationMessage = "Please select a title to attach to, or start a new title.";
                    return false;
                }

                State.AttachToExistingBibID = bibid;
                return true;
            }

            if (mode == "newtitle")
            {
                State.AttachToExistingBibID = null;
                return true;
            }

            // The generic footer Continue button was used without an explicit choice on this step
            State.ValidationMessage = "Please select an existing title to attach to, or start a new title.";
            return false;
        }
    }
}
