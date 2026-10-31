using System.IO.Compression;
using PPak;
using Promptuarium;
using static PPakZ.PPakZFormat;

namespace PPakZ;

/// <summary>
/// Unpacks a ppakz tree into a directory. The file may come from anywhere, so the names are checked (no file is
/// written outside the target directory), a content can not be longer than its size (no "gzip bomb"), and the CRCs
/// are checked.
/// </summary>
internal static class Unpacker
{
    /// <summary>Checks the signature of a loaded tree.</summary>
    /// <exception cref="InvalidDataException">It is not a ppakz tree</exception>
    public static void CheckSignature(Element root)
    {
        if (!root.Data.IsEmpty || root.MetaData.AsAsciiString() != Signature)
        {
            throw new InvalidDataException($"Not a ppakz file: the signature is not \"{Signature}\".");
        }
    }

    /// <summary>Unpacks the entries of the root into the directory.</summary>
    /// <returns>Whether the owners could be set (only root can set them)</returns>
    public static bool Unpack(Element root, string directory)
    {
        CheckSignature(root);
        System.IO.Directory.CreateDirectory(directory);
        return Extract(root, directory);
    }

    /// <summary>The files whose content can not be decompressed, or does not match its size or CRC, with their paths.</summary>
    public static IEnumerable<string> Corrupted(Element directory, string path = "")
    {
        foreach (Element entry in EntriesOf(directory))
        {
            string entryPath = path + "/" + Name(entry);

            if (KindOf(entry) == EntryKind.File && !IsIntact(entry))
            {
                yield return entryPath;
            }

            foreach (string corrupted in Corrupted(entry, entryPath))
            {
                yield return corrupted;
            }
        }
    }

    private static bool IsIntact(Element entry)
    {
        try
        {
            Decompress(entry, Stream.Null);
            return true;
        }
        catch (InvalidDataException)
        {
            return false;
        }
    }

    private static bool Extract(Element directory, string path)
    {
        bool owners = true;
        var names = new HashSet<string>(StringComparer.Ordinal);

        foreach (Element entry in EntriesOf(directory))
        {
            string name = Name(entry);

            if (name is "" or "." or ".." || name.Contains('/') || name.Contains('\\') || name.Contains('\0') || !names.Add(name))
            {
                throw new InvalidDataException($"Invalid or repeated name in {path}: \"{name}\".");
            }

            string target = Path.Combine(path, name);
            EntryKind kind = KindOf(entry);

            switch (kind)
            {
                case EntryKind.Directory:
                    System.IO.Directory.CreateDirectory(target);
                    owners &= Extract(entry, target);
                    break;

                case EntryKind.Symlink:
                    File.CreateSymbolicLink(target, Find(entry, Flag.Target)?.Data.AsUtf8String() ?? throw new InvalidDataException($"{target}: a link without a target."));
                    break;

                case EntryKind.File:
                    try
                    {
                        using FileStream file = File.Create(target);
                        Decompress(entry, file);
                    }
                    catch (InvalidDataException ex)
                    {
                        File.Delete(target);
                        throw new InvalidDataException($"{target}: {ex.Message}", ex);
                    }

                    break;

                default:
                    throw new InvalidDataException($"{target}: unknown type {(byte)kind}.");
            }

            owners &= SetAttributes(entry, target, kind);
        }

        return owners;
    }

    // The content is decompressed as a stream, and it is checked while it is written
    private static void Decompress(Element entry, Stream output)
    {
        if (Find(entry, Flag.Content) is not { } content)
        {
            return;
        }

        ulong size = Find(entry, Flag.Size)?.Data.AsVarUInt() ?? throw new InvalidDataException("a content without a size.");
        var crc = new System.IO.Hashing.Crc32();
        byte[] buffer = new byte[81920];
        ulong written = 0;

        using var gzip = new GZipStream(new MemoryStream(content.Data.ToArray(), writable: false), CompressionMode.Decompress);
        int read;

        while ((read = gzip.Read(buffer)) > 0)
        {
            written += (ulong)read;

            if (written > size)
            {
                throw new InvalidDataException("the content is longer than its size.");
            }

            crc.Append(buffer.AsSpan(0, read));
            output.Write(buffer, 0, read);
        }

        if (written != size)
        {
            throw new InvalidDataException("the content is shorter than its size.");
        }

        if (Find(entry, Flag.Crc32) is { } expected && crc.GetCurrentHashAsUInt32() != expected.Data.AsUInt())
        {
            throw new InvalidDataException("the content does not match its CRC.");
        }
    }

    // The permissions and the time are set after the content (writing the children of a directory changes its time)
    private static bool SetAttributes(Element entry, string target, EntryKind kind)
    {
        if (kind != EntryKind.Symlink)
        {
            if (!OperatingSystem.IsWindows() && Find(entry, Flag.Mode) is { } mode)
            {
                File.SetUnixFileMode(target, (UnixFileMode)mode.Data.AsUShort());
            }

            if (Find(entry, Flag.Modified) is { } modified)
            {
                DateTime time = modified.Data.AsDateTimeOffset().UtcDateTime;

                if (kind == EntryKind.Directory)
                {
                    System.IO.Directory.SetLastWriteTimeUtc(target, time);
                }
                else
                {
                    File.SetLastWriteTimeUtc(target, time);
                }
            }
        }

        string? owner = Find(entry, Flag.Owner)?.Data.AsUtf8String();
        string? group = Find(entry, Flag.Group)?.Data.AsUtf8String();
        return owner is null && group is null || UnixOwner.Set(target, owner, group) is null;
    }
}
