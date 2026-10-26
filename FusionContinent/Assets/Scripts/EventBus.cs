using System;
using System.Collections.Generic;
using Config;
using FusionContinent;
using FusionContinent.Buff;
using FusionContinent.Skill.Handler;
using UnityEngine;

public interface IGameEvent { }


public static class EventBus
{
    private static Dictionary<Type, Delegate> _eventTable = new Dictionary<Type, Delegate>();

    // 订阅
    public static void Subscribe<T>(Action<T> callback) where T : IGameEvent
    {
        var type = typeof(T);

        if (_eventTable.TryGetValue(type, out var del))
        {
            _eventTable[type] = Delegate.Combine(del, callback);
        }
        else
        {
            _eventTable[type] = callback;
        }
    }

    // 取消订阅
    public static void Unsubscribe<T>(Action<T> callback) where T : IGameEvent
    {
        var type = typeof(T);

        if (_eventTable.TryGetValue(type, out var del))
        {
            var currentDel = Delegate.Remove(del, callback);

            if (currentDel == null)
                _eventTable.Remove(type);
            else
                _eventTable[type] = currentDel;
        }
    }

    // 发布事件
    public static void Publish<T>(T e) where T : IGameEvent
    {
        var type = typeof(T);

        if (_eventTable.TryGetValue(type, out var del))
        {
            (del as Action<T>)?.Invoke(e);
        }
    }

    // 清空（切场景用）
    public static void Clear()
    {
        _eventTable.Clear();
    }
}

public struct HpDamageEvent : IGameEvent
{
    public Vector3 Position;
    public int Damage;
    public bool IsCrit;
    public TakeDamageType DamageType;
    public Transform Target;
    public MonsterCard TargetCard;
    public SoldierCard Caster;
    public bool IsExecution;
    public bool IsMiss;
    public string SfXName;
}

public struct UseSkillEvent : IGameEvent
{
    public string skillName;
    public string SfXName;
}

//子弹命中
public struct ProjectileHitEvent : IGameEvent
{
    public SkillContext skillContext;
    public Vector3 hitpoint;
    public MonsterCard monsterCard;
}

public struct ConditionsTypeEvent : IGameEvent
{
    public ConditionsType coditionsType;
    public IBuffable caster;
    public IBuffable target;
    public Vector3 direction;
    /// <summary>
    /// 英灵受击时刻（毫秒时间戳，用于 TriggerBuff 判断是否刷新叠层）
    /// </summary>
    public long heroAfterHarmedTime;
}

public struct SoldierAwakeningSkillEvent : IGameEvent
{
    public int SoldierId;
}

public struct BuffAddHuman:IGameEvent
{
    public int BuffId;
}