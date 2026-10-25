using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using PStudio.ViewModels;
using PStudio.Views;
using Promptuarium;

namespace PStudio.Headless.Tests;

[TestClass]
public sealed class EditorScenarios
{
    [TestMethod]
    public Task OpenedTreeIsShown_AndTheSelectedNodeIsEditable() => Scenario.Run(async editor =>
    {
        string path = Path.Combine(editor.Directory, "tree.prmt");
        var tree = new ElementBuilder()
            .WithSignature(Data.FromAsciiString("PRMT"))
            .WithChildren(
                new Element(Data.FromUtf8String("hello"), new byte[] { 1, 2 }),
                new Element(new byte[] { 0, 255 }, null, new Element(Data.FromAsciiString("deep"), null)))
            .Build();
        await tree.SaveAsync(path);

        Assert.IsTrue(await editor.ViewModel.OpenFileAsync(path));
        Editor.Pump();

        Assert.AreEqual("tree.prmt - Promptuarium Studio", editor.Window.Title);
        Assert.AreEqual(
            "root  [\"PRMT\"] | #0  \"hello\"  [0102] | #1  00FF",
            string.Join(" | ", editor.Find<TreeViewItem>().Select(item => ((NodeViewModel)item.DataContext!).Header)));

        // Selecting in the tree view selects in the view model
        TreeView treeView = editor.Find<TreeView>().Single();
        treeView.SelectedItem = editor.ViewModel.Root.Children[0];
        Editor.Pump();

        Assert.AreSame(editor.ViewModel.Root.Children[0], editor.ViewModel.SelectedNode);
        Assert.AreEqual("hello | 01 02", string.Join(" | ", Editors(editor).Select(box => box.Text)));
        editor.Screenshot("node");

        // Typing into the data editor and applying with Ctrl+Enter
        TextBox data = Editors(editor)[0];
        data.Focus();
        data.Text = "hello, world";
        data.SelectionStart = data.SelectionEnd = data.Text.Length;
        editor.Window.KeyPress(Key.Enter, RawInputModifiers.Control, PhysicalKey.Enter, null);
        Editor.Pump();

        Assert.AreEqual("hello, world", editor.ViewModel.Root.Element[0].Data.AsUtf8String());
        Assert.AreEqual("tree.prmt * - Promptuarium Studio", editor.Window.Title);
    });

    [TestMethod]
    public Task KeysOfTheTreeView_EditTheTree() => Scenario.Run(editor =>
    {
        TreeView treeView = editor.Find<TreeView>().Single();
        editor.Find<TreeViewItem>().First().Focus();
        Editor.Pump();
        Assert.IsTrue(treeView.IsKeyboardFocusWithin, editor.Window.FocusManager?.GetFocusedElement()?.ToString() ?? "nothing is focused");

        editor.Window.KeyPress(Key.Insert, RawInputModifiers.None, PhysicalKey.Insert, null);
        editor.Window.KeyPress(Key.Insert, RawInputModifiers.Control, PhysicalKey.Insert, null);
        editor.Window.KeyPress(Key.Insert, RawInputModifiers.Control, PhysicalKey.Insert, null);
        Editor.Pump();

        Assert.AreEqual(3, editor.ViewModel.Root.Element.Children.Count, editor.ViewModel.StatusText);
        Assert.AreEqual("/2", editor.ViewModel.SelectedNode!.Path);

        // Alt+Up moves the node, and the tree view keeps it selected
        NodeViewModel moved = editor.ViewModel.SelectedNode;
        editor.Window.KeyPress(Key.Up, RawInputModifiers.Alt, PhysicalKey.ArrowUp, null);
        Editor.Pump();

        Assert.AreEqual("/1", moved.Path);
        Assert.AreSame(moved, editor.ViewModel.SelectedNode);
        Assert.AreSame(moved, treeView.SelectedItem);

        // Alt+Right indents it under its previous sibling
        editor.Window.KeyPress(Key.Right, RawInputModifiers.Alt, PhysicalKey.ArrowRight, null);
        Editor.Pump();

        Assert.AreEqual("/0/0", moved.Path);
        Assert.AreSame(moved, treeView.SelectedItem);
        Assert.AreEqual(4, editor.Find<TreeViewItem>().Count());

        editor.Window.KeyPress(Key.Delete, RawInputModifiers.None, PhysicalKey.Delete, null);
        Editor.Pump();

        Assert.AreEqual(0, editor.ViewModel.Root.Element[0].Children.Count);
        Assert.AreEqual("/0", editor.ViewModel.SelectedNode!.Path);
        return Task.CompletedTask;
    });

    [TestMethod]
    public Task ChunkView_ListsTheChunks() => Scenario.Run(editor =>
    {
        editor.ViewModel.AddChildCommand.Execute(null);
        editor.ViewModel.SelectedNode!.SetData(Data.FromAsciiString("abc"));

        TabControl tabs = editor.Find<TabControl>().Single();
        tabs.SelectedIndex = 1;
        Editor.Pump();

        Assert.IsTrue(editor.ViewModel.IsChunkViewActive);
        CollectionAssert.AreEqual(new[] { "40", "43", "00" }, editor.ViewModel.Chunks.Select(chunk => chunk.Control).ToArray());
        Assert.AreEqual(3, editor.Find<ListBoxItem>().Count());
        editor.Screenshot("chunks");
        return Task.CompletedTask;
    });

    [TestMethod]
    public Task ClosingWithUnsavedChanges_Asks() => Scenario.Run(editor =>
    {
        editor.ViewModel.AddChildCommand.Execute(null);

        editor.Window.Close();
        Editor.Pump();

        MessageDialog dialog = editor.Window.OwnedWindows.OfType<MessageDialog>().Single();
        editor.Screenshot("unsaved");
        Press(dialog, "Cancel");
        Editor.Pump();

        Assert.IsTrue(editor.Window.IsVisible);

        editor.Window.Close();
        Editor.Pump();
        Press(editor.Window.OwnedWindows.OfType<MessageDialog>().Single(), "Don't save");
        Editor.Pump();

        Assert.IsFalse(editor.Window.IsVisible);
        return Task.CompletedTask;
    });

    [TestMethod]
    public Task NestedTree_OpensInItsOwnWindow_AndSavesIntoTheNode() => Scenario.Run(editor =>
    {
        editor.ViewModel.Root.SetData(new Element(Data.FromAsciiString("inner"), null).ToBytes());

        editor.ViewModel.OpenNestedCommand.Execute(null);
        Editor.Pump();

        MainWindow nested = editor.Window.OwnedWindows.OfType<MainWindow>().Single();
        var viewModel = (MainWindowViewModel)nested.DataContext!;
        Assert.AreEqual("untitled / (nested) - Promptuarium Studio", nested.Title);

        viewModel.AddChildCommand.Execute(null);
        viewModel.SaveCommand.Execute(null);
        Editor.Pump();

        Assert.AreEqual(1, Element.FromBytes(editor.ViewModel.Root.Element.Data).Children.Count);
        Assert.AreEqual("untitled * - Promptuarium Studio", editor.Window.Title);

        // Closing the nested window after saving asks nothing
        nested.Close();
        Editor.Pump();
        Assert.IsFalse(nested.IsVisible);
        Assert.IsFalse(editor.ViewModel.HasUnsavedChanges && !editor.ViewModel.IsModified);
        return Task.CompletedTask;
    });

    private static TextBox[] Editors(Editor editor)
    {
        return editor.Find<ContentEditorView>().Select(view => view.GetVisualDescendantsOf<TextBox>().Single(box => box.AcceptsReturn)).ToArray();
    }

    private static void Press(Window dialog, string caption)
    {
        Button button = dialog.GetVisualDescendantsOf<Button>().Single(candidate => (string?)candidate.Content == caption);
        button.Command!.Execute(button.CommandParameter);
    }
}

internal static class VisualExtensions
{
    public static IEnumerable<T> GetVisualDescendantsOf<T>(this Avalonia.Visual visual)
        where T : Avalonia.Visual
    {
        return Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(visual).OfType<T>();
    }
}
