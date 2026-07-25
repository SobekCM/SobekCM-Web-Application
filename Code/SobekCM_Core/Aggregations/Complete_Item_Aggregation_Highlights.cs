#region Using directives

using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

#endregion

namespace SobekCM.Core.Aggregations
{
    /// <summary> Holds all the information about an item aggregation highlight, including text, image, tooltip, and link </summary>
    [Serializable, DataContract, ProtoContract]
    public class Complete_Item_Aggregation_Highlights
    {
        private readonly Dictionary<string, string> text;
        private readonly Dictionary<string, string> tooltips;

        /// <summary> Constructor for a new instance of the Item_Aggregation_Complete_Highlights class </summary>
        public Complete_Item_Aggregation_Highlights()
        {
            tooltips = new Dictionary<string, string>();
            text = new Dictionary<string, string>();
            Image = String.Empty;
        }

        /// <summary> Gets the dictionary of languages to text </summary>
        [DataMember(Name = "text", EmitDefaultValue = false), ProtoMember(3)]
        public Dictionary<string, string> Text_Dictionary
        {
            get { return text; }
        }

        /// <summary> Gets the dictionary of languages to tooltips </summary>
        [DataMember(Name = "tooltips", EmitDefaultValue = false), ProtoMember(4)]
        public Dictionary<string, string> Tooltip_Dictionary
        {
            get { return tooltips; }
        }

        /// <summary> Gets and set the link that the user goes to when they click on this image </summary>
        [DataMember(Name = "link", EmitDefaultValue = false), ProtoMember(2)]
        public string Link { get; set; }

        /// <summary> Gets and sets the image to display as the highlight </summary>
        [DataMember(Name = "image", EmitDefaultValue = false), ProtoMember(1)]
        public string Image { get; set; }

        internal void Write_In_Configuration_XML_File(StreamWriter Writer)
        {
            Writer.WriteLine("    <hi:highlight>");
            Writer.WriteLine("      <hi:source>" + Image + "</hi:source>");
            if (!String.IsNullOrEmpty(Link))
            {
                Writer.WriteLine("      <hi:link>" + Link + "</hi:link>");
            }
            foreach (KeyValuePair<string, string> thisTooltip in tooltips)
            {
                if (thisTooltip.Key == "")
                    Writer.WriteLine("      <hi:tooltip>" + thisTooltip.Value + "</hi:tooltip>");
                else
                    Writer.WriteLine("      <hi:tooltip lang=\"" + thisTooltip.Key + "\">" + thisTooltip.Value + "</hi:tooltip>");
            }
            foreach (KeyValuePair<string, string> thisText in text)
            {
                if (thisText.Key == "")
                    Writer.WriteLine("      <hi:text>" + thisText.Value + "</hi:text>");
                else
                    Writer.WriteLine("      <hi:text lang=\"" + thisText.Key + "\">" + thisText.Value + "</hi:text>");
            }
            Writer.WriteLine("    </hi:highlight>");
        }

        /// <summary> Add a language tooltip to this highlight </summary>
        /// <param name="Language">Language enumeration for this tooltip </param>
        /// <param name="Tooltip"> Tooltip </param>
        public void Add_Tooltip(string Language, string Tooltip)
        {
            tooltips[Language] = Tooltip;
        }

        /// <summary> Gets the language-specific tooltip, if one exists </summary>
        /// <param name="Language"> Language of the tooltip to retrieve </param>
        /// <returns> Language-specific tooltip </returns>
        public string Get_Tooltip(string Language)
        {
            if (tooltips.ContainsKey(Language))
                return tooltips[Language];

            if (tooltips.ContainsKey("default"))
                return tooltips["default"];

            if (tooltips.ContainsKey("en"))
                return tooltips["en"];

            if (tooltips.Count > 0)
                return tooltips.ElementAt(0).Value;

            return string.Empty;
        }

        /// <summary> Add a language text to this highlight </summary>
        /// <param name="Language">Language enumeration for this text </param>
        /// <param name="Text"> Text </param>
        public void Add_Text(string Language, string Text)
        {
            text[Language] = Text;
        }

        /// <summary> Gets the language-specific text, if one exists </summary>
        /// <param name="Language"> Language of the text to retrieve </param>
        /// <returns> Language-specific text </returns>
        public string Get_Text(string Language)
        {
            if (text.ContainsKey(Language))
                return text[Language];

            if (text.ContainsKey("default"))
                return text["default"];

            if (text.ContainsKey("en"))
                return text["en"];

            if (text.Count > 0)
                return text.ElementAt(0).Value;

            return string.Empty;
        }
    }
}



