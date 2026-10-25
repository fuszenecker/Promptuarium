using Avalonia.Controls;

namespace PStudio.Presentation;

/// <summary>
/// The windows the dialogs belong to.
/// </summary>
public interface IWindowContext
{
    /// <summary>The main editor window.</summary>
    Window? MainWindow { get; set; }

    /// <summary>
    /// The active (focused) window, or the main window if no window is active.
    /// </summary>
    Window? ActiveWindow { get; }
}
