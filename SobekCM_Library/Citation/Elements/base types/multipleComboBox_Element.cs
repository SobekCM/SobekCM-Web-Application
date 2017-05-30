#region Using directives

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml;
using SobekCM.Core.ApplicationState;
using SobekCM.Core.Configuration.Localization;
using SobekCM.Core.Users;
using SobekCM.Resource_Object;

#endregion

namespace SobekCM.Library.Citation.Elements
{
    /// <summary> Abstract base class for all elements which are made up of multiple small combo/select boxes </summary>
    /// <remarks> This class implements the <see cref="iElement"/> interface and extends the <see cref="abstract_Element"/> class. </remarks>
    public abstract class MultipleComboBox_Element : abstract_Element
	{
        /// <summary> Protected field holds how many boxes are allowed per line, or -1 if there is no limit </summary>
        protected int BoxesPerLine = -1;

        /// <summary> Protected field holds all the possible, selectable values </summary>
        protected List<string> Items;

        /// <summary> Protected field holds the defauls, if this is set as a constant </summary>
        protected List<string> Defaults;

        /// <summary> Protected field holds how many boxes are allowed total for this element, or -1 if there is no limit </summary>
        protected int MaxBoxes = -1;

        /// <summary> Protected field holds any html to insert as the view choices option after the boxes </summary>
        protected string ViewChoicesString;



        /// <summary> Constructor for a new instance of the MultipleComboBox_Element class </summary>
        /// <param name="Title"> Title for this element </param>
        /// <param name="Html_Element_Name"> Name for the html components and styles for this element </param>
        protected MultipleComboBox_Element(string Title, string Html_Element_Name)
		{
			base.Title = Title;
            html_element_name = Html_Element_Name;
            ViewChoicesString = String.Empty;

            Items = new List<string>();
            Defaults = new List<string>();
		}

        /// <summary> Adds a possible, selectable value to the combo/select box </summary>
        /// <param name="NewItem"> New possible, selectable value </param>
        public void Add_Item(string NewItem)
        {
            if (!Items.Contains(NewItem))
            {
                Items.Add(NewItem);
            }
        }

        /// <summary> Adds a series of possible, selectable values to the combo/select box </summary>
        /// <param name="New_Items"> List of new possible, selectable value </param>
        public void Add_Items(string[] New_Items)
        {
            foreach (string thisItem in New_Items)
            {
                Add_Item(thisItem);
            }
        }

        /// <summary> Method helps to render all multiple combo box based elements </summary>
        /// <param name="Output"> Output for the generated html for this element </param>
        /// <param name="InstanceValues"> Value(s) for the current digital resource to display</param>
        /// <param name="Skin_Code"> Code for the current html skin </param>
        /// <param name="Current_User"> Current user, who's rights may impact the way an element is rendered </param>
        /// <param name="CurrentLanguage"> Current user-interface language </param>
        /// <param name="Translator"> Language support object which handles simple translational duties </param>
        /// <param name="Base_URL"> Base URL for the current request </param>
        protected void render_helper(TextWriter Output, List<string> InstanceValues, string Skin_Code, User_Object Current_User, Web_Language_Enum CurrentLanguage, Language_Support_Info Translator, string Base_URL)
        {
            render_helper(Output, new ReadOnlyCollection<string>(InstanceValues), Items, Skin_Code, Current_User, CurrentLanguage, Translator, Base_URL );
        }

        /// <summary> Method helps to render all multiple combo box based elements </summary>
        /// <param name="Output"> Output for the generated html for this element </param>
        /// <param name="InstanceValues"> Value(s) for the current digital resource to display</param>
        /// <param name="Skin_Code"> Code for the current html skin </param>
        /// <param name="Current_User"> Current user, who's rights may impact the way an element is rendered </param>
        /// <param name="CurrentLanguage"> Current user-interface language </param>
        /// <param name="Translator"> Language support object which handles simple translational duties </param>
        /// <param name="Base_URL"> Base URL for the current request </param>
        protected void render_helper(TextWriter Output, ReadOnlyCollection<string> InstanceValues, string Skin_Code, User_Object Current_User, Web_Language_Enum CurrentLanguage, Language_Support_Info Translator, string Base_URL)
        {
            render_helper(Output, InstanceValues, Items, Skin_Code, Current_User, CurrentLanguage, Translator, Base_URL);
        }

        /// <summary> Method helps to render all multiple combo box based elements </summary>
        /// <param name="Output"> Output for the generated html for this element </param>
        /// <param name="InstanceValues"> Value(s) for the current digital resource to display</param>
        /// <param name="PossibleValues"> Possible vlaues for the combo boxes </param>
        /// <param name="Skin_Code"> Code for the current html skin </param>
        /// <param name="Current_User"> Current user, who's rights may impact the way an element is rendered </param>
        /// <param name="CurrentLanguage"> Current user-interface language </param>
        /// <param name="Translator"> Language support object which handles simple translational duties </param>
        /// <param name="Base_URL"> Base URL for the current request </param>
        protected void render_helper(TextWriter Output, List<string> InstanceValues, List<string> PossibleValues, string Skin_Code, User_Object Current_User, Web_Language_Enum CurrentLanguage, Language_Support_Info Translator, string Base_URL)
        {
            render_helper(Output, new ReadOnlyCollection<string>(InstanceValues), PossibleValues, Skin_Code, Current_User, CurrentLanguage, Translator, Base_URL);
        }

        /// <summary> Method helps to render all multiple combo box based elements </summary>
        /// <param name="Output"> Output for the generated html for this element </param>
        /// <param name="InstanceValue"> Value for the current digital resource to display</param>
        /// <param name="Skin_Code"> Code for the current html skin </param>
        /// <param name="Current_User"> Current user, who's rights may impact the way an element is rendered </param>
        /// <param name="CurrentLanguage"> Current user-interface language </param>
        /// <param name="Translator"> Language support object which handles simple translational duties </param>
        /// <param name="Base_URL"> Base URL for the current request </param>
        protected void render_helper(TextWriter Output, string InstanceValue, string Skin_Code, User_Object Current_User, Web_Language_Enum CurrentLanguage, Language_Support_Info Translator, string Base_URL)
        {
            render_helper(Output, InstanceValue, Items, Skin_Code, Current_User, CurrentLanguage, Translator, Base_URL);
        }

        /// <summary> Method helps to render all multiple combo box based elements </summary>
        /// <param name="Output"> Output for the generated html for this element </param>
        /// <param name="InstanceValues"> Value(s) for the current digital resource to display</param>
        /// <param name="PossibleValues"> Possible vlaues for the combo boxes </param>
        /// <param name="Skin_Code"> Code for the current html skin </param>
        /// <param name="Current_User"> Current user, who's rights may impact the way an element is rendered </param>
        /// <param name="CurrentLanguage"> Current user-interface language </param>
        /// <param name="Translator"> Language support object which handles simple translational duties </param>
        /// <param name="Base_URL"> Base URL for the current request </param>
        protected void render_helper(TextWriter Output, ReadOnlyCollection<string> InstanceValues, List<string> PossibleValues, string Skin_Code, User_Object Current_User, Web_Language_Enum CurrentLanguage, Language_Support_Info Translator, string Base_URL )
        {
            List<string> allValues = new List<string>();
            allValues.AddRange(InstanceValues);

            if (allValues.Count == 0)
            {
                render_helper(Output, String.Empty, PossibleValues, Skin_Code, Current_User, CurrentLanguage, Translator, Base_URL);
                return;
            }

            if (allValues.Count == 1)
            {
                render_helper(Output, allValues[0], PossibleValues, Skin_Code, Current_User, CurrentLanguage, Translator, Base_URL);
                return;
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


            if (Read_Only)
            {
                Output.Write("    <td>");
                for (int i = 0; i < InstanceValues.Count; i++)
                {
                    Output.Write(InstanceValues[i]);
                    if (i < (InstanceValues.Count - 1))
                        Output.Write("<br />");
                }
                Output.WriteLine("</td>");
            }
            else
            {
                Output.WriteLine("    <td>");
                Output.WriteLine("      <table>");
                Output.WriteLine("        <tr>");
                Output.WriteLine("          <td>");
                Output.WriteLine("            <div id=\"" + html_element_name + "_div\">");
                for (int i = 1; i <= allValues.Count; i++)
                {
                    string value = allValues[i - 1];
                    Output.Write("              <select name=\"" + id_name + i + "\" id=\"" + id_name + i + "\" class=\"" + html_element_name + "_input\" type=\"text\" ");
                    if (comboBoxEvents != null)
                        comboBoxEvents.Add_Events_HTML(Output);
                    Output.WriteLine(" >");


                    bool found = false;
                    if (value.Length == 0)
                    {
                        found = true;
                        Output.WriteLine("                <option value=\"\" selected=\"selected\" >&nbsp;</option>");
                    }
                    else
                    {
                        Output.WriteLine("                <option value=\"\">&nbsp;</option>");
                    }
                    foreach (string item in PossibleValues)
                    {
                        if (item.ToUpper() == value.ToUpper())
                        {
                            found = true;
                            Output.WriteLine("                <option value=\"" + item + "\" selected=\"selected\" >" + item + "</option>");
                        }
                        else
                        {
                            Output.WriteLine("                <option value=\"" + item + "\">" + item + "</option>");
                        }
                    }
                    if (!found)
                    {
                        Output.WriteLine("                <option value=\"" + value + "\" selected=\"selected\" >" + value + "</option>");
                    }
                    Output.WriteLine("              </select>");

                    if (i == allValues.Count)
                    {
                        Output.WriteLine("</div>");
                    }
                }

                Output.WriteLine("          </td>");
                Output.WriteLine("          <td style=\"vertical-align:bottom\" >");

                if ( !String.IsNullOrEmpty(ViewChoicesString))
                {
                    Output.WriteLine("            " + ViewChoicesString.Replace("<%WEBSKIN%>", Skin_Code).Replace("<%?URLOPTS%>", "") + "&nbsp; ");
                }

                if ((Repeatable) && ((MaxBoxes < 0) || (allValues.Count < MaxBoxes)))
                {
                    Output.WriteLine("          <span id=\"" + html_element_name + "_repeaticon\" name=\"" + html_element_name + "_repeaticon\"><img title=\"" + Translator.Get_Translation("Click to add another " + Title.ToLower(), CurrentLanguage) + ".\" alt=\"+\" class=\"repeat_button\" src=\"" + REPEAT_BUTTON_URL + "\" onmousedown=\"add_new_multi_combo_element('" + html_element_name + "', " + allValues.Count + "," + MaxBoxes + "," + BoxesPerLine + "); return false;\" /></span>");
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


        /// <summary> Method helps to render all multiple combo box based elements </summary>
        /// <param name="Output"> Output for the generated html for this element </param>
        /// <param name="InstanceValue"> Value for the current digital resource to display</param>
        /// <param name="PossibleValues"> Possible vlaues for this combo boxes </param>
        /// <param name="Skin_Code"> Code for the current html skin </param>
        /// <param name="Current_User"> Current user, who's rights may impact the way an element is rendered </param>
        /// <param name="CurrentLanguage"> Current user-interface language </param>
        /// <param name="Translator"> Language support object which handles simple translational duties </param>
        /// <param name="Base_URL"> Base URL for the current request </param>
        protected void render_helper(TextWriter Output, string InstanceValue, List<string> PossibleValues, string Skin_Code, User_Object Current_User, Web_Language_Enum CurrentLanguage, Language_Support_Info Translator, string Base_URL )
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


            if (Read_Only)
            {
                Output.Write("    <td>");
                Output.Write(InstanceValue);
                Output.WriteLine("</td>");
            }
            else
            {
                Output.WriteLine("    <td>");
                Output.WriteLine("      <table>");
                Output.WriteLine("        <tr>");
                Output.WriteLine("          <td>");
                Output.WriteLine("            <div id=\"" + html_element_name + "_div\">");
                
                const int i = 1;

                string value = InstanceValue;
                    Output.Write("             <select name=\"" + id_name + i + "\" id=\"" + id_name + i + "\" class=\"" + html_element_name + "_input\" type=\"text\" ");
                    if (comboBoxEvents != null)
                        comboBoxEvents.Add_Events_HTML(Output);
                    Output.WriteLine(" >");

                    bool found = false;
                    if (value.Length == 0)
                    {
                        found = true;
                        Output.WriteLine("                <option value=\"\" selected=\"selected\" >&nbsp;</option>");
                    }
                    else
                    {
                        Output.WriteLine("                <option value=\"\">&nbsp;</option>");
                    }
                    foreach (string item in PossibleValues)
                    {
                        if (item.ToUpper() == value.ToUpper())
                        {
                            found = true;
                            Output.WriteLine("                <option value=\"" + item + "\" selected=\"selected\" >" + item + "</option>");
                        }
                        else
                        {
                            Output.WriteLine("                <option value=\"" + item + "\">" + item + "</option>");
                        }
                    }
                    if (!found)
                    {
                        Output.WriteLine("                <option value=\"" + value + "\" selected=\"selected\" >" + value + "</option>");
                    }
                    Output.WriteLine("              </select>");

                    Output.WriteLine("</div>");
                }

                Output.WriteLine("          </td>");
                Output.WriteLine("          <td style=\"vertical-align:bottom\" >");

                if (!String.IsNullOrEmpty(ViewChoicesString))
                {
                    Output.WriteLine("            " + ViewChoicesString.Replace("<%WEBSKIN%>", Skin_Code).Replace("<%?URLOPTS%>","") + "&nbsp; ");
                }

                if (Repeatable)
                {
                    Output.WriteLine("          <span id=\"" + html_element_name + "_repeaticon\" name=\"" + html_element_name + "_repeaticon\"><img title=\"" + Translator.Get_Translation("Click to add another " + Title.ToLower(), CurrentLanguage) + ".\" alt=\"+\" class=\"repeat_button\" src=\"" + REPEAT_BUTTON_URL + "\" onmousedown=\"add_new_multi_combo_element('" + html_element_name + "', 1," + MaxBoxes + "," + BoxesPerLine + "); return false;\" /></span>");
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
        protected override void Inner_Read_Data(XmlReader XMLReader)
        {
            while ( XMLReader.Read() )
            {
                if ( XMLReader.NodeType == XmlNodeType.Element ) 
                {
                    if (XMLReader.Name.ToLower() == "value")
                    {
                        XMLReader.Read();
                        string inner_data = XMLReader.Value.Trim();
                        Add_Item(inner_data);

                        // If this not set as a default already, add it
                        if ( !Defaults.Contains(inner_data))
                            Defaults.Add(inner_data);

                    }
                    if (XMLReader.Name.ToLower() == "options")
                    {
                        XMLReader.Read();
                        string options = XMLReader.Value.Trim();
                        Items.Clear();
                        if (options.Length > 0)
                        {
                            string[] options_parsed = options.Split(",".ToCharArray());
                            foreach (string thisOption in options_parsed.Where(ThisOption => !Items.Contains(ThisOption.Trim())))
                            {
                                Items.Add(thisOption.Trim());
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region Methods and properties exposing events for the HTML elements in this base type

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



