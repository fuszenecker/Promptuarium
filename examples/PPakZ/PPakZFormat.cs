using Promptuarium;

namespace PPakZ;

/// <summary>The flags of the info nodes: one byte in the metadata.</summary>
internal enum Flag : byte
{
    /// <summary>The kind of the entry (<see cref="EntryKind"/>, one byte).</summary>
    Type = 1,

    /// <summary>The content of a file, as a gzip stream.</summary>
    Content = 2,

    /// <summary>The length of the content before compression (VarUInt).</summary>
    Size = 3,

    /// <summary>The CRC-32 of the content before compression (uint).</summary>
    Crc32 = 4,

    /// <summary>The target of a symbolic link (UTF-8).</summary>
    Target = 5,

    /// <summary>The name of the owner (UTF-8).</summary>
    Owner = 6,

    /// <summary>The name of the group (UTF-8).</summary>
    Group = 7,

    /// <summary>The Unix permissions (ushort, e.g. 0644).</summary>
    Mode = 8,

    /// <summary>The time of the last modification (DateTimeOffset).</summary>
    Modified = 9,

    /// <summary>The entries of a directory: the children of this info node.</summary>
    Entries = 10,
}

/// <summary>The kinds of the entries (the values of <see cref="Flag.Type"/>).</summary>
internal enum EntryKind : byte
{
    File = 1,
    Directory = 2,
    Symlink = 3,
}

/// <summary>
/// The layout of a ppakz file: the layout of ppak, but the flags and the kinds are bytes, and the contents are
/// gzip streams.
///
/// <code>
/// root                      metadata: the signature "PRMT:PPAKZ1"
/// ├── entry                 data: the name of the file, directory or symbolic link (UTF-8)
/// │   ├── info              metadata: a flag (one byte, <see cref="Flag"/>), data: its value
/// │   ├── ...
/// │   └── info Entries      for a directory: its children are the entries of the directory
/// └── ...
/// </code>
///
/// A reader skips the info nodes it does not know, so new flags can be added without a new version.
/// </summary>
internal static class PPakZFormat
{
    /// <summary>The signature of the root (BINARY_FORMAT.md, 10.3): "file" shows it as "Promptuarium container, PPAKZ1".</summary>
    public const string Signature = "PRMT:PPAKZ1";

    /// <summary>An info node: the flag in the metadata, the value in the data.</summary>
    public static Element Info(Flag flag, ReadOnlyMemory<byte> value) => new(value, Data.FromByte((byte)flag));

    /// <summary>The info node of an entry with the flag, or null if the entry has no such info.</summary>
    public static Element? Find(Element entry, Flag flag)
    {
        return entry.Children.FirstOrDefault(info => info.MetaData.Length == 1 && info.MetaData.Span[0] == (byte)flag);
    }

    /// <summary>The kind of an entry (a file, if it is not given).</summary>
    public static EntryKind KindOf(Element entry) => Find(entry, Flag.Type) is { } type ? (EntryKind)type.Data.AsByte() : EntryKind.File;

    /// <summary>The name of an entry.</summary>
    public static string Name(Element entry) => entry.Data.AsUtf8String();

    /// <summary>The entries of a directory: the root, or the children of the Entries info of a directory entry.</summary>
    public static IReadOnlyList<Element> EntriesOf(Element directory)
    {
        return directory.Parent is null ? directory.Children : Find(directory, Flag.Entries)?.Children ?? [];
    }
}
