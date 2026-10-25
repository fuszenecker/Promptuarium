using CommunityToolkit.Mvvm.ComponentModel;
using PStudio.Models;

namespace PStudio.ViewModels;

/// <summary>
/// A chunk in the chunk panel; the chunks of the selected node are highlighted.
/// </summary>
public sealed partial class ChunkRowViewModel(ChunkRow row) : ViewModelBase
{
    [ObservableProperty]
    private bool isOfSelectedNode;

    /// <summary>The chunk.</summary>
    public ChunkRow Row { get; } = row;
}
