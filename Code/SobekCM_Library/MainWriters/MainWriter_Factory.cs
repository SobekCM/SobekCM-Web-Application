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
    /// <remarks> <see cref="Html_Echo_MainWriter"/> is a special case, checked before the switch below rather
    /// than dispatched through <see cref="Writer_Codes.HTML_Echo"/>: whether to use it depends on
    /// <c>Current_Mode.Is_Robot</c>/<c>Info_Browse_Mode</c>/<c>Aggregation_Type</c>, not a pre-set
    /// <c>Writer_Type</c>, since that decision requires <c>SobekCM_Assistant</c> (in this same layer) and
    /// can't be made earlier in <c>QueryString_Analyzer</c> (a lower layer). </remarks>
    public static class MainWriter_Factory
    {
        private static Dictionary<string, Type> pluginMainWriters;
        private static readonly object pluginMainWritersLock = new object();

        /// <summary> Returns the appropriate main writer, based on the current request's Writer_Type </summary>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        /// <param name="Context"> Context for this individual HTTP request </param>
        /// <returns> Built main writer </returns>
        public static abstractMainWriter Get_MainWriter(RequestCache RequestSpecificValues, HttpContext Context)
        {
            Navigation_Object currentMode = RequestSpecificValues.Current_Mode;

            // A robot hitting a full "browse all" aggregation page gets a pre-rendered static snapshot
            // instead of the normal Html_MainWriter rendering pipeline. Detected here (not in
            // QueryString_Analyzer) because building the snapshot path needs SobekCM_Assistant, which
            // lives in SobekCM_Library - a layer QueryString_Analyzer (SobekCM_Engine_Library) can't
            // reach; Is_Robot/Aggregation_Type/Info_Browse_Mode are already fully resolved by the time
            // this factory runs, regardless of which of QueryString_Analyzer's several internal code
            // paths set them.
            if ((currentMode.Is_Robot) && (currentMode.Info_Browse_Mode == "all") &&
                ((currentMode.Aggregation_Type == Aggregation_Type_Enum.Browse_Info) || (currentMode.Aggregation_Type == Aggregation_Type_Enum.Child_Page_Edit)))
            {
                var assistant = new SobekCM_Assistant();
                string echoFilePath = assistant.Get_All_Browse_Static_HTML(currentMode, RequestSpecificValues.Tracer);
                return new Html_Echo_MainWriter(Context, RequestSpecificValues, echoFilePath);
            }

            string writer_type = currentMode.Writer_Type;

            switch (writer_type)
            {
                case Writer_Codes.HTML:
                case Writer_Codes.HTML_LoggedIn:
                    return new Html_MainWriter(Context, RequestSpecificValues);
            }

            // Not a core writer - check the plugin-registered main writers
            if (!String.IsNullOrEmpty(writer_type))
            {
                Dictionary<string, Type> pluginWriters = configurePluginMainWriters();
                if (pluginWriters.TryGetValue(writer_type, out Type pluginWriterType))
                {
                    abstractMainWriter pluginWriter = create_plugin_main_writer(pluginWriterType, RequestSpecificValues, Context);
                    if (pluginWriter != null)
                        return pluginWriter;
                }
            }

            // Unrecognized writer code - fall back to HTML
            return new Html_MainWriter(Context, RequestSpecificValues);
        }

        /// <summary> Instantiates an already-resolved plugin main writer class via reflection </summary>
        /// <returns> The built main writer, or NULL if its constructor failed </returns>
        private static abstractMainWriter create_plugin_main_writer(Type WriterType, RequestCache RequestSpecificValues, HttpContext Context)
        {
            try
            {
                return (abstractMainWriter)Activator.CreateInstance(WriterType, Context, RequestSpecificValues);
            }
            catch (Exception)
            {
                // Not sure exactly what to do here, honestly ( matches ItemViewer_Factory.configurePrototyper's
                // own handling of a plugin class/assembly that fails to resolve )
                return null;
            }
        }

        /// <summary> Resolves a plugin-registered main writer's class, loading its assembly first if one was specified </summary>
        /// <returns> The writer class, or NULL if the assembly or class can't be resolved or isn't a main writer </returns>
        private static Type resolve_plugin_main_writer_type(ExtensionMainWriterInfo WriterInfo)
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
                return ((writerType != null) && (!writerType.IsAbstract) && (typeof(abstractMainWriter).IsAssignableFrom(writerType))) ? writerType : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary> Builds the lookup of every plugin-registered main writer code, from every currently
        /// enabled extension's <see cref="ExtensionInfo.MainWriters"/> list </summary>
        /// <remarks> Each writer's class is resolved here, once, rather than on every request. A registration whose
        /// assembly or class can't be resolved is left out, so the lookup only holds writers that can actually be
        /// built -- see <see cref="Has_Plugin_Writer"/>. </remarks>
        private static Dictionary<string, Type> configurePluginMainWriters()
        {
            Dictionary<string, Type> lookup = pluginMainWriters;
            if (lookup != null)
                return lookup;

            lock (pluginMainWritersLock)
            {
                // Another thread may have already finished building this while this thread waited for the lock
                lookup = pluginMainWriters;
                if (lookup != null)
                    return lookup;

                var newLookup = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
                Extension_Configuration extensions = UI_ApplicationCache_Gateway.Configuration.Extensions;
                if ((extensions != null) && (extensions.Extensions != null))
                {
                    foreach (ExtensionInfo extension in extensions.Extensions)
                    {
                        if ((!extension.Enabled) || (extension.MainWriters == null))
                            continue;

                        foreach (ExtensionMainWriterInfo mainWriter in extension.MainWriters)
                        {
                            if (String.IsNullOrEmpty(mainWriter.Code))
                                continue;

                            // The last registration for a code wins, as before -- including when it fails to resolve,
                            // so a broken later registration doesn't quietly fall back to an earlier one
                            Type writerType = resolve_plugin_main_writer_type(mainWriter);
                            if (writerType != null)
                                newLookup[mainWriter.Code] = writerType;
                            else
                                newLookup.Remove(mainWriter.Code);
                        }
                    }
                }

                pluginMainWriters = newLookup;
                return newLookup;
            }
        }

        /// <summary> Whether an enabled extension has registered a main writer for this writer code, and its class resolved </summary>
        /// <param name="WriterCode"> Writer code to look up (case-insensitive) </param>
        /// <returns> TRUE if a resolvable plugin main writer is registered for the code, otherwise FALSE </returns>
        /// <remarks> Answers from the same resolved lookup <see cref="Get_MainWriter"/> uses, so a FALSE here means
        /// that code renders through <see cref="Html_MainWriter"/> -- which matters to anything deciding whether a
        /// request produces a normal HTML page, like LoginOnlyModeInitializer. The one case this can't foresee is a
        /// writer whose constructor throws on a particular request; that still falls back to HTML. </remarks>
        public static bool Has_Plugin_Writer(string WriterCode)
        {
            return (!String.IsNullOrEmpty(WriterCode)) && configurePluginMainWriters().ContainsKey(WriterCode);
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
