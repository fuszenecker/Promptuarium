using System.Threading.Tasks;

namespace PStudio.Presentation;

/// <summary>
/// The file pickers of the editor.
/// </summary>
public interface IFileDialogService
{
    /// <summary>Asks for a tree file to open; null if cancelled.</summary>
    Task<string?> PickTreeToOpenAsync();

    /// <summary>Asks for the file to save a tree to; null if cancelled.</summary>
    Task<string?> PickTreeToSaveAsync(string? suggestedName);

    /// <summary>Asks for a file to import a content from; null if cancelled.</summary>
    Task<string?> PickContentToImportAsync(string title);

    /// <summary>Asks for the file to export a content to; null if cancelled.</summary>
    Task<string?> PickContentToExportAsync(string title, string suggestedName);
}
