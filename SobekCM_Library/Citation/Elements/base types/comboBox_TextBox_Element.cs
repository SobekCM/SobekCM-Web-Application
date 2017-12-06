#region Using directives

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml;
using SobekCM.Core.ApplicationState;
using SobekCM.Core.Configuration.Localization;
using SobekCM.Core.Users;

#endregion

namespace SobekCM.Library.Citation.Elements
{
    /// <summary> Abstract base class for all elements which are made up of a single combo/select box followed by a text box  </summary>
    /// <remarks> This class implements the <see cref="iElement"/> interface and extends the <see cref="abstract_Element"/> class. </remarks>
    public abstract class ComboBox_TextBox_Element : abstract_Element
    {
        /// <summary> Flag indicates if the text box should be cleared when the combo box changes </summary>
        protected bool ClearTextboxOnComboboxChange;

        /// <summary> Protected field holds the default value(s) for the combo box </summary>
        protected List<string> DefaultCodes;

        /// <summary> Protected field holds the default value(s) for the text box </summary>
        protected List<string> DefaultValues;

        /// <summary> Protected field holds all of the possible, selectable values for the combo box </summary>
        protected List<string> PossibleSelectItems;

        /// <summary> Protected field holds any label to place before the text box, after the combo box </summary>
        protected string SecondLabel;

        /// <summary> Protected field holds the dictionary that maps from a code to a statement </summary>
        /// <remarks> This is only used if selecting a code should set a default statement </remarks>
        protected Dictionary<string, string> CodeToStatementDictionary;

        /// <summary> Constructor for a new instance of the ComboBox_TextBox_Element class </summary>
        /// <param name="Title"> Title for this element </param>
        /// <param name="Html_Element_Name"> Name for the html components and styles for this element </param>
        protected ComboBox_TextBox_Element(string Title, string Html_Element_Name)
        {
            base.Title = Title;
            html_element_name = Html_Element_Name;
            Restrict_Values = false;
            PossibleSelectItems = new List<string>();
            SecondLabel = String.Empty;

            DefaultCodes = new List<string>();
            DefaultValues = new List<string>();
        }

        /// <summary> Flag indicates if values are limited to those in the drop down list. </summary>
        protected bool Restrict_Values { get; set; }

        /// <summary> Adds a possible, selectable value to the combo/select box </summary>
        /// <param name="Newitem"> New possible, selectable value </param>
        public void Add_Item(string Newitem )
        {
            PossibleSelectItems.Add(Newitem);
        }

        /// <summary> Adds a link between a code and default statement, so that selecting the
        /// code will set the corresponding default statement </summary>
        /// <param name="Code"> Code </param>
        /// <param name="Statement"> Default statement for that code </param>
        protected void Add_Code_Statement_Link(string Code, string Statement)
        {
            if (CodeToStatementDictionary == null)
                CodeToStatementDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            CodeToStatementDictionary[Code] = Statement;
        }

        /// <summary> Method helps to render the html for all elements based on the comboBox_TextBox_Element class </summary>
        /// <param name="Output"> Output for the generate html for this element </param>
        /// <param name="SelectValue"> Value for the current digital resource to display in the combo box</param>
        /// <param name="TextValue"> Value for the current digital resource to display in the text box</param>
        /// <param name="Skin_Code"> Code for the current html skin </param>
        /// <param name="Current_User"> Current user, who's rights may impact the way an element is rendered </param>
        /// <param name="CurrentLanguage"> Current user-interface language </param>
        /// <param name="Translator"> Language support object which handles simple translational duties </param>
        /// <param name="Base_URL"> Base URL for the current request </param>
        protected void render_helper(TextWriter Output, string SelectValue, string TextValue, string Skin_Code, User_Object Current_User, Web_Language_Enum CurrentLanguage, Language_Support_Info Translator, string Base_URL)
        {
            render_helper(Output, SelectValue, PossibleSelectItems, TextValue, Skin_Code, Current_User, CurrentLanguage, Translator, Base_URL, false);
        }

        /// <summary> Method helps to render the html for all elements based on comboBox_TextBox_Element class </summary>
        /// <param name="Output"> Output for the generate html for this element </param>
        /// <param name="SelectValue"> Value for the current digital resource to display in the combo box</param>
        /// <param name="TextValue"> Value for the current digital resource to display in the text box</param>
        /// <param name="Skin_Code"> Code for the current html skin </param>
        /// <param name="Current_User"> Current user, who's rights may impact the way an element is rendered </param>
        /// <param name="CurrentLanguage"> Current user-interface language </param>
        /// <param name="Translator"> Language support object which handles simple translational duties </param>
        /// <param name="Base_URL"> Base URL for the current request </param>
        /// <param name="InitialValue"> Flag indicates if the value in the select_value param is actually instructional text, and not a true value</param>
        protected void render_helper(TextWriter Output, string SelectValue, string TextValue, string Skin_Code, User_Object Current_User, Web_Language_Enum CurrentLanguage, Language_Support_Info Translator, string Base_URL, bool InitialValue)
        {
            render_helper(Output, SelectValue, PossibleSelectItems, TextValue, Skin_Code, Current_User, CurrentLanguage, Translator, Base_URL, InitialValue);
        }


        /// <summary> Method helps to render the html for all elements based on comboBox_TextBox_Element class </summary>
        /// <param name="Output"> Output for the generate html for this element </param>
        /// <param name="SelectValues"> Value(s) for the current digital resource to display in the combo box</param>
        /// <param name="TextValues"> Value(s) for the current digital resource to display in the text box </param>
        /// <param name="Skin_Code"> Code for the current html skin </param>
        /// <param name="Current_User"> Current user, who's rights may impact the way an element is rendered </param>
        /// <param name="CurrentLanguage"> Current user-interface language </param>
        /// <param name="Translator"> Language support object which handles simple translational duties </param>
        /// <param name="Base_URL"> Base URL for the current request </param>
        protected void render_helper(TextWriter Output, List<string> SelectValues, List<string> TextValues, string Skin_Code, User_Object Current_User, Web_Language_Enum CurrentLanguage, Language_Support_Info Translator, string Base_URL)
        {
            if (TextValues.Count == 0)
            {
                render_helper(Output, String.Empty, String.Empty, Skin_Code, Current_User, CurrentLanguage, Translator, Base_URL);
            }

            string id_name = html_element_name.Replace("_", "");

            Output.WriteLine("  <!-- " + Title + " Element -->");
            Output.WriteLine("  <tr>");
            Output.WriteLine("    <td style=\"width:" + LEFT_MARGIN + "px\">&nbsp;</td>");
            if (Acronym.Length > 0)
            {
                Output.WriteLine("    <td class=\"metadata_label\"><a href=\"" + Help_URL(Skin_Code, Base_URL) + "\" target=\"_" + html_element_name.ToUpper() + "\"><acronym title=\"" + Acronym + "\">" + Translator.Get_Translation(Title, CurrentLanguage) + ":</acronym></a></td>");
            }
            else
            {
                Output.WriteLine("    <td class=\"metadata_label\"><a href=\"" + Help_URL(Skin_Code, Base_URL) + "\" target=\"_" + html_element_name.ToUpper() + "\">" + Translator.Get_Translation(Title, CurrentLanguage) + ":</a></td>");
            }
            Output.WriteLine("    <td>");

            Output.WriteLine("      <table>");
            Output.WriteLine("        <tr>");
            Output.WriteLine("          <td>");
            Output.WriteLine("            <div id=\"" + html_element_name + "_div\">");

            // Determine if the code --> statement linking should be present
            bool codeStatementMappingPresent = !((CodeToStatementDictionary == null) || (CodeToStatementDictionary.Count == 0));

            // Add each line with a combo box
            for (int i = 1; i <= TextValues.Count; i++)
            {
                // Determine the javascript to append
                string javascript = String.Empty;
                if (comboBoxEvents != null)
                {
                    string currentOnChange = comboBoxEvents.OnChange ?? String.Empty;
                    string newOnChange = (currentOnChange.Length > 0) ? currentOnChange.Trim() + ";" : String.Empty;
                    if (ClearTextboxOnComboboxChange)
                    {
                        if (codeStatementMappingPresent)
                        {
                            newOnChange = newOnChange + "\"combo_text_element_onchange('" + id_name + "_select" + i + "', '" + id_name + "_text" + i + "',true)\"";
                        }
                        else
                        {
                            newOnChange = newOnChange + "\"clear_textbox('" + id_name + "_text" + i + "')\"";
                        }
                    }
                    else if (codeStatementMappingPresent)
                    {
                        newOnChange = newOnChange + "\"combo_text_element_onchange('" + id_name + "_select" + i + "', '" + id_name + "_text" + i + "',false)\"";
                    }
                    comboBoxEvents.OnChange = newOnChange;
                    javascript = comboBoxEvents.ToString();
                    comboBoxEvents.OnChange = currentOnChange;
                }
                else
                {
                    if (ClearTextboxOnComboboxChange)
                    {
                        if (codeStatementMappingPresent)
                        {
                            javascript = "onchange=\"combo_text_element_onchange('" + id_name + "_select" + i + "', '" + id_name + "_text" + i + "',true)\" ";
                        }
                        else
                        {
                            javascript = "onchange=\"clear_textbox('" + id_name + "_text" + i + "')\" ";
                        }
                    }
                    else if (codeStatementMappingPresent)
                    {
                        javascript = "onchange=\"combo_text_element_onchange('" + id_name + "_select" + i + "', '" + id_name + "_text" + i + "',false)\" ";
                    }
                }

                // Write the combo box
                Output.Write("            <select class=\"" + html_element_name + "_select\" name=\"" + id_name + "_select" + i + "\" id=\"" + id_name + "_select" + i + "\" onblur=\"javascript:selectbox_leave('" + id_name + "_select" + i + "','" + html_element_name + "_select', '" + html_element_name + "_select_init')\" " + javascript + " >" );

                bool found_option = false;
                foreach (string thisOption in PossibleSelectItems)
                {
                    // Determine the value
                    string value = thisOption;
                    if ((codeStatementMappingPresent) && (CodeToStatementDictionary.ContainsKey(thisOption)))
                        value = thisOption + "|" + CodeToStatementDictionary[thisOption];

                    if ((i < PossibleSelectItems.Count) && (thisOption == SelectValues[i - 1]))
                    {
                        Output.Write("<option value=\"" + HttpUtility.HtmlEncode(value) + "\" selected=\"selected=\">" + thisOption + "</option>");
                        found_option = true;
                    }
                    else
                    {
                        Output.Write("<option value=\"" + HttpUtility.HtmlEncode(value) + "\" >" + thisOption + "</option>");
                    }
                }

                if ((i <= SelectValues.Count) && (SelectValues[i - 1].Length > 0) && (!Restrict_Values) && (!found_option))
                {
                    // Get this option
                    string option = SelectValues[i - 1];

                    // Determine the value
                    string value = option;
                    if ((codeStatementMappingPresent) && (CodeToStatementDictionary.ContainsKey(option)))
                        value = option + "|" + CodeToStatementDictionary[option];

                    Output.Write("<option value=\"" + HttpUtility.HtmlEncode(value) + "\" selected=\"selected=\">" + option + "</option>");
                }
                Output.Write("</select>");

                // Write the second text
                if (SecondLabel.Length > 0)
                {
                    Output.Write("<span class=\"metadata_sublabel\">" + SecondLabel + ":</span>");
                }

                // Write the text box
				Output.Write("<input name=\"" + id_name + "_text" + i + "\" id=\"" + id_name + "_text" + i + "\" class=\"" + html_element_name + "_input sbk_Focusable\" type=\"text\" value=\"" + HttpUtility.HtmlEncode(TextValues[i - 1]) + "\" ");
                if (textBoxEvents != null)
                    textBoxEvents.Add_Events_HTML(Output);
                Output.Write(" />");

                Output.WriteLine(i == TextValues.Count ? "</div>" : "<br />");
            }

            Output.WriteLine("        </td>");
            Output.WriteLine("        <td valign=\"bottom\" >");
            if (Repeatable)
            {
                Output.WriteLine("          <a title=\"" + Translator.Get_Translation("Click to add another " + Title.ToLower(), CurrentLanguage) + ".\" href=\"" + Base_URL + "l/technical/javascriptrequired\" onmousedown=\"return " + html_element_name + "_add_new_item();\"><img class=\"repeat_button\" src=\"" + REPEAT_BUTTON_URL + "\" /></a>");
            }

            Output.WriteLine("            <a target=\"_" + html_element_name.ToUpper() + "\"  title=\"" + Translator.Get_Translation("Get help.", CurrentLanguage) + "\" href=\"" + Help_URL(Skin_Code, Base_URL) + "\" ><img class=\"help_button\" src=\"" + HELP_BUTTON_URL + "\" /></a>");

            Output.WriteLine("        </td>");
            Output.WriteLine("      </tr>");
            Output.WriteLine("    </table>");

            Output.WriteLine("    </td>");
            Output.WriteLine("  </tr>");
            Output.WriteLine();
        }

        /// <summary> Method helps to render the html for all elements based on comboBox_TextBox_Element class </summary>
        /// <param name="Output"> Output for the generate html for this element </param>
        /// <param name="SelectValue"> Value for the current digital resource to display in the combo box</param>
        /// <param name="UserdefinedPossible"> List of possible select values, set by the user </param>
        /// <param name="TextValue"> Value for the current digital resource to display in the text box</param>
        /// <param name="Skin_Code"> Code for the current html skin </param>
        /// <param name="Current_User"> Current user, who's rights may impact the way an element is rendered </param>
        /// <param name="CurrentLanguage"> Current user-interface language </param>
        /// <param name="Translator"> Language support object which handles simple translational duties </param>
        /// <param name="Base_URL"> Base URL for the current request </param>
        /// <param name="InitialValue"> Flag indicates if the value in the select_value param is actually instructional text, and not a true value</param>
        protected void render_helper(TextWriter Output, string SelectValue, List<string> UserdefinedPossible, string TextValue, string Skin_Code, User_Object Current_User, Web_Language_Enum CurrentLanguage, Language_Support_Info Translator, string Base_URL, bool InitialValue)
        {
            string id_name = html_element_name.Replace("_", "");

            Output.WriteLine("  <!-- " + Title + " Element -->");
            Output.WriteLine("  <tr>");
            Output.WriteLine("    <td style=\"width:" + LEFT_MARGIN + "px\">&nbsp;</td>");
            if (Acronym.Length > 0)
            {
                Output.WriteLine("    <td class=\"metadata_label\"><a href=\"" + Help_URL(Skin_Code, Base_URL) + "\" target=\"_" + html_element_name.ToUpper() + "\"><acronym title=\"" + Acronym + "\">" + Translator.Get_Translation(Title, CurrentLanguage) + ":</acronym></a></td>");
            }
            else
            {
                Output.WriteLine("    <td class=\"metadata_label\"><a href=\"" + Help_URL(Skin_Code, Base_URL) + "\" target=\"_" + html_element_name.ToUpper() + "\">" + Translator.Get_Translation(Title, CurrentLanguage) + ":</a></td>");
            }
            Output.WriteLine("    <td>");

            Output.WriteLine("      <table>");
            Output.WriteLine("        <tr>");
            Output.WriteLine("          <td>");
            Output.WriteLine("            <div id=\"" + html_element_name + "_div\">");

            const int i = 1;

            // Determine if the code --> statement linking should be present
            bool codeStatementMappingPresent = !((CodeToStatementDictionary == null) || (CodeToStatementDictionary.Count == 0));

            // Determine the javascript to append
            string javascript = String.Empty;
            if (comboBoxEvents != null)
            {
                string currentOnChange = comboBoxEvents.OnChange ?? String.Empty;
                string newOnChange = (currentOnChange.Length > 0) ? currentOnChange.Trim() + ";" : String.Empty;
                if (ClearTextboxOnComboboxChange)
                {
                    if (codeStatementMappingPresent)
                    {
                        newOnChange = newOnChange + "\"combo_text_element_onchange('" + id_name + "_select" + i + "', '" + id_name + "_text" + i + "',true)\"";
                    }
                    else
                    {
                        newOnChange = newOnChange + "\"clear_textbox('" + id_name + "_text" + i + "')\"";
                    }
                }
                else if (codeStatementMappingPresent)
                {
                    newOnChange = newOnChange + "\"combo_text_element_onchange('" + id_name + "_select" + i + "', '" + id_name + "_text" + i + "',false)\"";
                }
                comboBoxEvents.OnChange = newOnChange;
                javascript = comboBoxEvents.ToString();
                comboBoxEvents.OnChange = currentOnChange;
            }
            else
            {
                if (ClearTextboxOnComboboxChange)
                {
                    if (codeStatementMappingPresent)
                    {
                        javascript = "onchange=\"combo_text_element_onchange('" + id_name + "_select" + i + "', '" + id_name + "_text" + i + "',true)\" ";
                    }
                    else
                    {
                        javascript = "onchange=\"clear_textbox('" + id_name + "_text" + i + "')\" ";
                    }
                }
                else if (codeStatementMappingPresent)
                {
                    javascript = "onchange=\"combo_text_element_onchange('" + id_name + "_select" + i + "', '" + id_name + "_text" + i + "',false)\" ";
                }
            }

            // Start the select box
            Output.Write("            <select class=\"" + html_element_name + "_select\" name=\"" + id_name + "_select" + i + "\" id=\"" + id_name + "_select" + i + "\" onblur=\"javascript:selectbox_leave('" + id_name + "_select" + i + "','" + html_element_name + "_select', '" + html_element_name + "_select_init')\"" + javascript + " >");

            // Apply any default
            if ((String.IsNullOrEmpty(SelectValue)) && (PossibleSelectItems.Count > 0 ))
            {
                if ((DefaultCodes != null) && (DefaultCodes.Count > 0))
                {
                    SelectValue = DefaultCodes[0];
                    if ((DefaultValues != null) && (DefaultValues.Count > 0))
                    {
                        TextValue = DefaultValues[0];
                    }
                    else if ((codeStatementMappingPresent) && (CodeToStatementDictionary.ContainsKey(SelectValue)))
                    {
                        TextValue = CodeToStatementDictionary[SelectValue];
                    }
                }
                else if (!PossibleSelectItems.Contains(SelectValue ?? String.Empty))
                {
                    SelectValue = PossibleSelectItems[0];
                    if ((codeStatementMappingPresent) && ( CodeToStatementDictionary.ContainsKey(SelectValue)))
                    {
                        TextValue = CodeToStatementDictionary[SelectValue];
                    }
                }
            }

            bool found_option = false;
            foreach (string thisOption in PossibleSelectItems)
            {
                // Determine the value
                string value = thisOption;
                if ((codeStatementMappingPresent) && (CodeToStatementDictionary.ContainsKey(thisOption)))
                    value = thisOption + "|" + CodeToStatementDictionary[thisOption];

                if ((i < PossibleSelectItems.Count) && (thisOption == SelectValue))
                {
                    Output.Write("<option value=\"" + HttpUtility.HtmlEncode(value) + "\" selected=\"selected=\">" + thisOption + "</option>");
                    found_option = true;
                }
                else
                {
                    Output.Write("<option value=\"" + HttpUtility.HtmlEncode(value) + "\" >" + thisOption + "</option>");
                }
            }

            if ((SelectValue.Length > 0) && (!Restrict_Values) && (!found_option))
            {
                // Get this option
                string option = SelectValue;

                // Determine the value
                string value = option;
                if ((codeStatementMappingPresent) && (CodeToStatementDictionary.ContainsKey(option)))
                    value = option + "|" + CodeToStatementDictionary[option];

                Output.Write("<option value=\"" + HttpUtility.HtmlEncode(value) + "\" selected=\"selected=\">" + option + "</option>");
            }
            Output.Write("</select>");

            // Write the second text
            if (SecondLabel.Length > 0)
            {
                Output.Write("<span class=\"metadata_sublabel\">" + SecondLabel + ":</span>");
            }

            // Write the text box
            Output.Write("<input name=\"" + id_name + "_text" + i + "\" id=\"" + id_name + "_text" + i + "\" class=\"" + html_element_name + "_input sbk_Focusable\" type=\"text\" value=\"" + HttpUtility.HtmlEncode(TextValue) + "\" ");
            if (textBoxEvents != null)
                textBoxEvents.Add_Events_HTML(Output);
            Output.Write(" />");

            Output.WriteLine("</div>");


            Output.WriteLine("        </td>");
            Output.WriteLine("        <td style=\"vertical-align:bottom\" >");
            if (Repeatable)
            {
                Output.WriteLine("          <a title=\"" + Translator.Get_Translation("Click to add another " + Title.ToLower(), CurrentLanguage) + ".\" href=\"" + Base_URL + "l/technical/javascriptrequired\" onmousedown=\"return " + html_element_name + "_add_new_item();\"><img class=\"repeat_button\" src=\"" + REPEAT_BUTTON_URL + "\" /></a>");
            }

            Output.WriteLine("            <a target=\"_" + html_element_name.ToUpper() + "\"  title=\"" + Translator.Get_Translation("Get help.", CurrentLanguage) + "\" href=\"" + Help_URL(Skin_Code, Base_URL) + "\" ><img class=\"help_button\" src=\"" + HELP_BUTTON_URL + "\" /></a>");

            Output.WriteLine("        </td>");
            Output.WriteLine("      </tr>");
            Output.WriteLine("    </table>");

            Output.WriteLine("    </td>");
            Output.WriteLine("  </tr>");
            Output.WriteLine();
        }

        #region Methods Implementing the Abstract Methods from abstract_Element class

        /// <summary> Reads the inner data from the CompleteTemplate XML format </summary>
        /// <param name="XMLReader"> Current template xml configuration reader </param>
        /// <remarks> This reads the possible values for the combo box from an <i>options</i> subelement.  The default value for the combo box is from a <i>code</i> subelement and the default value for the text box is from a <i>statement</i> subelement. </remarks>
        protected override void Inner_Read_Data( XmlReader XMLReader )
        {
            while (XMLReader.Read())
            {
                if ((XMLReader.NodeType == XmlNodeType.Element) && (XMLReader.Name.ToLower() == "options"))
                {
                    XMLReader.Read();
                    string options = XMLReader.Value.Trim();
                    PossibleSelectItems.Clear();
                    if (options.Length > 0)
                    {
                        SortedList<string, string> sorted_codes = new SortedList<string, string>();
                        string[] options_parsed = options.Split(",".ToCharArray());
                        foreach (string thisOption in options_parsed.Where(ThisOption => !sorted_codes.ContainsKey(ThisOption.Trim())))
                        {
                            sorted_codes.Add(thisOption.Trim(), thisOption.Trim());
                        }

                        PossibleSelectItems.AddRange(sorted_codes.Values);
                    }
                }

                if ((XMLReader.NodeType == XmlNodeType.Element) && (XMLReader.Name.ToLower() == "code"))
                {
                    XMLReader.Read();
                    DefaultCodes.Add(XMLReader.Value.Trim());
                }

                if ((XMLReader.NodeType == XmlNodeType.Element) && (XMLReader.Name.ToLower() == "statement"))
                {
                    XMLReader.Read();
                    DefaultValues.Add(XMLReader.Value.Trim());
                }
            }
        }

        #endregion

        #region Methods and properties exposing events for the HTML elements in this base type

        private HtmlEventsHelper textBoxEvents;

        /// <summary> Access to the complete text box events object </summary>
        /// <remarks> Requesting this property will create a new object, if one does not already exist </remarks>
        protected HtmlEventsHelper TextBoxEvents
        {
            get { return textBoxEvents ?? (textBoxEvents = new HtmlEventsHelper()); }
        }

        /// <summary> Add some event text to an event on the primary text box for the citation control </summary>
        /// <param name="Event"> Type of the event to add text to </param>
        /// <param name="EventText"> Text (html format) to add to the event, such as "getElementById('demo').innerHTML = Date()", or "myFunction();return false;", etc.. </param>
        protected void Add_TextBox_Event(HtmlEventsEnum Event, string EventText)
        {
            // If the events is null, create it
            if (textBoxEvents == null)
                textBoxEvents = new HtmlEventsHelper();

            // Add this event
            textBoxEvents.Add_Event(Event, EventText);
        }

        private HtmlEventsHelper comboBoxEvents;

        /// <summary> Access to the complete combo box events object </summary>
        /// <remarks> Requesting this property will create a new object, if one does not already exist </remarks>
        protected HtmlEventsHelper ComboBoxEvents
        {
            get { return comboBoxEvents ?? (comboBoxEvents = new HtmlEventsHelper()); }
        }

        /// <summary> Add some event text to an event on the primary combo box for the citation control </summary>
        /// <param name="Event"> Type of the event to add text to </param>
        /// <param name="EventText"> Text (html format) to add to the event, such as "getElementById('demo').innerHTML = Date()", or "myFunction();return false;", etc.. </param>
        protected void Add_ComboBox_Event(HtmlEventsEnum Event, string EventText)
        {
            // If the events is null, create it
            if (comboBoxEvents == null)
                comboBoxEvents = new HtmlEventsHelper();

            // Add this event
            comboBoxEvents.Add_Event(Event, EventText);
        }

        #endregion

    }
}
