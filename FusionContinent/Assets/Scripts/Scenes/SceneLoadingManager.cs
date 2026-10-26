using System;
using System.Collections;
using System.Collections.Generic;
using Game.Battle;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 场景加载管理器 - 管理游戏场景的初始化流程
/// 按照先数据初始化、后显示初始化的顺序执行
/// </summary>
public class SceneLoadingManager : MonoBehaviour
{
    public static SceneLoadingManager Instance { get; private set; }
    
    [Header("数据初始化模块（按顺序执行）")]
    [SerializeField]
    private List<SceneMapBase> dataModules = new List<SceneMapBase>();
    
    [Header("显示初始化模块（按顺序执行）")]
    [SerializeField]
    private List<SceneMapBase> displayModules = new List<SceneMapBase>();
    
    [Header("加载完成回调")]
    public UnityEvent onDataPhaseComplete = new UnityEvent();
    public UnityEvent onDisplayPhaseComplete = new UnityEvent();
    public UnityEvent onAllComplete = new UnityEvent();
    
    [Header("调试")]
    [SerializeField]
    private bool showDebugLog = true;
    
    private int currentModuleIndex = 0;
    private bool isDataPhase = true;
    private bool isLoading = false;
    
    // 兼容旧版接口
    private List<ISceneLoadingModule> _modules = new List<ISceneLoadingModule>();
    private bool _isRunning = false;
    private float _totalProgress = 0f;
    
    public event System.Action<float> OnProgressUpdate;
    public event System.Action OnLoadingComplete;
    
    public float TotalProgress 
    { 
        get { return _totalProgress; } 
    }
    
    public bool IsRunning 
    { 
        get { return _isRunning || isLoading; } 
    }
    
    public bool IsLoadingProp 
    { 
        get { return isLoading; } 
    }
    
    public bool IsDataPhaseProp 
    { 
        get { return isDataPhase; } 
    }
    
    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        // service
        Instance = this;
    }
    
    #region 新版接口（数据/显示分离）
    
    /// <summary>
    /// 设置数据初始化模块
    /// </summary>
    public void SetDataModules(List<SceneMapBase> modules)
    {
        dataModules = modules;
    }
    
    /// <summary>
    /// 设置显示初始化模块
    /// </summary>
    public void SetDisplayModules(List<SceneMapBase> modules)
    {
        displayModules = modules;
    }
    
    /// <summary>
    /// 添加数据初始化模块
    /// </summary>
    public void AddDataModule(SceneMapBase module)
    {
        if (module != null && !dataModules.Contains(module))
        {
            dataModules.Add(module);
        }
    }
    
    /// <summary>
    /// 添加显示初始化模块
    /// </summary>
    public void AddDisplayModule(SceneMapBase module)
    {
        if (module != null && !displayModules.Contains(module))
        {
            displayModules.Add(module);
        }
    }
    
    /// <summary>
    /// 开始加载流程（新版）
    /// </summary>
    public void StartLoading()
    {
        if (isLoading)
        {
            Log("加载已在进行中");
            return;
        }
        
        isLoading = true;
        _isRunning = true;
        isDataPhase = true;
        currentModuleIndex = 0;
        
        Log("开始数据初始化阶段");
        StartCoroutine(LoadPhase(dataModules, () =>
        {
            Log("数据初始化阶段完成");
            if (onDataPhaseComplete != null)
                onDataPhaseComplete.Invoke();
            
            // 切换到显示初始化阶段
            isDataPhase = false;
            currentModuleIndex = 0;
            Log("开始显示初始化阶段");
            StartCoroutine(LoadPhase(displayModules, () =>
            {
                Log("显示初始化阶段完成");
                if (onDisplayPhaseComplete != null)
                    onDisplayPhaseComplete.Invoke();
                Log("所有初始化完成");
                if (onAllComplete != null)
                    onAllComplete.Invoke();
                if (OnLoadingComplete != null)
                    OnLoadingComplete.Invoke();
                isLoading = false;
                _isRunning = false;
            }));
        }));
    }
    
    private IEnumerator LoadPhase(List<SceneMapBase> modules, Action onComplete)
    {
        while (currentModuleIndex < modules.Count)
        {
            var module = modules[currentModuleIndex];
            if (module != null)
            {
                Log("加载模块 [" + module.ModuleName + "]...");
                module.OnInit();
                module.StartLoad();
                
                // 等待当前模块完成
                yield return new WaitUntil(() => module.IsDone);
                
                // 检查是否有错误
                if (!string.IsNullOrEmpty(module.ErrorMessage))
                {
                    LogError("模块 [" + module.ModuleName + "] 加载失败: " + module.ErrorMessage);
                }
                else
                {
                    Log("模块 [" + module.ModuleName + "] 加载完成");
                }
            }
            
            currentModuleIndex++;
            UpdateTotalProgress();
        }
        
        if (onComplete != null)
            onComplete();
    }
    
    #endregion
    
    #region 旧版接口（兼容）
    
    /// <summary>
    /// 注册子加载模块（旧版兼容接口）
    /// </summary>
    public void RegisterModule(ISceneLoadingModule module)
    {
        if (module == null) return;
        if (!_modules.Contains(module))
        {
            _modules.Add(module);
            module.OnInit();
        }
    }
    
    #endregion
    
    #region 进度查询
    
    /// <summary>
    /// 获取总体进度 (0.0 - 1.0)
    /// </summary>
    public float GetTotalProgress()
    {
        int totalModules = (dataModules != null ? dataModules.Count : 0) + (displayModules != null ? displayModules.Count : 0);
        if (totalModules == 0) return 0f;
        
        float dataProgress = 0f;
        float displayProgress = 0f;
        
        // 数据阶段进度
        if (dataModules != null && dataModules.Count > 0)
        {
            foreach (var module in dataModules)
            {
                if (module != null) dataProgress += module.Progress;
            }
            dataProgress /= dataModules.Count;
        }
        
        // 显示阶段进度
        if (displayModules != null && displayModules.Count > 0)
        {
            foreach (var module in displayModules)
            {
                if (module != null) displayProgress += module.Progress;
            }
            displayProgress /= displayModules.Count;
        }
        
        // 如果是数据阶段，只计算数据进度
        if (isDataPhase)
        {
            return dataProgress * 0.5f; // 数据阶段占50%
        }
        
        // 显示阶段，数据已完成50%，加上显示进度的50%
        return 0.5f + displayProgress * 0.5f;
    }
    
    /// <summary>
    /// 获取当前阶段进度 (0.0 - 1.0)
    /// </summary>
    public float GetPhaseProgress()
    {
        var modules = isDataPhase ? dataModules : displayModules;
        if (modules == null || modules.Count == 0) return 0f;
        
        float totalProgress = 0f;
        
        // 已完成的模块计为1.0
        for (int i = 0; i < currentModuleIndex && i < modules.Count; i++)
        {
            if (modules[i] != null) totalProgress += 1.0f;
        }
        
        // 当前模块按实际进度
        if (currentModuleIndex < modules.Count && modules[currentModuleIndex] != null)
        {
            totalProgress += modules[currentModuleIndex].Progress;
        }
        
        return totalProgress / modules.Count;
    }
    
    /// <summary>
    /// 获取当前正在加载的模块名称
    /// </summary>
    public string GetCurrentModuleName()
    {
        var modules = isDataPhase ? dataModules : displayModules;
        if (modules != null && currentModuleIndex < modules.Count && modules[currentModuleIndex] != null)
            return modules[currentModuleIndex].ModuleName;
        return "等待中...";
    }
    
    /// <summary>
    /// 获取当前阶段名称
    /// </summary>
    public string GetCurrentPhaseName()
    {
        return isDataPhase ? "数据初始化" : "显示初始化";
    }
    
    private void UpdateTotalProgress()
    {
        _totalProgress = GetTotalProgress();
        if (OnProgressUpdate != null)
            OnProgressUpdate(_totalProgress);
    }
    
    #endregion
    
    #region 日志
    
    private void Log(string message)
    {
        if (showDebugLog)
        {
            LogTool.Log("[SceneLoadingManager] " + message);
        }
    }
    
    private void LogError(string message)
    {
        LogTool.LogError("[SceneLoadingManager] " + message);
    }
    
    #endregion
    
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
