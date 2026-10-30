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
    public const string Signature = "PRMT:PStNT1";

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
    public static string DefaultPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PStudio", "forms.prmt");

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

        Save();
    }

    private Memory Load()
    {
        if (memory is not null)
        {
            return memory;
        }

        try
        {
            memory = File.Exists(settingsPath) ? PromptuariumSerializer.Deserialize<Memory>(File.ReadAllBytes(settingsPath), Options) : new Memory();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            memory = new Memory();
        }

        return memory;
    }

    // The file is replaced only after the whole memory is written
    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
            string temporary = settingsPath + ".tmp";
            File.WriteAllBytes(temporary, PromptuariumSerializer.Serialize(memory!, Options));
            File.Move(temporary, settingsPath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    /// <summary>The content of the file.</summary>
    internal sealed class Memory
    {
        /// <summary>The forms by the full paths of the tree files.</summary>
        public Dictionary<string, FileForms> Files { get; } = new(StringComparer.Ordinal);
    }
}
