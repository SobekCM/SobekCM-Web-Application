using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using ProtoBuf;

namespace SobekCM.Core.UI_Configuration.Viewers
{
    /// <summary> Configuration information for the special item results HTML writer, including
    /// the result viewers configuration </summary>
    [Serializable, DataContract, ProtoContract]
    [XmlRoot("ResultsWriterConfig")]
    public class ResultsWriterConfig
    {
        private Dictionary<string, ResultsSubViewerConfig> viewersByCode;
        private Dictionary<string, ResultsSubViewerConfig> viewersByType;

        /// <summary> Fully qualified (including namespace) name of the main class used
        /// as the results HTML writer </summary>
        /// <remarks> By default, this would be 'SobekCM.Library.HTML.Search_Results_HtmlSubwriter' </remarks>
        [DataMember(Name = "class")]
        [XmlAttribute("class")]
        [ProtoMember(1)]
        public string Class { get; set; }

        /// <summary> Name of the assembly within which this class resides, unless this
        /// is one of the default subviewers included in the core code </summary>
        /// <remarks> By default, this would be blank </remarks>
        [DataMember(Name = "assembly", EmitDefaultValue = false)]
        [XmlAttribute("assembly")]
        [ProtoMember(2)]
        public string Assembly { get; set; }

        /// <summary> Collection of results viewers mapped to viewer codes </summary>
        [DataMember(Name = "viewers")]
        [XmlArray("viewers")]
        [XmlArrayItem("viewer", typeof(ResultsSubViewerConfig))]
        [ProtoMember(3)]
        public List<ResultsSubViewerConfig> Viewers { get; set; }

        /// <summary> Special results viewer used when there are no results </summary>
        [DataMember(Name = "noResultsViewer", EmitDefaultValue = false)]
        [XmlAttribute("noResultsViewer")]
        [ProtoMember(4)]
        public ResultsSubViewerConfig NoResultsViewer { get; set; }
        

        /// <summary> Constructor for a new instance of the <see cref="ResultsWriterConfig"/> class </summary>
        public ResultsWriterConfig()
        {
            Class = "SobekCM.Library.HTML.Search_Results_HtmlSubwriter";
            Viewers = new List<ResultsSubViewerConfig>();
            
            set_default();
        }

        /// <summary> Get the configuration about the results viewer, by viewer code (usually from the URL) </summary>
        /// <param name="Code"> Viewer code </param>
        /// <returns> Matching results configuration, or NULL </returns>
        public ResultsSubViewerConfig GetViewerByCode(string Code)
        {
            // Ensure the dictionaries are built
            if (viewersByCode == null) viewersByCode = new Dictionary<string, ResultsSubViewerConfig>(StringComparer.InvariantCultureIgnoreCase);
            if (viewersByType == null) viewersByType = new Dictionary<string, ResultsSubViewerConfig>(StringComparer.InvariantCultureIgnoreCase);

            // Check for the count of items in the dictionaries
            if (viewersByCode.Count != Viewers.Count)
            {
                viewersByCode.Clear();
                foreach (ResultsSubViewerConfig existingConfig in Viewers)
                    viewersByCode[existingConfig.ViewerCode] = existingConfig;
            }
            if (viewersByType.Count != Viewers.Count)
            {
                viewersByType.Clear();
                foreach (ResultsSubViewerConfig existingConfig in Viewers)
                    viewersByType[existingConfig.ViewerType] = existingConfig;
            }

            return viewersByCode.ContainsKey(Code) ? viewersByCode[Code] : null;
        }


        /// <summary> Get the configuration about the results viewer, by viewer type, which matches back to the database </summary>
        /// <param name="Type"> Viewer type </param>
        /// <returns> Matching results configuration, or NULL </returns>
        public ResultsSubViewerConfig GetViewerByType(string Type)
        {
            // Ensure the dictionaries are built
            if (viewersByCode == null) viewersByCode = new Dictionary<string, ResultsSubViewerConfig>(StringComparer.InvariantCultureIgnoreCase);
            if (viewersByType == null) viewersByType = new Dictionary<string, ResultsSubViewerConfig>(StringComparer.InvariantCultureIgnoreCase);

            // Check for the count of items in the dictionaries
            if (viewersByCode.Count != Viewers.Count)
            {
                viewersByCode.Clear();
                foreach (ResultsSubViewerConfig existingConfig in Viewers)
                    viewersByCode[existingConfig.ViewerCode] = existingConfig;
            }
            if (viewersByType.Count != Viewers.Count)
            {
                viewersByType.Clear();
                foreach (ResultsSubViewerConfig existingConfig in Viewers)
                    viewersByType[existingConfig.ViewerType] = existingConfig;
            }

            return viewersByType.ContainsKey(Type) ? viewersByType[Type] : null;
        }

        /// <summary> Clears all the previously loaded information, such as the default values </summary>
        /// <remarks> This clears all the item viewer information, clears the assembly, and sets the class to the
        /// default item html subwriter class. </remarks>
        public void ClearAll()
        {
            Viewers.Clear();
            if (viewersByCode != null) viewersByCode.Clear();
            if (viewersByType != null) viewersByType.Clear();
            Assembly = String.Empty;
            Class = "SobekCM.Library.HTML.Search_Results_HtmlSubwriter";
        }


        /// <summary> Add a new result viewer for the writer to use </summary>
        /// <param name="NewViewer"> New viewer to add </param>
        /// <remarks> If a viewer config already exists for the viewer type or viewer code, this 
        /// will replace the existing one </remarks>
        public void Add_Viewer(ResultsSubViewerConfig NewViewer)
        {
            // Ensure the dictionaries are built
            if (viewersByCode == null) viewersByCode = new Dictionary<string, ResultsSubViewerConfig>(StringComparer.InvariantCultureIgnoreCase);
            if (viewersByType == null) viewersByType = new Dictionary<string, ResultsSubViewerConfig>(StringComparer.InvariantCultureIgnoreCase);

            // Check for the count of items in the dictionaries
            if (viewersByCode.Count != Viewers.Count)
            {
                viewersByCode.Clear();
                foreach (ResultsSubViewerConfig existingConfig in Viewers)
                    viewersByCode[existingConfig.ViewerCode] = existingConfig;
            }
            if (viewersByType.Count != Viewers.Count)
            {
                viewersByType.Clear();
                foreach (ResultsSubViewerConfig existingConfig in Viewers)
                    viewersByType[existingConfig.ViewerType] = existingConfig;
            }

            // Look for a match by code - remove any existing matches
            if (viewersByCode.ContainsKey(NewViewer.ViewerCode))
            {
                if (Viewers.Contains(viewersByCode[NewViewer.ViewerCode]))
                    Viewers.Remove(viewersByCode[NewViewer.ViewerCode]);
            }

            // Look for a match by type - remove any existing matches
            if (viewersByType.ContainsKey(NewViewer.ViewerType))
            {
                if (Viewers.Contains(viewersByType[NewViewer.ViewerType]))
                    Viewers.Remove(viewersByType[NewViewer.ViewerType]);
            }

            // Now, add the new viewer
            viewersByCode[NewViewer.ViewerCode] = NewViewer;
            viewersByType[NewViewer.ViewerType] = NewViewer;
            Viewers.Add(NewViewer);

        }


        private void set_default()
        {
            Assembly = null;
            Class = "SobekCM.Library.HTML.Search_Results_HtmlSubwriter";
            Viewers.Clear();

           
            // Add all the standard viewers here!!
        }
    }
}
