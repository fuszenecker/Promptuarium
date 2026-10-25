using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
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
        using var writer = new ChunkWriter(stream);

        // The tree is written in pre-order. The decoder starts on a virtual root (depth -1),
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
                // Up climbs one level only, the extra levels are climbed by empty Up markers
                for (int x = 0; x < previousDepth - depth - 1; x++)
                {
                    await writer.WriteControlByteAsync(ControlByte(Directions.Up, DataType.Data, SizeType.Linear, 0), cancellationToken).ConfigureAwait(false);
                }

                direction = Directions.Up;
            }

            await node.SerializeNodeAsync(writer, direction, cancellationToken).ConfigureAwait(false);
            previousDepth = depth;

            for (int childIndex = node.Children.Count - 1; childIndex >= 0; childIndex--)
            {
                nodes.Push((node.Children[childIndex], depth + 1));
            }
        }

        await writer.WriteControlByteAsync(ControlByte(Directions.Append, DataType.Data, SizeType.Linear, 0), cancellationToken).ConfigureAwait(false);
        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task SerializeNodeAsync(ChunkWriter writer, Directions direction, CancellationToken cancellationToken)
    {
        // A node without content is created by an empty chunk:
        // - a node with children is a placeholder (Data type), the decoder keeps it because of its children,
        // - an empty node is marked to be kept (MetaData type), otherwise the decoder would remove it.
        if (!Contains(Data) && !Contains(MetaData))
        {
            DataType marker = Children.Count > 0 ? DataType.Data : DataType.MetaData;
            await writer.WriteControlByteAsync(ControlByte(direction, marker, SizeType.Linear, 0), cancellationToken).ConfigureAwait(false);
        }

        #region Saving Data

        // Firing event
        OnDataSaving?.Invoke(this, new PromptuariumSavingEventArgs());

        if (Contains(Data))
        {
            direction = await SerializeContentAsync(writer, Data!, DataType.Data, direction, cancellationToken).ConfigureAwait(false);
        }

        // Firing event
        OnDataSaved?.Invoke(this, new PromptuariumSavedEventArgs());

        #endregion

        #region Saving MetaData

        // Firing event
        OnMetaDataSaving?.Invoke(this, new PromptuariumSavingEventArgs());

        if (Contains(MetaData))
        {
            await SerializeContentAsync(writer, MetaData!, DataType.MetaData, direction, cancellationToken).ConfigureAwait(false);
        }

        // Firing event
        OnMetaDataSaved?.Invoke(this, new PromptuariumSavedEventArgs());

        #endregion
    }

    /// <returns>The direction of the next chunk of the same node (always Append)</returns>
    private static async Task<Directions> SerializeContentAsync(ChunkWriter writer, Stream source, DataType dataType, Directions direction, CancellationToken cancellationToken)
    {
        // The position of the content is restored, saving does not change the stream
        long originalPosition = source.Position;
        source.Position = 0;

        try
        {
            foreach (SizeDescriptor size in GetSizes(source.Length))
            {
                int chunkSize = size.SizeType == SizeType.Linear ? size.SizeBits : 1 << (size.SizeBits + NibbleSizeInBits);

                await writer.WriteChunkAsync(ControlByte(direction, dataType, size.SizeType, size.SizeBits), source, chunkSize, cancellationToken).ConfigureAwait(false);
                direction = Directions.Append;
            }
        }
        finally
        {
            source.Position = originalPosition;
        }

        return direction;
    }

    private static byte ControlByte(Directions direction, DataType dataType, SizeType sizeType, byte sizeBits)
    {
        if (sizeBits <= NibbleMaxValue)
        {
            return (byte)((byte)direction | (byte)dataType | (byte)sizeType | sizeBits);
        }

        throw new PromptuariumException($"Size bits greater than the max value ({NibbleMaxValue.ToString(CultureInfo.InvariantCulture)} bits)");
    }

    private static IEnumerable<SizeDescriptor> GetSizes(long size)
    {
        long word = size;

        while (word > 0)
        {
            // Logarithmic sizes
            if (word > ChunkMaxValue)
            {
                yield return new SizeDescriptor
                {
                    SizeType = SizeType.Logarithmic,
                    SizeBits = NibbleMaxValue
                };

                word -= ChunkMaxValue;
            }
            else if (word > NibbleMaxValue)
            {
                // The highest set bit (between NibbleSizeInBits and ChunkSizeInBits)
                byte bit = (byte)(63 - BitOperations.LeadingZeroCount((ulong)word));

                yield return new SizeDescriptor
                {
                    SizeType = SizeType.Logarithmic,
                    SizeBits = (byte)(bit - NibbleSizeInBits)
                };

                word -= (long)1 << bit;
            }
            // Linear sizes
            else
            {
                yield return new SizeDescriptor
                {
                    SizeType = SizeType.Linear,
                    SizeBits = (byte)word
                };

                word = 0;
            }
        }
    }

    private static bool Contains(Stream? stream)
    {
        return stream?.Length > 0;
    }

    #endregion

    #region Deserialization routines

    private static async Task<Element> DeserializeAsync(Stream stream, LoadOptions? options, CancellationToken cancellationToken)
    {
        var root = new Element();
        Element parent = root;
        Element node = root;
        long nodes = 0;

        // Nodes created by an empty MetaData chunk: empty nodes that are part of the tree
        var keptNodes = new HashSet<Element>(ReferenceEqualityComparer.Instance);

        // The content of the current node that has been started (for the loaded events)
        bool dataStarted = false;
        bool metaDataStarted = false;

        void FinishNode()
        {
            if (dataStarted)
            {
                options?.FireDataLoaded(node);
            }

            if (metaDataStarted)
            {
                options?.FireMetaDataLoaded(node);
            }

            dataStarted = false;
            metaDataStarted = false;
        }

        Element AddNode(Element nodeParent)
        {
            if (++nodes > options?.MaxNodes)
            {
                throw new PromptuariumException($"The stream contains more nodes than the limit ({options.MaxNodes.Value.ToString(CultureInfo.InvariantCulture)}).");
            }

            FinishNode();

            var newNode = new Element();
            nodeParent.UnsafeAdd(newNode);
            return newNode;
        }

        using var reader = new ChunkReader(stream, options?.MaxBytes);

        try
        {
            // Reading stops at the terminator (or at the end of the stream), and never reads beyond it,
            // so the stream does not need to be seekable and may contain further data after the tree.
            int nextByte;

            while ((nextByte = await reader.ReadByteAsync(cancellationToken).ConfigureAwait(false)) >= 0)
            {
                byte controlByte = (byte)nextByte;

                switch (controlByte & (byte) Directions.Mask)
                {
                    case (byte) Directions.New:
                        node = AddNode(parent);
                        break;

                    case (byte) Directions.Down:
                        parent = node;
                        node = AddNode(parent);
                        break;

                    case (byte) Directions.Up:
                        if (parent.Parent != null)
                        {
                            parent = parent.Parent;
                            node = AddNode(parent);
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
                    if (!IsDataChunk(controlByte))
                    {
                        keptNodes.Add(node);
                    }

                    continue;
                }

                if (IsDataChunk(controlByte))
                {
                    if (!dataStarted)
                    {
                        node.Data ??= new MemoryStream();
                        dataStarted = true;
                        options?.FireDataLoading(node);
                    }

                    await reader.ReadPayloadAsync(node.Data!, chunkSize, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    if (!metaDataStarted)
                    {
                        node.MetaData ??= new MemoryStream();
                        metaDataStarted = true;
                        options?.FireMetaDataLoading(node);
                    }

                    await reader.ReadPayloadAsync(node.MetaData!, chunkSize, cancellationToken).ConfigureAwait(false);
                }
            }

            FinishNode();
        }
        finally
        {
            reader.Finish();
        }

        FinishLoadedTree(root, keptNodes);

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

    /// <summary>
    /// Removes the empty nodes (placeholders and Up markers, but not the kept empty nodes) and rewinds the loaded streams.
    /// </summary>
    private static void FinishLoadedTree(Element root, HashSet<Element> keptNodes)
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
            node.children.RemoveAll(child => child.Data == null && child.MetaData == null && child.Children.Count == 0 && !keptNodes.Contains(child));
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
