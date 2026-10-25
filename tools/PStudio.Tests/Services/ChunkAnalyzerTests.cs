using PStudio.Services;
using Promptuarium;

namespace PStudio.Tests.Services;

[TestClass]
public sealed class ChunkAnalyzerTests
{
    [TestMethod]
    public void Analyze_ListsTheChunksOfTheSavedTree()
    {
        // See BINARY_FORMAT.md, 10.2: a signed root with one child
        var root = new ElementBuilder()
            .WithSignature(Data.FromAsciiString("PRMT"))
            .WithChildren(new Element(Data.FromAsciiString("a"), null))
            .Build();

        var chunks = new ChunkAnalyzer().Analyze(root);

        CollectionAssert.AreEqual(new[] { "64", "41", "00" }, chunks.Select(chunk => chunk.Control).ToArray());
        Assert.AreEqual("Down", chunks[0].Direction);
        Assert.AreEqual("metadata", chunks[0].ContentType);
        Assert.AreEqual("\"PRMT\"", chunks[0].Payload);
        Assert.AreEqual("Down", chunks[1].Direction);
        Assert.AreEqual(1, chunks[1].Depth);
        Assert.AreEqual("000005", chunks[1].Offset);
        Assert.AreEqual("terminator", chunks[2].Kind);
    }

    [TestMethod]
    public void Analyze_ShowsLogarithmicLengths()
    {
        var chunks = new ChunkAnalyzer().Analyze(new Element(new byte[16], null));

        Assert.AreEqual("50", chunks[0].Control);
        Assert.AreEqual("16 (log)", chunks[0].Length);
    }

    [TestMethod]
    public void Analyze_TellsTheNodeOfEveryChunk()
    {
        // root (no content) > a > (b > c), d: the root is a placeholder, and Up markers lead back from c to d
        var root = new Element(null, null,
            new Element(Data.FromAsciiString("a"), null,
                new Element(Data.FromAsciiString("b"), null,
                    new Element(Data.FromAsciiString("c"), null))),
            new Element(Data.FromAsciiString("d"), new byte[20]));

        var chunks = new ChunkAnalyzer().Analyze(root);

        // Pre-order: root 0, a 1, b 2, c 3, d 4; the metadata of d is two chunks (16 + 4 bytes)
        Assert.AreEqual(
            "40:0 41:1 41:2 41:3 80:-1 81:4 30:4 24:4 00:-1",
            string.Join(" ", chunks.Select(chunk => $"{chunk.Control}:{chunk.NodeIndex}")));
        Assert.AreEqual(chunks[^1].Position + chunks[^1].Size, root.ToBytes().Length);
    }
}
