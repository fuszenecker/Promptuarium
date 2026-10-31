# Examples

Small applications that show how the library is used. Every example builds with the solution, and CI runs
them:

```sh
dotnet run --project examples/GettingStarted
```

| Example | What it shows |
|---------|---------------|
| [GettingStarted](GettingStarted/Program.cs) | Building a tree, saving and loading it, typed contents (`Data`), `ToBytes`, statistics. |
| [Configuration](Configuration/Program.cs) | Settings in sections with [Promptuarium.Serializer](../src/Promptuarium.Serializer/README.md), as configuration files (`PRMT:CFG1-LE`): plain classes with comments (`[PromptuariumComment]`, written into the file), the defaults and the user's changes on top (`Populate`), a layer of another kind skipped (`UnexpectedSignature`). |
| [PPak](PPak/README.md) | `ppak`, a file archiver: a directory tree with sizes, CRC-32s, permissions, owners, times and symbolic links in one signed file (`PRMT:PPAK1-LE`, specified in [PPAK_FORMAT.md](PPak/PPAK_FORMAT.md)). Only the names and the contents are mandatory, a discrepancy in the rest is a warning. Loading files that come from anywhere: limits, signature, names, a check before anything is written. |
| [PPakZ](PPakZ/README.md) | `ppakz`, ppak in a binary form (`PRMT:PPAKZ1`): the flags and the kinds are enums (one byte), the contents are gzip streams, decompressed with a size limit. |
| [PXml](PXml/README.md) | `pxml`, which converts an XML document to a signed file (`PRMT:PXML1`) and back, without losing content: elements, attributes, namespaces, text, CDATA, comments, processing instructions, entities, the document type (the whitespace between the markup is formatting, so it is left out). |

The serializer between .NET objects and trees is not an example any more: it is the [Promptuarium.Serializer](../src/Promptuarium.Serializer/README.md) package.
