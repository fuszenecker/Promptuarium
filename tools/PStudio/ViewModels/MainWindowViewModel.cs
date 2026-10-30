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
using Promptuarium;

namespace PStudio.ViewModels;

/// <summary>
/// The view model of an editor window. It edits a tree stored in a file, or (nested) a tree stored in the data
/// of a node of another editor; the commands of the menu and of the tree view are here.
/// </summary>
public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly ITreeFileService files;
    private readonly IChunkAnalyzer chunkAnalyzer;
    private readonly INodeClipboard clipboard;
    private readonly IFileDialogService fileDialogs;
    private readonly IMessageDialogService messages;
    private readonly IEditorWindowService windows;
    private readonly IFormMemory formMemory;
    private readonly ISettingsStore settings;
    private readonly List<MainWindowViewModel> nestedEditors = [];

    // The node the content editors show (the selection, until the edits are applied)
    private NodeViewModel? editedNode;

    // For a nested tree: the editor and the node whose data holds the tree
    private MainWindowViewModel? hostEditor;
    private NodeViewModel? hostNode;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NodePath), nameof(NodeSummary), nameof(NodeEncoding))]
    [NotifyCanExecuteChangedFor(
        nameof(AddChildCommand), nameof(AddSiblingCommand), nameof(DeleteCommand),
        nameof(MoveUpCommand), nameof(MoveDownCommand), nameof(IndentCommand), nameof(OutdentCommand),
        nameof(CopyCommand), nameof(CutCommand), nameof(PasteAsChildCommand), nameof(PasteAfterCommand),
        nameof(OpenNestedCommand))]
    private NodeViewModel? selectedNode;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title), nameof(DocumentName))]
    private string? fileName;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    private bool isModified;

    [ObservableProperty]
    private string statusText = "Ready";

    [ObservableProperty]
    private bool isStatusError;

    [ObservableProperty]
    private bool isChunkPanelVisible = true;

    [ObservableProperty]
    private ChunkRowViewModel? selectedChunk;

    // The nodes in pre-order: the chunks refer to them by index
    private List<NodeViewModel> preOrder = [];

    // Set while the selection of the chunk panel follows the selected node
    private bool syncingChunks;

    [ObservableProperty]
    private string chunkSummary = string.Empty;

    /// <summary>
    /// Creates the view model with a new, empty tree.
    /// </summary>
    public MainWindowViewModel(
        ITreeFileService files,
        IChunkAnalyzer chunkAnalyzer,
        INodeClipboard clipboard,
        IFileDialogService fileDialogs,
        IMessageDialogService messages,
        IEditorWindowService windows,
        IViewModelFactory viewModelFactory,
        IFormMemory formMemory,
        ISettingsStore settings)
    {
        ArgumentNullException.ThrowIfNull(viewModelFactory);

        this.files = files ?? throw new ArgumentNullException(nameof(files));
        this.chunkAnalyzer = chunkAnalyzer ?? throw new ArgumentNullException(nameof(chunkAnalyzer));
        this.clipboard = clipboard ?? throw new ArgumentNullException(nameof(clipboard));
        this.fileDialogs = fileDialogs ?? throw new ArgumentNullException(nameof(fileDialogs));
        this.messages = messages ?? throw new ArgumentNullException(nameof(messages));
        this.windows = windows ?? throw new ArgumentNullException(nameof(windows));
        this.formMemory = formMemory ?? throw new ArgumentNullException(nameof(formMemory));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        isChunkPanelVisible = settings.Settings.View.IsChunkPanelVisible;
        settings.Saved += OnSettingsSaved;
        RefreshRecentFiles();

        Data = viewModelFactory.CreateContentEditor("Data");
        MetaData = viewModelFactory.CreateContentEditor("MetaData");
        Data.ContentChanged += (_, content) => StoreContent(content, data: true);
        MetaData.ContentChanged += (_, content) => StoreContent(content, data: false);
        Data.FormChosen += (_, form) => editedNode?.DataForm = form;
        MetaData.FormChosen += (_, form) => editedNode?.MetaDataForm = form;

        SetTree(new Element(), null);
    }

    /// <summary>Raised when the window should close (after the changes are handled).</summary>
    public event Action? CloseRequested;

    /// <summary>The root of the tree, as the only item of the tree view.</summary>
    public ObservableCollection<NodeViewModel> Roots { get; } = [];

    /// <summary>The root of the tree.</summary>
    public NodeViewModel Root => Roots[0];

    /// <summary>The items of File / Open Recent: the recent files, then Clear Recent Files.</summary>
    public ObservableCollection<MenuEntry> RecentFiles { get; } = [];

    /// <summary>Whether there are recent files.</summary>
    public bool HasRecentFiles => RecentFiles.Count > 0;

    /// <summary>The editor of the data of the selected node.</summary>
    public ContentEditorViewModel Data { get; }

    /// <summary>The editor of the metadata of the selected node.</summary>
    public ContentEditorViewModel MetaData { get; }

    /// <summary>The chunks of the tree, as it would be saved.</summary>
    public ObservableCollection<ChunkRowViewModel> Chunks { get; } = [];

    /// <summary>Whether the tree is stored in the data of a node of another editor.</summary>
    public bool IsNested => hostEditor is not null;

    /// <summary>Whether the editor is not nested (New, Open and Save As are only there).</summary>
    public bool IsNotNested => !IsNested;

    /// <summary>The name of the edited tree.</summary>
    public string DocumentName => hostEditor is not null && hostNode is not null
        ? $"{hostEditor.DocumentName} {hostNode.Path}"
        : FileName is null ? "untitled" : System.IO.Path.GetFileName(FileName);

    /// <summary>The title of the window.</summary>
    public string Title => $"{DocumentName}{(IsNested ? " (nested)" : string.Empty)}{(IsModified ? " *" : string.Empty)} - Promptuarium Studio";

    /// <summary>The path of the selected node.</summary>
    public string NodePath => SelectedNode?.Path ?? "No node is selected";

    /// <summary>The children and the contents of the selected node.</summary>
    public string NodeSummary
    {
        get
        {
            if (SelectedNode is not { } node)
            {
                return string.Empty;
            }

            Element element = node.Element;
            string summary = $"{Count(element.Children.Count, "child", "children")}  ·  data {Count(element.Data.Length, "byte", "bytes")}  ·  metadata {Count(element.MetaData.Length, "byte", "bytes")}";

            // See BINARY_FORMAT.md, 10: a root without data, with a short metadata, identifies the file
            if (node.IsRoot && element.Data.IsEmpty && element.MetaData.Length is > 0 and <= ElementBuilder.MaxSignatureLength)
            {
                summary += $"  ·  signature {Services.ContentFormat.Preview(element.MetaData.Span)}";
            }

            return summary;
        }
    }

    /// <summary>The chunks of the selected node in the saved form.</summary>
    public string NodeEncoding
    {
        get
        {
            List<ChunkRow> chunks = Chunks.Where(chunk => chunk.IsOfSelectedNode).Select(chunk => chunk.Row).ToList();

            if (chunks.Count == 0)
            {
                return string.Empty;
            }

            return $"saved as {Count(chunks.Count, "chunk", "chunks")}, {Count(chunks.Sum(chunk => (long)chunk.Size), "byte", "bytes")}, from offset {chunks[0].Offset}";
        }
    }

    /// <summary>Whether closing the window needs a question (there are unsaved changes here or in a nested editor).</summary>
    public bool HasUnsavedChanges => IsModified || nestedEditors.Any(editor => editor.HasUnsavedChanges);

    /// <summary>
    /// Opens a tree file (without asking about the unsaved changes).
    /// </summary>
    /// <returns>False if the file can not be opened</returns>
    public async Task<bool> OpenFileAsync(string path)
    {
        try
        {
            Element root = await files.LoadAsync(path);
            SetTree(root, path);
            AddRecentFile(path);
            ShowStatus($"Opened {path}", error: false);
            return true;
        }
        catch (Exception ex) when (ex is PromptuariumException or IOException or UnauthorizedAccessException)
        {
            // A recent file that can not be opened any more is dropped from the list
            RemoveRecentFile(path);
            ShowStatus($"Can not open {path}: {ex.Message}", error: true);
            await messages.ShowErrorAsync("Can not open the file", $"{path}\n\n{ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Makes this editor edit the data of a node of another editor as a tree.
    /// </summary>
    /// <exception cref="PromptuariumException">The data is not a valid tree</exception>
    public void OpenNested(MainWindowViewModel host, NodeViewModel node)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(node);

        Element root = Element.FromBytes(node.Element.Data);

        hostEditor = host;
        hostNode = node;
        host.nestedEditors.Add(this);
        SetTree(root, null);
        OnPropertyChanged(nameof(IsNested));
        OnPropertyChanged(nameof(IsNotNested));
        OpenRecentCommand.NotifyCanExecuteChanged();
        ShowStatus($"Editing the data of {node.Path} as a tree; Save stores it in the node", error: false);
    }

    /// <summary>
    /// Asks about the unsaved changes (of the nested editors first) before the window closes.
    /// </summary>
    /// <returns>Whether the window can close</returns>
    public async Task<bool> ConfirmCloseAsync()
    {
        foreach (MainWindowViewModel nested in nestedEditors.ToList())
        {
            if (!await nested.ConfirmCloseAsync())
            {
                return false;
            }
        }

        // An invalid edit makes Save fail below, so the window stays open with the edit
        CommitEdits();

        if (!IsModified)
        {
            RememberForms();
            return true;
        }

        switch (await messages.AskSaveChangesAsync(DocumentName))
        {
            case SaveChoice.Save:
                return await SaveTreeAsync(saveAs: false);
            case SaveChoice.Discard:
                IsModified = false;
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Tells the view model that its window closed.
    /// </summary>
    public void Closed()
    {
        settings.Saved -= OnSettingsSaved;
        hostEditor?.nestedEditors.Remove(this);
    }

    [RelayCommand(CanExecute = nameof(IsNotNested))]
    private async Task NewAsync()
    {
        if (await ConfirmDiscardAsync())
        {
            SetTree(new Element(), null);
            ShowStatus("New tree", error: false);
        }
    }

    [RelayCommand(CanExecute = nameof(IsNotNested))]
    private async Task OpenAsync()
    {
        if (await ConfirmDiscardAsync() && await fileDialogs.PickTreeToOpenAsync() is string path)
        {
            await OpenFileAsync(path);
        }
    }

    [RelayCommand(CanExecute = nameof(IsNotNested))]
    private async Task OpenRecentAsync(string path)
    {
        if (await ConfirmDiscardAsync())
        {
            await OpenFileAsync(path);
        }
    }

    [RelayCommand]
    private void ClearRecentFiles()
    {
        settings.Settings.RecentFiles.Clear();
        settings.Save();
    }

    [RelayCommand]
    private Task SaveAsync() => SaveTreeAsync(saveAs: false);

    [RelayCommand(CanExecute = nameof(IsNotNested))]
    private Task SaveAsAsync() => SaveTreeAsync(saveAs: true);

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke();

    [RelayCommand]
    private void ToggleChunkPanel() => IsChunkPanelVisible = !IsChunkPanelVisible;

    // The forms chosen by the user are forgotten: every content gets its default form again
    [RelayCommand]
    private void ResetTypes()
    {
        if (!CommitEdits())
        {
            ShowStatus("The types were not reset: an edited content is not valid", error: true);
            return;
        }

        var stack = new Stack<NodeViewModel>([Root]);

        while (stack.Count > 0)
        {
            NodeViewModel node = stack.Pop();
            node.DataForm = null;
            node.MetaDataForm = null;

            foreach (NodeViewModel child in node.Children)
            {
                stack.Push(child);
            }
        }

        if (editedNode is not null)
        {
            Data.Load(editedNode.Element.Data, preferredForm: DataFormOf(editedNode));
            MetaData.Load(editedNode.Element.MetaData, preferredForm: MetaDataFormOf(editedNode));
        }

        // The tree is the same as the file, so the memory can follow at once
        if (!IsModified)
        {
            RememberForms();
        }

        ShowStatus("The types were reset to the defaults", error: false);
    }

    private void AddRecentFile(string path)
    {
        string fullPath = System.IO.Path.GetFullPath(path);
        List<string> recent = settings.Settings.RecentFiles;

        recent.Remove(fullPath);
        recent.Insert(0, fullPath);

        if (recent.Count > StudioSettings.MaxRecentFiles)
        {
            recent.RemoveRange(StudioSettings.MaxRecentFiles, recent.Count - StudioSettings.MaxRecentFiles);
        }

        settings.Save();
    }

    private void RemoveRecentFile(string path)
    {
        if (settings.Settings.RecentFiles.Remove(System.IO.Path.GetFullPath(path)))
        {
            settings.Save();
        }
    }

    // Another window (or this one) changed the settings
    private void OnSettingsSaved(object? sender, EventArgs e) => RefreshRecentFiles();

    private void RefreshRecentFiles()
    {
        RecentFiles.Clear();
        List<string> recent = settings.Settings.RecentFiles;

        for (int i = 0; i < recent.Count; i++)
        {
            // The number is the access key; an underscore in the name is not one
            string name = System.IO.Path.GetFileName(recent[i]).Replace("_", "__", StringComparison.Ordinal);
            string folder = (System.IO.Path.GetDirectoryName(recent[i]) ?? string.Empty).Replace("_", "__", StringComparison.Ordinal);
            string key = i < 9 ? $"_{i + 1} " : string.Empty;
            RecentFiles.Add(new MenuEntry($"{key}{name}  ({folder})", OpenRecentCommand, recent[i]));
        }

        if (recent.Count > 0)
        {
            RecentFiles.Add(new MenuEntry("-"));
            RecentFiles.Add(new MenuEntry("_Clear Recent Files", ClearRecentFilesCommand));
        }

        OnPropertyChanged(nameof(HasRecentFiles));
    }

    // The choice is kept for the next windows and sessions
    partial void OnIsChunkPanelVisibleChanged(bool value)
    {
        settings.Settings.View.IsChunkPanelVisible = value;
        settings.Save();
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void AddChild()
    {
        NodeViewModel parent = SelectedNode!;
        var child = new NodeViewModel(new Element());

        parent.Insert(parent.Children.Count, child);
        Changed(child, $"Added {child.Path}");
    }

    [RelayCommand(CanExecute = nameof(IsNotRootSelected))]
    private void AddSibling()
    {
        NodeViewModel node = SelectedNode!;
        var sibling = new NodeViewModel(new Element());

        node.Parent!.Insert(node.Index + 1, sibling);
        Changed(sibling, $"Added {sibling.Path}");
    }

    [RelayCommand(CanExecute = nameof(IsNotRootSelected))]
    private void Delete()
    {
        NodeViewModel node = SelectedNode!;
        NodeViewModel parent = node.Parent!;
        int index = node.Index;
        string path = node.Path;

        // The content editors show the deleted node: their edits are dropped
        editedNode = null;
        parent.Detach(node);

        NodeViewModel next = parent.Children.Count > index ? parent.Children[index]
            : index > 0 ? parent.Children[index - 1]
            : parent;

        Changed(next, $"Deleted {path}");
    }

    [RelayCommand(CanExecute = nameof(CanMoveUp))]
    private void MoveUp() => MoveBy(-1);

    [RelayCommand(CanExecute = nameof(CanMoveDown))]
    private void MoveDown() => MoveBy(+1);

    [RelayCommand(CanExecute = nameof(CanIndent))]
    private void Indent()
    {
        NodeViewModel node = SelectedNode!;
        NodeViewModel previous = node.Parent!.Children[node.Index - 1];

        node.Parent.Detach(node);
        previous.Insert(previous.Children.Count, node);
        Changed(node, $"Moved to {node.Path}");
    }

    [RelayCommand(CanExecute = nameof(CanOutdent))]
    private void Outdent()
    {
        NodeViewModel node = SelectedNode!;
        NodeViewModel parent = node.Parent!;
        NodeViewModel grandParent = parent.Parent!;

        parent.Detach(node);
        grandParent.Insert(parent.Index + 1, node);
        Changed(node, $"Moved to {node.Path}");
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void Copy()
    {
        CommitEdits();
        clipboard.Copy(SelectedNode!.Element);
        NotifyPasteCommands();
        ShowStatus($"Copied {SelectedNode.Path}", error: false);
    }

    [RelayCommand(CanExecute = nameof(IsNotRootSelected))]
    private void Cut()
    {
        CommitEdits();
        clipboard.Copy(SelectedNode!.Element);
        NotifyPasteCommands();
        Delete();
    }

    [RelayCommand(CanExecute = nameof(CanPasteAsChild))]
    private void PasteAsChild()
    {
        NodeViewModel parent = SelectedNode!;
        var pasted = new NodeViewModel(clipboard.Paste()!);

        parent.Insert(parent.Children.Count, pasted);
        Changed(pasted, $"Pasted {pasted.Path}");
    }

    [RelayCommand(CanExecute = nameof(CanPasteAfter))]
    private void PasteAfter()
    {
        NodeViewModel node = SelectedNode!;
        var pasted = new NodeViewModel(clipboard.Paste()!);

        node.Parent!.Insert(node.Index + 1, pasted);
        Changed(pasted, $"Pasted {pasted.Path}");
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task OpenNestedAsync()
    {
        if (!CommitEdits())
        {
            return;
        }

        try
        {
            windows.OpenNested(this, SelectedNode!);
        }
        catch (PromptuariumException ex)
        {
            ShowStatus($"The data of {SelectedNode!.Path} is not a tree: {ex.Message}", error: true);
            await messages.ShowErrorAsync("Not a tree", $"The data of {SelectedNode.Path} is not a valid tree.\n\n{ex.Message}");
        }
    }

    private bool HasSelection() => SelectedNode is not null;

    private bool IsNotRootSelected() => SelectedNode is { IsRoot: false };

    private bool CanMoveUp() => SelectedNode is { IsRoot: false } node && node.Index > 0;

    private bool CanMoveDown() => SelectedNode is { IsRoot: false } node && node.Index < node.Parent!.Children.Count - 1;

    private bool CanIndent() => CanMoveUp();

    private bool CanOutdent() => SelectedNode is { Parent.IsRoot: false };

    private bool CanPasteAsChild() => SelectedNode is not null && clipboard.HasNode;

    private bool CanPasteAfter() => SelectedNode is { IsRoot: false } && clipboard.HasNode;

    partial void OnSelectedNodeChanged(NodeViewModel? value)
    {
        if (editedNode is not null && editedNode != value && !CommitEdits())
        {
            ShowStatus($"The edit of {editedNode.Path} is not valid, it was dropped", error: true);
        }

        editedNode = value;
        Data.Load(value?.Element.Data ?? ReadOnlyMemory<byte>.Empty, value is not null, DataFormOf(value));
        MetaData.Load(value?.Element.MetaData ?? ReadOnlyMemory<byte>.Empty, value is not null, MetaDataFormOf(value));
        HighlightChunks();
    }

    partial void OnSelectedChunkChanged(ChunkRowViewModel? value)
    {
        // A chunk chosen in the chunk panel selects its node
        if (!syncingChunks && value is { Row.NodeIndex: >= 0 and int index } && index < preOrder.Count)
        {
            SelectedNode = preOrder[index];
        }
    }

    /// <summary>
    /// Moves a node (dragged and dropped) before, after or into another one.
    /// </summary>
    /// <returns>False if the move is not possible: the root, or into the node itself or into its descendant</returns>
    public bool MoveNode(NodeViewModel node, NodeViewModel target, DropPosition position)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(target);

        if (!CanMoveNode(node, target, position))
        {
            return false;
        }

        CommitEdits();
        node.Parent!.Detach(node);

        if (position == DropPosition.Inside)
        {
            target.Insert(target.Children.Count, node);
        }
        else
        {
            target.Parent!.Insert(target.Index + (position == DropPosition.After ? 1 : 0), node);
        }

        Changed(node, $"Moved to {node.Path}");

        // The selection is the same node, but the tree view lost it while the node was detached
        OnPropertyChanged(nameof(SelectedNode));
        return true;
    }

    /// <summary>
    /// Whether a node can be dropped there.
    /// </summary>
    public static bool CanMoveNode(NodeViewModel node, NodeViewModel target, DropPosition position)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(target);

        if (node.IsRoot || (target.IsRoot && position != DropPosition.Inside))
        {
            return false;
        }

        for (NodeViewModel? ancestor = target; ancestor is not null; ancestor = ancestor.Parent)
        {
            if (ancestor == node)
            {
                return false;
            }
        }

        return true;
    }

    private void MoveBy(int offset)
    {
        NodeViewModel node = SelectedNode!;

        node.Parent!.Move(node.Index, node.Index + offset);
        Changed(node, $"Moved to {node.Path}");
    }

    private async Task<bool> SaveTreeAsync(bool saveAs)
    {
        if (!CommitEdits())
        {
            ShowStatus("Not saved: an edited content is not valid", error: true);
            return false;
        }

        if (hostEditor is not null && hostNode is not null)
        {
            hostEditor.StoreNestedTree(hostNode, Root.Element.ToBytes());
            IsModified = false;
            ShowStatus($"Stored in the data of {hostNode.Path} of {hostEditor.DocumentName}", error: false);
            return true;
        }

        string? path = saveAs || FileName is null ? await fileDialogs.PickTreeToSaveAsync(FileName is null ? null : System.IO.Path.GetFileName(FileName)) : FileName;

        if (path is null)
        {
            return false;
        }

        try
        {
            await files.SaveAsync(Root.Element, path);
            FileName = path;
            IsModified = false;
            RememberForms();
            AddRecentFile(path);
            ShowStatus($"Saved {path}", error: false);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowStatus($"Can not save {path}: {ex.Message}", error: true);
            await messages.ShowErrorAsync("Can not save the file", $"{path}\n\n{ex.Message}");
            return false;
        }
    }

    // A nested editor saved its tree into the data of a node
    private void StoreNestedTree(NodeViewModel node, byte[] bytes)
    {
        node.SetData(bytes);

        if (node == editedNode)
        {
            Data.Load(node.Element.Data, preferredForm: DataFormOf(node));
        }

        Changed(null, $"The nested tree of {node.Path} was stored");
    }

    private async Task<bool> ConfirmDiscardAsync()
    {
        CommitEdits();

        if (!IsModified)
        {
            RememberForms();
            return true;
        }

        return await messages.AskSaveChangesAsync(DocumentName) switch
        {
            SaveChoice.Save => await SaveTreeAsync(saveAs: false),
            SaveChoice.Discard => true,
            _ => false,
        };
    }

    // Applies the edited texts of the content editors; false if one of them is invalid
    private bool CommitEdits()
    {
        bool data = Data.ApplyPending();
        bool metaData = MetaData.ApplyPending();
        return data && metaData;
    }

    private void StoreContent(ReadOnlyMemory<byte> content, bool data)
    {
        if (editedNode is null)
        {
            return;
        }

        if (data)
        {
            editedNode.SetData(content);
        }
        else
        {
            editedNode.SetMetaData(content);

            // The default form of the data follows the tag (e.g. a NAME is UTF-8, a TYPE is a type code)
            if (editedNode.DataForm is null && !Data.IsPending)
            {
                Data.Load(editedNode.Element.Data, preferredForm: DataFormOf(editedNode));
            }
        }

        Changed(null, $"Changed the {(data ? "data" : "metadata")} of {editedNode.Path}");
    }

    private void SetTree(Element root, string? path)
    {
        editedNode = null;
        Roots.Clear();
        Roots.Add(new NodeViewModel(root));
        RecallForms(path);
        FileName = path;
        IsModified = false;
        SelectedNode = Root;
        OnPropertyChanged(nameof(Root));
        OnPropertyChanged(nameof(DocumentName));
        OnPropertyChanged(nameof(Title));
        RefreshChunks();
    }

    // The form the user chose, otherwise the one of the serializer layout (a one-byte metadata is a tag)
    private static ContentForm? DataFormOf(NodeViewModel? node)
    {
        return node is null ? null : node.DataForm ?? Services.SerializerLayout.DataForm(node.Element, node.Parent?.Element);
    }

    private static ContentForm? MetaDataFormOf(NodeViewModel? node)
    {
        return node is null ? null : node.MetaDataForm ?? Services.SerializerLayout.MetaDataForm(node.Element, node.Parent?.Element);
    }

    // The forms chosen in an earlier session; a path that is not in the tree any more is ignored
    private void RecallForms(string? path)
    {
        if (path is null)
        {
            return;
        }

        FileForms forms = formMemory.Recall(path);

        foreach ((string nodePath, ContentForm form) in forms.Data)
        {
            FindNode(nodePath)?.DataForm = form;
        }

        foreach ((string nodePath, ContentForm form) in forms.MetaData)
        {
            FindNode(nodePath)?.MetaDataForm = form;
        }
    }

    // The forms are remembered by the paths of the nodes, so only when the tree is the same as the file: after saving,
    // or when the window closes or opens another tree without changes. A nested tree has no file.
    private void RememberForms()
    {
        if (FileName is null || IsNested)
        {
            return;
        }

        var forms = new FileForms();
        var stack = new Stack<NodeViewModel>([Root]);

        while (stack.Count > 0)
        {
            NodeViewModel node = stack.Pop();

            if (node.DataForm is { } dataForm)
            {
                forms.Data[node.Path] = dataForm;
            }

            if (node.MetaDataForm is { } metaDataForm)
            {
                forms.MetaData[node.Path] = metaDataForm;
            }

            foreach (NodeViewModel child in node.Children)
            {
                stack.Push(child);
            }
        }

        formMemory.Remember(FileName, forms);
    }

    // A node by its path ("/", "/0/2"), or null
    private NodeViewModel? FindNode(string path)
    {
        NodeViewModel? node = Root;

        foreach (string part in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            node = int.TryParse(part, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int index) && node is not null && index < node.Children.Count
                ? node.Children[index]
                : null;
        }

        return node;
    }

    // After a change of the tree: selects the node (if given), and refreshes everything that depends on the tree
    private void Changed(NodeViewModel? select, string status)
    {
        IsModified = true;

        if (select is not null && select != SelectedNode)
        {
            SelectedNode = select;
        }

        NotifyNodeCommands();
        OnPropertyChanged(nameof(NodePath));
        OnPropertyChanged(nameof(NodeSummary));
        RefreshChunks();
        ShowStatus(status, error: false);
    }

    private void RefreshChunks()
    {
        preOrder = [];
        var stack = new Stack<NodeViewModel>();
        stack.Push(Root);

        while (stack.Count > 0)
        {
            NodeViewModel node = stack.Pop();
            preOrder.Add(node);

            for (int i = node.Children.Count - 1; i >= 0; i--)
            {
                stack.Push(node.Children[i]);
            }
        }

        IReadOnlyList<ChunkRow> rows = chunkAnalyzer.Analyze(Root.Element);

        syncingChunks = true;
        Chunks.Clear();

        foreach (ChunkRow row in rows)
        {
            Chunks.Add(new ChunkRowViewModel(row));
        }

        syncingChunks = false;

        long bytes = rows.Count == 0 ? 0 : rows[^1].Position + rows[^1].Size;
        ChunkSummary = $"{Count(rows.Count, "chunk", "chunks")}, {Count(bytes, "byte", "bytes")}";
        HighlightChunks();
    }

    // The chunks of the selected node are highlighted, and the first one is selected (so it is scrolled into view)
    private void HighlightChunks()
    {
        int index = SelectedNode is null ? -1 : preOrder.IndexOf(SelectedNode);
        ChunkRowViewModel? first = null;

        foreach (ChunkRowViewModel chunk in Chunks)
        {
            chunk.IsOfSelectedNode = index >= 0 && chunk.Row.NodeIndex == index;
            first ??= chunk.IsOfSelectedNode ? chunk : null;
        }

        syncingChunks = true;
        SelectedChunk = first;
        syncingChunks = false;
        OnPropertyChanged(nameof(NodeEncoding));
    }

    // The position of the selection changed, so the moves are possible or not
    private void NotifyNodeCommands()
    {
        MoveUpCommand.NotifyCanExecuteChanged();
        MoveDownCommand.NotifyCanExecuteChanged();
        IndentCommand.NotifyCanExecuteChanged();
        OutdentCommand.NotifyCanExecuteChanged();
    }

    private void NotifyPasteCommands()
    {
        PasteAsChildCommand.NotifyCanExecuteChanged();
        PasteAfterCommand.NotifyCanExecuteChanged();
    }

    private void ShowStatus(string text, bool error)
    {
        StatusText = text;
        IsStatusError = error;
    }

    private static string Count(long count, string one, string many) => $"{count:N0} {(count == 1 ? one : many)}";
}
