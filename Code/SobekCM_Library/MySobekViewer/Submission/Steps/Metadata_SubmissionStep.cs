#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.Navigation;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Library.Citation.Template;
using SobekCM.Library.Database;
using SobekCM.Library.UI;
using SobekCM.Resource_Object;
using SobekCM.Tools;
using System;
using System.Data;
using System.IO;

#endregion

namespace SobekCM.Library.MySobekViewer.Submission.Steps
{
    /// <summary> The one metadata entry page -- assembled from the chosen Type's ordered metadata
    /// blocks, rendered as a trimmed-down <c>CompleteTemplate</c> </summary>
    /// <remarks> Every assigned <c>SobekCM_Metadata_Block</c> becomes one <see cref="Template_Panel"/>
    /// (via <see cref="Template_XML_Reader.Read_Panel_XML"/>), all added to a single
    /// <see cref="Template_Page"/> on a fresh <see cref="CompleteTemplate"/> built new on every request --
    /// deliberately not cached, unlike the old per-file <c>Template_MemoryMgmt_Utility</c> cache, so an
    /// admin editing a Type's block list or a block's XML is reflected immediately rather than waiting
    /// out a sliding cache expiration. First-pass scope, per Mark: letting a submitter add a block not in
    /// the Type's default bundle is NOT built here yet (see the 5.2.0 redesign notes' point 5/6 scope-prompt
    /// idea) -- only the admin-assigned bundle renders. Full <c>SobekCM_Item_Validator</c> validation and
    /// the old edit viewers' "complex element" pop-up-form add flow (typed <c>new_element_requested</c>
    /// values like "name") are also not ported -- a complex repeatable element's postback is handled
    /// safely (redisplays this step without advancing) but won't yet add a new blank instance the way
    /// <c>Edit_Item_Metadata_MySobekViewer</c> does; ordinary <c>repeatable="true"</c> simple elements are
    /// unaffected, since those are added purely client-side by <c>sobekcm_metadata.js</c>. Not
    /// polymorphic, no interface, called directly by <see cref="New_Submission_MySobekViewer"/>. </remarks>
    public class Metadata_SubmissionStep
    {
        /// <summary> Title shown for this step in the wizard header/stepper </summary>
        public string Step_Title => "Metadata";

        /// <summary> Builds a fresh <see cref="CompleteTemplate"/> from the chosen Type's ordered
        /// metadata blocks -- one <see cref="Template_Panel"/> per block, all on a single page </summary>
        private static CompleteTemplate build_template(int ItemTypeID, Custom_Tracer Tracer)
        {
            var completeTemplate = new CompleteTemplate();
            var page = new Template_Page();

            DataSet blocksSet = SobekCM_Database.Get_Item_Type_Blocks(ItemTypeID, Tracer);
            if ((blocksSet != null) && (blocksSet.Tables.Count > 0))
            {
                var reader = new Template_XML_Reader();
                foreach (DataRow thisRow in blocksSet.Tables[0].Rows)
                {
                    string blockXml = thisRow["BlockXml"] == DBNull.Value ? String.Empty : thisRow["BlockXml"].ToString();
                    Template_Panel panel = reader.Read_Panel_XML(blockXml);
                    page.Add_Panel(panel);
                }
            }

            completeTemplate.Add_Page(page);
            completeTemplate.Build_Final_Adjustment_And_Checks();
            return completeTemplate;
        }

        /// <summary> Ensures <see cref="Submission_State.Item"/> exists before this step needs to save
        /// into it -- the first step in the wizard to actually need a real <see cref="SobekCM_Item"/> </summary>
        private static void ensure_item(Submission_State State)
        {
            if (State.Item == null)
            {
                State.Item = new SobekCM_Item();
                State.Item.Bib_Info.SobekCM_Type_String = State.MarcTypeOfResource ?? String.Empty;
            }
        }

        /// <summary> Renders this step's HTML </summary>
        public void Render_HTML(TextWriter Output, Submission_State State, RequestCache RequestSpecificValues, Custom_Tracer Tracer)
        {
            ensure_item(State);

            Output.WriteLine("<h1>Describe this " + System.Net.WebUtility.HtmlEncode(State.ItemTypeName) + "</h1>");
            Output.WriteLine("<script type=\"text/javascript\" src=\"" + Static_Resources_Gateway.Jquery_Ui_1_14_2_Js + "\"></script>");

            CompleteTemplate completeTemplate = build_template(State.ItemTypeID, Tracer);

            bool isMozilla = (!String.IsNullOrEmpty(RequestSpecificValues.Current_Mode.Browser_Type)) &&
                              (RequestSpecificValues.Current_Mode.Browser_Type.ToUpper().IndexOf("FIREFOX") >= 0);
            string skinCode = RequestSpecificValues.Current_Mode.Skin == RequestSpecificValues.Current_Mode.Default_Skin
                ? RequestSpecificValues.Current_Mode.Skin.ToUpper()
                : RequestSpecificValues.Current_Mode.Skin;

            string popUpFormsHtml = completeTemplate.Render_Template_HTML(Output, State.Item, skinCode, isMozilla,
                RequestSpecificValues.Current_User, RequestSpecificValues.Current_Mode.Language, UI_ApplicationCache_Gateway.Translation,
                RequestSpecificValues.Current_Mode.Base_URL, 1);

            Output.WriteLine("<!-- Hidden field is used for postbacks to add new form elements (i.e., new name, new other titles, etc..) -->");
            Output.WriteLine("<input type=\"hidden\" id=\"new_element_requested\" name=\"new_element_requested\" value=\"\" />");

            if (!String.IsNullOrEmpty(popUpFormsHtml))
                Output.WriteLine(popUpFormsHtml);
        }

        /// <summary> Handles a postback from this step </summary>
        /// <returns> TRUE if this step is complete and the wizard should advance </returns>
        public bool Handle_Postback(IFormCollection Form, Submission_State State, RequestCache RequestSpecificValues, HttpContext Context, Custom_Tracer Tracer)
        {
            ensure_item(State);

            CompleteTemplate completeTemplate = build_template(State.ItemTypeID, Tracer);
            completeTemplate.Save_To_Bib(State.Item, RequestSpecificValues.Current_User, 1, Context);

            // A request for a complex repeatable element's pop-up add form (e.g. another Name) --
            // not built yet (see class remarks); redisplay this step rather than incorrectly advance
            string hiddenRequest = Form["new_element_requested"].ToString();
            if (!String.IsNullOrEmpty(hiddenRequest))
                return false;

            return true;
        }
    }
}
