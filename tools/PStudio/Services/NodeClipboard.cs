using System;
using System.Collections.Generic;
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
        storedForms = forms is null ? new FileForms() : CopyOf(forms);
    }

    /// <inheritdoc/>
    public Element? Paste() => stored is null ? null : new Element(stored);

    /// <inheritdoc/>
    public FileForms PasteForms() => CopyOf(storedForms);

    private static FileForms CopyOf(FileForms forms)
    {
        var copy = new FileForms();

        foreach (KeyValuePair<string, ContentForm> form in forms.Data)
        {
            copy.Data[form.Key] = form.Value;
        }

        foreach (KeyValuePair<string, ContentForm> form in forms.MetaData)
        {
            copy.MetaData[form.Key] = form.Value;
        }

        return copy;
    }
}
