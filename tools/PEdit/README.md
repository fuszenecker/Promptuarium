# pedit

`pedit` creates, dumps and edits Promptuarium trees from the command line. It is the command line interface of the reference implementation:

* every operation is a **single, non-interactive call**;
* with `--yaml` or `--json`, the output is **machine-readable**;
* the **exit code** tells the result;
* `chunks` decodes a file **directly from the specification** ([BINARY_FORMAT.md](../../BINARY_FORMAT.md)), independently of the library.

So it can be driven by scripts and AI agents, and it is the reference to compare another implementation (e.g. a port to another language) with.

## Installing

pedit comes with [PStudio](../PStudio/README.md), whose command line counterpart it is: every [release](https://github.com/fuszenecker/Promptuarium/releases) has

* a Debian package (amd64; self-contained, .NET is not needed) with both, and the man page of pedit (`man pedit`): `sudo apt install ./pstudio_8.1.0_amd64.deb` (it replaces the former `pedit` package);
* an RPM package (x86_64; self-contained) with the same: `sudo dnf install ./pstudio-8.1.0-1.x86_64.rpm` (Fedora, RHEL; on openSUSE `sudo zypper install`);
* a Windows installer (x64; .NET is not needed): `pedit.exe` is next to `pstudio.exe`, and the Start Menu has a command prompt for it.

## Running

```sh
dotnet run --project tools/PEdit -- help
# or, after building:
dotnet tools/PEdit/bin/Debug/net10.0/pedit.dll help
```

## Commands

```text
pedit new    FILE [--signature VALUE] [--force]
pedit dump   FILE [--path PATH] [--max-bytes N] [--yaml|--json]
pedit chunks FILE [--yaml|--json]
pedit add    FILE PARENT-PATH [--index N] [--metadata VALUE] [--data VALUE] [--tree FILE|-] [--output FILE] [--yaml|--json]
pedit set    FILE PATH [--metadata VALUE] [--data VALUE] [--tree FILE|-] [--output FILE] [--yaml|--json]
pedit delete FILE PATH [--output FILE] [--yaml|--json]
pedit get    FILE PATH metadata|data [--format hex|base64|utf8] [--out FILE]
pedit help
```

| Command  | What it does |
|----------|--------------|
| `new`    | Creates a file with an empty root. With `--signature`, the root is a signed root (see BINARY_FORMAT.md, section 10). Refuses to overwrite an existing file without `--force`. |
| `dump`   | Shows the tree: the nodes with their paths and contents, the children under their parents. `--path` dumps a subtree only; `--max-bytes` shortens the contents (default 32 bytes in the text form, no limit in YAML and JSON). |
| `chunks` | Lists the chunks of the file as they are in the stream: offset, control byte, direction, content type, size type, length, depth, kind, payload. It also reports the number of bytes consumed (up to and including the terminator) and the trailing bytes. |
| `add`    | Adds a new node to `PARENT-PATH`, at the end or at `--index` (0 = first child). Without `--metadata` and `--data` the new node is an empty node. With `--tree`, the new node is a whole subtree. Prints the path of the new node. |
| `set`    | Sets the metadata and/or the data of a node. `empty` clears a content. With `--tree`, it replaces the node, with its children, with a subtree. |
| `delete` | Deletes a node with its subtree. The root can not be deleted. |
| `get`    | Prints the metadata or the data of a node as hex (default), base64 or UTF-8, or writes the raw bytes into `--out FILE`. |

### Subtrees

`--tree FILE` (or `-`, the standard input) gives `add` and `set` a subtree, in the form of `dump --yaml` or `dump --json`, so a dump can be edited and written back:

```sh
pedit dump tree.prm --path /0 --yaml > part.yaml
pedit set tree.prm /0 --tree part.yaml
```

A hand-written subtree needs less:

```yaml
data: {utf8: "a"}
children:
  - metadata: {hex: "00"}
    data: {utf8: "hello"}
  - data: {hex: "FF00"}
```

`metadata`, `data` and `children` are optional. A content has `hex` or `utf8`; if it has both, they must be the same bytes, so an edited `utf8` is not lost next to the old `hex`. `path` and `length` are ignored, a content with `truncated: true` is refused. `--tree` can not be used together with `--metadata` and `--data`.

The editing commands (`add`, `set`, `delete`) save the file in place, or into `--output FILE`. The file is written into a temporary file first and replaced only when the save succeeded.

### Paths

`/` is the root, `/0` is its first child, `/0/2` is the third child of the first child. The indexes are 0-based.

### Values

The container stores bytes, so a value states how the bytes are given:

| Form          | Bytes |
|---------------|-------|
| `hex:4152`    | The bytes in hexadecimal; spaces and `-` are ignored (`hex:41 52`, `hex:41-52`). |
| `utf8:TEXT`   | The UTF-8 encoding of the text. |
| `ascii:TEXT`  | The ASCII encoding of the text; non-ASCII characters are an error. |
| `base64:QVI=` | The bytes in base64. |
| `file:PATH`   | The content of a file. |
| `empty`       | No content (the same as zero bytes). |

## Output

Text output is meant for people. For scripts and agents, use `--yaml` or `--json`: they have the same structure. In YAML every string is quoted, so a hex text such as `"4152"` stays a string.

`add`, `set`, `delete` and `new`:

```yaml
added: "/1"
```

`dump --yaml` returns the tree: the `children` of a node are nested in it. What is empty is left out: `metadata` and `data` if the content is empty, `children` if the node has none. `utf8` is `null` if the bytes are not valid UTF-8, `truncated: true` tells that `--max-bytes` shortened `hex` and `utf8` (`length` is always the whole length):

```yaml
path: "/"
children:
  - path: "/0"
    data: {length: 1, hex: "41", utf8: "A"}
    children:
      - path: "/0/0"
        metadata: {length: 2, hex: "FF00", utf8: null}
  - path: "/1"
    data: {length: 4, hex: "4142", truncated: true, utf8: "AB"}
```

The same with `--json`:

```json
{
  "path": "/",
  "children": [
    { "path": "/0", "data": { "length": 1, "hex": "41", "utf8": "A" } }
  ]
}
```

A very deep tree is written too, but other readers have limits of their own (e.g. 64 levels by default in `System.Text.Json`).

`chunks --json`:

```json
{
  "chunks": [
    { "offset": 0, "control": "41", "direction": "Down", "type": "data", "size": "linear",
      "length": 1, "kind": "node", "depth": 0, "payload": "52" },
    { "offset": 2, "control": "00", "direction": "Append", "type": "data", "size": "linear",
      "length": 0, "kind": "terminator", "depth": 0, "payload": "" }
  ],
  "consumed": 3,
  "trailing": 0,
  "error": null
}
```

The `kind` of a chunk is `node` (starts a node with content), `append` (more content of the same node), `auxiliary` (a zero-length node-creating chunk with `T = 0`: a placeholder or an Up marker), `empty node` (a zero-length node-creating chunk with `T = 1`) or `terminator`.

## Exit codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | Invalid command line: unknown command or option, missing argument, invalid path, index or value |
| 2 | The file or the node does not exist |
| 3 | The operation failed: invalid stream, existing file (without `--force`), deleting the root, I/O error |

Errors are written to the standard error, prefixed with `pedit:`.

## Example

Building the worked example of the specification (BINARY_FORMAT.md, section 9.1):

```sh
pedit new tree.prm
pedit set tree.prm / --data ascii:R
pedit add tree.prm / --data ascii:X          # added /0
pedit add tree.prm /0                        # added /0/0   (no content yet)
pedit add tree.prm /0/0 --data ascii:d       # added /0/0/0
pedit add tree.prm / --metadata ascii:m --data ascii:Y   # added /1

xxd -p tree.prm                              # 41524158404164808159216d00
pedit chunks tree.prm
```

## Using pedit as a reference for another implementation

1. **Encoder:** build the same tree with `pedit` and with the other implementation, and compare the files byte by byte. If they differ, compare `pedit chunks --json` of the two files: the first differing chunk shows where the encoders disagree.
2. **Decoder:** let the other implementation load files written by `pedit` (and the decoding-only test vectors of BINARY_FORMAT.md, section 9.2), and compare its tree with `pedit dump --json` of the same file.
3. **Both directions:** load a file of the other implementation with `pedit dump` (it must succeed, exit code 0), edit it with `pedit`, and load it back with the other implementation.

`chunks` does not use the library, only the specification, so it also helps when the library itself is suspected.
