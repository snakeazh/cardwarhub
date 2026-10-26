using FusionContinent.UI.Components;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 将选中预制体内的 Button 组件批量替换为 UIPointerClickAndLongPress。
/// 复制过渡/导航/交互/onClick 等序列化数据，并把预制体内部对旧 Button 的引用重定向到新组件。
/// 用法：Project 窗口选中一个或多个预制体 → Tools → UI → 替换选中预制体的 Button。
/// 操作会直写预制体资产，执行前请先提交 git 以便回滚。
/// </summary>
public static class ButtonReplaceTool
{
    [MenuItem("Tools/UI/替换选中预制体的 Button → UIPointerClickAndLongPress")]
    private static void ReplaceSelectedPrefabs()
    {
        var guids = Selection.assetGUIDs;
        if (guids == null || guids.Length == 0)
        {
            LogTool.LogWarning("[ButtonReplaceTool] 请先在 Project 窗口选中预制体");
            return;
        }

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".prefab"))
            {
                continue;
            }

            ReplaceInPrefab(path);
        }

        AssetDatabase.SaveAssets();
    }

    private static void ReplaceInPrefab(string path)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var buttons = root.GetComponentsInChildren<Button>(true);
            int replaced = 0;
            int remapped = 0;

            foreach (var oldButton in buttons)
            {
                var go = oldButton.gameObject;

                // UIButtonClickSoundListener 依赖 Button（RequireComponent），随替换一并删除
                var soundListener = go.GetComponent<FusionContinent.Sounds.UIButtonClickSoundListener>();
                if (soundListener != null)
                {
                    Object.DestroyImmediate(soundListener);
                }

                // 一个 GameObject 只能有一个 Selectable，必须先销毁旧 Button 再添加新组件。
                // 先缓存组件数据与实例 ID（销毁后引用对比要用 objectReferenceInstanceIDValue）。
                int oldButtonId = oldButton.GetInstanceID();
                UnityEditorInternal.ComponentUtility.CopyComponent(oldButton);
                Object.DestroyImmediate(oldButton);

                // 同一物体上不允许同时存在两个可点击组件，已挂目标组件的复用它
                var newComp = go.GetComponent<UIPointerClickAndLongPress>();
                if (newComp == null)
                {
                    newComp = go.AddComponent<UIPointerClickAndLongPress>();
                    UnityEditorInternal.ComponentUtility.PasteComponentValues(newComp);
                }

                remapped += RemapReferences(root, oldButtonId, newComp);
                replaced++;
            }

            if (replaced > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }

            LogTool.Log($"[ButtonReplaceTool] {path}: 替换 {replaced} 个 Button，重定向 {remapped} 处引用");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    /// <summary>
    /// 遍历预制体内所有组件的序列化属性，把指向旧 Button 的引用改指向新组件。
    /// 覆盖 Designer 面板字段、业务组件字段以及其他组件 onClick 持久监听里的目标引用。
    /// </summary>
    private static int RemapReferences(GameObject root, int oldButtonId, UIPointerClickAndLongPress newComp)
    {
        int count = 0;
        var allComponents = root.GetComponentsInChildren<Component>(true);
        foreach (var comp in allComponents)
        {
            if (comp == null || comp == newComp)
            {
                continue;
            }

            if (comp is Transform)
            {
                continue;
            }

            var so = new SerializedObject(comp);
            var prop = so.GetIterator();
            bool enterChildren = true;
            bool modified = false;
            while (prop.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (prop.propertyType != SerializedPropertyType.ObjectReference)
                {
                    continue;
                }

                if (prop.objectReferenceInstanceIDValue != oldButtonId)
                {
                    continue;
                }

                prop.objectReferenceValue = newComp;
                modified = true;
                count++;
            }

            if (modified)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        return count;
    }
}
