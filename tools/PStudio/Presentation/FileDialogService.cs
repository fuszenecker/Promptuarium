using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace PStudio.Presentation;

/// <inheritdoc/>
public sealed class FileDialogService : IFileDialogService
{
    private static readonly FilePickerFileType TreeFileType = new("Promptuarium tree")
    {
        Patterns = ["*.prmt", "*.bin"],
    };

    private readonly IWindowContext windowContext;

    /// <summary>
    /// Creates the service.
    /// </summary>
    public FileDialogService(IWindowContext windowContext)
    {
        this.windowContext = windowContext ?? throw new ArgumentNullException(nameof(windowContext));
    }

    /// <inheritdoc/>
    public Task<string?> PickTreeToOpenAsync()
    {
        return OpenAsync(new FilePickerOpenOptions
        {
            Title = "Open tree",
            AllowMultiple = false,
            FileTypeFilter = [TreeFileType, FilePickerFileTypes.All],
        });
    }

    /// <inheritdoc/>
    public Task<string?> PickTreeToSaveAsync(string? suggestedName)
    {
        return SaveAsync(new FilePickerSaveOptions
        {
            Title = "Save tree",
            SuggestedFileName = suggestedName ?? "tree.prmt",
            DefaultExtension = "prmt",
            ShowOverwritePrompt = true,
            FileTypeChoices = [TreeFileType, FilePickerFileTypes.All],
        });
    }

    /// <inheritdoc/>
    public Task<string?> PickContentToImportAsync(string title)
    {
        return OpenAsync(new FilePickerOpenOptions { Title = title, AllowMultiple = false });
    }

    /// <inheritdoc/>
    public Task<string?> PickContentToExportAsync(string title, string suggestedName)
    {
        return SaveAsync(new FilePickerSaveOptions { Title = title, SuggestedFileName = suggestedName, ShowOverwritePrompt = true });
    }

    private async Task<string?> OpenAsync(FilePickerOpenOptions options)
    {
        if (windowContext.ActiveWindow is not { } owner)
        {
            return null;
        }

        var files = await owner.StorageProvider.OpenFilePickerAsync(options);
        return files.FirstOrDefault()?.TryGetLocalPath();
    }

    private async Task<string?> SaveAsync(FilePickerSaveOptions options)
    {
        if (windowContext.ActiveWindow is not { } owner)
        {
            return null;
        }

        var file = await owner.StorageProvider.SaveFilePickerAsync(options);
        return file?.TryGetLocalPath();
    }
}
