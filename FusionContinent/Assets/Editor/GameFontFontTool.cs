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
/// 基于 GameFont.otf 生成常用字 Static 子集字体 Assets/Res/Fonts/GameFont.asset：
/// 英文字母/常用符号 + Assets/Res/Config/Json 中字符串里的文字与数字。
/// </summary>
public static class GameFontFontTool
{
    private const string SourceFontPath = "Assets/Fonts/GameFont.otf";
    private const string TargetFontPath = "Assets/Res/Fonts/GameFont.asset";
    private const string AsciiCharsetPath = "Assets/Fonts/Number.txt";
    private const string JsonConfigDir = "Assets/Res/Config/Json";
    private const string ExportedCharsetPath = "Assets/Fonts/GameFontCharacters.txt";

    private const int SamplingPointSize = 72;
    private const int AtlasPadding = 7;
    private const int AtlasWidth = 2048;
    private const int AtlasHeight = 2048;

    // 常用 ASCII：数字、大小写字母、常见符号（Number.txt 缺失时的兜底）
    private const string FallbackAscii =
        "0123456789" +
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ" +
        ",.:-+*/=%()[]<>!?#$@&^~_|\\`'\"";

    private static readonly Regex JsonStringRegex = new Regex(
        "\"((?:\\\\.|[^\\\\\"])*)\"",
        RegexOptions.Compiled);

    [MenuItem("Tools/Fonts/生成 GameFont 常用字 Static 子集")]
    private static void GenerateDefaultGameFont()
    {
        var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        if (sourceFont == null)
        {
            LogTool.LogError($"[GameFontFontTool] 找不到源字体: {SourceFontPath}");
            return;
        }

        string characters = CollectCharacters();
        if (string.IsNullOrEmpty(characters))
        {
            LogTool.LogError("[GameFontFontTool] 字集为空");
            return;
        }

        ExportCharsetPreview(characters);
        LogTool.Log($"[GameFontFontTool] 常用字集共 {characters.Length} 个字符，已写出 {ExportedCharsetPath}");

        bool createdNew;
        TMP_FontAsset fontAsset = EnsureFontAsset(sourceFont, out createdNew);
        if (fontAsset == null)
        {
            LogTool.LogError("[GameFontFontTool] 创建/加载 GameFont 失败");
            return;
        }

        BakeCharacters(fontAsset, sourceFont, characters);
        EnsureSubAssetsPersisted(fontAsset);

        fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        AssignAsTmpDefault(fontAsset);

        LogTool.Log(
            $"[GameFontFontTool] 完成。{(createdNew ? "新建" : "更新")} {TargetFontPath}，" +
            $"SamplingPointSize={SamplingPointSize}，Atlas={AtlasWidth}x{AtlasHeight}，" +
            $"字符数={characters.Length}，Mode=Static。已设为 TMP 默认字体。");
    }

    private static string CollectCharacters()
    {
        var charSet = new SortedSet<char>();

        AddAsciiCharset(charSet);
        AddJsonConfigCharset(charSet);

        // TMP 常用空白
        charSet.Add(' ');
        charSet.Add('\u00A0');

        return new string(charSet.ToArray());
    }

    private static void AddAsciiCharset(SortedSet<char> charSet)
    {
        string ascii = FallbackAscii;
        if (File.Exists(AsciiCharsetPath))
        {
            ascii = File.ReadAllText(AsciiCharsetPath, Encoding.UTF8);
        }

        foreach (char c in ascii)
        {
            if (!char.IsControl(c))
            {
                charSet.Add(c);
            }
        }
    }

    private static void AddJsonConfigCharset(SortedSet<char> charSet)
    {
        if (!Directory.Exists(JsonConfigDir))
        {
            LogTool.LogWarning($"[GameFontFontTool] JSON 目录不存在: {JsonConfigDir}");
            return;
        }

        string[] files = Directory.GetFiles(JsonConfigDir, "*.json", SearchOption.TopDirectoryOnly);
        int fileCount = 0;
        foreach (string file in files)
        {
            string json = File.ReadAllText(file, Encoding.UTF8);
            foreach (Match match in JsonStringRegex.Matches(json))
            {
                string raw = match.Groups[1].Value;
                string value = UnescapeJsonString(raw);
                foreach (char c in value)
                {
                    if (ShouldIncludeJsonChar(c))
                    {
                        charSet.Add(c);
                    }
                }
            }

            fileCount++;
        }

        LogTool.Log($"[GameFontFontTool] 已扫描 JSON 文件 {fileCount} 个: {JsonConfigDir}");
    }

    private static bool ShouldIncludeJsonChar(char c)
    {
        if (char.IsControl(c))
        {
            return false;
        }

        // 数字、英文、常见符号
        if (c <= 0x7F)
        {
            return !char.IsControl(c);
        }

        // 中日韩及常用全角标点等
        return true;
    }

    private static string UnescapeJsonString(string raw)
    {
        if (string.IsNullOrEmpty(raw) || raw.IndexOf('\\') < 0)
        {
            return raw;
        }

        var sb = new StringBuilder(raw.Length);
        for (int i = 0; i < raw.Length; i++)
        {
            char c = raw[i];
            if (c != '\\' || i + 1 >= raw.Length)
            {
                sb.Append(c);
                continue;
            }

            char next = raw[++i];
            switch (next)
            {
                case '"':
                case '\\':
                case '/':
                    sb.Append(next);
                    break;
                case 'b':
                    sb.Append('\b');
                    break;
                case 'f':
                    sb.Append('\f');
                    break;
                case 'n':
                    sb.Append('\n');
                    break;
                case 'r':
                    sb.Append('\r');
                    break;
                case 't':
                    sb.Append('\t');
                    break;
                case 'u':
                    if (i + 4 < raw.Length)
                    {
                        string hex = raw.Substring(i + 1, 4);
                        int code;
                        if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out code))
                        {
                            sb.Append((char)code);
                            i += 4;
                            break;
                        }
                    }

                    sb.Append('u');
                    break;
                default:
                    sb.Append(next);
                    break;
            }
        }

        return sb.ToString();
    }

    private static void ExportCharsetPreview(string characters)
    {
        File.WriteAllText(ExportedCharsetPath, characters, new UTF8Encoding(false));
        AssetDatabase.ImportAsset(ExportedCharsetPath);
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

        fontAsset.name = "GameFont";
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

                atlas.name = "GameFont Atlas";
                AssetDatabase.AddObjectToAsset(atlas, fontAsset);
            }
        }

        if (fontAsset.material != null)
        {
            fontAsset.material.name = "GameFont Atlas Material";
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
                int previewLen = missingCharacters.Length > 64 ? 64 : missingCharacters.Length;
                LogTool.LogWarning(
                    $"[GameFontFontTool] 部分字形未能加入（缺失 {missingCharacters.Length} 字），预览: {missingCharacters.Substring(0, previewLen)}");
            }
            else
            {
                LogTool.LogWarning("[GameFontFontTool] TryAddCharacters 未完全成功（可能图集空间不足，已开 Multi Atlas）");
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
                    atlas.name = i == 0 ? "GameFont Atlas" : $"GameFont Atlas {i}";
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
                fontAsset.material.name = "GameFont Atlas Material";
            }

            if (!AssetDatabase.IsSubAsset(fontAsset.material) && !EditorUtility.IsPersistent(fontAsset.material))
            {
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }
        }
    }

    private static void AssignAsTmpDefault(TMP_FontAsset fontAsset)
    {
        TMP_Settings settings = TMP_Settings.instance;
        if (settings == null)
        {
            LogTool.LogWarning("[GameFontFontTool] 未找到 TMP Settings，跳过默认字体赋值");
            return;
        }

        var so = new SerializedObject(settings);
        SerializedProperty defaultFontProp = so.FindProperty("m_defaultFontAsset");
        if (defaultFontProp == null)
        {
            LogTool.LogWarning("[GameFontFontTool] TMP Settings 无 m_defaultFontAsset 字段");
            return;
        }

        defaultFontProp.objectReferenceValue = fontAsset;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
    }
}
