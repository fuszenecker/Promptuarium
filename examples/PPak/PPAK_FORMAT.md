# ppak Format Specification (PPak1-LE)

This document specifies **PPak1-LE**, the format of the archives of [`ppak`](README.md): a directory tree (files,
directories, symbolic links) with the sizes, the CRC-32s, the permissions, the owners, the groups and the times of
its entries, in one Promptuarium container. It builds on the [Binary Format Specification](../../BINARY_FORMAT.md):
the container defines the tree, this document defines the meaning of its nodes.

**The only mandatory components of an archive are the names and the contents.** Everything else (the type, the size,
the CRC-32, the target of a link, the owner, the group, the permissions, the time) is **additional information**: a
reader uses it when it can, but the names and the contents decide what the archive holds. A discrepancy between an
additional information and the mandatory components (e.g. a CRC-32 that does not match the content) is **not an
error, but it MUST be reported as a warning**.

**Contents**

1. [Notation](#1-notation)
2. [Overview](#2-overview)
3. [Signature](#3-signature)
4. [Entries and names](#4-entries-and-names)
5. [Contents: the kind of an entry](#5-contents-the-kind-of-an-entry)
6. [Additional information](#6-additional-information)
   1. [type](#61-type)
   2. [size](#62-size)
   3. [crc32](#63-crc32)
   4. [target](#64-target)
   5. [owner and group](#65-owner-and-group)
   6. [mode](#66-mode)
   7. [modified](#67-modified)
   8. [Extensions](#68-extensions)
7. [Reading: errors and warnings](#7-reading-errors-and-warnings)
8. [Writing](#8-writing)
9. [Security considerations](#9-security-considerations)
10. [Example](#10-example)
11. [Test vector](#11-test-vector)

---

## 1. Notation

* `[d: xxx]` means data with the value `xxx`, `[m: yyy]` means metadata with the value `yyy`.
* **MUST**, **MUST NOT**, **SHOULD**, **SHOULD NOT** and **MAY** are used as in RFC 2119.
* All multi-byte numbers are **little-endian** (as written by the `Data` class of the library); the `-LE` suffix of the
  signature says so, so that a big-endian variant could get a signature of its own.
* A **flag** is the metadata of an info node: ASCII, case-sensitive (`content`, not `Content`).

## 2. Overview

Every level of the directory tree is **two levels** of the Promptuarium tree: an **entry** node, whose data is the
name, and below it the **info** nodes of the entry, each with a flag in its metadata and the value in its data. The
entries of a directory are the children of its `entries` info node.

```text
root                         [m: "PRMT:PPak1-LE"]                   the signature (section 3)
├── entry                    [d: name]                              section 4
│   ├── info                 [m: "content"]  [d: the bytes]         mandatory for a file (section 5)
│   ├── info                 [m: "type"]     [d: "file"]            additional (section 6)
│   ├── info                 [m: "size"]     [d: ulong]
│   ├── info                 [m: "crc32"]    [d: uint]
│   ├── info                 [m: "owner"]    [d: UTF-8]
│   ├── info                 [m: "group"]    [d: UTF-8]
│   ├── info                 [m: "mode"]     [d: ushort]
│   └── info                 [m: "modified"] [d: DateTimeOffset]
├── entry                    [d: name]
│   ├── info                 [m: "entries"]                         mandatory for a directory (section 5)
│   │   ├── entry            [d: name]                              the entries of the directory
│   │   └── ...
│   └── ...                                                         its additional information
└── ...
```

| Component | Where | Status |
|-----------|-------|--------|
| signature | metadata of the root | mandatory |
| name | data of an entry | mandatory |
| `content` | info of a file | mandatory (it decides that the entry is a file) |
| `entries` | info of a directory | mandatory (it decides that the entry is a directory) |
| `type`, `size`, `crc32`, `target`, `owner`, `group`, `mode`, `modified` | infos of an entry | additional |
| any other flag | infos of an entry | extension, ignored by a reader that does not know it |

## 3. Signature

The root follows the [file identification convention](../../BINARY_FORMAT.md#10-file-identification-optional-convention):

* the root MUST have **no data**;
* its metadata MUST be the 13 ASCII bytes `PRMT:PPak1-LE` (case-sensitive), so every archive starts with
  `6D 50 52 4D 54 3A 50 50 61 6B 31 2D 4C 45`, and `file` (with [`misc/promptuarium.magic`](../../misc/promptuarium.magic))
  shows it as `Promptuarium container, PPak1-LE`;
* its children are the entries of the top directory (the directory that was packed is not an entry itself).

The `1` of `PPak1-LE` changes only if the meaning of an existing component changes; new flags do not need a new version
(section 6.8).

## 4. Entries and names

An entry is a file, a directory or a symbolic link. Its **data is its name**, in UTF-8; its metadata SHOULD be empty
(a reader ignores it).

A name MUST:

* be valid UTF-8,
* not be empty, `.` or `..`,
* not contain `/`, `\` or NUL (U+0000),
* be unique among the entries of its directory (compared byte by byte, so `a` and `A` are different names).

The names are mandatory: a name that breaks these rules is an **error** (section 7). The path of an entry is the
names from the root, joined by `/`.

## 5. Contents: the kind of an entry

The contents are mandatory, and they decide the kind of an entry, whatever its `type` says:

| The entry has | Kind |
|---------------|------|
| a `content` info, no `entries` info | **file**: the data of the `content` info is its content (any bytes, possibly none) |
| an `entries` info, no `content` info | **directory**: the children of the `entries` info are its entries (possibly none) |
| neither, but a `type` of `symlink` and a valid `target` | **symbolic link** (section 6.4) |
| both | error |
| neither (and it is not a symbolic link) | error |

* The data of the `entries` info SHOULD be empty (a reader ignores it).
* An info node other than `entries` SHOULD have no children; a reader ignores them, with a warning.
* If a flag is repeated in an entry, the first one is used; the repetition is a warning (for `content` and `entries`
  too).

A symbolic link is the only kind that depends on additional information: it has no content of its own. A reader that
can not create symbolic links skips them, with a warning.

## 6. Additional information

The additional information describes an entry; it does not define it. Every item is optional. A reader:

* uses an item that is valid and applies to the kind of the entry;
* ignores an item that is not valid (a wrong length, invalid UTF-8, a value out of range) or that belongs to another
  kind, **with a warning**;
* reports **a warning** if a valid item contradicts the mandatory components (a `type`, `size` or `crc32` that does
  not match);
* by default, still uses the mandatory components: the name, and the content as it is.

| Flag | Applies to | Value | Length |
|------|-----------|-------|--------|
| `type` | every entry | `file`, `directory` or `symlink` (UTF-8) | 4, 9 or 7 |
| `size` | file | the length of the content in bytes (ulong) | 8 |
| `crc32` | file | the CRC-32 of the content (uint) | 4 |
| `target` | symbolic link | the target of the link (UTF-8) | any |
| `owner` | every entry | the name of the owner (UTF-8) | any |
| `group` | every entry | the name of the group (UTF-8) | any |
| `mode` | file, directory | the permissions (ushort, at most `07777`) | 2 |
| `modified` | every entry | the time of the last modification (DateTimeOffset) | 16 |

### 6.1 type

`file`, `directory` or `symlink`, in UTF-8. For a file and a directory it is a redundant statement of the kind:

* a type that is not one of the three is a warning (`unknown type`), and the kind is the one of section 5;
* a type that is not the kind of section 5 is a warning (`the type is "directory", but it is a file by its content`),
  and the kind of section 5 is used.

For a symbolic link the type is needed (section 5).

### 6.2 size

The length of the content in bytes, an unsigned 64-bit integer (8 bytes). It lets a tool show the size without
measuring the content. A size that is not 8 bytes long, or that is not the length of the content, is a warning.

### 6.3 crc32

The CRC-32 of the content, an unsigned 32-bit integer (4 bytes, little-endian). It is the CRC-32 of IEEE 802.3 (as in
zip, gzip and PNG):

| Parameter | Value |
|-----------|-------|
| polynomial | `0x04C11DB7` (reflected: `0xEDB88320`) |
| initial value | `0xFFFFFFFF` |
| input and output reflected | yes |
| final XOR | `0xFFFFFFFF` |
| check value (`123456789`) | `0xCBF43926` |

The CRC-32 of an empty content is `0x00000000`. A CRC-32 that is not 4 bytes long, or that does not match the content,
is a warning; by default the content is unpacked anyway (`ppak unpack --strict` refuses it, section 7).

### 6.4 target

The target of a symbolic link, in UTF-8, written as the link holds it (relative or absolute; it is not resolved, and
it may point outside the archive or nowhere). A target on an entry that is not a symbolic link is ignored, with a
warning.

### 6.5 owner and group

The **names** (not the numeric ids, which differ between systems) of the owner and of the group, in UTF-8. A reader
sets them only where it can (on Linux, as root, with `lchown`, so for a symbolic link the link itself); otherwise it
reports that they were not restored (`ppak` does it in one warning). A name that is not known on the system is a
warning, and that id is left unchanged.

### 6.6 mode

The Unix permissions, an unsigned 16-bit integer (2 bytes), with the bits of POSIX:

| Bits (octal) | Meaning |
|--------------|---------|
| `04000` | set-user-ID |
| `02000` | set-group-ID |
| `01000` | sticky |
| `0400`, `0200`, `0100` | read, write, execute by the owner |
| `0040`, `0020`, `0010` | read, write, execute by the group |
| `0004`, `0002`, `0001` | read, write, execute by the others |

The bits above `07777` MUST be zero; if they are not, they are ignored, with a warning (the file type is not in the
mode, it is the kind of section 5). The mode of a symbolic link means nothing: a writer SHOULD NOT write it, and a
reader ignores it. On a system without Unix permissions (Windows) a reader ignores the mode.

### 6.7 modified

The time of the last modification, a `DateTimeOffset` of .NET in 16 bytes:

| Offset | Length | Value |
|--------|--------|-------|
| 0 | 8 | signed 64-bit integer: the local clock time, in ticks (100 ns) since 0001-01-01 00:00:00 |
| 8 | 8 | signed 64-bit integer: the offset from UTC, in ticks |

The time in UTC is the first value minus the second. `ppak` writes the time in UTC (offset 0): 2026-10-31 12:00:00 UTC
is `00 20 57 7D 46 37 DF 08` `00 00 00 00 00 00 00 00`. A value that is not 16 bytes long, or that is out of the range of
`DateTimeOffset` (the offset is at most ±14 hours, in whole minutes), is ignored, with a warning. The time of a
symbolic link is not restored by `ppak` (.NET can not set it without following the link).

### 6.8 Extensions

A reader MUST skip the info nodes whose flag it does not know, **without a warning**: they are the extensions of the
format (e.g. a hash, extended attributes, access control lists, numeric ids), and they can be added without a new
version. An extension MUST NOT change the meaning of the components of this document; if a future version must, it
gets a new signature (`PRMT:PPak2-LE`). An extension SHOULD be additional information too: an archive is still
unpacked correctly by a reader that does not know it.

## 7. Reading: errors and warnings

A reader SHOULD check the whole archive before it writes anything, so that an error does not leave half an archive
unpacked. `ppak` loads the archive with `LoadOptions.MaxBytes` set to the length of the file, checks it, and only then
unpacks it.

**Errors** are in the mandatory components; the archive is refused:

| Error | Example message of `ppak` |
|-------|---------------------------|
| the root has data, or its metadata is not `PRMT:PPak1-LE` | `Not a ppak file: the signature is not "PRMT:PPak1-LE".` |
| a name that is not valid UTF-8 | `/docs/: a name that is not valid UTF-8.` |
| an empty name, `.`, `..`, or a name with `/`, `\` or NUL | `/: invalid name "../evil".` |
| a name repeated in a directory | `/a.txt: the name is repeated in the directory.` |
| an entry with both `content` and `entries` | `/a.txt: both a content and entries.` |
| an entry with neither (and not a symbolic link) | `/a.txt: neither a content nor entries (nor a symbolic link with a target).` |
| the container itself is broken | the error of the library (`PromptuariumException`) |

**Warnings** are in the additional information, or in what can not be done on the system; they MUST be reported (each
with the path of the entry), and by default the archive is still unpacked, by its mandatory components:

| Warning | Then |
|---------|------|
| a `type` that is unknown, or that is not the kind by the contents | the kind by the contents is used |
| a `size` or a `crc32` that does not match the content | the content is used as it is |
| a value of a wrong length, invalid UTF-8, out of range | the item is ignored |
| `mode` bits above `07777` | they are ignored |
| `size` or `crc32` on a directory or a link, `target` on a file or a directory | the item is ignored |
| a repeated flag | the first one is used |
| an info node (other than `entries`) with children | the children are ignored |
| a symbolic link that can not be created | it is skipped |
| an owner or a group that is not known, or can not be set | it is left unchanged |

A reader MAY offer a **strict** mode, in which a warning in the archive is an error, and nothing is written
(`ppak unpack --strict`). What can not be done on the system (the owners without root) is a warning in both modes.

## 8. Writing

A writer:

* MUST write the signature (section 3), the names (section 4) and the contents (section 5): a `content` info for every
  file (also an empty one), an `entries` info for every directory (also an empty one);
* SHOULD write every additional information it knows, correctly; at least `type`, `size` and `crc32`, so that the
  archive can be listed and checked without reading the contents;
* MUST NOT write a flag twice in an entry, or children under an info node other than `entries`;
* SHOULD write the entries of a directory in the order of their names (ordinal, byte by byte), so that the same tree
  always gives the same archive (`ppak` does); a reader MUST NOT depend on the order, neither of the entries nor of the
  infos.

`ppak pack` writes, for every entry, in this order: `type`; for a file `content`, `size` and `crc32`; for a link
`target`; then `owner` and `group` (on Linux), `mode` (not for a link, not on Windows) and `modified`; and for a
directory `entries`.

## 9. Security considerations

An archive may come from anywhere:

* The rules of the names (section 4) guarantee that every entry is written inside the target directory. A reader
  MUST check them before it writes anything.
* A symbolic link may point anywhere. The archive itself never writes through a link it creates (the names are unique
  in a directory, and a link has no entries), but a link that is **already** in the target directory would be followed.
  A reader SHOULD unpack into a new or empty directory (or refuse to follow existing links).
* The size of the tree is limited by the length of the file (`LoadOptions.MaxBytes`); `size` is not trusted for
  memory allocation, the length of the content is.
* The set-user-ID and set-group-ID bits, and the owners, are restored only as they are in the archive; a reader that
  runs as root SHOULD offer not to restore them for an untrusted archive.
* The CRC-32 detects accidental corruption, not a deliberate change: it is not a signature.

## 10. Example

A directory with `hi.txt` (`hi`) and an empty directory `docs` with an empty file `empty`, with only the mandatory
components, and `size` and `crc32` for `hi.txt`:

```text
root                    [m: "PRMT:PPak1-LE"]
├── entry               [d: "hi.txt"]
│   ├── info            [m: "content"]  [d: "hi"]
│   ├── info            [m: "size"]     [d: 02 00 00 00 00 00 00 00]   2 bytes
│   └── info            [m: "crc32"]    [d: AC 2A 93 D8]               0xD8932AAC
└── entry               [d: "docs"]
    └── info            [m: "entries"]
        └── entry       [d: "empty"]
            └── info    [m: "content"]                                 an empty file
```

`ppak list` shows it as:

```text
?          ?:?                           2 ?  hi.txt
?          ?:?                           0 ?  docs
?          ?:?                           0 ?  docs/empty
```

(`?` is an additional information that the archive does not have.)

## 11. Test vector

The archive of section 10, 85 bytes (the chunks as `pedit chunks` shows them, [BINARY_FORMAT.md](../../BINARY_FORMAT.md)):

```text
6D 50 52 4D 54 3A 50 50 61 6B 31 2D 4C 45   Down, metadata, 13:   the signature "PRMT:PPak1-LE" (root)
46 68 69 2E 74 78 74                        Down, data, 6:        entry "hi.txt"
42 68 69                                    Down, data, 2:        info: "hi"
27 63 6F 6E 74 65 6E 74                     Append, metadata, 7:    "content"
C8 02 00 00 00 00 00 00 00                  New, data, 8:         info: 2
24 73 69 7A 65                              Append, metadata, 4:    "size"
C4 AC 2A 93 D8                              New, data, 4:         info: 0xD8932AAC
25 63 72 63 33 32                           Append, metadata, 5:    "crc32"
84 64 6F 63 73                              Up, data, 4:          entry "docs"
67 65 6E 74 72 69 65 73                     Down, metadata, 7:    info "entries"
45 65 6D 70 74 79                           Down, data, 5:        entry "empty"
67 63 6F 6E 74 65 6E 74                     Down, metadata, 7:    info "content" (no data: an empty file)
00                                          terminator
```

```text
00000000: 6d50 524d 543a 5050 616b 312d 4c45 4668  mPRMT:PPak1-LEFh
00000010: 692e 7478 7442 6869 2763 6f6e 7465 6e74  i.txtBhi'content
00000020: c802 0000 0000 0000 0024 7369 7a65 c4ac  .........$size..
00000030: 2a93 d825 6372 6333 3284 646f 6373 6765  *..%crc32.docsge
00000040: 6e74 7269 6573 4565 6d70 7479 6763 6f6e  ntriesEemptygcon
00000050: 7465 6e74 00                             tent.
```

`ppak verify` says `OK`, and `ppak unpack` creates `hi.txt` and `docs/empty`.
