using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 检测当前场景所有物体挂载的脚本类名工具
/// </summary>
public class SceneObjectScriptChecker : EditorWindow
{
    // 滚动视图位置
    private Vector2 _scrollPos;
    // 存储检测结果（GameObject路径 → 脚本类名列表）
    private Dictionary<string, List<string>> _objectScriptMap = new Dictionary<string, List<string>>();
    // 热更类名关键词（可自定义，用于标记）
    private string _hotUpdateKeyWord = "HotUpdate";

    [MenuItem("Tools/场景物体脚本类名检测工具")]
    public static void ShowCheckerWindow()
    {
        GetWindow<SceneObjectScriptChecker>("场景脚本类名检测");
    }

    private void OnGUI()
    {
        // 标题
        GUILayout.Label("当前场景物体脚本类名检测", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // 配置区域
        GUILayout.BeginHorizontal();
        GUILayout.Label("热更类名关键词：", GUILayout.Width(100));
        _hotUpdateKeyWord = EditorGUILayout.TextField(_hotUpdateKeyWord);
        GUILayout.EndHorizontal();
        GUILayout.Space(5);

        // 核心按钮
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("扫描当前打开的场景", GUILayout.Height(35)))
        {
            ScanCurrentSceneScripts();
        }
        if (GUILayout.Button("清空结果", GUILayout.Height(35)))
        {
            _objectScriptMap.Clear();
            Repaint();
        }
        GUILayout.EndHorizontal();

        // 结果展示区域
        GUILayout.Space(10);
        GUILayout.Label($"检测结果（共{_objectScriptMap.Count}个物体挂载脚本）：", EditorStyles.boldLabel);
        
        _scrollPos = GUILayout.BeginScrollView(_scrollPos);
        foreach (var kvp in _objectScriptMap)
        {
            // 显示GameObject完整路径
            GUILayout.Label($"� GameObject：{kvp.Key}", EditorStyles.boldLabel);
            
            // 显示挂载的所有脚本类名（标记热更类）
            foreach (string className in kvp.Value)
            {
                Color originalColor = GUI.contentColor;
                // 标记包含热更关键词的类名（红色）
                if (!string.IsNullOrEmpty(_hotUpdateKeyWord) && className.Contains(_hotUpdateKeyWord))
                {
                    GUI.contentColor = Color.red;
                }
                
                GUILayout.Label($"  → 脚本类名：{className}");
                GUI.contentColor = originalColor;
            }

            // 分隔线
            GUILayout.Space(5);
            GUILayout.Label("————————————————————", EditorStyles.miniLabel);
            GUILayout.Space(5);
        }
        GUILayout.EndScrollView();

        // 空结果提示
        if (_objectScriptMap.Count == 0)
        {
            GUILayout.Label("暂无检测结果，请点击「扫描当前打开的场景」按钮", EditorStyles.centeredGreyMiniLabel);
        }
    }

    /// <summary>
    /// 核心方法：扫描当前打开场景的所有物体，提取挂载的脚本类名
    /// </summary>
    private void ScanCurrentSceneScripts()
    {
        try
        {
            // 清空旧结果
            _objectScriptMap.Clear();

            // 获取当前激活的场景
            Scene currentScene = EditorSceneManager.GetActiveScene();
            if (!currentScene.IsValid())
            {
                EditorUtility.DisplayDialog("提示", "当前无有效打开的场景！", "确定");
                return;
            }

            // 获取场景中所有根物体
            GameObject[] rootObjects = currentScene.GetRootGameObjects();
            if (rootObjects.Length == 0)
            {
                EditorUtility.DisplayDialog("提示", "当前场景无任何物体！", "确定");
                return;
            }

            // 遍历所有物体（含子物体）
            int scannedCount = 0;
            foreach (GameObject rootObj in rootObjects)
            {
                // 递归遍历子物体
                ScanGameObjectRecursive(rootObj, ref scannedCount);
            }

            // 刷新UI并提示
            Repaint();
            EditorUtility.DisplayDialog("成功", $"扫描完成！\n当前场景：{currentScene.name}\n共扫描到{scannedCount}个挂载脚本的物体", "确定");
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("错误", $"扫描失败：{e.Message}", "确定");
        }
    }

    /// <summary>
    /// 递归遍历GameObject及其子物体，提取挂载的脚本类名
    /// </summary>
    private void ScanGameObjectRecursive(GameObject targetObj, ref int scannedCount)
    {
        if (targetObj == null) return;

        // 获取该物体上所有MonoBehaviour组件（即所有脚本）
        MonoBehaviour[] scripts = targetObj.GetComponents<MonoBehaviour>();
        if (scripts.Length > 0)
        {
            // 获取GameObject的完整路径（如 "Canvas/Panel/Button"）
            string objFullPath = GetGameObjectFullPath(targetObj);
            
            // 提取所有脚本类名（去重）
            List<string> classNames = scripts
                .Where(script => script != null) // 过滤空组件（如丢失的脚本）
                .Select(script => script.GetType().Name)
                .Distinct() // 去重（同一物体挂载多个相同脚本的情况）
                .ToList();

            if (classNames.Count > 0)
            {
                _objectScriptMap[objFullPath] = classNames;
                scannedCount++;
            }
        }

        // 递归遍历子物体
        foreach (Transform child in targetObj.transform)
        {
            ScanGameObjectRecursive(child.gameObject, ref scannedCount);
        }
    }

    /// <summary>
    /// 获取GameObject的完整层级路径（如 "Root/Child/GrandChild"）
    /// </summary>
    private string GetGameObjectFullPath(GameObject obj)
    {
        List<string> pathParts = new List<string>();
        Transform currentTransform = obj.transform;
        
        while (currentTransform != null)
        {
            pathParts.Add(currentTransform.name);
            currentTransform = currentTransform.parent;
        }
        
        // 反转路径（从根到子）
        pathParts.Reverse();
        return string.Join("/", pathParts);
    }
}