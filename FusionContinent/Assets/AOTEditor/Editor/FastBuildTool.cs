#if HYBRIDCLR
using System.IO;
using UnityEditor;
using UnityEngine;
using HybridCLR.Editor.Commands; // 必须引用

public class FastBuildTool
{
    // ================= 配置区域 =================
    
    // 1. 你的热更程序集名称 (不用带 .dll)
    // 如果你的 asmdef 叫 HotFix，这里就填 "HotFix"
    public static string HotFixDllName = "HotFix"; 

    // 2. 目标资源目录 (YooAsset 扫描目录)
    public static string TargetResDir = "Assets/Res/Code";

    // ===========================================

    [MenuItem("HybridCLR/⚡ 极速构建 (只编译+拷贝Hotfix)")]
    public static void CompileAndCopyHotFix()
    {
        BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
        
        // ---------------------------------------------------------
        // 1. 编译 DLL (这一步只编译 C#，不生成 C++，速度极快)
        // ---------------------------------------------------------
        CompileDllCommand.CompileDll(target);
        Debug.Log($"[FastBuild] {target} 平台 DLL 编译完成！");

        // ---------------------------------------------------------
        // 2. 准备路径
        // ---------------------------------------------------------
        string projectRoot = Directory.GetParent(Application.dataPath).ToString();
        string hybridClrOutputDir = $"{projectRoot}/HybridCLRData/HotUpdateDlls/{target}";
        string targetDir = $"{projectRoot}/{TargetResDir}";

        // 确保目标目录存在
        if (!Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        // ---------------------------------------------------------
        // 3. 拷贝 Hotfix.dll 并改名
        // ---------------------------------------------------------
        string srcDllPath = $"{hybridClrOutputDir}/{HotFixDllName}.dll";
        string srcPdbPath = $"{hybridClrOutputDir}/{HotFixDllName}.pdb"; // 拷贝pdb以便输出报错行号
        
        string destDllPath = $"{targetDir}/{HotFixDllName}.dll.bytes"; // 加上 .bytes
        string destPdbPath = $"{targetDir}/{HotFixDllName}.pdb.bytes";

        if (File.Exists(srcDllPath))
        {
            File.Copy(srcDllPath, destDllPath, true);
            Debug.Log($"✅ 成功拷贝: {destDllPath}");
        }
        else
        {
            Debug.LogError($"❌ 未找到编译后的文件: {srcDllPath}，请检查配置的 HotFixDllName 是否正确！");
            return;
        }

        // (可选) 拷贝 PDB 文件，方便看报错堆栈行号
        if (File.Exists(srcPdbPath))
        {
            File.Copy(srcPdbPath, destPdbPath, true);
            Debug.Log($"✅ 成功拷贝 PDB: {destPdbPath}");
        }

        // ---------------------------------------------------------
        // 4. 刷新 Unity 资源数据库
        // ---------------------------------------------------------
        AssetDatabase.Refresh();
        Debug.Log("🎉 极速构建完成！可以直接打 Bundle 了。");
    }
}
#endif