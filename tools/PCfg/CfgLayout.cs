using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Promptuarium;

namespace PCfg;

/// <summary>
/// The layout of SERIALIZER_FORMAT.md: the tags of the nodes, the type codes, and the helpers that read and build the
/// nodes of a configuration. The root is an implicit OBJECT; a NAME or an ITEM node is a holder, whose first child is
/// its TYPE node.
/// </summary>
internal static class CfgLayout
{
    /// <summary>The signature of configuration files.</summary>
    public const string ConfigurationSignature = "PRMT:CFG1-LE";

    /// <summary>Metadata tags (section 3).</summary>
    public const byte NameTag = 0x00;

    /// <summary>The tag of a TYPE node.</summary>
    public const byte TypeTag = 0x01;

    /// <summary>The tag of an ITEM node.</summary>
    public const byte ItemTag = 0x02;

    /// <summary>The tag of a VALUE node.</summary>
    public const byte ValueTag = 0x03;

    /// <summary>The tag of a COMMENT node.</summary>
    public const byte CommentTag = 0x04;

    /// <summary>Type codes (section 4).</summary>
    public const byte Null = 0x00;

    /// <summary>BOOLEAN.</summary>
    public const byte Boolean = 0x01;

    /// <summary>OBJECT.</summary>
    public const byte Object = 0x20;

    /// <summary>ARRAY.</summary>
    public const byte Array = 0x21;

    /// <summary>FLOAT64.</summary>
    public const byte Float64 = 0x63;

    /// <summary>UTF8_STRING.</summary>
    public const byte Utf8String = 0x80;

    /// <summary>BLOB.</summary>
    public const byte Blob = 0xF0;

    /// <summary>The UTF-8 decoder that rejects invalid bytes.</summary>
    public static readonly UTF8Encoding StrictUtf8 = new(false, true);

    /// <summary>The names of the tags, by code.</summary>
    public static IReadOnlyDictionary<byte, string> Tags { get; } = new Dictionary<byte, string>
    {
        [NameTag] = "NAME",
        [TypeTag] = "TYPE",
        [ItemTag] = "ITEM",
        [ValueTag] = "VALUE",
        [CommentTag] = "COMMENT",
    };

    /// <summary>The names of the type codes, the reserved ones too, by code.</summary>
    public static IReadOnlyDictionary<byte, string> Types { get; } = new Dictionary<byte, string>
    {
        [Null] = "NULL",
        [Boolean] = "BOOLEAN",
        [Object] = "OBJECT",
        [Array] = "ARRAY",
        [0x40] = "INT8",
        [0x41] = "INT16",
        [0x42] = "INT32",
        [0x43] = "INT64",
        [0x44] = "INT128",
        [0x50] = "UINT8",
        [0x51] = "UINT16",
        [0x52] = "UINT32",
        [0x53] = "UINT64",
        [0x54] = "UINT128",
        [0x60] = "FLOAT8",
        [0x61] = "FLOAT16",
        [0x62] = "FLOAT32",
        [Float64] = "FLOAT64",
        [0x64] = "FLOAT128",
        [Utf8String] = "UTF8_STRING",
        [Blob] = "BLOB",
    };

    /// <summary>The type codes of version 1 of the layout; the others are reserved or unknown.</summary>
    public static IReadOnlySet<byte> SupportedTypes { get; } = new HashSet<byte> { Null, Boolean, Object, Array, Float64, Utf8String, Blob };

    /// <summary>The name of a type code, e.g. "FLOAT64 (0x63)".</summary>
    public static string TypeName(byte code) => $"{Types.GetValueOrDefault(code, "unknown")} (0x{code:X2})";

    /// <summary>The tag of a node, or null if its metadata is not one byte.</summary>
    public static byte? Tag(Element node) => node.MetaData.Length == 1 ? node.MetaData.Span[0] : null;

    /// <summary>True if the node is a COMMENT node.</summary>
    public static bool IsComment(Element node) => Tag(node) == CommentTag;

    /// <summary>The type of a holder (of a valid file); the root is an OBJECT.</summary>
    public static byte TypeOf(Element holder) => holder.Parent is null ? Object : holder.Children[0].Data.Span[0];

    /// <summary>True if the holder (or the root) holds an OBJECT or an ARRAY.</summary>
    public static bool IsContainer(Element holder) => TypeOf(holder) is Object or Array;

    /// <summary>The VALUE node of a holder of a scalar (of a valid file).</summary>
    public static Element? ValueOf(Element holder) => holder.Children.FirstOrDefault(child => Tag(child) == ValueTag);

    /// <summary>The comments of a holder or of the root.</summary>
    public static IEnumerable<string> Comments(Element holder) => holder.Children.Where(IsComment).Select(child => Encoding.UTF8.GetString(child.Data.Span));

    /// <summary>The NAME nodes of an object holder or of the root, in order.</summary>
    public static IEnumerable<Element> Members(Element holder) => holder.Children.Where(child => Tag(child) == NameTag);

    /// <summary>The ITEM nodes of an array holder, in order.</summary>
    public static IReadOnlyList<Element> Items(Element holder) => holder.Children.Where(child => Tag(child) == ItemTag).ToList();

    /// <summary>The name of a NAME node.</summary>
    public static string NameOf(Element member) => Encoding.UTF8.GetString(member.Data.Span);

    /// <summary>The member with the name, the last one if there are several (it wins, as in IConfiguration).</summary>
    public static Element? Member(Element holder, string name) => Members(holder).LastOrDefault(member => NameOf(member) == name);

    /// <summary>A node with a tag and data.</summary>
    public static Element Node(byte tag, ReadOnlyMemory<byte> data, params Element[] children) => new(data, new[] { tag }, children);

    /// <summary>A NAME node (an empty holder).</summary>
    public static Element NameNode(string name) => Node(NameTag, Encoding.UTF8.GetBytes(name));

    /// <summary>An ITEM node (an empty holder).</summary>
    public static Element ItemNode() => Node(ItemTag, ReadOnlyMemory<byte>.Empty);

    /// <summary>A COMMENT node.</summary>
    public static Element CommentNode(string text) => Node(CommentTag, Encoding.UTF8.GetBytes(text));

    /// <summary>The TYPE node of a type code.</summary>
    public static Element TypeNode(byte type) => Node(TypeTag, new[] { type });

    /// <summary>The children of a holder of a string.</summary>
    public static Element[] StringContent(string value) => [TypeNode(Utf8String), Node(ValueTag, Encoding.UTF8.GetBytes(value))];

    /// <summary>The children of a holder of a number.</summary>
    public static Element[] NumberContent(double value)
    {
        byte[] bytes = new byte[8];
        BinaryPrimitives.WriteDoubleLittleEndian(bytes, value);
        return [TypeNode(Float64), Node(ValueTag, bytes)];
    }

    /// <summary>The children of a holder of a boolean.</summary>
    public static Element[] BooleanContent(bool value) => [TypeNode(Boolean), Node(ValueTag, new[] { value ? (byte)1 : (byte)0 })];

    /// <summary>The children of a holder of a BLOB.</summary>
    public static Element[] BlobContent(byte[] value) => [TypeNode(Blob), Node(ValueTag, value)];

    /// <summary>The children of a holder of null, of an empty object or of an empty array.</summary>
    public static Element[] EmptyContent(byte type) => [TypeNode(type)];

    /// <summary>The number in a FLOAT64 VALUE node.</summary>
    public static double ReadNumber(Element value) => BinaryPrimitives.ReadDoubleLittleEndian(value.Data.Span);

    /// <summary>
    /// Replaces the value of a holder with the content (a TYPE node first, then the VALUE node or the members or the
    /// elements). The comments of the holder are kept, right after the TYPE node.
    /// </summary>
    public static void Fill(Element holder, IReadOnlyList<Element> content)
    {
        foreach (Element child in holder.Children.Where(child => !IsComment(child)).ToList())
        {
            holder.Remove(child);
        }

        holder.Insert(0, content[0]);
        holder.Add(content.Skip(1));
    }

    /// <summary>
    /// Replaces the comments of a holder (or of the root) with the texts, where a writer writes them: after the TYPE
    /// node of a holder, first in the root.
    /// </summary>
    public static void SetComments(Element holder, IEnumerable<string> texts)
    {
        foreach (Element comment in holder.Children.Where(IsComment).ToList())
        {
            holder.Remove(comment);
        }

        int index = holder.Parent is null ? 0 : 1;

        foreach (string text in texts)
        {
            holder.Insert(index++, CommentNode(text));
        }
    }
}
