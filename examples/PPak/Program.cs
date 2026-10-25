// ppak: packs a directory tree into a single file, and unpacks it, with the permissions, the owners, the times,
// the symbolic links and a CRC-32 of every file. See PPakFormat.cs for the layout, and README.md.
//
//   ppak pack DIRECTORY FILE      packs the contents of the directory
//   ppak unpack FILE DIRECTORY    unpacks into the directory
//   ppak list FILE                lists the entries
//   ppak verify FILE              checks the CRCs
//   ppak                          packs and unpacks a sample directory, and compares the result

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
            corrupted.ForEach(path => Console.WriteLine($"CRC mismatch: {path}"));
            Console.WriteLine(corrupted.Count == 0 ? "OK" : $"{corrupted.Count} corrupted files");
            return corrupted.Count == 0 ? 0 : 1;

        case []:
            return await Demo.RunAsync();

        default:
            Console.Error.WriteLine("Usage: ppak pack DIRECTORY FILE | unpack FILE DIRECTORY | list FILE | verify FILE");
            return 2;
    }
}
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PromptuariumException or InvalidDataException)
{
    // InvalidDataException: a wrong signature, CRC or name
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
        string type = Find(entry, TypeFlag)?.Data.AsUtf8String() ?? FileKind;
        string mode = Find(entry, ModeFlag) is { } m ? Permissions(type, (UnixFileMode)m.Data.AsUShort()) : "?";
        string owner = $"{Find(entry, OwnerFlag)?.Data.AsUtf8String() ?? "?"}:{Find(entry, GroupFlag)?.Data.AsUtf8String() ?? "?"}";
        long size = Find(entry, ContentFlag)?.Data.Length ?? 0;
        string time = Find(entry, ModifiedFlag) is { } t ? t.Data.AsDateTimeOffset().ToLocalTime().ToString("yyyy-MM-dd HH:mm") : "?";
        string link = Find(entry, TargetFlag) is { } target ? $" -> {target.Data.AsUtf8String()}" : string.Empty;

        Console.WriteLine($"{mode,-10} {owner,-20} {size,10:N0} {time}  {path}{Name(entry)}{link}");

        if (type == DirectoryKind)
        {
            List(entry, $"{path}{Name(entry)}/");
        }
    }
}

static string Permissions(string type, UnixFileMode mode)
{
    char kind = type switch { DirectoryKind => 'd', SymlinkKind => 'l', _ => '-' };
    string bits = "rwxrwxrwx";
    return kind + new string(bits.Select((c, i) => ((int)mode & (1 << (8 - i))) != 0 ? c : '-').ToArray());
}
