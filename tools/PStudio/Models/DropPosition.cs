namespace PStudio.Models;

/// <summary>
/// Where a dragged node is dropped, relative to the target node.
/// </summary>
public enum DropPosition
{
    /// <summary>Before the target, as its sibling.</summary>
    Before,

    /// <summary>After the target, as its sibling.</summary>
    After,

    /// <summary>As the last child of the target.</summary>
    Inside,
}
