using System;
using System.Collections.Generic;
using PStudio.Models;

namespace PStudio.ViewModels;

/// <summary>
/// The jumps of the selection by the keyboard in the raw tree and in the serializer view: Home, End, Page Up and Page
/// Down, over the rows that are shown (the ones whose ancestors are all expanded).
/// </summary>
public sealed partial class MainWindowViewModel
{
    /// <summary>The rows of the raw tree that are shown, from the top.</summary>
    public IReadOnlyList<NodeViewModel> ShownRawRows()
    {
        var rows = new List<NodeViewModel>();
        AddShown(Roots, rows, node => node.IsExpanded, node => node.Children);
        return rows;
    }

    /// <summary>The rows of the serializer view that are shown, from the top.</summary>
    public IReadOnlyList<SerializerNodeViewModel> ShownSerializerRows()
    {
        var rows = new List<SerializerNodeViewModel>();
        AddShown(SerializerRoots, rows, node => node.IsExpanded, node => node.Children);
        return rows;
    }

    /// <summary>Moves the selection of the raw tree; a page is the given number of rows.</summary>
    public void JumpRawRow(RowJump jump, int page)
    {
        if (Jump(ShownRawRows(), SelectedNode, jump, page) is { } node)
        {
            SelectedNode = node;
        }
    }

    /// <summary>Moves the selection of the serializer view; a page is the given number of rows.</summary>
    public void JumpSerializerRow(RowJump jump, int page)
    {
        if (Jump(ShownSerializerRows(), SelectedSerializerNode, jump, page) is { } node)
        {
            SelectedSerializerNode = node;
        }
    }

    /// <summary>The row the jump goes to from the current one (the first row if none is current); null if there are no rows.</summary>
    internal static T? Jump<T>(IReadOnlyList<T> rows, T? current, RowJump jump, int page)
        where T : class
    {
        if (rows.Count == 0)
        {
            return null;
        }

        int index = current is null ? -1 : IndexOf(rows, current);
        page = Math.Max(1, page);

        int target = jump switch
        {
            RowJump.First => 0,
            RowJump.Last => rows.Count - 1,
            RowJump.PageUp => index < 0 ? 0 : index - page,
            RowJump.PageDown => index < 0 ? 0 : index + page,
            _ => throw new ArgumentOutOfRangeException(nameof(jump)),
        };

        return rows[Math.Clamp(target, 0, rows.Count - 1)];
    }

    private static int IndexOf<T>(IReadOnlyList<T> rows, T item)
        where T : class
    {
        for (int i = 0; i < rows.Count; i++)
        {
            if (ReferenceEquals(rows[i], item))
            {
                return i;
            }
        }

        return -1;
    }

    private static void AddShown<T>(IEnumerable<T> nodes, List<T> rows, Func<T, bool> isExpanded, Func<T, IEnumerable<T>> children)
    {
        foreach (T node in nodes)
        {
            rows.Add(node);

            if (isExpanded(node))
            {
                AddShown(children(node), rows, isExpanded, children);
            }
        }
    }
}
