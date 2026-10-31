using System.Text;
using Promptuarium;

namespace PPakZ;

/// <summary>
/// The flags of the info nodes: a ushort (two bytes, little-endian) in the metadata. They start at 0x100, so the
/// metadata of an info node never looks like a one-byte tag of the serializer.
/// </summary>
internal enum Flag : ushort
{
    /// <summary>The kind of the entry (<see cref="EntryKind"/>, ushort).</summary>
    Type = 0x100,

    /// <summary>Mandatory for a file: its content, compressed by the algorithm of <see cref="Compression"/>.</summary>
    Content = 0x101,

    /// <summary>The length of the content before compression (VarUInt).</summary>
    Size = 0x102,

    /// <summary>The CRC-32 of the content before compression (uint).</summary>
    Crc32 = 0x103,

    /// <summary>The target of a symbolic link (UTF-8).</summary>
    Target = 0x104,

    /// <summary>The name of the owner (UTF-8).</summary>
    Owner = 0x105,

    /// <summary>The name of the group (UTF-8).</summary>
    Group = 0x106,

    /// <summary>The Unix permissions (ushort, e.g. 0644; at most 07777).</summary>
    Mode = 0x107,

    /// <summary>The time of the last modification (DateTimeOffset).</summary>
    Modified = 0x108,

    /// <summary>Mandatory for a directory: the children of this info node are the entries of the directory.</summary>
    Entries = 0x109,

    /// <summary>The algorithm of the content of a file (<see cref="CompressionKind"/>, ushort); gzip if it is not given.</summary>
    Compression = 0x10A,
}

/// <summary>The kinds of the entries (the values of <see cref="Flag.Type"/>): a ushort, from 0x100 as the flags.</summary>
internal enum EntryKind : ushort
{
    File = 0x100,
    Directory = 0x101,
    Symlink = 0x102,
}

/// <summary>The compression algorithms of the contents (the values of <see cref="Flag.Compression"/>): a ushort, from 0x100.</summary>
internal enum CompressionKind : ushort
{
    /// <summary>A gzip stream (RFC 1952).</summary>
    Gzip = 0x100,
}

/// <summary>
/// The layout of a ppakz file: the layout of ppak, but the flags and the kinds are ushorts from 0x100, and the
/// contents are compressed (gzip streams, the algorithm is in the Compression info).
///
/// <code>
/// root                      metadata: the signature "PRMT:PPAKZ1-LE"
/// ├── entry                 data: the name of the file, directory or symbolic link (UTF-8)
/// │   ├── info              metadata: a flag (ushort, <see cref="Flag"/>), data: its value
/// │   ├── ...
/// │   └── info Entries      for a directory: its children are the entries of the directory
/// └── ...
/// </code>
///
/// As in ppak, only the names and the contents are mandatory (the Content of a file, with its Compression, which is
/// gzip if it is not given; the Entries of a directory; a symbolic link, which has no content, needs its Type and Target). Every other info is additional, and a discrepancy
/// in it is a warning. A reader skips the info nodes it does not know, so new flags can be added without a new version.
/// </summary>
internal static class PPakZFormat
{
    /// <summary>The signature of the root (BINARY_FORMAT.md, 10.3): "file" shows it as "Promptuarium container, PPAKZ1-LE".
    /// "-LE": the numbers are little-endian.</summary>
    public const string Signature = "PRMT:PPAKZ1-LE";

    /// <summary>The bits of a mode: the permissions, set-user-ID, set-group-ID and sticky.</summary>
    public const ushort ModeMask = 0xFFF;

    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>An info node: the flag in the metadata, the value in the data.</summary>
    public static Element Info(Flag flag, ReadOnlyMemory<byte> value) => new(value, Data.FromUShort((ushort)flag));

    /// <summary>The flag of an info node, or null if its metadata is not two bytes long (not an info of this format).</summary>
    public static Flag? FlagOf(Element info) => info.MetaData.Length == sizeof(ushort) ? (Flag)info.MetaData.AsUShort() : null;

    /// <summary>The (first) info node of an entry with the flag, or null if the entry has no such info.</summary>
    public static Element? Find(Element entry, Flag flag) => entry.Children.FirstOrDefault(info => FlagOf(info) == flag);

    /// <summary>The name of an entry, or null if it is not valid UTF-8.</summary>
    public static string? Name(Element entry) => Utf8(entry.Data);

    /// <summary>The entries of a directory: the root, or the children of the Entries info of a directory entry.</summary>
    public static IReadOnlyList<Element> EntriesOf(Element directory)
    {
        return directory.Parent is null ? directory.Children : Find(directory, Flag.Entries)?.Children ?? [];
    }

    /// <summary>
    /// The kind of an entry, by its mandatory components: a directory if it has Entries, a file if it has Content, a
    /// symbolic link if neither, but its Type is Symlink and it has a target; otherwise null. The type alone decides
    /// only for a symbolic link.
    /// </summary>
    public static EntryKind? Kind(Element entry)
    {
        bool entries = Find(entry, Flag.Entries) is not null;
        bool content = Find(entry, Flag.Content) is not null;

        return (entries, content) switch
        {
            (true, false) => EntryKind.Directory,
            (false, true) => EntryKind.File,
            (false, false) when Type(entry) == EntryKind.Symlink && Text(entry, Flag.Target) is not null => EntryKind.Symlink,
            _ => null,
        };
    }

    // The values of the additional infos: null if the entry does not have the info, or if its value is not valid

    /// <summary>
    /// The compression algorithm of the content (any ushort, known or not): gzip if the entry has no Compression info,
    /// null if its value is not 2 bytes long. It belongs to the content, so a reader can not ignore it.
    /// </summary>
    public static CompressionKind? Compression(Element entry)
    {
        return Find(entry, Flag.Compression) switch
        {
            null => CompressionKind.Gzip,
            { Data.Length: sizeof(ushort) } info => (CompressionKind)info.Data.AsUShort(),
            _ => null,
        };
    }

    /// <summary>The type (any ushort, known or not), or null.</summary>
    public static EntryKind? Type(Element entry) => Find(entry, Flag.Type) is { Data.Length: sizeof(ushort) } info ? (EntryKind)info.Data.AsUShort() : null;

    /// <summary>A UTF-8 value, or null.</summary>
    public static string? Text(Element entry, Flag flag) => Find(entry, flag) is { } info ? Utf8(info.Data) : null;

    /// <summary>The size (a VarUInt, 1 to 8 bytes), or null.</summary>
    public static ulong? Size(Element entry) => Find(entry, Flag.Size) is { Data.Length: >= 1 and <= sizeof(ulong) } info ? info.Data.AsVarUInt() : null;

    /// <summary>The CRC-32, or null.</summary>
    public static uint? Crc32(Element entry) => Find(entry, Flag.Crc32) is { Data.Length: sizeof(uint) } info ? info.Data.AsUInt() : null;

    /// <summary>The mode (only its valid bits), or null.</summary>
    public static UnixFileMode? Mode(Element entry) => Find(entry, Flag.Mode) is { Data.Length: sizeof(ushort) } info ? (UnixFileMode)(info.Data.AsUShort() & ModeMask) : null;

    /// <summary>The time of the last modification, or null.</summary>
    public static DateTimeOffset? Modified(Element entry)
    {
        if (Find(entry, Flag.Modified) is not { Data.Length: 2 * sizeof(long) } info)
        {
            return null;
        }

        try
        {
            return info.Data.AsDateTimeOffset();
        }
        catch (ArgumentException)
        {
            // Ticks or an offset out of range
            return null;
        }
    }

    private static string? Utf8(ReadOnlyMemory<byte> bytes)
    {
        try
        {
            return StrictUtf8.GetString(bytes.Span);
        }
        catch (DecoderFallbackException)
        {
            return null;
        }
    }
}
