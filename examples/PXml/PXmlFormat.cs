using Promptuarium;

namespace PXml;

/// <summary>
/// The layout of a pxml file. Every level of the XML tree is two levels of the Promptuarium tree: a node, and
/// below it its attributes and its children.
///
/// <code>
/// root                          metadata: the signature "PRMT:PXML1"
/// ├── node                      an element: data = its name (e.g. "x:item"), metadata empty
/// │   ├── attribute             metadata = the name of the attribute, data = its value (xmlns too)
/// │   ├── ...
/// │   └── info "#children"      its children are the child nodes of the element
/// ├── node "#text"              metadata = the kind of the node, data = its text
/// └── ...
/// </code>
///
/// The kinds and the infos start with '#', which can not start an XML name, so they never collide with an
/// element or an attribute.
/// </summary>
internal static class PXmlFormat
{
    /// <summary>The signature of the root (BINARY_FORMAT.md, 10.3): "file" shows it as "Promptuarium container, PXML1".</summary>
    public const string Signature = "PRMT:PXML1";

    /// <summary>The info whose children are the child nodes of an element. An element that has it, even without
    /// children, has an end tag (&lt;a&gt;&lt;/a&gt;); one without it is an empty element (&lt;a/&gt;).</summary>
    public const string Children = "#children";

    /// <summary>Text (data: the text).</summary>
    public const string Text = "#text";

    /// <summary>A CDATA section (data: its text).</summary>
    public const string CData = "#cdata";

    /// <summary>A reference to an entity of the document type (data: the name of the entity).</summary>
    public const string EntityReference = "#entity";

    /// <summary>A comment (data: its text).</summary>
    public const string Comment = "#comment";

    /// <summary>Whitespace between the markup (data: the whitespace).</summary>
    public const string Whitespace = "#whitespace";

    /// <summary>A processing instruction (data: its target, info "#value": its content).</summary>
    public const string ProcessingInstruction = "#pi";

    /// <summary>The content of a processing instruction.</summary>
    public const string Value = "#value";

    /// <summary>The XML declaration (infos "version", "encoding" and "standalone", like attributes).</summary>
    public const string Declaration = "#xml-declaration";

    /// <summary>The document type (data: its name; infos "#public", "#system" and "#subset").</summary>
    public const string DocumentType = "#doctype";

    public const string PublicId = "#public";
    public const string SystemId = "#system";
    public const string InternalSubset = "#subset";

    /// <summary>A node: the kind in the metadata (empty for an element), the name or the text in the data.</summary>
    public static Element Node(string? kind, string data) => new(Data.FromUtf8String(data), kind is null ? null : Data.FromUtf8String(kind));

    /// <summary>An attribute or an info: the name in the metadata, the value in the data.</summary>
    public static Element Info(string name, string value) => new(Data.FromUtf8String(value), Data.FromUtf8String(name));

    /// <summary>The kind of a node; null for an element.</summary>
    public static string? KindOf(Element node) => node.MetaData.IsEmpty ? null : node.MetaData.AsUtf8String();

    /// <summary>The value of an info of a node, or null.</summary>
    public static string? Find(Element node, string name)
    {
        return node.Children.FirstOrDefault(info => info.MetaData.AsUtf8String() == name)?.Data.AsUtf8String();
    }

    /// <summary>The attributes of a node: its infos whose names do not start with '#'.</summary>
    public static IEnumerable<(string Name, string Value)> Attributes(Element node)
    {
        return node.Children
            .Select(info => (Name: info.MetaData.AsUtf8String(), Value: info.Data.AsUtf8String()))
            .Where(attribute => !attribute.Name.StartsWith('#'));
    }

    /// <summary>The child nodes: the children of the root, or of the "#children" info of a node.</summary>
    public static IReadOnlyList<Element> ChildrenOf(Element node)
    {
        return node.Parent is null
            ? node.Children
            : node.Children.FirstOrDefault(info => info.MetaData.AsUtf8String() == Children)?.Children ?? [];
    }
}
