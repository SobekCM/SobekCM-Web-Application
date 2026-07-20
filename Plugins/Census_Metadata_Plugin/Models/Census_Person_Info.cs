using System;

namespace Census_Metadata_Plugin.Models
{
    /// <summary> Information about a single person from a page in the 
    /// census or city directory </summary>
    [Serializable]
    public class Census_Person_Info
    {
        /// <summary> First name for this person from the census </summary>
        public string FirstName { get; set; }

        /// <summary> Last name for this person from the census </summary>
        public string LastName { get; set; }

        /// <summary> Gender for this person from the census </summary>
        public string Gender { get; set; }

        /// <summary> Nativity for this person from the census </summary>
        public string Nativity { get; set; }

        /// <summary> Occupation for this person from the census </summary>
        public string Occupation { get; set; }

        /// <summary> Information about the residence for this person </summary>
        public string Residence { get; set; }

        /// <summary> Information about the business for this person </summary>
        public string Business { get; set; }

        /// <summary> Age, in years </summary>
        public string AgeYears { get; set; }

        /// <summary> Age, in months  </summary>
        public string AgeMonth { get; set; }

        /// <summary> Type of source ( i.e., 'seal', 'letter', etc.. ) </summary>
        public string SourceType { get; set; }

        /// <summary> Name of the source ( i.e., the census or city directory ) </summary>
        public string Source { get; set; }

        /// <summary> Line number for this person from the source </summary>
        public int LineNumber { get; set; }

        /// <summary> Page number on which this person appears from the source </summary>
        public int PageNumner { get; set; }

        /// <summary> Link to this source </summary>
        public string Link { get; set; }
    }
}
