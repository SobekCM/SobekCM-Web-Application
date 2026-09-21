using ProtoBuf;
using System;
using System.Globalization;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace SobekCM.Core.Configuration.Localization
{
    /// <summary> A single supported language, as read from the language support configuration file
    /// ( sobekcm_language_support.config, under the &lt;Languages&gt; element ) </summary>
    [Serializable, DataContract, ProtoContract]
    [XmlRoot("LanguageInfo")]
    public class Web_Language_Info
    {
        /// <summary> Display name for this language, as given in the configuration file (e.g. "Dutch") </summary>
        [DataMember(Name = "name")]
        [XmlAttribute("name")]
        [ProtoMember(1)]
        public string Name { get; set; }

        /// <summary> ISO code for this language (e.g. "nl"), matching the configuration file's element text </summary>
        [DataMember(Name = "code")]
        [XmlAttribute("code")]
        [ProtoMember(2)]
        public string Code { get; set; }

        /// <summary> Name of this language in the language itself (e.g. "Nederlands"), from the optional 'native'
        /// attribute in the configuration file.  Use <see cref="Get_Native_Name"/> to get a value with fallbacks. </summary>
        [DataMember(Name = "native", EmitDefaultValue = false)]
        [XmlAttribute("native")]
        [ProtoMember(3)]
        public string Native_Name { get; set; }

        /// <summary> Gets the name of this language in the language itself, for language pickers where each
        /// visitor needs to be able to find their own language whatever language the page is shown in </summary>
        /// <returns> The configured native name if there is one; otherwise the .NET culture's native name for the
        /// code (first letter capitalized, since some cultures return it lowercase); otherwise the English <see cref="Name"/> </returns>
        public string Get_Native_Name()
        {
            if (!String.IsNullOrWhiteSpace(Native_Name))
                return Native_Name;

            if (!String.IsNullOrWhiteSpace(Code))
            {
                try
                {
                    CultureInfo culture = CultureInfo.GetCultureInfo(Code);

                    // Unknown codes can come back as a made-up culture whose "native name" is just the code again
                    string native = culture.NativeName;
                    if ((!String.IsNullOrWhiteSpace(native)) && (!String.Equals(native, Code, StringComparison.OrdinalIgnoreCase)))
                        return culture.TextInfo.ToUpper(native[0]) + native.Substring(1);
                }
                catch (CultureNotFoundException)
                {
                    // Not a code .NET knows, so fall through to the configured name
                }
            }

            return Name;
        }
    }
}
