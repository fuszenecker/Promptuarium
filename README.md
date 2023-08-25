# Promptuarium

[![.NET](https://github.com/fuszenecker/Promptuarium/actions/workflows/dotnet.yml/badge.svg)](https://github.com/fuszenecker/Promptuarium/actions/workflows/dotnet.yml)

## Project summary

Promptuarium is a versatile, tree-based data structure to store or exchange data and metadata efficiently (in binary format). Written in pure C#, but can be used in F#, as well.

Last reviewed: 13. Aug. 2023.

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
The **events** help you to load the node content dynamically, i.e. keep big data in file streams.

The ```Statistics``` property gives information about i.e.

* the number of nodes,
* the depth of the tree,
* maximum and minimum of the length of the data/metadata,
* the minimum and maximum number of children,
* the number of nodes with and without data/metadata/children.

## Documentation

See also the [Wiki page](https://github.com/fuszenecker/Promptuarium/wiki/Examples) or the [API Reference](docs/README.md).

## Unit tests

**Unit tests** help to keep the project code robust.

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
