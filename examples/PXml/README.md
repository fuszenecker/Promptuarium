# pxml

`pxml` converts an XML document to a Promptuarium file, and back, without losing a node: elements, attributes
(namespace declarations too), text, CDATA sections, comments, whitespace, processing instructions, entity
references, the XML declaration and the document type.

```sh
pxml pack XML-FILE FILE      # converts the XML document to a pxml file
pxml unpack FILE XML-FILE    # converts a pxml file back to XML
```

```sh
dotnet run --project examples/PXml -- pack examples/PXml/sample.xml sample.prmt
dotnet run --project examples/PXml -- unpack sample.prmt sample.xml
file -m misc/promptuarium.magic sample.prmt     # sample.prmt: Promptuarium container, PXML1
```

## Layout

Every level of the XML tree is **two levels** of the Promptuarium tree: a **node**, and below it its
**attributes** and the **`#children`** info, whose children are the child nodes.

```text
root                          metadata "PRMT:PXML1" (the signature, BINARY_FORMAT.md 10.3)
├── node                      an element: data = its name ("dc:title"), metadata empty
│   ├── attribute             metadata = the name ("id", "xmlns:dc"), data = the value
│   ├── ...
│   └── info "#children"      its children are the child nodes of the element
│       ├── node              (the next level of the XML tree)
│       └── ...
├── node "#text"              text: metadata = the kind, data = the text
├── node "#cdata"             a CDATA section
├── node "#comment"           a comment
├── node "#whitespace"        whitespace between the markup
├── node "#entity"            a reference to an entity: data = its name ("publisher" for &publisher;)
├── node "#pi"                a processing instruction: data = the target, info "#value" = its content
├── node "#xml-declaration"   infos "version", "encoding", "standalone"
└── node "#doctype"           data = the name; infos "#public", "#system", "#subset"
```

The kinds and the infos start with `#`, which can not start an XML name, so they never collide with an element
or an attribute. An element with a `#children` info has an end tag even without children (`<a></a>`); one
without it is an empty element (`<a/>`). The names are kept with their prefixes; the namespaces are resolved
from the `xmlns` attributes when the document is built again.

## What is kept, and what is not

The document is kept as XML parsers see it, so `pack`, `unpack` and `pack` again give the same tree, byte for
byte. Formatting that is not part of the document is not kept: a byte order mark, the whitespace inside a tag
(between attributes, before `/>`), and the quotes around the attribute values (`unpack` writes `<a />` and
`"`). [`sample.xml`](sample.xml) is written that way, so it comes back byte for byte.

The XML file is read without fetching anything from the network (no external DTD or entity), and the entities
can expand to at most 1 MB.
