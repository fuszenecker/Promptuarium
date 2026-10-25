using System;
using Avalonia.Controls;
using PStudio.ViewModels;

namespace PStudio.Views;

/// <summary>
/// An editor window. Before it closes, the view model asks about the unsaved changes.
/// </summary>
public partial class MainWindow : Window
{
    private MainWindowViewModel? viewModel;
    private bool closeConfirmed;

    /// <summary>
    /// Creates the window.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnDataContextChanged(EventArgs e)
    {
        if (viewModel is not null)
        {
            viewModel.CloseRequested -= Close;
        }

        viewModel = DataContext as MainWindowViewModel;

        if (viewModel is not null)
        {
            viewModel.CloseRequested += Close;
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
}
