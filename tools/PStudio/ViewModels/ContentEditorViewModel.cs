using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PStudio.Interfaces;
using PStudio.Models;
using PStudio.Presentation;
using PStudio.Services;

namespace PStudio.ViewModels;

/// <summary>
/// Shows and edits one content (the data or the metadata of a node) in a chosen form.
/// The edited text is stored by Apply; Clear and Import store the new content immediately.
/// </summary>
public sealed partial class ContentEditorViewModel : ViewModelBase
{
    private readonly IFileDialogService fileDialogs;
    private readonly ITreeFileService files;

    private ReadOnlyMemory<byte> content;

    // Set while the text and the form are changed by the view model, not by the user
    private bool loading;

    [ObservableProperty]
    private int selectedFormIndex;

    [ObservableProperty]
    private string text = string.Empty;

    [ObservableProperty]
    private bool isReadOnly;

    [ObservableProperty]
    private bool canChangeForm = true;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ImportCommand), nameof(ClearCommand))]
    private bool isEnabled;

    [ObservableProperty]
    private string info = string.Empty;

    [ObservableProperty]
    private bool isError;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand), nameof(RevertCommand), nameof(ClearCommand))]
    private bool isPending;

    /// <summary>
    /// Creates the editor.
    /// </summary>
    public ContentEditorViewModel(string title, IFileDialogService fileDialogs, ITreeFileService files)
    {
        Title = title;
        this.fileDialogs = fileDialogs ?? throw new ArgumentNullException(nameof(fileDialogs));
        this.files = files ?? throw new ArgumentNullException(nameof(files));
        Load(ReadOnlyMemory<byte>.Empty, enabled: false);
    }

    /// <summary>
    /// Raised when a new content should be stored in the node.
    /// </summary>
    public event EventHandler<ReadOnlyMemory<byte>>? ContentChanged;

    /// <summary>The names of the forms, in the order of <see cref="ContentForm"/>.</summary>
    public static IReadOnlyList<string> FormNames { get; } = Enum.GetValues<ContentForm>().Select(ContentFormat.Name).ToArray();

    /// <summary>The title (Data or MetaData).</summary>
    public string Title { get; }

    /// <summary>The form of the text.</summary>
    public ContentForm Form => (ContentForm)SelectedFormIndex;

    /// <summary>The shown content (without the edits that are not applied).</summary>
    public ReadOnlyMemory<byte> Content => content;

    /// <summary>
    /// Shows a content, in the form that suits it. A disabled editor shows nothing (no node is selected).
    /// </summary>
    public void Load(ReadOnlyMemory<byte> value, bool enabled = true)
    {
        content = value;
        IsEnabled = enabled;
        Show(ContentFormat.Suggest(value.Span));
        ExportCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Applies the edited text, if there is any.
    /// </summary>
    /// <returns>False if the text is not valid in its form (the edit is kept)</returns>
    public bool ApplyPending()
    {
        if (!IsPending)
        {
            return true;
        }

        try
        {
            Store(ContentFormat.Parse(Text, Form), Form);
            return true;
        }
        catch (FormatException ex)
        {
            ShowInfo(ex.Message, error: true);
            return false;
        }
    }

    [RelayCommand(CanExecute = nameof(IsPending))]
    private void Apply() => ApplyPending();

    [RelayCommand(CanExecute = nameof(IsPending))]
    private void Revert() => Show(Form);

    [RelayCommand(CanExecute = nameof(CanClear))]
    private void Clear() => Store(ReadOnlyMemory<byte>.Empty, null);

    private bool CanClear() => IsEnabled && (!content.IsEmpty || IsPending);

    [RelayCommand(CanExecute = nameof(IsEnabled))]
    private async Task ImportAsync()
    {
        if (await fileDialogs.PickContentToImportAsync($"Import {Title}") is not string path)
        {
            return;
        }

        try
        {
            Store(await files.ReadBytesAsync(path), null);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowInfo(ex.Message, error: true);
        }
    }

    [RelayCommand(CanExecute = nameof(CanExport))]
    private async Task ExportAsync()
    {
        string suggestedName = Title.ToLowerInvariant() + ".bin";

        if (await fileDialogs.PickContentToExportAsync($"Export {Title}", suggestedName) is not string path)
        {
            return;
        }

        try
        {
            await files.WriteBytesAsync(path, content);
            ShowInfo($"{Describe(content.Length)}, exported to {System.IO.Path.GetFileName(path)}", error: false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowInfo(ex.Message, error: true);
        }
    }

    private bool CanExport() => IsEnabled && !content.IsEmpty;

    partial void OnSelectedFormIndexChanged(int oldValue, int newValue)
    {
        if (loading || newValue < 0)
        {
            return;
        }

        var oldForm = (ContentForm)oldValue;

        if (!IsPending)
        {
            Show(Form);
            return;
        }

        // An edit that is valid in the old form, and can be shown in the new one, is converted, without storing it.
        // Otherwise the text was meant in the new form (e.g. text typed before choosing UTF-8), so it is kept as it is.
        byte[]? bytes = null;

        try
        {
            bytes = ContentFormat.Parse(Text, oldForm);
        }
        catch (FormatException)
        {
        }

        if (bytes is not null && ContentFormat.CanShow(bytes, Form))
        {
            loading = true;
            Text = ContentFormat.Format(bytes, Form);
            loading = false;
            ShowInfo($"{Describe(bytes.Length)}, not applied yet", error: false);
        }
        else
        {
            ShowInfo($"Edited as {FormNames[newValue]}, not applied yet", error: false);
        }

        IsReadOnly = false;
    }

    partial void OnTextChanged(string value)
    {
        if (loading)
        {
            return;
        }

        IsPending = true;
        ShowInfo("Edited, not applied yet (Ctrl+Enter applies, Escape reverts)", error: false);
    }

    private void Store(ReadOnlyMemory<byte> value, ContentForm? form)
    {
        content = value;
        Show(form ?? ContentFormat.Suggest(value.Span));
        ContentChanged?.Invoke(this, content);
        ExportCommand.NotifyCanExecuteChanged();
    }

    private void Show(ContentForm form)
    {
        loading = true;

        SelectedFormIndex = (int)form;

        if (!IsEnabled)
        {
            Text = string.Empty;
            IsReadOnly = true;
            CanChangeForm = false;
        }
        else if (content.Length > ContentFormat.MaxEditableLength)
        {
            Text = $"The content is {Describe(content.Length)} long, which is too long to edit here. Use Import or Export.";
            IsReadOnly = true;
            CanChangeForm = false;
        }
        else
        {
            // A content that the form can not show is shown as hex, read-only
            IsReadOnly = !ContentFormat.CanShow(content.Span, form);
            Text = ContentFormat.Format(content.Span, IsReadOnly ? ContentForm.Hex : form);
            CanChangeForm = true;
        }

        loading = false;
        IsPending = false;
        ClearCommand.NotifyCanExecuteChanged();

        if (!IsEnabled)
        {
            ShowInfo(string.Empty, error: false);
        }
        else if (IsReadOnly && content.Length <= ContentFormat.MaxEditableLength)
        {
            ShowInfo($"{Describe(content.Length)}, not a valid {FormNames[(int)form]}: shown as hex, read-only", error: true);
        }
        else
        {
            ShowInfo(Describe(content.Length), error: false);
        }
    }

    private void ShowInfo(string message, bool error)
    {
        Info = message;
        IsError = error;
    }

    private static string Describe(int length) => length == 1 ? "1 byte" : $"{length:N0} bytes";
}
