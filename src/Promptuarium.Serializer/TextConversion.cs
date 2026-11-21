using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;

namespace Promptuarium.Serializer;

/// <summary>
/// A type stored as UTF8_STRING by its own text, because it is declared so: <c>[PromptuariumType(Utf8String)]</c> on a
/// property, or on the type (then everywhere, in collections and dictionaries too). It is written with
/// <see cref="IFormattable.ToString(string?, IFormatProvider?)"/> (invariant culture), or <see cref="object.ToString"/>,
/// and read with <see cref="IParsable{TSelf}.TryParse(string?, IFormatProvider?, out TSelf)"/> (invariant culture).
/// Writing requires the text to give back an equal value (<see cref="object.Equals(object?)"/>).
/// </summary>
internal sealed class TextConversion
{
    private static readonly ConcurrentDictionary<Type, TextConversion?> Declared = new();
    private static readonly ConcurrentDictionary<Type, TextConversion?> Parsable = new();
    private static readonly MethodInfo ParserDefinition = typeof(TextConversion).GetMethod(nameof(Parser), BindingFlags.NonPublic | BindingFlags.Static)!;

    private readonly Func<string, object?> parse;

    private TextConversion(Type type, Func<string, object?> parse)
    {
        Type = type;
        this.parse = parse;
    }

    public Type Type { get; }

    /// <summary>The conversion of a type declared as text on the type itself, or null if it is not declared.</summary>
    public static TextConversion? ForType(Type type)
    {
        return Declared.GetOrAdd(type, static type =>
        {
            PromptuariumTypeAttribute? attribute = type.GetCustomAttribute<PromptuariumTypeAttribute>(inherit: false);

            if (attribute is null)
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

    /// <summary>
    /// The conversion of a property declared as Utf8String. Null for the types that are stored as UTF8_STRING anyway
    /// (strings, chars, enums, dates, ...) and for <see cref="object"/>: there the declaration only confirms the type.
    /// </summary>
    public static TextConversion? ForProperty(PropertyInfo property)
    {
        Type type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        if (type == typeof(string) || type == typeof(object) || ScalarText.Handles(type))
        {
            return null;
        }

        return ForType(type) ?? OfParsable(type)
            ?? throw new NotSupportedException($"The property {property.Name} is declared as Utf8String, but {type.Name} does not implement IParsable<{type.Name}>.");
    }

    /// <summary>The text of <paramref name="value"/>; it must give back an equal value.</summary>
    public string Format(object value)
    {
        string text = value is IFormattable formattable ? formattable.ToString(null, CultureInfo.InvariantCulture) : value.ToString() ?? "";

        if (!Equals(parse(text), value))
        {
            throw new NotSupportedException($"The {Type.Name} \"{text}\" can not be stored as text: its text does not give back the same value.");
        }

        return text;
    }

    public object Parse(string text) => parse(text) ?? throw new InvalidDataException($"\"{text}\" is not a {Type.Name}.");

    private static TextConversion? OfParsable(Type type)
    {
        return Parsable.GetOrAdd(type, static type =>
        {
            bool parsable = Array.Exists(type.GetInterfaces(), candidate => candidate.IsGenericType
                && candidate.GetGenericTypeDefinition() == typeof(IParsable<>)
                && candidate.GenericTypeArguments[0] == type);

            return parsable ? new TextConversion(type, (Func<string, object?>)ParserDefinition.MakeGenericMethod(type).Invoke(null, null)!) : null;
        });
    }

    private static Func<string, object?> Parser<T>()
        where T : IParsable<T>
    {
        return static text => T.TryParse(text, CultureInfo.InvariantCulture, out T? value) ? value : null;
    }
}
