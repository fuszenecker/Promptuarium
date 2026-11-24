using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Promptuarium;
using Promptuarium.Serializer;
using PStudio.Services;
using static PStudio.Services.SerializerEditing;

namespace PStudio.ViewModels;

/// <summary>
/// The serializer view: the tree as the values of SERIALIZER_FORMAT.md (members, elements, types and values), shown
/// when the tree follows the format (PromptuariumSerializer.Validate finds no error in it; the signature does not
/// matter) and the view is switched on. Its selection and the one of the raw tree follow each other, and its edits
/// are made in the raw tree.
/// </summary>
public sealed partial class MainWindowViewModel
{
    // The values of the serializer view by their holders (the root too)
    private readonly Dictionary<NodeViewModel, SerializerNodeViewModel> serializerNodes = [];

    // Set while the selection of one view follows the other one
    private bool syncingSerializer;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSerializerPanelShown))]
    private bool isSerializerPanelVisible = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSerializerPanelShown))]
    private bool isSerializable;

    // The tab of the tree: the raw view or the serializer view
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRawViewSelected))]
    private int selectedViewIndex;

    /// <summary>
    /// Whether the raw tree is shown, with the editors of the metadata and the data beside it; the serializer view edits
    /// in its tree, and the tree is shown beside it as YAML or JSON.
    /// </summary>
    public bool IsRawViewSelected => SelectedViewIndex == RawView;

    // The text beside the serializer view: YAML or JSON (the choice is kept), made only while it is shown
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsJsonShown))]
    private bool isYamlShown = true;

    [ObservableProperty]
    private string serializerText = string.Empty;

    /// <summary>Whether the text beside the serializer view is JSON.</summary>
    public bool IsJsonShown
    {
        get => !IsYamlShown;
        set => IsYamlShown = !value;
    }

    /// <summary>The index of the tab of the raw tree.</summary>
    public const int RawView = 0;

    /// <summary>The index of the tab of the serializer view.</summary>
    public const int SerializerView = 1;

    [ObservableProperty]
    private string serializerProblem = string.Empty;

    [ObservableProperty]
    private string serializerSummary = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(
        nameof(SerializerAddCommand), nameof(SerializerAddAfterCommand), nameof(SerializerDeleteCommand),
        nameof(SerializerMoveUpCommand), nameof(SerializerMoveDownCommand), nameof(SerializerOpenContainerCommand))]
    private SerializerNodeViewModel? selectedSerializerNode;

    /// <summary>Raised when the name (or the value) of a new member or element should get the keyboard focus.</summary>
    public event Action<SerializerNodeViewModel>? SerializerEditRequested;

    /// <summary>
    /// The width of the names of the members of the root in the serializer view: the longest name (or label) of the view
    /// fits at its depth, and the deeper names are narrower by their indentation.
    /// </summary>
    public double SerializerNameWidth { get; private set; }

    /// <summary>The root of the serializer view, as its only item (none if the tree does not follow the format).</summary>
    public ObservableCollection<SerializerNodeViewModel> SerializerRoots { get; } = [];

    /// <summary>Whether the serializer view is shown: it is switched on, and the tree follows the format.</summary>
    public bool IsSerializerPanelShown => IsSerializerPanelVisible && IsSerializable;

    [RelayCommand]
    private void ToggleSerializerPanel()
    {
        IsSerializerPanelVisible = !IsSerializerPanelVisible;

        // Switched on, the view comes to the front
        if (IsSerializerPanelShown)
        {
            SelectedViewIndex = SerializerView;
        }
        else if (IsSerializerPanelVisible)
        {
            ShowStatus($"The serializer view opens when the tree is serializable; now it is not: {SerializerProblem}", error: true);
        }
    }

    // The choice is kept for the next windows and sessions
    partial void OnIsSerializerPanelVisibleChanged(bool value)
    {
        ShowRawViewIfNeeded();
        settings.Settings.View.IsSerializerPanelVisible = value;
        settings.Save();
    }

    partial void OnIsSerializableChanged(bool value) => ShowRawViewIfNeeded();

    // The editors of the metadata and the data are hidden with the raw tree: their edits are applied first (an invalid one is dropped)
    partial void OnIsYamlShownChanged(bool value)
    {
        settings.Settings.View.IsYamlShown = value;
        settings.Save();
        RefreshSerializerText();
    }

    // The tree as YAML or JSON (a BLOB is !!binary or base64, the comments are # lines in YAML)
    private void RefreshSerializerText()
    {
        if (IsRawViewSelected || !IsSerializable)
        {
            return;
        }

        try
        {
            SerializerText = IsYamlShown ? Services.SerializerText.Yaml(Root.Element) : Services.SerializerText.Json(Root.Element);
        }
        catch (Exception ex) when (ex is FormatException or InvalidOperationException or ArgumentException)
        {
            SerializerText = $"The tree can not be shown as {(IsYamlShown ? "YAML" : "JSON")}: {ex.Message}";
        }
    }

    partial void OnSelectedViewIndexChanged(int value)
    {
        RefreshSerializerText();

        if (value == SerializerView && !CommitEdits() && editedNode is not null)
        {
            ShowStatus($"The edit of {editedNode.Path} is not valid, it was dropped", error: true);
            Data.Load(editedNode.Element.Data, preferredForm: DataFormOf(editedNode));
            MetaData.Load(editedNode.Element.MetaData, preferredForm: MetaDataFormOf(editedNode));
        }
    }

    // The tab of a hidden serializer view is hidden too, so the raw tree is shown
    private void ShowRawViewIfNeeded()
    {
        if (!IsSerializerPanelShown)
        {
            SelectedViewIndex = RawView;
        }
    }

    partial void OnSelectedSerializerNodeChanged(SerializerNodeViewModel? value)
    {
        // A value chosen in the serializer view selects its holder, unless a node of the holder is selected already
        // (e.g. its TYPE or VALUE node)
        if (syncingSerializer || value is null || SerializerNodeOf(SelectedNode) == value)
        {
            return;
        }

        syncingSerializer = true;
        ExpandAncestors(value.Holder);
        SelectedNode = value.Holder;
        syncingSerializer = false;
    }

    // The serializer view follows the selected node: its holder (the node itself, or the holder of a TYPE, VALUE or COMMENT node)
    private void SyncSerializerSelection()
    {
        if (syncingSerializer)
        {
            return;
        }

        SerializerNodeViewModel? node = SerializerNodeOf(SelectedNode);

        for (SerializerNodeViewModel? ancestor = node?.Parent; ancestor is not null; ancestor = ancestor.Parent)
        {
            ancestor.IsExpanded = true;
        }

        syncingSerializer = true;
        SelectedSerializerNode = node;
        syncingSerializer = false;
    }

    private SerializerNodeViewModel? SerializerNodeOf(NodeViewModel? node)
    {
        for (; node is not null; node = node.Parent)
        {
            if (serializerNodes.TryGetValue(node, out SerializerNodeViewModel? value))
            {
                return value;
            }
        }

        return null;
    }

    private static void ExpandAncestors(NodeViewModel node)
    {
        for (NodeViewModel? ancestor = node.Parent; ancestor is not null; ancestor = ancestor.Parent)
        {
            ancestor.IsExpanded = true;
        }
    }

    /// <summary>
    /// Checks the tree after a change, and shows it in the serializer view again (keeping the values that are still
    /// there); returns whether the tree stopped being serializable.
    /// </summary>
    private bool RefreshSerializer()
    {
        bool was = IsSerializable;
        IReadOnlyList<ValidationProblem> problems = PromptuariumSerializer.Validate(Root.Element);
        ValidationProblem? error = problems.FirstOrDefault(problem => problem.IsError);
        int warnings = problems.Count(problem => !problem.IsError);

        SerializerProblem = error is null ? string.Empty : $"{error.Path}: {error.Message}";
        serializerNodes.Clear();

        if (error is null)
        {
            if (SerializerRoots.Count == 0 || SerializerRoots[0].Holder != Root)
            {
                SerializerRoots.Clear();
                SerializerRoots.Add(new SerializerNodeViewModel(this, Root, null));
            }
            else
            {
                SerializerRoots[0].Refresh();
            }

            foreach (SerializerNodeViewModel node in SerializerRoots[0].SelfAndDescendants())
            {
                serializerNodes[node.Holder] = node;
            }

            // The names end in one column: wide enough for the longest one at its depth (in the monospaced font of the
            // names, with the padding of their boxes)
            double width = Math.Clamp(
                serializerNodes.Values.Max(node => ((node.IsMember ? node.NameText.Length : node.Label.Length) * 8.6) + 26 + ((node.Depth - 1) * SerializerNodeViewModel.Indent)),
                90,
                360);

            if (width != SerializerNameWidth)
            {
                SerializerNameWidth = width;

                foreach (SerializerNodeViewModel node in serializerNodes.Values)
                {
                    node.NotifyAlignment();
                }
            }

            SerializerSummary = warnings == 0 ? string.Empty
                : $"{warnings} {(warnings == 1 ? "warning" : "warnings")}: {string.Join("; ", problems.Select(problem => $"{problem.Path}: {problem.Message}"))}";
        }
        else
        {
            SerializerRoots.Clear();
            SerializerSummary = string.Empty;
        }

        IsSerializable = error is null;
        RefreshSerializerText();
        SyncSerializerSelection();
        return was && !IsSerializable;
    }

    // ---------------------------------------------------------------- edits

    /// <summary>Tells that an edited value is not a value of its type (it is not applied).</summary>
    internal void SerializerValueRejected(SerializerNodeViewModel node, string message)
    {
        ShowStatus($"The value of {node.Holder.Path} was not applied: {message}", error: true);
    }

    /// <summary>Renames a member.</summary>
    internal void SerializerRename(SerializerNodeViewModel node, string name)
    {
        if (BeginSerializerEdit())
        {
            node.Holder.SetData(System.Text.Encoding.UTF8.GetBytes(name));
            EndSerializerEdit(node, $"Renamed {node.Holder.Path} to \"{name}\"");
        }
    }

    /// <summary>Sets the value of a holder (the data of its VALUE node).</summary>
    internal void SerializerSetValue(SerializerNodeViewModel node, byte[] value)
    {
        if (node.ValueNode is { } valueNode && BeginSerializerEdit())
        {
            valueNode.SetData(value);
            EndSerializerEdit(node, $"Changed the value of {node.Holder.Path}");
        }
    }

    /// <summary>
    /// Changes the type of a holder: a value is converted by its text (or gets the default of the type), an object
    /// becomes an array of its values and an array an object of members named by the indexes; the members or the
    /// elements are not dropped (an object or an array that has them can only become the other one).
    /// </summary>
    internal void SerializerChangeType(SerializerNodeViewModel node, byte type)
    {
        NodeViewModel holder = node.Holder;
        byte old = node.Type;
        List<NodeViewModel> content = holder.Children.Skip(1).Where(child => Tag(child.Element) != CommentTag).ToList();

        if (old is ObjectType or ArrayType && content.Count > 0 && type is not (ObjectType or ArrayType))
        {
            ShowStatus($"The type of {holder.Path} was not changed: delete its {content.Count} {(old == ObjectType ? "members" : "items")} first", error: true);
            return;
        }

        if (!BeginSerializerEdit())
        {
            return;
        }

        byte[]? value = node.ValueNode is { } valueNode && IsScalar(type) ? SerializerEditing.Convert(old, valueNode.Element.Data.Span, type) : null;
        holder.Children[0].SetData(new[] { type });

        if (old is ObjectType or ArrayType && type is ObjectType or ArrayType)
        {
            // The members become elements, or the elements members named by their indexes
            for (int i = 0; i < content.Count; i++)
            {
                content[i].SetMetaData(new[] { type == ArrayType ? ItemTag : NameTag });
                content[i].SetData(type == ArrayType ? ReadOnlyMemory<byte>.Empty : System.Text.Encoding.UTF8.GetBytes(i.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            }
        }
        else
        {
            foreach (NodeViewModel child in content)
            {
                holder.Detach(child);
            }

            if (IsScalar(type))
            {
                holder.Insert(holder.Children.Count, new NodeViewModel(new Element(value ?? DefaultValue(type), new[] { ValueTag })));
            }
        }

        // The forms of the raw view follow the new type
        foreach (NodeViewModel child in holder.Children)
        {
            child.DataForm = null;
        }

        EndSerializerEdit(node, $"Changed the type of {holder.Path} to {TypeOf(type).Name}");
    }

    /// <summary>Sets the comment of a holder (or of the root): one COMMENT node after the TYPE node, or none for an empty text.</summary>
    internal void SerializerSetComment(SerializerNodeViewModel node, string comment)
    {
        if (!BeginSerializerEdit())
        {
            return;
        }

        NodeViewModel holder = node.Holder;

        foreach (NodeViewModel child in holder.Children.Where(child => Tag(child.Element) == CommentTag).ToList())
        {
            holder.Detach(child);
        }

        if (comment.Length > 0)
        {
            holder.Insert(node.IsRoot ? 0 : 1, new NodeViewModel(new Element(System.Text.Encoding.UTF8.GetBytes(comment), new[] { CommentTag })));
        }

        EndSerializerEdit(node, $"Changed the comment of {holder.Path}");
    }

    [RelayCommand(CanExecute = nameof(HasSerializerSelection))]
    private void SerializerAdd()
    {
        SerializerNodeViewModel node = SelectedSerializerNode!;

        // Into an object or an array, otherwise after the value
        if (node.IsStructure)
        {
            AddSerializerValue(node, node.Holder.Children.Count);
        }
        else
        {
            AddSerializerValue(node.Parent!, node.Holder.Index + 1);
        }
    }

    [RelayCommand(CanExecute = nameof(IsSerializerValueSelected))]
    private void SerializerAddAfter()
    {
        SerializerNodeViewModel node = SelectedSerializerNode!;
        AddSerializerValue(node.Parent!, node.Holder.Index + 1);
    }

    [RelayCommand(CanExecute = nameof(IsSerializerValueSelected))]
    private void SerializerDelete()
    {
        if (!BeginSerializerEdit())
        {
            return;
        }

        SerializerNodeViewModel node = SelectedSerializerNode!;
        SerializerNodeViewModel parent = node.Parent!;
        int index = parent.Children.IndexOf(node);
        string path = node.Holder.Path;

        // The content editors may show a node of the deleted holder: their edits are dropped
        editedNode = null;
        parent.Holder.Detach(node.Holder);

        NodeViewModel next = parent.Children.Count > index + 1 ? parent.Children[index + 1].Holder
            : index > 0 ? parent.Children[index - 1].Holder
            : parent.Holder;

        Changed(next, $"Deleted {path}");
    }

    [RelayCommand(CanExecute = nameof(CanSerializerMoveUp))]
    private void SerializerMoveUp() => SerializerMoveBy(-1);

    [RelayCommand(CanExecute = nameof(CanSerializerMoveDown))]
    private void SerializerMoveDown() => SerializerMoveBy(+1);

    [RelayCommand(CanExecute = nameof(IsContainerSelected))]
    private void SerializerOpenContainer()
    {
        if (SelectedSerializerNode?.ValueNode is { } valueNode && CommitEdits())
        {
            windows.OpenNested(this, valueNode);
        }
    }

    private bool HasSerializerSelection() => SelectedSerializerNode is not null;

    private bool IsSerializerValueSelected() => SelectedSerializerNode is { IsRoot: false };

    private bool IsContainerSelected() => SelectedSerializerNode is { IsContainer: true };

    private bool CanSerializerMoveUp() => SelectedSerializerNode is { Parent: { } parent } node && parent.Children.IndexOf(node) > 0;

    private bool CanSerializerMoveDown() => SelectedSerializerNode is { Parent: { } parent } node && parent.Children.IndexOf(node) < parent.Children.Count - 1;

    // A new member (named so that the object does not have it yet) or element, a UTF8_STRING, at the index of the raw children
    private void AddSerializerValue(SerializerNodeViewModel parent, int index)
    {
        if (!BeginSerializerEdit())
        {
            return;
        }

        bool member = parent.Type == ObjectType;
        string name = member ? NewName(parent.Children.Select(child => child.NameText)) : string.Empty;
        var holder = new Element(
            System.Text.Encoding.UTF8.GetBytes(name),
            new[] { member ? NameTag : ItemTag },
            new Element(new[] { Utf8StringType }, new[] { TypeTag }),
            new Element(ReadOnlyMemory<byte>.Empty, new[] { ValueTag }));
        var node = new NodeViewModel(holder);

        parent.Holder.Insert(index, node);
        parent.IsExpanded = true;
        Changed(node, $"Added {node.Path}");

        if (serializerNodes.TryGetValue(node, out SerializerNodeViewModel? added))
        {
            added.IsEditing = true;
            SerializerEditRequested?.Invoke(added);
        }
    }

    private void SerializerMoveBy(int offset)
    {
        if (!BeginSerializerEdit())
        {
            return;
        }

        SerializerNodeViewModel node = SelectedSerializerNode!;
        SerializerNodeViewModel parent = node.Parent!;
        NodeViewModel other = parent.Children[parent.Children.IndexOf(node) + offset].Holder;

        parent.Holder.Move(node.Holder.Index, other.Index);
        Changed(node.Holder, $"Moved to {node.Holder.Path}");
        OnPropertyChanged(nameof(SelectedNode));
    }

    // The edits of the content editors are applied first, so that they are not lost or applied to a changed node
    private bool BeginSerializerEdit()
    {
        if (CommitEdits())
        {
            return true;
        }

        ShowStatus("Not changed: an edited content is not valid", error: true);
        return false;
    }

    private void EndSerializerEdit(SerializerNodeViewModel node, string status)
    {
        Changed(node.Holder, status);

        // The content editors show the changed node (or a node in it) again
        if (editedNode is not null)
        {
            Data.Load(editedNode.Element.Data, preferredForm: DataFormOf(editedNode));
            MetaData.Load(editedNode.Element.MetaData, preferredForm: MetaDataFormOf(editedNode));
        }
    }

    private void NotifySerializerCommands()
    {
        SerializerMoveUpCommand.NotifyCanExecuteChanged();
        SerializerMoveDownCommand.NotifyCanExecuteChanged();
        SerializerOpenContainerCommand.NotifyCanExecuteChanged();
    }
}
