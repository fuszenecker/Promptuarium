using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using PStudio.ViewModels;
using PStudio.Views;
using Promptuarium;
using Promptuarium.Serializer;

namespace PStudio.Headless.Tests;

/// <summary>
/// The keyboard in the serializer view and in the raw tree: the arrows, Home, End, Page Up, Page Down, editing a row
/// (F2, Enter, Tab, Shift+Tab, Space, Escape), adding, deleting and moving values, switching the tabs; and the comment
/// as a tooltip, the colored YAML and JSON, the texts that are not there.
/// </summary>
[TestClass]
public sealed class SerializerKeyboardScenarios
{
    // The rows of SerializerScenarios.Settings: the root, Title, Count, Enabled, Tags (a, b), Audio (Rate, Since)
    private const int Title = 0;
    private const int Count = 1;
    private const int Enabled = 2;
    private const int Tags = 3;
    private const int Audio = 4;

    // ---------------------------------------------------------------- opening

    [TestMethod]
    public Task OpenedSerializableTree_HasTheKeyboardOnItsRoot() => Scenario.Run(async editor =>
    {
        await Open(editor);

        Assert.IsTrue(editor.Window.FindControl<TabItem>("SerializerTab")!.IsSelected);
        AssertRowFocused(editor, Root(editor));

        Press(editor, Key.Down);
        AssertRowFocused(editor, Member(editor, Title));
    });

    [TestMethod]
    public Task OpenedTreeThatIsNotSerializable_HasTheKeyboardInTheRawTree() => Scenario.Run(async editor =>
    {
        string path = Path.Combine(editor.Directory, "raw.prmt");
        await new Element().Add(new Element(), new Element()).SaveAsync(path);

        Assert.IsTrue(await editor.ViewModel.OpenFileAsync(path));
        Editor.Pump();

        Assert.AreEqual(MainWindowViewModel.RawView, editor.ViewModel.SelectedViewIndex);
        Assert.IsFalse(editor.Window.FindControl<TabItem>("SerializerTab")!.IsVisible);
        AssertRawFocused(editor, editor.ViewModel.Root);

        Press(editor, Key.Down);
        AssertRawFocused(editor, editor.ViewModel.Root.Children[0]);
    });

    // ---------------------------------------------------------------- arrows

    [TestMethod]
    public Task UpAndDown_MoveTheSelection_AndTheRawTreeFollows() => Scenario.Run(async editor =>
    {
        await Open(editor);

        foreach (int index in new[] { Title, Count, Enabled, Tags, Audio })
        {
            Press(editor, Key.Down);
            AssertRowFocused(editor, Member(editor, index));
            Assert.AreSame(Member(editor, index).Holder, editor.ViewModel.SelectedNode);
        }

        // The last row stays the last one
        Press(editor, Key.Down);
        AssertRowFocused(editor, Member(editor, Audio));

        Press(editor, Key.Up);
        Press(editor, Key.Up);
        AssertRowFocused(editor, Member(editor, Enabled));
        Assert.AreSame(Member(editor, Enabled).Holder, editor.ViewModel.SelectedNode);
    });

    [TestMethod]
    public Task RightAndLeft_ExpandCollapseAndGoInAndOut() => Scenario.Run(async editor =>
    {
        await Open(editor);
        Select(editor, Member(editor, Tags));
        Assert.IsFalse(Member(editor, Tags).IsExpanded);

        Press(editor, Key.Right);
        Assert.IsTrue(Member(editor, Tags).IsExpanded);
        AssertRowFocused(editor, Member(editor, Tags));

        Press(editor, Key.Right);
        AssertRowFocused(editor, Member(editor, Tags).Children[0]);

        Press(editor, Key.Down);
        AssertRowFocused(editor, Member(editor, Tags).Children[1]);

        Press(editor, Key.Left);
        AssertRowFocused(editor, Member(editor, Tags));
        Assert.IsTrue(Member(editor, Tags).IsExpanded);

        Press(editor, Key.Left);
        Assert.IsFalse(Member(editor, Tags).IsExpanded);
        AssertRowFocused(editor, Member(editor, Tags));

        Press(editor, Key.Down);
        AssertRowFocused(editor, Member(editor, Audio));
    });

    // ---------------------------------------------------------------- Home, End, Page Up, Page Down

    [TestMethod]
    public Task HomeAndEnd_GoToTheFirstAndTheLastShownRow() => Scenario.Run(async editor =>
    {
        await Open(editor);
        Select(editor, Member(editor, Count));

        Press(editor, Key.End);
        AssertRowFocused(editor, Member(editor, Audio));

        Press(editor, Key.Home);
        AssertRowFocused(editor, Root(editor));

        // An expanded value: End goes into it
        Member(editor, Audio).IsExpanded = true;
        Editor.Pump();
        Press(editor, Key.End);
        AssertRowFocused(editor, Member(editor, Audio).Children[^1]);
        Assert.AreEqual("Since", editor.ViewModel.SelectedSerializerNode!.NameText);

        // A collapsed root: Home and End stay on it
        Press(editor, Key.Home);
        Press(editor, Key.Left);
        Assert.IsFalse(Root(editor).IsExpanded);
        Press(editor, Key.End);
        AssertRowFocused(editor, Root(editor));
    });

    [TestMethod]
    public Task PageDownAndPageUp_MoveByAPage_AndKeepTheRowInSight() => Scenario.Run(async editor =>
    {
        await OpenMany(editor, 80);
        SerializerNodeViewModel root = Root(editor);

        Press(editor, Key.PageDown);
        int first = root.Children.IndexOf(editor.ViewModel.SelectedSerializerNode!);
        Assert.IsTrue(first > 2, $"a page down went to the row {first}");
        Assert.IsTrue(first < 40, $"a page down went to the row {first}");
        AssertRowFocused(editor, root.Children[first]);
        AssertInSight(editor, root.Children[first]);

        Press(editor, Key.PageDown);
        int second = root.Children.IndexOf(editor.ViewModel.SelectedSerializerNode!);
        Assert.AreEqual(first + 1 + first, second, "a page is the same number of rows");
        AssertInSight(editor, root.Children[second]);

        for (int i = 0; i < 20; i++)
        {
            Press(editor, Key.PageDown);
        }

        AssertRowFocused(editor, root.Children[^1]);
        AssertInSight(editor, root.Children[^1]);

        Press(editor, Key.PageUp);
        int up = root.Children.IndexOf(editor.ViewModel.SelectedSerializerNode!);
        Assert.AreEqual(root.Children.Count - 1 - (first + 1), up);
        AssertInSight(editor, root.Children[up]);

        for (int i = 0; i < 20; i++)
        {
            Press(editor, Key.PageUp);
        }

        AssertRowFocused(editor, root);
        AssertInSight(editor, root);

        Press(editor, Key.End);
        AssertInSight(editor, root.Children[^1]);
        Press(editor, Key.Home);
        AssertInSight(editor, root);
    });

    [TestMethod]
    public Task HomeEndAndPages_InTheRawTree() => Scenario.Run(async editor =>
    {
        await OpenMany(editor, 40);
        editor.ShowRawView();
        NodeViewModel root = editor.ViewModel.Root;
        AssertRawFocused(editor, root);

        Press(editor, Key.End);
        AssertRawFocused(editor, root.Children[^1]);

        Press(editor, Key.Home);
        AssertRawFocused(editor, root);

        Press(editor, Key.PageDown);
        Assert.AreNotSame(root, editor.ViewModel.SelectedNode);
        Assert.AreSame(root, editor.ViewModel.SelectedNode!.Parent);
        AssertRawFocused(editor, editor.ViewModel.SelectedNode!);

        Press(editor, Key.PageUp);
        AssertRawFocused(editor, root);
    });

    [TestMethod]
    public Task JumpKeys_WithModifiers_AreLeftToTheTree() => Scenario.Run(async editor =>
    {
        await Open(editor);
        Select(editor, Member(editor, Count));

        Press(editor, Key.End, RawInputModifiers.Shift);
        Press(editor, Key.Home, RawInputModifiers.Control);

        Assert.AreNotSame(Root(editor), editor.ViewModel.SelectedSerializerNode);
        Assert.AreNotSame(Member(editor, Audio), editor.ViewModel.SelectedSerializerNode);
    });

    // ---------------------------------------------------------------- editing a row

    [TestMethod]
    public Task F2AndEnter_EditTheRow_WithTheKeyboardInTheName() => Scenario.Run(async editor =>
    {
        await Open(editor);
        SerializerNodeViewModel count = Member(editor, Count);
        Select(editor, count);

        Press(editor, Key.F2);
        Assert.IsTrue(count.IsEditing);
        Assert.AreEqual("name", FocusedField(editor, count));
        Assert.AreEqual("Count", ((TextBox)Focused(editor)!).SelectedText);

        Press(editor, Key.Escape);
        Assert.IsFalse(count.IsEditing);
        AssertRowFocused(editor, count);

        Press(editor, Key.Enter);
        Assert.IsTrue(count.IsEditing);
        Assert.AreEqual("name", FocusedField(editor, count));

        Press(editor, Key.Escape);
        AssertRowFocused(editor, count);
    });

    [TestMethod]
    public Task EditedElement_HasTheKeyboardInItsValue() => Scenario.Run(async editor =>
    {
        await Open(editor);
        Member(editor, Tags).IsExpanded = true;
        Editor.Pump();
        SerializerNodeViewModel element = Member(editor, Tags).Children[1];
        Select(editor, element);

        Press(editor, Key.F2);

        Assert.AreEqual("value", FocusedField(editor, element));
        Assert.AreEqual("b", ((TextBox)Focused(editor)!).SelectedText);
    });

    [TestMethod]
    public Task EditedRoot_HasTheKeyboardInItsComment() => Scenario.Run(async editor =>
    {
        await Open(editor);

        Press(editor, Key.F2);

        Assert.IsTrue(Root(editor).IsEditing);
        Assert.AreEqual("comment", FocusedField(editor, Root(editor)));

        // Tab stays in its only box
        Press(editor, Key.Tab);
        Assert.AreEqual("comment", FocusedField(editor, Root(editor)));
        Press(editor, Key.Tab, RawInputModifiers.Shift);
        Assert.AreEqual("comment", FocusedField(editor, Root(editor)));
        Assert.IsTrue(Root(editor).IsEditing);
    });

    [TestMethod]
    public Task Tab_GoesRoundTheBoxesOfAScalar() => Scenario.Run(async editor =>
    {
        await Open(editor);
        SerializerNodeViewModel count = Member(editor, Count);
        Select(editor, count);
        Press(editor, Key.F2);

        string[] order = ["name", "type", "value", "comment", "name", "type"];

        foreach (string field in order)
        {
            Assert.AreEqual(field, FocusedField(editor, count));
            Assert.IsTrue(count.IsEditing, $"the row is edited in its {field}");
            Press(editor, Key.Tab);
        }

        // Shift+Tab goes back, round the boxes too
        Assert.AreEqual("value", FocusedField(editor, count));
        foreach (string field in new[] { "type", "name", "comment", "value" })
        {
            Press(editor, Key.Tab, RawInputModifiers.Shift);
            Assert.AreEqual(field, FocusedField(editor, count));
            Assert.IsTrue(count.IsEditing);
        }
    });

    [TestMethod]
    public Task Tab_SelectsTheTextOfATextBox() => Scenario.Run(async editor =>
    {
        await Open(editor);
        SerializerNodeViewModel title = Member(editor, Title);
        Select(editor, title);
        Press(editor, Key.F2);

        Press(editor, Key.Tab);
        Press(editor, Key.Tab);

        Assert.AreEqual("value", FocusedField(editor, title));
        Assert.AreEqual("demo", ((TextBox)Focused(editor)!).SelectedText);
    });

    [TestMethod]
    public Task Tab_GoesRoundTheBoxesOfABoolean_AndSpaceTogglesIt() => Scenario.Run(async editor =>
    {
        await Open(editor);
        SerializerNodeViewModel enabled = Member(editor, Enabled);
        Select(editor, enabled);
        Press(editor, Key.F2);

        Assert.AreEqual("name", FocusedField(editor, enabled));
        Press(editor, Key.Tab);
        Assert.AreEqual("type", FocusedField(editor, enabled));
        Press(editor, Key.Tab);
        Assert.AreEqual("check", FocusedField(editor, enabled));
        Press(editor, Key.Tab);
        Assert.AreEqual("comment", FocusedField(editor, enabled));
        Press(editor, Key.Tab);
        Assert.AreEqual("name", FocusedField(editor, enabled));

        Press(editor, Key.Tab, RawInputModifiers.Shift);
        Press(editor, Key.Tab, RawInputModifiers.Shift);
        Assert.AreEqual("check", FocusedField(editor, enabled));

        Assert.IsTrue(enabled.BoolValue);
        Press(editor, Key.Space);
        Assert.IsFalse(enabled.BoolValue);

        Press(editor, Key.Enter);
        Assert.IsFalse(enabled.IsEditing);
        Assert.AreEqual(0, editor.ViewModel.Root.Element[Enabled].Children.Single(child => child.MetaData.Span[0] == 0x03).Data.Span[0]);
        AssertRowFocused(editor, enabled);
    });

    [TestMethod]
    public Task Tab_GoesRoundTheBoxesOfAnObject() => Scenario.Run(async editor =>
    {
        await Open(editor);
        SerializerNodeViewModel audio = Member(editor, Audio);
        Select(editor, audio);
        Press(editor, Key.F2);

        foreach (string field in new[] { "name", "type", "comment", "name" })
        {
            Assert.AreEqual(field, FocusedField(editor, audio));
            Press(editor, Key.Tab);
        }
    });

    [TestMethod]
    public Task Tab_GoesRoundTheBoxesOfAnElement() => Scenario.Run(async editor =>
    {
        await Open(editor);
        Member(editor, Tags).IsExpanded = true;
        Editor.Pump();
        SerializerNodeViewModel element = Member(editor, Tags).Children[0];
        Select(editor, element);
        Press(editor, Key.F2);

        foreach (string field in new[] { "value", "comment", "type", "value" })
        {
            Assert.AreEqual(field, FocusedField(editor, element));
            Press(editor, Key.Tab);
        }
    });

    [TestMethod]
    public Task Tab_OutsideOfAnEditedRow_LeavesTheTree() => Scenario.Run(async editor =>
    {
        await Open(editor);
        Select(editor, Member(editor, Count));

        Press(editor, Key.Tab);

        Assert.IsFalse(Member(editor, Count).IsEditing);
        Assert.IsFalse(editor.SerializerTree.IsKeyboardFocusWithin);
    });

    [TestMethod]
    public Task Enter_AppliesEveryBoxOfTheRow_FromAnyOfThem() => Scenario.Run(async editor =>
    {
        await Open(editor);
        SerializerNodeViewModel count = Member(editor, Count);
        Select(editor, count);
        Press(editor, Key.F2);

        RowBox(editor, count, "name").Text = "Total";
        RowBox(editor, count, "value").Text = "12";
        RowBox(editor, count, "comment").Text = "the total";
        Press(editor, Key.Tab);
        Press(editor, Key.Tab);
        Press(editor, Key.Tab);
        Assert.AreEqual("comment", FocusedField(editor, count));

        Press(editor, Key.Enter);

        Element holder = editor.ViewModel.Root.Element[Count];
        Assert.AreEqual("Total", holder.Data.AsUtf8String());
        Assert.AreEqual(12, holder.Children.Single(child => child.MetaData.Span[0] == 0x03).Data.AsInt());
        Assert.AreEqual("the total", holder.Children.Single(child => child.MetaData.Span[0] == 0x04).Data.AsUtf8String());
        Assert.IsFalse(count.IsEditing);
        AssertRowFocused(editor, count);

        // The keyboard goes on in the tree
        Press(editor, Key.Down);
        AssertRowFocused(editor, Member(editor, Enabled));
    });

    [TestMethod]
    public Task Escape_RevertsEveryBoxOfTheRow() => Scenario.Run(async editor =>
    {
        await Open(editor);
        SerializerNodeViewModel count = Member(editor, Count);
        Select(editor, count);
        Press(editor, Key.F2);

        RowBox(editor, count, "name").Text = "Total";
        RowBox(editor, count, "value").Text = "12";
        RowBox(editor, count, "comment").Text = "the total";
        Press(editor, Key.Tab);

        Press(editor, Key.Escape);

        Element holder = editor.ViewModel.Root.Element[Count];
        Assert.AreEqual("Count", holder.Data.AsUtf8String());
        Assert.AreEqual(3, holder.Children.Single(child => child.MetaData.Span[0] == 0x03).Data.AsInt());
        Assert.IsFalse(holder.Children.Any(child => child.MetaData.Span[0] == 0x04));
        Assert.AreEqual("Count", count.NameText);
        Assert.AreEqual("3", count.ValueText);
        Assert.AreEqual(string.Empty, count.CommentText);
        Assert.IsFalse(editor.ViewModel.IsModified);
        AssertRowFocused(editor, count);

        Press(editor, Key.Up);
        AssertRowFocused(editor, Member(editor, Title));
    });

    [TestMethod]
    public Task AnInvalidValue_KeepsTheRowEdited_AndTheKeyboardInIt() => Scenario.Run(async editor =>
    {
        await Open(editor);
        SerializerNodeViewModel count = Member(editor, Count);
        Select(editor, count);
        Press(editor, Key.F2);
        Press(editor, Key.Tab);
        Press(editor, Key.Tab);
        TextBox value = RowBox(editor, count, "value");
        value.Text = "many";

        Press(editor, Key.Enter);

        Assert.IsTrue(count.IsEditing);
        Assert.IsTrue(value.Classes.Contains("invalid"));
        Assert.AreEqual("value", FocusedField(editor, count));
        Assert.AreEqual(3, editor.ViewModel.Root.Element[Count].Children.Single(child => child.MetaData.Span[0] == 0x03).Data.AsInt());

        // Tab still goes round the row
        Press(editor, Key.Tab);
        Assert.AreEqual("comment", FocusedField(editor, count));
        Assert.IsTrue(count.IsEditing);

        Press(editor, Key.Escape);
        Assert.IsFalse(count.IsEditing);
        AssertRowFocused(editor, count);
    });

    [TestMethod]
    public Task ShiftEnter_BreaksALineOfTheComment_AndNotOfTheName() => Scenario.Run(async editor =>
    {
        await Open(editor);
        SerializerNodeViewModel title = Member(editor, Title);
        Select(editor, title);
        Press(editor, Key.F2);

        // In the name Shift+Enter is an Enter: it applies the row
        TextBox name = RowBox(editor, title, "name");
        name.Text = "Name";
        Press(editor, Key.Enter, RawInputModifiers.Shift);
        Assert.IsFalse(title.IsEditing);
        Assert.AreEqual("Name", editor.ViewModel.Root.Element[Title].Data.AsUtf8String());

        Press(editor, Key.F2);
        TextBox comment = RowBox(editor, title, "comment");
        comment.Focus();
        comment.Text = "onetwo";
        comment.SelectionStart = 3;
        comment.SelectionEnd = 3;
        Press(editor, Key.Enter, RawInputModifiers.Shift);
        Assert.AreEqual("one\ntwo", comment.Text);
        Assert.AreEqual(4, comment.CaretIndex);

        // A selection is replaced by the line break
        comment.SelectionStart = 0;
        comment.SelectionEnd = 3;
        Press(editor, Key.Enter, RawInputModifiers.Shift);
        Assert.AreEqual("\n\ntwo", comment.Text);

        Press(editor, Key.Enter);
        Assert.AreEqual("\n\ntwo", editor.ViewModel.Root.Element[Title].Children.Single(child => child.MetaData.Span[0] == 0x04).Data.AsUtf8String());
    });

    // ---------------------------------------------------------------- adding, deleting, moving

    [TestMethod]
    public Task Insert_AddsAMember_AndEditsIt_EscapeLeavesItThere() => Scenario.Run(async editor =>
    {
        await Open(editor);
        Select(editor, Member(editor, Audio));

        Press(editor, Key.Insert);

        SerializerNodeViewModel added = editor.ViewModel.SelectedSerializerNode!;
        Assert.AreSame(Member(editor, Audio), added.Parent);
        Assert.AreEqual(3, Member(editor, Audio).Children.Count);
        Assert.IsTrue(added.IsEditing);
        Assert.AreEqual("name", FocusedField(editor, added));

        Press(editor, Key.Escape);
        Assert.IsFalse(added.IsEditing);
        AssertRowFocused(editor, added);
        Assert.AreEqual(3, Member(editor, Audio).Children.Count);
    });

    [TestMethod]
    public Task ShiftInsert_AddsAfterTheValue() => Scenario.Run(async editor =>
    {
        await Open(editor);
        Select(editor, Member(editor, Count));

        Press(editor, Key.Insert, RawInputModifiers.Shift);

        SerializerNodeViewModel added = editor.ViewModel.SelectedSerializerNode!;
        Assert.AreSame(Root(editor), added.Parent);
        Assert.AreEqual(Count + 1, Root(editor).Children.IndexOf(added));
        Assert.IsTrue(added.IsEditing);
    });

    [TestMethod]
    public Task Delete_RemovesTheValue_AndTheKeyboardGoesToTheNextRow() => Scenario.Run(async editor =>
    {
        await Open(editor);
        Select(editor, Member(editor, Count));

        Press(editor, Key.Delete);

        Assert.AreEqual("Enabled", editor.ViewModel.SelectedSerializerNode!.NameText);
        AssertRowFocused(editor, editor.ViewModel.SelectedSerializerNode!);
        Assert.AreEqual(4, Root(editor).Children.Count);

        // The last one: the keyboard goes to the one before
        Press(editor, Key.End);
        Press(editor, Key.Delete);
        Assert.AreEqual("Tags", editor.ViewModel.SelectedSerializerNode!.NameText);
        AssertRowFocused(editor, editor.ViewModel.SelectedSerializerNode!);

        Press(editor, Key.Up);
        Assert.AreEqual("Enabled", editor.ViewModel.SelectedSerializerNode!.NameText);
    });

    [TestMethod]
    public Task Delete_InAnEditedBox_DeletesText_NotTheValue() => Scenario.Run(async editor =>
    {
        await Open(editor);
        SerializerNodeViewModel title = Member(editor, Title);
        Select(editor, title);
        Press(editor, Key.F2);

        Press(editor, Key.Delete);

        Assert.AreEqual(5, Root(editor).Children.Count);
        Assert.IsTrue(title.IsEditing);
    });

    [TestMethod]
    [DataRow(Key.Insert, RawInputModifiers.None)]
    [DataRow(Key.Insert, RawInputModifiers.Shift)]
    [DataRow(Key.Up, RawInputModifiers.Shift)]
    [DataRow(Key.Down, RawInputModifiers.Shift)]
    [DataRow(Key.Delete, RawInputModifiers.None)]
    [DataRow(Key.Home, RawInputModifiers.None)]
    [DataRow(Key.End, RawInputModifiers.None)]
    [DataRow(Key.PageDown, RawInputModifiers.None)]
    public Task KeysOfTheRows_InTheBoxesOfAnEditedRow_EditTheText(Key key, RawInputModifiers modifiers) => Scenario.Run(async editor =>
    {
        await Open(editor);
        SerializerNodeViewModel count = Member(editor, Count);
        Select(editor, count);
        Press(editor, Key.F2);

        foreach (string field in new[] { "name", "value", "comment" })
        {
            TextBox box = RowBox(editor, count, field);
            box.Focus();
            Editor.Pump();

            Press(editor, key, modifiers);

            Assert.AreEqual(5, Root(editor).Children.Count, $"{modifiers}+{key} in the {field}");
            Assert.AreSame(count, Root(editor).Children[Count], $"{modifiers}+{key} in the {field}");
            Assert.AreSame(count, editor.ViewModel.SelectedSerializerNode, $"{modifiers}+{key} in the {field}");
            Assert.IsTrue(count.IsEditing, $"{modifiers}+{key} in the {field}");
            Assert.AreEqual(field, FocusedField(editor, count));
        }
    });

    [TestMethod]
    [DataRow(Key.Up)]
    [DataRow(Key.Down)]
    [DataRow(Key.Left)]
    [DataRow(Key.Right)]
    [DataRow(Key.PageUp)]
    [DataRow(Key.PageDown)]
    [DataRow(Key.Space)]
    [DataRow(Key.Add)]
    [DataRow(Key.Subtract)]
    [DataRow(Key.Multiply)]
    public Task KeysOfTheTree_InTheBoxesOfAnEditedRow_DoNotReachTheTree(Key key) => Scenario.Run(async editor =>
    {
        await Open(editor);
        SerializerNodeViewModel audio = Member(editor, Audio);
        Select(editor, audio);
        Press(editor, Key.F2);

        foreach (string field in new[] { "name", "comment" })
        {
            TextBox box = RowBox(editor, audio, field);
            box.Focus();
            Editor.Pump();

            Press(editor, key);

            Assert.AreSame(audio, editor.ViewModel.SelectedSerializerNode, $"{key} in the {field}");
            Assert.IsFalse(audio.IsExpanded, $"{key} in the {field}");
            Assert.IsTrue(audio.IsEditing, $"{key} in the {field}");
            Assert.AreEqual(field, FocusedField(editor, audio));
        }
    });

    [TestMethod]
    public Task TypingInTheBoxes_OfAnEditedRow_EditsTheirTexts() => Scenario.Run(async editor =>
    {
        await Open(editor);
        SerializerNodeViewModel title = Member(editor, Title);
        Select(editor, title);
        Press(editor, Key.F2);
        TextBox name = RowBox(editor, title, "name");
        name.Text = string.Empty;

        editor.Window.KeyTextInput("My Title");
        Editor.Pump();

        Assert.AreEqual("My Title", name.Text);
        Assert.IsTrue(title.IsEditing);
        Press(editor, Key.Enter);
        Assert.AreEqual("My Title", editor.ViewModel.Root.Element[Title].Data.AsUtf8String());
    });

    [TestMethod]
    public Task CtrlS_SavesWhileARowIsEdited() => Scenario.Run(async editor =>
    {
        await Open(editor);
        string path = editor.ViewModel.FileName!;
        SerializerNodeViewModel title = Member(editor, Title);
        Select(editor, title);
        Press(editor, Key.F2);
        RowBox(editor, title, "value").Focus();
        Editor.Pump();
        File.Delete(path);

        Press(editor, Key.S, RawInputModifiers.Control);

        Assert.IsTrue(File.Exists(path), "Ctrl+S reached the window");
    });

    [TestMethod]
    public Task KeysOfTheRows_OnTheFirstAndTheLastRow_DoNothing() => Scenario.Run(async editor =>
    {
        await Open(editor);
        Select(editor, Member(editor, Title));

        Press(editor, Key.Up, RawInputModifiers.Shift);
        Assert.AreEqual("Title", Root(editor).Children[0].NameText);
        AssertRowFocused(editor, Member(editor, Title));

        Press(editor, Key.End);
        Press(editor, Key.Down, RawInputModifiers.Shift);
        Assert.AreEqual("Audio", Root(editor).Children[^1].NameText);
        AssertRowFocused(editor, Member(editor, Audio));

        // The root can not be deleted nor moved
        Press(editor, Key.Home);
        Press(editor, Key.Delete);
        Press(editor, Key.Up, RawInputModifiers.Shift);
        Assert.AreEqual(5, Root(editor).Children.Count);
        AssertRowFocused(editor, Root(editor));
    });

    [TestMethod]
    public Task Insert_OnTheRoot_AddsAMemberToIt() => Scenario.Run(async editor =>
    {
        await Open(editor);

        Press(editor, Key.Insert);

        Assert.AreEqual(6, Root(editor).Children.Count);
        Assert.AreSame(Root(editor), editor.ViewModel.SelectedSerializerNode!.Parent);
        Assert.IsTrue(editor.ViewModel.SelectedSerializerNode!.IsEditing);
    });

    [TestMethod]
    public Task Insert_OnAnArray_AddsAnElement_WithTheKeyboardInItsValue() => Scenario.Run(async editor =>
    {
        await Open(editor);
        Select(editor, Member(editor, Tags));

        Press(editor, Key.Insert);

        SerializerNodeViewModel added = editor.ViewModel.SelectedSerializerNode!;
        Assert.AreSame(Member(editor, Tags), added.Parent);
        Assert.AreEqual("[2]", added.Label);
        Assert.IsTrue(added.IsEditing);
        Assert.AreEqual("value", FocusedField(editor, added));
    });

    [TestMethod]
    public Task ShiftUpAndDown_MoveTheValue_AndTheKeyboardStaysOnIt() => Scenario.Run(async editor =>
    {
        await Open(editor);
        Select(editor, Member(editor, Count));

        Press(editor, Key.Up, RawInputModifiers.Shift);
        Assert.AreEqual("Count", Root(editor).Children[0].NameText);
        AssertRowFocused(editor, Root(editor).Children[0]);
        Assert.AreEqual("Count", editor.ViewModel.Root.Element[0].Data.AsUtf8String());

        Press(editor, Key.Down, RawInputModifiers.Shift);
        Press(editor, Key.Down, RawInputModifiers.Shift);
        Assert.AreEqual("Count", Root(editor).Children[2].NameText);
        AssertRowFocused(editor, Root(editor).Children[2]);

        Press(editor, Key.Down);
        AssertRowFocused(editor, Root(editor).Children[3]);
    });

    // ---------------------------------------------------------------- the tabs

    [TestMethod]
    public Task SwitchingTheTabs_BringsTheKeyboardToTheSelectedRow() => Scenario.Run(async editor =>
    {
        await Open(editor);
        Select(editor, Member(editor, Count));

        editor.Window.FindControl<TabControl>("Views")!.SelectedIndex = MainWindowViewModel.RawView;
        Editor.Pump();
        AssertRawFocused(editor, Member(editor, Count).Holder);

        Press(editor, Key.Down);
        Assert.AreSame(Member(editor, Enabled).Holder, editor.ViewModel.SelectedNode);
        AssertRawFocused(editor, Member(editor, Enabled).Holder);

        editor.Window.FindControl<TabControl>("Views")!.SelectedIndex = MainWindowViewModel.SerializerView;
        Editor.Pump();
        AssertRowFocused(editor, Member(editor, Enabled));

        Press(editor, Key.Down);
        AssertRowFocused(editor, Member(editor, Tags));
    });

    [TestMethod]
    public Task F7_SwitchesTheSerializerView_AndTheKeyboardFollows() => Scenario.Run(async editor =>
    {
        await Open(editor);
        Select(editor, Member(editor, Count));

        Press(editor, Key.F7);
        Assert.IsFalse(editor.ViewModel.IsSerializerPanelShown);
        AssertRawFocused(editor, Member(editor, Count).Holder);

        Press(editor, Key.F7);
        Assert.IsTrue(editor.ViewModel.IsSerializerPanelShown);
        AssertRowFocused(editor, editor.ViewModel.SelectedSerializerNode!);
        Assert.AreSame(Member(editor, Count).Holder, editor.ViewModel.SelectedSerializerNode!.Holder);
    });

    [TestMethod]
    public Task TheKeyboard_IsNotTakenFromAnEditor_OfTheRawView() => Scenario.Run(async editor =>
    {
        await Open(editor);
        editor.ShowRawView();
        TextBox data = editor.Find<ContentEditorView>().Last().GetVisualDescendants().OfType<TextBox>().First(box => box.IsEffectivelyVisible);
        data.Focus();
        Editor.Pump();

        editor.ViewModel.SelectedNode = editor.ViewModel.Root.Children[1];
        Editor.Pump();

        Assert.IsTrue(data.IsFocused || editor.Tree.IsKeyboardFocusWithin);
    });

    // ---------------------------------------------------------------- the comment as a tooltip

    [TestMethod]
    public Task Comment_IsTheTooltipOfItsRow_NotAText() => Scenario.Run(async editor =>
    {
        await Open(editor);
        Grid audio = RowOf(editor, Member(editor, Audio));

        Assert.AreEqual("The audio device", ToolTip.GetTip(audio));
        Assert.IsNull(ToolTip.GetTip(RowOf(editor, Member(editor, Count))));
        Assert.IsFalse(editor.SerializerTree.GetVisualDescendants().OfType<TextBlock>().Any(block => block.IsEffectivelyVisible && block.Text == "The audio device"));

        // Edited, the comment is in its box, not in the tooltip
        Select(editor, Member(editor, Audio));
        Press(editor, Key.F2);
        Assert.IsNull(ToolTip.GetTip(audio));
        Assert.AreEqual("The audio device", RowBox(editor, Member(editor, Audio), "comment").Text);

        Press(editor, Key.Escape);
        Assert.AreEqual("The audio device", ToolTip.GetTip(audio));
    });

    [TestMethod]
    public Task CommentBox_IsUnderTheRow_ThreeLinesHigh() => Scenario.Run(async editor =>
    {
        await Open(editor);
        SerializerNodeViewModel enabled = Member(editor, Enabled);
        Select(editor, enabled);
        Press(editor, Key.F2);

        TextBox comment = RowBox(editor, enabled, "comment");
        TextBox name = RowBox(editor, enabled, "name");
        Point commentTop = comment.TranslatePoint(default, editor.SerializerTree)!.Value;
        Point nameTop = name.TranslatePoint(default, editor.SerializerTree)!.Value;

        Assert.IsTrue(commentTop.Y >= nameTop.Y + name.Bounds.Height, "the comment is under the name");
        Assert.AreEqual(nameTop.X, commentTop.X, 1);
        Assert.AreEqual(3, comment.MinLines);
        Assert.AreEqual(3, comment.MaxLines);
        Assert.AreEqual(TextWrapping.Wrap, comment.TextWrapping);
        Assert.AreEqual(FontStyle.Normal, comment.FontStyle);
        Assert.AreEqual(name.FontSize, comment.FontSize);

        // Three lines high: higher than two single-line boxes and lower than five
        Assert.IsTrue(comment.Bounds.Height > name.Bounds.Height * 1.5, $"{comment.Bounds.Height} vs {name.Bounds.Height}");
        Assert.IsTrue(comment.Bounds.Height < name.Bounds.Height * 4, $"{comment.Bounds.Height} vs {name.Bounds.Height}");

        // As wide as the boxes of a text value too
        Press(editor, Key.Escape);
        Select(editor, Member(editor, Title));
        Press(editor, Key.F2);
        double width = RowBox(editor, Member(editor, Title), "comment").Bounds.Width;
        Assert.AreEqual(comment.Bounds.Width, width, 1, $"the comment of a boolean is {comment.Bounds.Width} wide, of a text {width}");

        // A long comment wraps, and stays three lines high
        TextBox titleComment = RowBox(editor, Member(editor, Title), "comment");
        double height = titleComment.Bounds.Height;
        titleComment.Text = string.Join(' ', Enumerable.Repeat("word", 200));
        Editor.Pump();
        Assert.AreEqual(height, titleComment.Bounds.Height, 1);
        Assert.AreEqual(width, titleComment.Bounds.Width, 1);
    });

    // ---------------------------------------------------------------- the texts beside the view

    [TestMethod]
    public Task NoSerializableOrReadOnlyLabel_IsShown() => Scenario.Run(async editor =>
    {
        await Open(editor);

        string[] texts = editor.Find<TextBlock>().Where(block => block.IsEffectivelyVisible && block is not HighlightedText).Select(block => block.Text ?? string.Empty).ToArray();

        Assert.IsFalse(texts.Any(text => text.Contains("serializable", StringComparison.OrdinalIgnoreCase)), string.Join(" | ", texts));
        Assert.IsFalse(texts.Any(text => text.Contains("read-only", StringComparison.OrdinalIgnoreCase)), string.Join(" | ", texts));
        Assert.AreEqual(string.Empty, editor.ViewModel.SerializerSummary);
    });

    [TestMethod]
    public Task Yaml_IsColored_ByTheKindsOfItsPieces() => Scenario.Run(async editor =>
    {
        await Open(editor);
        HighlightedText text = editor.Window.FindControl<HighlightedText>("SerializerText")!;
        Run[] runs = text.Inlines!.OfType<Run>().ToArray();

        Assert.AreEqual(editor.ViewModel.SerializerText, string.Concat(runs.Select(run => run.Text)));
        Assert.IsTrue(runs.Any(run => run.Classes.Contains("key") && run.Text == "Title"));
        Assert.IsTrue(runs.Any(run => run.Classes.Contains("string") && run.Text == "demo"));
        Assert.IsTrue(runs.Any(run => run.Classes.Contains("number") && run.Text == "3"));
        Assert.IsTrue(runs.Any(run => run.Classes.Contains("keyword") && run.Text == "true"));
        Assert.IsTrue(runs.Any(run => run.Classes.Contains("tag") && run.Text == "!timestamp"));
        Assert.IsTrue(runs.Any(run => run.Classes.Contains("comment") && run.Text.Contains("The audio device")));
        Assert.IsTrue(runs.Any(run => run.Classes.Contains("punctuation") && run.Text == "-"));

        // The kinds have colors of their own
        IBrush? Color(string kind) => runs.First(run => run.Classes.Contains(kind)).Foreground;
        string[] kinds = ["key", "string", "number", "keyword", "tag", "comment", "punctuation"];
        string[] colors = kinds.Select(kind => (Color(kind) as ISolidColorBrush)?.Color.ToString() ?? "none").ToArray();
        Assert.IsFalse(colors.Contains("none"), string.Join(", ", colors));
        Assert.AreEqual(kinds.Length, colors.Distinct().Count(), string.Join(", ", colors));
        Assert.AreEqual(FontStyle.Italic, runs.First(run => run.Classes.Contains("comment")).FontStyle);

        editor.Screenshot("yaml-colored");
    });

    [TestMethod]
    public Task Json_IsColored_AndFollowsTheChoice() => Scenario.Run(async editor =>
    {
        await Open(editor);
        HighlightedText text = editor.Window.FindControl<HighlightedText>("SerializerText")!;

        editor.ViewModel.IsJsonShown = true;
        Editor.Pump();
        Run[] runs = text.Inlines!.OfType<Run>().ToArray();

        Assert.IsFalse(text.IsYaml);
        Assert.AreEqual(editor.ViewModel.SerializerText, string.Concat(runs.Select(run => run.Text)));
        Assert.IsTrue(runs.Any(run => run.Classes.Contains("key") && run.Text == "\"Title\""));
        Assert.IsTrue(runs.Any(run => run.Classes.Contains("string") && run.Text == "\"demo\""));
        Assert.IsTrue(runs.Any(run => run.Classes.Contains("punctuation") && run.Text == "{"));
        Assert.IsFalse(runs.Any(run => run.Classes.Contains("comment")));

        // An edit is shown at once
        editor.ViewModel.IsYamlShown = true;
        Editor.Pump();
        SerializerNodeViewModel title = Member(editor, Title);
        Select(editor, title);
        Press(editor, Key.F2);
        RowBox(editor, title, "value").Text = "changed";
        Press(editor, Key.Enter);
        Assert.IsTrue(text.Inlines!.OfType<Run>().Any(run => run.Classes.Contains("string") && run.Text == "changed"));
    });

    [TestMethod]
    public Task ColoredText_CanBeSelected() => Scenario.Run(async editor =>
    {
        await Open(editor);
        HighlightedText text = editor.Window.FindControl<HighlightedText>("SerializerText")!;

        text.SelectAll();

        Assert.AreEqual(editor.ViewModel.SerializerText, text.SelectedText);
    });

    // ---------------------------------------------------------------- helpers

    private static async Task Open(Editor editor)
    {
        string path = Path.Combine(editor.Directory, "settings.prmt");
        await File.WriteAllBytesAsync(path, PromptuariumSerializer.Serialize(new SerializerScenarios.Settings()));
        Assert.IsTrue(await editor.ViewModel.OpenFileAsync(path));
        Editor.Pump();
        Editor.Pump();
    }

    private static async Task OpenMany(Editor editor, int count)
    {
        string path = Path.Combine(editor.Directory, "many.prmt");
        var values = new Dictionary<string, object>();

        for (int i = 0; i < count; i++)
        {
            values[$"Value{i:D3}"] = i;
        }

        await PromptuariumSerializer.ToElement(values).SaveAsync(path);
        Assert.IsTrue(await editor.ViewModel.OpenFileAsync(path));
        Editor.Pump();
        Editor.Pump();
    }

    private static SerializerNodeViewModel Root(Editor editor) => editor.ViewModel.SerializerRoots[0];

    private static SerializerNodeViewModel Member(Editor editor, int index) => Root(editor).Children[index];

    private static void Press(Editor editor, Key key, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        // Down and up, as a key is pressed (a check box is toggled when Space is released)
        editor.Window.KeyPress(key, modifiers, PhysicalKey.None, null);
        editor.Window.KeyRelease(key, modifiers, PhysicalKey.None, null);
        Editor.Pump();
        Editor.Pump();
    }

    private static void Select(Editor editor, SerializerNodeViewModel node)
    {
        ((TreeViewItem)editor.SerializerTree.TreeContainerFromItem(node)!).Focus();
        Editor.Pump();
        Editor.Pump();
        AssertRowFocused(editor, node);
    }

    private static object? Focused(Editor editor) => editor.Window.FocusManager?.GetFocusedElement();

    private static string Describe(object? focused) => focused is Control control
        ? $"{control.GetType().Name} of {(control.DataContext as SerializerNodeViewModel)?.Holder.Path ?? (control.DataContext as NodeViewModel)?.Path ?? "?"} ({control.Tag})"
        : focused?.ToString() ?? "nothing";

    private static void AssertRowFocused(Editor editor, SerializerNodeViewModel node)
    {
        object? focused = Focused(editor);
        Assert.IsTrue(focused is TreeViewItem { DataContext: SerializerNodeViewModel row } && row == node, $"{node.Holder.Path} should have the keyboard, not {Describe(focused)}");
        Assert.AreSame(node, editor.ViewModel.SelectedSerializerNode);
        Assert.IsTrue(editor.SerializerTree.IsKeyboardFocusWithin);
    }

    private static void AssertRawFocused(Editor editor, NodeViewModel node)
    {
        object? focused = Focused(editor);
        Assert.IsTrue(focused is TreeViewItem { DataContext: NodeViewModel row } && row == node, $"{node.Path} should have the keyboard, not {Describe(focused)}");
        Assert.AreSame(node, editor.ViewModel.SelectedNode);
        Assert.IsTrue(editor.Tree.IsKeyboardFocusWithin);
    }

    // The box of an edited row that has the keyboard: name, type, value, check, comment
    private static string FocusedField(Editor editor, SerializerNodeViewModel node)
    {
        object? focused = Focused(editor);
        Control? control = focused as Control;
        ComboBox? combo = control as ComboBox ?? control?.FindAncestorOfType<ComboBox>();

        if (combo is not null && combo.DataContext == node)
        {
            return "type";
        }

        return control switch
        {
            TextBox { Tag: string tag } box when box.DataContext == node => tag,
            CheckBox box when box.DataContext == node => "check",
            _ => $"not a box of {node.Holder.Path}: {Describe(focused)}",
        };
    }

    private static TextBox RowBox(Editor editor, SerializerNodeViewModel node, string field)
    {
        return editor.SerializerTree.GetVisualDescendants().OfType<TextBox>()
            .Single(box => Equals(box.Tag, field) && box.DataContext == node && box.IsEffectivelyVisible);
    }

    private static Grid RowOf(Editor editor, SerializerNodeViewModel node)
    {
        return editor.SerializerTree.GetVisualDescendants().OfType<PStudio.Views.SerializerRow>().First(row => row.DataContext == node);
    }

    // The row is within the visible part of the tree
    private static void AssertInSight(Editor editor, SerializerNodeViewModel node)
    {
        ScrollViewer scroll = editor.SerializerTree.GetVisualDescendants().OfType<ScrollViewer>().First();
        Grid row = RowOf(editor, node);
        Point top = row.TranslatePoint(default, scroll)!.Value;

        Assert.IsTrue(top.Y >= -1 && top.Y + row.Bounds.Height <= scroll.Viewport.Height + 1, $"{node.Holder.Path} is at {top.Y}, the view is {scroll.Viewport.Height} high");
    }
}
