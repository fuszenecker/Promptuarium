# Promptuarium

[![CI](https://github.com/fuszenecker/Promptuarium/actions/workflows/ci.yml/badge.svg)](https://github.com/fuszenecker/Promptuarium/actions/workflows/ci.yml)
[![Release](https://img.shields.io/github/v/release/fuszenecker/Promptuarium)](https://github.com/fuszenecker/Promptuarium/releases/latest)
[![NuGet](https://img.shields.io/nuget/v/Promptuarium)](https://www.nuget.org/packages/Promptuarium/)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

## Project summary

Promptuarium is a versatile, tree-based data structure to store or exchange data and metadata efficiently (in binary format). Written in pure C#, but can be used in F#, as well.

Last reviewed: 25. Oct. 2026. Targets .NET 10.

## Promptuarium details

Promptuarium is a tree-based solution to store data and metadata in a file stream or exchange data and metadata through network streams efficiently.

However, Promptuarium doesn't use any compression algorithms, **it saves as many bytes as possible** (uses binary container format). Theoretically, there is **no size limit** in the binary container, but the .NET limits the data/metadata sizes to 8 exabytes.

The class has list- and **LINQ-friendly** operations to make it easy to use Promptuarium in any .NET project.

There are many-many **conversion operations** that simplifies storing and exchanging data (supported by the converter class):

* bool,
* byte, byte array,
* char,
* short, integer, long, (enumeration),
* float, double,
* decimal,
* DateTime, DateTimeOffset, TimeSpan,
* GUID,
* string (ASCII, UTF-8, UTF-16LE and UTF-32LE),
* **VarInt** and **VarUInt** saves as many bytes as possible.

The contents (`Data` and `MetaData`) are `ReadOnlyMemory<byte>`: a byte array can be assigned directly, and the conversions turn values into contents and back (all numbers are little-endian, on every platform). Storing other types is also easy, just serialize them into bytes.

```csharp
var node = new Element(Data.FromInt(42), Data.FromAsciiString("answer"));
int answer = node.Data.AsInt();

node.MetaData = new byte[] { 1, 2, 3 };
```

The format is **streamable**: trees can be loaded from and saved to non-seekable streams (e.g. network streams), several trees can follow each other in one stream, and the reader never reads beyond the end of a tree.
For untrusted sources, `LoadOptions` can limit the number of bytes and nodes. Arbitrarily deep trees are handled without recursion.

Without streams, a tree can be converted to bytes and back directly:

```csharp
byte[] bytes = tree.ToBytes();
var copy = Element.FromBytes(bytes);
```

```csharp
var tree = new Element(Data.FromUtf8String("Root"), null,
    new Element(Data.FromInt(42), Data.FromAsciiString("answer")),
    new Element(Data.FromVarInt(-3), null));

await tree.SaveAsync(networkStream);

var options = new LoadOptions { MaxBytes = 10 * 1024 * 1024, MaxNodes = 100_000 };
var received = await Element.LoadAsync(networkStream, options, cancellationToken);
```

The **structure is always preserved**: every node is saved and loaded in the same position, empty nodes included. `StructurallyEquals` compares two trees (structure and contents).

A file can carry a **signature** (see [File identification](BINARY_FORMAT.md#10-file-identification-optional-convention)), so that it can be recognized e.g. by the `file` command. The `ElementBuilder` creates such a root:

```csharp
var root = new ElementBuilder()
    .WithSignature(Data.FromAsciiString("PRMT:IMGZ5.6"))
    .WithChildren(content)
    .Build();
```

The signature is `PRMT`, optionally followed by `:` and a tag of up to 10 bytes (the kind and the version of the content). With the rules of [`misc/promptuarium.magic`](misc/promptuarium.magic) (append them to `~/.magic` or `/etc/magic`), `file` recognizes such files and shows the tag: `photo.prmt: Promptuarium container, IMGZ5.6`.

The `GetStatistics()` method gives information about i.e.

* the number of nodes,
* the depth of the tree,
* maximum and minimum of the length of the data/metadata,
* the minimum and maximum number of children,
* the number of nodes with and without data/metadata/children.

## Documentation

See also the [Wiki page](https://github.com/fuszenecker/Promptuarium/wiki/Examples), the [API Reference](docs/README.md) or the [Binary Format specification](BINARY_FORMAT.md).

The [`pedit`](tools/PEdit/README.md) command line tool creates, dumps and edits trees, and lists the chunks of a file. It is non-interactive and has JSON output, so it can be driven by scripts and AI agents, and it serves as the reference when the format is implemented in another language.

[PStudio](tools/PStudio/README.md) is the graphical editor (Avalonia, for Windows, Linux and macOS): a tree view, the contents as hex, UTF-8, ASCII or base64, import and export, a chunk view, and nested trees in their own windows.

Other implementations:

* TypeScript / JavaScript: [promptuarium-js](https://github.com/fuszenecker/promptuarium-js), byte-identical with this one (cross-checked with `pedit`).

## Unit tests

**Unit tests** help to keep the project code robust. The stability tests (random trees) are slow, they can be left out:

```sh
dotnet test --filter "TestCategory!=Slow"
```

## Packages and releases

* NuGet: [Promptuarium](https://www.nuget.org/packages/Promptuarium/)
* [GitHub releases](https://github.com/fuszenecker/Promptuarium/releases): the NuGet package, the symbol package, `pedit` and PStudio of every version.

A release is created automatically for every version tag:

1. set the new `Version` (and the release notes) in `src/Promptuarium.csproj`, and commit;
2. tag the commit with the version (`git tag 7.1.0`) and push the tag (`git push origin 7.1.0`).

The release workflow checks that the tag is the version of the package, runs the tests, and publishes the release as the latest one (a tag like `7.1.0-beta.1` is a pre-release). Releases of existing tags can be created by hand: `gh workflow run release.yml -f tag=7.0.0`.

## License

This code is distributed under the terms of the MIT license.

## Internals

Generate documentation:

```sh
cd src
roslynator.exe generate-doc .\Promptuarium.csproj -o ..\docs --host github --heading "Promptuarium API Reference"
```
