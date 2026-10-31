# Promptuarium Binary Format Specification

This document specifies the Promptuarium binary container format.
It is self-contained and language independent: an implementation in any language that follows it reads every Promptuarium stream, and writes byte-identical streams.

## Structure, not content

Promptuarium is a **container for a tree**. What it defines is the **structure**: a tree of nodes, where every node has a data and a metadata byte sequence and an ordered list of children. That is all.

The **content** of the data and the metadata is not part of the format. It is a plain byte sequence (only its length is known), without type or name information, and its meaning (a string, a number, an image, another Promptuarium tree, …) is entirely up to the writer and the reader. How an application or a programming language turns its values into bytes is outside of the scope of this specification.

The format is complete and has **no version**: there is no version field, and none is needed. Every stream ever written follows these rules (see [section 11](#11-compatibility-and-history)).

**Contents**

1. [Conventions](#1-conventions)
2. [Data model](#2-data-model)
3. [Stream structure](#3-stream-structure)
4. [Control byte](#4-control-byte)
5. [Decoding](#5-decoding)
6. [Encoding](#6-encoding)
7. [Streaming](#7-streaming)
8. [Errors](#8-errors)
9. [Test vectors](#9-test-vectors)
10. [File identification (optional convention)](#10-file-identification-optional-convention)
11. [Compatibility and history](#11-compatibility-and-history)
12. [Implementation notes](#12-implementation-notes)

---

## 1. Conventions

* **MUST**, **MUST NOT**, **SHOULD** and **MAY** are used as in RFC 2119.
* Bytes are written in hexadecimal (`41`, `0x41`). Byte sequences are written left to right in stream order.
* Bits are numbered from 0 (least significant) to 7 (most significant).
* `2^n` is 2 to the power of `n`. `KiB` is 1024 bytes.
* In tree diagrams, `X("abc")` is a node whose data is the ASCII bytes of `abc`, and `[...]` is the list of its children.

## 2. Data model

A Promptuarium tree consists of **nodes**. Every node has:

| Field      | Type                            | Notes |
|------------|---------------------------------|-------|
| `data`     | byte sequence (0 or more bytes) | opaque to the container |
| `metadata` | byte sequence (0 or more bytes) | opaque to the container |
| `children` | ordered list of nodes           | may be empty |

The container stores **no type information**, no names and no keys: data and metadata are plain bytes, and their meaning is an agreement between the writer and the reader (see [Structure, not content](#structure-not-content)).

The format does not distinguish an *absent* content from an *empty* (zero-length) one: an implementation MAY represent both as an empty byte sequence.

A node is **empty** if its data and metadata are both zero-length and it has no children. Empty nodes are part of the structure like any other node, and they are preserved (see [6.4](#64-empty-nodes)): a tree is always loaded with exactly the same structure as it was saved.

A stream holds exactly **one tree** (one root node). Several trees can follow each other in the same stream (see [section 7](#7-streaming)).

## 3. Stream structure

A stream is a sequence of **chunks**, ended by a **terminator**. There is no header, no magic number, no version field, no length field and no checksum: the stream consists of the tree and nothing else.

```text
stream     = *chunk terminator
chunk      = control-byte payload
payload    = 0 to 524288 bytes (the length is encoded in the control byte)
terminator = the control byte of an Append chunk with length 0 (written as 00)
```

Every chunk either **starts a new node** (and positions it in the tree) or **appends** bytes to the node started last. The payload of a chunk is appended either to the data or to the metadata of that node.

The nodes are written in **depth-first pre-order**: a node, then the subtree of its first child, then the subtree of its second child, and so on.

## 4. Control byte

```text
  bit:   7   6   5   4   3   2   1   0
       +-------+---+---+---------------+
       |  DIR  | T | S |   SIZE (n)    |
       +-------+---+---+---------------+
```

| Bits | Mask   | Field              | Values |
|------|--------|--------------------|--------|
| 7–6  | `0xC0` | Direction (`DIR`)  | `00` Append, `01` Down, `10` Up, `11` New |
| 5    | `0x20` | Content type (`T`) | `0` data, `1` metadata |
| 4    | `0x10` | Size type (`S`)    | `0` linear, `1` logarithmic |
| 3–0  | `0x0F` | Size nibble (`n`)  | `0`–`15` |

So `control = DIR | T | S | n`, with these values:

| Name        | Value  |
|-------------|--------|
| Append      | `0x00` |
| Down        | `0x40` |
| Up          | `0x80` |
| New         | `0xC0` |
| Data        | `0x00` |
| MetaData    | `0x20` |
| Linear      | `0x00` |
| Logarithmic | `0x10` |

### 4.1 Direction

The decoder keeps two cursors, `parent` and `node` (see [section 5](#5-decoding)).

| Direction | Effect |
|-----------|--------|
| Append    | No navigation. The payload is appended to the current `node`. |
| Down      | `parent = node`, then a new node is created as the last child of `parent` and becomes `node`. (The new node is the first child of the previous node.) |
| Up        | `parent = parent.parent`, then a new node is created as the last child of `parent` and becomes `node`. (The new node is a sibling of the previous parent.) |
| New       | A new node is created as the last child of `parent` and becomes `node`. (The new node is a sibling of the previous node.) |

### 4.2 Content type

The payload is appended to `node.data` (`T = 0`) or to `node.metadata` (`T = 1`).

A zero-length payload has no content to append, so on a chunk that creates a node (Down, Up or New with length 0) the `T` bit has a structural meaning instead:

* `T = 0`: an **auxiliary node**. It is a placeholder (a node without content that has children) or an Up marker (see [6.3](#63-climbing-more-than-one-level)). It is removed after loading if it ends up empty.
* `T = 1`: an **empty node**. It is part of the tree and is kept, even though it has no content and no children.

### 4.3 Payload length

| Size type   | `S` | Payload length | Range |
|-------------|-----|----------------|-------|
| Linear      | `0` | `n`            | 0 – 15 bytes |
| Logarithmic | `1` | `2^(n + 4)`    | 16 bytes (`n = 0`) – 512 KiB (`n = 15`) |

| `n` | Logarithmic length | `n` | Logarithmic length | `n` | Logarithmic length | `n` | Logarithmic length |
|-----|------|-----|------|-----|-------|-----|--------|
| 0   | 16   | 4   | 256  | 8   | 4096  | 12  | 65536  |
| 1   | 32   | 5   | 512  | 9   | 8192  | 13  | 131072 |
| 2   | 64   | 6   | 1024 | 10  | 16384 | 14  | 262144 |
| 3   | 128  | 7   | 2048 | 11  | 32768 | 15  | 524288 |

### 4.4 Special control bytes

| Byte(s)    | Meaning |
|------------|---------|
| `00`, `20` | Terminator (Append, linear, length 0). The encoder writes `00`; the decoder MUST accept both. |
| `10`, `30` | **Not** a terminator: Append with a logarithmic length of 16 bytes. |
| `40`, `80`, `C0` | Down, Up or New with an empty payload and `T = 0`: an **auxiliary node**, removed if it ends up empty. Written as a placeholder for a node without content that has children (`40`, `80`, `C0`), and as an **Up marker** to climb more than one level (`80`). |
| `60`, `A0`, `E0` | Down, Up or New with an empty payload and `T = 1`: an **empty node** (no content, no children), which is kept. |

## 5. Decoding

### 5.1 Algorithm

This algorithm is normative: a conforming decoder MUST produce the same tree for every input.

```text
function decode(stream) -> Node:
    virtual = new Node                  // a virtual root, never returned
    parent  = virtual
    node    = virtual

    loop:
        c = read 1 byte
        if end of stream: break         // a missing terminator is accepted

        dir = c & 0xC0
        len = (c & 0x10) != 0 ? 2^((c & 0x0F) + 4) : (c & 0x0F)

        if dir == New:
            node = new Node; append node to parent.children
        else if dir == Down:
            parent = node
            node = new Node; append node to parent.children
        else if dir == Up:
            if parent is virtual: error "navigating above the root"
            parent = parent.parent
            node = new Node; append node to parent.children
        // else Append: no navigation

        if dir == Append and len == 0: break      // terminator

        if len > 0:
            bytes = read exactly len bytes        // end of stream here is an error
            if (c & 0x20) == 0: append bytes to node.data
            else:               append bytes to node.metadata
        else if (c & 0x20) != 0:
            node.kept = true                      // empty node (60, A0, E0): part of the tree

    remove_empty_nodes(virtual)
    if virtual.children is empty: return new Node      // no data, no metadata, no children
    root = virtual.children[0]
    root.parent = none
    return root                         // further children of the virtual root are ignored

function remove_empty_nodes(virtual):
    // Removes the auxiliary nodes (placeholders and Up markers) that ended up empty.
    // Bottom-up: every node is processed after all of its descendants.
    for each node in reverse pre-order of the tree under virtual (including virtual):
        remove every child c of node where
            c.data is empty and c.metadata is empty and c.children is empty and not c.kept
```

Reverse pre-order visits every node after all of its descendants, so a node whose children were all removed is removed in turn when its parent is processed. The `kept` flag is only needed while loading.

### 5.2 Rules and edge cases

* **Content before the first node.** An Append chunk before any node was started appends to the virtual root; the bytes are discarded.
* **First node.** The first chunk that starts a node is normally a Down chunk (`41`, `40`, …). A New chunk (`C1`, …) has the same effect at the beginning, because `parent` and `node` are both the virtual root. An Up chunk at the beginning is an error.
* **Siblings of the root.** Up or New chunks that create further nodes at the root level (more children of the virtual root) are valid, but these nodes are ignored.
* **Interleaving.** Data and metadata chunks of the same node MAY be interleaved; each payload is appended to its own content in stream order. (The encoder writes all data chunks first.)
* **Chunk sizes.** Any split of a content into chunks is valid, e.g. a decoder MUST accept 100 linear chunks of 15 bytes. The split of the encoder is described in [6.2](#62-splitting-a-content-into-chunks).
* **Zero-length chunks.** A Down, Up or New chunk with length 0 only creates a node: an auxiliary node with `T = 0`, an empty node with `T = 1` (see [4.2](#42-content-type)). An Append chunk with length 0 is always the terminator.
* **Auxiliary nodes.** Placeholders and Up markers create nodes that may end up empty; `remove_empty_nodes` removes them. The removal is bottom-up, so an auxiliary node whose whole subtree was removed disappears as well.
* **Content after a marker.** A node created by a zero-length chunk can still receive content by Append chunks; it is then an ordinary node. (Encoders never write this.)
* **Terminator.** After the terminator the decoder MUST NOT read any further byte.
* **Parent of the root.** The returned root has no parent.

## 6. Encoding

A writer MAY produce any stream that decodes to the intended tree (section 5). This section defines the **canonical** encoding: every tree has exactly one canonical byte sequence, and an encoder that follows this section exactly is byte-identical with every other conforming encoder.

### 6.1 Algorithm

```text
function encode(root, out):
    stack = [(root, 0)]                 // (node, depth); the root is at depth 0
    prev  = -1                          // depth of the previously written node; -1 is the virtual root

    while stack is not empty:
        (n, d) = stack.pop()

        if d > prev:                    // always d == prev + 1
            dir = Down
        else if d == prev:
            dir = New
        else:                           // d < prev: climb prev - d levels
            repeat (prev - d - 1) times: write byte 0x80      // Up markers
            dir = Up

        if len(n.data) == 0 and len(n.metadata) == 0:
            if n.children is not empty:
                write byte (dir | Data | Linear | 0)          // placeholder: 40, C0 or 80
            else:
                write byte (dir | MetaData | Linear | 0)      // empty node:  60, E0 or A0

        for each (S, nibble, length) in split(len(n.data)):
            write byte (dir | Data | S | nibble)
            write the next `length` bytes of n.data
            dir = Append

        for each (S, nibble, length) in split(len(n.metadata)):
            write byte (dir | MetaData | S | nibble)
            write the next `length` bytes of n.metadata
            dir = Append

        prev = d
        for each child c of n, from the LAST to the FIRST:
            stack.push((c, d + 1))

    write byte 0x00                     // terminator
```

Notes:

* Every node is written, including the empty ones.
* The first chunk of a node carries `dir`; every further chunk of the same node (data and metadata) is an Append chunk.
* `d > prev + 1` never happens: a node is pushed only after its parent was written, and the subtree of the parent is processed right after it.
* Up markers are only written before a node. After the last node no markers are written; the terminator follows directly.

### 6.2 Splitting a content into chunks

```text
function split(L) -> list of (S, nibble, length):
    result = []
    while L > 0:
        if L > 524288:
            result.add((Logarithmic, 15, 524288));  L = L - 524288
        else if L > 15:
            b = index of the highest set bit of L    // 4 <= b <= 19
            result.add((Logarithmic, b - 4, 2^b));   L = L - 2^b
        else:
            result.add((Linear, L, L));              L = 0
    return result
```

In words: if `L > 524288`, full 512 KiB chunks are written until at most 524288 bytes remain. The remainder (`1..524288` bytes) is written in binary: one logarithmic chunk for every set bit from bit 19 down to bit 4, then the low 4 bits as one linear chunk (if they are not all zero).
A zero-length content produces no chunk at all.

The overhead is one control byte per chunk: at most `floor(L / 524288) + 16` bytes for a content of `L` bytes.

Examples (the control bytes of the data chunks of a root node; its first chunk is Down):

| Length  | Split                                                | Control bytes |
|---------|------------------------------------------------------|---------------|
| 5       | 5                                                    | `45` |
| 15      | 15                                                   | `4F` |
| 16      | 16                                                   | `50` |
| 37      | 32 + 5                                               | `51 05` |
| 600000  | 524288 + 65536 + 8192 + 1024 + 512 + 256 + 128 + 64  | `5F 1C 19 16 15 14 13 12` |
| 1048576 | 524288 + 524288                                      | `5F 1F` |
| 1048577 | 524288 + 524288 + 1                                  | `5F 1F 01` |

### 6.3 Climbing more than one level

```text
R("R")                      41 52         Down, d=0 (prev=-1)
├── A("A")                  41 41         Down, d=1
│   └── B("B")              41 42         Down, d=2
│       └── C("C")          41 43         Down, d=3
│           └── D("D")      41 44         Down, d=4
└── E("E")                  80 80 81 45   d=1, prev=4: 4 - 1 - 1 = 2 Up markers, then Up
                            00            terminator
```

Decoding the markers: after `D` the parent is `C`. `80` makes `B` the parent (and creates an auxiliary node), the next `80` makes `A` the parent (and creates an auxiliary node), `81 45` makes `R` the parent and creates `E`. The two auxiliary nodes are removed at the end.

If `E` were an empty node, the last chunk would be `A0` instead of `81 45`.

### 6.4 Empty nodes

An empty node (no data, no metadata, no children) is written as a single zero-length chunk with `T = 1` (`60`, `E0` or `A0`), so it is kept by the decoder. Like any other node, it changes `prev`.

A node without content but with children is written as a placeholder with `T = 0` (`40`, `C0` or `80`); the decoder keeps it, because its children are kept.

So the structure is always preserved: the same nodes in the same order, wherever they are and whatever they contain. Positions in a list of children never shift.

## 7. Streaming

The terminator makes the format streamable:

* The reader needs neither the total length nor the end of the stream; it reads exactly up to and including the terminator.
* The reader never seeks, so it works on sockets, pipes and other non-seekable streams. It MUST tolerate partial reads (a read may return fewer bytes than requested).
* After a tree has been read, the stream is positioned right after its terminator, so **several trees can be concatenated** in one stream and read one after the other. Any other data may follow a tree as well.
* A tree can be **nested** into the data or metadata of another tree: the nested stream (with its own terminator) is simply the content of a node.
* The writer only writes forward, so it works on non-seekable streams too. It needs the length of each content in advance (to compute the split), but not the size of the whole tree.

A reader SHOULD buffer its input, but it MUST NOT leave the stream positioned beyond the terminator. For example:

* on a **seekable** stream it can read ahead in blocks, and at the end seek back to the byte right after the terminator;
* on a **non-seekable** stream it must not request more bytes than it needs: one byte for a control byte, at most the remaining payload for a payload.

Likewise, a writer SHOULD collect small chunks and write them at once, e.g. in a buffer of 524289 bytes (a control byte and the largest payload), written out when the next chunk does not fit.

A reader MAY limit the input of an untrusted source, e.g. the number of bytes read (control bytes included) and the number of nodes created (placeholders and Up markers included), and report an error when a limit is exceeded.

## 8. Errors

A decoder MUST report an error when:

* an Up chunk is read while `parent` is the virtual root ("navigating above the root");
* the stream ends inside a payload ("unexpected end of stream");
* the input exceeds a limit set by the reader (optional, see [section 7](#7-streaming)).

Everything else is valid input, including a stream that ends between two chunks without a terminator, and an empty stream (which decodes to an empty node).

## 9. Test vectors

All payloads are ASCII. `(empty)` is a node without data, metadata and children; `c` is a node without data and metadata (but with children).

### 9.1 Encoding (tree → bytes)

An encoder MUST produce exactly these bytes, and a decoder MUST reproduce exactly the same tree from them.

| Tree | Bytes |
|------|-------|
| `(empty)` | `60 00` |
| `R("R")` | `41 52 00` |
| node with data `"ab"` and metadata `"c"` | `42 61 62 21 63 00` |
| node with metadata `"m"` only, child `a("a")` | `61 6D 41 61 00` |
| `R("R") [A("A"), B("B"), C("C")]` | `41 52 41 41 C1 42 C1 43 00` |
| `R("R") [X("X") [c [d("d")]], Y(data "Y", metadata "m")]` | `41 52 41 58 40 41 64 80 81 59 21 6D 00` |
| `R("R") [A("A") [B("B") [C("C") [D("D")]]], E("E")]` | `41 52 41 41 41 42 41 43 41 44 80 80 81 45 00` |
| `R("R") [X("X") [c [(empty)]], Y("Y")]` | `41 52 41 58 40 60 80 81 59 00` |
| `P("P") [(empty), M("M")]` | `41 50 60 C1 4D 00` |
| `P("P") [(empty), M("M"), c [(empty), (empty)]]` | `41 50 60 C1 4D C0 60 E0 00` |
| `R("R") [A("A") [B("B")], (empty)]` | `41 52 41 41 41 42 A0 00` |
| data of 37 zero bytes | `51`, 32 × `00`, `05`, 5 × `00`, `00` |

The worked example `41 52 41 58 40 41 64 80 81 59 21 6D 00`, byte by byte:

| Bytes   | Control byte                | Decoder |
|---------|-----------------------------|---------|
| `41 52` | Down, data, linear, 1       | parent = virtual, node = `R`; data `"R"` |
| `41 58` | Down, data, linear, 1       | parent = `R`, node = `X`; data `"X"` |
| `40`    | Down, data, linear, 0       | parent = `X`, node = `c` (placeholder) |
| `41 64` | Down, data, linear, 1       | parent = `c`, node = `d`; data `"d"` |
| `80`    | Up, data, linear, 0         | parent = `X`, node = new auxiliary node (removed later) |
| `81 59` | Up, data, linear, 1         | parent = `R`, node = `Y`; data `"Y"` |
| `21 6D` | Append, metadata, linear, 1 | metadata of `Y`: `"m"` |
| `00`    | Append, data, linear, 0     | terminator |

### 9.2 Decoding only (bytes → tree)

The encoder never writes these streams, but a decoder MUST handle them like this:

| Bytes | Result |
|-------|--------|
| (empty stream) | `(empty)` |
| `00` | `(empty)` (an empty root written by older encoders) |
| `41 61` (no terminator) | `a("a")` |
| `42 61` (truncated payload) | error: unexpected end of stream |
| `81 52 00` | error: navigating above the root |
| `C1 52 00` | `R("R")` |
| `01 58 41 52 00` | `R("R")` (the `"X"` before the first node is discarded) |
| `41 52 C1 53 00` | `R("R")` (the sibling of the root is ignored) |
| `41 61 21 6D 01 62 00` | data `"ab"`, metadata `"m"` (interleaved chunks) |
| `41 61 10`, 16 × `62`, `00` | data `"a"` followed by 16 × `"b"` (`10` is not a terminator) |
| `41 61 20 41 62 00` | `a("a")`: `20` is a terminator, `41 62 00` is not read |
| `60 41 61 00` | a root without content, with child `a("a")` |
| `41 52 40 C0 C1 41 00` | `R("R") [A("A")]` (the auxiliary nodes `40` and `C0` are removed) |
| `41 52 41 41`, N × `C0`, `C1 42 00` | `R("R") [A("A"), B("B")]` (the N auxiliary siblings are removed) |
| N × `40`, `00` | `(empty)` (all placeholders are removed) |

## 10. File identification (optional convention)

The format has no magic number, so tools like `file` (libmagic) can not recognize a Promptuarium file on their own.
A **signature** can be added without changing the format, purely by the way the tree is built. This section describes such a convention; it is optional, and plain decoders do not need to know about it.

### 10.1 Idea

The first bytes of a stream are fully determined by the root node:

* the root is the first node, so its first chunk is a **Down** chunk;
* within a node, data chunks are written before metadata chunks.

So if the root has **no data** and a **fixed metadata** (the signature), the stream always starts with the same bytes: the control byte of the metadata chunk, then the signature itself. The real content of the file goes into the children of the root.

```text
signed root (no data, metadata = signature)
├── child 1   ← the actual content of the file
├── child 2
└── ...
```

The first control byte is `Down | MetaData | Linear | length` = `0x60 | length`. With a signature of 1 to 15 bytes this is a single linear chunk, so the file starts with `0x60 + length` followed by the signature bytes, at offset 0.

(A longer signature also gives a fixed prefix, since the split of a fixed length is always the same, but it is split into several chunks with control bytes in between, see [6.2](#62-splitting-a-content-into-chunks). Keep it at most 15 bytes.)

Rules of the convention:

* The root MUST NOT have data; otherwise the data chunks come first and the signature is no longer at offset 0.
* The signature MUST be the whole metadata of the root, 1 to 15 bytes long.
* The root SHOULD have at least one child; a root with no children is still a valid file (`64 50 52 4D 54 00`).
* A reader that knows the convention checks the root metadata after loading (or the first bytes before loading), then uses the children of the root.
* A reader that does not know the convention simply sees a root with some metadata and the content as its children.

### 10.2 Example

Signature: the 4 ASCII bytes `PRMT` (`50 52 4D 54`). Content: one child `a("a")`.

```text
root (metadata "PRMT")
└── a("a")
```

```text
64 50 52 4D 54    Down, metadata, linear, 4: the signature "PRMT" (root)
41 61             Down, data, linear, 1:     child a("a")
00                terminator
```

Every file built this way starts with `64 50 52 4D 54`, whatever its content is.

### 10.3 Tagged signature

The signature can carry the kind and the version of the content (not of the format, which has no version), so that different applications, or different versions of the content of one application, are told apart: `PRMT`, a colon, then a **tag** of 1 to 10 bytes (the whole signature is at most 15 bytes):

```text
PRMT:IMGZ5.6      12 bytes: the file starts with 6C 50 52 4D 54 3A 49 4D 47 5A 35 2E 36
```

The tag is free-form; printable ASCII is recommended, so that tools can show it. A signature is compared **byte by byte**, so its case matters (`prmt:imgz5.6` is not `PRMT:IMGZ5.6`); the signatures of this project (`PRMT:GEN1-LE`, `PRMT:CFG1-LE`, `PRMT:PSTNT1`, and those of the examples) are written in **upper case** (letters, digits, `.` and `-`), and new ones SHOULD be too. As the length of the signature is in the first control byte (`0x60 + length`), the end of the tag is known without a terminator.

| Signature | Length | First bytes |
| --- | --- | --- |
| `PRMT` | 4 | `64 50 52 4D 54` |
| `PRMT:A` | 6 | `66 50 52 4D 54 3A 41` |
| `PRMT:IMGZ5.6` | 12 | `6C 50 52 4D 54 3A 49 4D 47 5A 35 2E 36` |
| `PRMT:ABCDEFGHIJ` | 15 | `6F 50 52 4D 54 3A 41 42 …` |

An application can use a signature of its own as well (e.g. `MYAPP-CONFIG`, 12 bytes, starting with `6C`), but then the rule below does not recognize it as a Promptuarium file.

### 10.4 Rules for `file` (libmagic)

A magic rule matches the first bytes: `PRMT` at offset 1, after a control byte of `Down | MetaData | Linear` (`0x6_`). The tag can not be printed with a plain `%s`, because the next chunk follows it directly, and its control byte and content are often printable too. So there is one rule for every length of the signature, printing exactly the bytes of the tag:

```text
# Promptuarium container: a signed root whose signature starts with "PRMT"
1	string	PRMT
>0	byte&0xF0	0x60	Promptuarium container
!:mime	application/x-promptuarium
# The tag after "PRMT:" (the control byte tells its length)
>>0	byte	0x66
>>>5	string	:
>>>>6	string	x	\b, %.1s
>>0	byte	0x67
>>>5	string	:
>>>>6	string	x	\b, %.2s
...
>>0	byte	0x6F
>>>5	string	:
>>>>6	string	x	\b, %.10s
```

The complete file is [`misc/promptuarium.magic`](misc/promptuarium.magic) (the fields are separated by tabs). It can be appended to `~/.magic` (per user, used together with the system rules) or to `/etc/magic` (system wide), or tried out directly:

```text
$ file -m misc/promptuarium.magic photo.prmt tree.prmt
photo.prmt: Promptuarium container, IMGZ5.6
tree.prmt:  Promptuarium container
$ file --mime-type -m misc/promptuarium.magic photo.prmt
photo.prmt: application/x-promptuarium
```

Without a signature, a Promptuarium stream can not be recognized reliably: it usually starts with a Down chunk (`40`–`7F`), but so do many other files.

## 11. Compatibility and history

Every stream ever written can be loaded. The version numbers below are versions of the reference implementation in this repository, not of the format.

**Empty nodes.** Older encoders did not write empty nodes at all, and never wrote a zero-length chunk with `T = 1` (`60`, `A0`, `E0`): in every older stream, all zero-length node-creating chunks are auxiliary nodes. Therefore:

* streams of older encoders are loaded exactly as before;
* a decoder that does not know about empty nodes (any older version) loads a current stream correctly, except that it drops the empty nodes, as it always did;
* for trees without empty nodes, the current encoder writes exactly the same bytes as the older encoders (where they were correct).

**Encoder bugs of older versions** (all of them with empty nodes):

* **Up to 5.0.0:** an empty first child (or an empty last child after a subtree) still counted as the previous node, so the next node was attached to the wrong parent.
  `P("P") [(empty), M("M")]` was written as `41 50 C1 4D 00`: `M` became a sibling of the root and was lost.
* **Up to 6.0.0:** a node with only empty children produced a superfluous Up marker, so the next node was attached one level too high: it was lost, or the stream could not be loaded at all ("navigating above the root").
  `R("R") [X("X") [c [(empty)]], Y("Y")]` was written as `41 52 41 58 40 80 81 59 00`: `Y` was lost.

Since 6.0.0 the reference reader works on non-seekable streams, handles partial reads and never reads beyond the terminator.

## 12. Implementation notes

* **No recursion.** Trees can be arbitrarily deep (a malicious stream may contain millions of `40` bytes). Use explicit stacks for encoding, decoding, removing auxiliary nodes, copying and freeing trees. Where destructors are recursive (e.g. nested owned children in Rust or C++), freeing a deep tree overflows the stack as well: store the nodes in an arena (an array of nodes referring to each other by index) or free them iteratively.
* **Linear removal of auxiliary nodes.** Remove the auxiliary children of a node in one pass (e.g. `retain`), not one by one; otherwise a stream with many auxiliary siblings takes quadratic time.
* **Buffers.** One buffer of 524289 bytes (control byte + largest payload) is enough for encoding, 524288 bytes for decoding.
* **Lengths.** The format does not limit the length of a content; a content of more than 512 KiB is simply a sequence of chunks. Implementations SHOULD use 64-bit lengths.
* **Untrusted input.** The format has no length prefix to validate: a decoder simply grows the contents. Limit the total number of bytes or nodes if the input is untrusted.
* **Equality.** Two trees are equal for the format if they have the same structure (the same nodes, in the same order, empty nodes included) and the same contents; absent and zero-length contents are the same.
