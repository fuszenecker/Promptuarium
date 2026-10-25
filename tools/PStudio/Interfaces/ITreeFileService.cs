using System.Threading.Tasks;
using Promptuarium;

namespace PStudio.Interfaces;

/// <summary>
/// Reads and writes tree files.
/// </summary>
public interface ITreeFileService
{
    /// <summary>
    /// Loads a tree.
    /// </summary>
    /// <exception cref="PromptuariumException">The file is not a valid tree</exception>
    /// <exception cref="System.IO.IOException">The file can not be read</exception>
    Task<Element> LoadAsync(string path);

    /// <summary>
    /// Saves a tree; the file is replaced only after the whole tree is written.
    /// </summary>
    Task SaveAsync(Element root, string path);

    /// <summary>
    /// Reads a file as a content.
    /// </summary>
    Task<byte[]> ReadBytesAsync(string path);

    /// <summary>
    /// Writes a content to a file.
    /// </summary>
    Task WriteBytesAsync(string path, System.ReadOnlyMemory<byte> content);
}
