#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.Navigation;
using SobekCM.Tools;
using System;
using System.IO;

#endregion

namespace SobekCM.Library.MySobekViewer.Submission.Steps
{
    /// <summary> Terminal success/failure landing page -- always shown, never skipped, never backed
    /// into. Content is ported from the old <c>New_Group_And_Item_MySobekViewer.add_congratulations_html</c>,
    /// trimmed of per-CompleteTemplate customization (SuccessfulSubmitMessages, Open Publishing submode,
    /// Default_Visibility branching) that no longer has anywhere to live now that per-user
    /// templates/projects are gone. </summary>
    /// <remarks> Purely a rendering step -- the actual save (<c>New_Submission_MySobekViewer</c>'s
    /// <c>perform_final_submission</c>) runs once, on the postback that first advances into this step,
    /// never from here. This step only ever reads the outcome (<see cref="Submission_State.SubmissionErrorMessage"/>)
    /// that save left behind. Not polymorphic, no interface, called directly by
    /// <see cref="New_Submission_MySobekViewer"/>. </remarks>
    public class Congratulations_SubmissionStep
    {
        /// <summary> Title shown for this step in the wizard header/stepper </summary>
        public string Step_Title => "Done";

        /// <summary> Renders this step's HTML </summary>
        public void Render_HTML(TextWriter Output, Submission_State State, RequestCache RequestSpecificValues, Custom_Tracer Tracer)
        {
            bool failed = !String.IsNullOrEmpty(State.SubmissionErrorMessage);

            Output.WriteLine("<div class=\"sbkNsub_Congratulations\">");

            if (!failed)
            {
                Output.WriteLine("<strong><center><h1>CONGRATULATIONS!</h1></center></strong>");
                Output.WriteLine("<p>Your item has been successfully added to the digital library.</p>");
                Output.WriteLine("<p>Search indexes may take a couple minutes to build, at which time this item will be discoverable through the search interface.</p>");

                if (RequestSpecificValues.Current_User.Send_Email_On_Submission)
                    Output.WriteLine("<p>An email has been sent to you with the new item information.</p>");
            }
            else
            {
                Output.WriteLine("<strong><center><h1>Oops! We encountered a problem!</h1></center></strong>");
                Output.WriteLine("<p>An email has been sent to the programmer who will attempt to correct your issue. You should be contacted within the next 24-48 hours regarding this issue.</p>");
            }

            Output.WriteLine("<p>What would you like to do next?</p>");
            Output.WriteLine("<blockquote>");

            RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Home;
            Output.WriteLine("  <a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">Return to my home</a><br /><br />");

            if (!failed)
            {
                Output.WriteLine("  <a href=\"" + RequestSpecificValues.Current_Mode.Base_URL + "l/" + State.Item.BibID + "/" + State.Item.VID + "\">View this item</a><br /><br />");

                RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Edit_Item_Metadata;
                RequestSpecificValues.Current_Mode.My_Sobek_SubMode = "1";
                RequestSpecificValues.Current_Mode.BibID = State.Item.BibID;
                RequestSpecificValues.Current_Mode.VID = State.Item.VID;
                Output.WriteLine("  <a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">Edit this item</a><br /><br />");

                RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.New_Item;
                RequestSpecificValues.Current_Mode.My_Sobek_SubMode = String.Empty;
                RequestSpecificValues.Current_Mode.BibID = String.Empty;
                RequestSpecificValues.Current_Mode.VID = String.Empty;
                Output.WriteLine("  <a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">Add another item</a><br /><br />");
            }

            RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Folder_Management;
            RequestSpecificValues.Current_Mode.Result_Display_Type = "brief";
            RequestSpecificValues.Current_Mode.My_Sobek_SubMode = "Submitted Items";
            Output.WriteLine("  <a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">View all my submitted items</a><br /><br />");

            Output.WriteLine("</blockquote>");
            Output.WriteLine("</div>");
        }

        /// <summary> Handles a postback from this step -- never actually invoked, since the orchestrator
        /// renders no Back/Continue footer once this terminal step is reached; kept only so the fixed
        /// step dispatch switches don't need a special case </summary>
        public bool Handle_Postback(IFormCollection Form, Submission_State State, RequestCache RequestSpecificValues, Custom_Tracer Tracer)
        {
            return false;
        }
    }
}
