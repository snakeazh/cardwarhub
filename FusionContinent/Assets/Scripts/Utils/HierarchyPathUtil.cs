namespace FusionContinent.Utils
{
    using System.Collections.Generic;
    using UnityEngine;

// 通用：获取 Transform/Component/GameObject 的层级路径（Root/Parent/Child）
    public static class HierarchyPathUtil
    {
        // 获取 Transform 的完整路径；可选传入 stopAt 作为相对起点
        public static string GetPath(Transform t, Transform stopAt = null)
        {
            if (t == null) return string.Empty;
            var stack = new Stack<string>();
            var current = t;
            while (current != null && current != stopAt)
            {
                stack.Push(current.name);
                current = current.parent;
            }
            return string.Join("/", stack.ToArray());
        }

        // GameObject 重载
        public static string GetPath(GameObject go, Transform stopAt = null)
        {
            return go == null ? string.Empty : GetPath(go.transform, stopAt);
        }

        // Component 重载
        public static string GetPath(Component comp, Transform stopAt = null)
        {
            return comp == null ? string.Empty : GetPath(comp.transform, stopAt);
        }

        // 打印路径到控制台
        public static void LogPath(Object obj, Transform stopAt = null)
        {
            switch (obj)
            {
                case Transform t:
                    LogTool.Log(GetPath(t, stopAt));
                    break;
                case GameObject go:
                    LogTool.Log(GetPath(go, stopAt));
                    break;
                case Component comp:
                    LogTool.Log(GetPath(comp, stopAt));
                    break;
                default:
                    LogTool.LogWarning("不支持的对象类型，需为 Transform/GameObject/Component。");
                    break;
            }
        }
    }
}