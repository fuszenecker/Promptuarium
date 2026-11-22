using System.Diagnostics.CodeAnalysis;

namespace Promptuarium.Serializer;

/// <summary>
/// The type codes of SERIALIZER_FORMAT.md (section 4), the data of a TYPE node. Version 1 of the layout supports <see cref="Null"/>,
/// <see cref="Boolean"/>, <see cref="Object"/>, <see cref="Array"/>, <see cref="Float64"/>, <see cref="Utf8String"/>
/// and <see cref="Blob"/>; version 2 all of them but <see cref="Float8"/> and <see cref="Float128"/>, which are
/// reserved.
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

    /// <summary>Version 2: 1 byte, two's complement.</summary>
    Int8 = 0x40,

    /// <summary>Version 2: 2 bytes, two's complement, little-endian.</summary>
    Int16 = 0x41,

    /// <summary>Version 2: 4 bytes, two's complement, little-endian.</summary>
    Int32 = 0x42,

    /// <summary>Version 2: 8 bytes, two's complement, little-endian.</summary>
    Int64 = 0x43,

    /// <summary>Version 2: 16 bytes, two's complement, little-endian.</summary>
    Int128 = 0x44,

    /// <summary>Version 2: 1 byte, unsigned.</summary>
    UInt8 = 0x50,

    /// <summary>Version 2: 2 bytes, unsigned, little-endian.</summary>
    UInt16 = 0x51,

    /// <summary>Version 2: 4 bytes, unsigned, little-endian.</summary>
    UInt32 = 0x52,

    /// <summary>Version 2: 8 bytes, unsigned, little-endian.</summary>
    UInt64 = 0x53,

    /// <summary>Version 2: 16 bytes, unsigned, little-endian.</summary>
    UInt128 = 0x54,

    /// <summary>Reserved.</summary>
    Float8 = 0x60,

    /// <summary>Version 2: 2 bytes, IEEE 754 binary16, little-endian.</summary>
    Float16 = 0x61,

    /// <summary>Version 2: 4 bytes, IEEE 754 binary32, little-endian.</summary>
    Float32 = 0x62,

    /// <summary>8 bytes, IEEE 754 binary64, little-endian.</summary>
    Float64 = 0x63,

    /// <summary>Reserved.</summary>
    Float128 = 0x64,

    /// <summary>Version 2: the canonical text of a decimal number, UTF-8.</summary>
    Decimal = 0x70,

    /// <summary>Version 2: an integer of any size, two's complement, little-endian, in its shortest form.</summary>
    BigInt = 0x78,

    /// <summary>UTF-8 bytes.</summary>
    Utf8String = 0x80,

    /// <summary>Version 2: 16 bytes, in the order of RFC 9562.</summary>
    Uuid = 0x90,

    /// <summary>Version 2: 4 bytes, days since 1970-01-01.</summary>
    Date = 0xD0,

    /// <summary>Version 2: 8 bytes, ticks of 100 ns since midnight.</summary>
    Time = 0xD1,

    /// <summary>Version 2: 8 bytes, ticks of 100 ns since 1970-01-01T00:00, without an offset.</summary>
    DateTime = 0xD2,

    /// <summary>Version 2: 10 bytes, ticks of 100 ns since 1970-01-01T00:00Z, and the offset in minutes.</summary>
    Timestamp = 0xD3,

    /// <summary>Version 2: 8 bytes, ticks of 100 ns.</summary>
    Duration = 0xD4,

    /// <summary>Raw bytes.</summary>
    Blob = 0xF0,
}

/// <summary>
/// The type a property is stored as. Writing stores the property with this type code and rejects a value that does
/// not map to it; reading requires the file to have this type (or NULL) for the property.
/// </summary>
/// <remarks>
/// <para>
/// Experimental. <see cref="PromptuariumType.Utf8String"/> on a property whose type is not a text of its own (e.g. an
/// <c>IPAddress</c>, a <c>BigInteger</c>, an <c>int</c>) stores its text: written with
/// <see cref="IFormattable.ToString(string?, IFormatProvider?)"/> (invariant culture) or <see cref="object.ToString"/>,
/// read with <see cref="IParsable{TSelf}.TryParse(string?, IFormatProvider?, out TSelf)"/> (invariant culture); the
/// type must implement <see cref="IParsable{TSelf}"/>, and the text must give back an equal value. On a class or a
/// struct (only <see cref="PromptuariumType.Utf8String"/>), every value of the type is stored so, in collections and
/// dictionaries too; such a value can not be the root. <see cref="PromptuariumType.Blob"/> on a property of another
/// type than <c>byte[]</c> stores its bytes, if the type is registered with
/// <see cref="PromptuariumSerializerOptions.AddBlobType{T}"/>.
/// </para>
/// <para>
/// Otherwise it can only state the type the value maps to anyway (an <c>int</c> is a
/// <see cref="PromptuariumType.Float64"/>), and the reserved types are rejected. It is the place where e.g.
/// <see cref="PromptuariumType.Int64"/> instead of <see cref="PromptuariumType.Float64"/> will be chosen once the
/// format supports it.
/// </para>
/// </remarks>
/// <param name="type">The type code the property is stored as.</param>
[Experimental(DiagnosticId)]
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class PromptuariumTypeAttribute(PromptuariumType type) : Attribute
{
    /// <summary>The diagnostic that a use of the experimental type control reports.</summary>
    public const string DiagnosticId = "PRMTSER001";

    /// <summary>The type code the property is stored as.</summary>
    public PromptuariumType Type { get; } = type;

    /// <summary>Whether the format supports the type (it is not reserved).</summary>
    internal static bool IsSupported(PromptuariumType type) => CfgFormat.IsSupported((byte)type);
}
