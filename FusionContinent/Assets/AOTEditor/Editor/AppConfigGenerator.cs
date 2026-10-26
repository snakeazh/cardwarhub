using System.IO;
using UnityEditor;
using UnityEngine;
using YooAsset;

public class AppConfigGenerator
{
    // 定义文件保存路径 (相对于 Assets 文件夹)
    // 注意：Resources 必须在根目录或者子目录下，且文件名不带后缀用于 Load
    private const string FolderPath = "Assets/Resources";
    // private const string FileName = "AppConfig.json"; 
    public static void GenerateDataJson(EPlayMode playMode)
    {
        // 1. 创建并填充数据
        // 你可以在这里修改你想要生成的默认值
        AppConfigData data = new AppConfigData
        {
            AppVersion = PlayerSettings.bundleVersion, // 自动获取当前 PlayerSettings 的版本号
            Channel = "Mainland,Intranet",
            CdnUrl = "192.168.0.188:8080",
            GamePlayMode = playMode
        };

        // 2. 序列化为 JSON 字符串
        // prettyPrint: true 表示格式化输出（带缩进和换行），方便人工阅读
        string jsonContent = JsonUtility.ToJson(data, true);

        // 3. 确保 Resources 目录存在
        if (!Directory.Exists(FolderPath))
        {
            Directory.CreateDirectory(FolderPath);
            Debug.Log($"创建目录: {FolderPath}");
        }

        // 4. 写入文件
        string fullPath = Path.Combine(FolderPath, ConfigAotManager.ConfigAotFileName+".txt");
        File.WriteAllText(fullPath, jsonContent);

        // 5. 刷新 AssetDatabase，让 Unity 立即识别到新文件
        AssetDatabase.Refresh();

        Debug.Log($"[AppConfigGenerator] 配置文件已生成: {fullPath}\n内容:\n{jsonContent}");
    }
    [MenuItem("Tools/生成Editor运行配置")]
    public static void GenerateJson()
    {
        GenerateDataJson(EPlayMode.EditorSimulateMode);
    }
    
    [MenuItem("Tools/生成Web运行配置")]
    public static void GenerateWebJson()
    {
        GenerateDataJson(EPlayMode.WebPlayMode);
    }
    
    [MenuItem("Tools/生成热更运行配置")]
    public static void GenerateHostPlayJson()
    {
        GenerateDataJson(EPlayMode.HostPlayMode);
    }
}