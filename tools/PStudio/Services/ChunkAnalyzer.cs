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
/// Lists the chunks with the decoder of pedit, which reads the bytes directly from the specification.
/// </summary>
public sealed class ChunkAnalyzer : IChunkAnalyzer
{
    /// <inheritdoc/>
    public IReadOnlyList<ChunkRow> Analyze(Element root)
    {
        ArgumentNullException.ThrowIfNull(root);

        using var stream = new MemoryStream(root.ToBytes(), writable: false);
        (List<Chunk> chunks, _, _) = ChunkDecoder.Decode(stream);

        return chunks.Select(chunk => new ChunkRow(
            chunk.Offset.ToString("X6", CultureInfo.InvariantCulture),
            chunk.Control.ToString("X2", CultureInfo.InvariantCulture),
            chunk.Direction,
            chunk.ContentType,
            chunk.SizeType == "logarithmic" ? $"{chunk.Length} (log)" : chunk.Length.ToString(CultureInfo.InvariantCulture),
            chunk.Kind,
            chunk.Depth,
            ContentFormat.Preview(chunk.Payload.Span, 32))).ToList();
    }
}
