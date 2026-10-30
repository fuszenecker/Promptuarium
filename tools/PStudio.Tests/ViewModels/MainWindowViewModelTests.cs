using NSubstitute;
using PStudio.Interfaces;
using PStudio.Models;
using PStudio.Presentation;
using PStudio.Services;
using PStudio.ViewModels;
using Promptuarium;

namespace PStudio.Tests.ViewModels;

[TestClass]
public sealed class MainWindowViewModelTests
{
    private readonly ITreeFileService files = Substitute.For<ITreeFileService>();
    private readonly IFileDialogService fileDialogs = Substitute.For<IFileDialogService>();
    private readonly IMessageDialogService messages = Substitute.For<IMessageDialogService>();
    private readonly IEditorWindowService windows = Substitute.For<IEditorWindowService>();
    private readonly IViewModelFactory factory = Substitute.For<IViewModelFactory>();
    private readonly INodeClipboard clipboard = new NodeClipboard();
    private readonly InMemoryFormMemory formMemory = new();

    public MainWindowViewModelTests()
    {
        factory.CreateContentEditor(Arg.Any<string>()).Returns(call => new ContentEditorViewModel(call.Arg<string>(), fileDialogs, files));
        factory.CreateEditor().Returns(_ => Create());
    }

    [TestMethod]
    public void NewEditor_HasAnEmptySelectedRoot()
    {
        MainWindowViewModel sut = Create();

        Assert.AreSame(sut.Root, sut.SelectedNode);
        Assert.AreEqual(0, sut.Root.Children.Count);
        Assert.IsFalse(sut.IsModified);
        Assert.AreEqual("untitled - Promptuarium Studio", sut.Title);
        Assert.IsTrue(sut.AddChildCommand.CanExecute(null));
        Assert.IsFalse(sut.DeleteCommand.CanExecute(null));
        Assert.IsFalse(sut.AddSiblingCommand.CanExecute(null));
    }

    [TestMethod]
    public void AddChild_AddSibling_ChangeTheTree()
    {
        MainWindowViewModel sut = Create();

        sut.AddChildCommand.Execute(null);
        NodeViewModel first = sut.SelectedNode!;
        sut.AddSiblingCommand.Execute(null);
        NodeViewModel second = sut.SelectedNode!;
        sut.AddChildCommand.Execute(null);

        Assert.AreEqual("/1/0", sut.SelectedNode!.Path);
        Assert.AreEqual("/0", first.Path);
        Assert.AreEqual("/1", second.Path);
        Assert.AreEqual(2, sut.Root.Element.Children.Count);
        Assert.AreEqual(1, sut.Root.Element[1].Children.Count);
        Assert.IsTrue(sut.IsModified);
        Assert.AreEqual("untitled * - Promptuarium Studio", sut.Title);
    }

    [TestMethod]
    public void Delete_SelectsTheNextSibling_ThenThePrevious_ThenTheParent()
    {
        MainWindowViewModel sut = CreateWithChildren("a", "b", "c");

        sut.SelectedNode = sut.Root.Children[1];
        sut.DeleteCommand.Execute(null);
        Assert.AreEqual("c", Selected(sut));

        sut.DeleteCommand.Execute(null);
        Assert.AreEqual("a", Selected(sut));

        sut.DeleteCommand.Execute(null);
        Assert.AreSame(sut.Root, sut.SelectedNode);
        Assert.AreEqual(0, sut.Root.Element.Children.Count);
    }

    [TestMethod]
    public void Moves_KeepTheSelection()
    {
        MainWindowViewModel sut = CreateWithChildren("a", "b", "c");
        sut.SelectedNode = sut.Root.Children[0];

        Assert.IsFalse(sut.MoveUpCommand.CanExecute(null));
        sut.MoveDownCommand.Execute(null);
        sut.MoveDownCommand.Execute(null);

        Assert.AreEqual("/2", sut.SelectedNode!.Path);
        Assert.IsFalse(sut.MoveDownCommand.CanExecute(null));
        CollectionAssert.AreEqual(new[] { "b", "c", "a" }, sut.Root.Element.Children.Select(child => child.Data.AsAsciiString()).ToArray());

        // Indent moves it under its previous sibling, Outdent back after it
        sut.IndentCommand.Execute(null);
        Assert.AreEqual("/1/0", sut.SelectedNode!.Path);
        Assert.AreEqual("a", sut.Root.Element[1][0].Data.AsAsciiString());
        Assert.IsTrue(sut.OutdentCommand.CanExecute(null));

        sut.OutdentCommand.Execute(null);
        Assert.AreEqual("/2", sut.SelectedNode!.Path);
        Assert.IsFalse(sut.OutdentCommand.CanExecute(null));
    }

    [TestMethod]
    public void CutAndPaste_MovesACopy()
    {
        MainWindowViewModel sut = CreateWithChildren("a", "b");
        sut.SelectedNode = sut.Root.Children[0];
        Assert.IsFalse(sut.PasteAsChildCommand.CanExecute(null));

        sut.CutCommand.Execute(null);
        Assert.AreEqual("b", Selected(sut));
        Assert.IsTrue(sut.PasteAfterCommand.CanExecute(null));

        sut.PasteAfterCommand.Execute(null);
        sut.PasteAsChildCommand.Execute(null);

        Assert.AreEqual("/1/0", sut.SelectedNode!.Path);
        Assert.AreEqual("a", Selected(sut));
        CollectionAssert.AreEqual(new[] { "b", "a" }, sut.Root.Element.Children.Select(child => child.Data.AsAsciiString()).ToArray());
    }

    [TestMethod]
    public void ContentEdits_AreStoredInTheSelectedNode()
    {
        MainWindowViewModel sut = CreateWithChildren("a");
        sut.SelectedNode = sut.Root.Children[0];

        sut.Data.Text = "changed";
        Choose(sut.MetaData, ContentForm.Hex);
        sut.MetaData.Text = "01 02";

        // Changing the selection applies the edits
        sut.SelectedNode = sut.Root;

        Element node = sut.Root.Element[0];
        Assert.AreEqual("changed", node.Data.AsAsciiString());
        CollectionAssert.AreEqual(new byte[] { 1, 2 }, node.MetaData.ToArray());
        Assert.IsTrue(sut.IsModified);
        Assert.AreEqual("#0  \"changed\"  [0102]", sut.Root.Children[0].Header);
    }

    [TestMethod]
    public void InvalidEdit_IsDroppedWhenTheSelectionChanges()
    {
        MainWindowViewModel sut = CreateWithChildren("a");
        sut.SelectedNode = sut.Root.Children[0];
        Choose(sut.Data, ContentForm.Hex);
        sut.Data.Text = "0";

        sut.SelectedNode = sut.Root;

        Assert.AreEqual("a", sut.Root.Element[0].Data.AsAsciiString());
        Assert.IsTrue(sut.IsStatusError);
    }

    [TestMethod]
    public async Task Open_LoadsTheFile()
    {
        MainWindowViewModel sut = Create();
        fileDialogs.PickTreeToOpenAsync().Returns("/data/tree.prmt");
        files.LoadAsync("/data/tree.prmt").Returns(new Element(null, null, new Element(Data.FromAsciiString("x"), null)));

        await sut.OpenCommand.ExecuteAsync(null);

        Assert.AreEqual("tree.prmt - Promptuarium Studio", sut.Title);
        Assert.AreEqual("x", sut.Root.Children.Single().Element.Data.AsAsciiString());
        Assert.AreSame(sut.Root, sut.SelectedNode);
    }

    [TestMethod]
    public async Task ChosenForms_AreRememberedOnSave_AndRecalledOnOpen()
    {
        var tree = new Element(null, null, new Element(Data.FromInt(5), Data.FromAsciiString("count")));
        files.LoadAsync("/data/tree.prmt").Returns(_ => Element.FromBytes(tree.ToBytes()));

        MainWindowViewModel sut = Create();
        await sut.OpenFileAsync("/data/tree.prmt");
        sut.SelectedNode = sut.Root.Children[0];
        Choose(sut.Data, ContentForm.Int32);
        Choose(sut.MetaData, ContentForm.Ascii);
        await sut.SaveCommand.ExecuteAsync(null);

        FileForms remembered = formMemory.Recall("/data/tree.prmt");
        Assert.AreEqual(ContentForm.Int32, remembered.Data["/0"]);
        Assert.AreEqual(ContentForm.Ascii, remembered.MetaData["/0"]);

        MainWindowViewModel reopened = Create();
        await reopened.OpenFileAsync("/data/tree.prmt");
        reopened.SelectedNode = reopened.Root.Children[0];

        Assert.AreEqual(ContentForm.Int32, reopened.Data.Form);
        Assert.AreEqual("5", reopened.Data.Text);
        Assert.AreEqual(ContentForm.Ascii, reopened.MetaData.Form);
    }

    [TestMethod]
    public async Task ChosenForms_FollowTheNode_AndAreRememberedWhenClosingWithoutChanges()
    {
        files.LoadAsync("/data/tree.prmt").Returns(_ => new Element(null, null, new Element(Data.FromInt(1), null), new Element(Data.FromInt(2), null)));

        MainWindowViewModel sut = Create();
        await sut.OpenFileAsync("/data/tree.prmt");
        sut.SelectedNode = sut.Root.Children[1];
        Choose(sut.Data, ContentForm.Single);
        sut.MoveUpCommand.Execute(null);
        sut.MoveDownCommand.Execute(null);
        sut.SelectedNode = sut.Root.Children[0];
        sut.SelectedNode = sut.Root.Children[1];

        Assert.AreEqual(ContentForm.Single, sut.Data.Form);

        // The moves changed the tree: closing without saving does not remember paths of an unsaved tree
        messages.AskSaveChangesAsync(Arg.Any<string>()).Returns(SaveChoice.Discard);
        Assert.IsTrue(await sut.ConfirmCloseAsync());
        Assert.IsTrue(formMemory.Recall("/data/tree.prmt").IsEmpty);

        MainWindowViewModel unchanged = Create();
        await unchanged.OpenFileAsync("/data/tree.prmt");
        unchanged.SelectedNode = unchanged.Root.Children[1];
        Choose(unchanged.Data, ContentForm.Single);
        Assert.IsTrue(await unchanged.ConfirmCloseAsync());

        Assert.AreEqual(ContentForm.Single, formMemory.Recall("/data/tree.prmt").Data["/1"]);
    }

    [TestMethod]
    public async Task Open_ReportsInvalidFiles()
    {
        MainWindowViewModel sut = Create();
        files.LoadAsync("bad").Returns(Task.FromException<Element>(new PromptuariumException("broken")));

        Assert.IsFalse(await sut.OpenFileAsync("bad"));

        Assert.IsTrue(sut.IsStatusError);
        await messages.Received(1).ShowErrorAsync(Arg.Any<string>(), Arg.Is<string>(text => text.Contains("broken")));
    }

    [TestMethod]
    public async Task Open_AsksAboutTheUnsavedChanges()
    {
        MainWindowViewModel sut = CreateWithChildren("a");
        messages.AskSaveChangesAsync(Arg.Any<string>()).Returns(SaveChoice.Cancel);

        await sut.OpenCommand.ExecuteAsync(null);

        await fileDialogs.DidNotReceive().PickTreeToOpenAsync();
        Assert.AreEqual(1, sut.Root.Children.Count);
    }

    [TestMethod]
    public async Task Save_AsksForTheFileOnlyFirst()
    {
        MainWindowViewModel sut = CreateWithChildren("a");
        fileDialogs.PickTreeToSaveAsync(null).Returns("/data/new.prmt");

        await sut.SaveCommand.ExecuteAsync(null);
        sut.AddChildCommand.Execute(null);
        await sut.SaveCommand.ExecuteAsync(null);

        await fileDialogs.Received(1).PickTreeToSaveAsync(Arg.Any<string?>());
        await files.Received(2).SaveAsync(sut.Root.Element, "/data/new.prmt");
        Assert.IsFalse(sut.IsModified);
        Assert.AreEqual("new.prmt - Promptuarium Studio", sut.Title);
    }

    [TestMethod]
    public async Task Save_AppliesThePendingEdit()
    {
        MainWindowViewModel sut = CreateWithChildren("a");
        fileDialogs.PickTreeToSaveAsync(null).Returns("t.prmt");
        sut.SelectedNode = sut.Root.Children[0];
        sut.Data.Text = "edited";

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.AreEqual("edited", sut.Root.Element[0].Data.AsAsciiString());
        await files.Received(1).SaveAsync(sut.Root.Element, "t.prmt");
    }

    [TestMethod]
    public async Task ConfirmClose_SavesOrDiscardsOrCancels()
    {
        MainWindowViewModel sut = CreateWithChildren("a");
        Assert.IsTrue(sut.HasUnsavedChanges);

        messages.AskSaveChangesAsync(Arg.Any<string>()).Returns(SaveChoice.Cancel);
        Assert.IsFalse(await sut.ConfirmCloseAsync());

        messages.AskSaveChangesAsync(Arg.Any<string>()).Returns(SaveChoice.Save);
        fileDialogs.PickTreeToSaveAsync(null).Returns((string?)null);
        Assert.IsFalse(await sut.ConfirmCloseAsync());

        messages.AskSaveChangesAsync(Arg.Any<string>()).Returns(SaveChoice.Discard);
        Assert.IsTrue(await sut.ConfirmCloseAsync());
        Assert.IsFalse(sut.HasUnsavedChanges);
    }

    [TestMethod]
    public void NestedTree_IsSavedIntoTheDataOfTheHostNode()
    {
        var inner = new Element(Data.FromAsciiString("inner"), null);
        MainWindowViewModel host = Create();
        host.Root.SetData(inner.ToBytes());

        MainWindowViewModel nested = Create();
        nested.OpenNested(host, host.Root);

        Assert.IsTrue(nested.IsNested);
        Assert.IsFalse(nested.SaveAsCommand.CanExecute(null));
        Assert.IsFalse(nested.OpenCommand.CanExecute(null));
        Assert.AreEqual("untitled / (nested) - Promptuarium Studio", nested.Title);
        Assert.AreEqual("inner", nested.Root.Element.Data.AsAsciiString());

        nested.AddChildCommand.Execute(null);
        Assert.IsTrue(host.HasUnsavedChanges);

        nested.SaveCommand.Execute(null);

        Element stored = Element.FromBytes(host.Root.Element.Data);
        Assert.AreEqual(1, stored.Children.Count);
        Assert.IsFalse(nested.IsModified);
        Assert.IsTrue(host.IsModified);

        // The host editor shows the new data
        CollectionAssert.AreEqual(host.Root.Element.Data.ToArray(), host.Data.Content.ToArray());
    }

    [TestMethod]
    public void NestedTree_OfInvalidData_IsRefused()
    {
        MainWindowViewModel host = Create();
        host.Root.SetData(new byte[] { 0x42 });

        Assert.ThrowsExactly<PromptuariumException>(() => Create().OpenNested(host, host.Root));
    }

    [TestMethod]
    public void OpenNested_UsesTheWindowService()
    {
        MainWindowViewModel sut = Create();

        sut.OpenNestedCommand.Execute(null);

        windows.Received(1).OpenNested(sut, sut.Root);
    }

    [TestMethod]
    public void ChunkPanel_FollowsTheChanges_AndTheSelection()
    {
        MainWindowViewModel sut = Create();
        CollectionAssert.AreEqual(new[] { "60", "00" }, Controls(sut));
        Assert.IsTrue(sut.Chunks[0].IsOfSelectedNode);

        sut.AddChildCommand.Execute(null);
        CollectionAssert.AreEqual(new[] { "40", "60", "00" }, Controls(sut));
        Assert.AreEqual("3 chunks, 3 bytes", sut.ChunkSummary);

        // The new child is selected: its chunk is highlighted and selected
        CollectionAssert.AreEqual(new[] { false, true, false }, sut.Chunks.Select(chunk => chunk.IsOfSelectedNode).ToArray());
        Assert.AreSame(sut.Chunks[1], sut.SelectedChunk);
        Assert.AreEqual("saved as 1 chunk, 1 byte, from offset 000001", sut.NodeEncoding);

        // Choosing a chunk selects its node
        sut.SelectedChunk = sut.Chunks[0];
        Assert.AreSame(sut.Root, sut.SelectedNode);

        // The terminator belongs to no node
        sut.SelectedChunk = sut.Chunks[2];
        Assert.AreSame(sut.Root, sut.SelectedNode);
    }

    [TestMethod]
    public void ChunkPanel_CanBeHidden()
    {
        MainWindowViewModel sut = Create();
        Assert.IsTrue(sut.IsChunkPanelVisible);

        sut.ToggleChunkPanelCommand.Execute(null);

        Assert.IsFalse(sut.IsChunkPanelVisible);
    }

    [TestMethod]
    public void NodeSummary_ShowsTheSignature()
    {
        MainWindowViewModel sut = Create();

        Choose(sut.MetaData, ContentForm.Ascii);
        sut.MetaData.Text = "PRMT";
        sut.MetaData.ApplyPending();

        Assert.AreEqual("/", sut.NodePath);
        StringAssert.Contains(sut.NodeSummary, "signature \"PRMT\"");
    }

    [TestMethod]
    public void MoveNode_DropsBeforeAfterOrInside()
    {
        MainWindowViewModel sut = CreateWithChildren("a", "b", "c");
        NodeViewModel a = sut.Root.Children[0];
        NodeViewModel b = sut.Root.Children[1];
        NodeViewModel c = sut.Root.Children[2];

        Assert.IsTrue(sut.MoveNode(c, a, DropPosition.Before));
        Assert.AreEqual("c a b", Order(sut.Root));

        Assert.IsTrue(sut.MoveNode(c, b, DropPosition.After));
        Assert.AreEqual("a b c", Order(sut.Root));

        Assert.IsTrue(sut.MoveNode(a, b, DropPosition.Inside));
        Assert.AreEqual("b c", Order(sut.Root));
        Assert.AreEqual("/0/0", a.Path);
        Assert.AreSame(a, sut.SelectedNode);
        Assert.AreEqual("a", sut.Root.Element[0][0].Data.AsAsciiString());
        Assert.IsTrue(sut.IsModified);
    }

    [TestMethod]
    public void MoveNode_RefusesTheImpossibleMoves()
    {
        MainWindowViewModel sut = CreateWithChildren("a", "b");
        NodeViewModel a = sut.Root.Children[0];
        sut.MoveNode(sut.Root.Children[1], a, DropPosition.Inside);
        NodeViewModel b = a.Children[0];

        Assert.IsFalse(sut.MoveNode(sut.Root, a, DropPosition.Inside));
        Assert.IsFalse(sut.MoveNode(a, a, DropPosition.Inside));
        Assert.IsFalse(sut.MoveNode(a, b, DropPosition.After));
        Assert.IsFalse(sut.MoveNode(b, sut.Root, DropPosition.Before));
        Assert.IsTrue(sut.MoveNode(b, sut.Root, DropPosition.Inside));
        Assert.AreEqual("a b", Order(sut.Root));
    }

    private static string[] Controls(MainWindowViewModel sut) => sut.Chunks.Select(chunk => chunk.Row.Control).ToArray();

    private static string Order(NodeViewModel parent)
    {
        string view = string.Join(" ", parent.Children.Select(child => child.Element.Data.AsAsciiString()));
        Assert.AreEqual(view, string.Join(" ", parent.Element.Children.Select(child => child.Data.AsAsciiString())));
        return view;
    }

    private static void Choose(ContentEditorViewModel editor, ContentForm form)
    {
        editor.SelectedForm = editor.Forms.Single(option => option.Form == form);
    }

    private sealed class InMemoryFormMemory : IFormMemory
    {
        private readonly Dictionary<string, FileForms> files = [];

        public FileForms Recall(string path) => files.TryGetValue(path, out FileForms? forms) ? forms : new FileForms();

        public void Remember(string path, FileForms forms) => files[path] = forms;
    }

    private MainWindowViewModel Create()
    {
        return new MainWindowViewModel(files, new ChunkAnalyzer(), clipboard, fileDialogs, messages, windows, factory, formMemory);
    }

    private MainWindowViewModel CreateWithChildren(params string[] data)
    {
        MainWindowViewModel sut = Create();

        foreach (string text in data)
        {
            sut.SelectedNode = sut.Root;
            sut.AddChildCommand.Execute(null);
            sut.SelectedNode!.SetData(Data.FromAsciiString(text));
        }

        // The content editors show the root, not the stale contents of the last child
        sut.SelectedNode = sut.Root;

        return sut;
    }

    private static string Selected(MainWindowViewModel sut) => sut.SelectedNode!.Element.Data.AsAsciiString();
}
