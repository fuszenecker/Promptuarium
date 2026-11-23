using System;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
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

        SerializerPanel.AddHandler(KeyDownEvent, OnSerializerKeyDown, RoutingStrategies.Tunnel);
        SerializerPanel.AddHandler(LostFocusEvent, OnSerializerLostFocus, RoutingStrategies.Bubble, handledEventsToo: true);
        SerializerTree.AddHandler(GotFocusEvent, OnSerializerGotFocus, RoutingStrategies.Bubble, handledEventsToo: true);
        SerializerTree.AddHandler(DoubleTappedEvent, OnSerializerDoubleTapped, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    /// <inheritdoc/>
    protected override void OnDataContextChanged(EventArgs e)
    {
        if (viewModel is not null)
        {
            viewModel.CloseRequested -= Close;
            viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            viewModel.SerializerEditRequested -= OnSerializerEditRequested;
        }

        viewModel = DataContext as MainWindowViewModel;

        if (viewModel is not null)
        {
            viewModel.CloseRequested += Close;
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
            viewModel.SerializerEditRequested += OnSerializerEditRequested;

            // The panel may be hidden from the start (the setting of the previous session)
            ShowChunkPanel(viewModel.IsChunkPanelVisible);
        }

        base.OnDataContextChanged(e);
    }

    /// <inheritdoc/>
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        // The window fits the screen: at most 90% of its working area (without the panels of the desktop)
        if (Screens.ScreenFromWindow(this) is { } screen)
        {
            double width = screen.WorkingArea.Width / screen.Scaling * 0.9;
            double height = screen.WorkingArea.Height / screen.Scaling * 0.9;

            if (Width > width || Height > height)
            {
                Width = Math.Max(MinWidth, Math.Min(Width, width));
                Height = Math.Max(MinHeight, Math.Min(Height, height));
            }
        }
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

        if (e.PropertyName == nameof(MainWindowViewModel.IsChunkPanelVisible))
        {
            ShowChunkPanel(viewModel.IsChunkPanelVisible);
            return;
        }


        // NodePath is refreshed after every change of the tree too (a node moved, indented, added), not only after a new selection
        if (e.PropertyName is nameof(MainWindowViewModel.SelectedNode) or nameof(MainWindowViewModel.NodePath))
        {
            FocusSelectedNode();
        }
    }

    // The keyboard focus follows the selection of the tree view, so that the next key goes to the selected node
    // (and not to the menu, where the focus falls when the focused container of a moved node is removed).
    // It is never taken from an editor or from the chunk panel. On the tab of the serializer view, the focus follows
    // its selection there.
    private void FocusSelectedNode()
    {
        object? focusedElement = FocusManager?.GetFocusedElement();
        bool inSerializerTree = SerializerTree.IsKeyboardFocusWithin && focusedElement is TreeViewItem;

        if (!Tree.IsKeyboardFocusWithin && !inSerializerTree && focusedElement is Visual focused && MainGrid.IsVisualAncestorOf(focused))
        {
            return;
        }

        // The container of a moved or a new node is created by the next layout pass
        Dispatcher.UIThread.Post(() =>
        {
            if (viewModel is { SelectedViewIndex: MainWindowViewModel.SerializerView, SelectedSerializerNode: { } value })
            {
                if (SerializerTree.TreeContainerFromItem(value) is TreeViewItem serializerItem && !serializerItem.IsFocused)
                {
                    serializerItem.Focus(NavigationMethod.Directional);
                }
            }
            else if (viewModel?.SelectedNode is { } node && Tree.TreeContainerFromItem(node) is TreeViewItem item && !item.IsFocused)
            {
                item.Focus(NavigationMethod.Directional);
            }
        }, DispatcherPriority.Loaded);
    }

    // Enter applies the edited name or value (Ctrl+Enter the comment), Escape reverts the edits of the value
    // F2 on a member of the tree renames it
    private void OnSerializerKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.F2 && e.Source is TreeViewItem { DataContext: SerializerNodeViewModel { IsMember: true } member })
        {
            EditName(member);
            e.Handled = true;
            return;
        }

        if (e.Source is not TextBox { Tag: string field } box || box.DataContext is not SerializerNodeViewModel node)
        {
            return;
        }

        if (e.Key == Key.Escape)
        {
            bool wasEditingName = field == "name";
            node.Revert();
            EndNameEdit(node, wasEditingName);
            e.Handled = true;
        }
        else if (e.Key == Key.Enter && (field != "comment" || e.KeyModifiers.HasFlag(KeyModifiers.Control)))
        {
            Commit(node, field);
            EndNameEdit(node, field == "name");
            e.Handled = true;
        }
    }

    private void OnSerializerLostFocus(object? sender, RoutedEventArgs e)
    {
        if (e.Source is TextBox { Tag: string field } box && box.DataContext is SerializerNodeViewModel node)
        {
            Commit(node, field);

            if (field == "name")
            {
                node.IsEditingName = false;
            }
        }
    }

    // A double click on a name renames the member
    private void OnSerializerDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (e.Source is Visual visual && (visual as TextBlock ?? visual.FindAncestorOfType<TextBlock>()) is { Tag: "name", DataContext: SerializerNodeViewModel node })
        {
            EditName(node);
            e.Handled = true;
        }
    }

    private void EditName(SerializerNodeViewModel node)
    {
        node.IsEditingName = true;
        OnSerializerEditRequested(node);
    }

    // After Enter or Escape in a name: the name is a text again, and the keyboard goes back to the row
    private void EndNameEdit(SerializerNodeViewModel node, bool name)
    {
        if (!name || !node.IsEditingName)
        {
            return;
        }

        node.IsEditingName = false;

        if (SerializerTree.TreeContainerFromItem(node) is TreeViewItem item)
        {
            item.Focus(NavigationMethod.Directional);
        }
    }

    private static void Commit(SerializerNodeViewModel node, string field)
    {
        switch (field)
        {
            case "name":
                node.CommitName();
                break;
            case "value":
                node.CommitValue();
                break;
            case "comment":
                node.CommitComment();
                break;
        }
    }

    // A control of a row (a name, a type, a value) selects its value, as a click on the row does
    private void OnSerializerGotFocus(object? sender, FocusChangedEventArgs e)
    {
        if (e.Source is Control { DataContext: SerializerNodeViewModel node } && viewModel is not null && viewModel.SelectedSerializerNode != node)
        {
            viewModel.SelectedSerializerNode = node;
        }
    }

    // A new member gets the focus in its name, a new element in its value
    private void OnSerializerEditRequested(SerializerNodeViewModel node)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (SerializerTree.TreeContainerFromItem(node) is not TreeViewItem item)
            {
                return;
            }

            string field = node.IsMember ? "name" : "value";
            TextBox? box = item.GetVisualDescendants().OfType<TextBox>().FirstOrDefault(text => Equals(text.Tag, field) && text.DataContext == node && text.IsVisible);

            if (box is not null)
            {
                box.Focus(NavigationMethod.Tab);
                box.SelectAll();
            }
        }, DispatcherPriority.Loaded);
    }

    // The hidden chunk panel gives its row back to the editor (and gets its height back when it is shown again)
    private void ShowChunkPanel(bool visible)
    {
        RowDefinition row = MainGrid.RowDefinitions[2];

        if (visible)
        {
            row.Height = chunkPanelHeight;
        }
        else if (row.Height.Value > 0)
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
