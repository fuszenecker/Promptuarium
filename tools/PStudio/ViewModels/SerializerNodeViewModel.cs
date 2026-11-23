using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using PStudio.Services;
using Promptuarium;
using Promptuarium.Tools;
using static PStudio.Services.SerializerEditing;

namespace PStudio.ViewModels;

/// <summary>
/// A value of the serializer view: the root (an implicit OBJECT), a member (a NAME holder) or an element (an ITEM
/// holder) of a tree of SERIALIZER_FORMAT.md. It shows its holder in the raw tree, and every edit is made there by the
/// editor, which then refreshes the view; so the two views can not differ.
/// </summary>
public sealed partial class SerializerNodeViewModel : ViewModelBase
{
    private readonly MainWindowViewModel editor;

    // Set while the properties are refreshed from the holder, so that the bound controls do not edit it
    private bool refreshing;

    [ObservableProperty]
    private bool isExpanded;

    [ObservableProperty]
    private string nameText = string.Empty;

    // The row is shown as texts, and edited (the name, the type, the value) after a double click or F2
    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(IsNameShown), nameof(IsNameEdited), nameof(IsTypeShown), nameof(IsTypeEdited),
        nameof(IsValueShown), nameof(IsBoolEdited), nameof(IsTextValueEdited), nameof(IsCommentShown))]
    private bool isEditing;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasValueError), nameof(ValueTip))]
    private string? valueError;

    [ObservableProperty]
    private string valueText = string.Empty;

    [ObservableProperty]
    private string commentText = string.Empty;

    /// <summary>
    /// Creates the view model of a holder (or of the root) and of the values in it.
    /// </summary>
    internal SerializerNodeViewModel(MainWindowViewModel editor, NodeViewModel holder, SerializerNodeViewModel? parent)
    {
        this.editor = editor;
        Holder = holder;
        Parent = parent;
        isExpanded = parent is null;
        Refresh();
    }

    /// <summary>The type options of the type selector.</summary>
    public static IReadOnlyList<TypeOption> TypeOptions => Types;

    /// <summary>The holder (or the root) in the raw tree.</summary>
    public NodeViewModel Holder { get; }

    /// <summary>The object or array that holds this value; null for the root.</summary>
    public SerializerNodeViewModel? Parent { get; }

    /// <summary>The members of an object (of the root), the elements of an array.</summary>
    public ObservableCollection<SerializerNodeViewModel> Children { get; } = [];

    /// <summary>Whether this is the root.</summary>
    public bool IsRoot => Parent is null;

    /// <summary>Whether this is a member of an object (it has a name).</summary>
    public bool IsMember => !IsRoot && Tag(Holder.Element) == NameTag;

    /// <summary>Whether the name is shown as a text (the row is not edited).</summary>
    public bool IsNameShown => IsMember && !IsEditing;

    /// <summary>Whether the comment is shown as a text (edited with the row, in a box above it).</summary>
    public bool IsCommentShown => HasComment && !IsEditing;

    /// <summary>Whether the name is edited.</summary>
    public bool IsNameEdited => IsMember && IsEditing;

    /// <summary>Whether the type is shown as a text.</summary>
    public bool IsTypeShown => HasType && !IsEditing;

    /// <summary>Whether the type is chosen.</summary>
    public bool IsTypeEdited => HasType && IsEditing;

    /// <summary>Whether the value is shown as a text (a BOOLEAN as true or false).</summary>
    public bool IsValueShown => (IsTextValue || IsBool) && !IsEditing;

    /// <summary>Whether a BOOLEAN is edited (a check box).</summary>
    public bool IsBoolEdited => IsBool && IsEditing;

    /// <summary>Whether the value is edited as a text.</summary>
    public bool IsTextValueEdited => IsTextValue && IsEditing;

    /// <summary>The value as a text, when the row is not edited.</summary>
    public string ValueDisplay => IsBool ? (BoolValue ? "true" : "false") : ValueText;

    /// <summary>Whether this is not a member: the root, or an element of an array (it has a label instead of a name).</summary>
    public bool HasLabel => !IsMember;

    /// <summary>The type code (OBJECT for the root).</summary>
    public byte Type { get; private set; } = ObjectType;

    /// <summary>The type, for the type selector; choosing another one converts the value.</summary>
    public TypeOption SelectedType
    {
        get => TypeOf(Type);
        set
        {
            if (!refreshing && value is not null && value.Code != Type)
            {
                editor.SerializerChangeType(this, value.Code);

                // The type stays the old one if it was not changed
                OnPropertyChanged();
            }
        }
    }

    /// <summary>Whether the type can be chosen (not for the root).</summary>
    public bool HasType => !IsRoot;

    /// <summary>Whether this is an object (or the root) or an array, which has members or elements.</summary>
    public bool IsStructure => Type is ObjectType or ArrayType;

    /// <summary>The label of the root or of an element: "root", "[2]".</summary>
    public string Label => IsRoot ? "root" : $"[{Parent!.Children.IndexOf(this)}]";

    /// <summary>The value of a BOOLEAN.</summary>
    public bool BoolValue
    {
        get => Type == BooleanType && Value is { Length: 1 } value && value.Span[0] == 1;
        set
        {
            if (!refreshing && Type == BooleanType && value != BoolValue)
            {
                editor.SerializerSetValue(this, [(byte)(value ? 1 : 0)]);
            }
        }
    }

    /// <summary>Whether the value is a BOOLEAN (a check box).</summary>
    public bool IsBool => Type == BooleanType;

    /// <summary>Whether the value is edited as a text (every value but NULL, OBJECT, ARRAY, BOOLEAN; a CONTAINER and a long BLOB are not).</summary>
    public bool IsTextValue { get; private set; }

    /// <summary>A short description of a value that is not edited here: the members, the elements, null, a nested tree.</summary>
    public string Summary { get; private set; } = string.Empty;

    /// <summary>Whether there is a summary.</summary>
    public bool HasSummary => Summary.Length > 0;

    /// <summary>Whether the edited value is not valid in its type.</summary>
    public bool HasValueError => ValueError is not null;

    /// <summary>The tooltip of the value: the error, or how the value is written.</summary>
    public string ValueTip => ValueError ?? $"{TypeOf(Type).Name}{(SerializerValues.Example(Type) is { Length: > 0 } example ? $": {example}" : string.Empty)}. Enter applies, Escape reverts.";

    /// <summary>The indentation of a level of the serializer view, in pixels (TreeViewItemIndent of its tree).</summary>
    public const double Indent = 20;

    /// <summary>The depth of the value: 0 for the root, 1 for its members.</summary>
    public int Depth => Parent is null ? 0 : Parent.Depth + 1;

    /// <summary>
    /// The width of the name (or of the label): the one of the members of the root, narrower by the indentation of a
    /// deeper value, so that the names end, and the types begin, in one column.
    /// </summary>
    public GridLength NameWidth => new(Math.Max(Indent, editor.SerializerNameWidth - ((Depth - 1) * Indent)));

    /// <summary>The comments of the value, in one line, for the tree.</summary>
    public string CommentPreview => string.Join("  ·  ", Comments.Select(comment => comment.ReplaceLineEndings(" ")));

    /// <summary>Whether the value has a comment.</summary>
    public bool HasComment => Comments.Count > 0;

    /// <summary>Whether this is a CONTAINER, whose value is a nested tree.</summary>
    public bool IsContainer => Type == ContainerType;

    /// <summary>The VALUE node, if the holder has one.</summary>
    public NodeViewModel? ValueNode => IsScalar(Type) && !IsRoot ? Holder.Children.Skip(1).FirstOrDefault(child => Tag(child.Element) == ValueTag) : null;

    /// <summary>The comments of the holder.</summary>
    public IReadOnlyList<string> Comments { get; private set; } = [];

    private ReadOnlyMemory<byte>? Value => ValueNode?.Element.Data;

    /// <summary>
    /// Shows the holder again after a change of the tree; the view models of the values that are still there are kept
    /// (with their expansion).
    /// </summary>
    internal void Refresh()
    {
        refreshing = true;
        Element element = Holder.Element;
        Type = IsRoot ? ObjectType : element.Children[0].Data.Span[0];
        NameText = IsMember ? Encoding.UTF8.GetString(element.Data.Span) : string.Empty;
        Comments = Holder.Children.Where(child => Tag(child.Element) == CommentTag).Select(child => Encoding.UTF8.GetString(child.Element.Data.Span)).ToList();
        CommentText = string.Join("\n", Comments);
        ValueError = null;

        ReadOnlyMemory<byte> value = Value ?? ReadOnlyMemory<byte>.Empty;
        IsTextValue = IsScalar(Type) && Type is not (BooleanType or ContainerType) && !(Type == BlobType && value.Length > MaxInlineBlobLength);
        ValueText = IsTextValue ? SerializerEditing.Format(Type, value.Span) : string.Empty;
        Summary = Type switch
        {
            _ when IsRoot => Signature(element) ?? string.Empty,
            NullType => "null",
            ContainerType => $"nested tree: {SerializerValues.ContainerSummary(value.Span)}",
            BlobType when !IsTextValue => $"{value.Length:N0} bytes, edit them in the Data editor",
            _ => string.Empty,
        };

        RefreshChildren();
        refreshing = false;

        OnPropertyChanged(string.Empty);
    }

    /// <summary>Applies the edited name.</summary>
    internal void CommitName()
    {
        if (IsMember && NameText != Encoding.UTF8.GetString(Holder.Element.Data.Span))
        {
            editor.SerializerRename(this, NameText);
        }
    }

    /// <summary>Applies the edited value; false (and the error is shown) if it is not a value of the type.</summary>
    internal bool CommitValue()
    {
        if (!IsTextValue || ValueText == SerializerEditing.Format(Type, (Value ?? ReadOnlyMemory<byte>.Empty).Span))
        {
            ValueError = null;
            return true;
        }

        try
        {
            byte[] bytes = Parse(Type, ValueText);
            ValueError = null;
            editor.SerializerSetValue(this, bytes);
            return true;
        }
        catch (FormatException exception)
        {
            ValueError = exception.Message;
            editor.SerializerValueRejected(this, exception.Message);
            return false;
        }
    }

    /// <summary>Applies the edited name and value, and ends the editing of the row; false (it stays edited) if the value is not valid.</summary>
    internal bool CommitEdit()
    {
        if (!IsEditing)
        {
            return true;
        }

        // Applying one refreshes the row from the tree, so the edited name and comment are kept while the others are applied
        string name = NameText;
        string comment = CommentText;

        if (!CommitValue())
        {
            return false;
        }

        NameText = name;
        CommitName();
        CommentText = comment;
        CommitComment();
        IsEditing = false;
        return true;
    }

    /// <summary>Drops the edits of the row, and ends its editing.</summary>
    internal void CancelEdit()
    {
        IsEditing = false;
        Refresh();
    }

    /// <summary>Applies the edited comment.</summary>
    internal void CommitComment()
    {
        if (CommentText.ReplaceLineEndings("\n") != string.Join("\n", Comments))
        {
            editor.SerializerSetComment(this, CommentText.ReplaceLineEndings("\n"));
        }
    }

    /// <summary>Tells the view that the padding of the alignment changed.</summary>
    internal void NotifyAlignment() => OnPropertyChanged(nameof(NameWidth));

    /// <summary>Drops the edits of the name, the value and the comment.</summary>
    internal void Revert() => Refresh();

    /// <summary>The members or the elements, with this one, in pre-order.</summary>
    internal IEnumerable<SerializerNodeViewModel> SelfAndDescendants()
    {
        yield return this;

        foreach (SerializerNodeViewModel descendant in Children.SelectMany(child => child.SelfAndDescendants()))
        {
            yield return descendant;
        }
    }

    // The holders among the children (the members of an object, the elements of an array), in their order; the view
    // models of the holders that are still there are reused
    private void RefreshChildren()
    {
        byte childTag = Type == ArrayType ? ItemTag : NameTag;
        List<NodeViewModel> wanted = IsStructure ? Holder.Children.Where(child => Tag(child.Element) == childTag).ToList() : [];
        var existing = Children.ToDictionary(child => child.Holder);

        for (int i = 0; i < wanted.Count; i++)
        {
            if (existing.TryGetValue(wanted[i], out SerializerNodeViewModel? child))
            {
                int index = Children.IndexOf(child);

                if (index != i)
                {
                    Children.Move(index, i);
                }

                child.Refresh();
            }
            else
            {
                Children.Insert(i, new SerializerNodeViewModel(editor, wanted[i], this));
            }
        }

        while (Children.Count > wanted.Count)
        {
            Children.RemoveAt(Children.Count - 1);
        }
    }

    private static string? Signature(Element root)
    {
        return root.MetaData.Length is > 0 and <= ElementBuilder.MaxSignatureLength && System.Text.Ascii.IsValid(root.MetaData.Span)
            ? Encoding.ASCII.GetString(root.MetaData.Span)
            : null;
    }
}
