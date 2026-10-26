using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 批量给所有贴图设置 WebGL 平台压缩格式为 ASTC 6x6(与 Android 的 textureFormat: 50 对齐)。
/// 现状:全项目贴图 WebGL 平台 overridden=0,走默认格式,微信小游戏移动端实际以 RGBA32 驻留内存(2048² = 16MB/张)。
/// 入口:WEBGL/贴图批量设置ASTC压缩(WebGL内存优化)
/// </summary>
public static class WebGLTextureAstcBatchTool
{
    private const string WebGLPlatform = "WebGL";

    [MenuItem("WEBGL/贴图批量设置ASTC压缩(WebGL内存优化)")]
    private static void Run()
    {
        if (!EditorUtility.DisplayDialog("贴图批量设置ASTC",
                "将给项目内全部贴图设置 WebGL 平台 override = ASTC 6x6。\n会修改大量 .meta 并触发重导入,建议先提交 git。继续?",
                "继续", "取消"))
        {
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Texture", new[] { "Assets" });
        int changed = 0, skipped = 0, failed = 0;
        var failedPaths = new List<string>();

        AssetDatabase.StartAssetEditing();
        try
        {
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (i % 50 == 0)
                {
                    EditorUtility.DisplayProgressBar("设置WebGL ASTC压缩", path, (float)i / guids.Length);
                }

                // 跳过示例/SDK目录,不进包的资源不值得重导入
                if (path.Contains("/Spine Examples/") || path.Contains("/FacebookSDK/") || path.Contains("/QFramework/"))
                {
                    skipped++;
                    continue;
                }

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    skipped++;
                    continue;
                }

                // 法线贴图跳过:ASTC 6x6 对法线精度损失明显,且法线数量少
                if (importer.textureType == TextureImporterType.NormalMap)
                {
                    skipped++;
                    continue;
                }

                try
                {
                    var settings = importer.GetPlatformTextureSettings(WebGLPlatform);
                    if (settings.overridden && settings.format == TextureImporterFormat.ASTC_6x6)
                    {
                        skipped++;
                        continue;
                    }

                    settings.overridden = true;
                    settings.format = TextureImporterFormat.ASTC_6x6;
                    importer.SetPlatformTextureSettings(settings);
                    importer.SaveAndReimport();
                    changed++;
                }
                catch (System.Exception e)
                {
                    failed++;
                    failedPaths.Add(path + " | " + e.Message);
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
        }

        LogTool.Log($"[WebGLTextureAstcBatchTool] 完成: 修改 {changed}, 跳过 {skipped}, 失败 {failed}");
        if (failedPaths.Count > 0)
        {
            LogTool.LogError("[WebGLTextureAstcBatchTool] 失败列表:\n" + string.Join("\n", failedPaths));
        }
        EditorUtility.DisplayDialog("贴图批量设置ASTC",
            $"完成\n修改: {changed}\n跳过: {skipped}\n失败: {failed}\n\n下一步: 重新构建 WebGL Bundle 才生效。", "确定");
    }
}
