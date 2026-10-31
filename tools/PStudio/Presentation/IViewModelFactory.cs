using PStudio.ViewModels;

namespace PStudio.Presentation;

/// <summary>
/// Creates the view models that are not singletons.
/// </summary>
public interface IViewModelFactory
{
    /// <summary>Creates the view model of an editor window.</summary>
    MainWindowViewModel CreateEditor();

    /// <summary>Creates the editor of a content (the metadata or the data).</summary>
    ContentEditorViewModel CreateContentEditor(string title);
}
