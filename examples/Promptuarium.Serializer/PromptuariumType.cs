using System.Diagnostics.CodeAnalysis;

namespace Promptuarium.Serializer;

/// <summary>
/// The type codes of CONFIG_FORMAT.md (section 4), the data of a TYPE node. CFG1-LE supports <see cref="Null"/>,
/// <see cref="Boolean"/>, <see cref="Object"/>, <see cref="Array"/>, <see cref="Float64"/>, <see cref="Utf8String"/>
/// and <see cref="Blob"/>; the others are reserved for later versions.
/// </summary>
[Experimental(PromptuariumTypeAttribute.DiagnosticId)]
public enum PromptuariumType : byte
{
    /// <summary>No value.</summary>
    Null = 0x00,

    /// <summary>One byte: 0x00 = false, 0x01 = true.</summary>
    Boolean = 0x01,

    /// <summary>NAME members.</summary>
    Object = 0x20,

    /// <summary>ITEM elements.</summary>
    Array = 0x21,

    /// <summary>Reserved.</summary>
    Int8 = 0x40,

    /// <summary>Reserved.</summary>
    Int16 = 0x41,

    /// <summary>Reserved.</summary>
    Int32 = 0x42,

    /// <summary>Reserved.</summary>
    Int64 = 0x43,

    /// <summary>Reserved.</summary>
    Int128 = 0x44,

    /// <summary>Reserved.</summary>
    UInt8 = 0x50,

    /// <summary>Reserved.</summary>
    UInt16 = 0x51,

    /// <summary>Reserved.</summary>
    UInt32 = 0x52,

    /// <summary>Reserved.</summary>
    UInt64 = 0x53,

    /// <summary>Reserved.</summary>
    UInt128 = 0x54,

    /// <summary>Reserved.</summary>
    Float8 = 0x60,

    /// <summary>Reserved.</summary>
    Float16 = 0x61,

    /// <summary>Reserved.</summary>
    Float32 = 0x62,

    /// <summary>8 bytes, IEEE 754 binary64, little-endian.</summary>
    Float64 = 0x63,

    /// <summary>Reserved.</summary>
    Float128 = 0x64,

    /// <summary>UTF-8 bytes.</summary>
    Utf8String = 0x80,

    /// <summary>Raw bytes.</summary>
    Blob = 0xF0,
}

/// <summary>
/// The type a property is stored as. Writing stores the property with this type code and rejects a value that does
/// not map to it; reading requires the file to have this type (or NULL) for the property.
/// </summary>
/// <remarks>
/// Experimental: today it can only state the type the value maps to anyway (an <c>int</c> is a
/// <see cref="PromptuariumType.Float64"/>), and the reserved types are rejected. It is the place where e.g.
/// <see cref="PromptuariumType.Int64"/> instead of <see cref="PromptuariumType.Float64"/> will be chosen once the
/// format supports it.
/// </remarks>
/// <param name="type">The type code the property is stored as.</param>
[Experimental(DiagnosticId)]
[AttributeUsage(AttributeTargets.Property)]
public sealed class PromptuariumTypeAttribute(PromptuariumType type) : Attribute
{
    /// <summary>The diagnostic that a use of the experimental type control reports.</summary>
    public const string DiagnosticId = "PRMTSER001";

    /// <summary>The type code the property is stored as.</summary>
    public PromptuariumType Type { get; } = type;

    /// <summary>Whether CFG1-LE supports the type (it is not reserved).</summary>
    internal static bool IsSupported(PromptuariumType type) => type is PromptuariumType.Null or PromptuariumType.Boolean
        or PromptuariumType.Object or PromptuariumType.Array or PromptuariumType.Float64 or PromptuariumType.Utf8String or PromptuariumType.Blob;
}
