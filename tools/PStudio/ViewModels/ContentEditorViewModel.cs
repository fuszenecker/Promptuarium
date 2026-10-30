using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
/// Shows and edits one content (the data or the metadata of a node) in a chosen form; only the forms that can show the
/// content are offered. The edited text is stored by Apply; Clear and Import store the new content immediately.
/// </summary>
public sealed partial class ContentEditorViewModel : ViewModelBase
{
    private readonly IFileDialogService fileDialogs;
    private readonly ITreeFileService files;

    private ReadOnlyMemory<byte> content;

    // Set while the text and the form are changed by the view model, not by the user
    private bool loading;

    [ObservableProperty]
    private FormOption? selectedForm;

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

    // The tooltip of the info: the whole message, or more (e.g. every valid value after an error); none without a message
    [ObservableProperty]
    private string? infoDetail;

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

    /// <summary>
    /// Raised when the user chooses a form (not when the editor chooses one for a loaded content).
    /// </summary>
    public event EventHandler<ContentForm>? FormChosen;

    /// <summary>Every form, in the order of <see cref="ContentForm"/>.</summary>
    public static IReadOnlyList<FormOption> AllForms { get; } = Enum.GetValues<ContentForm>().Select(form => new FormOption(form, ContentFormat.Name(form))).ToArray();

    /// <summary>The forms offered: the ones that can show the content (every form for an empty content).</summary>
    public ObservableCollection<FormOption> Forms { get; } = [];

    /// <summary>The title (Data or MetaData).</summary>
    public string Title { get; }

    /// <summary>The form of the text.</summary>
    public ContentForm Form => SelectedForm?.Form ?? ContentForm.Hex;

    /// <summary>The shown content (without the edits that are not applied).</summary>
    public ReadOnlyMemory<byte> Content => content;

    /// <summary>
    /// Shows a content, in <paramref name="preferredForm"/> if it can show the content, otherwise in the form that suits
    /// it. A disabled editor shows nothing (no node is selected).
    /// </summary>
    public void Load(ReadOnlyMemory<byte> value, bool enabled = true, ContentForm? preferredForm = null)
    {
        content = value;
        IsEnabled = enabled;
        Show(preferredForm ?? ContentFormat.Suggest(value.Span));
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
            ShowInfo(ex.Message, error: true, ContentFormat.Help(Form));
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

    partial void OnSelectedFormChanged(FormOption? oldValue, FormOption? newValue)
    {
        if (loading || newValue is null)
        {
            return;
        }

        ContentForm oldForm = oldValue?.Form ?? ContentForm.Hex;
        FormChosen?.Invoke(this, newValue.Form);

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
            ShowInfo($"Edited as {newValue.Name}, not applied yet", error: false);
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

        OfferForms(form);

        if (Forms.All(option => option.Form != form))
        {
            form = ContentFormat.Suggest(content.Span);
        }

        SelectedForm = Forms.First(option => option.Form == form);

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
            IsReadOnly = false;
            Text = ContentFormat.Format(content.Span, form);
            CanChangeForm = true;
        }

        loading = false;
        IsPending = false;
        ClearCommand.NotifyCanExecuteChanged();

        if (!IsEnabled)
        {
            ShowInfo(string.Empty, error: false);
        }
        else
        {
            ShowInfo(Describe(content.Length), error: false);
        }
    }

    // The forms that can show the content; a content too long to edit has only its form (it can not be changed). The
    // list is replaced only when it changes, so that the selector keeps its selection.
    private void OfferForms(ContentForm form)
    {
        List<FormOption> offered = content.Length > ContentFormat.MaxEditableLength
            ? [AllForms[(int)form]]
            : AllForms.Where(option => ContentFormat.CanShow(content.Span, option.Form)).ToList();

        if (offered.SequenceEqual(Forms))
        {
            return;
        }

        SelectedForm = null;
        Forms.Clear();

        foreach (FormOption option in offered)
        {
            Forms.Add(option);
        }
    }

    private void ShowInfo(string message, bool error, string? detail = null)
    {
        Info = message;
        InfoDetail = message.Length == 0 ? null : detail is null ? message : $"{message}\n\n{detail}";
        IsError = error;
    }

    private static string Describe(int length) => length == 1 ? "1 byte" : $"{length:N0} bytes";
}
