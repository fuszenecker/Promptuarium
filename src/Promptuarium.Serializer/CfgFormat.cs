namespace Promptuarium.Serializer;

/// <summary>The metadata tags and the type codes of SERIALIZER_FORMAT.md (sections 3 and 4).</summary>
internal static class CfgFormat
{
    // Metadata tags: the whole metadata of every node except the root
    public const byte Name = 0x00;
    public const byte Type = 0x01;
    public const byte Item = 0x02;
    public const byte Value = 0x03;

    // Type codes: the data of a TYPE node
    public const byte Null = 0x00;
    public const byte Boolean = 0x01;
    public const byte Object = 0x20;
    public const byte Array = 0x21;
    public const byte Float64 = 0x63;
    public const byte Utf8String = 0x80;
    public const byte Blob = 0xF0;
}
