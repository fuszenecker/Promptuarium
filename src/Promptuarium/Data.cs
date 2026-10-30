using System;
using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace Promptuarium;

/// <summary>
/// Conversions between values and contents (data or metadata).
/// </summary>
/// <remarks>
/// <para>The container does not store types: a content is a plain byte sequence, and the reader has to know how it was written.
/// All numbers are little-endian, independently of the platform.</para>
/// <para>The fixed-size conversions (e.g. <see cref="AsInt"/>) read the first bytes of the content:
/// a shorter content is padded with zero bytes, and the bytes after the value are ignored.</para>
/// </remarks>
/// <example>
/// <code>
/// var node = new Element(Data.FromInt(42), Data.FromUtf8String("answer"));
/// int value = node.Data.AsInt();
/// </code>
/// </example>
public static class Data
{
    // VarInt encoding of long.MinValue
    private const ulong NegativeZero = 1;

    #region Boolean Operations

    /// <summary>Converts a boolean value to a content: 01 for true, 00 for false.</summary>
    public static ReadOnlyMemory<byte> FromBool(bool value) => new[] { value ? (byte)1 : (byte)0 };

    /// <summary>Converts a content to a boolean value: true if the first byte is not zero.</summary>
    public static bool AsBool(this ReadOnlyMemory<byte> content) => Fixed(content, sizeof(bool))[0] != 0;

    #endregion

    #region Byte Operations

    /// <summary>Converts a byte to a content.</summary>
    public static ReadOnlyMemory<byte> FromByte(byte value) => new[] { value };

    /// <summary>Converts a content to a byte (0 for an empty content).</summary>
    public static byte AsByte(this ReadOnlyMemory<byte> content) => Fixed(content, sizeof(byte))[0];

    #endregion

    #region Byte Array Operations

    /// <summary>Converts bytes to a content. The bytes are copied, so later changes of the array do not change the content.</summary>
    public static ReadOnlyMemory<byte> FromByteArray(byte[] value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return (byte[])value.Clone();
    }

    /// <summary>Converts bytes to a content. The bytes are copied.</summary>
    public static ReadOnlyMemory<byte> FromBytes(ReadOnlySpan<byte> value) => value.ToArray();

    /// <summary>Copies a content into a new array.</summary>
    public static byte[] AsByteArray(this ReadOnlyMemory<byte> content) => content.ToArray();

    #endregion

    #region Char Operations

    /// <summary>Converts a character to a content (a UTF-16 code unit, 2 bytes).</summary>
    public static ReadOnlyMemory<byte> FromChar(char value) => FromUShort(value);

    /// <summary>Converts a content to a character (a UTF-16 code unit).</summary>
    public static char AsChar(this ReadOnlyMemory<byte> content) => (char)content.AsUShort();

    #endregion

    #region Integer Operations

    /// <summary>Converts a 16-bit integer to a content (2 bytes).</summary>
    public static ReadOnlyMemory<byte> FromShort(short value) => Write(sizeof(short), bytes => BinaryPrimitives.WriteInt16LittleEndian(bytes, value));

    /// <summary>Converts a content to a 16-bit integer.</summary>
    public static short AsShort(this ReadOnlyMemory<byte> content) => BinaryPrimitives.ReadInt16LittleEndian(Fixed(content, sizeof(short)));

    /// <summary>Converts an unsigned 16-bit integer to a content (2 bytes).</summary>
    public static ReadOnlyMemory<byte> FromUShort(ushort value) => Write(sizeof(ushort), bytes => BinaryPrimitives.WriteUInt16LittleEndian(bytes, value));

    /// <summary>Converts a content to an unsigned 16-bit integer.</summary>
    public static ushort AsUShort(this ReadOnlyMemory<byte> content) => BinaryPrimitives.ReadUInt16LittleEndian(Fixed(content, sizeof(ushort)));

    /// <summary>Converts a 32-bit integer to a content (4 bytes).</summary>
    public static ReadOnlyMemory<byte> FromInt(int value) => Write(sizeof(int), bytes => BinaryPrimitives.WriteInt32LittleEndian(bytes, value));

    /// <summary>Converts a content to a 32-bit integer.</summary>
    public static int AsInt(this ReadOnlyMemory<byte> content) => BinaryPrimitives.ReadInt32LittleEndian(Fixed(content, sizeof(int)));

    /// <summary>Converts an unsigned 32-bit integer to a content (4 bytes).</summary>
    public static ReadOnlyMemory<byte> FromUInt(uint value) => Write(sizeof(uint), bytes => BinaryPrimitives.WriteUInt32LittleEndian(bytes, value));

    /// <summary>Converts a content to an unsigned 32-bit integer.</summary>
    public static uint AsUInt(this ReadOnlyMemory<byte> content) => BinaryPrimitives.ReadUInt32LittleEndian(Fixed(content, sizeof(uint)));

    /// <summary>Converts a 64-bit integer to a content (8 bytes).</summary>
    public static ReadOnlyMemory<byte> FromLong(long value) => Write(sizeof(long), bytes => BinaryPrimitives.WriteInt64LittleEndian(bytes, value));

    /// <summary>Converts a content to a 64-bit integer.</summary>
    public static long AsLong(this ReadOnlyMemory<byte> content) => BinaryPrimitives.ReadInt64LittleEndian(Fixed(content, sizeof(long)));

    /// <summary>Converts an unsigned 64-bit integer to a content (8 bytes).</summary>
    public static ReadOnlyMemory<byte> FromULong(ulong value) => Write(sizeof(ulong), bytes => BinaryPrimitives.WriteUInt64LittleEndian(bytes, value));

    /// <summary>Converts a content to an unsigned 64-bit integer.</summary>
    public static ulong AsULong(this ReadOnlyMemory<byte> content) => BinaryPrimitives.ReadUInt64LittleEndian(Fixed(content, sizeof(ulong)));

    #endregion

    #region Floating Point Operations

    /// <summary>Converts a single precision number to a content (IEEE 754 binary32, 4 bytes).</summary>
    public static ReadOnlyMemory<byte> FromFloat(float value) => Write(sizeof(float), bytes => BinaryPrimitives.WriteSingleLittleEndian(bytes, value));

    /// <summary>Converts a content to a single precision number.</summary>
    public static float AsFloat(this ReadOnlyMemory<byte> content) => BinaryPrimitives.ReadSingleLittleEndian(Fixed(content, sizeof(float)));

    /// <summary>Converts a double precision number to a content (IEEE 754 binary64, 8 bytes).</summary>
    public static ReadOnlyMemory<byte> FromDouble(double value) => Write(sizeof(double), bytes => BinaryPrimitives.WriteDoubleLittleEndian(bytes, value));

    /// <summary>Converts a content to a double precision number.</summary>
    public static double AsDouble(this ReadOnlyMemory<byte> content) => BinaryPrimitives.ReadDoubleLittleEndian(Fixed(content, sizeof(double)));

    #endregion

    #region Decimal Operations

    /// <summary>
    /// Converts a decimal number to a content (16 bytes: lo, mid, hi and flags as 32-bit integers, like BinaryWriter.Write(decimal)).
    /// </summary>
    public static ReadOnlyMemory<byte> FromDecimal(decimal value)
    {
        Span<int> bits = stackalloc int[4];
        decimal.GetBits(value, bits);
        byte[] bytes = new byte[sizeof(decimal)];

        for (int i = 0; i < bits.Length; i++)
        {
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(i * sizeof(int)), bits[i]);
        }

        return bytes;
    }

    /// <summary>Converts a content to a decimal number.</summary>
    public static decimal AsDecimal(this ReadOnlyMemory<byte> content)
    {
        ReadOnlySpan<byte> bytes = Fixed(content, sizeof(decimal));
        Span<int> bits = stackalloc int[4];

        for (int i = 0; i < bits.Length; i++)
        {
            bits[i] = BinaryPrimitives.ReadInt32LittleEndian(bytes[(i * sizeof(int))..]);
        }

        return new decimal(bits);
    }

    #endregion

    #region VarInt Operations

    /// <summary>
    /// Converts a signed integer to a content of 1 to 8 bytes: the magnitude shifted left by one bit, the lowest bit is the sign
    /// (long.MinValue is "negative zero"), then encoded as <see cref="FromVarUInt"/>.
    /// </summary>
    public static ReadOnlyMemory<byte> FromVarInt(long value)
    {
        ulong buffer = value switch
        {
            long.MinValue => NegativeZero,
            >= 0 => (ulong)value << 1,
            _ => ((ulong)(-value) << 1) + 1
        };

        return FromVarUInt(buffer);
    }

    /// <summary>Converts a content of at most 8 bytes to a signed integer, see <see cref="FromVarInt"/>.</summary>
    /// <exception cref="PromptuariumException">Thrown if the content is longer than 8 bytes</exception>
    public static long AsVarInt(this ReadOnlyMemory<byte> content)
    {
        ulong buffer = content.AsVarUInt();

        if (buffer == NegativeZero)
        {
            return long.MinValue;
        }

        return ((buffer & 1) == 0) ? (long)(buffer >> 1) : -(long)(buffer >> 1);
    }

    /// <summary>Converts an unsigned integer to a content of 1 to 8 bytes: little-endian, without the trailing zero bytes.</summary>
    public static ReadOnlyMemory<byte> FromVarUInt(ulong value)
    {
        int length = Math.Max(1, sizeof(ulong) - (System.Numerics.BitOperations.LeadingZeroCount(value) / 8));
        return Write(sizeof(ulong), bytes => BinaryPrimitives.WriteUInt64LittleEndian(bytes, value)).AsMemory(0, length);
    }

    /// <summary>Converts a content of at most 8 bytes to an unsigned integer, see <see cref="FromVarUInt"/>.</summary>
    /// <exception cref="PromptuariumException">Thrown if the content is longer than 8 bytes</exception>
    public static ulong AsVarUInt(this ReadOnlyMemory<byte> content)
    {
        if (content.Length > sizeof(ulong))
        {
            throw new PromptuariumException($"A VarInt or VarUInt is at most {sizeof(ulong).ToString(CultureInfo.InvariantCulture)} bytes long, the content is {content.Length.ToString(CultureInfo.InvariantCulture)} bytes.");
        }

        return content.AsULong();
    }

    #endregion

    #region GUID Operations

    /// <summary>Converts a GUID to a content (16 bytes, the layout of Guid.ToByteArray).</summary>
    public static ReadOnlyMemory<byte> FromGuid(Guid value) => value.ToByteArray();

    /// <summary>Converts a content of exactly 16 bytes to a GUID.</summary>
    public static Guid AsGuid(this ReadOnlyMemory<byte> content) => new(content.Span);

    #endregion

    #region Date and Time Operations

    /// <summary>Converts a date and time to a content: its ticks (8 bytes). The kind (UTC, local) is not stored.</summary>
    public static ReadOnlyMemory<byte> FromDateTime(DateTime value) => FromLong(value.Ticks);

    /// <summary>Converts a content to a date and time.</summary>
    public static DateTime AsDateTime(this ReadOnlyMemory<byte> content) => new(content.AsLong());

    /// <summary>Converts a date and time with offset to a content: the ticks of the clock time, then the ticks of the offset (16 bytes).</summary>
    public static ReadOnlyMemory<byte> FromDateTimeOffset(DateTimeOffset value)
    {
        return Write(2 * sizeof(long), bytes =>
        {
            BinaryPrimitives.WriteInt64LittleEndian(bytes, value.Ticks);
            BinaryPrimitives.WriteInt64LittleEndian(bytes[sizeof(long)..], value.Offset.Ticks);
        });
    }

    /// <summary>Converts a content to a date and time with offset.</summary>
    public static DateTimeOffset AsDateTimeOffset(this ReadOnlyMemory<byte> content)
    {
        ReadOnlySpan<byte> bytes = Fixed(content, 2 * sizeof(long));

        return new DateTimeOffset(
            new DateTime(BinaryPrimitives.ReadInt64LittleEndian(bytes)),
            new TimeSpan(BinaryPrimitives.ReadInt64LittleEndian(bytes[sizeof(long)..])));
    }

    /// <summary>Converts a time span to a content: its ticks (8 bytes).</summary>
    public static ReadOnlyMemory<byte> FromTimeSpan(TimeSpan value) => FromLong(value.Ticks);

    /// <summary>Converts a content to a time span.</summary>
    public static TimeSpan AsTimeSpan(this ReadOnlyMemory<byte> content) => new(content.AsLong());

    #endregion

    #region String Operations

    /// <summary>Converts a string to ASCII bytes; characters above U+007F become '?'.</summary>
    public static ReadOnlyMemory<byte> FromAsciiString(string value) => Encoding.ASCII.GetBytes(value);

    /// <summary>Converts ASCII bytes to a string.</summary>
    public static string AsAsciiString(this ReadOnlyMemory<byte> content) => Encoding.ASCII.GetString(content.Span);

    /// <summary>Converts a string to UTF-8 bytes (without byte order mark).</summary>
    public static ReadOnlyMemory<byte> FromUtf8String(string value) => Encoding.UTF8.GetBytes(value);

    /// <summary>Converts UTF-8 bytes to a string.</summary>
    public static string AsUtf8String(this ReadOnlyMemory<byte> content) => Encoding.UTF8.GetString(content.Span);

    /// <summary>Converts a string to UTF-16 little-endian bytes (without byte order mark).</summary>
    public static ReadOnlyMemory<byte> FromUtf16String(string value) => Encoding.Unicode.GetBytes(value);

    /// <summary>Converts UTF-16 little-endian bytes to a string.</summary>
    public static string AsUtf16String(this ReadOnlyMemory<byte> content) => Encoding.Unicode.GetString(content.Span);

    /// <summary>Converts a string to UTF-32 little-endian bytes (without byte order mark).</summary>
    public static ReadOnlyMemory<byte> FromUtf32String(string value) => Encoding.UTF32.GetBytes(value);

    /// <summary>Converts UTF-32 little-endian bytes to a string.</summary>
    public static string AsUtf32String(this ReadOnlyMemory<byte> content) => Encoding.UTF32.GetString(content.Span);

    #endregion

    #region Tree Operations

    /// <summary>Converts a tree to a content: a complete nested Promptuarium stream.</summary>
    public static ReadOnlyMemory<byte> FromElement(Element value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.ToBytes();
    }

    /// <summary>Converts a content (a nested Promptuarium stream) to a tree.</summary>
    /// <exception cref="PromptuariumException">Thrown if the content is not a valid Promptuarium stream</exception>
    public static Element AsElement(this ReadOnlyMemory<byte> content) => Element.FromBytes(content);

    /// <summary>Converts a tree to a content, see <see cref="FromElement"/>.</summary>
    public static Task<ReadOnlyMemory<byte>> FromElementAsync(Element value) => Task.FromResult(FromElement(value));

    /// <summary>Converts a content to a tree, see <see cref="AsElement"/>.</summary>
    public static Task<Element> AsElementAsync(this ReadOnlyMemory<byte> content) => Task.FromResult(content.AsElement());

    #endregion

    #region Private Methods

    private delegate void SpanWriter(Span<byte> bytes);

    private static byte[] Write(int size, SpanWriter writer)
    {
        byte[] bytes = new byte[size];
        writer(bytes);
        return bytes;
    }

    /// <summary>
    /// The first <paramref name="size"/> bytes of the content; a shorter content is padded with zero bytes.
    /// </summary>
    private static ReadOnlySpan<byte> Fixed(ReadOnlyMemory<byte> content, int size)
    {
        if (content.Length >= size)
        {
            return content.Span[..size];
        }

        byte[] padded = new byte[size];
        content.Span.CopyTo(padded);
        return padded;
    }

    #endregion
}
