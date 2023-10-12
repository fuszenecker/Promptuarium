using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Promptuarium;

public partial class Element
{
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
    public Task<Element> LoadAsync(Stream stream, CancellationToken cancellationToken)
    {
        return DeserializeAsync(stream, cancellationToken);
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
    public Task<Element> LoadAsync(Stream stream)
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
    public async Task<Element> LoadAsync(string fileName, CancellationToken cancellationToken)
    {
        using var fileStream = new FileStream(fileName, FileMode.Open);
        return await LoadAsync(fileStream, cancellationToken).ConfigureAwait(false);
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
    public Task<Element> LoadAsync(string fileName)
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
    public async Task SaveAsync(Stream stream, CancellationToken cancellationToken)
    {
        await SerializeAsync(stream, new SerializationArguments(), cancellationToken).ConfigureAwait(false);
        stream.WriteByte(ControlByte(Directions.Append, DataType.Data, SizeType.Linear, 0));
    }

    /// <summary>
    /// Saves the tree into a stream.
    /// </summary>
    /// <param name="stream">The target stream</param>
    /// <returns>True, if no error occurred</returns>
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
        using Stream stream = new FileStream(fileName, FileMode.Create);
        await SaveAsync(stream, cancellationToken).ConfigureAwait(false);
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
