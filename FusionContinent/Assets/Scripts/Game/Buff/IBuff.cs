using Config;
using SkillContext = FusionContinent.Skill.Handler.SkillContext;
using ValueType = Config.ValueType;

namespace FusionContinent.Buff
{
    /// <summary>
    /// Buff 生命周期接口
    /// 所有 Buff 必须实现此接口，由 BuffController 统一驱动
    /// </summary>
    public interface IBuff
    {
        /// <summary>Buff 类型标识</summary>
        BuffType Type { get; }

        /// <summary>Buff 配置/实例标识，用于同类判定</summary>
        int BuffId { get; set; }

        /// <summary>
        /// 当前层数（从 1 开始）
        /// </summary>
        int Stack { get; set; }

        /// <summary>
        /// 最大层数（叠加上限，&lt;= 0 表示不限制）
        /// </summary>
        int MaxStack { get; }

        /// <summary>
        /// 触发间隔（秒），&lt;= 0 表示不使用间隔触发，仅使用 OnTick 每帧调用
        /// </summary>
        float TickInterval { get; set; }

        /// <summary>Buff 挂载特效（可选），OnAdd 时播放，OnRemove 时回收</summary>
        string BuffVfx { get; set; }

        /// <summary>持续时间类型</summary>
        BuffDurationType DurationType { get; }

        /// <summary>Buff 数值</summary>
        float Value { get; set; }

        /// <summary>Buff 造成伤害时使用的伤害类型（由编辑器配置传入）</summary>
        TakeDamageType DamageType { get; set; }

        /// <summary>数值类型（固定值/百分比）</summary>
        ValueType ValueType { get; }

        /// <summary>Buff 释放者（可为 null）</summary>
        object Caster { get; set; }

        /// <summary>技能执行上下文（可为 null）</summary>
        SkillContext SkillContext { get; set; }

        /// <summary>
        /// Buff 是否已结束（由 Controller 判定并移除）
        /// </summary>
        bool IsFinished { get; }

        /// <summary>
        /// 添加时调用，可用于注册属性修改器、播放特效等
        /// </summary>
        void OnAdd(object owner);

        /// <summary>
        /// 每帧/每 tick 调用，dt 为距离上一帧的时间（秒）
        /// </summary>
        void OnTick(object owner, float dt);

        /// <summary>
        /// 间隔触发调用，intervalAccumulator 为距离上次触发以来的累积时间（秒）
        /// 返回 true 表示需要重置累积器并继续循环，返回 false 表示停止间隔触发
        /// TickInterval > 0 时生效
        /// </summary>
        void OnIntervalTick(object owner, float intervalAccumulator);

        /// <summary>
        /// 移除时调用，用于清理 OnAdd 中注册的资源
        /// </summary>
        void OnRemove(object owner);

        /// <summary>
        /// 层数+1 时调用，子类可重写实现叠加效果（如累加属性加成、刷新时间等）
        /// </summary>
        void OnStackUp(object owner);

        /// <summary>
        /// 达到上限时刷新持续时间，子类可重写
        /// </summary>
        void OnRefreshDuration(object owner);

        /// <summary>
        /// 来源唯一标识（用于跨 Buff 判重，同一技能的多个 Buff 应共享 SourceKey）
        /// </summary>
        string SourceKey { get; }
    }
}
