#if UNITY_EDITOR
using FusionContinent.Skill.Config;
using UnityEditor;
using UnityEngine;

namespace FusionContinent.Skill.Editor
{
    /// <summary>
    /// 技能节点图编辑器的菜单和便捷工具
    /// </summary>
    public static class SkillGraphMenu
    {
        /// <summary>
        /// 创建新的技能图
        /// </summary>
        [MenuItem("Assets/Create/FusionContinent/Skill Graph", false, 100)]
        public static void CreateSkillGraph()
        {
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);

            if (string.IsNullOrEmpty(path))
            {
                path = "Assets";
            }
            else if (!AssetDatabase.IsValidFolder(path))
            {
                path = System.IO.Path.GetDirectoryName(path);
            }

            var assetPath = AssetDatabase.GenerateUniqueAssetPath($"{path}/NewSkillGraph.asset");

            var graph = ScriptableObject.CreateInstance<SkillGraphConfig>();

            AssetDatabase.CreateAsset(graph, assetPath);
            AssetDatabase.SaveAssets();

            // 设置图名称
            var so = new SerializedObject(graph);
            var graphNameProp = so.FindProperty("_graphName");
            if (graphNameProp != null)
            {
                graphNameProp.stringValue = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                so.ApplyModifiedProperties();
            }

            Selection.activeObject = graph;
            EditorGUIUtility.PingObject(graph);

            LogTool.Log($"[SkillGraphMenu] 已创建技能图: {assetPath}");
        }

        /// <summary>
        /// 打开技能图的编辑器
        /// </summary>
        [MenuItem("Assets/Open Skill Graph Editor", true)]
        public static bool OpenSkillGraphValidate()
        {
            return Selection.activeObject is SkillGraphConfig;
        }

        [MenuItem("Assets/Open Skill Graph Editor", false, 200)]
        public static void OpenSkillGraph()
        {
            if (Selection.activeObject is SkillGraphConfig graph)
            {
                SkillGraphEditorWindow.OpenWindow(graph);
            }
        }

        /// <summary>
        /// 从选中对象创建测试技能图
        /// </summary>
        [MenuItem("CONTEXT/SkillGraphConfig/Create Fire Ball Skill")]
        public static void CreateFireBallFromContext(MenuCommand command)
        {
            if (command.context is SkillGraphConfig graph)
            {
                graph.Clear();

                var testData = FusionContinent.Skill.SkillGraphTestData.CreateFireBallSkill();

                foreach (var node in testData.Nodes)
                {
                    graph.AddNode(node);
                }

                foreach (var edge in testData.Edges)
                {
                    graph.AddEdge(edge);
                }

                EditorUtility.SetDirty(graph);
                AssetDatabase.SaveAssets();

                LogTool.Log("[SkillGraphMenu] 已创建火球术测试技能");
            }
        }

        [MenuItem("CONTEXT/SkillGraphConfig/Create Lightning Chain Skill")]
        public static void CreateLightningChainFromContext(MenuCommand command)
        {
            if (command.context is SkillGraphConfig graph)
            {
                graph.Clear();

                var testData = FusionContinent.Skill.SkillGraphTestData.CreateLightningChainSkill();

                foreach (var node in testData.Nodes)
                {
                    graph.AddNode(node);
                }

                foreach (var edge in testData.Edges)
                {
                    graph.AddEdge(edge);
                }

                EditorUtility.SetDirty(graph);
                AssetDatabase.SaveAssets();

                LogTool.Log("[SkillGraphMenu] 已创建闪电链测试技能");
            }
        }

        [MenuItem("CONTEXT/SkillGraphConfig/Clear Graph")]
        public static void ClearGraphFromContext(MenuCommand command)
        {
            if (command.context is SkillGraphConfig graph)
            {
                if (EditorUtility.DisplayDialog("确认清空", "确定要清空当前技能图吗？", "确定", "取消"))
                {
                    graph.Clear();
                    EditorUtility.SetDirty(graph);
                    AssetDatabase.SaveAssets();
                    LogTool.Log("[SkillGraphMenu] 已清空技能图");
                }
            }
        }
    }
}
#endif
