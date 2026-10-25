using Promptuarium;
using static PPak.PPakFormat;

namespace PPak;

/// <summary>
/// Unpacks a ppak tree into a directory. The file may come from anywhere, so the names are checked (no file is
/// written outside the target directory), and so are the CRCs.
/// </summary>
internal static class Unpacker
{
    /// <summary>Checks the signature of a loaded tree.</summary>
    /// <exception cref="InvalidDataException">It is not a ppak tree</exception>
    public static void CheckSignature(Element root)
    {
        if (!root.Data.IsEmpty || root.MetaData.AsAsciiString() != Signature)
        {
            throw new InvalidDataException($"Not a ppak file: the signature is not \"{Signature}\".");
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

    /// <summary>The entries whose content does not match their CRC, with their paths.</summary>
    public static IEnumerable<string> Corrupted(Element directory, string path = "")
    {
        foreach (Element entry in EntriesOf(directory))
        {
            string entryPath = path + "/" + Name(entry);

            if (Find(entry, ContentFlag) is { } content && Find(entry, Crc32Flag) is { } crc
                && System.IO.Hashing.Crc32.HashToUInt32(content.Data.Span) != crc.Data.AsUInt())
            {
                yield return entryPath;
            }

            foreach (string corrupted in Corrupted(entry, entryPath))
            {
                yield return corrupted;
            }
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
            string type = Find(entry, TypeFlag)?.Data.AsUtf8String() ?? FileKind;

            switch (type)
            {
                case DirectoryKind:
                    System.IO.Directory.CreateDirectory(target);
                    owners &= Extract(entry, target);
                    break;

                case SymlinkKind:
                    File.CreateSymbolicLink(target, Find(entry, TargetFlag)?.Data.AsUtf8String() ?? throw new InvalidDataException($"{target}: a link without a target."));
                    break;

                case FileKind:
                    ReadOnlyMemory<byte> content = Find(entry, ContentFlag)?.Data ?? ReadOnlyMemory<byte>.Empty;

                    if (Find(entry, Crc32Flag) is { } crc && System.IO.Hashing.Crc32.HashToUInt32(content.Span) != crc.Data.AsUInt())
                    {
                        throw new InvalidDataException($"{target}: the content does not match its CRC.");
                    }

                    File.WriteAllBytes(target, content.ToArray());
                    break;

                default:
                    throw new InvalidDataException($"{target}: unknown type \"{type}\".");
            }

            owners &= SetAttributes(entry, target, type);
        }

        return owners;
    }

    // The permissions and the time are set after the content (writing the children of a directory changes its time)
    private static bool SetAttributes(Element entry, string target, string type)
    {
        if (type != SymlinkKind)
        {
            if (!OperatingSystem.IsWindows() && Find(entry, ModeFlag) is { } mode)
            {
                File.SetUnixFileMode(target, (UnixFileMode)mode.Data.AsUShort());
            }

            if (Find(entry, ModifiedFlag) is { } modified)
            {
                DateTime time = modified.Data.AsDateTimeOffset().UtcDateTime;

                if (type == DirectoryKind)
                {
                    System.IO.Directory.SetLastWriteTimeUtc(target, time);
                }
                else
                {
                    File.SetLastWriteTimeUtc(target, time);
                }
            }
        }

        string? owner = Find(entry, OwnerFlag)?.Data.AsUtf8String();
        string? group = Find(entry, GroupFlag)?.Data.AsUtf8String();
        return owner is null && group is null || UnixOwner.Set(target, owner, group);
    }
}
