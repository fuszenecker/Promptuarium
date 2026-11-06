using System.Globalization;
using System.IO;
using System.Text;
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
    /// <summary>A JSON report as YAML. Token by token, without recursion: a tree can be very deep.</summary>
    public static string FromJson(string json)
    {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json), new JsonReaderOptions { MaxDepth = int.MaxValue });
        using var text = new StringWriter(CultureInfo.InvariantCulture);

        // No folding: a long hex text stays on its line
        var emitter = new Emitter(text, new EmitterSettings(2, int.MaxValue, false, 1024, indentSequences: true, newLine: "\n"));
        bool content = false;

        emitter.Emit(new StreamStart());
        emitter.Emit(new DocumentStart());

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.StartObject:
                    // A content is short: on one line
                    emitter.Emit(new MappingStart(AnchorName.Empty, TagName.Empty, true, content ? MappingStyle.Flow : MappingStyle.Block));
                    break;
                case JsonTokenType.EndObject:
                    emitter.Emit(new MappingEnd());
                    break;
                case JsonTokenType.StartArray:
                    emitter.Emit(new SequenceStart(AnchorName.Empty, TagName.Empty, true, SequenceStyle.Block));
                    break;
                case JsonTokenType.EndArray:
                    emitter.Emit(new SequenceEnd());
                    break;
                case JsonTokenType.PropertyName:
                    string name = reader.GetString()!;
                    content = name is "metadata" or "data";
                    emitter.Emit(new Scalar(AnchorName.Empty, TagName.Empty, name, ScalarStyle.Plain, true, false));
                    break;
                case JsonTokenType.String:
                    emitter.Emit(new Scalar(AnchorName.Empty, TagName.Empty, reader.GetString()!, ScalarStyle.DoubleQuoted, false, true));
                    break;
                default:
                    emitter.Emit(new Scalar(AnchorName.Empty, TagName.Empty, Encoding.UTF8.GetString(reader.ValueSpan), ScalarStyle.Plain, true, false));
                    break;
            }
        }

        emitter.Emit(new DocumentEnd(isImplicit: true));
        emitter.Emit(new StreamEnd());

        return text.ToString().TrimEnd('\n');
    }
}
