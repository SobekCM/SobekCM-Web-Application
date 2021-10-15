﻿#region Using directives

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using SobekCM.Resource_Object.MARC;

#endregion

namespace SobekCM.Resource_Object.Bib_Info
{
    /// <summary> Title type enumeration </summary>
    public enum Title_Type_Enum
    {
        /// <summary> Type of title is unspecified (default) </summary>
        UNSPECIFIED = -1,

        /// <summary> Abbreviated title is a shorter version of the main title </summary>
        Abbreviated = 1,

        /// <summary> Translated title is the main title translated into a different language </summary>
        Translated,

        /// <summary> Alternative title refers to any title which can alternatively describe 
        /// the item, and cannot be described with any of the other title types  </summary>
        Alternative,

        /// <summary> Uniform title is a title used to describe a set of different items in a uniform way </summary>
        Uniform,

        /// <summary> Course title related to this material </summary>
        Course
    }

    /// <summary>A word, phrase, character, or group of characters, normally appearing in a resource, that names it or the work contained in it.</summary>
    [Serializable]
    public class Title_Info : XML_Node_Base_Type
    {
        private string authority;
        private string displayLabel;
        private string language;
        private string nonSort;
        private List<string> partNames;
        private List<string> partNumbers;
        private string subtitle;
        private string title;
        private Title_Type_Enum titleType;

        /// <summary> Constructor for a new instance of the Title_Info class </summary>
        public Title_Info()
        {
            titleType = Title_Type_Enum.UNSPECIFIED;
        }

        /// <summary> Constructor for a new instance of the Title_Info class </summary>
        /// <param name="Title">Text for this title</param>
        /// <param name="Type">Title type</param>
        public Title_Info(string Title, Title_Type_Enum Type)
        {
            titleType = Type;
            title = Title;

            partNames = new List<string>();
            partNumbers = new List<string>();
        }

        /// <summary> Clear all the information about this title object  </summary>
        public void Clear()
        {
            titleType = Title_Type_Enum.UNSPECIFIED;
            authority = null;
            displayLabel = null;
            language = null;
            title = null;
            subtitle = null;
            nonSort = null;
            if (partNames != null) partNames.Clear();
            if (partNumbers != null) partNumbers.Clear();
        }

        /// <summary> Writes this title object as a string </summary>
        /// <returns> Title object as a string </returns>
        public override string ToString()
        {
            if (String.IsNullOrEmpty(title))
                return String.Empty;

            if (!String.IsNullOrEmpty(nonSort))
            {
                if (nonSort[nonSort.Length - 1] == ' ')
                    return Convert_String_To_XML_Safe(nonSort + title);
                
                if (nonSort[nonSort.Length - 1] == '\'')
                {
                    return Convert_String_To_XML_Safe(nonSort + title);
                }
                
                return Convert_String_To_XML_Safe(nonSort + " " + title);
            }
            return Convert_String_To_XML_Safe(title);
        }

        internal MARC_Field to_MARC_HTML(int Tag, int FillIndicator, string StatementOfResponsibility, string HField)
        {
            if (String.IsNullOrEmpty(title))
                return null;

            MARC_Field returnValue = new MARC_Field();
            StringBuilder fieldBuilder = new StringBuilder();
            returnValue.Tag = Tag;
            if (Tag < 0)
                returnValue.Tag = 0;
            returnValue.Indicators = "  ";
            string fill_indicator_value = "0";
            if (!String.IsNullOrEmpty(nonSort))
            {
                if (nonSort[nonSort.Length - 1] == ' ')
                    fill_indicator_value = (nonSort.Length).ToString();
                else
                {
                    if (nonSort[nonSort.Length - 1] == '\'')
                    {
                        fill_indicator_value = (nonSort.Length).ToString();
                    }
                    else
                    {
                        fill_indicator_value = (nonSort.Length + 1).ToString();
                        nonSort = nonSort + " ";
                    }
                }
            }

            if ((!String.IsNullOrEmpty(displayLabel)) && (displayLabel != "Uncontrolled") && (displayLabel != "Main Entry"))
            {
                fieldBuilder.Append("|i " + displayLabel + ": ");
            }

            if (!String.IsNullOrEmpty(nonSort))
            {
                fieldBuilder.Append("|a " + nonSort + title.Replace("|", "&bar;") + " ");
            }
            else
            {
                fieldBuilder.Append("|a " + title.Replace("|", "&bar;") + " ");
            }

            if (HField.Length > 0)
            {
                fieldBuilder.Append("|h " + HField + " ");
            }

            if (!String.IsNullOrEmpty(subtitle))
            {
                fieldBuilder.Append("|b " + subtitle + " ");
            }

            if (StatementOfResponsibility.Length > 0)
            {
                fieldBuilder.Append("/ |c " + StatementOfResponsibility + " ");
            }

            if (partNumbers != null)
            {
                foreach (string thisPart in partNumbers)
                {
                    fieldBuilder.Append("|n " + thisPart + " ");
                }
            }

            if (partNames != null)
            {
                foreach (string thisPart in partNames)
                {
                    fieldBuilder.Append("|p " + thisPart + " ");
                }
            }

            if (!String.IsNullOrEmpty(language))
            {
                fieldBuilder.Append("|y " + language + " ");
            }

            string completeField = fieldBuilder.ToString().Trim();
            if (completeField.Length > 0)
            {
                if (completeField[completeField.Length - 1] != '.')
                    returnValue.Control_Field_Value = completeField + ".";
                else
                    returnValue.Control_Field_Value = completeField;
            }

            if (returnValue.Tag <= 0)
            {
                switch (titleType)
                {
                    case Title_Type_Enum.Uniform:
                        switch (displayLabel)
                        {
                            case "Main Entry":
                                returnValue.Tag = 130;
                                FillIndicator = 1;
                                break;

                            case "Uncontrolled":
                                returnValue.Tag = 730;
                                FillIndicator = 1;
                                break;

                            default:
                                returnValue.Tag = 240;
                                FillIndicator = 2;
                                break;
                        }
                        break;

                    case Title_Type_Enum.Translated:
                        returnValue.Tag = 242;
                        FillIndicator = 2;
                        break;

                    case Title_Type_Enum.Abbreviated:
                        returnValue.Tag = 210;
                        FillIndicator = -1;
                        break;

                    case Title_Type_Enum.Alternative:
                        if ((!String.IsNullOrEmpty(displayLabel) && (displayLabel == "Uncontrolled")))
                        {
                            returnValue.Tag = 740;
                            FillIndicator = 1;
                        }
                        else
                        {
                            returnValue.Tag = 246;
                            FillIndicator = -1;
                            returnValue.Indicators = "3 ";
                            if (!String.IsNullOrEmpty(displayLabel))
                            {
                                switch (displayLabel)
                                {
                                    case "Portion of title":
                                        returnValue.Indicators = "30";
                                        break;

                                    case "Parallel title":
                                        returnValue.Indicators = "31";
                                        break;

                                    case "Distinctive title":
                                        returnValue.Indicators = "32";
                                        break;

                                    case "Other title":
                                        returnValue.Indicators = "33";
                                        break;

                                    case "Cover title":
                                        returnValue.Indicators = "34";
                                        break;

                                    case "Added title page title":
                                        returnValue.Indicators = "35";
                                        break;

                                    case "Caption title":
                                        returnValue.Indicators = "36";
                                        break;

                                    case "Running title":
                                        returnValue.Indicators = "37";
                                        break;

                                    case "Spine title":
                                        returnValue.Indicators = "38";
                                        break;

                                    default:
                                        returnValue.Indicators = "3 ";
                                        break;
                                }
                            }
                        }
                        break;
                }
            }

            switch (FillIndicator)
            {
                case 1:
                    returnValue.Indicators = fill_indicator_value + " ";
                    break;

                case 2:
                    returnValue.Indicators = " " + fill_indicator_value;
                    break;
            }

            return returnValue;
        }

        internal void Add_MODS(TextWriter Results)
        {
            if (title.Length == 0)
                return;

            Results.Write("<mods:titleInfo");
            Add_ID(Results);
            switch (titleType)
            {
                case Title_Type_Enum.Abbreviated:
                    Results.Write(" type=\"abbreviated\"");
                    break;
                case Title_Type_Enum.Alternative:
                    Results.Write(" type=\"alternative\"");
                    break;
                case Title_Type_Enum.Translated:
                    Results.Write(" type=\"translated\"");
                    break;
                case Title_Type_Enum.Uniform:
                    Results.Write(" type=\"uniform\"");
                    break;
                case Title_Type_Enum.Course:
                    Results.Write(" type=\"course\"");
                    break;
            }

            if (!String.IsNullOrEmpty(displayLabel))
                Results.Write(" displayLabel=\"" + Convert_String_To_XML_Safe(displayLabel) + "\"");

            if (!String.IsNullOrEmpty(authority))
                Results.Write(" authority=\"" + authority + "\"");

            if (!String.IsNullOrEmpty(language))
                Results.Write(" lang=\"" + language + "\"");

            Results.Write(">\r\n");

            if (!String.IsNullOrEmpty(nonSort))
                Results.Write("<mods:nonSort>" + Convert_String_To_XML_Safe(nonSort) + "</mods:nonSort>\r\n");

            Results.Write("<mods:title>" + Convert_String_To_XML_Safe(title) + "</mods:title>\r\n");

            if (!String.IsNullOrEmpty(subtitle))
                Results.Write("<mods:subTitle>" + Convert_String_To_XML_Safe(subtitle) + "</mods:subTitle>\r\n");

            if (partNumbers != null)
            {
                foreach (string thisNumber in partNumbers)
                {
                    Results.Write("<mods:partNumber>" + Convert_String_To_XML_Safe(thisNumber) + "</mods:partNumber>\r\n");
                }
            }

            if (partNames != null)
            {
                foreach (string thisName in partNames)
                {
                    Results.Write("<mods:partName>" + Convert_String_To_XML_Safe(thisName) + "</mods:partName>\r\n");
                }
            }

            Results.Write("</mods:titleInfo>\r\n");
        }

        #region Public properties

        /// <summary> Gets or sets the type of this title </summary>
        public Title_Type_Enum Title_Type
        {
            get { return titleType; }
            set { titleType = value; }
        }

        /// <summary> Gets or sets the authority for this title </summary>
        public string Authority
        {
            get { return authority ?? String.Empty; }
            set { authority = value; }
        }

        /// <summary> Gets or sets the display label for this title </summary>
        public string Display_Label
        {
            get { return displayLabel ?? String.Empty; }
            set { displayLabel = value; }
        }

        /// <summary> Gets or sets the language for this title </summary>
        public string Language
        {
            get { return language ?? String.Empty; }
            set { language = value; }
        }

        /// <summary> Gets or sets the non sort portion of this title </summary>
        public string NonSort
        {
            get { return nonSort ?? String.Empty; }
            set { nonSort = value.Replace("  ", " "); }
        }

        /// <summary> Gets or sets the actual title portion of this title </summary>
        public string Title
        {
            get { return title ?? String.Empty; }
            set { title = value; }
        }

        /// <summary> Gets the title as XML-safe string </summary>
        internal string Title_XML
        {
            get { return Convert_String_To_XML_Safe(title); }
        }

        /// <summary> Gets or sets the subtitle portion </summary>
        public string Subtitle
        {
            get { return subtitle ?? String.Empty; }
            set { subtitle = value; }
        }

        /// <summary> Gets the number of part numbers associated with this title  </summary>
        /// <remarks> This should be used rather than the Count property of the <see cref="Part_Numbers"/> property.  Even if 
        /// there are no part numbers, the Part_Numbers property creates a readonly collection to pass back out.</remarks>
        public int Part_Numbers_Count
        {
            get {
                return partNumbers == null ? 0 : partNumbers.Count;
            }
        }

        /// <summary> Gets the collection of part numbers for this title </summary>
        /// <remarks> You should check the count of part numbers first using the <see cref="Part_Numbers_Count"/> property before using this property.
        /// Even if there are no part numbers, this property creates a readonly collection to pass back out.</remarks>
        public ReadOnlyCollection<string> Part_Numbers
        {
            get {
                return partNumbers == null ? new ReadOnlyCollection<string>(new List<string>()) : new ReadOnlyCollection<string>(partNumbers);
            }
        }

        /// <summary> Gets the number of part names associated with this title  </summary>
        /// <remarks> This should be used rather than the Count property of the <see cref="Part_Names"/> property.  Even if 
        /// there are no part names, the Part_Names property creates a readonly collection to pass back out.</remarks>
        public int Part_Names_Count
        {
            get {
                return partNames == null ? 0 : partNames.Count;
            }
        }

        /// <summary> Gets the collection of part names for this title </summary>
        /// <remarks> You should check the count of part names first using the <see cref="Part_Names_Count"/> property before using this property.
        /// Even if there are no part names, this property creates a readonly collection to pass back out.</remarks>
        public ReadOnlyCollection<string> Part_Names
        {
            get {
                return partNames == null ? new ReadOnlyCollection<string>(new List<string>()) : new ReadOnlyCollection<string>(partNames);
            }
        }

        /// <summary> Adds a new part number to this title information </summary>
        /// <param name="Part_Number"> New part number associated with this title </param>
        public void Add_Part_Number(string Part_Number)
        {
            if (partNumbers == null)
                partNumbers = new List<string>();

            partNumbers.Add(Part_Number);
        }

        /// <summary> Clears any existing part numbers associated with this title </summary>
        public void Clear_Part_Numbers()
        {
            if (partNumbers != null)
                partNumbers.Clear();
        }

        /// <summary> Adds a new part name to this title information </summary>
        /// <param name="Part_Name"> New part name associated with this title </param>
        public void Add_Part_Name(string Part_Name)
        {
            if (partNames == null)
                partNames = new List<string>();

            partNames.Add(Part_Name);
        }

        /// <summary> Clears any existing part names associated with this title </summary>
        public void Clear_Part_Names()
        {
            if (partNames != null)
                partNames.Clear();
        }

        #endregion
    }
}