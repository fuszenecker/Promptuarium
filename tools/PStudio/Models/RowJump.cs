namespace PStudio.Models;

/// <summary>A jump of the selection in a tree, by the keyboard (Home, End, Page Up, Page Down).</summary>
public enum RowJump
{
    /// <summary>To the first row (Home).</summary>
    First,

    /// <summary>To the last shown row (End).</summary>
    Last,

    /// <summary>A page up (Page Up).</summary>
    PageUp,

    /// <summary>A page down (Page Down).</summary>
    PageDown,
}
