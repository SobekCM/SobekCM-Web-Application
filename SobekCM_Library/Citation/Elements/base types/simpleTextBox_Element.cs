#region Using directives

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Web;
using System.Xml;
using SobekCM.Core.ApplicationState;
using SobekCM.Core.Configuration.Localization;
using SobekCM.Core.Users;

#endregion

namespace SobekCM.Library.Citation.Elements
{
    /// <summary> Abstract base class for all elements which are made up of a simple text box</summary>
    /// <remarks> This class implements the <see cref="iElement"/> interface and extends the <see cref="abstract_Element"/> class. </remarks>
    public abstract class SimpleTextBox_Element : abstract_Element
    {
        /// <summary> Protected field holds all the default values to display </summary>
        protected List<string> DefaultValues;

        /// <summary> Protected field holds the fixed type field from the template file </summary>
        protected string FixedTypeFromTemplateFile;

        /// <summary> Protected field holds the label field from the template file </summary>
        protected string LabelFromTemplateFile;

        /// <summary> Protected field holds any html to insert as the view choices option after the boxes </summary>
        protected string ViewChoicesString;

        /// <summary> Protected field holds placeholder that should appear in the empty textbox </summary>
        protected string Placeholder;


        /// <summary> Constructor for a new instance of the SimpleTextBox_Element class </summary>
        /// <param name="Title"> Title for this element </param>
        /// <param name="Html_Element_Name"> Name for the html components and styles for this element </param>
        protected SimpleTextBox_Element( string Title, string Html_Element_Name )
        {
            base.Title = Title;
            html_element_name = Html_Element_Name;
            ViewChoicesString = String.Empty;
            LabelFromTemplateFile = String.Empty;
            FixedTypeFromTemplateFile = String.Empty;

            DefaultValues = new List<string>();
        }

        /// <summary> Adds a new default value for this multiple text box type element </summary>
        /// <param name="DefaultValue"> New default value</param>
        public void Add_Default_Value(string DefaultValue)
        {
            DefaultValues.Add(DefaultValue);
        }

        /// <summary> Method helps to render all simple text box based elements </summary>
        /// <param name="Output"> Output for the generated html for this element </param>
        /// <param name="InstanceValues"> Value(s) for the current digital resource to display</param>
        /// <param name="Skin_Code"> Code for the current html skin </param>
        /// <param name="Current_User"> Current user, who's rights may impact the way an element is rendered </param>
        /// <param name="CurrentLanguage"> Current user-interface language </param>
        /// <param name="Translator"> Language support object which handles simple translational duties </param>
        /// <param name="Base_URL"> Base URL for the current request </param>
        protected void render_helper(TextWriter Output, ReadOnlyCollection<string> InstanceValues, string Skin_Code, User_Object Current_User, Web_Language_Enum CurrentLanguage, Language_Support_Info Translator, string Base_URL)
        {
            string id_name = html_element_name.Replace("_", "");
            render_helper(Output, InstanceValues, Skin_Code, Current_User, CurrentLanguage, Translator, Base_URL, id_name);
        }

        /// <summary> Method helps to render all simple text box based elements </summary>
        /// <param name="Output"> Output for the generated html for this element </param>
        /// <param name="InstanceValues"> Value(s) for the current digital resource to display</param>
        /// <param name="Skin_Code"> Code for the current html skin </param>
        /// <param name="Current_User"> Current user, who's rights may impact the way an element is rendered </param>
        /// <param name="CurrentLanguage"> Current user-interface language </param>
        /// <param name="Translator"> Language support object which handles simple translational duties </param>
        /// <param name="Base_URL"> Base URL for the current request </param>
        protected void render_helper(TextWriter Output, List<string> InstanceValues, string Skin_Code, User_Object Current_User, Web_Language_Enum CurrentLanguage, Language_Support_Info Translator, string Base_URL)
        {
            string id_name = html_element_name.Replace("_", "");
            render_helper(Output, new ReadOnlyCollection<string>(InstanceValues), Skin_Code, Current_User, CurrentLanguage, Translator, Base_URL, id_name);
        }

        /// <summary> Method helps to render all simple text box based elements </summary>
        /// <param name="Output"> Output for the generated html for this element </param>
        /// <param name="InstanceValues"> Value(s) for the current digital resource to display</param>
        /// <param name="Skin_Code"> Code for the current html skin </param>
        /// <param name="Current_User"> Current user, who's rights may impact the way an element is rendered </param>
        /// <param name="CurrentLanguage"> Current user-interface language </param>
        /// <param name="Translator"> Language support object which handles simple translational duties </param>
        /// <param name="HTML_ID_Name"> ID name used for these elements.  This is usually provided when there are multiple fixed-roles or fixed-type elements </param>
        /// <param name="Base_URL"> Base URL for the current request </param>
        protected void render_helper(TextWriter Output, List<string> InstanceValues, string Skin_Code, User_Object Current_User, Web_Language_Enum CurrentLanguage, Language_Support_Info Translator, string Base_URL, string HTML_ID_Name)
        {
            render_helper(Output, new ReadOnlyCollection<string>(InstanceValues), Skin_Code, Current_User, CurrentLanguage, Translator, Base_URL, HTML_ID_Name);
        }

        /// <summary> Method helps to render all simple text box based elements </summary>
        /// <param name="Output"> Output for the generated html for this element </param>
        /// <param name="InstanceValues"> Value(s) for the current digital resource to display</param>
        /// <param name="Skin_Code"> Code for the current html skin </param>
        /// <param name="Current_User"> Current user, who's rights may impact the way an element is rendered </param>
        /// <param name="CurrentLanguage"> Current user-interface language </param>
        /// <param name="Translator"> Language support object which handles simple translational duties </param>
        /// <param name="Base_URL"> Base URL for the current request </param>
        /// <param name="HTML_ID_Name"> ID name used for these elements.  This is usually provided when there are multiple fixed-roles or fixed-type elements </param>
        protected void render_helper(TextWriter Output, ReadOnlyCollection<string> InstanceValues, string Skin_Code, User_Object Current_User, Web_Language_Enum CurrentLanguage, Language_Support_Info Translator, string Base_URL, string HTML_ID_Name)
        {
            List<string> allValues = new List<string>();
            allValues.AddRange(DefaultValues);
            allValues.AddRange(InstanceValues);

            if (allValues.Count == 0)
            {
                render_helper(Output, String.Empty, Skin_Code, Current_User, CurrentLanguage, Translator, Base_URL, HTML_ID_Name);
                return;
            }

            if (allValues.Count == 1)
            {
                render_helper(Output, allValues[0], Skin_Code, Current_User, CurrentLanguage, Translator, Base_URL, HTML_ID_Name);
                return;
            }

            Output.WriteLine("  <!-- " + Title + " Element -->");
            Output.WriteLine("  <tr>");
            Output.WriteLine("    <td style=\"width:" + LEFT_MARGIN + "px\">&nbsp;</td>");
            if (Title.IndexOf(":") < 0)
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
            else
            {
                if (Acronym.Length > 0)
                {
                    Output.WriteLine("    <td class=\"metadata_label\"><a href=\"" + Help_URL(Skin_Code, Base_URL) + "\" target=\"_" + html_element_name.ToUpper() + "\"><acronym title=\"" + Acronym + "\">" + Translator.Get_Translation(Title, CurrentLanguage) + "</acronym></a></td>");
                }
                else
                {
                    Output.WriteLine("    <td class=\"metadata_label\"><a href=\"" + Help_URL(Skin_Code, Base_URL) + "\" target=\"_" + html_element_name.ToUpper() + "\">" + Translator.Get_Translation(Title, CurrentLanguage) + "</a></td>");
                }
            }


            if (Read_Only)
            {
                Output.WriteLine("    <td>");
                Output.WriteLine("      <table>");
                Output.WriteLine("        <tr>");
                Output.Write("          <td><div class=\"" + HTML_ID_Name + "_div\">");
                for (int i = 0; i < InstanceValues.Count; i++)
                {
                    Output.Write(InstanceValues[i]);
                    if (i < (InstanceValues.Count - 1))
                        Output.Write("<br />");
                }
                Output.WriteLine("</div></td>");
                Output.WriteLine("          <td style=\"vertical-align:bottom\" >");
                Output.WriteLine("            <a target=\"_" + html_element_name.ToUpper() + "\"  title=\"" + Translator.Get_Translation("Get help.", CurrentLanguage) + "\" href=\"" + Help_URL(Skin_Code, Base_URL) + "\" ><img class=\"help_button\" src=\"" + HELP_BUTTON_URL + "\" /></a>");
                Output.WriteLine("          </td>");
                Output.WriteLine("        </tr>");
                Output.WriteLine("      </table>");
                Output.WriteLine("    </td>");
            }
            else
            {
                Output.WriteLine("    <td>");
                Output.WriteLine("      <table>");
                Output.WriteLine("        <tr>");
                Output.WriteLine("          <td>");
                Output.WriteLine("            <div id=\"" + HTML_ID_Name + "_div\">");

                for (int i = 1; i <= allValues.Count; i++)
                {
                    Output.Write("              <input name=\"" + HTML_ID_Name + i + "\" id=\"" + HTML_ID_Name + i + "\" class=\"" + html_element_name + "_input sbk_Focusable\" type=\"text\" value=\"" + HttpUtility.HtmlEncode(allValues[i - 1].Replace("<i>", "").Replace("</i>", "")) + "\" ");

                    if (!String.IsNullOrWhiteSpace(Placeholder))
                        Output.Write(" placeholder=\"" + HttpUtility.HtmlEncode(Placeholder) + "\"");

                    if (textBoxEvents != null) 
                        textBoxEvents.Add_Events_HTML(Output);
                    


                    if (i == allValues.Count)
                    {
                        Output.WriteLine(" />");
                    }
                    else
                    {
                        Output.WriteLine(" /><br />");
                    }
                }

                Output.WriteLine("            </div>");
                Output.WriteLine("          </td>");
                Output.WriteLine("          <td style=\"vertical-align:bottom\" >");

                if (ViewChoicesString.Length > 0)
                {
                    Output.WriteLine("            " + ViewChoicesString.Replace("<%INTERFACE%>", Skin_Code) + "&nbsp; ");
                }

                if (Repeatable)
                {
                    Output.WriteLine("          <a title=\"" + Translator.Get_Translation("Click to add another " + Title.ToLower(), CurrentLanguage) + ".\" href=\"" + Base_URL + "l/technical/javascriptrequired\" onmousedown=\"return add_new_element_adv('" + HTML_ID_Name + "', '" + html_element_name + "');\"><img class=\"repeat_button\" src=\"" + REPEAT_BUTTON_URL + "\" /></a>");
                }

                Output.WriteLine("            <a target=\"_" + html_element_name.ToUpper() + "\"  title=\"" + Translator.Get_Translation("Get help.", CurrentLanguage) + "\" href=\"" + Help_URL(Skin_Code, Base_URL) + "\" ><img class=\"help_button\" src=\"" + HELP_BUTTON_URL + "\" /></a>");

                Output.WriteLine("          </td>");
                Output.WriteLine("        </tr>");
                Output.WriteLine("      </table>");

                Output.WriteLine("    </td>");
            }

            Output.WriteLine("  </tr>");
            Output.WriteLine();
        }

        /// <summary> Method helps to render all multiple text box based elements </summary>
        /// <param name="Output"> Output for the generated html for this element </param>
        /// <param name="InstanceValue"> Value for the current digital resource to display</param>
        /// <param name="Skin_Code"> Code for the current html skin </param>
        /// <param name="Current_User"> Current user, who's rights may impact the way an element is rendered </param>
        /// <param name="CurrentLanguage"> Current user-interface language </param>
        /// <param name="Translator"> Language support object which handles simple translational duties </param>
        /// <param name="Base_URL"> Base URL for the current request </param>
        protected void render_helper(TextWriter Output, string InstanceValue, string Skin_Code, User_Object Current_User, Web_Language_Enum CurrentLanguage, Language_Support_Info Translator, string Base_URL )
        {
            string id_name = html_element_name.Replace("_", "");
            render_helper(Output, InstanceValue, Skin_Code, Current_User, CurrentLanguage, Translator, Base_URL, id_name);
        }

        /// <summary> Method helps to render all multiple text box based elements </summary>
        /// <param name="Output"> Output for the generated html for this element </param>
        /// <param name="InstanceValue"> Value for the current digital resource to display</param>
        /// <param name="Skin_Code"> Code for the current html skin </param>
        /// <param name="Current_User"> Current user, who's rights may impact the way an element is rendered </param>
        /// <param name="CurrentLanguage"> Current user-interface language </param>
        /// <param name="Translator"> Language support object which handles simple translational duties </param>
        /// <param name="Base_URL"> Base URL for the current request </param>
        /// <param name="HTML_ID_Name"> ID name used for these elements.  This is usually provided when there are multiple fixed-roles or fixed-type elements </param>
        protected void render_helper(TextWriter Output, string InstanceValue, string Skin_Code, User_Object Current_User, Web_Language_Enum CurrentLanguage, Language_Support_Info Translator, string Base_URL, string HTML_ID_Name)
        {
            Output.WriteLine("  <!-- " + Title + " Element -->");
            Output.WriteLine("  <tr>");
            Output.WriteLine("    <td style=\"width:" + LEFT_MARGIN + "px\">&nbsp;</td>");

            // Get the label to show
            string label_to_show = Title.Replace(":", "");
            if (LabelFromTemplateFile.Length > 0)
                label_to_show = LabelFromTemplateFile;

            if (Acronym.Length > 0)
            {
                Output.WriteLine("    <td class=\"metadata_label\"><a href=\"" + Help_URL(Skin_Code, Base_URL) + "\" target=\"_" + html_element_name.ToUpper() + "\"><acronym title=\"" + Acronym + "\">" + Translator.Get_Translation(label_to_show, CurrentLanguage) + ":</acronym></a></td>");
            }
            else
            {
                Output.WriteLine("    <td class=\"metadata_label\"><a href=\"" + Help_URL(Skin_Code, Base_URL) + "\" target=\"_" + html_element_name.ToUpper() + "\">" + Translator.Get_Translation(label_to_show, CurrentLanguage) + ":</a></td>");
            }

            if (Read_Only)
            {
                Output.WriteLine("    <td>");
                Output.WriteLine("      <table>");
                Output.WriteLine("        <tr>");
                Output.WriteLine("          <td><div class=\"" + HTML_ID_Name + "_div\">" + InstanceValue + "</div></td>");
                Output.WriteLine("          <td style=\"vertical-align:bottom\" >");
                Output.WriteLine("            <a target=\"_" + html_element_name.ToUpper() + "\"  title=\"" + Translator.Get_Translation("Get help.", CurrentLanguage) + "\" href=\"" + Help_URL(Skin_Code, Base_URL) + "\" ><img border=\"0px\" class=\"help_button\" src=\"" + HELP_BUTTON_URL + "\" /></a>");
                Output.WriteLine("          </td>");
                Output.WriteLine("        </tr>");
                Output.WriteLine("      </table>");
                Output.WriteLine("    </td>");
            }
            else
            {
                Output.WriteLine("    <td>");

                Output.WriteLine("      <table>");
                Output.WriteLine("        <tr>");
                Output.WriteLine("          <td>");
                Output.WriteLine("            <div id=\"" + HTML_ID_Name + "_div\">");

                Output.Write("              <input name=\"" + HTML_ID_Name + "1\" id=\"" + HTML_ID_Name + "1\" class=\"" + html_element_name + "_input sbk_Focusable\" type=\"text\" value=\"" + HttpUtility.HtmlEncode(InstanceValue.Replace("<i>", "").Replace("</i>", "")) + "\" ");
                if (!String.IsNullOrWhiteSpace(Placeholder))
                    Output.Write(" placeholder=\"" + HttpUtility.HtmlEncode(Placeholder) + "\"");
                if (textBoxEvents != null)
                    textBoxEvents.Add_Events_HTML(Output);
                Output.WriteLine(" />");

                Output.WriteLine("            </div>");
                Output.WriteLine("          </td>");
                Output.WriteLine("          <td style=\"vertical-align:bottom\" >");

                if (ViewChoicesString.Length > 0)
                {
                    Output.WriteLine("            " + ViewChoicesString.Replace("<%INTERFACE%>", Skin_Code) + "&nbsp; ");
                }

                if (Repeatable)
                {
                    Output.WriteLine("          <a title=\"" + Translator.Get_Translation("Click to add another " + Title.ToLower(), CurrentLanguage) + ".\" href=\"" + Base_URL + "l/technical/javascriptrequired\" onmousedown=\"return add_new_element_adv('" + HTML_ID_Name + "', '" + html_element_name + "');\"><img class=\"repeat_button\" src=\"" + REPEAT_BUTTON_URL + "\" /></a>");
                }

                Output.WriteLine("            <a target=\"_" + html_element_name.ToUpper() + "\"  title=\"" + Translator.Get_Translation("Get help.", CurrentLanguage) + "\" href=\"" + Help_URL(Skin_Code, Base_URL) + "\" ><img class=\"help_button\" src=\"" + HELP_BUTTON_URL + "\" /></a>");

                Output.WriteLine("          </td>");
                Output.WriteLine("        </tr>");
                Output.WriteLine("      </table>");

                Output.WriteLine("    </td>");
            }

            Output.WriteLine("  </tr>");
            Output.WriteLine();
        }

        #region Methods Implementing the Abstract Methods from abstract_Element class

        /// <summary> Reads the inner data from the CompleteTemplate XML format </summary>
        /// <param name="XMLReader"> Current template xml configuration reader </param>
        /// <remarks> This reads the default value from a <i>value</i> subelement and the <i>label</i> subelement, which is used in several of the classes that extend this one </remarks>
        protected override void Inner_Read_Data(XmlReader XMLReader)
        {
            while (XMLReader.Read())
            {
                if ((XMLReader.NodeType == XmlNodeType.Element) && (XMLReader.Name.ToLower() == "value"))
                {
                    XMLReader.Read();
                    DefaultValues.Add(XMLReader.Value.Trim());
                }

                if ((XMLReader.NodeType == XmlNodeType.Element) && (XMLReader.Name.ToLower() == "label"))
                {
                    XMLReader.Read();
                    LabelFromTemplateFile = XMLReader.Value.Trim();
                }

                if ((XMLReader.NodeType == XmlNodeType.Element) && (XMLReader.Name.ToLower() == "fixed_type"))
                {
                    XMLReader.Read();
                    FixedTypeFromTemplateFile = XMLReader.Value.Trim();
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

        #endregion
    }
}
