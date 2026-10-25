namespace PStudio.Models;

/// <summary>
/// The answer to the question about unsaved changes.
/// </summary>
public enum SaveChoice
{
    /// <summary>Save the changes, then go on.</summary>
    Save,

    /// <summary>Go on without saving.</summary>
    Discard,

    /// <summary>Do not go on.</summary>
    Cancel,
}
