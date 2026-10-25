using Promptuarium;

namespace PPak;

/// <summary>
/// The layout of a ppak file. Every level of the directory tree is two levels of the Promptuarium tree:
///
/// <code>
/// root                      metadata: the signature "PRMT:PPAK1"
/// ├── entry                 data: the name of the file, directory or symbolic link
/// │   ├── info              metadata: a flag ("type", "content", ...), data: its value
/// │   ├── ...
/// │   └── info "entries"    for a directory: its children are the entries of the directory
/// └── ...
/// </code>
///
/// A reader skips the info nodes it does not know, so new flags can be added without a new version.
/// </summary>
internal static class PPakFormat
{
    /// <summary>The signature of the root (BINARY_FORMAT.md, 10.3): "file" shows it as "Promptuarium container, PPAK1".</summary>
    public const string Signature = "PRMT:PPAK1";

    /// <summary>The kind of the entry: "file", "directory" or "symlink" (UTF-8).</summary>
    public const string TypeFlag = "type";

    /// <summary>The content of a file.</summary>
    public const string ContentFlag = "content";

    /// <summary>The CRC-32 of the content (uint).</summary>
    public const string Crc32Flag = "crc32";

    /// <summary>The target of a symbolic link (UTF-8).</summary>
    public const string TargetFlag = "target";

    /// <summary>The name of the owner (UTF-8).</summary>
    public const string OwnerFlag = "owner";

    /// <summary>The name of the group (UTF-8).</summary>
    public const string GroupFlag = "group";

    /// <summary>The Unix permissions (ushort, e.g. 0644).</summary>
    public const string ModeFlag = "mode";

    /// <summary>The time of the last modification (DateTimeOffset).</summary>
    public const string ModifiedFlag = "modified";

    /// <summary>The entries of a directory: the children of this info node.</summary>
    public const string EntriesFlag = "entries";

    /// <summary>The kinds of the entries (the values of "type").</summary>
    public const string FileKind = "file", DirectoryKind = "directory", SymlinkKind = "symlink";

    /// <summary>An info node: the flag in the metadata, the value in the data.</summary>
    public static Element Info(string flag, ReadOnlyMemory<byte> value) => new(value, Data.FromAsciiString(flag));

    /// <summary>The value of an info node of an entry, or null if the entry has no such info.</summary>
    public static Element? Find(Element entry, string flag)
    {
        return entry.Children.FirstOrDefault(info => info.MetaData.AsAsciiString() == flag);
    }

    /// <summary>The name of an entry.</summary>
    public static string Name(Element entry) => entry.Data.AsUtf8String();

    /// <summary>The entries of a directory: the root, or the children of the "entries" info of a directory entry.</summary>
    public static IReadOnlyList<Element> EntriesOf(Element directory)
    {
        return directory.Parent is null ? directory.Children : Find(directory, EntriesFlag)?.Children ?? [];
    }
}
