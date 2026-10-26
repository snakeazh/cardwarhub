#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using FusionContinent.Framework.Graph;
using FusionContinent.Skill.Config;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace FusionContinent.Skill.Editor
{
    /// <summary>
    /// 技能图视图 - 处理节点和连线的显示与交互
    /// </summary>
    public class SkillGraphView : GraphView
    {
        private SkillGraphEditorWindow _editorWindow;
        private SkillGraphConfig _currentGraph;
        private Dictionary<string, SkillNodeView> _nodeViews = new Dictionary<string, SkillNodeView>();
        private bool _isLoading;

        // 剪贴板相关
        private List<SkillNodeConfig> _clipboardNodes = new List<SkillNodeConfig>();
        private List<EdgeConfig> _clipboardEdges = new List<EdgeConfig>();
        private Vector2 _clipboardCenterPos;

        public event Action OnGraphModified;

        public SkillGraphView(SkillGraphEditorWindow editorWindow)
        {
            _editorWindow = editorWindow;

            // 设置视口 - 使用默认值避免问题
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            // 添加操作器
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            // 延迟添加网格背景到下一帧
            schedule.Execute(() =>
            {
                var grid = new GridBackground();
                Insert(0, grid);
                grid.StretchToParentSize();
                grid.pickingMode = PickingMode.Ignore;
            });

            // 注册上下文菜单
            RegisterCallback<ContextualMenuPopulateEvent>(OnContextualMenu);
            
            // 注册键盘事件处理
            RegisterCallback<KeyDownEvent>(OnKeyDown);

            // 注册鼠标释放事件 - 用于检测连线创建
            RegisterCallback<MouseUpEvent>(OnMouseUp);

            // 通过事件同步连线
            graphViewChanged += OnGraphViewChanged;
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            // 加载期间不触发同步，避免边还未全部加入就被误删
            if (_currentGraph != null && !_isLoading)
                schedule.Execute(() => SyncEdgesWithData()).StartingIn(50);
            return change;
        }

        /// <summary>
        /// 将视图中的连线与数据同步：视图中新连线写入数据，数据中在视图已不存在的连线移除
        /// </summary>
        private void SyncEdgesWithData()
        {
            if (_currentGraph == null) return;

            bool changed = false;

            // 1. 视图中有的连线 → 数据里没有则加入
            foreach (var edge in edges)
            {
                if (edge.output?.node is SkillNodeView fromView && edge.input?.node is SkillNodeView toView)
                {
                    var fromPortId = edge.output.portName;
                    var toPortId = edge.input.portName;
                    var exists = _currentGraph.Edges.Any(e =>
                        e.FromNodeId == fromView.NodeId && e.FromPortId == fromPortId &&
                        e.ToNodeId == toView.NodeId && e.ToPortId == toPortId);
                    if (!exists)
                    {
                        _currentGraph.AddEdge(new EdgeConfig(fromView.NodeId, fromPortId, toView.NodeId, toPortId));
                        changed = true;
                    }
                }
            }

            // 2. 数据里有的连线 → 视图中已不存在（节点被删或线被删）则从数据移除
            var toRemove = _currentGraph.Edges.Where(dataEdge =>
            {
                if (!_nodeViews.TryGetValue(dataEdge.FromNodeId, out var fromNode) ||
                    !_nodeViews.TryGetValue(dataEdge.ToNodeId, out var toNode))
                    return true;
                var fromPort = fromNode.GetOutputPort(dataEdge.FromPortId);
                var toPort = toNode.GetInputPort(dataEdge.ToPortId);
                if (fromPort == null || toPort == null) return true;
                return !edges.Any(e => e.output == fromPort && e.input == toPort);
            }).ToList();
            foreach (var e in toRemove)
            {
                _currentGraph.RemoveEdge(e.FromNodeId, e.ToNodeId);
                changed = true;
            }

            if (changed)
                OnGraphModified?.Invoke();
        }

        // /// <summary>
        // /// 处理连线请求 - 重写此方法拦截连线操作
        // /// </summary>
        // public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        // {
        //     var compatiblePorts = new List<Port>();
        //
        //     ports.ForEach(port =>
        //     {
        //         if (port.node == startPort.node) return;
        //         if (port.direction == startPort.direction) return;
        //         compatiblePorts.Add(port);
        //     });
        //
        //     return compatiblePorts;
        // }

        /// <summary>
        /// 处理鼠标释放事件 - 可能刚完成拖拽连线，同步一次
        /// </summary>
        private void OnMouseUp(MouseUpEvent evt)
        {
            schedule.Execute(() => SyncEdgesWithData()).StartingIn(80);
        }

        /// <summary>
        /// 处理键盘按键
        /// </summary>
        private void OnKeyDown(KeyDownEvent evt)
        {
            // Ctrl+C / Cmd+C 复制选中节点
            if ((evt.keyCode == KeyCode.C && evt.ctrlKey) || (evt.keyCode == KeyCode.C && evt.commandKey))
            {
                CopySelection();
                evt.StopPropagation();
            }
            // Ctrl+V / Cmd+V 粘贴节点
            else if ((evt.keyCode == KeyCode.V && evt.ctrlKey) || (evt.keyCode == KeyCode.V && evt.commandKey))
            {
                PasteNodes();
                evt.StopPropagation();
            }
            // Delete 键或 Backspace 键删除选中元素
            else if (evt.keyCode == KeyCode.Delete || evt.keyCode == KeyCode.Backspace)
            {
                DeleteSelection();
                evt.StopPropagation();
            }
        }

        /// <summary>
        /// 删除选中的元素
        /// </summary>
        public void DeleteSelection()
        {
            var selection = this.selection.ToList();
            foreach (var element in selection)
            {
                if (element is SkillNodeView nodeView)
                {
                    RemoveNodeView(nodeView.NodeId);
                }
                else if (element is Edge edge)
                {
                    RemoveConnection(edge);
                }
            }
        }

        /// <summary>
        /// 复制选中的节点
        /// </summary>
        public void CopySelection()
        {
            _clipboardNodes.Clear();
            _clipboardEdges.Clear();

            var selectedNodeViews = selection.OfType<SkillNodeView>().ToList();
            if (selectedNodeViews.Count == 0) return;

            // 获取所有选中节点
            var selectedNodeIds = new HashSet<string>();
            foreach (var nodeView in selectedNodeViews)
            {
                selectedNodeIds.Add(nodeView.NodeId);
            }

            // 复制节点数据
            foreach (var nodeView in selectedNodeViews)
            {
                var config = nodeView.GetNodeConfig();
                _clipboardNodes.Add(config.Copy());
            }

            // 复制节点间的连线（只复制两端都在选中节点内的连线）
            foreach (var edge in _currentGraph.Edges)
            {
                if (selectedNodeIds.Contains(edge.FromNodeId) && selectedNodeIds.Contains(edge.ToNodeId))
                {
                    _clipboardEdges.Add(edge.Copy());
                }
            }

            // 计算选中节点的中心位置作为粘贴基准
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;
            foreach (var nodeView in selectedNodeViews)
            {
                var pos = nodeView.GetPosition().position;
                minX = Mathf.Min(minX, pos.x);
                minY = Mathf.Min(minY, pos.y);
                maxX = Mathf.Max(maxX, pos.x + NodeGraphStyles.NodeWidth);
                maxY = Mathf.Max(maxY, pos.y + 100);
            }
            _clipboardCenterPos = new Vector2(minX, minY);

            Debug.Log($"[SkillGraphView] 已复制 {_clipboardNodes.Count} 个节点和 {_clipboardEdges.Count} 条连线");
        }

        /// <summary>
        /// 粘贴节点
        /// </summary>
        public void PasteNodes()
        {
            if (_clipboardNodes.Count == 0) return;
            if (_currentGraph == null) return;

            // 创建旧ID到新ID的映射
            var idMapping = new Dictionary<string, string>();

            // 计算当前选中的节点区域中心（如果没有选中则以左上角为基准）
            Vector2 currentCenter;
            var selectedNodeViews = selection.OfType<SkillNodeView>().ToList();
            if (selectedNodeViews.Count > 0)
            {
                float minX = float.MaxValue, minY = float.MaxValue;
                foreach (var nodeView in selectedNodeViews)
                {
                    var pos = nodeView.GetPosition().position;
                    minX = Mathf.Min(minX, pos.x);
                    minY = Mathf.Min(minY, pos.y);
                }
                currentCenter = new Vector2(minX, minY);
            }
            else
            {
                currentCenter = new Vector2(50, 50);
            }

            // 计算原始节点的中心位置
            float origMinX = float.MaxValue, origMinY = float.MaxValue;
            foreach (var config in _clipboardNodes)
            {
                origMinX = Mathf.Min(origMinX, config.EditorPosition.x);
                origMinY = Mathf.Min(origMinY, config.EditorPosition.y);
            }

            // 计算偏移量
            Vector2 offset = currentCenter - new Vector2(origMinX, origMinY);

            // 创建新的节点
            var newNodeViews = new List<SkillNodeView>();
            foreach (var config in _clipboardNodes)
            {
                var newConfig = _currentGraph.CreateNode(config.NodeType, config.EditorPosition + offset);
                newConfig.CopyParamsFrom(config);
                idMapping[config.NodeId] = newConfig.NodeId;

                var nodeView = CreateNodeView(newConfig);
                newNodeViews.Add(nodeView);
            }

            // 创建连线
            foreach (var edge in _clipboardEdges)
            {
                if (idMapping.TryGetValue(edge.FromNodeId, out var newFromId) &&
                    idMapping.TryGetValue(edge.ToNodeId, out var newToId))
                {
                    _currentGraph.AddEdge(new EdgeConfig(newFromId, edge.FromPortId, newToId, edge.ToPortId));
                    // 创建视图连线
                    if (_nodeViews.TryGetValue(newFromId, out var fromNode) &&
                        _nodeViews.TryGetValue(newToId, out var toNode))
                    {
                        var fromPort = fromNode.GetOutputPort(edge.FromPortId);
                        var toPort = toNode.GetInputPort(edge.ToPortId);
                        if (fromPort != null && toPort != null)
                        {
                            var edgeView = new Edge
                            {
                                output = fromPort,
                                input = toPort
                            };
                            fromPort.Connect(edgeView);
                            toPort.Connect(edgeView);
                            AddElement(edgeView);
                        }
                    }
                }
            }

            // 清除选择，选中新粘贴的节点
            ClearSelection();
            foreach (var nodeView in newNodeViews)
            {
                AddToSelection(nodeView);
            }

            OnGraphModified?.Invoke();
            Debug.Log($"[SkillGraphView] 已粘贴 {newNodeViews.Count} 个节点");
        }

        /// <summary>
        /// 加载技能图
        /// </summary>
        public void LoadGraph(SkillGraphConfig graph)
        {
            ClearGraph();
            _currentGraph = graph;

            if (graph == null) return;

            // 加锁期间 graphViewChanged 不会触发 SyncEdgesWithData，避免边还未全部加入就被误删
            _isLoading = true;

            // 创建所有节点视图
            foreach (var node in graph.Nodes)
            {
                CreateNodeView(node);
            }

            // 创建所有连线视图
            foreach (var edge in graph.Edges)
            {
                CreateEdgeView(edge);
            }

            _isLoading = false;
        }

        /// <summary>
        /// 清空图
        /// </summary>
        public void ClearGraph()
        {
            _nodeViews.Clear();
            var elements = graphElements.ToList();
            foreach (var element in elements)
            {
                RemoveElement(element);
            }
            _currentGraph = null;
        }

        /// <summary>
        /// 创建节点视图
        /// </summary>
        public SkillNodeView CreateNodeView(SkillNodeConfig nodeConfig)
        {
            bool portsMissing = nodeConfig.InputPorts.Count == 0;
            nodeConfig.EnsureDefaultPortsIfMissing();
            if (portsMissing && nodeConfig.InputPorts.Count > 0)
            {
                EditorUtility.SetDirty(nodeConfig);
                if (_currentGraph != null)
                    EditorUtility.SetDirty(_currentGraph);
            }

            var nodeView = new SkillNodeView(nodeConfig, this);
            nodeView.SetPosition(new Rect(nodeConfig.EditorPosition, new Vector2(NodeGraphStyles.NodeWidth, 100)));
            _nodeViews[nodeConfig.NodeId] = nodeView;
            AddElement(nodeView);
            return nodeView;
        }

        /// <summary>
        /// 删除节点视图（同时移除与该节点相连的所有连线视图）
        /// </summary>
        public void RemoveNodeView(string nodeId)
        {
            if (!_nodeViews.TryGetValue(nodeId, out var nodeView))
                return;

            // 先移除所有与该节点相连的连线视图，避免出现“幽灵线”
            var edgesToRemove = graphElements.OfType<Edge>().Where(e =>
                e.output?.node == nodeView || e.input?.node == nodeView).ToList();
            foreach (var edge in edgesToRemove)
            {
                if (edge.output != null) edge.output.Disconnect(edge);
                if (edge.input != null) edge.input.Disconnect(edge);
                RemoveElement(edge);
            }

            _nodeViews.Remove(nodeId);
            RemoveElement(nodeView);

            // 使用 DeleteNode 确保子资产也被正确移除（内部会 RemoveNode 并清理 Edges）
            _currentGraph?.DeleteNode(nodeId);
            OnGraphModified?.Invoke();
        }

        /// <summary>
        /// 创建连线视图
        /// </summary>
        private Edge CreateEdgeView(EdgeConfig edgeConfig)
        {
            if (!_nodeViews.TryGetValue(edgeConfig.FromNodeId, out var fromNode))
                return null;

            if (!_nodeViews.TryGetValue(edgeConfig.ToNodeId, out var toNode))
                return null;

            var fromPort = fromNode.GetOutputPort(edgeConfig.FromPortId);
            var toPort = toNode.GetInputPort(edgeConfig.ToPortId);

            if (fromPort == null || toPort == null)
                return null;

            var edge = new Edge
            {
                output = fromPort,
                input = toPort
            };

            fromPort.Connect(edge);
            toPort.Connect(edge);

            AddElement(edge);
            return edge;
        }

        /// <summary>
        /// 创建节点间的连线
        /// </summary>
        public void CreateConnection(Port outputPort, Port inputPort)
        {
            if (_currentGraph == null) return;

            var fromNode = outputPort.node as SkillNodeView;
            var toNode = inputPort.node as SkillNodeView;

            if (fromNode == null || toNode == null) return;

            // 检查是否已存在该连线
            var fromPortId = outputPort.portName;
            var toPortId = inputPort.portName;

            var existingEdge = _currentGraph.Edges.FirstOrDefault(e =>
                e.FromNodeId == fromNode.NodeId &&
                e.FromPortId == fromPortId &&
                e.ToNodeId == toNode.NodeId &&
                e.ToPortId == toPortId);

            if (existingEdge != null) return;

            // 创建数据连线
            _currentGraph.AddEdge(new EdgeConfig(
                fromNode.NodeId,
                fromPortId,
                toNode.NodeId,
                toPortId
            ));

            // 创建视图连线
            var edge = new Edge
            {
                output = outputPort,
                input = inputPort
            };

            outputPort.Connect(edge);
            inputPort.Connect(edge);

            AddElement(edge);
            OnGraphModified?.Invoke();
        }

        /// <summary>
        /// 根据数据配置移除视图中的对应连线（用于从连线列表删除时）
        /// </summary>
        public void RemoveConnectionByConfig(EdgeConfig edgeConfig)
        {
            if (!_nodeViews.TryGetValue(edgeConfig.FromNodeId, out var fromNode) ||
                !_nodeViews.TryGetValue(edgeConfig.ToNodeId, out var toNode))
                return;
            var outputPort = fromNode.GetOutputPort(edgeConfig.FromPortId);
            var inputPort = toNode.GetInputPort(edgeConfig.ToPortId);
            if (outputPort == null || inputPort == null) return;
            var edge = edges.FirstOrDefault(e => e.output == outputPort && e.input == inputPort);
            if (edge != null)
                RemoveConnection(edge);
        }

        /// <summary>
        /// 删除连线
        /// </summary>
        public void RemoveConnection(Edge edge)
        {
            if (_currentGraph == null) return;

            var fromNode = edge.output?.node as SkillNodeView;
            var toNode = edge.input?.node as SkillNodeView;

            if (fromNode != null && toNode != null && !string.IsNullOrEmpty(edge.output?.portName) && !string.IsNullOrEmpty(edge.input?.portName))
            {
                _currentGraph.RemoveEdge(fromNode.NodeId, edge.output.portName, toNode.NodeId, edge.input.portName);
            }

            if (edge.output != null)
                edge.output.Disconnect(edge);
            if (edge.input != null)
                edge.input.Disconnect(edge);

            RemoveElement(edge);
            OnGraphModified?.Invoke();
        }

        /// <summary>
        /// 处理上下文菜单
        /// </summary>
        private void OnContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (_currentGraph == null) return;

            var mousePos = evt.localMousePosition;
            var graphPos = contentViewContainer.WorldToLocal(mousePos);

            evt.menu.AppendAction("添加 Cast 节点", action =>
                CreateNodeAt("Cast", graphPos));

            // 从 NodeTypeRegistry 读取所有节点类型（使用临时变量避免闭包陷阱）
            foreach (var kvp in NodeTypeRegistry.AllNodes)
            {
                var nodeType = kvp.Key;
                evt.menu.AppendAction($"添加 {kvp.Value.DisplayName}", action =>
                    CreateNodeAt(nodeType, graphPos));
            }

            evt.menu.AppendSeparator();

            // 粘贴选项（如果有复制内容）
            if (_clipboardNodes.Count > 0)
            {
                evt.menu.AppendAction($"粘贴 ({_clipboardNodes.Count} 个节点)", action =>
                {
                    PasteNodes();
                });
                evt.menu.AppendSeparator();
            }

            var contextTarget = evt.target as VisualElement;
            evt.menu.AppendAction("Delete", action =>
            {
                DeleteContextTarget(contextTarget);
            });
        }

        private void DeleteContextTarget(VisualElement contextTarget)
        {
            var element = contextTarget;
            while (element != null)
            {
                if (element is SkillNodeView nodeView)
                {
                    RemoveNodeView(nodeView.NodeId);
                    return;
                }

                if (element is Edge edge)
                {
                    RemoveConnection(edge);
                    return;
                }

                element = element.parent;
            }

            DeleteSelection();
        }

        /// <summary>
        /// 在指定位置创建节点
        /// </summary>
        private void CreateNodeAt(string nodeType, Vector2 position)
        {
            if (_currentGraph == null) return;

            var node = _currentGraph.CreateNode(nodeType, position);
            CreateNodeView(node);
            OnGraphModified?.Invoke();
        }

        /// <summary>
        /// 获取兼容的端口
        /// </summary>
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();

            ports.ForEach(port =>
            {
                // 不能连接到自己
                if (port.node == startPort.node) return;

                // 不能连接同方向的端口
                if (port.direction == startPort.direction) return;

                compatiblePorts.Add(port);
            });

            return compatiblePorts;
        }

        /// <summary>
        /// 通知图被修改
        /// </summary>
        public void NotifyGraphModified()
        {
            OnGraphModified?.Invoke();
        }
    }
}
#endif
