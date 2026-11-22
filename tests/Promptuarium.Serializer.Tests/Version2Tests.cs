// The declared forms use the experimental type control
#pragma warning disable PRMTSER001

using System.Globalization;
using System.Numerics;
using Promptuarium;
using Promptuarium.Serializer;

namespace Promptuarium.Serializer.Tests;

/// <summary>Version 2 of SERIALIZER_FORMAT.md: the types, the test vector, the versions and the reading rules.</summary>
[TestClass]
public sealed class Version2Tests
{
    // SERIALIZER_FORMAT.md, section 9.2
    private const string Vector = """
        6c 50 52 4d 54 3a 47 45 4e 32 2d 4c 45 42 69 38
        21 00 41 40 21 01 c1 fb 21 03 83 69 33 32 21 00
        41 42 21 01 c4 fe ff ff ff 21 03 83 69 36 34 21
        00 41 43 21 01 c8 01 00 00 00 00 00 20 00 21 03
        84 69 31 32 38 21 00 41 44 21 01 d0 00 00 00 00
        00 00 00 00 01 00 00 00 00 00 00 00 21 03 83 75
        31 36 21 00 41 51 21 01 c2 34 12 21 03 83 75 36
        34 21 00 41 53 21 01 c8 ff ff ff ff ff ff ff ff
        21 03 83 66 31 36 21 00 41 61 21 01 c2 00 3e 21
        03 83 66 33 32 21 00 41 62 21 01 c4 cd cc cc 3d
        21 03 83 6e 61 6e 21 00 41 63 21 01 c8 00 00 00
        00 00 00 f8 7f 21 03 83 64 65 63 21 00 41 70 21
        01 c6 31 32 2e 37 35 30 21 03 83 62 69 67 21 00
        41 78 21 01 c9 ff ff ff ff ff ff ff ff fe 21 03
        82 69 64 21 00 41 90 21 01 d0 00 11 22 33 44 55
        66 77 88 99 aa bb cc dd ee ff 21 03 84 64 61 74
        65 21 00 41 d0 21 01 c4 2b 51 00 00 21 03 84 74
        69 6d 65 21 00 41 d1 21 01 c8 c0 08 b3 7b a3 00
        00 00 21 03 82 64 74 21 00 41 d2 21 01 c8 80 fd
        e9 10 d9 c8 3f 00 21 03 82 74 73 21 00 41 d3 21
        01 ca 80 95 25 af d0 c8 3f 00 3c 00 21 03 83 64
        75 72 21 00 41 d4 21 01 c8 00 64 59 6d f3 ff ff
        ff 21 03 00
        """;

    public sealed class Values
    {
        [PromptuariumName("i8")] public sbyte I8 { get; set; }
        [PromptuariumName("i32")] public int I32 { get; set; }
        [PromptuariumName("i64")] public long I64 { get; set; }
        [PromptuariumName("i128")] public Int128 I128 { get; set; }
        [PromptuariumName("u16")] public ushort U16 { get; set; }
        [PromptuariumName("u64")] public ulong U64 { get; set; }
        [PromptuariumName("f16")] public Half F16 { get; set; }
        [PromptuariumName("f32")] public float F32 { get; set; }
        [PromptuariumName("nan")] public double NaN { get; set; }
        [PromptuariumName("dec")] public decimal Dec { get; set; }
        [PromptuariumName("big")] public BigInteger Big { get; set; }
        [PromptuariumName("id")] public Guid Id { get; set; }
        [PromptuariumName("date")] public DateOnly Date { get; set; }
        [PromptuariumName("time")] public TimeOnly Time { get; set; }
        [PromptuariumName("dt")] public DateTime Dt { get; set; }
        [PromptuariumName("ts")] public DateTimeOffset Ts { get; set; }
        [PromptuariumName("dur")] public TimeSpan Dur { get; set; }
    }

    private static Values Sample() => new()
    {
        I8 = -5,
        I32 = -2,
        I64 = (1L << 53) + 1,
        I128 = (Int128)ulong.MaxValue + 1,
        U16 = 0x1234,
        U64 = ulong.MaxValue,
        F16 = (Half)1.5,
        F32 = 0.1f,
        NaN = double.NaN,
        Dec = 12.750m,
        Big = -(BigInteger.One << 64) - 1,
        Id = Guid.Parse("00112233-4455-6677-8899-aabbccddeeff"),
        Date = new DateOnly(2026, 11, 22),
        Time = new TimeOnly(19, 30, 15, 500),
        Dt = new DateTime(2026, 11, 22, 19, 30, 15, DateTimeKind.Unspecified),
        Ts = new DateTimeOffset(2026, 11, 22, 19, 30, 15, TimeSpan.FromHours(1)),
        Dur = -TimeSpan.FromMinutes(90),
    };

    private static byte TypeOf(Element root, string name) => root.Children.Single(child => child.Data.AsUtf8String() == name).Children[0].Data.Span[0];

    private static Element Member(string name, byte type, params byte[] value) => Nodes.Member(name, type, value);

    private static Element Version2(params Element[] members)
    {
        Element root = Nodes.Root(members);
        root.MetaData = Data.FromAsciiString("PRMT:GEN2-LE");
        return root;
    }

    // ---------------------------------------------------------------- the test vector

    [TestMethod]
    public void TestVector_IsWrittenByteForByte()
    {
        CollectionAssert.AreEqual(Nodes.Hex(Vector), PromptuariumSerializer.Serialize(Sample()));
    }

    [TestMethod]
    public void TestVector_IsReadBack()
    {
        Values expected = Sample();

        Values copy = PromptuariumSerializer.Deserialize<Values>(Nodes.Hex(Vector));

        Assert.AreEqual(expected.I8, copy.I8);
        Assert.AreEqual(expected.I32, copy.I32);
        Assert.AreEqual(expected.I64, copy.I64);
        Assert.AreEqual(expected.I128, copy.I128);
        Assert.AreEqual(expected.U16, copy.U16);
        Assert.AreEqual(expected.U64, copy.U64);
        Assert.AreEqual(expected.F16, copy.F16);
        Assert.AreEqual(expected.F32, copy.F32);
        Assert.IsTrue(double.IsNaN(copy.NaN));
        Assert.AreEqual("12.750", copy.Dec.ToString(CultureInfo.InvariantCulture));
        Assert.AreEqual(expected.Big, copy.Big);
        Assert.AreEqual(expected.Id, copy.Id);
        Assert.AreEqual(expected.Date, copy.Date);
        Assert.AreEqual(expected.Time, copy.Time);
        Assert.AreEqual(expected.Dt, copy.Dt);
        Assert.AreEqual(DateTimeKind.Unspecified, copy.Dt.Kind);
        Assert.AreEqual(expected.Ts, copy.Ts);
        Assert.AreEqual(expected.Ts.Offset, copy.Ts.Offset);
        Assert.AreEqual(expected.Dur, copy.Dur);
    }

    [TestMethod]
    public void TestVector_ReadUntyped_GivesTheNaturalTypes()
    {
        var values = PromptuariumSerializer.Deserialize<Dictionary<string, object?>>(Nodes.Hex(Vector));

        Assert.AreEqual(-5L, values["i8"]);
        Assert.AreEqual((1L << 53) + 1, values["i64"]);
        Assert.AreEqual((Int128)ulong.MaxValue + 1, values["i128"]);
        Assert.AreEqual(0x1234L, values["u16"]);
        Assert.AreEqual(ulong.MaxValue, values["u64"]);
        Assert.AreEqual((Half)1.5, values["f16"]);
        Assert.AreEqual(0.1f, values["f32"]);
        Assert.AreEqual(12.750m, values["dec"]);
        Assert.AreEqual(-(BigInteger.One << 64) - 1, values["big"]);
        Assert.IsInstanceOfType<Guid>(values["id"]);
        Assert.IsInstanceOfType<DateOnly>(values["date"]);
        Assert.IsInstanceOfType<TimeOnly>(values["time"]);
        Assert.IsInstanceOfType<DateTime>(values["dt"]);
        Assert.IsInstanceOfType<DateTimeOffset>(values["ts"]);
        Assert.IsInstanceOfType<TimeSpan>(values["dur"]);
    }

    [TestMethod]
    public void Uuid_IsInTheOrderOfTheText()
    {
        Element root = PromptuariumSerializer.ToElement(new Values { Id = Guid.Parse("00112233-4455-6677-8899-aabbccddeeff") });
        Element id = root.Children.Single(child => child.Data.AsUtf8String() == "id");

        CollectionAssert.AreEqual(Nodes.Hex("00 11 22 33 44 55 66 77 88 99 aa bb cc dd ee ff"), id.Children[1].Data.ToArray());
    }

    // ---------------------------------------------------------------- versions

    public sealed class Plain
    {
        public double Rate { get; set; } = 44100;
        public string Name { get; set; } = "default";
        public Guid? Id { get; set; }
        public int? Count { get; set; }
    }

    [TestMethod]
    public void Signature_IsTheLowestVersionThatHoldsTheTree()
    {
        Assert.AreEqual("PRMT:GEN1-LE", PromptuariumSerializer.ToElement(new Plain()).MetaData.AsAsciiString());
        Assert.AreEqual("PRMT:GEN2-LE", PromptuariumSerializer.ToElement(new Plain { Count = 1 }).MetaData.AsAsciiString());
        Assert.AreEqual("PRMT:GEN2-LE", PromptuariumSerializer.ToElement(new Plain { Id = Guid.NewGuid() }).MetaData.AsAsciiString());
        Assert.AreEqual("PRMT:GEN2-LE", PromptuariumSerializer.ToElement(new Plain { Rate = double.PositiveInfinity }).MetaData.AsAsciiString());
        Assert.AreEqual("PRMT:GEN2-LE", PromptuariumSerializer.ToElement(new Dictionary<string, object> { ["deep"] = new List<object> { new Dictionary<string, object> { ["n"] = 1 } } }).MetaData.AsAsciiString());
    }

    [TestMethod]
    public void Signature_OfAConfiguration_FollowsTheContentToo()
    {
        var options = new PromptuariumSerializerOptions { Signature = PromptuariumSerializerOptions.ConfigurationSignature };

        Assert.AreEqual("PRMT:CFG1-LE", PromptuariumSerializer.ToElement(new Plain(), options).MetaData.AsAsciiString());
        Assert.AreEqual("PRMT:CFG2-LE", PromptuariumSerializer.ToElement(new Plain { Count = 1 }, options).MetaData.AsAsciiString());
        Assert.AreEqual(1, PromptuariumSerializer.Deserialize<Plain>(PromptuariumSerializer.Serialize(new Plain { Count = 1 }, options), options).Count);
    }

    [TestMethod]
    public void Signature_OfAnApplication_IsWrittenAsItIs()
    {
        var options = new PromptuariumSerializerOptions { Signature = "PRMT:MYAPP1" };

        byte[] bytes = PromptuariumSerializer.Serialize(new Plain { Count = 3 }, options);

        Assert.AreEqual("PRMT:MYAPP1", Element.FromBytes(bytes).MetaData.AsAsciiString());
        Assert.AreEqual(3, PromptuariumSerializer.Deserialize<Plain>(bytes, options).Count);
    }

    [TestMethod]
    public void Reading_AcceptsBothVersionsOfTheKind()
    {
        Assert.AreEqual(5, PromptuariumSerializer.Deserialize<Plain>(Version2(Member("Count", 0x42, 5, 0, 0, 0))).Count);
        Assert.AreEqual(2.5, PromptuariumSerializer.Deserialize<Plain>(Nodes.Root(Nodes.Member("Rate", 0x63, Data.FromDouble(2.5)))).Rate);
    }

    [TestMethod]
    public void Reading_AFileOfVersion1WithATypeOfVersion2_IsRejected()
    {
        Element root = Nodes.Root(Member("Count", 0x42, 5, 0, 0, 0));

        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<Plain>(root));
    }

    [TestMethod]
    public void Reading_AFileOfVersion1WithANaN_IsRejected()
    {
        Element version1 = Nodes.Root(Nodes.Member("Rate", 0x63, Data.FromDouble(double.NaN)));
        Element version2 = Version2(Nodes.Member("Rate", 0x63, Data.FromDouble(double.NaN)));

        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<Plain>(version1));
        Assert.IsTrue(double.IsNaN(PromptuariumSerializer.Deserialize<Plain>(version2).Rate));
    }

    // ---------------------------------------------------------------- reading rules

    public sealed class Targets
    {
        public int Int { get; set; }
        public byte Byte { get; set; }
        public double Double { get; set; }
        public float Float { get; set; }
        public Half Half { get; set; }
        public decimal Decimal { get; set; }
        public BigInteger Big { get; set; }
        public Guid Id { get; set; }
        public DateTime When { get; set; }
        public DateOnly Day { get; set; }
    }

    [TestMethod]
    public void Integer_IsReadIntoAnyIntegerTypeItFits()
    {
        Element root = Version2(Member("Int", 0x43, 7, 0, 0, 0, 0, 0, 0, 0), Member("Byte", 0x42, 255, 0, 0, 0), Member("Big", 0x40, 0xFE));

        Targets targets = PromptuariumSerializer.Deserialize<Targets>(root);

        Assert.AreEqual(7, targets.Int);
        Assert.AreEqual(255, targets.Byte);
        Assert.AreEqual(new BigInteger(-2), targets.Big);
    }

    [TestMethod]
    public void Integer_ThatDoesNotFit_IsRejected()
    {
        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<Targets>(Version2(Member("Byte", 0x41, 0x00, 0x01))));
        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<Targets>(Version2(Member("Byte", 0x40, 0xFF))));
    }

    [TestMethod]
    public void Integer_IsReadIntoAFloatingPointTypeOnlyIfHeldExactly()
    {
        byte[] exact = BitConverter.GetBytes(1L << 53);
        byte[] inexact = BitConverter.GetBytes((1L << 53) + 1);

        Assert.AreEqual(9007199254740992.0, PromptuariumSerializer.Deserialize<Targets>(Version2(Member("Double", 0x43, exact))).Double);
        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<Targets>(Version2(Member("Double", 0x43, inexact))));
        Assert.AreEqual(12m, PromptuariumSerializer.Deserialize<Targets>(Version2(Member("Decimal", 0x42, 12, 0, 0, 0))).Decimal);
    }

    [TestMethod]
    public void Float_IsReadIntoASmallerTypeOnlyIfHeldExactly()
    {
        Assert.AreEqual(0.5f, PromptuariumSerializer.Deserialize<Targets>(Version2(Nodes.Member("Float", 0x63, Data.FromDouble(0.5)))).Float);
        Assert.AreEqual((Half)1.5, PromptuariumSerializer.Deserialize<Targets>(Version2(Nodes.Member("Half", 0x63, Data.FromDouble(1.5)))).Half);
        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<Targets>(Version2(Nodes.Member("Float", 0x63, Data.FromDouble(0.1)))));
        Assert.AreEqual((double)0.1f, PromptuariumSerializer.Deserialize<Targets>(Version2(Nodes.Member("Double", 0x62, Data.FromFloat(0.1f)))).Double);
    }

    [TestMethod]
    public void Float_IsReadIntoAnIntegerOnlyIfWhole()
    {
        Assert.AreEqual(3, PromptuariumSerializer.Deserialize<Targets>(Version2(Nodes.Member("Int", 0x62, Data.FromFloat(3f)))).Int);
        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<Targets>(Version2(Nodes.Member("Int", 0x63, Data.FromDouble(double.NaN)))));
    }

    [TestMethod]
    public void TextOfVersion1_IsStillReadIntoTheDatesAndTheUuids()
    {
        Element root = Nodes.Root(
            Nodes.Member("Id", 0x80, Data.FromUtf8String("01234567-89ab-cdef-0123-456789abcdef")),
            Nodes.Member("When", 0x80, Data.FromUtf8String("2026-10-28T19:30:15.0000000Z")),
            Nodes.Member("Day", 0x80, Data.FromUtf8String("2026-10-28")),
            Nodes.Member("Decimal", 0x80, Data.FromUtf8String("12.5")));

        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<Targets>(root), "a decimal is not read from a text");

        root.Remove(root.Children[^1]);
        Targets targets = PromptuariumSerializer.Deserialize<Targets>(root);

        Assert.AreEqual(Guid.Parse("01234567-89ab-cdef-0123-456789abcdef"), targets.Id);
        Assert.AreEqual(new DateTime(2026, 10, 28, 19, 30, 15, DateTimeKind.Utc), targets.When);
        Assert.AreEqual(new DateOnly(2026, 10, 28), targets.Day);
    }

    // ---------------------------------------------------------------- canonical forms

    [TestMethod]
    [DataRow("7f 00", DisplayName = "a needless 00")]
    [DataRow("ff ff", DisplayName = "a needless ff")]
    [DataRow("", DisplayName = "empty")]
    public void BigInt_NotInItsShortestForm_IsRejected(string hex)
    {
        Element root = Version2(Member("Big", 0x78, Nodes.Hex(hex)));

        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<Targets>(root));
    }

    [TestMethod]
    public void BigInt_IsWrittenInItsShortestForm()
    {
        foreach ((BigInteger value, string hex) in new (BigInteger, string)[] { (0, "00"), (127, "7f"), (128, "80 00"), (-1, "ff"), (-128, "80"), (-129, "7f ff") })
        {
            Element root = PromptuariumSerializer.ToElement(new Targets { Big = value });
            Element big = root.Children.Single(child => child.Data.AsUtf8String() == "Big");

            CollectionAssert.AreEqual(Nodes.Hex(hex), big.Children[1].Data.ToArray(), value.ToString(CultureInfo.InvariantCulture));
        }
    }

    [TestMethod]
    [DataRow("012.5")]
    [DataRow("+1")]
    [DataRow("1e5")]
    [DataRow("1.")]
    [DataRow(".5")]
    [DataRow("-0")]
    [DataRow("-0.00")]
    [DataRow("1 000")]
    [DataRow("0.12345678901234567890123456789012", DisplayName = "more digits than a decimal holds")]
    public void Decimal_NotCanonicalOrNotHeld_IsRejected(string text)
    {
        Element root = Version2(Nodes.Member("Decimal", 0x70, Data.FromUtf8String(text)));

        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<Targets>(root));
    }

    [TestMethod]
    [DataRow("0")]
    [DataRow("0.00")]
    [DataRow("-12.750")]
    [DataRow("79228162514264337593543950335")]
    public void Decimal_KeepsItsScale(string text)
    {
        decimal value = decimal.Parse(text, CultureInfo.InvariantCulture);
        Element root = PromptuariumSerializer.ToElement(new Targets { Decimal = value });

        Assert.AreEqual(text, root.Children.Single(child => child.Data.AsUtf8String() == "Decimal").Children[1].Data.AsUtf8String());
        Assert.AreEqual(text, PromptuariumSerializer.Deserialize<Targets>(root).Decimal.ToString(CultureInfo.InvariantCulture));
    }

    [TestMethod]
    public void NaN_IsWrittenCanonical()
    {
        float payload = BitConverter.UInt32BitsToSingle(0x7FC00123);
        Element root = PromptuariumSerializer.ToElement(new Targets { Float = payload, Double = -double.NaN });

        CollectionAssert.AreEqual(Nodes.Hex("00 00 c0 7f"), root.Children.Single(child => child.Data.AsUtf8String() == "Float").Children[1].Data.ToArray());
        CollectionAssert.AreEqual(Nodes.Hex("00 00 00 00 00 00 f8 7f"), root.Children.Single(child => child.Data.AsUtf8String() == "Double").Children[1].Data.ToArray());
    }

    // ---------------------------------------------------------------- dates and times

    public sealed class Moments
    {
        public DateTime When { get; set; }
        public TimeOnly Time { get; set; }
        public DateTimeOffset At { get; set; }
    }

    [TestMethod]
    public void DateTime_KeepsItsKind()
    {
        var utc = new DateTime(2026, 11, 22, 18, 30, 15, DateTimeKind.Utc);
        var local = new DateTime(2026, 11, 22, 19, 30, 15, DateTimeKind.Local);
        var unspecified = new DateTime(2026, 11, 22, 19, 30, 15, DateTimeKind.Unspecified);

        Moments RoundTrip(DateTime when) => PromptuariumSerializer.Deserialize<Moments>(PromptuariumSerializer.Serialize(new Moments { When = when }));

        Assert.AreEqual(0xD3, TypeOf(PromptuariumSerializer.ToElement(new Moments { When = utc }), "When"));
        Assert.AreEqual(0xD2, TypeOf(PromptuariumSerializer.ToElement(new Moments { When = unspecified }), "When"));
        Assert.AreEqual(DateTimeKind.Utc, RoundTrip(utc).When.Kind);
        Assert.AreEqual(utc, RoundTrip(utc).When);
        Assert.AreEqual(DateTimeKind.Unspecified, RoundTrip(unspecified).When.Kind);
        Assert.AreEqual(unspecified, RoundTrip(unspecified).When);
        Assert.AreEqual(local.ToUniversalTime(), RoundTrip(local).When.ToUniversalTime());
    }

    [TestMethod]
    public void Time_OutOfADay_IsRejected()
    {
        Element root = Version2(Member("Time", 0xD1, BitConverter.GetBytes(TimeSpan.TicksPerDay)));

        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<Moments>(root));
    }

    [TestMethod]
    public void Timestamp_WithAnOffsetOutOfRange_IsRejected()
    {
        byte[] value = [.. BitConverter.GetBytes(0L), .. BitConverter.GetBytes((short)900)];

        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<Moments>(Version2(Member("At", 0xD3, value))));
    }

    [TestMethod]
    public void Values_OfAWrongSize_AreRejected()
    {
        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<Targets>(Version2(Member("Int", 0x42, 1, 0, 0))));
        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<Targets>(Version2(Member("Id", 0x90, 1, 2, 3))));
    }

    // ---------------------------------------------------------------- declared forms

    public sealed class Version1Forms
    {
        [PromptuariumType(PromptuariumType.Float64)]
        public int Count { get; set; } = 3;

        [PromptuariumType(PromptuariumType.Utf8String)]
        public Guid Id { get; set; } = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef");

        [PromptuariumType(PromptuariumType.Utf8String)]
        public DateTimeOffset At { get; set; } = new(2026, 11, 22, 19, 30, 15, TimeSpan.FromHours(1));

        [PromptuariumType(PromptuariumType.Float64)]
        public float Ratio { get; set; } = 0.5f;
    }

    [TestMethod]
    public void DeclaredForms_OfVersion1_GiveAFileOfVersion1()
    {
        var forms = new Version1Forms();

        Element root = PromptuariumSerializer.ToElement(forms);
        Version1Forms copy = PromptuariumSerializer.Deserialize<Version1Forms>(root);

        Assert.AreEqual("PRMT:GEN1-LE", root.MetaData.AsAsciiString());
        Assert.AreEqual(0x63, TypeOf(root, "Count"));
        Assert.AreEqual(0x80, TypeOf(root, "Id"));
        Assert.AreEqual(0x80, TypeOf(root, "At"));
        Assert.AreEqual(0x63, TypeOf(root, "Ratio"));
        Assert.AreEqual(forms.Count, copy.Count);
        Assert.AreEqual(forms.Id, copy.Id);
        Assert.AreEqual(forms.At, copy.At);
        Assert.AreEqual(forms.Ratio, copy.Ratio);
    }

    public sealed class Widened
    {
        [PromptuariumType(PromptuariumType.Int64)]
        public int Count { get; set; }

        [PromptuariumType(PromptuariumType.BigInt)]
        public long Big { get; set; }
    }

    public sealed class Narrowed
    {
        [PromptuariumType(PromptuariumType.Int8)]
        public int Count { get; set; }
    }

    public sealed class Single32
    {
        [PromptuariumType(PromptuariumType.Float32)]
        public double Value { get; set; }
    }

    [TestMethod]
    public void DeclaredIntegerType_IsWrittenIfTheValueFits()
    {
        Element root = PromptuariumSerializer.ToElement(new Widened { Count = 5, Big = -2 });

        Assert.AreEqual(0x43, TypeOf(root, "Count"));
        Assert.AreEqual(0x78, TypeOf(root, "Big"));
        Assert.AreEqual(5, PromptuariumSerializer.Deserialize<Widened>(root).Count);
        Assert.AreEqual(-2L, PromptuariumSerializer.Deserialize<Widened>(root).Big);
        Assert.ThrowsExactly<NotSupportedException>(() => PromptuariumSerializer.Serialize(new Narrowed { Count = 300 }));
    }

    [TestMethod]
    public void DeclaredFloatType_IsWrittenIfItHoldsTheValueExactly()
    {
        Assert.AreEqual(0.5, PromptuariumSerializer.Deserialize<Single32>(PromptuariumSerializer.Serialize(new Single32 { Value = 0.5 })).Value);
        Assert.ThrowsExactly<NotSupportedException>(() => PromptuariumSerializer.Serialize(new Single32 { Value = 0.1 }));
    }

    public sealed class LongAsFloat64
    {
        [PromptuariumType(PromptuariumType.Float64)]
        public long Value { get; set; }
    }

    public sealed class ULongAsFloat64
    {
        [PromptuariumType(PromptuariumType.Float64)]
        public ulong Value { get; set; }
    }

    public sealed class DecimalAsFloat64
    {
        [PromptuariumType(PromptuariumType.Float64)]
        public decimal Value { get; set; }
    }

    [TestMethod]
    [DataRow((1L << 53) + 1)]
    [DataRow(-(1L << 53) - 1)]
    [DataRow(long.MaxValue)]
    [DataRow(long.MinValue + 1)]
    public void LongDeclaredAsFloat64_NotHeldExactly_IsRejected(long value)
    {
        Assert.ThrowsExactly<NotSupportedException>(() => PromptuariumSerializer.Serialize(new LongAsFloat64 { Value = value }));
    }

    [TestMethod]
    [DataRow(1L << 53)]
    [DataRow(1L << 62)]
    [DataRow(long.MinValue)]
    public void LongDeclaredAsFloat64_HeldExactly_RoundTrips(long value)
    {
        Assert.AreEqual(value, PromptuariumSerializer.Deserialize<LongAsFloat64>(PromptuariumSerializer.Serialize(new LongAsFloat64 { Value = value })).Value);
    }

    [TestMethod]
    [DataRow((1UL << 53) + 1)]
    [DataRow(ulong.MaxValue)]
    public void ULongDeclaredAsFloat64_NotHeldExactly_IsRejected(ulong value)
    {
        Assert.ThrowsExactly<NotSupportedException>(() => PromptuariumSerializer.Serialize(new ULongAsFloat64 { Value = value }));
    }

    [TestMethod]
    [DataRow("12345678901234567.25")]
    [DataRow("0.1234567890123456789")]
    [DataRow("79228162514264337593543950335")]
    public void DecimalDeclaredAsFloat64_NotHeldExactly_IsRejected(string text)
    {
        decimal value = decimal.Parse(text, CultureInfo.InvariantCulture);

        Assert.ThrowsExactly<NotSupportedException>(() => PromptuariumSerializer.Serialize(new DecimalAsFloat64 { Value = value }));
    }

    [TestMethod]
    [DataRow("0.1")]
    [DataRow("-123456789.123456")]
    public void DecimalDeclaredAsFloat64_HeldExactly_RoundTrips(string text)
    {
        decimal value = decimal.Parse(text, CultureInfo.InvariantCulture);

        Assert.AreEqual(value, PromptuariumSerializer.Deserialize<DecimalAsFloat64>(PromptuariumSerializer.Serialize(new DecimalAsFloat64 { Value = value })).Value);
    }

    [TestMethod]
    public void WiderTypes_RoundTrip()
    {
        var values = new Dictionary<string, object>
        {
            ["int128"] = Int128.MaxValue,
            ["uint128"] = UInt128.MaxValue,
            ["nint"] = (nint)(-7),
            ["half"] = Half.MaxValue,
        };

        var copy = PromptuariumSerializer.Deserialize<Dictionary<string, object?>>(PromptuariumSerializer.Serialize(values));

        Assert.AreEqual(Int128.MaxValue, copy["int128"]);
        Assert.AreEqual(UInt128.MaxValue, copy["uint128"]);
        Assert.AreEqual(-7L, copy["nint"]);
        Assert.AreEqual(Half.MaxValue, copy["half"]);
    }
}
