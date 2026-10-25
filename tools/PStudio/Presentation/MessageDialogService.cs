using System;
using System.Threading.Tasks;
using PStudio.Models;
using PStudio.ViewModels;
using PStudio.Views;

namespace PStudio.Presentation;

/// <inheritdoc/>
public sealed class MessageDialogService : IMessageDialogService
{
    private readonly IWindowContext windowContext;

    /// <summary>
    /// Creates the service.
    /// </summary>
    public MessageDialogService(IWindowContext windowContext)
    {
        this.windowContext = windowContext ?? throw new ArgumentNullException(nameof(windowContext));
    }

    /// <inheritdoc/>
    public async Task<SaveChoice> AskSaveChangesAsync(string documentName)
    {
        var viewModel = new MessageDialogViewModel(
            "Unsaved changes",
            $"Do you want to save the changes of {documentName}?",
            [
                new DialogButton("Save", (int)SaveChoice.Save, IsDefault: true),
                new DialogButton("Don't save", (int)SaveChoice.Discard),
                new DialogButton("Cancel", (int)SaveChoice.Cancel, IsCancel: true),
            ]);

        int? result = await ShowAsync(viewModel);
        return result is int choice ? (SaveChoice)choice : SaveChoice.Cancel;
    }

    /// <inheritdoc/>
    public async Task ShowErrorAsync(string title, string message)
    {
        await ShowAsync(new MessageDialogViewModel(title, message, [new DialogButton("OK", 0, IsDefault: true, IsCancel: true)]));
    }

    private async Task<int?> ShowAsync(MessageDialogViewModel viewModel)
    {
        if (windowContext.ActiveWindow is not { } owner)
        {
            return null;
        }

        return await new MessageDialog(viewModel).ShowDialog<int?>(owner);
    }
}
