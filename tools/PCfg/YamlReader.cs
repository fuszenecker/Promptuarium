using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Promptuarium;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace PCfg;

/// <summary>
/// Turns one YAML document into the content of a holder, by the core schema of YAML 1.2: a plain scalar is null, a
/// boolean, a number or a string (so "yes" is a string), a quoted or a block one is a string, !!binary is a BLOB.
/// A comment belongs to the value after it, a comment at the end of a line to the value of that line, and the comments
/// before a "---" line to the document. Other comments, e.g. at the end of the document, are left out. Aliases are
/// refused: they could make a small document huge.
/// </summary>
internal sealed partial class YamlReader
{
    private const string TagPrefix = "tag:yaml.org,2002:";

    private readonly Parser parser;
    private readonly string source;
    private readonly List<(int Line, string Text, bool Inline)> pending = new();
    private readonly List<(Element Holder, List<string> Comments)> commented = new();
    private List<string>? lineComments;
    private long line;

    private YamlReader(TextReader yaml, string source)
    {
        parser = new Parser(new Scanner(yaml, skipComments: false));
        this.source = source;
    }

    /// <summary>The content of the holder of a document, and the comments of the document.</summary>
    public static (Element[] Content, IReadOnlyList<string> Comments) Read(string yaml, string source)
    {
        using var text = new StringReader(yaml);

        try
        {
            return new YamlReader(text, source).Document();
        }
        catch (YamlException ex)
        {
            throw new PCfgException(ExitCodes.Failed, $"{source} is not valid YAML (line {ex.Start.Line}): {ex.Message}");
        }
    }

    /// <summary>The members of the root, from a document whose root is a mapping, and the comments of the document.</summary>
    public static (Element[] Members, IReadOnlyList<string> Comments) ReadRoot(string yaml, string source)
    {
        (Element[] content, IReadOnlyList<string> comments) = Read(yaml, source);

        if (content[0].Data.Span[0] != CfgLayout.Object)
        {
            throw new PCfgException(ExitCodes.Failed, $"The root of {source} is not an object.");
        }

        return (content.Skip(1).ToArray(), comments);
    }

    private (Element[] Content, IReadOnlyList<string> Comments) Document()
    {
        Next();

        if (Next() is not DocumentStart start)
        {
            throw Invalid("there is no document");
        }

        ParsingEvent first = Read();

        // Only an explicit "---" tells the comments of the document from those of the first value
        List<string> comments = start.IsImplicit ? new() : TakeComments(comment => comment.Line < start.Start.Line);
        Element[] content = Value(first, new List<string>(), 0);

        Read();

        if (Next() is DocumentStart)
        {
            throw Invalid("there is more than one document");
        }

        // Only now: a comment at the end of a line comes after its value. They follow the TYPE node of the holder
        foreach ((Element holder, List<string> texts) in commented)
        {
            for (int index = 0; index < texts.Count; index++)
            {
                holder.Insert(index + 1, CfgLayout.CommentNode(texts[index]));
            }
        }

        return (content, comments);
    }

    private Element[] Value(ParsingEvent first, List<string> comments, int depth)
    {
        switch (first)
        {
            case Scalar scalar:
                line = scalar.End.Line;
                lineComments = comments;
                return ScalarContent(scalar);
            case MappingStart:
                return Mapping(Deeper(depth));
            case SequenceStart:
                return Sequence(Deeper(depth));
            case AnchorAlias:
                throw Invalid("aliases are not supported");
            default:
                throw Invalid($"unexpected {first.GetType().Name}");
        }
    }

    private Element[] Mapping(int depth)
    {
        // A repeated name: the last one wins (as in IConfiguration), so only that one is written
        var members = new List<(string Name, Element Holder)>();
        lineComments = null;

        for (ParsingEvent next = Read(); next is not MappingEnd; next = Read())
        {
            if (next is not Scalar key)
            {
                throw Invalid("a key is not a scalar");
            }

            List<string> comments = TakeComments(_ => true);
            line = key.End.Line;
            lineComments = comments;

            Element holder = CfgLayout.NameNode(key.Value);
            holder.Add(Value(Read(), comments, depth));
            commented.Add((holder, comments));
            members.RemoveAll(member => member.Name == key.Value);
            members.Add((key.Value, holder));
        }

        lineComments = null;
        return [CfgLayout.TypeNode(CfgLayout.Object), .. members.Select(member => member.Holder)];
    }

    private Element[] Sequence(int depth)
    {
        var items = new List<Element> { CfgLayout.TypeNode(CfgLayout.Array) };
        lineComments = null;

        for (ParsingEvent next = Read(); next is not SequenceEnd; next = Read())
        {
            // A comment after the "-" of a mapping or a sequence is about its first value, not about the item
            List<string> comments = TakeComments(comment => next is Scalar || !comment.Inline);
            Element item = CfgLayout.ItemNode();
            item.Add(Value(next, comments, depth));
            commented.Add((item, comments));
            items.Add(item);
        }

        lineComments = null;
        return items.ToArray();
    }

    private Element[] ScalarContent(Scalar scalar)
    {
        if (scalar.Tag.IsEmpty)
        {
            return scalar.Style == ScalarStyle.Plain ? Plain(scalar.Value) : CfgLayout.StringContent(scalar.Value);
        }

        switch (scalar.Tag.Value)
        {
            case "!":
            case TagPrefix + "str":
                return CfgLayout.StringContent(scalar.Value);
            case TagPrefix + "binary":
                return Binary(scalar.Value);
            case TagPrefix + "null" or TagPrefix + "bool" or TagPrefix + "int" or TagPrefix + "float":
                return Plain(scalar.Value);
            default:
                throw Invalid($"the tag {scalar.Tag.Value} is not supported");
        }
    }

    private Element[] Plain(string text)
    {
        switch (text)
        {
            case "" or "~" or "null" or "Null" or "NULL":
                return CfgLayout.EmptyContent(CfgLayout.Null);
            case "true" or "True" or "TRUE":
                return CfgLayout.BooleanContent(true);
            case "false" or "False" or "FALSE":
                return CfgLayout.BooleanContent(false);
        }

        if (Radix().IsMatch(text))
        {
            int radix = text[1] == 'x' ? 16 : 8;
            return Finite(text[2..].Aggregate(0.0, (value, digit) => (value * radix) + Convert.ToInt32(digit.ToString(), 16)), text);
        }

        if (Float().IsMatch(text))
        {
            return Finite(double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture), text);
        }

        return NotANumber().IsMatch(text) ? Finite(double.NaN, text) : CfgLayout.StringContent(text);
    }

    private Element[] Finite(double number, string text) =>
        double.IsFinite(number) ? CfgLayout.NumberContent(number) : throw Invalid($"the number {text} is not a finite double");

    private Element[] Binary(string text)
    {
        try
        {
            return CfgLayout.BlobContent(Convert.FromBase64String(text));
        }
        catch (FormatException ex)
        {
            throw Invalid(ex.Message);
        }
    }

    private int Deeper(int depth) =>
        depth < Validator.MaxDepth ? depth + 1 : throw Invalid($"it is nested deeper than {Validator.MaxDepth} levels");

    /// <summary>The next event that is not a comment.</summary>
    private ParsingEvent Read() => Next() ?? throw Invalid("it ends too early");

    /// <summary>The next event that is not a comment, or null at the end; the comments are put aside.</summary>
    private ParsingEvent? Next()
    {
        while (parser.MoveNext())
        {
            if (parser.Current is not Comment comment)
            {
                return parser.Current;
            }

            if (comment.IsInline && lineComments != null && comment.Start.Line == line)
            {
                lineComments.Add(comment.Value);
            }
            else
            {
                pending.Add(((int)comment.Start.Line, comment.Value, comment.IsInline));
            }
        }

        return null;
    }

    /// <summary>The comments put aside: the lines that follow each other are one comment.</summary>
    private List<string> TakeComments(Func<(int Line, string Text, bool Inline), bool> filter)
    {
        var comments = new List<string>();
        int previous = -1;

        foreach ((int number, string text, _) in pending.Where(filter))
        {
            if (number == previous + 1 && comments.Count > 0)
            {
                comments[^1] += "\n" + text;
            }
            else
            {
                comments.Add(text);
            }

            previous = number;
        }

        pending.RemoveAll(comment => filter(comment));
        return comments;
    }

    private PCfgException Invalid(string reason) => new(ExitCodes.Failed, $"{source} can not be read: {reason}.");

    [GeneratedRegex("^0(x[0-9a-fA-F]+|o[0-7]+)$", RegexOptions.CultureInvariant)]
    private static partial Regex Radix();

    [GeneratedRegex(@"^[-+]?(\.[0-9]+|[0-9]+(\.[0-9]*)?)([eE][-+]?[0-9]+)?$", RegexOptions.CultureInvariant)]
    private static partial Regex Float();

    [GeneratedRegex(@"^([-+]?\.(inf|Inf|INF)|\.(nan|NaN|NAN))$", RegexOptions.CultureInvariant)]
    private static partial Regex NotANumber();
}
