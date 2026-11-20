using System;
using System.Collections.Generic;

namespace PStudio.Models;

/// <summary>
/// The forms the user chose for the contents of the nodes of one tree file, by the paths of the nodes ("/", "/0/2"),
/// and the forms of the nested trees in the data of its nodes.
/// </summary>
public sealed class FileForms
{
    /// <summary>When the forms were stored last (the least recently used files are forgotten first).</summary>
    public DateTime LastUsed { get; set; }

    /// <summary>The forms of the data.</summary>
    public Dictionary<string, ContentForm> Data { get; } = new(StringComparer.Ordinal);

    /// <summary>The forms of the metadata.</summary>
    public Dictionary<string, ContentForm> MetaData { get; } = new(StringComparer.Ordinal);

    /// <summary>The forms of the nested trees, by the paths of the nodes whose data they are.</summary>
    public Dictionary<string, FileForms> Nested { get; } = new(StringComparer.Ordinal);

    /// <summary>Whether no form is stored.</summary>
    public bool IsEmpty => Data.Count == 0 && MetaData.Count == 0 && Nested.Count == 0;

    /// <summary>
    /// A deep copy (with the nested trees), so a change of one does not change the other.
    /// </summary>
    public FileForms Clone()
    {
        var copy = new FileForms { LastUsed = LastUsed };

        foreach (KeyValuePair<string, ContentForm> form in Data)
        {
            copy.Data[form.Key] = form.Value;
        }

        foreach (KeyValuePair<string, ContentForm> form in MetaData)
        {
            copy.MetaData[form.Key] = form.Value;
        }

        foreach (KeyValuePair<string, FileForms> nested in Nested)
        {
            copy.Nested[nested.Key] = nested.Value.Clone();
        }

        return copy;
    }
}
