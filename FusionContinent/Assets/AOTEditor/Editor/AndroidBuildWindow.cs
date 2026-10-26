using System;
using System.Text;
using UnityEditor;
using UnityEngine;
using YooAsset;

/// <summary>
/// Android 手动打包窗口。窗口只负责收集参数，
/// 与 Jenkins 命令行共用同一个执行入口 AndroidBuilder.Run(BuildContext)，不含任何打包逻辑。
/// </summary>
public class AndroidBuildWindow : EditorWindow
{
    [MenuItem("Tools/Android 打包工具")]
    static void Open()
    {
        GetWindow<AndroidBuildWindow>("Android 打包");
    }

    AndroidBuildSettings _settings;
    string[] _playModeOptions; // 第 0 项为"（不修改）"
    Vector2 _scroll;

    static readonly string[] ChannelOptions = { "douyin", "wenxin" };

    void OnEnable()
    {
        _settings = AssetDatabase.LoadAssetAtPath<AndroidBuildSettings>(AndroidBuildSettings.AssetPath);
        if (_settings == null)
        {
            _settings = CreateInstance<AndroidBuildSettings>();
            AssetDatabase.CreateAsset(_settings, AndroidBuildSettings.AssetPath);
            AssetDatabase.SaveAssets();
        }

        var names = Enum.GetNames(typeof(EPlayMode));
        _playModeOptions = new string[names.Length + 1];
        _playModeOptions[0] = "（不修改）";
        Array.Copy(names, 0, _playModeOptions, 1, names.Length);

        if (string.IsNullOrEmpty(_settings.version))
        {
            _settings.version = PlayerSettings.bundleVersion;
        }
        if (string.IsNullOrEmpty(_settings.bundleVersion))
        {
            _settings.bundleVersion = PlayerSettings.Android.bundleVersionCode.ToString();
        }
    }

    void OnGUI()
    {
        if (_settings == null)
        {
            OnEnable();
        }

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        EditorGUILayout.LabelField("构建内容", EditorStyles.boldLabel);
        _settings.buildApk = EditorGUILayout.Toggle("打 APK/AAB", _settings.buildApk);
        EditorGUI.BeginDisabledGroup(!_settings.buildApk);
        _settings.aab = EditorGUILayout.Toggle("AAB（否则 APK）", _settings.aab);
        EditorGUI.EndDisabledGroup();
        _settings.bundle = EditorGUILayout.Toggle("重新打 Bundle", _settings.bundle);
        _settings.release = EditorGUILayout.Toggle("Release 环境", _settings.release);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("版本", EditorStyles.boldLabel);
        _settings.version = EditorGUILayout.TextField("版本号", _settings.version);
        _settings.bundleVersion = EditorGUILayout.TextField("VersionCode", _settings.bundleVersion);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("资源包", EditorStyles.boldLabel);
        EditorGUI.BeginDisabledGroup(!_settings.bundle);
        EditorGUILayout.LabelField("ResVersion（自增）", _settings.resVersionCounter.ToString());
        _settings.cdnRootDir = EditorGUILayout.TextField("CDN 拷贝目录", _settings.cdnRootDir);
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("渠道与运行配置", EditorStyles.boldLabel);
        int channelIndex = Mathf.Max(0, Array.IndexOf(ChannelOptions, _settings.channel));
        channelIndex = EditorGUILayout.Popup("渠道", channelIndex, ChannelOptions);
        _settings.channel = ChannelOptions[channelIndex];
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.TextField("CDN 下载地址", AndroidBuildSettings.CdnUrl);
        EditorGUI.EndDisabledGroup();
        int modeIndex = string.IsNullOrEmpty(_settings.gamePlayMode)
            ? 0
            : Array.IndexOf(_playModeOptions, _settings.gamePlayMode);
        modeIndex = EditorGUILayout.Popup("GamePlayMode", Mathf.Max(modeIndex, 0), _playModeOptions);
        _settings.gamePlayMode = modeIndex <= 0 ? string.Empty : _playModeOptions[modeIndex];

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("产物输出", EditorStyles.boldLabel);
        _settings.packageName = EditorGUILayout.TextField("文件名前缀", _settings.packageName);
        _settings.branch = EditorGUILayout.TextField("分支", _settings.branch);
        _settings.outputPath = EditorGUILayout.TextField("拷贝输出目录", _settings.outputPath);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("等效 Jenkins 参数", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(BuildCommandPreview(), EditorStyles.wordWrappedMiniLabel);

        EditorGUILayout.Space();
        EditorGUI.BeginDisabledGroup(!_settings.buildApk && !_settings.bundle);
        if (GUILayout.Button("开始打包", GUILayout.Height(30)))
        {
            StartBuild();
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndScrollView();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(_settings);
        }
    }

    /// <summary>
    /// 展示当前窗口参数等效的 Jenkins 命令行参数，便于两边同步配置。
    /// </summary>
    string BuildCommandPreview()
    {
        var sb = new StringBuilder();
        sb.Append("-release=").Append(_settings.release ? "true" : "false");
        sb.Append(" -buildApk=").Append(_settings.buildApk ? "true" : "false");
        sb.Append(" -bundle=").Append(_settings.bundle ? "true" : "false");
        sb.Append(" -aab=").Append(_settings.aab ? "true" : "false");
        AppendArg(sb, "version", _settings.version);
        AppendArg(sb, "bundleVersion", _settings.bundleVersion);
        AppendArg(sb, "resVersion", _settings.bundle ? _settings.resVersionCounter.ToString() : null);
        AppendArg(sb, "channel", _settings.channel);
        AppendArg(sb, "gpm", _settings.gamePlayMode);
        AppendArg(sb, "cdnUrl", AndroidBuildSettings.CdnUrl);
        AppendArg(sb, "cdnRootDir", _settings.cdnRootDir);
        AppendArg(sb, "packageName", _settings.packageName);
        AppendArg(sb, "branch", _settings.branch);
        AppendArg(sb, "outputPath", _settings.outputPath);
        return sb.ToString();
    }

    static void AppendArg(StringBuilder sb, string key, string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            sb.Append(" -").Append(key).Append('=').Append(value);
        }
    }

    void StartBuild()
    {
        if (!Validate(out string error))
        {
            EditorUtility.DisplayDialog("参数校验失败", error, "确定");
            return;
        }

        var ctx = _settings.ToBuildContext();
        if (ctx.bundle)
        {
            _settings.resVersionCounter++;
            EditorUtility.SetDirty(_settings);
        }

        AssetDatabase.SaveAssets();

        // Run 内部会切换宏/刷新资源，可能触发重编译与域重载；
        // 延后到 OnGUI 结束后再执行，避免打断 GUILayout 布局栈（EndLayoutGroup 报错）。
        EditorApplication.delayCall += () => AndroidBuilder.Run(ctx);
    }

    bool Validate(out string error)
    {
        if (!string.IsNullOrEmpty(_settings.bundleVersion) && !int.TryParse(_settings.bundleVersion, out _))
        {
            error = "VersionCode 必须是整数";
            return false;
        }
        error = null;
        return true;
    }
}
