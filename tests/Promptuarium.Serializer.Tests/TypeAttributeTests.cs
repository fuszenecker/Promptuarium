// The type control is experimental: a caller opts in like this
#pragma warning disable PRMTSER001

using Promptuarium;
using Promptuarium.Serializer;

namespace Promptuarium.Serializer.Tests;

/// <summary>The experimental <see cref="PromptuariumTypeAttribute"/>.</summary>
[TestClass]
public sealed class TypeAttributeTests
{
    public sealed class Declared
    {
        [PromptuariumType(PromptuariumType.Float64)]
        public int Count { get; set; }

        [PromptuariumType(PromptuariumType.Blob)]
        public byte[]? Payload { get; set; }

        [PromptuariumType(PromptuariumType.Utf8String)]
        public string? Name { get; set; }

        [PromptuariumType(PromptuariumType.Float64)]
        public object? Anything { get; set; }
    }

    public sealed class Reserved
    {
        [PromptuariumType(PromptuariumType.Int64)]
        public long Ticks { get; set; }
    }

    public sealed class Mismatch
    {
        [PromptuariumType(PromptuariumType.Boolean)]
        public string Text { get; set; } = "not a boolean";
    }

    [TestMethod]
    public void TypeCodes_AreTheOnesOfTheFormat()
    {
        Assert.AreEqual(0x43, (byte)PromptuariumType.Int64);
        Assert.AreEqual(0x63, (byte)PromptuariumType.Float64);
        Assert.AreEqual(0x80, (byte)PromptuariumType.Utf8String);
        Assert.AreEqual(0xF0, (byte)PromptuariumType.Blob);
    }

    [TestMethod]
    public void Declared_TypesThatFit_AreWrittenAndRead()
    {
        byte[] bytes = PromptuariumSerializer.Serialize(new Declared { Count = 3, Payload = [1, 2], Name = "x" });

        Declared copy = PromptuariumSerializer.Deserialize<Declared>(bytes);

        Assert.AreEqual(3, copy.Count);
        CollectionAssert.AreEqual(new byte[] { 1, 2 }, copy.Payload);
        Assert.AreEqual("x", copy.Name);
    }

    [TestMethod]
    public void Declared_NullIsAlwaysAllowed()
    {
        byte[] bytes = PromptuariumSerializer.Serialize(new Declared { Payload = null, Name = null });

        Assert.IsNull(PromptuariumSerializer.Deserialize<Declared>(bytes).Payload);
    }

    [TestMethod]
    public void Declared_ReservedType_IsRejectedWhenWriting()
    {
        var exception = Assert.ThrowsExactly<NotSupportedException>(() => PromptuariumSerializer.Serialize(new Reserved { Ticks = 1 }));

        StringAssert.Contains(exception.Message, "Int64");
    }

    [TestMethod]
    public void Declared_TypeTheValueDoesNotMapTo_IsRejectedWhenWriting()
    {
        Assert.ThrowsExactly<NotSupportedException>(() => PromptuariumSerializer.Serialize(new Mismatch()));
    }

    [TestMethod]
    public void Declared_OtherTypeInTheFile_IsRejectedWhenReading()
    {
        // An object member takes any value, so only the declaration rejects the string
        Element untyped = Nodes.Root(Nodes.Member("Anything", 0x80, Data.FromUtf8String("text")));
        Element declared = Nodes.Root(Nodes.Member("Anything", 0x63, Data.FromDouble(2.5)));

        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<Declared>(untyped));
        Assert.AreEqual(2.5, PromptuariumSerializer.Deserialize<Declared>(declared).Anything);
    }
}
