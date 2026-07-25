using System.Collections.Generic;
using System.IO;
using System.Net;

namespace SobekCM.Library.HTML.Helpers
{
    /// <summary> A node in an <see cref="HtmlTreeView_HtmlHelper"/>, rendered as nested HTML list elements </summary>
    public class HtmlTreeNode
    {
        /// <summary> HTML content to display for this node </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary> Optional href for leaf nodes that should render as a link (NavigateUrl is not embedded in Text) </summary>
        public string NavigateUrl { get; set; } = string.Empty;

        /// <summary> Optional tooltip; becomes a title attribute on the rendered element </summary>
        public string ToolTip { get; set; } = string.Empty;

        /// <summary> Optional icon shown before the node text </summary>
        public string ImageUrl { get; set; } = string.Empty;

        /// <summary> Alt/title text for the node icon </summary>
        public string ImageToolTip { get; set; } = string.Empty;

        /// <summary> Whether this node starts in the expanded state </summary>
        public bool IsExpanded { get; private set; }

        /// <summary> Parent node; set automatically when this node is added via <see cref="ChildNodes"/> </summary>
        public HtmlTreeNode Parent { get; internal set; }

        /// <summary> Child nodes; adding a child here sets its <see cref="Parent"/> automatically </summary>
        public HtmlTreeNodeList ChildNodes { get; }

        public HtmlTreeNode()
        {
            ChildNodes = new HtmlTreeNodeList(this);
        }

        /// <summary> Mark this node as expanded </summary>
        public void Expand() => IsExpanded = true;

        /// <summary> Collapse this node and all descendants </summary>
        public void CollapseAll()
        {
            IsExpanded = false;
            foreach (HtmlTreeNode child in ChildNodes)
                child.CollapseAll();
        }
    }

    /// <summary> List of <see cref="HtmlTreeNode"/> children that automatically sets <see cref="HtmlTreeNode.Parent"/> on add </summary>
    public class HtmlTreeNodeList : IEnumerable<HtmlTreeNode>
    {
        private readonly List<HtmlTreeNode> items = new List<HtmlTreeNode>();
        private readonly HtmlTreeNode owner;

        internal HtmlTreeNodeList(HtmlTreeNode owner) => this.owner = owner;

        /// <summary> Number of child nodes </summary>
        public int Count => items.Count;

        /// <summary> Add a child node and set its Parent </summary>
        public void Add(HtmlTreeNode node)
        {
            node.Parent = owner;
            items.Add(node);
        }

        public IEnumerator<HtmlTreeNode> GetEnumerator() => items.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => items.GetEnumerator();
    }

    /// <summary> Renders a tree of <see cref="HtmlTreeNode"/> objects as nested HTML
    /// <c>&lt;ul&gt;/&lt;li&gt;</c> elements using the native <c>&lt;details&gt;/&lt;summary&gt;</c>
    /// element for expand/collapse with no JavaScript required </summary>
    public class HtmlTreeView_HtmlHelper
    {
        /// <summary> CSS class applied to the outermost <c>&lt;ul&gt;</c> element </summary>
        public string CssClass { get; set; } = string.Empty;

        /// <summary> Root-level nodes </summary>
        public List<HtmlTreeNode> Nodes { get; } = new List<HtmlTreeNode>();

        /// <summary> Collapse all nodes in the tree </summary>
        public void CollapseAll()
        {
            foreach (HtmlTreeNode node in Nodes)
                node.CollapseAll();
        }

        /// <summary> Write the complete tree as HTML to <paramref name="writer"/> </summary>
        public void Render(TextWriter writer)
        {
            string cssAttr = string.IsNullOrEmpty(CssClass)
                ? string.Empty
                : " class=\"" + WebUtility.HtmlEncode(CssClass) + "\"";

            writer.Write("<ul" + cssAttr + ">");
            foreach (HtmlTreeNode node in Nodes)
                RenderNode(writer, node);
            writer.Write("</ul>");
        }

        private static void RenderNode(TextWriter writer, HtmlTreeNode node)
        {
            writer.Write("<li>");

            string imgHtml = string.Empty;
            if (!string.IsNullOrEmpty(node.ImageUrl))
            {
                string imgTitle = string.IsNullOrEmpty(node.ImageToolTip)
                    ? string.Empty
                    : " title=\"" + WebUtility.HtmlEncode(node.ImageToolTip) + "\"";
                imgHtml = "<img src=\"" + node.ImageUrl + "\"" + imgTitle + " alt=\"\" /> ";
            }

            string titleAttr = string.IsNullOrEmpty(node.ToolTip)
                ? string.Empty
                : " title=\"" + WebUtility.HtmlEncode(node.ToolTip) + "\"";

            if (node.ChildNodes.Count > 0)
            {
                string openAttr = node.IsExpanded ? " open" : string.Empty;
                writer.Write("<details" + openAttr + "><summary" + titleAttr + ">" + imgHtml + node.Text + "</summary><ul>");
                foreach (HtmlTreeNode child in node.ChildNodes)
                    RenderNode(writer, child);
                writer.Write("</ul></details>");
            }
            else if (!string.IsNullOrEmpty(node.NavigateUrl))
            {
                writer.Write(imgHtml + "<a href=\"" + node.NavigateUrl + "\"" + titleAttr + ">" + node.Text + "</a>");
            }
            else if (!string.IsNullOrEmpty(titleAttr))
            {
                writer.Write("<span" + titleAttr + ">" + imgHtml + node.Text + "</span>");
            }
            else
            {
                writer.Write(imgHtml + node.Text);
            }

            writer.Write("</li>");
        }
    }
}
