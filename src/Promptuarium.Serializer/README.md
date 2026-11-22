# Promptuarium.Serializer

A general serializer between .NET objects and Promptuarium trees, in the layout of [SERIALIZER_FORMAT.md](../../SERIALIZER_FORMAT.md):
every value is a holder node with a TYPE child and, for scalars, a VALUE child. There is no intermediate representation
(no JSON): a `byte[]` is a BLOB, the bytes as they are. The files are signed with `PRMT:GEN1-LE` or `PRMT:GEN2-LE`
(general objects) by default; configuration files have `PRMT:CFG1-LE` or `PRMT:CFG2-LE`
(`PromptuariumSerializerOptions.ConfigurationSignature`). It writes **version 2** of the layout: every number, date,
time and UUID has a type of its own, and an `Element` is a nested tree (`CONTAINER`); a file is signed with the lowest
version that holds it (see [Versions](#versions)).

The serializer is **experimental**: the API and the format can still change. The assembly is marked
`[Experimental("PRMTSER000")]`, so using it needs an opt-in, e.g. in the project file:

```xml
<PropertyGroup>
  <NoWarn>$(NoWarn);PRMTSER000</NoWarn>
</PropertyGroup>
```

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
| `sbyte`, `short`, `int`, `long` (and `nint`), `Int128` | `INT8`, `INT16`, `INT32`, `INT64`, `INT128` |
| `byte`, `ushort`, `uint`, `ulong` (and `nuint`), `UInt128` | `UINT8`, `UINT16`, `UINT32`, `UINT64`, `UINT128` |
| `BigInteger` | `BIGINT`, two's complement, the shortest form |
| `Half`, `float`, `double` | `FLOAT16`, `FLOAT32`, `FLOAT64`; NaN and the infinities too |
| `decimal` | `DECIMAL`, its canonical text (`"12.750"`, the scale kept) |
| `string`, `char` | `UTF8_STRING` |
| enums | `UTF8_STRING`, the name (`"Read, Write"` for flags), or the number with `[PromptuariumType]` (see below); read from a name in any case, or a number |
| `Guid` | `UUID`, 16 bytes in the order of RFC 9562 |
| `DateOnly`, `TimeOnly`, `TimeSpan` | `DATE`, `TIME`, `DURATION` |
| `DateTime` | `DATETIME` (`Kind` `Unspecified`), or `TIMESTAMP` (`Utc` with offset 0, `Local` with its offset); the kind comes back |
| `DateTimeOffset` | `TIMESTAMP`, the instant and the offset |
| `Version`, `Uri`, `IPAddress`, `IPNetwork` | `UTF8_STRING`, e.g. `"8.2.2"`, the original string of the `Uri` (absolute or relative), `"2001:db8::1"`, `"10.0.0.0/8"` (default types, see [Text types](#text-types-addtexttype)) |
| `byte[]` | `BLOB` |
| `Element` | `CONTAINER`, a nested tree (see [Nested trees](#nested-trees-container)) |
| string-keyed dictionaries (`Dictionary<string, T>`, `IReadOnlyDictionary<string, T>`, …) | `OBJECT` |
| objects and structs: their public properties with a public getter | `OBJECT` |
| other collections (`T[]`, `List<T>`, `IReadOnlyList<T>`, `HashSet<T>`, …) | `ARRAY` |
| `null` | `NULL` |

Read into `object`, a file gives `Dictionary<string, object?>`, `List<object?>`, `bool`, `long` (`ulong`, `Int128`,
`UInt128`, `BigInteger` for `UINT64`, `INT128`, `UINT128`, `BIGINT`), `double` (`float`, `Half`), `decimal`, `string`,
`Guid`, `DateOnly`, `TimeOnly`, `DateTime`, `DateTimeOffset`, `TimeSpan`, `byte[]`, `Element` or `null`, so a file of
any shape can be read without a type.

## Nested trees: `CONTAINER`

An `Element` property is stored as a `CONTAINER` (SERIALIZER_FORMAT.md, 6.6): the bytes of the whole tree, as in a file
of its own. The nested tree can be any Promptuarium tree: a file of this layout, a ppak archive, a tree built by hand;
the serializer does not look into it.

```csharp
public sealed class Envelope
{
    public string Sender { get; set; } = "";
    public Element? Payload { get; set; }        // CONTAINER
}

var payload = PromptuariumSerializer.ToElement(new Order { Id = 42 });   // a file of its own, PRMT:GEN1-LE
byte[] bytes = PromptuariumSerializer.Serialize(new Envelope { Sender = "shop", Payload = payload });

Envelope envelope = PromptuariumSerializer.Deserialize<Envelope>(bytes);
Order order = PromptuariumSerializer.Deserialize<Order>(envelope.Payload!);   // the nested file, when it is needed
```

* Reading decodes the nested tree into an `Element` right away (into `object` too): the value is checked anyway, so it
  is decoded once. It is not read further: what the tree means is told by its own signature, and the application
  reads it (e.g. with `Deserialize<T>(element)`) when it needs it.
* The value must be one complete stream: a value that ends without its terminator, has bytes after it, or is empty is
  rejected (`InvalidDataException`).
* The limits of the options cover the nested tree: it is decoded within `LoadOptions`, and its depth counts with the
  depth of the file (its root is one level below the member, `MaxDepth`), when written and when read.
* `[PromptuariumType(PromptuariumType.Container)]` on a `byte[]` stores bytes that are already a tree, checked when
  written, and gives them back as they are (checked) when read.
* A file with a `CONTAINER` is of version 2, whatever the version of the nested file.

## Versions

A file is signed with the **lowest version that holds it** (SERIALIZER_FORMAT.md, 7): a tree with only the types of
version 1 (`NULL`, `BOOLEAN`, `OBJECT`, `ARRAY`, a finite `FLOAT64`, `UTF8_STRING`, `BLOB`) gets `PRMT:GEN1-LE`
(`PRMT:CFG1-LE`), so a reader of version 1 still reads it; any other tree `PRMT:GEN2-LE` (`PRMT:CFG2-LE`). A signature
of an application is written as it is. There is no option to write version 1: a property gets the form of version 1 by
its declaration (`[PromptuariumType(Float64)]` on an `int`, `[PromptuariumType(Utf8String)]` on a `Guid` or a
`DateTime`, see [the type control](#the-experimental-type-control)), or by a type of version 1 (a `double`, a
`string`).

Reading accepts both versions of the kind of `Signature`, and rejects a file of version 1 with a type of version 2 (or a
NaN or an infinity). It is lenient where nothing is lost:

* any integer type is read into any integer type (or `BigInteger`) the value fits;
* a `FLOAT64` is read into an integer type if it is a whole number that fits (the files of version 1);
* an integer is read into a `double`, a `float`, a `Half` or a `decimal`, and a floating point number into a smaller
  floating point type, only if the value is held exactly (a `FLOAT64` into a `decimal` as in version 1);
* the text of version 1 is still read into the dates, the times and `Guid`.

The root is always an `OBJECT`: an object or a dictionary.

## Reading

* Member names match **regardless of case** (as in `IConfiguration`), unless
  `PropertyNameCaseInsensitive = false`.
* Unknown members are **validated and ignored**; so are computed (get-only) properties, and the comments.
* A type without a parameterless constructor, e.g. a **positional record**, is created through its public constructor
  whose parameters are its members; a missing parameter gets its default value.
* A file with another signature (or a root with data) is rejected, unless the `UnexpectedSignature` callback decides
  otherwise (see [Signatures](#signatures)).
* Everything the specification requires a reader to reject is rejected with `InvalidDataException`: unknown metadata, reserved or unknown type codes, invalid booleans, wrongly sized values,
  a `DECIMAL` that is not canonical, a `BIGINT` that is not in its shortest form, dates and times out of range, a type
  of version 2 in a file of version 1, a `CONTAINER` that is not one complete stream, invalid UTF-8, malformed holders. So is a file that does not fit the type (a string for an `int`, `1.5` for an `int`,
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

## Signatures

The signature tells the kind of the file; the layout is the same for every signature. A reader requires `Signature`,
and asks `UnexpectedSignature` about every other file; it gets the metadata of the root as a string (Latin-1, byte
by byte). The answer:

| `SignatureAction` | |
|-------------------|---|
| `Reject` | `InvalidDataException` (also when there is no callback) |
| `Accept` | the file is read by the usual rules |
| `Skip` | the file is read as if it had no members: `Deserialize` gives the default values, `Populate` changes nothing |

The callback can also throw an exception of its own. Configuration layers that take general files too, and skip the
rest:

```csharp
var options = new PromptuariumSerializerOptions
{
    Signature = PromptuariumSerializerOptions.ConfigurationSignature,
    UnexpectedSignature = signature => signature == PromptuariumSerializerOptions.DefaultSignature
        ? SignatureAction.Accept
        : SignatureAction.Skip,
};
```

## Comments

`[PromptuariumComment]` writes a comment with the value, a COMMENT node right after its TYPE node
([SERIALIZER_FORMAT.md, 5.1](../../SERIALIZER_FORMAT.md#51-comments)), for the people and the editors (e.g. PStudio)
that read the file. Reading ignores the comments.

```csharp
[PromptuariumComment("The audio settings")]   // in the root (or in the holder of every Audio object)
public sealed class Audio
{
    [PromptuariumComment("The sample rate, in Hz")]   // in the holder of the member
    public int SampleRate { get; set; } = 44100;
}
```

The comment of a property wins over the comment of its type.

## Control

| Attribute | Effect |
|-----------|--------|
| `[PromptuariumName("sample-rate")]` | the property is stored under another name |
| `[PromptuariumIgnore]` | the property is neither written nor read |
| `[PromptuariumComment("In Hz")]` | a comment written with the value (a COMMENT node, [section 5.1](../../SERIALIZER_FORMAT.md#51-comments)): on a property, in its member; on a class or a struct, in its objects (in the root for the root object), unless the property has one; readers ignore it |
| `[PromptuariumType(PromptuariumType.Float64)]` | **experimental** (`PRMTSER001`): the type the property is stored as; `Utf8String` stores the text of an `IParsable` type, also on a class or a struct; see below |

`PromptuariumSerializerOptions`:

| Option | Default | |
|--------|---------|---|
| `Signature` | `PRMT:GEN1-LE` | the root metadata, 1 to 15 ASCII bytes; a reader requires it. A signature of the layout names its kind: the version follows the content, and both versions are read |
| `UnexpectedSignature` | `null` (reject) | what a reader does with a file of another signature |
| `PropertyNameCaseInsensitive` | `true` | |
| `MaxDepth` | `64` | the deepest nesting written or read; it stops cycles, too |
| `LoadOptions` | 16 MiB, 1 000 000 nodes | the limits of loading; `null` for none |
| `AddTextType<T>(format, parse)` | | stores every `T` as its text (see [Text types](#text-types-addtexttype)) |
| `AddBlobType<T>(toBytes, fromBytes)` | | stores every `T` as its bytes (see [Binary types](#binary-types-addblobtype)) |
| `ClearDefaultTypes()` | | removes the default text types (`Version`, `Uri`, `IPAddress`, `IPNetwork`) |

### The experimental type control

`PromptuariumType` lists every type code of the specification, the reserved ones (`Float8`, `Float128`) too, and
`[PromptuariumType(...)]` states the type a property is stored as; reading requires the file to have that type (`NULL`
is always allowed). It chooses another form than the natural one:

* an integer type for an integer (`Int64` on an `int`, `BigInt` on a `long`), if the value fits;
* a floating point type for a number (`Float64` on an `int`, a `long`, a `decimal`, a `float`: the form of version 1;
  `Float32` on a `double`), if the type holds the value exactly (a `long` beyond 2⁵³ as `Float64` is rejected);
* `Utf8String` for a date, a time or a `Guid`: the text of version 1 (`"O"` for the dates, `"c"` for a `TimeSpan`,
  `"D"` for a `Guid`); for another type, see below;
* an integer type or `Float64` for an enum: its number instead of its name (see below).

A value that does not fit the declared type is rejected (`NotSupportedException`), and so is a reserved type. Both
types are marked `[Experimental("PRMTSER001")]`, so using them needs an opt-in:

```csharp
#pragma warning disable PRMTSER001
public sealed class Counter
{
    [PromptuariumType(PromptuariumType.Float64)]   // FLOAT64, the form of version 1
    public long Value { get; set; }
}
```

#### Enums: by name or by number

An enum is stored by its name by default (`UTF8_STRING`, `"Read, Write"` for flags). An integer type (`Int32`,
`UInt8`, ...) or `Float64` (the form of version 1) stores its number instead; the number must fit the type:

* on a property, for that property;
* on the enum type itself, for every value of the type: as a member, and in collections and dictionaries too;
* a property can declare another form than its type, e.g. `Utf8String` (the name) on a property of an enum declared
  as `Int32`.

The name survives the renumbering of the enum, the number survives its renaming (and is shorter). Reading takes both
forms anyway, so a file can move from one to the other.

```csharp
#pragma warning disable PRMTSER001
[PromptuariumType(PromptuariumType.UInt8)]       // every Color is a UINT8
public enum Color { Red, Green, Blue }

public sealed class Pixel
{
    public Color Color { get; set; }             // UINT8 2

    [PromptuariumType(PromptuariumType.Utf8String)]
    public Color Background { get; set; }        // "Blue"

    [PromptuariumType(PromptuariumType.Int32)]
    public DayOfWeek Day { get; set; }           // INT32 5
}
```

#### Text: `Utf8String` on a type that is not a text of its own

`[PromptuariumType(PromptuariumType.Utf8String)]` on a property of a type that is not stored as `UTF8_STRING` anyway
(e.g. an `IPAddress`, a `BigInteger`, an `int`) stores the text of the value:

* written with `IFormattable.ToString(null, CultureInfo.InvariantCulture)`, or `ToString()`;
* read with `IParsable<T>.TryParse(text, CultureInfo.InvariantCulture, out ...)`; a text that does not parse is
  rejected (`InvalidDataException`);
* the type must implement `IParsable<T>`, or be registered with `AddTextType` (`NotSupportedException` otherwise,
  both when writing and when reading);
* the text must give back an equal value (`Equals`), or the value is rejected when written (`NotSupportedException`),
  so a lossy text is never stored; records and the .NET types have such an `Equals`.

On a class or a struct, every value of the type is stored as its text: as a member, and in collections and
dictionaries too (such a value can not be the root). Only `Utf8String` can be declared on a type (on an enum, its
forms, see above).

```csharp
#pragma warning disable PRMTSER001
public sealed class Server
{
    [PromptuariumType(PromptuariumType.Utf8String)]
    public IPAddress Address { get; set; } = IPAddress.Loopback;   // "127.0.0.1"
}

[PromptuariumType(PromptuariumType.Utf8String)]
public readonly record struct Rgb(byte R, byte G, byte B) : IParsable<Rgb> { /* "#RRGGBB" */ }
```

For the types that are stored as `UTF8_STRING` anyway (`string`, `char`, enums, the dates, `TimeSpan`, `Guid`) and
for `object`, `Utf8String` only confirms the type, as before. The file is not different: an ordinary `UTF8_STRING`.

#### Text types: `AddTextType`

A type that implements no `IParsable<T>` (e.g. `Version`, `Uri`), or that is not the application's own (so no
attribute can be put on it), is registered in the options with its own format and parse:

```csharp
var options = new PromptuariumSerializerOptions()
    .AddTextType<Version>(version => version.ToString(), Version.Parse)
    .AddTextType<Uri>(uri => uri.OriginalString, text => new Uri(text, UriKind.RelativeOrAbsolute));
```

Every value of a registered type is stored as its text (`UTF8_STRING`), as a member and in collections and
dictionaries too, without an attribute; a registration wins over a `[PromptuariumType(Utf8String)]` on the type, and
serves a property declared as `Utf8String`, too. The rules are the ones above: the text must give back an equal value
when written, a text that `parse` does not take (a `FormatException`, `ArgumentException` or `OverflowException`, or
null) is rejected when read. The types that are stored anyway (numbers, `bool`, strings, chars, enums, the dates,
`TimeSpan`, `Guid`, `byte[]`, `object`) can not be registered. The options are set up before they are used: neither
`PromptuariumSerializerOptions.Default` nor options already used can be changed (`InvalidOperationException`).

Some types are stored as text by default, without a registration: `Version` (`"8.2.2"`), `Uri` (its original
string, absolute or relative), `IPAddress` (IPv4 and IPv6, `"192.168.0.1"`, `"2001:db8::1"`) and `IPNetwork`
(`"10.0.0.0/8"`). A registration of the application wins over them; `ClearDefaultTypes()` removes them all:

```csharp
var options = new PromptuariumSerializerOptions().ClearDefaultTypes();
```

#### Binary types: `AddBlobType`

A type whose bytes are shorter than its text can be registered with its own bytes instead; every value of it is
stored as a `BLOB`, by the same rules as a text type (as a member and in collections and dictionaries too, the bytes
must give back an equal value, bytes that `fromBytes` does not take are rejected), and a property of it declared as
`[PromptuariumType(PromptuariumType.Blob)]` uses the registration (a `Blob` property of another type than `byte[]`
needs one, `NotSupportedException` otherwise):

```csharp
var options = new PromptuariumSerializerOptions()
    .AddBlobType<Point3>(point => point.ToBytes(), Point3.FromBytes);
```

A `BLOB` is raw bytes: a reader without the type (into `object`, `pcfg`, PStudio, another language) sees bytes, not
the value, so text is the better choice unless the size matters. A type is registered once, as text or as bytes.

## Writing

`NotSupportedException` for what can not be stored: a scalar or a collection as the root, dictionaries with keys
other than strings, nesting deeper than `MaxDepth` (a cycle), a value that does not fit its declared
`PromptuariumType` (e.g. a `long` beyond 2⁵³ declared as `Float64`, a `decimal` with more than 15 digits as `Float64`,
300 as `Int8`). A NaN is written as the canonical quiet NaN.

## Tests

```sh
dotnet test tests/Promptuarium.Serializer.Tests
```

The tests check the test vectors of the specification byte for byte, every rejection rule, the mapping of every
supported type, layering, constructors, the attributes and the options.
