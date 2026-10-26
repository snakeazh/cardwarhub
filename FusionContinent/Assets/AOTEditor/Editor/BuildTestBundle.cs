using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
#if UNITY_2019_1_OR_NEWER
using UnityEditor.ShortcutManagement;
#endif

// 将选中的资产打成一个单独的 AssetBundle
// 快捷键：Ctrl + Shift + Alt + B
// 菜单：AssetBundles/Build Selected
public static class BuildSelectedAssetBundles
{
    private const string MenuBase = "AssetBundles/Build Selected";
    private const string MenuWithHotkey = MenuBase + " %#&b"; // Ctrl(%) + Shift(#) + Alt(&) + B

    [MenuItem(MenuWithHotkey)]
    public static void BuildSelected()
    {
        var selectedGuids = Selection.assetGUIDs;
        if (selectedGuids == null || selectedGuids.Length == 0)
        {
            Debug.LogWarning("未选择任何资产文件。");
            return;
        }

        var assetPaths = selectedGuids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => !string.IsNullOrEmpty(p))
            .Where(p => !AssetDatabase.IsValidFolder(p)) // 只打包文件，忽略文件夹
            .Distinct()
            .ToList();

        if (assetPaths.Count == 0)
        {
            Debug.LogWarning("选择的对象中没有可打包的文件（可能是文件夹）。");
            return;
        }

        // 将所有选中的文件合并到一个 Bundle
        var combinedBuild = new AssetBundleBuild
        {
            assetBundleName = MakeCombinedBundleName(assetPaths),
            assetNames = assetPaths.ToArray(),
        };
        var builds = new List<AssetBundleBuild> { combinedBuild };

        var target = EditorUserBuildSettings.activeBuildTarget;
        var outputRoot = Path.Combine("AssetBundleTests", target.ToString());
        Directory.CreateDirectory(outputRoot);

        var options = BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode;
        var manifest = BuildPipeline.BuildAssetBundles(outputRoot, builds.ToArray(), options, target);

        if (manifest == null)
        {
            Debug.LogError("AssetBundle 构建失败。");
            return;
        }

        AssetDatabase.Refresh();
        Debug.Log($"已构建合并的 AssetBundle（{combinedBuild.assetBundleName}），共包含 {assetPaths.Count} 个文件，输出目录: {outputRoot}");
    }

    [MenuItem(MenuWithHotkey, true)]
    public static bool ValidateBuildSelected()
    {
        var guids = Selection.assetGUIDs;
        if (guids == null || guids.Length == 0) return false;

        return guids.Any(g =>
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            return !string.IsNullOrEmpty(p) && !AssetDatabase.IsValidFolder(p);
        });
    }

    private static string MakeBundleName(string assetPath)
    {
        // 用路径+文件名生成较稳定的 bundle 名（小写，分隔符改为下划线）
        var fileName = Path.GetFileNameWithoutExtension(assetPath);
        var dir = Path.GetDirectoryName(assetPath) ?? string.Empty;
        dir = dir.Replace(Path.DirectorySeparatorChar, '_').Replace(Path.AltDirectorySeparatorChar, '_');
        var name = string.IsNullOrEmpty(dir) ? fileName : $"{dir}_{fileName}";
        return name.ToLowerInvariant();
    }

    private static string MakeCombinedBundleName(List<string> assetPaths)
    {
        if (assetPaths == null || assetPaths.Count == 0) return "selected_combined";
        if (assetPaths.Count == 1) return MakeBundleName(assetPaths[0]);

        // 以第一个资产所在目录为前缀，生成合并包名
        var dir = Path.GetDirectoryName(assetPaths[0]) ?? string.Empty;
        dir = dir.Replace(Path.DirectorySeparatorChar, '_').Replace(Path.AltDirectorySeparatorChar, '_');
        var name = string.IsNullOrEmpty(dir) ? "selected_combined" : $"{dir}_selected_combined";
        return name.ToLowerInvariant();
    }

#if UNITY_2019_1_OR_NEWER
    // 现代快捷键系统（2019.1+），与菜单热键并存，任选其一即可
    [Shortcut("AssetBundles/Build Selected", KeyCode.B, ShortcutModifiers.Action | ShortcutModifiers.Shift | ShortcutModifiers.Alt)]
    public static void BuildSelectedShortcut()
    {
        BuildSelected();
    }
#endif
}