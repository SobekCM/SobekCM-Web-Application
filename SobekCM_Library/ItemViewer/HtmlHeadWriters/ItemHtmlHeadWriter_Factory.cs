using System;
using System.Collections.Generic;
using System.Reflection;
using SobekCM.Engine_Library.ApplicationState;

namespace SobekCM.Library.ItemViewer.HtmlHeadWriters
{
    /// <summary> Static class is used as both a factory for new item html head writers, as 
    /// well as a cache to prevent new objects having to be created </summary>
    public static class ItemHtmlHeadWriter_Factory
    {
        private static readonly Dictionary<string, iItemHtmlHeadWriter> headWriters;
        private static readonly Object lockObj = new Object();


        /// <summary> Static constructor/initializer for the ItemHtmlHeadWriter_Factory class </summary>
        static ItemHtmlHeadWriter_Factory()
        {
            headWriters = new Dictionary<string, iItemHtmlHeadWriter>();
        }

        /// <summary> Get the item html head writer indicated by the assembly and class </summary>
        /// <param name="Assembly"> Name of the assembly within which this class resides, unless this
        /// is one of the default class/assembly included in the core code </param>
        /// <param name="Class"> Fully qualified (including namespace) name of the class used 
        /// for this class/assembly information </param>
        /// <returns> Fully built item html head writer object, or NULL </returns>
        public static iItemHtmlHeadWriter Get_ItemHtmlHeadWriter(string Assembly, string Class)
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
                if (headWriters.ContainsKey(key))
                    return headWriters[key];

                // Try to create this then
                iItemHtmlHeadWriter returnObj = createWriter(Assembly, Class);
                
                // Save in the dictionary
                headWriters[key] = returnObj;

                // Return it
                return returnObj;
            }
        }

        private static iItemHtmlHeadWriter createWriter(string assembly, string className)
        {

            // If this is just a standard object, try to create it here
            if (String.IsNullOrEmpty(assembly))
            {
                switch (className)
                {
                    case "SobekCM.Library.ItemViewer.HtmlHeadWriters.DublinCore_ItemHtmlHeadWriter":
                        return new DublinCore_ItemHtmlHeadWriter();

                    case "SobekCM.Library.ItemViewer.HtmlHeadWriters.MODS_ItemHtmlHeadWriter":
                        return new MODS_ItemHtmlHeadWriter();

                    case "SobekCM.Library.ItemViewer.HtmlHeadWriters.OpenGraph_ItemHtmlHeadWriter":
                        return new OpenGraph_ItemHtmlHeadWriter();
                }

                // If it made it here, there is no assembly, but it is an unexpected type.  
                // Just create it from the same assembly then
                try
                {
                    Assembly dllAssembly = System.Reflection.Assembly.GetCallingAssembly();
                    Type prototyperType = dllAssembly.GetType(className);
                    iItemHtmlHeadWriter returnObj = (iItemHtmlHeadWriter)Activator.CreateInstance(prototyperType);
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
                iItemHtmlHeadWriter returnObj = (iItemHtmlHeadWriter)Activator.CreateInstance(prototyperType);
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
