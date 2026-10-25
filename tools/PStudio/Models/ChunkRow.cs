namespace PStudio.Models;

/// <summary>
/// One chunk of the binary form of a tree, as shown in the chunk view.
/// </summary>
/// <param name="Offset">Position of the control byte, in hex</param>
/// <param name="Control">The control byte, in hex</param>
/// <param name="Direction">Append, Down, Up or New</param>
/// <param name="ContentType">data or metadata</param>
/// <param name="Length">Length of the payload (with "log" for a logarithmic length)</param>
/// <param name="Kind">node, append, auxiliary, empty node or terminator</param>
/// <param name="Depth">Depth of the node the chunk belongs to</param>
/// <param name="Payload">Preview of the payload</param>
public sealed record ChunkRow(string Offset, string Control, string Direction, string ContentType, string Length, string Kind, int Depth, string Payload);
