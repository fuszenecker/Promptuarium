# ppak

`ppak` packs a directory tree into a single Promptuarium file, and unpacks it: the files, the directories and the
symbolic links, with the size and the CRC-32 of every file, the permissions, the owners, the groups and the times.
The format, **PPAK1**, is specified in [PPAK_FORMAT.md](PPAK_FORMAT.md).

## Usage

```sh
ppak pack DIRECTORY FILE                 # packs the contents of the directory (not the directory itself)
ppak unpack FILE DIRECTORY [--strict]    # unpacks into the directory (created if it does not exist)
ppak list FILE                           # lists the entries, like ls -l
ppak verify FILE                         # checks the archive, without unpacking it
```

From the repository:

```sh
dotnet run --project examples/PPak -- pack ~/notes notes.ppak
dotnet run --project examples/PPak -- list notes.ppak
file -m misc/promptuarium.magic notes.ppak      # notes.ppak: Promptuarium container, PPAK1
```

```text
-rw-r--r-- robert:robert              1,204 2026-10-30 18:12  todo.md
drwxr-x--- robert:robert                  0 2026-10-31 09:40  work
-rw-r----- robert:robert             12,877 2026-10-31 09:40  work/plan.md
lrwxrwxrwx robert:robert                  0 2026-10-31 09:41  latest -> work/plan.md
```

`list` shows `?` for what the archive does not have (e.g. the owner of an archive packed on Windows, or the mode of a
link).

## What is mandatory, and what is not

**Only the names and the contents are mandatory.** Everything else (type, size, CRC-32, owner, group, permissions,
time, the target of a link) is additional information:

* an **error** in the mandatory components (the signature, a name that is invalid or repeated, an entry without a
  content and without entries) stops `ppak`, before anything is written;
* a **discrepancy** in the additional information (a CRC-32 or a size that does not match the content, a type that
  contradicts it, a value that is not valid) is a **warning**: it is always written to the standard error, with the
  path of the entry, and by default the content is unpacked as it is;
* `unpack --strict` treats the warnings of the archive as errors, and then unpacks nothing.

```text
$ ppak verify damaged.ppak
warning: /a.txt: the size is 6 bytes, but the content is 7 bytes long.
warning: /a.txt: CRC mismatch: 363A3020 stored, 42ECE232 computed.
2 warnings

$ ppak unpack damaged.ppak out --strict
warning: /a.txt: the size is 6 bytes, but the content is 7 bytes long.
warning: /a.txt: CRC mismatch: 363A3020 stored, 42ECE232 computed.
2 warnings, nothing was unpacked (--strict).
```

What can not be done on the system is a warning too: the owners are restored only when `ppak` runs as root on Linux
(otherwise one warning says how many were not restored), and a user or a group that the system does not know is left
unchanged.

An archive with only the mandatory components (e.g. written by another tool, see the example in
[PPAK_FORMAT.md](PPAK_FORMAT.md#10-example)) is valid: `verify` says `OK`, and `unpack` creates the files and the
directories with the default permissions and the current time.

| Command | Exit code |
|---------|-----------|
| `pack`, `list` | 0, or 1 on an error |
| `unpack` | 0 (also with warnings), 1 on an error, or with warnings and `--strict` |
| `verify` | 0 if there is no warning, 1 if there are warnings or an error |
| anything else | 2 (the usage) |

## Files from anywhere

A ppak file may come from anywhere, so `ppak` loads it carefully:

* `LoadOptions.MaxBytes` is the length of the file, so a broken file can not make the loader read more;
* the whole archive is checked before anything is written: the signature, the names (not empty, `.` or `..`, no `/`,
  `\` or NUL, unique in a directory, so no entry is written outside the target directory), the contents, and the
  additional information;
* unpack into a new or empty directory: a symbolic link that is already there would be followed.

The contents are held in memory, so a file can be at most about 2 GB.

## The code

| File | What it does |
|------|--------------|
| [PPakFormat.cs](PPakFormat.cs) | the flags, and reading the values (`null` for a value that is not valid) |
| [Packer.cs](Packer.cs) | a directory to a tree |
| [Checker.cs](Checker.cs) | the errors and the warnings of a loaded tree (PPAK_FORMAT.md, 7) |
| [Unpacker.cs](Unpacker.cs) | a checked tree to a directory |
| [UnixOwner.cs](UnixOwner.cs) | the owner and the group on Linux (`statx`, `lchown`) |

[PPakZ](../PPakZ/README.md) is the same in a binary form (`PRMT:PPAKZ1`, with compressed contents); it is a different
format.
