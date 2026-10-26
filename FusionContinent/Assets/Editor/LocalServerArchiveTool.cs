using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// LocalServer 本地存档工具：删除 persistentDataPath/save_archive.json（整档单文件），
/// 并顺带清理分块存储时代遗留的 save_slot 目录。
/// 删除后下次登录走 NewPlayerInitializer 重新建档。
/// </summary>
public static class LocalServerArchiveTool
{
    private static string SaveFile
    {
        get { return Path.Combine(Application.persistentDataPath, "save_archive.json"); }
    }

    private static string LegacySaveDir
    {
        get { return Path.Combine(Application.persistentDataPath, "save_slot"); }
    }

    [MenuItem("Tools/LocalServer/删除本地存档 (save_archive.json)")]
    private static void DeleteArchive()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("删除本地存档", "请先退出 Play 模式再删除存档，否则内存中的存档会重新落盘。", "确定");
            return;
        }

        bool hasFile = File.Exists(SaveFile);
        bool hasLegacyDir = Directory.Exists(LegacySaveDir);
        if (!hasFile && !hasLegacyDir)
        {
            EditorUtility.DisplayDialog("删除本地存档", "存档不存在，无需删除：\n" + SaveFile, "确定");
            return;
        }

        bool ok = EditorUtility.DisplayDialog(
            "删除本地存档",
            "将删除本地存档（整档文件及旧版分块目录），下次进游戏会重新建档：\n" + SaveFile,
            "删除",
            "取消");
        if (!ok)
        {
            return;
        }

        if (hasFile)
        {
            File.Delete(SaveFile);
        }
        if (hasLegacyDir)
        {
            Directory.Delete(LegacySaveDir, true);
        }
        LogTool.Log("[LocalServerArchiveTool] 已删除本地存档: " + SaveFile);
        EditorUtility.DisplayDialog("删除本地存档", "删除成功，下次进游戏将重新建档。", "确定");
    }

    [MenuItem("Tools/LocalServer/打开存档目录")]
    private static void OpenArchiveDir()
    {
        EditorUtility.RevealInFinder(File.Exists(SaveFile) ? SaveFile : Application.persistentDataPath);
    }
}
