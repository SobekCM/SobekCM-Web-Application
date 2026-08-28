#region Using directives

using SobekCM.Library.MySobekViewer.Submission.UploadSteps;
using System;

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
