#region Using directives

using SobekCM.Library.MySobekViewer.Submission.UploadSteps;
using System;
using System.Collections.Generic;

#endregion

namespace SobekCM.Library.MySobekViewer.Submission
{
    /// <summary> Resolves an Item Type's upload code to the concrete <see cref="iUploadSubmissionStep"/>
    /// that renders its Upload step </summary>
    /// <remarks> Same two-tier lookup shape as <c>AdminViewer_Factory</c>'s plugin registry and
    /// <c>Element_Factory.getElement</c>: a hardcoded switch over built-in upload shapes, with an
    /// assembly-override escape hatch to be added once a real external upload-step plugin exists
    /// (not needed yet -- every upload shape so far ships in this assembly). </remarks>
    public static class Upload_Step_Factory
    {
        /// <summary> Every upload code this factory recognizes, paired with a display name -- the
        /// single source of truth <see cref="Item_Type_Single_AdminViewer"/>'s Upload Code dropdown
        /// reads from, so the admin screen can never drift out of sync with what <see cref="Get_Upload_Step"/>
        /// actually resolves. The blank entry maps to <see cref="Generic_Upload_SubmissionStep"/>. </remarks>
        public static readonly IReadOnlyList<(string Code, string DisplayName)> Known_Upload_Codes = new List<(string, string)>
        {
            (String.Empty, "Generic (default multi-file upload)"),
            ("ORALHISTORY", "Oral History (fixed slots)")
        };

        /// <summary> Gets the upload step implementation for a given upload code </summary>
        /// <param name="UploadCode"> Code from <c>SobekCM_Item_Type</c> identifying which upload shape
        /// this Type uses, or NULL/empty for the default generic shape </param>
        /// <returns> The matching <see cref="iUploadSubmissionStep"/>, or the generic default if the
        /// code is blank or unrecognized </returns>
        public static iUploadSubmissionStep Get_Upload_Step(string UploadCode)
        {
            switch ((UploadCode ?? String.Empty).ToUpper())
            {
                case "ORALHISTORY":
                    return new OralHistory_Upload_SubmissionStep();

                default:
                    return new Generic_Upload_SubmissionStep();
            }
        }
    }
}
