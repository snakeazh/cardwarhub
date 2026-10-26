using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public class SceneAutoLoader
{
    // // 设置默认的启动场景的路径
    // private const string startScenePath = "Assets/Scenes/GameStart.unity"; // 修改为你的 Start 场景的路径
    //
    // static SceneAutoLoader()
    // {
    //     EditorApplication.playModeStateChanged += OnPlayModeChanged;
    // }
    //
    // private static void OnPlayModeChanged(PlayModeStateChange state)
    // {
    //     if (state == PlayModeStateChange.ExitingEditMode)
    //     {
    //         // 检查当前场景是否为起始场景
    //         if (EditorSceneManager.GetActiveScene().path != startScenePath)
    //         {
    //             // 加载起始场景并保留当前编辑器中的其他场景
    //             EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
    //             EditorSceneManager.OpenScene(startScenePath);
    //         }
    //     }
    // }
}