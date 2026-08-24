#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.Configuration.Extensions;
using SobekCM.Core.Navigation;
using SobekCM.Library.UI;
using System;
using System.Collections.Generic;
using System.Reflection;

#endregion

namespace SobekCM.Library.MainWriters
{
    /// <summary> Factory class returns the appropriate main writer </summary>
    /// <remarks> Core writers are returned directly by the switch below, with zero reflection - this mirrors
    /// <c>AdminViewer_Factory</c>'s (and, in turn, <c>ItemViewer_Factory.configurePrototyper</c>'s) "known
    /// classes get a hardcoded fast path" design. A code not recognized there is looked up in the
    /// plugin-registered main writer registry (built from every enabled extension's
    /// <see cref="ExtensionInfo.MainWriters"/>, e.g. an extension's
    /// <c>&lt;mainWriter code="" class="" assembly=""/&gt;</c> config element) and, if found, loaded via
    /// reflection - optionally from a plugin assembly resolved through
    /// <see cref="Extension_Configuration.Get_Assembly"/>. A still-unrecognized code falls back to
    /// <see cref="Html_MainWriter"/>. </remarks>
    /// <remarks> Deliberately does NOT handle <see cref="Writer_Codes.HTML_Echo"/> - that writer is triggered
    /// mid-render from <c>Aggregation_HtmlSubwriter</c>, not from the upfront writer-selection this factory
    /// performs, and its full trigger mechanism isn't yet understood (see the MainWriters-as-plugins plan).
    /// <c>QueryInitializer</c> still special-cases it directly, outside this factory. </remarks>
    public static class MainWriter_Factory
    {
        private static Dictionary<string, ExtensionMainWriterInfo> pluginMainWriters;
        private static readonly object pluginMainWritersLock = new object();

        /// <summary> Returns the appropriate main writer, based on the current request's Writer_Type </summary>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        /// <param name="Context"> Context for this individual HTTP request </param>
        /// <returns> Built main writer </returns>
        public static abstractMainWriter Get_MainWriter(RequestCache RequestSpecificValues, HttpContext Context)
        {
            string writer_type = RequestSpecificValues.Current_Mode.Writer_Type;

            switch (writer_type)
            {
                case Writer_Codes.HTML:
                case Writer_Codes.HTML_LoggedIn:
                    return new Html_MainWriter(Context, RequestSpecificValues);
            }

            // Not a core writer - check the plugin-registered main writers
            if (!String.IsNullOrEmpty(writer_type))
            {
                Dictionary<string, ExtensionMainWriterInfo> pluginWriters = configurePluginMainWriters();
                if (pluginWriters.TryGetValue(writer_type, out ExtensionMainWriterInfo pluginWriterInfo))
                {
                    abstractMainWriter pluginWriter = create_plugin_main_writer(pluginWriterInfo, RequestSpecificValues, Context);
                    if (pluginWriter != null)
                        return pluginWriter;
                }
            }

            // Unrecognized writer code - fall back to HTML
            return new Html_MainWriter(Context, RequestSpecificValues);
        }

        /// <summary> Instantiates a plugin-registered main writer via reflection, loading its assembly
        /// first if one was specified </summary>
        /// <returns> The built main writer, or NULL if the class/assembly could not be resolved </returns>
        private static abstractMainWriter create_plugin_main_writer(ExtensionMainWriterInfo WriterInfo, RequestCache RequestSpecificValues, HttpContext Context)
        {
            try
            {
                Assembly dllAssembly;
                if (String.IsNullOrEmpty(WriterInfo.Assembly))
                {
                    dllAssembly = Assembly.GetExecutingAssembly();
                }
                else
                {
                    string assemblyFilePath = UI_ApplicationCache_Gateway.Configuration.Extensions.Get_Assembly(WriterInfo.Assembly);
                    dllAssembly = (assemblyFilePath != null) ? Assembly.LoadFrom(assemblyFilePath) : null;
                }

                Type writerType = dllAssembly?.GetType(WriterInfo.Class);
                return (writerType != null) ? (abstractMainWriter)Activator.CreateInstance(writerType, Context, RequestSpecificValues) : null;
            }
            catch (Exception)
            {
                // Not sure exactly what to do here, honestly ( matches ItemViewer_Factory.configurePrototyper's
                // own handling of a plugin class/assembly that fails to resolve )
                return null;
            }
        }

        /// <summary> Builds the lookup of every plugin-registered main writer code, from every currently
        /// enabled extension's <see cref="ExtensionInfo.MainWriters"/> list </summary>
        private static Dictionary<string, ExtensionMainWriterInfo> configurePluginMainWriters()
        {
            Dictionary<string, ExtensionMainWriterInfo> lookup = pluginMainWriters;
            if (lookup != null)
                return lookup;

            lock (pluginMainWritersLock)
            {
                // Another thread may have already finished building this while this thread waited for the lock
                lookup = pluginMainWriters;
                if (lookup != null)
                    return lookup;

                var newLookup = new Dictionary<string, ExtensionMainWriterInfo>(StringComparer.OrdinalIgnoreCase);
                Extension_Configuration extensions = UI_ApplicationCache_Gateway.Configuration.Extensions;
                if ((extensions != null) && (extensions.Extensions != null))
                {
                    foreach (ExtensionInfo extension in extensions.Extensions)
                    {
                        if ((!extension.Enabled) || (extension.MainWriters == null))
                            continue;

                        foreach (ExtensionMainWriterInfo mainWriter in extension.MainWriters)
                        {
                            if (!String.IsNullOrEmpty(mainWriter.Code))
                                newLookup[mainWriter.Code] = mainWriter;
                        }
                    }
                }

                pluginMainWriters = newLookup;
                return newLookup;
            }
        }

        /// <summary> Clears the cached plugin main writer lookup, used when the cache is reset either
        /// manually or automatically ( mirrors <c>AdminViewer_Factory.Clear</c>/<c>ItemViewer_Factory.Clear</c> ) </summary>
        public static void Clear()
        {
            lock (pluginMainWritersLock)
            {
                pluginMainWriters = null;
            }
        }
    }
}
