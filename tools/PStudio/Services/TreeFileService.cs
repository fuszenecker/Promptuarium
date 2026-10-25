using System;
using System.IO;
using System.Threading.Tasks;
using PStudio.Interfaces;
using Promptuarium;

namespace PStudio.Services;

/// <inheritdoc/>
public sealed class TreeFileService : ITreeFileService
{
    /// <inheritdoc/>
    public async Task<Element> LoadAsync(string path)
    {
        return Element.FromBytes(await File.ReadAllBytesAsync(path).ConfigureAwait(false));
    }

    /// <inheritdoc/>
    public async Task SaveAsync(Element root, string path)
    {
        ArgumentNullException.ThrowIfNull(root);

        string temporary = path + ".tmp";
        await File.WriteAllBytesAsync(temporary, root.ToBytes()).ConfigureAwait(false);
        File.Move(temporary, path, overwrite: true);
    }

    /// <inheritdoc/>
    public Task<byte[]> ReadBytesAsync(string path) => File.ReadAllBytesAsync(path);

    /// <inheritdoc/>
    public async Task WriteBytesAsync(string path, ReadOnlyMemory<byte> content)
    {
        await File.WriteAllBytesAsync(path, content).ConfigureAwait(false);
    }
}
