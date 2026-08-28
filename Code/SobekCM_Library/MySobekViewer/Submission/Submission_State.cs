#region Using directives

using SobekCM.Resource_Object;
using System;
using System.Collections.Generic;

#endregion

namespace SobekCM.Library.MySobekViewer.Submission
{
    /// <summary> Everything carried from step to step across one in-progress submission </summary>
    /// <remarks> Held in <see cref="SobekCM.Core.MemoryMgmt.SessionObjectStore"/> (a complex workflow
    /// object, per this codebase's session-storage split -- not <c>ISession</c>), keyed per-session by
    /// <see cref="New_Submission_MySobekViewer"/>. Replaces the ad-hoc mix of session keys and private
    /// fields the old monolithic <c>New_Group_And_Item_MySobekViewer</c> used for the same purpose. </remarks>
    [Serializable]
    public class Submission_State
    {
        /// <summary> Which step is currently being shown </summary>
        public Submission_Step_Enum CurrentStep { get; set; }

        /// <summary> Primary key of the Item Type chosen in <see cref="Submission_Step_Enum.TypeSelection"/> </summary>
        public int ItemTypeID { get; set; }

        /// <summary> Display name of the chosen Item Type, cached here so later steps (and the stepper
        /// bar) don't need to re-query it </summary>
        public string ItemTypeName { get; set; }

        /// <summary> Whether the chosen Type shows the Series Finder step (mirrors
        /// <c>SobekCM_Item_Type.ShowSeriesFinder</c>) </summary>
        public bool ShowSeriesFinder { get; set; }

        /// <summary> Code identifying which <see cref="iUploadSubmissionStep"/> implementation the
        /// chosen Type uses, resolved through <see cref="Upload_Step_Factory"/> </summary>
        public string UploadCode { get; set; }

        /// <summary> The chosen Type's BibID prefix, used to seed a brand-new BibID when this
        /// submission is a new title rather than attached to an existing one (mirrors
        /// <c>SobekCM_Item_Type.BibIDRoot</c>) </summary>
        public string BibIDRoot { get; set; }

        /// <summary> The chosen Type's legacy MARC/MODS resource type value, stamped into
        /// <c>SobekCM_Item_Group.Type</c> for a brand-new title, and also used by
        /// <see cref="Submission_Step_Enum.SeriesFinder"/> to find existing groups never re-typed onto
        /// the modern <c>ItemTypeID</c> column </summary>
        public string MarcTypeOfResource { get; set; }

        /// <summary> BibID of the existing title this submission attaches to, or NULL for a brand-new
        /// title. Only meaningful when <see cref="ShowSeriesFinder"/> is true -- for every other Type
        /// this submission is always a new title </summary>
        public string AttachToExistingBibID { get; set; }

        /// <summary> The Series Finder step's last-submitted search text, kept here (not on the step
        /// instance) so it survives back to the fresh instance <see cref="New_Submission_MySobekViewer"/>
        /// creates for rendering, the same reason <see cref="ValidationMessage"/> lives here </summary>
        public string SeriesFinderSearchText { get; set; }

        /// <summary> Primary key of the permissions agreement this user must accept, or NULL if none is
        /// required </summary>
        public int? PermissionsAgreementID { get; set; }

        /// <summary> Whether the required agreement has already been accepted this submission </summary>
        public bool PermissionsAgreementAccepted { get; set; }

        /// <summary> The item under construction. Starts empty at <see cref="Submission_Step_Enum.TypeSelection"/>
        /// and accumulates behaviors/metadata as each later step runs its own <c>Handle_Postback</c> </summary>
        public SobekCM_Item Item { get; set; }

        /// <summary> Names of files uploaded so far this submission, in whatever form the active
        /// <see cref="iUploadSubmissionStep"/> chose to record them (a fixed-slot upload step and a
        /// generic multi-file one will populate this differently) </summary>
        public List<string> UploadedFileNames { get; private set; }

        /// <summary> Validation message from the current step's last <c>Handle_Postback</c> call, shown
        /// on redisplay if that step reported it was not complete </summary>
        /// <remarks> Lives here, not on the step instance itself -- <see cref="New_Submission_MySobekViewer"/>
        /// constructs a fresh step instance for <c>Handle_Postback</c> and another for <c>Render_HTML</c>
        /// within the same request, so an instance field would never survive to be rendered. Cleared by
        /// the orchestrator at the start of every postback, before the current step runs. </remarks>
        public string ValidationMessage { get; set; }

        /// <summary> Set once <c>perform_final_submission</c> has run (successfully or not), so a
        /// refresh/re-postback of <see cref="Submission_Step_Enum.Congratulations"/> never saves the
        /// item twice </summary>
        public bool SubmissionAttempted { get; set; }

        /// <summary> User-facing reason the final save failed, or NULL if it succeeded (or hasn't run
        /// yet). Deliberately generic -- the real exception detail goes to the system error email, not
        /// the browser, matching the old viewer's approach </summary>
        public string SubmissionErrorMessage { get; set; }

        /// <summary> Constructor for a new instance of the Submission_State class </summary>
        public Submission_State()
        {
            CurrentStep = Submission_Step_Enum.Permissions;
            UploadedFileNames = new List<string>();
        }
    }
}
