using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Promptuarium
{
    public partial class Element
    {
        #region Properties
        /// <summary>
        /// Data storage
        /// </summary>
        public Stream Data { get; set; }

        /// <summary>
        /// Metadata storage
        /// </summary>
        public Stream MetaData { get; set; }

        /// <summary>
        /// Reference to the parent node; root node has null as parent.
        /// </summary>
        public Element Parent { get; set; }

        internal List<Element> children;

        /// <summary>
        /// List of children. it is never null.
        /// </summary>
        public IList<Element> Children
        {
            get
            {
                return children;
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Conctructor without any data
        /// </summary>
        public Element()
        {
            children = new List<Element>();
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="other">Name of the object to by copied</param>
        public Element(Element other)
        {
            Data = other.Data;
            MetaData = other.MetaData;

            Parent = other.Parent;
            children = new List<Element>(other.Children.Where(child => child != null));

            foreach (Element child in children)
            {
                child.Parent = this;
            }
        }

        /// <summary>
        /// Contructor that also adds child elements
        /// </summary>
        /// <param name="data">Data of the parent node</param>
        /// <param name="metaData"></param>
        /// <param name="children">List of child nodes</param>
        public Element(Stream data, Stream metaData, params Element[] children)
        {
            Data = data;
            MetaData = metaData;

            if (children != null)
            {
                this.children = new List<Element>(children.Where(child => child != null));

                foreach (Element child in this.children)
                {
                    child.Parent = this;
                }
            }
            else
            {
                this.children = new List<Element>();
            }
        }

        /// <summary>
        /// LINQ friendly Contructor that also adds child elements
        /// </summary>
        /// <param name="data">Data of the parent node</param>
        /// <param name="metaData"></param>
        /// <param name="children">LINQ query of child nodes</param>
        public Element(Stream data, Stream metaData, IEnumerable<Element> children)
        {
            Data = data;
            MetaData = metaData;

            if (children != null)
            {
                this.children = new List<Element>(children.Where(child => child != null));

                foreach (Element child in this.children)
                {
                    child.Parent = this;
                }
            }
            else
            {
                this.children = new List<Element>();
            }
        }
        #endregion

        #region Indexer
        public Element this[int index]
        {
            get
            {
                if (index >= 0 && index < children.Count)
                {
                    return children[index];
                }

                throw new PromptuariumException(string.Format("Index {0} in out of range [0...{1}]", index, children.Count));
            }
        }
        #endregion

        #region Tree operations
        /// <summary>
        /// Add node(s) to the tree. Node can be null, in this case nothing will happen.
        /// </summary>
        /// <param name="nodes">The nodes to be added</param>
        public void Add(params Element[] nodes)
        {
            if (nodes != null)
            {
                foreach (Element node in nodes)
                {
                    if (node != null)
                    {
                        if (!Children.Contains(node))
                        {
                            UnsafeAdd(node);
                        }
                        else
                        {
                            throw new PromptuariumException("Node already exists in the tree");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Add node(s) to the tree. Node can be null, in this case nothing will happen.
        /// </summary>
        /// <param name="nodes">The nodes to be added</param>
        public void Add(IEnumerable<Element> nodes)
        {
            if (nodes != null)
            {
                foreach (Element node in nodes)
                {
                    Add(node);
                }
            }
        }

        /// <summary>
        /// Add node(s) to the tree. Node can be null, in this case nothing will happen.
        /// </summary>
        /// <param name="tree">The tree to be extended</param>
        /// <param name="node">The node to be added</param>
        public static Element operator +(Element tree, Element node)
        {
            tree.Add(node);
            return tree;
        }

        /// <summary>
        /// Removes a subtree or node recursively.
        /// </summary>
        /// <param name="node">The node to be removed</param>
        public void Remove(Element node)
        {
            if (node != null)
            {
                if (node.Children == null)
                {
                    throw new PromptuariumException("Children list must not be null");
                }

                List<Element> elementsToRemove = new List<Element>(node.Children);

                foreach (Element child in elementsToRemove)
                {
                    Remove(child);
                }

                Detach(node);
            }
        }

        /// <summary>
        /// Removes a subtree or node recursively.
        /// </summary>
        /// <param name="tree">The tree to be shrinked</param>
        /// <param name="node">The node to be removed</param>
        public static Element operator -(Element tree, Element node)
        {
            tree.Remove(node);
            return tree;
        }

        /// <summary>
        /// Detaches a subtree or node.
        /// </summary>
        /// <param name="node">The node to be detached</param>
        /// <returns>The node itself</returns>
        public Element Detach(Element node)
        {
            if (node?.Parent != null)
            {
                if (node.Parent.Children.Contains(node))
                {
                    node.Parent.children.Remove(node);
                }
                else
                {
                    throw new PromptuariumException("Node can not be detached from its parent");
                }

                node.Parent = null;
            }

            return node;
        }

        /// <summary>
        /// The method is called for each nodes.
        /// </summary>
        /// <param name="node">The current node</param>
        /// <param name="ancestors">A list of ancestors</param>
        public delegate void WalkHandler(Element node, List<Element> ancestors);

        /// <summary>
        /// Walks through the tree, and calls the handler for each node.
        /// </summary>
        /// <param name="handler">The method to be called for each nodes</param>
        public void Walk(WalkHandler handler)
        {
            List<Element> ancestors = new List<Element>();

            handler?.Invoke(this, ancestors);

            Walk(this, ancestors, handler);
        }
        #endregion

        #region Conversion functions
        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            NodeToString(this, 0, stringBuilder, false);

            return stringBuilder.ToString().Trim();
        }

        public string TreeToString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            NodeToString(this, 0, stringBuilder, true);

            return stringBuilder.ToString();
        }

        public async Task<string> ToBase64StringAsync(CancellationToken cancellationToken)
        {
            string base64String;

            using (MemoryStream memoryStream = new MemoryStream())
            {
                await SaveAsync(memoryStream, cancellationToken).ConfigureAwait(false);

                byte[] buffer = new byte[memoryStream.Length];
                Array.Copy(memoryStream.ToArray(), buffer, (int)memoryStream.Length);

                base64String = Convert.ToBase64String(buffer);
            }

            return base64String;
        }

        public Task<string> ToBase64StringAsync() => ToBase64StringAsync(CancellationToken.None);

        public static async Task<Element> FromBase64StringAsync(string base64String, CancellationToken cancellationToken)
        {
            Element tree;

            using (MemoryStream memoryStream = new MemoryStream(Convert.FromBase64String(base64String)))
            {
                tree = await LoadAsync(memoryStream, cancellationToken).ConfigureAwait(false);
            }

            return tree;
        }

        public static Task<Element> FromBase64StringAsync(string base64String) => FromBase64StringAsync(base64String, CancellationToken.None);
        #endregion

        #region Private Methods
        /// <summary>
        /// Add node(s) to the tree, unsafely, but quickly. Node can be null, in this case nothing will happen.
        /// </summary>
        /// <param name="node">The node to be added</param>
        private void UnsafeAdd(Element node)
        {
            children.Add(node);
            node.Parent = this;
        }

        private void NodeToString(Element node, int level, StringBuilder stringBuilder, bool recursive)
        {
            if (recursive)
            {
                for (int i = 0; i < level; ++i)
                {
                    stringBuilder.Append("--> ");
                }
            }

            stringBuilder.Append("(");

            if (Contains(node.MetaData))
            {
                stringBuilder.Append("[");
                StreamToString(node.MetaData, stringBuilder);

                stringBuilder.Append(" = \"");
                stringBuilder.Append(node.MetaData
                    .AsUtf8String()
                    .Select(c => c < '\u0020' || c > '\u007F' ? '.' : c)
                    .ToArray());
                stringBuilder.Append("\"");
                stringBuilder.Append("]");
            }

            if (Contains(node.Data) && Contains(node.MetaData))
            {
                stringBuilder.Append(" ");
            }

            if (Contains(node.Data))
            {
                StreamToString(node.Data, stringBuilder);

                stringBuilder.Append(" = \"");
                stringBuilder.Append(node.Data
                    .AsUtf8String()
                    .Select(c => c < '\u0020' || c > '\u007F' ? '.' : c)
                    .ToArray());
                stringBuilder.Append("\"");
            }

            stringBuilder.Append(")\n");

            if (recursive)
            {
                foreach (Element child in node.Children)
                {
                    NodeToString(child, level + 1, stringBuilder, true);
                }
            }
        }

        private void StreamToString(Stream data, StringBuilder stringBuilder)
        {
            data.Position = 0;

            while (data.Position < data.Length)
            {
                stringBuilder.AppendFormat("{0:X2}", data.ReadByte());
            }
        }

        private void Walk(Element parent, List<Element> ancestors, WalkHandler handler)
        {
            if (parent.children != null)
            {
                List<Element> _ancestors = new List<Element>(ancestors)
                {
                    parent
                };

                foreach (Element child in parent.children)
                {
                    handler?.Invoke(child, _ancestors);
                    Walk(child, _ancestors, handler);
                }
            }
        }
        #endregion
    }
}
