using System;

namespace Promptuarium;

#region Event types

/// <summary>
/// Base class for all Promptuarium events
/// </summary>
public class PromptuariumEventArgs : EventArgs
{
}

#region Load event arguments

/// <summary>
/// Base class for all Promptuarium load events
/// </summary>
public class PromptuariumLoadEventArg : PromptuariumEventArgs
{
}

/// <summary>
/// Argument class for all Promptuarium loading events
/// </summary>
public class PromptuariumLoadingEventArgs : PromptuariumLoadEventArg
{
}

/// <summary>
/// Argument class for all Promptuarium loaded events
/// </summary>
public class PromptuariumLoadedEventArgs : PromptuariumLoadEventArg
{
}
#endregion

#region Save event arguments

/// <summary>
/// Base class for all Promptuarium save events
/// </summary>
public class PromptuariumSaveEventArg : PromptuariumEventArgs
{
}

/// <summary>
/// Argument class for all Promptuarium saving events
/// </summary>
public class PromptuariumSavingEventArgs : PromptuariumSaveEventArg
{
}

/// <summary>
/// Argument class for all Promptuarium saved events
/// </summary>
public class PromptuariumSavedEventArgs : PromptuariumSaveEventArg
{
}
#endregion

#endregion

#region Handlers and events
public partial class Element
{
    #region Events

    /// <summary>
    /// Event handler for all Promptuarium data loading events
    /// </summary>
    public event EventHandler<PromptuariumLoadingEventArgs>? OnDataLoading;

    /// <summary>
    /// Event handler for all Promptuarium data loaded events
    /// </summary>
    public event EventHandler<PromptuariumLoadedEventArgs>? OnDataLoaded;

    /// <summary>
    /// Event handler for all Promptuarium data saving events
    /// </summary>
    public event EventHandler<PromptuariumSavingEventArgs>? OnDataSaving;

    /// <summary>
    /// Event handler for all Promptuarium data saved events
    /// </summary>
    public event EventHandler<PromptuariumSavedEventArgs>? OnDataSaved;

    /// <summary>
    /// Event handler for all Promptuarium metadata loading events
    /// </summary>
    public event EventHandler<PromptuariumLoadingEventArgs>? OnMetaDataLoading;

    /// <summary>
    /// Event handler for all Promptuarium metadata loaded events
    /// </summary>
    public event EventHandler<PromptuariumLoadedEventArgs>? OnMetaDataLoaded;

    /// <summary>
    /// Event handler for all Promptuarium metadata saving events
    /// </summary>
    public event EventHandler<PromptuariumSavingEventArgs>? OnMetaDataSaving;

    /// <summary>
    /// Event handler for all Promptuarium metadata saved events
    /// </summary>
    public event EventHandler<PromptuariumSavedEventArgs>? OnMetaDataSaved;
    
    #endregion
}
#endregion
