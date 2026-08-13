using System;
using System.Collections.Generic;

namespace SobekCM.Resource_Object.Configuration
{
    /// <summary> Settings necessary for the digital resource library to operate, such as
    /// all the metadata configuration (or the default) </summary>
    public static class ResourceObjectSettings
    {
        private static Dictionary<string, string> assemblyDictionary;


        /// <summary> Configuration information regarding how to read and write metadata files in the system </summary>
        public static Metadata_Configuration MetadataConfig { get; set; }

        /// <summary> File name (no path) of the cached, pre-built metadata protobuf file kept alongside a
        /// digital resource's other files, directly in its resource folder </summary>
        public const string Metadata_Cache_FileName = "cache.protobuf";

        /// <summary> Full file names (case-insensitive) which should never be attached to a digital resource's
        /// METS package, or shown in the online file management/upload lists -- internally-generated cache
        /// files, primarily </summary>
        public static readonly HashSet<string> Files_To_Exclude_From_Package = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Metadata_Cache_FileName,
            "agreement.txt",
            "doc.xml",
            "marc.xml",
            "TEMP000001_00001.mets",
            "ufdc_mets.xml",
            "sobek_mets.xml",
            "citation_mets.xml"
        };

        /// <summary> File name fragments (case-insensitive) which, if found anywhere within a file name, mean
        /// that file is a standard SobekCM working file (a generated METS, MARC, or agreement file, etc.) and
        /// should never be attached to a digital resource's METS package, or shown in the online file
        /// management/upload lists. </summary>
        /// <remarks> This consolidates a set of exclusion rules which used to be duplicated -- and had drifted
        /// slightly out of sync with each other -- across the builder module and web viewers that build or
        /// display a digital resource's file list.  "Contains anywhere" matching is used throughout (rather than
        /// "starts with" or "equals", which some of the original duplicated checks used) since it is a superset
        /// of both, so folding the old checks in here could only exclude the same files as before, or more --
        /// never fewer. </remarks>
        public static readonly HashSet<string> Filename_Fragments_To_Exclude_From_Package = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "_ingest.xml",
            ".mets"
        };

        /// <summary> Constructor for a new instance of the ResourceObjectSettings class </summary>
        static ResourceObjectSettings()
        {
            MetadataConfig = new Metadata_Configuration();
        }

        /// <summary> Determines if a file should always be excluded from a digital resource's METS package
        /// and from the online file management/upload lists, such as internally-generated cache files and
        /// standard SobekCM working files (generated METS, MARC, or agreement files, etc..) </summary>
        /// <param name="FileName"> File name only (with extension), not the full path </param>
        /// <returns> TRUE if this file should always be excluded </returns>
        public static bool Is_File_Excluded_From_Package(string FileName)
        {
            if (String.IsNullOrEmpty(FileName))
                return false;

            if (Files_To_Exclude_From_Package.Contains(FileName))
                return true;

            foreach (string fragment in Filename_Fragments_To_Exclude_From_Package)
            {
                if (FileName.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        /// <summary> Add an external assembly, likely from an extension or plug-in </summary>
        /// <param name="AssemblyCode"> Code for this assembly </param>
        /// <param name="AssemblyFilePath"> File path for the DLL assembly to be referenced </param>
        public static void Add_Assembly(string AssemblyCode, string AssemblyFilePath)
        {
            if (assemblyDictionary == null)
                assemblyDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            assemblyDictionary[AssemblyCode] = AssemblyFilePath;
        }

        /// <summary> Gets the absolute path and filename for an assembly included in one of the 
        /// extensions, by extension ID </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        public static string Get_Assembly(string ID)
        {
            // Now look and return the assembly if the ID exists
            if ((assemblyDictionary != null) && (assemblyDictionary.ContainsKey(ID)))
                return assemblyDictionary[ID];

            return null;
        }

        /// <summary> Clears the dictionary of assemblies </summary>
        public static void Clear_Assemblies()
        {
            if (assemblyDictionary != null)
                assemblyDictionary.Clear();
        }
    }
}
