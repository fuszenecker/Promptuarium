using System;
using System.Buffers.Binary;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Promptuarium;
using Promptuarium.Tools;
using static PStudio.Services.SerializerEditing;

namespace PStudio.Services;

/// <summary>
/// A serializable tree (SERIALIZER_FORMAT.md) as YAML or JSON, for reading only, beside the serializer view. The
/// numbers are numbers, a value of another type of version 2 is its text (in YAML with its type as a tag, e.g.
/// !timestamp), a BLOB is base64 (!!binary in YAML), a CONTAINER the hex of its stream; the comments are # lines in
/// YAML and left out of JSON.
/// </summary>
public static partial class SerializerText
{
    private static readonly JsonWriterOptions JsonOptions = new() { Indented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, SkipValidation = true };

    /// <summary>The tree (its root is an object) as YAML.</summary>
    public static string Yaml(Element root)
    {
        ArgumentNullException.ThrowIfNull(root);
        var text = new StringBuilder();

        WriteComments(root, string.Empty, text);
        if (Content(root, ObjectType).Count == 0)
        {
            text.Append("{}\n");
        }
        else
        {
            WriteYamlChildren(root, ObjectType, string.Empty, text);
        }

        return text.ToString().TrimEnd('\n');
    }

    /// <summary>The tree (its root is an object) as JSON.</summary>
    public static string Json(Element root)
    {
        ArgumentNullException.ThrowIfNull(root);
        using var buffer = new MemoryStream();

        using (var writer = new Utf8JsonWriter(buffer, JsonOptions))
        {
            WriteJson(root, ObjectType, writer);
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    // ---------------------------------------------------------------- YAML

    // The members of an object ("name: value") or the elements of an array ("- value"), with their comments before them
    private static void WriteYamlChildren(Element holder, byte type, string indent, StringBuilder text)
    {
        foreach (Element child in Content(holder, type))
        {
            WriteComments(child, indent, text);
            string lead = type == ObjectType ? $"{indent}{Key(Encoding.UTF8.GetString(child.Data.Span))}:" : $"{indent}-";
            byte childType = TypeOf(child);

            if (childType is ObjectType or ArrayType && Content(child, childType).Count > 0)
            {
                text.Append(lead).Append('\n');
                WriteYamlChildren(child, childType, indent + "  ", text);
            }
            else
            {
                text.Append(lead).Append(' ').Append(YamlScalar(child, childType)).Append('\n');
            }
        }
    }

    private static string YamlScalar(Element holder, byte type)
    {
        ReadOnlySpan<byte> value = IsScalar(type) ? ValueOf(holder) : default;

        return type switch
        {
            NullType => "null",
            ObjectType => "{}",
            ArrayType => "[]",
            BooleanType => value[0] == 1 ? "true" : "false",
            Utf8StringType => YamlString(Encoding.UTF8.GetString(value)),
            BlobType => $"!!binary {System.Convert.ToBase64String(value)}",
            ContainerType => $"!container {System.Convert.ToHexString(value)}",
            _ when IsNumber(type) && NumberText(type, value) is { } number => number,
            _ => $"!{SerializerValues.Prefix(type)} {YamlString(SerializerValues.Format(type, value))}",
        };
    }

    // A text plain if YAML reads it back as the same text, otherwise in double quotes (escaped as in JSON)
    private static string YamlString(string text) => PlainText().IsMatch(text) && !YamlKeyword().IsMatch(text) && !LooksLikeNumber(text) ? text : Quote(text);

    private static string Key(string name) => YamlString(name);

    private static bool LooksLikeNumber(string text) => double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out _) || text.StartsWith("0x", StringComparison.OrdinalIgnoreCase) || text.StartsWith("0o", StringComparison.OrdinalIgnoreCase);

    private static void WriteComments(Element holder, string indent, StringBuilder text)
    {
        foreach (Element comment in holder.Children.Where(child => Tag(child) == CommentTag))
        {
            foreach (string line in Encoding.UTF8.GetString(comment.Data.Span).Split('\n'))
            {
                text.Append(indent).Append(line.Length == 0 ? "#" : "# " + line.TrimEnd('\r')).Append('\n');
            }
        }
    }

    // ---------------------------------------------------------------- JSON

    private static void WriteJson(Element holder, byte type, Utf8JsonWriter writer)
    {
        ReadOnlySpan<byte> value = IsScalar(type) ? ValueOf(holder) : default;

        switch (type)
        {
            case ObjectType:
                writer.WriteStartObject();

                foreach (Element member in Content(holder, ObjectType))
                {
                    writer.WritePropertyName(Encoding.UTF8.GetString(member.Data.Span));
                    WriteJson(member, TypeOf(member), writer);
                }

                writer.WriteEndObject();
                break;

            case ArrayType:
                writer.WriteStartArray();

                foreach (Element item in Content(holder, ArrayType))
                {
                    WriteJson(item, TypeOf(item), writer);
                }

                writer.WriteEndArray();
                break;

            case NullType:
                writer.WriteNullValue();
                break;

            case BooleanType:
                writer.WriteBooleanValue(value[0] == 1);
                break;

            case Utf8StringType:
                writer.WriteStringValue(Encoding.UTF8.GetString(value));
                break;

            case BlobType:
                writer.WriteBase64StringValue(value);
                break;

            case ContainerType:
                writer.WriteStringValue(System.Convert.ToHexString(value));
                break;

            default:
                if (IsNumber(type) && NumberText(type, value) is { } number)
                {
                    writer.WriteRawValue(number, skipInputValidation: true);
                }
                else
                {
                    writer.WriteStringValue(SerializerValues.Format(type, value));
                }

                break;
        }
    }

    // ---------------------------------------------------------------- the tree

    // The members of an object (of the root) or the elements of an array, without the TYPE node and the comments
    private static System.Collections.Generic.List<Element> Content(Element holder, byte type)
    {
        byte tag = type == ArrayType ? ItemTag : NameTag;
        return holder.Children.Where(child => Tag(child) == tag).ToList();
    }

    private static byte TypeOf(Element holder) => holder.Parent is null ? ObjectType : holder.Children[0].Data.Span[0];

    private static ReadOnlySpan<byte> ValueOf(Element holder) => holder.Children.First(child => Tag(child) == ValueTag).Data.Span;

    // The integers, the binary floating point numbers and DECIMAL
    private static bool IsNumber(byte type) => type is (>= 0x40 and <= 0x44) or (>= 0x50 and <= 0x54) or 0x61 or 0x62 or 0x63 or 0x70 or 0x78;

    // The text of a number, as a number of JSON and YAML; null for a NaN or an infinity (which is shown as text)
    private static string? NumberText(byte type, ReadOnlySpan<byte> value)
    {
        double? floating = type switch
        {
            0x61 => (double)BinaryPrimitives.ReadHalfLittleEndian(value),
            0x62 => BinaryPrimitives.ReadSingleLittleEndian(value),
            0x63 => BinaryPrimitives.ReadDoubleLittleEndian(value),
            _ => null,
        };

        return floating is { } number && !double.IsFinite(number) ? null : SerializerValues.Format(type, value);
    }

    private static string Quote(string text)
    {
        using var buffer = new MemoryStream();

        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
        {
            writer.WriteStringValue(text);
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    [GeneratedRegex(@"^[A-Za-z_$][A-Za-z0-9_$ .\-/@()+]*(?<! )$", RegexOptions.CultureInvariant)]
    private static partial Regex PlainText();

    [GeneratedRegex(@"^(true|false|yes|no|on|off|y|n|null|~|nan|inf|\.nan|\.inf)$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex YamlKeyword();
}
