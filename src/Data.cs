using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Promptuarium;

public static class Data
{
    #region Boolean Operations

    /// <summary>
    /// Creates a stream from a boolean value.
    /// </summary>
    /// <param name="value"></param>
    /// <returns>The stream</returns>
    /// <example>
    /// <code>
    /// node.Data = Data.FromBool(true);
    /// node.MetaData = Data.FromBool(false);
    /// </code>
    /// </example>
    public static Stream FromBool(bool value)
    {
        return new MemoryStream(BitConverter.GetBytes(value));
    }

    /// <summary>
    /// Converts a stream to a boolean value.
    /// </summary>
    /// <param name="stream">The source stream</param>
    /// <returns>The boolean value</returns>
    /// <example>
    /// <code>
    /// bool data = node.Data.AsBool();
    /// bool metadata = node.MetaData.AsBool();
    /// </code>
    /// </example>
    public static bool AsBool(this Stream stream)
    {
        return BitConverter.ToBoolean(GetBuffer(stream, sizeof(bool)), 0);
    }

    #endregion

    #region Byte Operations

    /// <summary>
    /// Creates a stream from a byte value.
    /// </summary>
    /// <param name="value">The byte value</param>
    /// <returns>The stream</returns>
    /// <example>
    /// <code>
    /// node.Data = Data.FromByte(0x12);
    /// node.MetaData = Data.FromByte(0x34);
    /// </code>
    /// </example>
    public static Stream FromByte(byte value)
    {
        var result = new MemoryStream();
        result.WriteByte(value);
        result.Position = 0;
        return result;
    }

    /// <summary>
    /// Converts a stream to a byte value.
    /// </summary>
    /// <param name="stream">The source stream</param>
    /// <returns>The byte value</returns>
    /// <example>
    /// <code>
    /// byte data = node.Data.AsByte();
    /// byte metadata = node.MetaData.AsByte();
    /// </code>
    /// </example>
    public static byte AsByte(this Stream stream)
    {
        byte result;
        stream.Position = 0;
        result = (byte)stream.ReadByte();
        stream.Position = 0;
        return result;
    }

    #endregion

    #region Byte Array Operations

    /// <summary>
    /// Creates a stream from a byte array.
    /// </summary>
    /// <param name="value">The byte array</param>
    /// <returns>The stream</returns>
    /// <example>
    /// <code>
    /// node.Data = Data.FromByteArray(new byte[] { 0x12, 0x34, 0x56, 0x78 });
    /// node.MetaData = Data.FromByteArray(new byte[] { 0x9A, 0xBC, 0xDE, 0xF0 });
    /// </code>
    /// </example>
    public static Stream FromByteArray(byte[] value)
    {
        return new MemoryStream(value);
    }

    /// <summary>
    /// Converts a stream to a byte array.
    /// </summary>
    /// <param name="stream">The source stream</param>
    /// <returns>The byte array</returns>
    /// <example>
    /// <code>
    /// byte[] data = node.Data.AsByteArray();
    /// </code>
    /// </example>
    public static byte[] AsByteArray(this Stream stream)
    {
        return GetBuffer(stream, (int)stream.Length);
    }

    #endregion

    #region Char Operations

    /// <summary>
    /// Creates a stream from a char value.
    /// </summary>
    /// <param name="value">The char value</param>
    /// <returns>The stream</returns>
    /// <example>
    /// <code>
    /// node.Data = Data.FromChar('A');
    /// node.MetaData = Data.FromChar('B');
    /// </code>
    /// </example>
    public static Stream FromChar(char value)
    {
        return new MemoryStream(BitConverter.GetBytes(value));
    }

    /// <summary>
    /// Converts a stream to a char value.
    /// </summary>
    /// <param name="stream">The source stream</param>
    /// <returns>The char value</returns>
    /// <example>
    /// <code>
    /// char metadata = node.MetaData.AsChar();
    /// </code>
    /// </example>
    public static char AsChar(this Stream stream)
    {
        return BitConverter.ToChar(GetBuffer(stream, sizeof(char)), 0);
    }

    #endregion

    #region Double Operations

    /// <summary>
    /// Creates a stream from a double value.
    /// </summary>
    /// <param name="value">The double value</param>
    /// <returns>The stream</returns>
    /// <example>
    /// <code>
    /// node.Data = Data.FromDouble(1.234);
    /// node.MetaData = Data.FromDouble(5.678);
    /// </code>
    /// </example>
    public static Stream FromDouble(double value)
    {
        return new MemoryStream(BitConverter.GetBytes(value));
    }

    /// <summary>
    /// Converts a stream to a double value.
    /// </summary>
    /// <param name="stream">The source stream</param>
    /// <returns>The double value</returns>
    /// <example>
    /// <code>
    /// double data = node.Data.AsDouble();
    /// </code>
    /// </example>
    public static double AsDouble(this Stream stream)
    {
        return BitConverter.ToDouble(GetBuffer(stream, sizeof(double)), 0);
    }

    #endregion

    #region Short Operations

    /// <summary>
    /// Creates a stream from a short value.
    /// </summary>
    /// <param name="value">The short value</param>
    /// <returns>The stream</returns>
    /// <example>
    /// <code>
    /// node.Data = Data.FromShort(0x1234);
    /// node.MetaData = Data.FromShort(0x5678);
    /// </code>
    /// </example>
    public static Stream FromShort(short value)
    {
        return new MemoryStream(BitConverter.GetBytes(value));
    }

    /// <summary>
    /// Converts a stream to a short value.
    /// </summary>
    /// <param name="stream">The source stream</param>
    /// <returns>The short value</returns>
    /// <example>
    /// <code>
    /// short data = node.Data.AsShort();
    /// </code>
    /// </example>
    public static short AsShort(this Stream stream)
    {
        return BitConverter.ToInt16(GetBuffer(stream, sizeof(short)), 0);
    }

    #endregion

    #region Int Operations

    /// <summary>
    /// Creates a stream from an int value.
    /// </summary>
    /// <param name="value">The int value</param>
    /// <returns>The stream</returns>
    /// <example>
    /// <code>
    /// node.Data = Data.FromInt(0x12345678);
    /// node.MetaData = Data.FromInt(0x9ABCDEF0);
    /// </code>
    /// </example>
    public static Stream FromInt(int value)
    {
        return new MemoryStream(BitConverter.GetBytes(value));
    }

    /// <summary>
    /// Converts a stream to an int value.
    /// </summary>
    /// <param name="stream">The source stream</param>
    /// <returns>The int value</returns>
    /// <example>
    /// <code>
    /// int data = node.Data.AsInt();
    /// </code>
    /// </example>
    public static int AsInt(this Stream stream)
    {
        return BitConverter.ToInt32(GetBuffer(stream, sizeof(int)), 0);
    }

    #endregion

    #region Long Operations

    /// <summary>
    /// Creates a stream from a long value.
    /// </summary>
    /// <param name="value">The long value</param>
    /// <returns>The stream</returns>
    /// <example>
    /// <code>
    /// node.Data = Data.FromLong(0x123456789ABCDEF0);
    /// node.MetaData = Data.FromLong(0x123456789ABCDEF0);
    /// </code>
    /// </example>
    public static Stream FromLong(long value)
    {
        return new MemoryStream(BitConverter.GetBytes(value));
    }

    /// <summary>
    /// Converts a stream to a long value.
    /// </summary>
    /// <param name="stream">The source stream</param>
    /// <returns>The long value</returns>
    /// <example>
    /// <code>
    /// long data = node.Data.AsLong();
    /// </code>
    /// </example>
    public static long AsLong(this Stream stream)
    {
        return BitConverter.ToInt64(GetBuffer(stream, sizeof(long)), 0);
    }

    #endregion

    #region Float Operations

    /// <summary>
    /// Creates a stream from a float value.
    /// </summary>
    /// <param name="value">The float value</param>
    /// <returns>The stream</returns>
    /// <example>
    /// <code>
    /// node.Data = Data.FromFloat(1.234f);
    /// node.MetaData = Data.FromFloat(5.678f);
    /// </code>
    /// </example>
    public static Stream FromFloat(float value)
    {
        return new MemoryStream(BitConverter.GetBytes(value));
    }

    /// <summary>
    /// Converts a stream to a float value.
    /// </summary>
    /// <param name="stream">The source stream</param>
    /// <returns>The float value</returns>
    /// <example>
    /// <code>
    /// float data = node.Data.AsFloat();
    /// </code>
    /// </example>
    public static float AsFloat(this Stream stream)
    {
        return BitConverter.ToSingle(GetBuffer(stream, sizeof(float)), 0);
    }

    #endregion

    #region UShort Operations

    /// <summary>
    /// Creates a stream from an ushort value.
    /// </summary>
    /// <param name="value">The ushort value</param>
    /// <returns>The stream</returns>
    /// <example>
    /// <code>
    /// node.Data = Data.FromUShort(0x1234);
    /// node.MetaData = Data.FromUShort(0x5678);
    /// </code>
    /// </example>
    public static Stream FromUShort(ushort value)
    {
        return new MemoryStream(BitConverter.GetBytes(value));
    }

    /// <summary>
    /// Converts a stream to an ushort value.
    /// </summary>
    /// <param name="stream">The source stream</param>
    /// <returns>The ushort value</returns>
    /// <example>
    /// <code>
    /// ushort data = node.Data.AsUShort();
    /// </code>
    /// </example>
    public static ushort AsUShort(this Stream stream)
    {
        return BitConverter.ToUInt16(GetBuffer(stream, sizeof(ushort)), 0);
    }

    #endregion

    #region UInt Operations

    /// <summary>
    /// Creates a stream from an uint value.
    /// </summary>
    /// <param name="value">The uint value</param>
    /// <returns>The stream</returns>
    /// <example>
    /// <code>
    /// node.Data = Data.FromUInt(0x12345678);
    /// node.MetaData = Data.FromUInt(0x9ABCDEF0);
    /// </code>
    /// </example>
    public static Stream FromUInt(uint value)
    {
        return new MemoryStream(BitConverter.GetBytes(value));
    }

    /// <summary>
    /// Converts a stream to an uint value.
    /// </summary>
    /// <param name="stream">The source stream</param>
    /// <returns>The uint value</returns>
    /// <example>
    /// <code>
    /// uint data = node.Data.AsUInt();
    /// </code>
    /// </example>
    public static uint AsUInt(this Stream stream)
    {
        return BitConverter.ToUInt32(GetBuffer(stream, sizeof(uint)), 0);
    }

    #endregion

    #region ULong Operations

    /// <summary>
    /// Creates a stream from an ulong value.
    /// </summary>
    /// <param name="value">The ulong value</param>
    /// <returns>The stream</returns>
    /// <example>
    /// <code>
    /// node.Data = Data.FromULong(0x123456789ABCDEF0);
    /// node.MetaData = Data.FromULong(0x123456789ABCDEF0);
    /// </code>
    /// </example>
    public static Stream FromULong(ulong value)
    {
        return new MemoryStream(BitConverter.GetBytes(value));
    }

    /// <summary>
    /// Converts a stream to an ulong value.
    /// </summary>
    /// <param name="stream">The source stream</param>
    /// <returns>The ulong value</returns>
    /// <example>
    /// <code>
    /// ulong data = node.Data.AsULong();
    /// </code>
    /// </example>
    public static ulong AsULong(this Stream stream)
    {
        return BitConverter.ToUInt64(GetBuffer(stream, sizeof(ulong)), 0);
    }

    #endregion

    #region Decimal Operations

    // Decimal: (value as a 96-bit integer) * 10^(-exponent)
    // Bytes of Decimal (4 × 30 bit unsigned integers in LE): L0:L1:L2:L3:M0:M1:M2:M3:H0:H1:H2:H3:F0:F1:F2:F2
    // Value: H3H2H1H0M3M2M1M0L3L2L1L0
    // Flags: F3F2F1F0
    // MSB = sign of the decimal number (F3.7)
    // Exponent is: the MSB of F3 and F2 (F3.0:F2)
    //     Example: 1.978 = 1978 * 10^(-3)
    //         value: BA:07:00:00:00:00:00:00:00:00:00:00
    //         exponent: 3
    //         flags: 00:00:03:00

    /// <summary>
    /// Creates a stream from an decimal value.
    /// </summary>
    /// <param name="value">The decimal value</param>
    /// <returns>The stream</returns>
    /// <example>
    /// <code>
    /// node.Data = Data.FromDecimal(1.234m);
    /// node.MetaData = Data.FromDecimal(5.678m);
    /// </code>
    /// </example>
    public static Stream FromDecimal(decimal value)
    {
        var stream = new MemoryStream();
        var binaryWriter = new BinaryWriter(stream);
        binaryWriter.Write(value);
        stream.Position = 0;
        return stream;
    }


    /// <summary>
    /// Converts a stream to a decimal value.
    /// </summary>
    /// <param name="stream">The source stream</param>
    /// <returns>The decimal value</returns>
    /// <example>
    /// <code>
    /// decimal data = node.Data.AsDecimal();
    /// </code>
    /// </example>
    public static decimal AsDecimal(this Stream stream)
    {
        decimal result;
        stream.Position = 0;
        var binaryReader = new BinaryReader(stream);
        result = binaryReader.ReadDecimal();
        stream.Position = 0;
        return result;
    }

    #endregion

    #region VarInt Operations

    /// <summary>
    /// Creates a stream from a long value.
    /// </summary>
    /// <param name="value">The long value</param>
    /// <returns>The stream</returns>
    /// <example>
    /// <code>
    /// node.Data = Data.FromVarInt(100);
    /// node.MetaData = Data.FromVarInt(200);
    /// </code>
    /// </example>
    public static Stream FromVarInt(long value)
    {
        var result = new MemoryStream();
        ulong buffer = (value >= 0) ? (ulong)(value << 1) : (((ulong)(-value) << 1) + 1);
        CompactBytes(result, buffer);
        return result;
    }

    /// <summary>
    /// Converts a stream to a long value.
    /// </summary>
    /// <param name="stream">The source stream</param>
    /// <returns>The long value</returns>
    /// <example>
    /// <code>
    /// long data = node.Data.AsVarInt();
    /// </code>
    /// </example>
    public static long AsVarInt(this Stream stream)
    {
        ulong buffer = BitConverter.ToUInt64(GetBuffer(stream, sizeof(ulong)), 0);
        return ((buffer & 1) == 0) ? (long)(buffer >> 1) : -(long)(buffer >> 1);
    }

    #endregion

    #region VarInt Operations

    /// <summary>
    /// Creates a stream from a ulong value.
    /// </summary>
    /// <param name="value">The ulong value</param>
    /// <returns>The stream</returns>
    /// <example>
    /// <code>
    /// node.Data = Data.FromVarUInt(100);
    /// node.MetaData = Data.FromVarUInt(200);
    /// </code>
    /// </example>
    public static Stream FromVarUInt(ulong value)
    {
        var result = new MemoryStream();
        CompactBytes(result, value);
        return result;
    }

    /// <summary>
    /// Converts a stream to a ulong value.
    /// </summary>
    /// <param name="stream">The source stream</param>
    /// <returns>The ulong value</returns>
    /// <example>
    /// <code>
    /// ulong data = node.Data.AsVarUInt();
    /// </code>
    /// </example>
    public static ulong AsVarUInt(this Stream stream)
    {
        return AsULong(stream);
    }

    #endregion

    #region GUID Operations

    /// <summary>
    /// Creates a stream from a GUID value.
    /// </summary>
    /// <param name="value">The GUID</param>
    /// <returns>The stream</returns>
    /// <example>
    /// <code>
    /// node.MetaData = Data.FromGuid(Guid.NewGuid());
    /// </code>
    /// </example>
    public static Stream FromGuid(Guid value)
    {
        return new MemoryStream(value.ToByteArray());
    }

    /// <summary>
    /// Converts a stream to a GUID value.
    /// </summary>
    /// <param name="stream">The source stream</param>
    /// <returns>The GUID value</returns>
    /// <example>
    /// <code>
    /// Guid metadata = node.MetaData.AsGuid();
    /// </code>
    /// </example>
    public static Guid AsGuid(this Stream stream)
    {
        return new Guid(GetBuffer(stream, (int)stream.Length));
    }

    #endregion

    #region Date and Time Operations

    /// <summary>
    /// Creates a stream from a DateTime value.
    /// </summary>
    /// <param name="value">The DateTime value</param>
    /// <returns>The stream</returns>
    /// <example>
    /// <code>
    /// node.Data = Data.FromDateTime(DateTime.Now);
    /// node.MetaData = Data.FromDateTime(DateTime.UtcNow);
    /// </code>
    /// </example>
    public static Stream FromDateTime(DateTime value)
    {
        return FromLong(value.Ticks);
    }

    /// <summary>
    /// Converts a stream to a DateTime value.
    /// </summary>
    /// <param name="stream">The source stream</param>
    /// <returns>The DateTime value</returns>
    /// <example>
    /// <code>
    /// DateTime data = node.Data.AsDateTime();
    /// </code>
    /// </example>
    public static DateTime AsDateTime(this Stream stream)
    {
        return new DateTime(stream.AsLong());
    }

    /// <summary>
    /// Creates a stream from a DateTimeOffset value.
    /// </summary>
    /// <param name="value">The DateTimeOffset value</param>
    /// <returns>The stream</returns>
    /// <example>
    /// <code>
    /// node.Data = Data.FromDateTimeOffset(DateTimeOffset.Now);
    /// node.MetaData = Data.FromDateTimeOffset(DateTimeOffset.UtcNow);
    /// </code>
    /// </example>
    public static Stream FromDateTimeOffset(DateTimeOffset value)
    {
        var result = new MemoryStream();
        result.Write(BitConverter.GetBytes(value.Ticks), 0, sizeof(long));
        result.Write(BitConverter.GetBytes(value.Offset.Ticks), 0, sizeof(long));
        result.Position = 0;
        return result;
    }

    /// <summary>
    /// Converts a stream to a DateTimeOffset value.
    /// </summary>
    /// <param name="stream">The source stream</param>
    /// <returns>The DateTimeOffset value</returns>
    /// <example>
    /// <code>
    /// DateTimeOffset data = node.Data.AsDateTimeOffset();
    /// </code>
    /// </example>
    public static DateTimeOffset AsDateTimeOffset(this Stream stream)
    {
        var bytes = GetBuffer(stream, 2 * sizeof(long));
        var datetimeBytes = new Span<byte>(bytes, 0, sizeof(long));
        var offsetBytes = new Span<byte>(bytes, sizeof(long), sizeof(long));

        return new DateTimeOffset(
            new DateTime(BitConverter.ToInt64(datetimeBytes)),
            new TimeSpan(BitConverter.ToInt64(offsetBytes))
        );
    }

    /// <summary>
    /// Creates a stream from a TimeSpan value.
    /// </summary>
    /// <param name="value">The TimeSpan value</param>
    /// <returns>The stream</returns>
    /// <example>
    /// <code>
    /// node.Data = Data.FromTimeSpan(TimeSpan.FromHours(1));
    /// node.MetaData = Data.FromTimeSpan(TimeSpan.FromMinutes(30));
    /// </code>
    /// </example>
    public static Stream FromTimeSpan(TimeSpan value)
    {
        return FromLong(value.Ticks);
    }

    /// <summary>
    /// Converts a stream to a TimeSpan value.
    /// </summary>
    /// <param name="stream">The source stream</param>
    /// <returns>The TimeSpan value</returns>
    /// <example>
    /// <code>
    /// TimeSpan data = node.Data.AsTimeSpan();
    /// </code>
    /// </example>
    public static TimeSpan AsTimeSpan(this Stream stream)
    {
        return new TimeSpan(stream.AsLong());
    }

    #endregion

    #region ASCII String Operations

    /// <summary>
    /// Creates a stream from a string using ASCII encoding.
    /// </summary>
    /// <param name="value">The string</param>
    /// <returns>The stream</returns>
    /// <example>
    /// <code>
    /// node.Data = Data.FromAsciiString("Hello");
    /// node.MetaData = Data.FromAsciiString("World");
    /// </code>
    /// </example>
    public static Stream FromAsciiString(string value)
    {
        return new MemoryStream(Encoding.ASCII.GetBytes(value));
    }

    /// <summary>
    /// Converts a stream to a string using ASCII encoding.
    /// </summary>
    /// <param name="stream">The source stream</param>
    /// <returns>The string</returns>
    /// <example>
    /// <code>
    /// string data = node.Data.AsAsciiString();
    /// </code>
    /// </example>
    public static string AsAsciiString(this Stream stream)
    {
        return Encoding.ASCII.GetString(GetBuffer(stream, (int)stream.Length));
    }

    #endregion

    #region UTF-8 String Operations

    /// <summary>
    /// Creates a stream from a string using UTF-8 encoding.
    /// </summary>
    /// <param name="value">The string</param>
    /// <returns>The stream</returns>
    /// <example>
    /// <code>
    /// node.Data = Data.FromUtf8String("Hello");
    /// node.MetaData = Data.FromUtf8String("World");
    /// </code>
    /// </example>
    public static Stream FromUtf8String(string value)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(value));
    }

    /// <summary>
    /// Converts a stream to a string using UTF-8 encoding.
    /// </summary>
    /// <param name="stream">The source stream</param>
    /// <returns>The string</returns>
    /// <example>
    /// <code>
    /// string data = node.Data.AsUtf8String();
    /// </code>
    /// </example>
    public static string AsUtf8String(this Stream stream)
    {
        return Encoding.UTF8.GetString(GetBuffer(stream, (int)stream.Length));
    }

    #endregion

    #region UTF-16 String Operations

    /// <summary>
    /// Creates a stream from a string using UTF-16 encoding.
    /// </summary>
    /// <param name="value">The string</param>
    /// <returns>The stream</returns>
    /// <example>
    /// <code>
    /// node.Data = Data.FromUtf16String("Hello");
    /// node.MetaData = Data.FromUtf16String("World");
    /// </code>
    /// </example>
    public static Stream FromUtf16String(string value)
    {
        return new MemoryStream(Encoding.Unicode.GetBytes(value));
    }

    /// <summary>
    /// Converts a stream to a string using UTF-16 encoding.
    /// </summary>
    /// <param name="stream">The source stream</param>
    /// <returns>The string</returns>
    /// <example>
    /// <code>
    /// string data = node.Data.AsUtf16String();
    /// </code>
    /// </example>
    public static string AsUtf16String(this Stream stream)
    {
        return Encoding.Unicode.GetString(GetBuffer(stream, (int)stream.Length));
    }

    #endregion

    #region UTF-32 String Operations

    /// <summary>
    /// Creates a stream from a string using UTF-32 encoding.
    /// </summary>
    /// <param name="value">The string</param>
    /// <returns>The stream</returns>
    /// <example>
    /// <code>
    /// node.Data = Data.FromUtf32String("Hello");
    /// node.MetaData = Data.FromUtf32String("World");
    /// </code>
    /// </example>
    public static Stream FromUtf32String(string value)
    {
        return new MemoryStream(Encoding.UTF32.GetBytes(value));
    }

    /// <summary>
    /// Converts a stream to a string using UTF-32 encoding.
    /// </summary>
    /// <param name="stream">The source stream</param>
    /// <returns>The string</returns>
    /// <example>
    /// <code>
    /// string data = node.Data.AsUtf32String();
    /// </code>
    /// </example>
    public static string AsUtf32String(this Stream stream)
    {
        return Encoding.UTF32.GetString(GetBuffer(stream, (int)stream.Length));
    }

    #endregion

    #region Tree Operation

    /// <summary>
    /// Creates a stream from a tree.
    /// </summary>
    /// <param name="value">The tree</param>
    /// <returns>The stream</returns>
    /// <example>
    /// <code>
    /// var tree = new Element();
    /// tree.Data = Data.FromInt(123);
    /// tree.MetaData = Data.FromUtf8String("Hello");
    /// 
    /// tree += new Element();
    /// 
    /// var stream = Data.FromTree(tree);
    /// </code>
    /// </example>
    public static async Task<Stream> FromElementAsync(Element value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var stream = new MemoryStream();
        await value.SaveAsync(stream).ConfigureAwait(false);
        return stream;
    }

    /// <summary>
    /// Converts a stream to a tree.
    /// </summary>
    /// <param name="stream">The source stream</param>
    /// <returns>The tree</returns>
    /// <example>
    /// <code>
    /// var tree = await Data.AsTreeAsync(stream);
    /// </code>
    /// </example>
    public static async Task<Element> AsElementAsync(this Stream stream, Element loaderElement = default)
    {
        loaderElement ??= new Element();
        Element result;
        stream.Position = 0;
        result = await loaderElement.LoadAsync(stream).ConfigureAwait(false);
        stream.Position = 0;
        return result;
    }
    #endregion

    #region Private Methods
    private static byte[] GetBuffer(Stream stream, int size)
    {
        byte[] buffer = new byte[size];
        stream.Position = 0;
        stream.Read(buffer, 0, size);
        stream.Position = 0;
        return buffer;
    }

    private static void CompactBytes(MemoryStream result, ulong buffer)
    {
        for (int x = 0; x < sizeof(ulong); x++)
        {
            byte lsb = (byte)(buffer & 0xFF);
            buffer >>= sizeof(byte) * 8;

            if (x == 0 || lsb != 0 || (lsb == 0 && buffer != 0))
            {
                result.WriteByte(lsb);
            }
        }

        result.Position = 0;
    }
    #endregion
}
