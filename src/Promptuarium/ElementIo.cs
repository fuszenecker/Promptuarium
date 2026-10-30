using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Promptuarium;

public partial class Element
{
    // Buffer size of the file streams
    private const int BufferSize = 64 * 1024;

    #region Byte array functions

    /// <summary>
    /// Encodes the tree into its canonical byte sequence (BINARY_FORMAT.md, section 6).
    /// </summary>
    /// <returns>A new array, exactly as long as the encoding</returns>
    /// <example>
    /// <code>
    /// byte[] bytes = tree.ToBytes();
    /// </code>
    /// </example>
    public byte[] ToBytes()
    {
        long length = 0;

        foreach ((_, ReadOnlyMemory<byte> payload) in EncodeChunks())
        {
            length += 1 + payload.Length;
        }

        if (length > Array.MaxLength)
        {
            throw new PromptuariumException($"The tree is too large for a byte array ({length.ToString(System.Globalization.CultureInfo.InvariantCulture)} bytes), save it into a stream instead.");
        }

        byte[] result = new byte[length];
        int offset = 0;

        foreach ((byte control, ReadOnlyMemory<byte> payload) in EncodeChunks())
        {
            result[offset++] = control;
            payload.Span.CopyTo(result.AsSpan(offset));
            offset += payload.Length;
        }

        return result;
    }

    /// <summary>
    /// Decodes a tree from bytes. Further data (e.g. another tree) may follow the tree.
    /// </summary>
    /// <param name="bytes">The bytes</param>
    /// <param name="options">The limits, or null</param>
    /// <returns>The tree</returns>
    /// <exception cref="PromptuariumException">Thrown if the bytes are not a valid Promptuarium stream, or if they exceed a limit</exception>
    /// <example>
    /// <code>
    /// var tree = Element.FromBytes(bytes);
    /// </code>
    /// </example>
    public static Element FromBytes(ReadOnlyMemory<byte> bytes, LoadOptions? options = null)
    {
        return FromBytes(bytes, options, out _);
    }

    /// <summary>
    /// Decodes a tree from bytes, and tells where the tree ends (further trees may follow from there).
    /// </summary>
    /// <param name="bytes">The bytes</param>
    /// <param name="options">The limits, or null</param>
    /// <param name="consumed">The number of bytes of the tree, up to and including the terminator</param>
    /// <returns>The tree</returns>
    /// <exception cref="PromptuariumException">Thrown if the bytes are not a valid Promptuarium stream, or if they exceed a limit</exception>
    /// <example>
    /// <code>
    /// var first = Element.FromBytes(bytes, null, out int consumed);
    /// var second = Element.FromBytes(bytes[consumed..]);
    /// </code>
    /// </example>
    public static Element FromBytes(ReadOnlyMemory<byte> bytes, LoadOptions? options, out int consumed)
    {
        var decoder = new Decoder(options);
        consumed = decoder.Push(bytes.Span);
        return decoder.Finish();
    }

    #endregion

    #region Load functions

    /// <summary>
    /// Loads a tree from a stream.
    /// </summary>
    /// <param name="stream">The source stream</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The tree, if no error occurred</returns>
    /// <exception cref="PromptuariumException">Thrown if the stream is not a valid Promptuarium stream</exception>
    /// <example>
    /// <code>
    /// using var stream = new FileStream("test.prm", FileMode.Open);
    /// var tree = await Element.LoadAsync(stream, cancellationToken);
    /// </code>
    /// </example>
    public static Task<Element> LoadAsync(Stream stream, CancellationToken cancellationToken)
    {
        return LoadAsync(stream, null, cancellationToken);
    }

    /// <summary>
    /// Loads a tree from a stream, with limits.
    /// </summary>
    /// <param name="stream">The source stream</param>
    /// <param name="options">The limits, or null</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The tree, if no error occurred</returns>
    /// <exception cref="PromptuariumException">Thrown if the stream is not a valid Promptuarium stream, or if it exceeds a limit</exception>
    /// <example>
    /// <code>
    /// var options = new LoadOptions { MaxBytes = 10 * 1024 * 1024, MaxNodes = 100_000 };
    /// var tree = await Element.LoadAsync(networkStream, options, cancellationToken);
    /// </code>
    /// </example>
    public static Task<Element> LoadAsync(Stream stream, LoadOptions? options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return DeserializeAsync(stream, options, cancellationToken);
    }

    /// <summary>
    /// Loads a tree from a stream.
    /// </summary>
    /// <param name="stream">The source stream</param>
    /// <returns>The tree, if no error occurred</returns>
    /// <exception cref="PromptuariumException">Thrown if the stream is not a valid Promptuarium stream</exception>
    /// <example>
    /// <code>
    /// using var stream = new FileStream("test.prm", FileMode.Open);
    /// var tree = await Element.LoadAsync(stream);
    /// </code>
    /// </example>
    public static Task<Element> LoadAsync(Stream stream)
    {
        return LoadAsync(stream, CancellationToken.None);
    }

    /// <summary>
    /// Loads a tree from a file.
    /// </summary>
    /// <param name="fileName">The source file name</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The tree, if no error occurred</returns>
    /// <exception cref="PromptuariumException">Thrown if the file is not a valid Promptuarium file</exception>
    /// <example>
    /// <code>
    /// var tree = await Element.LoadAsync("test.prm", cancellationToken);
    /// </code>
    /// </example>
    public static Task<Element> LoadAsync(string fileName, CancellationToken cancellationToken)
    {
        return LoadAsync(fileName, null, cancellationToken);
    }

    /// <summary>
    /// Loads a tree from a file, with limits.
    /// </summary>
    /// <param name="fileName">The source file name</param>
    /// <param name="options">The limits, or null</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The tree, if no error occurred</returns>
    /// <exception cref="PromptuariumException">Thrown if the file is not a valid Promptuarium file, or if it exceeds a limit</exception>
    /// <example>
    /// <code>
    /// var tree = await Element.LoadAsync("test.prm", new LoadOptions { MaxNodes = 1000 }, cancellationToken);
    /// </code>
    /// </example>
    public static async Task<Element> LoadAsync(string fileName, LoadOptions? options, CancellationToken cancellationToken)
    {
        var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true);

        await using (fileStream.ConfigureAwait(false))
        {
            return await LoadAsync(fileStream, options, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Loads a tree from a file.
    /// </summary>
    /// <param name="fileName">The source file name</param>
    /// <returns>The tree, if no error occurred</returns>
    /// <exception cref="PromptuariumException">Thrown if the file is not a valid Promptuarium file</exception>
    /// <example>
    /// <code>
    /// var tree = await Element.LoadAsync("test.prm");
    /// </code>
    /// </example>
    public static Task<Element> LoadAsync(string fileName)
    {
        return LoadAsync(fileName, CancellationToken.None);
    }

    #endregion

    #region Save functions

    /// <summary>
    /// Saves the tree into a stream.
    /// </summary>
    /// <param name="stream">The target stream</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <example>
    /// <code>
    /// using var stream = new FileStream("test.prm", FileMode.Create);
    /// await tree.SaveAsync(stream, cancellationToken);
    /// </code>
    /// </example>
    public Task SaveAsync(Stream stream, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return SerializeAsync(stream, cancellationToken);
    }

    /// <summary>
    /// Saves the tree into a stream.
    /// </summary>
    /// <param name="stream">The target stream</param>
    /// <example>
    /// <code>
    /// using var stream = new FileStream("test.prm", FileMode.Create);
    /// await tree.SaveAsync(stream);
    /// </code>
    /// </example>
    public Task SaveAsync(Stream stream)
    {
        return SaveAsync(stream, CancellationToken.None);
    }

    /// <summary>
    /// Saves the tree into a file.
    /// </summary>
    /// <param name="fileName">The target file name</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <example>
    /// <code>
    /// await tree.SaveAsync("test.prm", cancellationToken);
    /// </code>
    /// </example>
    public async Task SaveAsync(string fileName, CancellationToken cancellationToken)
    {
        var stream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, useAsync: true);

        await using (stream.ConfigureAwait(false))
        {
            await SaveAsync(stream, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Saves the tree into a file.
    /// </summary>
    /// <param name="fileName">The target file name</param>
    /// <example>
    /// <code>
    /// await tree.SaveAsync("test.prm");
    /// </code>
    /// </example>
    public Task SaveAsync(string fileName)
    {
        return SaveAsync(fileName, CancellationToken.None);
    }

    #endregion
}
