#region Using directives

using SobekCM.Library.Citation.Elements;
using System;
using System.Xml;

#endregion

namespace SobekCM.Library.Citation.Template
{
    /// <summary> Reader for the CompleteTemplate XML configuration file which stores the information about a single metadata CompleteTemplate </summary>
    public class Template_XML_Reader
    {
        /// <summary> Reads the CompleteTemplate XML configuration file specified into a CompleteTemplate object </summary>
        /// <param name="XML_File"> Filename of the CompleteTemplate XML configuraiton file to read  </param>
        /// <param name="ThisCompleteTemplate"> CompleteTemplate object to populate form the configuration file </param>
        /// <param name="exclude_divisions"> Flag indicates whether to include the structure map, if included in the CompleteTemplate file </param>
        public void Read_XML(string XML_File, CompleteTemplate ThisCompleteTemplate, bool exclude_divisions)
        {
            // Load this MXF File
            var templateXml = new XmlDocument();
            templateXml.Load(XML_File);

            // create the node reader
            var nodeReader = new XmlNodeReader(templateXml);

            // Read through all main input CompleteTemplate tag is found
            move_to_node(nodeReader, "input_template");

            // Process all of the header information for this CompleteTemplate
            process_template_header(nodeReader, ThisCompleteTemplate);

            // Process all of the input portion / hierarchy
            process_inputs(nodeReader, ThisCompleteTemplate, exclude_divisions);

            // Process any constant sectoin
            process_constants(nodeReader, ThisCompleteTemplate);

            // Do any final processing
            ThisCompleteTemplate.Build_Final_Adjustment_And_Checks();

        }

        /// <summary> Parses one metadata block's XML into a <see cref="Template_Panel"/> </summary>
        /// <param name="BlockXml"> Raw XML content of a <c>SobekCM_Metadata_Block.BlockXml</c> row --
        /// a single &lt;panel&gt; fragment, the same shape as a panel nested inside a full template's
        /// &lt;inputs&gt;&lt;page&gt; section (see e.g. <c>ir.xml</c>) </param>
        /// <returns> Parsed panel (title + elements), or an empty untitled panel if the XML is missing,
        /// malformed, or not rooted at &lt;panel&gt; </returns>
        /// <remarks> Confirms the "cheap to add a fragment-parsing method" assumption from the Type/Block
        /// redesign discussion -- reuses the same <see cref="process_element"/>/<see cref="read_text_node"/>
        /// helpers <see cref="process_inputs"/> and <see cref="process_constants"/> already use for a full
        /// template file, just scoped to one panel's worth of XML instead of an entire document. </remarks>
        public Template_Panel Read_Panel_XML(string BlockXml)
        {
            var panel = new Template_Panel();

            if (String.IsNullOrWhiteSpace(BlockXml))
                return panel;

            try
            {
                var blockXmlDoc = new XmlDocument();
                blockXmlDoc.LoadXml(BlockXml);

                var nodeReader = new XmlNodeReader(blockXmlDoc);
                move_to_node(nodeReader, "panel");

                while (nodeReader.Read())
                {
                    string nodeName = nodeReader.Name.Trim().ToUpper();

                    if ((nodeReader.NodeType == XmlNodeType.EndElement) && (nodeName == "PANEL"))
                        break;

                    if (nodeReader.NodeType != XmlNodeType.Element)
                        continue;

                    if (nodeName == "NAME")
                    {
                        panel.Title = read_text_node(nodeReader);
                    }
                    else if ((nodeName == "ELEMENT") && (nodeReader.HasAttributes))
                    {
                        abstract_Element element = process_element(nodeReader, -1);
                        if (element != null)
                            panel.Add_Element(element);
                    }
                }
            }
            catch (Exception)
            {
                // Malformed BlockXml -- return whatever was parsed so far (likely just an empty,
                // untitled panel); the admin Metadata Block editor is where this should be caught and
                // fixed, not here
            }

            return panel;
        }

        /// <summary> Scans past the template's header section up to the start of the &lt;inputs&gt; or
        /// &lt;constants&gt; section </summary>
        /// <remarks> Used to store every header-level value onto <paramref name="ThisCompleteTemplate"/>
        /// (banner, title, permissions agreement, help URL, etc.) -- removed along with the rest of
        /// <c>CompleteTemplate</c>'s "Basic Properties" once nothing outside the retired legacy
        /// submission/edit viewers read them (see <c>CompleteTemplate.cs</c>). Individual header tags no
        /// longer need a case here at all: with nothing to store, each one is simply skipped over by
        /// this same scan, the same way any other unrecognized tag already was. </remarks>
        private void process_template_header(XmlNodeReader nodeReader, CompleteTemplate ThisCompleteTemplate)
        {
            // Read all the nodes
            while (nodeReader.Read())
            {
                // Get the node name, trimmed and to upper
                string nodeName = nodeReader.Name.Trim().ToUpper();

                // If this is the inputs or constant start tag, return
                if ((nodeReader.NodeType == XmlNodeType.Element) &&
                    ((nodeName == "INPUTS") || (nodeName == "CONSTANTS")))
                {
                    return;
                }
            }
        }

        private void process_inputs(XmlNodeReader nodeReader, CompleteTemplate ThisCompleteTemplate, bool exclude_divisions)
        {
            // Keep track of the current pages and panels
            Template_Page currentPage = null;
            Template_Panel currentPanel = null;
            bool inPanel = false;

            // Read all the nodes
            while (nodeReader.Read())
            {
                // Get the node name, trimmed and to upper
                string nodeName = nodeReader.Name.Trim().ToUpper();

                // If this is the inputs or constant start tag, return
                if (((nodeReader.NodeType == XmlNodeType.EndElement) && (nodeName == "INPUTS")) ||
                    ((nodeReader.NodeType == XmlNodeType.Element) && (nodeReader.Name == "CONSTANTS")))
                {
                    return;
                }

                // If this is the beginning tag for an element, assign the next values accordingly
                if (nodeReader.NodeType == XmlNodeType.Element)
                {
                    // Does this start a new page?
                    if (nodeName == "PAGE")
                    {
                        // Set the inPanel flag to false
                        inPanel = false;

                        // Create the new page and add to this CompleteTemplate
                        currentPage = new Template_Page();
                        ThisCompleteTemplate.Add_Page(currentPage);
                    }

                    // Does this start a new panel?
                    if ((nodeName == "PANEL") && (currentPage != null))
                    {
                        // Set the inPanel flag to true
                        inPanel = true;

                        // Create the new panel and add to the current page
                        currentPanel = new Template_Panel();
                        currentPage.Add_Panel(currentPanel);
                    }

                    // Is this a name element?
                    if ((nodeName == "NAME") && (currentPage != null))
                    {
                        // Get the text
                        string title = read_text_node(nodeReader);

                        // Set the name for either the page or panel
                        if (inPanel)
                        {
                            currentPanel.Title = title;
                        }
                        else
                        {
                            currentPage.Title = title;
                        }
                    }

                    // Is this a name element?
                    if ((nodeName == "INSTRUCTIONS") && (currentPage != null))
                    {
                        // Get the text
                        string instructions = read_text_node(nodeReader);

                        // Set the name for either the page or panel
                        if (!inPanel)
                        {
                            currentPage.Instructions = instructions;
                        }
                    }

                    // Is this a new element?
                    if ((nodeName == "ELEMENT") && (nodeReader.HasAttributes) && (currentPanel != null))
                    {
                        abstract_Element currentElement = process_element(nodeReader, ThisCompleteTemplate.InputPages.Count);
                        if (currentElement != null)
                        {
                            currentPanel.Add_Element(currentElement);
                        }
                    }
                }
            }
        }

        private abstract_Element process_element(XmlNodeReader nodeReader, int current_page_count)
        {
            string type = String.Empty;
            string subtype = String.Empty;

            // Step through all the attributes until the type is found
            nodeReader.MoveToFirstAttribute();
            do
            {
                // Get the type attribute
                if (nodeReader.Name.ToUpper().Trim() == "TYPE")
                {
                    type = nodeReader.Value;
                }

                // Get the subtype attribute
                if (nodeReader.Name.ToUpper().Trim() == "SUBTYPE")
                {
                    subtype = nodeReader.Value;
                }

            } while (nodeReader.MoveToNextAttribute());

            // Make sure a type was specified
            if (type == String.Empty)
                return null;

            // Build the element
            abstract_Element newElement = Element_Factory.getElement(type, subtype);

            // If thie element was null, return null
            if (newElement == null)
                return null;

            // Set the page number for post back reasons
            newElement.Template_Page = current_page_count;

            // Now, step through all the attributes again
            nodeReader.MoveToFirstAttribute();
            do
            {

                switch (nodeReader.Name.ToUpper().Trim())
                {
                    case "REPEATABLE":
                        bool repeatable;
                        if (Boolean.TryParse(nodeReader.Value, out repeatable))
                            newElement.Repeatable = repeatable;
                        break;
                    case "MANDATORY":
                        bool mandatory;
                        if (Boolean.TryParse(nodeReader.Value, out mandatory))
                            newElement.Mandatory = mandatory;
                        break;
                    case "READONLY":
                        bool isReadOnly;
                        if (Boolean.TryParse(nodeReader.Value, out isReadOnly))
                            newElement.Read_Only = isReadOnly;
                        break;
                    case "ACRONYM":
                        newElement.Acronym = nodeReader.Value;
                        break;
                    case "TITLE":
                        newElement.Title = nodeReader.Value;
                        break;
                }
            } while (nodeReader.MoveToNextAttribute());

            // Move back to the element, if there were attributes (should be)
            nodeReader.MoveToElement();

            // Is there element_data?
            if (!nodeReader.IsEmptyElement)
            {
                nodeReader.Read();
                if ((nodeReader.NodeType == XmlNodeType.Element) && (nodeReader.Name.ToLower() == "element_data"))
                {
                    // Let the element process this inner data
                    newElement.Read_XML(nodeReader.ReadSubtree());
                }
            }

            // Return this built element
            return newElement;
        }

        private void process_constants(XmlNodeReader nodeReader, CompleteTemplate ThisCompleteTemplate)
        {
            // Read all the nodes
            while (nodeReader.Read())
            {
                // Get the node name, trimmed and to upper
                string nodeName = nodeReader.Name.Trim().ToUpper();

                // If this is the inputs or constant start tag, return
                if ((nodeReader.NodeType == XmlNodeType.EndElement) && (nodeName == "CONSTANTS"))
                {
                    return;
                }

                // If this is the beginning tag for an element, assign the next values accordingly
                if ((nodeReader.NodeType == XmlNodeType.Element) && (nodeName == "ELEMENT") && (nodeReader.HasAttributes))
                {
                    abstract_Element newConstant = process_element(nodeReader, -1);
                    if (newConstant != null)
                    {
                        newConstant.isConstant = true;
                        ThisCompleteTemplate.Add_Constant(newConstant);
                    }
                }
            }
        }

        private static void move_to_node(XmlNodeReader nodeReader, string nodeName)
        {
            while ((nodeReader.Read()) && (nodeReader.Name.Trim() != nodeName))
            {
                // Do nothing here... 
            }
        }

        private static string read_text_node(XmlNodeReader nodeReader)
        {
            if ((nodeReader.Read()) && (nodeReader.NodeType == XmlNodeType.Text))
            {
                return nodeReader.Value.Trim();
            }
            return String.Empty;
        }
    }
}
