using System.Collections;
using System.Globalization;

namespace Promptuarium.Serializer;

/// <summary>
/// Maps .NET objects to Promptuarium trees in the layout of <c>CONFIG_FORMAT.md</c> (<c>PRMT:CFG1-LE</c>) and back,
/// directly, without an intermediate representation such as JSON.
/// <list type="table">
/// <item><term><c>bool</c></term><description>BOOLEAN</description></item>
/// <item><term>number types (<c>int</c>, <c>double</c>, <c>decimal</c>, ...)</term><description>FLOAT64</description></item>
/// <item><term><c>string</c>, <c>char</c>, enums (by name)</term><description>UTF8_STRING</description></item>
/// <item><term><c>DateTime</c>, <c>DateTimeOffset</c>, <c>DateOnly</c>, <c>TimeOnly</c>, <c>TimeSpan</c>, <c>Guid</c></term><description>UTF8_STRING, invariant round-trip text</description></item>
/// <item><term><c>byte[]</c></term><description>BLOB, the bytes as they are</description></item>
/// <item><term>string-keyed dictionaries, objects (their public properties)</term><description>OBJECT</description></item>
/// <item><term>other collections (arrays, lists, ...)</term><description>ARRAY</description></item>
/// <item><term><c>null</c></term><description>NULL</description></item>
/// </list>
/// Read into <see cref="object"/>, a file gives <see cref="Dictionary{TKey,TValue}"/> (string to object),
/// <see cref="List{T}"/> (of object), <see cref="bool"/>, <see cref="double"/>, <see cref="string"/>,
/// <c>byte[]</c> or <c>null</c>. Unknown members are validated and ignored.
/// </summary>
/// <remarks>
/// Writing throws <see cref="NotSupportedException"/> for a value that can not be stored (an unsupported type, a
/// non-finite number, nesting deeper than <see cref="PromptuariumSerializerOptions.MaxDepth"/>). Reading throws
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

        IEnumerable<(string Name, object? Value, byte? Declared)>? members = TryScalar(value, out _, out _) ? null : MembersOf(value, options);

        if (members is null)
        {
            throw new NotSupportedException($"A {value.GetType().Name} can not be the root: the root is an OBJECT.");
        }

        Element root = new ElementBuilder().WithSignature(Data.FromAsciiString(options.Signature)).Build();

        foreach ((string name, object? member, byte? declared) in members)
        {
            root.Add(Holder(CfgFormat.Name, Data.FromUtf8String(name), member, declared, 1, options));
        }

        return root;
    }

    // ---------------------------------------------------------------- writing

    private static Element Holder(byte tag, ReadOnlyMemory<byte> name, object? value, byte? declared, int depth, PromptuariumSerializerOptions options)
    {
        if (declared is { } reserved && !PromptuariumTypeAttribute.IsSupported((PromptuariumType)reserved))
        {
            throw new NotSupportedException($"The type {(PromptuariumType)reserved} is reserved in CFG1-LE.");
        }

        Element holder = Holder(tag, name, value, depth, options);
        byte stored = holder.Children[0].Data.Span[0];

        if (declared is { } type && stored != CfgFormat.Null && stored != type)
        {
            throw new NotSupportedException($"A {value!.GetType().Name} is stored as {(PromptuariumType)stored}, not as the declared {(PromptuariumType)type}.");
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

        if (TryScalar(value, out byte type, out ReadOnlyMemory<byte> content))
        {
            return holder.Add(TypeNode(type), ValueNode(content));
        }

        if (MembersOf(value, options) is { } members)
        {
            holder.Add(TypeNode(CfgFormat.Object));

            foreach ((string memberName, object? member, byte? declared) in members)
            {
                holder.Add(Holder(CfgFormat.Name, Data.FromUtf8String(memberName), member, declared, depth + 1, options));
            }

            return holder;
        }

        holder.Add(TypeNode(CfgFormat.Array));

        foreach (object? item in (IEnumerable)value)
        {
            holder.Add(Holder(CfgFormat.Item, ReadOnlyMemory<byte>.Empty, item, depth + 1, options));
        }

        return holder;
    }

    // A value that is stored in one VALUE node
    private static bool TryScalar(object value, out byte type, out ReadOnlyMemory<byte> content)
    {
        switch (value)
        {
            case bool flag:
                (type, content) = (CfgFormat.Boolean, Data.FromBool(flag));
                return true;

            case string text:
                (type, content) = (CfgFormat.Utf8String, Data.FromUtf8String(text));
                return true;

            case byte[] blob:
                (type, content) = (CfgFormat.Blob, blob);
                return true;

            case sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal:
                (type, content) = (CfgFormat.Float64, Data.FromDouble(FiniteNumber(value)));
                return true;

            case nint or nuint or Int128 or UInt128 or Half:
                throw new NotSupportedException($"A {value.GetType().Name} can not be stored: CFG1-LE has FLOAT64 numbers only.");

            default:
                if (ScalarText.TryFormat(value, out string? formatted))
                {
                    (type, content) = (CfgFormat.Utf8String, Data.FromUtf8String(formatted));
                    return true;
                }

                (type, content) = (0, ReadOnlyMemory<byte>.Empty);
                return false;
        }
    }

    private static double FiniteNumber(object value)
    {
        double number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
        return double.IsFinite(number) ? number : throw new NotSupportedException("NaN and infinity can not be stored.");
    }

    // The members of an OBJECT, or null when the value is an ARRAY
    private static IEnumerable<(string Name, object? Value, byte? Declared)>? MembersOf(object value, PromptuariumSerializerOptions options)
    {
        if (value is IDictionary dictionary)
        {
            return DictionaryMembers(dictionary);
        }

        if (value is IEnumerable)
        {
            return null;
        }

        return TypeShape.Of(value.GetType(), options).Members.Select(member => (member.Name, member.Property.GetValue(value), member.DeclaredType));
    }

    // IDictionary's own enumerator: the generic one of a Dictionary<,> yields KeyValuePairs, not DictionaryEntries
    private static IEnumerable<(string Name, object? Value, byte? Declared)> DictionaryMembers(IDictionary dictionary)
    {
        IDictionaryEnumerator entries = dictionary.GetEnumerator();

        while (entries.MoveNext())
        {
            yield return (entries.Key as string ?? throw new NotSupportedException("Only dictionaries with string keys can be stored."), entries.Value, null);
        }
    }

    private static Element TypeNode(byte type) => new(Data.FromByte(type), Data.FromByte(CfgFormat.Type));

    private static Element ValueNode(ReadOnlyMemory<byte> value) => new(value, Data.FromByte(CfgFormat.Value));
}
