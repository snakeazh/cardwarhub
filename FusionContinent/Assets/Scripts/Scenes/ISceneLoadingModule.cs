using System;

public interface ISceneLoadingModule
{
    /// <summary>
    /// 模块名称（用于调试和进度显示）
    /// </summary>
    string ModuleName { get; }

    /// <summary>
    /// 当前加载进度 (0.0 - 1.0)
    /// </summary>
    float Progress { get; }

    /// <summary>
    /// 是否加载完成
    /// </summary>
    bool IsDone { get; }

    /// <summary>
    /// 初始化模块（在加载前调用）
    /// </summary>
    void OnInit();

    /// <summary>
    /// 开始加载逻辑
    /// </summary>
    void StartLoad();

    /// <summary>
    /// 每帧更新（由管理器驱动）
    /// </summary>
    void OnUpdate();
}