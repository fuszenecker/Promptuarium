using System.Threading.Tasks;
using PStudio.Models;

namespace PStudio.Presentation;

/// <summary>
/// The message boxes of the editor.
/// </summary>
public interface IMessageDialogService
{
    /// <summary>
    /// Asks whether the changes of a document are saved; Cancel when no window can own the dialog.
    /// </summary>
    Task<SaveChoice> AskSaveChangesAsync(string documentName);

    /// <summary>
    /// Shows an error.
    /// </summary>
    Task ShowErrorAsync(string title, string message);
}
