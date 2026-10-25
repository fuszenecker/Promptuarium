namespace Promptuarium;

/// <summary>
/// Limits for loading a tree, e.g. from an untrusted source.
/// </summary>
/// <example>
/// <code>
/// var options = new LoadOptions { MaxBytes = 10 * 1024 * 1024, MaxNodes = 100_000 };
/// var tree = await Element.LoadAsync(networkStream, options, cancellationToken);
/// </code>
/// </example>
public class LoadOptions
{
    /// <summary>
    /// Maximum number of bytes read from the stream (control bytes included), or null for no limit.
    /// </summary>
    public long? MaxBytes { get; set; }

    /// <summary>
    /// Maximum number of nodes created while reading the stream (auxiliary nodes included), or null for no limit.
    /// </summary>
    public long? MaxNodes { get; set; }
}
