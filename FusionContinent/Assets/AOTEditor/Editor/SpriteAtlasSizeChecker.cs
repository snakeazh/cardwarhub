using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

public static class SpriteAtlasSizeChecker
{
    // 阈值：超过等于该尺寸将被标记
    private const int ThresholdWidth = 200;
    private const int ThresholdHeight = 200;

    // 菜单入口：Tools/Check Sprites >= 200x200
    [MenuItem("Tools/Check Sprites >= 200x200")] 
    public static void CheckSprites()
    {
        var findings = new List<string>();

        // 1) 扫描所有 Texture2D 下的 Sprite（单图或多图切片）
        var textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] {"Assets"});
        foreach (var guid in textureGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var a in assets)
            {
                if (a is Sprite sprite)
                {
                    var w = (int)sprite.rect.width;
                    var h = (int)sprite.rect.height;
                    if (w >= ThresholdWidth && h >= ThresholdHeight)
                    {
                        var sourcePath = AssetDatabase.GetAssetPath(sprite);
                        findings.Add($"[Sprite] {sourcePath} | {sprite.name} | {w}x{h}");
                    }
                }
            }
        }

        // 2) 扫描所有 SpriteAtlas（打包后的图集中的 Sprite 尺寸）
        var atlasGuids = AssetDatabase.FindAssets("t:SpriteAtlas", new[] {"Assets"});
        foreach (var guid in atlasGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
            if (atlas == null) continue;

            CollectSpritesFromAtlas(atlas, findings);
        }

        // 3) 输出结果与保存报告
        if (findings.Count == 0)
        {
            Debug.Log("检查完成：未发现尺寸 >= 200x200 的图。");
            EditorUtility.DisplayDialog("Sprite 尺寸检查", "未发现尺寸 >= 200x200 的图。", "确定");
            return;
        }

        // 控制台输出详情
        Debug.Log($"检查完成：发现 {findings.Count} 条记录（尺寸 >= {ThresholdWidth}x{ThresholdHeight}）。\n" + string.Join("\n", findings));

        // 另存报告 CSV（便于归档查看）
        var outDir = Path.Combine("Builds", "Reports");
        Directory.CreateDirectory(outDir);
        var outCsv = Path.Combine(outDir, "sprite_size_report.csv");
        File.WriteAllLines(outCsv, new[]{"type,assetPath,spriteName,size"}.Concat(findings.Select(line =>
        {
            // 简单拆分以逗号拼接（避免逗号冲突这里不做复杂转义）
            // 示例：[Sprite] Assets/... | name | 300x400
            var norm = line.Replace(" | ", ",");
            if (norm.StartsWith("[Sprite]")) return norm.Replace("[Sprite] ", "sprite,");
            if (norm.StartsWith("[Atlas:"))
            {
                // 将 [Atlas:Name] 转为 atlas(Name)
                var end = norm.IndexOf("]");
                var atlasTag = norm.Substring(0, end + 1);
                var atlasName = atlasTag.Replace("[Atlas:", "").Replace("]", "");
                var rest = norm.Substring(end + 2); // 跳过空格
                return $"atlas({atlasName})," + rest;
            }
            return norm;
        }))); 

        EditorUtility.DisplayDialog("Sprite 尺寸检查", $"发现 {findings.Count} 条记录。报告已保存：\n{outCsv}", "确定");
    }

    // 仅检查选中的资源（Sprite、Texture2D、SpriteAtlas 或文件夹）
    [MenuItem("Tools/Check Selected Sprites >= 200x200")] 
    public static void CheckSelectedSprites()
    {
        var selectionGuids = Selection.assetGUIDs;
        if (selectionGuids == null || selectionGuids.Length == 0)
        {
            Debug.LogWarning("未选择任何资源。请选择 Sprite、Texture2D、SpriteAtlas 或包含资源的文件夹。");
            EditorUtility.DisplayDialog("Sprite 尺寸检查", "未选择任何资源。", "确定");
            return;
        }

        var findings = new List<string>();

        foreach (var guid in selectionGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);

            if (AssetDatabase.IsValidFolder(path))
            {
                // 文件夹：在该文件夹内扫描相关资源
                var innerGuids = AssetDatabase.FindAssets("t:Sprite t:Texture2D t:SpriteAtlas", new[] { path });
                foreach (var ig in innerGuids)
                {
                    var innerPath = AssetDatabase.GUIDToAssetPath(ig);
                    CollectFindingsForPath(innerPath, findings);
                }
            }
            else
            {
                // 单个资源：根据类型处理
                CollectFindingsForPath(path, findings);
            }
        }

        if (findings.Count == 0)
        {
            Debug.Log("检查完成：未发现尺寸 >= 200x200 的图（在选中项中）。");
            EditorUtility.DisplayDialog("Sprite 尺寸检查", "选中项中未发现尺寸 >= 200x200 的图。", "确定");
            return;
        }

        Debug.Log($"选中项检查完成：发现 {findings.Count} 条记录（尺寸 >= {ThresholdWidth}x{ThresholdHeight}）。\n" + string.Join("\n", findings));

        var outDir = Path.Combine("Builds", "Reports");
        Directory.CreateDirectory(outDir);
        var outCsv = Path.Combine(outDir, "sprite_size_report_selected.csv");
        File.WriteAllLines(outCsv, new[]{"type,assetPath,spriteName,size"}.Concat(findings.Select(line =>
        {
            var norm = line.Replace(" | ", ",");
            if (norm.StartsWith("[Sprite]")) return norm.Replace("[Sprite] ", "sprite,");
            if (norm.StartsWith("[Atlas:"))
            {
                var end = norm.IndexOf("]");
                var atlasTag = norm.Substring(0, end + 1);
                var atlasName = atlasTag.Replace("[Atlas:", "").Replace("]", "");
                var rest = norm.Substring(end + 2);
                return $"atlas({atlasName})," + rest;
            }
            return norm;
        })));

        EditorUtility.DisplayDialog("Sprite 尺寸检查", $"发现 {findings.Count} 条记录（选中项）。报告已保存：\n{outCsv}", "确定");
    }

    private static void CollectFindingsForPath(string path, List<string> findings)
    {
        // SpriteAtlas：直接获取其中的 Sprites
        var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
        if (atlas != null)
        {
            CollectSpritesFromAtlas(atlas, findings);
            return;
        }

        // Texture2D / Sprite / 其他：读取所有内嵌资源并筛选 Sprite
        var assets = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var a in assets)
        {
            if (a is Sprite sprite)
            {
                var w = (int)sprite.rect.width;
                var h = (int)sprite.rect.height;
                if (w >= ThresholdWidth && h >= ThresholdHeight)
                {
                    var sourcePath = AssetDatabase.GetAssetPath(sprite);
                    findings.Add($"[Sprite] {sourcePath} | {sprite.name} | {w}x{h}");
                }
            }
        }
    }

    private static void CollectSpritesFromAtlas(SpriteAtlas atlas, List<string> findings)
    {
        // 使用编辑器扩展 API 获取打包对象（可能是文件夹、Sprite、Texture2D 等）
        var packables = SpriteAtlasExtensions.GetPackables(atlas);
        foreach (var obj in packables)
        {
            var objPath = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(objPath))
            {
                // 可能是场景对象或无效项，跳过
                continue;
            }

            if (AssetDatabase.IsValidFolder(objPath))
            {
                // 如果是文件夹，递归查找其中的 Sprite/Texture2D
                var innerGuids = AssetDatabase.FindAssets("t:Sprite t:Texture2D", new[] { objPath });
                foreach (var ig in innerGuids)
                {
                    var innerPath = AssetDatabase.GUIDToAssetPath(ig);
                    var assets = AssetDatabase.LoadAllAssetsAtPath(innerPath);
                    foreach (var a in assets)
                    {
                        if (a is Sprite s)
                        {
                            var w = (int)s.rect.width;
                            var h = (int)s.rect.height;
                            if (w >= ThresholdWidth && h >= ThresholdHeight)
                            {
                                var sourcePath = AssetDatabase.GetAssetPath(s);
                                findings.Add($"[Atlas:{atlas.name}] {sourcePath} | {s.name} | {w}x{h}");
                            }
                        }
                    }
                }
            }
            else
            {
                // 如果是具体资源（Sprite/Texture2D），读取其内的所有 Sprite
                var assets = AssetDatabase.LoadAllAssetsAtPath(objPath);
                foreach (var a in assets)
                {
                    if (a is Sprite s)
                    {
                        var w = (int)s.rect.width;
                        var h = (int)s.rect.height;
                        if (w >= ThresholdWidth && h >= ThresholdHeight)
                        {
                            var sourcePath = AssetDatabase.GetAssetPath(s);
                            findings.Add($"[Atlas:{atlas.name}] {sourcePath} | {s.name} | {w}x{h}");
                        }
                    }
                }
            }
        }
    }
}