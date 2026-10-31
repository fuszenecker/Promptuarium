using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace PCfg;

/// <summary>
/// The key of a value, as in IConfiguration: the names of the members and the indexes of the elements, separated by
/// ':' (e.g. "Logging:LogLevel:Default", "Hosts:0"). A ':' or a '\' in a name is escaped with a '\'. The empty key is
/// the root.
/// </summary>
internal sealed class ConfigKey
{
    private ConfigKey(IReadOnlyList<string> segments)
    {
        Segments = segments;
    }

    /// <summary>The key of the root.</summary>
    public static ConfigKey Root { get; } = new(System.Array.Empty<string>());

    /// <summary>The names and the indexes, in order.</summary>
    public IReadOnlyList<string> Segments { get; }

    /// <summary>True for the root.</summary>
    public bool IsRoot => Segments.Count == 0;

    /// <summary>The key of the container of this value (not for the root).</summary>
    public ConfigKey Parent => new(Segments.Take(Segments.Count - 1).ToList());

    /// <summary>The last segment (not for the root).</summary>
    public string Last => Segments[^1];

    /// <summary>Parses a key; the empty text is the root.</summary>
    public static ConfigKey Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (text.Length == 0)
        {
            return Root;
        }

        var segments = new List<string>();
        var current = new StringBuilder();

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (c == '\\')
            {
                if (i + 1 >= text.Length || (text[i + 1] != ':' && text[i + 1] != '\\'))
                {
                    throw new PCfgException(ExitCodes.Usage, $"Invalid key '{text}': a '\\' escapes a ':' or a '\\' only.");
                }

                current.Append(text[++i]);
            }
            else if (c == ':')
            {
                segments.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        segments.Add(current.ToString());
        return new ConfigKey(segments);
    }

    /// <summary>The key of a member or an element of this value.</summary>
    public ConfigKey Child(string segment) => new(Segments.Append(segment).ToList());

    /// <summary>The key of an element of this value.</summary>
    public ConfigKey Child(int index) => Child(index.ToString(CultureInfo.InvariantCulture));

    /// <summary>The segment as an index, if it is one.</summary>
    public static bool TryIndex(string segment, out int index) => int.TryParse(segment, NumberStyles.None, CultureInfo.InvariantCulture, out index);

    /// <summary>A name escaped for a key.</summary>
    public static string Escape(string segment) => segment.Replace("\\", "\\\\", StringComparison.Ordinal).Replace(":", "\\:", StringComparison.Ordinal);

    /// <inheritdoc/>
    public override string ToString() => string.Join(':', Segments.Select(Escape));

    /// <summary>The key for the messages: the root is "(root)".</summary>
    public string Display => IsRoot ? "(root)" : ToString();
}
