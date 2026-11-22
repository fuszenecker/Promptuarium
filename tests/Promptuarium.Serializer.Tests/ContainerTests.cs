// The declared forms use the experimental type control
#pragma warning disable PRMTSER001

namespace Promptuarium.Serializer.Tests;

/// <summary>CONTAINER (SERIALIZER_FORMAT.md, 6.6): a nested tree, an Element.</summary>
[TestClass]
public sealed class ContainerTests
{
    // SERIALIZER_FORMAT.md, 9.2: {"a": "b"} as a general object of version 1, nested in the member "inner"
    private const string Inner = "6c 50 52 4d 54 3a 47 45 4e 31 2d 4c 45 41 61 21 00 41 80 21 01 c1 62 21 03 00";

    private const string TestVector = """
        6c 50 52 4d 54 3a 47 45 4e 32 2d 4c 45 45 69 6e
        6e 65 72 21 00 41 f1 21 01 d0 6c 50 52 4d 54 3a
        47 45 4e 31 2d 4c 45 41 61 21 0a 00 41 80 21 01
        c1 62 21 03 00 21 03 00
        """;

    public sealed class Outer
    {
        public Element? Inner { get; set; }
    }

    public sealed class Raw
    {
        [PromptuariumType(PromptuariumType.Container)]
        public byte[]? Inner { get; set; }
    }

    [TestMethod]
    public void TestVector_IsWrittenAndRead()
    {
        Element inner = PromptuariumSerializer.ToElement(new Dictionary<string, object?> { ["a"] = "b" });

        byte[] bytes = PromptuariumSerializer.Serialize(new Dictionary<string, object?> { ["inner"] = inner });

        CollectionAssert.AreEqual(Nodes.Hex(TestVector), bytes);

        Outer read = PromptuariumSerializer.Deserialize<Outer>(Nodes.Hex(TestVector));
        Assert.IsTrue(inner.StructurallyEquals(read.Inner));
        Assert.AreEqual("b", PromptuariumSerializer.Deserialize<Dictionary<string, string>>(read.Inner!)["a"]);
    }

    [TestMethod]
    public void AnyTree_RoundTrips_AndIsAnElementInAnObject()
    {
        var tree = new Element(Data.FromUtf8String("data"), Data.FromUtf8String("meta"),
            new Element(Data.FromInt(42), ReadOnlyMemory<byte>.Empty),
            new Element(ReadOnlyMemory<byte>.Empty, ReadOnlyMemory<byte>.Empty, new Element(Data.FromUtf8String("deep"), ReadOnlyMemory<byte>.Empty)));

        byte[] bytes = PromptuariumSerializer.Serialize(new Outer { Inner = tree });

        Assert.IsTrue(tree.StructurallyEquals(PromptuariumSerializer.Deserialize<Outer>(bytes).Inner));
        Assert.IsInstanceOfType<Element>(PromptuariumSerializer.Deserialize<Dictionary<string, object?>>(bytes)["Inner"]);
        Assert.IsTrue(new Element().StructurallyEquals(PromptuariumSerializer.Deserialize<Outer>(PromptuariumSerializer.Serialize(new Outer { Inner = new Element() })).Inner));
        Assert.IsNull(PromptuariumSerializer.Deserialize<Outer>(PromptuariumSerializer.Serialize(new Outer())).Inner);
    }

    [TestMethod]
    public void AFileWithAContainer_IsOfVersion2_WhateverTheNestedTree()
    {
        Element version1 = PromptuariumSerializer.ToElement(new Dictionary<string, object?> { ["a"] = "b" });

        Element root = PromptuariumSerializer.ToElement(new Outer { Inner = version1 });

        Assert.AreEqual("PRMT:GEN2-LE", root.MetaData.AsAsciiString());
        Assert.AreEqual("PRMT:GEN1-LE", version1.MetaData.AsAsciiString());
    }

    [TestMethod]
    public void ByteArray_DeclaredAsContainer_IsCheckedAndRoundTrips()
    {
        byte[] inner = Nodes.Hex(Inner);

        byte[] bytes = PromptuariumSerializer.Serialize(new Raw { Inner = inner });

        CollectionAssert.AreEqual(PromptuariumSerializer.Serialize(new Outer { Inner = Element.FromBytes(inner) }), bytes);
        CollectionAssert.AreEqual(inner, PromptuariumSerializer.Deserialize<Raw>(bytes).Inner);
        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Serialize(new Raw { Inner = inner[..^1] }));
        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Serialize(new Raw { Inner = [] }));
    }

    [TestMethod]
    [DataRow("", DisplayName = "empty")]
    [DataRow("6c 50 52 4d 54 3a 47 45 4e 31 2d 4c 45 41 61 21 00 41 80 21 01 c1 62 21 03", DisplayName = "without the terminator")]
    [DataRow("6c 50 52 4d 54 3a 47 45 4e 31 2d 4c 45 41 61 21 00 41 80 21 01 c1 62 21 03 00 00", DisplayName = "a byte after the terminator")]
    [DataRow("6c 50 52 4d", DisplayName = "ends inside a payload")]
    public void AValueThatIsNotOneCompleteStream_IsRejected(string value)
    {
        Element root = Version2(Nodes.Member("Inner", 0xF1, Nodes.Hex(value)));

        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<Outer>(root));
        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<Dictionary<string, object?>>(root));
    }

    [TestMethod]
    public void AContainer_InAFileOfVersion1_OrForAnotherType_IsRejected()
    {
        Element version1 = Nodes.Root(Nodes.Member("Inner", 0xF1, Nodes.Hex(Inner)));

        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<Outer>(version1));
        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<Dictionary<string, string>>(Version2(Nodes.Member("Inner", 0xF1, Nodes.Hex(Inner)))));
        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<Outer>(Version2(Nodes.Member("Inner", 0xF0, Nodes.Hex(Inner)))));
    }

    [TestMethod]
    public void TheLimits_CoverTheNestedTree()
    {
        var deep = new Element();
        Element leaf = deep;

        for (int i = 0; i < 10; i++)
        {
            var child = new Element(Data.FromInt(i), ReadOnlyMemory<byte>.Empty);
            leaf.Add(child);
            leaf = child;
        }

        byte[] bytes = PromptuariumSerializer.Serialize(new Outer { Inner = deep });

        // The member is at depth 1, the root of the nested tree at 2, its deepest node at 12
        Assert.IsNotNull(PromptuariumSerializer.Deserialize<Outer>(bytes, new PromptuariumSerializerOptions { MaxDepth = 12 }).Inner);
        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<Outer>(bytes, new PromptuariumSerializerOptions { MaxDepth = 11 }));
        Assert.ThrowsExactly<NotSupportedException>(() => PromptuariumSerializer.Serialize(new Outer { Inner = deep }, new PromptuariumSerializerOptions { MaxDepth = 11 }));

        Element root = PromptuariumSerializer.ToElement(new Outer { Inner = deep });
        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<Outer>(root, new PromptuariumSerializerOptions { LoadOptions = new LoadOptions { MaxNodes = 5 } }));
    }

    [TestMethod]
    public void AnElement_CanNotBeTheRoot_NorBeRegistered()
    {
        Assert.ThrowsExactly<NotSupportedException>(() => PromptuariumSerializer.ToElement(new Element()));
        Assert.ThrowsExactly<ArgumentException>(() => new PromptuariumSerializerOptions().AddBlobType<Element>(tree => tree.ToBytes(), bytes => Element.FromBytes(bytes)));
    }

    private static Element Version2(params Element[] members)
    {
        Element root = Nodes.Root(members);
        root.MetaData = Data.FromAsciiString("PRMT:GEN2-LE");
        return root;
    }
}
