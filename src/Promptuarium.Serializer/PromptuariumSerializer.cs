using System.Collections;
using System.Globalization;
using System.Reflection;

namespace Promptuarium.Serializer;

/// <summary>
/// Maps .NET objects to Promptuarium trees in the layout of <c>SERIALIZER_FORMAT.md</c> and back, directly, without an
/// intermediate representation such as JSON. The files are signed with
/// <see cref="PromptuariumSerializerOptions.Signature"/> (<c>PRMT:GEN1-LE</c> or <c>PRMT:GEN2-LE</c> by default: the lowest
/// version of the layout that holds the tree).
/// <list type="table">
/// <item><term><c>bool</c></term><description>BOOLEAN</description></item>
/// <item><term><c>sbyte</c> ... <c>long</c>, <c>Int128</c>; <c>byte</c> ... <c>ulong</c>, <c>UInt128</c>; <c>BigInteger</c></term><description>INT8 ... INT128, UINT8 ... UINT128, BIGINT (version 2)</description></item>
/// <item><term><c>Half</c>, <c>float</c>, <c>double</c></term><description>FLOAT16, FLOAT32 (version 2), FLOAT64</description></item>
/// <item><term><c>decimal</c></term><description>DECIMAL, its canonical text (version 2)</description></item>
/// <item><term><c>string</c>, <c>char</c>, enums (by name; by number with <c>[PromptuariumType]</c>, e.g. Int32)</term><description>UTF8_STRING</description></item>
/// <item><term><c>Guid</c></term><description>UUID (version 2)</description></item>
/// <item><term><c>DateOnly</c>, <c>TimeOnly</c>, <c>DateTime</c>, <c>DateTimeOffset</c>, <c>TimeSpan</c></term><description>DATE, TIME, DATETIME (an unspecified <c>DateTime</c>) or TIMESTAMP, DURATION (version 2)</description></item>
/// <item><term><c>byte[]</c></term><description>BLOB, the bytes as they are</description></item>
/// <item><term><see cref="Element"/></term><description>CONTAINER, the nested tree (version 2)</description></item>
/// <item><term>string-keyed dictionaries, objects (their public properties)</term><description>OBJECT</description></item>
/// <item><term>other collections (arrays, lists, ...)</term><description>ARRAY</description></item>
/// <item><term><c>null</c></term><description>NULL</description></item>
/// </list>
/// Read into <see cref="object"/>, a file gives <see cref="Dictionary{TKey,TValue}"/> (string to object),
/// <see cref="List{T}"/> (of object), <see cref="bool"/>, <see cref="long"/> (or a wider integer type),
/// <see cref="double"/> (<see cref="float"/>, <see cref="Half"/>), <see cref="decimal"/>, <see cref="string"/>,
/// <see cref="Guid"/>, the date and time types, <c>byte[]</c>, <see cref="Element"/> or <c>null</c>. Unknown members are validated and ignored.
/// <c>[PromptuariumType]</c> declares another form for a property, e.g. FLOAT64 (the form of version 1) for an
/// <c>int</c>, or UTF8_STRING for a <c>Guid</c>.
/// </summary>
/// <remarks>
/// Writing throws <see cref="NotSupportedException"/> for a value that can not be stored (an unsupported type, a
/// number that its declared type does not hold exactly, nesting deeper than <see cref="PromptuariumSerializerOptions.MaxDepth"/>). Reading throws
/// <see cref="InvalidDataException"/> for a file that is not valid, or does not fit the requested type.
/// </remarks>
public static partial class PromptuariumSerializer
{
    /// <summary>Writes <paramref name="value"/> (an object or a string-keyed dictionary) as bytes.</summary>
    public static byte[] Serialize(object value, PromptuariumSerializerOptions? options = null) => ToElement(value, options).ToBytes();

    /// <summary>Writes <paramref name="value"/> (an object or a string-keyed dictionary) to a stream.</summary>
    public static Task SerializeAsync(Stream stream, object value, PromptuariumSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return ToElement(value, options).SaveAsync(stream, cancellationToken);
    }

    /// <summary>The tree of <paramref name="value"/> (an object or a string-keyed dictionary): the signed root and its members.</summary>
    public static Element ToElement(object value, PromptuariumSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(value);
        options ??= PromptuariumSerializerOptions.Default;

        IEnumerable<(string Name, object? Value, byte? Declared, string? Comment, ValueConversion? Conversion)>? members = TryScalar(value, out _, out _) || options.ConversionForType(value.GetType()) is not null
            ? null
            : MembersOf(value, options);

        if (members is null)
        {
            throw new NotSupportedException($"A {value.GetType().Name} can not be the root: the root is an OBJECT.");
        }

        var root = new Element();

        if (TypeComment(value) is { } comment)
        {
            root.Add(CommentNode(comment));
        }

        foreach ((string name, object? member, byte? declared, string? memberComment, ValueConversion? conversion) in members)
        {
            root.Add(Holder(CfgFormat.Name, Data.FromUtf8String(name), member, declared, memberComment, conversion, 1, options));
        }

        root.MetaData = Data.FromAsciiString(options.SignatureFor(NeedsVersion2(root)));
        return root;
    }

    // ---------------------------------------------------------------- writing

    private static Element Holder(byte tag, ReadOnlyMemory<byte> name, object? value, byte? declared, string? comment, ValueConversion? conversion, int depth, PromptuariumSerializerOptions options)
    {
        if (declared is { } reserved && !PromptuariumTypeAttribute.IsSupported((PromptuariumType)reserved))
        {
            throw new NotSupportedException($"The type {(PromptuariumType)reserved} is reserved.");
        }

        // A property declared as Utf8String or Blob whose type is stored otherwise: its text or its bytes; a property
        // declared as another number type (e.g. FLOAT64, the form of version 1), or a date, a time or a UUID declared as
        // text: that form
        Element holder;

        if (conversion is not null && value is not null)
        {
            holder = ConvertedHolder(tag, name, conversion, value);
        }
        else if (declared is { } form && value is not null && Scalars.TryEncodeAs(value, form, out ReadOnlyMemory<byte> content))
        {
            holder = new Element(name, Data.FromByte(tag), TypeNode(form), ValueNode(content));
        }
        else
        {
            holder = Holder(tag, name, value, depth, options);
        }

        byte stored = holder.Children[0].Data.Span[0];

        if (declared is { } type && stored != CfgFormat.Null && stored != type)
        {
            throw new NotSupportedException($"A {value!.GetType().Name} is stored as {(PromptuariumType)stored}, not as the declared {(PromptuariumType)type}.");
        }

        // The comment follows the TYPE node; the one of the member, or else the one of the type of an object
        if ((comment ?? (stored == CfgFormat.Object ? TypeComment(value!) : null)) is { } commentText)
        {
            holder.Insert(1, CommentNode(commentText));
        }

        return holder;
    }

    private static Element Holder(byte tag, ReadOnlyMemory<byte> name, object? value, int depth, PromptuariumSerializerOptions options)
    {
        if (depth > options.MaxDepth)
        {
            throw new NotSupportedException($"The value is nested deeper than {options.MaxDepth} levels (or contains a cycle).");
        }

        var holder = new Element(name, Data.FromByte(tag));

        if (value is null)
        {
            return holder.Add(TypeNode(CfgFormat.Null));
        }

        // An enum whose type declares a number: that form (a property declaring another one is handled by the caller)
        if (value is Enum && Scalars.EnumForm(value.GetType()) is { } enumForm && Scalars.TryEncodeAs(value, enumForm, out ReadOnlyMemory<byte> number))
        {
            return holder.Add(TypeNode(enumForm), ValueNode(number));
        }

        if (TryScalar(value, out byte type, out ReadOnlyMemory<byte> content))
        {
            // A nested tree counts with the depth of the file: its root is one level below the holder
            if (value is Element tree && Containers.DeepestLevel(tree, depth + 1) > options.MaxDepth)
            {
                throw new NotSupportedException($"The value is nested deeper than {options.MaxDepth} levels (with the nodes of a nested tree).");
            }

            return holder.Add(TypeNode(type), ValueNode(content));
        }

        // A type registered in the options (text or bytes), or declared as Utf8String on the type itself
        if (options.ConversionForType(value.GetType()) is { } conversion)
        {
            return ConvertedHolder(tag, name, conversion, value);
        }

        if (MembersOf(value, options) is { } members)
        {
            holder.Add(TypeNode(CfgFormat.Object));

            foreach ((string memberName, object? member, byte? declared, string? comment, ValueConversion? memberConversion) in members)
            {
                holder.Add(Holder(CfgFormat.Name, Data.FromUtf8String(memberName), member, declared, comment, memberConversion, depth + 1, options));
            }

            return holder;
        }

        holder.Add(TypeNode(CfgFormat.Array));

        foreach (object? item in (IEnumerable)value)
        {
            holder.Add(Holder(CfgFormat.Item, ReadOnlyMemory<byte>.Empty, item, null, null, null, depth + 1, options));
        }

        return holder;
    }

    private static Element ConvertedHolder(byte tag, ReadOnlyMemory<byte> name, ValueConversion conversion, object value)
    {
        return new Element(name, Data.FromByte(tag), TypeNode(conversion.StoredType), ValueNode(conversion.Encode(value)));
    }

    // A value that is stored in one VALUE node (SERIALIZER_FORMAT.md, version 2)
    private static bool TryScalar(object value, out byte type, out ReadOnlyMemory<byte> content) => Scalars.TryEncode(value, out type, out content);

    // The lowest version that holds the tree (SERIALIZER_FORMAT.md, 7): a type of version 2, or a NaN or an infinity
    private static bool NeedsVersion2(Element root)
    {
        foreach (Element node in Descendants(root))
        {
            if (node.MetaData.Length != 1 || node.MetaData.Span[0] != CfgFormat.Type || node.Data.Length != 1)
            {
                continue;
            }

            byte type = node.Data.Span[0];

            if (!CfgFormat.IsVersion1(type))
            {
                return true;
            }

            if (type == CfgFormat.Float64 && node.Parent!.Children.FirstOrDefault(sibling => sibling.MetaData.Length == 1 && sibling.MetaData.Span[0] == CfgFormat.Value) is { Data.Length: 8 } value
                && !double.IsFinite(value.Data.AsDouble()))
            {
                return true;
            }
        }

        return false;
    }

    // The members of an OBJECT, or null when the value is an ARRAY
    private static IEnumerable<(string Name, object? Value, byte? Declared, string? Comment, ValueConversion? Conversion)>? MembersOf(object value, PromptuariumSerializerOptions options)
    {
        if (value is IDictionary dictionary)
        {
            return DictionaryMembers(dictionary);
        }

        if (value is IEnumerable)
        {
            return null;
        }

        return TypeShape.Of(value.GetType(), options).Members.Select(member => (member.Name, member.Property.GetValue(value), member.DeclaredType, member.Comment, member.Conversion(options)));
    }

    // IDictionary's own enumerator: the generic one of a Dictionary<,> yields KeyValuePairs, not DictionaryEntries
    private static IEnumerable<(string Name, object? Value, byte? Declared, string? Comment, ValueConversion? Conversion)> DictionaryMembers(IDictionary dictionary)
    {
        IDictionaryEnumerator entries = dictionary.GetEnumerator();

        while (entries.MoveNext())
        {
            yield return (entries.Key as string ?? throw new NotSupportedException("Only dictionaries with string keys can be stored."), entries.Value, null, null, null);
        }
    }

    // The comment of a class or a struct (not of a dictionary or a collection, which are not objects of their own)
    private static string? TypeComment(object value)
    {
        return value is IEnumerable ? null : value.GetType().GetCustomAttribute<PromptuariumCommentAttribute>(inherit: false)?.Text;
    }

    // Every node below the root, without recursion (the enumerator of an Element gives its children only)
    private static IEnumerable<Element> Descendants(Element root)
    {
        var pending = new Stack<Element>(root.Children);

        while (pending.Count > 0)
        {
            Element node = pending.Pop();
            yield return node;

            foreach (Element child in node.Children)
            {
                pending.Push(child);
            }
        }
    }

    private static Element CommentNode(string comment) => new(Data.FromUtf8String(comment), Data.FromByte(CfgFormat.Comment));

    private static Element TypeNode(byte type) => new(Data.FromByte(type), Data.FromByte(CfgFormat.Type));

    private static Element ValueNode(ReadOnlyMemory<byte> value) => new(value, Data.FromByte(CfgFormat.Value));
}
