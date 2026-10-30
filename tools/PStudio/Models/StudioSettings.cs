namespace PStudio.Models;

/// <summary>
/// The settings of PStudio that are kept between the sessions (settings.prmt). A member that a file does not have gets
/// its default value, and a member that this version does not know is ignored, so new settings can be added freely.
/// </summary>
public sealed class StudioSettings
{
    /// <summary>The settings of the view.</summary>
    public ViewSettings View { get; } = new();
}

/// <summary>
/// The settings of the view.
/// </summary>
public sealed class ViewSettings
{
    /// <summary>Whether the chunk panel is shown (View / Chunks).</summary>
    public bool IsChunkPanelVisible { get; set; } = true;
}
