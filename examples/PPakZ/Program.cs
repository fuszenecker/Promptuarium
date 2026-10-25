// ppakz: ppak with binary flags and kinds, and with the contents compressed as gzip streams. See PPakZFormat.cs
// for the layout, and README.md.
//
//   ppakz pack DIRECTORY FILE      packs the contents of the directory
//   ppakz unpack FILE DIRECTORY    unpacks into the directory
//   ppakz list FILE                lists the entries
//   ppakz verify FILE              checks the contents (gzip, size, CRC)

using PPakZ;
using Promptuarium;
using static PPakZ.PPakZFormat;

try
{
    switch (args)
    {
        case ["pack", string directory, string file]:
            Element packed = Packer.Pack(directory);
            await packed.SaveAsync(file);
            Console.WriteLine($"{file}: {packed.GetStatistics().Nodes} nodes, {new FileInfo(file).Length:N0} bytes");
            return 0;

        case ["unpack", string file, string directory]:
            if (!Unpacker.Unpack(await LoadAsync(file), directory))
            {
                Console.WriteLine("The owners were not restored (only root can set them).");
            }

            return 0;

        case ["list", string file]:
            List(await LoadAsync(file));
            return 0;

        case ["verify", string file]:
            List<string> corrupted = Unpacker.Corrupted(await LoadAsync(file)).ToList();
            corrupted.ForEach(path => Console.WriteLine($"Corrupted: {path}"));
            Console.WriteLine(corrupted.Count == 0 ? "OK" : $"{corrupted.Count} corrupted files");
            return corrupted.Count == 0 ? 0 : 1;

        default:
            Console.Error.WriteLine("Usage: ppakz pack DIRECTORY FILE | unpack FILE DIRECTORY | list FILE | verify FILE");
            return 2;
    }
}
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PromptuariumException or InvalidDataException)
{
    // InvalidDataException: a wrong signature, content or name
    Console.Error.WriteLine(ex.Message);
    return 1;
}

static async Task<Element> LoadAsync(string file)
{
    // The file may come from anywhere: the tree can not be larger than the file
    Element root = await Element.LoadAsync(file, new LoadOptions { MaxBytes = new FileInfo(file).Length }, CancellationToken.None);
    Unpacker.CheckSignature(root);
    return root;
}

static void List(Element directory, string path = "")
{
    foreach (Element entry in EntriesOf(directory))
    {
        EntryKind kind = KindOf(entry);
        string mode = Find(entry, Flag.Mode) is { } m ? Permissions(kind, (UnixFileMode)m.Data.AsUShort()) : "?";
        string owner = $"{Find(entry, Flag.Owner)?.Data.AsUtf8String() ?? "?"}:{Find(entry, Flag.Group)?.Data.AsUtf8String() ?? "?"}";
        ulong size = Find(entry, Flag.Size)?.Data.AsVarUInt() ?? 0;
        long packed = Find(entry, Flag.Content)?.Data.Length ?? 0;
        string time = Find(entry, Flag.Modified) is { } t ? t.Data.AsDateTimeOffset().ToLocalTime().ToString("yyyy-MM-dd HH:mm") : "?";
        string link = Find(entry, Flag.Target) is { } target ? $" -> {target.Data.AsUtf8String()}" : string.Empty;

        Console.WriteLine($"{mode,-10} {owner,-20} {size,10:N0} {packed,10:N0} {time}  {path}{Name(entry)}{link}");

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
