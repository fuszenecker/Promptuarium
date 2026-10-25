using System;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using PStudio.Models;
using PStudio.ViewModels;

namespace PStudio.Views;

/// <summary>
/// An editor window. Before it closes, the view model asks about the unsaved changes.
/// The nodes of the tree view can be dragged before, after or into another node.
/// </summary>
public partial class MainWindow : Window
{
    // The pointer has to move this far before a press becomes a drag
    private const double DragThreshold = 6;

    private static readonly string[] DropClasses = ["dropBefore", "dropAfter", "dropInside"];

    private MainWindowViewModel? viewModel;
    private bool closeConfirmed;
    private GridLength chunkPanelHeight = new(220);

    private NodeViewModel? dragged;
    private Point dragStart;
    private bool dragging;
    private Border? dropHeader;
    private NodeViewModel? dropTarget;
    private DropPosition dropPosition;

    /// <summary>
    /// Creates the window.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        Tree.AddHandler(PointerPressedEvent, OnTreePointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        Tree.AddHandler(PointerMovedEvent, OnTreePointerMoved, RoutingStrategies.Tunnel, handledEventsToo: true);
        Tree.AddHandler(PointerReleasedEvent, OnTreePointerReleased, RoutingStrategies.Tunnel, handledEventsToo: true);
        Tree.AddHandler(PointerCaptureLostEvent, (_, _) => EndDrag(), RoutingStrategies.Bubble, handledEventsToo: true);
    }

    /// <inheritdoc/>
    protected override void OnDataContextChanged(EventArgs e)
    {
        if (viewModel is not null)
        {
            viewModel.CloseRequested -= Close;
            viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        viewModel = DataContext as MainWindowViewModel;

        if (viewModel is not null)
        {
            viewModel.CloseRequested += Close;
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        base.OnDataContextChanged(e);
    }

    /// <inheritdoc/>
    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);

        if (closeConfirmed || viewModel is null || !viewModel.HasUnsavedChanges)
        {
            return;
        }

        e.Cancel = true;

        if (await viewModel.ConfirmCloseAsync())
        {
            closeConfirmed = true;
            Close();
        }
    }

    /// <inheritdoc/>
    protected override void OnClosed(EventArgs e)
    {
        viewModel?.Closed();
        base.OnClosed(e);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (viewModel is null)
        {
            return;
        }

        // The first chunk of the selected node is scrolled into view
        if (e.PropertyName == nameof(MainWindowViewModel.SelectedChunk) && viewModel.SelectedChunk is { } chunk)
        {
            ChunkGrid.ScrollIntoView(chunk, null);
            return;
        }

        // The hidden chunk panel gives its row back to the editor (and gets its height back when it is shown again)
        if (e.PropertyName != nameof(MainWindowViewModel.IsChunkPanelVisible))
        {
            return;
        }

        RowDefinition row = MainGrid.RowDefinitions[2];

        if (viewModel.IsChunkPanelVisible)
        {
            row.Height = chunkPanelHeight;
        }
        else
        {
            chunkPanelHeight = row.Height;
            row.Height = new GridLength(0);
        }
    }

    private void OnTreePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        EndDrag();

        if (e.GetCurrentPoint(Tree).Properties.IsLeftButtonPressed && ItemAt(e.Source) is { DataContext: NodeViewModel { IsRoot: false } node })
        {
            dragged = node;
            dragStart = e.GetPosition(Tree);
        }
    }

    private void OnTreePointerMoved(object? sender, PointerEventArgs e)
    {
        if (dragged is null)
        {
            return;
        }

        Point position = e.GetPosition(Tree);

        if (!dragging)
        {
            if (Math.Abs(position.X - dragStart.X) < DragThreshold && Math.Abs(position.Y - dragStart.Y) < DragThreshold)
            {
                return;
            }

            dragging = true;
            e.Pointer.Capture(Tree);
        }

        ShowDropTarget(position);
        e.Handled = true;
    }

    private void OnTreePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (dragging && dragged is not null && dropTarget is not null && viewModel is not null)
        {
            NodeViewModel node = dragged;
            NodeViewModel target = dropTarget;
            DropPosition position = dropPosition;

            EndDrag();
            e.Pointer.Capture(null);
            viewModel.MoveNode(node, target, position);
            Tree.SelectedItem = node;
            e.Handled = true;
            return;
        }

        bool wasDragging = dragging;
        EndDrag();

        if (wasDragging)
        {
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    // Finds the node under the pointer, and where the dragged node would go: the upper quarter of the row is before,
    // the lower quarter is after, the middle is into the node
    private void ShowDropTarget(Point position)
    {
        ClearDropTarget();

        if (dragged is null || Tree.InputHitTest(position) is not Visual hit || ItemAt(hit) is not { DataContext: NodeViewModel target } item)
        {
            return;
        }

        Border? header = item.GetVisualDescendants().OfType<Border>().FirstOrDefault(border => border.Classes.Contains("nodeHeader"));

        if (header is null)
        {
            return;
        }

        double y = Tree.TranslatePoint(position, header)?.Y ?? 0;
        double height = Math.Max(1, header.Bounds.Height);
        DropPosition where = target.IsRoot ? DropPosition.Inside
            : y < height * 0.25 ? DropPosition.Before
            : y > height * 0.75 ? DropPosition.After
            : DropPosition.Inside;

        if (!MainWindowViewModel.CanMoveNode(dragged, target, where))
        {
            return;
        }

        dropTarget = target;
        dropPosition = where;
        dropHeader = header;
        header.Classes.Add(where switch
        {
            DropPosition.Before => "dropBefore",
            DropPosition.After => "dropAfter",
            _ => "dropInside",
        });
    }

    private void ClearDropTarget()
    {
        dropHeader?.Classes.RemoveAll(DropClasses);
        dropHeader = null;
        dropTarget = null;
    }

    private void EndDrag()
    {
        ClearDropTarget();
        dragged = null;
        dragging = false;
    }

    private static TreeViewItem? ItemAt(object? source)
    {
        return source is Visual visual
            ? visual as TreeViewItem ?? visual.FindAncestorOfType<TreeViewItem>()
            : null;
    }
}
