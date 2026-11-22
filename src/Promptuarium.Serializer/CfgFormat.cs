namespace Promptuarium.Serializer;

/// <summary>The metadata tags and the type codes of SERIALIZER_FORMAT.md (sections 3 and 4).</summary>
internal static class CfgFormat
{
    // Metadata tags: the whole metadata of every node except the root
    public const byte Name = 0x00;
    public const byte Type = 0x01;
    public const byte Item = 0x02;
    public const byte Value = 0x03;
    public const byte Comment = 0x04;

    // Type codes: the data of a TYPE node (version 1)
    public const byte Null = 0x00;
    public const byte Boolean = 0x01;
    public const byte Object = 0x20;
    public const byte Array = 0x21;
    public const byte Float64 = 0x63;
    public const byte Utf8String = 0x80;
    public const byte Blob = 0xF0;

    // Type codes of version 2
    public const byte Int8 = 0x40;
    public const byte Int16 = 0x41;
    public const byte Int32 = 0x42;
    public const byte Int64 = 0x43;
    public const byte Int128 = 0x44;
    public const byte UInt8 = 0x50;
    public const byte UInt16 = 0x51;
    public const byte UInt32 = 0x52;
    public const byte UInt64 = 0x53;
    public const byte UInt128 = 0x54;
    public const byte Float16 = 0x61;
    public const byte Float32 = 0x62;
    public const byte Decimal = 0x70;
    public const byte BigInt = 0x78;
    public const byte Uuid = 0x90;
    public const byte Date = 0xD0;
    public const byte Time = 0xD1;
    public const byte DateTime = 0xD2;
    public const byte Timestamp = 0xD3;
    public const byte Duration = 0xD4;
    public const byte Container = 0xF1;

    /// <summary>Whether <paramref name="type"/> is a type code of version 1.</summary>
    public static bool IsVersion1(byte type) => type is Null or Boolean or Object or Array or Float64 or Utf8String or Blob;

    /// <summary>Whether <paramref name="type"/> is a type code of version 2 (the ones of version 1 included).</summary>
    public static bool IsSupported(byte type) => IsVersion1(type) || IsInteger(type) || type is Float16 or Float32
        or Decimal or Uuid or Date or Time or DateTime or Timestamp or Duration or Container;

    /// <summary>Whether <paramref name="type"/> is an integer type: INT8 ... INT128, UINT8 ... UINT128, BIGINT.</summary>
    public static bool IsInteger(byte type) => type is (>= Int8 and <= Int128) or (>= UInt8 and <= UInt128) or BigInt;

    /// <summary>Whether <paramref name="type"/> is a binary floating point type: FLOAT16, FLOAT32, FLOAT64.</summary>
    public static bool IsFloat(byte type) => type is Float16 or Float32 or Float64;
}
