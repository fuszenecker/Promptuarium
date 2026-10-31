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
| [PXml](PXml/README.md) | `pxml`, which converts an XML document to a signed file (`PRMT:PXML1`) and back, without losing content: elements, attributes, namespaces, text, CDATA, comments, processing instructions, entities, the document type (the whitespace between the markup is formatting, so it is left out). |

The serializer between .NET objects and trees is not an example any more: it is the [Promptuarium.Serializer](../src/Promptuarium.Serializer/README.md) package. The archivers `ppak` and `ppakz` are not examples any more either: they are tools ([`tools/PPak`](../tools/PPak/README.md), [`tools/PPakZ`](../tools/PPakZ/README.md)), installed with PStudio and `pedit`.
