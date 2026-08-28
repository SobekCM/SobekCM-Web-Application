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
    /// <summary> The Type grid -- "what are you adding to the library?" -- always the same, first real
    /// step of every submission </summary>
    /// <remarks> Not polymorphic -- called directly by <see cref="New_Submission_MySobekViewer"/>, no
    /// interface. </remarks>
    public class TypeSelection_SubmissionStep
    {
        /// <summary> Title shown for this step in the wizard header/stepper </summary>
        public string Step_Title => "Choose Type";

        /// <summary> Renders this step's HTML </summary>
        public void Render_HTML(TextWriter Output, Submission_State State, RequestCache RequestSpecificValues, Custom_Tracer Tracer)
        {
            Output.WriteLine("<h1>What are you adding to the library?</h1>");
            Output.WriteLine("<p>Pick the closest match. Your choice determines which upload options and metadata fields show up next.</p>");

            DataSet typesSet = SobekCM_Database.Get_Item_Types_For_Submission(RequestSpecificValues.Current_User.UserID, Tracer);
            if ((typesSet == null) || (typesSet.Tables.Count == 0) || (typesSet.Tables[0].Rows.Count == 0))
            {
                Output.WriteLine("<p><i>No Item Types are currently available to you. Please contact your administrator.</i></p>");
                return;
            }

            Output.WriteLine("<div class=\"sbkNsub_TypeGrid\">");
            foreach (DataRow thisRow in typesSet.Tables[0].Rows)
            {
                int typeId = Convert.ToInt32(thisRow["TypeID"]);
                string name = thisRow["Name"].ToString();
                string description = thisRow["Description"] == DBNull.Value ? String.Empty : thisRow["Description"].ToString();
                bool selected = (State.ItemTypeID == typeId);

                Output.WriteLine("  <label class=\"sbkNsub_TypeCard\">");
                Output.Write("    <input type=\"radio\" name=\"submission_selected_type\" value=\"" + typeId + "\"");
                if (selected)
                    Output.Write(" checked=\"checked\"");
                Output.WriteLine(" />");
                Output.WriteLine("    <div class=\"sbkNsub_TypeCardName\">" + System.Net.WebUtility.HtmlEncode(name) + "</div>");
                if (!String.IsNullOrEmpty(description))
                    Output.WriteLine("    <div class=\"sbkNsub_TypeCardDesc\">" + System.Net.WebUtility.HtmlEncode(description) + "</div>");
                Output.WriteLine("  </label>");
            }
            Output.WriteLine("</div>");
        }

        /// <summary> Handles a postback from this step </summary>
        /// <returns> TRUE if this step is complete and the wizard should advance </returns>
        public bool Handle_Postback(IFormCollection Form, Submission_State State, RequestCache RequestSpecificValues, Custom_Tracer Tracer)
        {
            if (!Int32.TryParse(Form["submission_selected_type"], out int selectedTypeId))
            {
                State.ValidationMessage = "Please choose a type to continue.";
                return false;
            }

            // Re-derive the Type's details from the same allowlist-filtered query the grid itself was
            // built from, rather than trusting anything else posted alongside the TypeID -- this also
            // rejects a TypeID for a Type this user isn't actually permitted to select
            DataSet typesSet = SobekCM_Database.Get_Item_Types_For_Submission(RequestSpecificValues.Current_User.UserID, Tracer);
            DataRow matchedRow = null;
            if ((typesSet != null) && (typesSet.Tables.Count > 0))
            {
                foreach (DataRow thisRow in typesSet.Tables[0].Rows)
                {
                    if (Convert.ToInt32(thisRow["TypeID"]) == selectedTypeId)
                    {
                        matchedRow = thisRow;
                        break;
                    }
                }
            }

            if (matchedRow == null)
            {
                State.ValidationMessage = "Please choose a type to continue.";
                return false;
            }

            // If the user went back and picked a different Type, any earlier Series Finder choice
            // no longer applies to this Type
            if (State.ItemTypeID != selectedTypeId)
            {
                State.AttachToExistingBibID = null;
                State.SeriesFinderSearchText = null;
            }

            State.ItemTypeID = selectedTypeId;
            State.ItemTypeName = matchedRow["Name"].ToString();
            State.ShowSeriesFinder = Convert.ToBoolean(matchedRow["ShowSeriesFinder"]);
            State.UploadCode = matchedRow["UploadCode"] == DBNull.Value ? String.Empty : matchedRow["UploadCode"].ToString();
            State.BibIDRoot = matchedRow["BibIDRoot"] == DBNull.Value ? String.Empty : matchedRow["BibIDRoot"].ToString();
            State.MarcTypeOfResource = matchedRow["MARC_TypeOfResource"] == DBNull.Value ? String.Empty : matchedRow["MARC_TypeOfResource"].ToString();

            return true;
        }
    }
}
