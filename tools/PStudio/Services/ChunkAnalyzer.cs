using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using PEdit;
using PStudio.Interfaces;
using PStudio.Models;
using Promptuarium;

namespace PStudio.Services;

/// <summary>
/// Lists the chunks with the decoder of pedit, which reads the bytes directly from the specification,
/// and tells which node each chunk belongs to.
/// </summary>
public sealed class ChunkAnalyzer : IChunkAnalyzer
{
    /// <inheritdoc/>
    public IReadOnlyList<ChunkRow> Analyze(Element root)
    {
        ArgumentNullException.ThrowIfNull(root);

        using var stream = new MemoryStream(root.ToBytes(), writable: false);
        (List<Chunk> chunks, _, _) = ChunkDecoder.Decode(stream);
        int[] nodeIndexes = AssignNodes(chunks);

        return chunks.Select((chunk, i) => new ChunkRow(
            chunk.Offset.ToString("X6", CultureInfo.InvariantCulture),
            chunk.Control.ToString("X2", CultureInfo.InvariantCulture),
            chunk.Direction,
            chunk.ContentType,
            chunk.SizeType == "logarithmic" ? $"{chunk.Length} (log)" : chunk.Length.ToString(CultureInfo.InvariantCulture),
            chunk.Kind,
            chunk.Depth,
            ContentFormat.Preview(chunk.Payload.Span, 32),
            nodeIndexes[i],
            chunk.Offset,
            1 + chunk.Length)).ToList();
    }

    // Rebuilds the tree from the chunks (see BINARY_FORMAT.md): the auxiliary nodes that stay empty are removed,
    // and the pre-order indexes of the remaining nodes are the same as the ones of the elements.
    private static int[] AssignNodes(List<Chunk> chunks)
    {
        int[] indexes = Enumerable.Repeat(-1, chunks.Count).ToArray();
        var virtualRoot = new DecodedNode(null, auxiliary: false);
        DecodedNode parent = virtualRoot;
        DecodedNode node = virtualRoot;

        for (int i = 0; i < chunks.Count; i++)
        {
            Chunk chunk = chunks[i];
            bool auxiliary = chunk.Kind == "auxiliary";

            switch (chunk.Direction)
            {
                case "Append" when chunk.Kind == "terminator":
                    continue;
                case "Append":
                    node.Chunks.Add(i);
                    node.HasContent = true;
                    continue;
                case "Down":
                    parent = node;
                    break;
                case "Up":
                    parent = parent.Parent ?? virtualRoot;
                    break;
            }

            node = new DecodedNode(parent, auxiliary) { HasContent = chunk.Length > 0 };
            node.Chunks.Add(i);
            parent.Children.Add(node);
        }

        int next = 0;

        foreach (DecodedNode decoded in virtualRoot.Children)
        {
            Number(decoded, indexes, ref next);
        }

        return indexes;
    }

    // Numbers the kept nodes in pre-order (iteratively, so deep trees do not overflow the stack)
    private static void Number(DecodedNode root, int[] indexes, ref int next)
    {
        Prune(root);

        var stack = new Stack<DecodedNode>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            DecodedNode node = stack.Pop();

            if (node.Removed)
            {
                continue;
            }

            foreach (int chunk in node.Chunks)
            {
                indexes[chunk] = next;
            }

            next++;

            for (int i = node.Children.Count - 1; i >= 0; i--)
            {
                stack.Push(node.Children[i]);
            }
        }
    }

    // An auxiliary node without content and kept children is removed (post-order, so a removal can empty its parent)
    private static void Prune(DecodedNode root)
    {
        var stack = new Stack<(DecodedNode Node, bool Visited)>();
        stack.Push((root, false));

        while (stack.Count > 0)
        {
            (DecodedNode node, bool visited) = stack.Pop();

            if (!visited)
            {
                stack.Push((node, true));

                foreach (DecodedNode child in node.Children)
                {
                    stack.Push((child, false));
                }
            }
            else
            {
                node.Removed = node.Auxiliary && !node.HasContent && node.Children.All(child => child.Removed);
            }
        }
    }

    private sealed class DecodedNode(DecodedNode? parent, bool auxiliary)
    {
        public DecodedNode? Parent { get; } = parent;

        public bool Auxiliary { get; } = auxiliary;

        public bool HasContent { get; set; }

        public bool Removed { get; set; }

        public List<int> Chunks { get; } = [];

        public List<DecodedNode> Children { get; } = [];
    }
}
