using NSubstitute;
using PStudio.Interfaces;
using PStudio.Models;
using PStudio.Presentation;
using PStudio.Services;
using PStudio.ViewModels;
using Promptuarium;

namespace PStudio.Tests.ViewModels;

[TestClass]
public sealed class ContentEditorViewModelTests
{
    private readonly IFileDialogService dialogs = Substitute.For<IFileDialogService>();
    private readonly ITreeFileService files = Substitute.For<ITreeFileService>();
    private readonly List<byte[]> stored = [];

    [TestMethod]
    public void Load_ChoosesTheForm_AndIsNotPending()
    {
        ContentEditorViewModel sut = Create();

        sut.Load("text"u8.ToArray());
        Assert.AreEqual(ContentForm.Utf8, sut.Form);
        Assert.AreEqual("text", sut.Text);
        Assert.IsFalse(sut.IsPending);

        sut.Load(new byte[] { 0, 255 });
        Assert.AreEqual(ContentForm.Hex, sut.Form);
        Assert.AreEqual("00 FF", sut.Text);
        Assert.IsFalse(sut.ApplyCommand.CanExecute(null));
    }

    [TestMethod]
    public void Edit_IsStoredByApply()
    {
        ContentEditorViewModel sut = Create();
        sut.Load("old"u8.ToArray());

        sut.Text = "new";

        Assert.IsTrue(sut.IsPending);
        Assert.IsTrue(sut.ApplyCommand.CanExecute(null));
        Assert.AreEqual(0, stored.Count);

        sut.ApplyCommand.Execute(null);

        Assert.IsFalse(sut.IsPending);
        CollectionAssert.AreEqual("new"u8.ToArray(), stored.Single());
        CollectionAssert.AreEqual("new"u8.ToArray(), sut.Content.ToArray());
    }

    [TestMethod]
    public void InvalidEdit_IsKept_WithAnError()
    {
        ContentEditorViewModel sut = Create();
        sut.Load(new byte[] { 1 });

        sut.Text = "0";

        Assert.IsFalse(sut.ApplyPending());
        Assert.IsTrue(sut.IsPending);
        Assert.IsTrue(sut.IsError);
        Assert.AreEqual("0", sut.Text);
        Assert.AreEqual(0, stored.Count);
    }

    [TestMethod]
    public void Revert_DropsTheEdit()
    {
        ContentEditorViewModel sut = Create();
        sut.Load("old"u8.ToArray());
        sut.Text = "new";

        sut.RevertCommand.Execute(null);

        Assert.AreEqual("old", sut.Text);
        Assert.IsFalse(sut.IsPending);
        Assert.AreEqual(0, stored.Count);
    }

    [TestMethod]
    public void ChangingTheForm_ConvertsTheEditWithoutStoringIt()
    {
        ContentEditorViewModel sut = Create();
        sut.Load("AB"u8.ToArray());
        sut.Text = "AC";

        Choose(sut, ContentForm.Hex);

        Assert.AreEqual("41 43", sut.Text);
        Assert.IsTrue(sut.IsPending);
        Assert.AreEqual(0, stored.Count);

        sut.ApplyPending();
        CollectionAssert.AreEqual("AC"u8.ToArray(), stored.Single());
    }

    [TestMethod]
    public void ChangingTheForm_KeepsAnEditMeantInTheNewForm()
    {
        // Text typed into an empty hex content, then UTF-8 is chosen
        ContentEditorViewModel sut = Create();
        sut.Load(new byte[] { 1 });
        sut.Text = "hello";

        Choose(sut, ContentForm.Utf8);

        Assert.AreEqual("hello", sut.Text);
        Assert.IsFalse(sut.IsError);
        Assert.IsTrue(sut.ApplyPending());
        CollectionAssert.AreEqual("hello"u8.ToArray(), stored.Single());
    }

    [TestMethod]
    public void ChangingTheForm_KeepsANumberTypedAsText()
    {
        ContentEditorViewModel sut = Create();
        sut.Load(Array.Empty<byte>());
        sut.Text = "12";

        Choose(sut, ContentForm.Int32);

        Assert.AreEqual("12", sut.Text);
        sut.ApplyPending();
        CollectionAssert.AreEqual(new byte[] { 12, 0, 0, 0 }, stored.Single());
    }

    [TestMethod]
    public void EmptyContent_IsTypedAsUtf8()
    {
        ContentEditorViewModel sut = Create();

        sut.Load(Array.Empty<byte>());

        Assert.AreEqual(ContentForm.Utf8, sut.Form);
        Assert.IsFalse(sut.IsReadOnly);
    }

    [TestMethod]
    public void EmptyContent_OffersEveryForm()
    {
        ContentEditorViewModel sut = Create();

        sut.Load(Array.Empty<byte>());

        CollectionAssert.AreEqual(Enum.GetValues<ContentForm>(), sut.Forms.Select(option => option.Form).ToArray());
        Assert.AreEqual("DateTimeOffset", sut.Forms.Single(option => option.Form == ContentForm.DateTimeOffset).Name);
    }

    [TestMethod]
    public void FormsThatCanNotShowTheContent_AreNotOffered()
    {
        ContentEditorViewModel sut = Create();

        sut.Load(new byte[] { 0xFF });
        ContentForm[] offered = sut.Forms.Select(option => option.Form).ToArray();

        CollectionAssert.Contains(offered, ContentForm.Hex);
        CollectionAssert.Contains(offered, ContentForm.Base64);
        CollectionAssert.Contains(offered, ContentForm.Byte);
        CollectionAssert.DoesNotContain(offered, ContentForm.Utf8);
        CollectionAssert.DoesNotContain(offered, ContentForm.Bool);
        CollectionAssert.DoesNotContain(offered, ContentForm.Int32);

        sut.Load(Data.FromInt(-5).ToArray());
        offered = sut.Forms.Select(option => option.Form).ToArray();

        CollectionAssert.Contains(offered, ContentForm.Int32);
        CollectionAssert.Contains(offered, ContentForm.UInt32);
        CollectionAssert.DoesNotContain(offered, ContentForm.Single, "FB FF FF FF is a NaN, which does not give back the same bytes");
        CollectionAssert.DoesNotContain(offered, ContentForm.Int64);
        CollectionAssert.DoesNotContain(offered, ContentForm.Guid);
    }

    [TestMethod]
    public void Load_UsesThePreferredForm_IfItCanShowTheContent()
    {
        ContentEditorViewModel sut = Create();

        sut.Load(Data.FromInt(1234).ToArray(), preferredForm: ContentForm.Int32);
        Assert.AreEqual(ContentForm.Int32, sut.Form);
        Assert.AreEqual("1234", sut.Text);

        sut.Load("text"u8.ToArray(), preferredForm: ContentForm.Int64);
        Assert.AreEqual(ContentForm.Utf8, sut.Form, "a long can not show 4 bytes");
    }

    [TestMethod]
    public void FormChosen_IsRaisedForTheUsersChoiceOnly()
    {
        ContentEditorViewModel sut = Create();
        var chosen = new List<ContentForm>();
        sut.FormChosen += (_, form) => chosen.Add(form);

        sut.Load(Data.FromInt(7).ToArray(), preferredForm: ContentForm.UInt32);
        Choose(sut, ContentForm.Int32);
        sut.Load("x"u8.ToArray());

        CollectionAssert.AreEqual(new[] { ContentForm.Int32 }, chosen);
    }

    [TestMethod]
    public void TooLongContent_IsNotEditable()
    {
        ContentEditorViewModel sut = Create();

        sut.Load(new byte[ContentFormat.MaxEditableLength + 1]);

        Assert.IsTrue(sut.IsReadOnly);
        Assert.IsFalse(sut.CanChangeForm);
        Assert.IsTrue(sut.ExportCommand.CanExecute(null));
    }

    [TestMethod]
    public void DisabledEditor_HasNoCommands()
    {
        ContentEditorViewModel sut = Create();

        sut.Load("x"u8.ToArray(), enabled: false);

        Assert.AreEqual(string.Empty, sut.Text);
        Assert.IsFalse(sut.ClearCommand.CanExecute(null));
        Assert.IsFalse(sut.ImportCommand.CanExecute(null));
        Assert.IsFalse(sut.ExportCommand.CanExecute(null));
    }

    [TestMethod]
    public void Clear_StoresAnEmptyContent()
    {
        ContentEditorViewModel sut = Create();
        sut.Load("x"u8.ToArray());

        sut.ClearCommand.Execute(null);

        Assert.AreEqual(0, stored.Single().Length);
        Assert.IsFalse(sut.ClearCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task Import_StoresTheFile()
    {
        ContentEditorViewModel sut = Create();
        sut.Load(Array.Empty<byte>());
        dialogs.PickContentToImportAsync(Arg.Any<string>()).Returns("in.bin");
        files.ReadBytesAsync("in.bin").Returns([1, 2, 3]);

        await sut.ImportCommand.ExecuteAsync(null);

        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, stored.Single());
        Assert.AreEqual("01 02 03", sut.Text);
    }

    [TestMethod]
    public async Task Import_ReportsIoErrors()
    {
        ContentEditorViewModel sut = Create();
        sut.Load(Array.Empty<byte>());
        dialogs.PickContentToImportAsync(Arg.Any<string>()).Returns("in.bin");
        files.ReadBytesAsync("in.bin").Returns(Task.FromException<byte[]>(new IOException("gone")));

        await sut.ImportCommand.ExecuteAsync(null);

        Assert.AreEqual(0, stored.Count);
        Assert.IsTrue(sut.IsError);
        Assert.AreEqual("gone", sut.Info);
    }

    [TestMethod]
    public async Task Export_WritesTheContent()
    {
        ContentEditorViewModel sut = Create();
        sut.Load(new byte[] { 7 });
        dialogs.PickContentToExportAsync("Export Data", "data.bin").Returns("out.bin");

        await sut.ExportCommand.ExecuteAsync(null);

        await files.Received(1).WriteBytesAsync("out.bin", Arg.Is<ReadOnlyMemory<byte>>(content => content.ToArray().SequenceEqual(new byte[] { 7 })));
    }

    private static void Choose(ContentEditorViewModel sut, ContentForm form)
    {
        sut.SelectedForm = sut.Forms.Single(option => option.Form == form);
    }

    private ContentEditorViewModel Create()
    {
        var sut = new ContentEditorViewModel("Data", dialogs, files);
        sut.ContentChanged += (_, content) => stored.Add(content.ToArray());
        return sut;
    }
}
