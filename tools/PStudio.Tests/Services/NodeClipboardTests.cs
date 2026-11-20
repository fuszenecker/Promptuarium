using PStudio.Models;
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

    [TestMethod]
    public void PasteForms_GivesCopiesOfTheForms_OrNone()
    {
        var clipboard = new NodeClipboard();
        Assert.IsTrue(clipboard.PasteForms().IsEmpty);

        var forms = new FileForms();
        forms.Data["/"] = ContentForm.Hex;
        forms.MetaData["/0"] = ContentForm.Base64;
        clipboard.Copy(new Element(), forms);
        forms.Data["/"] = ContentForm.Utf8;

        FileForms first = clipboard.PasteForms();
        first.Data.Clear();
        FileForms second = clipboard.PasteForms();

        Assert.AreEqual(ContentForm.Hex, second.Data["/"]);
        Assert.AreEqual(ContentForm.Base64, second.MetaData["/0"]);

        // A node copied without forms has none
        clipboard.Copy(new Element());
        Assert.IsTrue(clipboard.PasteForms().IsEmpty);
    }
}
