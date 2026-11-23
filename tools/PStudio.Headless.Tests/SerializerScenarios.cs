using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
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
        TextBox text = editor.Window.FindControl<TextBox>("SerializerTextBox")!;
        Assert.IsTrue(text.IsEffectivelyVisible);
        StringAssert.Contains(text.Text, "Title: demo");
        editor.ViewModel.IsJsonShown = true;
        Editor.Pump();
        StringAssert.Contains(text.Text, "\"Title\": \"demo\"");
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

        // The comment is edited in the row too, above it; there is no other comment box
        Assert.IsFalse(editor.SerializerPanel().GetVisualDescendants().OfType<TextBox>().Any(box => Equals(box.Tag, "comment") && box.IsEffectivelyVisible));
        ((TreeViewItem)editor.SerializerTree.TreeContainerFromItem(countNode)!).Focus();
        Editor.Pump();
        editor.Window.KeyPress(Key.F2, RawInputModifiers.None, PhysicalKey.F2, null);
        Editor.Pump();
        TextBox comment = RowBox(editor, countNode, "comment");
        comment.Focus();
        comment.Text = "the total";
        editor.Window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Editor.Pump();
        Assert.IsFalse(countNode.IsEditing);
        Assert.AreEqual("the total", editor.ViewModel.Root.Element[1][1].Data.AsUtf8String());
        Assert.IsTrue(editor.SerializerTree.GetVisualDescendants().OfType<TextBlock>().Any(block => block.Classes.Contains("comment") && block.Text == "the total" && block.IsEffectivelyVisible));

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

    private static TextBox RowBox(Editor editor, SerializerNodeViewModel node, string field)
    {
        return editor.SerializerTree.GetVisualDescendants().OfType<TextBox>()
            .Single(box => Equals(box.Tag, field) && box.DataContext == node && box.IsEffectivelyVisible);
    }
}
