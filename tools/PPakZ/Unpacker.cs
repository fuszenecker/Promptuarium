using PPak;
using Promptuarium;
using static PPakZ.PPakZFormat;

namespace PPakZ;

/// <summary>
/// Unpacks a ppakz tree into a directory. The tree is checked by <see cref="Checker"/> first, so the names are valid
/// (no file is written outside the target directory), every entry has a kind, and every content can be decompressed
/// within the limit. The additional infos are applied when they are valid; what can not be applied is a warning.
/// </summary>
internal static class Unpacker
{
    /// <summary>Unpacks the entries of a checked tree into the directory.</summary>
    /// <param name="warn">Gets the warnings of the unpacking</param>
    public static void Unpack(Element root, string directory, Action<string> warn)
    {
        System.IO.Directory.CreateDirectory(directory);
        int owners = Extract(root, directory, warn);

        if (owners > 0)
        {
            warn($"The owners and groups of {owners:N0} entries were not restored (only root can set them).");
        }
    }

    // The number of entries whose owners could not be set, because the process is not root
    private static int Extract(Element directory, string path, Action<string> warn)
    {
        int owners = 0;

        foreach (Element entry in EntriesOf(directory))
        {
            string target = Path.Combine(path, Name(entry)!);
            EntryKind kind = Kind(entry)!.Value;

            switch (kind)
            {
                case EntryKind.Directory:
                    System.IO.Directory.CreateDirectory(target);
                    owners += Extract(entry, target, warn);
                    break;

                case EntryKind.Symlink:
                    try
                    {
                        File.CreateSymbolicLink(target, Text(entry, Flag.Target)!);
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        warn($"{target}: the symbolic link was not created: {ex.Message}");
                        continue;
                    }

                    break;

                default:
                    // The checker has decompressed it already, so it fits in the limit
                    using (FileStream file = File.Create(target))
                    {
                        Checker.Decompress(Find(entry, Flag.Content)!.Data, file, ulong.MaxValue);
                    }

                    break;
            }

            SetAttributes(entry, target, kind);

            string? owner = Text(entry, Flag.Owner);
            string? group = Text(entry, Flag.Group);

            if (owner is not null || group is not null)
            {
                if (!UnixOwner.CanSet)
                {
                    owners++;
                }
                else if (UnixOwner.Set(target, owner, group) is { } problem)
                {
                    warn($"{target}: {problem}");
                }
            }
        }

        return owners;
    }

    // The permissions and the time are set after the content (writing the children of a directory changes its time).
    // A symbolic link has neither: its permissions mean nothing, and setting them would follow the link.
    private static void SetAttributes(Element entry, string target, EntryKind kind)
    {
        if (kind == EntryKind.Symlink)
        {
            return;
        }

        if (!OperatingSystem.IsWindows() && Mode(entry) is { } mode)
        {
            File.SetUnixFileMode(target, mode);
        }

        if (Modified(entry) is { } modified)
        {
            if (kind == EntryKind.Directory)
            {
                System.IO.Directory.SetLastWriteTimeUtc(target, modified.UtcDateTime);
            }
            else
            {
                File.SetLastWriteTimeUtc(target, modified.UtcDateTime);
            }
        }
    }
}
