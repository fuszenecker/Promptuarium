using Promptuarium;

namespace PStudio.Interfaces;

/// <summary>
/// Holds a copied node (with its descendants), shared by all editor windows.
/// </summary>
public interface INodeClipboard
{
    /// <summary>Whether there is a node to paste.</summary>
    bool HasNode { get; }

    /// <summary>Stores a copy of the node.</summary>
    void Copy(Element node);

    /// <summary>A new copy of the stored node, or null if there is none.</summary>
    Element? Paste();
}
