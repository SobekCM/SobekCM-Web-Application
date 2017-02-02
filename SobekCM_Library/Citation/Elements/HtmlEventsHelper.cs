using System;
using System.IO;
using System.Text;
using System.Web.UI;

namespace SobekCM.Library.Citation.Elements
{
    /// <summary> Enumeration of supported HTML events </summary>
    public enum HtmlEventsEnum : byte
    {
        /// <summary> Event is fired when the user clicks within the html element area </summary>
        onclick,

        /// <summary> Event is fired when the user right clicks to bring up the context menu within the html element area </summary>
        oncontextmenu,

        /// <summary> Event is fired when the user double clicks within the html element area </summary>
        ondblclick,

        /// <summary> Event is fired when the user presses down on a mouse button within the html element area </summary>
        onmousedown,

        /// <summary> Event is fired when the user moves the mouse cursor into the html element area </summary>
        onmouseenter,

        /// <summary> Event is fired when the user mouses away from the html element area </summary>
        onmouseleave,

        /// <summary> Event is fired when the user moves the mouse within the html element area </summary>
        onmousemove,

        /// <summary> Event is fired when the user mouses over the html element area </summary>
        onmouseover,

        /// <summary> Event is fired when the user mouses away from the html element area </summary>
        onmouseout,

        /// <summary> Event is fired when the user releases a mouse button within the html element area </summary>
        onmouseup,

        /// <summary> Event is fired when a key is pressed down clicks within the html element area </summary>
        onkeydown,

        /// <summary> Event is fired when a key is pressed within the html element area </summary>
        onkeypress,

        /// <summary> Event is fired when the a key is released within the html element area </summary>
        onkeyup,

        /// <summary> Event is fired when the html element is loaded </summary>
        onload,

        /// <summary> Event is fired when the user selects the html element area </summary>
        onselect,

        /// <summary> Event is fired when the user changes the value of the html element area </summary>
        onchange,

        /// <summary> Event is fired when the user moves the mouse wheel within the html element area </summary>
        onwheel
    }

    /// <summary> Class encapsulates most of the common HTML elements to allow javascript to be 
    /// added to events to a HTML element </summary>
    public class HtmlEventsHelper
    {
        /// <summary> Property exposes the onclick event for an HTML element </summary>
        public string OnClick { get; set; }

        /// <summary> Property exposes the oncontextmenu event for an HTML element </summary>
        public string OnContextMenu { get; set; }

        /// <summary> Property exposes the ondblclick event for an HTML element </summary>
        public string OnDblClick { get; set; }

        /// <summary> Property exposes the onmousedown event for an HTML element </summary>
        public string OnMouseDown { get; set; }

        /// <summary> Property exposes the onmouseenter event for an HTML element </summary>
        public string OnMouseEnter { get; set; }

        /// <summary> Property exposes the onmouseleave event for an HTML element </summary>
        public string OnMouseLeave { get; set; }

        /// <summary> Property exposes the onmousemove event for an HTML element </summary>
        public string OnMouseMove { get; set; }

        /// <summary> Property exposes the onmouseover event for an HTML element </summary>
        public string OnMouseOver { get; set; }

        /// <summary> Property exposes the onmouseout event for an HTML element </summary>
        public string OnMouseOut { get; set; }

        /// <summary> Property exposes the onmouseup event for an HTML element </summary>
        public string OnMouseUp { get; set; }

        /// <summary> Property exposes the onkeydown event for an HTML element </summary>
        public string OnKeyDown { get; set; }

        /// <summary> Property exposes the onkeypress event for an HTML element </summary>
        public string OnKeyPress { get; set; }

        /// <summary> Property exposes the onkeyup event for an HTML element </summary>
        public string OnKeyUp { get; set; }

        /// <summary> Property exposes the onload event for an HTML element </summary>
        public string OnLoad { get; set; }

        /// <summary> Property exposes the onselect event for an HTML element </summary>
        public string OnSelect { get; set; }

        /// <summary> Property exposes the onchange event for an HTML element </summary>
        public string OnChange { get; set; }

        /// <summary> Property exposes the onwheel event for an HTML element </summary>
        public string OnWheel { get; set; }

        /// <summary> Add some event text to an event </summary>
        /// <param name="Event"> Type of the event to add text to </param>
        /// <param name="EventText"> Text (html format) to add to the event, such as "getElementById('demo').innerHTML = Date()", or "myFunction();return false;", etc.. </param>
        public void Add_Event(HtmlEventsEnum Event, string EventText)
        {
            switch (Event)
            {
                case HtmlEventsEnum.onchange:
                    OnChange = EventText;
                    break;

                case HtmlEventsEnum.onclick:
                    OnClick = EventText;
                    break;

                case HtmlEventsEnum.oncontextmenu:
                    OnContextMenu = EventText;
                    break;

                case HtmlEventsEnum.ondblclick:
                    OnDblClick = EventText;
                    break;

                case HtmlEventsEnum.onkeydown:
                    OnKeyDown = EventText;
                    break;

                case HtmlEventsEnum.onkeypress:
                    OnKeyPress = EventText;
                    break;

                case HtmlEventsEnum.onkeyup:
                    OnKeyUp = EventText;
                    break;

                case HtmlEventsEnum.onload:
                    OnLoad = EventText;
                    break;

                case HtmlEventsEnum.onmousedown:
                    OnMouseDown = EventText;
                    break;

                case HtmlEventsEnum.onmouseenter:
                    OnMouseEnter = EventText;
                    break;

                case HtmlEventsEnum.onmouseleave:
                    OnMouseLeave = EventText;
                    break;

                case HtmlEventsEnum.onmousemove:
                    OnMouseMove = EventText;
                    break;

                case HtmlEventsEnum.onmouseout:
                    OnMouseOut = EventText;
                    break;

                case HtmlEventsEnum.onmouseover:
                    OnMouseOver = EventText;
                    break;

                case HtmlEventsEnum.onmouseup:
                    OnMouseUp = EventText;
                    break;

                case HtmlEventsEnum.onselect:
                    OnSelect = EventText;
                    break;

                case HtmlEventsEnum.onwheel:
                    OnWheel = EventText;
                    break;
            }
        }

        /// <summary> Return all these events as a string </summary>
        /// <returns> All existing events as a string </returns>
        public string ToString()
        {
            StringBuilder builder = new StringBuilder();
            using (StringWriter writer = new StringWriter(builder))
            {
                Add_Events(writer);
                writer.Flush();
                writer.Close();
            }
            return builder.ToString();
        }

        /// <summary> Adds the events for this html element directly to the text writer output </summary>
        /// <param name="Output"> Output to write directly to </param>
        public void Add_Events(TextWriter Output)
        {
            if ( !String.IsNullOrWhiteSpace(OnClick))
                Output.Write("onclick=\"" + OnClick.Replace("\"","'") + "\" ");

            if ( !String.IsNullOrWhiteSpace(OnContextMenu))
                Output.Write("oncontextmenu=\"" + OnContextMenu.Replace("\"","'") + "\" ");

            if ( !String.IsNullOrWhiteSpace(OnDblClick))
                Output.Write("ondblclick=\"" + OnDblClick.Replace("\"","'") + "\" ");

            if ( !String.IsNullOrWhiteSpace(OnMouseDown))
                Output.Write("onmousedown=\"" + OnMouseDown.Replace("\"","'") + "\" ");

            if ( !String.IsNullOrWhiteSpace(OnMouseEnter))
                Output.Write("onmouseenter=\"" + OnMouseEnter.Replace("\"","'") + "\" ");

            if ( !String.IsNullOrWhiteSpace(OnMouseLeave))
                Output.Write("onmouseleave=\"" + OnMouseLeave.Replace("\"","'") + "\" ");

            if ( !String.IsNullOrWhiteSpace(OnMouseMove))
                Output.Write("onmousemove=\"" + OnMouseMove.Replace("\"","'") + "\" ");

            if ( !String.IsNullOrWhiteSpace(OnMouseOver))
                Output.Write("onmouseover=\"" + OnMouseOver.Replace("\"","'") + "\" ");

            if ( !String.IsNullOrWhiteSpace(OnMouseOut))
                Output.Write("onmouseout=\"" + OnMouseOut.Replace("\"","'") + "\" ");

            if ( !String.IsNullOrWhiteSpace(OnMouseUp))
                Output.Write("onmouseup=\"" + OnMouseUp.Replace("\"","'") + "\" ");

            if ( !String.IsNullOrWhiteSpace(OnKeyDown))
                Output.Write("onkeydown=\"" + OnKeyDown.Replace("\"","'") + "\" ");

            if ( !String.IsNullOrWhiteSpace(OnKeyPress))
                Output.Write("onkeypress=\"" + OnKeyPress.Replace("\"","'") + "\" ");

            if ( !String.IsNullOrWhiteSpace(OnKeyUp))
                Output.Write("onkeyup=\"" + OnKeyUp.Replace("\"","'") + "\" ");

            if ( !String.IsNullOrWhiteSpace(OnLoad))
                Output.Write("onload=\"" + OnLoad.Replace("\"","'") + "\" ");

            if ( !String.IsNullOrWhiteSpace(OnSelect))
                Output.Write("onselect=\"" + OnSelect.Replace("\"","'") + "\" ");

            if ( !String.IsNullOrWhiteSpace(OnChange))
                Output.Write("onchange=\"" + OnChange.Replace("\"","'") + "\" ");

            if ( !String.IsNullOrWhiteSpace(OnWheel))
                Output.Write("onwheel=\"" + OnWheel.Replace("\"","'") + "\" ");
        }
    }
}
