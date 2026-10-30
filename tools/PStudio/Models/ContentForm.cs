namespace PStudio.Models;

/// <summary>
/// The forms in which a content (a byte array) is shown and edited: the encodings, and the types of the Data class.
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

    /// <summary>UTF-16 (little-endian) text.</summary>
    Utf16,

    /// <summary>UTF-32 (little-endian) text.</summary>
    Utf32,

    /// <summary>bool, 1 byte.</summary>
    Bool,

    /// <summary>byte.</summary>
    Byte,

    /// <summary>char, 2 bytes.</summary>
    Char,

    /// <summary>short, 2 bytes.</summary>
    Int16,

    /// <summary>ushort, 2 bytes.</summary>
    UInt16,

    /// <summary>int, 4 bytes.</summary>
    Int32,

    /// <summary>uint, 4 bytes.</summary>
    UInt32,

    /// <summary>long, 8 bytes.</summary>
    Int64,

    /// <summary>ulong, 8 bytes.</summary>
    UInt64,

    /// <summary>Variable-length signed integer.</summary>
    VarInt,

    /// <summary>Variable-length unsigned integer.</summary>
    VarUInt,

    /// <summary>float, 4 bytes.</summary>
    Single,

    /// <summary>double, 8 bytes.</summary>
    Double,

    /// <summary>decimal, 16 bytes.</summary>
    Decimal,

    /// <summary>Guid, 16 bytes.</summary>
    Guid,

    /// <summary>DateTime (ticks), 8 bytes.</summary>
    DateTime,

    /// <summary>DateTimeOffset, 16 bytes.</summary>
    DateTimeOffset,

    /// <summary>TimeSpan (ticks), 8 bytes.</summary>
    TimeSpan,

    /// <summary>A metadata tag of SERIALIZER_FORMAT.md (NAME, TYPE, ITEM, VALUE, COMMENT), 1 byte.</summary>
    SerializerTag,

    /// <summary>A type code of SERIALIZER_FORMAT.md (NULL, BOOLEAN, ..., FLOAT64, UTF8_STRING, BLOB), 1 byte.</summary>
    SerializerType,
}
