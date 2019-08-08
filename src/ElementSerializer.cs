﻿using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace Promptuarium
{
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
            public Element previouslySerialized;
            public int stepUpNodesRequired;
        }

        #endregion

        #region Serialization ruotines

        private async Task SerializeAsync(Stream stream, SerializationArguments serializationArguments)
        {
            for (int x = 0; x < serializationArguments.stepUpNodesRequired; x++)
            {
                stream.WriteByte(ControlByte(Directions.Up, DataType.Data, SizeType.Linear, 0));
            }

            serializationArguments.stepUpNodesRequired = 0;

            Directions direction;

            if (serializationArguments.previouslySerialized == Parent || serializationArguments.previouslySerialized == null)
            {
                direction = Directions.Down;
            }
            else if (serializationArguments.previouslySerialized.Parent == Parent)
            {
                direction = Directions.New;
            }
            else if (serializationArguments.previouslySerialized.Parent.Parent == Parent)
            {
                direction = Directions.Up;
            }
            else
            {
                throw new PromptuariumException("Previously serialized node is invalid");
            }

            bool appending = false;

            // If the node has only children
            if (!Contains(Data) && !Contains(MetaData) && children.Count > 0)
            {
                stream.WriteByte(ControlByte(direction, DataType.Data, SizeType.Linear, 0));
            }

            #region Saving Data

            #region Fireing event

            if (OnDataSaving != null)
            {
                OnDataSaving(this, new PromptuariumSavingEventArgs());
            }

            #endregion

            if (Contains(Data))
            {
                appending = await SerializeContentAsync(stream, Data, DataType.Data, direction, appending);
            }

            #region Fireing event

            if (OnDataSaved != null)
            {
                OnDataSaved(this, new PromptuariumSavedEventArgs());
            }

            #endregion

            #endregion

            #region Saving MetaData

            #region Fireing event

            if (OnMetaDataSaving != null)
            {
                OnMetaDataSaving(this, new PromptuariumSavingEventArgs());
            }

            #endregion

            if (Contains(MetaData))
            {
                appending = await SerializeContentAsync(stream, MetaData, DataType.MetaData, direction, appending);
            }

            #region Fireing event

            if (OnMetaDataSaved != null)
            {
                OnMetaDataSaved(this, new PromptuariumSavedEventArgs());
            }

            #endregion

            #endregion

            serializationArguments.previouslySerialized = this;

            if (children.Count > 0)
            {
                Element child = new Element();

                for (int childIndex = 0; childIndex < children.Count; childIndex++)
                {
                    child = children[childIndex];

                    if (child != null)
                    {
                        await child.SerializeAsync(stream, serializationArguments);
                    }
                }

                if (child.children.Count > 0)
                {
                    serializationArguments.stepUpNodesRequired++;
                    serializationArguments.previouslySerialized = child;
                }
            }
        }

        private async Task<bool> SerializeContentAsync(Stream target, Stream source, DataType dataType, Directions direction, bool appending)
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
                await source.ReadAsync(buffer, 0, bufferSize);
                await target.WriteAsync(buffer, 0, bufferSize);
            }            
            
            return appending;
        }

        private byte ControlByte(Directions direction, DataType dataType, SizeType sizeType, byte sizeBits)
        {
            if (sizeBits <= NibbleMaxValue)
            {
                return (byte)((byte)direction | (byte)dataType | (byte)sizeType | sizeBits);
            }
            
            throw new PromptuariumException("Size bits greater than the max value (" + NibbleMaxValue.ToString(CultureInfo.InvariantCulture) + " bits)");
        }

        private IEnumerable<SizeDescriptor> GetSizes(long size)
        {
            List<SizeDescriptor> sizes = new List<SizeDescriptor>();
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

        private bool Contains(Stream stream)
        {
            return stream != null && stream.Length > 0;
        }

        #endregion

        #region Deserialization routines
        
        private static async Task<Element> DeserializeAsync(Stream stream)
        {
            Element root = new Element();
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
                        parent = parent.Parent;
                        node = AddNewNode(parent);
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
                await stream.ReadAsync(buffer, 0, chunkSize);

                if (IsDataChunk(controlByte))
                {
                    if (node != null && chunkSize > 0)
                    {
                        if (node.Data == null)
                        {
                            node.Data = new MemoryStream();
                        }

                        #region Fireing event

                        if (OnDataLoading != null)
                        {
                            OnDataLoading(node, new PromptuariumLoadingEventArgs());
                        }

                        #endregion

                        await node.Data.WriteAsync(buffer, 0, chunkSize);

                        #region Fireing event

                        if (OnDataLoaded != null)
                        {
                            OnDataLoaded(node, new PromptuariumLoadedEventArgs());
                        }

                        #endregion
                    }
                }
                else
                {
                    if (node != null && chunkSize > 0)
                    {
                        if (node.MetaData == null)
                        {
                            node.MetaData = new MemoryStream();
                        }

                        #region Fireing event

                        if (OnMetaDataLoading != null)
                        {
                            OnMetaDataLoading(node, new PromptuariumLoadingEventArgs());
                        }

                        #endregion

                        await node.MetaData.WriteAsync(buffer, 0, chunkSize);

                        #region Fireing event

                        if (OnMetaDataLoaded != null)
                        {
                            OnMetaDataLoaded(node, new PromptuariumLoadedEventArgs());
                        }

                        #endregion
                    }
                }
            }

            PurgeEmptyNodes(root);

            if (root.children.Count > 0)
            {
                return root.children[0];
            }
            else
            {
                return new Element();
            }
        }

        private static int GetChunkSize(byte controlByte)
        {
            int result;

            if (IsSizeLogarithmic(controlByte))
            {
                result = 1 << ((controlByte & NibbleMaxValue) + NibbleSizeInBits);
            }
            else
            {
                result = controlByte & NibbleMaxValue;
            }

            return result;
        }

        private static bool IsSizeLogarithmic(byte controlByte)
        {
            return (controlByte & (byte)SizeType.Mask) == (byte)SizeType.Logarithmic;
        }

        private static bool IsDataChunk(byte controlByte)
        {
            return (controlByte & (byte)DataType.Mask) == (byte) DataType.Data;
        }

        private static Element AddNewNode(Element parent)
        {
            Element node = null;

            if (parent != null)
            {
                node = new Element();
                parent.UnsafeAdd(node);
            }

            return node;
        }
        
        private static void PurgeEmptyNodes(Element node)
        {
            foreach (Element child in new List<Element>(node.Children))
            {
                PurgeEmptyNodes(child);
            }

            if (node.Data == null && node.MetaData == null && node.children.Count == 0 && node.Parent != null)
            {
                node.Parent.Remove(node);
            }
        }

        #endregion
    }
}
