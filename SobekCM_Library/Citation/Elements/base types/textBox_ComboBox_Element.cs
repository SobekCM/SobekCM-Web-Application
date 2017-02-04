#region Using directives

using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Xml;
using SobekCM.Core.ApplicationState;
using SobekCM.Core.Configuration.Localization;
using SobekCM.Core.Users;

#endregion

namespace SobekCM.Library.Citation.Elements
{
    /// <summary> Abstract base class for all elements which are made up of a text box followed by a combo/select box </summary>
    /// <remarks> This class implements the <see cref="iElement"/> interface and extends the <see cref="abstract_Element"/> class. </remarks>
    public abstract class TextBox_ComboBox_Element : abstract_Element
    {
        /// <summary> Protected field contains all the possible strings for the combo/select box </summary>
        protected List<string> PossibleSelectItemsText;

        /// <summary> Protected field contains all the values for the possible strings for the combo/select box </summary>
        protected List<string> PossibleSelectItemsValue;

        /// <summary> Protected field contains a possible second label to show before the combo box </summary>
        protected string SecondLabel;


        /// <summary> Constructor for a new instance of the TextBox_ComboBox_Element class </summary>
        /// <param name="Title"> Title for this element </param>
        /// <param name="Html_Element_Name"> Name for the html components and styles for this element </param>
        protected TextBox_ComboBox_Element(string Title, string Html_Element_Name)
        {
            base.Title = Title;
            html_element_name = Html_Element_Name;
            SecondLabel = String.Empty;

            PossibleSelectItemsText = new List<string>();
            PossibleSelectItemsValue = new List<string>();
        }

        /// <summary> Flag indicates if values are limited to those in the drop down list. </summary>
        protected bool Restrict_Values { get; set; }

        /// <summary> Adds a new possible string for the combo/select box, along with associated value </summary>
        /// <param name="Text"> Text to display in the combo/select box</param>
        /// <param name="Value"> Associated value for this option </param>
        public void Add_Select_Item(string Text, string Value )
        {
            PossibleSelectItemsText.Add(Text);
            PossibleSelectItemsValue.Add(Value);
        }


        /// <summary> Method helps to render the html for all elements based on TextBox_ComboBox_Element class </summary>
        /// <param name="Output"> Output for the generate html for this element </param>
        /// <param name="TextValues"> Value(s) for the current digital resource to display in the text box </param>
        /// <param name="SelectValues"> Value(s) for the current digital resource to display in the combo box</param>
        /// <param name="Skin_Code"> Code for the current html skin </param>
        /// <param name="Current_User"> Current user, who's rights may impact the way an element is rendered </param>
        /// <param name="CurrentLanguage"> Current user-interface language </param>
        /// <param name="Translator"> Language support object which handles simple translational duties </param>
        /// <param name="Base_URL"> Base URL for the current request </param>
        protected void render_helper(TextWriter Output, List<string> TextValues, List<string> SelectValues, string Skin_Code, User_Object Current_User, Web_Language_Enum CurrentLanguage, Language_Support_Info Translator, string Base_URL )
        {
            if (TextValues.Count == 0)
            {
                TextValues.Add(String.Empty);
                SelectValues.Add(String.Empty);
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
            Output.WriteLine("      <table><tr><td>");
            Output.WriteLine("      <div id=\"" + html_element_name + "_div\">");
            for (int i = 1; i <= TextValues.Count; i++)
            {
                // Write the text box
                Output.Write("        <input name=\"" + id_name + "_text" + i + "\" id=\"" + id_name + "_text" + i + "\" class=\"" + html_element_name + "_input sbk_Focusable\" type=\"text\" value=\"" + HttpUtility.HtmlEncode(TextValues[i - 1]) + "\" ");
                if (textBoxEvents != null)
                    textBoxEvents.Add_Events_HTML(Output);
                Output.Write(" />");

                // If there is a second label, draw that
                if (SecondLabel.Length > 0)
                {
                    Output.WriteLine("<span class=\"metadata_sublabel\">" + Translator.Get_Translation(SecondLabel, CurrentLanguage) + ":</span>");
                }
                else
                {
                    Output.WriteLine();
                }

                // Write the combo box
                Output.Write("        <select class=\"" + html_element_name + "_select\" name=\"" + id_name + "_select" + i + "\" id=\"" + id_name + "_select" + i + "\" ");
                if (comboBoxEvents != null)
                    comboBoxEvents.Add_Events_HTML(Output);
                Output.WriteLine(" >");

            

                bool found_option = false;
                for ( int j = 0 ; j < PossibleSelectItemsText.Count ; j++ )
                {
                    if ((i < PossibleSelectItemsText.Count) && (PossibleSelectItemsText[j] == SelectValues[i - 1]))
                    {
                        if (PossibleSelectItemsValue.Count > j)
                        {
                            Output.WriteLine("          <option selected=\"selected=\" value=\"" + PossibleSelectItemsValue[j] + "\">" + PossibleSelectItemsText[j] + "</option>");
                        }
                        else
                        {
                            Output.WriteLine("          <option selected=\"selected=\" value=\"" + PossibleSelectItemsText[j] + "\">" + PossibleSelectItemsText[j] + "</option>");
                        }
                        found_option = true;
                    }
                    else
                    {
                        if (PossibleSelectItemsValue.Count > j)
                        {
                            Output.WriteLine("          <option value=\"" + PossibleSelectItemsValue[j] + "\">" + PossibleSelectItemsText[j] + "</option>");
                        }
                        else
                        {
                            Output.WriteLine("          <option value=\"" + PossibleSelectItemsText[j] + "\">" + PossibleSelectItemsText[j] + "</option>");
                        }
                    }
                }

                if (( i <= SelectValues.Count ) && ( SelectValues[i-1].Length > 0  ) && ( !Restrict_Values ) && ( !found_option ))
                {
                    Output.WriteLine("          <option value=\"" + SelectValues[i-1] + "\" selected=\"selected=\">" + SelectValues[i-1] + "</option>");
                }
                Output.Write("        </select>");

                if (i == TextValues.Count )
                {
                    Output.WriteLine();
                    Output.WriteLine("      </div>");
                }
                else
                {
                    Output.WriteLine("<br />");
                }
            }

            Output.WriteLine("    </td>");
            Output.WriteLine("    <td style=\"vertical-align:bottom\" >");

            if (Repeatable)
            {
                Output.WriteLine("            <a title=\"" + Translator.Get_Translation("Click to add another " + Title.ToLower(), CurrentLanguage) + ".\" href=\"" + Base_URL + "l/technical/javascriptrequired\" onmousedown=\"return add_text_box_select_element('" + html_element_name + "', '" + SecondLabel + "');\"><img class=\"repeat_button\" src=\"" + REPEAT_BUTTON_URL + "\" /></a>");
            }
            Output.WriteLine("            <a target=\"_" + html_element_name.ToUpper() + "\"  title=\"" + Translator.Get_Translation("Get help.", CurrentLanguage) + "\" href=\"" + Help_URL(Skin_Code, Base_URL) + "\" ><img class=\"help_button\" src=\"" + HELP_BUTTON_URL + "\" /></a>");
            Output.WriteLine("    </td>");
            Output.WriteLine("  </tr></table></td></tr>");

            Output.WriteLine();
        }

        #region Methods Implementing the Abstract Methods from abstract_Element class

        /// <summary> Reads the inner data from the CompleteTemplate XML format </summary>
        /// <param name="XMLReader"> Current template xml configuration reader </param>
        /// <remarks> This reads the possible values for the combo box from a <i>options</i> subelement </remarks>
        protected override void Inner_Read_Data(XmlTextReader XMLReader)
        {
            while (XMLReader.Read())
            {
                if ((XMLReader.NodeType == XmlNodeType.Element) && (XMLReader.Name.ToLower() == "options"))
                {
                    XMLReader.Read();
                    string options = XMLReader.Value.Trim();
                    PossibleSelectItemsText.Clear();
                    PossibleSelectItemsValue.Clear();
                    if (options.Length > 0)
                    {
                        string[] options_parsed = options.Split(",".ToCharArray());
                        foreach (string thisOption in options_parsed)
                        {
                            if (!PossibleSelectItemsText.Contains(thisOption.Trim()))
                            {
                                PossibleSelectItemsText.Add(thisOption.Trim());

                            }
                        }
                    }
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
