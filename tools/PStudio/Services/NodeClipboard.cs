using System;
using PStudio.Interfaces;
using PStudio.Models;
using Promptuarium;

namespace PStudio.Services;

/// <inheritdoc/>
public sealed class NodeClipboard : INodeClipboard
{
    private Element? stored;
    private FileForms storedForms = new();

    /// <inheritdoc/>
    public bool HasNode => stored is not null;

    /// <inheritdoc/>
    public void Copy(Element node, FileForms? forms = null)
    {
        ArgumentNullException.ThrowIfNull(node);

        // Copies (the node without a parent), so later edits of the original do not change them
        stored = new Element(node);
        storedForms = forms?.Clone() ?? new FileForms();
    }

    /// <inheritdoc/>
    public Element? Paste() => stored is null ? null : new Element(stored);

    /// <inheritdoc/>
    public FileForms PasteForms() => storedForms.Clone();
}
