using NSubstitute;
using PStudio.Interfaces;
using PStudio.Models;
using PStudio.Presentation;
using PStudio.Services;
using PStudio.ViewModels;
using Promptuarium;
using Promptuarium.Serializer;

namespace PStudio.Tests.ViewModels;

/// <summary>The serializer view of the editor: shown for a serializable tree, edited in place, following the raw tree.</summary>
[TestClass]
public sealed class SerializerViewTests
{
    private readonly ITreeFileService files = Substitute.For<ITreeFileService>();
    private readonly IFileDialogService fileDialogs = Substitute.For<IFileDialogService>();
    private readonly IViewModelFactory factory = Substitute.For<IViewModelFactory>();
    private readonly ISettingsStore settings = Substitute.For<ISettingsStore>();
    private readonly IFormMemory formMemory = Substitute.For<IFormMemory>();

    public SerializerViewTests()
    {
        factory.CreateContentEditor(Arg.Any<string>()).Returns(call => new ContentEditorViewModel(call.Arg<string>(), fileDialogs, files));
        settings.Settings.Returns(new StudioSettings());
        formMemory.Recall(Arg.Any<string>()).Returns(_ => new FileForms());
    }

    public sealed class Sample
    {
        public string Name { get; set; } = "demo";

        public int Count { get; set; } = 3;

        public bool Enabled { get; set; } = true;

        public List<string> Tags { get; set; } = ["a", "b"];

        public Inner Inner { get; set; } = new();
    }

    public sealed class Inner
    {
        public double Rate { get; set; } = 1.5;
    }

    [TestMethod]
    public void SerializableTree_IsShown_WhateverItsSignature()
    {
        Element tree = PromptuariumSerializer.ToElement(new Sample());
        tree.MetaData = Data.FromAsciiString("MYAPP1");

        MainWindowViewModel sut = Open(tree);

        Assert.IsTrue(sut.IsSerializable);
        Assert.IsTrue(sut.IsSerializerPanelShown);
        SerializerNodeViewModel root = sut.SerializerRoots.Single();
        CollectionAssert.AreEqual(new[] { "Name", "Count", "Enabled", "Tags", "Inner" }, root.Children.Select(child => child.NameText).ToArray());
        Assert.AreEqual("demo", root.Children[0].ValueText);
        Assert.AreEqual("INT32", root.Children[1].SelectedType.Name);
        Assert.AreEqual("3", root.Children[1].ValueText);
        Assert.IsTrue(root.Children[2].BoolValue);
        Assert.AreEqual("[1]", root.Children[3].Children[1].Label);
        Assert.AreEqual("1.5", root.Children[4].Children[0].ValueText);
    }

    [TestMethod]
    public void NewTree_IsAnEmptyObject()
    {
        MainWindowViewModel sut = Create();

        Assert.IsTrue(sut.IsSerializerPanelShown);
        Assert.AreSame(sut.Root, sut.SelectedSerializerNode!.Holder);
    }

    [TestMethod]
    public void TreeThatIsNotSerializable_IsNotShown_AndTheViewClosesWhenItBreaks()
    {
        MainWindowViewModel sut = Open(PromptuariumSerializer.ToElement(new Sample()));

        // A raw child of the root without a tag
        sut.SelectedNode = sut.Root;
        sut.AddChildCommand.Execute(null);

        Assert.IsFalse(sut.IsSerializable);
        Assert.IsFalse(sut.IsSerializerPanelShown);
        Assert.IsEmpty(sut.SerializerRoots);
        Assert.IsTrue(sut.IsStatusError);
        StringAssert.Contains(sut.StatusText, "serializer view is closed");

        sut.DeleteCommand.Execute(null);

        Assert.IsTrue(sut.IsSerializerPanelShown);
    }

    [TestMethod]
    public void View_CanBeSwitchedOff_AndTheChoiceIsKept()
    {
        MainWindowViewModel sut = Create();

        sut.ToggleSerializerPanelCommand.Execute(null);

        Assert.IsFalse(sut.IsSerializerPanelShown);
        Assert.IsFalse(settings.Settings.View.IsSerializerPanelVisible);
        settings.Received().Save();
    }

    [TestMethod]
    public void Selections_FollowEachOther()
    {
        MainWindowViewModel sut = Open(PromptuariumSerializer.ToElement(new Sample()));
        SerializerNodeViewModel rate = sut.SerializerRoots[0].Children[4].Children[0];

        sut.SelectedSerializerNode = rate;

        Assert.AreSame(rate.Holder, sut.SelectedNode);
        Assert.IsTrue(rate.Holder.Parent!.IsExpanded);

        // The VALUE node of a holder selects the holder in the serializer view, and stays selected in the raw tree
        NodeViewModel countValue = sut.Root.Children[1].Children[1];
        sut.SelectedNode = countValue;

        Assert.AreSame(sut.SerializerRoots[0].Children[1], sut.SelectedSerializerNode);
        Assert.AreSame(countValue, sut.SelectedNode);
    }

    [TestMethod]
    public void EditedValue_IsTypeChecked_AndStoredInTheRawTree()
    {
        MainWindowViewModel sut = Open(PromptuariumSerializer.ToElement(new Sample()));
        SerializerNodeViewModel count = sut.SerializerRoots[0].Children[1];

        count.ValueText = "12x";
        Assert.IsFalse(count.CommitValue());
        Assert.IsTrue(count.HasValueError);
        Assert.IsFalse(sut.IsModified);

        count.ValueText = "-7";
        Assert.IsTrue(count.CommitValue());

        Assert.AreEqual(-7, sut.Root.Element[1][1].Data.AsInt());
        Assert.IsTrue(sut.IsModified);
        Assert.AreSame(count, sut.SerializerRoots[0].Children[1]);
    }

    [TestMethod]
    public void RawEdit_IsShownInTheSerializerView()
    {
        MainWindowViewModel sut = Open(PromptuariumSerializer.ToElement(new Sample()));
        sut.SelectedNode = sut.Root.Children[0];

        sut.Data.Text = "renamed";
        sut.Data.ApplyPending();

        Assert.AreEqual("renamed", sut.SerializerRoots[0].Children[0].NameText);
    }

    [TestMethod]
    public void Rename_And_Comment_ChangeTheRawTree()
    {
        MainWindowViewModel sut = Open(PromptuariumSerializer.ToElement(new Sample()));
        SerializerNodeViewModel name = sut.SerializerRoots[0].Children[0];

        name.NameText = "Title";
        name.CommitName();
        name.CommentText = "the title";
        name.CommitComment();

        Element holder = sut.Root.Element[0];
        Assert.AreEqual("Title", holder.Data.AsUtf8String());
        Assert.AreEqual("the title", holder[1].Data.AsUtf8String());
        Assert.AreEqual("// the title", name.CommentPreview);
        Assert.IsTrue(PromptuariumSerializer.IsValid(sut.Root.Element));
    }

    [TestMethod]
    public void TypeChange_ConvertsTheValue()
    {
        MainWindowViewModel sut = Open(PromptuariumSerializer.ToElement(new Sample()));
        SerializerNodeViewModel count = sut.SerializerRoots[0].Children[1];

        count.SelectedType = SerializerEditing.TypeOf(0x43);
        Assert.AreEqual("INT64", count.SelectedType.Name);
        Assert.AreEqual("3", count.ValueText);
        Assert.HasCount(8, sut.Root.Element[1][1].Data.ToArray());

        count.SelectedType = SerializerEditing.TypeOf(0xD0);
        Assert.AreEqual("1970-01-01", count.ValueText);

        count.SelectedType = SerializerEditing.TypeOf(0x20);
        Assert.IsTrue(count.IsStructure);
        Assert.HasCount(1, sut.Root.Element[1].Children);
        Assert.IsTrue(PromptuariumSerializer.IsValid(sut.Root.Element));
    }

    [TestMethod]
    public void ArrayBecomesAnObject_OfItsElements_ButNotAScalar()
    {
        MainWindowViewModel sut = Open(PromptuariumSerializer.ToElement(new Sample()));
        SerializerNodeViewModel tags = sut.SerializerRoots[0].Children[3];

        tags.SelectedType = SerializerEditing.TypeOf(0x80);
        Assert.AreEqual("ARRAY", tags.SelectedType.Name);
        Assert.IsTrue(sut.IsStatusError);

        tags.SelectedType = SerializerEditing.TypeOf(0x20);
        CollectionAssert.AreEqual(new[] { "0", "1" }, tags.Children.Select(child => child.NameText).ToArray());
        Assert.AreEqual("b", tags.Children[1].ValueText);
        Assert.IsTrue(PromptuariumSerializer.IsValid(sut.Root.Element));
    }

    [TestMethod]
    public void Add_Delete_Move_KeepTheTreeSerializable()
    {
        MainWindowViewModel sut = Open(PromptuariumSerializer.ToElement(new Sample()));
        SerializerEditRequestedProbe probe = new(sut);
        SerializerNodeViewModel root = sut.SerializerRoots[0];

        sut.SelectedSerializerNode = root;
        sut.SerializerAddCommand.Execute(null);
        // "Name" is taken already: the names are matched regardless of case when read
        Assert.AreEqual("name2", root.Children[^1].NameText);
        Assert.AreSame(root.Children[^1], sut.SelectedSerializerNode);
        Assert.AreSame(root.Children[^1], probe.Last);

        sut.SerializerAddAfterCommand.Execute(null);
        Assert.AreEqual("name3", root.Children[^1].NameText);

        sut.SelectedSerializerNode = root.Children[3];
        sut.SerializerAddCommand.Execute(null);
        Assert.AreEqual("[2]", root.Children[3].Children[2].Label);

        sut.SerializerMoveUpCommand.Execute(null);
        Assert.AreEqual("", root.Children[3].Children[1].ValueText);
        Assert.AreSame(root.Children[3].Children[1], sut.SelectedSerializerNode);

        sut.SerializerDeleteCommand.Execute(null);
        Assert.HasCount(2, root.Children[3].Children);

        Assert.IsTrue(PromptuariumSerializer.IsValid(sut.Root.Element));
        Assert.IsTrue(sut.IsSerializerPanelShown);
    }

    [TestMethod]
    public void BoolValue_IsStored()
    {
        MainWindowViewModel sut = Open(PromptuariumSerializer.ToElement(new Sample()));
        SerializerNodeViewModel enabled = sut.SerializerRoots[0].Children[2];

        enabled.BoolValue = false;

        Assert.AreEqual(0, sut.Root.Element[2][1].Data.Span[0]);
        Assert.IsFalse(enabled.BoolValue);
    }

    private sealed class SerializerEditRequestedProbe
    {
        public SerializerEditRequestedProbe(MainWindowViewModel sut) => sut.SerializerEditRequested += node => Last = node;

        public SerializerNodeViewModel? Last { get; private set; }
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
