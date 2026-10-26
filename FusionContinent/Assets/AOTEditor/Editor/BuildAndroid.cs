using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FusionContinent;
using QFramework;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using YooAsset;
using YooAsset.Editor;
using BuildResult = UnityEditor.Build.Reporting.BuildResult;

// 在 Unity 编辑器启动时初始化 Android 签名配置
[InitializeOnLoad]
public static class AndroidSigningInitializer
{
    // 按需修改你的 keystore 路径与别名
    private const string KeystorePath = "user.keystore";
    private const string KeyAliasName = "user"; // 如别名不同请修改
    private const string Password = "123123";   // Keystore 和 KeyAlias 使用同一密码

    static AndroidSigningInitializer()
    {
        SetupSigning();
    }

    private static void SetupSigning()
    {
        // 配置 Android 签名
        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = KeystorePath;
        PlayerSettings.keystorePass = Password; // Keystore 密码
        PlayerSettings.Android.keyaliasName = KeyAliasName;
        PlayerSettings.Android.keyaliasPass = Password; // Key Alias 密码

        // 可选：根据需要设置是否打 AAB（默认不改）
        // EditorUserBuildSettings.buildAppBundle = false;

        // 反馈日志，便于确认初始化是否完成
        if (!File.Exists(KeystorePath))
        {
            Debug.LogWarning($"Android 签名已初始化，但未找到 keystore 文件：{KeystorePath}。请将 keystore 放到该路径或修改脚本常量。");
        }
        else
        {
            Debug.Log($"Android 签名已初始化：keystore='{KeystorePath}', alias='{KeyAliasName}'。");
        }
    }
}
public static class AndroidBuilder
{
    // 菜单：Tools/Build Android (APK)
    [MenuItem("Tools/Build Android (APK)")]
    public static void BuildApkMenu()
    {
        SwitchDev();
        BuildApk(new BuildContext());
    }
    
    public class BuildContext
    {
        public bool bundle;
        public string channel;
        public bool release;
        public string version;
        public string bundleVersion;
        public string branch;
        public bool aab;
        public string packageName;
        public string outputPath;
        public bool buildApk = true;
        public string gamePlayMode;
        public string cdnUrl;
        /// <summary>
        /// 备用 CDN 地址，主 CDN 拉取失败时由 YooAsset 回退使用；为空则与 cdnUrl 相同。
        /// </summary>
        public string fallbackCdnUrl;
        /// <summary>
        /// CDN 拷贝根目录，为空时使用默认路径（Win: D:/CDN/Android，OSX: /Users/apple/WWW/CDN/Android）。
        /// </summary>
        public string cdnRootDir;
        /// <summary>
        /// Jenkins 传入 -resVersion=，例如 BUILD_NUMBER。
        /// </summary>
        public string resVersion;
        public override string ToString()
        {
            return $"包名前缀:{packageName}, 资源打包:{bundle}, 打APK:{buildApk}, 渠道:{channel}, release:{release}, " +
                   $"版本号:{version}, bundleVersionCode:{bundleVersion}, 分支:{branch}, aab:{aab}, " +
                   $"gpm:{gamePlayMode}, cdnUrl:{cdnUrl}, fallbackCdnUrl:{fallbackCdnUrl}, cdnRootDir:{cdnRootDir}, resVersion:{resVersion}, outputPath:{outputPath}";
        }
    }

    const string DefaultPackageName = "DefaultPackage";

    static Dictionary<string, string> _commandLineArgs;

    /// <summary>
    /// 解析 Unity 命令行参数，支持：
    /// -key=value、-key value、--key=value、--key value；
    /// 单独 -key / --key（无值）视为 true，供 bool 开关使用。
    /// </summary>
    static Dictionary<string, string> GetCommandLineArgs()
    {
        if (_commandLineArgs != null)
        {
            return _commandLineArgs;
        }

        _commandLineArgs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (!arg.StartsWith("-"))
            {
                continue;
            }

            string body = arg.StartsWith("--", StringComparison.Ordinal) ? arg.Substring(2) : arg.Substring(1);
            string key;
            string value;

            int eqIndex = body.IndexOf('=');
            if (eqIndex >= 0)
            {
                key = body.Substring(0, eqIndex);
                value = body.Substring(eqIndex + 1);
            }
            else
            {
                key = body;
                if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                {
                    value = args[++i];
                }
                else
                {
                    value = "true";
                }
            }

            if (!string.IsNullOrEmpty(key))
            {
                _commandLineArgs[key] = value;
            }
        }

        return _commandLineArgs;
    }

    static string GetArg(string key, string defaultValue = null)
    {
        return GetCommandLineArgs().TryGetValue(key, out var value) ? value : defaultValue;
    }

    static bool GetBoolArg(string key, bool defaultValue)
    {
        if (!GetCommandLineArgs().TryGetValue(key, out var value) || value.IsNullOrEmpty())
        {
            return defaultValue;
        }

        if (bool.TryParse(value, out var result))
        {
            return result;
        }

        if (value == "1")
        {
            return true;
        }

        if (value == "0")
        {
            return false;
        }

        return defaultValue;
    }

    static BuildContext ParseBuildContextFromCommandLine()
    {
        var parsedArgs = GetCommandLineArgs();
        if (parsedArgs.Count > 0)
        {
            Debug.Log("Jenkins 构建参数: " + string.Join(", ", parsedArgs.Select(kv => $"{kv.Key}={kv.Value}")));
        }

        return new BuildContext
        {
            channel = GetArg("channel"),
            release = GetBoolArg("release", false),
            version = GetArg("version"),
            bundleVersion = GetArg("bundleVersion"),
            branch = GetArg("branch"),
            gamePlayMode = GetArg("gpm"),
            buildApk = GetBoolArg("buildApk", true),
            bundle = GetBoolArg("bundle", false),
            aab = GetBoolArg("aab", false),
            outputPath = GetArg("outputPath"),
            packageName = GetArg("packageName"),
            cdnUrl = GetArg("cdnUrl"),
            fallbackCdnUrl = GetArg("fallbackCdnUrl"),
            cdnRootDir = GetArg("cdnRootDir"),
            resVersion = GetArg("resVersion"),
        };
    }
    
    // 菜单：Tools/Build Android (APK)
    [MenuItem("Tools/Build Android (APK)重新生成Bundle")]
    public static void BuildApkAndBundleMenu()
    {
        Run(ParseBuildContextFromCommandLine());
    }

    /// <summary>
    /// 打包唯一执行入口。Jenkins 命令行（ParseBuildContextFromCommandLine）
    /// 与编辑器窗口（AndroidBuildWindow）都只是参数来源，最终都走这里。
    /// </summary>
    public static void Run(BuildContext buildContext)
    {
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;

        Debug.Log($"构建上下文: {buildContext}");
        SwitchEnvironment(buildContext);

        SetUpVersion(buildContext);
        if (buildContext.buildApk)
        {
            UpdateChannel(buildContext);
        }
#if HYBRIDCLR
        HybridCLRTool.BuildAndCopyDlls();
#endif
        if (buildContext.bundle)
        {
            BuildResKit(buildContext);
        }
        if (buildContext.buildApk)
        {
            BuildApk(buildContext);
        }
    }

    static string ResolveResVersion(BuildContext buildContext)
    {
        if (!buildContext.resVersion.IsNullOrEmpty())
        {
            Debug.Log($"使用 ResVersion: {buildContext.resVersion}");
            return buildContext.resVersion;
        }

        throw new BuildFailedException("构建资源包必须传入 -resVersion=，例如 Jenkins: -resVersion=${BUILD_NUMBER}");
    }

    static AppConfigData LoadExistingAppConfig()
    {
        string configPath = Path.Combine(Application.dataPath, "Resources", ConfigAotManager.ConfigAotFileName + ".txt");
        if (!File.Exists(configPath))
        {
            return null;
        }

        return JsonUtility.FromJson<AppConfigData>(File.ReadAllText(configPath));
    }

    /// <summary>
    /// 更新渠道配置，如果没有传入对应字段则保留已有值。
    /// </summary>
    public static void UpdateChannel(BuildContext buildContext)
    {
        AppConfigData configData = LoadExistingAppConfig() ?? new AppConfigData();

        if (!buildContext.channel.IsNullOrEmpty())
        {
            configData.Channel = buildContext.channel;
        }

        if (!buildContext.gamePlayMode.IsNullOrEmpty())
        {
            configData.GamePlayMode = System.Enum.Parse<EPlayMode>(buildContext.gamePlayMode);
        }

        if (!buildContext.cdnUrl.IsNullOrEmpty())
        {
            configData.CdnUrl = buildContext.cdnUrl;
        }

        if (!buildContext.fallbackCdnUrl.IsNullOrEmpty())
        {
            configData.FallbackCdnUrl = buildContext.fallbackCdnUrl;
        }

        if (!buildContext.version.IsNullOrEmpty())
        {
            configData.AppVersion = buildContext.version;
        }

        string configPath = Path.Combine(Application.dataPath, "Resources", ConfigAotManager.ConfigAotFileName + ".txt");
        FileInfo f = new FileInfo(configPath);
        if (!f.Exists)
        {
            f.Create();
        }

        File.WriteAllText(configPath, JsonUtility.ToJson(configData, true));
        AssetDatabase.Refresh();
        Debug.Log($"AotConfig 已更新: {configPath}");
    }
 
    public static void UpdateAppVersion(BuildContext buildContext)
    {
        // int a, b, c, d = 0;
        // var versionNum = Application.version.Split('.');
        PlayerSettings.bundleVersion = buildContext.version;
        PlayerSettings.Android.bundleVersionCode = int.Parse(buildContext.bundleVersion);
        // version
    }
    public static string IncrementVersion(string version)
    {
        var parts = version.Split('.');
        var nums = new int[parts.Length];
        for (int j = 0; j< parts.Length; j++) nums[j] = int.Parse(parts[j]);
        int i = nums.Length - 1;
        nums[i]++;
        while (i >= 0 && nums[i] >= 10)
        {
            nums[i] = 0;
            i--;
            if (i >= 0) nums[i]++;
        }
        return string.Join(".", nums);
    }
    [MenuItem("Tools/Build 重新生成Bundle")]
    public static void BuildApkAndBundleMenuRelease()
    {
        // SwitchRelease();

        BuildResKit(new BuildContext());
        // BuildApk(true);
    }

    // 菜单：Tools/Build WebGL 重新生成Bundle（微信小游戏资源，产物拷到 CDN 的 WebGL 目录，不进 StreamingAssets）
    [MenuItem("Tools/Build WebGL 重新生成Bundle")]
    public static void BuildWebGLBundleMenu()
    {
        BuildWebGLBundle(ParseBuildContextFromCommandLine());
    }

    /// <summary>
    /// WebGL bundle 构建公开入口，供打包面板调用。
    /// WebGL 资源走 CDN，不拷贝进 StreamingAssets；resVersion 为空时缺省用 Application.version。
    /// </summary>
    public static void BuildWebGLBundle(BuildContext buildContext)
    {
        buildContext.buildApk = false;
        if (buildContext.resVersion.IsNullOrEmpty())
        {
            buildContext.resVersion = Application.version;
        }
        BuildResKit(buildContext, BuildTarget.WebGL);
    }
    public static class DirCopyUtil
    {
        public static void CopyDirectory(string sourceDir, string destDir, bool overwrite = true)
        {
            if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var name = Path.GetFileName(file);
                var dest = Path.Combine(destDir, name);
                File.Copy(file, dest, overwrite);
            }
            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var name = Path.GetFileName(dir);
                var dest = Path.Combine(destDir, name);
                CopyDirectory(dir, dest, overwrite);
            }
        }
    }
    static void WriteCdnPackageVersionFile(string cdnRootDir, string resVersion)
    {
        string versionFileName = $"{DefaultPackageName}.version";
        string versionFilePath = Path.Combine(cdnRootDir, versionFileName);
        File.WriteAllText(versionFilePath, resVersion);
        Debug.Log($"CDN 版本指针已更新: {versionFilePath} -> {resVersion}");
    }

    static void BuildResKit(BuildContext buildContext, BuildTarget buildTarget = BuildTarget.Android)
    {
        string platformFolder = buildTarget == BuildTarget.WebGL ? "WebGL" : "Android";
        Debug.Log($"开始构建 : {buildTarget}");

        buildContext.resVersion = ResolveResVersion(buildContext);

        var buildoutputRoot = AssetBundleBuilderHelper.GetDefaultBuildOutputRoot();
        var streamingAssetsRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot();
        
        // 构建参数
        BuiltinBuildParameters buildParameters = new BuiltinBuildParameters();
        buildParameters.BuildOutputRoot = buildoutputRoot;
        buildParameters.BuildinFileRoot = streamingAssetsRoot;
        buildParameters.BuildPipeline = EBuildPipeline.ScriptableBuildPipeline.ToString();
        buildParameters.BuildBundleType = (int)EBuildBundleType.AssetBundle; //必须指定资源包类型
        buildParameters.BuildTarget = buildTarget;
        buildParameters.PackageName = DefaultPackageName;
        buildParameters.PackageVersion = buildContext.resVersion;
        buildParameters.VerifyBuildingResult = true;
        buildParameters.EnableSharePackRule = true; //启用共享资源构建模式，兼容1.5x版本
        buildParameters.FileNameStyle = EFileNameStyle.HashName;
        buildParameters.BuildinFileCopyOption = buildContext.buildApk ? EBuildinFileCopyOption.ClearAndCopyAll : EBuildinFileCopyOption.None;
        buildParameters.BuildinFileCopyParams = string.Empty;
        buildParameters.EncryptionServices = new YooAsset.Editor.EncryptionNone();
        buildParameters.CompressOption = ECompressOption.LZ4;
        buildParameters.ClearBuildCacheFiles = true; //不清理构建缓存，启用增量构建，可以提高打包速度！
        buildParameters.UseAssetDependencyDB = false; //使用资源依赖关系数据库，可以提高打包速度！
    
        // 执行构建
        BuiltinBuildPipeline pipeline = new BuiltinBuildPipeline();
        var buildResult = pipeline.Run(buildParameters, true);
        if (buildResult.Success)
        {
            Debug.Log($"构建成功 : {buildResult.OutputPackageDirectory}");
            GenerateFileList(buildResult.OutputPackageDirectory);

            string cdnRootDir = null;
            if (!buildContext.cdnRootDir.IsNullOrEmpty())
            {
                cdnRootDir = Path.Combine(buildContext.cdnRootDir, "v" + Application.version);
                DirCopyUtil.CopyDirectory(buildResult.OutputPackageDirectory, cdnRootDir, true);
            }
#if UNITY_EDITOR_WIN
            else
            {
                cdnRootDir = Path.Combine($"D:/CDN/{platformFolder}/", "v" + Application.version);
                DirCopyUtil.CopyDirectory(buildResult.OutputPackageDirectory, cdnRootDir, true);
            }
#elif UNITY_EDITOR_OSX
            else
            {
                cdnRootDir = Path.Combine($"/Users/apple/WWW/CDN/{platformFolder}/", "v" + Application.version);
                Debug.Log($"[OSX] Copying to CDN: {cdnRootDir}");
                DirCopyUtil.CopyDirectory(buildResult.OutputPackageDirectory, cdnRootDir, true);
            }
#endif
            if (!cdnRootDir.IsNullOrEmpty())
            {
                WriteCdnPackageVersionFile(cdnRootDir, buildContext.resVersion);
            }
        }
        else
        {
            Debug.LogError($"构建失败 : {buildResult.ErrorInfo}");
            throw new BuildFailedException(buildResult.ErrorInfo);
        }
    }

    [MenuItem("Tools/Switch (DEV)")]
    static void SwitchDev()
    {

        string[] symbols;
        PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android, out symbols);
        var list = symbols.ToList();
        if (list.Contains(ReleaseKey))
        {
            list.Remove(ReleaseKey);
        }
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android,list.ToArray());
        AssetDatabase.Refresh();
    }
    private const string ReleaseKey = "GAME_RELEASE";
    [MenuItem("Tools/Switch (RELEASE)")]
    static void SwitchRelease()
    {
        string[] symbols;
        PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android, out symbols);
        bool have = false;
        foreach (var symbol in symbols)
        {
            if (symbol.Equals(ReleaseKey))
            {
                have = true;
                break;
            }
        }

        if (have)
        {
            return;
        }

        var list = symbols.ToList();
        list.Add(ReleaseKey);
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android,list.ToArray());
        AssetDatabase.Refresh();
    }


    static bool SwitchEnvironment(BuildContext buildContext)
    {
        bool haveRelease = buildContext.release;

        if (haveRelease)
        {
            SwitchRelease();
        }
        else
        {
            SwitchDev();
        }

        return haveRelease;
    }
    static bool IsNullOrEmpty(this string selfStr)
    {
        return string.IsNullOrEmpty(selfStr);
    }

    static void SetUpVersion(BuildContext buildContext)
    {
        var version = buildContext.version;
        if (!version.IsNullOrEmpty())
        {
            PlayerSettings.bundleVersion = buildContext.version;
            Debug.Log("设置版本号"+PlayerSettings.bundleVersion);
        }

        var bundleVersion = buildContext.bundleVersion;
        if (!bundleVersion.IsNullOrEmpty())
        {
            PlayerSettings.Android.bundleVersionCode = int.Parse(bundleVersion);
            Debug.Log("设置BundleVersion"+PlayerSettings.bundleVersion);
        }
    }
    
    // 命令行：-executeMethod AndroidBuilder.BuildApk
    public static void BuildApk(BuildContext buildContext)
    {
        // SetUpVersion( buildContext);
        // 1) 根据命名规则生成文件名
        var projectName = PlayerSettings.productName;                 // 项目名称
        var version = PlayerSettings.bundleVersion;                    // 项目版本号
        var bundleCode = PlayerSettings.Android.bundleVersionCode;     // bundle code
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmm");       // 日期（年月日时分）

        var leftKey = !buildContext.release ? "Dev" : "Release";
        var fileNameBase = $"{buildContext.packageName}_{leftKey}_{projectName}_{version}_{bundleCode}_{timestamp}";
        var outputDir = Path.Combine("Builds", "Android");
        var branchName = buildContext.branch;
        string moveDir = null;
        if (!branchName.IsNullOrEmpty())
        {
            moveDir = Path.Combine(buildContext.outputPath, branchName);
            Debug.Log($"Android 开始构建：{moveDir}，{branchName}");
        }
        
        Directory.CreateDirectory(outputDir);

        bool isAAB = buildContext.aab;
        
        EditorUserBuildSettings.buildAppBundle = isAAB;
        var outputFile = Path.Combine(outputDir,  fileNameBase + (isAAB ? ".aab":".apk"));

        // FindArg("jenkins");
        
        // 3) 收集要打包的场景
        var scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            Debug.LogError("没有启用的场景，至少在 Build Settings 勾选一个场景。");
            return;
        }

        // 5) 开始构建
        var report = BuildPipeline.BuildPlayer(
            scenes,
            outputFile,
            BuildTarget.Android,
            BuildOptions.None
        );

        // 6) 结果反馈
        var summary = report.summary;
        if (summary.result == BuildResult.Succeeded)
        {
            var file =  new FileInfo(outputFile);
            Debug.Log($"Android 构建成功：{outputFile}，大小：{file.Length / (1024f * 1024f):F2} MB");
            if (!moveDir.IsNullOrEmpty())
            {
                moveDir.CreateDirIfNotExists();
                file.CopyTo(Path.Combine(moveDir,file.Name));
            }
        }
        else
        {
            Debug.LogError($"Android 构建失败：{summary.result}，错误数：{summary.totalErrors}");
        }
    }

    private static void GenerateFileList(string directoryPath)
    {
        var stringBuilder = new System.Text.StringBuilder();
        var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            if (file.EndsWith("file_list.txt")) continue;

            var relativePath = file.Replace(directoryPath, "").TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace("\\", "/");
            stringBuilder.AppendLine(relativePath);
        }

        File.WriteAllText(Path.Combine(directoryPath, "file_list.txt"), stringBuilder.ToString());
        Debug.Log($"Generated file_list.txt in {directoryPath}");
    }
}