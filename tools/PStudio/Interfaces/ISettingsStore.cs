using PStudio.Models;

namespace PStudio.Interfaces;

/// <summary>
/// The settings of PStudio, kept between the sessions and shared by the windows.
/// </summary>
public interface ISettingsStore
{
    /// <summary>The settings; change them, then <see cref="Save"/>.</summary>
    StudioSettings Settings { get; }

    /// <summary>
    /// Stores the settings. It is best effort: an error is not reported.
    /// </summary>
    void Save();
}
