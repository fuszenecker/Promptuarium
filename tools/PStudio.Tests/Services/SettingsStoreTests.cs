using PStudio.Models;
using Promptuarium;
using Promptuarium.Serializer;
using PStudio.Services;

namespace PStudio.Tests.Services;

[TestClass]
public sealed class SettingsStoreTests
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "pstudio-settings-" + Guid.NewGuid().ToString("N"));

    private string SettingsPath => Path.Combine(directory, "PStudio", "settings.prmt");

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void MissingFile_GivesTheDefaults()
    {
        Assert.IsTrue(new SettingsStore(SettingsPath).Settings.View.IsChunkPanelVisible);
    }

    [TestMethod]
    public void SavedSettings_AreReadByANewStore()
    {
        var store = new SettingsStore(SettingsPath);
        store.Settings.View.IsChunkPanelVisible = false;

        store.Save();

        Assert.IsFalse(new SettingsStore(SettingsPath).Settings.View.IsChunkPanelVisible);
    }

    [TestMethod]
    public void LayoutOfTheWindow_IsReadByANewStore()
    {
        var store = new SettingsStore(SettingsPath);
        store.Settings.Layout.Width = 1000;
        store.Settings.Layout.X = -20;
        store.Settings.Layout.HasPosition = true;
        store.Settings.Layout.IsMaximized = true;
        store.Settings.Layout.TreeWidth = 600;
        store.Settings.Layout.ChunkPanelHeight = 150;

        store.Save();

        WindowLayout layout = new SettingsStore(SettingsPath).Settings.Layout;
        Assert.AreEqual(1000, layout.Width);
        Assert.AreEqual(-20, layout.X);
        Assert.IsTrue(layout.HasPosition);
        Assert.IsTrue(layout.IsMaximized);
        Assert.AreEqual(600, layout.TreeWidth);
        Assert.AreEqual(150, layout.ChunkPanelHeight);
    }

    [TestMethod]
    public void TheFile_IsAConfigurationFile()
    {
        new SettingsStore(SettingsPath).Save();

        Assert.AreEqual(PromptuariumSerializerOptions.ConfigurationSignature, Element.FromBytes(File.ReadAllBytes(SettingsPath)).MetaData.AsAsciiString());
    }

    [TestMethod]
    public void UnknownAndMissingSettings_AreTolerated()
    {
        // A file of another version: a setting this version does not know, and no View
        var options = new PromptuariumSerializerOptions { Signature = PromptuariumSerializerOptions.ConfigurationSignature };
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllBytes(SettingsPath, PromptuariumSerializer.Serialize(new Dictionary<string, object> { ["Future"] = 42 }, options));

        Assert.IsTrue(new SettingsStore(SettingsPath).Settings.View.IsChunkPanelVisible);
    }

    [TestMethod]
    public void ABrokenFile_GivesTheDefaults()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, "not a tree");

        Assert.IsTrue(new SettingsStore(SettingsPath).Settings.View.IsChunkPanelVisible);
    }

    [TestMethod]
    public void Layout_IsUnknown_ByDefault()
    {
        WindowLayout layout = new SettingsStore(SettingsPath).Settings.Layout;

        Assert.AreEqual(0, layout.Width);
        Assert.AreEqual(0, layout.Height);
        Assert.IsFalse(layout.HasPosition);
        Assert.IsFalse(layout.IsMaximized);
        Assert.AreEqual(0, layout.TreeWidth);
        Assert.AreEqual(0, layout.SideWidth);
        Assert.AreEqual(0, layout.ChunkPanelHeight);
        Assert.AreEqual(0, layout.MetaDataHeight);
        Assert.AreEqual(0, layout.DataHeight);
    }

    [TestMethod]
    public void EveryMemberOfTheLayout_IsReadByANewStore()
    {
        var store = new SettingsStore(SettingsPath);
        WindowLayout saved = store.Settings.Layout;
        saved.Width = 1234;
        saved.Height = 777;
        saved.X = 10;
        saved.Y = -300;
        saved.HasPosition = true;
        saved.IsMaximized = false;
        saved.TreeWidth = 640;
        saved.SideWidth = 480;
        saved.ChunkPanelHeight = 222;
        saved.MetaDataHeight = 100;
        saved.DataHeight = 300;

        store.Save();

        WindowLayout layout = new SettingsStore(SettingsPath).Settings.Layout;
        Assert.AreEqual(1234, layout.Width);
        Assert.AreEqual(777, layout.Height);
        Assert.AreEqual(10, layout.X);
        Assert.AreEqual(-300, layout.Y);
        Assert.IsTrue(layout.HasPosition);
        Assert.IsFalse(layout.IsMaximized);
        Assert.AreEqual(640, layout.TreeWidth);
        Assert.AreEqual(480, layout.SideWidth);
        Assert.AreEqual(222, layout.ChunkPanelHeight);
        Assert.AreEqual(100, layout.MetaDataHeight);
        Assert.AreEqual(300, layout.DataHeight);
    }

    [TestMethod]
    public void TheFileWithALayout_IsStillAConfigurationFileOfVersion1()
    {
        var store = new SettingsStore(SettingsPath);
        store.Settings.Layout.Width = 1000;
        store.Settings.Layout.X = -20;
        store.Settings.Layout.HasPosition = true;

        store.Save();

        Assert.AreEqual(PromptuariumSerializerOptions.ConfigurationSignature, Element.FromBytes(File.ReadAllBytes(SettingsPath)).MetaData.AsAsciiString());
    }

    [TestMethod]
    public void AFileWithoutALayout_GivesTheDefaultLayout()
    {
        var store = new SettingsStore(SettingsPath);
        store.Settings.View.IsYamlShown = false;
        store.Save();

        // An older file: the same settings without the layout
        Element tree = Element.FromBytes(File.ReadAllBytes(SettingsPath));
        Element layout = tree.Children.Single(child => child.Data.AsUtf8String() == "Layout");
        tree.Detach(layout);
        File.WriteAllBytes(SettingsPath, tree.ToBytes());

        StudioSettings settings = new SettingsStore(SettingsPath).Settings;
        Assert.IsFalse(settings.View.IsYamlShown);
        Assert.AreEqual(0, settings.Layout.Width);
        Assert.IsFalse(settings.Layout.HasPosition);
    }
}
