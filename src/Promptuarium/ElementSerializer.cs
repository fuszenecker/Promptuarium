using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace Promptuarium;

public partial class Element
{
    #region Encoding

    /// <summary>
    /// The canonical chunks of the tree (BINARY_FORMAT.md, section 6), the terminator included.
    /// A payload is a slice of the content, nothing is copied.
    /// </summary>
    private IEnumerable<(byte Control, ReadOnlyMemory<byte> Payload)> EncodeChunks()
    {
        // Pre-order, without recursion. The decoder starts on a virtual root (depth -1),
        // so the direction of each node follows from its depth and the depth of the previously written node.
        var nodes = new Stack<(Element Node, int Depth)>();
        nodes.Push((this, 0));
        int previousDepth = -1;

        while (nodes.Count > 0)
        {
            (Element node, int depth) = nodes.Pop();
            Directions direction;

            if (depth > previousDepth)
            {
                direction = Directions.Down;
            }
            else if (depth == previousDepth)
            {
                direction = Directions.New;
            }
            else
            {
                // Up climbs one level only, the extra levels are climbed by Up markers
                for (int level = 0; level < previousDepth - depth - 1; level++)
                {
                    yield return (ControlByte(Directions.Up, DataType.Data, SizeType.Linear, 0), ReadOnlyMemory<byte>.Empty);
                }

                direction = Directions.Up;
            }

            if (node.Data.IsEmpty && node.MetaData.IsEmpty)
            {
                // A placeholder (Data type) for a node with children, an empty node (MetaData type) otherwise
                DataType marker = node.Children.Count > 0 ? DataType.Data : DataType.MetaData;
                yield return (ControlByte(direction, marker, SizeType.Linear, 0), ReadOnlyMemory<byte>.Empty);
            }

            foreach ((byte control, ReadOnlyMemory<byte> payload) in EncodeContent(node.Data, DataType.Data, direction))
            {
                yield return (control, payload);
                direction = Directions.Append;
            }

            foreach ((byte control, ReadOnlyMemory<byte> payload) in EncodeContent(node.MetaData, DataType.MetaData, direction))
            {
                yield return (control, payload);
                direction = Directions.Append;
            }

            previousDepth = depth;

            for (int index = node.Children.Count - 1; index >= 0; index--)
            {
                nodes.Push((node.Children[index], depth + 1));
            }
        }

        yield return (ControlByte(Directions.Append, DataType.Data, SizeType.Linear, 0), ReadOnlyMemory<byte>.Empty);
    }

    /// <summary>
    /// The chunks of a content (BINARY_FORMAT.md, 6.2): full 512 KiB chunks, the remainder in binary, the low 4 bits as a linear chunk.
    /// The first chunk has the given direction, the others are Append chunks.
    /// </summary>
    private static IEnumerable<(byte Control, ReadOnlyMemory<byte> Payload)> EncodeContent(ReadOnlyMemory<byte> content, DataType dataType, Directions direction)
    {
        while (!content.IsEmpty)
        {
            byte control;
            int length;

            if (content.Length > ChunkMaxValue)
            {
                control = ControlByte(direction, dataType, SizeType.Logarithmic, NibbleMaxValue);
                length = (int)ChunkMaxValue;
            }
            else if (content.Length > NibbleMaxValue)
            {
                // The highest set bit, between NibbleSizeInBits and ChunkSizeInBits
                int bit = BitOperations.Log2((uint)content.Length);
                control = ControlByte(direction, dataType, SizeType.Logarithmic, (byte)(bit - NibbleSizeInBits));
                length = 1 << bit;
            }
            else
            {
                control = ControlByte(direction, dataType, SizeType.Linear, (byte)content.Length);
                length = content.Length;
            }

            yield return (control, content[..length]);
            content = content[length..];
            direction = Directions.Append;
        }
    }

    private async Task SerializeAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var writer = new ChunkWriter(stream);

        foreach ((byte control, ReadOnlyMemory<byte> payload) in EncodeChunks())
        {
            await writer.WriteAsync(control, payload, cancellationToken).ConfigureAwait(false);
        }

        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static byte ControlByte(Directions direction, DataType dataType, SizeType sizeType, byte sizeBits)
    {
        if (sizeBits <= NibbleMaxValue)
        {
            return (byte)((byte)direction | (byte)dataType | (byte)sizeType | sizeBits);
        }

        throw new PromptuariumException($"Size bits greater than the max value ({NibbleMaxValue.ToString(CultureInfo.InvariantCulture)} bits)");
    }

    #endregion

    #region Decoding

    private static async Task<Element> DeserializeAsync(Stream stream, LoadOptions? options, CancellationToken cancellationToken)
    {
        var decoder = new Decoder(options);

        // On a seekable stream, blocks are read ahead, and the stream is positioned back after the tree at the end.
        // On a non-seekable stream, only the bytes the decoder needs are read, so nothing after the tree is consumed.
        bool readAhead = stream.CanSeek;
        byte[] buffer = ArrayPool<byte>.Shared.Rent(ReadBufferSize);
        int position = 0;
        int count = 0;

        try
        {
            while (!decoder.Done)
            {
                if (position == count)
                {
                    int length = readAhead ? ReadBufferSize : Math.Min(decoder.Wanted, ReadBufferSize);
                    count = await stream.ReadAsync(buffer.AsMemory(0, length), cancellationToken).ConfigureAwait(false);
                    position = 0;

                    if (count == 0)
                    {
                        break;
                    }
                }

                position += decoder.Push(buffer.AsSpan(position, count - position));
            }

            if (readAhead && count > position)
            {
                stream.Seek(position - count, SeekOrigin.Current);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        return decoder.Finish();
    }

    /// <summary>
    /// A push-based decoder (BINARY_FORMAT.md, section 5): it is fed with bytes until it is done, then it gives the tree.
    /// It never consumes bytes after the terminator.
    /// </summary>
    private sealed class Decoder
    {
        private readonly long maxBytes;
        private readonly long maxNodes;
        private readonly Element virtualRoot = new();

        // Nodes created by an empty MetaData chunk: empty nodes that are part of the tree
        private readonly HashSet<Element> keptNodes = new(ReferenceEqualityComparer.Instance);

        // The contents of the current node, collected until the decoder moves to another node
        private readonly ArrayBufferWriter<byte> data = new();
        private readonly ArrayBufferWriter<byte> metaData = new();

        private Element parent;
        private Element node;

        // The rest of the payload being read
        private int remaining;
        private ArrayBufferWriter<byte>? payloadTarget;

        private long bytes;
        private long nodes;

        public Decoder(LoadOptions? options)
        {
            maxBytes = options?.MaxBytes ?? long.MaxValue;
            maxNodes = options?.MaxNodes ?? long.MaxValue;
            parent = virtualRoot;
            node = virtualRoot;
        }

        /// <summary>True after the terminator.</summary>
        public bool Done { get; private set; }

        /// <summary>The number of bytes the decoder needs at least: 1 for a control byte, or the rest of the payload.</summary>
        public int Wanted => remaining > 0 ? remaining : 1;

        /// <returns>The number of bytes consumed; less than the length of the input only if the terminator was reached</returns>
        public int Push(ReadOnlySpan<byte> input)
        {
            int offset = 0;

            while (offset < input.Length && !Done)
            {
                if (remaining > 0)
                {
                    int length = Math.Min(remaining, input.Length - offset);
                    Count(length);
                    payloadTarget!.Write(input.Slice(offset, length));
                    remaining -= length;
                    offset += length;
                    continue;
                }

                Count(1);
                Chunk(input[offset++]);
            }

            return offset;
        }

        /// <summary>
        /// Removes the auxiliary nodes and returns the tree. A missing terminator is accepted, but a stream that ends inside a payload is an error.
        /// </summary>
        public Element Finish()
        {
            if (remaining > 0)
            {
                throw new PromptuariumException("Unexpected end of stream during deserialization.");
            }

            FinishNode();
            RemoveAuxiliaryNodes();

            if (virtualRoot.Children.Count == 0)
            {
                return new Element();
            }

            Element root = virtualRoot.Children[0];
            root.Parent = null;
            return root;
        }

        private void Chunk(byte controlByte)
        {
            switch (controlByte & (byte)Directions.Mask)
            {
                case (byte)Directions.New:
                    NewNode(parent);
                    break;

                case (byte)Directions.Down:
                    parent = node;
                    NewNode(parent);
                    break;

                case (byte)Directions.Up:
                    parent = parent.Parent ?? throw new PromptuariumException("Navigating upper the root node during deserialization.");
                    NewNode(parent);
                    break;
            }

            bool isMetaData = (controlByte & (byte)DataType.Mask) == (byte)DataType.MetaData;
            int length = (controlByte & (byte)SizeType.Mask) == (byte)SizeType.Logarithmic
                ? 1 << ((controlByte & NibbleMaxValue) + NibbleSizeInBits)
                : controlByte & NibbleMaxValue;

            if (length > 0)
            {
                remaining = length;
                payloadTarget = isMetaData ? metaData : data;
            }
            else if ((controlByte & (byte)Directions.Mask) == (byte)Directions.Append)
            {
                Done = true;
            }
            else if (isMetaData)
            {
                // An empty node (60, A0, E0): part of the tree
                keptNodes.Add(node);
            }
        }

        private void NewNode(Element nodeParent)
        {
            if (++nodes > maxNodes)
            {
                throw new PromptuariumException($"The stream contains more nodes than the limit ({maxNodes.ToString(CultureInfo.InvariantCulture)}).");
            }

            FinishNode();
            node = new Element();
            nodeParent.UnsafeAdd(node);
        }

        /// <summary>Stores the collected contents into the current node.</summary>
        private void FinishNode()
        {
            if (data.WrittenCount > 0)
            {
                node.Data = data.WrittenSpan.ToArray();
                data.ResetWrittenCount();
            }

            if (metaData.WrittenCount > 0)
            {
                node.MetaData = metaData.WrittenSpan.ToArray();
                metaData.ResetWrittenCount();
            }
        }

        private void Count(int length)
        {
            bytes += length;

            if (bytes > maxBytes)
            {
                throw new PromptuariumException($"The stream is longer than the limit ({maxBytes.ToString(CultureInfo.InvariantCulture)} bytes).");
            }
        }

        /// <summary>Removes the auxiliary nodes that ended up empty, bottom-up and in linear time.</summary>
        private void RemoveAuxiliaryNodes()
        {
            // In reverse pre-order every node comes after its descendants
            List<Element> preOrder = PreOrder(virtualRoot);

            for (int index = preOrder.Count - 1; index >= 0; index--)
            {
                preOrder[index].children.RemoveAll(child =>
                    child.Data.IsEmpty && child.MetaData.IsEmpty && child.Children.Count == 0 && !keptNodes.Contains(child));
            }
        }
    }

    private static List<Element> PreOrder(Element root)
    {
        var result = new List<Element>();
        var nodes = new Stack<Element>();
        nodes.Push(root);

        while (nodes.Count > 0)
        {
            Element node = nodes.Pop();
            result.Add(node);

            for (int index = node.Children.Count - 1; index >= 0; index--)
            {
                nodes.Push(node.Children[index]);
            }
        }

        return result;
    }

    #endregion
}
