using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace PStudio.Presentation;

/// <inheritdoc/>
public sealed class WindowContext : IWindowContext
{
    /// <inheritdoc/>
    public Window? MainWindow { get; set; }

    /// <inheritdoc/>
    public Window? ActiveWindow
    {
        get
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                return desktop.Windows.FirstOrDefault(window => window.IsActive) ?? MainWindow ?? desktop.MainWindow;
            }

            return MainWindow;
        }
    }
}
