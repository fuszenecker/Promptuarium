# Promptuarium.Serializer

A general serializer between .NET objects and Promptuarium trees, in the layout of [CONFIG_FORMAT.md](CONFIG_FORMAT.md)
(`PRMT:CFG1-LE`): every value is a holder node with a TYPE child and, for scalars, a VALUE child. There is no
intermediate representation (no JSON): a `byte[]` is a BLOB, the bytes as they are.

```csharp
using Promptuarium.Serializer;

byte[] bytes = PromptuariumSerializer.Serialize(settings);
Settings copy = PromptuariumSerializer.Deserialize<Settings>(bytes);

await PromptuariumSerializer.SerializeAsync(stream, settings);
Settings loaded = await PromptuariumSerializer.DeserializeAsync<Settings>(stream);
```

## Mapping

| .NET | Stored as |
|------|-----------|
| `bool` | `BOOLEAN` |
| `sbyte` … `ulong`, `float`, `double`, `decimal` | `FLOAT64` (an integer read back must be whole and in range) |
| `string`, `char` | `UTF8_STRING` |
| enums | `UTF8_STRING`, the name (`"Read, Write"` for flags); read from a name in any case, or a number |
| `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly` | `UTF8_STRING`, round-trip text (`"O"`), the kind and the offset kept |
| `TimeSpan`, `Guid` | `UTF8_STRING`, `"c"` and `"D"` |
| `byte[]` | `BLOB` |
| string-keyed dictionaries (`Dictionary<string, T>`, `IReadOnlyDictionary<string, T>`, …) | `OBJECT` |
| objects and structs: their public properties with a public getter | `OBJECT` |
| other collections (`T[]`, `List<T>`, `IReadOnlyList<T>`, `HashSet<T>`, …) | `ARRAY` |
| `null` | `NULL` |

Read into `object`, a file gives `Dictionary<string, object?>`, `List<object?>`, `bool`, `double`, `string`, `byte[]`
or `null`, so a file of any shape can be read without a type.

The root is always an `OBJECT`: an object or a dictionary.

## Reading

* Member names match **regardless of case** (as in `IConfiguration`), unless
  `PropertyNameCaseInsensitive = false`.
* Unknown members are **validated and ignored**; so are computed (get-only) properties.
* A type without a parameterless constructor, e.g. a **positional record**, is created through its public constructor
  whose parameters are its members; a missing parameter gets its default value.
* Everything the specification requires a reader to reject is rejected with `InvalidDataException`: another
  signature, unknown metadata, reserved or unknown type codes, invalid booleans, non-finite or wrongly sized numbers,
  invalid UTF-8, malformed holders. So is a file that does not fit the type (a string for an `int`, `1.5` for an `int`,
  `256` for a `byte`, ...).

## Layering

`Populate` applies a file onto an existing object: the members in the file overwrite, nested objects and dictionaries
are applied member by member (a get-only one too), arrays are replaced, and a `NULL` member keeps the value underneath.
Defaults with the user's changes on top:

```csharp
Settings settings = PromptuariumSerializer.Deserialize<Settings>(File.ReadAllBytes("defaults.prmt"));

if (File.Exists(userPath))
{
    PromptuariumSerializer.Populate(File.ReadAllBytes(userPath), settings);
}
```

## Control

| Attribute | Effect |
|-----------|--------|
| `[PromptuariumName("sample-rate")]` | the property is stored under another name |
| `[PromptuariumIgnore]` | the property is neither written nor read |
| `[PromptuariumType(PromptuariumType.Float64)]` | **experimental** (`PRMTSER001`): the type the property is stored as; see below |

`PromptuariumSerializerOptions`:

| Option | Default | |
|--------|---------|---|
| `Signature` | `PRMT:CFG1-LE` | the root metadata, 1 to 15 ASCII bytes; a reader requires it |
| `PropertyNameCaseInsensitive` | `true` | |
| `MaxDepth` | `64` | the deepest nesting written or read; it stops cycles, too |
| `LoadOptions` | 16 MiB, 1 000 000 nodes | the limits of loading; `null` for none |

### The experimental type control

`PromptuariumType` lists every type code of the specification, the reserved ones (`Int64`, `Float32`, …) too, and
`[PromptuariumType(...)]` states the type a property is stored as. Writing rejects a value that does not map to it,
reading rejects a file that has another type (`NULL` is always allowed). For now it can only confirm the type a value
maps to anyway, and a reserved type is rejected (`NotSupportedException`); it is where e.g. `Int64` instead of
`Float64` will be chosen once the format supports it. Both types are marked `[Experimental("PRMTSER001")]`, so using
them needs an opt-in:

```csharp
#pragma warning disable PRMTSER001
public sealed class Counter
{
    [PromptuariumType(PromptuariumType.Float64)]
    public long Value { get; set; }
}
```

## Writing

`NotSupportedException` for what can not be stored: a scalar or a collection as the root, `NaN` and infinity,
dictionaries with keys other than strings, number types without a mapping (`Int128`, `Half`, `nint`, …), nesting
deeper than `MaxDepth` (a cycle), a value that does not fit its declared `PromptuariumType`.

## Tests

```sh
dotnet test tests/Promptuarium.Serializer.Tests
```

The tests check the test vectors of the specification byte for byte, every rejection rule, the mapping of every
supported type, layering, constructors, the attributes and the options.
