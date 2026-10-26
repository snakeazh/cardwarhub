using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 将 Assets/Sprites 下的图片按原目录结构迁移到 Assets/Res/UIIconTextures 下。
/// 默认提供“复制”和“移动”两种模式（移动有风险，请谨慎使用）。
/// </summary>
public static class SpriteFolderMigrationTool
{
    private const string SourceRoot = "Assets/Sprites";
    private const string SourceRootAlt = "Assets/Textures/Sprites";
    private const string DestRoot = "Assets/Res/UIIconTextures";

    [MenuItem("Tools/Assets Migration/Migrate Sprites -> Res/UIIconTextures (Move) %#f", false, 2100)]
    private static void MigrateMove()
    {
        var ok = EditorUtility.DisplayDialog(
            "Migrate Sprites",
            "即将把 Assets/Sprites 下的图片移动到 Assets/Res/UIIconTextures，并保持原目录结构。\n\n注意：移动会改变资源引用路径，可能影响现有引用。\n\n确定继续？",
            "继续移动",
            "取消");

        if (!ok)
        {
            return;
        }

        MigrateSelected();
    }

    [MenuItem("Tools/Assets Migration/Migrate Textures/Sprites -> Res/UIIconTextures (Move)", false, 2111)]
    private static void MigrateAltMove()
    {
        var ok = EditorUtility.DisplayDialog(
            "Migrate Sprites",
            "即将把 Assets/Textures/Sprites 下的图片移动到 Assets/Res/UIIconTextures，并保持原目录结构。\n\n注意：移动会改变资源引用路径，可能影响现有引用。\n\n确定继续？",
            "继续移动",
            "取消");

        if (!ok)
        {
            return;
        }

        MigrateSelected(forceRoot: SourceRootAlt);
    }

    [MenuItem("Tools/Assets Migration/Migrate Sprites -> Res/UIIconTextures (Move) %#f", true)]
    private static bool ValidateMigrateMove()
    {
        return HasValidSelectionUnderRoots();
    }

    private static string ResolveSourceRoot()
    {
        if (AssetDatabase.IsValidFolder(SourceRoot))
        {
            return SourceRoot;
        }

        if (AssetDatabase.IsValidFolder(SourceRootAlt))
        {
            Debug.LogWarning($"[SpriteFolderMigrationTool] 未找到 {SourceRoot}，改用 {SourceRootAlt}");
            return SourceRootAlt;
        }

        return SourceRoot;
    }

    private static bool HasValidSelectionUnderRoots()
    {
        var paths = GetSelectedAssetPaths();
        if (paths.Count == 0)
        {
            return false;
        }

        for (var i = 0; i < paths.Count; i++)
        {
            var p = paths[i];
            if (string.IsNullOrEmpty(p))
            {
                continue;
            }

            if (IsUnderRoot(p, SourceRoot) || IsUnderRoot(p, SourceRootAlt))
            {
                return true;
            }
        }

        return false;
    }

    private static void MigrateSelected(string forceRoot = null)
    {
        var selectedPaths = GetSelectedAssetPaths();
        if (selectedPaths.Count == 0)
        {
            EditorUtility.DisplayDialog("Migrate Sprites", "请在 Project 视图中选中要迁移的图片或文件夹。", "确定");
            return;
        }

        var sourceRoot = string.IsNullOrEmpty(forceRoot) ? ResolveSourceRoot() : forceRoot;
        if (!AssetDatabase.IsValidFolder(sourceRoot))
        {
            Debug.LogError($"[SpriteFolderMigrationTool] 源目录不存在: {sourceRoot}");
            EditorUtility.DisplayDialog("Migrate Sprites", $"源目录不存在:\n{sourceRoot}", "确定");
            return;
        }

        EnsureFolderExists(DestRoot);

        // 收集选中范围内的 Texture2D（文件夹递归 + 文件直选）
        var srcTexturePaths = CollectSelectedTexturePaths(selectedPaths, sourceRoot);
        if (srcTexturePaths.Count == 0)
        {
            Debug.LogWarning($"[SpriteFolderMigrationTool] 选中范围内未找到可迁移 Texture2D。sourceRoot={sourceRoot}");
            EditorUtility.DisplayDialog("Migrate Sprites", "选中范围内未找到可迁移图片（需要在 Sprites 目录下）。", "确定");
            return;
        }

        var migrated = 0;
        var skippedExists = 0;
        var failed = 0;
        var skippedOutsideRoot = 0;

        AssetDatabase.StartAssetEditing();
        try
        {
            for (var i = 0; i < srcTexturePaths.Count; i++)
            {
                var srcPath = srcTexturePaths[i];
                if (string.IsNullOrEmpty(srcPath))
                {
                    continue;
                }

                // 只处理位于 sourceRoot 下的资源（保持相对路径）
                if (!srcPath.StartsWith(sourceRoot + "/", StringComparison.Ordinal))
                {
                    skippedOutsideRoot++;
                    continue;
                }

                var relative = srcPath.Substring((sourceRoot + "/").Length);
                var destPath = $"{DestRoot}/{relative}";
                var destDir = Path.GetDirectoryName(destPath)?.Replace("\\", "/");
                if (!string.IsNullOrEmpty(destDir))
                {
                    EnsureFolderExists(destDir);
                }

                if (AssetDatabase.LoadMainAssetAtPath(destPath) != null)
                {
                    skippedExists++;
                    continue;
                }

                var err = AssetDatabase.MoveAsset(srcPath, destPath);
                if (!string.IsNullOrEmpty(err))
                {
                    failed++;
                    Debug.LogError($"[SpriteFolderMigrationTool] Move 失败: {srcPath} -> {destPath}\n{err}");
                    continue;
                }

                migrated++;
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            failed++;
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var msg =
            $"完成（移动）\n\n" +
            $"迁移成功: {migrated}\n" +
            $"目标已存在跳过: {skippedExists}\n" +
            $"不在源根目录跳过: {skippedOutsideRoot}\n" +
            $"失败: {failed}\n\n" +
            $"源根目录: {sourceRoot}\n" +
            $"目标目录: {DestRoot}";
        Debug.Log($"[SpriteFolderMigrationTool] {msg.Replace("\n", " | ")}");
        EditorUtility.DisplayDialog("Migrate Sprites", msg, "确定");
    }

    private static List<string> GetSelectedAssetPaths()
    {
        var result = new List<string>();
        var guids = Selection.assetGUIDs;
        if (guids == null || guids.Length == 0)
        {
            return result;
        }

        var set = new HashSet<string>();
        for (var i = 0; i < guids.Length; i++)
        {
            var p = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (string.IsNullOrEmpty(p))
            {
                continue;
            }

            p = p.Replace("\\", "/");
            if (set.Add(p))
            {
                result.Add(p);
            }
        }

        return result;
    }

    private static bool IsUnderRoot(string assetPath, string root)
    {
        if (string.IsNullOrEmpty(assetPath) || string.IsNullOrEmpty(root))
        {
            return false;
        }

        assetPath = assetPath.Replace("\\", "/");
        root = root.Replace("\\", "/").TrimEnd('/');
        return assetPath == root || assetPath.StartsWith(root + "/", StringComparison.Ordinal);
    }

    private static List<string> CollectSelectedTexturePaths(List<string> selectedPaths, string sourceRoot)
    {
        var result = new List<string>();
        var set = new HashSet<string>();

        for (var i = 0; i < selectedPaths.Count; i++)
        {
            var p = selectedPaths[i];
            if (string.IsNullOrEmpty(p))
            {
                continue;
            }

            // 如果选中的是文件夹，则在该文件夹下递归找 Texture2D
            if (AssetDatabase.IsValidFolder(p))
            {
                // 只在 sourceRoot 下的文件夹里找（否则路径相对会错）
                if (!IsUnderRoot(p, sourceRoot))
                {
                    continue;
                }

                var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { p });
                for (var g = 0; g < guids.Length; g++)
                {
                    var texPath = AssetDatabase.GUIDToAssetPath(guids[g])?.Replace("\\", "/");
                    if (string.IsNullOrEmpty(texPath))
                    {
                        continue;
                    }

                    if (IsUnderRoot(texPath, sourceRoot) && set.Add(texPath))
                    {
                        result.Add(texPath);
                    }
                }

                continue;
            }

            // 选中的是文件：只处理 Texture2D，且必须在 sourceRoot 下
            if (!IsUnderRoot(p, sourceRoot))
            {
                continue;
            }

            var obj = AssetDatabase.LoadMainAssetAtPath(p);
            if (obj is Texture2D && set.Add(p))
            {
                result.Add(p);
            }
        }

        return result;
    }

    private static void EnsureFolderExists(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath))
        {
            return;
        }

        folderPath = folderPath.Replace("\\", "/").TrimEnd('/');
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        var parts = folderPath.Split('/');
        if (parts.Length == 0)
        {
            return;
        }

        var current = parts[0];
        for (var i = 1; i < parts.Length; i++)
        {
            var next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }
}

