using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Promptuarium;

public partial class Element
{
    // Buffer size of the file streams
    private const int BufferSize = 64 * 1024;

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
    /// Loads a tree from a stream, with limits and events.
    /// </summary>
    /// <param name="stream">The source stream</param>
    /// <param name="options">The limits and events, or null</param>
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
    /// Loads a tree from a file, with limits and events.
    /// </summary>
    /// <param name="fileName">The source file name</param>
    /// <param name="options">The limits and events, or null</param>
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
