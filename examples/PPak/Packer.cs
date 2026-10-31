using System.IO.Hashing;
using Promptuarium;
using static PPak.PPakFormat;

namespace PPak;

/// <summary>
/// Packs a directory tree into a ppak tree.
/// </summary>
internal static class Packer
{
    /// <summary>The ppak tree of the contents of a directory (the directory itself is not an entry).</summary>
    public static Element Pack(string directory)
    {
        Element root = new ElementBuilder().WithSignature(Data.FromAsciiString(Signature)).Build();
        root.Add(Entries(new DirectoryInfo(directory)));
        return root;
    }

    // The entries in the order of their names, so that the same directory always gives the same file
    private static IEnumerable<Element> Entries(DirectoryInfo directory)
    {
        return directory.EnumerateFileSystemInfos()
            .OrderBy(info => info.Name, StringComparer.Ordinal)
            .Select(Entry)
            .ToList();
    }

    private static Element Entry(FileSystemInfo info)
    {
        var entry = new Element(Data.FromUtf8String(info.Name), null);

        if (info.LinkTarget is string target)
        {
            entry.Add(Info(TypeFlag, Data.FromUtf8String(SymlinkKind)), Info(TargetFlag, Data.FromUtf8String(target)));
        }
        else if (info is DirectoryInfo directory)
        {
            entry.Add(Info(TypeFlag, Data.FromUtf8String(DirectoryKind)));
        }
        else
        {
            byte[] content = File.ReadAllBytes(info.FullName);
            entry.Add(
                Info(TypeFlag, Data.FromUtf8String(FileKind)),
                Info(ContentFlag, content),
                Info(SizeFlag, Data.FromULong((ulong)content.LongLength)),
                Info(Crc32Flag, Data.FromUInt(System.IO.Hashing.Crc32.HashToUInt32(content))));
        }

        (string? owner, string? group) = UnixOwner.Get(info.FullName);

        if (owner is not null)
        {
            entry.Add(Info(OwnerFlag, Data.FromUtf8String(owner)));
        }

        if (group is not null)
        {
            entry.Add(Info(GroupFlag, Data.FromUtf8String(group)));
        }

        // The permissions of a symbolic link mean nothing (and reading them would follow the link)
        if (!OperatingSystem.IsWindows() && info.LinkTarget is null)
        {
            entry.Add(Info(ModeFlag, Data.FromUShort((ushort)info.UnixFileMode)));
        }

        entry.Add(Info(ModifiedFlag, Data.FromDateTimeOffset(new DateTimeOffset(info.LastWriteTimeUtc))));

        if (info is DirectoryInfo { LinkTarget: null } subdirectory)
        {
            entry.Add(new Element(null, Data.FromAsciiString(EntriesFlag), Entries(subdirectory)));
        }

        return entry;
    }
}
