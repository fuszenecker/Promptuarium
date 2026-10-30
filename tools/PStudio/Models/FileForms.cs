using System;
using System.Collections.Generic;

namespace PStudio.Models;

/// <summary>
/// The forms the user chose for the contents of the nodes of one tree file, by the paths of the nodes ("/", "/0/2").
/// </summary>
public sealed class FileForms
{
    /// <summary>When the forms were stored last (the least recently used files are forgotten first).</summary>
    public DateTime LastUsed { get; set; }

    /// <summary>The forms of the data.</summary>
    public Dictionary<string, ContentForm> Data { get; } = new(StringComparer.Ordinal);

    /// <summary>The forms of the metadata.</summary>
    public Dictionary<string, ContentForm> MetaData { get; } = new(StringComparer.Ordinal);

    /// <summary>Whether no form is stored.</summary>
    public bool IsEmpty => Data.Count == 0 && MetaData.Count == 0;
}
