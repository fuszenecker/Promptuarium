using System;
using System.Collections.Generic;

namespace PStudio.Services;

/// <summary>The kind of a piece of the YAML or JSON text, by which it is colored.</summary>
public enum SyntaxKind
{
    Plain,
    Key,
    String,
    Number,
    Keyword,
    Tag,
    Comment,
    Punctuation,
}

/// <summary>A piece of the text and its kind.</summary>
public readonly record struct SyntaxSpan(string Text, SyntaxKind Kind);

/// <summary>
/// Splits the YAML and the JSON of <see cref="SerializerText"/> into colored pieces: the keys, the strings, the
/// numbers, true/false/null, the tags (!timestamp), the comments and the punctuation. Adjacent pieces of one kind are
/// merged, the line breaks are plain.
/// </summary>
public static class SyntaxHighlighter
{
    /// <summary>The pieces of a YAML text.</summary>
    public static IReadOnlyList<SyntaxSpan> Yaml(string text) => Split(text, YamlLine);

    /// <summary>The pieces of a JSON text.</summary>
    public static IReadOnlyList<SyntaxSpan> Json(string text) => Split(text, JsonLine);

    private static List<SyntaxSpan> Split(string text, Action<string, List<SyntaxSpan>> line)
    {
        ArgumentNullException.ThrowIfNull(text);
        var spans = new List<SyntaxSpan>();
        string[] lines = text.ReplaceLineEndings("\n").Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0)
            {
                Add(spans, "\n", SyntaxKind.Plain);
            }

            line(lines[i], spans);
        }

        return spans;
    }

    // "# comment", "- value", "key: value", "key:" (a nested object or array follows)
    private static void YamlLine(string line, List<SyntaxSpan> spans)
    {
        int i = SkipSpaces(line, 0);
        Add(spans, line[..i], SyntaxKind.Plain);

        if (i < line.Length && line[i] == '#')
        {
            Add(spans, line[i..], SyntaxKind.Comment);
            return;
        }

        while (i < line.Length && line[i] == '-' && (i + 1 == line.Length || line[i + 1] == ' '))
        {
            Add(spans, "-", SyntaxKind.Punctuation);
            int next = SkipSpaces(line, i + 1);
            Add(spans, line[(i + 1)..next], SyntaxKind.Plain);
            i = next;
        }

        int keyEnd = YamlKeyEnd(line, i);

        if (keyEnd > i)
        {
            Add(spans, line[i..keyEnd], SyntaxKind.Key);
            Add(spans, ":", SyntaxKind.Punctuation);
            i = SkipSpaces(line, keyEnd + 1);
            Add(spans, line[(keyEnd + 1)..i], SyntaxKind.Plain);
        }

        YamlScalar(line[i..], spans);
    }

    // The end of the key of "key: value" or "key:" (where its colon is), or the start if the line has no key
    private static int YamlKeyEnd(string line, int start)
    {
        int end = start < line.Length && line[start] == '"' ? QuotedEnd(line, start) : start;

        for (int i = end; i < line.Length; i++)
        {
            if (line[i] == ':' && (i + 1 == line.Length || line[i + 1] == ' '))
            {
                return i;
            }

            if (line[i] is '"' or '#' or '!')
            {
                break;
            }
        }

        return start;
    }

    private static void YamlScalar(string value, List<SyntaxSpan> spans)
    {
        if (value.Length == 0)
        {
            return;
        }

        if (value[0] == '!')
        {
            int space = value.IndexOf(' ');
            string tag = space < 0 ? value : value[..space];
            Add(spans, tag, SyntaxKind.Tag);

            if (space >= 0)
            {
                Add(spans, " ", SyntaxKind.Plain);
                Add(spans, value[(space + 1)..], SyntaxKind.String);
            }

            return;
        }

        Add(spans, value, value switch
        {
            "{}" or "[]" => SyntaxKind.Punctuation,
            "true" or "false" or "null" => SyntaxKind.Keyword,
            _ when value[0] == '"' => SyntaxKind.String,
            _ when IsNumber(value) => SyntaxKind.Number,
            _ => SyntaxKind.String,
        });
    }

    private static void JsonLine(string line, List<SyntaxSpan> spans)
    {
        int i = 0;

        while (i < line.Length)
        {
            char c = line[i];

            if (c == ' ')
            {
                int end = SkipSpaces(line, i);
                Add(spans, line[i..end], SyntaxKind.Plain);
                i = end;
            }
            else if (c == '"')
            {
                int end = QuotedEnd(line, i);
                int after = SkipSpaces(line, end);
                Add(spans, line[i..end], after < line.Length && line[after] == ':' ? SyntaxKind.Key : SyntaxKind.String);
                i = end;
            }
            else if (c is '{' or '}' or '[' or ']' or ',' or ':')
            {
                Add(spans, c.ToString(), SyntaxKind.Punctuation);
                i++;
            }
            else
            {
                int end = i;

                while (end < line.Length && line[end] is not (' ' or ',' or '}' or ']'))
                {
                    end++;
                }

                string word = line[i..end];
                Add(spans, word, word is "true" or "false" or "null" ? SyntaxKind.Keyword : IsNumber(word) ? SyntaxKind.Number : SyntaxKind.Plain);
                i = end;
            }
        }
    }

    private static bool IsNumber(string text) => text.Length > 0 && (char.IsAsciiDigit(text[0]) || (text[0] is '-' or '+' or '.' && text.Length > 1));

    private static int SkipSpaces(string line, int i)
    {
        while (i < line.Length && line[i] == ' ')
        {
            i++;
        }

        return i;
    }

    // The index after the closing quote of the string that starts at the given quote (escapes as in JSON)
    private static int QuotedEnd(string line, int start)
    {
        for (int i = start + 1; i < line.Length; i++)
        {
            if (line[i] == '\\')
            {
                i++;
            }
            else if (line[i] == '"')
            {
                return i + 1;
            }
        }

        return line.Length;
    }

    private static void Add(List<SyntaxSpan> spans, string text, SyntaxKind kind)
    {
        if (text.Length == 0)
        {
            return;
        }

        if (spans.Count > 0 && spans[^1].Kind == kind)
        {
            spans[^1] = new SyntaxSpan(spans[^1].Text + text, kind);
        }
        else
        {
            spans.Add(new SyntaxSpan(text, kind));
        }
    }
}
