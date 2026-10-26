using UnityEditor;

/// <summary>
/// 一键切换当前构建平台到 WebGL（微信小游戏）。
/// WebGL 平台没有 HYBRIDCLR 宏，切换后自动走纯 AOT 路径；
/// 切回 Android 用 Build Settings 或 Tools 下的 Android 打包菜单即可。
/// </summary>
public static class WebGLSwitchTool
{
    [MenuItem("WEBGL/一键切换到 WebGL 平台")]
    private static void SwitchToWebGL()
    {
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL)
        {
            LogTool.Log("[WEBGL] 当前已是 WebGL 平台，无需切换");
            return;
        }

        // 注意：切换平台会触发脚本重编译和域重载，此调用之后的代码不会执行，日志要打在前面
        LogTool.Log("[WEBGL] 开始切换到 WebGL 平台，切换期间编辑器会重新编译脚本...");
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
    }

    [MenuItem("WEBGL/一键切换到 WebGL 平台", true)]
    private static bool SwitchToWebGLValidate()
    {
        return EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL;
    }
}
