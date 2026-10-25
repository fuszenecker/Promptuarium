using PStudio.Services;
using Promptuarium;

namespace PStudio.Tests.Services;

[TestClass]
public sealed class NodeClipboardTests
{
    [TestMethod]
    public void Paste_GivesNewCopies_ThatDoNotFollowTheOriginal()
    {
        var clipboard = new NodeClipboard();
        var parent = new Element();
        var node = new Element(Data.FromAsciiString("a"), null, new Element(Data.FromAsciiString("b"), null));
        parent.Add(node);

        Assert.IsFalse(clipboard.HasNode);
        Assert.IsNull(clipboard.Paste());

        clipboard.Copy(node);
        node.Data = Data.FromAsciiString("changed");

        Element first = clipboard.Paste()!;
        Element second = clipboard.Paste()!;

        Assert.IsTrue(clipboard.HasNode);
        Assert.AreNotSame(first, second);
        Assert.IsNull(first.Parent);
        Assert.AreEqual("a", first.Data.AsAsciiString());
        Assert.AreEqual("b", first[0].Data.AsAsciiString());
    }
}
