using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using PStudio.Models;
using Promptuarium;

namespace PStudio.Services;

/// <summary>
/// Converts contents to and from their text forms. The typed forms use the conversions of the Data class.
/// </summary>
public static class ContentFormat
{
    /// <summary>
    /// Contents longer than this are not shown in the text editor (they can be imported and exported).
    /// </summary>
    public const int MaxEditableLength = 1024 * 1024;

    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>
    /// The name of a form, for the form selector.
    /// </summary>
    public static string Name(ContentForm form) => form switch
    {
        ContentForm.Utf8 => "UTF-8",
        ContentForm.Ascii => "ASCII",
        ContentForm.Utf16 => "UTF-16",
        ContentForm.Utf32 => "UTF-32",
        ContentForm.Bool => "bool",
        ContentForm.Byte => "byte",
        ContentForm.Char => "char",
        ContentForm.Int16 => "short",
        ContentForm.UInt16 => "ushort",
        ContentForm.Int32 => "int",
        ContentForm.UInt32 => "uint",
        ContentForm.Int64 => "long",
        ContentForm.UInt64 => "ulong",
        ContentForm.Single => "float",
        ContentForm.Double => "double",
        ContentForm.Decimal => "decimal",
        ContentForm.SerializerTag => "serializer tag",
        ContentForm.SerializerType => "serializer type",
        _ => form.ToString(),
    };

    /// <summary>
    /// The form a content is best shown in: UTF-8 for printable text (and for an empty content, which is typed in), otherwise hex.
    /// </summary>
    public static ContentForm Suggest(ReadOnlySpan<byte> content)
    {
        return content.IsEmpty || TryUtf8(content, out string text) && text.All(c => !char.IsControl(c) || c is '\n' or '\r' or '\t')
            ? ContentForm.Utf8
            : ContentForm.Hex;
    }

    /// <summary>
    /// Whether the content can be shown in the form without losing bytes: its text form gives back the same bytes.
    /// </summary>
    public static bool CanShow(ReadOnlySpan<byte> content, ContentForm form)
    {
        if (content.IsEmpty || form is ContentForm.Hex or ContentForm.Base64)
        {
            return true;
        }

        try
        {
            return Parse(Format(content, form), form).AsSpan().SequenceEqual(content);
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or ArgumentException or PromptuariumException)
        {
            return false;
        }
    }

    /// <summary>
    /// The text form of a content. A content that the form can not show is not shown faithfully (see <see cref="CanShow"/>).
    /// </summary>
    public static string Format(ReadOnlySpan<byte> content, ContentForm form)
    {
        if (content.IsEmpty)
        {
            return string.Empty;
        }

        ReadOnlyMemory<byte> memory = content.ToArray();

        return form switch
        {
            ContentForm.Hex => FormatHex(content),
            ContentForm.Utf8 => Encoding.UTF8.GetString(content),
            ContentForm.Ascii => Encoding.ASCII.GetString(content),
            ContentForm.Base64 => Convert.ToBase64String(content, Base64FormattingOptions.InsertLineBreaks),
            ContentForm.Utf16 => memory.AsUtf16String(),
            ContentForm.Utf32 => memory.AsUtf32String(),
            ContentForm.Bool => memory.AsBool() ? "true" : "false",
            ContentForm.Byte => memory.AsByte().ToString(Invariant),
            ContentForm.Char => memory.AsChar().ToString(),
            ContentForm.Int16 => memory.AsShort().ToString(Invariant),
            ContentForm.UInt16 => memory.AsUShort().ToString(Invariant),
            ContentForm.Int32 => memory.AsInt().ToString(Invariant),
            ContentForm.UInt32 => memory.AsUInt().ToString(Invariant),
            ContentForm.Int64 => memory.AsLong().ToString(Invariant),
            ContentForm.UInt64 => memory.AsULong().ToString(Invariant),
            ContentForm.VarInt => memory.AsVarInt().ToString(Invariant),
            ContentForm.VarUInt => memory.AsVarUInt().ToString(Invariant),
            ContentForm.Single => memory.AsFloat().ToString("R", Invariant),
            ContentForm.Double => memory.AsDouble().ToString("R", Invariant),
            ContentForm.Decimal => memory.AsDecimal().ToString(Invariant),
            ContentForm.Guid => memory.AsGuid().ToString("D"),
            ContentForm.DateTime => memory.AsDateTime().ToString("o", Invariant),
            ContentForm.DateTimeOffset => memory.AsDateTimeOffset().ToString("o", Invariant),
            ContentForm.TimeSpan => memory.AsTimeSpan().ToString("c", Invariant),
            ContentForm.SerializerTag => FormatCode(content, SerializerLayout.Tags, "tag"),
            ContentForm.SerializerType => FormatCode(content, SerializerLayout.Types, "type code"),
            _ => throw new ArgumentOutOfRangeException(nameof(form), form, null),
        };
    }

    /// <summary>
    /// Parses the text form of a content; an empty text is an empty content in every form.
    /// </summary>
    /// <exception cref="FormatException">The text is not valid in the form</exception>
    public static byte[] Parse(string text, ContentForm form)
    {
        ArgumentNullException.ThrowIfNull(text);

        bool isText = form is ContentForm.Utf8 or ContentForm.Ascii or ContentForm.Utf16 or ContentForm.Utf32;
        string value = isText ? text : text.Trim();

        if (value.Length == 0)
        {
            return [];
        }

        try
        {
            ReadOnlyMemory<byte> bytes = form switch
            {
                ContentForm.Hex => Convert.FromHexString(new string(value.Where(c => !char.IsWhiteSpace(c) && c != '-').ToArray())),
                ContentForm.Utf8 => Encoding.UTF8.GetBytes(value),
                ContentForm.Ascii => value.All(char.IsAscii) ? Encoding.ASCII.GetBytes(value) : throw new FormatException("The text contains non-ASCII characters."),
                ContentForm.Base64 => Convert.FromBase64String(value),
                ContentForm.Utf16 => Data.FromUtf16String(value),
                ContentForm.Utf32 => Data.FromUtf32String(value),
                ContentForm.Bool => Data.FromBool(bool.Parse(value)),
                ContentForm.Byte => Data.FromByte(byte.Parse(value, Invariant)),
                ContentForm.Char => value.Length == 1 ? Data.FromChar(value[0]) : throw new FormatException("A char is a single UTF-16 character."),
                ContentForm.Int16 => Data.FromShort(short.Parse(value, Invariant)),
                ContentForm.UInt16 => Data.FromUShort(ushort.Parse(value, Invariant)),
                ContentForm.Int32 => Data.FromInt(int.Parse(value, Invariant)),
                ContentForm.UInt32 => Data.FromUInt(uint.Parse(value, Invariant)),
                ContentForm.Int64 => Data.FromLong(long.Parse(value, Invariant)),
                ContentForm.UInt64 => Data.FromULong(ulong.Parse(value, Invariant)),
                ContentForm.VarInt => Data.FromVarInt(long.Parse(value, Invariant)),
                ContentForm.VarUInt => Data.FromVarUInt(ulong.Parse(value, Invariant)),
                ContentForm.Single => Data.FromFloat(float.Parse(value, Invariant)),
                ContentForm.Double => Data.FromDouble(double.Parse(value, Invariant)),
                ContentForm.Decimal => Data.FromDecimal(decimal.Parse(value, Invariant)),
                ContentForm.Guid => Data.FromGuid(System.Guid.Parse(value)),
                ContentForm.DateTime => Data.FromDateTime(System.DateTime.Parse(value, Invariant, DateTimeStyles.RoundtripKind)),
                ContentForm.DateTimeOffset => Data.FromDateTimeOffset(System.DateTimeOffset.Parse(value, Invariant)),
                ContentForm.TimeSpan => Data.FromTimeSpan(System.TimeSpan.Parse(value, Invariant)),
                ContentForm.SerializerTag => ParseCode(value, SerializerLayout.Tags, "Not a tag: NAME, TYPE, ITEM, VALUE or COMMENT."),
                ContentForm.SerializerType => ParseCode(value, SerializerLayout.Types, "Not a type code, e.g. FLOAT64, UTF8_STRING or BLOB."),
                _ => throw new ArgumentOutOfRangeException(nameof(form), form, null),
            };

            return bytes.ToArray();
        }
        catch (OverflowException ex)
        {
            throw new FormatException($"The value is out of the range of {Name(form)}.", ex);
        }
    }

    /// <summary>
    /// The valid values of a form, for the tooltip of an error (the message has to be short), or null.
    /// </summary>
    public static string? Help(ContentForm form) => form switch
    {
        ContentForm.SerializerTag => SerializerLayout.TagList,
        ContentForm.SerializerType => SerializerLayout.TypeList,
        _ => null,
    };

    /// <summary>
    /// A short, one-line preview of a content, for the tree.
    /// </summary>
    public static string Preview(ReadOnlySpan<byte> content, int maxLength = 24)
    {
        if (content.IsEmpty)
        {
            return string.Empty;
        }

        ReadOnlySpan<byte> head = content[..Math.Min(content.Length, maxLength)];
        string ellipsis = head.Length < content.Length ? "…" : string.Empty;

        if (Suggest(content) == ContentForm.Utf8)
        {
            // The cut may split a UTF-8 sequence (at most 4 bytes long)
            for (int length = head.Length; length > head.Length - 4 && length > 0; length--)
            {
                if (TryUtf8(head[..length], out string text))
                {
                    return "\"" + text.ReplaceLineEndings(" ") + ellipsis + "\"";
                }
            }
        }

        return Convert.ToHexString(head) + ellipsis;
    }

    // A one-byte code of SERIALIZER_FORMAT.md by its name
    private static string FormatCode(ReadOnlySpan<byte> content, IReadOnlyDictionary<byte, string> names, string kind)
    {
        return content.Length == 1 && names.TryGetValue(content[0], out string? name)
            ? name
            : throw new FormatException($"The content is not a {kind} of the serializer format.");
    }

    // The message is short, so that it fits in the line of the editor
    private static byte[] ParseCode(string value, IReadOnlyDictionary<byte, string> names, string error)
    {
        foreach ((byte code, string name) in names)
        {
            if (string.Equals(name, value, StringComparison.OrdinalIgnoreCase))
            {
                return [code];
            }
        }

        throw new FormatException(error);
    }

    private static string FormatHex(ReadOnlySpan<byte> content)
    {
        var builder = new StringBuilder(content.Length * 3);

        for (int i = 0; i < content.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(i % 16 == 0 ? '\n' : ' ');
            }

            builder.Append(Convert.ToHexString(content.Slice(i, 1)));
        }

        return builder.ToString();
    }

    private static bool TryUtf8(ReadOnlySpan<byte> content, out string text)
    {
        try
        {
            text = StrictUtf8.GetString(content);
            return true;
        }
        catch (ArgumentException)
        {
            text = string.Empty;
            return false;
        }
    }
}
