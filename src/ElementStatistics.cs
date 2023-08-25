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
    public int Nodes;

    /// <summary>
    /// The number of nodes with data
    /// </summary>
    public int NodesWithData;

    /// <summary>
    /// The number of nodes with metadata
    /// </summary>
    public int NodesWithMetaData;

    /// <summary>
    /// The number of nodes with data and metadata
    /// </summary>
    public int NodesWithDataAndMetaData;

    /// <summary>
    /// The number of nodes without data and metadata
    /// </summary>
    public int NodesWithoutDataAndMetaData;

    /// <summary>
    /// The number of nodes without children
    /// </summary>
    public int NodesWithoutChildren;

    /// <summary>
    /// The depth of the tree
    /// </summary>
    public int Depth;

    /// <summary>
    /// The maximum number of children of a node
    /// </summary>
    public int MaxChildren;

    /// <summary>
    /// The minimum number of children of a node
    /// </summary>
    public int MinChildren;

    /// <summary>
    /// The longest data in the tree
    /// </summary>
    public long LongestData;

    /// <summary>
    /// The longest metadata in the tree
    /// </summary>
    public long LongestMetaData;

    /// <summary>
    /// The shortest data in the tree
    /// </summary>
    public long ShortestData;

    /// <summary>
    /// The shortest metadata in the tree
    /// </summary>
    public long ShortestMetaData;

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
            $"NodesWithData: .................. {NodesWithData}{Environment.NewLine}" +
            $"NodesWithMetaData: .............. {NodesWithMetaData}{Environment.NewLine}" +
            $"NodesWithDataAndMetaData: ....... {NodesWithDataAndMetaData}{Environment.NewLine}" +
            $"NodesWithoutDataAndMetaData: .... {NodesWithoutDataAndMetaData}{Environment.NewLine}" +
            $"NodesWithoutChildren: ........... {NodesWithoutChildren}{Environment.NewLine}" +
            $"Depth: .......................... {Depth}{Environment.NewLine}" +
            $"MaxChildren: .................... {MaxChildren}{Environment.NewLine}" +
            $"MinChildren: .................... {MinChildren}{Environment.NewLine}" +
            $"LongestData: .................... {LongestData}{Environment.NewLine}" +
            $"LongestMetaData: ................ {LongestMetaData}{Environment.NewLine}" +
            $"ShortestData: ................... {ShortestData}{Environment.NewLine}" +
            $"ShortestMetaData: ............... {ShortestMetaData}{Environment.NewLine}";
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
        var statistics = new Statistics();

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

                if (node.Data != null && node.MetaData != null)
                {
                    statistics.NodesWithDataAndMetaData++;
                }
                else if (node.Data != null && node.MetaData == null)
                {
                    statistics.NodesWithData++;
                }
                else if (node.Data == null && node.MetaData != null)
                {
                    statistics.NodesWithMetaData++;
                }
                else
                {
                    statistics.NodesWithoutDataAndMetaData++;
                }

                if (node.Data != null && statistics.LongestData < node.Data.Length)
                {
                    statistics.LongestData = node.Data.Length;
                }

                if (node.MetaData != null && statistics.LongestMetaData < node.MetaData.Length)
                {
                    statistics.LongestMetaData = node.MetaData.Length;
                }

                if (node.Data != null && statistics.ShortestData > node.Data.Length)
                {
                    statistics.ShortestData = node.Data.Length;
                }

                if (node.MetaData != null && statistics.ShortestMetaData > node.MetaData.Length)
                {
                    statistics.ShortestMetaData = node.MetaData.Length;
                }

                if (node.Children.Count == 0)
                {
                    statistics.NodesWithoutChildren++;
                }
            });

        return statistics;
    }
}
#endregion
