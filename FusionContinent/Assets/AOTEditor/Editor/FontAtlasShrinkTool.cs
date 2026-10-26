using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// 一键重烘 ChironGoRound 两张字体:8192x8192 Alpha8 图集(各 64MB)按 2048 -> 3072 -> 4096 自动降级,装下全部字符为止。
/// 覆盖保存保 guid,prefab / fallback 引用不受影响;原资产各备份一份到 Assets/Fonts/_Backup/。
///
/// Static: padding 15 -> 9 以优先装进 2048(粗描边/大偏移投影若发虚,把 StaticAtlasPadding 调回 15 重跑即可,会自动落到更大图集)。
/// Dynamic: 保持 Dynamic 模式,padding 与 Static 对齐为 9(混排描边一致性;仅 410 字,2048 轻松装下),开多图集支持做运行时兜底。
/// 入口:Tools/字体/重烘ChironGoRound静态字体图集(2048) / 重烘ChironGoRound动态字体图集(2048)
/// </summary>
public static class FontAtlasShrinkTool
{
    private const string SourceFontPath = "Assets/Fonts/ChironGoRoundTC-700B.otf";
    private const string StaticCharsPath = "Assets/Fonts/ChironGoRound_Static_Chars.txt";
    private const string DynamicCharsPath = "Assets/Fonts/ChironGoRound_Dynamic_Chars.txt";
    private const string StaticAssetPath = "Assets/Res/Fonts/ChironGoRoundTC-700BSDFStatic.asset";
    private const string DynamicAssetPath = "Assets/Res/Fonts/ChironGoRoundTC-700BSDFDynamic.asset";
    // 备份放工程根目录 Assets 之外:Assets/Fonts 被 YooAsset 整目录收集,放里面会被打进包
    private const string BackupDir = "FontBackup";

    // 与原资产 m_CreationSettings / m_FaceInfo 对齐;装不下时报缺字,回调尺寸重跑
    // 两张字体 padding 必须一致:Static 缺的字从 Dynamic 取字形混排,padding 不同会导致同一句文本描边粗细不一
    private const int SamplingPointSize = 36;
    private const int StaticAtlasPadding = 9;
    private const int DynamicAtlasPadding = 9;

    // 图集尺寸按序尝试,装下全部字符为止(2048 不够自动升 3072/4096)
    private static readonly int[] AtlasSizeCandidates = { 2048, 3072, 4096 };

    [MenuItem("Tools/字体/重烘ChironGoRound静态字体图集(2048)")]
    private static void RunStatic()
    {
        // fallback 写死指向 Dynamic 字体(与原资产一致),不从可能已损坏的当前资产拷贝
        var dynamicFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DynamicAssetPath);
        if (dynamicFont == null)
        {
            EditorUtility.DisplayDialog("重烘失败", $"找不到 Dynamic 字体: {DynamicAssetPath}", "确定");
            return;
        }

        BakeFontAsset(
            targetAssetPath: StaticAssetPath,
            charsFilePath: StaticCharsPath,
            atlasPadding: StaticAtlasPadding,
            finalPopulationMode: AtlasPopulationMode.Static,
            enableMultiAtlasSupport: false,
            fallbacks: new List<TMP_FontAsset> { dynamicFont });
    }

    [MenuItem("Tools/字体/重烘ChironGoRound动态字体图集(2048)")]
    private static void RunDynamic()
    {
        BakeFontAsset(
            targetAssetPath: DynamicAssetPath,
            charsFilePath: DynamicCharsPath,
            atlasPadding: DynamicAtlasPadding,
            finalPopulationMode: AtlasPopulationMode.Dynamic,
            enableMultiAtlasSupport: true,
            fallbacks: null);
    }

    private static void BakeFontAsset(
        string targetAssetPath,
        string charsFilePath,
        int atlasPadding,
        AtlasPopulationMode finalPopulationMode,
        bool enableMultiAtlasSupport,
        List<TMP_FontAsset> fallbacks)
    {
        string assetName = Path.GetFileNameWithoutExtension(targetAssetPath);
        if (!EditorUtility.DisplayDialog("重烘字体图集",
                $"将重建 {targetAssetPath}\n图集 8192x8192 -> {AtlasSizeCandidates[0]}x{AtlasSizeCandidates[0]} 起,装不下自动升级\n" +
                $"模式: {finalPopulationMode}, padding: {atlasPadding}\n覆盖保存保 guid,引用不动。\n" +
                "原资产会先备份到工程根目录 FontBackup/(Assets 外,不进包),建议先提交 git。继续?",
                "继续", "取消"))
        {
            return;
        }

        var font = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        if (font == null)
        {
            EditorUtility.DisplayDialog("重烘失败", $"找不到源字体: {SourceFontPath}", "确定");
            return;
        }

        var charsAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(charsFilePath);
        if (charsAsset == null)
        {
            EditorUtility.DisplayDialog("重烘失败", $"找不到字符集文件: {charsFilePath}", "确定");
            return;
        }

        string backupPath = $"{BackupDir}/{assetName}_8192.asset";

        // 默认材质外观从当前资产继承(备份在 Assets 外,不作为资产加载;
        // 注意不能从备份拷 _MainTex 之类的纹理引用,本地引用跨文件序列化会把引用拐进备份文件)
        var refAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(targetAssetPath);
        var oldMaterial = refAsset != null ? refAsset.material : null;

        // 重建后材质/图集子资产 fileID 会变,先记下当前值,保存后做全项目引用重映射
        long oldMaterialId = ExtractSubAssetId(targetAssetPath, 21);
        long oldTextureId = ExtractSubAssetId(targetAssetPath, 28);

        var unicodes = charsAsset.text
            .Where(c => c >= 32 && !(c >= 0x7F && c <= 0x9F))
            .Select(c => (uint)c)
            .Distinct()
            .ToArray();
        LogTool.Log($"[FontAtlasShrinkTool] {assetName} 待渲染字符数: {unicodes.Length}");

        // 只备份一次,避免用上次失败重建的空壳覆盖好备份;纯文件拷贝(备份在 Assets 外)
        if (!File.Exists(backupPath))
        {
            Directory.CreateDirectory(BackupDir);
            File.Copy(targetAssetPath, backupPath);
        }

        // 注意:必须以 Dynamic 模式创建,TryAddCharacters 才会渲染字形;灌完字符再切到目标模式
        // 按候选尺寸依次尝试,全部字符装下才落盘,避免产出缺字的空壳
        TMP_FontAsset newAsset = null;
        uint[] missing = null;
        foreach (int atlasSize in AtlasSizeCandidates)
        {
            if (newAsset != null)
            {
                Object.DestroyImmediate(newAsset.atlasTexture);
                Object.DestroyImmediate(newAsset.material);
                Object.DestroyImmediate(newAsset, true);
            }

            newAsset = TMP_FontAsset.CreateFontAsset(
                font, SamplingPointSize, atlasPadding, GlyphRenderMode.SDFAA,
                atlasSize, atlasSize, AtlasPopulationMode.Dynamic, enableMultiAtlasSupport);
            newAsset.name = assetName;
            newAsset.TryAddCharacters(unicodes, out missing);

            if (missing == null || missing.Length == 0)
            {
                break;
            }
            LogTool.LogWarning($"[FontAtlasShrinkTool] {assetName} {atlasSize}x{atlasSize} 装不下(缺 {missing.Length} 字),尝试更大图集");
        }

        if (missing != null && missing.Length > 0)
        {
            LogTool.LogError($"[FontAtlasShrinkTool] {assetName} 最大图集仍缺字 unicode: " + string.Join(",", missing));
            Object.DestroyImmediate(newAsset.atlasTexture);
            Object.DestroyImmediate(newAsset.material);
            Object.DestroyImmediate(newAsset, true);
            EditorUtility.DisplayDialog("重烘失败", $"{assetName} 最大图集 {AtlasSizeCandidates[AtlasSizeCandidates.Length - 1]} 仍缺 {missing.Length} 字,未写入。", "确定");
            return;
        }

        newAsset.atlasPopulationMode = finalPopulationMode;
        if (fallbacks != null)
        {
            newAsset.fallbackFontAssetTable = fallbacks;
        }

        // 覆盖到原路径,guid 不变,引用不动
        AssetDatabase.CreateAsset(newAsset, targetAssetPath);

        newAsset.atlasTexture.name = assetName + " Atlas";
        AssetDatabase.AddObjectToAsset(newAsset.atlasTexture, newAsset);

        if (oldMaterial != null && newAsset.material != null)
        {
            newAsset.material.shader = oldMaterial.shader;
            newAsset.material.CopyPropertiesFromMaterial(oldMaterial);
            // CopyPropertiesFromMaterial 会把旧图集纹理引用一并拷过来,必须重新绑回新图集
            newAsset.material.SetTexture("_MainTex", newAsset.atlasTexture);
            newAsset.material.name = assetName + " Material";
        }
        // 拷贝来的材质带着旧图集的烘焙参数,必须改回新图集的值:
        // TMP 加载时会从材质 _GradientScale 反推 padding(见 ReadFontAssetDefinition),错了全局渲染都会变形
        if (newAsset.material != null)
        {
            newAsset.material.SetFloat("_GradientScale", atlasPadding + 1);
            newAsset.material.SetFloat("_TextureWidth", newAsset.atlasWidth);
            newAsset.material.SetFloat("_TextureHeight", newAsset.atlasHeight);
        }
        AssetDatabase.AddObjectToAsset(newAsset.material, newAsset);

        EditorUtility.SetDirty(newAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(targetAssetPath, ImportAssetOptions.ForceSynchronousImport);

        // 材质/图集子资产 fileID 变化的全项目引用重映射(场景/prefab/材质里的内嵌引用)
        RemapSubAssetReferences(targetAssetPath, oldMaterialId, ExtractSubAssetId(targetAssetPath, 21));
        RemapSubAssetReferences(targetAssetPath, oldTextureId, ExtractSubAssetId(targetAssetPath, 28));

        // 引用该字体的独立材质预设(*.mat)同步图集参数:TMP 不会自动更新它们
        SyncPresetMaterialParams(targetAssetPath, atlasPadding, newAsset.atlasWidth, newAsset.atlasHeight);

        int charCount = newAsset.characterTable != null ? newAsset.characterTable.Count : -1;
        string msg = $"重烘完成\n字符: {charCount}/{unicodes.Length}\n" +
                     $"缺字: {(missing != null ? missing.Length : 0)}\n" +
                     $"图集: {newAsset.atlasWidth}x{newAsset.atlasHeight}\n备份: {backupPath}";
        LogTool.Log($"[FontAtlasShrinkTool] {assetName} {msg.Replace("\n", " | ")}");
        if (missing != null && missing.Length > 0)
        {
            LogTool.LogError($"[FontAtlasShrinkTool] {assetName} 缺字 unicode: " + string.Join(",", missing));
        }
        EditorUtility.DisplayDialog("重烘字体图集", msg + "\n\n下一步: 重新构建 WebGL Bundle 才生效。", "确定");
    }

    private static long ExtractSubAssetId(string assetPath, int classId)
    {
        if (!File.Exists(assetPath)) return 0;
        var m = System.Text.RegularExpressions.Regex.Match(File.ReadAllText(assetPath), $@"--- !u!{classId} &(-?\d+)");
        return m.Success ? long.Parse(m.Groups[1].Value) : 0;
    }

    /// <summary>全项目把 guid+旧 fileID 的内嵌子资产引用改指到新 fileID</summary>
    private static void RemapSubAssetReferences(string targetAssetPath, long oldId, long newId)
    {
        if (oldId == 0 || newId == 0 || oldId == newId) return;

        string guid = AssetDatabase.AssetPathToGUID(targetAssetPath);
        string oldRef = $"fileID: {oldId}, guid: {guid}";
        string newRef = $"fileID: {newId}, guid: {guid}";
        int touched = 0;

        foreach (string file in Directory.GetFiles("Assets", "*.*", SearchOption.AllDirectories))
        {
            if (!file.EndsWith(".unity") && !file.EndsWith(".prefab") && !file.EndsWith(".mat") && !file.EndsWith(".asset")) continue;
            string normalized = file.Replace('\\', '/');
            if (normalized.Contains("/_Backup/") || normalized == targetAssetPath) continue;

            string text = File.ReadAllText(file);
            if (!text.Contains(oldRef)) continue;
            File.WriteAllText(file, text.Replace(oldRef, newRef));
            touched++;
        }

        LogTool.Log($"[FontAtlasShrinkTool] 引用重映射 {oldId} -> {newId}, 修改 {touched} 个文件");
        if (touched > 0)
        {
            AssetDatabase.Refresh();
        }
    }

    /// <summary>把引用该字体的独立材质预设的图集参数(渐变比例/纹理尺寸)同步到新图集</summary>
    private static void SyncPresetMaterialParams(string targetAssetPath, int atlasPadding, int atlasWidth, int atlasHeight)
    {
        string guid = AssetDatabase.AssetPathToGUID(targetAssetPath);
        int touched = 0;

        foreach (string file in Directory.GetFiles("Assets", "*.mat", SearchOption.AllDirectories))
        {
            string normalized = file.Replace('\\', '/');
            if (normalized.Contains("/_Backup/")) continue;

            string text = File.ReadAllText(file);
            if (!text.Contains($"guid: {guid}")) continue;

            string result = System.Text.RegularExpressions.Regex.Replace(text, @"- _GradientScale: [\d.]+", $"- _GradientScale: {atlasPadding + 1}");
            result = System.Text.RegularExpressions.Regex.Replace(result, @"- _TextureWidth: \d+", $"- _TextureWidth: {atlasWidth}");
            result = System.Text.RegularExpressions.Regex.Replace(result, @"- _TextureHeight: \d+", $"- _TextureHeight: {atlasHeight}");
            if (result != text)
            {
                File.WriteAllText(file, result);
                touched++;
            }
        }

        LogTool.Log($"[FontAtlasShrinkTool] 材质预设参数同步(GradientScale={atlasPadding + 1}, {atlasWidth}x{atlasHeight}), 修改 {touched} 个文件");
        if (touched > 0)
        {
            AssetDatabase.Refresh();
        }
    }
}
