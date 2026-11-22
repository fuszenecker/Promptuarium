# Plan: version 2 of the serializer format

Status: **specified, not implemented**. The format of version 2 (the type codes, the values, the versions and a
test vector) is specified in [SERIALIZER_FORMAT.md](SERIALIZER_FORMAT.md), which is the reference; this document keeps
the decisions and the plan of the implementation (the serializer, pcfg, PStudio, promptuarium-js), and is removed
when it is done.

**Contents**

1. [Goals](#1-goals)
2. [Decisions](#2-decisions)
3. [Type codes](#3-type-codes)
4. [Integers](#4-integers)
5. [Binary floating point](#5-binary-floating-point)
6. [Decimal](#6-decimal)
7. [Identifiers: UUID](#7-identifiers-uuid)
8. [Dates and times](#8-dates-and-times)
9. [Versions, reading and writing](#9-versions-reading-and-writing)
10. [The rest of the ecosystem](#10-the-rest-of-the-ecosystem)
11. [Order of the work](#11-order-of-the-work)
12. [Open questions](#12-open-questions)

---

## 1. Goals

Version 1 has one number type (`FLOAT64`) and stores dates, times and identifiers as text. So:

* a `long` or a `ulong` beyond 2^53 and a `decimal` with more than 15 digits can not be stored (since 8.3.0 they are
  rejected when written, before that they were stored changed);
* a `float` is stored as 8 bytes, and the type of a number (an `int`, a `byte`) is lost in the file;
* `Guid`, `DateTimeOffset`, `DateTime`, `DateOnly`, `TimeOnly` and `TimeSpan` are text: readable, but longer than
  their bytes, and a reader without the type sees a string, not a date or an identifier.

Version 2 gives every one of them a type of its own, exactly (every value comes back as it was written).

## 2. Decisions

| | Decision |
|---|---|
| Signature | `PRMT:GEN2-LE` and `PRMT:CFG2-LE`: a reader of version 1 must reject the new type codes, and a later version of the layout should change the signature (SERIALIZER_FORMAT.md, 2) |
| Readers | read both versions; a version 1 file is read as today |
| Writers | write **version 2**, there is no option for version 1. A property is written in the form of version 1 with `[PromptuariumType]` (e.g. `Float64` on an `int`, `Utf8String` on a `Guid`), or by a type of version 1 (a `double`, a `string`); see [9](#9-versions-reading-and-writing) |
| The type of a value | follows the .NET type, not the value: an `int` is always `INT32`, even if it is 5. A type tells what the value is; choosing the smallest code that holds the value would save a few bytes, but lose that |
| NaN and ±Infinity | **allowed** in version 2 (`FLOAT16`, `FLOAT32`, `FLOAT64`), so every value comes back, `-0.0` too; exported to JSON, they are the strings `"NaN"`, `"Infinity"`, `"-Infinity"` (see [10](#10-the-rest-of-the-ecosystem)) |
| `decimal` | `DECIMAL`, the **canonical text** of the number with a type of its own (not a binary form), see [6](#6-decimal) |
| JSON import (pcfg) | every number stays `FLOAT64`: a JSON number has no type of its own |
| Dates and times | **all of them** get a type: `DATE`, `TIME`, `DATETIME`, `TIMESTAMP`, `DURATION` |
| `BigInteger` | `BIGINT` (0x78): two's complement, little-endian, the shortest form (the encoding of the `INTn` types, of any length) |
| `Version`, `Uri`, `IPAddress`, `IPNetwork` | stay text (their text is their natural form) |

## 3. Type codes

The codes of version 1 already follow a pattern: the **high nibble is the family**, and in the number families the
**low nibble is the size**, 2^n bytes (`INT8` = 0x40, 1 byte; `INT16` = 0x41, 2 bytes; ... `INT128` = 0x44, 16
bytes). Version 2 keeps the pattern, and gives every new kind of value a family of its own:

| Family | Kind | Codes |
|--------|------|-------|
| `0x0_` | special values | `NULL` 0x00, `BOOLEAN` 0x01 |
| `0x2_` | structures | `OBJECT` 0x20, `ARRAY` 0x21 |
| `0x4_` | signed integers, 2^n bytes | `INT8` 0x40 ... `INT128` 0x44 |
| `0x5_` | unsigned integers, 2^n bytes | `UINT8` 0x50 ... `UINT128` 0x54 |
| `0x6_` | binary floating point (IEEE 754), 2^n bytes | `FLOAT16` 0x61, `FLOAT32` 0x62, `FLOAT64` 0x63 |
| `0x7_` | **new:** arbitrary-precision numbers | `DECIMAL` 0x70 (canonical text), `BIGINT` 0x78 (two's complement, any length) |
| `0x8_` | text | `UTF8_STRING` 0x80 |
| `0x9_` | **new:** identifiers | `UUID` 0x90 |
| `0xD_` | **new:** dates and times | `DATE` 0xD0, `TIME` 0xD1, `DATETIME` 0xD2, `TIMESTAMP` 0xD3, `DURATION` 0xD4 |
| `0xF_` | raw bytes | `BLOB` 0xF0 |

* The arbitrary-precision numbers are a family of their own; their values have any length, so the size rule of the
  binary number families does not apply to it.
* The identifiers are a family of their own, so that others (e.g. a ULID) can follow the UUID without mixing with
  other kinds.
* The dates and times are `0xD_` (*D* for date), in the order of their content: a day, a time of a day, both, both
  with an offset, a length of time.
* The free families (`0x1_`, `0x3_`, `0xA_` ... `0xC_`, `0xE_`) stay free for later versions.

`FLOAT8` (0x60) and `FLOAT128` (0x64) stay reserved: .NET has no such types, and there are several 8-bit floating
point formats.

## 4. Integers

| Type | Code | Value | .NET |
|------|------|-------|------|
| `INT8` | 0x40 | 1 byte, two's complement | `sbyte` |
| `INT16` | 0x41 | 2 bytes, two's complement, little-endian | `short` |
| `INT32` | 0x42 | 4 bytes, two's complement, little-endian | `int` |
| `INT64` | 0x43 | 8 bytes, two's complement, little-endian | `long`, `nint` |
| `INT128` | 0x44 | 16 bytes, two's complement, little-endian | `Int128` |
| `UINT8` | 0x50 | 1 byte | `byte` |
| `UINT16` | 0x51 | 2 bytes, little-endian | `ushort` |
| `UINT32` | 0x52 | 4 bytes, little-endian | `uint` |
| `UINT64` | 0x53 | 8 bytes, little-endian | `ulong`, `nuint` |
| `UINT128` | 0x54 | 16 bytes, little-endian | `UInt128` |

* A value of a wrong size is rejected.
* Read into `object`: `INT8` ... `INT64` and `UINT8` ... `UINT32` give a `long`, `UINT64` a `ulong`, `INT128` and
  `UINT128` an `Int128` and a `UInt128`.
* Enums stay names (`UTF8_STRING`); storing them as numbers can be an option later. A `char` stays text.

## 5. Binary floating point

| Type | Code | Value | .NET |
|------|------|-------|------|
| `FLOAT16` | 0x61 | 2 bytes, IEEE 754 binary16, little-endian | `Half` |
| `FLOAT32` | 0x62 | 4 bytes, IEEE 754 binary32, little-endian | `float` |
| `FLOAT64` | 0x63 | 8 bytes, IEEE 754 binary64, little-endian | `double` |

* NaN, +Infinity, -Infinity and -0.0 are allowed in version 2 (version 1 still forbids NaN and the infinities).
* A NaN is written as the canonical quiet NaN of its size; its payload is not kept.

## 6. Decimal

| Type | Code | Value | .NET |
|------|------|-------|------|
| `DECIMAL` | 0x70 | UTF-8 text, the canonical form of the number (below) | `decimal` |

The canonical form:

* an optional `-`, the integer part, and an optional `.` followed by the fraction: `-?(0|[1-9][0-9]*)(\.[0-9]+)?`;
* no `+`, no exponent, no group separators, no leading zeros (except the single `0` of the integer part), no spaces;
* the trailing zeros of the fraction are kept: they are the scale, `12.750` is not `12.75`;
* a zero has no sign (`0`, `0.00`; never `-0`).

Rules:

* A reader rejects a text that is not in the canonical form.
* The format has no limit of digits; a reader that can not hold the value exactly rejects it. .NET holds 28–29
  significant digits and a scale up to 28: `decimal.ToString(CultureInfo.InvariantCulture)` writes the canonical form,
  and a reader parses the text and requires the value to give the same text back (`decimal.Parse` would round the
  digits it can not hold).
* A text is chosen instead of a binary form so that every language can read it exactly (as a string, or into its own
  decimal type), and a reader without the type still sees the number.

## 7. Identifiers: UUID

| Type | Code | Value | .NET |
|------|------|-------|------|
| `UUID` | 0x90 | 16 bytes, in the order of RFC 9562 (the order of the text) | `Guid` |

* The bytes are in the order of the text: `00112233-4455-6677-8899-aabbccddeeff` is `00 11 22 33 44 55 66 77 88 99 AA
  BB CC DD EE FF`, as other languages and databases store a UUID. In .NET: `guid.TryWriteBytes(span, bigEndian: true,
  out _)` and `new Guid(span, bigEndian: true)`.
* **Not** the layout of `Guid.ToByteArray()`, which has the first three groups little-endian. `Data.FromGuid` and
  `Data.AsGuid` of the library use that layout, and stay so (a change would break the existing files); the difference
  has to be pointed out in both documents.

## 8. Dates and times

All of them count **ticks** of 100 nanoseconds (the precision of .NET), from the **Unix epoch**, 1970-01-01, so that
other languages need no other base. The 64-bit counts cover the whole range of .NET (years 1 to 9999) with room.

| Type | Code | Value | .NET |
|------|------|-------|------|
| `DATE` | 0xD0 | 4 bytes: INT32, days since 1970-01-01 | `DateOnly` |
| `TIME` | 0xD1 | 8 bytes: INT64, ticks since midnight, 0 to 863 999 999 999 | `TimeOnly` |
| `DATETIME` | 0xD2 | 8 bytes: INT64, ticks since 1970-01-01T00:00, a date and a time without an offset | `DateTime` of `Kind` `Unspecified` |
| `TIMESTAMP` | 0xD3 | 10 bytes: INT64, ticks since 1970-01-01T00:00Z (the instant, in UTC), then INT16, the offset in minutes (-840 to 840) | `DateTimeOffset`; `DateTime` of `Kind` `Utc` (offset 0) and `Local` (its offset) |
| `DURATION` | 0xD4 | 8 bytes: INT64, ticks, may be negative | `TimeSpan` |

All little-endian.

* A `DateTime` comes back with its `Kind`: `DATETIME` gives `Unspecified`; `TIMESTAMP` with offset 0 written from a
  `Utc` `DateTime` gives `Utc`, and read into a `DateTime`, a `TIMESTAMP` with another offset gives `Local` (the
  instant converted to the local time). This is what the text of version 1 (`"O"`) does.
* Read into `object`: `DATE` gives a `DateOnly`, `TIME` a `TimeOnly`, `DATETIME` a `DateTime`, `TIMESTAMP` a
  `DateTimeOffset`, `DURATION` a `TimeSpan`.
* A reader rejects values out of the range of .NET (and a `TIME` out of a day, an offset out of ±14 hours).

## 9. Versions, reading and writing

* **Writing** chooses the type by the .NET type (sections 4–8). There is no option to write version 1: a property is
  written in the form of version 1 by its attribute, e.g. `[PromptuariumType(Float64)]` on an `int`, a `float` or a
  `decimal` (with the rule of 8.3.0: only if a double holds it exactly), `[PromptuariumType(Utf8String)]` on a `Guid`,
  a `DateTimeOffset` or a `decimal`, or by a type of version 1 (a `double`, a `string`).
* **The signature follows the content**: with the default signatures, a tree that has only the types of
  version 1 (and no NaN or infinity, which version 1 forbids) is signed `PRMT:GEN1-LE` (`PRMT:CFG1-LE`), so a reader
  of version 1 still reads it; a tree with a type of version 2 (or a NaN or an infinity) is signed `PRMT:GEN2-LE` (`PRMT:CFG2-LE`). So the attributes and the types of version 1 give a file of
  version 1. A signature of the application is written as it is: the application names its own versions
  (SERIALIZER_FORMAT.md, 2).
* **Reading** accepts both versions of the expected kind, and is lenient where nothing is lost:
  * any integer type is read into any integer type, if the value fits;
  * a `FLOAT64` is read into an integer type if it is a whole number that fits (as today);
  * an integer is read into a `float`, a `double` or a `Half` only if it is held exactly, the rule of 8.3.0 for
    writing, now for reading;
  * a version 1 text is still read into the dates, the times, `Guid` and `decimal` (as today), so a version 1 file
    stays readable.
* The types registered with `AddTextType` and `AddBlobType`, and the default text types, are unchanged.

## 10. The rest of the ecosystem

* **SERIALIZER_FORMAT.md**: a version 2 section with every new type, test vectors (byte for byte) for every one of
  them, and the transition from version 1.
* **pcfg**: the names of the types in `show` and `check`; `set` with a type.
  * `import` from JSON: every number stays `FLOAT64`, as in version 1 (a JSON number has no type of its own).
  * `export` to JSON: NaN and the infinities become the strings `"NaN"`, `"Infinity"` and `"-Infinity"` (JSON has no
    such numbers); imported again, they are strings. The other new types become strings (their text, as in version
    1), `DECIMAL` a JSON number with all its digits, the integers JSON numbers.
* **PStudio**: the type codes by name, and for every type the form that fits it.
* **promptuarium-js**: reads and writes the same, byte for byte (`BigInt` for the 64- and 128-bit integers).
* **Benchmarks**: version 1 and version 2 cases in `SerializerBenchmarks` (size and speed).

## 11. Order of the work

1. ~~The specification: the version 2 section of SERIALIZER_FORMAT.md and its test vectors, before any code.~~ Done.
2. The serializer: integers and binary floating point, the signature by the content, and the reading rules.
3. `UUID` and `TIMESTAMP`, then `DATE`, `TIME`, `DATETIME` and `DURATION`, then `DECIMAL` and `BIGINT`.
4. pcfg and PStudio, then promptuarium-js.

## 12. Open questions

None: the type codes by families (section 3) and the signature by the content (section 9) are confirmed.
