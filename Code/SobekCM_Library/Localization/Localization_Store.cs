#region Using directives

using SobekCM.Core.Configuration.Localization;
using SobekCM.Core.MemoryMgmt;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

#endregion

namespace SobekCM.Library.Localization
{
    /// <summary> Loads and caches per-language localization phrase tables from the XML files under
    /// config/default/localization/{languageCode}/, one file per (category, language) pair, grouped into
    /// a subfolder per language code (e.g. config/default/localization/nl/sobekcm_localization_mysobek_nl.config).
    /// Deliberately separate from
    /// <c>Configuration_Files_Reader</c> — that reader eagerly loads every config file at startup by
    /// root-element name; these files are numerous (one per language x category) and load lazily, only
    /// for languages actually requested. Internal — consumed only through <see cref="Localization_Gateway"/>. </summary>
    internal static class Localization_Store
    {
        private const string ENGLISH_CODE = "en";

        private static readonly object cacheLock = new object();

        // "category:languageCode" -> section -> key -> value
        private static readonly Dictionary<string, Dictionary<string, Dictionary<string, string>>> cache
            = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>(StringComparer.OrdinalIgnoreCase);

        /// <summary> Get a single localized phrase, falling back from the requested language to English,
        /// and finally to a visible marker if even English is missing the key (so a missing translation
        /// is obvious in the rendered page rather than silently blank) </summary>
        /// <param name="Category"> "items" or "aggregations" — matches the config file name suffix </param>
        /// <param name="Section"> Section name (matches the <c>&lt;Section Name="..."&gt;</c> element and
        /// the Localization_Gateway nested class name) </param>
        /// <param name="Key"> Phrase key (matches the <c>&lt;Phrase Key="..."&gt;</c> attribute and the
        /// Localization_Gateway property name) </param>
        /// <param name="Language"> Requested display language for the current request </param>
        internal static string Get(string Category, string Section, string Key, Web_Language_Enum Language)
        {
            string languageCode = Web_Language_Enum_Converter.Enum_To_Code(Language);
            if (String.IsNullOrEmpty(languageCode))
                languageCode = ENGLISH_CODE;

            if (!String.Equals(languageCode, ENGLISH_CODE, StringComparison.OrdinalIgnoreCase))
            {
                Dictionary<string, Dictionary<string, string>> requestedTable = Get_Or_Load(Category, languageCode);
                if ((requestedTable != null) && requestedTable.TryGetValue(Section, out Dictionary<string, string> requestedSection)
                    && requestedSection.TryGetValue(Key, out string requestedValue))
                {
                    return requestedValue;
                }
            }

            Dictionary<string, Dictionary<string, string>> englishTable = Get_Or_Load(Category, ENGLISH_CODE);
            if ((englishTable != null) && englishTable.TryGetValue(Section, out Dictionary<string, string> englishSection)
                && englishSection.TryGetValue(Key, out string englishValue))
            {
                return englishValue;
            }

            return "[[" + Section + "." + Key + "]]";
        }

        /// <summary> Clears every cached language table, so edited XML files are picked up on next
        /// access without an app restart. Wired into <c>UI_ApplicationCache_Gateway.ResetAll()</c>. </summary>
        internal static void Clear_Cache()
        {
            lock (cacheLock)
            {
                cache.Clear();
            }
        }

        private static Dictionary<string, Dictionary<string, string>> Get_Or_Load(string Category, string LanguageCode)
        {
            string cacheKey = Category + ":" + LanguageCode;

            lock (cacheLock)
            {
                if (cache.TryGetValue(cacheKey, out Dictionary<string, Dictionary<string, string>> existing))
                    return existing;

                Dictionary<string, Dictionary<string, string>> loaded = Read_File(Category, LanguageCode);

                // Cache a (possibly empty) table either way, so a missing file isn't re-probed on every request
                cache[cacheKey] = loaded ?? new Dictionary<string, Dictionary<string, string>>();
                return cache[cacheKey];
            }
        }

        private static Dictionary<string, Dictionary<string, string>> Read_File(string Category, string LanguageCode)
        {
            string fileName = "sobekcm_localization_" + Category + "_" + LanguageCode + ".config";
            string filePath = Path.Combine(ContentRoot_Gateway.ContentRootPath, "config", "default", "localization", LanguageCode, fileName);

            if (!File.Exists(filePath))
                return null;

            var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

            using (var readerStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                using (var readerXml = new XmlTextReader(readerStream))
                {
                    while (readerXml.Read())
                    {
                        if ((readerXml.NodeType == XmlNodeType.Element) && (readerXml.Name.ToLower() == "localization"))
                        {
                            read_sections(readerXml.ReadSubtree(), result);
                        }
                    }
                }
            }

            return result;
        }

        private static void read_sections(XmlReader ReaderXml, Dictionary<string, Dictionary<string, string>> Result)
        {
            string currentSection = null;

            while (ReaderXml.Read())
            {
                if ((ReaderXml.NodeType == XmlNodeType.EndElement) && (ReaderXml.Name.ToLower() == "section"))
                {
                    currentSection = null;
                    continue;
                }

                if (ReaderXml.NodeType == XmlNodeType.Element)
                {
                    switch (ReaderXml.Name.ToLower())
                    {
                        case "section":
                            currentSection = ReaderXml.MoveToAttribute("Name") ? ReaderXml.Value.Trim() : null;
                            if ((!String.IsNullOrEmpty(currentSection)) && (!Result.ContainsKey(currentSection)))
                                Result[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                            ReaderXml.MoveToElement();
                            break;

                        case "phrase":
                            if (!String.IsNullOrEmpty(currentSection))
                            {
                                string key = ReaderXml.MoveToAttribute("Key") ? ReaderXml.Value.Trim() : null;
                                ReaderXml.MoveToElement();
                                if (!String.IsNullOrEmpty(key))
                                {
                                    string value = ReaderXml.IsEmptyElement ? String.Empty : ReaderXml.ReadElementContentAsString();
                                    Result[currentSection][key] = value;
                                }
                            }
                            break;
                    }
                }
            }
        }
    }
}
