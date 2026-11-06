# pcfg

`pcfg` shows and edits Promptuarium configuration files: containers signed `PRMT:CFG1-LE`, in the layout of the serializer ([SERIALIZER_FORMAT.md](../../SERIALIZER_FORMAT.md)). Where [pedit](../PEdit/README.md) sees nodes, metadata and data, `pcfg` knows the tags (NAME, TYPE, ITEM, VALUE, COMMENT) and the type codes, so it shows a configuration by its values, with their types and comments:

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
```

| Command   | What it does |
|-----------|--------------|
| `new`     | Creates a configuration without members (with `--comment`, a comment about the file). |
| `show`    | Shows the configuration (or the value of `KEY`) as a tree, or as `key = value` lines (`--flat`). The comments are `#` lines before the value they are about. A string is quoted, a BLOB is `hex:...` (shortened to `--max-bytes`, 32 by default). |
| `get`     | Prints a value as it is: a string without quotes, a BLOB as hex, a number, `true`, `false` or `null`; an object or an array as a tree. `--yaml` prints YAML, `--json` prints JSON, `--out` writes the bytes of the value into a file. |
| `set`     | Sets a value. The missing objects on the way are created; an index of an array must exist or be the next one (it appends). The comments of the value are kept; `--comment` replaces them. |
| `delete`  | Deletes a value; every member with the name, so that an earlier one does not come back. |
| `rename`  | Renames a member of an object. |
| `comment` | Replaces the comments of a value (of the file: key `''`) with `TEXT`; `--add` appends it; without `TEXT` they are removed. |
| `import`  | Creates a configuration from a YAML file or the standard input (`-`), with its comments. A `.json` file, or any source with `--json`, is read as JSON, e.g. an `appsettings.json`; comments and trailing commas are allowed there, but its comments are not kept. |
| `export`  | Writes the configuration (or a value) as YAML: a BLOB is `!!binary`, a comment is a `#` line before its value, so importing it gives the configuration back. With `--json`, or into a `.json` file, it writes JSON: a BLOB is a base64 string, the comments are left out, so the JSON is not a full copy. |
| `check`   | Checks the file by SERIALIZER_FORMAT.md: the signature, the tags, the type codes (a reserved one is an error), the holders, the values, the comments. A repeated name is a warning (the last one wins); `--strict` makes it an error. |

Every command takes `--signature SIG`: the signature of the file, `PRMT:CFG1-LE` by default (e.g. `PRMT:GEN1-LE` for the general objects of the serializer). The editing commands save the file in place or into `--output FILE`, through a temporary file.

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

A value without a form keeps the type of the existing value: `pcfg set app.cfg Port 9090` stays a number, a string stays a string. A new value is a boolean (`true`, `false`), a number (as in JSON: `8080`, `1.5`, but not `1.0.3`), or else a string. Use a form when the guess is not right (`string:8080`).

### YAML

`pcfg` reads one YAML document, by the core schema of YAML 1.2. A plain scalar is null (`null`, `~` or nothing), a boolean (`true`, `false`), a number (also `0x10`, `0o17`) or else a string, so `yes` and `no` are strings. A quoted or a block scalar is a string, `!!binary` is a BLOB; the tags `!!str`, `!!int`, `!!float`, `!!bool` and `!!null` are understood too. When it writes YAML, it quotes the strings that a reader (of YAML 1.1 too) could take for something else.

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
