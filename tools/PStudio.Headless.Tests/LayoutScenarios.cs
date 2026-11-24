using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using PStudio.Models;
using PStudio.Views;

namespace PStudio.Headless.Tests;

/// <summary>The layout of the window: restored from the settings when it opens, stored when it closes.</summary>
[TestClass]
public sealed class LayoutScenarios
{
    [TestMethod]
    public Task Layout_IsRestored_WhenTheWindowOpens() => Scenario.Run(editor =>
    {
        WindowLayout layout = editor.ViewModel.Layout;
        layout.Width = 900;
        layout.Height = 600;
        layout.X = 40;
        layout.Y = 30;
        layout.HasPosition = true;
        layout.TreeWidth = 300;
        layout.SideWidth = 600;
        layout.ChunkPanelHeight = 150;
        layout.MetaDataHeight = 100;
        layout.DataHeight = 300;

        var window = new MainWindow { DataContext = editor.ViewModel };
        window.Show();
        Editor.Pump();

        Assert.AreEqual(900, window.Width);
        Assert.AreEqual(600, window.Height);
        Assert.AreEqual(new PixelPoint(40, 30), window.Position);
        Assert.AreEqual(WindowStartupLocation.Manual, window.WindowStartupLocation);

        Grid upper = window.FindControl<Grid>("UpperGrid")!;
        Assert.AreEqual(new GridLength(300, GridUnitType.Star), upper.ColumnDefinitions[0].Width);
        Assert.AreEqual(new GridLength(600, GridUnitType.Star), upper.ColumnDefinitions[2].Width);
        Assert.AreEqual(0.5, upper.ColumnDefinitions[0].ActualWidth / upper.ColumnDefinitions[2].ActualWidth, 0.02);

        Grid main = window.FindControl<Grid>("MainGrid")!;
        Assert.AreEqual(150, main.RowDefinitions[2].Height.Value);
        Assert.AreEqual(150, main.RowDefinitions[2].ActualHeight, 1);

        Grid editors = window.FindControl<Grid>("EditorGrid")!;
        Assert.AreEqual(new GridLength(100, GridUnitType.Star), editors.RowDefinitions[1].Height);
        Assert.AreEqual(new GridLength(300, GridUnitType.Star), editors.RowDefinitions[3].Height);
        return Task.CompletedTask;
    });

    [TestMethod]
    public Task UnknownLayout_LeavesTheDefaults() => Scenario.Run(editor =>
    {
        var window = new MainWindow { DataContext = editor.ViewModel };
        window.Show();
        Editor.Pump();

        Assert.AreEqual(1200, window.Width);
        Assert.AreEqual(680, window.Height);
        Grid upper = window.FindControl<Grid>("UpperGrid")!;
        Assert.AreEqual(new GridLength(5, GridUnitType.Star), upper.ColumnDefinitions[0].Width);
        Assert.AreEqual(new GridLength(4, GridUnitType.Star), upper.ColumnDefinitions[2].Width);
        Assert.AreEqual(220, window.FindControl<Grid>("MainGrid")!.RowDefinitions[2].Height.Value);
        return Task.CompletedTask;
    });

    [TestMethod]
    public Task TooSmallASize_IsTheSmallestOne() => Scenario.Run(editor =>
    {
        editor.ViewModel.Layout.Width = 100;
        editor.ViewModel.Layout.Height = 50;

        var window = new MainWindow { DataContext = editor.ViewModel };

        Assert.AreEqual(window.MinWidth, window.Width);
        Assert.AreEqual(window.MinHeight, window.Height);
        return Task.CompletedTask;
    });

    [TestMethod]
    public Task Layout_IsStored_WhenTheWindowCloses() => Scenario.Run(editor =>
    {
        var window = new MainWindow { DataContext = editor.ViewModel, Width = 1000, Height = 650 };
        window.Show();
        window.Position = new PixelPoint(12, 34);
        Grid upper = window.FindControl<Grid>("UpperGrid")!;
        upper.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
        upper.ColumnDefinitions[2].Width = new GridLength(3, GridUnitType.Star);
        window.FindControl<Grid>("MainGrid")!.RowDefinitions[2].Height = new GridLength(180);
        Editor.Pump();
        PixelPoint position = window.Position;

        window.Close();
        Editor.Pump();

        WindowLayout layout = editor.ViewModel.Layout;
        Assert.AreEqual(1000, layout.Width);
        Assert.AreEqual(650, layout.Height);
        Assert.AreEqual(position.X, layout.X, $"the window was at {position}, the layout has {layout.X}, {layout.Y}");
        Assert.AreEqual(position.Y, layout.Y);
        Assert.IsTrue(layout.HasPosition);
        Assert.IsFalse(layout.IsMaximized);
        Assert.AreEqual(3, layout.SideWidth / layout.TreeWidth, 0.05);
        Assert.AreEqual(180, layout.ChunkPanelHeight);

        // Whole pixels
        Assert.AreEqual(Math.Round(layout.TreeWidth), layout.TreeWidth);
        Assert.AreEqual(Math.Round(layout.SideWidth), layout.SideWidth);

        // And saved into the settings file
        var stored = new PStudio.Services.SettingsStore(Path.Combine(editor.Directory, "settings.prmt")).Settings.Layout;
        Assert.AreEqual(1000, stored.Width);
        Assert.AreEqual(180, stored.ChunkPanelHeight);
        return Task.CompletedTask;
    });

    [TestMethod]
    public Task HiddenChunkPanel_KeepsItsHeight() => Scenario.Run(editor =>
    {
        var window = new MainWindow { DataContext = editor.ViewModel };
        window.Show();
        window.FindControl<Grid>("MainGrid")!.RowDefinitions[2].Height = new GridLength(170);
        Editor.Pump();
        editor.ViewModel.IsChunkPanelVisible = false;
        Editor.Pump();
        Assert.AreEqual(0, window.FindControl<Grid>("MainGrid")!.RowDefinitions[2].Height.Value);

        window.Close();
        Editor.Pump();

        Assert.AreEqual(170, editor.ViewModel.Layout.ChunkPanelHeight);
        return Task.CompletedTask;
    });

    [TestMethod]
    public Task SplittersOfAHiddenRawView_AreKept() => Scenario.Run(editor =>
    {
        editor.ViewModel.Layout.MetaDataHeight = 120;
        editor.ViewModel.Layout.DataHeight = 240;
        var window = new MainWindow { DataContext = editor.ViewModel };
        window.Show();
        Editor.Pump();

        // A new tree is in the serializer view: the editors of the raw view are hidden
        Assert.IsFalse(window.FindControl<Grid>("EditorGrid")!.IsVisible);

        window.Close();
        Editor.Pump();

        Assert.AreEqual(120, editor.ViewModel.Layout.MetaDataHeight);
        Assert.AreEqual(240, editor.ViewModel.Layout.DataHeight);
        return Task.CompletedTask;
    });

    [TestMethod]
    public Task MaximizedWindow_KeepsItsFormerSize() => Scenario.Run(editor =>
    {
        editor.ViewModel.Layout.Width = 800;
        editor.ViewModel.Layout.Height = 500;
        var window = new MainWindow { DataContext = editor.ViewModel };
        window.Show();
        window.WindowState = WindowState.Maximized;
        Editor.Pump();

        window.Close();
        Editor.Pump();

        Assert.IsTrue(editor.ViewModel.Layout.IsMaximized);
        Assert.AreEqual(800, editor.ViewModel.Layout.Width);
        Assert.AreEqual(500, editor.ViewModel.Layout.Height);
        return Task.CompletedTask;
    });

    [TestMethod]
    public Task MaximizedLayout_OpensMaximized() => Scenario.Run(editor =>
    {
        editor.ViewModel.Layout.IsMaximized = true;

        var window = new MainWindow { DataContext = editor.ViewModel };

        Assert.AreEqual(WindowState.Maximized, window.WindowState);
        return Task.CompletedTask;
    });

    [TestMethod]
    public Task CancelledClosing_DoesNotStoreTheLayout_AConfirmedOneDoes() => Scenario.Run(editor =>
    {
        editor.Window.Width = 950;
        editor.ViewModel.AddChildCommand.Execute(null);
        Editor.Pump();

        editor.Window.Close();
        Editor.Pump();
        PressButton(editor.Window.OwnedWindows.OfType<MessageDialog>().Single(), "Cancel");
        Editor.Pump();

        Assert.IsTrue(editor.Window.IsVisible);
        Assert.AreEqual(0, editor.ViewModel.Layout.Width);

        editor.Window.Close();
        Editor.Pump();
        PressButton(editor.Window.OwnedWindows.OfType<MessageDialog>().Single(), "Don't save");
        Editor.Pump();

        Assert.IsFalse(editor.Window.IsVisible);
        Assert.AreEqual(950, editor.ViewModel.Layout.Width);
        Assert.AreEqual(950, new PStudio.Services.SettingsStore(Path.Combine(editor.Directory, "settings.prmt")).Settings.Layout.Width);
        return Task.CompletedTask;
    });

    [TestMethod]
    public Task RestoredLayout_IsStoredAgainUnchanged() => Scenario.Run(editor =>
    {
        WindowLayout layout = editor.ViewModel.Layout;
        layout.Width = 1000;
        layout.Height = 640;
        layout.TreeWidth = 400;
        layout.SideWidth = 400;
        layout.ChunkPanelHeight = 160;

        var window = new MainWindow { DataContext = editor.ViewModel };
        window.Show();
        Editor.Pump();
        window.Close();
        Editor.Pump();

        Assert.AreEqual(1000, layout.Width);
        Assert.AreEqual(640, layout.Height);
        Assert.AreEqual(1, layout.TreeWidth / layout.SideWidth, 0.02);
        Assert.AreEqual(160, layout.ChunkPanelHeight);
        return Task.CompletedTask;
    });

    private static void PressButton(Window dialog, string caption)
    {
        Button button = dialog.GetVisualDescendants().OfType<Button>().Single(candidate => (string?)candidate.Content == caption);
        button.Command!.Execute(button.CommandParameter);
    }
}
