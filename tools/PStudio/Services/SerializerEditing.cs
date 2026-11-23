using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PStudio.Models;
using Promptuarium;
using Promptuarium.Tools;

namespace PStudio.Services;

/// <summary>
/// A type code of SERIALIZER_FORMAT.md, for the type selector of the serializer view.
/// </summary>
/// <param name="Code">The type code</param>
/// <param name="Name">Its name (INT32, UTF8_STRING, ...)</param>
public sealed record TypeOption(byte Code, string Name)
{
    /// <inheritdoc/>
    public override string ToString() => Name;
}

/// <summary>
/// The values of the serializer view (SERIALIZER_FORMAT.md): the type of a holder, the text of its value in the form of its
/// type, the default value of a type, and the conversion of a value to another type. Every text gives back the same bytes.
/// </summary>
public static class SerializerEditing
{
    /// <summary>The tags of the metadata.</summary>
    public const byte NameTag = 0x00, TypeTag = 0x01, ItemTag = 0x02, ValueTag = 0x03, CommentTag = 0x04;

    /// <summary>The type codes of the structures and of NULL, which have no VALUE.</summary>
    public const byte NullType = 0x00, BooleanType = 0x01, ObjectType = 0x20, ArrayType = 0x21, Utf8StringType = 0x80, BlobType = 0xF0, ContainerType = 0xF1;

    /// <summary>The longest BLOB whose hex is edited in the serializer view (a longer one is edited in the Data editor).</summary>
    public const int MaxInlineBlobLength = 4096;

    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    /// <summary>The types offered by the type selector: the supported ones of version 2, in the order of their codes.</summary>
    public static IReadOnlyList<TypeOption> Types { get; } = SerializerValues.Version2Types.Order()
        .Select(code => new TypeOption(code, SerializerValues.TypeNames[code]))
        .ToArray();

    /// <summary>The option of a type code.</summary>
    public static TypeOption TypeOf(byte code) => Types.First(option => option.Code == code);

    /// <summary>The tag of a node, or null if its metadata is not one byte.</summary>
    public static byte? Tag(Element node) => node.MetaData.Length == 1 ? node.MetaData.Span[0] : null;

    /// <summary>Whether a holder of the type has a VALUE node (every type but NULL, OBJECT and ARRAY).</summary>
    public static bool IsScalar(byte type) => type is not (NullType or ObjectType or ArrayType);

    /// <summary>The text of a value: true or false, the text of a UTF8_STRING, the hex of a BLOB or a CONTAINER, the text of a typed value.</summary>
    /// <exception cref="FormatException">The value is not valid.</exception>
    public static string Format(byte type, ReadOnlySpan<byte> value) => type switch
    {
        BooleanType => value.Length == 1 && value[0] <= 1 ? (value[0] == 1 ? "true" : "false") : throw new FormatException("the BOOLEAN value is not 00 or 01"),
        Utf8StringType => StrictUtf8.GetString(value),
        BlobType or ContainerType => ContentFormat.Format(value, ContentForm.Hex),
        _ => SerializerValues.Format(type, value),
    };

    /// <summary>The bytes of the text of a value of the type.</summary>
    /// <exception cref="FormatException">The text is not a value of the type.</exception>
    public static byte[] Parse(byte type, string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        switch (type)
        {
            case BooleanType:
                return bool.TryParse(text.Trim(), out bool flag) ? [(byte)(flag ? 1 : 0)] : throw new FormatException($"'{text}' is not true or false");

            case Utf8StringType:
                return Encoding.UTF8.GetBytes(text);

            case BlobType:
            case ContainerType:
                byte[] bytes;

                try
                {
                    bytes = ContentFormat.Parse(text, ContentForm.Hex);
                }
                catch (FormatException)
                {
                    throw new FormatException($"'{text}' is not hex, e.g. 01 02 FF");
                }

                return type == ContainerType && SerializerValues.ContainerProblem(bytes) is { } problem ? throw new FormatException(problem) : bytes;

            default:
                return SerializerValues.Parse(type, text.Trim());
        }
    }

    /// <summary>The value of a new holder of the type: false, zero, an empty text, the Unix epoch, an empty tree.</summary>
    public static byte[] DefaultValue(byte type) => type switch
    {
        BooleanType => [0],
        Utf8StringType or BlobType => [],
        ContainerType => new Element().ToBytes(),
        0x70 => "0"u8.ToArray(),
        0x78 => [0],
        0x90 => new byte[16],
        0xD0 => new byte[4],
        0xD1 or 0xD2 or 0xD4 => new byte[8],
        0xD3 => new byte[10],
        _ => SerializerValues.Parse(type, "0"),
    };

    /// <summary>
    /// The value of a holder converted to another type, by its text (e.g. 42 from INT32 to INT64 or to UTF8_STRING); the
    /// default value of the type if the text is not a value of it.
    /// </summary>
    public static byte[] Convert(byte from, ReadOnlySpan<byte> value, byte to)
    {
        try
        {
            return Parse(to, Format(from, value));
        }
        catch (FormatException)
        {
            return DefaultValue(to);
        }
    }

    /// <summary>A name for a new member that the object does not have yet: name, name2, name3, ...</summary>
    public static string NewName(IEnumerable<string> names, string stem = "name")
    {
        var taken = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
        string name = stem;

        for (int i = 2; taken.Contains(name); i++)
        {
            name = $"{stem}{i}";
        }

        return name;
    }
}
