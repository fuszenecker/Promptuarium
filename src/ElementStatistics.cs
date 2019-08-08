using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
    }

    #endregion
    
    #region Private fields

    public partial class Element
    {
        private Statistics _statistics;

        public Statistics Statistics
        { 
            get
            {
                _statistics = new Statistics();

                Walk((Element node, List<Element> ancestors) =>
                    {
                        _statistics.Nodes++;

                        if (ancestors.Count + 1 > _statistics.Depth)
                        {
                            _statistics.Depth = ancestors.Count + 1;
                        }

                        if (node.Children.Count() > _statistics.MaxChildren)
                        {
                            _statistics.MaxChildren = node.Children.Count();
                        }

                        if (node.Children.Count() < _statistics.MinChildren)
                        {
                            _statistics.MinChildren = node.Children.Count();
                        }

                        if (node.Data != null && node.MetaData != null)
                        {
                            _statistics.NodesWithDataAndMetaData++;
                        }
                        else if (node.Data != null && node.MetaData == null)
                        {
                            _statistics.NodesWithData++;
                        }
                        else if (node.Data == null && node.MetaData != null)
                        {
                            _statistics.NodesWithMetaData++;
                        }
                        else
                        {
                            _statistics.NodesWithoutDataAndMetaData++;
                        }

                        if (node.Data != null && _statistics.LongestData < node.Data.Length)
                        {
                            _statistics.LongestData = node.Data.Length;
                        }

                        if (node.MetaData != null && _statistics.LongestMetaData < node.MetaData.Length)
                        {
                            _statistics.LongestMetaData = node.MetaData.Length;
                        }

                        if (node.Data != null && _statistics.ShortestData > node.Data.Length)
                        {
                            _statistics.ShortestData = node.Data.Length;
                        }

                        if (node.MetaData != null && _statistics.ShortestMetaData > node.MetaData.Length)
                        {
                            _statistics.ShortestMetaData = node.MetaData.Length;
                        }
                        
                        if (node.Children.Count() == 0)
                        {
                            _statistics.NodesWithoutChildren++;
                        }
                    });

                return _statistics;
            }
        }
    }

    #endregion
}
