using System.IO.Compression;
using Promptuarium;
using static PPakZ.PPakZFormat;

namespace PPakZ;

/// <summary>
/// Checks a loaded ppakz tree before anything is done with it, as ppak does (PPAK_FORMAT.md, 7). The mandatory
/// components (the signature, the names, the contents) must be right: an error is an <see cref="InvalidDataException"/>.
/// A content must be compressed by a known algorithm (gzip, the only one so far) and it must decompress, and all of them together can not be longer than the
/// limit, so a small archive can not unpack into a huge one ("gzip bomb"). Every other info is additional: a
/// discrepancy in it (a CRC or a size that does not match the decompressed content, a value that is not valid, a type
/// that contradicts the content) is a warning, returned with the path of the entry.
/// </summary>
internal static class Checker
{
    /// <summary>The default limit of the decompressed contents of an archive, together: 4 GiB.</summary>
    public const ulong DefaultMaxBytes = 4UL << 30;

    /// <summary>The warnings of a tree.</summary>
    /// <param name="root">The loaded tree</param>
    /// <param name="maxBytes">The limit of the decompressed contents, together</param>
    /// <exception cref="InvalidDataException">The signature, a name or a content is wrong, or the contents are too long</exception>
    public static List<string> Check(Element root, ulong maxBytes = DefaultMaxBytes)
    {
        if (!root.Data.IsEmpty || root.MetaData.AsAsciiString() != Signature)
        {
            throw new InvalidDataException($"Not a ppakz file: the signature is not \"{Signature}\".");
        }

        var warnings = new List<string>();
        ulong remaining = maxBytes;
        CheckDirectory(root, string.Empty, warnings, ref remaining);
        return warnings;
    }

    /// <summary>
    /// Decompresses a gzip content into the output, at most <paramref name="limit"/> bytes.
    /// </summary>
    /// <returns>The length and the CRC-32 of the decompressed content</returns>
    /// <exception cref="InvalidDataException">It is not a gzip stream, or it is longer than the limit</exception>
    public static (ulong Length, uint Crc) Decompress(ReadOnlyMemory<byte> content, Stream output, ulong limit)
    {
        var crc = new System.IO.Hashing.Crc32();
        byte[] buffer = new byte[81920];
        ulong length = 0;

        using var gzip = new GZipStream(new MemoryStream(content.ToArray(), writable: false), CompressionMode.Decompress);
        int read;

        while ((read = gzip.Read(buffer)) > 0)
        {
            length += (ulong)read;

            if (length > limit)
            {
                throw new InvalidDataException("the decompressed contents are longer than the limit.");
            }

            crc.Append(buffer.AsSpan(0, read));
            output.Write(buffer, 0, read);
        }

        return (length, crc.GetCurrentHashAsUInt32());
    }

    private static void CheckDirectory(Element directory, string path, List<string> warnings, ref ulong remaining)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);

        foreach (Element entry in EntriesOf(directory))
        {
            string name = Name(entry) ?? throw new InvalidDataException($"{path}/: a name that is not valid UTF-8.");

            if (name is "" or "." or ".." || name.Contains('/') || name.Contains('\\') || name.Contains('\0'))
            {
                throw new InvalidDataException($"{path}/: invalid name \"{name}\".");
            }

            if (!names.Add(name))
            {
                throw new InvalidDataException($"{path}/{name}: the name is repeated in the directory.");
            }

            string entryPath = $"{path}/{name}";
            EntryKind kind = Kind(entry) ?? throw new InvalidDataException(Find(entry, Flag.Content) is not null
                ? $"{entryPath}: both a content and entries."
                : $"{entryPath}: neither a content nor entries (nor a symbolic link with a target).");

            CheckInfos(entry, kind, entryPath, warnings, ref remaining);

            if (kind == EntryKind.Directory)
            {
                CheckDirectory(entry, entryPath, warnings, ref remaining);
            }
        }
    }

    private static void CheckInfos(Element entry, EntryKind kind, string path, List<string> warnings, ref ulong remaining)
    {
        void Warn(string message) => warnings.Add($"{path}: {message}");

        foreach (IGrouping<Flag?, Element> flag in entry.Children.GroupBy(FlagOf).Where(group => group.Key is not null && group.Count() > 1))
        {
            Warn($"{flag.Key} is repeated, the first one is used.");
        }

        // The type is additional: the mandatory components decide the kind
        if (Find(entry, Flag.Type) is not null)
        {
            EntryKind? type = Type(entry);

            if (type is not (EntryKind.File or EntryKind.Directory or EntryKind.Symlink))
            {
                Warn(type is null ? "the type is not 2 bytes long." : $"unknown type 0x{(ushort)type:X}, it is a {kind} by its content.");
            }
            else if (type != kind)
            {
                Warn($"the type is {type}, but it is a {kind} by its content.");
            }
        }

        if (kind == EntryKind.File)
        {
            // The content is mandatory, and so is its algorithm: a content that can not be decompressed is an error
            if (Compression(entry) is not CompressionKind.Gzip)
            {
                throw new InvalidDataException(Compression(entry) is { } unknown
                    ? $"{path}: unknown compression 0x{(ushort)unknown:X}."
                    : $"{path}: the compression is not 2 bytes long.");
            }

            // It must be a gzip stream, within the limit
            (ulong length, uint actual) content;

            try
            {
                content = Decompress(Find(entry, Flag.Content)!.Data, Stream.Null, remaining);
            }
            catch (InvalidDataException ex)
            {
                throw new InvalidDataException($"{path}: {ex.Message}", ex);
            }

            remaining -= content.length;

            if (Find(entry, Flag.Size) is not null)
            {
                if (Size(entry) is not { } size)
                {
                    Warn("the size is not 1 to 8 bytes long.");
                }
                else if (size != content.length)
                {
                    Warn($"the size is {size:N0} bytes, but the content is {content.length:N0} bytes long.");
                }
            }

            if (Find(entry, Flag.Crc32) is not null)
            {
                if (Crc32(entry) is not { } crc)
                {
                    Warn("the CRC-32 is not 4 bytes long.");
                }
                else if (crc != content.actual)
                {
                    Warn($"CRC mismatch: {crc:X8} stored, {content.actual:X8} computed.");
                }
            }
        }
        else
        {
            foreach (Flag flag in new[] { Flag.Size, Flag.Crc32, Flag.Compression }.Where(flag => Find(entry, flag) is not null))
            {
                Warn($"{flag} belongs to a file, it is ignored.");
            }
        }

        if (Find(entry, Flag.Target) is not null)
        {
            if (Text(entry, Flag.Target) is null)
            {
                Warn("the target is not valid UTF-8.");
            }
            else if (kind != EntryKind.Symlink)
            {
                Warn("a target belongs to a symbolic link, it is ignored.");
            }
        }

        foreach (Flag flag in new[] { Flag.Owner, Flag.Group }.Where(flag => Find(entry, flag) is not null && Text(entry, flag) is null))
        {
            Warn($"the {flag} is not valid UTF-8.");
        }

        if (Find(entry, Flag.Mode) is { } mode)
        {
            if (mode.Data.Length != sizeof(ushort))
            {
                Warn("the mode is not 2 bytes long.");
            }
            else if ((mode.Data.AsUShort() & ~ModeMask) != 0)
            {
                Warn($"the mode ({Convert.ToString(mode.Data.AsUShort(), 8)}) has bits above 07777, they are ignored.");
            }
        }

        if (Find(entry, Flag.Modified) is not null && Modified(entry) is null)
        {
            Warn("the time of the modification is not valid.");
        }

        foreach (Element info in entry.Children.Where(info => FlagOf(info) is { } flag && flag != Flag.Entries && info.Children.Count > 0))
        {
            Warn($"{FlagOf(info)} has children, they are ignored.");
        }

        // The unknown flags (and the infos whose metadata is not 2 bytes long) are skipped without a warning: they are
        // the extensions of the format
    }
}
