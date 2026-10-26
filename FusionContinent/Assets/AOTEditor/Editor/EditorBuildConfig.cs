#if UNITY_EDITOR


using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class EditorBuildConfig
{
    [MenuItem("Tools/配置导出 Switcher %#d")] // Ctrl+Shift+D
    public static void runTestBat() {
        var batPath = Path.Combine(Application.dataPath, "../../Luban/");
        
        Debug.Log(Path.GetDirectoryName(batPath));
        var psi = new ProcessStartInfo
        {
            FileName = "gen.bat",
            UseShellExecute = true,
            WorkingDirectory = Path.GetDirectoryName(batPath),
            // WindowStyle = ProcessWindowStyle.Minimized // 尽量隐藏窗口（不一定完全隐藏）
        };
        Process.Start(psi);
    }
    
    [MenuItem("Tools/配置可视化工具 Switcher %#e")] // Ctrl+Shift+D
    public static void runEditorBat() {
        var batPath = Path.Combine(Application.dataPath, "../../Luban/");
        
        Debug.Log(Path.GetDirectoryName(batPath));
        var psi = new ProcessStartInfo
        {
            FileName = "defineEditorRun.bat",
            UseShellExecute = true,
            WorkingDirectory = Path.GetDirectoryName(batPath),
            // WindowStyle = ProcessWindowStyle.Minimized // 尽量隐藏窗口（不一定完全隐藏）
        };
        Process.Start(psi);
    }
}
#endif