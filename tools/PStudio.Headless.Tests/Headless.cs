using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using PStudio.Composition;
using PStudio.Presentation;
using PStudio.ViewModels;
using PStudio.Views;

namespace PStudio.Headless.Tests;

/// <summary>
/// The real application (theme and resources), rendered by Skia without a display.
/// </summary>
public static class HeadlessTestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
    }
}

/// <summary>
/// Runs a scenario on the UI thread of a shared headless session, with the production services and a shown editor window.
/// </summary>
internal static class Scenario
{
    private static readonly HeadlessUnitTestSession Session = HeadlessUnitTestSession.GetOrStartForAssembly(typeof(Scenario).Assembly);

    public static async Task Run(Func<Editor, Task> scenario)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));

        // The bridge through the thread pool keeps the next test from running inside the dispatcher
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = Session.Dispatch<bool>(
            async () =>
            {
                using var editor = new Editor();
                await scenario(editor);
                return true;
            },
            timeout.Token).ContinueWith(
                task =>
                {
                    if (task.Exception is { } error)
                    {
                        completion.TrySetException(error.InnerExceptions);
                    }
                    else if (task.IsCanceled)
                    {
                        completion.TrySetCanceled();
                    }
                    else
                    {
                        completion.TrySetResult();
                    }
                },
                TaskScheduler.Default);

        await completion.Task.WaitAsync(timeout.Token);
    }

    public static Task DisposeAsync() => Session.DisposeAsync().AsTask();
}

[TestClass]
public sealed class ScenarioLifecycle
{
    [AssemblyCleanup]
    public static Task Cleanup() => Scenario.DisposeAsync();
}

/// <summary>
/// An editor window with its own service container and temporary directory.
/// </summary>
internal sealed class Editor : IDisposable
{
    private readonly ServiceProvider services;

    public Editor()
    {
        var collection = new ServiceCollection();
        collection.AddPresentation();
        collection.AddServices();
        collection.AddViewModels();
        services = collection.BuildServiceProvider();

        Directory = Path.Combine(Path.GetTempPath(), "pstudio-headless-" + Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(Directory);

        ViewModel = services.GetRequiredService<MainWindowViewModel>();
        Window = new MainWindow { DataContext = ViewModel, Width = 1100, Height = 720 };
        services.GetRequiredService<IWindowContext>().MainWindow = Window;
        Window.Show();
        Window.Activate();
        Pump();
    }

    public string Directory { get; }

    public MainWindowViewModel ViewModel { get; }

    public MainWindow Window { get; }

    /// <summary>Runs the pending UI work (layout, bindings, rendering).</summary>
    public static void Pump()
    {
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }

    public IEnumerable<T> Find<T>()
        where T : Visual
    {
        return Window.GetVisualDescendants().OfType<T>();
    }

    /// <summary>Saves a picture of the window, if PSTUDIO_SCREENSHOTS names a directory.</summary>
    public void Screenshot(string name)
    {
        if (Environment.GetEnvironmentVariable("PSTUDIO_SCREENSHOTS") is { Length: > 0 } directory)
        {
            Pump();
            using var frame = Window.CaptureRenderedFrame();
            frame?.Save(Path.Combine(directory, name + ".png"), new Avalonia.Media.Imaging.PngBitmapEncoderOptions());
        }
    }

    public void Dispose()
    {
        // The windows stay open: the scenarios leave unsaved changes behind, and closing would ask about them
        services.Dispose();
        System.IO.Directory.Delete(Directory, recursive: true);
    }
}
