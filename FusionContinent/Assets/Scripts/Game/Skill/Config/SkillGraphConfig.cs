using System;
using System.Collections.Generic;
using System.Linq;
using FusionContinent.Framework;
using FusionContinent.Framework.Graph;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FusionContinent.Skill.Config
{
    /// <summary>
    /// 技能图配置
    /// </summary>
    [CreateAssetMenu(fileName = "NewSkillGraph", menuName = "FusionContinent/Skill/Skill Graph")]
    public class SkillGraphConfig : ScriptableObject
    {
        [SerializeField]
        private int _graphId;
        public int GraphId
        {
            get => _graphId;
            internal set => _graphId = value;
        }

        [SerializeField]
        private string _graphName;
        public string GraphName
        {
            get => _graphName;
            internal set => _graphName = value;
        }

        [SerializeField]
        private int _skillId;
        public int SkillId
        {
            get => _skillId;
            internal set => _skillId = value;
        }
        [SerializeField]
        private List<SkillNodeConfig> _nodes = new List<SkillNodeConfig>();

        [SerializeField]
        private List<EdgeConfig> _edges = new List<EdgeConfig>();

        // 节点计数器，用于生成唯一ID
        [SerializeField]
        private SerializableDictionary<string, int> _nodeTypeCounters = new SerializableDictionary<string, int>();
        
        public IReadOnlyList<SkillNodeConfig> Nodes => _nodes;
        public IReadOnlyList<EdgeConfig> Edges => _edges;

        /// <summary>
        /// 生成唯一节点ID，格式：{GraphName}_{NodeType}_{序号}
        /// </summary>
        public string GenerateNodeId(string nodeType)
        {
            var safeGraphName = string.IsNullOrEmpty(_graphName) ? "Unknown" : _graphName;
            safeGraphName = safeGraphName.Replace(" ", "_").Replace("-", "_");

            if (!_nodeTypeCounters.ContainsKey(nodeType))
                _nodeTypeCounters[nodeType] = 0;

            _nodeTypeCounters[nodeType]++;
            var count = _nodeTypeCounters[nodeType];

            return $"{safeGraphName}_{nodeType}_{count:D3}";
        }

        /// <summary>
        /// 重置计数器
        /// </summary>
        public void ResetCounters()
        {
            _nodeTypeCounters.Clear();
            foreach (var node in _nodes)
            {
                var nodeType = node.NodeType;
                if (!_nodeTypeCounters.ContainsKey(nodeType))
                    _nodeTypeCounters[nodeType] = 0;

                // 提取序号
                var parts = node.NodeId.Split('_');
                if (parts.Length >= 3 && int.TryParse(parts[^1], out var num))
                {
                    _nodeTypeCounters[nodeType] = Mathf.Max(_nodeTypeCounters[nodeType], num);
                }
            }
        }

        public SkillNodeConfig GetNode(string nodeId)
        {
            return _nodes.Find(n => n.NodeId == nodeId);
        }

        public List<SkillNodeConfig> GetNodesByType(string nodeType)
        {
            return _nodes.Where(n => n.NodeType == nodeType).ToList();
        }

        public SkillNodeConfig GetEntryNode()
        {
            var castNode = _nodes.FirstOrDefault(n => n.NodeType == "Cast");
            return castNode ?? _nodes.FirstOrDefault();
        }

        public List<EdgeConfig> GetOutputEdges(string nodeId)
        {
            return _edges.Where(e => e.FromNodeId == nodeId).ToList();
        }

        public List<EdgeConfig> GetInputEdges(string nodeId)
        {
            return _edges.Where(e => e.ToNodeId == nodeId).ToList();
        }

        public void AddNode(SkillNodeConfig node)
        {
            if (string.IsNullOrEmpty(node.NodeId))
                node.NodeId = Guid.NewGuid().ToString();
            _nodes.Add(node);
#if UNITY_EDITOR
            // 只有父资产（this）已持久化时才注册子资产
            var parentPath = AssetDatabase.GetAssetPath(this);
            if (string.IsNullOrEmpty(parentPath))
            {
                EditorUtility.SetDirty(this);
                return;
            }

            // 如果节点已属于另一个资产（从测试数据复制而来），先剥离
            var nodePath = AssetDatabase.GetAssetPath(node);
            if (!string.IsNullOrEmpty(nodePath) && nodePath != parentPath)
            {
                AssetDatabase.RemoveObjectFromAsset(node);
            }

            AssetDatabase.AddObjectToAsset(node, this);
            EditorUtility.SetDirty(this);
#endif
        }

        public void RemoveNode(string nodeId)
        {
            _nodes.RemoveAll(n => n.NodeId == nodeId);
            _edges.RemoveAll(e => e.FromNodeId == nodeId || e.ToNodeId == nodeId);
        }

        public void AddEdge(EdgeConfig edge)
        {
            _edges.Add(edge);
        }

        public void RemoveEdge(string fromNodeId, string toNodeId)
        {
            _edges.RemoveAll(e => e.FromNodeId == fromNodeId && e.ToNodeId == toNodeId);
        }

        /// <summary>
        /// 按端口精确移除一条连线
        /// </summary>
        public void RemoveEdge(string fromNodeId, string fromPortId, string toNodeId, string toPortId)
        {
            _edges.RemoveAll(e =>
                e.FromNodeId == fromNodeId && e.FromPortId == fromPortId &&
                e.ToNodeId == toNodeId && e.ToPortId == toPortId);
        }

        public void Clear()
        {
#if UNITY_EDITOR
            // 先从资产中移除所有子资产
            foreach (var node in _nodes)
            {
                AssetDatabase.RemoveObjectFromAsset(node);
                UnityEngine.Object.DestroyImmediate(node, true);
            }
#endif
            _nodes.Clear();
            _edges.Clear();
        }

        public SkillGraphConfig Clone()
        {
            var clone = Instantiate(this);
            return clone;
        }

        #region 编辑器辅助方法

        public void SetGraphName(string name)
        {
            _graphName = name;
        }

        public void SetSkillId(int skillId)
        {
            _skillId = skillId;
        }

        public SkillNodeConfig CreateNode(string nodeType, Vector2 position)
        {
            var node = SkillNodeConfig.Create(nodeType, position);
            node.NodeId = GenerateNodeId(nodeType);
#if UNITY_EDITOR
            AssetDatabase.AddObjectToAsset(node, this);
            EditorUtility.SetDirty(this);
#endif
            _nodes.Add(node);
            return node;
        }

        public void DeleteNode(string nodeId)
        {
            var node = _nodes.FirstOrDefault(n => n.NodeId == nodeId);
            if (node != null)
            {
                RemoveNode(nodeId);
#if UNITY_EDITOR
                AssetDatabase.RemoveObjectFromAsset(node);
                UnityEngine.Object.DestroyImmediate(node, true);
                EditorUtility.SetDirty(this);
#endif
            }
        }

        public void CreateEdge(string fromNodeId, string fromPortId, string toNodeId, string toPortId)
        {
            var edge = new EdgeConfig(fromNodeId, fromPortId, toNodeId, toPortId);
            AddEdge(edge);
        }

        public void DeleteEdge(string fromNodeId, string toNodeId)
        {
            RemoveEdge(fromNodeId, toNodeId);
        }

        #endregion
    }
}
