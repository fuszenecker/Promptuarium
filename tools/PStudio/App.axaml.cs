using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using PStudio.Composition;
using PStudio.Presentation;
using PStudio.ViewModels;
using PStudio.Views;

namespace PStudio;

/// <summary>
/// The application; the dependency injection container is built here.
/// </summary>
public partial class App : Application
{
    private ServiceProvider? serviceProvider;

    /// <inheritdoc/>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <inheritdoc/>
    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        services.AddPresentation();
        services.AddServices();
        services.AddViewModels();
        serviceProvider = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var viewModel = serviceProvider.GetRequiredService<MainWindowViewModel>();
            var window = new MainWindow { DataContext = viewModel };

            serviceProvider.GetRequiredService<IWindowContext>().MainWindow = window;
            desktop.MainWindow = window;
            desktop.Exit += (_, _) => serviceProvider.Dispose();

            // The optional argument is the file to open
            if (desktop.Args is [string path, ..])
            {
                window.Opened += async (_, _) => await viewModel.OpenFileAsync(path);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}
