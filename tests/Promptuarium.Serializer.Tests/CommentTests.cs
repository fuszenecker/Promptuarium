using Promptuarium;
using Promptuarium.Serializer;

namespace Promptuarium.Serializer.Tests;

/// <summary>COMMENT nodes: written from PromptuariumCommentAttribute, validated and ignored when read.</summary>
[TestClass]
public sealed class CommentTests
{
    // SERIALIZER_FORMAT.md, section 7: the member key8 with a comment, inserted before the final terminator
    private const string CommentedMember = """
        84 6b 65 79 38 21 00 41 80 21 01 ce 74 68 65 20
        65 69 67 68 74 68 20 6b 65 79 21 04 c6 76 61 6c
        75 65 38 21 03
        """;

    [PromptuariumComment("Audio settings")]
    public sealed class Audio
    {
        [PromptuariumComment("In Hz")]
        public int Rate { get; set; } = 8000;

        public Device Output { get; set; } = new();

        [PromptuariumComment("The microphone")]
        public Device Input { get; set; } = new();

        public List<Device> Others { get; set; } = [new()];
    }

    [PromptuariumComment("A sound device")]
    public sealed class Device
    {
        public string Name { get; set; } = "default";
    }

    [TestMethod]
    public void Comments_FollowTheTypeNode()
    {
        Element root = PromptuariumSerializer.ToElement(new Audio());

        Assert.AreEqual("Audio settings", Comment(root.Children[0]), "the comment of the root type is the first child of the root");

        Element rate = root.Children[1];
        CollectionAssert.AreEqual(new byte[] { 0x01, 0x04, 0x03 }, Tags(rate));
        Assert.AreEqual("In Hz", Comment(rate.Children[1]));

        Assert.AreEqual("A sound device", Comment(root.Children[2].Children[1]), "an object gets the comment of its type");
        Assert.AreEqual("The microphone", Comment(root.Children[3].Children[1]), "the comment of the property wins");
        Assert.AreEqual(1, root.Children[3].Children.Count(child => child.MetaData.Span[0] == 0x04));

        Element item = root.Children[4].Children[1];
        Assert.AreEqual("A sound device", Comment(item.Children[1]), "an object in an array, too");
    }

    [TestMethod]
    public void Comments_AreIgnoredWhenReading()
    {
        byte[] bytes = PromptuariumSerializer.Serialize(new Audio { Rate = 44100 });

        Audio copy = PromptuariumSerializer.Deserialize<Audio>(bytes);
        var untyped = PromptuariumSerializer.Deserialize<Dictionary<string, object?>>(bytes);

        Assert.AreEqual(44100, copy.Rate);
        Assert.AreEqual("default", copy.Others.Single().Name);
        CollectionAssert.AreEqual(new[] { "Rate", "Output", "Input", "Others" }, untyped.Keys.ToArray());
        Assert.AreEqual(1, ((List<object?>)untyped["Others"]!).Count, "a comment is not an element");
    }

    [TestMethod]
    public void TheSpecExampleWithAComment_IsTheTestVector()
    {
        Element member = new(Data.FromUtf8String("key8"), Data.FromByte(0x00),
            new Element(Data.FromByte(0x80), Data.FromByte(0x01)),
            new Element(Data.FromUtf8String("the eighth key"), Data.FromByte(0x04)),
            new Element(Data.FromUtf8String("value8"), Data.FromByte(0x03)));
        var options = new PromptuariumSerializerOptions { Signature = PromptuariumSerializerOptions.ConfigurationSignature };
        Element example = PromptuariumSerializer.ToElement(FormatTests.Example(), options);
        example.Add(member);

        byte[] vector = PromptuariumSerializer.Serialize(FormatTests.Example(), options);
        byte[] expected = [.. vector[..^1], .. Nodes.Hex(CommentedMember), 0x00];

        CollectionAssert.AreEqual(expected, example.ToBytes());
        Assert.AreEqual(197, expected.Length);
        Assert.AreEqual("value8", PromptuariumSerializer.Deserialize<Dictionary<string, object?>>(expected, options)["key8"]);
    }

    [TestMethod]
    public void Comments_AreAllowedAnywhereButFirst()
    {
        Element array = Nodes.Holder(0x00, Data.FromUtf8String("list"), 0x21);
        array.Add(CommentNode("before"), Nodes.Holder(0x02, ReadOnlyMemory<byte>.Empty, 0x80, Data.FromUtf8String("a")), CommentNode("after"));
        Element nothing = Nodes.Member("nothing", 0x00);
        nothing.Add(CommentNode("no value"));
        Element text = Nodes.Member("text", 0x80, Data.FromUtf8String("t"));
        text.Add(CommentNode("after the value"));
        Element root = Nodes.Root(array, nothing, text, CommentNode("at the end of the file"));

        var read = PromptuariumSerializer.Deserialize<Dictionary<string, object?>>(root);

        CollectionAssert.AreEqual(new List<object?> { "a" }, (List<object?>)read["list"]!);
        Assert.IsNull(read["nothing"]);
        Assert.AreEqual("t", read["text"]);
        Assert.AreEqual(3, read.Count);
    }

    [TestMethod]
    public void ACommentAsTheFirstChildOfAHolder_IsRejected()
    {
        var member = new Element(Data.FromUtf8String("x"), Data.FromByte(0x00), CommentNode("first"), new Element(Data.FromByte(0x00), Data.FromByte(0x01)));

        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<object>(Nodes.Root(member)));
    }

    [TestMethod]
    public void AnInvalidComment_IsRejected()
    {
        Element withChild = CommentNode("x");
        withChild.Add(new Element());
        var notUtf8 = new Element(new byte[] { 0xFF }, Data.FromByte(0x04));

        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<object>(Nodes.Root(withChild)));
        Assert.ThrowsExactly<InvalidDataException>(() => PromptuariumSerializer.Deserialize<object>(Nodes.Root(notUtf8)));
    }

    private static Element CommentNode(string text) => new(Data.FromUtf8String(text), Data.FromByte(0x04));

    private static string Comment(Element node)
    {
        Assert.AreEqual(0x04, node.MetaData.Span[0]);
        return node.Data.AsUtf8String();
    }

    private static byte[] Tags(Element holder) => [.. holder.Children.Select(child => child.MetaData.Span[0])];
}
