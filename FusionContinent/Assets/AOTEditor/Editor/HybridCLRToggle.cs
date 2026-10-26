using System.Linq;
using HybridCLR.Editor;
using UnityEditor;

/// <summary>
/// HybridCLR 热更开关：一键同步 Android 平台的 HYBRIDCLR 编译宏与 HybridCLRSettings.enable。
/// 开启：Android 走热更流程（补充 AOT 元数据 + Assembly.Load 热更 dll）。
/// 关闭：纯 AOT，HotFix 代码直接编译进主包，资源初始化后反射调用入口。
/// 注意：WebGL/微信小游戏平台永远不加 HYBRIDCLR 宏，恒为纯 AOT。
/// </summary>
public static class HybridCLRToggle
{
    private const string Define = "HYBRIDCLR";

    [MenuItem("HybridCLR/开关/启用热更 (Android)", false, 300)]
    private static void EnableHotUpdate()
    {
        Set(true);
    }

    [MenuItem("HybridCLR/开关/禁用热更 (纯AOT)", false, 301)]
    private static void DisableHotUpdate()
    {
        Set(false);
    }

    [MenuItem("HybridCLR/开关/启用热更 (Android)", true)]
    private static bool EnableHotUpdateValidate()
    {
        return !HasDefine();
    }

    [MenuItem("HybridCLR/开关/禁用热更 (纯AOT)", true)]
    private static bool DisableHotUpdateValidate()
    {
        return HasDefine();
    }

    private static bool HasDefine()
    {
        return PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android)
            .Split(';').Contains(Define);
    }

    private static void Set(bool on)
    {
        var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android)
            .Split(';').Where(s => s.Length > 0).ToList();
        if (on)
            defines.Add(Define);
        else
            defines.Remove(Define);
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, string.Join(";", defines));

        SettingsUtil.Enable = on;

        LogTool.Log(on
            ? "[HybridCLR] 已启用热更：Android 添加 HYBRIDCLR 宏，HybridCLRSettings.enable = true"
            : "[HybridCLR] 已禁用热更：Android 移除 HYBRIDCLR 宏，HybridCLRSettings.enable = false（纯 AOT）");
    }
}
