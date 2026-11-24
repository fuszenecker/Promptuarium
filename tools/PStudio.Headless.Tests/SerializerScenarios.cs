using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using PStudio.ViewModels;
using Promptuarium;
using Promptuarium.Serializer;

namespace PStudio.Headless.Tests;

[TestClass]
public sealed class SerializerScenarios
{
    public sealed class Settings
    {
        public string Title { get; set; } = "demo";

        public int Count { get; set; } = 3;

        public bool Enabled { get; set; } = true;

        public List<string> Tags { get; set; } = ["a", "b"];

        public Audio Audio { get; set; } = new();
    }

    [PromptuariumComment("The audio device")]
    public sealed class Audio
    {
        public double Rate { get; set; } = 44100;

        public DateTimeOffset Since { get; set; } = new(2026, 11, 22, 19, 30, 15, TimeSpan.FromHours(1));
    }

    [TestMethod]
    public Task SerializerView_EditsInPlace_AndFollowsTheRawTree() => Scenario.Run(async editor =>
    {
        editor.Window.Width = 1200;
        editor.Window.Height = 800;
        string path = Path.Combine(editor.Directory, "settings.prmt");
        await File.WriteAllBytesAsync(path, PromptuariumSerializer.Serialize(new Settings()));

        Assert.IsTrue(await editor.ViewModel.OpenFileAsync(path));
        editor.Screenshot("raw");

        // The serializer view is on its own tab
        Assert.IsTrue(editor.Window.FindControl<TabItem>("SerializerTab")!.IsVisible);
        editor.Window.FindControl<TabItem>("SerializerTab")!.IsSelected = true;
        Editor.Pump();
        Assert.AreEqual(MainWindowViewModel.SerializerView, editor.ViewModel.SelectedViewIndex);

        // The editors of the metadata and the data are not there: the serializer view edits in its tree
        Assert.IsFalse(editor.Find<PStudio.Views.ContentEditorView>().Any(view => view.IsEffectivelyVisible));
        PStudio.Views.HighlightedText text = editor.Window.FindControl<PStudio.Views.HighlightedText>("SerializerText")!;
        Assert.IsTrue(text.IsEffectivelyVisible);
        StringAssert.Contains(text.Source, "Title: demo");
        Assert.IsTrue(text.Inlines!.OfType<Avalonia.Controls.Documents.Run>().Any(run => run.Text == "Title" && run.Classes.Contains("key")));
        editor.Screenshot("serializer-yaml");
        editor.ViewModel.IsJsonShown = true;
        Editor.Pump();
        StringAssert.Contains(text.Source, "\"Title\": \"demo\"");
        Assert.IsTrue(text.Inlines!.OfType<Avalonia.Controls.Documents.Run>().Any(run => run.Text == "\"demo\"" && run.Classes.Contains("string")));
        editor.ViewModel.SerializerRoots[0].Children[4].IsExpanded = true;
        editor.ViewModel.SerializerRoots[0].Children[3].IsExpanded = true;
        Editor.Pump();

        editor.Screenshot("serializer");

        // The rows are texts: nothing can be typed into them until a row is edited
        Assert.IsFalse(editor.SerializerTree.GetVisualDescendants().OfType<TextBox>().Any(box => box.IsEffectivelyVisible));
        Assert.IsFalse(editor.SerializerTree.GetVisualDescendants().OfType<ComboBox>().Any(box => box.IsEffectivelyVisible));

        // F2 edits the whole row: its name gets the focus; Enter applies the name and the value
        SerializerNodeViewModel countNode = editor.ViewModel.SerializerRoots[0].Children[1];
        ((TreeViewItem)editor.SerializerTree.TreeContainerFromItem(countNode)!).Focus();
        Editor.Pump();
        Assert.AreSame(editor.ViewModel.Root.Children[1], editor.ViewModel.SelectedNode);
        editor.Window.KeyPress(Key.F2, RawInputModifiers.None, PhysicalKey.F2, null);
        Editor.Pump();
        Assert.IsTrue(countNode.IsEditing);
        TextBox name = RowBox(editor, countNode, "name");
        Assert.IsTrue(name.IsFocused);
        Assert.IsTrue(editor.SerializerTree.GetVisualDescendants().OfType<ComboBox>().Single(box => box.IsEffectivelyVisible).DataContext == countNode);

        name.Text = "Total";
        TextBox count = RowBox(editor, countNode, "value");
        count.Text = "42";
        editor.Window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Editor.Pump();
        Assert.AreEqual("Total", editor.ViewModel.Root.Element[1].Data.AsUtf8String());
        Assert.AreEqual(42, editor.ViewModel.Root.Element[1][1].Data.AsInt());
        Assert.IsFalse(countNode.IsEditing);

        // An invalid value is not applied, and the row stays edited; Escape drops the edits
        editor.Window.KeyPress(Key.F2, RawInputModifiers.None, PhysicalKey.F2, null);
        Editor.Pump();
        count = RowBox(editor, countNode, "value");
        count.Focus();
        count.Text = "forty";
        editor.Window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Editor.Pump();
        Assert.IsTrue(countNode.IsEditing);
        Assert.IsTrue(count.Classes.Contains("invalid"));
        Assert.AreEqual(42, editor.ViewModel.Root.Element[1][1].Data.AsInt());
        editor.Screenshot("serializer-invalid");
        editor.Window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        Editor.Pump();
        Assert.IsFalse(countNode.IsEditing);
        Assert.AreEqual("42", countNode.ValueDisplay);

        // The comment is edited in the row too, under it; there is no other comment box
        Assert.IsFalse(editor.SerializerPanel().GetVisualDescendants().OfType<TextBox>().Any(box => Equals(box.Tag, "comment") && box.IsEffectivelyVisible));
        ((TreeViewItem)editor.SerializerTree.TreeContainerFromItem(countNode)!).Focus();
        Editor.Pump();
        editor.Window.KeyPress(Key.F2, RawInputModifiers.None, PhysicalKey.F2, null);
        Editor.Pump();
        TextBox comment = RowBox(editor, countNode, "comment");
        comment.Focus();
        comment.Text = "the";
        comment.CaretIndex = 3;

        // The box of the comment spans the name, the type and the value, three lines high; Shift+Enter breaks a line
        TextBox nameBox = RowBox(editor, countNode, "name");
        TextBox valueBox = RowBox(editor, countNode, "value");
        Point commentLeft = comment.TranslatePoint(default, editor.SerializerTree)!.Value;
        Assert.AreEqual(nameBox.TranslatePoint(default, editor.SerializerTree)!.Value.X, commentLeft.X, 1);
        Assert.AreEqual(valueBox.TranslatePoint(new Point(valueBox.Bounds.Width, 0), editor.SerializerTree)!.Value.X, commentLeft.X + comment.Bounds.Width, 1);
        Assert.AreEqual(TextWrapping.Wrap, comment.TextWrapping);
        Assert.AreEqual(3, comment.MinLines);
        Assert.AreEqual(nameBox.FontSize, comment.FontSize);
        editor.Window.KeyPress(Key.Enter, RawInputModifiers.Shift, PhysicalKey.Enter, null);
        Editor.Pump();
        Assert.IsTrue(countNode.IsEditing);
        comment.Text += "total";
        editor.Screenshot("serializer-comment");
        editor.Window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Editor.Pump();
        Assert.IsFalse(countNode.IsEditing);
        Assert.AreEqual("the\ntotal", editor.ViewModel.Root.Element[1][1].Data.AsUtf8String());
        Assert.AreEqual("the\ntotal", ToolTip.GetTip(RowOf(editor, countNode)));

        // A boolean is edited with a check box in the middle of its row
        SerializerNodeViewModel enabledNode = editor.ViewModel.SerializerRoots[0].Children[2];
        ((TreeViewItem)editor.SerializerTree.TreeContainerFromItem(enabledNode)!).Focus();
        Editor.Pump();
        editor.Window.KeyPress(Key.F2, RawInputModifiers.None, PhysicalKey.F2, null);
        Editor.Pump();
        CheckBox check = editor.SerializerTree.GetVisualDescendants().OfType<CheckBox>().Single(box => box.IsEffectivelyVisible);
        ComboBox type = editor.SerializerTree.GetVisualDescendants().OfType<ComboBox>().Single(box => box.IsEffectivelyVisible);
        Visual box = check.GetVisualDescendants().OfType<Border>().FirstOrDefault(border => border.Name == "NormalRectangle") ?? (Visual)check;
        Avalonia.Point checkMiddle = box.TranslatePoint(new Avalonia.Point(0, box.Bounds.Height / 2), editor.SerializerTree)!.Value;
        Avalonia.Point typeMiddle = type.TranslatePoint(new Avalonia.Point(0, type.Bounds.Height / 2), editor.SerializerTree)!.Value;
        Assert.AreEqual(typeMiddle.Y, checkMiddle.Y, 1);
        editor.Screenshot("serializer-bool");
        editor.Window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        Editor.Pump();
        ((TreeViewItem)editor.SerializerTree.TreeContainerFromItem(countNode)!).Focus();
        Editor.Pump();

        // Leaving an edited row applies it
        editor.Window.KeyPress(Key.F2, RawInputModifiers.None, PhysicalKey.F2, null);
        Editor.Pump();
        RowBox(editor, countNode, "value").Text = "7";
        ((TreeViewItem)editor.SerializerTree.TreeContainerFromItem(editor.ViewModel.SerializerRoots[0].Children[0])!).Focus();
        Editor.Pump();
        Assert.IsFalse(countNode.IsEditing);
        Assert.AreEqual(7, editor.ViewModel.Root.Element[1].Children.Single(child => child.MetaData.Span[0] == 0x03).Data.AsInt());

        // A node selected in the raw tree selects its holder in the serializer view
        editor.Tree.SelectedItem = editor.ViewModel.Root.Children[4].Children[1];
        Editor.Pump();
        Assert.AreSame(editor.ViewModel.SerializerRoots[0].Children[4], editor.SerializerTree.SelectedItem);

        // Breaking the tree in the raw view hides the tab of the serializer view, and shows the raw tree
        editor.ViewModel.SelectedNode = editor.ViewModel.Root;
        editor.ViewModel.AddChildCommand.Execute(null);
        Editor.Pump();
        Assert.IsFalse(editor.Window.FindControl<TabItem>("SerializerTab")!.IsVisible);
        Assert.AreEqual(MainWindowViewModel.RawView, editor.ViewModel.SelectedViewIndex);
        Assert.IsTrue(editor.Tree.IsEffectivelyVisible);
        Assert.IsTrue(editor.Find<PStudio.Views.ContentEditorView>().All(view => view.IsEffectivelyVisible));
        editor.Screenshot("serializer-closed");
    });

    private static Grid RowOf(Editor editor, SerializerNodeViewModel node)
    {
        return editor.SerializerTree.GetVisualDescendants().OfType<Grid>().First(grid => grid.DataContext == node && grid.RowDefinitions.Count == 2);
    }

    private static TextBox RowBox(Editor editor, SerializerNodeViewModel node, string field)
    {
        return editor.SerializerTree.GetVisualDescendants().OfType<TextBox>()
            .Single(box => Equals(box.Tag, field) && box.DataContext == node && box.IsEffectivelyVisible);
    }
}
