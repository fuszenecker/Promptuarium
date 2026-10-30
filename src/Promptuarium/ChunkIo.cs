using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Promptuarium;

/// <summary>
/// Writes chunks to a stream: small chunks are collected in a buffer and written at once,
/// large payloads are written directly from the content, without copying.
/// </summary>
internal sealed class ChunkWriter : IDisposable
{
    private const int BufferSize = 64 * 1024;

#pragma warning disable CA2213 // The stream is owned by the caller
    private readonly Stream target;
#pragma warning restore CA2213
    private readonly byte[] buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
    private int filled;

    public ChunkWriter(Stream target)
    {
        this.target = target;
    }

    public async ValueTask WriteAsync(byte control, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
    {
        if (filled + 1 + payload.Length <= BufferSize)
        {
            buffer[filled++] = control;
            payload.Span.CopyTo(buffer.AsSpan(filled));
            filled += payload.Length;
            return;
        }

        if (filled == BufferSize)
        {
            await FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        // The control byte goes with the buffered chunks, the large payload is written directly
        buffer[filled++] = control;
        await FlushAsync(cancellationToken).ConfigureAwait(false);
        await target.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask FlushAsync(CancellationToken cancellationToken)
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
}
