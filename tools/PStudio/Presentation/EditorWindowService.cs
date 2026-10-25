using System;
using PStudio.ViewModels;
using PStudio.Views;

namespace PStudio.Presentation;

/// <inheritdoc/>
public sealed class EditorWindowService : IEditorWindowService
{
    private readonly IWindowContext windowContext;
    private readonly IViewModelFactory viewModelFactory;

    /// <summary>
    /// Creates the service.
    /// </summary>
    public EditorWindowService(IWindowContext windowContext, IViewModelFactory viewModelFactory)
    {
        this.windowContext = windowContext ?? throw new ArgumentNullException(nameof(windowContext));
        this.viewModelFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));
    }

    /// <inheritdoc/>
    public void OpenNested(MainWindowViewModel host, NodeViewModel node)
    {
        MainWindowViewModel viewModel = viewModelFactory.CreateEditor();
        viewModel.OpenNested(host, node);

        var window = new MainWindow { DataContext = viewModel };

        if (windowContext.ActiveWindow is { } owner)
        {
            window.Show(owner);
        }
        else
        {
            window.Show();
        }
    }
}
