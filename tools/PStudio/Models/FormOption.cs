namespace PStudio.Models;

/// <summary>
/// A form in the form selector.
/// </summary>
/// <param name="Form">The form.</param>
/// <param name="Name">Its name.</param>
public sealed record FormOption(ContentForm Form, string Name)
{
    /// <inheritdoc/>
    public override string ToString() => Name;
}
