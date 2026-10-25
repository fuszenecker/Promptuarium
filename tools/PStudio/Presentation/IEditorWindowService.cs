using PStudio.ViewModels;

namespace PStudio.Presentation;

/// <summary>
/// Opens editor windows.
/// </summary>
public interface IEditorWindowService
{
    /// <summary>
    /// Opens a window that edits the data of a node as a nested tree.
    /// </summary>
    /// <exception cref="Promptuarium.PromptuariumException">The data is not a valid tree</exception>
    void OpenNested(MainWindowViewModel host, NodeViewModel node);
}
