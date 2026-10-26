using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

public static class SpriteAtlasIncludeInBuildTool
{
    [MenuItem("Assets/Sprite Atlas/Set Include In Build (Selected) %#i", false, 2004)]
    private static void SetIncludeInBuildSelected()
    {
        var atlases = GetSelectedAtlases();
        if (atlases.Count == 0)
        {
            EditorUtility.DisplayDialog("Sprite Atlas", "未选中任何 SpriteAtlas（.spriteatlas）资源。", "确定");
            return;
        }

        var changed = 0;
        for (var i = 0; i < atlases.Count; i++)
        {
            var atlas = atlases[i];
            if (atlas == null)
            {
                continue;
            }

            if (!atlas.isVariant)
            {
                // 仅标记主 atlas；variant 的 includeInBuild 行为可能依赖 master
            }

            // 兼容：部分 Unity 版本 SetIncludeInBuild(true) 不会驱动 Inspector 勾选变化，直接改序列化字段更稳
            var so = new SerializedObject(atlas);
            var includeProp = so.FindProperty("m_IncludeInBuild") ?? so.FindProperty("includeInBuild");
            if (includeProp != null && includeProp.propertyType == SerializedPropertyType.Boolean)
            {
                if (!includeProp.boolValue)
                {
                    includeProp.boolValue = true;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(atlas);
                    changed++;
                }
            }
            else
            {
                // 回退：至少确保逻辑上设置为 true
                atlas.SetIncludeInBuild(true);
                EditorUtility.SetDirty(atlas);
                changed++;
            }
        }

        if (changed > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        var msg = $"完成。\n\n选中图集: {atlases.Count}\n实际变更: {changed}\n快捷键: Ctrl+Shift+I";
        Debug.Log($"[SpriteAtlasIncludeInBuildTool] Selected={atlases.Count}, Changed={changed}");
        EditorUtility.DisplayDialog("Sprite Atlas", msg, "确定");
    }

    [MenuItem("Assets/Sprite Atlas/Set Include In Build (Selected) %#i", true)]
    private static bool ValidateSetIncludeInBuildSelected()
    {
        return GetSelectedAtlases().Count > 0;
    }

    private static List<SpriteAtlas> GetSelectedAtlases()
    {
        var result = new List<SpriteAtlas>();
        var objs = Selection.objects;
        if (objs == null || objs.Length == 0)
        {
            return result;
        }

        for (var i = 0; i < objs.Length; i++)
        {
            if (objs[i] is SpriteAtlas atlas)
            {
                result.Add(atlas);
            }
        }

        return result;
    }
}

