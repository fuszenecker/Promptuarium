using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;

namespace Promptuarium.Serializer;

/// <summary>
/// A type stored as UTF8_STRING by its own text. Either registered with
/// <see cref="PromptuariumSerializerOptions.AddTextType{T}"/> (its own format and
/// parse), or declared with <c>[PromptuariumType(Utf8String)]</c> on a property or on the type, and then written with
/// <see cref="IFormattable.ToString(string?, IFormatProvider?)"/> (invariant culture), or <see cref="object.ToString"/>,
/// and read with <see cref="IParsable{TSelf}.TryParse(string?, IFormatProvider?, out TSelf)"/> (invariant culture).
/// Writing requires the text to give back an equal value (<see cref="object.Equals(object?)"/>).
/// </summary>
internal sealed class TextConversion
{
    private static readonly ConcurrentDictionary<Type, TextConversion?> Declared = new();
    private static readonly ConcurrentDictionary<Type, TextConversion?> Parsable = new();
    private static readonly MethodInfo ParserDefinition = typeof(TextConversion).GetMethod(nameof(Parser), BindingFlags.NonPublic | BindingFlags.Static)!;

    private readonly Func<object, string> format;
    private readonly Func<string, object?> tryParse;

    private TextConversion(Type type, Func<object, string> format, Func<string, object?> tryParse)
    {
        Type = type;
        this.format = format;
        this.tryParse = tryParse;
    }

    public Type Type { get; }

    /// <summary>A conversion of the application: a text that does not parse (an exception of the usual kinds, or null) is rejected.</summary>
    public static TextConversion Create<T>(Func<T, string> format, Func<string, T> parse)
    {
        return new TextConversion(
            typeof(T),
            value => format((T)value) ?? "",
            text =>
            {
                try
                {
                    return parse(text);
                }
                catch (Exception exception) when (exception is FormatException or ArgumentException or OverflowException)
                {
                    return null;
                }
            });
    }

    /// <summary>
    /// Whether <paramref name="type"/> is stored as UTF8_STRING anyway (strings, chars, enums, dates, ...), or is
    /// <see cref="object"/>: there a Utf8String declaration only confirms the type.
    /// </summary>
    public static bool IsTextOfItsOwn(Type type) => type == typeof(string) || type == typeof(object) || ScalarText.Handles(type);

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

    /// <summary>The conversion through <see cref="IParsable{TSelf}"/>, or null if the type does not implement it.</summary>
    public static TextConversion? OfParsable(Type type)
    {
        return Parsable.GetOrAdd(type, static type =>
        {
            bool parsable = Array.Exists(type.GetInterfaces(), candidate => candidate.IsGenericType
                && candidate.GetGenericTypeDefinition() == typeof(IParsable<>)
                && candidate.GenericTypeArguments[0] == type);

            return parsable
                ? new TextConversion(type, FormatInvariant, (Func<string, object?>)ParserDefinition.MakeGenericMethod(type).Invoke(null, null)!)
                : null;
        });
    }

    /// <summary>The text of <paramref name="value"/>; it must give back an equal value.</summary>
    public string Format(object value)
    {
        string text = format(value);

        if (!Equals(tryParse(text), value))
        {
            throw new NotSupportedException($"The {Type.Name} \"{text}\" can not be stored as text: its text does not give back the same value.");
        }

        return text;
    }

    public object Parse(string text) => tryParse(text) ?? throw new InvalidDataException($"\"{text}\" is not a {Type.Name}.");

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
