using NSubstitute;
using PStudio.Interfaces;
using PStudio.Models;
using PStudio.Presentation;
using PStudio.Services;
using PStudio.ViewModels;
using Promptuarium;
using Promptuarium.Serializer;

namespace PStudio.Tests.ViewModels;

/// <summary>
/// The jumps of the selection by the keyboard (Home, End, Page Up, Page Down) in the raw tree and in the serializer
/// view, the rows they go over, and the view a tree is opened in.
/// </summary>
[TestClass]
public sealed class NavigationTests
{
    private readonly ITreeFileService files = Substitute.For<ITreeFileService>();
    private readonly IFileDialogService fileDialogs = Substitute.For<IFileDialogService>();
    private readonly IViewModelFactory factory = Substitute.For<IViewModelFactory>();
    private readonly ISettingsStore settings = Substitute.For<ISettingsStore>();
    private readonly IFormMemory formMemory = Substitute.For<IFormMemory>();

    public NavigationTests()
    {
        factory.CreateContentEditor(Arg.Any<string>()).Returns(call => new ContentEditorViewModel(call.Arg<string>(), fileDialogs, files));
        settings.Settings.Returns(new StudioSettings());
        formMemory.Recall(Arg.Any<string>()).Returns(_ => new FileForms());
    }

    public sealed class Sample
    {
        public string Name { get; set; } = "demo";

        public int Count { get; set; } = 3;

        public List<string> Tags { get; set; } = ["a", "b", "c"];

        public Inner Inner { get; set; } = new();
    }

    public sealed class Inner
    {
        public double Rate { get; set; } = 1.5;

        public bool On { get; set; }
    }

    // ---------------------------------------------------------------- Jump

    private static readonly string[] Rows = ["a", "b", "c", "d", "e", "f", "g"];

    [TestMethod]
    public void Jump_WithoutRows_GoesNowhere()
    {
        Assert.IsNull(MainWindowViewModel.Jump<string>([], null, RowJump.First, 3));
        Assert.IsNull(MainWindowViewModel.Jump<string>([], "a", RowJump.Last, 3));
        Assert.IsNull(MainWindowViewModel.Jump<string>([], "a", RowJump.PageDown, 3));
        Assert.IsNull(MainWindowViewModel.Jump<string>([], "a", RowJump.PageUp, 3));
    }

    [TestMethod]
    [DataRow(RowJump.First, "a")]
    [DataRow(RowJump.Last, "g")]
    [DataRow(RowJump.PageUp, "a")]
    [DataRow(RowJump.PageDown, "e")]
    public void Jump_FromTheMiddle(RowJump jump, string expected)
    {
        Assert.AreEqual(expected, MainWindowViewModel.Jump(Rows, Rows[2], jump, 2));
    }

    [TestMethod]
    public void Jump_ByPages_StopsAtTheEnds()
    {
        Assert.AreEqual("g", MainWindowViewModel.Jump(Rows, Rows[5], RowJump.PageDown, 3));
        Assert.AreEqual("g", MainWindowViewModel.Jump(Rows, Rows[6], RowJump.PageDown, 3));
        Assert.AreEqual("a", MainWindowViewModel.Jump(Rows, Rows[1], RowJump.PageUp, 3));
        Assert.AreEqual("a", MainWindowViewModel.Jump(Rows, Rows[0], RowJump.PageUp, 3));
        Assert.AreEqual("g", MainWindowViewModel.Jump(Rows, Rows[0], RowJump.PageDown, 100));
    }

    [TestMethod]
    public void Jump_ByPages_ExactlyAPage()
    {
        Assert.AreEqual("d", MainWindowViewModel.Jump(Rows, Rows[0], RowJump.PageDown, 3));
        Assert.AreEqual("a", MainWindowViewModel.Jump(Rows, Rows[3], RowJump.PageUp, 3));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-5)]
    public void Jump_APageIsAtLeastARow(int page)
    {
        Assert.AreEqual("d", MainWindowViewModel.Jump(Rows, Rows[2], RowJump.PageDown, page));
        Assert.AreEqual("b", MainWindowViewModel.Jump(Rows, Rows[2], RowJump.PageUp, page));
    }

    [TestMethod]
    [DataRow(RowJump.First, "a")]
    [DataRow(RowJump.Last, "g")]
    [DataRow(RowJump.PageUp, "a")]
    [DataRow(RowJump.PageDown, "a")]
    public void Jump_WithoutACurrentRow_StartsAtTheTop(RowJump jump, string expected)
    {
        Assert.AreEqual(expected, MainWindowViewModel.Jump(Rows, null, jump, 3));
    }

    [TestMethod]
    public void Jump_FromARowThatIsNotShown_StartsAtTheTop()
    {
        Assert.AreEqual("a", MainWindowViewModel.Jump(Rows, "hidden", RowJump.PageDown, 3));
        Assert.AreEqual("g", MainWindowViewModel.Jump(Rows, "hidden", RowJump.Last, 3));
    }

    [TestMethod]
    public void Jump_ComparesTheRowsByReference()
    {
        string first = new('x', 1);
        string second = new('x', 1);

        // Equal to the first, the second row is still the second one: a page down goes to the third
        Assert.AreEqual("z", MainWindowViewModel.Jump([first, second, "z"], second, RowJump.PageDown, 1));
        Assert.AreSame(first, MainWindowViewModel.Jump([first, second, "z"], second, RowJump.PageUp, 1));
    }

    [TestMethod]
    public void Jump_OfOneRow_StaysOnIt()
    {
        foreach (RowJump jump in Enum.GetValues<RowJump>())
        {
            Assert.AreEqual("a", MainWindowViewModel.Jump(["a"], "a", jump, 5));
        }
    }

    // ---------------------------------------------------------------- the shown rows

    [TestMethod]
    public void ShownSerializerRows_AreTheExpandedOnes_FromTheTop()
    {
        MainWindowViewModel sut = Open(PromptuariumSerializer.ToElement(new Sample()));
        SerializerNodeViewModel root = sut.SerializerRoots[0];

        CollectionAssert.AreEqual(new[] { "", "Name", "Count", "Tags", "Inner" }, sut.ShownSerializerRows().Select(row => row.NameText).ToArray());

        root.Children[2].IsExpanded = true;
        CollectionAssert.AreEqual(new[] { "", "Name", "Count", "Tags", "", "", "", "Inner" }, sut.ShownSerializerRows().Select(row => row.NameText).ToArray());
        CollectionAssert.AreEqual(new[] { "[0]", "[1]", "[2]" }, sut.ShownSerializerRows().Skip(4).Take(3).Select(row => row.Label).ToArray());

        root.Children[3].IsExpanded = true;
        Assert.AreEqual(10, sut.ShownSerializerRows().Count);
        Assert.AreEqual("On", sut.ShownSerializerRows()[^1].NameText);

        root.IsExpanded = false;
        Assert.AreEqual(1, sut.ShownSerializerRows().Count);
    }

    [TestMethod]
    public void ShownSerializerRows_OfATreeThatIsNotSerializable_AreNone()
    {
        MainWindowViewModel sut = Open(new Element().Add(new Element(), new Element()));

        Assert.AreEqual(0, sut.ShownSerializerRows().Count);
        sut.JumpSerializerRow(RowJump.Last, 3);
        Assert.IsNull(sut.SelectedSerializerNode);
    }

    [TestMethod]
    public void ShownRawRows_AreTheExpandedNodes_FromTheTop()
    {
        MainWindowViewModel sut = Open(new Element().Add(new Element().Add(new Element(), new Element()), new Element()));
        NodeViewModel root = sut.Roots[0];

        root.IsExpanded = false;
        CollectionAssert.AreEqual(new[] { root }, sut.ShownRawRows().ToArray());

        root.IsExpanded = true;
        root.Children[0].IsExpanded = false;
        CollectionAssert.AreEqual(new[] { root, root.Children[0], root.Children[1] }, sut.ShownRawRows().ToArray());

        root.Children[0].IsExpanded = true;
        CollectionAssert.AreEqual(new[] { root, root.Children[0], root.Children[0].Children[0], root.Children[0].Children[1], root.Children[1] }, sut.ShownRawRows().ToArray());
    }

    // ---------------------------------------------------------------- the jumps of the views

    [TestMethod]
    public void JumpSerializerRow_SelectsTheRow_AndItsHolderInTheRawTree()
    {
        MainWindowViewModel sut = Open(PromptuariumSerializer.ToElement(new Sample()));
        SerializerNodeViewModel root = sut.SerializerRoots[0];

        sut.JumpSerializerRow(RowJump.Last, 2);
        Assert.AreSame(root.Children[3], sut.SelectedSerializerNode);
        Assert.AreSame(root.Children[3].Holder, sut.SelectedNode);

        sut.JumpSerializerRow(RowJump.PageUp, 2);
        Assert.AreSame(root.Children[1], sut.SelectedSerializerNode);
        Assert.AreSame(root.Children[1].Holder, sut.SelectedNode);

        sut.JumpSerializerRow(RowJump.First, 2);
        Assert.AreSame(root, sut.SelectedSerializerNode);
        Assert.AreSame(sut.Root, sut.SelectedNode);

        sut.JumpSerializerRow(RowJump.PageDown, 2);
        Assert.AreSame(root.Children[1], sut.SelectedSerializerNode);
    }

    [TestMethod]
    public void JumpSerializerRow_GoesIntoTheExpandedValues()
    {
        MainWindowViewModel sut = Open(PromptuariumSerializer.ToElement(new Sample()));
        SerializerNodeViewModel root = sut.SerializerRoots[0];
        root.Children[3].IsExpanded = true;

        sut.JumpSerializerRow(RowJump.Last, 1);

        Assert.AreSame(root.Children[3].Children[1], sut.SelectedSerializerNode);
        Assert.AreEqual("On", sut.SelectedSerializerNode!.NameText);
    }

    [TestMethod]
    public void JumpSerializerRow_FromAValueOfAHolder_CountsFromTheHolder()
    {
        MainWindowViewModel sut = Open(PromptuariumSerializer.ToElement(new Sample()));
        SerializerNodeViewModel root = sut.SerializerRoots[0];

        // The VALUE node of Count selected in the raw tree selects Count in the serializer view
        sut.SelectedNode = root.Children[1].Holder.Children[^1];
        Assert.AreSame(root.Children[1], sut.SelectedSerializerNode);

        sut.JumpSerializerRow(RowJump.PageDown, 1);
        Assert.AreSame(root.Children[2], sut.SelectedSerializerNode);
    }

    [TestMethod]
    public void JumpRawRow_SelectsTheShownNodes()
    {
        MainWindowViewModel sut = Open(new Element().Add(new Element().Add(new Element(), new Element()), new Element(), new Element()));
        NodeViewModel root = sut.Roots[0];
        root.Children[0].IsExpanded = false;

        sut.JumpRawRow(RowJump.Last, 3);
        Assert.AreSame(root.Children[2], sut.SelectedNode);

        sut.JumpRawRow(RowJump.PageUp, 2);
        Assert.AreSame(root.Children[0], sut.SelectedNode);

        root.Children[0].IsExpanded = true;
        sut.JumpRawRow(RowJump.PageDown, 1);
        Assert.AreSame(root.Children[0].Children[0], sut.SelectedNode);

        sut.JumpRawRow(RowJump.First, 1);
        Assert.AreSame(root, sut.SelectedNode);
    }

    // ---------------------------------------------------------------- the view a tree opens in

    [TestMethod]
    public void SerializableTree_OpensInTheSerializerView()
    {
        MainWindowViewModel sut = Open(PromptuariumSerializer.ToElement(new Sample()));

        Assert.AreEqual(MainWindowViewModel.SerializerView, sut.SelectedViewIndex);
        Assert.IsFalse(sut.IsRawViewSelected);
    }

    [TestMethod]
    public void TreeThatIsNotSerializable_OpensInTheRawView()
    {
        MainWindowViewModel sut = Open(new Element().Add(new Element()));

        Assert.AreEqual(MainWindowViewModel.RawView, sut.SelectedViewIndex);
        Assert.IsTrue(sut.IsRawViewSelected);
    }

    [TestMethod]
    public void SerializableTree_OpensInTheRawView_WhenTheSerializerViewIsSwitchedOff()
    {
        settings.Settings.View.IsSerializerPanelVisible = false;

        MainWindowViewModel sut = Open(PromptuariumSerializer.ToElement(new Sample()));

        Assert.AreEqual(MainWindowViewModel.RawView, sut.SelectedViewIndex);
    }

    [TestMethod]
    public void NewTree_OpensInTheSerializerView()
    {
        MainWindowViewModel sut = Create();

        Assert.AreEqual(MainWindowViewModel.SerializerView, sut.SelectedViewIndex);
    }

    [TestMethod]
    public void OpeningASerializableTree_AfterTheRawViewWasChosen_ShowsTheSerializerView()
    {
        MainWindowViewModel sut = Open(PromptuariumSerializer.ToElement(new Sample()));
        sut.SelectedViewIndex = MainWindowViewModel.RawView;

        Assert.IsTrue(sut.OpenFileAsync("/data/tree.prmt").GetAwaiter().GetResult());

        Assert.AreEqual(MainWindowViewModel.SerializerView, sut.SelectedViewIndex);
    }

    [TestMethod]
    public void OpeningATreeThatIsNotSerializable_FromTheSerializerView_ShowsTheRawView()
    {
        MainWindowViewModel sut = Create();
        Assert.AreEqual(MainWindowViewModel.SerializerView, sut.SelectedViewIndex);
        files.LoadAsync("/data/broken.prmt").Returns(_ => new Element().Add(new Element()));

        Assert.IsTrue(sut.OpenFileAsync("/data/broken.prmt").GetAwaiter().GetResult());

        Assert.AreEqual(MainWindowViewModel.RawView, sut.SelectedViewIndex);
    }

    [TestMethod]
    public void TreeThatBecomesSerializableInTheRawView_StaysInTheRawView()
    {
        MainWindowViewModel sut = Open(PromptuariumSerializer.ToElement(new Sample()));
        sut.SelectedViewIndex = MainWindowViewModel.RawView;
        sut.SelectedNode = sut.Root;
        sut.AddChildCommand.Execute(null);
        Assert.IsFalse(sut.IsSerializable);

        sut.SelectedNode = sut.Root.Children[^1];
        sut.DeleteCommand.Execute(null);

        Assert.IsTrue(sut.IsSerializable);
        Assert.AreEqual(MainWindowViewModel.RawView, sut.SelectedViewIndex);
    }

    // ---------------------------------------------------------------- the comment and the summary

    [TestMethod]
    public void CommentTip_IsTheComment_WhenTheRowIsNotEdited()
    {
        MainWindowViewModel sut = Open(PromptuariumSerializer.ToElement(new Sample()));
        SerializerNodeViewModel count = sut.SerializerRoots[0].Children[1];
        Assert.IsNull(count.CommentTip);

        count.CommentText = "first\nsecond";
        count.CommitComment();

        Assert.AreEqual("first\nsecond", count.CommentTip);

        count.IsEditing = true;
        Assert.IsNull(count.CommentTip);

        count.IsEditing = false;
        Assert.AreEqual("first\nsecond", count.CommentTip);
    }

    [TestMethod]
    public void CommentTip_TellsItsChange_WithTheEditing()
    {
        MainWindowViewModel sut = Open(PromptuariumSerializer.ToElement(new Sample()));
        SerializerNodeViewModel count = sut.SerializerRoots[0].Children[1];
        count.CommentText = "c";
        count.CommitComment();
        var changed = new List<string?>();
        count.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        count.IsEditing = true;

        CollectionAssert.Contains(changed, nameof(SerializerNodeViewModel.CommentTip));
    }

    [TestMethod]
    public void CommentTip_OfTheRoot_IsTheCommentOfTheFile()
    {
        MainWindowViewModel sut = Open(PromptuariumSerializer.ToElement(new Sample()));
        SerializerNodeViewModel root = sut.SerializerRoots[0];

        root.CommentText = "the file";
        root.CommitComment();

        Assert.AreEqual("the file", root.CommentTip);
    }

    [TestMethod]
    public void CommentTip_OfAnEmptiedComment_IsNull()
    {
        MainWindowViewModel sut = Open(PromptuariumSerializer.ToElement(new Sample()));
        SerializerNodeViewModel count = sut.SerializerRoots[0].Children[1];
        count.CommentText = "c";
        count.CommitComment();

        count.CommentText = string.Empty;
        count.CommitComment();

        Assert.IsNull(count.CommentTip);
    }

    [TestMethod]
    public void SerializerSummary_IsEmpty_WithoutWarnings()
    {
        MainWindowViewModel sut = Open(PromptuariumSerializer.ToElement(new Sample()));

        Assert.AreEqual(string.Empty, sut.SerializerSummary);
        Assert.IsFalse(sut.SerializerSummary.Contains("serializable", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void SerializerSummary_OfATreeThatIsNotSerializable_IsEmpty()
    {
        MainWindowViewModel sut = Open(new Element().Add(new Element()));

        Assert.AreEqual(string.Empty, sut.SerializerSummary);
    }

    // ---------------------------------------------------------------- the layout

    [TestMethod]
    public void Layout_IsTheOneOfTheSettings()
    {
        MainWindowViewModel sut = Create();

        Assert.AreSame(settings.Settings.Layout, sut.Layout);
    }

    [TestMethod]
    public void SaveSettings_SavesTheStore()
    {
        MainWindowViewModel sut = Create();
        settings.ClearReceivedCalls();

        sut.SaveSettings();

        settings.Received(1).Save();
    }

    private MainWindowViewModel Create()
    {
        IMessageDialogService messages = Substitute.For<IMessageDialogService>();
        return new MainWindowViewModel(files, new ChunkAnalyzer(), new NodeClipboard(), fileDialogs, messages, Substitute.For<IEditorWindowService>(), factory, formMemory, settings);
    }

    private MainWindowViewModel Open(Element tree)
    {
        files.LoadAsync("/data/tree.prmt").Returns(_ => Element.FromBytes(tree.ToBytes()));
        MainWindowViewModel sut = Create();
        Assert.IsTrue(sut.OpenFileAsync("/data/tree.prmt").GetAwaiter().GetResult());
        return sut;
    }
}
