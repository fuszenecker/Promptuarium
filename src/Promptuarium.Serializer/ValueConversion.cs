using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Reflection;

namespace Promptuarium.Serializer;

/// <summary>
/// A type stored by a conversion of its own, as UTF8_STRING (its text) or as BLOB (its bytes):
/// <list type="bullet">
/// <item>registered with <see cref="PromptuariumSerializerOptions.AddTextType{T}"/> or
/// <see cref="PromptuariumSerializerOptions.AddBlobType{T}"/> (the format and the parse of the application);</item>
/// <item>declared with <c>[PromptuariumType(Utf8String)]</c> on a property or on the type: written with
/// <see cref="IFormattable.ToString(string?, IFormatProvider?)"/> (invariant culture), or <see cref="object.ToString"/>,
/// and read with <see cref="IParsable{TSelf}.TryParse(string?, IFormatProvider?, out TSelf)"/> (invariant culture).</item>
/// </list>
/// Writing requires the stored form to give back an equal value (<see cref="object.Equals(object?)"/>).
/// </summary>
internal sealed class ValueConversion
{
    private static readonly ConcurrentDictionary<Type, ValueConversion?> Declared = new();
    private static readonly ConcurrentDictionary<Type, ValueConversion?> Parsable = new();
    private static readonly MethodInfo ParserDefinition = typeof(ValueConversion).GetMethod(nameof(Parser), BindingFlags.NonPublic | BindingFlags.Static)!;

    // encode gives a string (UTF8_STRING) or a byte[] (BLOB); tryDecode takes the same, and gives null if it does not take it
    private readonly Func<object, object> encode;
    private readonly Func<object, object?> tryDecode;

    private ValueConversion(Type type, byte storedType, Func<object, object> encode, Func<object, object?> tryDecode)
    {
        Type = type;
        StoredType = storedType;
        this.encode = encode;
        this.tryDecode = tryDecode;
    }

    public Type Type { get; }

    /// <summary>The type code the values are stored as: <see cref="CfgFormat.Utf8String"/> or <see cref="CfgFormat.Blob"/>.</summary>
    public byte StoredType { get; }

    /// <summary>
    /// The types stored as text by default (<see cref="PromptuariumSerializerOptions.ClearDefaultTypes"/> removes them):
    /// <see cref="Version"/>, <see cref="Uri"/> (its original string, absolute or relative), <see cref="IPAddress"/>
    /// and <see cref="IPNetwork"/>. Without a conversion they would be objects that do not come back.
    /// </summary>
    public static IReadOnlyDictionary<Type, ValueConversion> Defaults { get; } = new Dictionary<Type, ValueConversion>
    {
        [typeof(Version)] = Text<Version>(version => version.ToString(), Version.Parse),
        [typeof(Uri)] = Text<Uri>(uri => uri.OriginalString, text => new Uri(text, UriKind.RelativeOrAbsolute)),
        [typeof(IPAddress)] = OfParsable(typeof(IPAddress))!,
        [typeof(IPNetwork)] = OfParsable(typeof(IPNetwork))!,
    };

    /// <summary>A text conversion of the application.</summary>
    public static ValueConversion Text<T>(Func<T, string> format, Func<string, T> parse)
    {
        return new ValueConversion(typeof(T), CfgFormat.Utf8String, value => format((T)value) ?? "", text => Guarded(() => parse((string)text)));
    }

    /// <summary>A binary conversion of the application.</summary>
    public static ValueConversion Blob<T>(Func<T, byte[]> toBytes, Func<byte[], T> fromBytes)
    {
        return new ValueConversion(typeof(T), CfgFormat.Blob, value => toBytes((T)value) ?? [], bytes => Guarded(() => fromBytes((byte[])bytes)));
    }

    /// <summary>
    /// Whether <paramref name="type"/> is stored as UTF8_STRING anyway (strings, chars, enums, dates, ...), or is
    /// <see cref="object"/>: there a Utf8String declaration only confirms the type.
    /// </summary>
    public static bool IsTextOfItsOwn(Type type) => type == typeof(string) || type == typeof(object) || ScalarText.Handles(type);

    /// <summary>Whether <paramref name="type"/> has a mapping of its own, so it can not be registered.</summary>
    public static bool IsStoredAnyway(Type type) => type.IsPrimitive || type == typeof(decimal) || type == typeof(byte[]) || IsTextOfItsOwn(type)
        || type == typeof(Half) || type == typeof(Int128) || type == typeof(UInt128) || type == typeof(System.Numerics.BigInteger);

    /// <summary>The conversion of a type declared as text on the type itself, or null if it is not declared.</summary>
    public static ValueConversion? ForType(Type type)
    {
        return Declared.GetOrAdd(type, static type =>
        {
            PromptuariumTypeAttribute? attribute = type.GetCustomAttribute<PromptuariumTypeAttribute>(inherit: false);

            // The form of an enum (its name or its number) is a scalar, not a conversion: Scalars.EnumForm
            if (attribute is null || type.IsEnum)
            {
                return null;
            }

            if (attribute.Type != PromptuariumType.Utf8String)
            {
                throw new NotSupportedException($"Only Utf8String can be declared on a type: {type.Name} is declared as {attribute.Type}.");
            }

            return OfParsable(type) ?? throw new NotSupportedException($"{type.Name} is declared as Utf8String, but it does not implement IParsable<{type.Name}>.");
        });
    }

    /// <summary>The text conversion through <see cref="IParsable{TSelf}"/>, or null if the type does not implement it.</summary>
    public static ValueConversion? OfParsable(Type type)
    {
        return Parsable.GetOrAdd(type, static type =>
        {
            bool parsable = Array.Exists(type.GetInterfaces(), candidate => candidate.IsGenericType
                && candidate.GetGenericTypeDefinition() == typeof(IParsable<>)
                && candidate.GenericTypeArguments[0] == type);

            if (!parsable)
            {
                return null;
            }

            var parse = (Func<string, object?>)ParserDefinition.MakeGenericMethod(type).Invoke(null, null)!;
            return new ValueConversion(type, CfgFormat.Utf8String, FormatInvariant, text => parse((string)text));
        });
    }

    /// <summary>The content of the VALUE node of <paramref name="value"/>; it must give back an equal value.</summary>
    public ReadOnlyMemory<byte> Encode(object value)
    {
        object stored = encode(value);

        if (!Equals(tryDecode(stored), value))
        {
            string what = stored is string text ? $"text \"{text}\"" : "bytes";
            throw new NotSupportedException($"A {Type.Name} can not be stored so: its {what} do not give back the same value.");
        }

        return stored as byte[] ?? Data.FromUtf8String((string)stored);
    }

    /// <summary>The value of a text (UTF8_STRING) or of bytes (BLOB).</summary>
    public object Decode(object stored)
    {
        return tryDecode(stored)
            ?? throw new InvalidDataException(stored is string text ? $"\"{text}\" is not a {Type.Name}." : $"The bytes are not a {Type.Name}.");
    }

    // A value the application's conversion does not take: an exception of the usual kinds, or null
    private static object? Guarded(Func<object?> decode)
    {
        try
        {
            return decode();
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException or OverflowException)
        {
            return null;
        }
    }

    private static string FormatInvariant(object value)
    {
        return value is IFormattable formattable ? formattable.ToString(null, CultureInfo.InvariantCulture) : value.ToString() ?? "";
    }

    private static Func<string, object?> Parser<T>()
        where T : IParsable<T>
    {
        return static text => T.TryParse(text, CultureInfo.InvariantCulture, out T? value) ? value : null;
    }
}
