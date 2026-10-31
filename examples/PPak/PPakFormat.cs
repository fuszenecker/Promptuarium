using System.Text;
using Promptuarium;

namespace PPak;

/// <summary>
/// The layout of a ppak file (PPAK_FORMAT.md). Every level of the directory tree is two levels of the Promptuarium tree:
///
/// <code>
/// root                      metadata: the signature "PRMT:PPAK1-LE"
/// ├── entry                 data: the name of the file, directory or symbolic link
/// │   ├── info              metadata: a flag ("content", "entries", "crc32", ...), data: its value
/// │   ├── ...
/// │   └── info "entries"    for a directory: its children are the entries of the directory
/// └── ...
/// </code>
///
/// Only the names and the contents (the "content" of a file, the "entries" of a directory) are mandatory; every other
/// info is additional, and a discrepancy in it is a warning. A reader skips the info nodes it does not know, so new
/// flags can be added without a new version.
/// </summary>
internal static class PPakFormat
{
    /// <summary>The signature of the root (BINARY_FORMAT.md, 10.3): "file" shows it as "Promptuarium container, PPAK1-LE".
    /// "-LE": the numbers are little-endian.</summary>
    public const string Signature = "PRMT:PPAK1-LE";

    /// <summary>Mandatory for a file: its content (any bytes).</summary>
    public const string ContentFlag = "content";

    /// <summary>Mandatory for a directory: its children are the entries of the directory.</summary>
    public const string EntriesFlag = "entries";

    /// <summary>The kind of the entry: "file", "directory" or "symlink" (UTF-8).</summary>
    public const string TypeFlag = "type";

    /// <summary>The length of the content in bytes (ulong, 8 bytes).</summary>
    public const string SizeFlag = "size";

    /// <summary>The CRC-32 (IEEE 802.3) of the content (uint, 4 bytes).</summary>
    public const string Crc32Flag = "crc32";

    /// <summary>The target of a symbolic link (UTF-8).</summary>
    public const string TargetFlag = "target";

    /// <summary>The name of the owner (UTF-8).</summary>
    public const string OwnerFlag = "owner";

    /// <summary>The name of the group (UTF-8).</summary>
    public const string GroupFlag = "group";

    /// <summary>The Unix permissions (ushort, 2 bytes, e.g. 0644; at most 07777).</summary>
    public const string ModeFlag = "mode";

    /// <summary>The time of the last modification (DateTimeOffset, 16 bytes).</summary>
    public const string ModifiedFlag = "modified";

    /// <summary>The kinds of the entries (the values of "type").</summary>
    public const string FileKind = "file", DirectoryKind = "directory", SymlinkKind = "symlink";

    /// <summary>The bits of a mode: the permissions, set-user-ID, set-group-ID and sticky.</summary>
    public const ushort ModeMask = 0xFFF;

    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>An info node: the flag in the metadata, the value in the data.</summary>
    public static Element Info(string flag, ReadOnlyMemory<byte> value) => new(value, Data.FromAsciiString(flag));

    /// <summary>The (first) info node of an entry with the flag, or null if the entry has no such info.</summary>
    public static Element? Find(Element entry, string flag)
    {
        return entry.Children.FirstOrDefault(info => info.MetaData.AsAsciiString() == flag);
    }

    /// <summary>The name of an entry, or null if it is not valid UTF-8.</summary>
    public static string? Name(Element entry) => Utf8(entry.Data);

    /// <summary>The entries of a directory: the root, or the children of the "entries" info of a directory entry.</summary>
    public static IReadOnlyList<Element> EntriesOf(Element directory)
    {
        return directory.Parent is null ? directory.Children : Find(directory, EntriesFlag)?.Children ?? [];
    }

    /// <summary>
    /// The kind of an entry, by its mandatory components (PPAK_FORMAT.md, 5): a directory if it has "entries", a file
    /// if it has "content", a symbolic link if neither, but its type is "symlink" and it has a target; otherwise null.
    /// The type alone does not decide.
    /// </summary>
    public static string? Kind(Element entry)
    {
        bool entries = Find(entry, EntriesFlag) is not null;
        bool content = Find(entry, ContentFlag) is not null;

        return (entries, content) switch
        {
            (true, false) => DirectoryKind,
            (false, true) => FileKind,
            (false, false) when Text(entry, TypeFlag) == SymlinkKind && Text(entry, TargetFlag) is not null => SymlinkKind,
            _ => null,
        };
    }

    // The values of the additional infos: null if the entry does not have the info, or if its value is not valid

    /// <summary>A UTF-8 value, or null.</summary>
    public static string? Text(Element entry, string flag) => Find(entry, flag) is { } info ? Utf8(info.Data) : null;

    /// <summary>The size, or null.</summary>
    public static ulong? Size(Element entry) => Find(entry, SizeFlag) is { Data.Length: sizeof(ulong) } info ? info.Data.AsULong() : null;

    /// <summary>The CRC-32, or null.</summary>
    public static uint? Crc32(Element entry) => Find(entry, Crc32Flag) is { Data.Length: sizeof(uint) } info ? info.Data.AsUInt() : null;

    /// <summary>The mode (only its valid bits), or null.</summary>
    public static UnixFileMode? Mode(Element entry) => Find(entry, ModeFlag) is { Data.Length: sizeof(ushort) } info ? (UnixFileMode)(info.Data.AsUShort() & ModeMask) : null;

    /// <summary>The time of the last modification, or null.</summary>
    public static DateTimeOffset? Modified(Element entry)
    {
        if (Find(entry, ModifiedFlag) is not { Data.Length: 2 * sizeof(long) } info)
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
