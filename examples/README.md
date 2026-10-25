# Examples

Small applications that show how the library is used. Every example builds with the solution, and CI runs
them:

```sh
dotnet run --project examples/GettingStarted
```

| Example | What it shows |
|---------|---------------|
| [GettingStarted](GettingStarted/Program.cs) | Building a tree, saving and loading it, typed contents (`Data`), `ToBytes`, statistics. |
| [Configuration](Configuration/Program.cs) | Settings in sections: the name in the metadata, the typed value in the data, lookup by path. |
| [PPak](PPak/README.md) | `ppak`, a file archiver: a directory tree with permissions, owners, times, symbolic links and CRC-32s in one signed file (`PRMT:PPAK1`). Loading files that come from anywhere: limits, signature, names, CRCs. |
| [PPakZ](PPakZ/README.md) | `ppakz`, ppak in a binary form (`PRMT:PPAKZ1`): the flags and the kinds are enums (one byte), the contents are gzip streams, decompressed with a size limit. |
| [PXml](PXml/README.md) | `pxml`, which converts an XML document to a signed file (`PRMT:PXML1`) and back, without losing content: elements, attributes, namespaces, text, CDATA, comments, processing instructions, entities, the document type (the whitespace between the markup is formatting, so it is left out). |
