using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using PStudio.Services;
using Promptuarium;

namespace PStudio.ViewModels;

/// <summary>
/// A node of the tree view. It wraps an element, and every change of the children changes the element too.
/// </summary>
public sealed partial class NodeViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool isExpanded;

    /// <summary>
    /// Creates the view models of an element and of its descendants.
    /// </summary>
    public NodeViewModel(Element element, NodeViewModel? parent = null)
    {
        ArgumentNullException.ThrowIfNull(element);

        Element = element;
        Parent = parent;
        Children = new ObservableCollection<NodeViewModel>(element.Children.Select(child => new NodeViewModel(child, this)));
        isExpanded = parent is null;
    }

    /// <summary>The element.</summary>
    public Element Element { get; }

    /// <summary>The parent, or null for the root (and for a removed node).</summary>
    public NodeViewModel? Parent { get; private set; }

    /// <summary>The children.</summary>
    public ObservableCollection<NodeViewModel> Children { get; }

    /// <summary>Whether this is the root of the tree.</summary>
    public bool IsRoot => Parent is null;

    /// <summary>Index among the siblings (0 for the root).</summary>
    public int Index => Parent?.Children.IndexOf(this) ?? 0;

    /// <summary>The path from the root, as pedit writes it: "/" for the root, "/0/2" for the third child of the first child.</summary>
    public string Path => Parent is null ? "/" : Parent.Parent is null ? $"/{Index}" : $"{Parent.Path}/{Index}";

    /// <summary>The text of the node in the tree view.</summary>
    public string Header
    {
        get
        {
            string name = Parent is null ? "root" : $"#{Index}";

            if (Element.Data.IsEmpty && Element.MetaData.IsEmpty)
            {
                return $"{name}  (empty)";
            }

            string data = Element.Data.IsEmpty ? string.Empty : $"  {ContentFormat.Preview(Element.Data.Span)}";
            string metaData = Element.MetaData.IsEmpty ? string.Empty : $"  [{ContentFormat.Preview(Element.MetaData.Span)}]";
            return name + data + metaData;
        }
    }

    /// <summary>Sets the data.</summary>
    public void SetData(ReadOnlyMemory<byte> data)
    {
        Element.Data = data;
        OnPropertyChanged(nameof(Header));
    }

    /// <summary>Sets the metadata.</summary>
    public void SetMetaData(ReadOnlyMemory<byte> metaData)
    {
        Element.MetaData = metaData;
        OnPropertyChanged(nameof(Header));
    }

    /// <summary>Inserts a node (without a parent) as a child.</summary>
    public void Insert(int index, NodeViewModel child)
    {
        ArgumentNullException.ThrowIfNull(child);

        if (child.Parent is not null)
        {
            throw new InvalidOperationException("The node already has a parent.");
        }

        Element.Insert(index, child.Element);
        child.Parent = this;
        Children.Insert(index, child);
        IsExpanded = true;
        RefreshFrom(index);
    }

    /// <summary>Removes a child, keeping its descendants (so it can be inserted elsewhere).</summary>
    public NodeViewModel Detach(NodeViewModel child)
    {
        ArgumentNullException.ThrowIfNull(child);

        int index = Children.IndexOf(child);

        if (index < 0)
        {
            throw new InvalidOperationException("The node is not a child of this node.");
        }

        Element.Detach(child.Element);
        Children.RemoveAt(index);
        child.Parent = null;
        RefreshFrom(index);
        return child;
    }

    /// <summary>Moves a child to another index.</summary>
    public void Move(int oldIndex, int newIndex)
    {
        NodeViewModel child = Children[oldIndex];

        Element.Detach(child.Element);
        Element.Insert(newIndex, child.Element);
        Children.Move(oldIndex, newIndex);
        RefreshFrom(Math.Min(oldIndex, newIndex));
    }

    // The headers show the indexes, which change after the changed position
    private void RefreshFrom(int index)
    {
        for (int i = index; i < Children.Count; i++)
        {
            Children[i].OnPropertyChanged(nameof(Header));
        }
    }
}
