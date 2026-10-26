using Config;
using UnityEngine;

namespace Game.Battle
{
    /// <summary>
    /// 规则触发上下文（由战斗层在触发时传入）。
    /// </summary>
    public class BattleRuleTriggerContext
    {
        /// <summary>
        /// 当前触发时机（攻击前、受击后等）。
        /// </summary>
        public TriggerType Trigger { get; set; }

        /// <summary>
        /// 规则归属者 Id（通常是战斗单位唯一 Id）。
        /// </summary>
        public int OwnerId { get; set; }

        /// <summary>
        /// 触发源对象（例如施法者/攻击者）。
        /// </summary>
        public object Source { get; set; }

        /// <summary>
        /// 触发目标对象（例如受击者）。
        /// </summary>
        public object Target { get; set; }

        /// <summary>
        /// 触发时间戳（秒）；为空时服务层会回填 Time.time。
        /// </summary>
        public float CurrentTime { get; set; }

        /// <summary>
        /// 条件判断所需的数据读取器。
        /// </summary>
        public IBattleRuleValueProvider ValueProvider { get; set; }

        /// <summary>
        /// 效果执行器（将配置层效果落到真实战斗对象上）。
        /// </summary>
        public IBattleRuleEffectExecutor EffectExecutor { get; set; }

        /// <summary>
        /// 当前伤害计算上下文（DamagePipeline 触发规则时传入）。
        /// </summary>
        public DamageContext DamageContext { get; set; }

        /// <summary>施法时传入的攻击方向（普攻 Before 连击等施法时刻效果使用）</summary>
        public Vector3 CastDirection { get; set; }

        /// <summary>本次施法累计 AddSkillNoBuff 重复次数（多条规则叠加）。</summary>
        public int CastRepeatCount { get; set; }

        /// <summary>施法时刻 ModifyAttr（如奥术回响减伤），在全部 Before 规则结算后统一注册。</summary>
        public EffectParamStruct PendingCastDamageEffect { get; set; }

        /// <summary>命中点世界坐标（暴击后 CastSkill 等在暴击点释放）</summary>
        public Vector3 HitPosition { get; set; }

        /// <summary>同 RuleId 多次学习时的实例级来源 Key（如 Equip_实例Id_词条Id）。</summary>
        public string StackSourceKey { get; set; }

        /// <summary>持久 ModifyAttr 写入 AttributeSet 时的来源类型。</summary>
        public AttributeSourceType ModifierSourceType { get; set; }

        /// <summary>
        /// 英雄专属规则作用域（<see cref="BattleLearnedRuleEntry.ScopeHeroCardId"/>）。
        /// 技能来源 Buff 解析施法者时使用。
        /// </summary>
        public int ScopeHeroCardId { get; set; }

        public BattleRuleTriggerContext(TriggerType trigger, int ownerId)
        {
            Trigger = trigger;
            OwnerId = ownerId;
        }
    }

    /// <summary>
    /// 条件比较所需的数值提供器（由具体战斗单位实现）。
    /// </summary>
    public interface IBattleRuleValueProvider
    {
        /// <summary>
        /// 获取某单位某属性值。
        /// </summary>
        float GetAttributeValue(BattleRuleTriggerContext context, UnitType unitType, AttributeType attributeType);

        /// <summary>
        /// 获取特殊条件值（职业数量、累计计数等）。
        /// </summary>
        float GetSpecialValue(BattleRuleTriggerContext context, SpecialsType specialType);

        /// <summary>
        /// 获取比较右值（动态值）；返回 0 时会回退到配置表 ComparisonValue。
        /// </summary>
        float GetComparisonValue(BattleRuleTriggerContext context, ConditionGroupConfig conditionConfig);
    }

    /// <summary>
    /// 效果执行器（将配置效果映射到真实战斗逻辑）。
    /// </summary>
    public interface IBattleRuleEffectExecutor
    {
        /// <summary>
        /// 执行一次效果组。
        /// </summary>
        void ExecuteEffect(BattleRuleEffectExecuteInfo executeInfo);
    }

    /// <summary>
    /// 效果执行参数。
    /// </summary>
    public class BattleRuleEffectExecuteInfo
    {
        /// <summary>
        /// 当前触发的规则配置。
        /// </summary>
        public SkillRuleConfig RuleConfig { get; }

        /// <summary>
        /// 当前执行的效果组配置。
        /// </summary>
        public EffectGroupConfig EffectGroupConfig { get; }

        /// <summary>
        /// 触发上下文（source/target/owner 等）。
        /// </summary>
        public BattleRuleTriggerContext Context { get; }

        public BattleRuleEffectExecuteInfo(
            SkillRuleConfig ruleConfig,
            EffectGroupConfig effectGroupConfig,
            BattleRuleTriggerContext context)
        {
            RuleConfig = ruleConfig;
            EffectGroupConfig = effectGroupConfig;
            Context = context;
        }
    }
}
