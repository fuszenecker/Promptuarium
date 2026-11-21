using Promptuarium;

namespace Promptuarium.Benchmarks;

/// <summary>The shapes of the trees the benchmarks work on.</summary>
public enum TreeShape
{
    /// <summary>A root with every other node as its child.</summary>
    Wide,

    /// <summary>Every node has 8 children (a balanced tree).</summary>
    Balanced,

    /// <summary>A chain: every node has one child.</summary>
    Deep,
}

internal static class Trees
{
    /// <summary>A tree of <paramref name="nodes"/> nodes, with small, typical contents (an int, a string, a VarInt).</summary>
    public static Element Create(TreeShape shape, int nodes)
    {
        var root = Node(0);
        var created = new List<Element>(nodes) { root };

        for (int i = 1; i < nodes; i++)
        {
            Element parent = shape switch
            {
                TreeShape.Wide => root,
                TreeShape.Balanced => created[(i - 1) / 8],
                TreeShape.Deep => created[i - 1],
                _ => throw new ArgumentOutOfRangeException(nameof(shape)),
            };

            var node = Node(i);
            parent.Add(node);
            created.Add(node);
        }

        return root;
    }

    private static Element Node(int i) => (i % 3) switch
    {
        0 => new Element(Data.FromInt(i), Data.FromAsciiString("int")),
        1 => new Element(Data.FromUtf8String($"node {i}"), Data.FromAsciiString("string")),
        _ => new Element(Data.FromVarInt(-i), ReadOnlyMemory<byte>.Empty),
    };
}
