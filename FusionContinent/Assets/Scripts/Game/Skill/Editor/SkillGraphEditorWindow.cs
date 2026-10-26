#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using FusionContinent.Framework.Graph;
using FusionContinent.Skill.Config;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FusionContinent.Skill.Editor
{
    /// <summary>
    /// 技能节点图编辑器窗口
    /// </summary>
    public class SkillGraphEditorWindow : EditorWindow
    {
        private SkillGraphView _graphView;
        private SkillGraphConfig _currentGraph;
        private ObjectField _graphSelector;
        private Label _graphNameLabel;
        private VisualElement _edgeListContainer;

        private const float ToolbarHeight = 25f;
        private const float SidebarWidth = 200f;
        private const string LastGraphGuidPrefsKey = "SkillGraphEditor_LastGraphGuid";

        [MenuItem("FusionContinent/Skill Graph Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<SkillGraphEditorWindow>();
            window.titleContent = new GUIContent("技能节点图编辑器");
            window.minSize = new Vector2(800, 600);
        }

        /// <summary>
        /// 打开指定技能图的编辑器
        /// </summary>
        public static void OpenWindow(SkillGraphConfig graph)
        {
            var window = GetWindow<SkillGraphEditorWindow>();
            window.titleContent = new GUIContent("技能节点图编辑器");
            window.minSize = new Vector2(800, 600);
            window.LoadGraph(graph);
        }

        private void OnEnable()
        {
            CreateUI();

            // 启动时自动加载上次打开的图
            RestoreLastGraph();
        }

        private void RestoreLastGraph()
        {
            var guid = EditorPrefs.GetString(LastGraphGuidPrefsKey, "");
            if (!string.IsNullOrEmpty(guid))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path))
                {
                    var graph = AssetDatabase.LoadAssetAtPath<SkillGraphConfig>(path);
                    if (graph != null)
                        LoadGraph(graph);
                }
            }
        }

        private void SaveLastGraphGuid()
        {
            if (_currentGraph != null)
            {
                var path = AssetDatabase.GetAssetPath(_currentGraph);
                if (!string.IsNullOrEmpty(path))
                {
                    var guid = AssetDatabase.AssetPathToGUID(path);
                    EditorPrefs.SetString(LastGraphGuidPrefsKey, guid);
                }
            }
        }

        private void OnDisable()
        {
            if (_graphView != null)
            {
                _graphView.OnGraphModified -= OnGraphModified;
            }
        }

        private void CreateUI()
        {
            rootVisualElement.Clear();

            // 创建主容器
            var mainContainer = new VisualElement();
            mainContainer.style.flexDirection = FlexDirection.Column;
            mainContainer.style.flexGrow = 1;
            rootVisualElement.Add(mainContainer);

            // 创建工具栏
            CreateToolbar(mainContainer);

            // 创建内容区域
            var contentContainer = new VisualElement();
            contentContainer.style.flexDirection = FlexDirection.Row;
            contentContainer.style.flexGrow = 1;
            mainContainer.Add(contentContainer);

            // 创建节点选择面板
            CreateNodePalette(contentContainer);

            // 创建连线列表面板
            CreateEdgeListPanel(contentContainer);

            // 创建图视图
            CreateGraphView(contentContainer);

            // 应用样式
            ApplyStyles();
        }

        private void CreateToolbar(VisualElement parent)
        {
            var toolbar = new Toolbar();
            toolbar.style.height = ToolbarHeight;
            toolbar.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);

            // 图选择器
            var selectorLabel = new Label("技能图:");
            selectorLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            selectorLabel.style.marginLeft = 5;
            selectorLabel.style.width = 50;
            toolbar.Add(selectorLabel);

            _graphSelector = new ObjectField();
            _graphSelector.objectType = typeof(SkillGraphConfig);
            _graphSelector.style.width = 200;
            _graphSelector.RegisterValueChangedCallback(evt =>
            {
                LoadGraph(evt.newValue as SkillGraphConfig);
            });
            toolbar.Add(_graphSelector);

            // 图名称标签
            _graphNameLabel = new Label("未加载");
            _graphNameLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _graphNameLabel.style.flexGrow = 1;
            toolbar.Add(_graphNameLabel);

            // 新建按钮
            var newButton = new ToolbarButton(() => CreateNewGraph()) { text = "新建" };
            toolbar.Add(newButton);

            // 保存按钮
            var saveButton = new ToolbarButton(() => SaveGraph()) { text = "保存" };
            toolbar.Add(saveButton);

            // 添加节点按钮
            var addButton = new ToolbarButton(() => ShowNodeCreationMenu()) { text = "添加节点" };
            toolbar.Add(addButton);

            // 清空按钮
            var clearButton = new ToolbarButton(() => ClearGraph()) { text = "清空" };
            toolbar.Add(clearButton);

            parent.Add(toolbar);
        }

        private void CreateNodePalette(VisualElement parent)
        {
            var sidebar = new VisualElement();
            sidebar.style.width = SidebarWidth;
            sidebar.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
            sidebar.style.borderRightWidth = 1;
            sidebar.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);

            // 标题
            var title = new Label("节点类型");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.paddingLeft = 10;
            title.style.paddingTop = 10;
            title.style.paddingBottom = 5;
            sidebar.Add(title);

            // 从 NodeTypeRegistry 读取所有节点类型，按分类分组显示
            foreach (var category in System.Enum.GetValues(typeof(NodeTypeRegistry.NodeCategory)))
            {
                var cat = (NodeTypeRegistry.NodeCategory)category;

                // 分类标题
                var catLabel = new Label($"--- {cat} ---");
                catLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                catLabel.style.fontSize = 10;
                catLabel.style.color = NodeTypeRegistry.GetCategoryColor(cat);
                catLabel.style.paddingLeft = 5;
                catLabel.style.paddingTop = 8;
                sidebar.Add(catLabel);

                // 该分类下的节点按钮
                foreach (var kvp in NodeTypeRegistry.AllNodes)
                {
                    if (NodeTypeRegistry.GetCategory(kvp.Key) != cat)
                        continue;

                    var nodeType = kvp.Key;
                    var button = new Button(() => CreateNode(nodeType))
                    {
                        text = kvp.Value.DisplayName
                    };
                    button.style.marginLeft = 5;
                    button.style.marginRight = 5;
                    button.style.marginTop = 2;
                    button.style.marginBottom = 2;
                    button.style.unityTextAlign = TextAnchor.MiddleLeft;

                    var color = NodeTypeRegistry.GetNodeColor(nodeType);
                    button.style.borderTopColor = color;
                    button.style.borderBottomColor = color;
                    button.style.borderLeftColor = color;
                    button.style.borderRightColor = color;

                    sidebar.Add(button);
                }
            }

            parent.Add(sidebar);
        }

        private void CreateEdgeListPanel(VisualElement parent)
        {
            var panel = new VisualElement();
            panel.style.width = 180f;
            panel.style.height = Length.Percent(100);
            panel.style.backgroundColor = new Color(0.12f, 0.12f, 0.12f);
            panel.style.borderRightWidth = 1;
            panel.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);

            // 滚动视图包含标题和列表
            var scrollView = new UnityEngine.UIElements.ScrollView(ScrollViewMode.Vertical);
            scrollView.style.flexGrow = 1;
            scrollView.style.width = Length.Percent(100);

            // 标题
            var title = new Label("连线列表");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.paddingLeft = 10;
            title.style.paddingTop = 10;
            title.style.paddingBottom = 5;
            title.style.color = new Color(0.9f, 0.7f, 0.3f);
            scrollView.Add(title);

            // 连线列表容器
            _edgeListContainer = new VisualElement();
            _edgeListContainer.style.paddingLeft = 5;
            _edgeListContainer.style.paddingRight = 5;
            _edgeListContainer.style.paddingBottom = 10;
            scrollView.Add(_edgeListContainer);

            panel.Add(scrollView);
            parent.Add(panel);
        }

        /// <summary>
        /// 刷新连线列表显示
        /// </summary>
        private void RefreshEdgeList()
        {
            _edgeListContainer?.Clear();

            if (_currentGraph == null) return;

            if (_currentGraph.Edges.Count == 0)
            {
                var emptyLabel = new Label("(无连线)");
                emptyLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
                emptyLabel.style.fontSize = 11;
                emptyLabel.style.paddingLeft = 5;
                emptyLabel.style.paddingTop = 5;
                _edgeListContainer.Add(emptyLabel);
                return;
            }

            foreach (var edge in _currentGraph.Edges)
            {
                var edgeItem = CreateEdgeListItem(edge);
                _edgeListContainer.Add(edgeItem);
            }
        }

        private VisualElement CreateEdgeListItem(EdgeConfig edge)
        {
            var item = new VisualElement();
            item.style.flexDirection = FlexDirection.Column;
            item.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            item.style.marginBottom = 3;
            item.style.paddingTop = 4;
            item.style.paddingBottom = 4;
            item.style.paddingLeft = 5;
            item.style.paddingRight = 5;
            item.style.borderTopLeftRadius = 3;
            item.style.borderTopRightRadius = 3;
            item.style.borderBottomLeftRadius = 3;
            item.style.borderBottomRightRadius = 3;

            // 获取节点名称
            var fromNodeName = GetNodeDisplayName(edge.FromNodeId);
            var toNodeName = GetNodeDisplayName(edge.ToNodeId);

            // 第一行：From -> To
            var line1 = new Label($"{fromNodeName} → {toNodeName}");
            line1.style.color = new Color(0.3f, 0.8f, 0.3f);
            line1.style.fontSize = 10;
            item.Add(line1);

            // 第二行：端口信息
            var line2 = new Label($"  [{edge.FromPortId}] → [{edge.ToPortId}]");
            line2.style.color = new Color(0.5f, 0.5f, 0.5f);
            line2.style.fontSize = 9;
            item.Add(line2);

            // 删除按钮
            var deleteBtn = new Button(() => DeleteEdge(edge))
            {
                text = "删除"
            };
            deleteBtn.style.marginTop = 3;
            deleteBtn.style.backgroundColor = new Color(0.6f, 0.2f, 0.2f);
            deleteBtn.style.color = Color.white;
            deleteBtn.style.fontSize = 10;
            item.Add(deleteBtn);

            return item;
        }

        private string GetNodeDisplayName(string nodeId)
        {
            if (_currentGraph == null) return nodeId;

            var node = _currentGraph.Nodes.FirstOrDefault(n => n.NodeId == nodeId);
            return node != null ? $"{node.NodeType}" : nodeId;
        }

        private void DeleteEdge(EdgeConfig edge)
        {
            if (_currentGraph == null) return;

            _currentGraph.RemoveEdge(edge.FromNodeId, edge.FromPortId, edge.ToNodeId, edge.ToPortId);
            RefreshEdgeList();
            OnGraphModified();

            // 从视图中移除该连线
            _graphView.RemoveConnectionByConfig(edge);
        }

        private void CreateGraphView(VisualElement parent)
        {
            _graphView = new SkillGraphView(this);
            _graphView.style.flexGrow = 1;
            _graphView.OnGraphModified += OnGraphModified;
            parent.Add(_graphView);
        }

        private void ApplyStyles()
        {
            rootVisualElement.style.backgroundColor = NodeGraphStyles.GridBackgroundColor;
        }

        /// <summary>
        /// 加载技能图
        /// </summary>
        public void LoadGraph(SkillGraphConfig graph)
        {
            _currentGraph = graph;

            if (_graphSelector != null)
                _graphSelector.SetValueWithoutNotify(graph);

            if (graph != null)
            {
                _graphNameLabel.text = graph.name;
                SaveLastGraphGuid();

                if (_graphView != null)
                {
                    _graphView.LoadGraph(graph);
                }
                RefreshEdgeList();
            }
            else
            {
                _graphNameLabel.text = "未加载";
                if (_graphView != null)
                {
                    _graphView.ClearGraph();
                }
                RefreshEdgeList();
            }
        }

        /// <summary>
        /// 创建新技能图
        /// </summary>
        private void CreateNewGraph()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "创建新技能图",
                "NewSkillGraph",
                "asset",
                "请输入技能图名称"
            );

            if (string.IsNullOrEmpty(path)) return;

            var graph = CreateInstance<SkillGraphConfig>();

            // 设置图名称
            var so = new SerializedObject(graph);
            var graphNameProp = so.FindProperty("_graphName");
            if (graphNameProp != null)
            {
                graphNameProp.stringValue = System.IO.Path.GetFileNameWithoutExtension(path);
                so.ApplyModifiedProperties();
            }

            AssetDatabase.CreateAsset(graph, path);
            AssetDatabase.SaveAssets();

            LoadGraph(graph);
        }

        /// <summary>
        /// 保存当前技能图
        /// </summary>
        private void SaveGraph()
        {
            if (_currentGraph == null)
            {
                EditorUtility.DisplayDialog("提示", "请先加载或创建技能图", "确定");
                return;
            }

            EditorUtility.SetDirty(_currentGraph);
            AssetDatabase.SaveAssets();
            LogTool.Log($"[SkillGraphEditor] 已保存: {_currentGraph.name}");
        }

        /// <summary>
        /// 显示节点创建菜单
        /// </summary>
        private void ShowNodeCreationMenu()
        {
            if (_currentGraph == null)
            {
                EditorUtility.DisplayDialog("提示", "请先加载或创建技能图", "确定");
                return;
            }

            var menu = new GenericMenu();

            foreach (var category in System.Enum.GetValues(typeof(NodeTypeRegistry.NodeCategory)))
            {
                var cat = (NodeTypeRegistry.NodeCategory)category;
                foreach (var (nodeType, displayName) in NodeTypeRegistry.GetAllNodeTypes())
                {
                    if (NodeTypeRegistry.GetCategory(nodeType) != cat)
                        continue;

                    var type = nodeType;
                    menu.AddItem(new GUIContent($"{cat}/{displayName}"), false, () => CreateNode(type));
                }
            }

            menu.ShowAsContext();
        }

        /// <summary>
        /// 在图中央创建新节点
        /// </summary>
        private void CreateNode(string nodeType)
        {
            if (_currentGraph == null) return;

            // 在视口中央创建
            var viewCenter = _graphView.contentViewContainer.WorldToLocal(
                new Vector2(_graphView.layout.width / 2, _graphView.layout.height / 2)
            );

            var node = _currentGraph.CreateNode(nodeType, viewCenter);
            _graphView.CreateNodeView(node);
        }

        /// <summary>
        /// 清空当前图
        /// </summary>
        private void ClearGraph()
        {
            if (_currentGraph == null) return;

            if (EditorUtility.DisplayDialog("确认清空", "确定要清空当前技能图吗？", "确定", "取消"))
            {
                _currentGraph.Clear();
                _graphView.ClearGraph();
            }
        }

        private void OnGraphModified()
        {
            if (_currentGraph != null)
            {
                EditorUtility.SetDirty(_currentGraph);
                RefreshEdgeList();
            }
        }

        /// <summary>
        /// 获取当前加载的图
        /// </summary>
        public SkillGraphConfig GetCurrentGraph()
        {
            return _currentGraph;
        }

        /// <summary>
        /// 获取图视图
        /// </summary>
        public SkillGraphView GetGraphView()
        {
            return _graphView;
        }
    }
}
#endif
