using System.Collections.Generic;
using PStudio.Models;
using Promptuarium;

namespace PStudio.Interfaces;

/// <summary>
/// Lists the chunks of the binary form of a tree.
/// </summary>
public interface IChunkAnalyzer
{
    /// <summary>
    /// The chunks of the tree, as it would be saved.
    /// </summary>
    IReadOnlyList<ChunkRow> Analyze(Element root);
}
