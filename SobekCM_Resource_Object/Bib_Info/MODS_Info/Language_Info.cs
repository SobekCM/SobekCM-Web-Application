#region Using directives

using System;
using System.IO;

#endregion

namespace SobekCM.Resource_Object.Bib_Info
{
    /// <summary>A designation of the language in which the content of a resource is expressed</summary>
    /// <remarks>This class extends the <see cref="XML_Node_Base_Type"/> class for writing to XML </remarks>
    [Serializable]
    public class Language_Info : XML_Writing_Base_Type, IEquatable<Language_Info>
    {
        private string language_iso_code;
        private string language_rfc_code;
        private string language_text;

        private string script_text;
        private string script_iso_code;


        /// <summary> Constructor creates an empty instance of the Language_Info class </summary>
        public Language_Info()
        {
            // Do nothing by default
        }
       
        /// <summary> Constructor creates an empty instance of the Language_Info class </summary>
        /// <param name="Language_Text">Language term for this language</param>
        /// <param name="Language_ISO_Code">Iso639-2b code for this language</param>
        /// <param name="Language_RFC_Code">Rfc3066 code for this language</param>       
        public Language_Info(string Language_Text, string Language_ISO_Code, string Language_RFC_Code)
        {
            language_text = Language_Text;
            language_iso_code = Language_ISO_Code;
            language_rfc_code = Language_RFC_Code;

            if ((language_iso_code.Length == 0) && (language_text.Length > 0))
            {
                string code = Get_Code_By_Language(language_text);
                if (code.Length > 0)
                    language_iso_code = code;
            }

            if ((language_iso_code.Length > 0) && (language_text.Length == 0))
            {
                string text = Get_Language_By_Code(language_iso_code);
                if (text.Length > 0)
                    language_text = text;
            }

            // The code may be appearing in the text portion
            if ((language_iso_code.Length == 0) && ((language_text.Length == 3) || ((language_text.Length > 3) && ((language_text[2] == '-') || (language_text[3] == '-')))))
            {
                string possible_code = language_text;
                if ((language_text.Length > 3) && ((language_text[2] == '-') || (language_text[3] == '-')))
                    possible_code = language_text.Split("-".ToCharArray())[0];

                string possible_text = Get_Language_By_Code(possible_code);
                if (possible_text.Length > 0)
                {
                    if (language_text.IndexOf("-") < 0)
                        language_iso_code = language_text;
                    else
                        language_rfc_code = language_text;
                    language_text = possible_text;
                }
            }
            else if ((language_iso_code.Length == 0) && (language_text.Length > 3) && ((language_text[2] == '_') || (language_text[3] == '_')))
            {
                string possible_code = language_text.Split("_".ToCharArray())[0];

                string possible_text = Get_Language_By_Code(possible_code);
                if (possible_text.Length > 0)
                {
                    if (language_text.IndexOf("_") < 0)
                        language_iso_code = language_text;
                    else
                        language_rfc_code = language_text.Replace("_", "-");
                    language_text = possible_text;
                }
            }
        }

        /// <summary> Constructor creates an empty instance of the Language_Info class </summary>
        /// <param name="Language_Text">Language term for this language</param>
        /// <param name="Language_ISO_Code">Iso639-2b code for this language</param>
        /// <param name="Language_RFC_Code">Rfc3066 code for this language</param>   
        /// <param name="Script_Text">Script term for the alphabet of this language</param>
        /// <param name="Script_ISO_Code">Script Iso15924 code for this script </param>      
        public Language_Info(string Language_Text, string Language_ISO_Code, string Language_RFC_Code, string Script_Text, string Script_ISO_Code)
        {
            language_text = Language_Text ?? String.Empty;
            language_iso_code = Language_ISO_Code ?? String.Empty;
            language_rfc_code = Language_RFC_Code ?? String.Empty;

            if ((String.IsNullOrEmpty(language_iso_code)) && (!String.IsNullOrEmpty(language_text)))
            {
                string code = Get_Code_By_Language(language_text);
                if (code.Length > 0)
                    language_iso_code = code;
            }

            if ((!String.IsNullOrEmpty(language_iso_code)) && (String.IsNullOrEmpty(language_text)))
            {
                string text = Get_Language_By_Code(language_iso_code);
                if (text.Length > 0)
                    language_text = text;
            }

            // The code may be appearing in the text portion
            if ((language_iso_code.Length == 0) && ((language_text.Length == 3) || ((language_text.Length > 3) && ((language_text[2] == '-') || (language_text[3] == '-')))))
            {
                string possible_code = language_text;
                if ((language_text.Length > 3) && ((language_text[2] == '-') || (language_text[3] == '-')))
                    possible_code = language_text.Split("-".ToCharArray())[0];

                string possible_text = Get_Language_By_Code(possible_code);
                if (possible_text.Length > 0)
                {
                    if (language_text.IndexOf("-") < 0)
                        language_iso_code = language_text;
                    else
                        language_rfc_code = language_text;
                    language_text = possible_text;
                }
            }
            else if ((language_iso_code.Length == 0) && (language_text.Length > 3) && ((language_text[2] == '_') || (language_text[3] == '_')))
            {
                string possible_code = language_text.Split("_".ToCharArray())[0];

                string possible_text = Get_Language_By_Code(possible_code);
                if (possible_text.Length > 0)
                {
                    if (language_text.IndexOf("_") < 0)
                        language_iso_code = language_text;
                    else
                        language_rfc_code = language_text.Replace("_", "-");
                    language_text = possible_text;
                }
            }

            script_text = Script_Text;
            script_iso_code = Script_ISO_Code;

            if ((String.IsNullOrEmpty(script_iso_code)) && (!String.IsNullOrEmpty(script_text)))
            {
                string code = Get_Code_By_Script(script_text);
                if (code.Length > 0)
                    script_iso_code = code;
            }

            if ((!String.IsNullOrEmpty(script_iso_code)) && (String.IsNullOrEmpty(script_text)))
            {
                string text = Get_Script_By_Code(script_iso_code);
                if (text.Length > 0)
                    script_text = text;
            }
        }

        /// <summary> Gets or sets the Language term for this language  </summary>
        public string Language_Text
        {
            get { return language_text ?? String.Empty; }
            set
            {
                language_text = value;

                if ((String.IsNullOrEmpty(language_iso_code)) && (!String.IsNullOrEmpty(language_text)))
                {
                    string code = Get_Code_By_Language(language_text);
                    if (code.Length > 0)
                        language_iso_code = code;
                }
            }
        }

        /// <summary> Gets or sets the Iso639-2b code for this language </summary>
        public string Language_ISO_Code
        {
            get { return language_iso_code ?? String.Empty; }
            set
            {
                language_iso_code = value;

                if ((!String.IsNullOrEmpty(language_iso_code)) && (String.IsNullOrEmpty(language_text)))
                {
                    string text = Get_Language_By_Code(language_iso_code);
                    if (text.Length > 0)
                        language_text = text;
                }
            }
        }

        /// <summary> Gets or sets the Rfc3066 code for this language  </summary>
        public string Language_RFC_Code
        {
            get { return language_rfc_code ?? String.Empty; }
            set { language_rfc_code = value; }
        }

        /// <summary> Gets or sets the script type ( in text ) </summary>
        public string Script_Text
        {
            get { return script_text ?? String.Empty; }
            set
            {
                script_text = value;

                if (String.IsNullOrEmpty(script_iso_code))
                {
                    string code = Get_Code_By_Script(Script_Text);
                    if (code.Length > 0)
                        script_iso_code = code;
                }
            }
        }

        /// <summary> Gets or sets the Iso15924 code for this script </summary>
        public string Script_ISO_Code
        {
            get { return script_iso_code ?? String.Empty; }
            set
            {
                script_iso_code = value;

                if ((!String.IsNullOrEmpty(script_iso_code)) && (String.IsNullOrEmpty(script_text)))
                {
                    string text = Get_Script_By_Code(script_iso_code);
                    if (text.Length > 0)
                        script_text = text;
                }
            }
        }


        /// <summary> Flag indicates if this language object has data, or is an empty language </summary>
        internal bool hasData
        {
            get { return (!String.IsNullOrEmpty(language_text)) || (!String.IsNullOrEmpty(language_iso_code)) || (!String.IsNullOrEmpty(language_rfc_code))
                    || (!String.IsNullOrEmpty(script_text)) || (!String.IsNullOrEmpty(script_iso_code)); }
        }

        #region IEquatable<Language_Info> Members

        /// <summary> Compares this object with another similarly typed object </summary>
        /// <param name="Other">Similarly types object </param>
        /// <returns>TRUE if the two objects are sufficiently similar</returns>
        public bool Equals(Language_Info Other)
        {
            if ((!String.IsNullOrEmpty(Language_Text)) && (String.Compare(Language_Text, Other.Language_Text, StringComparison.Ordinal) == 0))
                return true;

            if ((!String.IsNullOrEmpty(Language_RFC_Code)) && (String.Compare(Language_RFC_Code, Other.Language_RFC_Code, StringComparison.Ordinal) == 0))
                return true;

            return (!String.IsNullOrEmpty(Language_ISO_Code)) && (String.Compare(Language_ISO_Code, Other.Language_ISO_Code, StringComparison.Ordinal) == 0);
        }

        #endregion

        /// <summary> Writes this language as MODS to a writer writing to a stream ( either a file or web response stream )</summary>
        /// <param name="ReturnValue"> Writer to the MODS building stream </param>
        internal void Add_MODS(TextWriter ReturnValue)
        {
            if (!hasData)
                return;

            ReturnValue.Write("<mods:language>\r\n");

            if (!String.IsNullOrEmpty(language_text))
            {
                ReturnValue.Write("<mods:languageTerm type=\"text\">" + Convert_String_To_XML_Safe(language_text) + "</mods:languageTerm>\r\n");
            }

            if (!String.IsNullOrEmpty(language_iso_code))
            {
                ReturnValue.Write("<mods:languageTerm type=\"code\" authority=\"iso639-2b\">" + Convert_String_To_XML_Safe(language_iso_code) + "</mods:languageTerm>\r\n");
            }

            if (!String.IsNullOrEmpty(language_rfc_code))
            {
                ReturnValue.Write("<mods:languageTerm type=\"code\" authority=\"rfc3066\">" + Convert_String_To_XML_Safe(language_rfc_code) + "</mods:languageTerm>\r\n");
            }

            if (!String.IsNullOrEmpty(script_text))
            {
                ReturnValue.Write("<mods:scriptTerm type=\"text\">" + Convert_String_To_XML_Safe(script_text) + "</mods:scriptTerm>\r\n");
            }

            if (!String.IsNullOrEmpty(script_iso_code))
            {
                ReturnValue.Write("<mods:scriptTerm type=\"code\" authority=\"iso15924\">" + Convert_String_To_XML_Safe(script_iso_code) + "</mods:scriptTerm>\r\n");
            }
            ReturnValue.Write("</mods:language>\r\n");
        }

        #region Methods to get the language text from the ISO code

        /// <summary> Get the language text from the ISO code </summary>
        /// <param name="CodeCheck"> ISO code to convert to language name </param>
        /// <returns> The associated language name or an empty string </returns>
        public string Get_Language_By_Code(string CodeCheck)
        {
            return Get_Language_By_Code_Static(CodeCheck);
        }

        /// <summary> Get the language text from the ISO code </summary>
        /// <param name="CodeCheck"> ISO code to convert to language name </param>
        /// <returns> The associated language name or an empty string </returns>
        public static string Get_Language_By_Code_Static(string CodeCheck)
        {
            string code_upper = CodeCheck.ToUpper();
            string text = String.Empty;
            switch (code_upper)
            {
                case "AAR":
                case "AA":
                    text = "Afar";
                    break;
                case "ABK":
                case "AB":
                    text = "Abkhaz";
                    break;
                case "ACE":
                    text = "Achinese";
                    break;
                case "ACH":
                    text = "Acoli";
                    break;
                case "ADA":
                    text = "Adangme";
                    break;
                case "ADY":
                    text = "Adygei";
                    break;
                case "AFA":
                    text = "Afroasiatic (Other)";
                    break;
                case "AFH":
                    text = "Afrihili (Artificial language)";
                    break;
                case "AFR":
                case "AF":
                    text = "Afrikaans";
                    break;
                case "AJM":
                    text = "Aljamía";
                    break;
                case "AKA":
                case "AK":
                    text = "Akan";
                    break;
                case "AKK":
                    text = "Akkadian";
                    break;
                case "ALB":
                case "SQI":
                case "SQ":
                    text = "Albanian";
                    break;
                case "ALE":
                    text = "Aleut";
                    break;
                case "ALG":
                    text = "Algonquian (Other)";
                    break;
                case "AMH":
                case "AM":
                    text = "Amharic";
                    break;
                case "ANG":
                    text = "English, Old (ca. 450-1100)";
                    break;
                case "APA":
                    text = "Apache languages";
                    break;
                case "ARA":
                case "AR":
                    text = "Arabic";
                    break;
                case "ARC":
                    text = "Aramaic";
                    break;
                case "ARG":
                case "AN":
                    text = "Aragonese Spanish";
                    break;
                case "ARM":
                case "HYE":
                case "HY":
                    text = "Armenian";
                    break;
                case "ARN":
                    text = "Mapuche";
                    break;
                case "ARP":
                    text = "Arapaho";
                    break;
                case "ART":
                    text = "Artificial (Other)";
                    break;
                case "ARW":
                    text = "Arawak";
                    break;
                case "ASM":
                case "AS":
                    text = "Assamese";
                    break;
                case "AST":
                    text = "Bable";
                    break;
                case "ATH":
                    text = "Athapascan (Other)";
                    break;
                case "AUS":
                    text = "Australian languages";
                    break;
                case "AVA":
                case "AV":
                    text = "Avaric";
                    break;
                case "AVE":
                case "AE":
                    text = "Avestan";
                    break;
                case "AWA":
                    text = "Awadhi";
                    break;
                case "AYM":
                case "AY":
                    text = "Aymara";
                    break;
                case "AZE":
                case "AZ":
                    text = "Azerbaijani";
                    break;
                case "BAD":
                    text = "Banda";
                    break;
                case "BAI":
                    text = "Bamileke languages";
                    break;
                case "BAK":
                case "BA":
                    text = "Bashkir";
                    break;
                case "BAL":
                    text = "Baluchi";
                    break;
                case "BAM":
                case "BM":
                    text = "Bambara";
                    break;
                case "BAN":
                    text = "Balinese";
                    break;
                case "BAQ":
                case "EUS":
                case "EU":
                    text = "Basque";
                    break;
                case "BAS":
                    text = "Basa";
                    break;
                case "BAT":
                    text = "Baltic (Other)";
                    break;
                case "BEJ":
                    text = "Beja";
                    break;
                case "BEL":
                case "BE":
                    text = "Belarusian";
                    break;
                case "BEM":
                    text = "Bemba";
                    break;
                case "BEN":
                case "BN":
                    text = "Bengali";
                    break;
                case "BER":
                    text = "Berber (Other)";
                    break;
                case "BHO":
                    text = "Bhojpuri";
                    break;
                case "BIH":
                case "BH":
                    text = "Bihari";
                    break;
                case "BIK":
                    text = "Bikol";
                    break;
                case "BIN":
                    text = "Edo";
                    break;
                case "BIS":
                case "BI":
                    text = "Bislama";
                    break;
                case "BLA":
                    text = "Siksika";
                    break;
                case "BNT":
                    text = "Bantu (Other)";
                    break;
                case "BOS":
                case "BS":
                    text = "Bosnian";
                    break;
                case "BRA":
                    text = "Braj";
                    break;
                case "BRE":
                case "BR":
                    text = "Breton";
                    break;
                case "BTK":
                    text = "Batak";
                    break;
                case "BUA":
                    text = "Buriat";
                    break;
                case "BUG":
                    text = "Bugis";
                    break;
                case "BUL":
                case "BG":
                    text = "Bulgarian";
                    break;
                case "BUR":
                case "MYA":
                case "MY":
                    text = "Burmese";
                    break;
                case "CAD":
                    text = "Caddo";
                    break;
                case "CAI":
                    text = "Central American Indian (Other)";
                    break;
                case "CAM":
                    text = "Khmer";
                    break;
                case "CAR":
                    text = "Carib";
                    break;
                case "CAT":
                case "CA":
                    text = "Catalan";
                    break;
                case "CAU":
                    text = "Caucasian (Other)";
                    break;
                case "CEB":
                    text = "Cebuano";
                    break;
                case "CEL":
                    text = "Celtic (Other)";
                    break;
                case "CHA":
                case "CH":
                    text = "Chamorro";
                    break;
                case "CHB":
                    text = "Chibcha";
                    break;
                case "CHE":
                case "CE":
                    text = "Chechen";
                    break;
                case "CHG":
                    text = "Chagatai";
                    break;
                case "CHI":
                case "ZHO":
                case "ZH":
                    text = "Chinese";
                    break;
                case "CHK":
                    text = "Truk";
                    break;
                case "CHM":
                    text = "Mari";
                    break;
                case "CHN":
                    text = "Chinook jargon";
                    break;
                case "CHO":
                    text = "Choctaw";
                    break;
                case "CHP":
                    text = "Chipewyan";
                    break;
                case "CHR":
                    text = "Cherokee";
                    break;
                case "CHU":
                case "CU":
                    text = "Church Slavic";
                    break;
                case "CHV":
                case "CV":
                    text = "Chuvash";
                    break;
                case "CHY":
                    text = "Cheyenne";
                    break;
                case "CMC":
                    text = "Chamic languages";
                    break;
                case "COP":
                    text = "Coptic";
                    break;
                case "COR":
                case "KW":
                    text = "Cornish";
                    break;
                case "COS":
                case "CO":
                    text = "Corsican";
                    break;
                case "CPE":
                    text = "Creoles and Pidgins, English-based (Other)";
                    break;
                case "CPF":
                    text = "Creoles and Pidgins, French-based (Other)";
                    break;
                case "CPP":
                    text = "Creoles and Pidgins, Portuguese-based (Other)";
                    break;
                case "CRE":
                case "CR":
                    text = "Cree";
                    break;
                case "CRH":
                    text = "Crimean Tatar";
                    break;
                case "CRP":
                    text = "Creoles and Pidgins (Other)";
                    break;
                case "CUS":
                    text = "Cushitic (Other)";
                    break;
                case "CZE":
                case "CES":
                case "CS":
                    text = "Czech";
                    break;
                case "DAK":
                    text = "Dakota";
                    break;
                case "DAN":
                case "DA":
                    text = "Danish";
                    break;
                case "DAR":
                    text = "Dargwa";
                    break;
                case "DAY":
                    text = "Dayak";
                    break;
                case "DEL":
                    text = "Delaware";
                    break;
                case "DEN":
                    text = "Slave";
                    break;
                case "DGR":
                    text = "Dogrib";
                    break;
                case "DIN":
                    text = "Dinka";
                    break;
                case "DIV":
                case "DV":
                    text = "Divehi";
                    break;
                case "DOI":
                    text = "Dogri";
                    break;
                case "DRA":
                    text = "Dravidian (Other)";
                    break;
                case "DUA":
                    text = "Duala";
                    break;
                case "DUM":
                    text = "Dutch, Middle (ca. 1050-1350)";
                    break;
                case "DUT":
                case "NL":
                case "NLD":
                    text = "Dutch";
                    break;
                case "DYU":
                    text = "Dyula";
                    break;
                case "DZO":
                case "DZ":
                    text = "Dzongkha";
                    break;
                case "EFI":
                    text = "Efik";
                    break;
                case "EGY":
                    text = "Egyptian";
                    break;
                case "EKA":
                    text = "Ekajuk";
                    break;
                case "ELX":
                    text = "Elamite";
                    break;
                case "ENG":
                case "EN":
                    text = "English";
                    break;
                case "ENM":
                    text = "English, Middle (1100-1500)";
                    break;
                case "EPO":
                case "EO":
                    text = "Esperanto";
                    break;
                case "ESK":
                    text = "Eskimo languages";
                    break;
                case "ESP":
                    text = "Esperanto";
                    break;
                case "EST":
                case "ET":
                    text = "Estonian";
                    break;
                case "ETH":
                    text = "Ethiopic";
                    break;
                case "EWE":
                case "EE":
                    text = "Ewe";
                    break;
                case "EWO":
                    text = "Ewondo";
                    break;
                case "FAN":
                    text = "Fang";
                    break;
                case "FAO":
                case "FO":
                    text = "Faroese";
                    break;
                case "FAR":
                    text = "Faroese";
                    break;
                case "FAT":
                    text = "Fanti";
                    break;
                case "FIJ":
                case "FJ":
                    text = "Fijian";
                    break;
                case "FIN":
                case "FI":
                    text = "Finnish";
                    break;
                case "FIU":
                    text = "Finno-Ugrian (Other)";
                    break;
                case "FON":
                    text = "Fon";
                    break;
                case "FRE":
                case "FR":
                    text = "French";
                    break;
                case "FRI":
                    text = "Frisian";
                    break;
                case "FRM":
                    text = "French, Middle (ca. 1400-1600)";
                    break;
                case "FRO":
                    text = "French, Old (ca. 842-1400)";
                    break;
                case "FRY":
                case "FY":
                    text = "Frisian";
                    break;
                case "FUL":
                case "FF":
                    text = "Fula"; // Fulah?
                    break;
                case "FUR":
                    text = "Friulian";
                    break;
                case "GAA":
                    text = "Gã";
                    break;
                case "GAE":
                    text = "Scottish Gaelic";
                    break;
                case "GAG":
                    text = "Galician";
                    break;
                case "GAL":
                    text = "Oromo";
                    break;
                case "GAY":
                    text = "Gayo";
                    break;
                case "GBA":
                    text = "Gbaya";
                    break;
                case "GEM":
                    text = "Germanic (Other)";
                    break;
                case "GEO":
                case "KA":
                case "KAT":
                    text = "Georgian";
                    break;
                case "GER":
                case "DE":
                case "DEU":
                    text = "German";
                    break;
                case "GEZ":
                    text = "Ethiopic";
                    break;
                case "GIL":
                    text = "Gilbertese";
                    break;
                case "GLA":
                case "GD":
                    text = "Scottish Gaelic";
                    break;
                case "GLE":
                case "GA":
                    text = "Irish";
                    break;
                case "GLG":
                case "GL":
                    text = "Galician";
                    break;
                case "GLV":
                case "GV":
                    text = "Manx";
                    break;
                case "GMH":
                    text = "German, Middle High (ca. 1050-1500)";
                    break;
                case "GOH":
                    text = "German, Old High (ca. 750-1050)";
                    break;
                case "GON":
                    text = "Gondi";
                    break;
                case "GOR":
                    text = "Gorontalo";
                    break;
                case "GOT":
                    text = "Gothic";
                    break;
                case "GRB":
                    text = "Grebo";
                    break;
                case "GRC":
                    text = "Greek, Ancient (to 1453)";
                    break;
                case "GRE":
                case "ELL":
                case "EL":
                    text = "Greek, Modern (1453- )";
                    break;
                case "GRN":
                case "GN":
                    text = "Guarani";
                    break;
                case "GUA":
                    text = "Guarani";
                    break;
                case "GUJ":
                    text = "Gujarati";
                    break;
                case "GWI":
                    text = "Gwich'in";
                    break;
                case "HAI":
                    text = "Haida";
                    break;
                case "HAT":
                case "HT":
                    text = "Haitian French Creole";
                    break;
                case "HAU":
                case "HA":
                    text = "Hausa";
                    break;
                case "HAW":
                    text = "Hawaiian";
                    break;
                case "HEB":
                case "HE":
                    text = "Hebrew";
                    break;
                case "HER":
                case "HZ":
                    text = "Herero";
                    break;
                case "HIL":
                    text = "Hiligaynon";
                    break;
                case "HIM":
                    text = "Himachali";
                    break;
                case "HIN":
                case "HI":
                    text = "Hindi";
                    break;
                case "HIT":
                    text = "Hittite";
                    break;
                case "HMN":
                    text = "Hmong";
                    break;
                case "HMO":
                case "HO":
                    text = "Hiri Motu";
                    break;
                case "HUN":
                case "HU":
                    text = "Hungarian";
                    break;
                case "HUP":
                    text = "Hupa";
                    break;
                case "IBA":
                    text = "Iban";
                    break;
                case "IBO":
                case "IG":
                    text = "Igbo";
                    break;
                case "ICE":
                case "IS":
                    text = "Icelandic";
                    break;
                case "IDO":
                case "IO":
                    text = "Ido";
                    break;
                case "III":
                case "II":
                    text = "Sichuan Yi";
                    break;
                case "IJO":
                    text = "Ijo";
                    break;
                case "IKU":
                case "IU":
                    text = "Inuktitut";
                    break;
                case "ILE":
                case "IE":
                    text = "Interlingue"; // Also Occidental?
                    break;
                case "ILO":
                    text = "Iloko";
                    break;
                case "INA":
                case "IA":
                    text = "Interlingua (International Auxiliary Language Association";
                    break;
                case "INC":
                    text = "Indic (Other)";
                    break;
                case "IND":
                case "ID":
                    text = "Indonesian";
                    break;
                case "INE":
                    text = "Indo-European (Other)";
                    break;
                case "INH":
                    text = "Ingush";
                    break;
                case "INT":
                    text = "Interlingua (International Auxiliary Language Association)";
                    break;
                case "IPK":
                case "IK":
                    text = "Inupiaq";
                    break;
                case "IRA":
                    text = "Iranian (Other)";
                    break;
                case "IRI":
                    text = "Irish";
                    break;
                case "IRO":
                    text = "Iroquoian (Other)";
                    break;
                case "ITA":
                case "IT":
                    text = "Italian";
                    break;
                case "JAV":
                case "JV":
                    text = "Javanese";
                    break;
                case "JPN":
                case "JA":
                    text = "Japanese";
                    break;
                case "JPR":
                    text = "Judeo-Persian";
                    break;
                case "JRB":
                    text = "Judeo-Arabic";
                    break;
                case "KAA":
                    text = "Kara-Kalpak";
                    break;
                case "KAB":
                    text = "Kabyle";
                    break;
                case "KAC":
                    text = "Kachin";
                    break;
                case "KAL":
                case "KL":
                    text = "Kalâtdlisut";
                    break;
                case "KAM":
                    text = "Kamba";
                    break;
                case "KAN":
                case "KN":
                    text = "Kannada";
                    break;
                case "KAR":
                    text = "Karen";
                    break;
                case "KAS":
                case "KS":
                    text = "Kashmiri";
                    break;
                case "KAU":
                case "KR":
                    text = "Kanuri";
                    break;
                case "KAW":
                    text = "Kawi";
                    break;
                case "KAZ":
                case "KK":
                    text = "Kazakh";
                    break;
                case "KBD":
                    text = "Kabardian";
                    break;
                case "KHA":
                    text = "Khasi";
                    break;
                case "KHI":
                    text = "Khoisan (Other)";
                    break;
                case "KHM":
                case "KM":
                    text = "Khmer";
                    break;
                case "KHO":
                    text = "Khotanese";
                    break;
                case "KIK":
                case "KI":
                    text = "Kikuyu";
                    break;
                case "KIN":
                case "RW":
                    text = "Kinyarwanda";
                    break;
                case "KIR":
                case "KY":
                    text = "Kyrgyz";
                    break;
                case "KMB":
                    text = "Kimbundu";
                    break;
                case "KOK":
                    text = "Konkani";
                    break;
                case "KOM":
                case "KV":
                    text = "Komi";
                    break;
                case "KON":
                case "KG":
                    text = "Kongo";
                    break;
                case "KOR":
                case "KO":
                    text = "Korean";
                    break;
                case "KOS":
                    text = "Kusaie";
                    break;
                case "KPE":
                    text = "Kpelle";
                    break;
                case "KRO":
                    text = "Kru";
                    break;
                case "KRU":
                    text = "Kurukh";
                    break;
                case "KUA":
                case "KJ":
                    text = "Kuanyama";
                    break;
                case "KUM":
                    text = "Kumyk";
                    break;
                case "KUR":
                case "KU":
                    text = "Kurdish";
                    break;
                case "KUS":
                    text = "Kusaie";
                    break;
                case "KUT":
                    text = "Kutenai";
                    break;
                case "LAD":
                    text = "Ladino";
                    break;
                case "LAH":
                    text = "Lahnda";
                    break;
                case "LAM":
                    text = "Lamba";
                    break;
                case "LAN":
                    text = "Occitan (post-1500)";
                    break;
                case "LAO":
                case "LO":
                    text = "Lao";
                    break;
                case "LAP":
                    text = "Sami";
                    break;
                case "LAT":
                case "LA":
                    text = "Latin";
                    break;
                case "LAV":
                case "LV":
                    text = "Latvian";
                    break;
                case "LEZ":
                    text = "Lezgian";
                    break;
                case "LIM":
                case "LI":
                    text = "Limburgish";
                    break;
                case "LIN":
                case "LN":
                    text = "Lingala";
                    break;
                case "LIT":
                case "LT":
                    text = "Lithuanian";
                    break;
                case "LOL":
                    text = "Mongo-Nkundu";
                    break;
                case "LOZ":
                    text = "Lozi";
                    break;
                case "LTZ":
                    text = "Letzeburgesch";
                    break;
                case "LUA":
                    text = "Luba-Lulua";
                    break;
                case "LUB":
                case "LU":
                    text = "Luba-Katanga";
                    break;
                case "LUG":
                case "LG":
                    text = "Ganda";
                    break;
                case "LUI":
                    text = "Luiseño";
                    break;
                case "LUN":
                    text = "Lunda";
                    break;
                case "LUO":
                    text = "Luo (Kenya and Tanzania)";
                    break;
                case "LUS":
                    text = "Lushai";
                    break;
                case "MAC":
                case "MK":
                    text = "Macedonian";
                    break;
                case "MAD":
                    text = "Madurese";
                    break;
                case "MAG":
                    text = "Magahi";
                    break;
                case "MAH":
                case "MH":
                    text = "Marshallese";
                    break;
                case "MAI":
                    text = "Maithili";
                    break;
                case "MAK":
                    text = "Makasar";
                    break;
                case "MAL":
                case "ML":
                    text = "Malayalam";
                    break;
                case "MAN":
                    text = "Mandingo";
                    break;
                case "MAO":
                case "MI":
                    text = "Maori";
                    break;
                case "MAP":
                    text = "Austronesian (Other)";
                    break;
                case "MAR":
                case "MR":
                    text = "Marathi";
                    break;
                case "MAS":
                    text = "Masai";
                    break;
                case "MAX":
                    text = "Manx";
                    break;
                case "MAY":
                case "MS":
                    text = "Malay";
                    break;
                case "MDR":
                    text = "Mandar";
                    break;
                case "MEN":
                    text = "Mende";
                    break;
                case "MGA":
                    text = "Irish, Middle (ca. 1100-1550)";
                    break;
                case "MIC":
                    text = "Micmac";
                    break;
                case "MIN":
                    text = "Minangkabau";
                    break;
                case "MIS":
                    text = "Miscellaneous languages";
                    break;
                case "MKH":
                    text = "Mon-Khmer (Other)";
                    break;
                case "MLA":
                    text = "Malagasy";
                    break;
                case "MLG":
                case "MG":
                    text = "Malagasy";
                    break;
                case "MLT":
                case "MT":
                    text = "Maltese";
                    break;
                case "MNC":
                    text = "Manchu";
                    break;
                case "MNI":
                    text = "Manipuri";
                    break;
                case "MNO":
                    text = "Manobo languages";
                    break;
                case "MOH":
                    text = "Mohawk";
                    break;
                case "MOL":
                    text = "Moldavian";
                    break;
                case "MON":
                case "MN":
                    text = "Mongolian";
                    break;
                case "MOS":
                    text = "Mooré";
                    break;
                case "MUL":
                    text = "Multiple languages";
                    break;
                case "MUN":
                    text = "Munda (Other)";
                    break;
                case "MUS":
                    text = "Creek";
                    break;
                case "MWR":
                    text = "Marwari";
                    break;
                case "MYN":
                    text = "Mayan languages";
                    break;
                case "NAH":
                    text = "Nahuatl";
                    break;
                case "NAI":
                    text = "North American Indian (Other)";
                    break;
                case "NAP":
                    text = "Neapolitan Italian";
                    break;
                case "NAU":
                case "NA":
                    text = "Nauru";
                    break;
                case "NAV":
                case "NV":
                    text = "Navajo";
                    break;
                case "NBL":
                case "NR":
                    text = "Ndebele (South Africa)"; // South Ndebele
                    break;
                case "NDE":
                case "ND":
                    text = "Ndebele (Zimbabwe)"; // North Ndebele
                    break;
                case "NDO":
                case "NG":
                    text = "Ndonga";
                    break;
                case "NDS":
                    text = "Low German";
                    break;
                case "NEP":
                case "NE":
                    text = "Nepali";
                    break;
                case "NEW":
                    text = "Newari";
                    break;
                case "NIA":
                    text = "Nias";
                    break;
                case "NIC":
                    text = "Niger-Kordofanian (Other)";
                    break;
                case "NIU":
                    text = "Niuean";
                    break;
                case "NNO":
                case "NN":
                    text = "Norwegian (Nynorsk)";
                    break;
                case "NOB":
                case "NB":
                    text = "Norwegian (Bokmål)";
                    break;
                case "NOG":
                    text = "Nogai";
                    break;
                case "NON":
                    text = "Old Norse";
                    break;
                case "NOR":
                case "NO":
                    text = "Norwegian";
                    break;
                case "NSO":
                    text = "Northern Sotho";
                    break;
                case "NUB":
                    text = "Nubian languages";
                    break;
                case "NYA":
                case "NY":
                    text = "Nyanja"; // Chichewa, Chewa
                    break;
                case "NYM":
                    text = "Nyamwezi";
                    break;
                case "NYN":
                    text = "Nyankole";
                    break;
                case "NYO":
                    text = "Nyoro";
                    break;
                case "NZI":
                    text = "Nzima";
                    break;
                case "OCI":
                case "OC":
                    text = "Occitan (post-1500)";
                    break;
                case "OJI":
                case "OJ":
                    text = "Ojibwa";
                    break;
                case "ORI":
                case "OR":
                    text = "Oriya";
                    break;
                case "ORM":
                case "OM":
                    text = "Oromo";
                    break;
                case "OSA":
                    text = "Osage";
                    break;
                case "OSS":
                case "OS":
                    text = "Ossetic";
                    break;
                case "OTA":
                    text = "Turkish, Ottoman";
                    break;
                case "OTO":
                    text = "Otomian languages";
                    break;
                case "PAA":
                    text = "Papuan (Other)";
                    break;
                case "PAG":
                    text = "Pangasinan";
                    break;
                case "PAL":
                    text = "Pahlavi";
                    break;
                case "PAM":
                    text = "Pampanga";
                    break;
                case "PAN":
                case "PA":
                    text = "Panjabi";
                    break;
                case "PAP":
                    text = "Papiamento";
                    break;
                case "PAU":
                    text = "Palauan";
                    break;
                case "PEO":
                    text = "Old Persian (ca. 600-400 B.C.)";
                    break;
                case "PER":
                case "FA":
                    text = "Persian";
                    break;
                case "PHI":
                    text = "Philippine (Other)";
                    break;
                case "PHN":
                    text = "Phoenician";
                    break;
                case "PLI":
                case "PI":
                    text = "Pali";
                    break;
                case "POL":
                case "PL":
                    text = "Polish";
                    break;
                case "PON":
                    text = "Ponape";
                    break;
                case "POR":
                case "PT":
                    text = "Portuguese";
                    break;
                case "PRA":
                    text = "Prakrit languages";
                    break;
                case "PRO":
                    text = "Provençal (to 1500)";
                    break;
                case "PUS":
                case "PS":
                    text = "Pushto";
                    break;
                case "QUE":
                case "QU":
                    text = "Quechua";
                    break;
                case "RAJ":
                    text = "Rajasthani";
                    break;
                case "RAP":
                    text = "Rapanui";
                    break;
                case "RAR":
                    text = "Rarotongan";
                    break;
                case "ROA":
                    text = "Romance (Other)";
                    break;
                case "ROH":
                case "RM":
                    text = "Raeto-Romance"; // Romancsh
                    break;
                case "ROM":
                    text = "Romani";
                    break;
                case "RUM":
                case "RO":
                    text = "Romanian";
                    break;
                case "RUN":
                    text = "Rundi";
                    break;
                case "RUS":
                case "RU":
                    text = "Russian";
                    break;
                case "SAD":
                    text = "Sandawe";
                    break;
                case "SAG":
                case "SG":
                    text = "Sango (Ubangi Creole)";
                    break;
                case "SAH":
                    text = "Yakut";
                    break;
                case "SAI":
                    text = "South American Indian (Other)";
                    break;
                case "SAL":
                    text = "Salishan languages";
                    break;
                case "SAM":
                    text = "Samaritan Aramaic";
                    break;
                case "SAN":
                case "SA":
                    text = "Sanskrit";
                    break;
                case "SAO":
                    text = "Samoan";
                    break;
                case "SAS":
                    text = "Sasak";
                    break;
                case "SAT":
                    text = "Santali";
                    break;
                case "SCC":
                    text = "Serbian";
                    break;
                case "SCO":
                    text = "Scots";
                    break;
                case "SCR":
                    text = "Croatian";
                    break;
                case "SEL":
                    text = "Selkup";
                    break;
                case "SEM":
                    text = "Semitic (Other)";
                    break;
                case "SGA":
                    text = "Irish, Old (to 1100)";
                    break;
                case "SGN":
                    text = "Sign languages";
                    break;
                case "SHN":
                    text = "Shan";
                    break;
                case "SHO":
                    text = "Shona";
                    break;
                case "SID":
                    text = "Sidamo";
                    break;
                case "SIN":
                case "SI":
                    text = "Sinhalese";
                    break;
                case "SIO":
                    text = "Siouan (Other)";
                    break;
                case "SIT":
                    text = "Sino-Tibetan (Other)";
                    break;
                case "SLA":
                    text = "Slavic (Other)";
                    break;
                case "SLO":
                case "SK":
                case "SLK":
                    text = "Slovak";
                    break;
                case "SLV":
                case "SL":
                    text = "Slovenian";
                    break;
                case "SMA":
                    text = "Southern Sami";
                    break;
                case "SME":
                case "SE":
                    text = "Northern Sami";
                    break;
                case "SMI":
                    text = "Sami";
                    break;
                case "SMJ":
                    text = "Lule Sami";
                    break;
                case "SMN":
                    text = "Inari Sami";
                    break;
                case "SMO":
                case "SM":
                    text = "Samoan";
                    break;
                case "SMS":
                    text = "Skolt Sami";
                    break;
                case "SNA":
                case "SN":
                    text = "Shona";
                    break;
                case "SND":
                case "SD":
                    text = "Sindhi";
                    break;
                case "SNH":
                    text = "Sinhalese";
                    break;
                case "SNK":
                    text = "Soninke";
                    break;
                case "SOG":
                    text = "Sogdian";
                    break;
                case "SOM":
                case "SO":
                    text = "Somali";
                    break;
                case "SON":
                    text = "Songhai";
                    break;
                case "SOT":
                case "ST":
                    text = "Sotho";
                    break;
                case "SPA":
                case "ES":
                    text = "Spanish";
                    break;
                case "SRD":
                case "SC":
                    text = "Sardinian";
                    break;
                case "SRR":
                    text = "Serer";
                    break;
                case "SSA":
                    text = "Nilo-Saharan (Other)";
                    break;
                case "SSO":
                    text = "Sotho";
                    break;
                case "SSW":
                case "SS":
                    text = "Swazi"; // Swati
                    break;
                case "SUK":
                    text = "Sukuma";
                    break;
                case "SUN":
                case "SU":
                    text = "Sundanese";
                    break;
                case "SUS":
                    text = "Susu";
                    break;
                case "SUX":
                    text = "Sumerian";
                    break;
                case "SWA":
                case "SW":
                    text = "Swahili";
                    break;
                case "SWE":
                case "SV":
                    text = "Swedish";
                    break;
                case "SWZ":
                    text = "Swazi";
                    break;
                case "SYR":
                    text = "Syriac";
                    break;
                case "TAG":
                    text = "Tagalog";
                    break;
                case "TAH":
                case "TY":
                    text = "Tahitian";
                    break;
                case "TAI":
                    text = "Tai (Other)";
                    break;
                case "TAJ":
                    text = "Tajik";
                    break;
                case "TAM":
                case "TA":
                    text = "Tamil";
                    break;
                case "TAR":
                case "TT":
                    text = "Tatar";
                    break;
                case "TAT":
                    text = "Tatar";
                    break;
                case "TEL":
                case "TE":
                    text = "Telugu";
                    break;
                case "TEM":
                    text = "Temne";
                    break;
                case "TER":
                    text = "Terena";
                    break;
                case "TET":
                    text = "Tetum";
                    break;
                case "TGK":
                case "TG":
                    text = "Tajik";
                    break;
                case "TGL":
                case "TL":
                    text = "Tagalog";
                    break;
                case "THA":
                case "TH":
                    text = "Thai";
                    break;
                case "TIB":
                case "BOD":
                case "BO":
                    text = "Tibetan";
                    break;
                case "TIG":
                    text = "Tigré";
                    break;
                case "TIR":
                case "TI":
                    text = "Tigrinya";
                    break;
                case "TIV":
                    text = "Tiv";
                    break;
                case "TKL":
                    text = "Tokelauan";
                    break;
                case "TLI":
                    text = "Tlingit";
                    break;
                case "TMH":
                    text = "Tamashek";
                    break;
                case "TOG":
                case "TO":
                    text = "Tonga (Nyasa)";
                    break;
                case "TON":
                    text = "Tongan";
                    break;
                case "TPI":
                    text = "Tok Pisin";
                    break;
                case "TRU":
                    text = "Truk";
                    break;
                case "TSI":
                    text = "Tsimshian";
                    break;
                case "TSN":
                case "TN":
                    text = "Tswana";
                    break;
                case "TSO":
                case "TS":
                    text = "Tsonga";
                    break;
                case "TSW":
                    text = "Tswana";
                    break;
                case "TUK":
                case "TK":
                    text = "Turkmen";
                    break;
                case "TUM":
                    text = "Tumbuka";
                    break;
                case "TUP":
                    text = "Tupi languages";
                    break;
                case "TUR":
                case "TR":
                    text = "Turkish";
                    break;
                case "TUT":
                    text = "Altaic (Other)";
                    break;
                case "TVL":
                    text = "Tuvaluan";
                    break;
                case "TWI":
                case "TW":
                    text = "Twi";
                    break;
                case "TYV":
                    text = "Tuvinian";
                    break;
                case "UDM":
                    text = "Udmurt";
                    break;
                case "UGA":
                    text = "Ugaritic";
                    break;
                case "UIG":
                case "UG":
                    text = "Uighur";
                    break;
                case "UKR":
                case "UK":
                    text = "Ukrainian";
                    break;
                case "UMB":
                    text = "Umbundu";
                    break;
                case "UND":
                    text = "Undetermined";
                    break;
                case "URD":
                case "UR":
                    text = "Urdu";
                    break;
                case "UZB":
                case "UZ":
                    text = "Uzbek";
                    break;
                case "VAI":
                    text = "Vai";
                    break;
                case "VEN":
                case "VE":
                    text = "Venda";
                    break;
                case "VIE":
                case "VI":
                    text = "Vietnamese";
                    break;
                case "VOL":
                case "VO":
                    text = "Volapük";
                    break;
                case "VOT":
                    text = "Votic";
                    break;
                case "WAK":
                    text = "Wakashan languages";
                    break;
                case "WAL":
                    text = "Walamo";
                    break;
                case "WAR":
                    text = "Waray";
                    break;
                case "WAS":
                    text = "Washo";
                    break;
                case "WEL":
                case "CYM":
                case "CY":
                    text = "Welsh";
                    break;
                case "WEN":
                    text = "Sorbian languages";
                    break;
                case "WLN":
                case "WA":
                    text = "Walloon";
                    break;
                case "WOL":
                case "WO":
                    text = "Wolof";
                    break;
                case "XAL":
                    text = "Kalmyk";
                    break;
                case "XHO":
                case "SH":
                    text = "Xhosa";
                    break;
                case "YAO":
                    text = "Yao (Africa)";
                    break;
                case "YAP":
                    text = "Yapese";
                    break;
                case "YID":
                case "YI":
                    text = "Yiddish";
                    break;
                case "YOR":
                case "YO":
                    text = "Yoruba";
                    break;
                case "YPK":
                    text = "Yupik languages";
                    break;
                case "ZAP":
                    text = "Zapotec";
                    break;
                case "ZEN":
                    text = "Zenaga";
                    break;
                case "ZHA":
                case "ZA":
                    text = "Zhuang";
                    break;
                case "ZND":
                    text = "Zande";
                    break;
                case "ZUL":
                case "ZU":
                    text = "Zulu";
                    break;
                case "ZUN":
                    text = "Zuni";
                    break;
            }

            return text;
        }

        #endregion

        #region Methods to get the ISO code from the language text

        /// <summary> Gets the ISO code from the language text </summary>
        /// <param name="LanguageCheck"> Name of the language to check for ISO code </param>
        /// <returns> Associated ISO code, or an empty string </returns>
        private string Get_Code_By_Language(string LanguageCheck)
        {
            return Get_Code_By_Language_Static(LanguageCheck);
        }

        /// <summary> Gets the ISO code from the language text </summary>
        /// <param name="LanguageCheck"> Name of the language to check for ISO code </param>
        /// <returns> Associated ISO code, or an empty string </returns>
        private static string Get_Code_By_Language_Static(string LanguageCheck)
        {
            string language_upper = LanguageCheck.ToUpper();
            string code = String.Empty;
            switch (language_upper)
            {
                case "AFAR":
                    code = "aar";
                    break;
                case "ABKHAZ":
                    code = "abk";
                    break;
                case "ACHINESE":
                    code = "ace";
                    break;
                case "ACOLI":
                    code = "ach";
                    break;
                case "ADANGME":
                    code = "ada";
                    break;
                case "ADYGEI":
                    code = "ady";
                    break;
                case "AFROASIATIC (OTHER)":
                case "AFROASIATIC":
                    code = "afa";
                    break;
                case "AFRIHILI (ARTIFICIAL LANGUAGE)":
                case "AFRIHILI":
                    code = "afh";
                    break;
                case "AFRIKAANS":
                    code = "afr";
                    break;
                case "ALJAMÍA":
                    code = "ajm";
                    break;
                case "AKAN":
                    code = "aka";
                    break;
                case "AKKADIAN":
                    code = "akk";
                    break;
                case "ALBANIAN":
                    code = "alb";
                    break;
                case "ALEUT":
                    code = "ale";
                    break;
                case "ALGONQUIAN (OTHER)":
                case "ALGONQUIAN":
                    code = "alg";
                    break;
                case "AMHARIC":
                    code = "amh";
                    break;
                case "ENGLISH, OLD (CA. 450-1100)":
                case "ENGLISH, OLD":
                    code = "ang";
                    break;
                case "APACHE LANGUAGES":
                    code = "apa";
                    break;
                case "ARABIC":
                    code = "ara";
                    break;
                case "ARAMAIC":
                    code = "arc";
                    break;
                case "ARAGONESE SPANISH":
                    code = "arg";
                    break;
                case "ARMENIAN":
                    code = "arm";
                    break;
                case "MAPUCHE":
                    code = "arn";
                    break;
                case "ARAPAHO":
                    code = "arp";
                    break;
                case "ARTIFICIAL (OTHER)":
                case "ARTIFICIAL":
                    code = "art";
                    break;
                case "ARAWAK":
                    code = "arw";
                    break;
                case "ASSAMESE":
                    code = "asm";
                    break;
                case "BABLE":
                    code = "ast";
                    break;
                case "ATHAPASCAN (OTHER)":
                case "ATHAPASCAN":
                    code = "ath";
                    break;
                case "AUSTRALIAN LANGUAGES":
                    code = "aus";
                    break;
                case "AVARIC":
                    code = "ava";
                    break;
                case "AVESTAN":
                    code = "ave";
                    break;
                case "AWADHI":
                    code = "awa";
                    break;
                case "AYMARA":
                    code = "aym";
                    break;
                case "AZERBAIJANI":
                    code = "aze";
                    break;
                case "BANDA":
                    code = "bad";
                    break;
                case "BAMILEKE LANGUAGES":
                case "BAMILEKE":
                    code = "bai";
                    break;
                case "BASHKIR":
                    code = "bak";
                    break;
                case "BALUCHI":
                    code = "bal";
                    break;
                case "BAMBARA":
                    code = "bam";
                    break;
                case "BALINESE":
                    code = "ban";
                    break;
                case "BASQUE":
                    code = "baq";
                    break;
                case "BASA":
                    code = "bas";
                    break;
                case "BALTIC (OTHER)":
                case "BALTIC":
                    code = "bat";
                    break;
                case "BEJA":
                    code = "bej";
                    break;
                case "BELARUSIAN":
                    code = "bel";
                    break;
                case "BEMBA":
                    code = "bem";
                    break;
                case "BENGALI":
                    code = "ben";
                    break;
                case "BERBER (OTHER)":
                case "BERBER":
                    code = "ber";
                    break;
                case "BHOJPURI":
                    code = "bho";
                    break;
                case "BIHARI":
                    code = "bih";
                    break;
                case "BIKOL":
                    code = "bik";
                    break;
                case "EDO":
                    code = "bin";
                    break;
                case "BISLAMA":
                    code = "bis";
                    break;
                case "SIKSIKA":
                    code = "bla";
                    break;
                case "BANTU (OTHER)":
                case "BANTU":
                    code = "bnt";
                    break;
                case "BOSNIAN":
                    code = "bos";
                    break;
                case "BRAJ":
                    code = "bra";
                    break;
                case "BRETON":
                    code = "bre";
                    break;
                case "BATAK":
                    code = "btk";
                    break;
                case "BURIAT":
                    code = "bua";
                    break;
                case "BUGIS":
                    code = "bug";
                    break;
                case "BULGARIAN":
                    code = "bul";
                    break;
                case "BURMESE":
                    code = "bur";
                    break;
                case "CADDO":
                    code = "cad";
                    break;
                case "CENTRAL AMERICAN INDIAN (OTHER)":
                case "CENTRAL AMERICAN INDIAN)":
                    code = "cai";
                    break;
                case "KHMER":
                    code = "cam";
                    break;
                case "CARIB":
                    code = "car";
                    break;
                case "CATALAN":
                    code = "cat";
                    break;
                case "CAUCASIAN (OTHER)":
                case "CAUCASIAN":
                    code = "cau";
                    break;
                case "CEBUANO":
                    code = "ceb";
                    break;
                case "CELTIC (OTHER)":
                case "CELTIC":
                    code = "cel";
                    break;
                case "CHAMORRO":
                    code = "cha";
                    break;
                case "CHIBCHA":
                    code = "chb";
                    break;
                case "CHECHEN":
                    code = "che";
                    break;
                case "CHAGATAI":
                    code = "chg";
                    break;
                case "CHINESE":
                    code = "chi";
                    break;
                case "TRUK":
                    code = "chk";
                    break;
                case "MARI":
                    code = "chm";
                    break;
                case "CHINOOK JARGON":
                    code = "chn";
                    break;
                case "CHOCTAW":
                    code = "cho";
                    break;
                case "CHIPEWYAN":
                    code = "chp";
                    break;
                case "CHEROKEE":
                    code = "chr";
                    break;
                case "CHURCH SLAVIC":
                    code = "chu";
                    break;
                case "CHUVASH":
                    code = "chv";
                    break;
                case "CHEYENNE":
                    code = "chy";
                    break;
                case "CHAMIC LANGUAGES":
                    code = "cmc";
                    break;
                case "COPTIC":
                    code = "cop";
                    break;
                case "CORNISH":
                    code = "cor";
                    break;
                case "CORSICAN":
                    code = "cos";
                    break;
                case "CREOLES AND PIDGINS, ENGLISH-BASED (OTHER)":
                case "CREOLES AND PIDGINS, ENGLISH-BASED":
                    code = "cpe";
                    break;
                case "CREOLES AND PIDGINS, FRENCH-BASED (OTHER)":
                case "CREOLES AND PIDGINS, FRENCH-BASED":
                    code = "cpf";
                    break;
                case "CREOLES AND PIDGINS, PORTUGUESE-BASED (OTHER)":
                case "CREOLES AND PIDGINS, PORTUGUESE-BASED":
                    code = "cpp";
                    break;
                case "CREE":
                    code = "cre";
                    break;
                case "CRIMEAN TATAR":
                    code = "crh";
                    break;
                case "CREOLES AND PIDGINS (OTHER)":
                case "CREOLES AND PIDGINS":
                    code = "crp";
                    break;
                case "CUSHITIC (OTHER)":
                case "CUSHITIC":
                    code = "cus";
                    break;
                case "CZECH":
                    code = "cze";
                    break;
                case "DAKOTA":
                    code = "dak";
                    break;
                case "DANISH":
                    code = "dan";
                    break;
                case "DARGWA":
                    code = "dar";
                    break;
                case "DAYAK":
                    code = "day";
                    break;
                case "DELAWARE":
                    code = "del";
                    break;
                case "SLAVE":
                    code = "den";
                    break;
                case "DOGRIB":
                    code = "dgr";
                    break;
                case "DINKA":
                    code = "din";
                    break;
                case "DIVEHI":
                    code = "div";
                    break;
                case "DOGRI":
                    code = "doi";
                    break;
                case "DRAVIDIAN (OTHER)":
                case "DRAVIDIAN":
                    code = "dra";
                    break;
                case "DUALA":
                    code = "dua";
                    break;
                case "DUTCH, MIDDLE (CA. 1050-1350)":
                case "DUTCH, MIDDLE":
                    code = "dum";
                    break;
                case "DUTCH":
                    code = "dut";
                    break;
                case "DYULA":
                    code = "dyu";
                    break;
                case "DZONGKHA":
                    code = "dzo";
                    break;
                case "EFIK":
                    code = "efi";
                    break;
                case "EGYPTIAN":
                    code = "egy";
                    break;
                case "EKAJUK":
                    code = "eka";
                    break;
                case "ELAMITE":
                    code = "elx";
                    break;
                case "ENGLISH":
                    code = "eng";
                    break;
                case "ENGLISH, MIDDLE (1100-1500)":
                case "ENGLISH, MIDDLE":
                    code = "enm";
                    break;
                case "ESKIMO LANGUAGES":
                case "ESKIMO":
                    code = "esk";
                    break;
                case "ESPERANTO":
                    code = "esp";
                    break;
                case "ESTONIAN":
                    code = "est";
                    break;
                case "ETHIOPIC":
                    code = "eth";
                    break;
                case "EWE":
                    code = "ewe";
                    break;
                case "EWONDO":
                    code = "ewo";
                    break;
                case "FANG":
                    code = "fan";
                    break;
                case "FAROESE":
                    code = "far";
                    break;
                case "FANTI":
                    code = "fat";
                    break;
                case "FIJIAN":
                    code = "fij";
                    break;
                case "FINNISH":
                    code = "fin";
                    break;
                case "FINNO-UGRIAN (OTHER)":
                case "FINNO-UGRIAN":
                    code = "fiu";
                    break;
                case "FON":
                    code = "fon";
                    break;
                case "FRENCH":
                    code = "fre";
                    break;
                case "FRISIAN":
                    code = "fri";
                    break;
                case "FRENCH, MIDDLE (CA. 1400-1600)":
                case "FRENCH, MIDDLE)":
                    code = "frm";
                    break;
                case "FRENCH, OLD (CA. 842-1400)":
                case "FRENCH, OLD":
                    code = "fro";
                    break;
                case "FULA":
                    code = "ful";
                    break;
                case "FRIULIAN":
                    code = "fur";
                    break;
                case "GÃ":
                    code = "gaa";
                    break;
                case "SCOTTISH GAELIC":
                    code = "gae";
                    break;
                case "GALICIAN":
                    code = "gag";
                    break;
                case "OROMO":
                    code = "gal";
                    break;
                case "GAYO":
                    code = "gay";
                    break;
                case "GBAYA":
                    code = "gba";
                    break;
                case "GERMANIC (OTHER)":
                case "GERMANIC":
                    code = "gem";
                    break;
                case "GEORGIAN":
                    code = "geo";
                    break;
                case "GERMAN":
                    code = "ger";
                    break;
                case "GILBERTESE":
                    code = "gil";
                    break;
                case "IRISH":
                    code = "gle";
                    break;

                case "MANX":
                    code = "glv";
                    break;
                case "GERMAN, MIDDLE HIGH (CA. 1050-1500)":
                case "GERMAN, MIDDLE HIGH":
                    code = "gmh";
                    break;
                case "GERMAN, OLD HIGH (CA. 750-1050)":
                case "GERMAN, OLD HIGH":
                    code = "goh";
                    break;
                case "GONDI":
                    code = "gon";
                    break;
                case "GORONTALO":
                    code = "gor";
                    break;
                case "GOTHIC":
                    code = "got";
                    break;
                case "GREBO":
                    code = "grb";
                    break;
                case "GREEK, ANCIENT (TO 1453)":
                case "GREEK, ANCIENT":
                    code = "grc";
                    break;
                case "GREEK, MODERN (1453- )":
                case "GREEK, MODERN":
                case "GREEK":
                    code = "gre";
                    break;
                case "GUARANI":
                    code = "grn";
                    break;
                case "GUJARATI":
                    code = "guj";
                    break;
                case "GWICH'IN":
                    code = "gwi";
                    break;
                case "HAIDA":
                    code = "hai";
                    break;
                case "HAITIAN FRENCH CREOLE":
                case "HAITIAN CREOLE":
                case "KREYOL":
                    code = "hat";
                    break;
                case "HAUSA":
                    code = "hau";
                    break;
                case "HAWAIIAN":
                    code = "haw";
                    break;
                case "HEBREW":
                    code = "heb";
                    break;
                case "HERERO":
                    code = "her";
                    break;
                case "HILIGAYNON":
                    code = "hil";
                    break;
                case "HIMACHALI":
                    code = "him";
                    break;
                case "HINDI":
                    code = "hin";
                    break;
                case "HITTITE":
                    code = "hit";
                    break;
                case "HMONG":
                    code = "hmn";
                    break;
                case "HIRI MOTU":
                    code = "hmo";
                    break;
                case "HUNGARIAN":
                    code = "hun";
                    break;
                case "HUPA":
                    code = "hup";
                    break;
                case "IBAN":
                    code = "iba";
                    break;
                case "IGBO":
                    code = "ibo";
                    break;
                case "ICELANDIC":
                    code = "ice";
                    break;
                case "IDO":
                    code = "ido";
                    break;
                case "SICHUAN YI":
                    code = "iii";
                    break;
                case "IJO":
                    code = "ijo";
                    break;
                case "INUKTITUT":
                    code = "iku";
                    break;
                case "INTERLINGUE":
                    code = "ile";
                    break;
                case "ILOKO":
                    code = "ilo";
                    break;
                case "INTERLINGUA (INTERNATIONAL AUXILIARY LANGUAGE ASSOCIATION":
                    code = "ina";
                    break;
                case "INDIC (OTHER)":
                    code = "inc";
                    break;
                case "INDONESIAN":
                    code = "ind";
                    break;
                case "INDO-EUROPEAN (OTHER)":
                    code = "ine";
                    break;
                case "INGUSH":
                    code = "inh";
                    break;
                case "INTERLINGUA (INTERNATIONAL AUXILIARY LANGUAGE ASSOCIATION)":
                    code = "int";
                    break;
                case "INUPIAQ":
                    code = "ipk";
                    break;
                case "IRANIAN (OTHER)":
                case "IRANIAN":
                    code = "ira";
                    break;
                case "IROQUOIAN (OTHER)":
                case "IROQUOIAN":
                    code = "iro";
                    break;
                case "ITALIAN":
                    code = "ita";
                    break;
                case "JAVANESE":
                    code = "jav";
                    break;
                case "JAPANESE":
                    code = "jpn";
                    break;
                case "JUDEO-PERSIAN":
                    code = "jpr";
                    break;
                case "JUDEO-ARABIC":
                    code = "jrb";
                    break;
                case "KARA-KALPAK":
                    code = "kaa";
                    break;
                case "KABYLE":
                    code = "kab";
                    break;
                case "KACHIN":
                    code = "kac";
                    break;
                case "KALÂTDLISUT":
                    code = "kal";
                    break;
                case "KAMBA":
                    code = "kam";
                    break;
                case "KANNADA":
                    code = "kan";
                    break;
                case "KAREN":
                    code = "kar";
                    break;
                case "KASHMIRI":
                    code = "kas";
                    break;
                case "KANURI":
                    code = "kau";
                    break;
                case "KAWI":
                    code = "kaw";
                    break;
                case "KAZAKH":
                    code = "kaz";
                    break;
                case "KABARDIAN":
                    code = "kbd";
                    break;
                case "KHASI":
                    code = "kha";
                    break;
                case "KHOISAN (OTHER)":
                case "KHOISAN":
                    code = "khi";
                    break;
                case "KHOTANESE":
                    code = "kho";
                    break;
                case "KIKUYU":
                    code = "kik";
                    break;
                case "KINYARWANDA":
                    code = "kin";
                    break;
                case "KYRGYZ":
                    code = "kir";
                    break;
                case "KIMBUNDU":
                    code = "kmb";
                    break;
                case "KONKANI":
                    code = "kok";
                    break;
                case "KOMI":
                    code = "kom";
                    break;
                case "KONGO":
                    code = "kon";
                    break;
                case "KOREAN":
                    code = "kor";
                    break;
                case "KUSAIE":
                    code = "kos";
                    break;
                case "KPELLE":
                    code = "kpe";
                    break;
                case "KRU":
                    code = "kro";
                    break;
                case "KURUKH":
                    code = "kru";
                    break;
                case "KUANYAMA":
                    code = "kua";
                    break;
                case "KUMYK":
                    code = "kum";
                    break;
                case "KURDISH":
                    code = "kur";
                    break;
                case "KUTENAI":
                    code = "kut";
                    break;
                case "LADINO":
                    code = "lad";
                    break;
                case "LAHNDA":
                    code = "lah";
                    break;
                case "LAMBA":
                    code = "lam";
                    break;
                case "OCCITAN (POST-1500)":
                    code = "lan";
                    break;
                case "LAO":
                    code = "lao";
                    break;
                case "SAMI":
                    code = "lap";
                    break;
                case "LATIN":
                    code = "lat";
                    break;
                case "LATVIAN":
                    code = "lav";
                    break;
                case "LEZGIAN":
                    code = "lez";
                    break;
                case "LIMBURGISH":
                    code = "lim";
                    break;
                case "LINGALA":
                    code = "lin";
                    break;
                case "LITHUANIAN":
                    code = "lit";
                    break;
                case "MONGO-NKUNDU":
                    code = "lol";
                    break;
                case "LOZI":
                    code = "loz";
                    break;
                case "LETZEBURGESCH":
                    code = "ltz";
                    break;
                case "LUBA-LULUA":
                    code = "lua";
                    break;
                case "LUBA-KATANGA":
                    code = "lub";
                    break;
                case "GANDA":
                    code = "lug";
                    break;
                case "LUISEÑO":
                    code = "lui";
                    break;
                case "LUNDA":
                    code = "lun";
                    break;
                case "LUO (KENYA AND TANZANIA)":
                case "LUO":
                    code = "luo";
                    break;
                case "LUSHAI":
                    code = "lus";
                    break;
                case "MACEDONIAN":
                    code = "mac";
                    break;
                case "MADURESE":
                    code = "mad";
                    break;
                case "MAGAHI":
                    code = "mag";
                    break;
                case "MARSHALLESE":
                    code = "mah";
                    break;
                case "MAITHILI":
                    code = "mai";
                    break;
                case "MAKASAR":
                    code = "mak";
                    break;
                case "MALAYALAM":
                    code = "mal";
                    break;
                case "MANDINGO":
                    code = "man";
                    break;
                case "MAORI":
                    code = "mao";
                    break;
                case "AUSTRONESIAN (OTHER)":
                case "AUSTRONESIAN":
                    code = "map";
                    break;
                case "MARATHI":
                    code = "mar";
                    break;
                case "MASAI":
                    code = "mas";
                    break;
                case "MALAY":
                    code = "may";
                    break;
                case "MANDAR":
                    code = "mdr";
                    break;
                case "MENDE":
                    code = "men";
                    break;
                case "IRISH, MIDDLE (CA. 1100-1550)":
                case "IRISH, MIDDLE":
                    code = "mga";
                    break;
                case "MICMAC":
                    code = "mic";
                    break;
                case "MINANGKABAU":
                    code = "min";
                    break;
                case "MISCELLANEOUS LANGUAGES":
                    code = "mis";
                    break;
                case "MON-KHMER (OTHER)":
                case "MON-KHMER":
                    code = "mkh";
                    break;
                case "MALAGASY":
                    code = "mla";
                    break;
                case "MALTESE":
                    code = "mlt";
                    break;
                case "MANCHU":
                    code = "mnc";
                    break;
                case "MANIPURI":
                    code = "mni";
                    break;
                case "MANOBO LANGUAGES":
                    code = "mno";
                    break;
                case "MOHAWK":
                    code = "moh";
                    break;
                case "MOLDAVIAN":
                    code = "mol";
                    break;
                case "MONGOLIAN":
                    code = "mon";
                    break;
                case "MOORÉ":
                    code = "mos";
                    break;
                case "MULTIPLE LANGUAGES":
                    code = "mul";
                    break;
                case "MUNDA (OTHER)":
                case "MUNDA":
                    code = "mun";
                    break;
                case "CREEK":
                    code = "mus";
                    break;
                case "MARWARI":
                    code = "mwr";
                    break;
                case "MAYAN LANGUAGES":
                    code = "myn";
                    break;
                case "NAHUATL":
                    code = "nah";
                    break;
                case "NORTH AMERICAN INDIAN (OTHER)":
                case "NORTH AMERICAN INDIAN":
                    code = "nai";
                    break;
                case "NEAPOLITAN ITALIAN":
                    code = "nap";
                    break;
                case "NAURU":
                    code = "nau";
                    break;
                case "NAVAJO":
                    code = "nav";
                    break;
                case "NDEBELE (SOUTH AFRICA)":
                    code = "nbl";
                    break;
                case "NDEBELE (ZIMBABWE)":
                    code = "nde";
                    break;
                case "NDONGA":
                    code = "ndo";
                    break;
                case "LOW GERMAN":
                    code = "nds";
                    break;
                case "NEPALI":
                    code = "nep";
                    break;
                case "NEWARI":
                    code = "new";
                    break;
                case "NIAS":
                    code = "nia";
                    break;
                case "NIGER-KORDOFANIAN (OTHER)":
                case "NIGER-KORDOFANIAN":
                    code = "nic";
                    break;
                case "NIUEAN":
                    code = "niu";
                    break;
                case "NORWEGIAN (NYNORSK)":
                case "NYNORSK":
                    code = "nno";
                    break;
                case "NORWEGIAN (BOKMÅL)":
                case "BOKMÅL":
                    code = "nob";
                    break;
                case "NOGAI":
                    code = "nog";
                    break;
                case "OLD NORSE":
                    code = "non";
                    break;
                case "NORWEGIAN":
                    code = "nor";
                    break;
                case "NORTHERN SOTHO":
                    code = "nso";
                    break;
                case "NUBIAN LANGUAGES":
                    code = "nub";
                    break;
                case "NYANJA":
                    code = "nya";
                    break;
                case "NYAMWEZI":
                    code = "nym";
                    break;
                case "NYANKOLE":
                    code = "nyn";
                    break;
                case "NYORO":
                    code = "nyo";
                    break;
                case "NZIMA":
                    code = "nzi";
                    break;
                case "OCCITAN":
                    code = "oci";
                    break;
                case "OJIBWA":
                    code = "oji";
                    break;
                case "ORIYA":
                    code = "ori";
                    break;
                case "OSAGE":
                    code = "osa";
                    break;
                case "OSSETIC":
                    code = "oss";
                    break;
                case "TURKISH, OTTOMAN":
                case "OTTOMAN":
                    code = "ota";
                    break;
                case "OTOMIAN LANGUAGES":
                    code = "oto";
                    break;
                case "PAPUAN (OTHER)":
                case "PAPUAN":
                    code = "paa";
                    break;
                case "PANGASINAN":
                    code = "pag";
                    break;
                case "PAHLAVI":
                    code = "pal";
                    break;
                case "PAMPANGA":
                    code = "pam";
                    break;
                case "PANJABI":
                    code = "pan";
                    break;
                case "PAPIAMENTO":
                    code = "pap";
                    break;
                case "PALAUAN":
                    code = "pau";
                    break;
                case "OLD PERSIAN (CA. 600-400 B.C.)":
                case "OLD PERSIAN":
                    code = "peo";
                    break;
                case "PERSIAN":
                    code = "per";
                    break;
                case "PHILIPPINE (OTHER)":
                case "PHILIPPINE":
                    code = "phi";
                    break;
                case "PHOENICIAN":
                    code = "phn";
                    break;
                case "PALI":
                    code = "pli";
                    break;
                case "POLISH":
                    code = "pol";
                    break;
                case "PONAPE":
                    code = "pon";
                    break;
                case "PORTUGUESE":
                    code = "por";
                    break;
                case "PRAKRIT LANGUAGES":
                case "PRAKRIT":
                    code = "pra";
                    break;
                case "PROVENÇAL (TO 1500)":
                case "PROVENÇAL":
                    code = "pro";
                    break;
                case "PUSHTO":
                    code = "pus";
                    break;
                case "QUECHUA":
                    code = "que";
                    break;
                case "RAJASTHANI":
                    code = "raj";
                    break;
                case "RAPANUI":
                    code = "rap";
                    break;
                case "RAROTONGAN":
                    code = "rar";
                    break;
                case "ROMANCE (OTHER)":
                    code = "roa";
                    break;
                case "RAETO-ROMANCE":
                    code = "roh";
                    break;
                case "ROMANI":
                    code = "rom";
                    break;
                case "ROMANIAN":
                    code = "rum";
                    break;
                case "RUNDI":
                    code = "run";
                    break;
                case "RUSSIAN":
                    code = "rus";
                    break;
                case "SANDAWE":
                    code = "sad";
                    break;
                case "SANGO (UBANGI CREOLE)":
                case "SANGO":
                    code = "sag";
                    break;
                case "YAKUT":
                    code = "sah";
                    break;
                case "SOUTH AMERICAN INDIAN (OTHER)":
                case "SOUTH AMERICAN INDIAN":
                    code = "sai";
                    break;
                case "SALISHAN LANGUAGES":
                    code = "sal";
                    break;
                case "SAMARITAN ARAMAIC":
                    code = "sam";
                    break;
                case "SANSKRIT":
                    code = "san";
                    break;
                case "SAMOAN":
                    code = "sao";
                    break;
                case "SASAK":
                    code = "sas";
                    break;
                case "SANTALI":
                    code = "sat";
                    break;
                case "SERBIAN":
                    code = "scc";
                    break;
                case "SCOTS":
                    code = "sco";
                    break;
                case "CROATIAN":
                    code = "scr";
                    break;
                case "SELKUP":
                    code = "sel";
                    break;
                case "SEMITIC (OTHER)":
                case "SEMITIC":
                    code = "sem";
                    break;
                case "IRISH, OLD (TO 1100)":
                case "IRISH, OLD":
                    code = "sga";
                    break;
                case "SIGN LANGUAGES":
                    code = "sgn";
                    break;
                case "SHAN":
                    code = "shn";
                    break;
                case "SHONA":
                    code = "sho";
                    break;
                case "SIDAMO":
                    code = "sid";
                    break;
                case "SINHALESE":
                    code = "sin";
                    break;
                case "SIOUAN (OTHER)":
                case "SIOUAN":
                    code = "sio";
                    break;
                case "SINO-TIBETAN (OTHER)":
                case "SINO-TIBETAN":
                    code = "sit";
                    break;
                case "SLAVIC (OTHER)":
                case "SLAVIC":
                    code = "sla";
                    break;
                case "SLOVAK":
                    code = "slo";
                    break;
                case "SLOVENIAN":
                    code = "slv";
                    break;
                case "SOUTHERN SAMI":
                    code = "sma";
                    break;
                case "NORTHERN SAMI":
                    code = "sme";
                    break;
                case "LULE SAMI":
                    code = "smj";
                    break;
                case "INARI SAMI":
                    code = "smn";
                    break;
                case "SKOLT SAMI":
                    code = "sms";
                    break;
                case "SINDHI":
                    code = "snd";
                    break;
                case "SONINKE":
                    code = "snk";
                    break;
                case "SOGDIAN":
                    code = "sog";
                    break;
                case "SOMALI":
                    code = "som";
                    break;
                case "SONGHAI":
                    code = "son";
                    break;
                case "SOTHO":
                    code = "sot";
                    break;
                case "SPANISH":
                    code = "spa";
                    break;
                case "SARDINIAN":
                    code = "srd";
                    break;
                case "SERER":
                    code = "srr";
                    break;
                case "NILO-SAHARAN (OTHER)":
                case "NILO-SAHARAN":
                    code = "ssa";
                    break;
                case "SWAZI":
                    code = "ssw";
                    break;
                case "SUKUMA":
                    code = "suk";
                    break;
                case "SUNDANESE":
                    code = "sun";
                    break;
                case "SUSU":
                    code = "sus";
                    break;
                case "SUMERIAN":
                    code = "sux";
                    break;
                case "SWAHILI":
                    code = "swa";
                    break;
                case "SWEDISH":
                    code = "swe";
                    break;
                case "SYRIAC":
                    code = "syr";
                    break;
                case "TAGALOG":
                    code = "tag";
                    break;
                case "TAHITIAN":
                    code = "tah";
                    break;
                case "TAI (OTHER)":
                case "TAI":
                    code = "tai";
                    break;
                case "TAJIK":
                    code = "taj";
                    break;
                case "TAMIL":
                    code = "tam";
                    break;
                case "TATAR":
                    code = "tar";
                    break;
                case "TELUGU":
                    code = "tel";
                    break;
                case "TEMNE":
                    code = "tem";
                    break;
                case "TERENA":
                    code = "ter";
                    break;
                case "TETUM":
                    code = "tet";
                    break;
                case "THAI":
                    code = "tha";
                    break;
                case "TIBETAN":
                    code = "tib";
                    break;
                case "TIGRÉ":
                    code = "tig";
                    break;
                case "TIGRINYA":
                    code = "tir";
                    break;
                case "TIV":
                    code = "tiv";
                    break;
                case "TOKELAUAN":
                    code = "tkl";
                    break;
                case "TLINGIT":
                    code = "tli";
                    break;
                case "TAMASHEK":
                    code = "tmh";
                    break;
                case "TONGA (NYASA)":
                case "TONGA":
                    code = "tog";
                    break;
                case "TONGAN":
                    code = "ton";
                    break;
                case "TOK PISIN":
                    code = "tpi";
                    break;
                case "TSIMSHIAN":
                    code = "tsi";
                    break;
                case "TSWANA":
                    code = "tsn";
                    break;
                case "TSONGA":
                    code = "tso";
                    break;
                case "TURKMEN":
                    code = "tuk";
                    break;
                case "TUMBUKA":
                    code = "tum";
                    break;
                case "TUPI LANGUAGES":
                    code = "tup";
                    break;
                case "TURKISH":
                    code = "tur";
                    break;
                case "ALTAIC (OTHER)":
                case "ALTAIC":
                    code = "tut";
                    break;
                case "TUVALUAN":
                    code = "tvl";
                    break;
                case "TWI":
                    code = "twi";
                    break;
                case "TUVINIAN":
                    code = "tyv";
                    break;
                case "UDMURT":
                    code = "udm";
                    break;
                case "UGARITIC":
                    code = "uga";
                    break;
                case "UIGHUR":
                    code = "uig";
                    break;
                case "UKRAINIAN":
                    code = "ukr";
                    break;
                case "UMBUNDU":
                    code = "umb";
                    break;
                case "UNDETERMINED":
                    code = "und";
                    break;
                case "URDU":
                    code = "urd";
                    break;
                case "UZBEK":
                    code = "uzb";
                    break;
                case "VAI":
                    code = "vai";
                    break;
                case "VENDA":
                    code = "ven";
                    break;
                case "VIETNAMESE":
                    code = "vie";
                    break;
                case "VOLAPÜK":
                    code = "vol";
                    break;
                case "VOTIC":
                    code = "vot";
                    break;
                case "WAKASHAN LANGUAGES":
                    code = "wak";
                    break;
                case "WALAMO":
                    code = "wal";
                    break;
                case "WARAY":
                    code = "war";
                    break;
                case "WASHO":
                    code = "was";
                    break;
                case "WELSH":
                    code = "wel";
                    break;
                case "SORBIAN LANGUAGES":
                    code = "wen";
                    break;
                case "WALLOON":
                    code = "wln";
                    break;
                case "WOLOF":
                    code = "wol";
                    break;
                case "KALMYK":
                    code = "xal";
                    break;
                case "XHOSA":
                    code = "xho";
                    break;
                case "YAO (AFRICA)":
                case "YAO":
                    code = "yao";
                    break;
                case "YAPESE":
                    code = "yap";
                    break;
                case "YIDDISH":
                    code = "yid";
                    break;
                case "YORUBA":
                    code = "yor";
                    break;
                case "YUPIK LANGUAGES":
                    code = "ypk";
                    break;
                case "ZAPOTEC":
                    code = "zap";
                    break;
                case "ZENAGA":
                    code = "zen";
                    break;
                case "ZHUANG":
                    code = "zha";
                    break;
                case "ZANDE":
                    code = "znd";
                    break;
                case "ZULU":
                    code = "zul";
                    break;
                case "ZUNI":
                    code = "zun";
                    break;
            }

            return code;
        }

        #endregion

        #region Methods to get the script text from the ISO code

        /// <summary> Get the script text from the ISO code </summary>
        /// <param name="CodeCheck"> ISO code to convert to script name </param>
        /// <returns> The associated script name or an empty string </returns>
        public string Get_Script_By_Code(string CodeCheck)
        {
            return Get_Script_By_Code_Static(CodeCheck);
        }

        /// <summary> Get the script text from the ISO code </summary>
        /// <param name="CodeCheck"> ISO code to convert to script name </param>
        /// <returns> The associated script name or an empty string </returns>
        public static string Get_Script_By_Code_Static(string CodeCheck)
        {
            string code_upper = CodeCheck.ToUpper();
            switch (code_upper)
            {
                case "ADLM": return "Adlam";
                case "AFAK": return "Afaka";
                case "AGHB": return "Caucasian Albanian";
                case "AHOM": return "Ahom";
                case "ARAB": return "Arabic";
                case "ARAN": return "Arabic (Nastaliq variant)";
                case "ARMI": return "Imperial Aramaic";
                case "ARMN": return "Armenian";
                case "AVST": return "Avestan";
                case "BALI": return "Balinese";
                case "BAMU": return "Bamum";
                case "BASS": return "Bassa Vah";
                case "BATK": return "Batak";
                case "BENG": return "Bengali";
                case "BHKS": return "Bhaiksuki";
                case "BLIS": return "Blissymbols";
                case "BOPO": return "Bopomofo";
                case "BRAH": return "Brahmi";
                case "BRAI": return "Braille";
                case "BUGI": return "Buginese";
                case "BUHD": return "Buhid";
                case "CAKM": return "Chakma";
                case "CANS": return "Unified Canadian Aboriginal Syllabics";
                case "CARI": return "Carian";
                case "CHAM": return "Cham";
                case "CHER": return "Cherokee";
                case "CIRT": return "Cirth";
                case "COPT": return "Coptic";
                case "CPMN": return "Cypro-Minoan";
                case "CPRT": return "Cypriot syllabary";
                case "CYRL": return "Cyrillic";
                case "CYRS": return "Cyrillic (Old Church Slavonic variant)";
                case "DEVA": return "Devanagari (Nagari)";
                case "DOGR": return "Dogra";
                case "DSRT": return "Deseret (Mormon)";
                case "DUPL": return "Duployan shorthand";
                case "EGYD": return "Egyptian demotic";
                case "EGYH": return "Egyptian hieratic";
                case "EGYP": return "Egyptian hieroglyphs";
                case "ELBA": return "Elbasan";
                case "ETHI": return "Ethiopic";
                case "GEOK": return "Khutsuri";
                case "GEOR": return "Georgian";
                case "GLAG": return "Glagolitic";
                case "GONG": return "Gunjala Gondi";
                case "GONM": return "Masaram Gondi";
                case "GOTH": return "Gothic";
                case "GRAN": return "Grantha";
                case "GREK": return "Greek";
                case "GUJR": return "Gujarati";
                case "GURU": return "Gurmukhi";
                case "HANB": return "Han with Bopomofo";
                case "HANG": return "Hangul";
                case "HANI": return "Han";
                case "HANO": return "Hanunoo";
                case "HANS": return "Han (Simplified variant)";
                case "HANT": return "Han (Traditional variant)";
                case "HATR": return "Hatran";
                case "HEBR": return "Hebrew";
                case "HIRA": return "Hiragana";
                case "HLUW": return "Anatolian Hieroglyphs";
                case "HMNG": return "Pahawh Hmong";
                case "HMNP": return "Nyiakeng Puachue Hmong";
                case "HRKT": return "Japanese syllabaries";
                case "HUNG": return "Old Hungarian";
                case "INDS": return "Indus";
                case "ITAL": return "Old Italic";
                case "JAMO": return "Jamo";
                case "JAVA": return "Javanese";
                case "JPAN": return "Japanese";
                case "JURC": return "Jurchen";
                case "KALI": return "Kayah Li";
                case "KANA": return "Katakana";
                case "KHAR": return "Kharoshthi";
                case "KHMR": return "Khmer";
                case "KHOJ": return "Khojki";
                case "KITL": return "Khitan large script";
                case "KITS": return "Khitan small script";
                case "KNDA": return "Kannada";
                case "KORE": return "Korean";
                case "KPEL": return "Kpelle";
                case "KTHI": return "Kaithi";
                case "LANA": return "Tai Tham";
                case "LAOO": return "Lao";
                case "LATF": return "Latin (Fraktur variant)";
                case "LATG": return "Latin (Gaelic variant)";
                case "LATN": return "Latin";
                case "LEKE": return "Leke";
                case "LEPC": return "Lepcha";
                case "LIMB": return "Limbu";
                case "LINA": return "Linear A";
                case "LINB": return "Linear B";
                case "LISU": return "Lisu";
                case "LOMA": return "Loma";
                case "LYCI": return "Lycian";
                case "LYDI": return "Lydian";
                case "MAHJ": return "Mahajani";
                case "MAKA": return "Makasar";
                case "MAND": return "Mandaic";
                case "MANI": return "Manichaean";
                case "MARC": return "Marchen";
                case "MAYA": return "Mayan hieroglyphs";
                case "MEDF": return "Medefaidrin";
                case "MEND": return "Mende Kikakui";
                case "MERC": return "Meroitic Cursive";
                case "MERO": return "Meroitic Hieroglyphs";
                case "MLYM": return "Malayalam";
                case "MODI": return "Modi";
                case "MONG": return "Mongolian";
                case "MOON": return "Moon";
                case "MROO": return "Mro";
                case "MTEI": return "Meitei Mayek";
                case "MULT": return "Multani";
                case "MYMR": return "Myanmar";
                case "NARB": return "Old North Arabian";
                case "NBAT": return "Nabataean";
                case "NEWA": return "Newa";
                case "NKDB": return "Naxi Dongba";
                case "NKGB": return "Naxi Geba";
                case "NKOO": return "N’Ko";
                case "NSHU": return "Nüshu";
                case "OGAM": return "Ogham";
                case "OLCK": return "Ol Chiki";
                case "ORKH": return "Old Turkic";
                case "ORYA": return "Oriya";
                case "OSGE": return "Osage";
                case "OSMA": return "Osmanya";
                case "PALM": return "Palmyrene";
                case "PAUC": return "Pau Cin Hau";
                case "PERM": return "Old Permic";
                case "PHAG": return "Phags-pa";
                case "PHLI": return "Inscriptional Pahlavi";
                case "PHLP": return "Psalter Pahlavi";
                case "PHLV": return "Book Pahlavi";
                case "PHNX": return "Phoenician";
                case "PIQD": return "Klingon";
                case "PLRD": return "Miao";
                case "PRTI": return "Inscriptional Parthian";
                case "RJNG": return "Rejang";
                case "ROHG": return "Hanifi Rohingya";
                case "RORO": return "Rongorongo";
                case "RUNR": return "Runic";
                case "SAMR": return "Samaritan";
                case "SARA": return "Sarati";
                case "SARB": return "Old South Arabian";
                case "SAUR": return "Saurashtra";
                case "SGNW": return "SignWriting";
                case "SHAW": return "Shavian";
                case "SHRD": return "Sharada";
                case "SHUI": return "Shuishu";
                case "SIDD": return "Siddham";
                case "SIND": return "Khudawadi";
                case "SINH": return "Sinhala";
                case "SOGD": return "Sogdian";
                case "SOGO": return "Old Sogdian";
                case "SORA": return "Sora Sompeng";
                case "SOYO": return "Soyombo";
                case "SUND": return "Sundanese";
                case "SYLO": return "Syloti Nagri";
                case "SYRC": return "Syriac";
                case "SYRE": return "Syriac (Estrangelo variant)";
                case "SYRJ": return "Syriac (Western variant)";
                case "SYRN": return "Syriac (Eastern variant)";
                case "TAGB": return "Tagbanwa";
                case "TAKR": return "Takri";
                case "TALE": return "Tai Le";
                case "TALU": return "New Tai Lue";
                case "TAML": return "Tamil";
                case "TANG": return "Tangut";
                case "TAVT": return "Tai Viet";
                case "TELU": return "Telugu";
                case "TENG": return "Tengwar";
                case "TFNG": return "Tifinagh";
                case "TGLG": return "Tagalog";
                case "THAA": return "Thaana";
                case "THAI": return "Thai";
                case "TIBT": return "Tibetan";
                case "TIRH": return "Tirhuta";
                case "UGAR": return "Ugaritic";
                case "VAII": return "Vai";
                case "VISP": return "Visible Speech";
                case "WARA": return "Warang Citi";
                case "WCHO": return "Wancho";
                case "WOLE": return "Woleai";
                case "XPEO": return "Old Persian";
                case "XSUX": return "Cuneiform";
                case "YIII": return "Yi";
                case "ZMTH": return "Mathematical notation";
                case "ZSYE": return "Symbols (Emoji variant)";
                case "ZSYM": return "Symbols";
            }

            return String.Empty;
        }

        #endregion

        #region Methods to get the ISO code from the script text

        /// <summary> Gets the ISO code from the script text </summary>
        /// <param name="ScriptCheck"> Name of the script to check for ISO code </param>
        /// <returns> Associated ISO code, or an empty string </returns>
        private string Get_Code_By_Script(string ScriptCheck)
        {
            return Get_Code_By_Script_Static(ScriptCheck);
        }

        /// <summary> Gets the ISO code from the script text </summary>
        /// <param name="ScriptCheck"> Name of the script to check for ISO code </param>
        /// <returns> Associated ISO code, or an empty string </returns>
        private static string Get_Code_By_Script_Static(string ScriptCheck)
        {
            string script_lower = ScriptCheck.ToLower();
            switch (script_lower)
            {
                case "adlam": return "Adlm";
                case "afaka": return "Afak";
                case "aghbanien": return "Aghb";
                case "caucasian albanian": return "Aghb";
                case "albanian": return "Aghb";
                case "âhom": return "Ahom";
                case "ahom": return "Ahom";
                case "tai ahom": return "Ahom";
                case "arabe": return "Arab";
                case "arabic": return "Arab";
                case "arabe (variante nastalique)": return "Aran";
                case "arabic (nastaliq variant)": return "Aran";
                case "araméen impérial": return "Armi";
                case "imperial aramaic": return "Armi";
                case "armenian": return "Armn";
                case "arménien": return "Armn";
                case "avestan": return "Avst";
                case "avestique": return "Avst";
                case "balinais": return "Bali";
                case "balinese": return "Bali";
                case "bamoum": return "Bamu";
                case "bamum": return "Bamu";
                case "bassa": return "Bass";
                case "bassa Vah": return "Bass";
                case "batak": return "Batk";
                case "batik": return "Batk";
                case "bengali": return "Beng";
                case "bengalî": return "Beng";
                case "bangla": return "Beng";
                case "bhaiksuki": return "Bhks";
                case "bhaïksukî": return "Bhks";
                case "blissymbols": return "Blis";
                case "symboles bliss": return "Blis";
                case "bopomofo": return "Bopo";
                case "brahma": return "Brah";
                case "brahmi": return "Brah";
                case "braille": return "Brai";
                case "bouguis": return "Bugi";
                case "buginese": return "Bugi";
                case "bouhide": return "Buhd";
                case "buhid": return "Buhd";
                case "chakma": return "Cakm";
                case "syllabaire autochtone canadien unifié": return "Cans";
                case "unified aanadian aboriginal syllabics": return "Cans";
                case "carian": return "Cari";
                case "carien": return "Cari";
                case "cham": return "Cham";
                case "tcham": return "Cham";
                case "cherokee": return "Cher";
                case "tchérokî": return "Cher";
                case "cirth": return "Cirt";
                case "copte": return "Copt";
                case "coptic": return "Copt";
                case "cypro-minoan": return "Cpmn";
                case "syllabaire chypro-minoen": return "Cpmn";
                case "cypriot syllabary": return "Cprt";
                case "cypriot": return "Cprt";
                case "syllabaire chypriote": return "Cprt";
                case "cyrillic": return "Cyrl";
                case "cyrillique": return "Cyrl";
                case "cyrillic (old church slavonic variant)": return "Cyrs";
                case "cyrillique (variante slavonne)": return "Cyrs";
                case "dévanâgarî": return "Deva";
                case "devanagari": return "Deva";
                case "nagari": return "Deva";
                case "dogra": return "Dogr";
                case "deseret": return "Dsrt";
                case "déseret": return "Dsrt";
                case "mormon": return "Dsrt";
                case "duployan": return "Dupl";
                case "duployan shorthand": return "Dupl";
                case "duployan stenography": return "Dupl";
                case "sténographie duployé": return "Dupl";
                case "duployé": return "Dupl";
                case "démotique égyptien": return "Egyd";
                case "egyptian demotic": return "Egyd";
                case "egyptian hieratic": return "Egyh";
                case "hiératique égyptien": return "Egyh";
                case "egyptian hieroglyphs": return "Egyp";
                case "hiéroglyphes égyptiens": return "Egyp";
                case "elbasan": return "Elba";
                case "ethiopic": return "Ethi";
                case "éthiopien": return "Ethi";
                case "guèze": return "Ethi";
                case "ge'ez": return "Ethi";
                case "khoutsouri (assomtavrouli et nouskhouri)": return "Geok";
                case "khoutsouri": return "Geok";
                case "assomtavrouli": return "Geok";
                case "nouskhouri": return "Geok";
                case "khutsuri (asomtavruli and nuskhuri)": return "Geok";
                case "khutsuri": return "Geok";
                case "asomtavruli": return "Geok";
                case "nuskhuri": return "Geok";
                case "georgian (mkhedruli and mtavruli)": return "Geor";
                case "georgian": return "Geor";
                case "mkhedruli": return "Geor";
                case "mtavruli": return "Geor";
                case "géorgien (mkhédrouli et mtavrouli)": return "Geor";
                case "géorgien": return "Geor";
                case "mkhédrouli": return "Geor";
                case "mtavrouli": return "Geor";
                case "glagolitic": return "Glag";
                case "glagolitique": return "Glag";
                case "gunjala gondi": return "Gong";
                case "gunjala gondî": return "Gong";
                case "masaram gondi": return "Gonm";
                case "masaram gondî": return "Gonm";
                case "gothic": return "Goth";
                case "gotique": return "Goth";
                case "grantha": return "Gran";
                case "grec": return "Grek";
                case "greek": return "Grek";
                case "goudjarâtî": return "Gujr";
                case "gujrâtî": return "Gujr";
                case "gujarati": return "Gujr";
                case "gourmoukhî": return "Guru";
                case "gurmukhi": return "Guru";
                case "han avec bopomofo": return "Hanb";
                case "han with bopomofo": return "Hanb";
                case "hangul": return "Hang";
                case "hangûl": return "Hang";
                case "hang ": return "Hang";
                case "hangeul": return "Hang";
                case "han": return "Hani";
                case "hanzi": return "Hani";
                case "kanji": return "Hani";
                case "hanja": return "Hani";
                case "idéogrammes han": return "Hani";
                case "idéogrammes han (sinogrammes)": return "Hani";
                case "hanounóo": return "Hano";
                case "hanunoo": return "Hano";
                case "hanunóo": return "Hano";
                case "han (simplified variant)": return "Hans";
                case "idéogrammes han (variante simplifiée)": return "Hans";
                case "han (traditional variant)": return "Hant";
                case "idéogrammes han (variante traditionnelle)": return "Hant";
                case "hatran": return "Hatr";
                case "hatrénien": return "Hatr";
                case "hébreu": return "Hebr";
                case "hebrew": return "Hebr";
                case "hiragana": return "Hira";
                case "anatolian hieroglyphs": return "Hluw";
                case "luwian hieroglyphs": return "Hluw";
                case "hittite hieroglyphs": return "Hluw";
                case "hiéroglyphes anatoliens": return "Hluw";
                case "hiéroglyphes louvites": return "Hluw";
                case "hiéroglyphes hittites": return "Hluw";
                case "pahawh hmong": return "Hmng";
                case "nyiakeng puachue hmong": return "Hmnp";
                case "japanese syllabaries": return "Hrkt";
                case "syllabaires japonais": return "Hrkt";
                case "old hungarian": return "Hung";
                case "hungarian runic": return "Hung";
                case "runes hongroises": return "Hung";
                case "ancien hongrois": return "Hung";
                case "indus": return "Inds";
                case "harappan": return "Inds";
                case "ancien italique": return "Ital";
                case "étrusque": return "Ital";
                case "osque": return "Ital";
                case "old italic": return "Ital";
                case "etruscan": return "Ital";
                case "oscan": return "Ital";
                case "jamo": return "Jamo";
                case "javanais": return "Java";
                case "javanese": return "Java";
                case "japanese": return "Jpan";
                case "japonais": return "Jpan";
                case "jurchen": return "Jurc";
                case "kayah li": return "Kali";
                case "katakana": return "Kana";
                case "kharochthî": return "Khar";
                case "kharoshthi": return "Khar";
                case "khmer": return "Khmr";
                case "khojki": return "Khoj";
                case "khojkî": return "Khoj";
                case "grande écriture khitan": return "Kitl";
                case "khitan large script": return "Kitl";
                case "khitan small script": return "Kits";
                case "petite écriture khitan": return "Kits";
                case "kannada": return "Knda";
                case "kannara": return "Knda";
                case "canara": return "Knda";
                case "coréen": return "Kore";
                case "korean": return "Kore";
                case "kpelle": return "Kpel";
                case "kpèllé": return "Kpel";
                case "kaithi": return "Kthi";
                case "kaithî": return "Kthi";
                case "yai tham": return "Lana";
                case "taï tham": return "Lana";
                case "lanna": return "Lana";
                case "lao": return "Laoo";
                case "laotien": return "Laoo";
                case "latin (fraktur variant)": return "Latf";
                case "latin (variante brisée)": return "Latf";
                case "latin (gaelic variant)": return "Latg";
                case "latin (variante gaélique)": return "Latg";
                case "latin": return "Latn";
                case "Leke": return "Leke";
                case "léké": return "Leke";
                case "Lepcha (Róng)": return "Lepc";
                case "lepcha (róng)": return "Lepc";
                case "limbou": return "Limb";
                case "limbu": return "Limb";
                case "linéaire a": return "Lina";
                case "linear a": return "Lina";
                case "linéaire b": return "Linb";
                case "linear b": return "Linb";
                case "fraser": return "Lisu";
                case "lisu": return "Lisu";
                case "loma": return "Loma";
                case "Lycian": return "Lyci";
                case "lycien": return "Lyci";
                case "Lydian": return "Lydi";
                case "lydien": return "Lydi";
                case "mahajani": return "Mahj";
                case "mahâjanî": return "Mahj";
                case "makasar": return "Maka";
                case "makassar": return "Maka";
                case "mandaic": return "Mand";
                case "mandaean": return "Mand";
                case "mandéen": return "Mand";
                case "manichaean": return "Mani";
                case "manichéen": return "Mani";
                case "marchen": return "Marc";
                case "hiéroglyphes mayas": return "Maya";
                case "mayan hieroglyphs": return "Maya";
                case "medefaidrin": return "Medf";
                case "oberi okaime": return "Medf";
                case "médéfaïdrine": return "Medf";
                case "mende kikakui": return "Mend";
                case "mendé kikakui": return "Mend";
                case "cursif méroïtique": return "Merc";
                case "meroitic bursive": return "Merc";
                case "hiéroglyphes méroïtiques": return "Mero";
                case "meroitic hieroglyphs": return "Mero";
                case "malayalam": return "Mlym";
                case "malayâlam": return "Mlym";
                case "modî": return "Modi";
                case "modi": return "Modi";
                case "mo": return "Modi";
                case "mongol": return "Mong";
                case "mongolian": return "Mong";
                case "écriture Moon": return "Moon";
                case "moon": return "Moon";
                case "moon code": return "Moon";
                case "moon script": return "Moon";
                case "moon type": return "Moon";
                case "mro": return "Mroo";
                case "mru": return "Mroo";
                case "meitei mayek": return "Mtei";
                case "meithei mayek": return "Mtei";
                case "meetei mayek": return "Mtei";
                case "meitei": return "Mtei";
                case "meithei": return "Mtei";
                case "meetei": return "Mtei";
                case "multani": return "Mult";
                case "multanî": return "Mult";
                case "birman": return "Mymr";
                case "myanmar": return "Mymr";
                case "burmese": return "Mymr";
                case "nord-arabique": return "Narb";
                case "old north arabian": return "Narb";
                case "ancient north arabian": return "Narb";
                case "nabataean": return "Nbat";
                case "nabatéen": return "Nbat";
                case "newa": return "Newa";
                case "newar": return "Newa";
                case "newari": return "Newa";
                case "néwa": return "Newa";
                case "néwar": return "Newa";
                case "néwari": return "Newa";
                case "naxi dongba": return "Nkdb";
                case "naxi tomba": return "Nkdb";
                case "naxi geba": return "Nkgb";
                case "nakhi geba": return "Nkgb";
                case "n’ko": return "Nkoo";
                case "nüshu": return "Nshu";
                case "ogam": return "Ogam";
                case "ogham": return "Ogam";
                case "ol chiki": return "Olck";
                case "santali": return "Olck";
                case "ol cemet": return "Olck";
                case "ol tchiki": return "Olck";
                case "old turkic, Orkhon Runic": return "Orkh";
                case "orkhon runic": return "Orkh";
                case "orkhon": return "Orkh";
                case "oriya": return "Orya";
                case "odia": return "Orya";
                case "oriyâ": return "Orya";
                case "osage": return "Osge";
                case "osmanais": return "Osma";
                case "osmanya": return "Osma";
                case "palmyrene": return "Palm";
                case "palmyrénien": return "Palm";
                case "paou chin haou": return "Pauc";
                case "pau cin hau": return "Pauc";
                case "ancien permien": return "Perm";
                case "old permic": return "Perm";
                case "’phags pa": return "Phag";
                case "phags-pa": return "Phag";
                case "inscriptional pahlavi": return "Phli";
                case "pehlevi des inscriptions": return "Phli";
                case "pehlevi des psautiers": return "Phlp";
                case "psalter pahlavi": return "Phlp";
                case "book pahlavi": return "Phlv";
                case "pehlevi des livres": return "Phlv";
                case "phénicien": return "Phnx";
                case "phoenician": return "Phnx";
                case "kli piqad": return "Piqd";
                case "piqad du kli": return "Piqd";
                case "klingon": return "Piqd";
                case "pollard": return "Plrd";
                case "miao": return "Plrd";
                case "inscriptional parthian": return "Prti";
                case "parthe des inscriptions": return "Prti";
                case "redjang": return "Rjng";
                case "rejang": return "Rjng";
                case "kaganga": return "Rjng";
                case "hanifi rohingya": return "Rohg";
                case "rongorongo": return "Roro";
                case "runic": return "Runr";
                case "runique": return "Runr";
                case "samaritain": return "Samr";
                case "samaritan": return "Samr";
                case "sarati": return "Sara";
                case "old south arabian": return "Sarb";
                case "sud - arabique, himyarite": return "Sarb";
                case "saurachtra": return "Saur";
                case "saurashtra": return "Saur";
                case "signécriture": return "Sgnw";
                case "signWriting": return "Sgnw";
                case "shavian": return "Shaw";
                case "shavien": return "Shaw";
                case "shaw": return "Shaw";
                case "charada": return "Shrd";
                case "shard": return "Shrd";
                case "sharada" : return "Shrd";
                case "shuishu": return "Shui";
                case "siddham": return "Sidd";
                case "siddha" : return "Sidd";
                case "sindhî": return "Sind";
                case "sindhi": return "Sind";
                case "khoudawadî": return "Sind";
                case "khudawadi": return "Sind";
                case "khoudawadî, sindhî": return "Sind";
                case "khudawadi, sindhi": return "Sind";
                case "singhalais": return "Sinh";
                case "sinhala": return "Sinh";
                case "sogdian": return "Sogd";
                case "sogdien": return "Sogd";
                case "ancien sogdien": return "Sogo";
                case "old sogdian": return "Sogo";
                case "sora sompeng": return "Sora";
                case "soyombo": return "Soyo";
                case "sundanais": return "Sund";
                case "sundanese": return "Sund";
                case "syloti nagri": return "Sylo";
                case "sylotî nâgrî": return "Sylo";
                case "syriac": return "Syrc";
                case "syriaque": return "Syrc";
                case "syriac (estrangelo variant)": return "Syre";
                case "syriaque (variante estranghélo)": return "Syre";
                case "syriac (western variant)": return "Syrj";
                case "syriaque (variante occidentale)": return "Syrj";
                case "syriac (eastern variant)": return "Syrn";
                case "syriaque (variante orientale)": return "Syrn";
                case "tagbanoua": return "Tagb";
                case "tagbanwa": return "Tagb";
                case "tâkrî": return "Takr";
                case "takri" : return "Takr";
                case "tai le": return "Tale";
                case "taï-le": return "Tale";
                case "new tai lue": return "Talu";
                case "nouveau taï-lue": return "Talu";
                case "tamil": return "Taml";
                case "tamoul": return "Taml";
                case "tangoute": return "Tang";
                case "tangut": return "Tang";
                case "tai viet": return "Tavt";
                case "taï viêt": return "Tavt";
                case "télougou": return "Telu";
                case "telugu": return "Telu";
                case "tengwar": return "Teng";
                case "tifinagh": return "Tfng";
                case "tifinagh (berber)": return "Tfng";
                case "tifinagh (berbère)": return "Tfng";
                case "tagal": return "Tglg";
                case "tagalog": return "Tglg";
                case "tagal (baybayin, alibata)": return "Tglg";
                case "tagalog (baybayin, alibata)": return "Tglg";
                case "thaana": return "Thaa";
                case "thâna": return "Thaa";
                case "thai": return "Thai";
                case "thaï": return "Thai";
                case "tibétain": return "Tibt";
                case "tibetan": return "Tibt";
                case "tirhouta": return "Tirh";
                case "tirhuta": return "Tirh";
                case "ougaritique": return "Ugar";
                case "ugaritic": return "Ugar";
                case "vai": return "Vaii";
                case "vaï": return "Vaii";
                case "parole visible": return "Visp";
                case "visible speech": return "Visp";
                case "warang citi": return "Wara";
                case "warang citi (Varang Kshiti)": return "Wara";
                case "wancho": return "Wcho";
                case "wantcho": return "Wcho";
                case "woleai": return "Wole";
                case "woléaï": return "Wole";
                case "cunéiforme persépolitain": return "Xpeo";
                case "old persian": return "Xpeo";
                case "cuneiform, sumero - akkadian": return "Xsux";
                case "cunéiforme suméro-akkadien": return "Xsux";
                case "yi": return "Yiii";
                case "mathematical notation": return "Zmth";
                case "notation mathématique": return "Zmth";
                case "symboles (variante émoji)": return "Zsye";
                case "symbols (emoji variant)": return "Zsye";
                case "symboles": return "Zsym";
                case "symbols": return "Zsym";
            }

            return String.Empty;
        }

        #endregion
    }
}