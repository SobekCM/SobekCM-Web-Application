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
    /// <summary> Abstract base class for all elements which are made up of two text boxes  </summary>
    /// <remarks> This class implements the <see cref="iElement"/> interface and extends the <see cref="abstract_Element"/> class. </remarks>
    public abstract class TextBox_TextBox_Element : abstract_Element
	{
        /// <summary> Protected field holds any label to place before the first text box </summary>
        protected string FirstLabel;

        /// <summary> Protected field holds any label to place before the second text box </summary>
        protected string SecondLabel;

        /// <summary> Constructor for a new instance of the TextBox_TextBox_Element class </summary>
        /// <param name="Title"> Title for this element </param>
        /// <param name="Html_Element_Name"> Name for the html components and styles for this element </param>
        protected TextBox_TextBox_Element(string Title, string Html_Element_Name)
		{
			base.Title = Title;
            html_element_name = Html_Element_Name;
            FirstLabel = String.Empty;
            SecondLabel = String.Empty;
		}

        /// <summary> Method helps to render the html for all elements based on TextBox_TextBox_Element class </summary>
        /// <param name="Output"> Output for the generate html for this element </param>
        /// <param name="InstanceValuesText1"> Value(s) for the current digital resource to display in the first text box</param>
        /// <param name="InstanceValuesText2" >Value(s) for the current digital resource to display in the second text box</param>
        /// <param name="Skin_Code"> Code for the current html skin </param>
        /// <param name="Current_User"> Current user, who's rights may impact the way an element is rendered </param>
        /// <param name="CurrentLanguage"> Current user-interface language </param>
        /// <param name="Translator"> Language support object which handles simple translational duties </param>
        /// <param name="Base_URL"> Base URL for the current request </param>
        protected void render_helper(TextWriter Output, List<string> InstanceValuesText1, List<string> InstanceValuesText2, string Skin_Code, User_Object Current_User, Web_Language_Enum CurrentLanguage, Language_Support_Info Translator, string Base_URL )
        {
            if ((InstanceValuesText1.Count == 0) && ( InstanceValuesText2.Count == 0 ))
            {
                render_helper(Output, String.Empty, String.Empty, Skin_Code, Current_User, CurrentLanguage, Translator, Base_URL);
                return;
            }

            if ((InstanceValuesText1.Count == 1) && (InstanceValuesText2.Count == 1))
            {
                render_helper(Output, InstanceValuesText1[0], InstanceValuesText2[0], Skin_Code, Current_User, CurrentLanguage, Translator, Base_URL);
                return;
            }


            string id_name = html_element_name.Replace("_", "");

            Output.WriteLine("  <!-- " + Title.Replace(":","") + " Element -->");
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
            Output.WriteLine("    <td>");

                Output.WriteLine("      <table>");
                Output.WriteLine("        <tr>");
                Output.WriteLine("          <td>");
                Output.WriteLine("            <div id=\"" + html_element_name + "_div\">");

                for (int i = 1; i <= InstanceValuesText1.Count; i++)
                {
                    // Write the first text
                    if (FirstLabel.Length > 0)
                    {
                        Output.Write("              <span class=\"metadata_sublabel2\">" + Translator.Get_Translation(FirstLabel, CurrentLanguage) + ":</span>");
                    }
                    else
                    {
                        Output.Write("              ");
                    }

                    // Write the first text box
					Output.Write("<input name=\"" + id_name + "_first" + i + "\" id=\"" + id_name + "_first" + i + "\" class=\"" + html_element_name + "_first_input sbk_Focusable\" type=\"text\" value=\"" + HttpUtility.HtmlEncode(InstanceValuesText1[i - 1]) + "\" ");
                    if (textBox1Events != null)
                        textBox1Events.Add_Events_HTML(Output);
                    Output.Write(" />");

                    // Write the second text
                    if (SecondLabel.Length > 0)
                    {
                        Output.Write("<span class=\"metadata_sublabel\">" + Translator.Get_Translation(SecondLabel, CurrentLanguage) + ":</span>");
                    }

                    // Write the second text box
                    Output.Write("<input name=\"" + id_name + "_second" + i + "\" id=\"" + id_name + "_second" + i + "\" class=\"" + html_element_name + "_second_input sbk_Focusable\" type=\"text\" value=\"" + HttpUtility.HtmlEncode(InstanceValuesText2[i - 1]) + "\" ");
                    if (textBox2Events != null)
                        textBox2Events.Add_Events_HTML(Output);
                    Output.Write(" />");
                    Output.WriteLine(i < InstanceValuesText1.Count ? "<br />" : "</div>");
                }

                Output.WriteLine("          </td>");
                Output.WriteLine("          <td style=\"vertical-align:bottom\" >");
                if (Repeatable)
                {
                    Output.WriteLine("            <a title=\"" + Translator.Get_Translation("Click to add another " + Title.ToLower(), CurrentLanguage) + ".\" href=\"" + Base_URL + "l/technical/javascriptrequired\" onmousedown=\"return add_two_text_box_element('" + html_element_name + "','" + FirstLabel + "','" + SecondLabel + "');\"><img class=\"repeat_button\" src=\"" + REPEAT_BUTTON_URL + "\" /></a>");
                }
                Output.WriteLine("            <a target=\"_" + html_element_name.ToUpper() + "\"  title=\"" + Translator.Get_Translation("Get help.", CurrentLanguage) + "\" href=\"" + Help_URL(Skin_Code, Base_URL) + "\" ><img class=\"help_button\" src=\"" + HELP_BUTTON_URL + "\" /></a>");
                Output.WriteLine("          </td>"); Output.WriteLine("        </tr>");
                Output.WriteLine("      </table>");
         
            Output.WriteLine("    </td>");
            Output.WriteLine("  </tr>");
            Output.WriteLine();
        }


        /// <summary> Method helps to render the html for all elements based on TextBox_TextBox_Element class </summary>
        /// <param name="Output"> Output for the generate html for this element </param>
        /// <param name="InstanceValueText1"> Value for the current digital resource to display in the first text box</param>
        /// <param name="InstanceValueText2" >Value for the current digital resource to display in the second text box</param>
        /// <param name="Skin_Code"> Code for the current html skin </param>
        /// <param name="Current_User"> Current user, who's rights may impact the way an element is rendered </param>
        /// <param name="CurrentLanguage"> Current user-interface language </param>
        /// <param name="Translator"> Language support object which handles simple translational duties </param>
        /// <param name="Base_URL"> Base URL for the current request </param>
        protected void render_helper(TextWriter Output, string InstanceValueText1, string InstanceValueText2, string Skin_Code, User_Object Current_User, Web_Language_Enum CurrentLanguage, Language_Support_Info Translator, string Base_URL)
        {
            string id_name = html_element_name.Replace("_", "");

            Output.WriteLine("  <!-- " + Title + " Element -->");
            Output.WriteLine("  <tr>");
            Output.WriteLine("    <td style=\"width:" + LEFT_MARGIN + "px\">&nbsp;</td>");
            if (Title.IndexOf(":") < 0)
            {
                if (Read_Only)
                {
                    Output.WriteLine("    <td class=\"metadata_label\">" + Title + ":</b></td>");
                }
                else
                {
                    if (Acronym.Length > 0)
                    {
                        Output.WriteLine("    <td class=\"metadata_label\"><a href=\"" + Help_URL(Skin_Code, Base_URL) + "\" target=\"_" + html_element_name.ToUpper() + "\"><acronym title=\"" + Acronym + "\">" + Title + ":</acronym></a></td>");
                    }
                    else
                    {
                        Output.WriteLine("    <td class=\"metadata_label\"><a href=\"" + Help_URL(Skin_Code, Base_URL) + "\" target=\"_" + html_element_name.ToUpper() + "\">" + Title + ":</a></td>");
                    }
                }
            }
            else
            {
                if (Read_Only)
                {
                    Output.WriteLine("    <td class=\"metadata_label\">" + Title + "</b></td>");
                }
                else
                {
                    if (Acronym.Length > 0)
                    {
                        Output.WriteLine("    <td class=\"metadata_label\"><a href=\"" + Help_URL(Skin_Code, Base_URL) + "\" target=\"_" + html_element_name.ToUpper() + "\"><acronym title=\"" + Acronym + "\">" + Title + "</acronym></a></td>");
                    }
                    else
                    {
                        Output.WriteLine("    <td class=\"metadata_label\"><a href=\"" + Help_URL(Skin_Code, Base_URL) + "\" target=\"_" + html_element_name.ToUpper() + "\">" + Title + "</a></td>");
                    }
                }
            }
            Output.WriteLine("    <td>");

            Output.WriteLine("      <table>");
            Output.WriteLine("        <tr>");
            Output.WriteLine("          <td>");
            Output.WriteLine("            <div id=\"" + html_element_name + "_div\">");

            const int i = 1;

            // Write the first text
            if (FirstLabel.Length > 0)
            {
                Output.Write("              <span class=\"metadata_sublabel2\">" + FirstLabel + ":</span>");
            }

            // Write the first text box
            Output.Write("<input name=\"" + id_name + "_first" + i + "\" id=\"" + id_name + "_first" + i + "\" class=\"" + html_element_name + "_first_input sbk_Focusable\" type=\"text\" value=\"" + HttpUtility.HtmlEncode(InstanceValueText1) + "\" ");
            if (textBox1Events != null)
                textBox1Events.Add_Events_HTML(Output);
            Output.Write(" />");

            // Write the second text
            if (SecondLabel.Length > 0)
            {
                Output.Write("<span class=\"metadata_sublabel\">" + SecondLabel + ":</span>");
            }

            // Write the second text box
			Output.Write("<input name=\"" + id_name + "_second" + i + "\" id=\"" + id_name + "_second" + i + "\" class=\"" + html_element_name + "_second_input sbk_Focusable\" type=\"text\" value=\"" + HttpUtility.HtmlEncode(InstanceValueText2) + "\" ");
            if (textBox2Events != null)
                textBox2Events.Add_Events_HTML(Output);
            Output.Write(" />");

            Output.WriteLine("</div>");
            Output.WriteLine("          </td>");
            Output.WriteLine("          <td style=\"vertical-align:bottom\" >");
            if (Repeatable)
            {
                Output.WriteLine("            <a title=\"" + Translator.Get_Translation("Click to add another " + Title.ToLower(), CurrentLanguage) + ".\" href=\"" + Base_URL + "l/technical/javascriptrequired\" onmousedown=\"return add_two_text_box_element('" + html_element_name + "','" + FirstLabel + "','" + SecondLabel + "');\"><img class=\"repeat_button\" src=\"" + REPEAT_BUTTON_URL + "\" /></a>");
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
        /// <remarks> This procedure does not currently read any inner xml (not yet necessary) </remarks>
        protected override void Inner_Read_Data( XmlTextReader XMLReader )
        {
            // Do nothing
        }

        #endregion

        #region Methods and properties exposing events for the HTML elements in this base type

        private HtmlEventsHelper textBox1Events;
        private HtmlEventsHelper textBox2Events;

        /// <summary> Access to the complete text box events object for the first text box</summary>
        /// <remarks> Requesting this property will create a new object, if one does not already exist </remarks>
        protected HtmlEventsHelper TextBox1Events
        {
            get { return textBox1Events ?? (textBox1Events = new HtmlEventsHelper()); }
        }

        /// <summary> Access to the complete text box events object for the second text box</summary>
        /// <remarks> Requesting this property will create a new object, if one does not already exist </remarks>
        protected HtmlEventsHelper TextBox2Events
        {
            get { return textBox2Events ?? (textBox2Events = new HtmlEventsHelper()); }
        }

        /// <summary> Add some event text to an event on the first text box for the citation control </summary>
        /// <param name="Event"> Type of the event to add text to </param>
        /// <param name="EventText"> Text (html format) to add to the event, such as "getElementById('demo').innerHTML = Date()", or "myFunction();return false;", etc.. </param>
        protected void Add_TextBox1_Event(HtmlEventsEnum Event, string EventText)
        {
            // If the events is null, create it
            if (textBox1Events == null)
                textBox1Events = new HtmlEventsHelper();

            // Add this event
            textBox1Events.Add_Event(Event, EventText);
        }
        
        /// <summary> Add some event text to an event on the second text box for the citation control </summary>
        /// <param name="Event"> Type of the event to add text to </param>
        /// <param name="EventText"> Text (html format) to add to the event, such as "getElementById('demo').innerHTML = Date()", or "myFunction();return false;", etc.. </param>
        protected void Add_TextBox2_Event(HtmlEventsEnum Event, string EventText)
        {
            // If the events is null, create it
            if (textBox2Events == null)
                textBox2Events = new HtmlEventsHelper();

            // Add this event
            textBox2Events.Add_Event(Event, EventText);
        }

        #endregion
	}
}
