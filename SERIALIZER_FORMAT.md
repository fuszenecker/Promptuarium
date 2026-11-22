# Promptuarium Serializer Format

This document specifies how structured values (objects, arrays, strings, numbers, booleans, binary data and null: a JSON-like value with BLOBs; in version 2 also typed integers and floating point numbers, decimals, big integers, UUIDs, dates and times), with comments, are stored in a Promptuarium container.
[Promptuarium.Serializer](src/Promptuarium.Serializer/README.md) writes and reads it, mapping .NET objects to it and back (the mapping is described in its README).
The same layout is used for general objects (`PRMT:GEN1-LE`), for configurations (`PRMT:CFG1-LE`, e.g. the content of an `appsettings.json`) and for the files of applications that give them a signature of their own; only the signature differs.
It builds on the [Binary Format Specification](BINARY_FORMAT.md): the container defines the tree, this document defines the meaning of the metadata and the data of its nodes.

There are two versions of the layout (see [section 7](#7-versions)). **Version 2** adds types to version 1; a file of version 1 is a valid file of version 2, but a reader of version 1 rejects a file of version 2. Promptuarium.Serializer reads both versions, and writes the lowest version that holds a tree.

**Contents**

1. [Notation](#1-notation)
2. [Signature](#2-signature)
3. [Metadata tags](#3-metadata-tags)
4. [Type codes](#4-type-codes)
   1. [Families](#41-families)
   2. [Supported types](#42-supported-types)
   3. [Reserved types](#43-reserved-types)
   4. [Complete list](#44-complete-list)
5. [Tree structure](#5-tree-structure)
   1. [Comments](#51-comments)
6. [Values of version 2](#6-values-of-version-2)
   1. [Integers](#61-integers)
   2. [Binary floating point](#62-binary-floating-point)
   3. [Decimal](#63-decimal)
   4. [UUID](#64-uuid)
   5. [Dates and times](#65-dates-and-times)
7. [Versions](#7-versions)
8. [Example](#8-example)
9. [Test vectors](#9-test-vectors)
   1. [Version 1](#91-version-1)
   2. [Version 2](#92-version-2)

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
| `PRMT:GEN1-LE` | general objects, version 1 (the default of Promptuarium.Serializer) | `6C 50 52 4D 54 3A 47 45 4E 31 2D 4C 45` |
| `PRMT:CFG1-LE` | configurations, version 1 (PentaGrammata: `settings.prmt`, the bundled defaults and the per-user settings, and `windows.prmt`, the window sizes) | `6C 50 52 4D 54 3A 43 46 47 31 2D 4C 45` |
| `PRMT:GEN2-LE` | general objects, version 2 | `6C 50 52 4D 54 3A 47 45 4E 32 2D 4C 45` |
| `PRMT:CFG2-LE` | configurations, version 2 | `6C 50 52 4D 54 3A 43 46 47 32 2D 4C 45` |

The tag is the kind (`GEN`, `CFG`), the version of this layout (`1` or `2`, see [section 7](#7-versions)) and the byte order (`LE`); the first byte is the control byte of the metadata chunk (`0x60 + 12`).

An application MAY sign its files with a signature of its own (1 to 15 ASCII bytes, e.g. `PRMT:PSTNT1`, the forms remembered by PStudio), so that its files are told apart from other files of this layout. Such a signature names the version of the layout only if the application says so: an application that adopts a later version of this layout SHOULD change its signature too.

A reader expects the signatures of the kind it reads: the one of version 1, and the one of version 2 if it reads version 2. It MUST reject a file with any other signature (including a truncated one such as `PRMT:CFG1`) and a root with data, unless the application explicitly decides otherwise for that file: it may read the file by the rules of this document anyway, or skip it (read it as an object without members, e.g. a configuration layer of another kind). Promptuarium.Serializer asks the application with `PromptuariumSerializerOptions.UnexpectedSignature`; without it, such a file is rejected.

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

### 4.1 Families

The high nibble of a type code is its **family**; in the families of the binary numbers (`0x4_`, `0x5_`, `0x6_`) the low nibble is the **size** of the value, 2^n bytes (`INT8` = 0x40 is 1 byte, `INT128` = 0x44 is 16 bytes):

| Family | Kind | Version |
|--------|------|---------|
| `0x0_` | special values | 1 |
| `0x2_` | structures | 1 |
| `0x4_` | signed integers, 2^n bytes | 2 |
| `0x5_` | unsigned integers, 2^n bytes | 2 |
| `0x6_` | binary floating point (IEEE 754), 2^n bytes | 1 (`FLOAT64`), 2 |
| `0x7_` | arbitrary-precision numbers (decimals, big integers) | 2 |
| `0x8_` | text | 1 |
| `0x9_` | identifiers | 2 |
| `0xD_` | dates and times | 2 |
| `0xF_` | raw bytes | 1 |

The other families (`0x1_`, `0x3_`, `0xA_` ... `0xC_`, `0xE_`) are free for later versions.

### 4.2 Supported types

| Type          | Code   | Version | VALUE node | Children of the holder |
|---------------|--------|---------|------------|------------------------|
| `NULL`        | `0x00` | 1 | none       | — |
| `BOOLEAN`     | `0x01` | 1 | 1 byte: `0x00` = false, `0x01` = true | — |
| `OBJECT`      | `0x20` | 1 | none       | NAME nodes (the members, in order) |
| `ARRAY`       | `0x21` | 1 | none       | ITEM nodes (the elements, in order) |
| `INT8`        | `0x40` | 2 | 1 byte, two's complement | — |
| `INT16`       | `0x41` | 2 | 2 bytes, two's complement | — |
| `INT32`       | `0x42` | 2 | 4 bytes, two's complement | — |
| `INT64`       | `0x43` | 2 | 8 bytes, two's complement | — |
| `INT128`      | `0x44` | 2 | 16 bytes, two's complement | — |
| `UINT8`       | `0x50` | 2 | 1 byte, unsigned | — |
| `UINT16`      | `0x51` | 2 | 2 bytes, unsigned | — |
| `UINT32`      | `0x52` | 2 | 4 bytes, unsigned | — |
| `UINT64`      | `0x53` | 2 | 8 bytes, unsigned | — |
| `UINT128`     | `0x54` | 2 | 16 bytes, unsigned | — |
| `FLOAT16`     | `0x61` | 2 | 2 bytes, IEEE 754 binary16 | — |
| `FLOAT32`     | `0x62` | 2 | 4 bytes, IEEE 754 binary32 | — |
| `FLOAT64`     | `0x63` | 1 | 8 bytes, IEEE 754 binary64 | — |
| `DECIMAL`     | `0x70` | 2 | UTF-8, the canonical text of a decimal number ([6.3](#63-decimal)) | — |
| `BIGINT`      | `0x78` | 2 | 1 or more bytes, two's complement, the shortest form ([6.1](#61-integers)) | — |
| `UTF8_STRING` | `0x80` | 1 | UTF-8 bytes, may be empty | — |
| `UUID`        | `0x90` | 2 | 16 bytes, in the order of RFC 9562 ([6.4](#64-uuid)) | — |
| `DATE`        | `0xD0` | 2 | 4 bytes: days since 1970-01-01 ([6.5](#65-dates-and-times)) | — |
| `TIME`        | `0xD1` | 2 | 8 bytes: ticks since midnight | — |
| `DATETIME`    | `0xD2` | 2 | 8 bytes: ticks since 1970-01-01T00:00, without an offset | — |
| `TIMESTAMP`   | `0xD3` | 2 | 10 bytes: ticks since 1970-01-01T00:00Z, and the offset in minutes | — |
| `DURATION`    | `0xD4` | 2 | 8 bytes: ticks | — |
| `BLOB`        | `0xF0` | 1 | raw bytes, may be empty | — |

All multi-byte numbers are little-endian. Rules:

* A `BOOLEAN` value MUST be `0x00` or `0x01`.
* A value of a fixed size (every type above but `DECIMAL`, `BIGINT`, `UTF8_STRING` and `BLOB`) MUST have exactly that size.
* A `FLOAT64` value MUST NOT be NaN or ±Infinity **in version 1** (JSON can not represent them); in version 2 they are allowed ([6.2](#62-binary-floating-point)).
* A `UTF8_STRING` value MUST be valid UTF-8.
* A `BLOB` value is any byte sequence, taken as is (a `byte[]` in .NET); it is never re-encoded as text (e.g. base64).
* The values of version 2 are specified in [section 6](#6-values-of-version-2).

### 4.3 Reserved types

`FLOAT8` (`0x60`) and `FLOAT128` (`0x64`) are reserved for later versions: there are several 8-bit floating point formats, and .NET has neither type.

A reader MUST reject a reserved or unknown type code, and a reader of a file of version 1 MUST reject the type codes of version 2 ([section 7](#7-versions)).

### 4.4 Complete list

```text
Metadata tags:

NAME        = 0x00
TYPE        = 0x01
ITEM        = 0x02
VALUE       = 0x03
COMMENT     = 0x04

Type codes (the version that introduced them):

NULL        = 0x00   (1)
BOOLEAN     = 0x01   (1)

OBJECT      = 0x20   (1)
ARRAY       = 0x21   (1)

INT8        = 0x40   (2)
INT16       = 0x41   (2)
INT32       = 0x42   (2)
INT64       = 0x43   (2)
INT128      = 0x44   (2)

UINT8       = 0x50   (2)
UINT16      = 0x51   (2)
UINT32      = 0x52   (2)
UINT64      = 0x53   (2)
UINT128     = 0x54   (2)

FLOAT8      = 0x60   (reserved)
FLOAT16     = 0x61   (2)
FLOAT32     = 0x62   (2)
FLOAT64     = 0x63   (1)
FLOAT128    = 0x64   (reserved)

DECIMAL     = 0x70   (2)
BIGINT      = 0x78   (2)

UTF8_STRING = 0x80   (1)

UUID        = 0x90   (2)

DATE        = 0xD0   (2)
TIME        = 0xD1   (2)
DATETIME    = 0xD2   (2)
TIMESTAMP   = 0xD3   (2)
DURATION    = 0xD4   (2)

BLOB        = 0xF0   (1)
```

## 5. Tree structure

A NAME node (an object member) or an ITEM node (an array element) is a **holder**: it holds one value.

A holder has exactly one TYPE child, as its **first** child, followed by (besides its comments, see [section 5.1](#51-comments)):

| Type | Further children |
|------|------------------|
| `BOOLEAN`, the numbers, `DECIMAL`, `UTF8_STRING`, `UUID`, the dates and times, `BLOB` (every type but `NULL`, `OBJECT` and `ARRAY`) | exactly one VALUE node |
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

## 6. Values of version 2

### 6.1 Integers

`INT8` ... `INT128` are two's complement, `UINT8` ... `UINT128` unsigned, both little-endian, of the size of their code (1, 2, 4, 8 or 16 bytes). Every bit pattern is a valid value.

A writer chooses the type by the type of the value in its language, not by the value: e.g. a .NET `int` is always an `INT32`, even if it is 5. A reader MAY read an integer into an integer type of another size or sign if the value fits it, and into a floating point type if the value is held exactly.

`BIGINT` is an integer of any size (.NET: `BigInteger`): the encoding of the signed integers, two's complement and little-endian, but of any length (the length of the VALUE node). `INT32` -2 is `FE FF FF FF`, `BIGINT` -2 is `FE`. A value is in its **shortest form**:

* it has at least 1 byte; zero is `00`;
* its last (most significant) byte is not a needless sign extension: it is not `00` if the byte before it is below `80`, and not `FF` if the byte before it is `80` or above.

| Value | `BIGINT` |
|-------|----------|
| 0 | `00` |
| 127 | `7F` |
| 128 | `80 00` |
| -1 | `FF` |
| -128 | `80` |
| -129 | `7F FF` |
| 2^64 | `00 00 00 00 00 00 00 00 01` |

A reader MUST reject an empty value and a value that is not in its shortest form, so that every value has one encoding. The format has no limit of size; a reader that can not hold a value MUST reject it. (.NET: `value.ToByteArray()` writes the shortest form, `new BigInteger(bytes)` reads it; the form is the shortest if `GetByteCount()` is the length.)

### 6.2 Binary floating point

`FLOAT16`, `FLOAT32` and `FLOAT64` are IEEE 754 binary16, binary32 and binary64, little-endian.

* In version 2, NaN, +Infinity, -Infinity and -0.0 are allowed, so every value comes back as it was written.
* A writer MUST write a NaN as the canonical quiet NaN: `00 7E` (`FLOAT16`), `00 00 C0 7F` (`FLOAT32`), `00 00 00 00 00 00 F8 7F` (`FLOAT64`); the payload of a NaN is not kept. A reader MUST accept any NaN.
* A text format without these numbers (e.g. JSON) gets the strings `"NaN"`, `"Infinity"` and `"-Infinity"` instead.

### 6.3 Decimal

A `DECIMAL` value is the canonical text of a decimal number, in UTF-8:

* an optional `-`, the integer part, and an optional `.` followed by the fraction: `-?(0|[1-9][0-9]*)(\.[0-9]+)?`;
* no `+`, no exponent, no group separators, no spaces, no leading zeros (except the single `0` of the integer part);
* the trailing zeros of the fraction are kept: they are the scale, `12.750` is not `12.75`;
* a zero has no sign: `0`, `0.00`, never `-0`.

A reader MUST reject a text that is not in the canonical form. The format has no limit of digits; a reader that can not hold a value exactly MUST reject it (and not round it).

A text, rather than a binary form, so that every language can read it exactly (into its own decimal type, or as a string), and a reader without the type still sees the number. (.NET: `decimal.ToString(CultureInfo.InvariantCulture)` writes the canonical form; a reader parses the text and requires the value to give the same text back, because `decimal.Parse` rounds the digits a `decimal` can not hold.)

### 6.4 UUID

A `UUID` value is 16 bytes, in the order of RFC 9562, the order of the text: `00112233-4455-6677-8899-aabbccddeeff` is `00 11 22 33 44 55 66 77 88 99 AA BB CC DD EE FF`, as other languages and databases store a UUID. Any 16 bytes are a valid value (the version and the variant are not checked).

(.NET: `guid.TryWriteBytes(span, bigEndian: true, out _)` and `new Guid(span, bigEndian: true)`. This is **not** the layout of `Guid.ToByteArray()`, which has its first three groups little-endian, and which `Data.FromGuid` of the library uses.)

### 6.5 Dates and times

The dates and times count **ticks** of 100 nanoseconds from the **Unix epoch**, 1970-01-01, in the proleptic Gregorian calendar. The range is the range of .NET, from 0001-01-01T00:00:00 to 9999-12-31T23:59:59.9999999:

| Type | Size | Value | Range |
|------|------|-------|-------|
| `DATE` | 4 | INT32: days since 1970-01-01 | -719 162 (0001-01-01) to 2 932 896 (9999-12-31) |
| `TIME` | 8 | INT64: ticks since midnight, a time of a day | 0 to 863 999 999 999 |
| `DATETIME` | 8 | INT64: ticks since 1970-01-01T00:00, a date and a time **without** an offset (a local time of unknown zone) | -621 355 968 000 000 000 to 2 534 023 007 999 999 999 |
| `TIMESTAMP` | 10 | INT64: ticks since 1970-01-01T00:00Z, the **instant** in UTC; then INT16: the offset from UTC in minutes | the instant as `DATETIME`; the offset -840 to 840 (±14 hours); the local time (the instant plus the offset) in the range too |
| `DURATION` | 8 | INT64: ticks, a length of time, may be negative | every INT64 |

All little-endian. A reader MUST reject a value out of its range.

A `TIMESTAMP` is an instant with the offset it was observed at: `2026-11-22T19:30:15+01:00` is the instant `2026-11-22T18:30:15Z` and the offset 60. (.NET maps: `DateOnly` to `DATE`, `TimeOnly` to `TIME`, `DateTime` of `Kind` `Unspecified` to `DATETIME`, of `Kind` `Utc` and `Local` to `TIMESTAMP` with its offset, `DateTimeOffset` to `TIMESTAMP`, `TimeSpan` to `DURATION`.)

## 7. Versions

**Version 1** has the types `NULL`, `BOOLEAN`, `OBJECT`, `ARRAY`, `FLOAT64` (finite), `UTF8_STRING` and `BLOB`. **Version 2** has every type of [section 4.2](#42-supported-types), and allows NaN and the infinities in `FLOAT64` too.

Writing:

* A writer SHOULD sign a file with the **lowest version that holds it**: a tree with only the types of version 1, and no NaN or infinity, is signed with the signature of version 1 (`PRMT:GEN1-LE`, `PRMT:CFG1-LE`), so that a reader of version 1 still reads it; any other tree with the signature of version 2 (`PRMT:GEN2-LE`, `PRMT:CFG2-LE`).
* So a writer that writes the values of an application in the form of version 1 (e.g. an integer as `FLOAT64`, a UUID as `UTF8_STRING`) produces a file of version 1.
* A signature of an application (section 2) is written as it is: the application tells which version of the layout it means.

Reading:

* A reader of version 2 reads the files of both versions of the kind it reads.
* A file signed with the signature of version 1 MUST NOT contain a type of version 2, nor a NaN or an infinity; a reader MUST reject such a file.
* A reader of version 1 rejects a file of version 2: its signature, and its type codes.
* A reader MAY read a value of version 1 into a type of version 2 where nothing is lost, e.g. the text of a date, a time or a UUID, or a whole `FLOAT64` into an integer, so that the files of version 1 stay readable when an application moves to version 2.

## 8. Example

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

## 9. Test vectors

### 9.1 Version 1

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

### 9.2 Version 2

A general object (`PRMT:GEN2-LE`) with one member of every new kind of value:

| Member | Type | Value | VALUE node |
|--------|------|-------|------------|
| `i8` | `INT8` | -5 | `fb` |
| `i32` | `INT32` | -2 | `fe ff ff ff` |
| `i64` | `INT64` | 2^53 + 1 = 9 007 199 254 740 993 | `01 00 00 00 00 00 20 00` |
| `i128` | `INT128` | 2^64 | `00 00 00 00 00 00 00 00 01 00 00 00 00 00 00 00` |
| `u16` | `UINT16` | 0x1234 | `34 12` |
| `u64` | `UINT64` | 2^64 - 1 | `ff ff ff ff ff ff ff ff` |
| `f16` | `FLOAT16` | 1.5 | `00 3e` |
| `f32` | `FLOAT32` | 0.1 (binary32) | `cd cc cc 3d` |
| `nan` | `FLOAT64` | NaN | `00 00 00 00 00 00 f8 7f` |
| `dec` | `DECIMAL` | 12.750 | `31 32 2e 37 35 30` (`12.750`) |
| `big` | `BIGINT` | -(2^64) - 1 | `ff ff ff ff ff ff ff ff fe` |
| `id` | `UUID` | 00112233-4455-6677-8899-aabbccddeeff | `00 11 22 33 44 55 66 77 88 99 aa bb cc dd ee ff` |
| `date` | `DATE` | 2026-11-22 (20 779 days) | `2b 51 00 00` |
| `time` | `TIME` | 19:30:15.5 (702 155 000 000 ticks) | `c0 08 b3 7b a3 00 00 00` |
| `dt` | `DATETIME` | 2026-11-22T19:30:15 (17 953 758 150 000 000 ticks) | `80 fd e9 10 d9 c8 3f 00` |
| `ts` | `TIMESTAMP` | 2026-11-22T19:30:15+01:00 (the instant 17 953 722 150 000 000 ticks, offset 60) | `80 95 25 af d0 c8 3f 00 3c 00` |
| `dur` | `DURATION` | -01:30:00 (-54 000 000 000 ticks) | `00 64 59 6d f3 ff ff ff` |

Every member is a holder with a TYPE and a VALUE node, as in [section 5](#5-tree-structure):

```text
[m: PRMT:GEN2-LE]
├── [m: NAME] [d: i8]
│   ├── [m: TYPE] [d: INT8]
│   └── [m: VALUE] [d: FB]
├── [m: NAME] [d: i32]
│   ├── [m: TYPE] [d: INT32]
│   └── [m: VALUE] [d: FE FF FF FF]
...
└── [m: NAME] [d: dur]
    ├── [m: TYPE] [d: DURATION]
    └── [m: VALUE] [d: 00 64 59 6D F3 FF FF FF]
```

The canonical encoding (356 bytes):

```text
6c 50 52 4d 54 3a 47 45 4e 32 2d 4c 45 42 69 38
21 00 41 40 21 01 c1 fb 21 03 83 69 33 32 21 00
41 42 21 01 c4 fe ff ff ff 21 03 83 69 36 34 21
00 41 43 21 01 c8 01 00 00 00 00 00 20 00 21 03
84 69 31 32 38 21 00 41 44 21 01 d0 00 00 00 00
00 00 00 00 01 00 00 00 00 00 00 00 21 03 83 75
31 36 21 00 41 51 21 01 c2 34 12 21 03 83 75 36
34 21 00 41 53 21 01 c8 ff ff ff ff ff ff ff ff
21 03 83 66 31 36 21 00 41 61 21 01 c2 00 3e 21
03 83 66 33 32 21 00 41 62 21 01 c4 cd cc cc 3d
21 03 83 6e 61 6e 21 00 41 63 21 01 c8 00 00 00
00 00 00 f8 7f 21 03 83 64 65 63 21 00 41 70 21
01 c6 31 32 2e 37 35 30 21 03 83 62 69 67 21 00
41 78 21 01 c9 ff ff ff ff ff ff ff ff fe 21 03
82 69 64 21 00 41 90 21 01 d0 00 11 22 33 44 55
66 77 88 99 aa bb cc dd ee ff 21 03 84 64 61 74
65 21 00 41 d0 21 01 c4 2b 51 00 00 21 03 84 74
69 6d 65 21 00 41 d1 21 01 c8 c0 08 b3 7b a3 00
00 00 21 03 82 64 74 21 00 41 d2 21 01 c8 80 fd
e9 10 d9 c8 3f 00 21 03 82 74 73 21 00 41 d3 21
01 ca 80 95 25 af d0 c8 3f 00 3c 00 21 03 83 64
75 72 21 00 41 d4 21 01 c8 00 64 59 6d f3 ff ff
ff 21 03 00
```

A reader of version 1 rejects this file (its signature, and its type codes); with the signature `PRMT:GEN1-LE` (bytes 6 to 9 `47 45 4e 31`) it is invalid for every reader, because a file of version 1 can not hold these types. The example of [section 8](#8-example) has only types of version 1, so a writer of version 2 signs it with the signature of version 1, and writes the 160 bytes of [9.1](#91-version-1).
