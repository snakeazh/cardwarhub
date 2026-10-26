using System;
using System.Collections;
using UnityEngine;
using YooAsset;

[Serializable]
public class AppConfigData
{
    public string AppVersion;
    public string Channel;
    public string CdnUrl;
    public string FallbackCdnUrl;
    public EPlayMode GamePlayMode;
    public override string ToString()
    {
        return $"AppVersion:{AppVersion}, Channel:{Channel}, CdnUrl:{CdnUrl}, FallbackCdnUrl:{FallbackCdnUrl}, GamePlayMode:{GamePlayMode}";
    }
}
public class ConfigAotManager : MonoBehaviour
{
    
    
    [SerializeField]
    EPlayMode playMode = EPlayMode.EditorSimulateMode ;
    
    bool isInitialized = false;
    public static ConfigAotManager Instance
    {
        get
        {
            return instance;
        }
    }
    private AppConfigData AppConfig;

    public AppConfigData ConfigData
    {
        get
        {
            return AppConfig;
        }
    }
    public const string ConfigAotFileName = "AotConfig";
    private static ConfigAotManager instance;

    public IEnumerator Initialize()
    {
        if (isInitialized)
        {
            yield break;
        }
        
        var file = Resources.LoadAsync<TextAsset>(ConfigAotFileName);
        yield return file;
        var asset = (TextAsset)file.asset;
        if (asset != null && !asset.text.Equals(""))
        {
            var playModeContent = asset.text;
            AppConfig = JsonUtility.FromJson<AppConfigData>(playModeContent);
            Debug.Log($"AppGameData Txt:{playModeContent}");
            playMode = AppConfig.GamePlayMode;
            Debug.Log($"AppGameData:{AppConfig}");
        }
        isInitialized = true;
    }
    public EPlayMode GetLocalPlayMode()
    {
      
        return playMode;
    }

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(instance);
            instance = null;
            return;
        }
        
        instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
