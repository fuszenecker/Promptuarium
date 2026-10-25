# ppakz

`ppakz` is [`ppak`](../PPak/README.md) in a binary form: the flags of the info nodes and the kinds of the entries
are bytes (enums), not text, and the content of every file is a **gzip stream**. The commands are the same:

```sh
ppakz pack DIRECTORY FILE      # packs the contents of the directory
ppakz unpack FILE DIRECTORY    # unpacks into the directory
ppakz list FILE                # lists the entries, with the size and the compressed size
ppakz verify FILE              # checks the contents: gzip, size and CRC
```

```sh
dotnet run --project examples/PPakZ -- pack ~/notes notes.ppakz
file -m misc/promptuarium.magic notes.ppakz     # notes.ppakz: Promptuarium container, PPAKZ1
```

## Layout

The tree is the tree of ppak; the metadata of an info node is one byte, the value of `Flag`:

```text
root                         metadata "PRMT:PPAKZ1" (the signature, BINARY_FORMAT.md 10.3)
├── entry                    data: the name (UTF-8)
│   ├── info 1  Type         the kind, one byte: 1 file, 2 directory, 3 symbolic link
│   ├── info 2  Content      the content of a file, as a gzip stream
│   ├── info 3  Size         the length of the content before compression (VarUInt)
│   ├── info 4  Crc32        the CRC-32 of the content before compression (uint)
│   ├── info 5  Target       the target of a symbolic link (UTF-8)
│   ├── info 6  Owner        the name of the owner (UTF-8)
│   ├── info 7  Group        the name of the group (UTF-8)
│   ├── info 8  Mode         the permissions (ushort, e.g. 0644)
│   ├── info 9  Modified     the time of the last modification (DateTimeOffset)
│   └── info 10 Entries      for a directory: its children are the entries of the directory
│       ├── entry
│       └── ...
└── ...
```

The names, the link targets, the owners and the groups stay UTF-8: they are text. A reader skips the info nodes
it does not know, as in ppak.

## Files from anywhere

Everything that [ppak checks](../PPak/README.md#files-from-anywhere) is checked, and since the contents are
compressed, also:

* a content is decompressed as a stream, and it can not be longer than its `Size`, so a small file can not
  unpack into a huge one ("gzip bomb");
* the length and the CRC are checked while the file is written; a file that does not match is deleted.

A file is read as a stream when it is packed, so only its compressed form is held in memory.
