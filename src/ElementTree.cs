using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Promptuarium;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
public partial class Element : IEnumerable<Element>
{
    #region Properties
    /// <summary>
    /// Data storage
    /// </summary>
    public Stream? Data { get; set; }

    /// <summary>
    /// Metadata storage
    /// </summary>
    public Stream? MetaData { get; set; }

    /// <summary>
    /// Reference to the parent node; root node has null as parent.
    /// </summary>
    public Element? Parent { get; set; }

    /// <summary>
    /// List of children. it is never null.
    /// </summary>
    public IList<Element> Children { get; } = new List<Element>();
    #endregion

    #region Constructors
    /// <summary>
    /// Constructor without any data
    /// </summary>
    public Element()
    {
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
        Children = new List<Element>(other.Children.Where(child => child != null));

        foreach (Element child in Children)
        {
            child.Parent = this;
        }
    }

    /// <summary>
    /// Contractor that also adds child elements
    /// </summary>
    /// <param name="data">Data of the parent node</param>
    /// <param name="metaData"></param>
    /// <param name="children">List of child nodes</param>
    public Element(Stream? data, Stream? metaData, params Element[] children)
    {
        Data = data;
        MetaData = metaData;

        Children = new List<Element>(children.Where(child => child != null));

        foreach (Element child in Children)
        {
            child.Parent = this;
        }
    }

    /// <summary>
    /// LINQ friendly contractor that also adds child elements
    /// </summary>
    /// <param name="data">Data of the parent node</param>
    /// <param name="metaData"></param>
    /// <param name="children">LINQ query of child nodes</param>
    public Element(Stream? data, Stream? metaData, IEnumerable<Element> children)
    {
        Data = data;
        MetaData = metaData;

        Children = new List<Element>(children.Where(child => child != null));

        foreach (Element child in Children)
        {
            child.Parent = this;
        }
    }
    #endregion

    #region Indexer
    /// <summary>
    /// Get an item by index.
    /// </summary>
    /// <param name="index">The index.</param>
    public Element this[int index]
    {
        get
        {
            if (index >= 0 && index < Children.Count)
            {
                return Children[index];
            }

            throw new PromptuariumException($"Index {index} in out of range [0...{Children.Count}]");
        }
    }
    #endregion

    #region Tree operations
    /// <summary>
    /// Add node(s) to the tree. Node can be null, in this case nothing will happen.
    /// </summary>
    /// <param name="nodes">The nodes to be added</param>
    public Element Add(params Element[] nodes)
    {
        foreach (Element node in nodes)
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

        return this;
    }

    /// <summary>
    /// Add node(s) to the tree. Node can be null, in this case nothing will happen.
    /// </summary>
    /// <param name="nodes">The nodes to be added</param>
    public Element Add(IEnumerable<Element> nodes)
    {
        foreach (Element node in nodes)
        {
            Add(node);
        }

        return this;
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
    public Element Remove(Element node)
    {
        var elementsToRemove = new List<Element>(node.Children);

        foreach (Element child in elementsToRemove)
        {
            Remove(child);
        }

        Detach(node);
        return this;
    }

    /// <summary>
    /// Removes a subtree or node recursively.
    /// </summary>
    /// <param name="tree">The tree to be shrunk</param>
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
        if (node.Parent != null)
        {
            if (node.Parent.Children.Contains(node))
            {
                node.Parent.Children.Remove(node);
            }
            else
            {
                throw new PromptuariumException("Node can not be detached from its parent");
            }

            node.Parent = null;
        }

        return this;
    }

    /// <summary>
    /// Detaches a subtree or node.
    /// </summary>
    /// <returns>The node itself</returns>
    public Element Detach()
    {
        return Detach(this);
    }

    /// <summary>
    /// The method is called for each nodes.
    /// </summary>
    /// <param name="node">The current node</param>
    /// <param name="ancestors">A list of ancestors</param>
    public delegate void WalkHandler(Element node, IReadOnlyCollection<Element> ancestors);

    /// <summary>
    /// Walks through the tree, and calls the handler for each node.
    /// </summary>
    /// <param name="handler">The method to be called for each nodes</param>
    public Element Walk(WalkHandler handler)
    {
        var ancestors = new List<Element>();
        handler.Invoke(this, ancestors);
        Walk(this, ancestors, handler);
        return this;
    }
    #endregion

    #region Conversion functions
    /// <summary>
    /// Converts the element to string (for debugging purposes).
    /// </summary>
    public override string ToString()
    {
        var stringBuilder = new StringBuilder();
        NodeToString(this, 0, stringBuilder, false, string.Empty);
        return stringBuilder.ToString().Trim();
    }

    /// <summary>
    /// Converts the whole tree to string (for debugging purposes).
    /// </summary>
    /// <param name="tabulator">An optional tabulator for conversion.</param>
    public string TreeToString(string tabulator = ">")
    {
        var stringBuilder = new StringBuilder();
        NodeToString(this, 0, stringBuilder, true, tabulator);
        return stringBuilder.ToString();
    }

    /// <summary>
    /// Converts the tree to Base64 string.
    /// </summary>
    public async Task<string> ToBase64StringAsync(CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();

        await SaveAsync(memoryStream, cancellationToken).ConfigureAwait(false);

        byte[] buffer = new byte[memoryStream.Length];
        Array.Copy(memoryStream.ToArray(), buffer, (int)memoryStream.Length);

        return Convert.ToBase64String(buffer);
    }

    /// <summary>
    /// Converts the tree to Base64 string.
    /// </summary>
    public Task<string> ToBase64StringAsync() => ToBase64StringAsync(CancellationToken.None);

    /// <summary>
    /// Creates a tree from a Base64 string.
    /// </summary>
    /// <param name="base64String">The tree in Base64.</param>
    public static async Task<Element> FromBase64StringAsync(string base64String, CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream(Convert.FromBase64String(base64String));
        return await LoadAsync(memoryStream, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a tree from a Base64 string.
    /// </summary>
    /// <param name="base64String">The tree in Base64.</param>
    public static Task<Element> FromBase64StringAsync(string base64String) => FromBase64StringAsync(base64String, CancellationToken.None);
    #endregion

    #region IEnumerable<Element> implementation
    public IEnumerator<Element> GetEnumerator()
    {
        return Children.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)Children).GetEnumerator();
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Add node(s) to the tree, unsafely, but quickly. Node can be null, in this case nothing will happen.
    /// </summary>
    /// <param name="node">The node to be added</param>
    private void UnsafeAdd(Element node)
    {
        Children.Add(node);
        node.Parent = this;
    }

    private void NodeToString(Element node, int level, StringBuilder stringBuilder, bool recursive, string tabulator)
    {
        if (recursive)
        {
            for (int i = 0; i < level; ++i)
            {
                stringBuilder.Append(tabulator);
            }
        }

        stringBuilder.Append('(');

        if (Contains(node.MetaData))
        {
            stringBuilder.Append('[');
            StreamToString(node.MetaData!, stringBuilder);

            stringBuilder.Append(" = \"");
            stringBuilder.Append(node.MetaData!
                .AsUtf8String()
                .Select(c => c < '\u0020' || c > '\u007F' ? '.' : c)
                .ToArray());
            stringBuilder.Append('\"');
            stringBuilder.Append(']');
        }

        if (Contains(node.Data) && Contains(node.MetaData))
        {
            stringBuilder.Append(' ');
        }

        if (Contains(node.Data))
        {
            StreamToString(node.Data!, stringBuilder);

            stringBuilder.Append(" = \"");
            stringBuilder.Append(node.Data!
                .AsUtf8String()
                .Select(c => c < '\u0020' || c > '\u007F' ? '.' : c)
                .ToArray());
            stringBuilder.Append('\"');
        }

        stringBuilder.Append(")\n");

        if (recursive)
        {
            foreach (Element child in node.Children)
            {
                NodeToString(child, level + 1, stringBuilder, true, tabulator);
            }
        }
    }

    private static void StreamToString(Stream data, StringBuilder stringBuilder)
    {
        data.Position = 0;

        while (data.Position < data.Length)
        {
            stringBuilder.AppendFormat("{0:X2}", data.ReadByte());
        }
    }

    private void Walk(Element parent, List<Element> ancestors, WalkHandler handler)
    {
        var _ancestors = new List<Element>(ancestors)
        {
            parent
        };

        foreach (Element child in parent.Children)
        {
            handler.Invoke(child, _ancestors);
            Walk(child, _ancestors, handler);
        }
    }

    private string DebuggerDisplay => ToString();
    #endregion
}
