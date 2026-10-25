using System;

namespace Promptuarium;

/// <summary>
/// Options for loading a tree: limits (e.g. for an untrusted source) and events.
/// </summary>
/// <remarks>
/// The sender of the events is the node being loaded. Each event is fired once per node:
/// the loading events before the first byte of the content is stored, the loaded events when the content of the node is complete.
/// </remarks>
/// <example>
/// <code>
/// var options = new LoadOptions { MaxBytes = 10 * 1024 * 1024, MaxNodes = 100_000 };
/// options.OnDataLoaded += (sender, _) => Console.WriteLine(((Element)sender!).Data!.AsUtf8String());
///
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
    /// Maximum number of nodes created while reading the stream (placeholders included), or null for no limit.
    /// </summary>
    public long? MaxNodes { get; set; }

    /// <summary>
    /// Fired before the first byte of the data of a node is stored.
    /// </summary>
    public event EventHandler<PromptuariumLoadingEventArgs>? OnDataLoading;

    /// <summary>
    /// Fired when the data of a node is complete.
    /// </summary>
    public event EventHandler<PromptuariumLoadedEventArgs>? OnDataLoaded;

    /// <summary>
    /// Fired before the first byte of the metadata of a node is stored.
    /// </summary>
    public event EventHandler<PromptuariumLoadingEventArgs>? OnMetaDataLoading;

    /// <summary>
    /// Fired when the metadata of a node is complete.
    /// </summary>
    public event EventHandler<PromptuariumLoadedEventArgs>? OnMetaDataLoaded;

    internal void FireDataLoading(Element node) => OnDataLoading?.Invoke(node, new PromptuariumLoadingEventArgs());

    internal void FireDataLoaded(Element node) => OnDataLoaded?.Invoke(node, new PromptuariumLoadedEventArgs());

    internal void FireMetaDataLoading(Element node) => OnMetaDataLoading?.Invoke(node, new PromptuariumLoadingEventArgs());

    internal void FireMetaDataLoaded(Element node) => OnMetaDataLoaded?.Invoke(node, new PromptuariumLoadedEventArgs());
}
