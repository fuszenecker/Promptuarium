using System;
using PStudio.Interfaces;
using Promptuarium;

namespace PStudio.Services;

/// <inheritdoc/>
public sealed class NodeClipboard : INodeClipboard
{
    private Element? stored;

    /// <inheritdoc/>
    public bool HasNode => stored is not null;

    /// <inheritdoc/>
    public void Copy(Element node)
    {
        ArgumentNullException.ThrowIfNull(node);

        // A copy (without a parent), so later edits of the original do not change it
        stored = new Element(node);
    }

    /// <inheritdoc/>
    public Element? Paste() => stored is null ? null : new Element(stored);
}
