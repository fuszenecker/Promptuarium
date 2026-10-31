using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Promptuarium;

namespace PCfg;

/// <summary>
/// A problem of a configuration file: an error (the file does not follow SERIALIZER_FORMAT.md) or a warning (e.g. a
/// repeated name: valid, but the earlier members are not used).
/// </summary>
/// <param name="IsError">True for an error, false for a warning</param>
/// <param name="Key">The key of the value (the root is "(root)")</param>
/// <param name="Message">What is wrong</param>
internal sealed record Problem(bool IsError, string Key, string Message)
{
    /// <inheritdoc/>
    public override string ToString() => $"{(IsError ? "error" : "warning")}: {Key}: {Message}";
}

/// <summary>
/// Checks a tree by the rules of SERIALIZER_FORMAT.md: the signature, the tags, the type codes, the structure of the
/// holders, the values and the comments. Every problem is reported, not only the first one.
/// </summary>
internal static class Validator
{
    /// <summary>The deepest nesting that is accepted (deeper trees are an error, so the recursion is bounded).</summary>
    public const int MaxDepth = 256;

    /// <summary>Checks a tree.</summary>
    public static List<Problem> Check(Element root, string signature)
    {
        ArgumentNullException.ThrowIfNull(root);
        var problems = new List<Problem>();
        byte[] expected = Encoding.ASCII.GetBytes(signature);

        if (!root.MetaData.Span.SequenceEqual(expected))
        {
            string found = root.MetaData.IsEmpty ? "none" : $"'{Printable(root.MetaData.Span)}'";
            problems.Add(new Problem(true, ConfigKey.Root.Display, $"the signature is {found}, not '{signature}' (use --signature for another kind)"));
        }

        if (!root.Data.IsEmpty)
        {
            problems.Add(new Problem(true, ConfigKey.Root.Display, "the root has data"));
        }

        CheckChildren(root, ConfigKey.Root, CfgLayout.Object, 0, problems);
        return problems;
    }

    private static void CheckHolder(Element holder, ConfigKey key, int depth, List<Problem> problems)
    {
        if (depth > MaxDepth)
        {
            problems.Add(new Problem(true, key.Display, $"nested deeper than {MaxDepth} levels"));
            return;
        }

        if (holder.Children.Count == 0 || CfgLayout.Tag(holder.Children[0]) != CfgLayout.TypeTag)
        {
            problems.Add(new Problem(true, key.Display, "the first child of the holder is not a TYPE node"));
            return;
        }

        Element typeNode = holder.Children[0];

        if (typeNode.Children.Count > 0)
        {
            problems.Add(new Problem(true, key.Display, "the TYPE node has children"));
        }

        if (typeNode.Data.Length != 1)
        {
            problems.Add(new Problem(true, key.Display, string.Create(CultureInfo.InvariantCulture, $"the TYPE node has {typeNode.Data.Length} bytes, not one type code")));
            return;
        }

        byte type = typeNode.Data.Span[0];

        if (!CfgLayout.SupportedTypes.Contains(type))
        {
            string what = CfgLayout.Types.ContainsKey(type) ? "reserved" : "unknown";
            problems.Add(new Problem(true, key.Display, $"the type {CfgLayout.TypeName(type)} is {what} (not supported by version 1 of the layout)"));
            return;
        }

        CheckChildren(holder, key, type, 1, problems, depth);
    }

    private static void CheckChildren(Element holder, ConfigKey key, byte type, int first, List<Problem> problems, int depth = 0)
    {
        int values = 0;
        int items = 0;
        var names = new Dictionary<string, int>(StringComparer.Ordinal);

        for (int i = first; i < holder.Children.Count; i++)
        {
            Element child = holder.Children[i];
            byte? tag = CfgLayout.Tag(child);

            if (tag is null || !CfgLayout.Tags.ContainsKey(tag.Value))
            {
                string metadata = child.MetaData.IsEmpty ? "no metadata" : $"the metadata {Convert.ToHexString(child.MetaData.Span)}";
                problems.Add(new Problem(true, key.Display, string.Create(CultureInfo.InvariantCulture, $"child {i} has {metadata}, not a tag")));
                continue;
            }

            switch (tag.Value)
            {
                case CfgLayout.CommentTag:
                    if (child.Children.Count > 0)
                    {
                        problems.Add(new Problem(true, key.Display, "a COMMENT node has children"));
                    }

                    if (!IsUtf8(child.Data.Span))
                    {
                        problems.Add(new Problem(true, key.Display, "a comment is not valid UTF-8"));
                    }

                    break;

                case CfgLayout.NameTag when type == CfgLayout.Object:
                    if (!IsUtf8(child.Data.Span))
                    {
                        problems.Add(new Problem(true, key.Display, $"the name {Convert.ToHexString(child.Data.Span)} is not valid UTF-8"));
                        break;
                    }

                    string name = CfgLayout.NameOf(child);
                    names[name] = names.GetValueOrDefault(name) + 1;
                    CheckHolder(child, key.Child(name), depth + 1, problems);
                    break;

                case CfgLayout.ItemTag when type == CfgLayout.Array:
                    if (!child.Data.IsEmpty)
                    {
                        problems.Add(new Problem(true, key.Child(items).Display, "the ITEM node has data"));
                    }

                    CheckHolder(child, key.Child(items), depth + 1, problems);
                    items++;
                    break;

                case CfgLayout.ValueTag when type is CfgLayout.Boolean or CfgLayout.Float64 or CfgLayout.Utf8String or CfgLayout.Blob:
                    values++;
                    CheckValue(child, key, type, problems);
                    break;

                default:
                    string where = holder.Parent is null ? "the root" : $"a holder of {CfgLayout.TypeName(type)}";
                    problems.Add(new Problem(true, key.Display, string.Create(CultureInfo.InvariantCulture, $"child {i} is a {CfgLayout.Tags[tag.Value]} node, which can not be in {where}")));
                    break;
            }
        }

        if ((type is CfgLayout.Boolean or CfgLayout.Float64 or CfgLayout.Utf8String or CfgLayout.Blob) && values != 1)
        {
            problems.Add(new Problem(true, key.Display, string.Create(CultureInfo.InvariantCulture, $"a holder of {CfgLayout.TypeName(type)} has {values} VALUE nodes, not one")));
        }

        foreach ((string name, int count) in names.Where(pair => pair.Value > 1))
        {
            problems.Add(new Problem(false, key.Child(name).Display, string.Create(CultureInfo.InvariantCulture, $"the name is repeated {count} times; the last one wins")));
        }
    }

    private static void CheckValue(Element value, ConfigKey key, byte type, List<Problem> problems)
    {
        if (value.Children.Count > 0)
        {
            problems.Add(new Problem(true, key.Display, "the VALUE node has children"));
        }

        ReadOnlySpan<byte> data = value.Data.Span;

        string? error = type switch
        {
            CfgLayout.Boolean when data.Length != 1 || data[0] > 1 => $"the BOOLEAN value {Convert.ToHexString(data)} is not 00 or 01",
            CfgLayout.Float64 when data.Length != 8 => string.Create(CultureInfo.InvariantCulture, $"the FLOAT64 value has {data.Length} bytes, not 8"),
            CfgLayout.Float64 when !double.IsFinite(CfgLayout.ReadNumber(value)) => "the FLOAT64 value is NaN or infinite",
            CfgLayout.Utf8String when !IsUtf8(data) => "the UTF8_STRING value is not valid UTF-8",
            _ => null,
        };

        if (error != null)
        {
            problems.Add(new Problem(true, key.Display, error));
        }
    }

    private static bool IsUtf8(ReadOnlySpan<byte> bytes)
    {
        try
        {
            CfgLayout.StrictUtf8.GetCharCount(bytes);
            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }

    private static string Printable(ReadOnlySpan<byte> bytes)
    {
        var text = new StringBuilder();

        foreach (byte b in bytes)
        {
            text.Append(b is >= 0x20 and < 0x7F ? (char)b : '.');
        }

        return text.ToString();
    }
}
