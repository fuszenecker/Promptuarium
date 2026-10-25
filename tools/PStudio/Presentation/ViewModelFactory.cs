using System;
using Microsoft.Extensions.DependencyInjection;
using PStudio.ViewModels;

namespace PStudio.Presentation;

/// <inheritdoc/>
public sealed class ViewModelFactory : IViewModelFactory
{
    private readonly IServiceProvider services;

    /// <summary>
    /// Creates the factory.
    /// </summary>
    public ViewModelFactory(IServiceProvider services)
    {
        this.services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <inheritdoc/>
    public MainWindowViewModel CreateEditor() => services.GetRequiredService<MainWindowViewModel>();

    /// <inheritdoc/>
    public ContentEditorViewModel CreateContentEditor(string title)
    {
        return ActivatorUtilities.CreateInstance<ContentEditorViewModel>(services, title);
    }
}
