# Promptuarium Binary Format

This document describes the binary container format produced by `Element.SaveAsync` and consumed by `Element.LoadAsync`.
The implementation lives in [`src/ElementSerializer.cs`](../src/ElementSerializer.cs) and [`src/ElementDefinitions.cs`](../src/ElementDefinitions.cs).

## Overview

A Promptuarium stream is a flat sequence of **chunks**. There is no file header, no magic number, no version field and no checksum.

Each chunk is:

```text
+--------------+---------------------------+
| control byte | payload (0 .. 512 KiB)    |
+--------------+---------------------------+
```

The control byte tells the decoder:

1. how to move in the tree before storing the payload (*direction*),
2. whether the payload belongs to the node's **data** or **metadata** (*type*),
3. how long the payload is (*size type* + *size nibble*).

The tree is written in depth-first, pre-order traversal: a node's own content first, then its children from first to last.
The stream ends with a single `0x00` byte (or at the physical end of the stream).

## Control byte layout

```text
  bit:   7   6   5   4   3   2   1   0
       +-------+---+---+---------------+
       |  DIR  | T | S |   SIZE (n)    |
       +-------+---+---+---------------+
```

| Bits | Mask   | Field              | Values |
|------|--------|--------------------|--------|
| 7–6  | `0xC0` | Direction (`DIR`)  | `00` Append, `01` Down, `10` Up, `11` New |
| 5    | `0x20` | Content type (`T`) | `0` Data, `1` MetaData |
| 4    | `0x10` | Size type (`S`)    | `0` Linear, `1` Logarithmic |
| 3–0  | `0x0F` | Size nibble (`n`)  | `0`–`15` |

### Direction (bits 7–6)

The decoder keeps two cursors: the **current parent** and the **current node**.
Initially both point to a virtual root that is not part of the result; the real root becomes the virtual root's first child.

| Value | Name   | Byte mask | Effect on the decoder |
|-------|--------|-----------|-----------------------|
| `00`  | Append | `0x00`    | Stay on the current node; the payload continues the current node's content. |
| `01`  | Down   | `0x40`    | The current node becomes the parent; create a new node as its child (first child). |
| `10`  | Up     | `0x80`    | The parent's parent becomes the parent; create a new node under it (i.e. a new sibling of the former parent). |
| `11`  | New    | `0xC0`    | Create a new node under the current parent (a new sibling of the current node). |

Every direction except Append starts a new node; the payload of the chunk is stored in that new node.
Moving Up above the virtual root raises a `PromptuariumException`.

### Content type (bit 5)

| Value | Name     | Byte mask | Payload is appended to |
|-------|----------|-----------|------------------------|
| `0`   | Data     | `0x00`    | `Element.Data` |
| `1`   | MetaData | `0x20`    | `Element.MetaData` |

### Size type (bit 4) and size nibble (bits 3–0)

The payload length `L` that follows the control byte is computed from the size type and the nibble `n`:

| Size type   | Bit 4 | Length              | Range |
|-------------|-------|---------------------|-------|
| Linear      | `0`   | `L = n`             | 0 – 15 bytes |
| Logarithmic | `1`   | `L = 2^(n + 4)`     | 16 B (`n = 0`) – 512 KiB (`n = 15`) |

Logarithmic lengths for each nibble value:

| `n` | Length    | `n` | Length    | `n` | Length    | `n` | Length    |
|-----|-----------|-----|-----------|-----|-----------|-----|-----------|
| 0   | 16 B      | 4   | 256 B     | 8   | 4 KiB     | 12  | 64 KiB    |
| 1   | 32 B      | 5   | 512 B     | 9   | 8 KiB     | 13  | 128 KiB   |
| 2   | 64 B      | 6   | 1 KiB     | 10  | 16 KiB    | 14  | 256 KiB   |
| 3   | 128 B     | 7   | 2 KiB     | 11  | 32 KiB    | 15  | 512 KiB   |

## Encoding the length of a content stream

A data or metadata stream of arbitrary length is split into one or more chunks. The encoder (`GetSizes`) works like this, starting with `remaining = length`:

1. While `remaining > 512 KiB`: emit a logarithmic chunk with `n = 15` (512 KiB) and subtract 512 KiB.
2. While `remaining > 15`: take the highest set bit `b` of `remaining` (`4 <= b <= 19`), emit a logarithmic chunk with `n = b - 4` (`2^b` bytes) and subtract `2^b`.
3. If `1 <= remaining <= 15`: emit one linear chunk with `n = remaining`.

In other words, after the full 512 KiB chunks, the remainder is written as its binary representation: one logarithmic chunk per set bit from bit 19 down to bit 4, then the low 4 bits as a single linear chunk.
The total length is not stored anywhere; it is the sum of the chunk lengths.

The overhead is one control byte per chunk, so a stream of `L` bytes costs at most `floor(L / 512 KiB) + 16` extra bytes.

Example: 37 bytes = 32 + 5 is written as two chunks:

```text
51  <32 bytes>      Down | Data | Logarithmic | n=1  (2^5 = 32 bytes)
05  <5 bytes>       Append | Data | Linear | n=5
```

## Serializing a node

For each node the encoder writes, in order:

1. **Pending Up markers** (see below), if any.
2. **A placeholder chunk**, only if the node has neither data nor metadata but has children: the control byte `DIR | Data | Linear | 0` with no payload. Its only purpose is to create the node in the decoder.
3. **Data chunks**, if `Data` is non-empty.
4. **MetaData chunks**, if `MetaData` is non-empty.
5. **The children**, recursively, from first to last.

The very first chunk of a node carries the navigation direction; every following chunk of the same node (the remaining data chunks *and* all metadata chunks) uses **Append**.
Data always precedes metadata within a node.

### Choosing the direction

The direction of a node's first chunk depends on the node that was serialized before it:

| Previously serialized node                   | Direction |
|----------------------------------------------|-----------|
| none (this is the root)                      | Down |
| the node's parent                            | Down (first child) |
| a sibling of the node                        | New |
| a child of a sibling (sibling's subtree ended) | Up |

### Climbing more than one level

`Up` climbs only one level. When a subtree that ends more than one level deeper than the next node is closed, the encoder emits additional **Up markers** before the next node: each marker is the byte `0x80` (`Up | Data | Linear | 0`, no payload).
Each marker moves the decoder's parent cursor one level up and creates an empty placeholder node, which is removed after loading (see [Empty nodes](#empty-nodes)).
Markers that would be needed after the last node of the tree are never written; the terminator ends the stream instead.

## End of stream

`SaveAsync` writes a final `0x00` byte (`Append | Data | Linear | 0`). The decoder stops when it reads an Append chunk with a length of 0, or when it reaches the end of the stream.
Because of this, an Append chunk with zero length never appears in the middle of a valid stream.

The terminator keeps the format streamable: the reader does not need to know the total length in advance, so a tree can be sent over a network connection, several trees can be concatenated in one stream, and a tree can be nested inside another tree's payload (`Data.FromElementAsync`).

The reader relies on this (since version 6.0.0):

* it never seeks and never queries `Length` or `Position`, so non-seekable streams (e.g. `NetworkStream`) work;
* it tolerates partial reads, as a network stream may return fewer bytes than requested;
* it never reads beyond the terminator, so the next tree (or any other data) can be read from the same stream right after `LoadAsync` returns;
* a stream that ends in the middle of a chunk raises a `PromptuariumException`. A stream that ends between two chunks without a terminator is accepted.

## Empty nodes

After decoding, every node that has no data, no metadata and no children is removed (`PurgeEmptyNodes`). This removes the placeholders created by Up markers.

The encoder writes nothing for a node that has no data, no metadata and no children. Such nodes are not preserved by a save/load round trip.

Empty nodes are skipped entirely: they do not count as the "previously serialized" node when the encoder picks the direction of the next node.
(Versions up to 5.0.0 did not skip them, so an empty first child — or an empty last child following a subtree — could make the next node attach to the wrong parent.)

## Worked example

Tree (ASCII strings; `c` has no data or metadata):

```text
R ("R")
├── X ("X")
│   └── c (empty, only children)
│       └── d ("d")
└── Y (data "Y", metadata "m")
```

Serialized bytes:

```text
41 52 41 58 40 41 64 80 81 59 21 6D 00
```

| Bytes   | Control byte decoded                | Meaning |
|---------|-------------------------------------|---------|
| `41 52` | Down, Data, Linear, 1               | Create root `R`, data `"R"` |
| `41 58` | Down, Data, Linear, 1               | Create `X` as first child of `R`, data `"X"` |
| `40`    | Down, Data, Linear, 0               | Placeholder: create `c` as first child of `X` (no data) |
| `41 64` | Down, Data, Linear, 1               | Create `d` as first child of `c`, data `"d"` |
| `80`    | Up, Data, Linear, 0                 | Up marker: parent becomes `X`, empty node is created (purged later) |
| `81 59` | Up, Data, Linear, 1                 | Parent becomes `R`; create `Y`, data `"Y"` |
| `21 6D` | Append, MetaData, Linear, 1         | Metadata `"m"` for `Y` |
| `00`    | Append, Data, Linear, 0             | End of stream |

## Payload contents

The container itself treats data and metadata as opaque byte sequences. The helpers in the `Data` class ([`src/Data.cs`](../src/Data.cs)) use the following encodings:

| Type | Encoding | Size |
|------|----------|------|
| `bool` | `BitConverter`: `0x00` / `0x01` | 1 |
| `byte` | the byte itself | 1 |
| `byte[]` | raw bytes | n |
| `char` | UTF-16 code unit, `BitConverter` | 2 |
| `short` / `ushort` | `BitConverter` | 2 |
| `int` / `uint` / `float` | `BitConverter` | 4 |
| `long` / `ulong` / `double` | `BitConverter` | 8 |
| `decimal` | `BinaryWriter.Write(decimal)`: lo, mid, hi, flags as 4 × 32-bit little-endian integers | 16 |
| `Guid` | `Guid.ToByteArray()` | 16 |
| `DateTime` | `Ticks` as `long` | 8 |
| `TimeSpan` | `Ticks` as `long` | 8 |
| `DateTimeOffset` | `Ticks` as `long`, then `Offset.Ticks` as `long` | 16 |
| ASCII / UTF-8 / UTF-16LE / UTF-32LE string | `Encoding.*.GetBytes`, no length prefix, no terminator, no BOM | n |
| `VarUInt` | little-endian `ulong` with trailing zero bytes removed (at least 1 byte) | 1–8 |
| `VarInt` | `(abs(value) << 1) \| sign` (sign bit is 1 for negative), then encoded as `VarUInt` | 1–8 |
| `Element` | a complete nested Promptuarium stream (including its `0x00` terminator) | n |

`BitConverter` uses the machine's byte order, which is little-endian on all mainstream .NET platforms.

Examples: `VarUInt(300)` is `2C 01`; `VarInt(-3)` is `07` (`3 << 1 | 1`).
