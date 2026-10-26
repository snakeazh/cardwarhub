using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// Unity Windows编辑器下打开缓存目录的工具类
/// </summary>
public static class CacheDirectoryOpener
{
    // 菜单栏路径（方便在Unity编辑器中快速调用）
    private const string MenuRoot = "Tools/缓存目录/";

    /// <summary>
    /// 打开Unity默认的缓存根目录
    /// </summary>
    [MenuItem(MenuRoot + "打开Unity缓存根目录", false, 1)]
    public static void OpenUnityCacheRootDirectory()
    {
        // Unity默认缓存目录路径（Windows平台）
        string cachePath = Application.temporaryCachePath;
        
        OpenDirectory(cachePath);
    }

    /// <summary>
    /// 打开项目的PersistentDataPath目录（持久化缓存目录）
    /// </summary>
    [MenuItem(MenuRoot + "打开持久化缓存目录", false, 2)]
    public static void OpenPersistentDataDirectory()
    {
        string persistentPath = Application.persistentDataPath;
        
        OpenDirectory(persistentPath);
    }

    /// <summary>
    /// 打开自定义缓存目录（可传入任意缓存路径）
    /// </summary>
    /// <param name="customPath">自定义缓存路径</param>
    public static void OpenCustomCacheDirectory(string customPath)
    {
        // 校验路径合法性
        if (string.IsNullOrEmpty(customPath))
        {
            EditorUtility.DisplayDialog("错误", "缓存路径不能为空！", "确定");
            return;
        }

        // 若路径不存在，先创建目录
        if (!Directory.Exists(customPath))
        {
            Directory.CreateDirectory(customPath);
            Debug.Log($"自定义缓存目录不存在，已创建：{customPath}");
        }

        OpenDirectory(customPath);
    }

    /// <summary>
    /// 通用打开目录方法（Windows平台）
    /// </summary>
    /// <param name="path">要打开的目录路径</param>
    private static void OpenDirectory(string path)
    {
        try
        {
            // 处理路径格式（统一转为Windows格式，避免斜杠问题）
            path = path.Replace('/', '\\');
            
            // 验证路径是否存在
            if (!Directory.Exists(path))
            {
                EditorUtility.DisplayDialog("提示", $"目录不存在，已自动创建：{path}", "确定");
                Directory.CreateDirectory(path);
            }

            // Windows下通过cmd命令打开文件夹
            System.Diagnostics.Process.Start("explorer.exe", path);
            
            Debug.Log($"成功打开缓存目录：{path}");
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("错误", $"打开目录失败：{e.Message}", "确定");
            Debug.LogError($"打开缓存目录异常：{e}");
        }
    }

    // 可选：添加清理缓存目录的功能（方便调试）
    [MenuItem(MenuRoot + "清理持久化缓存目录", false, 3)]
    public static void ClearPersistentCacheDirectory()
    {
        string persistentPath = Application.persistentDataPath;
        if (Directory.Exists(persistentPath))
        {
            if (EditorUtility.DisplayDialog("确认", "是否清空持久化缓存目录？此操作不可恢复！", "确定", "取消"))
            {
                // 删除目录下所有文件和子目录
                DirectoryInfo dirInfo = new DirectoryInfo(persistentPath);
                foreach (FileInfo file in dirInfo.GetFiles())
                {
                    file.Delete();
                }
                foreach (DirectoryInfo dir in dirInfo.GetDirectories())
                {
                    dir.Delete(true);
                }
                EditorUtility.DisplayDialog("成功", "缓存目录已清空", "确定");
                Debug.Log($"已清空持久化缓存目录：{persistentPath}");
            }
        }
        else
        {
            EditorUtility.DisplayDialog("提示", "持久化缓存目录不存在", "确定");
        }
    }
}