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

        // A value typed into its row, applied with Enter
        TextBox count = ValueBox(editor, "Count");
        count.Focus();
        Editor.Pump();
        Assert.AreSame(editor.ViewModel.Root.Children[1], editor.ViewModel.SelectedNode);

        count.Text = "42";
        editor.Window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Editor.Pump();
        Assert.AreEqual(42, editor.ViewModel.Root.Element[1][1].Data.AsInt());

        // An invalid value is not applied
        count.Text = "forty";
        editor.Window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Editor.Pump();
        Assert.IsTrue(count.Classes.Contains("invalid"));
        Assert.AreEqual(42, editor.ViewModel.Root.Element[1][1].Data.AsInt());
        editor.Screenshot("serializer-invalid");
        editor.Window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        Editor.Pump();
        Assert.AreEqual("42", count.Text);

        // A name is a text; F2 (or a double click) makes it a box, Enter renames the member
        Assert.IsFalse(editor.SerializerTree.GetVisualDescendants().OfType<TextBox>().Any(box => Equals(box.Tag, "name") && box.IsEffectivelyVisible));
        SerializerNodeViewModel title = editor.ViewModel.SerializerRoots[0].Children[0];
        ((TreeViewItem)editor.SerializerTree.TreeContainerFromItem(title)!).Focus();
        Editor.Pump();
        editor.Window.KeyPress(Key.F2, RawInputModifiers.None, PhysicalKey.F2, null);
        Editor.Pump();
        TextBox name = editor.SerializerTree.GetVisualDescendants().OfType<TextBox>().Single(box => Equals(box.Tag, "name") && box.IsEffectivelyVisible);
        Assert.IsTrue(name.IsFocused);
        name.Text = "Caption";
        editor.Window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Editor.Pump();
        Assert.AreEqual("Caption", editor.ViewModel.Root.Element[0].Data.AsUtf8String());
        Assert.IsFalse(title.IsEditingName);

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

    private static TextBox ValueBox(Editor editor, string name)
    {
        return editor.SerializerTree.GetVisualDescendants().OfType<TextBox>()
            .Single(box => Equals(box.Tag, "value") && box.DataContext is SerializerNodeViewModel node && node.NameText == name);
    }
}
