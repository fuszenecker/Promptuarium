using System;
using Avalonia;

namespace PStudio;

/// <summary>
/// The entry point of the editor.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Starts the editor; the optional argument is the file to open.
    /// </summary>
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    /// <summary>
    /// Configures the application (also used by the designer).
    /// </summary>
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace();
    }
}
