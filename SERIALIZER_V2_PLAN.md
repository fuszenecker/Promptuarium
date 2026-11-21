# Plan: version 2 of the serializer format

Status: **plan**, nothing of it is implemented. Version 1 is specified in [SERIALIZER_FORMAT.md](SERIALIZER_FORMAT.md);
this document collects what version 2 adds and the decisions taken so far. Once the specification is written, the
decided parts move to SERIALIZER_FORMAT.md and this document is removed.

**Contents**

1. [Goals](#1-goals)
2. [Decisions](#2-decisions)
3. [Type codes](#3-type-codes)
4. [Integers](#4-integers)
5. [Binary floating point](#5-binary-floating-point)
6. [Decimal](#6-decimal)
7. [Identifiers: UUID](#7-identifiers-uuid)
8. [Dates and times](#8-dates-and-times)
9. [Reading and writing rules](#9-reading-and-writing-rules)
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
| Writers | write version 1 or version 2 (an option, e.g. `FormatVersion`); the default is an [open question](#12-open-questions) |
| The type of a value | follows the .NET type, not the value: an `int` is always `INT32`, even if it is 5. A type tells what the value is; choosing the smallest code that holds the value would save a few bytes, but lose that |
| NaN and ±Infinity | **allowed** in version 2 (`FLOAT16`, `FLOAT32`, `FLOAT64`), so every value comes back, `-0.0` too; exporting to JSON, which has no such numbers, has to decide what to do with them (see [10](#10-the-rest-of-the-ecosystem)) |
| Dates and times | **all of them** get a type: `DATE`, `TIME`, `DATETIME`, `TIMESTAMP`, `DURATION` |
| `BigInteger` | not now (it may get a type later) |
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
| `0x7_` | **new:** decimal numbers, 2^n bytes | `DECIMAL128` 0x74 |
| `0x8_` | text | `UTF8_STRING` 0x80 |
| `0x9_` | **new:** identifiers | `UUID` 0x90 |
| `0xD_` | **new:** dates and times | `DATE` 0xD0, `TIME` 0xD1, `DATETIME` 0xD2, `TIMESTAMP` 0xD3, `DURATION` 0xD4 |
| `0xF_` | raw bytes | `BLOB` 0xF0 |

* The decimal numbers follow the size rule of the number families: 16 bytes, so 0x74 (and 0x73 would be a 64-bit
  decimal, if it is ever needed).
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
| `DECIMAL128` | 0x74 | 16 bytes, see below | `decimal` |

The value is the number `(-1)^sign × coefficient × 10^-scale`:

| Bytes | Content |
|-------|---------|
| 0–11 | the coefficient, a 96-bit unsigned integer, little-endian |
| 12 | the scale, 0 to 28 |
| 13 | the sign: 0x00 = positive, 0x01 = negative |
| 14–15 | 0x00 0x00 (reserved) |

* This is the content of a .NET `decimal` (the scale is kept: `12.750` stays `12.750`), in a layout that is simple to
  read in any language (a big integer and a power of ten); IEEE 754 decimal128 would be standard, but much more
  complex to encode and decode.
* A reader rejects a scale above 28, a sign other than 0 or 1, and reserved bytes that are not zero. A negative zero
  is allowed (a `decimal` has one).

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

## 9. Reading and writing rules

* **Writing** version 2 chooses the type by the .NET type (sections 4–8); version 1 writes as today.
* **Reading** is lenient where nothing is lost:
  * any integer type is read into any integer type, if the value fits;
  * a version 1 `FLOAT64` is read into an integer type if it is a whole number that fits (as today);
  * an integer is read into a `float`, a `double` or a `Half` only if it is held exactly, the rule of 8.3.0 for
    writing, now for reading;
  * a version 1 text is still read into the dates, the times and `Guid` (as today), so a version 1 file stays
    readable.
* **`[PromptuariumType]`** gets its real meaning: e.g. `[PromptuariumType(Int64)]` on an `int`, or
  `[PromptuariumType(Float64)]` on a `float`, or `[PromptuariumType(Utf8String)]` on a `Guid`, to keep the form of
  version 1 for one property.
* The types registered with `AddTextType` and `AddBlobType`, and the default text types, are unchanged.

## 10. The rest of the ecosystem

* **SERIALIZER_FORMAT.md**: a version 2 section with every new type, test vectors (byte for byte) for every one of
  them, and the transition from version 1.
* **pcfg**: the names of the types in `show` and `check`; `set` with a type; `import` from JSON: a whole number
  becomes `INT64`, any other number `FLOAT64` (an open question); `export` to JSON: what NaN and the infinities
  become (an error, or a string).
* **PStudio**: the type codes by name, and for every type the form that fits it.
* **promptuarium-js**: reads and writes the same, byte for byte (`BigInt` for the 64- and 128-bit integers).
* **Benchmarks**: version 1 and version 2 cases in `SerializerBenchmarks` (size and speed).

## 11. Order of the work

1. The specification: the version 2 section of SERIALIZER_FORMAT.md and its test vectors, before any code.
2. The serializer: integers and binary floating point, with the version option and the reading rules.
3. `UUID` and `TIMESTAMP`, then `DATE`, `TIME`, `DATETIME` and `DURATION`, then `DECIMAL128`.
4. pcfg and PStudio, then promptuarium-js.

## 12. Open questions

1. Do writers write version 2 by default, or version 1 until the applications reading the files (e.g. the
   configurations of PentaGrammata) can read version 2?
2. The type codes of section 3 (the families) are a proposal, to be confirmed.
3. `DECIMAL128`: the simple layout of section 6, or IEEE 754 decimal128, or text with a type of its own?
4. pcfg: does a whole number of a JSON file become `INT64`, or stay `FLOAT64` as in version 1?
5. JSON export: NaN and the infinities as an error, or as strings (`"NaN"`, `"Infinity"`)?
