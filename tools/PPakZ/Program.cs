// ppakz: ppak with binary flags and kinds, and with the contents compressed as gzip streams. See PPakZFormat.cs
// for the layout, and README.md.
//
//   ppakz pack DIRECTORY FILE                          packs the contents of the directory
//   ppakz unpack FILE DIRECTORY [--strict] [--max-size=BYTES]
//                                                      unpacks into the directory (--strict: no warnings allowed)
//   ppakz list FILE [--max-size=BYTES]                 lists the entries
//   ppakz verify FILE [--max-size=BYTES]               checks the file: the mandatory components, and the additional infos
//
// As in ppak, only the names and the contents are mandatory: an error in them (or a content that is not a gzip stream,
// or contents longer than --max-size together, 4 GiB by default) stops ppakz. A discrepancy in the additional infos
// (size, CRC-32, type, mode, ...) is a warning, written to the standard error; by default the content is used anyway.

using PPakZ;
using Promptuarium;
using static PPakZ.PPakZFormat;

const string Usage = "Usage: ppakz pack DIRECTORY FILE | unpack FILE DIRECTORY [--strict] [--max-size=BYTES] | list FILE [--max-size=BYTES] | verify FILE [--max-size=BYTES]";

try
{
    switch (args)
    {
        case ["pack", string directory, string file]:
            Element packed = Packer.Pack(directory);
            await packed.SaveAsync(file);
            Console.WriteLine($"{file}: {packed.GetStatistics().Nodes} nodes, {new FileInfo(file).Length:N0} bytes");
            return 0;

        case ["unpack", string file, string directory, .. var options] when Options(options, allowStrict: true) is { } parsed:
        {
            (Element root, List<string> warnings) = await LoadAsync(file, parsed.MaxBytes);

            // Strict: a discrepancy is an error, and nothing is written
            if (parsed.Strict && warnings.Count > 0)
            {
                Console.Error.WriteLine($"{warnings.Count:N0} warnings, nothing was unpacked (--strict).");
                return 1;
            }

            Unpacker.Unpack(root, directory, warning => Warn(warnings, warning));
            Summary(warnings);
            return 0;
        }

        case ["list", string file, .. var options] when Options(options, allowStrict: false) is { } parsed:
        {
            (Element root, List<string> warnings) = await LoadAsync(file, parsed.MaxBytes);
            List(root);
            Summary(warnings);
            return 0;
        }

        case ["verify", string file, .. var options] when Options(options, allowStrict: false) is { } parsed:
        {
            (_, List<string> warnings) = await LoadAsync(file, parsed.MaxBytes);
            Console.WriteLine(warnings.Count == 0 ? "OK" : $"{warnings.Count:N0} warnings");
            return warnings.Count == 0 ? 0 : 1;
        }

        default:
            Console.Error.WriteLine(Usage);
            return 2;
    }
}
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PromptuariumException or InvalidDataException)
{
    // InvalidDataException: a wrong signature, name or content (the mandatory components)
    Console.Error.WriteLine($"ppakz: {ex.Message}");
    return 1;
}

// The options, or null if they are not valid
static (bool Strict, ulong MaxBytes)? Options(string[] options, bool allowStrict)
{
    bool strict = false;
    ulong maxBytes = Checker.DefaultMaxBytes;

    foreach (string option in options)
    {
        if (allowStrict && option == "--strict")
        {
            strict = true;
        }
        else if (option.StartsWith("--max-size=", StringComparison.Ordinal) && ulong.TryParse(option["--max-size=".Length..], out ulong value))
        {
            maxBytes = value;
        }
        else
        {
            return null;
        }
    }

    return (strict, maxBytes);
}

// Loads and checks a file; the warnings of the check are written at once
static async Task<(Element Root, List<string> Warnings)> LoadAsync(string file, ulong maxBytes)
{
    // The file may come from anywhere: the tree can not be larger than the file
    Element root = await Element.LoadAsync(file, new LoadOptions { MaxBytes = new FileInfo(file).Length }, CancellationToken.None);
    List<string> warnings = [];
    Checker.Check(root, maxBytes).ForEach(warning => Warn(warnings, warning));
    return (root, warnings);
}

static void Warn(List<string> warnings, string warning)
{
    warnings.Add(warning);
    Console.Error.WriteLine($"warning: {warning}");
}

static void Summary(List<string> warnings)
{
    if (warnings.Count > 0)
    {
        Console.Error.WriteLine($"{warnings.Count:N0} warnings.");
    }
}

static void List(Element directory, string path = "")
{
    foreach (Element entry in EntriesOf(directory))
    {
        EntryKind kind = Kind(entry)!.Value;
        string mode = Mode(entry) is { } m ? Permissions(kind, m) : "?";
        string owner = $"{Text(entry, Flag.Owner) ?? "?"}:{Text(entry, Flag.Group) ?? "?"}";
        string size = Size(entry) is { } s ? s.ToString("N0") : "?";
        long packed = Find(entry, Flag.Content)?.Data.Length ?? 0;
        string time = Modified(entry) is { } t ? t.ToLocalTime().ToString("yyyy-MM-dd HH:mm") : "?";
        string link = kind == EntryKind.Symlink ? $" -> {Text(entry, Flag.Target)}" : string.Empty;

        Console.WriteLine($"{mode,-10} {owner,-20} {size,10} {packed,10:N0} {time}  {path}{Name(entry)}{link}");

        if (kind == EntryKind.Directory)
        {
            List(entry, $"{path}{Name(entry)}/");
        }
    }
}

static string Permissions(EntryKind kind, UnixFileMode mode)
{
    char type = kind switch { EntryKind.Directory => 'd', EntryKind.Symlink => 'l', _ => '-' };
    string bits = "rwxrwxrwx";
    return type + new string(bits.Select((c, i) => ((int)mode & (1 << (8 - i))) != 0 ? c : '-').ToArray());
}
