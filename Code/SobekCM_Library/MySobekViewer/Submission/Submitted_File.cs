#region Using directives

using System;

#endregion

namespace SobekCM.Library.MySobekViewer.Submission
{
    /// <summary> One file staged for the current submission, paired with whatever role the active
    /// upload step assigned it </summary>
    /// <remarks> Role is a plain string, not a shared enum -- each <see cref="iUploadSubmissionStep"/>
    /// defines its own small vocabulary (Oral History's eventual Transcript/Audio Recording/Video
    /// Recording/Supporting Materials slots; TEI's "tei" for the detected TEI document) rather than
    /// forcing one shared set of roles across upload shapes that don't need them -- <c>Generic_Upload_SubmissionStep</c>
    /// leaves every file's Role blank, since it never distinguishes between staged files at all. </remarks>
    [Serializable]
    public class Submitted_File
    {
        /// <summary> Name of the file, relative to the submission's staging directory </summary>
        public string FileName { get; set; }

        /// <summary> Role this file plays in the submission, in whatever vocabulary the active upload
        /// step defined -- blank if that step doesn't distinguish roles </summary>
        public string Role { get; set; }

        /// <summary> Constructor for a new instance of the Submitted_File class </summary>
        public Submitted_File()
        {
        }

        /// <summary> Constructor for a new instance of the Submitted_File class </summary>
        public Submitted_File(string FileName, string Role)
        {
            this.FileName = FileName;
            this.Role = Role;
        }
    }
}
