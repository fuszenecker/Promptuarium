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
        #region Handlers

        public delegate void DataLoadingEventHandler(Element source, PromptuariumLoadingEventArgs args);
        public delegate void DataLoadedEventHandler(Element source, PromptuariumLoadedEventArgs args);
        public delegate void DataSavingEventHandler(Element source, PromptuariumSavingEventArgs args);
        public delegate void DataSavedEventHandler(Element source, PromptuariumSavedEventArgs args);

        public delegate void MetaDataLoadingEventHandler(Element source, PromptuariumLoadingEventArgs args);
        public delegate void MetaDataLoadedEventHandler(Element source, PromptuariumLoadedEventArgs args);
        public delegate void MetaDataSavingEventHandler(Element source, PromptuariumSavingEventArgs args);
        public delegate void MetaDataSavedEventHandler(Element source, PromptuariumSavedEventArgs args);

        #endregion

        #region Events

        public static event DataLoadingEventHandler OnDataLoading;
        public static event DataLoadedEventHandler OnDataLoaded;
        public event DataSavingEventHandler OnDataSaving;
        public event DataSavedEventHandler OnDataSaved;

        public static event MetaDataLoadingEventHandler OnMetaDataLoading;
        public static event MetaDataLoadedEventHandler OnMetaDataLoaded;
        public event MetaDataSavingEventHandler OnMetaDataSaving;
        public event MetaDataSavedEventHandler OnMetaDataSaved;

        #endregion
    }
    
    #endregion
}