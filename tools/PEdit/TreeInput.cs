using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Promptuarium;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace PEdit;

/// <summary>
/// Reads a subtree in the form of the structured dump, in YAML or in JSON (a JSON document is YAML too). A node is a
/// mapping of metadata, data and children, all of them optional; its path is ignored, because the command tells the
/// place. A content has hex or utf8; its length is ignored. Without recursion: a tree can be very deep.
/// </summary>
internal sealed class TreeInput
{
    private const string NodeForm = "a node is a mapping of path, metadata, data and children";

    private readonly Parser parser;
    private readonly string source;

    private TreeInput(TextReader text, string source)
    {
        parser = new Parser(text);
        this.source = source;
    }

    /// <summary>The root of the subtree.</summary>
    public static Element Read(string text, string source)
    {
        using var reader = new StringReader(text);

        try
        {
            return new TreeInput(reader, source).Document();
        }
        catch (YamlException ex)
        {
            throw new PEditException(ExitCodes.Failed, $"{source} is not valid YAML or JSON (line {ex.Start.Line}): {ex.Message}");
        }
    }

    private Element Document()
    {
        Next();

        if (Next() is not DocumentStart || Next() is not MappingStart)
        {
            throw Invalid(NodeForm);
        }

        var open = new Stack<Element>();
        var node = new Element();

        while (true)
        {
            switch (Next())
            {
                case Scalar { Value: "children" }:
                    node = Next() is SequenceStart ? ChildOrSelf(node, open) : throw Invalid("children is a list of nodes");
                    break;
                case Scalar { Value: "metadata" }:
                    node.MetaData = Content("metadata");
                    break;
                case Scalar { Value: "data" }:
                    node.Data = Content("data");
                    break;
                case Scalar { Value: "path" }:
                    _ = ScalarValue("path");
                    break;
                case MappingEnd when open.Count == 0:
                    Next();
                    return Next() is DocumentStart ? throw Invalid("there is more than one document") : node;
                case MappingEnd:
                    Element parent = open.Pop();
                    parent.Add(node);
                    node = ChildOrSelf(parent, open);
                    break;
                default:
                    throw Invalid(NodeForm);
            }
        }
    }

    /// <summary>In the list of the children of a node: the next child, or the node itself at the end of the list.</summary>
    private Element ChildOrSelf(Element node, Stack<Element> open)
    {
        switch (Next())
        {
            case MappingStart:
                open.Push(node);
                return new Element();
            case SequenceEnd:
                return node;
            default:
                throw Invalid(NodeForm);
        }
    }

    private byte[] Content(string name)
    {
        ParsingEvent? first = Next();

        if (first is Scalar scalar && IsNull(scalar))
        {
            return [];
        }

        if (first is not MappingStart)
        {
            throw Invalid($"{name} is a mapping with hex or utf8");
        }

        string? hex = null;
        string? utf8 = null;

        for (ParsingEvent? next = Next(); next is not MappingEnd; next = Next())
        {
            switch (next)
            {
                case Scalar { Value: "hex" }:
                    hex = ScalarValue(name);
                    break;
                case Scalar { Value: "utf8" }:
                    utf8 = ScalarValue(name);
                    break;
                case Scalar { Value: "length" }:
                    _ = ScalarValue(name);
                    break;
                case Scalar { Value: "truncated" }:
                    if (ScalarValue(name) == "true")
                    {
                        throw Invalid($"{name} is truncated: dump without --max-bytes");
                    }

                    break;
                default:
                    throw Invalid($"{name} has hex, utf8, length and truncated only");
            }
        }

        return Bytes(name, hex, utf8);
    }

    private byte[] Bytes(string name, string? hex, string? utf8)
    {
        byte[]? text = utf8 is null ? null : Encoding.UTF8.GetBytes(utf8);

        if (hex is null)
        {
            return text ?? throw Invalid($"{name} has neither hex nor utf8");
        }

        try
        {
            byte[] bytes = Convert.FromHexString(hex);

            // An edited utf8 next to the old hex must not be lost silently
            return text is null || text.SequenceEqual(bytes) ? bytes : throw Invalid($"the hex and the utf8 of {name} differ: leave one of them out");
        }
        catch (FormatException ex)
        {
            throw Invalid($"the hex of {name} is invalid: {ex.Message}");
        }
    }

    /// <summary>The value of a scalar, or null for a YAML null.</summary>
    private string? ScalarValue(string name)
    {
        return Next() is Scalar scalar ? (IsNull(scalar) ? null : scalar.Value) : throw Invalid($"a value of {name} is not a scalar");
    }

    private static bool IsNull(Scalar scalar) => scalar.Style == ScalarStyle.Plain && scalar.Value is "" or "~" or "null";

    private ParsingEvent? Next() => parser.MoveNext() ? parser.Current : null;

    private PEditException Invalid(string reason) => new(ExitCodes.Failed, $"{source} can not be read: {reason}.");
}
