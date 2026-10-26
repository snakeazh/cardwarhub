using UnityEngine;

/// <summary>
/// Android 手动打包窗口的持久化配置，字段与 AndroidBuilder.BuildContext 一一对应。
/// 仅作用于编辑器窗口手动打包；Jenkins 命令行不读取本资产。
/// </summary>
public class AndroidBuildSettings : ScriptableObject
{
    public const string AssetPath = "Assets/AOTEditor/Editor/AndroidBuildSettings.asset";

    public bool release;
    public bool buildApk = true;
    public bool bundle;
    public bool aab;
    public string channel;
    public string gamePlayMode;
    public string version;
    public string bundleVersion;
    /// <summary>
    /// 资源版本自增计数，从 1 开始；每次重打 Bundle 后自动 +1。
    /// </summary>
    public int resVersionCounter = 1;
    public string packageName;
    /// <summary>
    /// CDN 下载地址，固定不变。
    /// </summary>
    public const string CdnUrl = "https://cardwar-1328828696.cos.ap-shanghai.myqcloud.com/";
    /// <summary>
    /// CDN 拷贝根目录，留空则使用 BuildAndroid 中的默认路径。
    /// </summary>
    public string cdnRootDir;
    public string branch;
    public string outputPath;

    public AndroidBuilder.BuildContext ToBuildContext()
    {
        return new AndroidBuilder.BuildContext
        {
            release = release,
            buildApk = buildApk,
            bundle = bundle,
            aab = aab,
            channel = channel,
            gamePlayMode = gamePlayMode,
            version = version,
            bundleVersion = bundleVersion,
            resVersion = resVersionCounter.ToString(),
            packageName = packageName,
            cdnUrl = CdnUrl,
            cdnRootDir = cdnRootDir,
            branch = branch,
            outputPath = outputPath,
        };
    }
}
