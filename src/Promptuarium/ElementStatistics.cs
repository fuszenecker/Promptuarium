using System;
using System.Collections.Generic;

namespace Promptuarium;

#region Public types

/// <summary>
/// Statistics of the tree
/// </summary>
/// <remarks>
/// This class is used to collect statistics of the tree.
/// </remarks>
/// <example>
/// <code>
/// var statistics = tree.GetStatistics().ToString();
/// Console.WriteLine(statistics);
/// </code>
/// </example>
public class Statistics
{
    /// <summary>
    /// The number of nodes in the tree
    /// </summary>
    public int Nodes { get; internal set; }

    /// <summary>
    /// The number of nodes with data, but without metadata
    /// </summary>
    public int NodesWithData { get; internal set; }

    /// <summary>
    /// The number of nodes with metadata, but without data
    /// </summary>
    public int NodesWithMetaData { get; internal set; }

    /// <summary>
    /// The number of nodes with data and metadata
    /// </summary>
    public int NodesWithDataAndMetaData { get; internal set; }

    /// <summary>
    /// The number of nodes without data and metadata
    /// </summary>
    public int NodesWithoutDataAndMetaData { get; internal set; }

    /// <summary>
    /// The number of nodes without children
    /// </summary>
    public int NodesWithoutChildren { get; internal set; }

    /// <summary>
    /// The depth of the tree
    /// </summary>
    public int Depth { get; internal set; }

    /// <summary>
    /// The maximum number of children of a node
    /// </summary>
    public int MaxChildren { get; internal set; }

    /// <summary>
    /// The minimum number of children of a node
    /// </summary>
    public int MinChildren { get; internal set; }

    /// <summary>
    /// The longest data in the tree
    /// </summary>
    public long LongestData { get; internal set; }

    /// <summary>
    /// The longest metadata in the tree
    /// </summary>
    public long LongestMetaData { get; internal set; }

    /// <summary>
    /// The shortest data in the tree
    /// </summary>
    public long ShortestData { get; internal set; }

    /// <summary>
    /// The shortest metadata in the tree
    /// </summary>
    public long ShortestMetaData { get; internal set; }

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    /// <returns>A string that represents the current object.</returns>
    /// <example>
    /// <code>
    /// var statistics = tree.GetStatistics();
    /// Console.WriteLine(statistics.ToString());
    /// </code>
    /// </example>
    public override string ToString()
    {
        return
            $"Nodes: .......................... {Nodes}{Environment.NewLine}" +
            $"NodesWithMetaData: .............. {NodesWithMetaData}{Environment.NewLine}" +
            $"NodesWithData: .................. {NodesWithData}{Environment.NewLine}" +
            $"NodesWithDataAndMetaData: ....... {NodesWithDataAndMetaData}{Environment.NewLine}" +
            $"NodesWithoutDataAndMetaData: .... {NodesWithoutDataAndMetaData}{Environment.NewLine}" +
            $"NodesWithoutChildren: ........... {NodesWithoutChildren}{Environment.NewLine}" +
            $"Depth: .......................... {Depth}{Environment.NewLine}" +
            $"MaxChildren: .................... {MaxChildren}{Environment.NewLine}" +
            $"MinChildren: .................... {MinChildren}{Environment.NewLine}" +
            $"LongestMetaData: ................ {LongestMetaData}{Environment.NewLine}" +
            $"LongestData: .................... {LongestData}{Environment.NewLine}" +
            $"ShortestMetaData: ............... {ShortestMetaData}{Environment.NewLine}" +
            $"ShortestData: ................... {ShortestData}{Environment.NewLine}";
    }
}

#endregion

#region Private fields

public partial class Element
{
    /// <summary>
    /// Gets the statistics about the tree
    /// </summary>
    /// <returns>The statistics</returns>
    /// <remarks>
    /// This method walks the tree and collects statistics about the tree.
    /// </remarks>
    /// <example>
    /// <code>
    /// var statistics = tree.GetStatistics();
    /// Console.WriteLine(statistics.ToString());
    /// </code>
    /// </example>
    public Statistics GetStatistics()
    {
        var statistics = new Statistics
        {
            MinChildren = int.MaxValue,
            ShortestData = long.MaxValue,
            ShortestMetaData = long.MaxValue
        };

        Walk((Element node, IReadOnlyCollection<Element> ancestors) =>
            {
                statistics.Nodes++;

                if (ancestors.Count + 1 > statistics.Depth)
                {
                    statistics.Depth = ancestors.Count + 1;
                }

                if (node.Children.Count > statistics.MaxChildren)
                {
                    statistics.MaxChildren = node.Children.Count;
                }

                if (node.Children.Count < statistics.MinChildren)
                {
                    statistics.MinChildren = node.Children.Count;
                }

                if (!node.Data.IsEmpty && !node.MetaData.IsEmpty)
                {
                    statistics.NodesWithDataAndMetaData++;
                }
                else if (!node.Data.IsEmpty && node.MetaData.IsEmpty)
                {
                    statistics.NodesWithData++;
                }
                else if (node.Data.IsEmpty && !node.MetaData.IsEmpty)
                {
                    statistics.NodesWithMetaData++;
                }
                else
                {
                    statistics.NodesWithoutDataAndMetaData++;
                }

                if (!node.Data.IsEmpty && statistics.LongestData < node.Data.Length)
                {
                    statistics.LongestData = node.Data.Length;
                }

                if (!node.MetaData.IsEmpty && statistics.LongestMetaData < node.MetaData.Length)
                {
                    statistics.LongestMetaData = node.MetaData.Length;
                }

                if (!node.Data.IsEmpty && statistics.ShortestData > node.Data.Length)
                {
                    statistics.ShortestData = node.Data.Length;
                }

                if (!node.MetaData.IsEmpty && statistics.ShortestMetaData > node.MetaData.Length)
                {
                    statistics.ShortestMetaData = node.MetaData.Length;
                }

                if (node.Children.Count == 0)
                {
                    statistics.NodesWithoutChildren++;
                }
            });

        // The minimums are 0 if there are no nodes with data or metadata
        if (statistics.ShortestData == long.MaxValue)
        {
            statistics.ShortestData = 0;
        }

        if (statistics.ShortestMetaData == long.MaxValue)
        {
            statistics.ShortestMetaData = 0;
        }

        return statistics;
    }
}
#endregion
