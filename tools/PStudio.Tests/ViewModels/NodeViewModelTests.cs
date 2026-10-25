using PStudio.ViewModels;
using Promptuarium;

namespace PStudio.Tests.ViewModels;

[TestClass]
public sealed class NodeViewModelTests
{
    [TestMethod]
    public void Constructor_WrapsTheWholeTree()
    {
        var root = new NodeViewModel(new Element(null, null, new Element(null, null, new Element(), new Element())));

        Assert.IsTrue(root.IsRoot);
        Assert.IsTrue(root.IsExpanded);
        Assert.AreEqual("/", root.Path);
        Assert.AreEqual("/0/1", root.Children[0].Children[1].Path);
        Assert.IsFalse(root.Children[0].IsExpanded);
    }

    [TestMethod]
    public void Insert_Detach_Move_ChangeTheElementsToo()
    {
        var root = new NodeViewModel(new Element());
        var a = new NodeViewModel(new Element(Data.FromAsciiString("a"), null));
        var b = new NodeViewModel(new Element(Data.FromAsciiString("b"), null));

        root.Insert(0, b);
        root.Insert(0, a);
        AssertOrder(root, "a", "b");

        root.Move(0, 1);
        AssertOrder(root, "b", "a");

        Assert.AreSame(a, root.Detach(a));
        Assert.IsNull(a.Parent);
        Assert.IsNull(a.Element.Parent);
        AssertOrder(root, "b");

        Assert.ThrowsExactly<InvalidOperationException>(() => root.Detach(a));
        Assert.ThrowsExactly<InvalidOperationException>(() => root.Insert(0, b));
    }

    [TestMethod]
    public void Header_ShowsTheIndexAndTheContents()
    {
        var root = new NodeViewModel(new Element(null, null, new Element(), new Element()));
        NodeViewModel second = root.Children[1];
        var changed = new List<string?>();
        second.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        Assert.AreEqual("root  (empty)", root.Header);
        Assert.AreEqual("#1  (empty)", second.Header);

        second.SetData(Data.FromAsciiString("text"));
        second.SetMetaData(new byte[] { 1, 2 });

        Assert.AreEqual("#1  \"text\"  [0102]", second.Header);
        CollectionAssert.AreEqual(new[] { "Header", "Header" }, changed);

        // Moving the first child changes the index of the second
        changed.Clear();
        root.Detach(root.Children[0]);
        Assert.AreEqual("#0  \"text\"  [0102]", second.Header);
        CollectionAssert.Contains(changed, "Header");
    }

    private static void AssertOrder(NodeViewModel parent, params string[] data)
    {
        CollectionAssert.AreEqual(data, parent.Children.Select(child => child.Element.Data.AsAsciiString()).ToArray());
        CollectionAssert.AreEqual(data, parent.Element.Children.Select(child => child.Data.AsAsciiString()).ToArray());
        Assert.IsTrue(parent.Children.All(child => child.Parent == parent && child.Element.Parent == parent.Element));
    }
}
