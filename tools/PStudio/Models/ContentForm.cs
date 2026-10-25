namespace PStudio.Models;

/// <summary>
/// The forms in which a content (a byte array) is shown and edited.
/// </summary>
public enum ContentForm
{
    /// <summary>Hexadecimal bytes, 16 per line.</summary>
    Hex,

    /// <summary>UTF-8 text.</summary>
    Utf8,

    /// <summary>ASCII text.</summary>
    Ascii,

    /// <summary>Base64.</summary>
    Base64,
}
