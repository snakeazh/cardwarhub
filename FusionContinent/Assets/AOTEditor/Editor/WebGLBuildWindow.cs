using System.IO;
using UnityEditor;
using UnityEngine;
using YooAsset;

/// <summary>
/// 微信小游戏（WebGL）打包面板。
/// CDN 地址的真值源是 Assets/Resources/AotConfig.txt（运行时读取），
/// 面板只是它的编辑入口；保存后编辑器与运行时共用同一份配置。
/// </summary>
public class WebGLBuildWindow : EditorWindow
{
    private const string ResVersionPrefsKey = "WebGLBuildWindow.ResVersionCounter";

    [MenuItem("WEBGL/打包小游戏面板")]
    private static void Open()
    {
        GetWindow<WebGLBuildWindow>("小游戏打包");
    }

    private string _cdnUrl;
    private string _fallbackCdnUrl;
    private int _resVersionCounter;

    private static string AotConfigPath
    {
        get { return Path.Combine(Application.dataPath, "Resources", ConfigAotManager.ConfigAotFileName + ".txt"); }
    }

    void OnEnable()
    {
        LoadConfig();
        _resVersionCounter = EditorPrefs.GetInt(ResVersionPrefsKey, 1);
    }

    void LoadConfig()
    {
        if (File.Exists(AotConfigPath))
        {
            var config = JsonUtility.FromJson<AppConfigData>(File.ReadAllText(AotConfigPath));
            _cdnUrl = config.CdnUrl;
            _fallbackCdnUrl = config.FallbackCdnUrl;
        }
        if (string.IsNullOrEmpty(_cdnUrl))
        {
            _cdnUrl = AndroidBuildSettings.CdnUrl;
        }
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("平台", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("当前平台", EditorUserBuildSettings.activeBuildTarget.ToString());
        EditorGUI.BeginDisabledGroup(EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL);
        if (GUILayout.Button("切换到 WebGL 平台"))
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("CDN 配置（保存进 AotConfig.txt）", EditorStyles.boldLabel);
        _cdnUrl = EditorGUILayout.TextField("CDN 地址", _cdnUrl);
        _fallbackCdnUrl = EditorGUILayout.TextField("备用 CDN 地址", _fallbackCdnUrl);
        EditorGUILayout.LabelField("运行时请求预览", $"{_cdnUrl}WebGL/v{Application.version}/webgl/...", EditorStyles.wordWrappedMiniLabel);

        if (GUILayout.Button("保存配置到 AotConfig"))
        {
            SaveConfig();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("资源构建", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("ResVersion（自增）", _resVersionCounter.ToString());
        EditorGUILayout.LabelField("产物输出", "Bundles/WebGL → D:/CDN/WebGL/", EditorStyles.wordWrappedMiniLabel);

        if (GUILayout.Button("构建 WebGL Bundle", GUILayout.Height(30)))
        {
            StartBuild();
        }
    }

    void SaveConfig()
    {
        AppConfigData config = File.Exists(AotConfigPath)
            ? JsonUtility.FromJson<AppConfigData>(File.ReadAllText(AotConfigPath))
            : new AppConfigData();
        config.CdnUrl = _cdnUrl;
        config.FallbackCdnUrl = _fallbackCdnUrl;
        config.GamePlayMode = EPlayMode.WebPlayMode;
        config.AppVersion = PlayerSettings.bundleVersion;
        File.WriteAllText(AotConfigPath, JsonUtility.ToJson(config, true));
        AssetDatabase.Refresh();
        LogTool.Log($"[小游戏打包] AotConfig 已保存: CdnUrl={_cdnUrl}, FallbackCdnUrl={_fallbackCdnUrl}");
    }

    void StartBuild()
    {
        if (string.IsNullOrEmpty(_cdnUrl))
        {
            EditorUtility.DisplayDialog("参数校验失败", "CDN 地址不能为空", "确定");
            return;
        }

        SaveConfig();

        var ctx = new AndroidBuilder.BuildContext
        {
            resVersion = _resVersionCounter.ToString(),
            cdnUrl = _cdnUrl,
            fallbackCdnUrl = _fallbackCdnUrl,
        };
        _resVersionCounter++;
        EditorPrefs.SetInt(ResVersionPrefsKey, _resVersionCounter);

        // 构建过程会刷新资源数据库，延后到 OnGUI 结束后再执行，避免打断 GUILayout 布局栈
        EditorApplication.delayCall += () => AndroidBuilder.BuildWebGLBundle(ctx);
    }
}
