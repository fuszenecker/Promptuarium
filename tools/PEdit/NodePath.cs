using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Promptuarium;

namespace PEdit;

/// <summary>
/// Address of a node: "/" is the root, "/0/2" is the third child of the first child of the root.
/// </summary>
internal sealed class NodePath
{
    private readonly int[] indexes;

    private NodePath(int[] indexes)
    {
        this.indexes = indexes;
    }

    /// <summary>
    /// The path of the root.
    /// </summary>
    public static NodePath Root { get; } = new(Array.Empty<int>());

    /// <summary>
    /// True for the path of the root.
    /// </summary>
    public bool IsRoot => indexes.Length == 0;

    /// <summary>
    /// The index of the node among its siblings (not for the root).
    /// </summary>
    public int LastIndex => indexes[^1];

    /// <summary>
    /// The path of the parent (not for the root).
    /// </summary>
    public NodePath Parent => new(indexes[..^1]);

    /// <summary>
    /// Parses a path like "/", "/0" or "/0/2".
    /// </summary>
    public static NodePath Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (text == "/")
        {
            return Root;
        }

        if (!text.StartsWith('/') || text.EndsWith('/'))
        {
            throw new PEditException(ExitCodes.Usage, $"Invalid node path '{text}'. A path is '/' (the root) or '/i/j/...' with child indexes, e.g. '/0/2'.");
        }

        var result = new List<int>();

        foreach (string part in text[1..].Split('/'))
        {
            if (!int.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out int index))
            {
                throw new PEditException(ExitCodes.Usage, $"Invalid node path '{text}': '{part}' is not a child index.");
            }

            result.Add(index);
        }

        return new NodePath(result.ToArray());
    }

    /// <summary>
    /// The path of a child of this node.
    /// </summary>
    public NodePath Child(int index) => new(indexes.Append(index).ToArray());

    /// <summary>
    /// Finds the node in the tree.
    /// </summary>
    public Element Resolve(Element root)
    {
        ArgumentNullException.ThrowIfNull(root);

        Element node = root;

        for (int depth = 0; depth < indexes.Length; depth++)
        {
            int index = indexes[depth];

            if (index >= node.Children.Count)
            {
                throw new PEditException(ExitCodes.NotFound, $"Node {this} does not exist: {Prefix(depth)} has {node.Children.Count.ToString(CultureInfo.InvariantCulture)} children.");
            }

            node = node.Children[index];
        }

        return node;
    }

    /// <inheritdoc/>
    public override string ToString() => IsRoot ? "/" : "/" + string.Join('/', indexes.Select(i => i.ToString(CultureInfo.InvariantCulture)));

    private string Prefix(int depth) => new NodePath(indexes[..depth]).ToString();
}
