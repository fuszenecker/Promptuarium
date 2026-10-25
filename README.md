# Promptuarium

[![.NET](https://github.com/fuszenecker/Promptuarium/actions/workflows/dotnet.yml/badge.svg)](https://github.com/fuszenecker/Promptuarium/actions/workflows/dotnet.yml)

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

Storing other types are also easy, just serialize them into a stream or byte array.
The **events** (saving events on the nodes, loading events in `LoadOptions`) help you to load or save the node content dynamically, i.e. keep big data in file streams.

The format is **streamable**: trees can be loaded from and saved to non-seekable streams (e.g. network streams), several trees can follow each other in one stream, and the reader never reads beyond the end of a tree.
For untrusted sources, `LoadOptions` can limit the number of bytes and nodes. Arbitrarily deep trees are handled without recursion.

```csharp
var tree = new Element(Data.FromUtf8String("Root"), null,
    new Element(Data.FromInt(42), Data.FromAsciiString("answer")),
    new Element(Data.FromVarInt(-3), null));

await tree.SaveAsync(networkStream);

var options = new LoadOptions { MaxBytes = 10 * 1024 * 1024, MaxNodes = 100_000 };
var received = await Element.LoadAsync(networkStream, options, cancellationToken);
```

The `GetStatistics()` method gives information about i.e.

* the number of nodes,
* the depth of the tree,
* maximum and minimum of the length of the data/metadata,
* the minimum and maximum number of children,
* the number of nodes with and without data/metadata/children.

## Documentation

See also the [Wiki page](https://github.com/fuszenecker/Promptuarium/wiki/Examples), the [API Reference](docs/README.md) or the [Binary Format specification](BINARY_FORMAT.md).

## Unit tests

**Unit tests** help to keep the project code robust. The stability tests (random trees) are slow, they can be left out:

```sh
dotnet test --filter "TestCategory!=Slow"
```

## NuGet packages

* [Promptuarium](https://www.nuget.org/packages/Promptuarium/)

## License

This code is is distributed under the terms of the MIT license.

## Internals

Generate documentation:

```sh
cd src
roslynator.exe generate-doc .\Promptuarium.csproj -o ..\docs --host github --heading "Promptuarium API Reference"
```
