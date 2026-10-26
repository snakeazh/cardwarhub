using System;
using System.Collections.Generic;
using System.Text;
using Config;

/// <summary>
/// 伤害计算步骤接口
/// </summary>
public interface IDamageCalculationStep
{
    string StepName { get; }
    int InputValue { get; }
    int OutputValue { get; }
    int DeltaValue { get; }
    float UsedValue { get; }
    string AttributeDescription { get; }
    bool IsApplied { get; }
    string Description { get; }
}

/// <summary>
/// 伤害计算步骤实现
/// </summary>
[Serializable]
public class DamageCalculationStep : IDamageCalculationStep
{
    public string StepName { get; set; }
    public int InputValue { get; set; }
    public int OutputValue { get; set; }
    public int DeltaValue => OutputValue - InputValue;
    public float UsedValue { get; set; }
    public string AttributeDescription { get; set; }
    public bool IsApplied { get; set; }
    public string Description { get; set; }

    public override string ToString()
    {
        if (!IsApplied)
            return $"[{StepName}] 未生效 - {Description}";
        
        var change = DeltaValue >= 0 ? $"+{DeltaValue}" : DeltaValue.ToString();
        return $"[{StepName}] {InputValue} -> {OutputValue} ({change}) | {AttributeDescription}={UsedValue:F2} | {Description}";
    }
}

/// <summary>
/// 伤害计算记录器接口
/// </summary>
public interface IDamageCalculationRecorder
{
    void BeginRecord(string soldierName, TakeDamageType damageType, int rawDamage);
    void RecordStep(IDamageCalculationStep step);
    void EndRecord(int finalDamage, bool isDodged);
    string GetRecordReport();
    void Clear();
}

/// <summary>
/// 内存伤害计算记录器 - 将记录保存在内存中
/// </summary>
public class MemoryDamageCalculationRecorder : IDamageCalculationRecorder
{
    private List<IDamageCalculationStep> _steps;
    private string _soldierName;
    private TakeDamageType _damageType;
    private int _rawDamage;
    private int _finalDamage;
    private bool _isDodged;
    private DateTime _startTime;
    private bool _isRecording;

    public void BeginRecord(string soldierName, TakeDamageType damageType, int rawDamage)
    {
        _steps = new List<IDamageCalculationStep>();
        _soldierName = soldierName;
        _damageType = damageType;
        _rawDamage = rawDamage;
        _startTime = DateTime.Now;
        _isRecording = true;
    }

    public void RecordStep(IDamageCalculationStep step)
    {
        if (!_isRecording) return;
        _steps?.Add(step);
    }

    public void EndRecord(int finalDamage, bool isDodged)
    {
        _finalDamage = finalDamage;
        _isDodged = isDodged;
        _isRecording = false;
    }

    public string GetRecordReport()
    {
        if (_steps == null || _steps.Count == 0)
            return "无记录";

        var sb = new StringBuilder();
        sb.AppendLine($"=== 伤害计算报告 [{_startTime:HH:mm:ss.fff}] ===");
        sb.AppendLine($"士兵: {_soldierName}");
        sb.AppendLine($"伤害类型: {_damageType}");
        sb.AppendLine($"原始伤害: {_rawDamage}");
        sb.AppendLine($"最终结果: {(_isDodged ? "闪避" : _finalDamage.ToString())}");
        sb.AppendLine();
        sb.AppendLine("--- 计算步骤 ---");

        foreach (var step in _steps)
        {
            sb.AppendLine(step.ToString());
        }

        sb.AppendLine();
        sb.AppendLine($"总减免: {_rawDamage - _finalDamage} ({((_rawDamage - _finalDamage) / (float)_rawDamage * 100):F1}%)");

        return sb.ToString();
    }

    public void Clear()
    {
        _steps?.Clear();
        _steps = null;
        _isRecording = false;
    }

    public IReadOnlyList<IDamageCalculationStep> GetSteps()
    {
        return _steps?.AsReadOnly();
    }
}

/// <summary>
/// 调试伤害计算记录器 - 输出到Unity控制台
/// </summary>
public class DebugDamageCalculationRecorder : IDamageCalculationRecorder
{
    private StringBuilder _sb;
    private string _soldierName;
    private TakeDamageType _damageType;
    private int _rawDamage;
    private int _finalDamage;
    private bool _isDodged;
    private DateTime _startTime;
    private bool _isRecording;
    private bool _showInRealTime;

    public DebugDamageCalculationRecorder(bool showInRealTime = false)
    {
        _showInRealTime = showInRealTime;
    }

    public void BeginRecord(string soldierName, TakeDamageType damageType, int rawDamage)
    {
        _sb = new StringBuilder();
        _soldierName = soldierName;
        _damageType = damageType;
        _rawDamage = rawDamage;
        _startTime = DateTime.Now;
        _isRecording = true;

        if (_showInRealTime)
        {
            LogTool.Log($"[伤害计算开始] {_soldierName} 受到 {_damageType} 伤害: {rawDamage}");
        }
    }

    public void RecordStep(IDamageCalculationStep step)
    {
        if (!_isRecording) return;

        if (_showInRealTime)
        {
            LogTool.Log($"[伤害步骤] {step}");
        }
    }

    public void EndRecord(int finalDamage, bool isDodged)
    {
        _finalDamage = finalDamage;
        _isDodged = isDodged;
        _isRecording = false;

        if (_showInRealTime)
        {
            var result = isDodged ? "闪避" : $"{finalDamage}";
            LogTool.Log($"[伤害计算结束] 最终结果: {result}");
        }
    }

    public string GetRecordReport()
    {
        return _showInRealTime ? "实时模式，请在控制台查看" : "请启用实时模式查看日志";
    }

    public void Clear()
    {
        _sb?.Clear();
        _isRecording = false;
    }
}

/// <summary>
/// 复合伤害计算记录器 - 可以同时使用多个记录器
/// </summary>
public class CompositeDamageCalculationRecorder : IDamageCalculationRecorder
{
    private List<IDamageCalculationRecorder> _recorders = new List<IDamageCalculationRecorder>();

    public void AddRecorder(IDamageCalculationRecorder recorder)
    {
        if (recorder != null && !_recorders.Contains(recorder))
        {
            _recorders.Add(recorder);
        }
    }

    public void RemoveRecorder(IDamageCalculationRecorder recorder)
    {
        _recorders.Remove(recorder);
    }

    public void BeginRecord(string soldierName, TakeDamageType damageType, int rawDamage)
    {
        foreach (var recorder in _recorders)
        {
            recorder.BeginRecord(soldierName, damageType, rawDamage);
        }
    }

    public void RecordStep(IDamageCalculationStep step)
    {
        foreach (var recorder in _recorders)
        {
            recorder.RecordStep(step);
        }
    }

    public void EndRecord(int finalDamage, bool isDodged)
    {
        foreach (var recorder in _recorders)
        {
            recorder.EndRecord(finalDamage, isDodged);
        }
    }

    public string GetRecordReport()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < _recorders.Count; i++)
        {
            sb.AppendLine($"=== 记录器 {i + 1} ===");
            sb.AppendLine(_recorders[i].GetRecordReport());
            sb.AppendLine();
        }
        return sb.ToString();
    }

    public void Clear()
    {
        foreach (var recorder in _recorders)
        {
            recorder.Clear();
        }
    }
}

/// <summary>
/// 空伤害计算记录器 - 不记录任何东西，用于性能优化
/// </summary>
public class NullDamageCalculationRecorder : IDamageCalculationRecorder
{
    public void BeginRecord(string soldierName, TakeDamageType damageType, int rawDamage) { }
    public void RecordStep(IDamageCalculationStep step) { }
    public void EndRecord(int finalDamage, bool isDodged) { }
    public string GetRecordReport() => "";
    public void Clear() { }
}