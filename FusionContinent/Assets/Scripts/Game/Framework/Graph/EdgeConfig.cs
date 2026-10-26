using System;
using UnityEngine;

namespace FusionContinent.Framework.Graph
{
    /// <summary>
    /// 节点连线配置
    /// </summary>
    [Serializable]
    public class EdgeConfig
    {
        [SerializeField]
        private string _fromNodeId;

        [SerializeField]
        private string _fromPortId;

        [SerializeField]
        private string _toNodeId;

        [SerializeField]
        private string _toPortId;

        public string FromNodeId => _fromNodeId;
        public string FromPortId => _fromPortId;
        public string ToNodeId => _toNodeId;
        public string ToPortId => _toPortId;

        public EdgeConfig() { }

        public EdgeConfig(string fromNode, string fromPort, string toNode, string toPort)
        {
            _fromNodeId = fromNode;
            _fromPortId = fromPort;
            _toNodeId = toNode;
            _toPortId = toPort;
        }

        /// <summary>
        /// 复制连线配置
        /// </summary>
        public EdgeConfig Copy()
        {
            return new EdgeConfig(_fromNodeId, _fromPortId, _toNodeId, _toPortId);
        }
    }
}
