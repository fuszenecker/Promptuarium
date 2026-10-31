# ppakz

`ppakz` is [`ppak`](../PPak/README.md) in a binary form: the flags of the info nodes and the kinds of the entries
are ushorts (enums, from `0x100`), not text, and the content of every file is **compressed**
(a gzip stream, with the algorithm stored next to it). The commands are the same:

```sh
ppakz pack DIRECTORY FILE                       # packs the contents of the directory
ppakz unpack FILE DIRECTORY [--strict]          # unpacks into the directory (--strict: no warnings allowed)
ppakz list FILE                                 # lists the entries, with the size and the compressed size
ppakz verify FILE                               # checks the mandatory components and the additional infos
```

`unpack`, `list` and `verify` take `--max-size=BYTES` too: the limit of the decompressed contents, together
(4 GiB by default, see below).

```sh
dotnet run --project tools/PPakZ -- pack ~/notes notes.ppakz
file -m misc/promptuarium.magic notes.ppakz     # notes.ppakz: Promptuarium container, PPAKZ1-LE
```

## Layout

The tree is the tree of ppak; the metadata of an info node is a ushort (two bytes, little-endian), the value of
`Flag`. The flags and the kinds start at `0x100`, so they never look like the one-byte tags of the serializer:

```text
root                           metadata "PRMT:PPAKZ1-LE" (the signature, BINARY_FORMAT.md 10.3)
├── entry                      data: the name (UTF-8)
│   ├── info 0x100 Type        the kind (ushort): 0x100 file, 0x101 directory, 0x102 symbolic link
│   ├── info 0x101 Content     the content of a file, compressed (by the algorithm of Compression)
│   ├── info 0x10A Compression the algorithm of the content (ushort): 0x100 gzip; gzip if it is not given
│   ├── info 0x102 Size        the length of the content before compression (VarUInt)
│   ├── info 0x103 Crc32       the CRC-32 of the content before compression (uint)
│   ├── info 0x104 Target      the target of a symbolic link (UTF-8)
│   ├── info 0x105 Owner       the name of the owner (UTF-8)
│   ├── info 0x106 Group       the name of the group (UTF-8)
│   ├── info 0x107 Mode        the permissions (ushort, e.g. 0644)
│   ├── info 0x108 Modified    the time of the last modification (DateTimeOffset)
│   └── info 0x109 Entries     for a directory: its children are the entries of the directory
│       ├── entry
│       └── ...
└── ...
```

The names, the link targets, the owners and the groups stay UTF-8: they are text. A reader skips the info nodes
it does not know, as in ppak.

## Mandatory and additional

As in ppak ([PPAK_FORMAT.md](../PPak/PPAK_FORMAT.md), sections 5 to 7), **only the names and the contents are
mandatory**: the `Content` of a file (with its `Compression`: an unknown algorithm is an error, a missing one
means gzip), the `Entries` of a directory, and for a symbolic link, which has no content, its `Type` (`Symlink`) and
its `Target`. The contents decide the kind of an entry, whatever its `Type` says. Everything else (`Type`, `Size`,
`Crc32`, `Owner`, `Group`, `Mode`, `Modified`) is additional information:

* an **error** in the mandatory components stops ppakz before anything is written: a wrong signature, an invalid or
  repeated name, an entry with both a content and entries (or neither), an unknown compression, a content that does not
  decompress;
* a **discrepancy** in the additional information (a `Size` or a `Crc32` that does not match the decompressed
  content, a `Type` that contradicts it, a value of a wrong length, invalid UTF-8, mode bits above `07777`, a repeated
  flag) is a **warning**, written to the standard error with the path of the entry; by default the content is
  unpacked as it is;
* `unpack --strict` treats the warnings of the archive as errors, and then unpacks nothing;
* an info whose flag is unknown, or whose metadata is not two bytes long, is skipped without a warning (an extension).

```text
$ ppakz verify damaged.ppakz
warning: /a.txt: CRC mismatch: 363A30DF stored, 363A3020 computed.
1 warnings
```

The exit codes are those of ppak: `verify` is 0 only without warnings, `unpack` is 0 with warnings too (1 with
`--strict`), an error is 1, the usage 2.

## Files from anywhere

Everything that [ppak checks](../PPak/README.md#files-from-anywhere) is checked, and since the contents are
compressed, also:

* every content is decompressed (as a stream, into nothing) while the archive is checked, before anything is
  written, so the lengths and the CRCs are compared with the real contents;
* since `Size` is only additional, it is not the limit: the decompressed contents can not be longer than
  `--max-size` together (4 GiB by default), so a small file can not unpack into a huge one ("gzip bomb"); a longer
  archive is an error.

A file is read as a stream when it is packed, so only its compressed form is held in memory.
