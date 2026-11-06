using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Promptuarium;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace PCfg;

/// <summary>
/// Shows a configuration (of a valid file) for people, as a tree or as flat key = value lines, and exports it as YAML or JSON.
/// </summary>
internal static partial class Renderer
{
    private const string BinaryTag = "tag:yaml.org,2002:binary";

    private static readonly JsonWriterOptions JsonOptions = new()
    {
        Indented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        MaxDepth = Validator.MaxDepth + 2,
    };

    /// <summary>
    /// Writes the value as a tree: "name = value" for a scalar, "name {" ... "}" for an object, "name [" ... "]" for an
    /// array (its elements by index), and the comments as "# ..." lines before the value they are about.
    /// </summary>
    public static void Tree(Element holder, string? label, TextWriter output, int maxBytes)
    {
        WriteTree(holder, label, 0, output, maxBytes);
    }

    /// <summary>Writes every scalar and every empty container as a "key = value" line, with its full key.</summary>
    public static void Flat(Element holder, ConfigKey key, TextWriter output, int maxBytes)
    {
        WriteComments(holder, string.Empty, output);

        if (!CfgLayout.IsContainer(holder))
        {
            output.WriteLine($"{key} = {Scalar(holder, maxBytes)}");
            return;
        }

        bool empty = true;

        foreach ((string segment, Element child) in Children(holder))
        {
            empty = false;
            Flat(child, key.Child(segment), output, maxBytes);
        }

        if (empty && !key.IsRoot)
        {
            output.WriteLine($"{key} = {(CfgLayout.TypeOf(holder) == CfgLayout.Object ? "{}" : "[]")}");
        }
    }

    /// <summary>A scalar for people and for the command line: a string quoted, a BLOB as hex:..., a number, true, false or null.</summary>
    public static string Scalar(Element holder, int maxBytes)
    {
        Element? value = CfgLayout.ValueOf(holder);

        switch (CfgLayout.TypeOf(holder))
        {
            case CfgLayout.Utf8String:
                return Quote(Encoding.UTF8.GetString(value!.Data.Span));
            case CfgLayout.Float64:
                return Number(CfgLayout.ReadNumber(value!));
            case CfgLayout.Boolean:
                return value!.Data.Span[0] == 1 ? "true" : "false";
            case CfgLayout.Blob:
                ReadOnlySpan<byte> bytes = value!.Data.Span;
                return bytes.Length <= maxBytes
                    ? "hex:" + Convert.ToHexString(bytes)
                    : string.Create(CultureInfo.InvariantCulture, $"hex:{Convert.ToHexString(bytes[..maxBytes])}...  # {bytes.Length} bytes");
            default:
                return "null";
        }
    }

    /// <summary>A scalar as it is: the text of a string, the hex of a BLOB, a number, true, false or null.</summary>
    public static string Raw(Element holder)
    {
        Element? value = CfgLayout.ValueOf(holder);

        return CfgLayout.TypeOf(holder) switch
        {
            CfgLayout.Utf8String => Encoding.UTF8.GetString(value!.Data.Span),
            CfgLayout.Blob => Convert.ToHexString(value!.Data.Span),
            _ => Scalar(holder, int.MaxValue),
        };
    }

    /// <summary>A number as it is read back: the shortest text of the double.</summary>
    public static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);

    /// <summary>
    /// The value as YAML: a BLOB is !!binary, a comment is a # line before its value. The comments of the value itself
    /// come before a "---" line, as those of the file: <see cref="YamlReader"/> reads all of them back.
    /// </summary>
    public static string Yaml(Element holder)
    {
        using var text = new StringWriter(CultureInfo.InvariantCulture);

        // No folding: a long text stays on its line
        var emitter = new Emitter(text, new EmitterSettings(2, int.MaxValue, false, 1024, indentSequences: true, newLine: "\n"));

        emitter.Emit(new StreamStart());
        bool commented = EmitComments(holder, emitter);
        emitter.Emit(new DocumentStart(null, null, isImplicit: !commented));
        EmitValue(holder, emitter);
        emitter.Emit(new DocumentEnd(isImplicit: true));
        emitter.Emit(new StreamEnd());

        return text.ToString().TrimEnd('\n');
    }

    /// <summary>The value as JSON; a BLOB is a base64 string, the comments are left out.</summary>
    public static string Json(Element holder)
    {
        using var buffer = new MemoryStream();

        using (var writer = new Utf8JsonWriter(buffer, JsonOptions))
        {
            WriteJson(holder, writer);
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    /// <summary>A text in double quotes, escaped as in JSON (the characters that can be printed are kept).</summary>
    public static string Quote(string text)
    {
        using var buffer = new MemoryStream();

        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
        {
            writer.WriteStringValue(text);
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    private static void WriteTree(Element holder, string? label, int depth, TextWriter output, int maxBytes)
    {
        string indent = new(' ', depth * 2);
        WriteComments(holder, indent, output);
        string prefix = label is null ? string.Empty : label + " ";

        if (!CfgLayout.IsContainer(holder))
        {
            output.WriteLine($"{indent}{(label is null ? string.Empty : label + " = ")}{Scalar(holder, maxBytes)}");
            return;
        }

        bool isObject = CfgLayout.TypeOf(holder) == CfgLayout.Object;
        var children = Children(holder).ToList();

        // The root has no braces: its members are the top level
        if (holder.Parent is null && label is null)
        {
            foreach ((string segment, Element child) in children)
            {
                WriteTree(child, Label(segment), depth, output, maxBytes);
            }

            return;
        }

        if (children.Count == 0)
        {
            output.WriteLine($"{indent}{prefix}{(isObject ? "{}" : "[]")}");
            return;
        }

        output.WriteLine($"{indent}{prefix}{(isObject ? "{" : "[")}");

        foreach ((string segment, Element child) in children)
        {
            WriteTree(child, isObject ? Label(segment) : segment, depth + 1, output, maxBytes);
        }

        output.WriteLine($"{indent}{(isObject ? "}" : "]")}");
    }

    private static void WriteComments(Element holder, string indent, TextWriter output)
    {
        foreach (string comment in CfgLayout.Comments(holder))
        {
            foreach (string line in comment.Split('\n'))
            {
                output.WriteLine($"{indent}#{(line.Length > 0 ? " " : string.Empty)}{line.TrimEnd('\r')}");
            }
        }
    }

    /// <summary>The members of an object (by name) or the elements of an array (by index).</summary>
    private static System.Collections.Generic.IEnumerable<(string Segment, Element Holder)> Children(Element holder)
    {
        return CfgLayout.TypeOf(holder) == CfgLayout.Object
            ? CfgLayout.Members(holder).Select(member => (CfgLayout.NameOf(member), member))
            : CfgLayout.Items(holder).Select((item, index) => (index.ToString(CultureInfo.InvariantCulture), item));
    }

    /// <summary>A name as it is shown: as it is if it is a plain word, otherwise quoted.</summary>
    public static string Label(string name) => PlainName().IsMatch(name) ? name : Quote(name);

    private static void WriteJson(Element holder, Utf8JsonWriter writer)
    {
        Element? value = CfgLayout.ValueOf(holder);

        switch (CfgLayout.TypeOf(holder))
        {
            case CfgLayout.Object:
                writer.WriteStartObject();

                foreach (Element member in DistinctMembers(holder))
                {
                    writer.WritePropertyName(CfgLayout.NameOf(member));
                    WriteJson(member, writer);
                }

                writer.WriteEndObject();
                break;
            case CfgLayout.Array:
                writer.WriteStartArray();

                foreach (Element item in CfgLayout.Items(holder))
                {
                    WriteJson(item, writer);
                }

                writer.WriteEndArray();
                break;
            case CfgLayout.Utf8String:
                writer.WriteStringValue(Encoding.UTF8.GetString(value!.Data.Span));
                break;
            case CfgLayout.Float64:
                writer.WriteNumberValue(CfgLayout.ReadNumber(value!));
                break;
            case CfgLayout.Boolean:
                writer.WriteBooleanValue(value!.Data.Span[0] == 1);
                break;
            case CfgLayout.Blob:
                writer.WriteBase64StringValue(value!.Data.Span);
                break;
            default:
                writer.WriteNullValue();
                break;
        }
    }

    /// <summary>The members of an object: the last one of a repeated name wins.</summary>
    private static System.Collections.Generic.IEnumerable<Element> DistinctMembers(Element holder)
    {
        var members = CfgLayout.Members(holder).ToList();
        return members.Where((member, index) => !members.Skip(index + 1).Any(later => CfgLayout.NameOf(later) == CfgLayout.NameOf(member)));
    }

    private static void EmitValue(Element holder, Emitter emitter)
    {
        Element? value = CfgLayout.ValueOf(holder);

        switch (CfgLayout.TypeOf(holder))
        {
            case CfgLayout.Object:
                emitter.Emit(new MappingStart(AnchorName.Empty, TagName.Empty, true, MappingStyle.Block));

                foreach (Element member in DistinctMembers(holder))
                {
                    EmitComments(member, emitter);
                    EmitText(CfgLayout.NameOf(member), emitter);
                    EmitValue(member, emitter);
                }

                emitter.Emit(new MappingEnd());
                break;
            case CfgLayout.Array:
                emitter.Emit(new SequenceStart(AnchorName.Empty, TagName.Empty, true, SequenceStyle.Block));

                foreach (Element item in CfgLayout.Items(holder))
                {
                    EmitComments(item, emitter);
                    EmitValue(item, emitter);
                }

                emitter.Emit(new SequenceEnd());
                break;
            case CfgLayout.Utf8String:
                EmitText(Encoding.UTF8.GetString(value!.Data.Span), emitter);
                break;
            case CfgLayout.Blob:
                emitter.Emit(new Scalar(AnchorName.Empty, new TagName(BinaryTag), Convert.ToBase64String(value!.Data.Span), ScalarStyle.Plain, false, false));
                break;
            default:
                emitter.Emit(new Scalar(AnchorName.Empty, TagName.Empty, Scalar(holder, int.MaxValue), ScalarStyle.Plain, true, false));
                break;
        }
    }

    /// <summary>A text: quoted if a reader could take it for something else than a string.</summary>
    private static void EmitText(string text, Emitter emitter)
    {
        bool quoted = text.Length == 0 || "0123456789+-.~".Contains(text[0], StringComparison.Ordinal) || YamlKeywords().IsMatch(text);
        emitter.Emit(new Scalar(AnchorName.Empty, TagName.Empty, text, quoted ? ScalarStyle.DoubleQuoted : ScalarStyle.Any, !quoted, quoted));
    }

    private static bool EmitComments(Element holder, Emitter emitter)
    {
        bool any = false;

        foreach (string comment in CfgLayout.Comments(holder))
        {
            foreach (string line in comment.Split('\n'))
            {
                emitter.Emit(new Comment(line.TrimEnd('\r'), false));
                any = true;
            }
        }

        return any;
    }

    // The words of YAML 1.1 too: other readers still take them for booleans
    [GeneratedRegex("^(null|true|false|yes|no|on|off|y|n)$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex YamlKeywords();

    [GeneratedRegex(@"^[A-Za-z0-9_$@./+-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex PlainName();
}
