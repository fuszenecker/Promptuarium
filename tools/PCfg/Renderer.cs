using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Promptuarium;
using Promptuarium.Tools;
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

    /// <summary>
    /// A scalar for people and for the command line: a string quoted, a BLOB as hex:..., a number, true, false or null;
    /// a value of version 2 with its type, as it is set (int32:8080, timestamp:2026-11-22T19:30:15+01:00), and so is a
    /// FLOAT64 NaN or infinity (float64:NaN); a CONTAINER as container:... (the hex of the stream) with its summary.
    /// </summary>
    public static string Scalar(Element holder, int maxBytes)
    {
        Element? value = CfgLayout.ValueOf(holder);
        byte type = CfgLayout.TypeOf(holder);

        if (type == CfgLayout.Float64 && double.IsFinite(CfgLayout.ReadNumber(value!)))
        {
            return Number(CfgLayout.ReadNumber(value!));
        }

        if (SerializerValues.TypedValues.Contains(type))
        {
            return SerializerValues.Prefix(type) + ":" + SerializerValues.Format(type, value!.Data.Span);
        }

        switch (type)
        {
            case CfgLayout.Utf8String:
                return Quote(Encoding.UTF8.GetString(value!.Data.Span));
            case CfgLayout.Boolean:
                return value!.Data.Span[0] == 1 ? "true" : "false";
            case CfgLayout.Blob:
                ReadOnlySpan<byte> bytes = value!.Data.Span;
                return bytes.Length <= maxBytes
                    ? "hex:" + Convert.ToHexString(bytes)
                    : string.Create(CultureInfo.InvariantCulture, $"hex:{Convert.ToHexString(bytes[..maxBytes])}...  # {bytes.Length} bytes");
            case CfgLayout.Container:
                ReadOnlySpan<byte> stream = value!.Data.Span;
                string hex = stream.Length <= maxBytes ? Convert.ToHexString(stream) : Convert.ToHexString(stream[..maxBytes]) + "...";
                return $"container:{hex}  # {SerializerValues.ContainerSummary(stream)}";
            default:
                return "null";
        }
    }

    /// <summary>A scalar as it is: the text of a string, the hex of a BLOB or a CONTAINER, a number, true, false or null.</summary>
    public static string Raw(Element holder)
    {
        Element? value = CfgLayout.ValueOf(holder);

        byte type = CfgLayout.TypeOf(holder);

        return type switch
        {
            CfgLayout.Utf8String => Encoding.UTF8.GetString(value!.Data.Span),
            CfgLayout.Blob or CfgLayout.Container => Convert.ToHexString(value!.Data.Span),
            _ when SerializerValues.TypedValues.Contains(type) => SerializerValues.Format(type, value!.Data.Span),
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

    /// <summary>
    /// The value as JSON; a BLOB is a base64 string, the comments are left out. The integers and the decimals are numbers
    /// with all their digits, a NaN or an infinity is the string "NaN", "Infinity" or "-Infinity", a UUID, a date or a
    /// time is its text.
    /// </summary>
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
            case CfgLayout.Boolean:
                writer.WriteBooleanValue(value!.Data.Span[0] == 1);
                break;
            case CfgLayout.Blob or CfgLayout.Container:
                writer.WriteBase64StringValue(value!.Data.Span);
                break;
            case byte type when SerializerValues.TypedValues.Contains(type):
                WriteTypedJson(type, value!.Data.Span, writer);
                break;
            default:
                writer.WriteNullValue();
                break;
        }
    }

    // A number of JSON for the numbers (all the digits of the text: JSON has no limit, a reader may have), a string for the rest
    private static void WriteTypedJson(byte type, ReadOnlySpan<byte> value, Utf8JsonWriter writer)
    {
        string text = SerializerValues.Format(type, value);
        bool number = type is (>= 0x40 and <= 0x44) or (>= 0x50 and <= 0x54) or 0x78 or 0x70
            || (type is 0x61 or 0x62 or 0x63 && double.IsFinite(double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture)));

        if (number)
        {
            writer.WriteRawValue(text);
        }
        else
        {
            writer.WriteStringValue(text);
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
            case CfgLayout.Container:
                // A nested tree: the base64 of its stream, as !!binary, with a local tag of its own: !container
                emitter.Emit(new Scalar(AnchorName.Empty, new TagName("!container"), Convert.ToBase64String(value!.Data.Span), ScalarStyle.Plain, false, false));
                break;
            case CfgLayout.Float64 when double.IsFinite(CfgLayout.ReadNumber(value!)):
                emitter.Emit(new Scalar(AnchorName.Empty, TagName.Empty, Scalar(holder, int.MaxValue), ScalarStyle.Plain, true, false));
                break;
            case byte type when SerializerValues.TypedValues.Contains(type):
                // A value of version 2 (and a FLOAT64 NaN or infinity) with its type as a local tag: !int32 8080
                emitter.Emit(new Scalar(AnchorName.Empty, new TagName("!" + SerializerValues.Prefix(type)), SerializerValues.Format(type, value!.Data.Span), ScalarStyle.Plain, false, false));
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
