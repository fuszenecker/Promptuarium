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
}
