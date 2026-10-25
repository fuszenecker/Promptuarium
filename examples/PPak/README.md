# ppak

`ppak` packs a directory tree into a single Promptuarium file, and unpacks it: the contents, the permissions,
the owners, the times and the symbolic links, with a CRC-32 of every file.

```sh
ppak pack DIRECTORY FILE      # packs the contents of the directory
ppak unpack FILE DIRECTORY    # unpacks into the directory
ppak list FILE                # lists the entries, like ls -l
ppak verify FILE              # checks the CRCs
ppak                          # packs and unpacks a sample directory, and compares the result
```

```sh
dotnet run --project examples/PPak -- pack ~/notes notes.ppak
file -m misc/promptuarium.magic notes.ppak      # notes.ppak: Promptuarium container, PPAK1
```

## Layout

Every level of the directory tree is **two levels** of the Promptuarium tree: an **entry** node with the name,
and below it the **info** nodes of the entry. An info node has a flag in its metadata, and the value in its data.

```text
root                         metadata "PRMT:PPAK1" (the signature, BINARY_FORMAT.md 10.3)
├── entry                    data: the name (UTF-8)
│   ├── info "type"          "file", "directory" or "symlink" (UTF-8)
│   ├── info "content"       the content of a file
│   ├── info "crc32"         the CRC-32 of the content (uint)
│   ├── info "target"        the target of a symbolic link (UTF-8)
│   ├── info "owner"         the name of the owner (UTF-8)
│   ├── info "group"         the name of the group (UTF-8)
│   ├── info "mode"          the permissions (ushort, e.g. 0644)
│   ├── info "modified"      the time of the last modification (DateTimeOffset)
│   └── info "entries"       for a directory: its children are the entries of the directory
│       ├── entry
│       └── ...
└── ...
```

The values are written with the `Data` class (little-endian). A reader skips the info nodes it does not know,
so new flags (e.g. a hash, extended attributes) can be added without a new version; `PPAK1` changes only if the
meaning of an existing flag changes.

## Files from anywhere

A ppak file may come from anywhere, so `ppak` loads it carefully:

* `LoadOptions.MaxBytes` is the length of the file, so a broken file can not make the loader read more;
* the signature is checked before anything is written;
* a name can not be empty, `.`, `..`, or contain `/`, `\` or NUL, and it can not repeat in a directory, so no file
  is written outside the target directory;
* the CRC of every file is checked before it is written.

The owners are restored only when `ppak` runs as root (on Linux, with `lchown`); otherwise they are kept in the
file, and shown by `list`. The contents are held in memory, so a file can be at most about 2 GB.
