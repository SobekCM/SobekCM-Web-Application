using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace SobekCM.Core.Configuration.Extensions
{
    /// <summary> Collection of all the extension information </summary>
    [Serializable, DataContract, ProtoContract]
    [XmlRoot("ExtensionConfig")]
    public class Extension_Configuration
    {
        private Dictionary<string, string> assemblyDictionary;
        private readonly object assemblyDictionaryLock = new object();

        private Dictionary<string, ExtensionInfo> extensionDictionary;
        private readonly object extensionDictionaryLock = new object();

        /// <summary> Collection of information about each extension </summary>
        [DataMember(Name = "extensions", EmitDefaultValue = false)]
        [XmlArray("extensions")]
        [XmlArrayItem("extension", typeof(ExtensionInfo))]
        [ProtoMember(1)]
        public List<ExtensionInfo> Extensions { get; set; }

        /// <summary> Add a new extension to this list of extensions </summary>
        /// <param name="NewExtension"> New extension to add </param>
        public void Add_Extension(ExtensionInfo NewExtension)
        {
            if (Extensions == null)
                Extensions = new List<ExtensionInfo>();

            Extensions.Add(NewExtension);
        }

        /// <summary> Gets an extension, by extension code, otherwise NULL </summary>
        /// <param name="ExtensionCode"> Unique extension code for the extension information to retrieve </param>
        /// <returns> Extension information, or NULL if no matching extension was found </returns>
        public ExtensionInfo Get_Extension(string ExtensionCode)
        {
            if ((Extensions == null) || (Extensions.Count == 0))
                return null;

            if (String.IsNullOrEmpty(ExtensionCode))
                return null;

            // Same shared, application-wide config instance / cold-start-race shape as Get_Assembly above,
            // so this uses the same double-checked-locking + atomic-publish fix
            Dictionary<string, ExtensionInfo> lookup = extensionDictionary;
            if (lookup == null)
            {
                lock (extensionDictionaryLock)
                {
                    lookup = extensionDictionary;
                    if (lookup == null)
                    {
                        lookup = build_extension_dictionary();
                        extensionDictionary = lookup;
                    }
                }
            }

            return lookup.TryGetValue(ExtensionCode, out ExtensionInfo extension) ? extension : null;
        }

        private Dictionary<string, ExtensionInfo> build_extension_dictionary()
        {
            var newDictionary = new Dictionary<string, ExtensionInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (ExtensionInfo thisExtension in Extensions)
            {
                newDictionary[thisExtension.Code] = thisExtension;
            }

            return newDictionary;
        }

        /// <summary> Gets the absolute path and filename for an assembly included in one of the 
        /// extensions, by extension ID </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        public string Get_Assembly(string ID)
        {
            if ((Extensions == null) || (Extensions.Count == 0))
                return null;

            // This is a shared, application-wide config instance, and this is called during item viewer
            // prototyper construction -- the same cold-start-race shape as ItemViewer_Factory's original
            // bug, so this uses the same double-checked-locking + atomic-publish fix.
            Dictionary<string, string> lookup = assemblyDictionary;
            if (lookup == null)
            {
                lock (assemblyDictionaryLock)
                {
                    lookup = assemblyDictionary;
                    if (lookup == null)
                    {
                        lookup = build_assembly_dictionary();
                        assemblyDictionary = lookup;
                    }
                }
            }

            // Now look and return the assembly if the ID exists
            return lookup.TryGetValue(ID, out string filePath) ? filePath : null;
        }

        private Dictionary<string, string> build_assembly_dictionary()
        {
            var newDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (ExtensionInfo thisExtension in Extensions)
            {
                if (thisExtension.Assemblies != null)
                {
                    foreach (ExtensionAssembly thisAssembly in thisExtension.Assemblies)
                    {
                        newDictionary[thisAssembly.ID] = thisAssembly.FilePath;
                    }
                }
            }

            return newDictionary;
        }
    }
}
