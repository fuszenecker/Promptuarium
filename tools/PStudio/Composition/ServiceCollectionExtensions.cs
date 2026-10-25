using Microsoft.Extensions.DependencyInjection;
using PStudio.Interfaces;
using PStudio.Presentation;
using PStudio.Services;
using PStudio.ViewModels;

namespace PStudio.Composition;

/// <summary>
/// Registers the services of the editor by kind.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// The windows, the dialogs and the view model factory.
        /// </summary>
        public void AddPresentation()
        {
            services.AddSingleton<IWindowContext, WindowContext>();
            services.AddSingleton<IFileDialogService, FileDialogService>();
            services.AddSingleton<IMessageDialogService, MessageDialogService>();
            services.AddSingleton<IEditorWindowService, EditorWindowService>();
            services.AddSingleton<IViewModelFactory, ViewModelFactory>();
        }

        /// <summary>
        /// The files, the chunk analysis and the node clipboard shared by the windows.
        /// </summary>
        public void AddServices()
        {
            services.AddSingleton<ITreeFileService, TreeFileService>();
            services.AddSingleton<IChunkAnalyzer, ChunkAnalyzer>();
            services.AddSingleton<INodeClipboard, NodeClipboard>();
        }

        /// <summary>
        /// The view models; every editor window has its own.
        /// </summary>
        public void AddViewModels()
        {
            services.AddTransient<MainWindowViewModel>();
        }
    }
}
