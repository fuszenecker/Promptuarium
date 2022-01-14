using System;

namespace Promptuarium
{
    #region Event types

    public class PromptuariumEventArgs : EventArgs
    {
    }

    #region Load event arguments
    public class PromptuariumLoadEventArg : PromptuariumEventArgs
    {
    }

    public class PromptuariumLoadingEventArgs : PromptuariumLoadEventArg
    {
    }

    public class PromptuariumLoadedEventArgs : PromptuariumLoadEventArg
    {
    }
    #endregion

    #region Save event arguments
    public class PromptuariumSaveEventArg : PromptuariumEventArgs
    {
    }

    public class PromptuariumSavingEventArgs : PromptuariumSaveEventArg
    {
    }

    public class PromptuariumSavedEventArgs : PromptuariumSaveEventArg
    {
    }
    #endregion

    #endregion

    #region Handlers and events
    public partial class Element
    {
        #region Events
        public static event EventHandler<PromptuariumLoadingEventArgs>? OnDataLoading;
        public static event EventHandler<PromptuariumLoadedEventArgs>? OnDataLoaded;
        public event EventHandler<PromptuariumSavingEventArgs>? OnDataSaving;
        public event EventHandler<PromptuariumSavedEventArgs>? OnDataSaved;

        public static event EventHandler<PromptuariumLoadingEventArgs>? OnMetaDataLoading;
        public static event EventHandler<PromptuariumLoadedEventArgs>? OnMetaDataLoaded;
        public event EventHandler<PromptuariumSavingEventArgs>? OnMetaDataSaving;
        public event EventHandler<PromptuariumSavedEventArgs>? OnMetaDataSaved;
        #endregion
    }
    #endregion
}