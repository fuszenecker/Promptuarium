using System;
using System.IO;
using Promptuarium.Serializer;
using PStudio.Interfaces;
using PStudio.Models;

namespace PStudio.Services;

/// <summary>
/// Keeps the settings in a configuration file (PRMT:CFG1-LE, written with Promptuarium.Serializer). A missing or broken
/// file, or one with another signature, gives the default settings.
/// </summary>
public sealed class SettingsStore : ISettingsStore
{
    private static readonly PromptuariumSerializerOptions Options = new() { Signature = PromptuariumSerializerOptions.ConfigurationSignature };

    private readonly string settingsPath;
    private StudioSettings? settings;

    /// <summary>
    /// Creates the store, kept in <paramref name="settingsPath"/>.
    /// </summary>
    public SettingsStore(string settingsPath)
    {
        this.settingsPath = settingsPath ?? throw new ArgumentNullException(nameof(settingsPath));
    }

    /// <summary>The default file: settings.prmt in the PStudio folder of the user's application data (~/.config on Linux).</summary>
    public static string DefaultPath => Path.Combine(SettingsFile.Folder, "settings.prmt");

    /// <inheritdoc/>
    public StudioSettings Settings => settings ??= SettingsFile.Load<StudioSettings>(settingsPath, Options);

    /// <inheritdoc/>
    public event EventHandler? Saved;

    /// <inheritdoc/>
    public void Save()
    {
        SettingsFile.Save(settingsPath, Settings, Options);
        Saved?.Invoke(this, EventArgs.Empty);
    }
}
