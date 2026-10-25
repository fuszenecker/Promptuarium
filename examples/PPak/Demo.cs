using Promptuarium;

namespace PPak;

/// <summary>
/// Packs a sample directory (files, a binary file, subdirectories, an empty directory, a symbolic link,
/// permissions, times), unpacks it, and compares the result with the original.
/// </summary>
internal static class Demo
{
    public static async Task<int> RunAsync()
    {
        string work = Path.Combine(Path.GetTempPath(), $"ppak-demo-{Guid.NewGuid():N}");
        string source = Path.Combine(work, "source");
        string target = Path.Combine(work, "target");
        string file = Path.Combine(work, "sample.ppak");

        try
        {
            CreateSample(source);

            Element packed = Packer.Pack(source);
            await packed.SaveAsync(file);
            Console.WriteLine($"Packed into {new FileInfo(file).Length:N0} bytes, {packed.GetStatistics().Nodes} nodes:");
            Console.WriteLine(packed.TreeToString("  ").Split('\n').Take(14).Aggregate((a, b) => a + "\n" + b) + "\n  ...");

            Element loaded = await Element.LoadAsync(file);
            Unpacker.Unpack(loaded, target);

            List<string> differences = Compare(source, target).ToList();
            differences.ForEach(difference => Console.WriteLine($"Difference: {difference}"));
            Console.WriteLine(differences.Count == 0 ? "Unpacked: the same as the original." : $"{differences.Count} differences.");
            return differences.Count == 0 ? 0 : 1;
        }
        finally
        {
            Directory.Delete(work, recursive: true);
        }
    }

    private static void CreateSample(string source)
    {
        Directory.CreateDirectory(Path.Combine(source, "docs", "notes"));
        Directory.CreateDirectory(Path.Combine(source, "empty"));
        File.WriteAllText(Path.Combine(source, "readme.txt"), "A sample directory, packed by ppak.\n");
        File.WriteAllText(Path.Combine(source, "docs", "notes", "árvíztűrő.md"), "# Unicode names work too\n");
        File.WriteAllBytes(Path.Combine(source, "docs", "random.bin"), Enumerable.Range(0, 100_000).Select(i => (byte)(i * 7919 >> 3)).ToArray());
        File.WriteAllBytes(Path.Combine(source, "empty.bin"), []);

        if (!OperatingSystem.IsWindows())
        {
            File.CreateSymbolicLink(Path.Combine(source, "link-to-readme"), "readme.txt");
            File.SetUnixFileMode(Path.Combine(source, "docs", "random.bin"), UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }

        File.SetLastWriteTimeUtc(Path.Combine(source, "readme.txt"), new DateTime(2001, 2, 3, 4, 5, 6, DateTimeKind.Utc));
    }

    private static IEnumerable<string> Compare(string source, string target)
    {
        foreach (string path in Directory.EnumerateFileSystemEntries(source, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(source, path);
            var original = new FileInfo(path);
            var copy = new FileInfo(Path.Combine(target, relative));

            if (!copy.Exists && !Directory.Exists(copy.FullName) && copy.LinkTarget is null)
            {
                yield return $"{relative} is missing";
                continue;
            }

            if (original.LinkTarget != copy.LinkTarget)
            {
                yield return $"{relative}: link target";
            }
            else if (original.LinkTarget is null && File.Exists(path) && !File.ReadAllBytes(path).AsSpan().SequenceEqual(File.ReadAllBytes(copy.FullName)))
            {
                yield return $"{relative}: content";
            }

            if (original.LinkTarget is null && !OperatingSystem.IsWindows() && File.GetUnixFileMode(path) != File.GetUnixFileMode(copy.FullName))
            {
                yield return $"{relative}: permissions";
            }

            if (original.LinkTarget is null && original.LastWriteTimeUtc != copy.LastWriteTimeUtc)
            {
                yield return $"{relative}: time";
            }
        }
    }
}
