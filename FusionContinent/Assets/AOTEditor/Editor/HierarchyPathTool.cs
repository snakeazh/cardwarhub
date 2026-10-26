using System.Text;
using UnityEditor;
using UnityEngine;

namespace GuideFramework.EditorTools
{
    internal static class HierarchyPathTool
    {
        // Tools 菜单：打印所有选中对象的层级路径，并复制到剪贴板
        [MenuItem("Tools/Hierarchy/Print Selected Path %#p")] // Ctrl+Shift+P
        private static void PrintSelectedPaths()
        {
            var gos = Selection.gameObjects;
            if (gos == null || gos.Length == 0)
            {
                Debug.Log("[HierarchyPath] 未选中任何对象。");
                return;
            }

            var sb = new StringBuilder();
            foreach (var go in gos)
            {
                var path = BuildHierarchyPath(go.transform);
                var sceneName = go.scene.IsValid() ? go.scene.name : "<NoScene>";
                var line = $"{sceneName}:{path}";
                sb.AppendLine(line);
                Debug.Log($"[HierarchyPath] {line}");
            }

            EditorGUIUtility.systemCopyBuffer = sb.ToString();
        }

        // GameObject 菜单：在层级右键复制单个对象路径
        [MenuItem("GameObject/Copy Hierarchy Path", false, 49)]
        private static void CopySingleHierarchyPath()
        {
            var go = Selection.activeGameObject;
            if (go == null)
            {
                Debug.Log("[HierarchyPath] 未选中对象。");
                return;
            }
            var path = BuildHierarchyPath(go.transform);
            var sceneName = go.scene.IsValid() ? go.scene.name : "<NoScene>";
            var line = $"{sceneName}:{path}";
            EditorGUIUtility.systemCopyBuffer = line;
            Debug.Log($"[HierarchyPath] {line}");
        }

        // 仅当有选中对象时才启用 GameObject 菜单项
        [MenuItem("GameObject/Copy Hierarchy Path", true)]
        private static bool CopySingleHierarchyPath_Validate()
        {
            return Selection.activeGameObject != null;
        }

        private static string BuildHierarchyPath(Transform t)
        {
            if (t == null) return string.Empty;
            var stack = new System.Collections.Generic.Stack<string>();
            var cur = t;
            while (cur != null)
            {
                stack.Push(cur.name);
                cur = cur.parent;
            }

            var sb = new StringBuilder();
            var first = true;
            foreach (var name in stack)
            {
                if (!first) sb.Append('/');
                sb.Append(name);
                first = false;
            }
            return sb.ToString();
        }
    }
}