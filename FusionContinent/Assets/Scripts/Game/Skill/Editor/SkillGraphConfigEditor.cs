#if UNITY_EDITOR
using FusionContinent.Framework.Graph;
using FusionContinent.Skill.Config;
using UnityEditor;
using UnityEngine;

namespace FusionContinent.Skill.Editor
{
    /// <summary>
    /// 技能图配置的自定义检视面板
    /// </summary>
    [CustomEditor(typeof(SkillGraphConfig))]
    public class SkillGraphConfigEditor : UnityEditor.Editor
    {
        private SkillGraphConfig _target;

        private void OnEnable()
        {
            _target = target as SkillGraphConfig;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // 绘制基本信息
            EditorGUILayout.LabelField("技能图配置", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 基本信息 - 可编辑字段
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_graphName"), new GUIContent("图名称"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_skillId"), new GUIContent("技能ID"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(_target);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("节点数量", _target.Nodes.Count.ToString());
            EditorGUILayout.LabelField("连线数量", _target.Edges.Count.ToString());

            EditorGUILayout.Space();

            // 打开编辑器按钮
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("打开可视化编辑器", GUILayout.Height(35)))
            {
                SkillGraphEditorWindow.OpenWindow(_target);
            }

            if (GUILayout.Button("在 Inspector 中编辑", GUILayout.Height(35), GUILayout.Width(150)))
            {
                // 展开节点编辑
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // 节点列表
            EditorGUILayout.LabelField("节点列表", EditorStyles.boldLabel);

            if (_target.Nodes.Count == 0)
            {
                EditorGUILayout.HelpBox("没有节点，请打开可视化编辑器添加节点", MessageType.Info);
            }
            else
            {
                foreach (var node in _target.Nodes)
                {
                    EditorGUILayout.BeginHorizontal("box");
                    EditorGUILayout.LabelField($"[{node.NodeType}]", GUILayout.Width(100));
                    EditorGUILayout.LabelField($"ID: {node.NodeId.Substring(0, 8)}...", GUILayout.Width(120));
                    if (GUILayout.Button("编辑", GUILayout.Width(50)))
                    {
                        SkillNodeEditorWindow.OpenWindow(node);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.Space();

            // 快捷操作
            EditorGUILayout.LabelField("快捷操作", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("创建火球术"))
            {
                ApplyTestData("FireBall", FusionContinent.Skill.SkillGraphTestData.CreateFireBallSkill);
            }

            if (GUILayout.Button("创建闪电链"))
            {
                ApplyTestData("LightningChain", FusionContinent.Skill.SkillGraphTestData.CreateLightningChainSkill);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("创建瞬袭斩"))
            {
                ApplyTestData("SwiftAssault", FusionContinent.Skill.SkillGraphTestData.CreateSwiftAssaultSkill);
            }

            if (GUILayout.Button("创建多重斩"))
            {
                ApplyTestData("MultiSlash", FusionContinent.Skill.SkillGraphTestData.CreateMultiSlashSkill);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("创建爆炎斩"))
            {
                ApplyTestData("FlamingBurst", FusionContinent.Skill.SkillGraphTestData.CreateFlamingBurstSkill);
            }

            if (GUILayout.Button("创建条件技能"))
            {
                ApplyTestData("ConditionalSkill", FusionContinent.Skill.SkillGraphTestData.CreateConditionalSkill);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void ApplyTestData(string graphName, System.Func<SkillGraphConfig> generator)
        {
            _target.Clear();

            var testData = generator();

            var idMap = new System.Collections.Generic.Dictionary<string, SkillNodeConfig>();

            foreach (var node in testData.Nodes)
            {
                AssetDatabase.RemoveObjectFromAsset(node);
                _target.AddNode(node);
                idMap[node.NodeId] = node;
            }

            foreach (var edge in testData.Edges)
            {
                if (idMap.TryGetValue(edge.FromNodeId, out var fromNode) &&
                    idMap.TryGetValue(edge.ToNodeId, out var toNode))
                {
                    _target.AddEdge(new EdgeConfig(fromNode.NodeId, edge.FromPortId, toNode.NodeId, edge.ToPortId));
                }
            }

            var so = new SerializedObject(_target);
            var graphNameProp = so.FindProperty("_graphName");
            if (graphNameProp != null)
            {
                graphNameProp.stringValue = graphName;
                so.ApplyModifiedProperties();
            }

            EditorUtility.SetDirty(_target);
            AssetDatabase.SaveAssets();

            LogTool.Log($"[SkillGraphConfigEditor] 已创建 {graphName} 测试技能");
        }

        private void CreateFireBallSkill()
        {
            _target.Clear();

            // 从 SkillGraphTestData 复制节点和边
            var testData = FusionContinent.Skill.SkillGraphTestData.CreateFireBallSkill();

            // 建立旧节点ID → 新节点实例的映射
            var idMap = new System.Collections.Generic.Dictionary<string, SkillNodeConfig>();

            foreach (var node in testData.Nodes)
            {
                AssetDatabase.RemoveObjectFromAsset(node);
                _target.AddNode(node);
                idMap[node.NodeId] = node;
            }

            // 用新节点ID重建边（避免旧节点ID已失效导致边找不到节点）
            foreach (var edge in testData.Edges)
            {
                if (idMap.TryGetValue(edge.FromNodeId, out var fromNode) &&
                    idMap.TryGetValue(edge.ToNodeId, out var toNode))
                {
                    _target.AddEdge(new EdgeConfig(fromNode.NodeId, edge.FromPortId, toNode.NodeId, edge.ToPortId));
                }
            }

            var so = new SerializedObject(_target);
            var graphNameProp = so.FindProperty("_graphName");
            if (graphNameProp != null)
            {
                graphNameProp.stringValue = "FireBall";
                so.ApplyModifiedProperties();
            }

            EditorUtility.SetDirty(_target);
            AssetDatabase.SaveAssets();

            LogTool.Log("[SkillGraphConfigEditor] 已创建火球术测试技能");
        }
    }
}
#endif
