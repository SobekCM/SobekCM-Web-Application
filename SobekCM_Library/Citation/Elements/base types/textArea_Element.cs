﻿#region Using directives

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
    /// <summary> Abstract base class for all elements which are made up of a text area element </summary>
    /// <remarks> This class implements the <see cref="iElement"/> interface and extends the <see cref="abstract_Element"/> class. </remarks>
    public abstract class TextArea_Element : abstract_Element
    {
        /// <summary> Protected field holds the number of columns (width) to display in most browsers </summary>
        protected int Cols;

        /// <summary> Protected field holds the number of columns (width) to display in Mozilla Firefox </summary>
        protected int ColsMozilla;

        protected readonly List<string> DefaultValues;

        /// <summary> Protected field holds placeholder that should appear in the empty textbox </summary>
        protected string Placeholder;

        /// <summary> Protected field holds the number of rows for the text area to display </summary>
        protected int Rows = 3;

        /// <summary> Constructor for a new instance of the TextArea_Element class </summary>
        /// <param name="Title"> Title for this element </param>
        /// <param name="Html_Element_Name"> Name for the html components and styles for this element </param>
        protected TextArea_Element(string Title, string Html_Element_Name)
        {
            base.Title = Title;
            html_element_name = Html_Element_Name;

            DefaultValues = new List<string>();
            Cols = TEXT_AREA_COLUMNS;
            ColsMozilla = MOZILLA_TEXT_AREA_COLUMNS;
        }

        /// <summary> Adds a default value for this text area based element </summary>
        /// <param name="DefaultValue"> New default value </param>
        public void Add_Default_Value(string DefaultValue)
        {
            DefaultValues.Add(DefaultValue);
        }

        /// <summary> Method helps to render all simple text area based elements </summary>
        /// <param name="Output"> Output for the generated html for this element </param>
        /// <param name="InstanceValues"> Value(s) for the current digital resource to display </param>
        /// <param name="Skin_Code"> Code for the current html skin </param>
        /// <param name="IsMozilla"> Flag indicates if the browser is Mozilla Firefox</param>
        /// <param name="Current_User"> Current user, who's rights may impact the way an element is rendered </param>
        /// <param name="CurrentLanguage"> Current user-interface language </param>
        /// <param name="Translator"> Language support object which handles simple translational duties </param>
        /// <param name="Base_URL"> Base URL for the current request </param>
        protected void render_helper(TextWriter Output, List<string> InstanceValues, string Skin_Code, bool IsMozilla, User_Object Current_User, Web_Language_Enum CurrentLanguage, Language_Support_Info Translator, string Base_URL)
        {
            List<string> allValues = new List<string>();
            allValues.AddRange(DefaultValues);
            allValues.AddRange(InstanceValues);

            if (allValues.Count == 0)
            {
                render_helper(Output, String.Empty, Skin_Code, IsMozilla, Current_User, CurrentLanguage, Translator, Base_URL);
                return;
            }

            if (allValues.Count == 1)
            {
                render_helper(Output, allValues[0], Skin_Code, IsMozilla, Current_User, CurrentLanguage, Translator, Base_URL);
                return;
            }

            // Determine the columns for this text area, based on browser
            int actual_cols = Cols;
            if (IsMozilla)
                actual_cols = ColsMozilla;


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
            for (int i = 0; i < allValues.Count; i++)
            {
                Output.Write("              <textarea rows=\"" + Rows + "\" cols=\"" + actual_cols + "\" name=\"" + id_name + i + "\" id=\"" + id_name + i + "\" class=\"" + html_element_name + "_input sbk_Focusable\" ");

                if (!String.IsNullOrWhiteSpace(Placeholder))
                    Output.Write(" placeholder=\"" + HttpUtility.HtmlEncode(Placeholder) + "\"");

                if (textAreaEvents != null)
                    textAreaEvents.Add_Events_HTML(Output);

                Output.Write(" >" + HttpUtility.HtmlEncode(allValues[i]) + "</textarea>");

                if (i == allValues.Count - 1)
                {
                    Output.WriteLine("</div>");
                }
                else
                {
                    Output.WriteLine("<br />");
                }
            }
            Output.WriteLine("            </div>");
            Output.WriteLine("          </td>");
            Output.WriteLine("          <td style=\"vertical-align:bottom\" >");
            if (Repeatable)
            {
                Output.WriteLine("            <a title=\"" + Translator.Get_Translation("Click to add another " + Title.ToLower(), CurrentLanguage) + ".\" href=\"" + Base_URL + "l/technical/javascriptrequired\" onmousedown=\"return " + html_element_name + "_add_new_item();\"><img class=\"repeat_button\" src=\"" + REPEAT_BUTTON_URL + "\" /></a>");
            }

            Output.WriteLine("            <a target=\"_" + html_element_name.ToUpper() + "\"  title=\"" + Translator.Get_Translation("Get help.", CurrentLanguage) + "\" href=\"" + Help_URL(Skin_Code, Base_URL) + "\" ><img class=\"help_button\" src=\"" + HELP_BUTTON_URL + "\" /></a>");

            Output.WriteLine("          </td>");
            Output.WriteLine("        </tr>");
            Output.WriteLine("      </table>");

            Output.WriteLine("    </td>");
            Output.WriteLine("  </tr>");
            Output.WriteLine();
        }

        /// <summary> Method helps to render all simple text area based elements </summary>
        /// <param name="Output"> Output for the generated html for this element </param>
        /// <param name="InstanceValue"> Value for the current digital resource to display </param>
        /// <param name="Skin_Code"> Code for the current html skin </param>
        /// <param name="IsMozilla"> Flag indicates if the browser is Mozilla Firefox</param>
        /// <param name="Current_User"> Current user, who's rights may impact the way an element is rendered </param>
        /// <param name="CurrentLanguage"> Current user-interface language </param>
        /// <param name="Translator"> Language support object which handles simple translational duties </param>
        /// <param name="Base_URL"> Base URL for the current request </param>
        protected void render_helper(TextWriter Output, string InstanceValue, string Skin_Code, bool IsMozilla, User_Object Current_User, Web_Language_Enum CurrentLanguage, Language_Support_Info Translator, string Base_URL)
        {
            string id_name = html_element_name.Replace("_", "");

            int actual_cols = Cols;
            if (IsMozilla)
                actual_cols = ColsMozilla;

            Output.WriteLine("  <!-- " + Title + " Element -->");
            Output.WriteLine("  <tr>");
            Output.WriteLine("    <td style=\"width:" + LEFT_MARGIN + "px\">&nbsp;</td>");
            if (Read_Only)
            {
                Output.WriteLine("    <td class=\"metadata_label\">" + Translator.Get_Translation(Title, CurrentLanguage) + ":</td>");
            }
            else
            {
                if (Acronym.Length > 0)
                {
                    Output.WriteLine("    <td class=\"metadata_label\"><a href=\"" + Help_URL(Skin_Code, Base_URL) + "\" target=\"_" + html_element_name.ToUpper() + "\"><acronym title=\"" + Acronym + "\">" + Translator.Get_Translation(Title, CurrentLanguage) + ":</acronym></a></td>");
                }
                else
                {
                    Output.WriteLine("    <td class=\"metadata_label\"><a href=\"" + Help_URL(Skin_Code, Base_URL) + "\" target=\"_" + html_element_name.ToUpper() + "\">" + Translator.Get_Translation(Title, CurrentLanguage) + ":</a></td>");
                }
            }
            Output.WriteLine("    <td>");
            const int i = 1;

            Output.WriteLine("      <table>");
            Output.WriteLine("        <tr>");
            Output.WriteLine("          <td>");
            Output.WriteLine("            <div id=\"" + html_element_name + "_div\">");

			Output.Write("              <textarea rows=\"" + Rows + "\" cols=\"" + actual_cols + "\" name=\"" + id_name + i + "\" id=\"" + id_name + i + "\" class=\"" + html_element_name + "_input sbk_Focusable\" ");
            if (!String.IsNullOrWhiteSpace(Placeholder))
                Output.Write(" placeholder=\"" + HttpUtility.HtmlEncode(Placeholder) + "\"");
            if ( textAreaEvents != null )
                textAreaEvents.Add_Events_HTML(Output);
            Output.WriteLine(" >" + HttpUtility.HtmlEncode(InstanceValue) + "</textarea></div>");

            Output.WriteLine("            </div>");
            Output.WriteLine("          </td>");
            Output.WriteLine("          <td valign=\"bottom\" >");
            if (Repeatable)
            {
                Output.WriteLine("            <a title=\"" + Translator.Get_Translation("Click to add another " + Title.ToLower(), CurrentLanguage) + ".\" href=\"" + Base_URL + "l/technical/javascriptrequired\" onmousedown=\"return " + html_element_name + "_add_new_item();\"><img class=\"repeat_button\" src=\"" + REPEAT_BUTTON_URL + "\" /></a>");
            }

            Output.WriteLine("            <a target=\"_" + html_element_name.ToUpper() + "\"  title=\"" + Translator.Get_Translation("Get help.", CurrentLanguage) + "\" href=\"" + Help_URL(Skin_Code, Base_URL) + "\" ><img class=\"help_button\" src=\"" + HELP_BUTTON_URL + "\" /></a>");
            Output.WriteLine("          </td>");
            Output.WriteLine("        </tr>");
            Output.WriteLine("      </table>");

            Output.WriteLine("    </td>");
            Output.WriteLine("  </tr>");
            Output.WriteLine();
        }

        #region Methods Implementing the Abstract Methods from abstract_Element class

        /// <summary> Reads the inner data from the CompleteTemplate XML format </summary>
        /// <param name="XMLReader"> Current template xml configuration reader </param>
        /// <remarks> This reads the default value from a <i>value</i> subelement </remarks>
        protected override void Inner_Read_Data( XmlReader XMLReader )
        {
            while ( XMLReader.Read() )
            {
                if (( XMLReader.NodeType == XmlNodeType.Element ) && ( XMLReader.Name.ToLower() == "value" ))
                {
                    XMLReader.Read();
                    DefaultValues.Add(XMLReader.Value.Trim());
                }

                if ((XMLReader.NodeType == XmlNodeType.Element) && (XMLReader.Name.ToLower() == "placeholder"))
                {
                    XMLReader.Read();
                    Placeholder = XMLReader.Value.Trim();
                }
            }
        }

        #endregion

        #region Methods and properties exposing events for the HTML elements in this base type

        private HtmlEventsHelper textAreaEvents;

        /// <summary> Access to the complete text area events object </summary>
        /// <remarks> Requesting this property will create a new object, if one does not already exist </remarks>
        protected HtmlEventsHelper TextBoxEvents
        {
            get { return textAreaEvents ?? (textAreaEvents = new HtmlEventsHelper()); }
        }

        /// <summary> Add some event text to an event on the primary text area for the citation control </summary>
        /// <param name="Event"> Type of the event to add text to </param>
        /// <param name="EventText"> Text (html format) to add to the event, such as "getElementById('demo').innerHTML = Date()", or "myFunction();return false;", etc.. </param>
        protected void Add_TextArea_Event(HtmlEventsEnum Event, string EventText)
        {
            // If the events is null, create it
            if (textAreaEvents == null)
                textAreaEvents = new HtmlEventsHelper();

            // Add this event
            textAreaEvents.Add_Event(Event, EventText);
        }

        #endregion
    }
}
