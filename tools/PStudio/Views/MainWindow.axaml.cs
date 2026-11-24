using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
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

            // The layout of the previous session; the panel may be hidden from the start
            RestoreLayout(viewModel.Layout);
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
        if (viewModel is not null)
        {
            StoreLayout(viewModel.Layout);
            viewModel.SaveSettings();
        }

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
    // F2 edits the selected row; in an edited row (its name, type, value and comment) Enter applies and Escape reverts it,
    // Shift+Enter breaks a line of the comment
    private void OnSerializerKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.F2 && e.Source is TreeViewItem { DataContext: SerializerNodeViewModel row })
        {
            BeginEdit(row);
            e.Handled = true;
            return;
        }

        if (e.Source is not Control { DataContext: SerializerNodeViewModel { IsEditing: true } node } control
            || (control as ComboBox ?? control.FindAncestorOfType<ComboBox>()) is { IsDropDownOpen: true })
        {
            return;
        }

        if (e.Key == Key.Escape)
        {
            node.CancelEdit();
            FocusRow(node);
            e.Handled = true;
        }
        else if (e.Key == Key.Enter && e.KeyModifiers == KeyModifiers.Shift && control is TextBox { Tag: "comment" } comment)
        {
            // A line break in the comment
            int start = Math.Min(comment.SelectionStart, comment.SelectionEnd);
            int end = Math.Max(comment.SelectionStart, comment.SelectionEnd);
            comment.Text = (comment.Text ?? string.Empty).Remove(start, end - start).Insert(start, "\n");
            comment.CaretIndex = start + 1;
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            if (node.CommitEdit())
            {
                FocusRow(node);
            }

            e.Handled = true;
        }
    }

    // Leaving an edited row (not into the list of its types) applies it
    private void OnSerializerLostFocus(object? sender, RoutedEventArgs e)
    {
        if (e.Source is not Control { DataContext: SerializerNodeViewModel { IsEditing: true } node })
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            if (!node.IsEditing || IsWithinRow(node, FocusManager?.GetFocusedElement() as Visual) || RowControls(node).OfType<ComboBox>().Any(box => box.IsDropDownOpen))
            {
                return;
            }

            node.CommitEdit();
        }, DispatcherPriority.Background);
    }

    // A double click on a row edits it (not on the arrow that expands it)
    private void OnSerializerDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (e.Source is Visual visual && visual.FindAncestorOfType<ToggleButton>(includeSelf: true) is null
            && (visual as Control ?? visual.FindAncestorOfType<Control>())?.DataContext is SerializerNodeViewModel node)
        {
            BeginEdit(node);
            e.Handled = true;
        }
    }

    private void BeginEdit(SerializerNodeViewModel node)
    {
        node.IsEditing = true;
        OnSerializerEditRequested(node);
    }

    // The keyboard goes back to the row after its editing
    private void FocusRow(SerializerNodeViewModel node)
    {
        if (SerializerTree.TreeContainerFromItem(node) is TreeViewItem item)
        {
            item.Focus(NavigationMethod.Directional);
        }
    }

    // The controls of the row of a value (not of the rows of its members, which are in its item too)
    private IEnumerable<Control> RowControls(SerializerNodeViewModel node)
    {
        return SerializerTree.TreeContainerFromItem(node) is TreeViewItem item
            ? item.GetVisualDescendants().OfType<Control>().Where(control => control.DataContext == node)
            : [];
    }

    private static bool IsWithinRow(SerializerNodeViewModel node, Visual? focused)
    {
        return focused is Control { DataContext: SerializerNodeViewModel focusedNode } && focusedNode == node && focused is not TreeViewItem;
    }

    // A control of a row (a name, a type, a value) selects its value, as a click on the row does
    private void OnSerializerGotFocus(object? sender, FocusChangedEventArgs e)
    {
        if (e.Source is Control { DataContext: SerializerNodeViewModel node } && viewModel is not null && viewModel.SelectedSerializerNode != node)
        {
            viewModel.SelectedSerializerNode = node;
        }
    }

    // An edited row gets the focus in its name (an element in its value)
    private void OnSerializerEditRequested(SerializerNodeViewModel node)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (SerializerTree.TreeContainerFromItem(node) is not TreeViewItem item)
            {
                return;
            }

            // The name, otherwise the value, otherwise the type, otherwise the comment (of the root)
            Control? first = RowControls(node).Where(control => control.IsEffectivelyVisible && control is TextBox or ComboBox or CheckBox)
                .OrderBy(control => control is TextBox { Tag: "name" } ? 0 : control is TextBox { Tag: "value" } or CheckBox ? 1 : control is ComboBox ? 2 : 3)
                .FirstOrDefault();

            if (first is not null)
            {
                first.Focus(NavigationMethod.Tab);
                (first as TextBox)?.SelectAll();
            }
        }, DispatcherPriority.Loaded);
    }

    // The size and the place of the window, and where its splitters were, as the window was closed last (doubles, the
    // FLOAT64 of version 1 of the serializer format, so the settings stay a PRMT:CFG1-LE file)
    private void RestoreLayout(WindowLayout layout)
    {
        if (layout.Width > 0 && layout.Height > 0)
        {
            Width = Math.Max(MinWidth, layout.Width);
            Height = Math.Max(MinHeight, layout.Height);
        }

        if (layout.HasPosition)
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Position = new PixelPoint((int)layout.X, (int)layout.Y);
        }

        if (layout.IsMaximized)
        {
            WindowState = WindowState.Maximized;
        }

        if (layout.TreeWidth > 0 && layout.SideWidth > 0)
        {
            UpperGrid.ColumnDefinitions[0].Width = new GridLength(layout.TreeWidth, GridUnitType.Star);
            UpperGrid.ColumnDefinitions[2].Width = new GridLength(layout.SideWidth, GridUnitType.Star);
        }

        if (layout.ChunkPanelHeight > 0)
        {
            chunkPanelHeight = new GridLength(layout.ChunkPanelHeight);
            MainGrid.RowDefinitions[2].Height = chunkPanelHeight;
        }

        if (layout.MetaDataHeight > 0 && layout.DataHeight > 0)
        {
            EditorGrid.RowDefinitions[1].Height = new GridLength(layout.MetaDataHeight, GridUnitType.Star);
            EditorGrid.RowDefinitions[3].Height = new GridLength(layout.DataHeight, GridUnitType.Star);
        }
    }

    private void StoreLayout(WindowLayout layout)
    {
        layout.IsMaximized = WindowState == WindowState.Maximized;

        // A maximized window keeps the size and the place it had before
        if (WindowState == WindowState.Normal)
        {
            layout.Width = Math.Round(Width);
            layout.Height = Math.Round(Height);
            layout.X = Position.X;
            layout.Y = Position.Y;
            layout.HasPosition = true;
        }

        // The widths and the heights as proportions (stars), so they fit a window of another size too
        if (UpperGrid.ColumnDefinitions[0].ActualWidth > 0 && UpperGrid.ColumnDefinitions[2].ActualWidth > 0)
        {
            layout.TreeWidth = Math.Round(UpperGrid.ColumnDefinitions[0].ActualWidth);
            layout.SideWidth = Math.Round(UpperGrid.ColumnDefinitions[2].ActualWidth);
        }

        RowDefinition chunks = MainGrid.RowDefinitions[2];
        layout.ChunkPanelHeight = Math.Round(chunks.ActualHeight > 0 ? chunks.ActualHeight : chunkPanelHeight.Value);

        if (EditorGrid.RowDefinitions[1].ActualHeight > 0 && EditorGrid.RowDefinitions[3].ActualHeight > 0)
        {
            layout.MetaDataHeight = Math.Round(EditorGrid.RowDefinitions[1].ActualHeight);
            layout.DataHeight = Math.Round(EditorGrid.RowDefinitions[3].ActualHeight);
        }
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
