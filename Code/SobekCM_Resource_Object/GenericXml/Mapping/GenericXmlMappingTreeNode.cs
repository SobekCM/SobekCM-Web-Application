using SobekCM.Resource_Object.GenericXml.Reader;
using System;
using System.Collections.Generic;

namespace SobekCM.Resource_Object.GenericXml.Mapping
{
    public class GenericXmlMappingTreeNode
    {
        public GenericXmlNode Node { get; set; }

        public Dictionary<string, GenericXmlMappingTreeNode> Children { get; set; }

        public PathMappingInstructions Instructions { get; set; }

        public Dictionary<string, PathMappingInstructions> AttributeInstructions { get; set; }

        public GenericXmlMappingTreeNode()
        {
            Children = new Dictionary<string, GenericXmlMappingTreeNode>(StringComparer.OrdinalIgnoreCase);
            AttributeInstructions = new Dictionary<string, PathMappingInstructions>(StringComparer.OrdinalIgnoreCase);
        }


    }
}
