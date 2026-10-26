using System.Collections.Generic;
using System;

/// <summary>
/// 基于整型分数(int)的排序器
/// 适合使用“权重分层”策略
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public class PrioritySorter<T>
{
    // --- 内部缓存结构 ---
    private struct ItemWithScore
    {
        public T Data;
        public int Score; // 修改为 int
    }

    // --- 数据存储 ---
    // 缓存层：存储数据和对应的分数
    private List<ItemWithScore> _cachedList = new List<ItemWithScore>(); 
    // 结果层：只存储最终排序后的数据引用
    private List<T> _resultList = new List<T>();                         
    
    // --- 策略与标记 ---
    private Func<T, int> _scoreCalculator; // 修改为返回 int
    private bool _isDirty = false;
    private bool _isDescending = true;     // 默认降序（分数高的在前面）

    // --- 外部接口 ---

    /// <summary>
    /// 设置/更新原始数据源
    /// </summary>
    public void SetData(IEnumerable<T> sourceData)
    {
        _cachedList.Clear();
        if (sourceData != null)
        {
            foreach (var item in sourceData)
            {
                // 初始分数为0
                _cachedList.Add(new ItemWithScore { Data = item, Score = 0 });
            }
        }
        _isDirty = true;
    }

    /// <summary>
    /// 设置计分策略
    /// </summary>
    /// <param name="calculator">输入T，返回int分数的函数</param>
    /// <param name="descending">true=大数在前(优先级)，false=小数在前(如距离)</param>
    public void SetScoreCalculator(Func<T, int> calculator, bool descending = true)
    {
        _scoreCalculator = calculator;
        _isDescending = descending;
        _isDirty = true;
    }

    /// <summary>
    /// 标记数据已变更（需要重算）
    /// </summary>
    public void SetDirty()
    {
        _isDirty = true;
    }

    /// <summary>
    /// 获取排序后的列表
    /// </summary>
    public List<T> GetSortedList()
    {
        if (_isDirty)
        {
            RebuildAndSort();
        }
        return _resultList;
    }

    // --- 内部核心逻辑 ---

    private void RebuildAndSort()
    {
        if (_scoreCalculator == null) return;

        // 1. 计算分数 (N次计算)
        for (int i = 0; i < _cachedList.Count; i++)
        {
            var item = _cachedList[i];
            item.Score = _scoreCalculator(item.Data);
            _cachedList[i] = item;
        }

        // 2. 排序 (N*LogN次比较)
        if (_isDescending)
        {
            _cachedList.Sort((a, b) => b.Score.CompareTo(a.Score)); // 降序
        }
        else
        {
            _cachedList.Sort((a, b) => a.Score.CompareTo(b.Score)); // 升序
        }

        // 3. 提取结果
        _resultList.Clear();
        if (_resultList.Capacity < _cachedList.Count) _resultList.Capacity = _cachedList.Count;

        for (int i = 0; i < _cachedList.Count; i++)
        {
            _resultList.Add(_cachedList[i].Data);
        }

        _isDirty = false;
    }
}