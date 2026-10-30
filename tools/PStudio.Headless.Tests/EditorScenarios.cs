using Avalonia;
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
    public Task ChunkPanel_HighlightsTheChunksOfTheSelectedNode() => Scenario.Run(editor =>
    {
        editor.ViewModel.AddChildCommand.Execute(null);
        editor.ViewModel.Data.Text = "abc";
        editor.ViewModel.Data.ApplyPending();
        Editor.Pump();

        CollectionAssert.AreEqual(new[] { "40", "43", "00" }, editor.ViewModel.Chunks.Select(chunk => chunk.Row.Control).ToArray());
        DataGridRow[] rows = editor.Find<DataGridRow>().OrderBy(row => row.Index).ToArray();
        Assert.AreEqual(3, rows.Length);
        CollectionAssert.AreEqual(new[] { false, true, false }, rows.Select(row => row.Classes.Contains("ofSelectedNode")).ToArray());
        editor.Screenshot("chunks");

        // A click on the first chunk selects the root
        ClickAt(editor, rows[0], new Point(20, 8), MouseButton.Left);
        Assert.AreSame(editor.ViewModel.Root, editor.ViewModel.SelectedNode);

        // The columns can be resized
        Assert.IsTrue(editor.Find<DataGrid>().Single().CanUserResizeColumns);

        // View / Chunks hides the panel, and gives its place to the editor; F6 shows it again
        Grid grid = editor.Window.FindControl<Grid>("MainGrid")!;
        MenuItem view = editor.Find<MenuItem>().Single(item => (string?)item.Header == "_View");
        view.Open();
        Editor.Pump();
        MenuItem chunks = view.Items.OfType<MenuItem>().Single();
        chunks.Command!.Execute(chunks.CommandParameter);
        view.Close();
        Editor.Pump();

        Assert.IsFalse(editor.ViewModel.IsChunkPanelVisible);
        Assert.IsFalse(chunks.IsChecked);
        Assert.AreEqual(0, grid.RowDefinitions[2].ActualHeight);
        Assert.IsFalse(editor.Find<DataGridRow>().Any(row => row.IsEffectivelyVisible));

        editor.Window.KeyPress(Key.F6, RawInputModifiers.None, PhysicalKey.F6, null);
        Editor.Pump();
        Assert.IsTrue(editor.ViewModel.IsChunkPanelVisible);
        Assert.IsTrue(chunks.IsChecked);
        Assert.AreEqual(220, grid.RowDefinitions[2].ActualHeight);
        return Task.CompletedTask;
    });

    [TestMethod]
    public Task Nodes_CanBeDraggedAndDropped() => Scenario.Run(editor =>
    {
        foreach (string text in new[] { "a", "b", "c" })
        {
            editor.ViewModel.SelectedNode = editor.ViewModel.Root;
            editor.ViewModel.AddChildCommand.Execute(null);
            editor.ViewModel.SelectedNode!.SetData(Data.FromAsciiString(text));
        }

        Editor.Pump();

        // c into the middle of a: it becomes the child of a
        Drag(editor, Header(editor, "c"), Header(editor, "a"), 0.5);
        Assert.AreEqual("a(c) b", Shape(editor.ViewModel.Root));

        // b onto the upper edge of a: before a
        Drag(editor, Header(editor, "b"), Header(editor, "a"), 0.1);
        Assert.AreEqual("b a(c)", Shape(editor.ViewModel.Root));
        Assert.AreEqual("b", editor.ViewModel.SelectedNode!.Element.Data.AsAsciiString());
        Assert.IsTrue(editor.ViewModel.IsModified);

        // a onto its own child: nothing happens
        Drag(editor, Header(editor, "a"), Header(editor, "c"), 0.5);
        Assert.AreEqual("b a(c)", Shape(editor.ViewModel.Root));
        return Task.CompletedTask;
    });

    [TestMethod]
    public Task ContextMenuOfTheTree_SelectsTheNodeAndHasTheCommands() => Scenario.Run(editor =>
    {
        editor.ViewModel.AddChildCommand.Execute(null);
        editor.ViewModel.SelectedNode = editor.ViewModel.Root;
        Editor.Pump();

        Border child = editor.Find<Border>().Where(border => border.Classes.Contains("nodeHeader")).ElementAt(1);
        ClickAt(editor, child, new Point(10, 5), MouseButton.Right);

        ContextMenu menu = editor.Find<TreeView>().Single().ContextMenu!;
        Assert.IsTrue(menu.IsOpen);
        Assert.AreSame(editor.ViewModel.Root.Children[0], editor.ViewModel.SelectedNode);
        editor.Screenshot("context-menu");

        MenuItem delete = menu.Items.OfType<MenuItem>().Single(item => (string?)item.Header == "Delete");
        Assert.IsTrue(delete.Command!.CanExecute(null));
        delete.Command.Execute(null);
        menu.Close();
        Editor.Pump();

        Assert.AreEqual(0, editor.ViewModel.Root.Children.Count);
        return Task.CompletedTask;
    });

    [TestMethod]
    public Task TypedForms_CanBeChosenAndEdited() => Scenario.Run(editor =>
    {
        ComboBox forms = editor.Find<ContentEditorView>().First().GetVisualDescendantsOf<ComboBox>().Single();
        forms.SelectedItem = forms.Items.OfType<PStudio.Models.FormOption>().Single(option => option.Form == PStudio.Models.ContentForm.Double);
        TextBox data = Editors(editor)[0];
        data.Text = "2.5";
        editor.ViewModel.Data.ApplyPending();
        Editor.Pump();

        Assert.AreEqual(2.5, editor.ViewModel.Root.Element.Data.AsDouble());
        editor.Screenshot("double");
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

    private static Border Header(Editor editor, string data)
    {
        return editor.Find<Border>().Single(border => border.Classes.Contains("nodeHeader") && border.DataContext is NodeViewModel node && node.Element.Data.AsAsciiString() == data && !node.IsRoot);
    }

    // Presses the left button on the source, moves it in steps to the given height of the target, and releases it
    private static void Drag(Editor editor, Border source, Border target, double height)
    {
        Point from = source.TranslatePoint(new Point(8, source.Bounds.Height / 2), editor.Window)!.Value;
        Point to = target.TranslatePoint(new Point(8, target.Bounds.Height * height), editor.Window)!.Value;

        editor.Window.MouseDown(from, MouseButton.Left);

        for (int step = 1; step <= 5; step++)
        {
            editor.Window.MouseMove(from + ((to - from) * (step / 5.0)));
            Editor.Pump();
        }

        editor.Window.MouseUp(to, MouseButton.Left);
        Editor.Pump();
    }

    private static void ClickAt(Editor editor, Control control, Point point, MouseButton button)
    {
        Point position = control.TranslatePoint(point, editor.Window)!.Value;
        editor.Window.MouseDown(position, button);
        editor.Window.MouseUp(position, button);
        Editor.Pump();
    }

    private static string Shape(NodeViewModel node)
    {
        return string.Join(" ", node.Children.Select(child =>
            child.Element.Data.AsAsciiString() + (child.Children.Count > 0 ? $"({Shape(child)})" : string.Empty)));
    }

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
