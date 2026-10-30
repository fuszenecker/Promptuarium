using PStudio.Models;

namespace PStudio.Interfaces;

/// <summary>
/// Remembers, between the sessions, the forms the user chose for the contents of the nodes of the tree files.
/// </summary>
public interface IFormMemory
{
    /// <summary>
    /// The forms of a file (empty if there are none).
    /// </summary>
    FileForms Recall(string path);

    /// <summary>
    /// Stores the forms of a file (an empty one forgets the file). It is best effort: an error is not reported.
    /// </summary>
    void Remember(string path, FileForms forms);
}
