# pcfg

`pcfg` shows and edits Promptuarium configuration files: containers signed `PRMT:CFG1-LE` or `PRMT:CFG2-LE` (version 2 of the layout, with the typed numbers, decimals, UUIDs, dates and times), in the layout of the serializer ([SERIALIZER_FORMAT.md](../../SERIALIZER_FORMAT.md)). Where [pedit](../PEdit/README.md) sees nodes, metadata and data, `pcfg` knows the tags (NAME, TYPE, ITEM, VALUE, COMMENT) and the type codes, so it shows a configuration by its values, with their types and comments:

```text
$ pcfg show app.cfg
# Settings of the app
Logging {
  LogLevel {
    # the default level
    Default = "Information"
  }
}
Hosts [
  0 = "a.example"
]
Port = 8080
Key = hex:DEADBEEF

$ pcfg show app.cfg --flat
# Settings of the app
Logging:LogLevel:Default = "Information"
Hosts:0 = "a.example"
Port = 8080
Key = hex:DEADBEEF
```

As pedit, every operation is a single, non-interactive call, and the exit code tells the result. A file is checked before it is shown or edited: an invalid file is refused, and `pcfg check` lists every problem of it.

## Installing

pcfg comes with [PStudio](../PStudio/README.md) and pedit: the Debian package, the RPM package (`man pcfg`) and the Windows installer of every [release](https://github.com/fuszenecker/Promptuarium/releases) have it.

```sh
dotnet run --project tools/PCfg -- help
```

## Commands

```text
pcfg new     FILE [--comment TEXT] [--force]
pcfg show    FILE [KEY] [--flat] [--max-bytes N]
pcfg get     FILE KEY [--yaml|--json] [--out FILE]
pcfg set     FILE KEY VALUE [--comment TEXT] [--output FILE]
pcfg delete  FILE KEY [--output FILE]
pcfg rename  FILE KEY NEW-NAME [--output FILE]
pcfg comment FILE KEY [TEXT] [--add] [--output FILE]
pcfg import  FILE SOURCE|- [--json] [--force]
pcfg export  FILE [KEY] [--json] [--output FILE]
pcfg check   FILE [--strict]
pcfg help
pcfg version                         # also --version, -V
```

| Command   | What it does |
|-----------|--------------|
| `new`     | Creates a configuration without members (with `--comment`, a comment about the file). |
| `show`    | Shows the configuration (or the value of `KEY`) as a tree, or as `key = value` lines (`--flat`). The comments are `#` lines before the value they are about. A string is quoted, a BLOB is `hex:...` (shortened to `--max-bytes`, 32 by default), a value of version 2 has its type as it is set (`int32:8080`, `timestamp:2026-11-22T19:30:15+01:00`, `float64:NaN`). |
| `get`     | Prints a value as it is: a string without quotes, a BLOB as hex, a number, `true`, `false` or `null`, the text of a value of version 2 (`2026-11-22T19:30:15+01:00`); an object or an array as a tree. `--yaml` prints YAML, `--json` prints JSON, `--out` writes the bytes of the value into a file. |
| `set`     | Sets a value. The missing objects on the way are created; an index of an array must exist or be the next one (it appends). The comments of the value are kept; `--comment` replaces them. |
| `delete`  | Deletes a value; every member with the name, so that an earlier one does not come back. |
| `rename`  | Renames a member of an object. |
| `comment` | Replaces the comments of a value (of the file: key `''`) with `TEXT`; `--add` appends it; without `TEXT` they are removed. |
| `import`  | Creates a configuration from a YAML file or the standard input (`-`), with its comments. A `.json` file, or any source with `--json`, is read as JSON, e.g. an `appsettings.json`; comments and trailing commas are allowed there, but its comments are not kept. |
| `export`  | Writes the configuration (or a value) as YAML: a BLOB is `!!binary`, a value of version 2 has its type as a tag (`!int32 8080`), a comment is a `#` line before its value, so importing it gives the configuration back, byte for byte. With `--json`, or into a `.json` file, it writes JSON: a BLOB is a base64 string, an integer or a decimal a number with all its digits, a NaN or an infinity the string `"NaN"`, `"Infinity"` or `"-Infinity"`, a UUID, a date or a time its text, and the comments are left out, so the JSON is not a full copy. |
| `check`   | Checks the file by SERIALIZER_FORMAT.md: the signature, the tags, the type codes (a reserved one is an error), the holders, the values, the comments. A repeated name is a warning (the last one wins); `--strict` makes it an error. |
| `version` | Prints the version and the commit it was built from, e.g. `pcfg 8.4.0 (4c18335)`. Also `--version` or `-V`. |

Every command takes `--signature SIG`: the signature of the file, `PRMT:CFG1-LE` by default (e.g. `PRMT:GEN1-LE` for the general objects of the serializer). A signature of the layout names its kind: both versions are read (`PRMT:CFG1-LE` and `PRMT:CFG2-LE`), and a file is saved with the lowest version that holds it, so a value of version 2 makes it `PRMT:CFG2-LE`, and without one it is `PRMT:CFG1-LE` again. A file signed with version 1 that holds a type of version 2 (or a NaN) is invalid. The editing commands save the file in place or into `--output FILE`, through a temporary file.

### Keys

As in `IConfiguration`: the names of the members and the indexes of the elements, separated by `:`, e.g. `Logging:LogLevel:Default` or `Hosts:0:Name`. A `:` or a `\` in a name is escaped with a `\` (`a\:b`). The empty key `''` is the root.

### Values

| Form | Type |
|------|------|
| `string:TEXT` | UTF8_STRING |
| `number:1.5` | FLOAT64 |
| `bool:true`, `bool:false` | BOOLEAN |
| `null`, `{}`, `[]` | NULL, an empty OBJECT, an empty ARRAY |
| `hex:DEADBEEF`, `base64:3q2+7w==`, `file:PATH` | BLOB |
| `yaml:YAML` | any YAML value, e.g. `yaml:{Name: b, Ports: [80, 443]}` |
| `json:JSON` | any JSON value, e.g. `json:{"Name": "b", "Ports": [80, 443]}` |
| `int8:-5` … `int128:`, `uint8:255` … `uint128:` | INT8 … INT128, UINT8 … UINT128 (version 2) |
| `bigint:-18446744073709551617` | BIGINT, an integer of any size |
| `float16:1.5`, `float32:0.1`, `float64:NaN` | FLOAT16, FLOAT32, FLOAT64; also `NaN`, `Infinity`, `-Infinity` |
| `decimal:12.750` | DECIMAL, the canonical text (the scale kept) |
| `uuid:00112233-4455-6677-8899-aabbccddeeff` | UUID |
| `date:2026-11-22`, `time:19:30:15.5` | DATE, TIME |
| `datetime:2026-11-22T19:30:15` | DATETIME, without an offset |
| `timestamp:2026-11-22T19:30:15+01:00` | TIMESTAMP, the local time with its offset |
| `duration:-01:30:00`, `duration:1.02:03:04.5` | DURATION |

A value without a form keeps the type of the existing value: `pcfg set app.cfg Port 9090` stays a number (an `int32:` stays an INT32), a string stays a string. JSON numbers (in `json:` and `import`) are FLOAT64: a JSON number has no type of its own. A new value is a boolean (`true`, `false`), a number (as in JSON: `8080`, `1.5`, but not `1.0.3`), or else a string. Use a form when the guess is not right (`string:8080`).

### YAML

`pcfg` reads one YAML document, by the core schema of YAML 1.2. A plain scalar is null (`null`, `~` or nothing), a boolean (`true`, `false`), a number (also `0x10`, `0o17`) or else a string, so `yes` and `no` are strings. A quoted or a block scalar is a string, `!!binary` is a BLOB; the tags `!!str`, `!!int`, `!!float`, `!!bool` and `!!null` are understood too. The types of version 2 are local tags, the forms above without the colon: `!int32 8080`, `!decimal 12.750`, `!timestamp 2026-11-22T19:30:15+01:00`. When it writes YAML, it quotes the strings that a reader (of YAML 1.1 too) could take for something else.

A comment belongs to the value after it, a comment at the end of a line to the value of that line, and the comments before a `---` line to the file. Lines that follow each other are one comment. Other comments, e.g. at the end of the document, are left out.

Refused, because a configuration can not hold them or they can be abused: more documents in a file, aliases (`*name`, so merge keys too), keys that are not scalars, other tags, `.inf` and `.nan`.

## Example

```sh
pcfg import app.cfg appsettings.yaml
pcfg set app.cfg Logging:LogLevel:Default Debug --comment "the default level"
pcfg set app.cfg Hosts:1 b.example          # appends to the array
pcfg comment app.cfg '' "Settings of the app"
pcfg get app.cfg Logging:LogLevel:Default    # Debug
pcfg check app.cfg --strict
pcfg export app.cfg --output appsettings.yaml
pcfg export app.cfg --output appsettings.json    # JSON, by the extension
```

## Exit codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | Invalid command line: unknown command or option, missing argument, invalid key or value |
| 2 | The file or the key does not exist |
| 3 | The operation failed: invalid file, existing file (without `--force`), a key through a scalar, I/O error |

Errors are written to the standard error, prefixed with `pcfg:`.
