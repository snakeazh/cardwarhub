using System;
using Config;
using FusionContinent.Skill.Handler;
using UnityEngine;
using Unity.Profiling;
using ValueType = Config.ValueType;

namespace FusionContinent.Buff
{
    /// <summary>
    /// Buff 基类
    /// 管理生命周期（计时/结束判断），子类只需实现业务逻辑
    /// </summary>
    public abstract class BuffBase : IBuff
    {
        public BuffType Type { get; set; }
        public int BuffId { get; set; }

        /// <summary>当前层数（从 1 开始），叠加时 +1，达到上限则刷新时间</summary>
        public int Stack { get; set; } = 1;

        /// <summary>最大层数（叠加上限，&lt;= 0 表示不限制）</summary>
        public virtual int MaxStack { get; set; } = 1;

        /// <summary>触发间隔（秒），&lt;= 0 表示不使用间隔触发，仅使用 OnTick 每帧调用</summary>
        public float TickInterval { get; set; }

        /// <summary>Buff 挂载特效（可选），OnAdd 时播放，OnRemove 时回收</summary>
        public string BuffVfx { get; set; }

        /// <summary>当前挂载的特效对象（由系统管理）</summary>
        protected GameObject m_BuffVfxInstance;

        /// <summary>触发间隔（秒），&lt;= 0 表示不使用间隔触发，仅使用 OnTick 每帧调用</summary>
        public BuffDurationType DurationType { get; set; }
        public float Value { get; set; }
        public TakeDamageType DamageType { get; set; } = TakeDamageType.General;
        public ValueType ValueType { get; set; }
        public object Caster { get; set; }

        /// <summary>技能执行上下文（可选，用于需要 SkillId/Level 等信息的 Buff）</summary>
        public SkillContext SkillContext { get; set; }

        /// <summary>来源唯一标识（用于跨 Buff 判重）</summary>
        public string SourceKey { get; set; }

        /// <summary>Buff 持续时间（秒），-1 表示永久</summary>
        protected float Duration { get; set; }

        /// <summary>已流逝时间（秒）</summary>
        protected float Elapsed { get; set; }

        /// <summary>Buff 是否已结束</summary>
        public virtual bool IsFinished
        {
            get
            {
                if (Duration < 0) return false; // 永久
                return Elapsed >= Duration;
            }
        }

        protected BuffBase() { Elapsed = 0f; Stack = 1; }

        protected BuffBase(BuffType type, float duration, float value, ValueType valueType,
            BuffDurationType durationType = BuffDurationType.Duration,float tickInterval = 0f)
            : this()
        {
            Type = type;
            Duration = duration;
            Value = value;
            ValueType = valueType;
            DurationType = durationType;
            TickInterval = tickInterval;
        }

        protected BuffBase(BuffType type, float duration, float value, ValueType valueType,
            float probability, BuffDurationType durationType, float elapsed,float tickInterval = 0f)
            : this(type, duration, value, valueType, durationType)
        {
            Probability = probability;
            Elapsed = elapsed;
            TickInterval = tickInterval;
        }

        /// <summary>触发概率（向后兼容）</summary>
        public float Probability { get; set; }

        /// <summary>叠加时调用（由 Controller 在层数+1 时调用），子类可重写实现叠加效果</summary>
        public virtual void OnStackUp(object owner) { }

        /// <summary>刷新持续时间（由 Controller 在层数达到上限时调用）</summary>
        public virtual void OnRefreshDuration(object owner) { ResetTimer(); }

        /// <summary>重置计时器</summary>
        protected void ResetTimer() { Elapsed = 0f; }

        public virtual void OnAdd(object owner)
        {
            PlayBuffVfx(owner);
        }

        public virtual void OnTick(object owner, float dt)
        {
            Elapsed += dt;
        }

        /// <summary>
        /// 由 Controller 驱动间隔触发（累积时间达到间隔时调用）
        /// 子类重写此方法实现间隔执行逻辑
        /// 注意：intervalAccumulator 已由 Controller 的累积器维护，此处不需额外处理
        /// </summary>
        public virtual void OnIntervalTick(object owner, float intervalAccumulator)
        {
        }

        public virtual void OnRemove(object owner)
        {
            StopBuffVfx();
        }

        /// <summary>
        /// 播放挂载特效（OnAdd 时自动调用，子类可重写覆盖）
        /// </summary>
        protected virtual void PlayBuffVfx(object owner)
        {
            if (EffectManager.Instance == null) return;
            if (string.IsNullOrEmpty(BuffVfx)) return;
            if (owner is not Component comp) return;

            Transform attach = comp.transform;
            var vfxPoint = attach.Find("EffectPos");
            Transform target = vfxPoint != null ? vfxPoint : attach;

            float lifetime = Duration > 0 ? Duration : -1f;
            using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.BuffTriggerEventDispatch))
            {
                m_BuffVfxInstance = EffectManager.Instance.PlayEffect(
                    BuffVfx,
                    target.position,
                    lifetime,
                    parent: target,
                    layer: EffectLayer.Character
                );
            }
        }

        /// <summary>
        /// 停止并回收挂载特效（OnRemove 时自动调用）
        /// </summary>
        protected virtual void StopBuffVfx()
        {
            if (EffectManager.Instance == null) return;
            if (m_BuffVfxInstance == null) return;

            EffectManager.Instance.RecycleEffect(m_BuffVfxInstance);
            m_BuffVfxInstance = null;
        }

        /// <summary>
        /// 获取修正后的属性值（子类用于计算最终影响值）
        /// </summary>
        protected float GetFinalValue(float baseValue)
        {
            if (ValueType == ValueType.Percentage)
                return baseValue * Value;
            return Value; // 固定值
        }
    }
}
