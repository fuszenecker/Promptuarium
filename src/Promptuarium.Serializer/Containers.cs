namespace Promptuarium.Serializer;

/// <summary>
/// The values of CONTAINER (SERIALIZER_FORMAT.md, 6.6): a nested tree, one complete Promptuarium stream, with its
/// terminator and nothing after it.
/// </summary>
internal static class Containers
{
    /// <summary>
    /// The tree of a CONTAINER value. The value is decoded within <paramref name="limits"/>, and rejected if it is not a
    /// valid stream, if it ends without its terminator, or if bytes follow its terminator.
    /// </summary>
    /// <exception cref="InvalidDataException">The value is not one complete Promptuarium stream.</exception>
    public static Element Decode(ReadOnlyMemory<byte> value, LoadOptions? limits)
    {
        if (value.IsEmpty)
        {
            throw new InvalidDataException("A CONTAINER value is not empty: the smallest one is 60 00, an empty tree.");
        }

        // One byte more: a stream without its terminator consumes it too, a complete one stops before it
        byte[] probe = new byte[value.Length + 1];
        value.Span.CopyTo(probe);

        Element tree;
        int consumed;

        try
        {
            tree = Element.FromBytes(probe, limits, out consumed);
        }
        catch (PromptuariumException exception)
        {
            throw new InvalidDataException("A CONTAINER value is not a Promptuarium stream.", exception);
        }

        if (consumed > value.Length)
        {
            throw new InvalidDataException("A CONTAINER value ends without the terminator of its stream.");
        }

        if (consumed < value.Length)
        {
            throw new InvalidDataException($"A CONTAINER value has {value.Length - consumed} bytes after the terminator of its stream.");
        }

        return tree;
    }

    /// <summary>The depth of the deepest node of <paramref name="tree"/>, if its root is at <paramref name="rootDepth"/>.</summary>
    public static int DeepestLevel(Element tree, int rootDepth)
    {
        int deepest = rootDepth;
        var pending = new Stack<(Element Node, int Depth)>();
        pending.Push((tree, rootDepth));

        while (pending.Count > 0)
        {
            (Element node, int depth) = pending.Pop();
            deepest = Math.Max(deepest, depth);

            foreach (Element child in node.Children)
            {
                pending.Push((child, depth + 1));
            }
        }

        return deepest;
    }
}
