# Promptuarium Serializer Format

This document specifies how structured values (objects, arrays, strings, numbers, booleans, binary data and null: a JSON-like value with BLOBs), with comments, are stored in a Promptuarium container.
[Promptuarium.Serializer](src/Promptuarium.Serializer/README.md) writes and reads it, mapping .NET objects to it and back (the mapping is described in its README).
The same layout is used for general objects (`PRMT:GEN1-LE`), for configurations (`PRMT:CFG1-LE`, e.g. the content of an `appsettings.json`) and for the files of applications that give them a signature of their own; only the signature differs.
It builds on the [Binary Format Specification](BINARY_FORMAT.md): the container defines the tree, this document defines the meaning of the metadata and the data of its nodes.

**Contents**

1. [Notation](#1-notation)
2. [Signature](#2-signature)
3. [Metadata tags](#3-metadata-tags)
4. [Type codes](#4-type-codes)
5. [Tree structure](#5-tree-structure)
   1. [Comments](#51-comments)
6. [Example](#6-example)
7. [Test vector](#7-test-vector)

---

## 1. Notation

* `[d: xxx]` means data with the value `xxx`, `[m: yyy]` means metadata with the value `yyy`.
* **MUST**, **MUST NOT**, **SHOULD** and **MAY** are used as in RFC 2119.
* All multi-byte numbers are **little-endian**; the `-LE` suffix of the signature says so, so that a big-endian variant could get a signature of its own.

## 2. Signature

The root node follows the [file identification convention](BINARY_FORMAT.md#10-file-identification-optional-convention):

* the root has **no data**,
* its metadata is the signature (see below),
* its children are the members of the stored object.

The root is an **implicit OBJECT**: it has no TYPE child, its children are NAME nodes (and COMMENT nodes, see [section 5.1](#51-comments)).

The signature tells the **kind** of the content; the layout is the same for every kind:

| Signature      | Kind | First bytes of the file |
|----------------|------|-------------------------|
| `PRMT:GEN1-LE` | general objects (the default of Promptuarium.Serializer) | `6C 50 52 4D 54 3A 47 45 4E 31 2D 4C 45` |
| `PRMT:CFG1-LE` | configurations (PentaGrammata: `settings.prmt`, the bundled defaults and the per-user settings, and `windows.prmt`, the window sizes) | `6C 50 52 4D 54 3A 43 46 47 31 2D 4C 45` |

The tag is the kind (`GEN`, `CFG`), the version of this layout (`1`) and the byte order (`LE`); the first byte is the control byte of the metadata chunk (`0x60 + 12`).

An application MAY sign its files with a signature of its own (1 to 15 ASCII bytes, e.g. `PRMT:PStNT1`, the forms remembered by PStudio), so that its files are told apart from other files of this layout. Such a signature names the version of the layout only if the application says so: an application that adopts a later version of this layout SHOULD change its signature too.

A reader expects one signature (the one of the kind it reads). It MUST reject a file with any other signature (including a truncated one such as `PRMT:CFG1`) and a root with data, unless the application explicitly decides otherwise for that file: it may read the file by the rules of this document anyway, or skip it (read it as an object without members, e.g. a configuration layer of another kind). Promptuarium.Serializer asks the application with `PromptuariumSerializerOptions.UnexpectedSignature`; without it, such a file is rejected.

## 3. Metadata tags

The metadata of every node (except the root) is exactly one byte, the tag:

| Tag       | Code   | Data of the node |
|-----------|--------|------------------|
| `NAME`    | `0x00` | the name of an object member, UTF-8 |
| `TYPE`    | `0x01` | one type code (see [section 4](#4-type-codes)) |
| `ITEM`    | `0x02` | empty (an array element has no name) |
| `VALUE`   | `0x03` | the value, encoded according to the type |
| `COMMENT` | `0x04` | a comment, UTF-8 (see [section 5.1](#51-comments)) |

A reader MUST reject a node with any other metadata.

## 4. Type codes

The data of a TYPE node is exactly one byte, the type code.

### 4.1 Supported types

| Type          | Code   | VALUE node | Children of the holder |
|---------------|--------|------------|------------------------|
| `NULL`        | `0x00` | none       | — |
| `BOOLEAN`     | `0x01` | 1 byte: `0x00` = false, `0x01` = true | — |
| `OBJECT`      | `0x20` | none       | NAME nodes (the members, in order) |
| `ARRAY`       | `0x21` | none       | ITEM nodes (the elements, in order) |
| `FLOAT64`     | `0x63` | 8 bytes, IEEE 754 binary64 | — |
| `UTF8_STRING` | `0x80` | UTF-8 bytes, may be empty | — |
| `BLOB`        | `0xF0` | raw bytes, may be empty | — |

Rules:

* A `BOOLEAN` value MUST be `0x00` or `0x01`.
* A `FLOAT64` value MUST NOT be NaN or ±Infinity (JSON can not represent them).
* A `UTF8_STRING` value MUST be valid UTF-8.
* A `BLOB` value is any byte sequence, taken as is (a `byte[]` in .NET); it is never re-encoded as text (e.g. base64).

### 4.2 Reserved types

These codes are reserved for later versions and are **not** supported by version 1 of the layout:

| Type       | Code   | Type       | Code   | Type       | Code   |
|------------|--------|------------|--------|------------|--------|
| `INT8`     | `0x40` | `UINT8`    | `0x50` | `FLOAT8`   | `0x60` |
| `INT16`    | `0x41` | `UINT16`   | `0x51` | `FLOAT16`  | `0x61` |
| `INT32`    | `0x42` | `UINT32`   | `0x52` | `FLOAT32`  | `0x62` |
| `INT64`    | `0x43` | `UINT64`   | `0x53` |            |        |
| `INT128`   | `0x44` | `UINT128`  | `0x54` | `FLOAT128` | `0x64` |

A reader MUST reject a reserved or unknown type code.

### 4.3 Complete list

```text
Metadata tags:

NAME        = 0x00
TYPE        = 0x01
ITEM        = 0x02
VALUE       = 0x03
COMMENT     = 0x04

Type codes:

NULL        = 0x00
BOOLEAN     = 0x01

OBJECT      = 0x20
ARRAY       = 0x21

INT8        = 0x40   (reserved)
INT16       = 0x41   (reserved)
INT32       = 0x42   (reserved)
INT64       = 0x43   (reserved)
INT128      = 0x44   (reserved)

UINT8       = 0x50   (reserved)
UINT16      = 0x51   (reserved)
UINT32      = 0x52   (reserved)
UINT64      = 0x53   (reserved)
UINT128     = 0x54   (reserved)

FLOAT8      = 0x60   (reserved)
FLOAT16     = 0x61   (reserved)
FLOAT32     = 0x62   (reserved)
FLOAT64     = 0x63
FLOAT128    = 0x64   (reserved)

UTF8_STRING = 0x80

BLOB        = 0xF0
```

## 5. Tree structure

A NAME node (an object member) or an ITEM node (an array element) is a **holder**: it holds one value.

A holder has exactly one TYPE child, as its **first** child, followed by (besides its comments, see [section 5.1](#51-comments)):

| Type | Further children |
|------|------------------|
| `UTF8_STRING`, `BLOB`, `FLOAT64`, `BOOLEAN` | exactly one VALUE node |
| `NULL` | none |
| `OBJECT` | zero or more NAME nodes |
| `ARRAY` | zero or more ITEM nodes |

So:

* `{}` is a holder with a single `TYPE OBJECT` child,
* `[]` is a holder with a single `TYPE ARRAY` child,
* arrays may be heterogeneous and may contain objects and arrays,
* the order of the members and of the elements is the order of the children,
* if an object has several members with the same name, the **last one wins** (as in `IConfiguration`); a writer SHOULD NOT write duplicate names.

TYPE and VALUE nodes have no children.

### 5.1 Comments

A COMMENT node is a note for the readers of the file (people and editors), not a part of the value:

* it MAY be a child of any holder, except its **first** child (that is the TYPE node), and of the root, anywhere; there may be several,
* it has no children, and its data MUST be valid UTF-8 (it may be empty),
* it is **not counted** by the rules above: a holder of a scalar has exactly one VALUE node besides its comments, a `NULL` holder has nothing but comments, and the members of an object and the elements of an array are its NAME and ITEM nodes,
* a reader MUST validate it, and otherwise ignore it: the comments do not change the value.

A comment in a holder is about its value (e.g. the member); a comment in the root is about the whole file. A writer SHOULD write a comment right after the TYPE node of the holder it is about (first in the root), so that it is read with the type:

```text
[m: NAME] [d: rate]
├── [m: TYPE] [d: FLOAT64]
├── [m: COMMENT] [d: The sample rate, in Hz]
└── [m: VALUE] [d: 44100]
```

Promptuarium.Serializer writes the comments of `[PromptuariumComment("...")]`: of a property in the holder of the member, of a class or a struct in the holder of its objects (in the root for the root object), unless the property has a comment of its own.

## 6. Example

```json
{
    "key1": "value1",
    "key2": 1.5,
    "key3": true,
    "key4": [ "value41", "value42" ],
    "key5": { "key51": "value51" },
    "key6": null
}
```

```text
[m: PRMT:CFG1-LE]
├── [m: NAME] [d: key1]
│   ├── [m: TYPE] [d: UTF8_STRING]
│   └── [m: VALUE] [d: value1]
├── [m: NAME] [d: key2]
│   ├── [m: TYPE] [d: FLOAT64]
│   └── [m: VALUE] [d: 1.5]
├── [m: NAME] [d: key3]
│   ├── [m: TYPE] [d: BOOLEAN]
│   └── [m: VALUE] [d: 0x01]
├── [m: NAME] [d: key4]
│   ├── [m: TYPE] [d: ARRAY]
│   ├── [m: ITEM]
│   │   ├── [m: TYPE] [d: UTF8_STRING]
│   │   └── [m: VALUE] [d: value41]
│   └── [m: ITEM]
│       ├── [m: TYPE] [d: UTF8_STRING]
│       └── [m: VALUE] [d: value42]
├── [m: NAME] [d: key5]
│   ├── [m: TYPE] [d: OBJECT]
│   └── [m: NAME] [d: key51]
│       ├── [m: TYPE] [d: UTF8_STRING]
│       └── [m: VALUE] [d: value51]
└── [m: NAME] [d: key6]
    └── [m: TYPE] [d: NULL]
```

## 7. Test vector

The canonical encoding of the example above, as a configuration (`PRMT:CFG1-LE`, 160 bytes):

```text
6c 50 52 4d 54 3a 43 46 47 31 2d 4c 45 44 6b 65
79 31 21 00 41 80 21 01 c6 76 61 6c 75 65 31 21
03 84 6b 65 79 32 21 00 41 63 21 01 c8 00 00 00
00 00 00 f8 3f 21 03 84 6b 65 79 33 21 00 41 01
21 01 c1 01 21 03 84 6b 65 79 34 21 00 41 21 21
01 e1 02 41 80 21 01 c7 76 61 6c 75 65 34 31 21
03 a1 02 41 80 21 01 c7 76 61 6c 75 65 34 32 21
03 80 84 6b 65 79 35 21 00 41 20 21 01 c5 6b 65
79 35 31 21 00 41 80 21 01 c7 76 61 6c 75 65 35
31 21 03 80 84 6b 65 79 36 21 00 41 00 21 01 00
```

A `BLOB` member `key7` with the bytes `DE AD BE EF`, appended to the example, is encoded as:

```text
[m: NAME] [d: key7]
├── [m: TYPE] [d: BLOB]
└── [m: VALUE] [d: DE AD BE EF]
```

```text
84 6b 65 79 37 21 00 41 f0 21 01 c4 de ad be ef 21 03
```

These 18 bytes are inserted before the final `00` of the test vector, which gives a 178-byte file.

A member `key8` with the value `value8` and the comment `the eighth key`:

```text
[m: NAME] [d: key8]
├── [m: TYPE] [d: UTF8_STRING]
├── [m: COMMENT] [d: the eighth key]
└── [m: VALUE] [d: value8]
```

```text
84 6b 65 79 38 21 00 41 80 21 01 ce 74 68 65 20
65 69 67 68 74 68 20 6b 65 79 21 04 c6 76 61 6c
75 65 38 21 03
```

These 37 bytes, inserted before the final `00` of the test vector, give a 197-byte file; read, it is the example with `"key8": "value8"`.

With the signature `PRMT:GEN1-LE`, the same example is the same 160 bytes, except bytes 6 to 8 (counted from 0): `47 45 4e` (`GEN`) instead of `43 46 47` (`CFG`).

