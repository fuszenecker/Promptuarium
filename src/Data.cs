using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Promptuarium
{
    public static class Data
    {
        #region Boolean Operations
        public static Stream FromBool(bool value)
        {
            return new MemoryStream(BitConverter.GetBytes(value));
        }

        public static bool AsBool(this Stream stream)
        {
            return BitConverter.ToBoolean(GetBuffer(stream, sizeof(bool)), 0);
        }
        #endregion

        #region Byte Operations
        public static Stream FromByte(byte value)
        {
            var result = new MemoryStream();
            result.WriteByte(value);
            result.Position = 0;
            return result;
        }

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
        public static Stream FromByteArray(byte[] value)
        {
            return new MemoryStream(value);
        }

        public static byte[] AsByteArray(this Stream stream)
        {
            return GetBuffer(stream, (int)stream.Length);
        }
        #endregion

        #region Char Operations
        public static Stream FromChar(char value)
        {
            return new MemoryStream(BitConverter.GetBytes(value));
        }

        public static char AsChar(this Stream stream)
        {
            return BitConverter.ToChar(GetBuffer(stream, sizeof(char)), 0);
        }
        #endregion

        #region Double Operations
        public static Stream FromDouble(double value)
        {
            return new MemoryStream(BitConverter.GetBytes(value));
        }

        public static double AsDouble(this Stream stream)
        {
            return BitConverter.ToDouble(GetBuffer(stream, sizeof(double)), 0);
        }
        #endregion

        #region Short Operations
        public static Stream FromShort(short value)
        {
            return new MemoryStream(BitConverter.GetBytes(value));
        }

        public static short AsShort(this Stream stream)
        {
            return BitConverter.ToInt16(GetBuffer(stream, sizeof(short)), 0);
        }
        #endregion

        #region Int Operations
        public static Stream FromInt(int value)
        {
            return new MemoryStream(BitConverter.GetBytes(value));
        }

        public static int AsInt(this Stream stream)
        {
            return BitConverter.ToInt32(GetBuffer(stream, sizeof(int)), 0);
        }
        #endregion

        #region Long Operations
        public static Stream FromLong(long value)
        {
            return new MemoryStream(BitConverter.GetBytes(value));
        }

        public static long AsLong(this Stream stream)
        {
            return BitConverter.ToInt64(GetBuffer(stream, sizeof(long)), 0);
        }
        #endregion

        #region Float Operations
        public static Stream FromFloat(float value)
        {
            return new MemoryStream(BitConverter.GetBytes(value));
        }

        public static float AsFloat(this Stream stream)
        {
            return BitConverter.ToSingle(GetBuffer(stream, sizeof(float)), 0);
        }
        #endregion

        #region UShort Operations
        public static Stream FromUShort(ushort value)
        {
            return new MemoryStream(BitConverter.GetBytes(value));
        }

        public static ushort AsUShort(this Stream stream)
        {
            return BitConverter.ToUInt16(GetBuffer(stream, sizeof(ushort)), 0);
        }
        #endregion

        #region UInt Operations
        public static Stream FromUInt(uint value)
        {
            return new MemoryStream(BitConverter.GetBytes(value));
        }

        public static uint AsUInt(this Stream stream)
        {
            return BitConverter.ToUInt32(GetBuffer(stream, sizeof(uint)), 0);
        }
        #endregion

        #region ULong Operations
        public static Stream FromULong(ulong value)
        {
            return new MemoryStream(BitConverter.GetBytes(value));
        }

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

        public static Stream FromDecimal(decimal value)
        {
            var stream = new MemoryStream();
            var binaryWriter = new BinaryWriter(stream);
            binaryWriter.Write(value);
            stream.Position = 0;
            return stream;
        }

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
        public static Stream FromVarInt(long value)
        {
            var result = new MemoryStream();
            ulong buffer = (value >= 0) ? (ulong)(value << 1) : (((ulong)(-value) << 1) + 1);
            CompactBytes(result, buffer);
            return result;
        }

        public static long AsVarInt(this Stream stream)
        {
            ulong buffer = BitConverter.ToUInt64(GetBuffer(stream, sizeof(ulong)), 0);
            return ((buffer & 1) == 0) ? (long)(buffer >> 1) : -(long)(buffer >> 1);
        }
        #endregion

        #region VarInt Operations
        public static Stream FromVarUInt(ulong value)
        {
            var result = new MemoryStream();
            CompactBytes(result, value);
            return result;
        }

        public static ulong AsVarUInt(this Stream stream)
        {
            return AsULong(stream);
        }
        #endregion

        #region GUID Operations
        public static Stream FromGuid(Guid value)
        {
            return new MemoryStream(value.ToByteArray());
        }

        public static Guid AsGuid(this Stream stream)
        {
            return new Guid(GetBuffer(stream, (int)stream.Length));
        }
        #endregion

        #region Date and Time Operations
        public static Stream FromDateTime(DateTime value)
        {
            return FromLong(value.Ticks);
        }

        public static DateTime AsDateTime(this Stream stream)
        {
            return new DateTime(stream.AsLong());
        }

        public static Stream FromDateTimeOffset(DateTimeOffset value)
        {
            var result = new MemoryStream();
            result.Write(BitConverter.GetBytes(value.Ticks), 0, sizeof(long));
            result.Write(BitConverter.GetBytes(value.Offset.Ticks), 0, sizeof(long));
            result.Position = 0;
            return result;
        }

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

        public static Stream FromTimeSpan(TimeSpan value)
        {
            return FromLong(value.Ticks);
        }

        public static TimeSpan AsTimeSpan(this Stream stream)
        {
            return new TimeSpan(stream.AsLong());
        }
        #endregion

        #region ASCII String Operations
        public static Stream FromAsciiString(string value)
        {
            return new MemoryStream(Encoding.ASCII.GetBytes(value));
        }

        public static string AsAsciiString(this Stream stream)
        {
            return Encoding.ASCII.GetString(GetBuffer(stream, (int)stream.Length));
        }
        #endregion

        #region UTF-8 String Operations
        public static Stream FromUtf8String(string value)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(value));
        }

        public static string AsUtf8String(this Stream stream)
        {
            return Encoding.UTF8.GetString(GetBuffer(stream, (int)stream.Length));
        }
        #endregion

        #region UTF-16 String Operations
        public static Stream FromUtf16String(string value)
        {
            return new MemoryStream(Encoding.Unicode.GetBytes(value));
        }

        public static string AsUtf16String(this Stream stream)
        {
            return Encoding.Unicode.GetString(GetBuffer(stream, (int)stream.Length));
        }
        #endregion

        #region UTF-32 String Operations
        public static Stream FromUtf32String(string value)
        {
            return new MemoryStream(Encoding.UTF32.GetBytes(value));
        }

        public static string AsUtf32String(this Stream stream)
        {
            return Encoding.UTF32.GetString(GetBuffer(stream, (int)stream.Length));
        }
        #endregion

        #region Tree Operation
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

        public static async Task<Element> AsElementAsync(this Stream stream)
        {
            Element result;
            stream.Position = 0;
            result = await Element.LoadAsync(stream).ConfigureAwait(false);
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
}
