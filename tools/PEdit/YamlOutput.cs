using System.Globalization;
using System.IO;
using System.Text.Json;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace PEdit;

/// <summary>
/// The YAML form of the machine-readable reports: the same structure as their JSON form. Every string is quoted, so a
/// hex text such as 4152 stays a string for every reader.
/// </summary>
internal static class YamlOutput
{
    /// <summary>A JSON report as YAML.</summary>
    public static string FromJson(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        using var text = new StringWriter(CultureInfo.InvariantCulture);

        // No folding: a long hex text stays on its line
        var emitter = new Emitter(text, new EmitterSettings(2, int.MaxValue, false, 1024, indentSequences: true, newLine: "\n"));

        emitter.Emit(new StreamStart());
        emitter.Emit(new DocumentStart());
        Emit(document.RootElement, emitter);
        emitter.Emit(new DocumentEnd(isImplicit: true));
        emitter.Emit(new StreamEnd());

        return text.ToString().TrimEnd('\n');
    }

    private static void Emit(JsonElement value, Emitter emitter)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                emitter.Emit(new MappingStart(AnchorName.Empty, TagName.Empty, true, MappingStyle.Block));

                foreach (JsonProperty property in value.EnumerateObject())
                {
                    emitter.Emit(new Scalar(AnchorName.Empty, TagName.Empty, property.Name, ScalarStyle.Plain, true, false));
                    Emit(property.Value, emitter);
                }

                emitter.Emit(new MappingEnd());
                break;
            case JsonValueKind.Array:
                emitter.Emit(new SequenceStart(AnchorName.Empty, TagName.Empty, true, SequenceStyle.Block));

                foreach (JsonElement element in value.EnumerateArray())
                {
                    Emit(element, emitter);
                }

                emitter.Emit(new SequenceEnd());
                break;
            case JsonValueKind.String:
                emitter.Emit(new Scalar(AnchorName.Empty, TagName.Empty, value.GetString()!, ScalarStyle.DoubleQuoted, false, true));
                break;
            default:
                emitter.Emit(new Scalar(AnchorName.Empty, TagName.Empty, value.GetRawText(), ScalarStyle.Plain, true, false));
                break;
        }
    }
}
