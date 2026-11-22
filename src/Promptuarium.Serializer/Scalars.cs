using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Promptuarium.Serializer;

/// <summary>
/// The scalar values of SERIALIZER_FORMAT.md: the natural type of a .NET value (version 2), the form a
/// <see cref="PromptuariumTypeAttribute"/> declares, and the values read back into a .NET type, exactly or not at all.
/// </summary>
internal static partial class Scalars
{
    private static readonly long EpochTicks = System.DateTime.UnixEpoch.Ticks;
    private static readonly int EpochDayNumber = DateOnly.FromDateTime(System.DateTime.UnixEpoch).DayNumber;
    private static readonly ConcurrentDictionary<Type, byte?> EnumForms = new();

    // ---------------------------------------------------------------- writing

    /// <summary>The natural type and the VALUE node of a scalar (version 2), or false if it is not a scalar.</summary>
    public static bool TryEncode(object value, out byte type, out ReadOnlyMemory<byte> content)
    {
        (type, content) = value switch
        {
            bool flag => (CfgFormat.Boolean, Data.FromBool(flag)),
            string text => (CfgFormat.Utf8String, Data.FromUtf8String(text)),
            byte[] blob => (CfgFormat.Blob, blob),
            Element tree => (CfgFormat.Container, tree.ToBytes()),
            char character => (CfgFormat.Utf8String, Data.FromUtf8String(character.ToString())),
            Enum name => (CfgFormat.Utf8String, Data.FromUtf8String(name.ToString())),
            sbyte number => (CfgFormat.Int8, new[] { (byte)number }),
            short number => (CfgFormat.Int16, Write(2, span => BinaryPrimitives.WriteInt16LittleEndian(span, number))),
            int number => (CfgFormat.Int32, Write(4, span => BinaryPrimitives.WriteInt32LittleEndian(span, number))),
            long number => (CfgFormat.Int64, Write(8, span => BinaryPrimitives.WriteInt64LittleEndian(span, number))),
            nint number => (CfgFormat.Int64, Write(8, span => BinaryPrimitives.WriteInt64LittleEndian(span, number))),
            Int128 number => (CfgFormat.Int128, Write(16, span => BinaryPrimitives.WriteInt128LittleEndian(span, number))),
            byte number => (CfgFormat.UInt8, new[] { number }),
            ushort number => (CfgFormat.UInt16, Write(2, span => BinaryPrimitives.WriteUInt16LittleEndian(span, number))),
            uint number => (CfgFormat.UInt32, Write(4, span => BinaryPrimitives.WriteUInt32LittleEndian(span, number))),
            ulong number => (CfgFormat.UInt64, Write(8, span => BinaryPrimitives.WriteUInt64LittleEndian(span, number))),
            nuint number => (CfgFormat.UInt64, Write(8, span => BinaryPrimitives.WriteUInt64LittleEndian(span, number))),
            UInt128 number => (CfgFormat.UInt128, Write(16, span => BinaryPrimitives.WriteUInt128LittleEndian(span, number))),
            BigInteger number => (CfgFormat.BigInt, number.ToByteArray()),
            Half number => (CfgFormat.Float16, EncodeFloat(CfgFormat.Float16, (double)number)),
            float number => (CfgFormat.Float32, EncodeFloat(CfgFormat.Float32, number)),
            double number => (CfgFormat.Float64, EncodeFloat(CfgFormat.Float64, number)),
            decimal number => (CfgFormat.Decimal, Data.FromUtf8String(number.ToString(CultureInfo.InvariantCulture))),
            Guid guid => (CfgFormat.Uuid, EncodeUuid(guid)),
            DateOnly date => (CfgFormat.Date, Write(4, span => BinaryPrimitives.WriteInt32LittleEndian(span, date.DayNumber - EpochDayNumber))),
            TimeOnly time => (CfgFormat.Time, Write(8, span => BinaryPrimitives.WriteInt64LittleEndian(span, time.Ticks))),
            DateTime { Kind: DateTimeKind.Unspecified } dateTime => (CfgFormat.DateTime, Write(8, span => BinaryPrimitives.WriteInt64LittleEndian(span, dateTime.Ticks - EpochTicks))),
            DateTime dateTime => (CfgFormat.Timestamp, EncodeTimestamp(new DateTimeOffset(dateTime))),
            DateTimeOffset timestamp => (CfgFormat.Timestamp, EncodeTimestamp(timestamp)),
            TimeSpan duration => (CfgFormat.Duration, Write(8, span => BinaryPrimitives.WriteInt64LittleEndian(span, duration.Ticks))),
            _ => ((byte)0, ReadOnlyMemory<byte>.Empty),
        };

        return type != 0;
    }

    /// <summary>
    /// The VALUE node of <paramref name="value"/> in the form of the declared <paramref name="type"/>: an integer as
    /// another integer type, a number as a floating point type (e.g. FLOAT64, the form of version 1), a date, a time or
    /// a UUID as UTF8_STRING (the text of version 1). False if the declaration is not about such a conversion; a value
    /// that the declared type does not hold exactly is rejected.
    /// </summary>
    public static bool TryEncodeAs(object value, byte type, out ReadOnlyMemory<byte> content)
    {
        content = ReadOnlyMemory<byte>.Empty;

        if (CfgFormat.IsInteger(type) && AsInteger(value) is { } integer)
        {
            content = EncodeInteger(type, integer, value);
            return true;
        }

        if (CfgFormat.IsFloat(type) && IsNumber(value))
        {
            content = EncodeFloat(type, ExactFloat(type, value));
            return true;
        }

        // The bytes of a tree, checked
        if (type == CfgFormat.Container && value is byte[] stream)
        {
            Containers.Decode(stream, limits: null);
            content = stream;
            return true;
        }

        // An enum by its name, also when its type declares a number
        if (type == CfgFormat.Utf8String && value is Enum name)
        {
            content = Data.FromUtf8String(name.ToString());
            return true;
        }

        if (type == CfgFormat.Utf8String && value is not (string or char) && ScalarText.TryFormat(value, out string? text))
        {
            content = Data.FromUtf8String(text);
            return true;
        }

        return false;
    }

    /// <summary>
    /// The form declared on an enum type with <see cref="PromptuariumTypeAttribute"/>: an integer type (its number),
    /// FLOAT64 (its number, the form of version 1) or UTF8_STRING (its name, the default); null if the type declares
    /// none.
    /// </summary>
    public static byte? EnumForm(Type enumType)
    {
        return EnumForms.GetOrAdd(enumType, static type =>
        {
            if (type.GetCustomAttribute<PromptuariumTypeAttribute>(inherit: false) is not { } attribute)
            {
                return null;
            }

            var form = (byte)attribute.Type;

            if (!CfgFormat.IsInteger(form) && form is not (CfgFormat.Float64 or CfgFormat.Utf8String))
            {
                throw new NotSupportedException($"An enum is stored by its name (Utf8String) or by its number (an integer type or Float64): {type.Name} is declared as {attribute.Type}.");
            }

            return form;
        });
    }

    private static byte[] Write(int size, Action<Span<byte>> write)
    {
        var bytes = new byte[size];
        write(bytes);
        return bytes;
    }

    // A NaN is the canonical quiet NaN of its size (SERIALIZER_FORMAT.md, 6.2)
    private static byte[] EncodeFloat(byte type, double number)
    {
        return type switch
        {
            CfgFormat.Float16 => Write(2, span => BinaryPrimitives.WriteUInt16LittleEndian(span, double.IsNaN(number) ? (ushort)0x7E00 : BitConverter.HalfToUInt16Bits((Half)number))),
            CfgFormat.Float32 => Write(4, span => BinaryPrimitives.WriteUInt32LittleEndian(span, double.IsNaN(number) ? 0x7FC00000u : BitConverter.SingleToUInt32Bits((float)number))),
            _ => Write(8, span => BinaryPrimitives.WriteUInt64LittleEndian(span, double.IsNaN(number) ? 0x7FF8000000000000ul : BitConverter.DoubleToUInt64Bits(number))),
        };
    }

    private static byte[] EncodeUuid(Guid guid)
    {
        var bytes = new byte[16];
        guid.TryWriteBytes(bytes, bigEndian: true, out _);
        return bytes;
    }

    private static byte[] EncodeTimestamp(DateTimeOffset timestamp)
    {
        var bytes = new byte[10];
        BinaryPrimitives.WriteInt64LittleEndian(bytes, timestamp.UtcTicks - EpochTicks);
        BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(8), (short)timestamp.Offset.TotalMinutes);
        return bytes;
    }

    private static BigInteger? AsInteger(object value) => value switch
    {
        sbyte number => number,
        short number => number,
        int number => number,
        long number => number,
        nint number => number,
        Int128 number => number,
        byte number => number,
        ushort number => number,
        uint number => number,
        ulong number => number,
        nuint number => number,
        UInt128 number => number,
        BigInteger number => number,
        Enum => EnumNumber(value),
        _ => null,
    };

    // The number of an enum, by its underlying type
    private static BigInteger EnumNumber(object value) => Convert.GetTypeCode(value) switch
    {
        TypeCode.Byte or TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64 => new BigInteger(Convert.ToUInt64(value, CultureInfo.InvariantCulture)),
        _ => new BigInteger(Convert.ToInt64(value, CultureInfo.InvariantCulture)),
    };

    private static bool IsNumber(object value) => AsInteger(value) is not null || value is Half or float or double or decimal;

    private static byte[] EncodeInteger(byte type, BigInteger integer, object value)
    {
        try
        {
            return type switch
            {
                CfgFormat.Int8 => [(byte)(sbyte)integer],
                CfgFormat.Int16 => Write(2, span => BinaryPrimitives.WriteInt16LittleEndian(span, (short)integer)),
                CfgFormat.Int32 => Write(4, span => BinaryPrimitives.WriteInt32LittleEndian(span, (int)integer)),
                CfgFormat.Int64 => Write(8, span => BinaryPrimitives.WriteInt64LittleEndian(span, (long)integer)),
                CfgFormat.Int128 => Write(16, span => BinaryPrimitives.WriteInt128LittleEndian(span, (Int128)integer)),
                CfgFormat.UInt8 => [(byte)integer],
                CfgFormat.UInt16 => Write(2, span => BinaryPrimitives.WriteUInt16LittleEndian(span, (ushort)integer)),
                CfgFormat.UInt32 => Write(4, span => BinaryPrimitives.WriteUInt32LittleEndian(span, (uint)integer)),
                CfgFormat.UInt64 => Write(8, span => BinaryPrimitives.WriteUInt64LittleEndian(span, (ulong)integer)),
                CfgFormat.UInt128 => Write(16, span => BinaryPrimitives.WriteUInt128LittleEndian(span, (UInt128)integer)),
                _ => integer.ToByteArray(),
            };
        }
        catch (OverflowException exception)
        {
            throw new NotSupportedException($"The {value.GetType().Name} {integer} does not fit the declared {(PromptuariumType)type}.", exception);
        }
    }

    // The value as a floating point number of the declared type, exactly: an integer or a decimal that the type does not
    // hold exactly is rejected (a long beyond 2^53 as FLOAT64, the rule of 8.3.0), so is a binary floating point number
    // that a smaller type does not hold
    private static double ExactFloat(byte type, object value)
    {
        double number;
        bool exact;

        switch (value)
        {
            case Half half:
                number = (double)half;
                exact = true;
                break;

            case float single:
                number = single;
                exact = true;
                break;

            case double wide:
                number = wide;
                exact = true;
                break;

            case decimal fraction:
                number = (double)fraction;
                exact = DecimalOf(number) == fraction;
                break;

            default:
                BigInteger integer = AsInteger(value)!.Value;
                number = (double)integer;
                exact = double.IsFinite(number) && new BigInteger(number) == integer;
                break;
        }

        exact = exact && (double.IsNaN(number) || type switch
        {
            CfgFormat.Float16 => (double)(Half)number == number,
            CfgFormat.Float32 => (float)number == number,
            _ => true,
        });

        return exact
            ? number
            : throw new NotSupportedException($"The {value.GetType().Name} {Convert.ToString(value, CultureInfo.InvariantCulture)} can not be stored exactly as {(PromptuariumType)type}.");
    }

    // The reader converts a FLOAT64 into a decimal with Convert.ToDecimal (15 significant digits)
    private static decimal? DecimalOf(double number)
    {
        try
        {
            return double.IsFinite(number) ? Convert.ToDecimal(number, CultureInfo.InvariantCulture) : null;
        }
        catch (OverflowException)
        {
            return null;
        }
    }

    // ---------------------------------------------------------------- reading

    /// <summary>
    /// The value of a VALUE node of a number, decimal, UUID, date or time type, as <paramref name="target"/> (or its
    /// natural .NET type for <see cref="object"/>): exactly, or <see cref="InvalidDataException"/>.
    /// </summary>
    public static object Decode(byte type, ReadOnlyMemory<byte> content, Type target)
    {
        ReadOnlySpan<byte> raw = content.Span;

        if (CfgFormat.IsInteger(type))
        {
            return FitInteger(DecodeInteger(type, raw), type, target);
        }

        if (CfgFormat.IsFloat(type))
        {
            return FitFloat(DecodeFloat(type, raw), type, target);
        }

        return type switch
        {
            CfgFormat.Decimal => Fit(DecodeDecimal(raw), target),
            CfgFormat.Uuid => Fit(new Guid(Sized(raw, 16, "a UUID"), bigEndian: true), target),
            CfgFormat.Date => Fit(DecodeDate(raw), target),
            CfgFormat.Time => Fit(DecodeTime(raw), target),
            CfgFormat.DateTime => Fit(DecodeDateTime(raw), target),
            CfgFormat.Timestamp => FitTimestamp(DecodeTimestamp(raw), target),
            CfgFormat.Duration => Fit(new TimeSpan(BinaryPrimitives.ReadInt64LittleEndian(Sized(raw, 8, "a DURATION"))), target),
            _ => throw new InvalidDataException($"Unsupported type code 0x{type:x2}."),
        };
    }

    private static ReadOnlySpan<byte> Sized(ReadOnlySpan<byte> raw, int size, string what)
    {
        return raw.Length == size ? raw : throw new InvalidDataException($"The value of {what} is {size} bytes.");
    }

    private static object Fit(object value, Type target)
    {
        return target == typeof(object) || target.IsInstanceOfType(value) ? value : throw new InvalidDataException($"A {value.GetType().Name} does not fit a {target.Name}.");
    }

    private static BigInteger DecodeInteger(byte type, ReadOnlySpan<byte> raw)
    {
        return type switch
        {
            CfgFormat.Int8 => (sbyte)Sized(raw, 1, "an INT8")[0],
            CfgFormat.Int16 => BinaryPrimitives.ReadInt16LittleEndian(Sized(raw, 2, "an INT16")),
            CfgFormat.Int32 => BinaryPrimitives.ReadInt32LittleEndian(Sized(raw, 4, "an INT32")),
            CfgFormat.Int64 => BinaryPrimitives.ReadInt64LittleEndian(Sized(raw, 8, "an INT64")),
            CfgFormat.Int128 => BinaryPrimitives.ReadInt128LittleEndian(Sized(raw, 16, "an INT128")),
            CfgFormat.UInt8 => Sized(raw, 1, "a UINT8")[0],
            CfgFormat.UInt16 => BinaryPrimitives.ReadUInt16LittleEndian(Sized(raw, 2, "a UINT16")),
            CfgFormat.UInt32 => BinaryPrimitives.ReadUInt32LittleEndian(Sized(raw, 4, "a UINT32")),
            CfgFormat.UInt64 => BinaryPrimitives.ReadUInt64LittleEndian(Sized(raw, 8, "a UINT64")),
            CfgFormat.UInt128 => BinaryPrimitives.ReadUInt128LittleEndian(Sized(raw, 16, "a UINT128")),
            _ => DecodeBigInt(raw),
        };
    }

    // The shortest form: at least one byte, no needless sign extension (SERIALIZER_FORMAT.md, 6.1)
    private static BigInteger DecodeBigInt(ReadOnlySpan<byte> raw)
    {
        if (raw.IsEmpty)
        {
            throw new InvalidDataException("A BIGINT value has at least one byte.");
        }

        var integer = new BigInteger(raw);
        return integer.GetByteCount() == raw.Length ? integer : throw new InvalidDataException("A BIGINT value is not in its shortest form.");
    }

    // An integer as the target: its natural type for object, any integer type it fits, a floating point type or a
    // decimal that holds it exactly, an enum by its number
    private static object FitInteger(BigInteger integer, byte type, Type target)
    {
        if (target == typeof(object))
        {
            // Each arm boxed as it is: a switch expression would convert them to their common type (BigInteger)
            return type switch
            {
                CfgFormat.UInt64 => (object)(ulong)integer,
                CfgFormat.Int128 => (object)(Int128)integer,
                CfgFormat.UInt128 => (object)(UInt128)integer,
                CfgFormat.BigInt => (object)integer,
                _ => (object)(long)integer,
            };
        }

        Type numeric = target.IsEnum ? Enum.GetUnderlyingType(target) : target;

        try
        {
            object? value = numeric switch
            {
                _ when numeric == typeof(sbyte) => (object)(sbyte)integer,
                _ when numeric == typeof(short) => (object)(short)integer,
                _ when numeric == typeof(int) => (object)(int)integer,
                _ when numeric == typeof(long) => (object)(long)integer,
                _ when numeric == typeof(nint) => (object)(nint)(long)integer,
                _ when numeric == typeof(Int128) => (object)(Int128)integer,
                _ when numeric == typeof(byte) => (object)(byte)integer,
                _ when numeric == typeof(ushort) => (object)(ushort)integer,
                _ when numeric == typeof(uint) => (object)(uint)integer,
                _ when numeric == typeof(ulong) => (object)(ulong)integer,
                _ when numeric == typeof(nuint) => (object)(nuint)(ulong)integer,
                _ when numeric == typeof(UInt128) => (object)(UInt128)integer,
                _ when numeric == typeof(BigInteger) => (object)integer,
                _ when numeric == typeof(decimal) => (object)(decimal)integer,
                _ when numeric == typeof(double) || numeric == typeof(float) || numeric == typeof(Half) => ExactlyAsFloat(integer, numeric),
                _ => null,
            };

            if (value is null)
            {
                throw new InvalidDataException($"An integer does not fit a {target.Name}.");
            }

            return target.IsEnum ? Enum.ToObject(target, value) : value;
        }
        catch (OverflowException exception)
        {
            throw new InvalidDataException($"{integer} does not fit a {target.Name}.", exception);
        }
    }

    private static object ExactlyAsFloat(BigInteger integer, Type target)
    {
        double number = (double)integer;
        bool exact = double.IsFinite(number) && new BigInteger(number) == integer
            && (target == typeof(double) || (target == typeof(float) ? (float)number == number : (double)(Half)number == number));

        if (!exact)
        {
            throw new InvalidDataException($"{integer} is not held exactly by a {target.Name}.");
        }

        return target == typeof(double) ? number : target == typeof(float) ? (object)(float)number : (object)(Half)number;
    }

    private static double DecodeFloat(byte type, ReadOnlySpan<byte> raw)
    {
        return type switch
        {
            CfgFormat.Float16 => (double)BinaryPrimitives.ReadHalfLittleEndian(Sized(raw, 2, "a FLOAT16")),
            CfgFormat.Float32 => BinaryPrimitives.ReadSingleLittleEndian(Sized(raw, 4, "a FLOAT32")),
            _ => BinaryPrimitives.ReadDoubleLittleEndian(Sized(raw, 8, "a FLOAT64")),
        };
    }

    // A floating point number as the target: its natural type for object, a floating point type that holds it exactly,
    // an integer type if it is a whole number that fits, a decimal (as in version 1), an enum by its number
    private static object FitFloat(double number, byte type, Type target)
    {
        if (target == typeof(object))
        {
            return type switch
            {
                CfgFormat.Float16 => (object)(Half)number,
                CfgFormat.Float32 => (object)(float)number,
                _ => (object)number,
            };
        }

        if (target == typeof(double))
        {
            return number;
        }

        if (target == typeof(float) || target == typeof(Half))
        {
            bool exact = double.IsNaN(number) || (target == typeof(float) ? (float)number == number : (double)(Half)number == number);
            return exact
                ? target == typeof(float) ? (object)(float)number : (object)(Half)number
                : throw new InvalidDataException($"{number.ToString(CultureInfo.InvariantCulture)} is not held exactly by a {target.Name}.");
        }

        if (target == typeof(decimal))
        {
            return DecimalOf(number) ?? throw new InvalidDataException($"{number.ToString(CultureInfo.InvariantCulture)} does not fit a Decimal.");
        }

        if (!double.IsFinite(number) || Math.Floor(number) != number)
        {
            throw new InvalidDataException($"{number.ToString(CultureInfo.InvariantCulture)} does not fit a {target.Name}.");
        }

        return FitInteger(new BigInteger(number), CfgFormat.Float64, target);
    }

    [GeneratedRegex(@"^-?(0|[1-9][0-9]*)(\.[0-9]+)?$", RegexOptions.CultureInvariant)]
    private static partial Regex CanonicalDecimal();

    // The canonical text, and a value a decimal holds exactly: parsed, it gives the same text back (SERIALIZER_FORMAT.md, 6.3)
    private static decimal DecodeDecimal(ReadOnlySpan<byte> raw)
    {
        string text;

        try
        {
            text = new System.Text.UTF8Encoding(false, true).GetString(raw);
        }
        catch (System.Text.DecoderFallbackException exception)
        {
            throw new InvalidDataException("A DECIMAL value is not valid UTF-8.", exception);
        }

        if (!CanonicalDecimal().IsMatch(text))
        {
            throw new InvalidDataException($"\"{text}\" is not the canonical text of a decimal number.");
        }

        bool held = decimal.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal value)
            && value.ToString(CultureInfo.InvariantCulture) == text;

        return held ? value : throw new InvalidDataException($"\"{text}\" is not held exactly by a Decimal (or it is a negative zero).");
    }

    private static DateOnly DecodeDate(ReadOnlySpan<byte> raw)
    {
        long dayNumber = (long)BinaryPrimitives.ReadInt32LittleEndian(Sized(raw, 4, "a DATE")) + EpochDayNumber;

        return dayNumber is >= 0 && dayNumber <= DateOnly.MaxValue.DayNumber
            ? DateOnly.FromDayNumber((int)dayNumber)
            : throw new InvalidDataException("A DATE value is out of range.");
    }

    private static TimeOnly DecodeTime(ReadOnlySpan<byte> raw)
    {
        long ticks = BinaryPrimitives.ReadInt64LittleEndian(Sized(raw, 8, "a TIME"));

        return ticks >= 0 && ticks <= TimeOnly.MaxValue.Ticks ? new TimeOnly(ticks) : throw new InvalidDataException("A TIME value is out of range.");
    }

    private static DateTime DecodeDateTime(ReadOnlySpan<byte> raw)
    {
        return new DateTime(EpochBased(BinaryPrimitives.ReadInt64LittleEndian(Sized(raw, 8, "a DATETIME")), "DATETIME"), DateTimeKind.Unspecified);
    }

    private static DateTimeOffset DecodeTimestamp(ReadOnlySpan<byte> raw)
    {
        raw = Sized(raw, 10, "a TIMESTAMP");
        long utcTicks = EpochBased(BinaryPrimitives.ReadInt64LittleEndian(raw), "TIMESTAMP");
        short minutes = BinaryPrimitives.ReadInt16LittleEndian(raw[8..]);

        if (minutes is < -840 or > 840)
        {
            throw new InvalidDataException("The offset of a TIMESTAMP value is out of range.");
        }

        TimeSpan offset = TimeSpan.FromMinutes(minutes);
        long localTicks = utcTicks + offset.Ticks;

        return localTicks >= 0 && localTicks <= System.DateTime.MaxValue.Ticks
            ? new DateTimeOffset(localTicks, offset)
            : throw new InvalidDataException("The local time of a TIMESTAMP value is out of range.");
    }

    // Ticks since the Unix epoch as ticks of .NET, in the range of a DateTime
    private static long EpochBased(long ticks, string what)
    {
        long dotnet = ticks + EpochTicks;

        return ticks >= -EpochTicks && ticks <= System.DateTime.MaxValue.Ticks - EpochTicks
            ? dotnet
            : throw new InvalidDataException($"A {what} value is out of range.");
    }

    // A TIMESTAMP as a DateTimeOffset, or as a DateTime: of offset 0 a Utc one, of another offset the instant as a Local one
    private static object FitTimestamp(DateTimeOffset timestamp, Type target)
    {
        if (target == typeof(System.DateTime))
        {
            return timestamp.Offset == TimeSpan.Zero ? timestamp.UtcDateTime : timestamp.LocalDateTime;
        }

        return Fit(timestamp, target);
    }
}
