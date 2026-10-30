using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Promptuarium.Serializer;

/// <summary>
/// The values that version 1 of the format has no type for, stored as UTF8_STRING: <c>char</c>, enums (by name) and the invariant
/// round-trip text of the common date, time and identifier types.
/// </summary>
internal static class ScalarText
{
    public static bool TryFormat(object value, [NotNullWhen(true)] out string? text)
    {
        text = value switch
        {
            char character => character.ToString(),
            Enum enumValue => enumValue.ToString(),
            DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
            DateOnly date => date.ToString("O", CultureInfo.InvariantCulture),
            TimeOnly time => time.ToString("O", CultureInfo.InvariantCulture),
            TimeSpan timeSpan => timeSpan.ToString("c", CultureInfo.InvariantCulture),
            Guid guid => guid.ToString("D"),
            _ => null,
        };

        return text is not null;
    }

    /// <summary>Whether a value of <paramref name="type"/> is read from a UTF8_STRING by <see cref="TryParse"/>.</summary>
    public static bool Handles(Type type)
    {
        return type == typeof(char) || type.IsEnum || type == typeof(DateTime) || type == typeof(DateTimeOffset)
            || type == typeof(DateOnly) || type == typeof(TimeOnly) || type == typeof(TimeSpan) || type == typeof(Guid);
    }

    public static bool TryParse(string text, Type type, [NotNullWhen(true)] out object? value)
    {
        CultureInfo invariant = CultureInfo.InvariantCulture;

        value = type switch
        {
            _ when type == typeof(char) => text.Length == 1 ? text[0] : (object?)null,
            _ when type.IsEnum => Enum.TryParse(type, text, ignoreCase: true, out object? parsed) ? parsed : null,
            _ when type == typeof(DateTime) => DateTime.TryParseExact(text, "O", invariant, DateTimeStyles.RoundtripKind, out DateTime dateTime) ? dateTime : (object?)null,
            _ when type == typeof(DateTimeOffset) => DateTimeOffset.TryParseExact(text, "O", invariant, DateTimeStyles.None, out DateTimeOffset offset) ? offset : (object?)null,
            _ when type == typeof(DateOnly) => DateOnly.TryParseExact(text, "O", invariant, DateTimeStyles.None, out DateOnly date) ? date : (object?)null,
            _ when type == typeof(TimeOnly) => TimeOnly.TryParseExact(text, "O", invariant, DateTimeStyles.None, out TimeOnly time) ? time : (object?)null,
            _ when type == typeof(TimeSpan) => TimeSpan.TryParseExact(text, "c", invariant, out TimeSpan timeSpan) ? timeSpan : (object?)null,
            _ when type == typeof(Guid) => Guid.TryParseExact(text, "D", out Guid guid) ? guid : (object?)null,
            _ => null,
        };

        return value is not null;
    }
}
