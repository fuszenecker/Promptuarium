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

    private class SerializationArguments
    {
        public Element? PreviouslySerialized;
        public int StepUpNodesRequired;
    }

    #endregion

    #region Serialization routines

    private async Task SerializeAsync(Stream stream, SerializationArguments serializationArguments, CancellationToken cancellationToken)
    {
        for (int x = 0; x < serializationArguments.StepUpNodesRequired; x++)
        {
            stream.WriteByte(ControlByte(Directions.Up, DataType.Data, SizeType.Linear, 0));
        }

        serializationArguments.StepUpNodesRequired = 0;

        Directions direction;

        if (serializationArguments.PreviouslySerialized == Parent || serializationArguments.PreviouslySerialized == null)
        {
            direction = Directions.Down;
        }
        else if (serializationArguments.PreviouslySerialized.Parent == Parent)
        {
            direction = Directions.New;
        }
        else if (serializationArguments.PreviouslySerialized?.Parent?.Parent == Parent)
        {
            direction = Directions.Up;
        }
        else
        {
            throw new PromptuariumException("Previously serialized node is invalid");
        }

        bool appending = false;

        // If the node has only children
        if (!Contains(Data) && !Contains(MetaData) && Children.Count > 0)
        {
            stream.WriteByte(ControlByte(direction, DataType.Data, SizeType.Linear, 0));
        }

        #region Saving Data

        // Firing event
        OnDataSaving?.Invoke(this, new PromptuariumSavingEventArgs());

        if (Contains(Data))
        {
            appending = await SerializeContentAsync(stream, Data!, DataType.Data, direction, appending, cancellationToken).ConfigureAwait(false);
        }

        // Firing event
        OnDataSaved?.Invoke(this, new PromptuariumSavedEventArgs());

        #endregion

        #region Saving MetaData

        // Firing event
        OnMetaDataSaving?.Invoke(this, new PromptuariumSavingEventArgs());

        if (Contains(MetaData))
        {
            appending = await SerializeContentAsync(stream, MetaData!, DataType.MetaData, direction, appending, cancellationToken).ConfigureAwait(false);
        }

        // Firing event
        OnMetaDataSaved?.Invoke(this, new PromptuariumSavedEventArgs());

        #endregion

        serializationArguments.PreviouslySerialized = this;

        if (Children.Count > 0)
        {
            var child = new Element();

            for (int childIndex = 0; childIndex < Children.Count; childIndex++)
            {
                child = Children[childIndex];

                if (child != null)
                {
                    await child.SerializeAsync(stream, serializationArguments, cancellationToken).ConfigureAwait(false);
                }
            }

            if (child?.Children.Count > 0)
            {
                serializationArguments.StepUpNodesRequired++;
                serializationArguments.PreviouslySerialized = child;
            }
        }
    }

    private static async Task<bool> SerializeContentAsync(Stream target, Stream source, DataType dataType, Directions direction, bool appending, CancellationToken cancellationToken)
    {
        IEnumerable<SizeDescriptor> chunks = GetSizes(source.Length);
        source.Position = 0;

        foreach (SizeDescriptor size in chunks)
        {
            if (!appending)
            {
                target.WriteByte(ControlByte(direction, dataType, size.SizeType, size.SizeBits));
                appending = true;
            }
            else
            {
                target.WriteByte(ControlByte(Directions.Append, dataType, size.SizeType, size.SizeBits));
            }

            int bufferSize = size.SizeType == SizeType.Linear ? size.SizeBits : 1 << (size.SizeBits + NibbleSizeInBits);

            byte[] buffer = new byte[bufferSize];
            await source.ReadAsync(buffer.AsMemory(0, bufferSize), cancellationToken).ConfigureAwait(false);
            await target.WriteAsync(buffer.AsMemory(0, bufferSize), cancellationToken).ConfigureAwait(false);
        }

        return appending;
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

    #endregion

    #region Deserialization routines

    private async Task<Element> DeserializeAsync(Stream stream, CancellationToken cancellationToken)
    {
        var root = new Element();
        Element parent = root;
        Element node = root;

        while (stream.Position < stream.Length)
        {
            byte controlByte = (byte) stream.ReadByte();

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

            byte[] buffer = new byte[chunkSize];
            await stream.ReadAsync(buffer.AsMemory(0, chunkSize), cancellationToken).ConfigureAwait(false);

            if (IsDataChunk(controlByte))
            {
                if (chunkSize > 0)
                {
                    node.Data ??= new MemoryStream();

                    // Firing event
                    OnDataLoading?.Invoke(node, new PromptuariumLoadingEventArgs());

                    await node.Data.WriteAsync(buffer.AsMemory(0, chunkSize), cancellationToken).ConfigureAwait(false);

                    // Firing event
                    OnDataLoaded?.Invoke(node, new PromptuariumLoadedEventArgs());
                }
            }
            else
            {
                if (chunkSize > 0)
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

        PurgeEmptyNodes(root);

        if (root.Children.Count > 0)
        {
            return root.Children[0];
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

    private static void PurgeEmptyNodes(Element node)
    {
        foreach (Element child in new List<Element>(node.Children))
        {
            PurgeEmptyNodes(child);
        }

        if (node.Data == null && node.MetaData == null && node.Children.Count == 0)
        {
            node.Parent?.Remove(node);
        }
    }

    #endregion
}
