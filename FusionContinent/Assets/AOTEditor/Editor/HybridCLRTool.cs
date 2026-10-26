#if HYBRIDCLR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using HybridCLR.Editor.Commands; // 确保引用了 HybridCLR 的 Editor 命名空间

public class HybridCLRTool
{
    // 配置：你的 YooAsset 资源存放目录
    private const string ResCodeDir = "Assets/Res/Code";

    [MenuItem("HybridCLR/Build/1. 生成并拷贝DLL (一键处理)")]
    public static void BuildAndCopyDlls()
    {
        // 1. 调用 HybridCLR 生成所有代码 (CompileDll + Il2CppDef + Metadata)
        PrebuildCommand.GenerateAll();
        Debug.Log("HybridCLR GenerateAll 完成！");

        // 2. 定义源文件路径 (根据当前激活的平台自动判断)
        BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
        string hybridClrDir = Application.dataPath.Replace("Assets", "HybridCLRData");
        
        // 热更 DLL 目录
        string hotFixSourceDir = $"{hybridClrDir}/HotUpdateDlls/{target}";
        // AOT 元数据 DLL 目录 (用于泛型补充)
        string aotSourceDir = $"{hybridClrDir}/AssembliesPostIl2CppStrip/{target}";

        // 3. 准备目标目录
        string targetDir = Application.dataPath.Replace("Assets", "") + ResCodeDir;
        if (Directory.Exists(targetDir))
        {
            Directory.Delete(targetDir, true); // 清空旧文件
        }
        Directory.CreateDirectory(targetDir);

        // 4. 拷贝并重命名
        // 这里列出你所有的热更 DLL 名字 (不带后缀)
        string[] hotFixDlls = { "HotFix.dll" }; 
        // 这里列出你所有的 AOT DLL 名字 (补充元数据用)
        string[] aotDlls = AOTGenericReferences.PatchedAOTAssemblyList.ToArray(); 

        CopyDlls(hotFixSourceDir, targetDir, hotFixDlls);
        CopyDlls(aotSourceDir, targetDir, aotDlls);

        // 5. 刷新资源数据库，让 Unity 看到这些新文件
        AssetDatabase.Refresh();
        Debug.Log($"🎉 DLL 拷贝完成！请检查目录: {ResCodeDir}");
    }

    private static void CopyDlls(string srcDir, string destDir, string[] dllNames)
    {
        foreach (var dllName in dllNames)
        {
            string srcPath = $"{srcDir}/{dllName}";
            // 目标文件加上 .bytes 后缀
            string destPath = $"{destDir}/{dllName}.bytes";

            if (File.Exists(srcPath))
            {
                File.Copy(srcPath, destPath, true);
                Debug.Log($"Copy: {dllName} -> {ResCodeDir}");
            }
            else
            {
                Debug.LogError($"[错误] 找不到 DLL 文件: {srcPath}，请检查 HybridCLR 设置或是否已编译成功。");
            }
        }
    }
}
#endif