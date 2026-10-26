#if HYBRIDCLR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using HybridCLR.Editor; // 确保引用了 HybridCLR 的 Editor 命名空间

public class HybridCLRDependencyChecker
{
    [MenuItem("HybridCLR/Check AOT Dependencies (检查 AOT 引用)", false, 200)]
    public static void CheckReferences()
    {
        Debug.Log("--- 开始检查 HybridCLR 热更引用依赖 ---");

        // 1. 获取所有配置的热更程序集名称
        // 注意：这里获取的是 AsmDef 的名字，不带 .dll
        // var hotUpdateAssemblies = SettingsUtil.HotUpdateAssemblyDefinitions.ToList();
        var hotUpdateAssemblies = SettingsUtil.HotUpdateAssemblyNamesIncludePreserved.ToList();
        if (hotUpdateAssemblies.Count == 0)
        {
            Debug.LogWarning("HybridCLR Settings 中未配置任何热更程序集！请先去配置。");
            return;
        }

        Debug.Log($"检测到热更程序集列表: {string.Join(", ", hotUpdateAssemblies)}");

        // 2. 获取 Unity 编译管线中的所有程序集
        Assembly[] allAssemblies = CompilationPipeline.GetAssemblies();

        bool hasError = false;

        // 3. 遍历所有程序集
        foreach (var assembly in allAssemblies)
        {
            string asmName = assembly.name;

            // 如果当前程序集本身就是热更程序集，跳过检测
            if (hotUpdateAssemblies.Contains(asmName))
            {
                continue;
            }

            // 如果是 Editor 程序集，通常不需要检测（Editor 引用热更是允许的），跳过
            // 如果你想严格检查 Editor 也不允许引用，可以注释掉这行
            if ((assembly.flags & AssemblyFlags.EditorAssembly) != 0)
            {
                continue;
            }

            // 4. 检查当前 AOT 程序集引用的所有程序集
            foreach (var reference in assembly.assemblyReferences)
            {
                string refName = reference.name;

                // 核心判断：如果 AOT 引用了 HotFix
                if (hotUpdateAssemblies.Contains(refName))
                {
                    Debug.LogError($"[严重错误] AOT 程序集 '{asmName}' 引用了热更程序集 '{refName}'！\n" +
                                   $"这将导致 '{refName}' 被编译进主包，无法进行热更。\n" +
                                   $"请打开 '{asmName}.asmdef' 并移除对 '{refName}' 的引用。");
                    hasError = true;
                }
            }
        }

        if (!hasError)
        {
            Debug.Log("<color=green>检查通过：没有发现 AOT 程序集直接引用热更程序集的情况。</color>");
        }
        else
        {
            Debug.LogError("检查失败：发现非法的引用关系，请查看上方的 Error 日志修正。");
        }
    }
}
#endif