using System;
using System.IO;
using Promptuarium.Serializer;

namespace PStudio.Services;

/// <summary>
/// Reads and writes a file of the settings of PStudio with Promptuarium.Serializer. It is best effort: a missing or
/// broken file, or one with another signature, gives a new object, and an error of writing is not reported.
/// </summary>
internal static class SettingsFile
{
    /// <summary>The folder of the files: PStudio in the user's application data (~/.config on Linux).</summary>
    public static string Folder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PStudio");

    public static T Load<T>(string path, PromptuariumSerializerOptions options)
        where T : new()
    {
        try
        {
            return File.Exists(path) ? PromptuariumSerializer.Deserialize<T>(File.ReadAllBytes(path), options) : new T();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            return new T();
        }
    }

    // The file is replaced only after the whole content is written
    public static void Save(string path, object value, PromptuariumSerializerOptions options)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            string temporary = path + ".tmp";
            File.WriteAllBytes(temporary, PromptuariumSerializer.Serialize(value, options));
            File.Move(temporary, path, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}
