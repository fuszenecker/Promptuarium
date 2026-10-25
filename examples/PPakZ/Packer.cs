using System.IO.Compression;
using PPak;
using Promptuarium;
using static PPakZ.PPakZFormat;

namespace PPakZ;

/// <summary>
/// Packs a directory tree into a ppakz tree.
/// </summary>
internal static class Packer
{
    /// <summary>The ppakz tree of the contents of a directory (the directory itself is not an entry).</summary>
    public static Element Pack(string directory)
    {
        Element root = new ElementBuilder().WithSignature(Data.FromAsciiString(Signature)).Build();
        root.Add(Entries(new DirectoryInfo(directory)));
        return root;
    }

    // The entries in the order of their names, so that the same directory always gives the same file
    private static IEnumerable<Element> Entries(DirectoryInfo directory)
    {
        return directory.EnumerateFileSystemInfos()
            .OrderBy(info => info.Name, StringComparer.Ordinal)
            .Select(Entry)
            .ToList();
    }

    private static Element Entry(FileSystemInfo info)
    {
        var entry = new Element(Data.FromUtf8String(info.Name), null);

        if (info.LinkTarget is string target)
        {
            entry.Add(Info(Flag.Type, Data.FromByte((byte)EntryKind.Symlink)), Info(Flag.Target, Data.FromUtf8String(target)));
        }
        else if (info is DirectoryInfo)
        {
            entry.Add(Info(Flag.Type, Data.FromByte((byte)EntryKind.Directory)));
        }
        else
        {
            (byte[] compressed, ulong size, uint crc) = Compress(info.FullName);
            entry.Add(
                Info(Flag.Type, Data.FromByte((byte)EntryKind.File)),
                Info(Flag.Content, compressed),
                Info(Flag.Size, Data.FromVarUInt(size)),
                Info(Flag.Crc32, Data.FromUInt(crc)));
        }

        (string? owner, string? group) = UnixOwner.Get(info.FullName);

        if (owner is not null)
        {
            entry.Add(Info(Flag.Owner, Data.FromUtf8String(owner)));
        }

        if (group is not null)
        {
            entry.Add(Info(Flag.Group, Data.FromUtf8String(group)));
        }

        // The permissions of a symbolic link mean nothing (and reading them would follow the link)
        if (!OperatingSystem.IsWindows() && info.LinkTarget is null)
        {
            entry.Add(Info(Flag.Mode, Data.FromUShort((ushort)info.UnixFileMode)));
        }

        entry.Add(Info(Flag.Modified, Data.FromDateTimeOffset(new DateTimeOffset(info.LastWriteTimeUtc))));

        if (info is DirectoryInfo { LinkTarget: null } subdirectory)
        {
            entry.Add(new Element(null, Data.FromByte((byte)Flag.Entries), Entries(subdirectory)));
        }

        return entry;
    }

    // The file is read as a stream, so only its compressed form is held in memory
    private static (byte[] Compressed, ulong Size, uint Crc) Compress(string path)
    {
        var crc = new System.IO.Hashing.Crc32();
        var compressed = new MemoryStream();
        byte[] buffer = new byte[81920];
        ulong size = 0;

        using (FileStream file = File.OpenRead(path))
        using (var gzip = new GZipStream(compressed, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            int read;

            while ((read = file.Read(buffer)) > 0)
            {
                crc.Append(buffer.AsSpan(0, read));
                gzip.Write(buffer, 0, read);
                size += (ulong)read;
            }
        }

        return (compressed.ToArray(), size, crc.GetCurrentHashAsUInt32());
    }
}
