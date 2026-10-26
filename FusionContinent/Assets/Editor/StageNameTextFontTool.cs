using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// 从 StageBattleConfig.StageName 收集字符，基于 ZhuoTeZiYouTi 生成/更新
/// Assets/Fonts/StageNameText.asset，并写回主界面 StageNameText。
/// </summary>
public static class StageNameTextFontTool
{
    private const string SourceFontPath = "Assets/Fonts/ZhuoTeZiYouTi.otf";
    private const string TargetFontPath = "Assets/Res/Fonts/StageNameText.asset";
    private const string StageConfigPath = "Assets/Res/Config/Json/tbstagebattleconfig.json";
    private const string PrefabPath = "Assets/Res/UIPrefab/MainInterface/MainInterfacePanel.prefab";
    private const string StageNameTextNodeName = "StageNameText";

    private const int SamplingPointSize = 108;
    private const int AtlasPadding = 9;
    private const int AtlasWidth = 1024;
    private const int AtlasHeight = 1024;

    [MenuItem("Tools/Fonts/生成 StageNameText 字体并应用到主界面")]
    private static void GenerateAndApply()
    {
        var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        if (sourceFont == null)
        {
            LogTool.LogError($"[StageNameTextFontTool] 找不到源字体: {SourceFontPath}");
            return;
        }

        if (!File.Exists(StageConfigPath))
        {
            LogTool.LogError($"[StageNameTextFontTool] 找不到关卡配置: {StageConfigPath}");
            return;
        }

        string characters = CollectStageNameCharacters(File.ReadAllText(StageConfigPath, Encoding.UTF8));
        if (string.IsNullOrEmpty(characters))
        {
            LogTool.LogError("[StageNameTextFontTool] StageName 字符集为空，请检查配置表");
            return;
        }

        LogTool.Log($"[StageNameTextFontTool] 收集到 {characters.Length} 个唯一字符: {characters}");

        bool createdNew;
        TMP_FontAsset fontAsset = EnsureFontAsset(sourceFont, out createdNew);
        if (fontAsset == null)
        {
            LogTool.LogError("[StageNameTextFontTool] 创建/加载 StageNameText 字体失败");
            return;
        }

        BakeCharacters(fontAsset, sourceFont, characters);
        EnsureSubAssetsPersisted(fontAsset);

        fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        ApplyToPrefab(fontAsset);

        LogTool.Log(
            $"[StageNameTextFontTool] 完成。{(createdNew ? "新建" : "更新")} {TargetFontPath}，" +
            $"字符数={characters.Length}，已应用到 {PrefabPath}");
    }

    private static string CollectStageNameCharacters(string json)
    {
        var charSet = new SortedSet<char>();
        var matches = Regex.Matches(json, "\"StageName\"\\s*:\\s*\"([^\"]*)\"");
        foreach (Match match in matches)
        {
            string stageName = match.Groups[1].Value;
            if (string.IsNullOrEmpty(stageName))
            {
                continue;
            }

            foreach (char c in stageName)
            {
                charSet.Add(c);
            }
        }

        return new string(charSet.ToArray());
    }

    private static TMP_FontAsset EnsureFontAsset(Font sourceFont, out bool createdNew)
    {
        createdNew = false;
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TargetFontPath);
        if (existing != null)
        {
            BindSourceFont(existing, sourceFont);
            return existing;
        }

        createdNew = true;
        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            SamplingPointSize,
            AtlasPadding,
            GlyphRenderMode.SDFAA,
            AtlasWidth,
            AtlasHeight,
            AtlasPopulationMode.Dynamic,
            true);

        if (fontAsset == null)
        {
            return null;
        }

        fontAsset.name = "StageNameText";
        AssetDatabase.CreateAsset(fontAsset, TargetFontPath);

        if (fontAsset.atlasTextures != null)
        {
            for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
            {
                Texture2D atlas = fontAsset.atlasTextures[i];
                if (atlas == null)
                {
                    continue;
                }

                atlas.name = "StageNameText Atlas";
                AssetDatabase.AddObjectToAsset(atlas, fontAsset);
            }
        }

        if (fontAsset.material != null)
        {
            fontAsset.material.name = "StageNameText Atlas Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }

        return fontAsset;
    }

    private static void BindSourceFont(TMP_FontAsset fontAsset, Font sourceFont)
    {
        var so = new SerializedObject(fontAsset);
        so.FindProperty("m_SourceFontFileGUID").stringValue = AssetDatabase.AssetPathToGUID(SourceFontPath);
        so.FindProperty("m_SourceFontFile_EditorRef").objectReferenceValue = sourceFont;
        so.FindProperty("m_AtlasWidth").intValue = AtlasWidth;
        so.FindProperty("m_AtlasHeight").intValue = AtlasHeight;
        so.FindProperty("m_AtlasPadding").intValue = AtlasPadding;
        so.FindProperty("m_AtlasRenderMode").intValue = (int)GlyphRenderMode.SDFAA;
        so.ApplyModifiedPropertiesWithoutUndo();
        fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
    }

    private static void BakeCharacters(TMP_FontAsset fontAsset, Font sourceFont, string characters)
    {
        BindSourceFont(fontAsset, sourceFont);
        fontAsset.isMultiAtlasTexturesEnabled = true;

        if (FontEngine.LoadFontFace(sourceFont, SamplingPointSize) == FontEngineError.Success)
        {
            fontAsset.faceInfo = FontEngine.GetFaceInfo();
        }

        fontAsset.ClearFontAssetData(true);

        string missingCharacters;
        bool allAdded = fontAsset.TryAddCharacters(characters, out missingCharacters, true);
        if (!allAdded)
        {
            if (!string.IsNullOrEmpty(missingCharacters))
            {
                LogTool.LogWarning(
                    $"[StageNameTextFontTool] 部分字形未能加入字体（源字体可能缺失）: {missingCharacters}");
            }
            else
            {
                LogTool.LogWarning("[StageNameTextFontTool] TryAddCharacters 未完全成功（可能图集空间不足）");
            }
        }

        fontAsset.ReadFontAssetDefinition();
    }

    private static void EnsureSubAssetsPersisted(TMP_FontAsset fontAsset)
    {
        if (fontAsset.atlasTextures != null)
        {
            for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
            {
                Texture2D atlas = fontAsset.atlasTextures[i];
                if (atlas == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(atlas.name))
                {
                    atlas.name = i == 0 ? "StageNameText Atlas" : $"StageNameText Atlas {i}";
                }

                if (!AssetDatabase.IsSubAsset(atlas) && !EditorUtility.IsPersistent(atlas))
                {
                    AssetDatabase.AddObjectToAsset(atlas, fontAsset);
                }
            }
        }

        if (fontAsset.material != null)
        {
            if (string.IsNullOrEmpty(fontAsset.material.name))
            {
                fontAsset.material.name = "StageNameText Atlas Material";
            }

            if (!AssetDatabase.IsSubAsset(fontAsset.material) && !EditorUtility.IsPersistent(fontAsset.material))
            {
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }
        }
    }

    private static void ApplyToPrefab(TMP_FontAsset fontAsset)
    {
        if (!File.Exists(PrefabPath))
        {
            LogTool.LogError($"[StageNameTextFontTool] 找不到预制体: {PrefabPath}");
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            int applied = 0;
            foreach (var tmp in texts)
            {
                if (tmp == null || tmp.gameObject.name != StageNameTextNodeName)
                {
                    continue;
                }

                tmp.font = fontAsset;
                tmp.fontSharedMaterial = fontAsset.material;
                applied++;
            }

            if (applied == 0)
            {
                LogTool.LogError(
                    $"[StageNameTextFontTool] 预制体中未找到名为 {StageNameTextNodeName} 的 TextMeshProUGUI");
                return;
            }

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            LogTool.Log($"[StageNameTextFontTool] 已写入 {applied} 处 StageNameText 字体引用");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
