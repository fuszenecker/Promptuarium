using Promptuarium;
using PStudio.Models;

namespace PStudio.Interfaces;

/// <summary>
/// Holds a copied node (with its descendants), shared by all editor windows, together with the forms the user chose for
/// the contents of the copied nodes.
/// </summary>
public interface INodeClipboard
{
    /// <summary>Whether there is a node to paste.</summary>
    bool HasNode { get; }

    /// <summary>
    /// Stores a copy of the node, and of the forms of its contents, by the paths relative to the node ("/" is the node
    /// itself, "/0/2" the third child of its first child), with the forms of the nested trees in their data.
    /// </summary>
    void Copy(Element node, FileForms? forms = null);

    /// <summary>A new copy of the stored node, or null if there is none.</summary>
    Element? Paste();

    /// <summary>A new copy of the stored forms (by the paths relative to the node), empty if there are none.</summary>
    FileForms PasteForms();
}
