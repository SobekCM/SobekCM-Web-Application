using System;
using System.Collections.Generic;
using System.Reflection;
using SobekCM.Engine_Library.ApplicationState;

namespace SobekCM.Library.ItemViewer.HtmlSectionWriters
{
    /// <summary> Static class is used as both a factory for new item section writers, as 
    /// well as a cache to prevent new objects having to be created </summary>
    public static class ItemSectionWriter_Factory
    {
        private static readonly Dictionary<string, iItemSectionWriter> sectionWriters;
        private static readonly Object lockObj = new Object();

        /// <summary> Static constructor/initializer for the ItemSectionWriter_Factory class </summary>
        static ItemSectionWriter_Factory()
        {
            sectionWriters = new Dictionary<string, iItemSectionWriter>();
        }

        /// <summary> Get the item section writer indicated by the assembly and class </summary>
        /// <param name="Assembly"> Name of the assembly within which this class resides, unless this
        /// is one of the default class/assembly included in the core code </param>
        /// <param name="Class"> Fully qualified (including namespace) name of the class used 
        /// for this class/assembly information </param>
        /// <returns> Fully built item section writer object, or NULL </returns>
        public static iItemSectionWriter Get_ItemSectionWriter(string Assembly, string Class)
        {
            // Must have a class value
            if (String.IsNullOrEmpty(Class))
                return null;

            // Determine the key
            string key = Class;
            if (!String.IsNullOrEmpty(Assembly))
                key = Assembly + ":" + Class;

            // Always lock here, since we may need to create one
            lock (lockObj)
            {
                // If it is contained in the dictionary already, just return it
                if (sectionWriters.ContainsKey(key))
                    return sectionWriters[key];

                // Try to create this then
                iItemSectionWriter returnObj = createWriter(Assembly, Class);
                
                // Save in the dictionary
                sectionWriters[key] = returnObj;

                // Return it
                return returnObj;
            }
        }

        private static iItemSectionWriter createWriter(string assembly, string className)
        {

            // If this is just a standard object, try to create it here
            if (String.IsNullOrEmpty(assembly))
            {
                switch (className)
                {
                    case "SobekCM.Library.ItemViewer.HtmlSectionWriters.TitleBar_ItemSectionWriter":
                        return new TitleBar_ItemSectionWriter();

                    case "SobekCM.Library.ItemViewer.HtmlSectionWriters.StandardMenu_ItemSectionWriter":
                        return new StandardMenu_ItemSectionWriter();

                    case "SobekCM.Library.ItemViewer.HtmlSectionWriters.ViewerNav_ItemSectionWriter":
                        return new ViewerNav_ItemSectionWriter();

                    case "SobekCM.Library.ItemViewer.HtmlSectionWriters.TOC_ItemSectionWriter":
                        return new TOC_ItemSectionWriter();

                    case "SobekCM.Library.ItemViewer.HtmlSectionWriters.Wordmarks_ItemSectionWriter":
                        return new Wordmarks_ItemSectionWriter();
                }

                // If it made it here, there is no assembly, but it is an unexpected type.  
                // Just create it from the same assembly then
                try
                {
                    Assembly dllAssembly = Assembly.GetCallingAssembly();
                    Type prototyperType = dllAssembly.GetType(className);
                    iItemSectionWriter returnObj = (iItemSectionWriter)Activator.CreateInstance(prototyperType);
                    return returnObj;
                }
                catch (Exception)
                {
                    // Not sure exactly what to do here, honestly
                    return null;
                }
            }

            // An assembly was indicated
            try
            {
                // Try to find the file/path for this assembly then
                Assembly dllAssembly = null;
                string assemblyFilePath = Engine_ApplicationCache_Gateway.Configuration.Extensions.Get_Assembly(assembly);
                if (assemblyFilePath != null)
                {
                    dllAssembly = Assembly.LoadFrom(assemblyFilePath);
                }

                Type prototyperType = dllAssembly.GetType(className);
                iItemSectionWriter returnObj = (iItemSectionWriter)Activator.CreateInstance(prototyperType);
                return returnObj;
            }
            catch (Exception ee)
            {
                // Not sure exactly what to do here, honestly
                if (ee.Message.Length > 0)
                    return null;
                return null;
            }
        }
    }
}
