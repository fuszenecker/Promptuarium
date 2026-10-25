using System;
using System.Buffers;
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
    /// It is maintained by the tree operations (Add, Remove, Detach).
    /// </summary>
    public Element? Parent { get; private set; }

    /// <summary>
    /// List of children. it is never null, but can be empty.
    /// The list is read-only, use the tree operations (Add, Remove, Detach) to change it.
    /// </summary>
    public IReadOnlyList<Element> Children { get; }

    private readonly List<Element> children = new();

    // Block size of the content comparison
    private const int CompareBlockSize = 64 * 1024;
    
    #endregion

    #region Constructors

    /// <summary>
    /// Constructor without any data
    /// </summary>
    /// <remarks>
    /// The constructor creates an empty node.
    /// </remarks>
    /// <example>
    /// <code>
    /// var node = new Element();
    /// </code>
    /// </example>
    public Element()
    {
        Children = children.AsReadOnly();
    }

    /// <summary>
    /// Copy constructor
    /// </summary>
    /// <param name="other">Name of the object to by copied</param>
    /// <remarks>
    /// The copy constructor creates a deep copy of the object.
    /// </remarks>
    /// <example>
    /// <code>
    /// var copy = new Element(original);
    /// </code>
    /// </example>
    public Element(Element other)
        : this()
    {
        ArgumentNullException.ThrowIfNull(other);

        // Iterating (instead of recursion) keeps arbitrarily deep trees from overflowing the stack
        var nodes = new Stack<(Element Source, Element Copy)>();
        nodes.Push((other, this));

        while (nodes.Count > 0)
        {
            (Element source, Element copy) = nodes.Pop();

            copy.Data = CopyStream(source.Data);
            copy.MetaData = CopyStream(source.MetaData);

            foreach (Element child in source.Children)
            {
                if (child != null)
                {
                    var childCopy = new Element();
                    copy.UnsafeAdd(childCopy);
                    nodes.Push((child, childCopy));
                }
            }
        }
    }

    /// <summary>
    /// Contractor that also adds child elements
    /// </summary>
    /// <param name="data">Data of the parent node</param>
    /// <param name="metaData"></param>
    /// <param name="children">List of child nodes</param>
    /// <example>
    /// <code>
    /// var child1 = new Element();
    /// var child2 = new Element();
    /// var child3 = new Element();
    /// 
    /// var data = Data.FromUtf8String("Hello world");
    /// var metaData = Data.FromUtf8String("Hello world");
    /// 
    /// var node = new Element(data, metaData, child1, child2, child3);
    /// </code>
    /// </example>
    public Element(Stream? data, Stream? metaData, params Element[] children)
        : this()
    {
        ArgumentNullException.ThrowIfNull(children);

        Data = data;
        MetaData = metaData;

        Add(children.Where(child => child != null));
    }

    /// <summary>
    /// LINQ friendly contractor that also adds child elements
    /// </summary>
    /// <param name="data">Data of the parent node</param>
    /// <param name="metaData"></param>
    /// <param name="children">LINQ query of child nodes</param>
    /// <example>
    /// <code>
    /// var data = Data.FromUtf8String("Hello world");
    /// var metaData = Data.FromInt32(123);
    /// 
    /// var tree = new Element(data, metaData,
    ///    Enumerable.Range(0, 10).Select(i => new Element(Data.FromVarInt(i))));
    /// </code>
    /// </example>
    public Element(Stream? data, Stream? metaData, IEnumerable<Element> children)
        : this()
    {
        ArgumentNullException.ThrowIfNull(children);

        Data = data;
        MetaData = metaData;

        Add(children.Where(child => child != null));
    }
    
    #endregion

    #region Indexer

    /// <summary>
    /// Get an item by index.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <returns>The element</returns>
    /// <exception cref="ArgumentOutOfRangeException">If the index is out of range</exception>
    /// <example>
    /// <code>
    /// var node = tree[0];
    /// </code>
    /// </example>
    public Element this[int index]
    {
        get
        {
            if (index >= 0 && index < Children.Count)
            {
                return Children[index];
            }

            throw new ArgumentOutOfRangeException(nameof(index), index, $"Index is out of range [0...{Children.Count - 1}]");
        }
    }

    #endregion

    #region Tree operations
    
    /// <summary>
    /// Adds node(s) to the tree. A node that has another parent is moved here.
    /// </summary>
    /// <param name="nodes">The nodes to be added</param>
    /// <exception cref="PromptuariumException">Thrown if the node is already a child of this node, or if it is this node or one of its ancestors</exception>
    /// <example>
    /// <code>
    /// var tree = new Element();
    /// 
    /// var node1 = new Element();
    /// var node2 = new Element();
    /// var node3 = new Element();
    /// 
    /// tree.Add(node1, node2, node3);
    /// </code>
    /// 
    /// <code>
    /// var tree = new Element()
    /// {
    ///     new Element(),
    ///     new Element(),
    ///     new Element()
    /// };
    /// </code>
    /// </example>
    public Element Add(params Element[] nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);

        foreach (Element node in nodes)
        {
            ArgumentNullException.ThrowIfNull(node, nameof(nodes));
            PrepareNewChild(node);
            UnsafeAdd(node);
        }

        return this;
    }

    /// <summary>
    /// Inserts a node at the given position of the children. A node that has another parent is moved here.
    /// </summary>
    /// <param name="index">The position of the new child, from 0 to the number of children (to append)</param>
    /// <param name="node">The node to be inserted</param>
    /// <returns>The tree itself</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the index is out of range</exception>
    /// <exception cref="PromptuariumException">Thrown if the node is already a child of this node, or if it is this node or one of its ancestors</exception>
    /// <example>
    /// <code>
    /// var tree = new Element(null, null, node1, node3);
    /// tree.Insert(1, node2);
    /// </code>
    /// </example>
    public Element Insert(int index, Element node)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, Children.Count);

        PrepareNewChild(node);
        children.Insert(index, node);
        node.Parent = this;
        return this;
    }

    private void PrepareNewChild(Element node)
    {
        if (node.Parent == this)
        {
            throw new PromptuariumException("Node already exists in the tree");
        }

        // Only a node with children can be an ancestor of this node, so leaves need no walk up the tree
        if (node == this || (node.Children.Count > 0 && IsAncestor(node, this)))
        {
            throw new PromptuariumException("A node can not be added to itself or to its own descendant");
        }

        // A node can have only one parent
        DetachFromParent(node);
    }

    /// <summary>
    /// Adds node(s) to the tree. A node that has another parent is moved here.
    /// </summary>
    /// <param name="nodes">The nodes to be added</param>
    /// <exception cref="PromptuariumException">Thrown if the node already exists in the tree</exception>
    /// <example>
    /// <code>
    /// var tree = new Element();
    /// 
    /// var node1 = new Element();
    /// var node2 = new Element();
    /// var node3 = new Element();
    /// 
    /// tree.Add(node1, node2, node3);
    /// </code>
    /// </example>
    public Element Add(IEnumerable<Element> nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);

        foreach (Element node in nodes)
        {
            Add(node);
        }

        return this;
    }

    /// <summary>
    /// Removes a subtree or node recursively.
    /// </summary>
    /// <param name="node">The node to be removed</param>
    /// <exception cref="PromptuariumException">Thrown if the node is not a child of this node</exception>
    /// <example>
    /// <code>
    /// var tree = new Element();
    /// 
    /// var node1 = new Element();
    /// var node2 = new Element();
    /// var node3 = new Element();
    /// 
    /// tree.Add(node1, node2, node3);
    /// 
    /// tree.Remove(node2);
    /// </code>
    /// </example>
    public Element Remove(Element node)
    {
        ArgumentNullException.ThrowIfNull(node);

        Detach(node);

        // Iterating (instead of recursion) keeps arbitrarily deep trees from overflowing the stack
        foreach (Element descendant in PreOrder(node))
        {
            foreach (Element child in descendant.children)
            {
                child.Parent = null;
            }

            descendant.children.Clear();
        }

        return this;
    }

    /// <summary>
    /// Detaches a subtree or node.
    /// </summary>
    /// <param name="node">The node to be detached</param>
    /// <returns>The node itself</returns>
    /// <exception cref="PromptuariumException">Thrown if the node is not a child of this node</exception>
    /// <example>
    /// <code>
    /// var tree = new Element();
    /// 
    /// var node1 = new Element();
    /// var node2 = new Element();
    /// var node3 = new Element();
    /// 
    /// tree.Add(node1, node2, node3);
    /// 
    /// var detachedNode = tree.Detach(node2);
    /// </code>
    /// </example>
    public Element Detach(Element node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (node.Parent != this)
        {
            throw new PromptuariumException("Node is not a child of this node");
        }

        DetachFromParent(node);
        return node;
    }

    /// <summary>
    /// Detaches a subtree or node.
    /// </summary>
    /// <returns>The node itself</returns>
    /// <example>
    /// <code>
    /// var tree = new Element();
    /// 
    /// var node1 = new Element();
    /// var node2 = new Element();
    /// var node3 = new Element();
    /// 
    /// tree.Add(node1, node2, node3);
    /// 
    /// var detachedNode = node2.Detach();
    /// </code>
    /// </example>
    public Element Detach()
    {
        DetachFromParent(this);
        return this;
    }

    /// <summary>
    /// The method is called for each nodes.
    /// </summary>
    /// <param name="node">The current node</param>
    /// <param name="ancestors">The ancestors of the node, from the root to the parent. The collection is only valid during the call: copy it if it is needed later.</param>
    public delegate void WalkHandler(Element node, IReadOnlyCollection<Element> ancestors);

    /// <summary>
    /// Walks through the tree, and calls the handler for each node.
    /// </summary>
    /// <param name="handler">The method to be called for each nodes</param>
    /// <returns>The node itself</returns>
    /// <example>
    /// <code>
    /// var tree = new Element();
    /// 
    /// var node1 = new Element();
    /// var node2 = new Element();
    /// var node3 = new Element();
    /// 
    /// tree.Add(node1, node2, node3);
    /// 
    /// tree.Walk((Element node, IReadOnlyCollection&lt;Element&gt; ancestors) =>
    ///    {
    ///        Console.WriteLine(node);
    ///    });
    ///    
    /// </code>
    /// </example>
    public Element Walk(WalkHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        // Iterating (instead of recursion) keeps arbitrarily deep trees from overflowing the stack.
        // The list of ancestors is the path from this node, it is updated in place (instead of copying it for every node).
        var nodes = new Stack<(Element Node, int Depth)>();
        var ancestors = new List<Element>();
        nodes.Push((this, 0));

        while (nodes.Count > 0)
        {
            (Element node, int depth) = nodes.Pop();
            ancestors.RemoveRange(depth, ancestors.Count - depth);
            handler.Invoke(node, ancestors);
            ancestors.Add(node);

            for (int childIndex = node.Children.Count - 1; childIndex >= 0; childIndex--)
            {
                nodes.Push((node.Children[childIndex], depth + 1));
            }
        }

        return this;
    }
    
    #endregion

    #region Comparison

    /// <summary>
    /// Compares the structure and the contents of two trees: the data, the metadata and the children (in order) of every node.
    /// </summary>
    /// <remarks>
    /// An absent (null) and an empty content are equal, as in the binary format. The parents are not compared,
    /// so a subtree can be compared to a standalone tree. The positions of the content streams do not change.
    /// (Equals is not overridden: a tree is mutable, and the tree operations identify the nodes by reference.)
    /// </remarks>
    /// <param name="other">The other tree</param>
    /// <returns>True, if the two trees have the same structure and contents</returns>
    /// <example>
    /// <code>
    /// var loaded = await Element.LoadAsync(stream);
    /// bool same = tree.StructurallyEquals(loaded);
    /// </code>
    /// </example>
    public bool StructurallyEquals(Element? other)
    {
        if (other is null)
        {
            return false;
        }

        // Iterating (instead of recursion) keeps arbitrarily deep trees from overflowing the stack
        var nodes = new Stack<(Element Left, Element Right)>();
        nodes.Push((this, other));

        while (nodes.Count > 0)
        {
            (Element left, Element right) = nodes.Pop();

            if (ReferenceEquals(left, right))
            {
                continue;
            }

            if (left.Children.Count != right.Children.Count ||
                !ContentEquals(left.Data, right.Data) ||
                !ContentEquals(left.MetaData, right.MetaData))
            {
                return false;
            }

            for (int childIndex = 0; childIndex < left.Children.Count; childIndex++)
            {
                nodes.Push((left.Children[childIndex], right.Children[childIndex]));
            }
        }

        return true;
    }

    #endregion

    #region Conversion functions
    /// <summary>
    /// Converts the element to string (for debugging purposes).
    /// </summary>
    /// <example>
    /// <code>
    /// var tree = new Element();
    /// tree.Data = Data.FromUtf8String("Hello world");
    /// var nodeString = tree.ToString();
    /// </code>
    /// </example>
    public override string ToString()
    {
        var stringBuilder = new StringBuilder();
        NodeToString(this, stringBuilder);
        return stringBuilder.ToString();
    }

    /// <summary>
    /// Converts the whole tree to string (for debugging purposes).
    /// </summary>
    /// <param name="tabulator">An optional tabulator for conversion.</param>
    /// <example>
    /// <code>
    /// var tree = new Element();
    /// 
    /// var node1 = new Element();
    ///     
    /// var node2 = new Element();
    /// node2.Data = Data.FromUtf8String("Hello world");
    /// 
    /// var node3 = new Element();
    /// node3.Data = Data.FromUtf8String("Hello world");
    /// 
    /// tree.Add(node1, node2, node3);
    /// 
    /// var treeString = tree.TreeToString();
    /// </code>
    /// </example>
    public string TreeToString(string tabulator = ">")
    {
        var stringBuilder = new StringBuilder();

        Walk((node, ancestors) =>
        {
            for (int i = 0; i < ancestors.Count; ++i)
            {
                stringBuilder.Append(tabulator);
            }

            NodeToString(node, stringBuilder);
            stringBuilder.Append('\n');
        });

        return stringBuilder.ToString();
    }

    /// <summary>
    /// Converts the tree to Base64 string.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tree in Base64.</returns>
    /// <example>
    /// <code>
    /// var tree = new Element();
    /// 
    /// var node1 = new Element();
    /// 
    /// var node2 = new Element();
    /// node2.Data = Data.FromUtf8String("Hello world");
    /// 
    /// var node3 = new Element();
    /// node3.Data = Data.FromUtf8String("Hello world");
    /// 
    /// tree.Add(node1, node2, node3);
    /// 
    /// var base64String = await tree.ToBase64StringAsync();
    /// </code>
    /// </example>
    public async Task<string> ToBase64StringAsync(CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();

        await SaveAsync(memoryStream, cancellationToken).ConfigureAwait(false);

        return Convert.ToBase64String(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);
    }

    /// <summary>
    /// Converts the tree to Base64 string.
    /// </summary>
    /// <returns>The tree in Base64.</returns>
    /// <example>
    /// <code>
    /// var tree = new Element();
    /// 
    /// var node1 = new Element();
    ///
    /// var node2 = new Element();
    /// node2.Data = Data.FromUtf8String("Hello world");
    /// 
    /// var node3 = new Element();
    /// node3.Data = Data.FromUtf8String("Hello world");
    /// 
    /// tree.Add(node1, node2, node3);
    /// 
    /// var base64String = await tree.ToBase64StringAsync();
    /// </code>
    /// </example>
    public Task<string> ToBase64StringAsync() => ToBase64StringAsync(CancellationToken.None);

    /// <summary>
    /// Creates a tree from a Base64 string.
    /// </summary>
    /// <param name="base64String">The tree in Base64.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tree.</returns>
    /// <example>
    /// <code>
    /// var tree = await Element.FromBase64StringAsync(base64String);
    /// </code>
    /// </example>
    public static async Task<Element> FromBase64StringAsync(string base64String, CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream(Convert.FromBase64String(base64String));
        return await LoadAsync(memoryStream, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a tree from a Base64 string.
    /// </summary>
    /// <param name="base64String">The tree in Base64.</param>
    /// <returns>The tree.</returns>
    /// <example>
    /// <code>
    /// var tree = await Element.FromBase64StringAsync(base64String);
    /// </code>
    /// </example>
    public static Task<Element> FromBase64StringAsync(string base64String) => FromBase64StringAsync(base64String, CancellationToken.None);
    
    #endregion

    #region IEnumerable<Element> implementation

    /// <summary>
    /// Returns an enumerator that iterates through the Children
    /// </summary>
    /// <returns>The enumerator of children</returns>
    /// <example>
    /// <code>
    /// var tree = new Element();
    /// 
    /// var node1 = new Element();
    /// var node2 = new Element();
    /// var node3 = new Element();
    /// 
    /// tree.Add(node1, node2, node3);
    /// 
    /// foreach (var node in tree)
    /// {
    ///    Console.WriteLine(node.ToString());
    /// }
    /// </code>
    /// </example>
    public IEnumerator<Element> GetEnumerator()
    {
        return Children.GetEnumerator();
    }

    /// <summary>
    /// Returns an enumerator that iterates through the Children
    /// </summary>
    /// <returns>The enumerator of children</returns>
    /// <example>
    /// <code>
    /// var tree = new Element();
    /// 
    /// var node1 = new Element();
    /// var node2 = new Element();
    /// var node3 = new Element();
    /// 
    /// tree.Add(node1, node2, node3);
    /// 
    /// foreach (var node in tree)
    /// {
    ///   Console.WriteLine(node.ToString());
    /// }
    /// </code>
    /// </example>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)Children).GetEnumerator();
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Adds a node to the tree, without checks.
    /// </summary>
    /// <param name="node">The node to be added</param>
    private void UnsafeAdd(Element node)
    {
        children.Add(node);
        node.Parent = this;
    }

    private static void DetachFromParent(Element node)
    {
        if (node.Parent != null)
        {
            node.Parent.children.Remove(node);
            node.Parent = null;
        }
    }

    private static void NodeToString(Element node, StringBuilder stringBuilder)
    {
        stringBuilder.Append('(');

        if (Contains(node.MetaData))
        {
            stringBuilder.Append('[');
            ContentToString(node.MetaData!, stringBuilder);
            stringBuilder.Append(']');
        }

        if (Contains(node.Data) && Contains(node.MetaData))
        {
            stringBuilder.Append(' ');
        }

        if (Contains(node.Data))
        {
            ContentToString(node.Data!, stringBuilder);
        }

        stringBuilder.Append(')');
    }

    private static void ContentToString(Stream content, StringBuilder stringBuilder)
    {
        byte[] bytes = content.AsByteArray();

        stringBuilder.Append(Convert.ToHexString(bytes));
        stringBuilder.Append(" = \"");
        stringBuilder.Append(Encoding.UTF8.GetString(bytes)
            .Select(c => c < '\u0020' || c > '\u007F' ? '.' : c)
            .ToArray());
        stringBuilder.Append('\"');
    }

    private static bool IsAncestor(Element ancestor, Element node)
    {
        for (Element? current = node.Parent; current != null; current = current.Parent)
        {
            if (current == ancestor)
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContentEquals(Stream? left, Stream? right)
    {
        long leftLength = left?.Length ?? 0;
        long rightLength = right?.Length ?? 0;

        if (leftLength != rightLength)
        {
            return false;
        }

        if (leftLength == 0 || ReferenceEquals(left, right))
        {
            return true;
        }

        // Compared in blocks, the positions of the streams are restored
        long leftPosition = left!.Position;
        long rightPosition = right!.Position;
        byte[] leftBuffer = ArrayPool<byte>.Shared.Rent(CompareBlockSize);
        byte[] rightBuffer = ArrayPool<byte>.Shared.Rent(CompareBlockSize);

        try
        {
            left.Position = 0;
            right.Position = 0;

            for (long remaining = leftLength; remaining > 0;)
            {
                int length = (int)Math.Min(remaining, CompareBlockSize);
                left.ReadExactly(leftBuffer, 0, length);
                right.ReadExactly(rightBuffer, 0, length);

                if (!leftBuffer.AsSpan(0, length).SequenceEqual(rightBuffer.AsSpan(0, length)))
                {
                    return false;
                }

                remaining -= length;
            }

            return true;
        }
        finally
        {
            left.Position = leftPosition;
            right.Position = rightPosition;
            ArrayPool<byte>.Shared.Return(leftBuffer);
            ArrayPool<byte>.Shared.Return(rightBuffer);
        }
    }

    private static MemoryStream? CopyStream(Stream? source)
    {
        if (source == null)
        {
            return null;
        }

        return new MemoryStream(source.AsByteArray());
    }

    private string DebuggerDisplay => ToString();

    #endregion
}
