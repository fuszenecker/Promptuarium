using System;
using System.Linq;
using System.Text;

using PStudio.Models;

namespace PStudio.Services;

/// <summary>
/// Converts contents to and from their text forms.
/// </summary>
public static class ContentFormat
{
    /// <summary>
    /// Contents longer than this are not shown in the text editor (they can be imported and exported).
    /// </summary>
    public const int MaxEditableLength = 1024 * 1024;

    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>
    /// The form a content is best shown in: UTF-8 for printable text, otherwise hex.
    /// </summary>
    public static ContentForm Suggest(ReadOnlySpan<byte> content)
    {
        return content.Length > 0 && TryUtf8(content, out string text) && text.All(c => !char.IsControl(c) || c is '\n' or '\r' or '\t')
            ? ContentForm.Utf8
            : ContentForm.Hex;
    }

    /// <summary>
    /// Whether the content can be shown in the form without losing bytes.
    /// </summary>
    public static bool CanShow(ReadOnlySpan<byte> content, ContentForm form)
    {
        return form switch
        {
            ContentForm.Utf8 => TryUtf8(content, out _),
            ContentForm.Ascii => Ascii.IsValid(content),
            _ => true,
        };
    }

    /// <summary>
    /// The text form of a content. Bytes that the form can not show are replaced (see <see cref="CanShow"/>).
    /// </summary>
    public static string Format(ReadOnlySpan<byte> content, ContentForm form)
    {
        return form switch
        {
            ContentForm.Hex => FormatHex(content),
            ContentForm.Utf8 => Encoding.UTF8.GetString(content),
            ContentForm.Ascii => Encoding.ASCII.GetString(content),
            ContentForm.Base64 => Convert.ToBase64String(content, Base64FormattingOptions.InsertLineBreaks),
            _ => throw new ArgumentOutOfRangeException(nameof(form), form, null),
        };
    }

    /// <summary>
    /// Parses the text form of a content.
    /// </summary>
    /// <exception cref="FormatException">The text is not valid in the form</exception>
    public static byte[] Parse(string text, ContentForm form)
    {
        ArgumentNullException.ThrowIfNull(text);

        return form switch
        {
            ContentForm.Hex => Convert.FromHexString(new string(text.Where(c => !char.IsWhiteSpace(c) && c != '-').ToArray())),
            ContentForm.Utf8 => Encoding.UTF8.GetBytes(text),
            ContentForm.Ascii => text.All(char.IsAscii) ? Encoding.ASCII.GetBytes(text) : throw new FormatException("The text contains non-ASCII characters."),
            ContentForm.Base64 => Convert.FromBase64String(text),
            _ => throw new ArgumentOutOfRangeException(nameof(form), form, null),
        };
    }

    /// <summary>
    /// A short, one-line preview of a content, for the tree.
    /// </summary>
    public static string Preview(ReadOnlySpan<byte> content, int maxLength = 24)
    {
        if (content.IsEmpty)
        {
            return string.Empty;
        }

        ReadOnlySpan<byte> head = content[..Math.Min(content.Length, maxLength)];
        string ellipsis = head.Length < content.Length ? "…" : string.Empty;

        if (Suggest(content) == ContentForm.Utf8)
        {
            // The cut may split a UTF-8 sequence (at most 4 bytes long)
            for (int length = head.Length; length > head.Length - 4 && length > 0; length--)
            {
                if (TryUtf8(head[..length], out string text))
                {
                    return "\"" + text.ReplaceLineEndings(" ") + ellipsis + "\"";
                }
            }
        }

        return Convert.ToHexString(head) + ellipsis;
    }

    private static string FormatHex(ReadOnlySpan<byte> content)
    {
        var builder = new StringBuilder(content.Length * 3);

        for (int i = 0; i < content.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(i % 16 == 0 ? '\n' : ' ');
            }

            builder.Append(Convert.ToHexString(content.Slice(i, 1)));
        }

        return builder.ToString();
    }

    private static bool TryUtf8(ReadOnlySpan<byte> content, out string text)
    {
        try
        {
            text = StrictUtf8.GetString(content);
            return true;
        }
        catch (ArgumentException)
        {
            text = string.Empty;
            return false;
        }
    }
}
