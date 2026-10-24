using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace Promptuarium;

public partial class Element
{
    #region Private types

    private struct SizeDescriptor
    {
        public SizeType SizeType;
        public byte SizeBits;
    }

    #endregion

    #region Serialization routines

    private async Task SerializeAsync(Stream stream, CancellationToken cancellationToken)
    {
        // The buffer holds a control byte and the largest possible payload
        byte[] buffer = ArrayPool<byte>.Shared.Rent((int)ChunkMaxValue + 1);

        try
        {
            // The tree is written in pre-order. The decoder starts on a virtual root (depth -1),
            // so the direction of each node follows from its depth and the depth of the previously written node.
            var nodes = new Stack<(Element Node, int Depth)>();
            nodes.Push((this, 0));
            int previousDepth = -1;

            while (nodes.Count > 0)
            {
                (Element node, int depth) = nodes.Pop();

                // Empty nodes are not written, so they must not affect the navigation of the following nodes
                if (IsEmpty(node))
                {
                    continue;
                }

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
                    // Up climbs one level only, the extra levels are climbed by empty Up markers
                    for (int x = 0; x < previousDepth - depth - 1; x++)
                    {
                        await WriteControlByteAsync(stream, buffer, ControlByte(Directions.Up, DataType.Data, SizeType.Linear, 0), cancellationToken).ConfigureAwait(false);
                    }

                    direction = Directions.Up;
                }

                await node.SerializeNodeAsync(stream, buffer, direction, cancellationToken).ConfigureAwait(false);
                previousDepth = depth;

                for (int childIndex = node.Children.Count - 1; childIndex >= 0; childIndex--)
                {
                    Element child = node.Children[childIndex];

                    if (child != null)
                    {
                        nodes.Push((child, depth + 1));
                    }
                }
            }

            await WriteControlByteAsync(stream, buffer, ControlByte(Directions.Append, DataType.Data, SizeType.Linear, 0), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private async Task SerializeNodeAsync(Stream stream, byte[] buffer, Directions direction, CancellationToken cancellationToken)
    {
        // If the node has only children, an empty chunk creates the node in the decoder
        if (!Contains(Data) && !Contains(MetaData))
        {
            await WriteControlByteAsync(stream, buffer, ControlByte(direction, DataType.Data, SizeType.Linear, 0), cancellationToken).ConfigureAwait(false);
        }

        #region Saving Data

        // Firing event
        OnDataSaving?.Invoke(this, new PromptuariumSavingEventArgs());

        if (Contains(Data))
        {
            direction = await SerializeContentAsync(stream, Data!, buffer, DataType.Data, direction, cancellationToken).ConfigureAwait(false);
        }

        // Firing event
        OnDataSaved?.Invoke(this, new PromptuariumSavedEventArgs());

        #endregion

        #region Saving MetaData

        // Firing event
        OnMetaDataSaving?.Invoke(this, new PromptuariumSavingEventArgs());

        if (Contains(MetaData))
        {
            await SerializeContentAsync(stream, MetaData!, buffer, DataType.MetaData, direction, cancellationToken).ConfigureAwait(false);
        }

        // Firing event
        OnMetaDataSaved?.Invoke(this, new PromptuariumSavedEventArgs());

        #endregion
    }

    /// <returns>The direction of the next chunk of the same node (always Append)</returns>
    private static async Task<Directions> SerializeContentAsync(Stream target, Stream source, byte[] buffer, DataType dataType, Directions direction, CancellationToken cancellationToken)
    {
        List<SizeDescriptor> chunks = GetSizes(source.Length);
        source.Position = 0;

        foreach (SizeDescriptor size in chunks)
        {
            int chunkSize = size.SizeType == SizeType.Linear ? size.SizeBits : 1 << (size.SizeBits + NibbleSizeInBits);

            // The control byte and the payload are written at once
            buffer[0] = ControlByte(direction, dataType, size.SizeType, size.SizeBits);
            await source.ReadExactlyAsync(buffer.AsMemory(1, chunkSize), cancellationToken).ConfigureAwait(false);
            await target.WriteAsync(buffer.AsMemory(0, chunkSize + 1), cancellationToken).ConfigureAwait(false);

            direction = Directions.Append;
        }

        return direction;
    }

    private static async Task WriteControlByteAsync(Stream stream, byte[] buffer, byte controlByte, CancellationToken cancellationToken)
    {
        buffer[0] = controlByte;
        await stream.WriteAsync(buffer.AsMemory(0, 1), cancellationToken).ConfigureAwait(false);
    }

    private static byte ControlByte(Directions direction, DataType dataType, SizeType sizeType, byte sizeBits)
    {
        if (sizeBits <= NibbleMaxValue)
        {
            return (byte)((byte)direction | (byte)dataType | (byte)sizeType | sizeBits);
        }

        throw new PromptuariumException($"Size bits greater than the max value ({NibbleMaxValue.ToString(CultureInfo.InvariantCulture)} bits)");
    }

    private static List<SizeDescriptor> GetSizes(long size)
    {
        var sizes = new List<SizeDescriptor>();
        long word = size;

        while (word > 0)
        {
            // Logarithmic sizes
            if (word > ChunkMaxValue)
            {
                sizes.Add(new SizeDescriptor
                {
                    SizeType = SizeType.Logarithmic,
                    SizeBits = NibbleMaxValue
                });

                word -= ChunkMaxValue;
            }
            else if (word > NibbleMaxValue)
            {
                byte bit;

                for (bit = ChunkSizeInBits; (bit >= NibbleSizeInBits) && (word & ((long) 1 << bit)) == 0;)
                {
                    bit--;
                }

                sizes.Add(new SizeDescriptor
                {
                    SizeType = SizeType.Logarithmic,
                    SizeBits = (byte)(bit - NibbleSizeInBits)
                });

                word -= (long)1 << bit;
            }
            // Linear sizes
            else
            {
                sizes.Add(new SizeDescriptor
                {
                    SizeType = SizeType.Linear,
                    SizeBits = (byte)word
                });

                word = 0;
            }
        }

        return sizes;
    }

    private static bool Contains(Stream? stream)
    {
        return stream?.Length > 0;
    }

    private static bool IsEmpty(Element node)
    {
        return !Contains(node.Data) && !Contains(node.MetaData) && node.Children.Count == 0;
    }

    #endregion

    #region Deserialization routines

    private static async Task<Element> DeserializeAsync(Stream stream, CancellationToken cancellationToken)
    {
        var root = new Element();
        Element parent = root;
        Element node = root;
        byte[] buffer = ArrayPool<byte>.Shared.Rent((int)ChunkMaxValue);

        try
        {
            // Reading stops at the terminator (or at the end of the stream), and never reads beyond it,
            // so the stream does not need to be seekable and may contain further data after the tree.
            while (await stream.ReadAsync(buffer.AsMemory(0, 1), cancellationToken).ConfigureAwait(false) > 0)
            {
                byte controlByte = buffer[0];

                switch (controlByte & (byte) Directions.Mask)
                {
                    case (byte) Directions.New:
                        node = AddNewNode(parent);
                        break;

                    case (byte) Directions.Down:
                        parent = node;
                        node = AddNewNode(parent);
                        break;

                    case (byte) Directions.Up:
                        if (parent.Parent != null)
                        {
                            parent = parent.Parent;
                            node = AddNewNode(parent);
                        }
                        else
                        {
                            throw new PromptuariumException("Navigating upper the root node during deserialization.");
                        }

                        break;

                    case (byte) Directions.Append:
                        break;
                }

                int chunkSize = GetChunkSize(controlByte);

                if (((controlByte & (byte)Directions.Mask) == (byte) Directions.Append) && (chunkSize == 0))
                {
                    break;
                }

                if (chunkSize == 0)
                {
                    continue;
                }

                try
                {
                    await stream.ReadExactlyAsync(buffer.AsMemory(0, chunkSize), cancellationToken).ConfigureAwait(false);
                }
                catch (EndOfStreamException ex)
                {
                    throw new PromptuariumException("Unexpected end of stream during deserialization.", ex);
                }

                if (IsDataChunk(controlByte))
                {
                    node.Data ??= new MemoryStream();

                    // Firing event
                    OnDataLoading?.Invoke(node, new PromptuariumLoadingEventArgs());

                    await node.Data.WriteAsync(buffer.AsMemory(0, chunkSize), cancellationToken).ConfigureAwait(false);

                    // Firing event
                    OnDataLoaded?.Invoke(node, new PromptuariumLoadedEventArgs());
                }
                else
                {
                    node.MetaData ??= new MemoryStream();

                    // Firing event
                    OnMetaDataLoading?.Invoke(node, new PromptuariumLoadingEventArgs());

                    await node.MetaData.WriteAsync(buffer.AsMemory(0, chunkSize), cancellationToken).ConfigureAwait(false);

                    // Firing event
                    OnMetaDataLoaded?.Invoke(node, new PromptuariumLoadedEventArgs());
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        FinishLoadedTree(root);

        if (root.Children.Count > 0)
        {
            Element result = root.Children[0];
            result.Parent = null;
            return result;
        }
        else
        {
            return new Element();
        }
    }

    private static int GetChunkSize(byte controlByte) => IsSizeLogarithmic(controlByte) ?
        1 << ((controlByte & NibbleMaxValue) + NibbleSizeInBits) :
        controlByte & NibbleMaxValue;

    private static bool IsSizeLogarithmic(byte controlByte) => (controlByte & (byte)SizeType.Mask) == (byte)SizeType.Logarithmic;

    private static bool IsDataChunk(byte controlByte) => (controlByte & (byte)DataType.Mask) == (byte)DataType.Data;

    private static Element AddNewNode(Element parent)
    {
        var node = new Element();
        parent.UnsafeAdd(node);
        return node;
    }

    /// <summary>
    /// Removes the empty nodes (placeholders) and rewinds the loaded streams.
    /// </summary>
    private static void FinishLoadedTree(Element root)
    {
        // In reverse pre-order every node comes after its descendants,
        // so a node's children are already purged when the node itself is processed.
        // Iterating (instead of recursion) keeps arbitrarily deep trees from overflowing the stack.
        List<Element> nodes = PreOrder(root);

        for (int index = nodes.Count - 1; index >= 0; index--)
        {
            Element node = nodes[index];
            node.Data?.Seek(0, SeekOrigin.Begin);
            node.MetaData?.Seek(0, SeekOrigin.Begin);
            ((List<Element>)node.Children).RemoveAll(child => child.Data == null && child.MetaData == null && child.Children.Count == 0);
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

            for (int childIndex = node.Children.Count - 1; childIndex >= 0; childIndex--)
            {
                nodes.Push(node.Children[childIndex]);
            }
        }

        return result;
    }

    #endregion
}
