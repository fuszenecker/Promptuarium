// ppak: packs a directory tree into a single file, and unpacks it, with the sizes, the CRC-32s, the permissions, the
// owners, the times and the symbolic links. See PPAK_FORMAT.md for the format, and README.md.
//
//   ppak pack DIRECTORY FILE               packs the contents of the directory
//   ppak unpack FILE DIRECTORY [--strict]  unpacks into the directory (--strict: no warnings allowed)
//   ppak list FILE                         lists the entries
//   ppak verify FILE                       checks the file: the mandatory components, and the additional infos
//
// Only the names and the contents are mandatory: an error in them stops ppak. A discrepancy in the additional infos
// (size, CRC-32, type, mode, ...) is a warning, written to the standard error; by default the content is used anyway.

using PPak;
using Promptuarium;
using static PPak.PPakFormat;

try
{
    switch (args)
    {
        case ["pack", string directory, string file]:
            Element packed = Packer.Pack(directory);
            await packed.SaveAsync(file);
            Console.WriteLine($"{file}: {packed.GetStatistics().Nodes} nodes, {new FileInfo(file).Length:N0} bytes");
            return 0;

        case ["unpack", string file, string directory, .. var options] when options is [] or ["--strict"]:
        {
            (Element root, List<string> warnings) = await LoadAsync(file);

            // Strict: a discrepancy is an error, and nothing is written
            if (options is ["--strict"] && warnings.Count > 0)
            {
                Console.Error.WriteLine($"{warnings.Count:N0} warnings, nothing was unpacked (--strict).");
                return 1;
            }

            Unpacker.Unpack(root, directory, warning => Warn(warnings, warning));
            Summary(warnings);
            return 0;
        }

        case ["list", string file]:
        {
            (Element root, List<string> warnings) = await LoadAsync(file);
            List(root);
            Summary(warnings);
            return 0;
        }

        case ["verify", string file]:
        {
            (_, List<string> warnings) = await LoadAsync(file);
            Console.WriteLine(warnings.Count == 0 ? "OK" : $"{warnings.Count:N0} warnings");
            return warnings.Count == 0 ? 0 : 1;
        }

        default:
            Console.Error.WriteLine("Usage: ppak pack DIRECTORY FILE | unpack FILE DIRECTORY [--strict] | list FILE | verify FILE");
            return 2;
    }
}
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PromptuariumException or InvalidDataException)
{
    // InvalidDataException: a wrong signature, name or content (the mandatory components)
    Console.Error.WriteLine($"ppak: {ex.Message}");
    return 1;
}

// Loads and checks a file; the warnings of the check are written at once
static async Task<(Element Root, List<string> Warnings)> LoadAsync(string file)
{
    // The file may come from anywhere: the tree can not be larger than the file
    Element root = await Element.LoadAsync(file, new LoadOptions { MaxBytes = new FileInfo(file).Length }, CancellationToken.None);
    List<string> warnings = [];
    Checker.Check(root).ForEach(warning => Warn(warnings, warning));
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
        string kind = Kind(entry)!;
        string mode = Mode(entry) is { } m ? Permissions(kind, m) : "?";
        string owner = $"{Text(entry, OwnerFlag) ?? "?"}:{Text(entry, GroupFlag) ?? "?"}";
        long size = Find(entry, ContentFlag)?.Data.Length ?? 0;
        string time = Modified(entry) is { } t ? t.ToLocalTime().ToString("yyyy-MM-dd HH:mm") : "?";
        string link = kind == SymlinkKind ? $" -> {Text(entry, TargetFlag)}" : string.Empty;

        Console.WriteLine($"{mode,-10} {owner,-20} {size,10:N0} {time}  {path}{Name(entry)}{link}");

        if (kind == DirectoryKind)
        {
            List(entry, $"{path}{Name(entry)}/");
        }
    }
}

static string Permissions(string kind, UnixFileMode mode)
{
    char type = kind switch { DirectoryKind => 'd', SymlinkKind => 'l', _ => '-' };
    string bits = "rwxrwxrwx";
    return type + new string(bits.Select((c, i) => ((int)mode & (1 << (8 - i))) != 0 ? c : '-').ToArray());
}
