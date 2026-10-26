using UnityEngine;
using System;

public abstract class SceneMapBase : MonoBehaviour, ISceneLoadingModule
{
    public abstract string ModuleName { get; }
    
    public float Progress
    {
        get => progress;
    }
    protected float progress;
    
    public bool IsDone
    {
        get => isDone;
    }
    protected bool isDone = false;
    
    /// <summary>
    /// 加载完成的回调
    /// </summary>
    public Action OnLoadComplete { get; set; }
    
    /// <summary>
    /// 加载失败的回调
    /// </summary>
    public Action<string> OnLoadFailed { get; set; }
    
    /// <summary>
    /// 错误信息
    /// </summary>
    public string ErrorMessage { get; protected set; }

    public virtual void OnInit()
    {
        progress = 0;
        isDone = false;
        ErrorMessage = null;
    }

    public virtual void StartLoad()
    {
        try
        {
            OnStartLoad();
            if (string.IsNullOrEmpty(ErrorMessage))
            {
                isDone = true;
                progress = 1.0f;
                OnLoadComplete?.Invoke();
            }
            else
            {
                // 失败也必须结束模块，否则 SceneLoadingManager.WaitUntil(IsDone) 会永久卡住
                isDone = true;
                progress = 1.0f;
                LogTool.LogError($"[{ModuleName}] 加载失败: {ErrorMessage}");
                OnLoadFailed?.Invoke(ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            isDone = true;
            progress = 1.0f;
            LogTool.LogError($"[{ModuleName}] 加载失败: {ex.Message}\n{ex.StackTrace}");
            OnLoadFailed?.Invoke(ex.Message);
        }
    }
    
    /// <summary>
    /// 子类实现具体的加载逻辑
    /// </summary>
    protected abstract void OnStartLoad();

    public virtual void OnUpdate()
    {
    }
}
