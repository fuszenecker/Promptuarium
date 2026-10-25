using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Promptuarium;

/// <summary>
/// Collects the chunks in a buffer, so that many small chunks are written to the target stream at once.
/// </summary>
internal sealed class ChunkWriter : IDisposable
{
    // Large enough for a control byte and the largest payload
    private const int BufferSize = (1 << 19) + 1;

#pragma warning disable CA2213 // The stream is owned by the caller
    private readonly Stream target;
#pragma warning restore CA2213
    private readonly byte[] buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
    private int filled;

    public ChunkWriter(Stream target)
    {
        this.target = target;
    }

    public async Task WriteControlByteAsync(byte controlByte, CancellationToken cancellationToken)
    {
        await EnsureSpaceAsync(1, cancellationToken).ConfigureAwait(false);
        buffer[filled++] = controlByte;
    }

    /// <summary>
    /// Writes a control byte and the next <paramref name="size"/> bytes of <paramref name="source"/>.
    /// </summary>
    public async Task WriteChunkAsync(byte controlByte, Stream source, int size, CancellationToken cancellationToken)
    {
        await EnsureSpaceAsync(size + 1, cancellationToken).ConfigureAwait(false);
        buffer[filled] = controlByte;
        await source.ReadExactlyAsync(buffer.AsMemory(filled + 1, size), cancellationToken).ConfigureAwait(false);
        filled += size + 1;
    }

    public async Task FlushAsync(CancellationToken cancellationToken)
    {
        if (filled > 0)
        {
            await target.WriteAsync(buffer.AsMemory(0, filled), cancellationToken).ConfigureAwait(false);
            filled = 0;
        }
    }

    public void Dispose()
    {
        ArrayPool<byte>.Shared.Return(buffer);
    }

    private Task EnsureSpaceAsync(int size, CancellationToken cancellationToken)
    {
        return filled + size > BufferSize ? FlushAsync(cancellationToken) : Task.CompletedTask;
    }
}

/// <summary>
/// Reads control bytes and payloads. It reads ahead on seekable streams (and seeks back at the end),
/// and never reads more than needed on non-seekable streams, so the stream is always positioned right after the tree.
/// </summary>
internal sealed class ChunkReader : IDisposable
{
    private const int BufferSize = 64 * 1024;

#pragma warning disable CA2213 // The stream is owned by the caller
    private readonly Stream source;
#pragma warning restore CA2213
    private readonly bool readAhead;
    private readonly long? maxBytes;
    private readonly byte[] buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
    private int position;
    private int count;
    private long totalBytes;

    public ChunkReader(Stream source, long? maxBytes)
    {
        this.source = source;
        this.maxBytes = maxBytes;
        readAhead = source.CanSeek;
    }

    /// <returns>The next byte, or -1 at the end of the stream</returns>
    public async ValueTask<int> ReadByteAsync(CancellationToken cancellationToken)
    {
        if (!await FillAsync(1, cancellationToken).ConfigureAwait(false))
        {
            return -1;
        }

        Count(1);
        return buffer[position++];
    }

    /// <summary>
    /// Copies the next <paramref name="size"/> bytes to <paramref name="target"/>.
    /// </summary>
    public async Task ReadPayloadAsync(Stream target, int size, CancellationToken cancellationToken)
    {
        while (size > 0)
        {
            if (!await FillAsync(size, cancellationToken).ConfigureAwait(false))
            {
                throw new PromptuariumException("Unexpected end of stream during deserialization.");
            }

            int length = Math.Min(size, count - position);
            Count(length);
            await target.WriteAsync(buffer.AsMemory(position, length), cancellationToken).ConfigureAwait(false);
            position += length;
            size -= length;
        }
    }

    /// <summary>
    /// Positions the stream right after the last byte consumed.
    /// </summary>
    public void Finish()
    {
        if (readAhead && count > position)
        {
            source.Seek(position - count, SeekOrigin.Current);
            count = position;
        }
    }

    public void Dispose()
    {
        ArrayPool<byte>.Shared.Return(buffer);
    }

    /// <returns>False at the end of the stream</returns>
    private async ValueTask<bool> FillAsync(int wanted, CancellationToken cancellationToken)
    {
        if (position < count)
        {
            return true;
        }

        int length = readAhead ? BufferSize : Math.Min(wanted, BufferSize);
        count = await source.ReadAsync(buffer.AsMemory(0, length), cancellationToken).ConfigureAwait(false);
        position = 0;
        return count > 0;
    }

    private void Count(int length)
    {
        totalBytes += length;

        if (totalBytes > maxBytes)
        {
            throw new PromptuariumException($"The stream is longer than the limit ({maxBytes} bytes).");
        }
    }
}
