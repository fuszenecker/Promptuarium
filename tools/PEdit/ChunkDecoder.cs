using System;
using System.Collections.Generic;
using System.IO;

namespace PEdit;

/// <summary>
/// A chunk of the binary format, as it is in the stream.
/// </summary>
/// <param name="Offset">Position of the control byte in the stream</param>
/// <param name="Control">The control byte</param>
/// <param name="Direction">Append, Down, Up or New</param>
/// <param name="ContentType">data or metadata</param>
/// <param name="SizeType">linear or logarithmic</param>
/// <param name="Length">Length of the payload</param>
/// <param name="Kind">What the chunk does: content, placeholder or Up marker (auxiliary), empty node, terminator</param>
/// <param name="Depth">Depth of the node the chunk belongs to (the root is 0)</param>
/// <param name="Payload">The payload</param>
internal sealed record Chunk(long Offset, byte Control, string Direction, string ContentType, string SizeType, int Length, string Kind, int Depth, ReadOnlyMemory<byte> Payload);

/// <summary>
/// Decodes the chunks of a stream directly from the specification (BINARY_FORMAT.md), independently of the library,
/// so that the chunks written by different implementations can be compared.
/// </summary>
internal static class ChunkDecoder
{
    /// <summary>
    /// Reads the chunks up to and including the terminator (or the end of the stream).
    /// </summary>
    /// <returns>The chunks, the number of bytes consumed, and an error message (null if the stream is valid)</returns>
    public static (List<Chunk> Chunks, long Consumed, string? Error) Decode(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var chunks = new List<Chunk>();
        long offset = 0;

        // Depth of the parent cursor and of the node cursor; the virtual root is at depth -1
        int parentDepth = -1;
        int nodeDepth = -1;

        while (true)
        {
            int next = stream.ReadByte();

            if (next < 0)
            {
                return (chunks, offset, null);
            }

            byte control = (byte)next;
            int direction = control & 0xC0;
            bool metaData = (control & 0x20) != 0;
            bool logarithmic = (control & 0x10) != 0;
            int nibble = control & 0x0F;
            int length = logarithmic ? 1 << (nibble + 4) : nibble;

            switch (direction)
            {
                case 0xC0:
                    nodeDepth = parentDepth + 1;
                    break;
                case 0x40:
                    parentDepth = nodeDepth;
                    nodeDepth = parentDepth + 1;
                    break;
                case 0x80:
                    if (parentDepth < 0)
                    {
                        return (chunks, offset + 1, $"Navigating above the root at offset {offset}.");
                    }

                    parentDepth--;
                    nodeDepth = parentDepth + 1;
                    break;
            }

            string kind = (direction, length, metaData) switch
            {
                (0x00, 0, _) => "terminator",
                (0x00, _, _) => "append",
                (_, 0, false) => "auxiliary",
                (_, 0, true) => "empty node",
                _ => "node",
            };

            byte[] payload = new byte[length];

            if (stream.ReadAtLeast(payload, length, throwOnEndOfStream: false) < length)
            {
                return (chunks, offset + 1, $"Unexpected end of stream in the payload of the chunk at offset {offset}.");
            }

            chunks.Add(new Chunk(
                offset,
                control,
                direction switch { 0x00 => "Append", 0x40 => "Down", 0x80 => "Up", _ => "New" },
                metaData ? "metadata" : "data",
                logarithmic ? "logarithmic" : "linear",
                length,
                kind,
                nodeDepth,
                payload));

            offset += 1 + length;

            if (kind == "terminator")
            {
                return (chunks, offset, null);
            }
        }
    }
}
