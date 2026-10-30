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
}
