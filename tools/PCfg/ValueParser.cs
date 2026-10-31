using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Promptuarium;

namespace PCfg;

/// <summary>
/// Turns the values of the command line and the values of a JSON document into the content of a holder (a TYPE node
/// first, then the VALUE node, the members or the elements).
/// </summary>
internal static partial class ValueParser
{
    /// <summary>The accepted forms, for the help text.</summary>
    public const string Forms = "string:TEXT | number:1.5 | bool:true | null | {} | [] | hex:DEADBEEF | base64:3q2+7w== | file:PATH | json:JSON";

    private static readonly JsonDocumentOptions JsonOptions = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        MaxDepth = Validator.MaxDepth,
    };

    /// <summary>
    /// Parses a value of the command line. A value with a form (string:, number:, ...) has that type. A value without a
    /// form keeps the type of the existing scalar (<paramref name="existingType"/>); a new value is a boolean (true,
    /// false), null, a number (as in JSON, e.g. 8080 or 1.5), or else a string.
    /// </summary>
    public static Element[] Parse(string text, byte? existingType)
    {
        ArgumentNullException.ThrowIfNull(text);

        switch (text)
        {
            case "null":
                return CfgLayout.EmptyContent(CfgLayout.Null);
            case "{}":
                return CfgLayout.EmptyContent(CfgLayout.Object);
            case "[]":
                return CfgLayout.EmptyContent(CfgLayout.Array);
        }

        int colon = text.IndexOf(':', StringComparison.Ordinal);
        string form = colon > 0 ? text[..colon] : string.Empty;
        string value = colon > 0 ? text[(colon + 1)..] : text;

        try
        {
            switch (form)
            {
                case "string":
                    return CfgLayout.StringContent(value);
                case "number":
                    return CfgLayout.NumberContent(ParseNumber(value, text));
                case "bool":
                    return CfgLayout.BooleanContent(ParseBoolean(value, text));
                case "hex":
                    return CfgLayout.BlobContent(Convert.FromHexString(new string(value.Where(c => !char.IsWhiteSpace(c) && c != '-').ToArray())));
                case "base64":
                    return CfgLayout.BlobContent(Convert.FromBase64String(value));
                case "file":
                    return CfgLayout.BlobContent(File.ReadAllBytes(value));
                case "json":
                    return FromJson(value, "the value");
            }
        }
        catch (FormatException ex)
        {
            throw Invalid(text, ex.Message);
        }
        catch (IOException ex)
        {
            throw new PCfgException(ExitCodes.NotFound, $"Can not read the content file: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new PCfgException(ExitCodes.NotFound, $"Can not read the content file: {ex.Message}");
        }

        // No form: the text as a whole
        return existingType switch
        {
            CfgLayout.Utf8String => CfgLayout.StringContent(text),
            CfgLayout.Float64 => CfgLayout.NumberContent(ParseNumber(text, text)),
            CfgLayout.Boolean => CfgLayout.BooleanContent(ParseBoolean(text, text)),
            CfgLayout.Blob => Parse("hex:" + text, null),
            _ when text is "true" or "false" => CfgLayout.BooleanContent(text == "true"),
            _ when JsonNumber().IsMatch(text) && double.IsFinite(double.Parse(text, CultureInfo.InvariantCulture)) =>
                CfgLayout.NumberContent(double.Parse(text, CultureInfo.InvariantCulture)),
            _ => CfgLayout.StringContent(text),
        };
    }

    /// <summary>The members of the root, from a JSON document whose root is an object.</summary>
    public static Element[] RootFromJson(string json, string source)
    {
        Element[] content = FromJson(json, source);

        if (content[0].Data.Span[0] != CfgLayout.Object)
        {
            throw new PCfgException(ExitCodes.Failed, $"The root of {source} is not an object.");
        }

        return content.Skip(1).ToArray();
    }

    private static Element[] FromJson(string json, string source)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json, JsonOptions);
            return FromJson(document.RootElement, source);
        }
        catch (JsonException ex)
        {
            throw new PCfgException(ExitCodes.Failed, $"{source} is not valid JSON: {ex.Message}");
        }
    }

    private static Element[] FromJson(JsonElement json, string source)
    {
        switch (json.ValueKind)
        {
            case JsonValueKind.Object:
                // A repeated name: the last one wins (as in IConfiguration), so only that one is written
                var members = new List<(string Name, Element Holder)>();

                foreach (JsonProperty property in json.EnumerateObject())
                {
                    members.RemoveAll(member => member.Name == property.Name);
                    Element holder = CfgLayout.NameNode(property.Name);
                    holder.Add(FromJson(property.Value, source));
                    members.Add((property.Name, holder));
                }

                return [CfgLayout.TypeNode(CfgLayout.Object), .. members.Select(member => member.Holder)];

            case JsonValueKind.Array:
                var items = new List<Element> { CfgLayout.TypeNode(CfgLayout.Array) };

                foreach (JsonElement element in json.EnumerateArray())
                {
                    Element item = CfgLayout.ItemNode();
                    item.Add(FromJson(element, source));
                    items.Add(item);
                }

                return items.ToArray();

            case JsonValueKind.String:
                return CfgLayout.StringContent(json.GetString()!);
            case JsonValueKind.Number:
                return json.TryGetDouble(out double number) && double.IsFinite(number)
                    ? CfgLayout.NumberContent(number)
                    : throw new PCfgException(ExitCodes.Failed, $"The number {json.GetRawText()} in {source} is not a finite double.");
            case JsonValueKind.True:
                return CfgLayout.BooleanContent(true);
            case JsonValueKind.False:
                return CfgLayout.BooleanContent(false);
            default:
                return CfgLayout.EmptyContent(CfgLayout.Null);
        }
    }

    private static double ParseNumber(string value, string text)
    {
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double number) && double.IsFinite(number))
        {
            return number;
        }

        throw Invalid(text, "not a finite number");
    }

    private static bool ParseBoolean(string value, string text) => value switch
    {
        "true" => true,
        "false" => false,
        _ => throw Invalid(text, "a boolean is true or false"),
    };

    private static PCfgException Invalid(string text, string reason) => new(ExitCodes.Usage, $"Invalid value '{text}': {reason}. Use {Forms}.");

    [GeneratedRegex(@"^-?(0|[1-9][0-9]*)(\.[0-9]+)?([eE][+-]?[0-9]+)?$", RegexOptions.CultureInvariant)]
    private static partial Regex JsonNumber();
}
