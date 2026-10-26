using UnityEngine;
using UnityEngine.Video;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class VideoLoaderTest : MonoBehaviour
{
    [System.Serializable]
    public class VideoEntry
    {
        public int key;
        public string relativePath; // 相对路径，如 "video/ViolentStormVideo.mp4"
    }

    public RenderTexture targetTexture;
    private Dictionary<int, string> videoDictionary = new Dictionary<int, string>();
    private VideoPlayer videoPlayer;
    private int currentVideoKey = 1;

    void Start()
    {
        videoDictionary[1] = "Video/ViolentStormVideo.mp4";
        InitializeVideoPlayer();
        StartCoroutine(CopyAndPlayVideoByKey(currentVideoKey));
    }

    void InitializeVideoPlayer()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        if (videoPlayer == null)
            videoPlayer = gameObject.AddComponent<VideoPlayer>();

        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = targetTexture;
        videoPlayer.source = VideoSource.Url;

        videoPlayer.prepareCompleted += OnPrepared;
        videoPlayer.errorReceived += OnError;
    }

    IEnumerator CopyAndPlayVideoByKey(int key)
    {
        if (!videoDictionary.ContainsKey(key))
        {
            LogTool.LogError($"未找到 key:{key} 的视频");
            yield break;
        }

        string relativePath = videoDictionary[key];
        string fileName = Path.GetFileName(relativePath);
        string destPath = Path.Combine(Application.persistentDataPath, fileName);

        // 先检查是否已有拷贝
        if (File.Exists(destPath))
        {
            LogTool.Log("使用已存在的视频文件: " + destPath);
            videoPlayer.url = destPath;
            videoPlayer.Prepare();
            yield break;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
    // Android特殊处理
    string sourcePath;
    if (Application.streamingAssetsPath.Contains("://"))
    {
        sourcePath = Path.Combine(Application.streamingAssetsPath, relativePath);
    }
    else
    {
        sourcePath = "jar:file://" + Application.dataPath + "!/assets/" + relativePath;
    }
#else
        // 其他平台
        string sourcePath = Path.Combine(Application.streamingAssetsPath, relativePath);
#endif

        LogTool.Log("尝试从路径加载: " + sourcePath);

        UnityWebRequest request = UnityWebRequest.Get(sourcePath);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            LogTool.LogError($"加载视频失败: {request.error}, 路径: {sourcePath}");
            yield break;
        }

        try
        {
            // 确保目录存在
            string directory = Path.GetDirectoryName(destPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        
            File.WriteAllBytes(destPath, request.downloadHandler.data);
            LogTool.Log("视频复制成功到: " + destPath);
        }
        catch (System.Exception ex)
        {
            LogTool.LogError("写入视频失败: " + ex.Message);
            yield break;
        }

        videoPlayer.url = destPath;
        videoPlayer.Prepare();
    }

    void OnPrepared(VideoPlayer vp)
    {
        vp.Play();
        LogTool.Log("视频准备完成，开始播放");
    }

    void OnError(VideoPlayer vp, string msg)
    {
        LogTool.LogError("VideoPlayer 错误: " + msg);
    }

    void OnDestroy()
    {
        videoPlayer.prepareCompleted -= OnPrepared;
        videoPlayer.errorReceived -= OnError;
    }
}
