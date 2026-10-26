using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// 基于 GameFont.otf + GameTitleCharacters.txt 生成固定标题 Static 字体
/// Assets/Res/Fonts/GameTitleFont.asset（配合字号 60）。
/// </summary>
public static class GameTitleFontTool
{
    private const string SourceFontPath = "Assets/Fonts/GameFont.otf";
    private const string TargetFontPath = "Assets/Res/Fonts/GameTitleFont.asset";
    private const string CharactersPath = "Assets/Fonts/GameTitleCharacters.txt";

    private const int SamplingPointSize = 90;
    private const int AtlasPadding = 9;
    private const int AtlasWidth = 1024;
    private const int AtlasHeight = 1024;

    [MenuItem("Tools/Fonts/生成 GameTitleFont 固定标题字体")]
    private static void GenerateTitleFont()
    {
        var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        if (sourceFont == null)
        {
            LogTool.LogError($"[GameTitleFontTool] 找不到源字体: {SourceFontPath}");
            return;
        }

        if (!File.Exists(CharactersPath))
        {
            LogTool.LogError($"[GameTitleFontTool] 找不到标题字集: {CharactersPath}");
            return;
        }

        string characters = CollectCharacters(File.ReadAllText(CharactersPath, Encoding.UTF8));
        if (string.IsNullOrEmpty(characters))
        {
            LogTool.LogError(
                "[GameTitleFontTool] 标题字集为空。请在 GameTitleCharacters.txt 中写入固定标题文案后重试。");
            return;
        }

        LogTool.Log($"[GameTitleFontTool] 收集到 {characters.Length} 个唯一字符: {characters}");

        bool createdNew;
        TMP_FontAsset fontAsset = EnsureFontAsset(sourceFont, out createdNew);
        if (fontAsset == null)
        {
            LogTool.LogError("[GameTitleFontTool] 创建/加载 GameTitleFont 失败");
            return;
        }

        BakeCharacters(fontAsset, sourceFont, characters);
        EnsureSubAssetsPersisted(fontAsset);

        fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        LogTool.Log(
            $"[GameTitleFontTool] 完成。{(createdNew ? "新建" : "更新")} {TargetFontPath}，" +
            $"SamplingPointSize={SamplingPointSize}，字符数={characters.Length}，Mode=Static。");
    }

    private static string CollectCharacters(string raw)
    {
        var charSet = new SortedSet<char>();
        foreach (string line in raw.Split('\n'))
        {
            string trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
            {
                continue;
            }

            foreach (char c in trimmed)
            {
                if (!char.IsWhiteSpace(c))
                {
                    charSet.Add(c);
                }
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

        fontAsset.name = "GameTitleFont";
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

                atlas.name = "GameTitleFont Atlas";
                AssetDatabase.AddObjectToAsset(atlas, fontAsset);
            }
        }

        if (fontAsset.material != null)
        {
            fontAsset.material.name = "GameTitleFont Atlas Material";
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
                    $"[GameTitleFontTool] 部分字形未能加入字体（源字体可能缺失）: {missingCharacters}");
            }
            else
            {
                LogTool.LogWarning("[GameTitleFontTool] TryAddCharacters 未完全成功（可能图集空间不足）");
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
                    atlas.name = i == 0 ? "GameTitleFont Atlas" : $"GameTitleFont Atlas {i}";
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
                fontAsset.material.name = "GameTitleFont Atlas Material";
            }

            if (!AssetDatabase.IsSubAsset(fontAsset.material) && !EditorUtility.IsPersistent(fontAsset.material))
            {
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }
        }
    }
}
