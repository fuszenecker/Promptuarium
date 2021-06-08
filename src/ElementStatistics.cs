using System;
using System.Collections.Generic;
using System.Linq;

namespace Promptuarium
{
    #region Public types
    public class Statistics
    {
        public int Nodes;

        public int NodesWithData;
        public int NodesWithMetaData;
        public int NodesWithDataAndMetaData;
        public int NodesWithoutDataAndMetaData;

        public int NodesWithoutChildren;

        public int Depth;

        public int MaxChildren;
        public int MinChildren;

        public long LongestData;
        public long LongestMetaData;
        public long ShortestData;
        public long ShortestMetaData;

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
        private Statistics statistics;

        public Statistics Statistics
        {
            get
            {
                statistics = new Statistics();

                Walk((Element node, List<Element> ancestors) =>
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
    }
    #endregion
}
