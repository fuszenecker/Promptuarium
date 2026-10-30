using Promptuarium;
using Promptuarium.Serializer;

namespace Promptuarium.Serializer.Tests;

/// <summary>The serializer against SERIALIZER_FORMAT.md: the test vectors and the rules a reader enforces.</summary>
[TestClass]
public sealed class FormatTests
{
    // SERIALIZER_FORMAT.md, section 7
    private const string TestVector = """
        6c 50 52 4d 54 3a 43 46 47 31 2d 4c 45 44 6b 65
        79 31 21 00 41 80 21 01 c6 76 61 6c 75 65 31 21
        03 84 6b 65 79 32 21 00 41 63 21 01 c8 00 00 00
        00 00 00 f8 3f 21 03 84 6b 65 79 33 21 00 41 01
        21 01 c1 01 21 03 84 6b 65 79 34 21 00 41 21 21
        01 e1 02 41 80 21 01 c7 76 61 6c 75 65 34 31 21
        03 a1 02 41 80 21 01 c7 76 61 6c 75 65 34 32 21
        03 80 84 6b 65 79 35 21 00 41 20 21 01 c5 6b 65
        79 35 31 21 00 41 80 21 01 c7 76 61 6c 75 65 35
        31 21 03 80 84 6b 65 79 36 21 00 41 00 21 01 00
        """;

    // The BLOB member key7 of section 7, inserted before the final terminator
    private const string BlobMember = "84 6b 65 79 37 21 00 41 f0 21 01 c4 de ad be ef 21 03";

    // The test vectors are configuration files
    private static readonly PromptuariumSerializerOptions Configuration = new() { Signature = PromptuariumSerializerOptions.ConfigurationSignature };

    private static Dictionary<string, object?> Example() => new()
    {
        ["key1"] = "value1",
        ["key2"] = 1.5,
        ["key3"] = true,
        ["key4"] = new List<object?> { "value41", "value42" },
        ["key5"] = new Dictionary<string, object?> { ["key51"] = "value51" },
        ["key6"] = null,
    };

    [TestMethod]
    public void Serialize_SpecExample_IsTheTestVector()
    {
        CollectionAssert.AreEqual(Nodes.Hex(TestVector), PromptuariumSerializer.Serialize(Example(), Configuration));
    }

    [TestMethod]
    public void Serialize_SpecExampleWithBlob_InsertsTheBlobMemberBeforeTheTerminator()
    {
        Dictionary<string, object?> example = Example();
        example["key7"] = new byte[] { 0xde, 0xad, 0xbe, 0xef };

        byte[] expected = [.. Nodes.Hex(TestVector)[..^1], .. Nodes.Hex(BlobMember), 0x00];

        byte[] actual = PromptuariumSerializer.Serialize(example, Configuration);

        Assert.AreEqual(178, actual.Length);
        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void Deserialize_TestVector_IsTheSpecExample()
    {
        var decoded = PromptuariumSerializer.Deserialize<Dictionary<string, object?>>(Nodes.Hex(TestVector), Configuration);

        CollectionAssert.AreEqual(new[] { "key1", "key2", "key3", "key4", "key5", "key6" }, decoded.Keys.ToArray());
        Assert.AreEqual("value1", decoded["key1"]);
        Assert.AreEqual(1.5, decoded["key2"]);
        Assert.AreEqual(true, decoded["key3"]);
        CollectionAssert.AreEqual(new List<object?> { "value41", "value42" }, (List<object?>)decoded["key4"]!);
        Assert.AreEqual("value51", ((Dictionary<string, object?>)decoded["key5"]!)["key51"]);
        Assert.IsNull(decoded["key6"]);
    }

    [TestMethod]
    public void Serialize_SpecExampleAsAGeneralObject_DiffersInTheSignatureOnly()
    {
        byte[] expected = Nodes.Hex(TestVector);
        "GEN"u8.CopyTo(expected.AsSpan(6));

        CollectionAssert.AreEqual(expected, PromptuariumSerializer.Serialize(Example()));
    }

    [TestMethod]
    public void Serialize_StartsWithTheSignature()
    {
        byte[] bytes = PromptuariumSerializer.Serialize(new Dictionary<string, object>());

        // PRMT:GEN1-LE
        CollectionAssert.AreEqual(Nodes.Hex("6c 50 52 4d 54 3a 47 45 4e 31 2d 4c 45"), bytes[..13]);
    }

    [TestMethod]
    public void Deserialize_ConfigurationFile_IsRejectedByDefault()
    {
        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<object>(Nodes.Hex(TestVector)));
    }

    [TestMethod]
    public void Deserialize_OtherSignature_IsRejected()
    {
        Element root = new ElementBuilder().WithSignature(Data.FromAsciiString("PRMT:CFG1")).Build();

        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<object>(root));
    }

    [TestMethod]
    public void Deserialize_RootWithData_IsRejected()
    {
        var root = new Element(Data.FromByte(1), Data.FromAsciiString(PromptuariumSerializerOptions.DefaultSignature));

        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<object>(root));
    }

    [TestMethod]
    public void Deserialize_NotAPromptuariumTree_IsRejected()
    {
        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<object>("not a tree"u8.ToArray()));
    }

    [TestMethod]
    [DataRow((byte)0x40, DisplayName = "INT8 (reserved)")]
    [DataRow((byte)0x42, DisplayName = "INT32 (reserved)")]
    [DataRow((byte)0x62, DisplayName = "FLOAT32 (reserved)")]
    [DataRow((byte)0x7f, DisplayName = "unknown")]
    public void Deserialize_ReservedOrUnknownType_IsRejected(byte type)
    {
        Element root = Nodes.Root(Nodes.Member("key", type, Data.FromInt(1)));

        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<object>(root));
    }

    [TestMethod]
    public void Deserialize_UnknownMetadataTag_IsRejected()
    {
        Element member = Nodes.Member("key", 0x80, Data.FromUtf8String("value"));
        member.MetaData = Data.FromByte(0x04);

        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<object>(Nodes.Root(member)));
    }

    [TestMethod]
    public void Deserialize_HolderWithoutType_IsRejected()
    {
        var member = new Element(Data.FromUtf8String("key"), Data.FromByte(0x00));

        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<object>(Nodes.Root(member)));
    }

    [TestMethod]
    public void Deserialize_ValueOnNull_IsRejected()
    {
        Element member = Nodes.Member("key", 0x00, Data.FromByte(0));

        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<object>(Nodes.Root(member)));
    }

    [TestMethod]
    [DataRow(new byte[] { 0x02 }, DisplayName = "not 0 or 1")]
    [DataRow(new byte[] { 0x00, 0x00 }, DisplayName = "two bytes")]
    public void Deserialize_InvalidBoolean_IsRejected(byte[] value)
    {
        Element root = Nodes.Root(Nodes.Member("key", 0x01, value));

        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<object>(root));
    }

    [TestMethod]
    [DataRow(double.NaN)]
    [DataRow(double.PositiveInfinity)]
    public void Deserialize_NonFiniteFloat_IsRejected(double value)
    {
        Element root = Nodes.Root(Nodes.Member("key", 0x63, Data.FromDouble(value)));

        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<object>(root));
    }

    [TestMethod]
    public void Deserialize_FloatOfWrongLength_IsRejected()
    {
        Element root = Nodes.Root(Nodes.Member("key", 0x63, Data.FromFloat(1)));

        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<object>(root));
    }

    [TestMethod]
    public void Deserialize_InvalidUtf8_IsRejected()
    {
        Element root = Nodes.Root(Nodes.Member("key", 0x80, new byte[] { 0xff, 0xfe }));

        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<object>(root));
    }

    [TestMethod]
    public void Deserialize_ItemWithData_IsRejected()
    {
        Element item = Nodes.Holder(0x02, Data.FromUtf8String("name"), 0x80, Data.FromUtf8String("value"));
        Element array = Nodes.Member("key", 0x21);
        array.Add(item);

        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<object>(Nodes.Root(array)));
    }

    [TestMethod]
    public void Deserialize_DuplicateNames_LastOneWins()
    {
        Element root = Nodes.Root(
            Nodes.Member("key", 0x80, Data.FromUtf8String("first")),
            Nodes.Member("key", 0x80, Data.FromUtf8String("second")));

        Assert.AreEqual("second", PromptuariumSerializer.Deserialize<Dictionary<string, object?>>(root)["key"]);
    }

    [TestMethod]
    public void Blob_EmptyIsStoredAndRead()
    {
        byte[] bytes = PromptuariumSerializer.Serialize(new Dictionary<string, object> { ["key"] = Array.Empty<byte>() });

        Element holder = Element.FromBytes(bytes).Children[0];
        Assert.AreEqual((byte)0xF0, holder.Children[0].Data.Span[0]);
        Assert.AreEqual(0, ((byte[])PromptuariumSerializer.Deserialize<Dictionary<string, object?>>(bytes)["key"]!).Length);
    }
}
