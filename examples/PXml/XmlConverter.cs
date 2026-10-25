using System.Xml;
using Promptuarium;
using static PXml.PXmlFormat;

namespace PXml;

/// <summary>
/// Converts an XML document to a pxml tree, and back. Every node of the document is kept: elements, attributes
/// (namespace declarations too), text, CDATA, comments, processing instructions, entity references, the XML
/// declaration and the document type. The whitespace between the markup is formatting, so it is not kept
/// (except where xml:space="preserve" says it is content).
/// </summary>
internal static class XmlConverter
{
    private const string XmlNamespace = "http://www.w3.org/XML/1998/namespace";
    private const string XmlnsNamespace = "http://www.w3.org/2000/xmlns/";

    /// <summary>Reads an XML file: the entity references are kept, the whitespace between the markup is not, and nothing is fetched from the network.</summary>
    public static XmlDocument Load(string path)
    {
        var document = new XmlDocument { XmlResolver = null };

        // XmlTextReader (unlike XmlReader.Create) can keep the references of the general entities
        using var reader = new XmlTextReader(path) { DtdProcessing = DtdProcessing.Parse, XmlResolver = null, EntityHandling = EntityHandling.ExpandCharEntities };
        document.Load(reader);
        return document;
    }

    /// <summary>The pxml tree of a document.</summary>
    public static Element ToTree(XmlDocument document)
    {
        Element root = new ElementBuilder().WithSignature(Data.FromAsciiString(Signature)).Build();
        root.Add(document.ChildNodes.Cast<XmlNode>().SelectMany(ToNodes));
        return root;
    }

    /// <summary>The document of a pxml tree.</summary>
    /// <exception cref="InvalidDataException">The tree is not a pxml tree</exception>
    public static XmlDocument ToXml(Element root)
    {
        if (!root.Data.IsEmpty || root.MetaData.AsAsciiString() != Signature)
        {
            throw new InvalidDataException($"Not a pxml file: the signature is not \"{Signature}\".");
        }

        var document = new XmlDocument { XmlResolver = null };
        var namespaces = new Dictionary<string, string> { ["xml"] = XmlNamespace, ["xmlns"] = XmlnsNamespace };

        foreach (Element node in ChildrenOf(root))
        {
            document.AppendChild(ToXmlNode(document, node, namespaces));
        }

        return document;
    }

    // One XML node is one node of the tree (an entity reference is replaced by its content)
    private static IEnumerable<Element> ToNodes(XmlNode node)
    {
        switch (node)
        {
            case XmlElement element:
                Element result = Node(null, element.Name);
                result.Add(element.Attributes.Cast<XmlAttribute>().Select(attribute => Info(attribute.Name, attribute.Value)));

                if (!element.IsEmpty)
                {
                    result.Add(new Element(null, Data.FromUtf8String(Children), element.ChildNodes.Cast<XmlNode>().SelectMany(ToNodes)));
                }

                yield return result;
                break;

            case XmlCDataSection cdata:
                yield return Node(CData, cdata.Value ?? string.Empty);
                break;

            case XmlText text:
                yield return Node(Text, text.Value ?? string.Empty);
                break;

            // Whitespace that xml:space="preserve" makes content
            case XmlSignificantWhitespace:
                yield return Node(Text, node.Value ?? string.Empty);
                break;

            case XmlComment comment:
                yield return Node(Comment, comment.Value ?? string.Empty);
                break;

            case XmlProcessingInstruction instruction:
                yield return Node(ProcessingInstruction, instruction.Target).Add(Info(Value, instruction.Data));
                break;

            case XmlDeclaration declaration:
                Element info = Node(Declaration, string.Empty).Add(Info("version", declaration.Version));

                if (declaration.Encoding.Length > 0)
                {
                    info.Add(Info("encoding", declaration.Encoding));
                }

                if (declaration.Standalone.Length > 0)
                {
                    info.Add(Info("standalone", declaration.Standalone));
                }

                yield return info;
                break;

            case XmlDocumentType type:
                Element doctype = Node(DocumentType, type.Name);
                AddIfPresent(doctype, PublicId, type.PublicId);
                AddIfPresent(doctype, SystemId, type.SystemId);
                AddIfPresent(doctype, InternalSubset, type.InternalSubset);
                yield return doctype;
                break;

            case XmlEntityReference reference:
                yield return Node(EntityReference, reference.Name);
                break;

            default:
                throw new NotSupportedException($"Unsupported XML node: {node.NodeType}.");
        }
    }

    private static void AddIfPresent(Element node, string name, string? value)
    {
        if (value is not null)
        {
            node.Add(Info(name, value));
        }
    }

    // The namespaces in scope are the ones of the ancestors, and the xmlns attributes of the element itself
    private static XmlNode ToXmlNode(XmlDocument document, Element node, Dictionary<string, string> namespaces)
    {
        string data = node.Data.AsUtf8String();

        switch (KindOf(node))
        {
            case null:
                var scope = new Dictionary<string, string>(namespaces);
                List<(string Name, string Value)> attributes = Attributes(node).ToList();

                foreach ((string name, string value) in attributes)
                {
                    if (name == "xmlns")
                    {
                        scope[string.Empty] = value;
                    }
                    else if (name.StartsWith("xmlns:", StringComparison.Ordinal))
                    {
                        scope[name[6..]] = value;
                    }
                }

                (string prefix, string localName) = Split(data);
                XmlElement element = document.CreateElement(prefix, localName, Resolve(scope, prefix, data));

                foreach ((string name, string value) in attributes)
                {
                    (string attributePrefix, _) = Split(name);
                    string uri = name == "xmlns" || attributePrefix == "xmlns" ? XmlnsNamespace
                        : attributePrefix.Length == 0 ? string.Empty
                        : Resolve(scope, attributePrefix, name);

                    XmlAttribute attribute = document.CreateAttribute(name, uri);
                    attribute.Value = value;
                    element.Attributes.Append(attribute);
                }

                foreach (Element child in ChildrenOf(node))
                {
                    element.AppendChild(ToXmlNode(document, child, scope));
                }

                element.IsEmpty = Find(node, PXmlFormat.Children) is null;
                return element;

            case Text:
                return document.CreateTextNode(data);
            case CData:
                return document.CreateCDataSection(data);
            case EntityReference:
                return document.CreateEntityReference(data);
            case Comment:
                return document.CreateComment(data);
            case ProcessingInstruction:
                return document.CreateProcessingInstruction(data, Find(node, Value) ?? string.Empty);
            case Declaration:
                return document.CreateXmlDeclaration(Find(node, "version") ?? "1.0", Find(node, "encoding"), Find(node, "standalone"));
            case DocumentType:
                return document.CreateDocumentType(data, Find(node, PublicId), Find(node, SystemId), Find(node, InternalSubset));
            case string kind:
                throw new InvalidDataException($"Unknown kind of node: \"{kind}\".");
        }
    }

    private static (string Prefix, string LocalName) Split(string name)
    {
        int colon = name.IndexOf(':', StringComparison.Ordinal);
        return colon < 0 ? (string.Empty, name) : (name[..colon], name[(colon + 1)..]);
    }

    private static string Resolve(Dictionary<string, string> scope, string prefix, string name)
    {
        return scope.TryGetValue(prefix, out string? uri) ? uri
            : prefix.Length == 0 ? string.Empty
            : throw new InvalidDataException($"The prefix of \"{name}\" is not declared.");
    }
}
