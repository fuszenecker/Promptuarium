using System;
using System.IO;
using System.Linq;
using System.Text;

namespace PEdit;

/// <summary>
/// Parses the content given on the command line. The container stores bytes, so the caller states how the bytes are given:
/// hex:4152, utf8:text, ascii:text, base64:QVI=, file:path or empty.
/// </summary>
internal static class ContentValue
{
    /// <summary>
    /// The accepted forms, for the help text.
    /// </summary>
    public const string Forms = "hex:4152 | utf8:TEXT | ascii:TEXT | base64:QVI= | file:PATH | empty";

    /// <summary>
    /// Parses a value; returns null for "empty" (no content).
    /// </summary>
    public static byte[]? Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (text == "empty")
        {
            return null;
        }

        int colon = text.IndexOf(':', StringComparison.Ordinal);

        if (colon < 0)
        {
            throw Invalid(text, "the form is missing");
        }

        string form = text[..colon];
        string value = text[(colon + 1)..];

        try
        {
            byte[] bytes = form switch
            {
                "hex" => Convert.FromHexString(new string(value.Where(c => !char.IsWhiteSpace(c) && c != '-').ToArray())),
                "utf8" => Encoding.UTF8.GetBytes(value),
                "ascii" => value.All(char.IsAscii) ? Encoding.ASCII.GetBytes(value) : throw Invalid(text, "it contains non-ASCII characters"),
                "base64" => Convert.FromBase64String(value),
                "file" => File.ReadAllBytes(value),
                _ => throw Invalid(text, $"unknown form '{form}'"),
            };

            // A zero-length content is the same as no content
            return bytes.Length > 0 ? bytes : null;
        }
        catch (FormatException ex)
        {
            throw Invalid(text, ex.Message);
        }
        catch (IOException ex)
        {
            throw new PEditException(ExitCodes.NotFound, $"Can not read the content file: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new PEditException(ExitCodes.NotFound, $"Can not read the content file: {ex.Message}");
        }
    }

    private static PEditException Invalid(string text, string reason)
    {
        return new PEditException(ExitCodes.Usage, $"Invalid content '{text}': {reason}. Use {Forms}.");
    }
}
