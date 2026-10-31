using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Promptuarium.Serializer;
using PStudio.Interfaces;
using PStudio.Models;

namespace PStudio.Services;

/// <summary>
/// Keeps the chosen forms in a file signed <see cref="Signature"/> (written with Promptuarium.Serializer), by the full
/// paths of the tree files. A missing or broken file, or one with another signature, is an empty memory.
/// </summary>
public sealed class FormMemory : IFormMemory
{
    /// <summary>The number of files remembered; the least recently used ones are forgotten.</summary>
    public const int MaxFiles = 200;

    /// <summary>The signature of the file: PStudio node types, version 1.</summary>
    public const string Signature = "PRMT:PSTNT1";

    private static readonly PromptuariumSerializerOptions Options = new() { Signature = Signature };

    private readonly string settingsPath;
    private Memory? memory;

    /// <summary>
    /// Creates the memory, stored in <paramref name="settingsPath"/>.
    /// </summary>
    public FormMemory(string settingsPath)
    {
        this.settingsPath = settingsPath ?? throw new ArgumentNullException(nameof(settingsPath));
    }

    /// <summary>The default file: forms.prmt in the PStudio folder of the user's application data (~/.config on Linux).</summary>
    public static string DefaultPath => Path.Combine(SettingsFile.Folder, "forms.prmt");

    /// <inheritdoc/>
    public FileForms Recall(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return Load().Files.TryGetValue(Path.GetFullPath(path), out FileForms? forms) ? forms : new FileForms();
    }

    /// <inheritdoc/>
    public void Remember(string path, FileForms forms)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(forms);

        Dictionary<string, FileForms> files = Load().Files;
        string key = Path.GetFullPath(path);

        if (forms.IsEmpty)
        {
            if (!files.Remove(key))
            {
                return;
            }
        }
        else
        {
            forms.LastUsed = DateTime.UtcNow;
            files[key] = forms;

            foreach (string old in files.OrderByDescending(file => file.Value.LastUsed).Skip(MaxFiles).Select(file => file.Key).ToList())
            {
                files.Remove(old);
            }
        }

        SettingsFile.Save(settingsPath, memory!, Options);
    }

    private Memory Load() => memory ??= SettingsFile.Load<Memory>(settingsPath, Options);

    /// <summary>The content of the file.</summary>
    internal sealed class Memory
    {
        /// <summary>The forms by the full paths of the tree files.</summary>
        public Dictionary<string, FileForms> Files { get; } = new(StringComparer.Ordinal);
    }
}
