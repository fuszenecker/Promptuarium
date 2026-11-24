using System.Collections.Generic;

namespace PStudio.Models;

/// <summary>
/// The settings of PStudio that are kept between the sessions (settings.prmt). A member that a file does not have gets
/// its default value, and a member that this version does not know is ignored, so new settings can be added freely.
/// </summary>
public sealed class StudioSettings
{
    /// <summary>The number of recent files kept.</summary>
    public const int MaxRecentFiles = 10;

    /// <summary>The settings of the view.</summary>
    public ViewSettings View { get; } = new();

    /// <summary>The layout of the window: its size, its place and where its splitters were dragged.</summary>
    public WindowLayout Layout { get; } = new();

    /// <summary>The full paths of the files opened or saved last, the most recent first (File / Open Recent).</summary>
    public List<string> RecentFiles { get; set; } = [];
}

/// <summary>
/// The settings of the view.
/// </summary>
public sealed class ViewSettings
{
    /// <summary>Whether the chunk panel is shown (View / Chunks).</summary>
    public bool IsChunkPanelVisible { get; set; } = true;

    /// <summary>Whether the serializer view is shown (View / Serializer), when the tree is serializable.</summary>
    public bool IsSerializerPanelVisible { get; set; } = true;

    /// <summary>Whether the text beside the serializer view is YAML (otherwise JSON).</summary>
    public bool IsYamlShown { get; set; } = true;
}

/// <summary>
/// The layout of the window, as it was closed. A size of 0 is not known (the default of the window is used).
/// </summary>
public sealed class WindowLayout
{
    /// <summary>The width of the window (not maximized).</summary>
    public double Width { get; set; }

    /// <summary>The height of the window (not maximized).</summary>
    public double Height { get; set; }

    /// <summary>The left of the window on the desktop, in pixels.</summary>
    public double X { get; set; }

    /// <summary>The top of the window on the desktop, in pixels.</summary>
    public double Y { get; set; }

    /// <summary>Whether the place of the window is known.</summary>
    public bool HasPosition { get; set; }

    /// <summary>Whether the window was maximized.</summary>
    public bool IsMaximized { get; set; }

    /// <summary>The width of the trees (the raw and the serializer view), left of the vertical splitter.</summary>
    public double TreeWidth { get; set; }

    /// <summary>The width right of the vertical splitter (the editors, the YAML or the JSON).</summary>
    public double SideWidth { get; set; }

    /// <summary>The height of the chunk panel.</summary>
    public double ChunkPanelHeight { get; set; }

    /// <summary>The height of the editor of the metadata.</summary>
    public double MetaDataHeight { get; set; }

    /// <summary>The height of the editor of the data.</summary>
    public double DataHeight { get; set; }
}
