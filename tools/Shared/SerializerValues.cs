using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

namespace Promptuarium.Tools;

/// <summary>
/// The type codes and the values of SERIALIZER_FORMAT.md for the tools (pcfg, PStudio), which read and write the layout
/// themselves: the names, the versions, the signatures, and the values of version 2 (the numbers, DECIMAL, UUID, the
/// dates and times) checked, shown as text and parsed back. Every text gives back the same bytes.
/// </summary>
internal static partial class SerializerValues
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
    private static readonly long EpochTicks = DateTime.UnixEpoch.Ticks;
    private static readonly int EpochDayNumber = DateOnly.FromDateTime(DateTime.UnixEpoch).DayNumber;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    private const string TimeFormat = "HH:mm:ss.FFFFFFF";
    private const string DateTimeFormat = "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF";
    private const string TimestampFormat = "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz";

    /// <summary>The names of the type codes (SERIALIZER_FORMAT.md, 4.4), the reserved ones too, by code.</summary>
    public static IReadOnlyDictionary<byte, string> TypeNames { get; } = new Dictionary<byte, string>
    {
        [0x00] = "NULL",
        [0x01] = "BOOLEAN",
        [0x20] = "OBJECT",
        [0x21] = "ARRAY",
        [0x40] = "INT8",
        [0x41] = "INT16",
        [0x42] = "INT32",
        [0x43] = "INT64",
        [0x44] = "INT128",
        [0x50] = "UINT8",
        [0x51] = "UINT16",
        [0x52] = "UINT32",
        [0x53] = "UINT64",
        [0x54] = "UINT128",
        [0x60] = "FLOAT8",
        [0x61] = "FLOAT16",
        [0x62] = "FLOAT32",
        [0x63] = "FLOAT64",
        [0x64] = "FLOAT128",
        [0x70] = "DECIMAL",
        [0x78] = "BIGINT",
        [0x80] = "UTF8_STRING",
        [0x90] = "UUID",
        [0xD0] = "DATE",
        [0xD1] = "TIME",
        [0xD2] = "DATETIME",
        [0xD3] = "TIMESTAMP",
        [0xD4] = "DURATION",
        [0xF0] = "BLOB",
        [0xF1] = "CONTAINER",
    };

    /// <summary>CONTAINER: a nested tree (SERIALIZER_FORMAT.md, 6.6).</summary>
    public const byte Container = 0xF1;

    /// <summary>The type codes of version 1 of the layout.</summary>
    public static IReadOnlySet<byte> Version1Types { get; } = new HashSet<byte> { 0x00, 0x01, 0x20, 0x21, 0x63, 0x80, 0xF0 };

    /// <summary>The type codes of version 2 of the layout (the ones of version 1 included); FLOAT8 and FLOAT128 are reserved.</summary>
    public static IReadOnlySet<byte> Version2Types { get; } = TypeNames.Keys.Where(code => code is not (0x60 or 0x64)).ToHashSet();

    /// <summary>The type codes whose values this class reads and writes as text: the numbers, DECIMAL, UUID, the dates and times.</summary>
    public static IReadOnlySet<byte> TypedValues { get; } = Version2Types.Where(code => code is not (0x00 or 0x01 or 0x20 or 0x21 or 0x80 or 0xF0 or 0xF1)).ToHashSet();

    /// <summary>
    /// The tree of a CONTAINER value (SERIALIZER_FORMAT.md, 6.6): one complete Promptuarium stream, with its terminator
    /// and nothing after it.
    /// </summary>
    /// <exception cref="FormatException">The value is not one complete stream.</exception>
    public static Element DecodeContainer(ReadOnlySpan<byte> value)
    {
        if (value.IsEmpty)
        {
            throw new FormatException("the CONTAINER value is empty; the smallest one is 6000, an empty tree");
        }

        // One byte more: a stream without its terminator consumes it too, a complete one stops before it
        byte[] probe = new byte[value.Length + 1];
        value.CopyTo(probe);

        Element tree;
        int consumed;

        try
        {
            tree = Element.FromBytes(probe, null, out consumed);
        }
        catch (PromptuariumException ex)
        {
            throw new FormatException($"the CONTAINER value is not a Promptuarium stream: {ex.Message}", ex);
        }

        if (consumed > value.Length)
        {
            throw new FormatException("the CONTAINER value ends without the terminator of its stream");
        }

        if (consumed < value.Length)
        {
            throw new FormatException(string.Create(Invariant, $"the CONTAINER value has {value.Length - consumed} bytes after the terminator of its stream"));
        }

        return tree;
    }

    /// <summary>What is wrong with a CONTAINER value, or null if it is one complete stream.</summary>
    public static string? ContainerProblem(ReadOnlySpan<byte> value)
    {
        try
        {
            DecodeContainer(value);
            return null;
        }
        catch (FormatException ex)
        {
            return ex.Message;
        }
    }

    /// <summary>A CONTAINER value in a few words: its size, its signature and its nodes, e.g. "26 bytes, PRMT:GEN1-LE, 7 nodes".</summary>
    public static string ContainerSummary(ReadOnlySpan<byte> value)
    {
        Element tree = DecodeContainer(value);
        string signature = tree.Data.IsEmpty && tree.MetaData.Length is > 0 and <= 15 && Ascii.IsValid(tree.MetaData.Span)
            ? Encoding.ASCII.GetString(tree.MetaData.Span)
            : "not signed";

        int nodes = tree.GetStatistics().Nodes;
        return string.Create(Invariant, $"{value.Length} bytes, {signature}, {nodes} {(nodes == 1 ? "node" : "nodes")}");
    }

    /// <summary>The signatures of the layout, by kind: version 1 and version 2 (SERIALIZER_FORMAT.md, 2).</summary>
    public static IReadOnlyList<(string Version1, string Version2)> Kinds { get; } =
    [
        ("PRMT:GEN1-LE", "PRMT:GEN2-LE"),
        ("PRMT:CFG1-LE", "PRMT:CFG2-LE"),
    ];

    /// <summary>Whether <paramref name="signature"/> is one of the signatures of the layout (of either version).</summary>
    public static bool IsLayoutSignature(string signature) => Kinds.Any(kind => kind.Version1 == signature || kind.Version2 == signature);

    /// <summary>Whether <paramref name="signature"/> is a signature of version 1 of the layout.</summary>
    public static bool IsVersion1Signature(string signature) => Kinds.Any(kind => kind.Version1 == signature);

    /// <summary>Whether a file signed <paramref name="found"/> is accepted where <paramref name="expected"/> is: the same, or the other version of its kind.</summary>
    public static bool Accepts(string expected, string found)
    {
        return expected == found || Kinds.Any(kind => (kind.Version1 == expected || kind.Version2 == expected) && (kind.Version1 == found || kind.Version2 == found));
    }

    /// <summary>The signature of the kind of <paramref name="signature"/> for a tree that needs version 2 or not; another signature as it is.</summary>
    public static string SignatureFor(string signature, bool needsVersion2)
    {
        foreach ((string version1, string version2) in Kinds)
        {
            if (signature == version1 || signature == version2)
            {
                return needsVersion2 ? version2 : version1;
            }
        }

        return signature;
    }

    /// <summary>The prefix of a type in the text of a value (pcfg) and the name of its YAML tag: the name in lower case, e.g. "int32", "timestamp".</summary>
    public static string Prefix(byte type) => TypeNames[type].ToLowerInvariant();

    /// <summary>The type of a prefix ("int32", "timestamp"), or null.</summary>
    public static byte? TypeOfPrefix(string prefix)
    {
        foreach (byte type in TypedValues)
        {
            if (Prefix(type) == prefix)
            {
                return type;
            }
        }

        return null;
    }

    /// <summary>Whether a FLOAT64 value is finite (a file of version 1 holds only finite ones).</summary>
    public static bool IsFiniteFloat64(ReadOnlySpan<byte> value) => value.Length != 8 || double.IsFinite(BinaryPrimitives.ReadDoubleLittleEndian(value));

    /// <summary>What is wrong with a value of a typed type, or null if it is valid (SERIALIZER_FORMAT.md, 6).</summary>
    public static string? Problem(byte type, ReadOnlySpan<byte> value)
    {
        try
        {
            Format(type, value);
            return null;
        }
        catch (FormatException exception)
        {
            return exception.Message;
        }
    }

    /// <summary>The text of a value of a typed type.</summary>
    /// <exception cref="FormatException">The value is not valid.</exception>
    public static string Format(byte type, ReadOnlySpan<byte> value)
    {
        string name = TypeNames.GetValueOrDefault(type, "unknown");

        if (FixedSize(type) is { } size && value.Length != size)
        {
            throw new FormatException(string.Create(Invariant, $"the {name} value has {value.Length} bytes, not {size}"));
        }

        return type switch
        {
            0x40 => ((sbyte)value[0]).ToString(Invariant),
            0x41 => BinaryPrimitives.ReadInt16LittleEndian(value).ToString(Invariant),
            0x42 => BinaryPrimitives.ReadInt32LittleEndian(value).ToString(Invariant),
            0x43 => BinaryPrimitives.ReadInt64LittleEndian(value).ToString(Invariant),
            0x44 => BinaryPrimitives.ReadInt128LittleEndian(value).ToString(Invariant),
            0x50 => value[0].ToString(Invariant),
            0x51 => BinaryPrimitives.ReadUInt16LittleEndian(value).ToString(Invariant),
            0x52 => BinaryPrimitives.ReadUInt32LittleEndian(value).ToString(Invariant),
            0x53 => BinaryPrimitives.ReadUInt64LittleEndian(value).ToString(Invariant),
            0x54 => BinaryPrimitives.ReadUInt128LittleEndian(value).ToString(Invariant),
            0x78 => FormatBigInt(value),
            0x61 => BinaryPrimitives.ReadHalfLittleEndian(value).ToString(Invariant),
            0x62 => BinaryPrimitives.ReadSingleLittleEndian(value).ToString(Invariant),
            0x63 => BinaryPrimitives.ReadDoubleLittleEndian(value).ToString(Invariant),
            0x70 => FormatDecimal(value),
            0x90 => new Guid(value, bigEndian: true).ToString("D"),
            0xD0 => ReadDate(value).ToString("yyyy-MM-dd", Invariant),
            0xD1 => ReadTime(value).ToString(TimeFormat, Invariant),
            0xD2 => new DateTime(EpochBased(BinaryPrimitives.ReadInt64LittleEndian(value), name), DateTimeKind.Unspecified).ToString(DateTimeFormat, Invariant),
            0xD3 => ReadTimestamp(value).ToString(TimestampFormat, Invariant),
            0xD4 => new TimeSpan(BinaryPrimitives.ReadInt64LittleEndian(value)).ToString("c", Invariant),
            _ => throw new FormatException($"the type {name} has no typed value"),
        };
    }

    /// <summary>The bytes of the text of a value of a typed type; a NaN is the canonical quiet NaN.</summary>
    /// <exception cref="FormatException">The text is not a value of the type.</exception>
    public static byte[] Parse(byte type, string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        string name = TypeNames.GetValueOrDefault(type, "unknown");
        const NumberStyles Integer = NumberStyles.AllowLeadingSign;
        const NumberStyles Float = NumberStyles.Float;

        try
        {
            return type switch
            {
                0x40 => [(byte)sbyte.Parse(text, Integer, Invariant)],
                0x41 => Write(2, span => BinaryPrimitives.WriteInt16LittleEndian(span, short.Parse(text, Integer, Invariant))),
                0x42 => Write(4, span => BinaryPrimitives.WriteInt32LittleEndian(span, int.Parse(text, Integer, Invariant))),
                0x43 => Write(8, span => BinaryPrimitives.WriteInt64LittleEndian(span, long.Parse(text, Integer, Invariant))),
                0x44 => Write(16, span => BinaryPrimitives.WriteInt128LittleEndian(span, Int128.Parse(text, Integer, Invariant))),
                0x50 => [byte.Parse(text, Integer, Invariant)],
                0x51 => Write(2, span => BinaryPrimitives.WriteUInt16LittleEndian(span, ushort.Parse(text, Integer, Invariant))),
                0x52 => Write(4, span => BinaryPrimitives.WriteUInt32LittleEndian(span, uint.Parse(text, Integer, Invariant))),
                0x53 => Write(8, span => BinaryPrimitives.WriteUInt64LittleEndian(span, ulong.Parse(text, Integer, Invariant))),
                0x54 => Write(16, span => BinaryPrimitives.WriteUInt128LittleEndian(span, UInt128.Parse(text, Integer, Invariant))),
                0x78 => BigInteger.Parse(text, Integer, Invariant).ToByteArray(),
                0x61 => Write(2, span => BinaryPrimitives.WriteUInt16LittleEndian(span, Half.Parse(text, Float, Invariant) is var half && Half.IsNaN(half) ? (ushort)0x7E00 : BitConverter.HalfToUInt16Bits(half))),
                0x62 => Write(4, span => BinaryPrimitives.WriteUInt32LittleEndian(span, float.Parse(text, Float, Invariant) is var single && float.IsNaN(single) ? 0x7FC00000u : BitConverter.SingleToUInt32Bits(single))),
                0x63 => Write(8, span => BinaryPrimitives.WriteUInt64LittleEndian(span, double.Parse(text, Float, Invariant) is var wide && double.IsNaN(wide) ? 0x7FF8000000000000ul : BitConverter.DoubleToUInt64Bits(wide))),
                0x70 => IsCanonicalDecimal(text) ? Encoding.UTF8.GetBytes(text) : throw new FormatException($"'{text}' is not the canonical text of a decimal number (e.g. -12.750)"),
                0x90 => Write(16, span => Guid.ParseExact(text, "D").TryWriteBytes(span, bigEndian: true, out _)),
                0xD0 => Write(4, span => BinaryPrimitives.WriteInt32LittleEndian(span, DateOnly.ParseExact(text, "yyyy-MM-dd", Invariant).DayNumber - EpochDayNumber)),
                0xD1 => Write(8, span => BinaryPrimitives.WriteInt64LittleEndian(span, TimeOnly.ParseExact(text, TimeFormat, Invariant).Ticks)),
                0xD2 => Write(8, span => BinaryPrimitives.WriteInt64LittleEndian(span, DateTime.ParseExact(text, DateTimeFormat, Invariant, DateTimeStyles.None).Ticks - EpochTicks)),
                0xD3 => WriteTimestamp(DateTimeOffset.ParseExact(text, TimestampFormat, Invariant, DateTimeStyles.None)),
                0xD4 => Write(8, span => BinaryPrimitives.WriteInt64LittleEndian(span, TimeSpan.ParseExact(text, "c", Invariant).Ticks)),
                _ => throw new FormatException($"the type {name} has no typed value"),
            };
        }
        catch (OverflowException exception)
        {
            throw new FormatException($"'{text}' is out of the range of {name}", exception);
        }
        catch (FormatException exception) when (!exception.Message.StartsWith('\''))
        {
            throw new FormatException($"'{text}' is not a {name}: {Example(type)}", exception);
        }
    }

    /// <summary>An example of the text of a value of a typed type, for the messages and the help.</summary>
    public static string Example(byte type) => type switch
    {
        0x40 or 0x41 or 0x42 or 0x43 or 0x44 or 0x78 => "an integer, e.g. -42",
        0x50 or 0x51 or 0x52 or 0x53 or 0x54 => "an unsigned integer, e.g. 42",
        0x61 or 0x62 or 0x63 => "a number, e.g. 1.5, NaN, Infinity or -Infinity",
        0x70 => "e.g. -12.750",
        0x90 => "e.g. 00112233-4455-6677-8899-aabbccddeeff",
        0xD0 => "e.g. 2026-11-22",
        0xD1 => "e.g. 19:30:15 or 19:30:15.5",
        0xD2 => "e.g. 2026-11-22T19:30:15",
        0xD3 => "e.g. 2026-11-22T19:30:15+01:00",
        0xD4 => "e.g. 01:30:00, -1.02:03:04.5",
        _ => string.Empty,
    };

    private static int? FixedSize(byte type) => type switch
    {
        0x40 or 0x50 => 1,
        0x41 or 0x51 or 0x61 => 2,
        0x42 or 0x52 or 0x62 or 0xD0 => 4,
        0x43 or 0x53 or 0x63 or 0xD1 or 0xD2 or 0xD4 => 8,
        0xD3 => 10,
        0x44 or 0x54 or 0x90 => 16,
        _ => null,
    };

    private static byte[] Write(int size, Action<Span<byte>> write)
    {
        var bytes = new byte[size];
        write(bytes);
        return bytes;
    }

    // The shortest form: at least one byte, no needless sign extension
    private static string FormatBigInt(ReadOnlySpan<byte> value)
    {
        if (value.IsEmpty)
        {
            throw new FormatException("the BIGINT value is empty");
        }

        var integer = new BigInteger(value);
        return integer.GetByteCount() == value.Length ? integer.ToString(Invariant) : throw new FormatException("the BIGINT value is not in its shortest form");
    }

    private static string FormatDecimal(ReadOnlySpan<byte> value)
    {
        string text;

        try
        {
            text = StrictUtf8.GetString(value);
        }
        catch (DecoderFallbackException)
        {
            throw new FormatException("the DECIMAL value is not valid UTF-8");
        }

        return IsCanonicalDecimal(text) ? text : throw new FormatException($"the DECIMAL value \"{text}\" is not in the canonical form");
    }

    // -?(0|[1-9][0-9]*)(\.[0-9]+)?, and a zero has no sign
    private static bool IsCanonicalDecimal(string text) => CanonicalDecimal().IsMatch(text) && !(text[0] == '-' && text.All(c => c is '-' or '0' or '.'));

    [GeneratedRegex(@"^-?(0|[1-9][0-9]*)(\.[0-9]+)?$", RegexOptions.CultureInvariant)]
    private static partial Regex CanonicalDecimal();

    private static DateOnly ReadDate(ReadOnlySpan<byte> value)
    {
        long dayNumber = (long)BinaryPrimitives.ReadInt32LittleEndian(value) + EpochDayNumber;
        return dayNumber >= 0 && dayNumber <= DateOnly.MaxValue.DayNumber ? DateOnly.FromDayNumber((int)dayNumber) : throw new FormatException("the DATE value is out of range");
    }

    private static TimeOnly ReadTime(ReadOnlySpan<byte> value)
    {
        long ticks = BinaryPrimitives.ReadInt64LittleEndian(value);
        return ticks >= 0 && ticks <= TimeOnly.MaxValue.Ticks ? new TimeOnly(ticks) : throw new FormatException("the TIME value is out of a day");
    }

    // Ticks since the Unix epoch as ticks of .NET, in the range of a DateTime
    private static long EpochBased(long ticks, string name)
    {
        return ticks >= -EpochTicks && ticks <= DateTime.MaxValue.Ticks - EpochTicks ? ticks + EpochTicks : throw new FormatException($"the {name} value is out of range");
    }

    private static DateTimeOffset ReadTimestamp(ReadOnlySpan<byte> value)
    {
        long utcTicks = EpochBased(BinaryPrimitives.ReadInt64LittleEndian(value), "TIMESTAMP");
        short minutes = BinaryPrimitives.ReadInt16LittleEndian(value[8..]);

        if (minutes is < -840 or > 840)
        {
            throw new FormatException("the offset of the TIMESTAMP value is out of ±14 hours");
        }

        long localTicks = utcTicks + (minutes * TimeSpan.TicksPerMinute);
        return localTicks >= 0 && localTicks <= DateTime.MaxValue.Ticks
            ? new DateTimeOffset(localTicks, TimeSpan.FromMinutes(minutes))
            : throw new FormatException("the local time of the TIMESTAMP value is out of range");
    }

    private static byte[] WriteTimestamp(DateTimeOffset timestamp)
    {
        var bytes = new byte[10];
        BinaryPrimitives.WriteInt64LittleEndian(bytes, timestamp.UtcTicks - EpochTicks);
        BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(8), (short)timestamp.Offset.TotalMinutes);
        return bytes;
    }
}
