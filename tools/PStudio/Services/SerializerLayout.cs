using System;
using System.Collections.Generic;
using System.Linq;
using PStudio.Models;
using Promptuarium;
using Promptuarium.Tools;

namespace PStudio.Services;

/// <summary>
/// The layout of SERIALIZER_FORMAT.md (Promptuarium.Serializer): the metadata of a node is a one-byte tag, the data of
/// a TYPE node is a type code, and the VALUE of a holder is encoded by its type. A node with a one-byte metadata is
/// shown in this layout by default.
/// </summary>
public static class SerializerLayout
{
    /// <summary>The metadata tag of a TYPE node.</summary>
    public const byte TypeTag = 0x01;

    /// <summary>The metadata tag of a COMMENT node.</summary>
    public const byte CommentTag = 0x04;

    /// <summary>The metadata tags (section 3), by code.</summary>
    public static IReadOnlyDictionary<byte, string> Tags { get; } = new Dictionary<byte, string>
    {
        [0x00] = "NAME",
        [TypeTag] = "TYPE",
        [0x02] = "ITEM",
        [0x03] = "VALUE",
        [CommentTag] = "COMMENT",
    };

    /// <summary>The type codes (section 4), the reserved ones too, by code.</summary>
    public static IReadOnlyDictionary<byte, string> Types => SerializerValues.TypeNames;

    /// <summary>The type codes that version 2 of the format supports; FLOAT8 and FLOAT128 are reserved.</summary>
    public static IReadOnlySet<byte> SupportedTypes => SerializerValues.Version2Types;

    /// <summary>Every tag, one per line, with its code.</summary>
    public static string TagList { get; } = "The tags:\n" + string.Join("\n", Tags.Select(tag => $"{tag.Value}  (0x{tag.Key:X2})"));

    /// <summary>Every type code, one per line, with its code; the reserved ones are marked.</summary>
    public static string TypeList { get; } = "The type codes:\n" + string.Join(
        "\n",
        Types.Select(type => $"{type.Value}  (0x{type.Key:X2}){(SupportedTypes.Contains(type.Key) ? string.Empty : ", reserved")}"));

    // The forms of the values of the supported types; NULL, OBJECT and ARRAY have no VALUE
    private static readonly Dictionary<byte, ContentForm> ValueForms = new()
    {
        [0x01] = ContentForm.Bool,
        [0x40] = ContentForm.SByte,
        [0x41] = ContentForm.Int16,
        [0x42] = ContentForm.Int32,
        [0x43] = ContentForm.Int64,
        [0x44] = ContentForm.Int128,
        [0x50] = ContentForm.Byte,
        [0x51] = ContentForm.UInt16,
        [0x52] = ContentForm.UInt32,
        [0x53] = ContentForm.UInt64,
        [0x54] = ContentForm.UInt128,
        [0x61] = ContentForm.Half,
        [0x62] = ContentForm.Single,
        [0x63] = ContentForm.Double,
        [0x70] = ContentForm.Utf8,
        [0x78] = ContentForm.BigInteger,
        [0x80] = ContentForm.Utf8,
        [0x90] = ContentForm.Uuid,
        [0xD0] = ContentForm.Date,
        [0xD1] = ContentForm.TimeOfDay,
        [0xD2] = ContentForm.UnixDateTime,
        [0xD3] = ContentForm.Timestamp,
        [0xD4] = ContentForm.TimeSpan,
        [0xF0] = ContentForm.Hex,
        [0xF1] = ContentForm.Hex,
    };

    /// <summary>
    /// The default form of the metadata: the tag, if it is one byte, or if it is empty and the node is in the layout
    /// (e.g. a new node, whose tag is typed in): its parent (<paramref name="parent"/>) is a holder or a root with a
    /// signature of the serializer, or a sibling has a tag. Otherwise none, the editor chooses.
    /// </summary>
    public static ContentForm? MetaDataForm(Element node, Element? parent)
    {
        ArgumentNullException.ThrowIfNull(node);

        bool inLayout = parent is not null
            && (IsTag(parent.MetaData) || IsSerializerRoot(parent) || parent.Children.Any(sibling => sibling != node && IsTag(sibling.MetaData)));

        return node.MetaData.Length == 1 || (node.MetaData.IsEmpty && inLayout) ? ContentForm.SerializerTag : null;
    }

    private static bool IsTag(ReadOnlyMemory<byte> metaData) => metaData.Length == 1 && Tags.ContainsKey(metaData.Span[0]);

    private static bool IsSerializerRoot(Element node)
    {
        return node.Data.IsEmpty && node.MetaData.Length is > 0 and <= ElementBuilder.MaxSignatureLength
            && SerializerValues.IsLayoutSignature(System.Text.Encoding.Latin1.GetString(node.MetaData.Span));
    }

    /// <summary>
    /// The default form of the data, by the tag: UTF-8 for a NAME and a COMMENT, the type code for a TYPE, and for a VALUE the form
    /// of the type in the TYPE node of its holder (<paramref name="holder"/>, the parent). None for anything else.
    /// </summary>
    public static ContentForm? DataForm(Element node, Element? holder)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (node.MetaData.Length != 1)
        {
            return null;
        }

        switch (node.MetaData.Span[0])
        {
            case 0x00 or CommentTag:
                return ContentForm.Utf8;

            case TypeTag:
                return ContentForm.SerializerType;

            case 0x03 when holder?.Children.FirstOrDefault() is { } type
                && type.MetaData.Length == 1 && type.MetaData.Span[0] == TypeTag && type.Data.Length == 1
                && ValueForms.TryGetValue(type.Data.Span[0], out ContentForm form):
                return form;

            default:
                return null;
        }
    }
}
