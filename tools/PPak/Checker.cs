using Promptuarium;
using static PPak.PPakFormat;

namespace PPak;

/// <summary>
/// Checks a loaded ppak tree before anything is done with it (PPAK_FORMAT.md, 7). The mandatory components (the
/// signature, the names, the contents) must be right: an error is an <see cref="InvalidDataException"/>. Every other
/// info is additional: a discrepancy in it (a CRC or a size that does not match the content, a value that is not
/// valid, a type that contradicts the content) is a warning, returned with the path of the entry.
/// </summary>
internal static class Checker
{
    /// <summary>The warnings of a tree.</summary>
    /// <exception cref="InvalidDataException">The signature, a name or a content is wrong</exception>
    public static List<string> Check(Element root)
    {
        if (!root.Data.IsEmpty || root.MetaData.AsAsciiString() != Signature)
        {
            throw new InvalidDataException($"Not a ppak file: the signature is not \"{Signature}\".");
        }

        var warnings = new List<string>();
        CheckDirectory(root, string.Empty, warnings);
        return warnings;
    }

    private static void CheckDirectory(Element directory, string path, List<string> warnings)
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
            string kind = Kind(entry) ?? throw new InvalidDataException(Find(entry, ContentFlag) is not null
                ? $"{entryPath}: both a content and entries."
                : $"{entryPath}: neither a content nor entries (nor a symbolic link with a target).");

            CheckInfos(entry, kind, entryPath, warnings);

            if (kind == DirectoryKind)
            {
                CheckDirectory(entry, entryPath, warnings);
            }
        }
    }

    private static void CheckInfos(Element entry, string kind, string path, List<string> warnings)
    {
        void Warn(string message) => warnings.Add($"{path}: {message}");

        foreach (IGrouping<string, Element> flag in entry.Children.GroupBy(info => info.MetaData.AsAsciiString()).Where(group => group.Count() > 1))
        {
            Warn($"\"{flag.Key}\" is repeated, the first one is used.");
        }

        // The type is additional: the mandatory components decide the kind
        if (Find(entry, TypeFlag) is not null)
        {
            string? type = Text(entry, TypeFlag);

            if (type is not (FileKind or DirectoryKind or SymlinkKind))
            {
                Warn(type is null ? "the type is not valid UTF-8." : $"unknown type \"{type}\", it is a {kind} by its content.");
            }
            else if (type != kind)
            {
                Warn($"the type is \"{type}\", but it is a {kind} by its content.");
            }
        }

        if (kind == FileKind)
        {
            ReadOnlyMemory<byte> content = Find(entry, ContentFlag)!.Data;

            if (Find(entry, SizeFlag) is not null)
            {
                if (Size(entry) is not { } size)
                {
                    Warn("the size is not 8 bytes long.");
                }
                else if (size != (ulong)content.Length)
                {
                    Warn($"the size is {size:N0} bytes, but the content is {content.Length:N0} bytes long.");
                }
            }

            if (Find(entry, Crc32Flag) is not null)
            {
                if (Crc32(entry) is not { } crc)
                {
                    Warn("the CRC-32 is not 4 bytes long.");
                }
                else if (System.IO.Hashing.Crc32.HashToUInt32(content.Span) is var actual && actual != crc)
                {
                    Warn($"CRC mismatch: {crc:X8} stored, {actual:X8} computed.");
                }
            }
        }
        else
        {
            foreach (string flag in new[] { SizeFlag, Crc32Flag }.Where(flag => Find(entry, flag) is not null))
            {
                Warn($"\"{flag}\" belongs to a file, it is ignored.");
            }
        }

        if (Find(entry, TargetFlag) is not null)
        {
            if (Text(entry, TargetFlag) is null)
            {
                Warn("the target is not valid UTF-8.");
            }
            else if (kind != SymlinkKind)
            {
                Warn("a target belongs to a symbolic link, it is ignored.");
            }
        }

        foreach (string flag in new[] { OwnerFlag, GroupFlag }.Where(flag => Find(entry, flag) is not null && Text(entry, flag) is null))
        {
            Warn($"the {flag} is not valid UTF-8.");
        }

        if (Find(entry, ModeFlag) is { } mode)
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

        if (Find(entry, ModifiedFlag) is not null && Modified(entry) is null)
        {
            Warn("the time of the modification is not valid.");
        }

        foreach (Element info in entry.Children.Where(info => info.MetaData.AsAsciiString() != EntriesFlag && info.Children.Count > 0))
        {
            Warn($"\"{info.MetaData.AsAsciiString()}\" has children, they are ignored.");
        }

        // The unknown flags are skipped without a warning: they are the extensions of the format (PPAK_FORMAT.md, 6)
    }
}
