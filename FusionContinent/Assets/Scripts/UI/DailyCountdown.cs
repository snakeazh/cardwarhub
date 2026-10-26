using TMPro;
using UnityEngine;
using System;
using FusionContinent;
using FusionContinent.Manager;

public class DailyCountdown : MonoBehaviour
{
    public TMP_Text countdownText;

    // 缓存结构体
    private struct CachedData
    {
        public DateTime InitialServerTime;  // 初始服务器时间
        public DateTime RefreshTime;        // 刷新时间
        public float LocalTimeWhenCached;   // 缓存时的本地时间
        public int ShopId;
    }

    private CachedData? _cachedData = null;
    private DateTime _targetTimeToday;
    private bool _isInitialized = false;

    void Update()
    {
        // UpdateCountdown(10005);
    }

    void UpdateCountdown(int shopId)
    {
        // 如果未初始化，尝试初始化
        if (!_isInitialized)
        {
            if (!InitializeCache(shopId))
            {
                // 初始化失败，显示错误信息或等待
                countdownText.text = "加载中...";
                return;
            }
        }

        var cachedData = _cachedData.Value;
        
        // 基于本地时间计算当前服务器时间
        float elapsedTimeSinceCache = Time.time - cachedData.LocalTimeWhenCached;
        DateTime currentServerTime = cachedData.InitialServerTime.AddSeconds(elapsedTimeSinceCache);
        
        // 计算时间差
        TimeSpan timeLeft = _targetTimeToday - currentServerTime;
        
        // 如果倒计时结束，重新初始化缓存
        if (timeLeft.TotalSeconds <= 0)
        {
            InitializeCache(shopId);
            return;
        }
        
        // 格式化显示 XX:XX:XX
        countdownText.text = "每日重置倒计时:" + string.Format("{0:D2}:{1:D2}:{2:D2}", 
            timeLeft.Hours, 
            timeLeft.Minutes, 
            timeLeft.Seconds);
    }

    // 初始化缓存数据（带空值检查）
    private bool InitializeCache(int shopId)
    {
        // 检查 ServerTimeManager 是否可用
        if (ServerTimeManager.Instance == null)
        {
            LogTool.LogWarning("ServerTimeManager 未初始化");
            return false;
        }

        // 检查 PlayerShops 是否可用
        if (Global.PlayerShops == null)
        {
            LogTool.LogWarning("PlayerShops 未初始化");
            return false;
        }

        // 检查指定的商店是否存在
        if (!Global.PlayerShops.ContainsKey(shopId))
        {
            LogTool.LogWarning($"商店ID {shopId} 不存在");
            return false;
        }

        // 检查 RefreshTime 是否有效
        if (Global.PlayerShops[shopId] == null)
        {
            LogTool.LogWarning($"商店数据 {shopId} 为 null");
            return false;
        }

        try
        {
            DateTime serverTime = ServerTimeManager.Instance.GetDataTime();
            DateTime refreshTime = Global.PlayerShops[shopId].RefreshTime;
            
            // 计算今天的目标时间
            _targetTimeToday = new DateTime(serverTime.Year, serverTime.Month, serverTime.Day, 
                refreshTime.Hour, refreshTime.Minute, refreshTime.Second);
            
            // 如果今天的目标时间已经过去，就计算到明天目标时间
            if (serverTime > _targetTimeToday)
            {
                _targetTimeToday = _targetTimeToday.AddDays(1);
            }

            CachedData newData = new CachedData
            {
                InitialServerTime = serverTime,
                RefreshTime = refreshTime,
                LocalTimeWhenCached = Time.time,
                ShopId = shopId
            };
            
            _cachedData = newData;
            _isInitialized = true;
            
            LogTool.Log($"初始化成功: 服务器时间={serverTime}, 刷新时间={refreshTime}, 目标时间={_targetTimeToday}");
            return true;
        }
        catch (Exception ex)
        {
            LogTool.LogError($"初始化缓存时发生错误: {ex.Message}");
            return false;
        }
    }

    // 在Inspector中更改目标时间时实时更新显示
    void OnValidate()
    {
        if (countdownText != null)
        {
            // 重置缓存，强制重新获取数据
            _cachedData = null;
            _isInitialized = false;
            // UpdateCountdown(10005);
        }
    }

    // 当对象禁用时清理缓存
    void OnDisable()
    {
        _cachedData = null;
        _isInitialized = false;
    }

    // 当对象启用时重置缓存
    void OnEnable()
    {
        _cachedData = null;
        _isInitialized = false;
    }

    // 添加重试机制，可以在外部调用
    public void RetryInitialize(int shopId)
    {
        _cachedData = null;
        _isInitialized = false;
        InitializeCache(shopId);
    }
}